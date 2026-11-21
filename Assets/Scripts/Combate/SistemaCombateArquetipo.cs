using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Random = UnityEngine.Random;

namespace VoidformBioForge
{
    /// <summary>
    /// Combate de los humanos: arquetipo (armadura + 3 habilidades Z/X/C) y arma modular (ataque básico + 3 habilidades E/Q/F).
    /// -----------------------------------------------------------------
    /// Nivel 1: ataque básico de cada arma. Combos cuerpo a cuerpo con anticipación e impacto retardado, arco con
    ///          carga y flecha balística, rifle de rayo con cargador y recarga, cañón de pulso con calor.
    /// Nivel 2: bloqueo frontal (espada y escudo), mira con zoom, dispersión en movimiento, enfriamientos de las
    ///          6 habilidades y modificadores temporales (camuflaje, estabilizador, disipador, sobrecarga, absorción).
    /// Nivel 3: las 30 habilidades del Doc 09 (15 de armadura y 15 de arma), críticos en puntos débiles, aguante,
    ///          barreras, marcas, interceptación, cargas con anticipación que el tambaleo cancela, y eventos para el
    ///          HUD y el animador procedural. Funciona igual para el jugador y para los bots (lee EntradaPersonaje).
    /// </summary>
    [RequireComponent(typeof(EntradaPersonaje))]
    public class SistemaCombateArquetipo : MonoBehaviour
    {
        public const int RanurasHabilidad = 6; // 0-2 armadura (Z X C), 3-5 arma (E Q F)

        private static readonly List<SistemaCombateArquetipo> combatientes = new List<SistemaCombateArquetipo>();

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void Reiniciar() => combatientes.Clear();

        private DatosArquetipo arquetipo;
        private DatosArmaModular arma;

        private EntradaPersonaje entrada;
        private ControladorMovimientoAssaultCube movimiento;
        private SistemaSaludYTraje salud;
        private SistemaEstadosAlterados estados;

        private readonly float[] enfriamientos = new float[RanurasHabilidad];
        private readonly float[] totales = new float[RanurasHabilidad];

        private int municion;
        private int reserva;
        private bool recargando;
        private float finRecarga;
        private float proximoAtaque;
        private int indiceCombo;
        private float finVentanaCombo;
        private bool cargandoArco;
        private float inicioCarga;
        private float ultimoDisparoCanon = -10f;
        private bool habilidadCargando;

        private float camufladoHasta = -1f;
        private float estabilizadorHasta = -1f;
        private float disipadorHasta = -1f;
        private float sobrecargaHasta = -1f;
        private bool pulsoSobrecargaPendiente;
        private float absorcionHasta = -1f;
        private float absorbido;
        private float visorHasta = -1f;
        private bool siguienteArticulacion;
        private bool siguientePerforante;

        public DatosArquetipo Arquetipo => arquetipo;
        public DatosArmaModular Arma => arma;
        public bool Bloqueando { get; private set; }
        public bool Apuntando { get; private set; }
        public bool Recargando => recargando;
        public bool CargandoArco => cargandoArco;
        public float CargaArco => cargandoArco && arma != null && arma.tiempoCarga > 0f ? Mathf.Clamp01((Time.time - inicioCarga) / arma.tiempoCarga) : 0f;
        public bool Camuflado => Time.time < camufladoHasta;
        public float FactorZoom => Apuntando && arma != null ? arma.zoomMira : 1f;
        public int Municion => municion;

        public event Action<string, float> AlCambiarRecurso;
        public event Action<int, float, float> AlCambiarEnfriamiento;
        public event Action<string> AlActivarHabilidad;
        public event Action<bool, bool> AlConfirmarImpacto;
        public event Action<TipoAccionCombate> AlAccion;
        public event Action<DatosArmaModular> AlCambiarArma;
        public event Action<string> AlAviso;

        /// <summary>¿Puede la IA rival percibir a este humano a esta distancia? (camuflaje de fibras).</summary>
        public static bool EstaOcultoPara(SistemaSaludYTraje objetivo, float distancia)
        {
            if (objetivo == null || distancia <= 12f) return false;
            SistemaCombateArquetipo c = objetivo.GetComponent<SistemaCombateArquetipo>();
            return c != null && c.Camuflado;
        }

        private void Awake()
        {
            entrada = GetComponent<EntradaPersonaje>();
            movimiento = GetComponent<ControladorMovimientoAssaultCube>();
            salud = GetComponent<SistemaSaludYTraje>();
            estados = GetComponent<SistemaEstadosAlterados>();
        }

        private void OnEnable()
        {
            if (!combatientes.Contains(this)) combatientes.Add(this);
            if (salud != null)
            {
                salud.AlRevivir += ManejarReaparicion;
                salud.AlRecibirDano += ManejarDanoRecibido;
                salud.AlTambalear += ManejarTambaleo;
                salud.FiltroDanoEntrante = FiltrarDano;
            }
        }

        private void OnDisable()
        {
            combatientes.Remove(this);
            if (salud != null)
            {
                salud.AlRevivir -= ManejarReaparicion;
                salud.AlRecibirDano -= ManejarDanoRecibido;
                salud.AlTambalear -= ManejarTambaleo;
                if (salud.FiltroDanoEntrante == (Func<InformacionDano, InformacionDano>)FiltrarDano) salud.FiltroDanoEntrante = null;
            }
        }

        /// <summary>Equipa arquetipo y arma (cualquier combinación es válida).</summary>
        public void Configurar(DatosArquetipo nuevoArquetipo, DatosArmaModular nuevaArma)
        {
            arquetipo = nuevoArquetipo != null ? nuevoArquetipo : DatosArquetipo.CrearPorDefecto(TipoArquetipo.Cazador);
            arma = nuevaArma != null ? nuevaArma : DatosArmaModular.CrearPorDefecto(TipoArmaModular.Mandoble);
            for (int i = 0; i < 3; i++)
            {
                totales[i] = arquetipo.ObtenerHabilidad(i).Enfriamiento;
                totales[i + 3] = arma.ObtenerHabilidad(i).Enfriamiento;
                enfriamientos[i] = 0f;
                enfriamientos[i + 3] = 0f;
            }
            movimiento?.EstablecerMultiplicadorTraje(arquetipo.multiplicadorVelocidad);
            RellenarMunicion();
            ForzarNotificacionCompleta();
        }

        private void RellenarMunicion()
        {
            municion = arma.cargador;
            reserva = arma.reservaMaxima;
            recargando = false;
        }

