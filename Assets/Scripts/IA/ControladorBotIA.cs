using System;
using System.Collections.Generic;
using UnityEngine;
using Random = UnityEngine.Random;

namespace VoidformBioForge
{
    /// <summary>Objetivo táctico general que recibe cada bot según el modo de juego.</summary>
    public enum ObjetivoBot
    {
        CazarEnemigos,
        CapturarBases,
        ExtraerFuente,
        DefenderFuente
    }

    /// <summary>
    /// Inteligencia artificial de los bots/mods que rellenan equipos (Destructores) o que actúan como
    /// guardianes/jefes. Escribe en EntradaPersonaje exactamente igual que un jugador humano, así que
    /// usa las mismas reglas de movimiento, armas, maná y enfriamientos (no hace trampas).
    /// -----------------------------------------------------------------
    /// Nivel 1: buscar al enemigo vivo más cercano y moverse hacia él.
    /// Nivel 2: percepción con línea de visión, tiempo de reacción, error de puntería según la
    ///          dificultad, disparo, recarga automática y ataque cuerpo a cuerpo de guardianes.
    /// Nivel 3: objetivos por modo (capturar bases, extraer o defender la fuente), esquiva lateral,
    ///          evasión de obstáculos y desatasco, uso táctico de habilidades respetando el maná
    ///          y, para Aethernum, rotación al elemento que más daño hace a los trajes enemigos.
    /// </summary>
    [DefaultExecutionOrder(-100)]
    [RequireComponent(typeof(EntradaPersonaje))]
    [RequireComponent(typeof(ControladorMovimientoAssaultCube))]
    [RequireComponent(typeof(SistemaSaludYTraje))]
    public class ControladorBotIA : MonoBehaviour
    {
        [Header("1. Percepción")]
        [SerializeField] private float radioDeteccion = 48f;
        [SerializeField] private float intervaloReevaluacion = 0.3f;

        [Header("2. Dificultad (0 = fácil, 1 = experto)")]
        [SerializeField] [Range(0f, 1f)] private float dificultad = 0.5f;

        [Header("3. Objetivo Táctico")]
        [SerializeField] private ObjetivoBot objetivo = ObjetivoBot.CazarEnemigos;

        private EntradaPersonaje entrada;
        private ControladorMovimientoAssaultCube movimiento;
        private SistemaSaludYTraje salud;
        private SistemaCombateArquetipo armas;
        private bool soltandoFlecha;
        private ControladorGuardianElemental guardian;
        private SistemaEstadosAlterados estados;

        private SistemaSaludYTraje enemigoActual;
        private bool enemigoVisible;
        private float momentoAdquisicion;
        private float proximaReevaluacion;
        private Vector3 destinoActual;
        private Vector3 puntoFuente;
        private ZonaCapturaBase baseObjetivo;
        private float faseEsquiva;
        private float tiempoAtascado;
        private float desvioHasta;
        private Vector3 direccionDesvio;
        private float proximaHabilidad;
        private float proximaRotacionElemento;
        private Vector3 errorPunteriaActual;
        private float proximoCambioError;

        public ObjetivoBot Objetivo => objetivo;

        private float TiempoReaccion => Mathf.Lerp(0.65f, 0.15f, dificultad);
        private float ErrorPunteriaGrados => Mathf.Lerp(5f, 0.8f, dificultad);
        private float VelocidadGiro => Mathf.Lerp(220f, 620f, dificultad);

        private void Awake()
        {
            entrada = GetComponent<EntradaPersonaje>();
            movimiento = GetComponent<ControladorMovimientoAssaultCube>();
            salud = GetComponent<SistemaSaludYTraje>();
            armas = GetComponent<SistemaCombateArquetipo>();
            guardian = GetComponent<ControladorGuardianElemental>();
            estados = GetComponent<SistemaEstadosAlterados>();
            faseEsquiva = Random.Range(0f, 10f);
        }

