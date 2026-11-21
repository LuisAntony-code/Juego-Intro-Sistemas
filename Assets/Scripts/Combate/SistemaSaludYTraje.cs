using System;
using System.Collections.Generic;
using UnityEngine;

namespace VoidformBioForge
{
    /// <summary>
    /// Gestiona la salud/integridad del jugador o traje, tanto de Destructores como de Cuidadores.
    /// -----------------------------------------------------------------
    /// Nivel 1: vida y blindaje con daño y curación básicos.
    /// Nivel 2: resistencias por tipo de daño según el traje, alerta de traje crítico (25%) y
    ///          REGLA DE EXPLOSIÓN: si el personaje es Destructor y su vida llega a 0, el traje
    ///          explota (daño en área a los enemigos + efecto visual + notificación) y el humano muere.
    /// Nivel 3: bloqueo global de daño durante transiciones de ronda (caso límite: explosión justo
    ///          al terminar la ronda o el tiempo), protección de reaparición, fuego amigo opcional,
    ///          registro estático de entidades y contaminación del ecosistema por cada explosión.
    /// </summary>
    public class SistemaSaludYTraje : MonoBehaviour
    {
        // ------------------------------------------------------------------
        // Registro estático y reglas globales (Nivel 3)
        // ------------------------------------------------------------------
        private static readonly List<SistemaSaludYTraje> entidadesRegistradas = new List<SistemaSaludYTraje>();

        /// <summary>Todas las entidades con salud presentes en la escena.</summary>
        public static IReadOnlyList<SistemaSaludYTraje> Entidades => entidadesRegistradas;

        /// <summary>Cuando es verdadero nadie recibe daño (pausa entre rondas o fin de partida).</summary>
        public static bool BloqueoGlobalDeDano { get; set; }

        /// <summary>Si es falso (valor por defecto), los aliados no se dañan entre sí.</summary>
        public static bool FuegoAmigoActivo { get; set; }

        public static event Action<SistemaSaludYTraje> AlRegistrarseEntidad;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ReiniciarEstadoEstatico()
        {
            entidadesRegistradas.Clear();
            BloqueoGlobalDeDano = false;
            FuegoAmigoActivo = false;
            AlRegistrarseEntidad = null;
        }

        // ------------------------------------------------------------------
        // Nivel 1: identidad y estadísticas base
        // ------------------------------------------------------------------
        [Header("Identidad y Estadísticas Base")]
        [SerializeField] private string nombreEntidad = "Operativo";
        [SerializeField] private BandoJuego bando = BandoJuego.Destructor;
        [SerializeField] private float vidaMaxima = 100f;
        [SerializeField] private float blindajeMaximo = 50f;
        [Tooltip("Porción de cada golpe que absorbe el blindaje mientras quede.")]
        [SerializeField] [Range(0f, 1f)] private float absorcionBlindaje = 0.6f;

        // ------------------------------------------------------------------
        // Nivel 2: explosión del traje humano
        // ------------------------------------------------------------------
        [Header("Explosión del Traje Humano al Llegar a Cero")]
        [SerializeField] private bool explotarTrajeAlMorirSiEsHumano = true;
        [SerializeField] private float radioExplosionTraje = 5.5f;
        [SerializeField] private float danoExplosionTraje = 45f;
        [SerializeField] private float empujeExplosionTraje = 9f;
        [SerializeField] private GameObject efectoVisualExplosionTraje;
        [Tooltip("Porcentaje de vida por debajo del cual se avisa del riesgo de explosión.")]
        [SerializeField] [Range(0.05f, 0.5f)] private float umbralTrajeCritico = 0.25f;

        private DatosArquetipo arquetipoAsignado;

        // ---------- Estabilidad (aguante) ----------
        private float aguanteMaximo = 60f;
        private float ultimoImpactoAguante = -10f;
        private float inmuneTambaleoHasta = -1f;