        public float ObtenerEnfriamiento(int ranura) => enfriamientos[Mathf.Clamp(ranura, 0, RanurasHabilidad - 1)];

        public string NombreRanura(int ranura)
        {
            if (arquetipo == null || arma == null) return "";
            return ranura < 3 ? arquetipo.ObtenerHabilidad(ranura).Nombre : arma.ObtenerHabilidad(ranura - 3).Nombre;
        }

        public void ForzarNotificacionCompleta()
        {
            AlCambiarArma?.Invoke(arma);
            for (int i = 0; i < RanurasHabilidad; i++) AlCambiarEnfriamiento?.Invoke(i, enfriamientos[i], totales[i]);
            NotificarRecurso();
        }

        private void ManejarReaparicion(SistemaSaludYTraje _)
        {
            RellenarMunicion();
            for (int i = 0; i < RanurasHabilidad; i++) enfriamientos[i] = 0f;
            cargandoArco = false;
            habilidadCargando = false;
            camufladoHasta = estabilizadorHasta = disipadorHasta = sobrecargaHasta = absorcionHasta = visorHasta = -1f;
            ForzarNotificacionCompleta();
        }

        private void ManejarDanoRecibido(InformacionDano _)
        {
            camufladoHasta = -1f; // Recibir daño rompe el camuflaje.
        }

        private void ManejarTambaleo(float _)
        {
            cargandoArco = false;
            AlAccion?.Invoke(TipoAccionCombate.Tambaleo);
        }

        // =====================================================================
        // Bucle principal
        // =====================================================================

        private void Update()
        {
            if (arma == null) return;
            ActualizarEnfriamientos();
            ActualizarRecarga();
            ActualizarModificadoresTemporales();

            if (salud != null && salud.EstaMuerto) return;
            if (!entrada.EntradaHabilitada || entrada.BloqueadaPorTambaleo)
            {
                Bloqueando = false;
                Apuntando = false;
                cargandoArco = false;
                return;
            }

            ProcesarSecundario();
            ProcesarAtaqueBasico();

            if (entrada.RecargarPresionado) IniciarRecarga();

            if (!habilidadCargando)
            {
                if (entrada.HabilidadArmaduraCorta) IntentarHabilidad(0);
                if (entrada.HabilidadArmaduraMedia) IntentarHabilidad(1);
                if (entrada.HabilidadArmaduraLarga) IntentarHabilidad(2);
                if (entrada.HabilidadArmaCorta) IntentarHabilidad(3);
                if (entrada.HabilidadArmaMedia) IntentarHabilidad(4);
                if (entrada.HabilidadArmaLarga) IntentarHabilidad(5);
            }

            // Velocidad: bloquear, apuntar con mira y estabilizar ralentizan.
            float multiplicador = 1f;
            if (Bloqueando) multiplicador *= 0.6f;
            if (Apuntando && arma.tipo == TipoArmaModular.RifleLargo) multiplicador *= 0.7f;
            if (Time.time < estabilizadorHasta) multiplicador *= 0.4f;
            if (cargandoArco) multiplicador *= 0.75f;
            movimiento?.EstablecerMultiplicadorVelocidad(multiplicador);
        }

        private void ActualizarEnfriamientos()
        {
            for (int i = 0; i < RanurasHabilidad; i++)
            {
                if (enfriamientos[i] <= 0f) continue;
                enfriamientos[i] = Mathf.Max(0f, enfriamientos[i] - Time.deltaTime);
                AlCambiarEnfriamiento?.Invoke(i, enfriamientos[i], totales[i]);
            }
        }

        private void ActualizarRecarga()
        {
            // Cañón de pulso: el calor se disipa solo si no se dispara durante 0,5 s.
            if (arma.tipo == TipoArmaModular.ArsenalBiomecanico && !recargando && municion < arma.cargador && Time.time - ultimoDisparoCanon > 0.5f)
            {
                municion = Mathf.Min(arma.cargador, municion + Mathf.Max(1, Mathf.RoundToInt(12f * Time.deltaTime + Random.value * 0.5f)));
                NotificarRecurso();
            }

            if (!recargando || Time.time < finRecarga) return;
            recargando = false;
            if (arma.tipo == TipoArmaModular.ArsenalBiomecanico)
            {
                municion = arma.cargador;
            }
            else
            {
                int transferidas = Mathf.Min(arma.cargador - municion, reserva);
                municion += transferidas;
                reserva -= transferidas;
            }
            NotificarRecurso();
        }

        private void ActualizarModificadoresTemporales()
        {
            if (pulsoSobrecargaPendiente && Time.time >= sobrecargaHasta)
            {
                // Fin de la Sobrecarga del núcleo: pulso de 3,5 m y 2 s de sobrecalentamiento.
                pulsoSobrecargaPendiente = false;
                foreach (SistemaSaludYTraje e in EnemigosEnRadio(transform.position, 3.5f)) Danar(e, 25f, TipoDano.Explosivo, "Sobrecarga del núcleo", e.transform.position, 20f, false);
                FabricaEfectosVisuales.CrearAnilloOnda(transform.position + Vector3.up * 0.2f, 3.5f, arquetipo.colorAcento);
                salud?.AplicarMultiplicadorTemporal(1.15f, 2f);
            }

            if (absorcionHasta > 0f && Time.time >= absorcionHasta)
            {
                // Fin de la Absorción de impacto: descarga frontal con el 40% de lo absorbido.
                absorcionHasta = -1f;
                float descarga = absorbido * 0.4f;
                absorbido = 0f;
                if (descarga > 1f)
                {
                    foreach (SistemaSaludYTraje e in EnemigosEnCono(6f, 45f)) Danar(e, descarga, TipoDano.Cinetico, "Descarga del escudo", e.transform.position, descarga * 0.6f, false);
                    FabricaEfectosVisuales.CrearEfectoElemental(TipoElemento.Tierra, transform.position + transform.forward * 2f, 3f);
                    AlActivarHabilidad?.Invoke($"Descarga del escudo: {Mathf.RoundToInt(descarga)} de daño");
                }
            }
        }

        // =====================================================================
        // Nivel 1-2: ataque básico, secundario y recarga
        // =====================================================================

