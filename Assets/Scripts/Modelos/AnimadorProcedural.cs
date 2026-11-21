using System.Collections.Generic;
using UnityEngine;

namespace VoidformBioForge
{
    /// <summary>
    /// Animación procedural de personajes (humanos y guardianes) sobre un EsqueletoProcedural.
    /// No necesita clips: calcula la pose cada fotograma a partir del movimiento y del combate, y la mezcla con
    /// suavizado exponencial, así que NUNCA hay cortes bruscos ni poses congeladas.
    /// -----------------------------------------------------------------
    /// Nivel 1: ciclo de paso según la velocidad real (fase continua; caminar y correr se mezclan solos),
    ///          balanceo de brazos opuesto a las piernas, rebote de cadera y respiración en reposo.
    /// Nivel 2: inclinación al acelerar y al girar, pose en el aire (y de flotación con gravedad reducida),
    ///          aterrizaje con absorción, pose de guardia según el arma o el linaje.
    /// Nivel 3: acciones en tres tiempos (anticipación → impacto → seguimiento) para golpes, disparos, tensado del
    ///          arco, recargas y habilidades; reacción a impactos con muelle; tambaleo; capa y piezas con inercia.
    /// Los ejes siguen la convención del esqueleto: X positivo dobla rodillas y codos "hacia atrás", y X negativo
    /// adelanta muslos y brazos.
    /// </summary>
    [DefaultExecutionOrder(100)]
    public class AnimadorProcedural : MonoBehaviour
    {
        private struct Accion
        {
            public TipoAccionCombate Tipo;
            public int RanuraGuardian;
            public bool EsGuardian;
            public float Inicio;
            public float Duracion;
            public float FinAnticipacion; // 0..1
            public float Impacto;         // 0..1
        }

        private EsqueletoProcedural e;
        private ControladorMovimientoAssaultCube movimiento;
        private CharacterController controlador;
        private SistemaSaludYTraje salud;
        private SistemaCombateArquetipo combate;
        private ControladorGuardianElemental guardian;

        private readonly Dictionary<Transform, Quaternion> reposo = new Dictionary<Transform, Quaternion>();
        private Vector3 posicionReposoCadera;
        private float fase;
        private float velocidadSuave;
        private float yawAnterior;
        private float inclinacionLateral;
        private float inclinacionFrontal;
        private Vector3 velocidadAnterior;
        private float pesoAire;
        private float absorcionAterrizaje;
        private Vector2 sacudidaImpacto;
        private Vector2 velocidadSacudida;
        private Accion accion;
        private bool hayAccion;
        private float capaAngulo;
        private float capaVelocidad;

        private void Start()
        {
            e = GetComponentInChildren<EsqueletoProcedural>();
            movimiento = GetComponent<ControladorMovimientoAssaultCube>();
            controlador = GetComponent<CharacterController>();
            salud = GetComponent<SistemaSaludYTraje>();
            combate = GetComponent<SistemaCombateArquetipo>();
            guardian = GetComponent<ControladorGuardianElemental>();
            if (e == null)
            {
                enabled = false;
                return;
            }

            foreach (Transform t in new[] { e.Cadera, e.Torso, e.Cabeza, e.BrazoI, e.AntebrazoI, e.ManoI, e.BrazoD, e.AntebrazoD, e.ManoD,
                                            e.MusloI, e.PiernaI, e.PieI, e.MusloD, e.PiernaD, e.PieD, e.Capa })
            {
                if (t != null && !reposo.ContainsKey(t)) reposo[t] = t.localRotation;
            }
            if (e.Cadera != null) posicionReposoCadera = e.Cadera.localPosition;
            yawAnterior = transform.eulerAngles.y;

            if (combate != null) combate.AlAccion += ManejarAccionHumana;
            if (guardian != null)
            {
                guardian.AlIniciarAnticipacion += ManejarAnticipacionGuardian;
            }
            if (salud != null)
            {
                salud.AlRecibirDano += ManejarImpacto;
                salud.AlTambalear += ManejarTambaleo;
            }
            if (movimiento != null) movimiento.AlAterrizar += ManejarAterrizaje;
        }