        // ---------- Modificadores temporales ----------
        private float multiplicadorTemporalRecibido = 1f;
        private float multiplicadorTemporalHasta = -1f;
        private float reduccionElementalTemporal;
        private float reduccionElementalHasta = -1f;
        private SistemaSaludYTraje protector;
        private float porcentajeProtector;
        private float protectorHasta = -1f;
        private float reveladoHasta = -1f;
        private float multiplicadorDanoRecibido = 1f;
        private float invulnerableHasta = -1f;
        private bool alertaCriticaActiva;
        private Vector3 posicionAparicion;
        private Quaternion rotacionAparicion;
        private Renderer[] renderizadores;
        private Collider[] colisionadores;

        public string NombreEntidad => nombreEntidad;
        public float VidaActual { get; private set; }
        public float VidaMaxima => vidaMaxima;
        public float BlindajeActual { get; private set; }
        public float BlindajeMaximo => blindajeMaximo;
        public bool EstaMuerto { get; private set; }
        public BandoJuego Bando => bando;
        public bool EsJugadorLocal { get; set; }
        public DatosArquetipo Arquetipo => arquetipoAsignado;

        public float AguanteActual { get; private set; } = 60f;
        public float AguanteMaximo => aguanteMaximo;
        public bool EstaTambaleando => Time.time < TambaleoHasta;
        public float TambaleoHasta { get; private set; } = -1f;
        public bool EstaRevelado => Time.time < reveladoHasta;

        /// <summary>
        /// Filtro opcional que ajusta el daño entrante antes de aplicarlo (bloqueo con escudo o espada,
        /// absorción del Bastión...). Lo registra el sistema de combate del propio personaje.
        /// </summary>
        public Func<InformacionDano, InformacionDano> FiltroDanoEntrante { get; set; }

        public event Action<float> AlTambalear;
        public float TiempoUltimaMuerte { get; private set; } = -1f;
        public InformacionDano UltimoDanoRecibido { get; private set; }
        public float PorcentajeVida => vidaMaxima <= 0f ? 0f : VidaActual / vidaMaxima;
        public bool EsInvulnerable => BloqueoGlobalDeDano || Time.time < invulnerableHasta;

        public event Action<float, float, float, float> AlCambiarSaludYBlindaje;
        public event Action<InformacionDano> AlRecibirDano;
        public event Action<bool> AlCambiarAlertaTrajeCritico;
        public event Action<SistemaSaludYTraje> AlExplotarTrajeHumano;
        public event Action<SistemaSaludYTraje> AlMorir;
        public event Action<SistemaSaludYTraje> AlRevivir;

        private void Awake()
        {
            posicionAparicion = transform.position;
            rotacionAparicion = transform.rotation;
            RestaurarEstadoCompleto();
        }

        private void OnEnable()
        {
            if (!entidadesRegistradas.Contains(this))
            {
                entidadesRegistradas.Add(this);
                AlRegistrarseEntidad?.Invoke(this);
            }
        }

        private void OnDestroy()
        {
            entidadesRegistradas.Remove(this);
        }

        // ================== Configuración ==================

        public void ConfigurarIdentidad(string nombre, BandoJuego nuevoBando, bool esJugadorLocal)
        {
            nombreEntidad = string.IsNullOrWhiteSpace(nombre) ? nombreEntidad : nombre;
            bando = nuevoBando;
            EsJugadorLocal = esJugadorLocal;
        }

        public void ConfigurarBandoYEstadisticas(BandoJuego nuevoBando, float nuevaVidaMax, float nuevoBlindajeMax)
        {
            bando = nuevoBando;
            vidaMaxima = Mathf.Max(1f, nuevaVidaMax);
            blindajeMaximo = Mathf.Max(0f, nuevoBlindajeMax);
            RestaurarEstadoCompleto();
        }

        /// <summary>Asigna un traje Destructor: aplica su vida, blindaje y resistencias.</summary>
        /// <summary>Asigna el arquetipo humano: vida, blindaje, aguante y resistencias de su armadura.</summary>
        public void AsignarArquetipo(DatosArquetipo arquetipo)
        {
            arquetipoAsignado = arquetipo;
            if (arquetipo != null)
            {
                aguanteMaximo = arquetipo.aguanteMaximo;
                ConfigurarBandoYEstadisticas(BandoJuego.Destructor, arquetipo.vidaMaxima, arquetipo.blindajeMaximo);
            }
        }