        private void ProcesarSecundario()
        {
            bool secundario = entrada.AtaqueSecundarioMantenido;
            bool bloqueabaAntes = Bloqueando;
            Bloqueando = secundario && arma.bloqueoFrontal > 0f && Time.time >= proximoAtaque - arma.intervalo * 0.3f;
            Apuntando = secundario && (arma.tipo == TipoArmaModular.ArcoCompuesto || arma.tipo == TipoArmaModular.RifleLargo);
            if (Bloqueando && !bloqueabaAntes) AlAccion?.Invoke(TipoAccionCombate.Bloqueo);

            // Arsenal: el clic derecho dispara el cañón de pulso.
            if (secundario && arma.tipo == TipoArmaModular.ArsenalBiomecanico && !recargando && Time.time >= proximoAtaque)
            {
                if (municion <= 0)
                {
                    IniciarRecarga();
                    return;
                }
                proximoAtaque = Time.time + 1f / (6f * VelocidadAtaque());
                if (Time.time >= disipadorHasta) municion--;
                ultimoDisparoCanon = Time.time;
                DispararRayo(12f, 60f, arma.dispersion, 1, "Cañón de pulso", arma.colorEnergia, 6f);
                AlAccion?.Invoke(TipoAccionCombate.Disparo);
                NotificarRecurso();
            }
        }

        private float VelocidadAtaque()
        {
            float v = Time.time < sobrecargaHasta ? 1.2f : 1f;
            if (Time.time < disipadorHasta) v *= 1.2f;
            return v;
        }

        private void ProcesarAtaqueBasico()
        {
            switch (arma.modo)
            {
                case ModoAtaqueArma.CuerpoACuerpo:
                    if (Bloqueando) return;
                    if ((entrada.AtaquePrincipalPresionado || entrada.AtaquePrincipalMantenido) && Time.time >= proximoAtaque)
                    {
                        IniciarGolpe();
                    }
                    break;

                case ModoAtaqueArma.Proyectil:
                    ProcesarArco();
                    break;

                default:
                    if ((entrada.AtaquePrincipalPresionado || entrada.AtaquePrincipalMantenido) && Time.time >= proximoAtaque && !recargando)
                    {
                        if (municion <= 0)
                        {
                            IniciarRecarga();
                            return;
                        }
                        DispararRifle();
                    }
                    break;
            }
        }

        private void IniciarGolpe()
        {
            if (Time.time > finVentanaCombo) indiceCombo = 0;
            int golpe = indiceCombo;
            indiceCombo = (indiceCombo + 1) % Mathf.Max(1, arma.golpesCombo);

            float intervalo = arma.intervalo / VelocidadAtaque();
            proximoAtaque = Time.time + intervalo;
            finVentanaCombo = Time.time + intervalo + 0.45f;

            bool ultimo = golpe == arma.golpesCombo - 1 && arma.golpesCombo > 1;
            TipoAccionCombate accion = arma.tipo == TipoArmaModular.MazaEscudo ? TipoAccionCombate.GolpeDescendente
                : ultimo && arma.tipo == TipoArmaModular.Mandoble ? TipoAccionCombate.Estocada
                : golpe % 2 == 0 ? TipoAccionCombate.GolpeHorizontal : TipoAccionCombate.GolpeDiagonal;
            AlAccion?.Invoke(accion);
            camufladoHasta = -1f;

            float multiplicador = (ultimo ? 1.35f : 1f) * (Time.time < sobrecargaHasta ? 1.3f : 1f);
            StartCoroutine(RutinaImpactoGolpe(intervalo * arma.momentoImpacto, multiplicador, ultimo));
        }

        private IEnumerator RutinaImpactoGolpe(float espera, float multiplicador, bool ultimo)
        {
            yield return new WaitForSeconds(espera);
            if (salud != null && salud.EstaMuerto) yield break;
            if (entrada.BloqueadaPorTambaleo) yield break;

            bool impacto = false;
            bool baja = false;
            bool critico = false;
            Vector3 puntoGolpe = transform.position + Vector3.up * 1.3f + transform.forward * (arma.alcance * 0.7f);
            foreach (SistemaSaludYTraje e in EnemigosEnCono(arma.alcance + 0.6f, arma.anguloGolpe))
            {
                float mult = PuntoDebil.EvaluarPunto(e, puntoGolpe, out bool esCritico);
                Danar(e, arma.dano * multiplicador * mult, TipoDano.Cinetico, arma.nombre, puntoGolpe, arma.impactoAguante * (ultimo ? 1.6f : 1f), esCritico);
                impacto = true;
                critico |= esCritico;
                if (e.EstaMuerto) baja = true;
            }

            if (impacto)
            {
                // Pausa de impacto: sensación de peso en cada golpe que conecta.
                SacudidaCamara.SacudirDesde(puntoGolpe, ultimo ? 0.12f : 0.06f, 0.12f, 8f);
                FabricaEfectosVisuales.CrearImpacto(puntoGolpe, -transform.forward, arma.colorEnergia);
                AlConfirmarImpacto?.Invoke(baja, critico);
            }
        }

        private void ProcesarArco()
        {
            if (municion <= 0 && reserva > 0 && !recargando)
            {
                int pasar = Mathf.Min(arma.cargador, reserva);
                municion += pasar;
                reserva -= pasar;
                NotificarRecurso();
            }

            if (!cargandoArco)
            {
                if (entrada.AtaquePrincipalMantenido && Time.time >= proximoAtaque && municion > 0)
                {
                    cargandoArco = true;
                    inicioCarga = Time.time;
                    AlAccion?.Invoke(TipoAccionCombate.TensarArco);
                }
                return;
            }

            if (entrada.AtaquePrincipalMantenido) return;

            // Soltar: la carga decide velocidad y daño (mínimo un 30% para no "regalar" flechas).
            float carga = Mathf.Max(0.3f, CargaArco);
            cargandoArco = false;
            proximoAtaque = Time.time + arma.intervalo;
            municion--;
            camufladoHasta = -1f;
            Transform origen = OrigenDisparo();
            Vector3 direccion = AplicarDispersion(origen.forward, DispersionActual());
            float danoFinal = Mathf.Lerp(25f, arma.dano, carga) * BonoDistanciaEstabilizador(30f);
            ProyectilBalistico.Lanzar(salud, origen.position + origen.forward * 0.6f, direccion, Mathf.Lerp(30f, arma.velocidadProyectil, carga),
                arma.gravedadProyectil, arma.alcance, danoFinal, TipoDano.Cinetico, arma.nombre, arma.colorEnergia, FormaProyectil.Flecha,
                1, 0f, CriticoBonus(), arma.impactoAguante, null, null, (b, c) => AlConfirmarImpacto?.Invoke(b, c));
            AlAccion?.Invoke(TipoAccionCombate.SoltarFlecha);
            NotificarRecurso();
        }