        private void OnDestroy()
        {
            if (combate != null) combate.AlAccion -= ManejarAccionHumana;
            if (guardian != null) guardian.AlIniciarAnticipacion -= ManejarAnticipacionGuardian;
            if (salud != null)
            {
                salud.AlRecibirDano -= ManejarImpacto;
                salud.AlTambalear -= ManejarTambaleo;
            }
            if (movimiento != null) movimiento.AlAterrizar -= ManejarAterrizaje;
        }

        // =====================================================================
        // Eventos → acciones
        // =====================================================================

        private void ManejarAccionHumana(TipoAccionCombate tipo)
        {
            float intervalo = combate != null && combate.Arma != null ? combate.Arma.intervalo : 0.45f;
            switch (tipo)
            {
                case TipoAccionCombate.GolpeHorizontal:
                case TipoAccionCombate.GolpeDiagonal:
                case TipoAccionCombate.Estocada:
                case TipoAccionCombate.GolpeDescendente:
                    IniciarAccion(tipo, Mathf.Max(0.25f, intervalo * 1.15f), combate.Arma.momentoImpacto * 0.9f, combate.Arma.momentoImpacto);
                    break;
                case TipoAccionCombate.Disparo:
                    IniciarAccion(tipo, 0.28f, 0.02f, 0.05f);
                    break;
                case TipoAccionCombate.TensarArco:
                    IniciarAccion(tipo, 30f, 0.99f, 1f); // Se mantiene hasta soltar.
                    break;
                case TipoAccionCombate.SoltarFlecha:
                    IniciarAccion(tipo, 0.35f, 0.02f, 0.08f);
                    break;
                case TipoAccionCombate.Recarga:
                    IniciarAccion(tipo, combate.Arma.tiempoRecarga > 0f ? combate.Arma.tiempoRecarga : 1f, 0.2f, 0.5f);
                    break;
                case TipoAccionCombate.Bloqueo:
                    break;
                case TipoAccionCombate.Tambaleo:
                    break;
                default:
                    IniciarAccion(tipo, tipo == TipoAccionCombate.HabilidadLarga ? 0.9f : 0.6f, 0.35f, 0.5f);
                    break;
            }
        }

        private void ManejarAnticipacionGuardian(int ranura, float duracion)
        {
            float total = duracion + (ranura == ControladorGuardianElemental.SlotDefinitiva ? 0.8f : ranura == 0 ? 0.3f : 0.45f);
            accion = new Accion
            {
                EsGuardian = true,
                RanuraGuardian = ranura,
                Inicio = Time.time,
                Duracion = total,
                FinAnticipacion = duracion / total * 0.95f,
                Impacto = duracion / total
            };
            hayAccion = true;
        }

        private void IniciarAccion(TipoAccionCombate tipo, float duracion, float finAnticipacion, float impacto)
        {
            accion = new Accion { Tipo = tipo, Inicio = Time.time, Duracion = duracion, FinAnticipacion = finAnticipacion, Impacto = impacto };
            hayAccion = true;
        }

        private void ManejarImpacto(InformacionDano info)
        {
            Vector3 local = transform.InverseTransformDirection(transform.position - info.Punto);
            float fuerza = Mathf.Clamp(info.Cantidad / 30f, 0.2f, 1.5f) * (e.EsGuardian ? 0.5f : 1f);
            velocidadSacudida += new Vector2(Mathf.Sign(local.z == 0f ? 1f : local.z) * 60f, -Mathf.Sign(local.x == 0f ? 1f : local.x) * 40f) * fuerza;
        }

        private void ManejarTambaleo(float duracion)
        {
            velocidadSacudida += new Vector2(-150f, Random.Range(-90f, 90f));
            hayAccion = false;
        }

        private void ManejarAterrizaje(float velocidadCaida)
        {
            absorcionAterrizaje = Mathf.Clamp01(velocidadCaida / 14f);
        }