        public void Configurar(ObjetivoBot nuevoObjetivo, float nuevaDificultad, Vector3 posicionFuente)
        {
            objetivo = nuevoObjetivo;
            dificultad = Mathf.Clamp01(nuevaDificultad);
            puntoFuente = posicionFuente;
        }

        private void Update()
        {
            if (salud.EstaMuerto || !entrada.EntradaHabilitada || entrada.BloqueadaPorTambaleo)
            {
                entrada.LimpiarTodo();
                enemigoActual = null;
                return;
            }

            if (armas == null) armas = GetComponent<SistemaCombateArquetipo>();
            if (guardian == null) guardian = GetComponent<ControladorGuardianElemental>();

            if (Time.time >= proximaReevaluacion)
            {
                proximaReevaluacion = Time.time + intervaloReevaluacion;
                ReevaluarEnemigo();
                destinoActual = CalcularDestino();
            }

            ActualizarErrorPunteria();
            ProcesarMovimiento();
            ProcesarCombate();
        }

        // =====================================================================
        // Nivel 1-2: percepción
        // =====================================================================

        private void ReevaluarEnemigo()
        {
            SistemaSaludYTraje mejor = null;
            float mejorPuntuacion = float.MaxValue;
            bool mejorVisible = false;

            foreach (SistemaSaludYTraje entidad in SistemaSaludYTraje.Entidades)
            {
                if (entidad == null || entidad.EstaMuerto || entidad.Bando == salud.Bando) continue;
                float distancia = Vector3.Distance(transform.position, entidad.transform.position);
                if (distancia > radioDeteccion * 1.5f) continue;

                // Camuflaje de fibras: a más de 12 m no se le percibe. Las marcas (faro, visor, enlace) lo delatan siempre.
                if (!entidad.EstaRevelado && SistemaCombateArquetipo.EstaOcultoPara(entidad, distancia)) continue;
                bool visible = entidad.EstaRevelado || (distancia <= radioDeteccion && TieneLineaDeVision(entidad));
                // Se prefiere a los visibles; entre ellos, al más cercano. Los guardianes que defienden
                // la fuente priorizan a quien está extrayendo.
                float puntuacion = distancia + (visible ? 0f : 40f);
                if (objetivo == ObjetivoBot.DefenderFuente && Vector3.Distance(entidad.transform.position, puntoFuente) < 9f) puntuacion -= 25f;

                if (puntuacion < mejorPuntuacion)
                {
                    mejorPuntuacion = puntuacion;
                    mejor = entidad;
                    mejorVisible = visible;
                }
            }

            if (mejor != enemigoActual || (mejorVisible && !enemigoVisible))
            {
                momentoAdquisicion = Time.time;
            }
            enemigoActual = mejor;
            enemigoVisible = mejorVisible;
        }

        private bool TieneLineaDeVision(SistemaSaludYTraje objetivoVision)
        {
            Vector3 origen = movimiento.PivoteCamara != null ? movimiento.PivoteCamara.position : transform.position + Vector3.up * 1.6f;
            Vector3 destino = objetivoVision.transform.position + Vector3.up * 1.2f;
            Vector3 direccion = destino - origen;
            if (Physics.Raycast(origen, direccion.normalized, out RaycastHit impacto, direccion.magnitude, ~0, QueryTriggerInteraction.Ignore))
            {
                SistemaSaludYTraje golpeado = impacto.collider.GetComponentInParent<SistemaSaludYTraje>();
                return golpeado == objetivoVision;
            }
            return true;
        }

        private Vector3 PuntoApuntado(SistemaSaludYTraje objetivoApuntado)
        {
            float altura = objetivoApuntado.GetComponent<ControladorGuardianElemental>() != null ? 1.7f : 1.25f;
            return objetivoApuntado.transform.position + Vector3.up * altura;
        }