        private void DispararRifle()
        {
            proximoAtaque = Time.time + arma.intervalo / VelocidadAtaque();
            if (Time.time >= disipadorHasta) municion--;
            camufladoHasta = -1f;

            float dano = arma.dano;
            int perforaciones = 1;
            string origenNombre = arma.nombre;
            if (siguientePerforante)
            {
                siguientePerforante = false;
                dano = 120f;
                perforaciones = 3;
                origenNombre = "Calibre perforante";
            }

            bool articulacion = siguienteArticulacion;
            siguienteArticulacion = false;
            DispararRayo(dano, arma.alcance, DispersionActual(), perforaciones, origenNombre, arma.colorEnergia, arma.impactoAguante,
                articulacion ? (Action<SistemaSaludYTraje>)AplicarTiroArticulacion : null);
            movimiento?.AplicarRetroceso(Apuntando ? 2.5f : 4f);
            AlAccion?.Invoke(TipoAccionCombate.Disparo);
            NotificarRecurso();
            if (municion <= 0) IniciarRecarga();
        }

        private void AplicarTiroArticulacion(SistemaSaludYTraje objetivo)
        {
            objetivo.GetComponent<SistemaEstadosAlterados>()?.AplicarRalentizacion(0.6f, 3f);
            ControladorGuardianElemental g = objetivo.GetComponent<ControladorGuardianElemental>();
            if (g != null && g.CancelarAnticipacion("Tiro a articulación")) AlAviso?.Invoke("¡Habilidad del guardián cancelada!");
        }

        public void IniciarRecarga()
        {
            if (recargando || arma == null) return;
            if (arma.tipo == TipoArmaModular.ArsenalBiomecanico)
            {
                if (municion >= arma.cargador) return;
            }
            else if (!arma.UsaMunicion || arma.tipo == TipoArmaModular.ArcoCompuesto || municion >= arma.cargador || reserva <= 0)
            {
                return;
            }
            recargando = true;
            finRecarga = Time.time + arma.tiempoRecarga;
            AlAccion?.Invoke(TipoAccionCombate.Recarga);
            NotificarRecurso();
        }

        private void NotificarRecurso()
        {
            if (arma == null) return;
            string texto;
            float porcentaje;
            switch (arma.tipo)
            {
                case TipoArmaModular.ArcoCompuesto:
                    texto = $"{municion}<size=26> flechas · {reserva}</size>";
                    porcentaje = arma.cargador > 0 ? municion / (float)arma.cargador : 0f;
                    break;
                case TipoArmaModular.RifleLargo:
                    texto = recargando ? "RECARGANDO" : $"{municion}<size=26> / {arma.cargador} · {reserva}</size>";
                    porcentaje = municion / (float)arma.cargador;
                    break;
                case TipoArmaModular.ArsenalBiomecanico:
                    texto = recargando ? "VENTILANDO" : $"Cañón {Mathf.RoundToInt(100f * municion / arma.cargador)}%";
                    porcentaje = municion / (float)arma.cargador;
                    break;
                default:
                    texto = arma.tipo == TipoArmaModular.MazaEscudo ? "MAZA Y ESCUDO" : "ACERO";
                    porcentaje = 1f;
                    break;
            }
            AlCambiarRecurso?.Invoke(texto, porcentaje);
        }

        // =====================================================================
        // Nivel 3: habilidades
        // =====================================================================

        private void IntentarHabilidad(int ranura)
        {
            if (enfriamientos[ranura] > 0f) return;
            bool ejecutada = ranura < 3 ? HabilidadArmadura(ranura) : HabilidadArma(ranura - 3);
            if (!ejecutada) return;

            enfriamientos[ranura] = totales[ranura];
            AlCambiarEnfriamiento?.Invoke(ranura, enfriamientos[ranura], totales[ranura]);
            AlActivarHabilidad?.Invoke(NombreRanura(ranura));
            AlAccion?.Invoke(ranura % 3 == 0 ? TipoAccionCombate.HabilidadCorta : ranura % 3 == 1 ? TipoAccionCombate.HabilidadMedia : TipoAccionCombate.HabilidadLarga);
            if (ranura != 1 || arquetipo.tipo != TipoArquetipo.Cazador) camufladoHasta = -1f;
        }