        // =====================================================================
        // Pose
        // =====================================================================

        private void LateUpdate()
        {
            if (e == null || controlador == null) return;
            float dt = Mathf.Max(Time.deltaTime, 0.0001f);
            bool muerto = salud != null && salud.EstaMuerto;

            // ---------- Datos de movimiento ----------
            Vector3 v = controlador.enabled ? controlador.velocity : Vector3.zero;
            Vector3 vPlano = new Vector3(v.x, 0f, v.z);
            float velocidad = vPlano.magnitude;
            velocidadSuave = Mathf.Lerp(velocidadSuave, velocidad, 1f - Mathf.Exp(-10f * dt));
            float velocidadReferencia = 10.5f * (e.EsGuardian ? 0.9f : 1f);
            float normal = Mathf.Clamp01(velocidadSuave / velocidadReferencia);
            bool enSuelo = movimiento == null || movimiento.EnSuelo;
            pesoAire = Mathf.MoveTowards(pesoAire, enSuelo ? 0f : 1f, dt * 6f);
            absorcionAterrizaje = Mathf.MoveTowards(absorcionAterrizaje, 0f, dt * 3f);

            // Fase del paso: frecuencia de 1,6 Hz al caminar a 2,7 Hz al correr (guardianes, más lenta y pesada).
            float frecuencia = Mathf.Lerp(1.6f, 2.7f, normal) * (e.EsGuardian ? 0.78f : 1f);
            if (velocidadSuave > 0.3f && enSuelo) fase += dt * frecuencia * Mathf.PI * 2f;
            float amplitud = Mathf.Lerp(6f, 42f, normal) * (enSuelo ? 1f : 0.3f);

            // Inclinaciones con muelles: lateral por giro y frontal por aceleración.
            float yaw = transform.eulerAngles.y;
            float velocidadGiro = Mathf.DeltaAngle(yawAnterior, yaw) / dt;
            yawAnterior = yaw;
            Vector3 aceleracion = (v - velocidadAnterior) / dt;
            velocidadAnterior = v;
            float aceleracionFrontal = Vector3.Dot(aceleracion, transform.forward);
            inclinacionLateral = Mathf.Lerp(inclinacionLateral, Mathf.Clamp(-velocidadGiro * velocidad * 0.004f, -15f, 15f), 1f - Mathf.Exp(-8f * dt));
            inclinacionFrontal = Mathf.Lerp(inclinacionFrontal, Mathf.Clamp(aceleracionFrontal * 0.35f, -8f, 10f) + normal * 7f, 1f - Mathf.Exp(-6f * dt));

            // Muelle de reacción a impactos.
            velocidadSacudida += (-sacudidaImpacto * 180f - velocidadSacudida * 16f) * dt;
            sacudidaImpacto += velocidadSacudida * dt;

            // ---------- Componentes de la pose ----------
            float paso = Mathf.Sin(fase);
            float pasoCruzado = Mathf.Sin(fase + Mathf.PI);
            Vector3 localVel = transform.InverseTransformDirection(vPlano);
            float direccion = velocidad > 0.3f && localVel.z < -0.3f ? -1f : 1f; // Caminar hacia atrás invierte el paso.

            float musloI = -paso * amplitud * direccion;
            float musloD = -pasoCruzado * amplitud * direccion;
            float rodillaI = Mathf.Max(0f, Mathf.Sin(fase - Mathf.PI * 0.5f)) * amplitud * 1.4f + 6f * normal;
            float rodillaD = Mathf.Max(0f, Mathf.Sin(fase + Mathf.PI * 0.5f)) * amplitud * 1.4f + 6f * normal;

            // En el aire: piernas recogidas (más abiertas y lentas en gravedad reducida).
            bool flotando = ControladorMovimientoAssaultCube.MultiplicadorGravedadAmbiente < 0.9f;
            musloI = Mathf.Lerp(musloI, -35f, pesoAire);
            musloD = Mathf.Lerp(musloD, flotando ? -5f : -15f, pesoAire);
            rodillaI = Mathf.Lerp(rodillaI, 55f, pesoAire);
            rodillaD = Mathf.Lerp(rodillaD, flotando ? 30f : 70f, pesoAire);
            float flexionAterrizaje = absorcionAterrizaje * 35f;

            float respiracion = Mathf.Sin(Time.time * (e.EsGuardian ? 1.1f : 1.6f)) * (1f - normal) * 2f;
            float rebote = -Mathf.Abs(Mathf.Sin(fase)) * 0.05f * normal * (e.EsGuardian ? 1.6f : 1f) - absorcionAterrizaje * 0.12f;

            // Brazos: balanceo opuesto a las piernas (reducido cuando sujetan un arma).
            bool conArma = combate != null && !e.EsGuardian;
            float balanceo = amplitud * (conArma ? 0.25f : 0.8f);
            Vector3 brazoI = new Vector3(paso * balanceo * direccion, 0f, flotando ? -25f * pesoAire : 0f);
            Vector3 brazoD = new Vector3(pasoCruzado * balanceo * direccion, 0f, flotando ? 25f * pesoAire : 0f);
            Vector3 antebrazoI = new Vector3(-10f - normal * 25f, 0f, 0f);
            Vector3 antebrazoD = new Vector3(-10f - normal * 25f, 0f, 0f);
            Vector3 torso = new Vector3(inclinacionFrontal + respiracion, 0f, inclinacionLateral);
            Vector3 cabeza = new Vector3(-inclinacionFrontal * 0.6f, 0f, -inclinacionLateral * 0.5f);

            if (e.EsGuardian)
            {
                // Postura de coloso: brazos algo abiertos y codos flexionados.
                brazoI.z -= 12f;
                brazoD.z += 12f;
                antebrazoI.x -= 15f;
                antebrazoD.x -= 15f;
            }
            else if (conArma)
            {
                AplicarGuardiaDeArma(ref brazoI, ref antebrazoI, ref brazoD, ref antebrazoD, ref torso);
            }

            // ---------- Acción en curso (anticipación → impacto → seguimiento) ----------
            if (hayAccion && !muerto)
            {
                float t = (Time.time - accion.Inicio) / accion.Duracion;
                bool soltarArco = accion.Tipo == TipoAccionCombate.TensarArco && combate != null && !combate.CargandoArco;
                if (t >= 1f || soltarArco)
                {
                    hayAccion = false;
                }
                else
                {
                    float peso;
                    float fasePose; // 0 = anticipación, 1 = impacto.
                    if (t < accion.FinAnticipacion)
                    {
                        peso = Suave(t / Mathf.Max(0.001f, accion.FinAnticipacion));
                        fasePose = 0f;
                    }
                    else if (t < accion.Impacto)
                    {
                        peso = 1f;
                        fasePose = Suave((t - accion.FinAnticipacion) / Mathf.Max(0.001f, accion.Impacto - accion.FinAnticipacion));
                    }
                    else
                    {
                        peso = 1f - Suave((t - accion.Impacto) / Mathf.Max(0.001f, 1f - accion.Impacto));
                        fasePose = 1f;
                    }
                    AplicarAccion(peso, fasePose, ref brazoI, ref antebrazoI, ref brazoD, ref antebrazoD, ref torso);
                }
            }

            // Bloqueo: el arma o el escudo se interponen.
            if (combate != null && combate.Bloqueando)
            {
                brazoI = Vector3.Lerp(brazoI, new Vector3(-80f, 0f, 20f), 0.85f);
                antebrazoI = Vector3.Lerp(antebrazoI, new Vector3(-70f, 0f, 0f), 0.85f);
                brazoD = Vector3.Lerp(brazoD, new Vector3(-70f, 0f, -15f), 0.7f);
            }

            torso += new Vector3(sacudidaImpacto.x, 0f, sacudidaImpacto.y);

            // ---------- Aplicación con suavizado (fluidez sin cortes) ----------
            float suavidad = 1f - Mathf.Exp(-(e.EsGuardian ? 14f : 20f) * dt);
            Rotar(e.MusloI, new Vector3(musloI - flexionAterrizaje, 0f, 0f), suavidad);
            Rotar(e.MusloD, new Vector3(musloD - flexionAterrizaje, 0f, 0f), suavidad);
            Rotar(e.PiernaI, new Vector3(rodillaI + flexionAterrizaje * 1.8f, 0f, 0f), suavidad);
            Rotar(e.PiernaD, new Vector3(rodillaD + flexionAterrizaje * 1.8f, 0f, 0f), suavidad);
            Rotar(e.PieI, new Vector3(-rodillaI * 0.35f - musloI * 0.3f, 0f, 0f), suavidad);
            Rotar(e.PieD, new Vector3(-rodillaD * 0.35f - musloD * 0.3f, 0f, 0f), suavidad);
            Rotar(e.Torso, torso, suavidad);
            Rotar(e.Cabeza, cabeza, suavidad);
            Rotar(e.BrazoI, brazoI, suavidad);
            Rotar(e.BrazoD, brazoD, suavidad);
            Rotar(e.AntebrazoI, antebrazoI, suavidad);
            Rotar(e.AntebrazoD, antebrazoD, suavidad);
            Rotar(e.Cadera, new Vector3(0f, Mathf.Sin(fase) * 4f * normal, Mathf.Sin(fase) * 3f * normal), suavidad);
            if (e.Cadera != null)
            {
                Vector3 objetivo = posicionReposoCadera + new Vector3(0f, rebote * e.Escala, 0f);
                e.Cadera.localPosition = Vector3.Lerp(e.Cadera.localPosition, objetivo, suavidad);
            }

            // Capa con inercia: se levanta al correr y ondea con retraso.
            if (e.Capa != null)
            {
                float objetivoCapa = -normal * 35f - pesoAire * 20f + Mathf.Sin(Time.time * 3f + fase) * 4f * normal;
                capaVelocidad += ((objetivoCapa - capaAngulo) * 60f - capaVelocidad * 9f) * dt;
                capaAngulo += capaVelocidad * dt;
                Rotar(e.Capa, new Vector3(capaAngulo, 0f, 0f), 1f);
            }
        }