        private void ActualizarErrorPunteria()
        {
            if (Time.time < proximoCambioError) return;
            proximoCambioError = Time.time + Random.Range(0.25f, 0.6f);
            errorPunteriaActual = Random.insideUnitSphere * Mathf.Tan(ErrorPunteriaGrados * Mathf.Deg2Rad);
        }

        // =====================================================================
        // Nivel 3: objetivos por modo
        // =====================================================================

        private Vector3 CalcularDestino()
        {
            bool esGuardian = guardian != null;

            if (enemigoActual != null && enemigoVisible)
            {
                float distancia = Vector3.Distance(transform.position, enemigoActual.transform.position);
                if (objetivo == ObjetivoBot.CazarEnemigos || objetivo == ObjetivoBot.DefenderFuente || distancia < 16f || esGuardian)
                {
                    if (esGuardian) return DestinoGuardianPorRol(distancia);
                    // Un humano mantiene distancia media (ritmo de arena shooter: no se lanza a quemarropa).
                    Vector3 alejamiento = (transform.position - enemigoActual.transform.position);
                    alejamiento.y = 0f;
                    // Distancia ideal según el arma: cuerpo a cuerpo se pega; arco y rifle mantienen la distancia.
                    float distanciaIdeal = armas != null && armas.Arma != null ? armas.Arma.DistanciaIdealIA : 14f;
                    return enemigoActual.transform.position + alejamiento.normalized * distanciaIdeal;
                }
            }

            switch (objetivo)
            {
                case ObjetivoBot.CapturarBases:
                    baseObjetivo = ElegirBaseObjetivo();
                    if (baseObjetivo != null)
                    {
                        // Cada bot elige un punto distinto dentro de la base para no amontonarse.
                        Vector2 desplazamiento = new Vector2(Mathf.Sin(faseEsquiva), Mathf.Cos(faseEsquiva)) * 3f;
                        return baseObjetivo.transform.position + new Vector3(desplazamiento.x, 0f, desplazamiento.y);
                    }
                    break;

                case ObjetivoBot.ExtraerFuente:
                    return puntoFuente + new Vector3(Mathf.Sin(faseEsquiva) * 3.5f, 0f, Mathf.Cos(faseEsquiva) * 3.5f);

                case ObjetivoBot.DefenderFuente:
                    if (enemigoActual != null) return enemigoActual.transform.position;
                    return puntoFuente;
            }

            if (enemigoActual != null) return enemigoActual.transform.position;
            return puntoFuente;
        }

        /// <summary>
        /// Coordinación por roles entre guardianes (Historia: 3 o 5 contra 4; o 2 contra 4):
        /// • Vanguardia: entra al cuerpo a cuerpo abriéndose hacia un flanco para no taponar a sus aliados.
        /// • Control: se queda entre el enemigo y la fuente, a media distancia (aprox. el radio de sus habilidades).
        /// • Artillería: mantiene la distancia larga y se desplaza en arco para buscar ángulo.
        /// </summary>
        private Vector3 DestinoGuardianPorRol(float distancia)
        {
            Vector3 enemigo = enemigoActual.transform.position;
            Vector3 desdeEnemigo = transform.position - enemigo;
            desdeEnemigo.y = 0f;
            if (desdeEnemigo.sqrMagnitude < 0.01f) desdeEnemigo = -transform.forward;
            desdeEnemigo.Normalize();
            Vector3 lateral = Vector3.Cross(Vector3.up, desdeEnemigo) * (Mathf.Sin(faseEsquiva) >= 0f ? 1f : -1f);

            switch (guardian.Rol)
            {
                case RolGuardian.Vanguardia:
                    return enemigo + lateral * 1.5f;

                case RolGuardian.Control:
                {
                    float ideal = Mathf.Max(guardian.AlcanceAtaqueBasico, guardian.RadioEfecto * 0.7f);
                    Vector3 haciaFuente = puntoFuente - enemigo;
                    haciaFuente.y = 0f;
                    Vector3 direccion = haciaFuente.sqrMagnitude > 1f ? haciaFuente.normalized : desdeEnemigo;
                    return enemigo + direccion * ideal;
                }

                default:
                {
                    float ideal = guardian.RadioEfecto * 0.85f;
                    Vector3 arco = Quaternion.Euler(0f, Mathf.Sin(Time.time * 0.3f + faseEsquiva) * 40f, 0f) * desdeEnemigo;
                    return enemigo + arco * ideal;
                }
            }
        }