        private bool HabilidadArmadura(int indice)
        {
            Vector3 pos = transform.position;
            Color c = arquetipo.colorAcento;
            switch (arquetipo.tipo)
            {
                case TipoArquetipo.Cazador:
                    if (indice == 0)
                    {
                        Vector3 dir = DireccionMovimientoODetras();
                        movimiento?.AplicarImpulso(dir * 9.5f + Vector3.up * 1.5f);
                        estados?.EstablecerInmunidad(TipoEstadoAlterado.Enraizado, 0.6f);
                        estados?.EstablecerInmunidad(TipoEstadoAlterado.Ralentizado, 0.6f);
                        FabricaEfectosVisuales.CrearEfectoElemental(TipoElemento.Arbol, pos, 2f);
                    }
                    else if (indice == 1)
                    {
                        camufladoHasta = Time.time + 6f;
                        FabricaEfectosVisuales.CrearAnilloOnda(pos + Vector3.up * 0.1f, 2f, new Color(0.3f, 0.6f, 0.35f));
                    }
                    else
                    {
                        estabilizadorHasta = Time.time + 5f;
                    }
                    return true;

                case TipoArquetipo.Paladin:
                    if (indice == 0)
                    {
                        foreach (SistemaSaludYTraje e in EnemigosEnRadio(pos, 4.5f))
                        {
                            Danar(e, 10f, TipoDano.Cinetico, "Aura de repulsión", e.transform.position, 30f, false);
                            e.GetComponent<SistemaEstadosAlterados>()?.AplicarEmpuje(DireccionDesde(pos, e.transform.position) * 10f + Vector3.up * 2f);
                        }
                        estados?.LimpiarTodos();
                        FabricaEfectosVisuales.CrearAnilloOnda(pos + Vector3.up * 0.2f, 4.5f, c);
                        LuzEfimera.Crear(pos + Vector3.up, c, 5f, 9f, 0.4f);
                    }
                    else if (indice == 1)
                    {
                        BarreraEnergia.Crear(pos + transform.forward * 2f, transform.rotation, new Vector2(3f, 2.5f), 4f, 0.3f, salud, c);
                    }
                    else
                    {
                        Vector3 punto = PuntoApuntado(30f);
                        FaroVanguardia.Crear(punto, salud, c);
                    }
                    return true;

                case TipoArquetipo.Bastion:
                    if (indice == 0)
                    {
                        foreach (SistemaSaludYTraje e in EnemigosEnRadio(pos, 4f)) e.GetComponent<SistemaEstadosAlterados>()?.AplicarRalentizacion(0.6f, 2f);
                        estados?.EstablecerInmunidad(TipoEstadoAlterado.Empujado, 4f);
                        estados?.EstablecerInmunidad(TipoEstadoAlterado.Enraizado, 4f);
                        estados?.EstablecerInmunidad(TipoEstadoAlterado.Ralentizado, 4f);
                        FabricaEfectosVisuales.CrearEfectoElemental(TipoElemento.Tierra, pos, 4f);
                        SacudidaCamara.SacudirDesde(pos, 0.2f, 0.3f, 12f);
                    }
                    else if (indice == 1)
                    {
                        absorcionHasta = Time.time + 3f;
                        absorbido = 0f;
                        FabricaEfectosVisuales.CrearAnilloOnda(pos + transform.forward + Vector3.up * 1.2f, 1.5f, c);
                    }
                    else
                    {
                        SistemaSaludYTraje aliado = AliadoMasAmenazado(25f);
                        if (aliado == null)
                        {
                            AlAviso?.Invoke("No hay ningún aliado a menos de 25 m.");
                            return false;
                        }
                        Vector3 hacia = aliado.transform.position - pos;
                        movimiento?.AplicarImpulso(new Vector3(hacia.x, 0f, hacia.z).normalized * Mathf.Sqrt(2f * 9f * Mathf.Max(1f, hacia.magnitude - 2f)) + Vector3.up * 5f);
                        aliado.EstablecerProtector(salud, 0.5f, 4f);
                        FabricaEfectosVisuales.CrearTrazador(pos + Vector3.up, aliado.transform.position + Vector3.up, c);
                    }
                    return true;

                case TipoArquetipo.Tirador:
                    if (indice == 0)
                    {
                        Transform o = OrigenDisparo();
                        foreach (SistemaSaludYTraje e in EnemigosEnCono(2.5f, 40f)) Danar(e, 15f, TipoDano.Explosivo, "Propulsión de retroceso", e.transform.position, 20f, false);
                        Vector3 atras = -new Vector3(o.forward.x, 0f, o.forward.z).normalized;
                        movimiento?.AplicarImpulso(atras * 11f + Vector3.up * 5f);
                        FabricaEfectosVisuales.CrearDestelloDisparo(o.position + o.forward, o.forward, new Color(1f, 0.7f, 0.3f));
                    }
                    else if (indice == 1)
                    {
                        disipadorHasta = Time.time + 5f;
                        municion = Mathf.Max(municion, arma.cargador);
                        NotificarRecurso();
                    }
                    else
                    {
                        visorHasta = Time.time + 6f;
                        foreach (SistemaSaludYTraje e in EnemigosEnRadio(pos, 60f)) e.Revelar(6f);
                    }
                    return true;

                default:
                    if (indice == 0)
                    {
                        sobrecargaHasta = Time.time + 4f;
                        pulsoSobrecargaPendiente = true;
                        LuzEfimera.Crear(pos + Vector3.up * 1.3f, c, 4f, 6f, 0.5f);
                    }
                    else if (indice == 1)
                    {
                        foreach (SistemaSaludYTraje a in AliadosEnRadio(pos, 3f))
                        {
                            SistemaEstadosAlterados est = a.GetComponent<SistemaEstadosAlterados>();
                            est?.EstablecerInmunidad(TipoEstadoAlterado.Enraizado, 3f);
                            est?.EstablecerInmunidad(TipoEstadoAlterado.Ralentizado, 3f);
                            est?.EstablecerInmunidad(TipoEstadoAlterado.Empujado, 3f);
                            a.AplicarReduccionElemental(0.4f, 3f);
                        }
                        FabricaEfectosVisuales.CrearAnilloOnda(pos + Vector3.up * 1f, 3f, c);
                    }
                    else
                    {
                        SistemaSaludYTraje objetivo = EnemigoEnMira(40f, 15f);
                        if (objetivo == null)
                        {
                            AlAviso?.Invoke("Apunta a un guardián a menos de 40 m.");
                            return false;
                        }
                        objetivo.Revelar(8f);
                        objetivo.AplicarMultiplicadorTemporal(1.1f, 8f);
                        FabricaEfectosVisuales.CrearTrazador(OrigenDisparo().position, objetivo.transform.position + Vector3.up * 1.5f, c);
                    }
                    return true;
            }
        }