        public void ConfigurarAguante(float maximo)
        {
            aguanteMaximo = Mathf.Max(1f, maximo);
            AguanteActual = aguanteMaximo;
        }

        // ================== Modificadores temporales (habilidades) ==================

        /// <summary>Multiplica TODO el daño recibido durante un tiempo (marcas, sobrecalentamiento...).</summary>
        public void AplicarMultiplicadorTemporal(float multiplicador, float duracion)
        {
            multiplicadorTemporalRecibido = multiplicador;
            multiplicadorTemporalHasta = Time.time + duracion;
        }

        /// <summary>Reduce el daño elemental (Madera, Agua, Tierra, Fuego, Metal) durante un tiempo.</summary>
        public void AplicarReduccionElemental(float reduccion, float duracion)
        {
            reduccionElementalTemporal = Mathf.Clamp01(reduccion);
            reduccionElementalHasta = Time.time + duracion;
        }

        /// <summary>Otro personaje recibe un porcentaje del daño de este (Intercepción del Bastión).</summary>
        public void EstablecerProtector(SistemaSaludYTraje nuevoProtector, float porcentaje, float duracion)
        {
            protector = nuevoProtector;
            porcentajeProtector = Mathf.Clamp01(porcentaje);
            protectorHasta = Time.time + duracion;
        }

        /// <summary>Hace visible al personaje para el bando rival durante un tiempo (faro, visor, enlace).</summary>
        public void Revelar(float duracion)
        {
            reveladoHasta = Mathf.Max(reveladoHasta, Time.time + duracion);
        }

        // ================== Estabilidad: aguante y tambaleo ==================

        private void Update()
        {
            if (EstaMuerto) return;
            if (Time.time - ultimoImpactoAguante > 1f && AguanteActual < aguanteMaximo)
            {
                AguanteActual = Mathf.Min(aguanteMaximo, AguanteActual + 25f * Time.deltaTime);
            }
        }

        /// <summary>Resta aguante. Si llega a 0, el personaje se tambalea 0,8 s y cancela lo que estaba cargando.</summary>
        public void DanarAguante(float cantidad)
        {
            if (EstaMuerto || cantidad <= 0f || Time.time < inmuneTambaleoHasta || EsInvulnerable) return;
            ultimoImpactoAguante = Time.time;
            AguanteActual -= cantidad;
            if (AguanteActual <= 0f)
            {
                AguanteActual = aguanteMaximo;
                Tambalear(0.8f);
            }
        }

        public void Tambalear(float duracion)
        {
            if (EstaMuerto) return;
            TambaleoHasta = Time.time + duracion;
            inmuneTambaleoHasta = Time.time + duracion + 2f;
            EntradaPersonaje entrada = GetComponent<EntradaPersonaje>();
            if (entrada != null) entrada.BloquearDurante(duracion);
            ControladorGuardianElemental guardian = GetComponent<ControladorGuardianElemental>();
            if (guardian != null) guardian.CancelarAnticipacion("tambaleo");
            AlTambalear?.Invoke(duracion);
        }

        /// <summary>Escala el daño recibido (por ejemplo, para jefes del Modo Historia).</summary>
        public void EstablecerMultiplicadorDanoRecibido(float multiplicador)
        {
            multiplicadorDanoRecibido = Mathf.Clamp(multiplicador, 0.05f, 5f);
        }

        public void EstablecerPuntoAparicion(Vector3 posicion, Quaternion rotacion)
        {
            posicionAparicion = posicion;
            rotacionAparicion = rotacion;
        }

        public void ConcederProteccion(float segundos)
        {
            invulnerableHasta = Mathf.Max(invulnerableHasta, Time.time + segundos);
        }

        // ================== Nivel 1: vida básica ==================