        private ZonaCapturaBase ElegirBaseObjetivo()
        {
            EstadoControlBase propia = salud.Bando == BandoJuego.Destructor ? EstadoControlBase.DominadaDestructores : EstadoControlBase.DominadaCuidadores;
            ZonaCapturaBase mejor = null;
            float mejorDistancia = float.MaxValue;

            foreach (ZonaCapturaBase b in ZonaCapturaBase.BasesActivas)
            {
                if (b == null || b.EstadoActual == propia) continue;
                // Pequeño sesgo por bot para repartir al equipo entre varias bases.
                float d = Vector3.Distance(transform.position, b.transform.position) + Mathf.Abs(Mathf.Sin(faseEsquiva + b.IndiceBase)) * 18f;
                if (d < mejorDistancia)
                {
                    mejorDistancia = d;
                    mejor = b;
                }
            }

            if (mejor == null && ZonaCapturaBase.BasesActivas.Count > 0)
            {
                // Todas son nuestras: defender la que esté en disputa o la central.
                foreach (ZonaCapturaBase b in ZonaCapturaBase.BasesActivas)
                {
                    if (b != null && b.SiendoDisputada) return b;
                }
                return ZonaCapturaBase.BasesActivas[Mathf.Abs((int)faseEsquiva) % ZonaCapturaBase.BasesActivas.Count];
            }
            return mejor;
        }

        // =====================================================================
        // Nivel 2-3: movimiento con evasión de obstáculos
        // =====================================================================

        private void ProcesarMovimiento()
        {
            Vector3 haciaDestino = destinoActual - transform.position;
            haciaDestino.y = 0f;
            float distancia = haciaDestino.magnitude;
            Vector3 direccionMundo = distancia > 1.2f ? haciaDestino / distancia : Vector3.zero;

            // Esquiva lateral mientras combate (más intensa con mayor dificultad).
            if (enemigoActual != null && enemigoVisible)
            {
                float esquiva = Mathf.Sin(Time.time * 1.7f + faseEsquiva) * Mathf.Lerp(0.3f, 0.85f, dificultad);
                direccionMundo += transform.right * esquiva;
                if (direccionMundo.sqrMagnitude > 1f) direccionMundo.Normalize();
            }

            if (Time.time < desvioHasta)
            {
                direccionMundo = direccionDesvio;
            }
            else if (direccionMundo.sqrMagnitude > 0.01f)
            {
                direccionMundo = EvitarObstaculos(direccionMundo);
            }

            // Detección de atasco: quiere moverse pero apenas avanza.
            if (direccionMundo.sqrMagnitude > 0.25f && movimiento.VelocidadActual < 1f && !movimiento.EstaEnraizado)
            {
                tiempoAtascado += Time.deltaTime;
                if (tiempoAtascado > 1.1f)
                {
                    tiempoAtascado = 0f;
                    desvioHasta = Time.time + 1f;
                    direccionDesvio = Quaternion.Euler(0f, Random.Range(0, 2) == 0 ? 100f : -100f, 0f) * direccionMundo;
                    entrada.SaltoPresionado = true;
                }
            }
            else
            {
                tiempoAtascado = 0f;
            }

            Vector3 local = transform.InverseTransformDirection(direccionMundo);
            entrada.Movimiento = Vector2.ClampMagnitude(new Vector2(local.x, local.z), 1f);

            // Orientación: al enemigo si está visible, si no hacia donde camina.
            if (enemigoActual != null && enemigoVisible)
            {
                Vector3 punto = PuntoApuntado(enemigoActual);
                float distanciaObjetivo = Vector3.Distance(transform.position, punto);
                movimiento.MirarHaciaPunto(punto + errorPunteriaActual * distanciaObjetivo, VelocidadGiro);
            }
            else if (direccionMundo.sqrMagnitude > 0.01f)
            {
                movimiento.MirarHaciaPunto(transform.position + direccionMundo * 10f + Vector3.up * 1.5f, VelocidadGiro * 0.6f);
            }
        }