        private bool HabilidadArma(int indice)
        {
            Transform o = OrigenDisparo();
            Vector3 pos = transform.position;
            Color c = arma.colorEnergia;
            switch (arma.tipo)
            {
                case TipoArmaModular.ArcoCompuesto:
                    if (indice == 0)
                    {
                        foreach (SistemaSaludYTraje e in EnemigosEnCono(3f, 35f))
                        {
                            Danar(e, 40f, TipoDano.Cinetico, "Disparo a quemarropa", e.transform.position + Vector3.up, 25f, false);
                            e.GetComponent<SistemaEstadosAlterados>()?.AplicarEmpuje(transform.forward * 8f);
                        }
                        movimiento?.AplicarImpulso(-transform.forward * 8.5f + Vector3.up * 3f);
                        FabricaEfectosVisuales.CrearDestelloDisparo(o.position + o.forward, o.forward, c);
                    }
                    else if (indice == 1)
                    {
                        ProyectilBalistico.Lanzar(salud, o.position + o.forward * 0.6f, o.forward, 60f, 2f, 20f, 15f, TipoDano.Cinetico, "Flecha de amarre", c,
                            FormaProyectil.Arpon, 1, 0f, 1f, 10f,
                            (e, p) => e.GetComponent<SistemaEstadosAlterados>()?.AplicarRalentizacion(0.5f, 3f),
                            (p, n) => ImpulsarHacia(p, 12f),
                            (b, cr) => AlConfirmarImpacto?.Invoke(b, cr));
                    }
                    else
                    {
                        StartCoroutine(RutinaCarga(1.5f, () =>
                        {
                            Transform oo = OrigenDisparo();
                            ProyectilBalistico.Lanzar(salud, oo.position + oo.forward * 0.6f, oo.forward, 120f, 0f, 200f, 70f * BonoDistanciaEstabilizador(20f),
                                TipoDano.Cinetico, "Tiro de núcleo", c, FormaProyectil.Flecha, 3, 0f, 2.5f, 40f, null, null, (b, cr) => AlConfirmarImpacto?.Invoke(b, cr));
                            LuzEfimera.Crear(oo.position + oo.forward, c, 4f, 6f, 0.2f);
                        }));
                    }
                    return true;

                case TipoArmaModular.Mandoble:
                    if (indice == 0)
                    {
                        foreach (SistemaSaludYTraje e in EnemigosEnCono(3f, 70f))
                        {
                            Danar(e, 45f, TipoDano.Cinetico, "Barrido ascendente", e.transform.position + Vector3.up, 45f, false);
                            e.GetComponent<SistemaEstadosAlterados>()?.AplicarEmpuje(Vector3.up * 7f + transform.forward * 2f);
                        }
                        FabricaEfectosVisuales.CrearAnilloOnda(pos + transform.forward * 1.5f + Vector3.up, 2f, c);
                    }
                    else if (indice == 1)
                    {
                        ProyectilBalistico.Lanzar(salud, pos + Vector3.up * 1.2f + transform.forward * 1f, transform.forward, 25f, 0f, 15f, 35f, TipoDano.Cinetico,
                            "Onda de filo", c, FormaProyectil.OndaDeFilo, 10, 0.9f, 1f, 25f, null, null, (b, cr) => AlConfirmarImpacto?.Invoke(b, cr));
                    }
                    else
                    {
                        StartCoroutine(RutinaEstocada());
                    }
                    return true;

                case TipoArmaModular.MazaEscudo:
                    if (indice == 0)
                    {
                        foreach (SistemaSaludYTraje e in EnemigosEnRadio(pos + transform.forward * 1.2f, 3.5f))
                        {
                            Danar(e, 40f, TipoDano.Cinetico, "Golpe de yunque", e.transform.position, 30f, false);
                            e.Tambalear(0.8f);
                        }
                        FabricaEfectosVisuales.CrearEfectoElemental(TipoElemento.Tierra, pos + transform.forward * 1.2f, 3.5f);
                        SacudidaCamara.SacudirDesde(pos, 0.25f, 0.3f, 10f);
                    }
                    else if (indice == 1)
                    {
                        ProyectilBalistico.Lanzar(salud, o.position + o.forward * 0.6f, o.forward, 40f, 1f, 12f, 20f, TipoDano.Cinetico, "Lanzamiento con cadena", c,
                            FormaProyectil.Cadena, 1, 0f, 1f, 20f,
                            (e, p) => Arrastrar(e, e.GetComponent<ControladorGuardianElemental>() != null && e.GetComponent<ControladorGuardianElemental>().Rol == RolGuardian.Vanguardia ? 2f : 5f),
                            null, (b, cr) => AlConfirmarImpacto?.Invoke(b, cr));
                    }
                    else
                    {
                        BarreraEnergia.Crear(pos + transform.forward * 1.3f, transform.rotation, new Vector2(2f, 1.8f), 6f, 0f, salud, c);
                    }
                    return true;

                case TipoArmaModular.RifleLargo:
                    if (indice == 0)
                    {
                        for (int i = 0; i < 8; i++)
                        {
                            DispararRayo(10f, 6f, 12.5f, 1, "Disparo de dispersión", c, 6f,
                                e => e.GetComponent<SistemaEstadosAlterados>()?.AplicarEmpuje(transform.forward * 6f));
                        }
                        movimiento?.AplicarRetroceso(6f);
                        AlAccion?.Invoke(TipoAccionCombate.Disparo);
                    }
                    else if (indice == 1)
                    {
                        siguienteArticulacion = true;
                        AlAviso?.Invoke("Siguiente disparo: Tiro a articulación");
                    }
                    else
                    {
                        siguientePerforante = true;
                        AlAviso?.Invoke("Siguiente disparo: Calibre perforante");
                    }
                    return true;

                default:
                    if (indice == 0)
                    {
                        foreach (SistemaSaludYTraje e in EnemigosEnCono(2.5f, 60f))
                        {
                            Danar(e, 50f * (Time.time < sobrecargaHasta ? 1.3f : 1f), TipoDano.Cinetico, "Cuchillas de antebrazo", e.transform.position + Vector3.up, 30f, false);
                            InformacionDano sangrado = InformacionDano.Crear(0f, TipoDano.Cinetico, salud, "Sangrado energético");
                            e.GetComponent<SistemaEstadosAlterados>()?.AplicarQuemadura(6f, 3f, sangrado);
                        }
                        AlAccion?.Invoke(TipoAccionCombate.GolpeHorizontal);
                    }
                    else if (indice == 1)
                    {
                        ProyectilBalistico.Lanzar(salud, o.position + o.forward * 0.6f, o.forward, 50f, 0.5f, 18f, 10f, TipoDano.Cinetico, "Cable arpón", c,
                            FormaProyectil.Arpon, 1, 0f, 1f, 10f, (e, p) => Arrastrar(e, 4f), (p, n) => ImpulsarHacia(p, 13f),
                            (b, cr) => AlConfirmarImpacto?.Invoke(b, cr));
                    }
                    else
                    {
                        StartCoroutine(RutinaCarga(1f, () =>
                        {
                            DispararRayo(90f, 150f, 0.05f, 1, "Pulso de riel", c, 50f, e =>
                            {
                                ControladorGuardianElemental g = e.GetComponent<ControladorGuardianElemental>();
                                if (g != null && g.ElementoActual == TipoElemento.Metal) g.AnularLevitacion(2f);
                            });
                            LuzEfimera.Crear(OrigenDisparo().position, c, 6f, 10f, 0.25f);
                        }));
                    }
                    return true;
            }
        }

        /// <summary>Anticipación de habilidades cargadas: si el humano se tambalea o muere, se pierde.</summary>
        private IEnumerator RutinaCarga(float segundos, Action alTerminar)
        {
            habilidadCargando = true;
            float fin = Time.time + segundos;
            LuzEfimera.Crear(OrigenDisparo().position + OrigenDisparo().forward * 0.5f, arma.colorEnergia, 2f, 3f, segundos);
            while (Time.time < fin)
            {
                if ((salud != null && salud.EstaMuerto) || entrada.BloqueadaPorTambaleo)
                {
                    habilidadCargando = false;
                    AlAviso?.Invoke("Carga interrumpida");
                    yield break;
                }
                yield return null;
            }
            habilidadCargando = false;
            alTerminar?.Invoke();
        }