        public void RestaurarEstadoCompleto()
        {
            EstaMuerto = false;
            VidaActual = vidaMaxima;
            BlindajeActual = blindajeMaximo;
            AguanteActual = aguanteMaximo;
            TambaleoHasta = -1f;
            multiplicadorTemporalHasta = -1f;
            reduccionElementalHasta = -1f;
            protectorHasta = -1f;
            EstablecerCuerpoVisible(true);
            ActualizarAlertaCritica();
            NotificarCambioSalud();
        }

        /// <summary>Compatibilidad con la versión básica: daño cinético sin atacante.</summary>
        public void RecibirDano(float cantidadDano)
        {
            RecibirDano(InformacionDano.Ambiental(cantidadDano, TipoDano.Cinetico, "Daño directo", transform.position));
        }

        // ================== Nivel 2: daño con reglas ==================

        public void RecibirDano(InformacionDano informacion)
        {
            if (EstaMuerto || informacion.Cantidad <= 0f) return;
            if (EsInvulnerable) return;
            if (EsDanoAliadoBloqueado(informacion)) return;

            // Barreras de energía (Égida refractaria, Postura de bastión) entre el atacante y este personaje.
            if (informacion.TieneAtacante && informacion.Atacante != gameObject
                && BarreraEnergia.Intercepta(informacion.Atacante != null ? informacion.Atacante.transform.position + Vector3.up * 1.2f : informacion.Punto,
                    transform.position + Vector3.up, bando, informacion))
            {
                return;
            }

            // Filtro propio (bloqueo con arma o escudo, absorción...).
            if (FiltroDanoEntrante != null)
            {
                informacion = FiltroDanoEntrante(informacion);
                if (informacion.Cantidad <= 0f) return;
            }

            // Intercepción: un protector recibe parte del daño.
            if (protector != null && Time.time < protectorHasta && !protector.EstaMuerto && protector != this)
            {
                InformacionDano parte = informacion;
                parte.Cantidad = informacion.Cantidad * porcentajeProtector;
                informacion.Cantidad -= parte.Cantidad;
                protector.RecibirDano(parte);
            }

            float multiplicadorArmadura = arquetipoAsignado != null ? arquetipoAsignado.ObtenerMultiplicador(informacion.Tipo) : 1f;
            float multiplicadorTemporal = Time.time < multiplicadorTemporalHasta ? multiplicadorTemporalRecibido : 1f;
            bool esElemental = informacion.Tipo != TipoDano.Cinetico && informacion.Tipo != TipoDano.Explosivo;
            float reduccion = esElemental && Time.time < reduccionElementalHasta ? reduccionElementalTemporal : 0f;
            float danoRestante = informacion.Cantidad * multiplicadorArmadura * multiplicadorDanoRecibido * multiplicadorTemporal * (1f - reduccion);

            // Estabilidad: los golpes restan aguante (las explosiones, el triple).
            float aguante = informacion.ImpactoAguante > 0f ? informacion.ImpactoAguante : informacion.Cantidad * 0.3f;
            if (informacion.Tipo == TipoDano.Explosivo) aguante *= 3f;

            if (BlindajeActual > 0f)
            {
                float absorbidoPorBlindaje = Mathf.Min(BlindajeActual, danoRestante * absorcionBlindaje);
                BlindajeActual -= absorbidoPorBlindaje;
                danoRestante -= absorbidoPorBlindaje;
            }

            VidaActual = Mathf.Max(0f, VidaActual - danoRestante);
            informacion.Cantidad = danoRestante;
            UltimoDanoRecibido = informacion;

            AlRecibirDano?.Invoke(informacion);
            NotificarCambioSalud();
            ActualizarAlertaCritica();

            if (VidaActual <= 0f)
            {
                EjecutarMuerte(informacion);
            }
            else
            {
                DanarAguante(aguante);
            }
        }

        private bool EsDanoAliadoBloqueado(InformacionDano informacion)
        {
            if (FuegoAmigoActivo || !informacion.TieneAtacante) return false;
            if (informacion.Atacante == gameObject) return false;
            return informacion.BandoAtacante == bando;
        }