        private Vector3 EvitarObstaculos(Vector3 direccion)
        {
            Vector3 origenBajo = transform.position + Vector3.up * 0.6f;
            Vector3 origenAlto = transform.position + Vector3.up * 1.7f;
            const float distanciaSondeo = 2.6f;

            if (!Physics.SphereCast(origenBajo, 0.35f, direccion, out RaycastHit choque, distanciaSondeo, ~0, QueryTriggerInteraction.Ignore)
                || choque.collider.GetComponentInParent<SistemaSaludYTraje>() != null)
            {
                return direccion;
            }

            // Obstáculo bajo (tronco caído, roca): saltar por encima.
            if (!Physics.Raycast(origenAlto, direccion, distanciaSondeo, ~0, QueryTriggerInteraction.Ignore))
            {
                entrada.SaltoPresionado = true;
                return direccion;
            }

            float[] angulos = { 45f, -45f, 90f, -90f, 135f, -135f };
            foreach (float angulo in angulos)
            {
                Vector3 alternativa = Quaternion.Euler(0f, angulo, 0f) * direccion;
                if (!Physics.SphereCast(origenBajo, 0.35f, alternativa, out _, distanciaSondeo, ~0, QueryTriggerInteraction.Ignore))
                {
                    return alternativa;
                }
            }
            return -direccion;
        }

        // =====================================================================
        // Nivel 2-3: combate y habilidades
        // =====================================================================

        private void ProcesarCombate()
        {
            entrada.AtaquePrincipalMantenido = false;
            entrada.AtaqueSecundarioMantenido = false;

            bool puedeReaccionar = enemigoActual != null && enemigoVisible && Time.time - momentoAdquisicion >= TiempoReaccion;
            if (!puedeReaccionar)
            {
                UsarHabilidadesDefensivas();
                return;
            }

            float distancia = Vector3.Distance(transform.position, enemigoActual.transform.position);
            Vector3 adelante = movimiento.PivoteCamara != null ? movimiento.PivoteCamara.forward : transform.forward;
            float angulo = Vector3.Angle(adelante, PuntoApuntado(enemigoActual) - (movimiento.PivoteCamara != null ? movimiento.PivoteCamara.position : transform.position));

            if (armas != null)
            {
                ProcesarAtaqueHumano(distancia, angulo);
                UsarHabilidadesDestructor(distancia);
            }
            else if (guardian != null)
            {
                if (distancia <= guardian.AlcanceAtaqueBasico + 1.2f && angulo < 50f)
                {
                    entrada.AtaquePrincipalPresionado = true;
                }
                UsarHabilidadesGuardian();
            }
        }

        private void UsarHabilidadesDefensivas()
        {
            if (Time.time < proximaHabilidad) return;
            if (armas != null) UsarHabilidadesDestructor(enemigoActual != null ? Vector3.Distance(transform.position, enemigoActual.transform.position) : 999f);
            else if (guardian != null && salud.PorcentajeVida < 0.5f && guardian.PuedeLanzar(ControladorGuardianElemental.SlotHabilidad2)
                     && (guardian.ElementoActual == TipoElemento.Arbol || guardian.ElementoActual == TipoElemento.Tierra))
            {
                entrada.Habilidad2Presionada = true;
                proximaHabilidad = Time.time + 1f;
            }
        }