        private static float Suave(float x)
        {
            x = Mathf.Clamp01(x);
            return x * x * (3f - 2f * x);
        }

        private void Rotar(Transform hueso, Vector3 euler, float suavidad)
        {
            if (hueso == null || !reposo.TryGetValue(hueso, out Quaternion r)) return;
            Quaternion objetivo = r * Quaternion.Euler(euler);
            hueso.localRotation = Quaternion.Slerp(hueso.localRotation, objetivo, suavidad);
        }

        // =====================================================================
        // Guardias por arma
        // =====================================================================

        private void AplicarGuardiaDeArma(ref Vector3 brazoI, ref Vector3 antebrazoI, ref Vector3 brazoD, ref Vector3 antebrazoD, ref Vector3 torso)
        {
            if (combate.Arma == null) return;
            bool apuntando = combate.Apuntando;
            switch (combate.Arma.tipo)
            {
                case TipoArmaModular.ArcoCompuesto:
                    // Brazo izquierdo sostiene el arco al frente; el derecho, junto a la cuerda.
                    brazoI += new Vector3(apuntando ? -85f : -55f, 0f, 10f);
                    antebrazoI += new Vector3(apuntando ? 5f : -10f, 0f, 0f);
                    brazoD += new Vector3(-60f, 0f, -20f);
                    antebrazoD += new Vector3(-70f, 0f, 0f);
                    torso += new Vector3(0f, apuntando ? 25f : 10f, 0f);
                    break;
                case TipoArmaModular.RifleLargo:
                    brazoD += new Vector3(apuntando ? -70f : -45f, 0f, -12f);
                    antebrazoD += new Vector3(-55f, 0f, 0f);
                    brazoI += new Vector3(apuntando ? -85f : -60f, 0f, 25f);
                    antebrazoI += new Vector3(-25f, 0f, 0f);
                    torso += new Vector3(apuntando ? 2f : 6f, 8f, 0f);
                    break;
                case TipoArmaModular.MazaEscudo:
                    brazoI += new Vector3(-45f, 0f, 15f);
                    antebrazoI += new Vector3(-75f, 0f, 0f);
                    brazoD += new Vector3(-30f, 0f, -10f);
                    antebrazoD += new Vector3(-80f, 0f, 0f);
                    break;
                case TipoArmaModular.ArsenalBiomecanico:
                    brazoI += new Vector3(-40f, 0f, 12f);
                    antebrazoI += new Vector3(-80f, 0f, 0f);
                    brazoD += new Vector3(-40f, 0f, -12f);
                    antebrazoD += new Vector3(-80f, 0f, 0f);
                    break;
                default:
                    // Mandoble a dos manos, hoja alta en guardia.
                    brazoD += new Vector3(-35f, 0f, -15f);
                    antebrazoD += new Vector3(-70f, 0f, 0f);
                    brazoI += new Vector3(-40f, 0f, 30f);
                    antebrazoI += new Vector3(-70f, 0f, 0f);
                    torso += new Vector3(0f, -10f, 0f);
                    break;
            }
        }