        public void Curar(float cantidadCuracion)
        {
            if (EstaMuerto || cantidadCuracion <= 0f) return;
            VidaActual = Mathf.Min(vidaMaxima, VidaActual + cantidadCuracion);
            NotificarCambioSalud();
            ActualizarAlertaCritica();
        }

        /// <summary>Fija la vida a un porcentaje de la máxima sin pasar por el blindaje (mínimo 1 punto).</summary>
        public void EstablecerPorcentajeVida(float porcentaje)
        {
            if (EstaMuerto) return;
            VidaActual = Mathf.Max(1f, vidaMaxima * Mathf.Clamp01(porcentaje));
            NotificarCambioSalud();
            ActualizarAlertaCritica();
        }

        public void RestaurarBlindaje(float cantidad)
        {
            if (EstaMuerto || cantidad <= 0f) return;
            BlindajeActual = Mathf.Min(blindajeMaximo, BlindajeActual + cantidad);
            NotificarCambioSalud();
        }

        /// <summary>Permite superar temporalmente el blindaje máximo (escudos de habilidades).</summary>
        public void AnadirBlindajeTemporal(float cantidad)
        {
            if (EstaMuerto || cantidad <= 0f) return;
            BlindajeActual += cantidad;
            NotificarCambioSalud();
        }

        private void ActualizarAlertaCritica()
        {
            bool critico = !EstaMuerto && bando == BandoJuego.Destructor && PorcentajeVida <= umbralTrajeCritico;
            if (critico != alertaCriticaActiva)
            {
                alertaCriticaActiva = critico;
                AlCambiarAlertaTrajeCritico?.Invoke(critico);
            }
        }

        private void NotificarCambioSalud()
        {
            AlCambiarSaludYBlindaje?.Invoke(VidaActual, vidaMaxima, BlindajeActual, blindajeMaximo);
        }

        // ================== Nivel 2/3: muerte y explosión ==================

        private void EjecutarMuerte(InformacionDano causa)
        {
            if (EstaMuerto) return;
            EstaMuerto = true;
            TiempoUltimaMuerte = Time.time;

            bool explota = bando == BandoJuego.Destructor && explotarTrajeAlMorirSiEsHumano;

            BusEventosJuego.PublicarBaja(new InformacionBaja
            {
                NombreVictima = nombreEntidad,
                BandoVictima = bando,
                NombreAtacante = causa.TieneAtacante ? causa.NombreAtacante : causa.Origen,
                TieneAtacante = causa.TieneAtacante,
                BandoAtacante = causa.BandoAtacante,
                FueExplosionDeTraje = causa.EsExplosionDeTraje,
                Origen = causa.Origen
            });

            // El cuerpo se oculta ANTES de la explosión para que la propia explosión no lo detecte.
            EstablecerCuerpoVisible(false);

            if (explota)
            {
                EjecutarExplosionDeTraje();
            }

            ActualizarAlertaCritica();
            AlMorir?.Invoke(this);
        }