        /// <summary>Ataque básico según el arma: combos a corta distancia, arco con carga y rifle con mira.</summary>
        private void ProcesarAtaqueHumano(float distancia, float angulo)
        {
            DatosArmaModular arma = armas.Arma;
            if (arma == null) return;
            bool alineado = angulo < 6f + ErrorPunteriaGrados;
            switch (arma.tipo)
            {
                case TipoArmaModular.ArcoCompuesto:
                    // Mantiene el clic para tensar y lo suelta (un fotograma) al llegar a la carga completa.
                    if (soltandoFlecha)
                    {
                        soltandoFlecha = false;
                        break;
                    }
                    if (armas.CargandoArco && armas.CargaArco >= Mathf.Lerp(0.6f, 1f, dificultad) && alineado)
                    {
                        soltandoFlecha = true;
                        break;
                    }
                    entrada.AtaquePrincipalMantenido = distancia <= arma.alcance;
                    entrada.AtaqueSecundarioMantenido = distancia > 15f;
                    break;
                case TipoArmaModular.RifleLargo:
                    entrada.AtaqueSecundarioMantenido = distancia > 18f;
                    if (alineado && distancia <= arma.alcance)
                    {
                        entrada.AtaquePrincipalMantenido = true;
                        entrada.AtaquePrincipalPresionado = true;
                    }
                    break;
                default:
                    if (distancia <= arma.alcance + 1.2f && angulo < 45f)
                    {
                        entrada.AtaquePrincipalMantenido = true;
                        entrada.AtaquePrincipalPresionado = true;
                    }
                    else if (arma.tipo == TipoArmaModular.ArsenalBiomecanico && distancia < 25f && alineado)
                    {
                        entrada.AtaqueSecundarioMantenido = true; // Cañón de pulso a media distancia.
                    }
                    else if (arma.bloqueoFrontal > 0f && distancia < 9f && enemigoActual != null)
                    {
                        // Si el guardián está cargando algo cerca, se cubre.
                        ControladorGuardianElemental g = enemigoActual.GetComponent<ControladorGuardianElemental>();
                        entrada.AtaqueSecundarioMantenido = g != null && g.EstaAnticipando;
                    }
                    break;
            }
        }