        // =====================================================================
        // Acciones
        // =====================================================================

        private void AplicarAccion(float peso, float fasePose, ref Vector3 brazoI, ref Vector3 antebrazoI, ref Vector3 brazoD, ref Vector3 antebrazoD, ref Vector3 torso)
        {
            Vector3 bI = Vector3.zero, aI = Vector3.zero, bD = Vector3.zero, aD = Vector3.zero, tr = Vector3.zero;

            if (accion.EsGuardian)
            {
                switch (accion.RanuraGuardian)
                {
                    case ControladorGuardianElemental.SlotAtaqueBasico:
                        // Puño: el brazo derecho se carga atrás y golpea hacia delante y abajo.
                        bD = Vector3.Lerp(new Vector3(30f, 0f, -20f), new Vector3(-95f, 0f, 0f), fasePose);
                        aD = Vector3.Lerp(new Vector3(-90f, 0f, 0f), new Vector3(-10f, 0f, 0f), fasePose);
                        tr = Vector3.Lerp(new Vector3(-6f, 25f, 0f), new Vector3(12f, -25f, 0f), fasePose);
                        break;
                    case ControladorGuardianElemental.SlotDefinitiva:
                        // Definitiva: alza ambos brazos y golpea el suelo con todo el peso.
                        bI = Vector3.Lerp(new Vector3(-165f, 0f, -20f), new Vector3(-50f, 0f, -10f), fasePose);
                        bD = Vector3.Lerp(new Vector3(-165f, 0f, 20f), new Vector3(-50f, 0f, 10f), fasePose);
                        aI = Vector3.Lerp(new Vector3(-20f, 0f, 0f), new Vector3(-10f, 0f, 0f), fasePose);
                        aD = aI;
                        tr = Vector3.Lerp(new Vector3(-18f, 0f, 0f), new Vector3(35f, 0f, 0f), fasePose);
                        break;
                    default:
                        // Habilidades: brazos abiertos (anticipación) y empuje hacia delante (ejecución).
                        bI = Vector3.Lerp(new Vector3(-40f, 0f, -60f), new Vector3(-80f, 0f, -10f), fasePose);
                        bD = Vector3.Lerp(new Vector3(-40f, 0f, 60f), new Vector3(-80f, 0f, 10f), fasePose);
                        aI = new Vector3(-30f, 0f, 0f);
                        aD = aI;
                        tr = Vector3.Lerp(new Vector3(-10f, 0f, 0f), new Vector3(15f, 0f, 0f), fasePose);
                        break;
                }
            }
            else
            {
                switch (accion.Tipo)
                {
                    case TipoAccionCombate.GolpeHorizontal:
                        bD = Vector3.Lerp(new Vector3(-80f, 0f, -75f), new Vector3(-80f, 0f, 30f), fasePose);
                        aD = new Vector3(-30f, 0f, 0f);
                        tr = Vector3.Lerp(new Vector3(0f, 40f, 0f), new Vector3(5f, -45f, 0f), fasePose);
                        break;
                    case TipoAccionCombate.GolpeDiagonal:
                        bD = Vector3.Lerp(new Vector3(-150f, 0f, -40f), new Vector3(-40f, 0f, 25f), fasePose);
                        aD = Vector3.Lerp(new Vector3(-40f, 0f, 0f), new Vector3(-15f, 0f, 0f), fasePose);
                        tr = Vector3.Lerp(new Vector3(-8f, 30f, 0f), new Vector3(15f, -30f, 0f), fasePose);
                        break;
                    case TipoAccionCombate.Estocada:
                        bD = Vector3.Lerp(new Vector3(-30f, 0f, -10f), new Vector3(-90f, 0f, 0f), fasePose);
                        aD = Vector3.Lerp(new Vector3(-100f, 0f, 0f), new Vector3(0f, 0f, 0f), fasePose);
                        tr = Vector3.Lerp(new Vector3(-5f, 20f, 0f), new Vector3(18f, -10f, 0f), fasePose);
                        break;
                    case TipoAccionCombate.GolpeDescendente:
                        bD = Vector3.Lerp(new Vector3(-170f, 0f, -10f), new Vector3(-50f, 0f, 0f), fasePose);
                        aD = Vector3.Lerp(new Vector3(-30f, 0f, 0f), new Vector3(-5f, 0f, 0f), fasePose);
                        tr = Vector3.Lerp(new Vector3(-12f, 10f, 0f), new Vector3(25f, -5f, 0f), fasePose);
                        break;
                    case TipoAccionCombate.Disparo:
                    case TipoAccionCombate.SoltarFlecha:
                        // Retroceso: hombro atrás y cañón arriba durante un instante.
                        bD = new Vector3(14f * (1f - fasePose * 0.5f), 0f, 0f);
                        tr = new Vector3(-5f, 0f, 0f);
                        break;
                    case TipoAccionCombate.TensarArco:
                        bD = new Vector3(-20f * fasePose, 0f, -45f);
                        aD = new Vector3(-40f, 0f, 0f);
                        tr = new Vector3(0f, 15f, 0f);
                        break;
                    case TipoAccionCombate.Recarga:
                        bI = new Vector3(25f, 0f, 10f);
                        aI = new Vector3(-50f, 0f, 0f);
                        tr = new Vector3(10f, 0f, 0f);
                        break;
                    case TipoAccionCombate.HabilidadLarga:
                        bI = Vector3.Lerp(new Vector3(-150f, 0f, -15f), new Vector3(-90f, 0f, 0f), fasePose);
                        bD = Vector3.Lerp(new Vector3(-150f, 0f, 15f), new Vector3(-90f, 0f, 0f), fasePose);
                        tr = Vector3.Lerp(new Vector3(-12f, 0f, 0f), new Vector3(10f, 0f, 0f), fasePose);
                        break;
                    default:
                        bD = Vector3.Lerp(new Vector3(-20f, 0f, -40f), new Vector3(-95f, 0f, 0f), fasePose);
                        bI = Vector3.Lerp(new Vector3(-20f, 0f, 40f), new Vector3(-70f, 0f, 0f), fasePose);
                        tr = Vector3.Lerp(new Vector3(-8f, 15f, 0f), new Vector3(12f, -10f, 0f), fasePose);
                        break;
                }
            }

            brazoI = Vector3.Lerp(brazoI, brazoI + bI, peso);
            antebrazoI = Vector3.Lerp(antebrazoI, antebrazoI + aI, peso);
            brazoD = Vector3.Lerp(brazoD, brazoD + bD, peso);
            antebrazoD = Vector3.Lerp(antebrazoD, antebrazoD + aD, peso);
            torso = Vector3.Lerp(torso, torso + tr, peso);
        }
    }
}