        private IEnumerator RutinaEstocada()
        {
            habilidadCargando = true;
            yield return new WaitForSeconds(0.2f);
            movimiento?.AplicarImpulso(transform.forward * 14.7f);
            float fin = Time.time + 0.45f;
            HashSet<SistemaSaludYTraje> golpeados = new HashSet<SistemaSaludYTraje>();
            while (Time.time < fin)
            {
                foreach (SistemaSaludYTraje e in EnemigosEnRadio(transform.position + transform.forward, 1.8f))
                {
                    if (!golpeados.Add(e)) continue;
                    Danar(e, 50f, TipoDano.Cinetico, "Estocada de impulso", e.transform.position + Vector3.up, 70f, false);
                    e.GetComponent<SistemaEstadosAlterados>()?.AplicarEmpuje(transform.forward * 9f);
                    SacudidaCamara.SacudirDesde(e.transform.position, 0.15f, 0.2f, 8f);
                }
                yield return null;
            }
            habilidadCargando = false;
        }

        // =====================================================================
        // Utilidades de combate
        // =====================================================================

        private InformacionDano FiltrarDano(InformacionDano info)
        {
            if (!info.TieneAtacante || info.Atacante == null) return info;
            Vector3 haciaAtacante = info.Atacante.transform.position - transform.position;
            haciaAtacante.y = 0f;
            bool frontal = haciaAtacante.sqrMagnitude > 0.01f && Vector3.Angle(transform.forward, haciaAtacante) < 70f;
            if (!frontal) return info;

            if (Time.time < absorcionHasta)
            {
                float absorbe = info.Cantidad * 0.8f;
                absorbido += absorbe;
                info.Cantidad -= absorbe;
                FabricaEfectosVisuales.CrearImpacto(transform.position + Vector3.up * 1.2f + transform.forward, transform.forward, arquetipo.colorAcento);
            }
            else if (Bloqueando)
            {
                info.Cantidad *= 1f - arma.bloqueoFrontal;
                info.ImpactoAguante *= 0.5f;
                FabricaEfectosVisuales.CrearImpacto(transform.position + Vector3.up * 1.2f + transform.forward * 0.8f, transform.forward, arma.colorEnergia);
            }
            return info;
        }

        private void Danar(SistemaSaludYTraje objetivo, float cantidad, TipoDano tipo, string origen, Vector3 punto, float aguante, bool critico)
        {
            if (objetivo == null || objetivo.EstaMuerto) return;
            InformacionDano info = InformacionDano.Crear(cantidad * (critico ? CriticoBonus() : 1f), tipo, salud, origen);
            info.Punto = punto;
            info.EsCritico = critico;
            info.ImpactoAguante = aguante;
            objetivo.RecibirDano(info);
        }

        private float CriticoBonus() => Time.time < visorHasta ? 1.25f : 1f;

        private float BonoDistanciaEstabilizador(float distancia)
        {
            return Time.time < estabilizadorHasta && distancia >= 20f ? 1.25f : 1f;
        }

        private float DispersionActual()
        {
            if (Time.time < estabilizadorHasta) return 0.02f;
            float movimientoFactor = movimiento != null ? movimiento.FactorMovimiento : 0f;
            float d = arma.dispersion + arma.dispersionMovimiento * movimientoFactor;
            if (movimiento != null && !movimiento.EnSuelo) d *= 2f;
            if (Apuntando) d *= 0.35f;
            return d;
        }

        private void DispararRayo(float dano, float alcance, float dispersion, int perforaciones, string origenNombre, Color color, float aguante,
            Action<SistemaSaludYTraje> alImpactar = null)
        {
            Transform o = OrigenDisparo();
            Vector3 direccion = AplicarDispersion(o.forward, dispersion);
            Ray rayo = new Ray(o.position, direccion);
            RaycastHit[] impactos = Physics.RaycastAll(rayo, alcance, ~0, QueryTriggerInteraction.Ignore);
            Array.Sort(impactos, (a, b) => a.distance.CompareTo(b.distance));
            Vector3 final = o.position + direccion * alcance;
            bool huboImpacto = false, baja = false, hubocritico = false;
            int restantes = perforaciones;

            foreach (RaycastHit h in impactos)
            {
                SistemaSaludYTraje e = h.collider.GetComponentInParent<SistemaSaludYTraje>();
                if (e == salud) continue;
                if (e == null)
                {
                    BarreraEnergia b = h.collider.GetComponent<BarreraEnergia>();
                    if (b != null && salud != null && b.Bando == salud.Bando) continue;
                    final = h.point;
                    FabricaEfectosVisuales.CrearImpacto(h.point, h.normal, color);
                    break;
                }
                if (e.EstaMuerto || (salud != null && e.Bando == salud.Bando)) continue;

                float mult = PuntoDebil.EvaluarRayo(e, rayo, out bool critico);
                float danoFinal = dano * mult * BonoDistanciaEstabilizador(h.distance);
                Danar(e, danoFinal, TipoDano.Cinetico, origenNombre, h.point, aguante, critico);
                alImpactar?.Invoke(e);
                FabricaEfectosVisuales.CrearImpacto(h.point, h.normal, critico ? PaletaVoidform.Disputa : PaletaVoidform.Alerta);
                huboImpacto = true;
                hubocritico |= critico;
                if (e.EstaMuerto) baja = true;
                final = h.point;
                if (--restantes <= 0) break;
            }

            Vector3 boca = o.position + o.forward * 0.7f + o.right * 0.18f - o.up * 0.15f;
            FabricaEfectosVisuales.CrearTrazador(boca, final, color);
            FabricaEfectosVisuales.CrearDestelloDisparo(boca, o.forward, color);
            if (huboImpacto) AlConfirmarImpacto?.Invoke(baja, hubocritico);
        }

        private static Vector3 AplicarDispersion(Vector3 direccion, float grados)
        {
            if (grados <= 0.001f) return direccion;
            Vector2 r = Random.insideUnitCircle * grados;
            return (Quaternion.LookRotation(direccion) * Quaternion.Euler(r.y, r.x, 0f)) * Vector3.forward;
        }

        private Transform OrigenDisparo()
        {
            return movimiento != null && movimiento.PivoteCamara != null ? movimiento.PivoteCamara : transform;
        }