        /// <summary>Uso táctico de las 6 habilidades humanas (3 de armadura y 3 de arma).</summary>
        private void UsarHabilidadesDestructor(float distanciaEnemigo)
        {
            if (Time.time < proximaHabilidad || armas.Arquetipo == null || armas.Arma == null) return;
            proximaHabilidad = Time.time + Mathf.Lerp(1.4f, 0.5f, dificultad);

            bool enraizado = estados != null && estados.TieneEstado(TipoEstadoAlterado.Enraizado);
            bool ralentizado = estados != null && estados.TieneEstado(TipoEstadoAlterado.Ralentizado);
            ControladorGuardianElemental g = enemigoActual != null ? enemigoActual.GetComponent<ControladorGuardianElemental>() : null;
            bool guardianCargando = g != null && g.EstaAnticipando && distanciaEnemigo < 14f;
            bool visible = enemigoActual != null && enemigoVisible;

            // ---- Armadura (Z X C) ----
            switch (armas.Arquetipo.tipo)
            {
                case TipoArquetipo.Cazador:
                    if (Libre(0) && (enraizado || guardianCargando || (distanciaEnemigo < 4f && armas.Arma.DistanciaIdealIA > 5f))) { entrada.HabilidadArmaduraCorta = true; return; }
                    if (Libre(1) && objetivo == ObjetivoBot.ExtraerFuente && distanciaEnemigo > 15f) { entrada.HabilidadArmaduraMedia = true; return; }
                    if (Libre(2) && visible && distanciaEnemigo > 22f) { entrada.HabilidadArmaduraLarga = true; return; }
                    break;
                case TipoArquetipo.Paladin:
                    if (Libre(0) && (distanciaEnemigo < 4.5f || enraizado || ralentizado)) { entrada.HabilidadArmaduraCorta = true; return; }
                    if (Libre(1) && guardianCargando) { entrada.HabilidadArmaduraMedia = true; return; }
                    if (Libre(2) && visible && distanciaEnemigo < 30f) { entrada.HabilidadArmaduraLarga = true; return; }
                    break;
                case TipoArquetipo.Bastion:
                    if (Libre(0) && (distanciaEnemigo < 4f || guardianCargando)) { entrada.HabilidadArmaduraCorta = true; return; }
                    if (Libre(1) && visible && distanciaEnemigo < 10f) { entrada.HabilidadArmaduraMedia = true; return; }
                    if (Libre(2) && AliadoEnApuros()) { entrada.HabilidadArmaduraLarga = true; return; }
                    break;
                case TipoArquetipo.Tirador:
                    if (Libre(0) && distanciaEnemigo < 4f) { entrada.HabilidadArmaduraCorta = true; return; }
                    if (Libre(1) && visible && distanciaEnemigo < 40f) { entrada.HabilidadArmaduraMedia = true; return; }
                    if (Libre(2) && visible) { entrada.HabilidadArmaduraLarga = true; return; }
                    break;
                default:
                    if (Libre(0) && distanciaEnemigo < 5f) { entrada.HabilidadArmaduraCorta = true; return; }
                    if (Libre(1) && (enraizado || ralentizado || guardianCargando)) { entrada.HabilidadArmaduraMedia = true; return; }
                    if (Libre(2) && visible && distanciaEnemigo < 40f) { entrada.HabilidadArmaduraLarga = true; return; }
                    break;
            }

            // ---- Arma (E Q F) ----
            if (!visible) return;
            switch (armas.Arma.tipo)
            {
                case TipoArmaModular.ArcoCompuesto:
                    if (Libre(3) && distanciaEnemigo < 3f) entrada.Habilidad1Presionada = true;
                    else if (Libre(4) && distanciaEnemigo < 20f) entrada.HabilidadEspecialPresionada = true;
                    else if (Libre(5) && distanciaEnemigo > 15f) entrada.CambiarElementoPresionado = true;
                    break;
                case TipoArmaModular.Mandoble:
                    if (Libre(3) && distanciaEnemigo < 3f) entrada.Habilidad1Presionada = true;
                    else if (Libre(4) && distanciaEnemigo < 15f) entrada.HabilidadEspecialPresionada = true;
                    else if (Libre(5) && distanciaEnemigo > 5f && distanciaEnemigo < 12f) entrada.CambiarElementoPresionado = true;
                    break;
                case TipoArmaModular.MazaEscudo:
                    if (Libre(3) && (distanciaEnemigo < 3.5f || guardianCargando)) entrada.Habilidad1Presionada = true;
                    else if (Libre(4) && distanciaEnemigo > 4f && distanciaEnemigo < 12f) entrada.HabilidadEspecialPresionada = true;
                    else if (Libre(5) && guardianCargando) entrada.CambiarElementoPresionado = true;
                    break;
                case TipoArmaModular.RifleLargo:
                    if (Libre(3) && distanciaEnemigo < 6f) entrada.Habilidad1Presionada = true;
                    else if (Libre(4) && guardianCargando) entrada.HabilidadEspecialPresionada = true;
                    else if (Libre(5) && distanciaEnemigo > 10f) entrada.CambiarElementoPresionado = true;
                    break;
                default:
                    if (Libre(3) && distanciaEnemigo < 2.5f) entrada.Habilidad1Presionada = true;
                    else if (Libre(4) && distanciaEnemigo > 6f && distanciaEnemigo < 18f) entrada.HabilidadEspecialPresionada = true;
                    else if (Libre(5) && distanciaEnemigo > 12f) entrada.CambiarElementoPresionado = true;
                    break;
            }
        }