        private void EjecutarExplosionDeTraje()
        {
            Vector3 centro = transform.position + Vector3.up * 1f;

            if (efectoVisualExplosionTraje != null)
            {
                Instantiate(efectoVisualExplosionTraje, centro, Quaternion.identity);
            }
            else
            {
                FabricaEfectosVisuales.CrearExplosionTraje(centro, radioExplosionTraje);
            }

            HashSet<SistemaSaludYTraje> yaAlcanzados = new HashSet<SistemaSaludYTraje>();
            Collider[] impactos = Physics.OverlapSphere(centro, radioExplosionTraje, ~0, QueryTriggerInteraction.Ignore);
            foreach (Collider col in impactos)
            {
                SistemaSaludYTraje objetivo = col.GetComponentInParent<SistemaSaludYTraje>();
                if (objetivo == null || objetivo == this || objetivo.EstaMuerto) continue;
                if (!yaAlcanzados.Add(objetivo)) continue;

                // Daño decreciente con la distancia: 100% en el centro, 40% en el borde.
                float distancia = Vector3.Distance(centro, objetivo.transform.position);
                float factor = Mathf.Lerp(1f, 0.4f, Mathf.Clamp01(distancia / radioExplosionTraje));

                InformacionDano info = InformacionDano.Crear(danoExplosionTraje * factor, TipoDano.Explosivo, this, "Explosión de traje");
                info.EsExplosionDeTraje = true;
                info.Punto = centro;
                objetivo.RecibirDano(info);

                ControladorMovimientoAssaultCube movimientoObjetivo = objetivo.GetComponent<ControladorMovimientoAssaultCube>();
                SistemaEstadosAlterados estados = objetivo.GetComponent<SistemaEstadosAlterados>();
                Vector3 direccion = (objetivo.transform.position - centro);
                direccion.y = 0f;
                Vector3 impulso = direccion.normalized * empujeExplosionTraje * factor + Vector3.up * 3f;
                if (estados != null) estados.AplicarEmpuje(impulso);
                else if (movimientoObjetivo != null) movimientoObjetivo.AplicarImpulso(impulso);
            }

            GestorEquilibrioEcosistema.Instancia?.RegistrarContaminacion(2.5f, "Explosión de traje");

            BusEventosJuego.PublicarMensaje($"¡El traje de {nombreEntidad} ha explotado!", TipoMensajeSistema.Critico);
            BusEventosJuego.PublicarExplosionTraje(this);
            AlExplotarTrajeHumano?.Invoke(this);
        }

        // ================== Nivel 3: reaparición ==================

        public void RevivirEnPuntoAparicion()
        {
            RevivirEnPuntoAparicion(0f);
        }

        public void RevivirEnPuntoAparicion(float segundosProteccion)
        {
            ControladorMovimientoAssaultCube movimiento = GetComponent<ControladorMovimientoAssaultCube>();
            if (movimiento != null)
            {
                movimiento.Teletransportar(posicionAparicion, rotacionAparicion);
                movimiento.EstablecerMovimientoHabilitado(true);
            }
            else
            {
                transform.SetPositionAndRotation(posicionAparicion, rotacionAparicion);
            }

            SistemaEstadosAlterados estados = GetComponent<SistemaEstadosAlterados>();
            if (estados != null) estados.LimpiarTodos();

            RestaurarEstadoCompleto();
            if (segundosProteccion > 0f) ConcederProteccion(segundosProteccion);
            AlRevivir?.Invoke(this);
        }

        /// <summary>
        /// Oculta o muestra el cuerpo sin desactivar el GameObject (así los gestores siguen
        /// pudiendo consultar este componente y las corrutinas no se interrumpen).
        /// </summary>
        private void EstablecerCuerpoVisible(bool visible)
        {
            if (renderizadores == null || renderizadores.Length == 0)
            {
                renderizadores = GetComponentsInChildren<Renderer>(true);
            }
            if (colisionadores == null || colisionadores.Length == 0)
            {
                colisionadores = GetComponentsInChildren<Collider>(true);
            }

            foreach (Renderer r in renderizadores)
            {
                if (r != null) r.enabled = visible;
            }
            foreach (Collider c in colisionadores)
            {
                if (c != null) c.enabled = visible;
            }

            EntradaPersonaje entrada = GetComponent<EntradaPersonaje>();
            if (entrada != null)
            {
                entrada.EntradaHabilitada = visible;
                if (!visible) entrada.LimpiarTodo();
            }

            ControladorMovimientoAssaultCube movimiento = GetComponent<ControladorMovimientoAssaultCube>();
            if (movimiento != null && !visible)
            {
                movimiento.EstablecerMovimientoHabilitado(false);
            }
        }

        /// <summary>Vuelve a leer los renderizadores (tras construir el modelo visual).</summary>
        public void RefrescarCacheVisual()
        {
            renderizadores = GetComponentsInChildren<Renderer>(true);
            colisionadores = GetComponentsInChildren<Collider>(true);
        }
    }
}