        private Vector3 PuntoApuntado(float alcance)
        {
            Transform o = OrigenDisparo();
            if (Physics.Raycast(o.position, o.forward, out RaycastHit h, alcance, ~0, QueryTriggerInteraction.Ignore)) return h.point;
            Vector3 lejos = o.position + o.forward * alcance;
            return Physics.Raycast(lejos + Vector3.up * 5f, Vector3.down, out RaycastHit suelo, 40f, ~0, QueryTriggerInteraction.Ignore) ? suelo.point : lejos;
        }

        private Vector3 DireccionMovimientoODetras()
        {
            Vector2 m = entrada.Movimiento;
            Vector3 d = transform.right * m.x + transform.forward * m.y;
            d.y = 0f;
            return d.sqrMagnitude > 0.05f ? d.normalized : -transform.forward;
        }

        private static Vector3 DireccionDesde(Vector3 desde, Vector3 hacia)
        {
            Vector3 d = hacia - desde;
            d.y = 0f;
            return d.sqrMagnitude < 0.001f ? Vector3.forward : d.normalized;
        }

        private void ImpulsarHacia(Vector3 punto, float rapidez)
        {
            Vector3 d = punto - transform.position;
            movimiento?.AplicarImpulso(new Vector3(d.x, 0f, d.z).normalized * rapidez + Vector3.up * Mathf.Clamp(d.y + 4f, 3f, 10f));
        }

        private void Arrastrar(SistemaSaludYTraje objetivo, float metros)
        {
            Vector3 hacia = DireccionDesde(objetivo.transform.position, transform.position);
            objetivo.GetComponent<SistemaEstadosAlterados>()?.AplicarEmpuje(hacia * Mathf.Sqrt(2f * 9f * metros) + Vector3.up * 1.5f);
        }

        private List<SistemaSaludYTraje> EnemigosEnRadio(Vector3 centro, float radio)
        {
            List<SistemaSaludYTraje> lista = new List<SistemaSaludYTraje>();
            foreach (SistemaSaludYTraje e in SistemaSaludYTraje.Entidades)
            {
                if (e == null || e == salud || e.EstaMuerto || (salud != null && e.Bando == salud.Bando)) continue;
                Vector3 d = e.transform.position - centro;
                d.y = 0f;
                if (d.magnitude <= radio + 0.6f) lista.Add(e);
            }
            return lista;
        }

        private List<SistemaSaludYTraje> AliadosEnRadio(Vector3 centro, float radio)
        {
            List<SistemaSaludYTraje> lista = new List<SistemaSaludYTraje>();
            foreach (SistemaSaludYTraje e in SistemaSaludYTraje.Entidades)
            {
                if (e == null || e.EstaMuerto || salud == null || e.Bando != salud.Bando) continue;
                if (Vector3.Distance(e.transform.position, centro) <= radio) lista.Add(e);
            }
            return lista;
        }

        private List<SistemaSaludYTraje> EnemigosEnCono(float alcance, float anguloMedio)
        {
            List<SistemaSaludYTraje> lista = new List<SistemaSaludYTraje>();
            foreach (SistemaSaludYTraje e in EnemigosEnRadio(transform.position, alcance))
            {
                Vector3 d = e.transform.position - transform.position;
                d.y = 0f;
                if (d.sqrMagnitude < 1f || Vector3.Angle(transform.forward, d) <= anguloMedio) lista.Add(e);
            }
            return lista;
        }

        private SistemaSaludYTraje EnemigoEnMira(float alcance, float anguloMaximo)
        {
            Transform o = OrigenDisparo();
            SistemaSaludYTraje mejor = null;
            float mejorAngulo = anguloMaximo;
            foreach (SistemaSaludYTraje e in EnemigosEnRadio(transform.position, alcance))
            {
                float a = Vector3.Angle(o.forward, e.transform.position + Vector3.up * 1.4f - o.position);
                if (a < mejorAngulo)
                {
                    mejorAngulo = a;
                    mejor = e;
                }
            }
            return mejor;
        }

        private SistemaSaludYTraje AliadoMasAmenazado(float alcance)
        {
            SistemaSaludYTraje mejor = null;
            float peor = float.MaxValue;
            foreach (SistemaSaludYTraje a in AliadosEnRadio(transform.position, alcance))
            {
                if (a == salud) continue;
                if (a.PorcentajeVida < peor)
                {
                    peor = a.PorcentajeVida;
                    mejor = a;
                }
            }
            return mejor;
        }
    }

    /// <summary>
    /// Faro de vanguardia del Paladín: durante 8 s, los aliados en 6 m ganan +15% de velocidad y regeneran
    /// 4 de blindaje por segundo, y los guardianes que entran quedan revelados.
    /// </summary>
    public class FaroVanguardia : MonoBehaviour
    {
        private SistemaSaludYTraje dueno;
        private float fin;
        private float proximoPulso;

        public static void Crear(Vector3 punto, SistemaSaludYTraje propietario, Color color)
        {
            GameObject go = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            go.name = "Faro de vanguardia";
            DestroyImmediate(go.GetComponent<Collider>());
            go.transform.position = punto + Vector3.up * 6f;
            go.transform.localScale = new Vector3(0.35f, 6f, 0.35f);
            Renderer r = go.GetComponent<Renderer>();
            r.sharedMaterial = MaterialesProcedurales.ObtenerMaterialLinea(color);
            r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            FaroVanguardia f = go.AddComponent<FaroVanguardia>();
            f.dueno = propietario;
            f.fin = Time.time + 8f;
            LuzEfimera.Crear(punto + Vector3.up * 2f, color, 6f, 14f, 8f);
            FabricaEfectosVisuales.CrearAnilloOnda(punto + Vector3.up * 0.1f, 6f, color);
        }

        private void Update()
        {
            if (Time.time >= fin || dueno == null)
            {
                Destroy(gameObject);
                return;
            }
            if (Time.time < proximoPulso) return;
            proximoPulso = Time.time + 0.5f;
            Vector3 centro = new Vector3(transform.position.x, transform.position.y - 6f, transform.position.z);
            foreach (SistemaSaludYTraje e in SistemaSaludYTraje.Entidades)
            {
                if (e == null || e.EstaMuerto || Vector3.Distance(e.transform.position, centro) > 6f) continue;
                if (e.Bando == dueno.Bando)
                {
                    e.RestaurarBlindaje(2f);
                    e.GetComponent<ControladorMovimientoAssaultCube>()?.AplicarBonificacionVelocidad(1.15f, 0.6f);
                }
                else
                {
                    e.Revelar(1f);
                }
            }
        }
    }
}