        private bool Libre(int ranura) => armas.ObtenerEnfriamiento(ranura) <= 0f;

        private bool AliadoEnApuros()
        {
            foreach (SistemaSaludYTraje a in SistemaSaludYTraje.Entidades)
            {
                if (a == null || a == salud || a.EstaMuerto || a.Bando != salud.Bando) continue;
                if (a.PorcentajeVida < 0.4f && Vector3.Distance(a.transform.position, transform.position) < 25f) return true;
            }
            return false;
        }

        private void UsarHabilidadesGuardian()
        {
            if (Time.time < proximaHabilidad) return;

            int enemigosCerca = 0;
            int enemigosAmplio = 0;
            foreach (SistemaSaludYTraje e in SistemaSaludYTraje.Entidades)
            {
                if (e == null || e.EstaMuerto || e.Bando == salud.Bando) continue;
                float d = Vector3.Distance(transform.position, e.transform.position);
                if (d <= guardian.RadioEfecto * 0.85f) enemigosCerca++;
                if (d <= guardian.RadioEfecto * 1.4f) enemigosAmplio++;
            }

            if (guardian.PuedeRotarElementos && Time.time >= proximaRotacionElemento)
            {
                proximaRotacionElemento = Time.time + Random.Range(10f, 16f);
                TipoElemento ideal = ElementoMasEficaz();
                if (ideal != guardian.ElementoActual)
                {
                    entrada.CambiarElementoPresionado = true;
                    proximaHabilidad = Time.time + 0.5f;
                    return;
                }
            }

            if (enemigosAmplio >= 2 && guardian.PuedeLanzar(ControladorGuardianElemental.SlotDefinitiva))
            {
                entrada.HabilidadEspecialPresionada = true;
            }
            else if (enemigosCerca >= 1 && guardian.PuedeLanzar(ControladorGuardianElemental.SlotHabilidad1))
            {
                entrada.Habilidad1Presionada = true;
            }
            else if (guardian.PuedeLanzar(ControladorGuardianElemental.SlotHabilidad2))
            {
                bool curativo = guardian.ElementoActual == TipoElemento.Arbol;
                if ((curativo && salud.PorcentajeVida < 0.7f) || (!curativo && enemigosCerca >= 1))
                {
                    entrada.Habilidad2Presionada = true;
                }
            }
            proximaHabilidad = Time.time + Mathf.Lerp(1.2f, 0.45f, dificultad);
        }

        /// <summary>Elige el elemento que más daño hace a los trajes enemigos presentes.</summary>
        private TipoElemento ElementoMasEficaz()
        {
            TipoElemento[] opciones = UtilidadesTipos.ElementosPrimarios;
            float[] puntuacion = new float[opciones.Length];
            foreach (SistemaSaludYTraje e in SistemaSaludYTraje.Entidades)
            {
                if (e == null || e.EstaMuerto || e.Bando == salud.Bando || e.Arquetipo == null) continue;
                for (int i = 0; i < opciones.Length; i++)
                {
                    puntuacion[i] += e.Arquetipo.ObtenerMultiplicador(UtilidadesTipos.ElementoATipoDano(opciones[i]));
                }
            }

            int actual = Array.IndexOf(opciones, guardian.ElementoActual);
            int mejor = actual < 0 ? 0 : actual;
            for (int i = 0; i < opciones.Length; i++)
            {
                if (puntuacion[i] > puntuacion[mejor] + 0.01f) mejor = i;
            }
            // La rotación avanza de uno en uno: si el ideal no es el siguiente, se pulsa igual y se
            // acerca en la próxima evaluación (el ciclo completo siempre llega al ideal).
            return opciones[mejor];
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(transform.position, radioDeteccion);
            Gizmos.color = Color.cyan;
            Gizmos.DrawLine(transform.position, destinoActual);
        }
    }
}
