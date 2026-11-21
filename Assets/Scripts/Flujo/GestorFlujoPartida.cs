using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace VoidformBioForge
{
    /// <summary>
    /// Director general del juego: une menús, salas, modos, personajes, biomas, resultados y progreso.
    /// Basta con UN GameObject vacío con este componente en una escena vacía para jugar todo el juego:
    /// crea la cámara, la interfaz, el fondo animado del menú y, al iniciar, la partida completa.
    /// -----------------------------------------------------------------
    /// Nivel 1: navegación entre pantallas (menú → bando → modo → personalización → sala/nivel → partida).
    /// Nivel 2: construcción de cada modo con su bioma, bases, equipos, bots, jugador local y cámara
    ///          (primera persona para Destructores, tercera persona para Guardianes).
    /// Nivel 3: conexión desacoplada por eventos: escucha el final de cada modo, aplica copas (4vs4),
    ///          avance de nivel y desenlace (Historia), gestiona pausa, espectador, reintentos y limpieza
    ///          completa de la partida sin dejar suscripciones ni objetos huérfanos.
    /// </summary>
    [DefaultExecutionOrder(-300)]
    public class GestorFlujoPartida : MonoBehaviour
    {
        public static GestorFlujoPartida Instancia { get; private set; }

        [Header("Configuración inicial (editable en el Inspector)")]
        [SerializeField] private ConfiguracionPartida configuracion = new ConfiguracionPartida();

        [Header("Flujo")]
        [SerializeField] private float esperaAntesDeResultados = 2.5f;
        [SerializeField] private bool crearInterfazAutomaticamente = true;
        [SerializeField] private bool mostrarFondoAnimadoEnMenus = true;

        private Camera camaraPrincipal;
        private GameObject raizPartida;
        private GameObject raizFondoMenu;
        private bool resultadoEnProceso;
        private Coroutine rutinaResultados;

        public ConfiguracionPartida Configuracion => configuracion;
        public EstadoFlujoJuego Estado { get; private set; } = EstadoFlujoJuego.MenuPrincipal;
        public GestorSalasYEmparejamiento Salas { get; private set; }
        public SistemaClasificacionYCopas Clasificacion { get; private set; }
        public GestorModoHistoria Historia { get; private set; }
        public ContextoPartida PartidaActual { get; private set; }
        public ResultadoPartida UltimoResultado { get; private set; }
        public CambioClasificacion? UltimoCambioCopas { get; private set; }
        public string UltimoDesenlace { get; private set; } = "";
        public bool UltimoNivelPermiteAvanzar { get; private set; }
        public Camera CamaraPrincipal => camaraPrincipal;

        public event Action<EstadoFlujoJuego> AlCambiarEstado;
        public event Action<ContextoPartida> AlPartidaConstruida;
        public event Action AlPartidaDestruida;
        public event Action<ResultadoPartida> AlResultadosListos;

        // =====================================================================
        // Arranque
        // =====================================================================

        private void Awake()
        {
            if (Instancia != null && Instancia != this)
            {
                Destroy(gameObject);
                return;
            }
            Instancia = this;
            Time.timeScale = 1f;

            Salas = ObtenerOAgregar<GestorSalasYEmparejamiento>();
            Clasificacion = ObtenerOAgregar<SistemaClasificacionYCopas>();
            Historia = ObtenerOAgregar<GestorModoHistoria>();

            Historia.AlFinalizarNivel += ManejarFinHistoria;
            Historia.AlCompletarHistoria += ManejarHistoriaCompletada;
            Historia.AlCambiarModoEspectador += ManejarEspectador;

            AsegurarCamara();

            if (crearInterfazAutomaticamente && FindFirstObjectByType<GestorInterfazJuego>() == null)
            {
                gameObject.AddComponent<GestorInterfazJuego>();
            }
        }

        private void Start()
        {
            CrearFondoMenu();
            IrA(EstadoFlujoJuego.MenuPrincipal);
        }

        private void OnDestroy()
        {
            if (Instancia != this) return;
            Instancia = null;
            if (Historia != null)
            {
                Historia.AlFinalizarNivel -= ManejarFinHistoria;
                Historia.AlCompletarHistoria -= ManejarHistoriaCompletada;
                Historia.AlCambiarModoEspectador -= ManejarEspectador;
            }
            Time.timeScale = 1f;
        }

        private T ObtenerOAgregar<T>() where T : Component
        {
            T c = GetComponent<T>();
            return c != null ? c : gameObject.AddComponent<T>();
        }

        private void AsegurarCamara()
        {
            camaraPrincipal = Camera.main;
            if (camaraPrincipal == null)
            {
                GameObject go = new GameObject("Main Camera") { tag = "MainCamera" };
                camaraPrincipal = go.AddComponent<Camera>();
                go.AddComponent<AudioListener>();
            }
            camaraPrincipal.clearFlags = CameraClearFlags.SolidColor;
            camaraPrincipal.backgroundColor = PaletaVoidform.FondoVacio;
        }

        // =====================================================================
        // Nivel 1: navegación entre pantallas
        // =====================================================================

        public void IrA(EstadoFlujoJuego nuevoEstado)
        {
            Estado = nuevoEstado;
            bool jugando = nuevoEstado == EstadoFlujoJuego.EnPartida;
            Cursor.lockState = jugando ? CursorLockMode.Locked : CursorLockMode.None;
            Cursor.visible = !jugando;
            EntradaJugadorLocal.EntradaBloqueadaPorInterfaz = !jugando;

            AlCambiarEstado?.Invoke(nuevoEstado);
            BusEventosJuego.PublicarEstadoFlujo(nuevoEstado);
        }

        public void ElegirBando(BandoJuego bando)
        {
            configuracion.bandoJugador = bando;
            IrA(EstadoFlujoJuego.SeleccionModo);
        }

        public void ElegirModo(ModoJuego modo)
        {
            configuracion.modo = modo;
            configuracion.jugadoresPorEquipo = 4;
            IrA(modo == ModoJuego.Historia ? EstadoFlujoJuego.SeleccionNivelHistoria : EstadoFlujoJuego.Personalizacion);
        }

        public bool ElegirNivelHistoria(int nivel)
        {
            int desbloqueado = GuardadoProgreso.ObtenerNivelDesbloqueado(configuracion.bandoJugador);
            if (nivel > desbloqueado)
            {
                BusEventosJuego.PublicarMensaje($"El nivel {nivel} está bloqueado: supera antes el nivel {desbloqueado}.", TipoMensajeSistema.Advertencia);
                return false;
            }
            configuracion.nivelHistoria = Mathf.Clamp(nivel, 1, GuardadoProgreso.NivelesTotales);
            IrA(configuracion.bandoJugador == BandoJuego.Destructor ? EstadoFlujoJuego.Personalizacion : EstadoFlujoJuego.Briefing);
            return true;
        }

        public void ElegirArquetipo(TipoArquetipo arquetipo) => configuracion.arquetipo = arquetipo;
        public void ElegirArmaModular(TipoArmaModular arma) => configuracion.armaModular = arma;
        public void ElegirElemento(TipoElemento elemento) => configuracion.elementoGuardian = elemento;
        public void ElegirBiomaVersus(int indice) => configuracion.indiceBiomaVersus = Mathf.Clamp(indice, -1, 5);
        public void ElegirVarianteGuardian(RolGuardian rol) => configuracion.varianteGuardian = rol;
        public void ElegirDificultadBots(float dificultad) => configuracion.dificultadBots = Mathf.Clamp01(dificultad);

        public void ContinuarDesdePersonalizacion()
        {
            IrA(EstadoFlujoJuego.SeleccionSala);
        }

        public void SeleccionarSala(TipoSalaConexion tipo, string codigo, bool privada)
        {
            configuracion.tipoSala = tipo;
            configuracion.salaPrivada = privada;
            Salas.SeleccionarTipoSala(tipo, codigo, configuracion.modo, configuracion.bandoJugador, configuracion.jugadoresPorEquipo, privada);
        }

        public bool ContinuarDesdeSala()
        {
            if (!Salas.PuedeIniciar)
            {
                BusEventosJuego.PublicarMensaje("Elige primero un tipo de sala y espera a que esté lista.", TipoMensajeSistema.Advertencia);
                return false;
            }
            Salas.AplicarAConfiguracion(configuracion);
            if (configuracion.modo == ModoJuego.Historia) IrA(EstadoFlujoJuego.Briefing);
            else IniciarPartida();
            return true;
        }

        public void ComenzarDesdeBriefing()
        {
            IniciarPartida();
        }

        public void Volver()
        {
            switch (Estado)
            {
                case EstadoFlujoJuego.SeleccionBando: IrA(EstadoFlujoJuego.MenuPrincipal); break;
                case EstadoFlujoJuego.SeleccionModo: IrA(EstadoFlujoJuego.SeleccionBando); break;
                case EstadoFlujoJuego.SeleccionNivelHistoria: IrA(EstadoFlujoJuego.SeleccionModo); break;
                case EstadoFlujoJuego.Personalizacion:
                    IrA(configuracion.modo == ModoJuego.Historia ? EstadoFlujoJuego.SeleccionNivelHistoria : EstadoFlujoJuego.SeleccionModo);
                    break;
                case EstadoFlujoJuego.SeleccionSala:
                    Salas.CerrarSala();
                    IrA(EstadoFlujoJuego.Personalizacion);
                    break;
                case EstadoFlujoJuego.Briefing:
                    IrA(configuracion.bandoJugador == BandoJuego.Destructor ? EstadoFlujoJuego.SeleccionSala : EstadoFlujoJuego.SeleccionNivelHistoria);
                    break;
                default: IrA(EstadoFlujoJuego.MenuPrincipal); break;
            }
        }

        // ---------------- Pausa y salida ----------------

        public void AlternarPausa()
        {
            if (Estado == EstadoFlujoJuego.EnPartida)
            {
                Time.timeScale = 0f;
                IrA(EstadoFlujoJuego.Pausa);
            }
            else if (Estado == EstadoFlujoJuego.Pausa)
            {
                Time.timeScale = 1f;
                IrA(EstadoFlujoJuego.EnPartida);
            }
        }

        public void VolverAlMenuPrincipal()
        {
            Time.timeScale = 1f;
            DestruirPartida();
            Salas.CerrarSala();
            CrearFondoMenu();
            IrA(EstadoFlujoJuego.MenuPrincipal);
        }

        public void Reintentar()
        {
            Time.timeScale = 1f;
            if (configuracion.modo == ModoJuego.Historia) IrA(EstadoFlujoJuego.Briefing);
            else IniciarPartida();
        }

        public void SiguienteNivel()
        {
            if (configuracion.modo != ModoJuego.Historia || !UltimoNivelPermiteAvanzar) return;
            configuracion.nivelHistoria = Mathf.Clamp(configuracion.nivelHistoria + 1, 1, GuardadoProgreso.NivelesTotales);
            IrA(EstadoFlujoJuego.Briefing);
        }

        public void SalirDelJuego()
        {
            Application.Quit();
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#endif
        }

        // =====================================================================
        // Nivel 2: construcción de la partida
        // =====================================================================

        public void IniciarPartida()
        {
            DestruirPartida();
            DestruirFondoMenu();

            Time.timeScale = 1f;
            SistemaSaludYTraje.BloqueoGlobalDeDano = false;
            resultadoEnProceso = false;
            UltimoDesenlace = "";
            UltimoCambioCopas = null;
            UltimoNivelPermiteAvanzar = false;

            raizPartida = new GameObject("Partida en curso");
            ContextoPartida ctx = new ContextoPartida { Configuracion = configuracion.Clonar() };
            ctx.Ecosistema = raizPartida.AddComponent<GestorEquilibrioEcosistema>();

            BiomaPlaneta bioma = configuracion.ObtenerBiomaEfectivo();
            ctx.Arena = GeneradorBiomas.Generar(raizPartida.transform, bioma, Environment.TickCount);
            ctx.Arena.Raiz.GetComponent<GeneradorBiomas>().ConectarEcosistema(ctx.Ecosistema);
            camaraPrincipal.backgroundColor = ctx.Arena.Datos.ColorCielo;

            switch (configuracion.modo)
            {
                case ModoJuego.Conquista5Bases: ConstruirConquista(ctx); break;
                case ModoJuego.Modo4vs4: Construir4vs4(ctx); break;
                default: ConstruirHistoria(ctx, bioma); break;
            }

            ConfigurarCamaraJugador(ctx);

            PartidaActual = ctx;
            AlPartidaConstruida?.Invoke(ctx);
            BusEventosJuego.PublicarInicioPartida(ctx.Configuracion);
            BusEventosJuego.PublicarMensaje($"{UtilidadesTipos.NombreModo(configuracion.modo)} · {ctx.Arena.Datos.Nombre} · Sala {configuracion.codigoSala}", TipoMensajeSistema.Informacion);
            IrA(EstadoFlujoJuego.EnPartida);
        }

        private void ConstruirConquista(ContextoPartida ctx)
        {
            for (int i = 0; i < 5; i++)
            {
                ctx.Bases.Add(ZonaCapturaBase.Crear(raizPartida.transform, ctx.Arena.PosicionesBases[i], i));
            }

            int n = Mathf.Clamp(configuracion.jugadoresPorEquipo, 1, 4);
            CrearEquiposVersus(ctx, n, ObjetivoBot.CapturarBases);

            GestorConquista5Bases conquista = raizPartida.AddComponent<GestorConquista5Bases>();
            conquista.ConfigurarBases(ctx.Bases.ToArray());
            conquista.ConfigurarPuntosAparicion(BandoJuego.Destructor, ctx.Arena.AparicionDestructores);
            conquista.ConfigurarPuntosAparicion(BandoJuego.Cuidador, ctx.Arena.AparicionCuidadores);
            foreach (SistemaSaludYTraje e in ctx.EquipoDestructores) conquista.RegistrarParticipante(e);
            foreach (SistemaSaludYTraje e in ctx.EquipoCuidadores) conquista.RegistrarParticipante(e);
            conquista.AlFinalizarConResultado += ManejarFinVersus;
            ctx.Conquista = conquista;

            ctx.Arena.Fuente.EstablecerExtraccionActiva(false);
        }

        private void Construir4vs4(ContextoPartida ctx)
        {
            CrearEquiposVersus(ctx, 4, ObjetivoBot.CazarEnemigos);

            GestorModo4vs4 modo = raizPartida.AddComponent<GestorModo4vs4>();
            modo.RegistrarEquipos(ctx.EquipoDestructores, ctx.EquipoCuidadores);
            modo.AlFinalizarPartida4vs4 += ManejarFinVersus;
            modo.AlCambiarModoEspectador += ManejarEspectador;
            ctx.Modo4vs4 = modo;

            ctx.Arena.Fuente.EstablecerExtraccionActiva(false);
        }

        private void ConstruirHistoria(ContextoPartida ctx, BiomaPlaneta bioma)
        {
            int nivel = configuracion.nivelHistoria;
            Historia.IniciarNivel(nivel, configuracion.bandoJugador, configuracion.jugadoresHumanosReales);
            ComposicionNivelHistoria comp = Historia.CalcularComposicion(nivel, configuracion.bandoJugador, configuracion.jugadoresHumanosReales);
            Vector3 fuente = ctx.Arena.Fuente.transform.position;

            // Escuadrón humano de 4 (jugador local + reales + bots/mods).
            for (int i = 0; i < 4; i++)
            {
                bool esLocal = !comp.JugadorEsGuardian && i == 0;
                bool esRealRemoto = !comp.JugadorEsGuardian && i > 0 && i < comp.HumanosReales;
                string nombre = esLocal ? "Tú" : esRealRemoto ? $"Amigo {i}" : $"[Bot] {GestorSalasYEmparejamiento.ObtenerNombreBot(BandoJuego.Destructor, i + nivel)}";

                TipoArquetipo traje = esLocal ? configuracion.arquetipo : (i % 2 == 0 ? comp.ArquetipoBotsHumanos : (TipoArquetipo)((i + nivel) % 5));
                TipoArmaModular arma = esLocal ? configuracion.armaModular : ArmaVariada(i + nivel);
                float dificultad = comp.JugadorEsGuardian ? comp.DificultadBots : configuracion.dificultadBots;

                SistemaSaludYTraje h = CrearDestructor(nombre, esLocal, ctx.Arena.AparicionDestructores[i], ctx.Arena, traje, arma,
                    ObjetivoBot.ExtraerFuente, dificultad, fuente);
                ctx.EquipoDestructores.Add(h);
                if (esLocal) AsignarLocal(ctx, h);
            }

            // Guardianes: 3 (o 5 en el nivel 6) controlados por la IA si el jugador es humano,
            // o 2 (el jugador + un aliado IA) si el jugador es guardián.
            float dificultadGuardianes = Mathf.Clamp01(0.4f + 0.1f * (nivel - 1));
            for (int i = 0; i < comp.Guardianes.Count; i++)
            {
                EspecGuardian espec = comp.Guardianes[i];
                Vector3 posicion = espec.EsJugador || comp.JugadorEsGuardian
                    ? ctx.Arena.AparicionCuidadores[i]
                    : PosicionDefensiva(fuente, i, comp.Guardianes.Count);
                string nombreBase = espec.Nombre;
                string nombre = espec.EsJugador ? $"Tú ({nombreBase})" : nombreBase;

                SistemaSaludYTraje g = CrearGuardian(nombre, espec.EsJugador, posicion, ctx.Arena, espec.Elemento, espec.Rol,
                    espec.RotaElementos, false, bioma, ObjetivoParaRol(espec.Rol), dificultadGuardianes, fuente);

                if (!comp.JugadorEsGuardian)
                {
                    g.GetComponent<ControladorGuardianElemental>().EstablecerEscaladoJefe(comp.MultiplicadorVidaGuardianesIA, comp.MultiplicadorDanoGuardianesIA);
                }
                ctx.EquipoCuidadores.Add(g);
                if (espec.EsJugador) AsignarLocal(ctx, g);
            }

            ctx.Arena.Fuente.EstablecerExtraccionActiva(true);
            Historia.RegistrarParticipantes(ctx.EquipoDestructores, ctx.EquipoCuidadores);
            ctx.Historia = Historia;
        }

        /// <summary>Reparte a los guardianes defensores en abanico alrededor de la fuente, del lado norte.</summary>
        private static Vector3 PosicionDefensiva(Vector3 fuente, int indice, int total)
        {
            float t = total <= 1 ? 0.5f : indice / (float)(total - 1);
            float angulo = Mathf.Lerp(-60f, 60f, t) * Mathf.Deg2Rad;
            return fuente + new Vector3(Mathf.Sin(angulo) * 11f, 1.1f, Mathf.Cos(angulo) * 11f);
        }

        /// <summary>
        /// Coordinación por roles: la vanguardia sale a cazar y cubre los flancos; el control y la artillería
        /// defienden la fuente (la artillería, además, mantiene la distancia; ver ControladorBotIA).
        /// </summary>
        private static ObjetivoBot ObjetivoParaRol(RolGuardian rol)
        {
            return rol == RolGuardian.Vanguardia ? ObjetivoBot.CazarEnemigos : ObjetivoBot.DefenderFuente;
        }

        private void CrearEquiposVersus(ContextoPartida ctx, int porEquipo, ObjetivoBot objetivo)
        {
            Vector3 fuente = ctx.Arena.Fuente.transform.position;
            BiomaPlaneta bioma = ctx.Arena.Datos.Bioma;
            int desfase = UnityEngine.Random.Range(0, 5);

            for (int i = 0; i < porEquipo; i++)
            {
                bool esLocal = configuracion.bandoJugador == BandoJuego.Destructor && i == 0;
                string nombre = esLocal ? "Tú" : $"[Bot] {GestorSalasYEmparejamiento.ObtenerNombreBot(BandoJuego.Destructor, i + desfase)}";
                TipoArquetipo traje = esLocal ? configuracion.arquetipo : (TipoArquetipo)((i + desfase) % 5);
                TipoArmaModular arma = esLocal ? configuracion.armaModular : ArmaVariada(i + desfase);
                SistemaSaludYTraje d = CrearDestructor(nombre, esLocal, ctx.Arena.AparicionDestructores[i], ctx.Arena, traje, arma,
                    objetivo, configuracion.dificultadBots, fuente);
                ctx.EquipoDestructores.Add(d);
                if (esLocal) AsignarLocal(ctx, d);
            }

            for (int i = 0; i < porEquipo; i++)
            {
                bool esLocal = configuracion.bandoJugador == BandoJuego.Cuidador && i == 0;
                TipoElemento elemento = esLocal ? configuracion.elementoGuardian
                    : UtilidadesTipos.ElementosJugables[(i + desfase) % UtilidadesTipos.ElementosJugables.Length];
                RolGuardian rol = esLocal ? configuracion.varianteGuardian : (RolGuardian)(i % 3);
                string nombre = esLocal ? "Tú" : $"[Bot] {DatosGuardianElemental.CrearVariante(elemento, rol).nombreGuardian}";
                SistemaSaludYTraje c = CrearGuardian(nombre, esLocal, ctx.Arena.AparicionCuidadores[i], ctx.Arena, elemento, rol, false, true, bioma,
                    objetivo, configuracion.dificultadBots, fuente);
                ctx.EquipoCuidadores.Add(c);
                if (esLocal) AsignarLocal(ctx, c);
            }
        }

        private static TipoArmaModular ArmaVariada(int indice)
        {
            // Los bots reparten las 5 armas para que cada escuadrón mezcle distancias.
            return (TipoArmaModular)(Mathf.Abs(indice) % 5);
        }

        private void AsignarLocal(ContextoPartida ctx, SistemaSaludYTraje entidad)
        {
            ctx.JugadorLocal = entidad;
            ctx.CombateLocal = entidad.GetComponent<SistemaCombateArquetipo>();
            ctx.GuardianLocal = entidad.GetComponent<ControladorGuardianElemental>();
            ctx.EstadosLocal = entidad.GetComponent<SistemaEstadosAlterados>();
        }

        // ---------------- Fábrica de personajes ----------------

        private GameObject CrearBasePersonaje(string nombre, bool esGuardian, Vector3 posicion, DescripcionArena arena)
        {
            GameObject go = new GameObject(nombre);
            go.transform.SetParent(raizPartida.transform, false);
            go.transform.SetPositionAndRotation(posicion, arena.RotacionHaciaCentro(posicion));

            CharacterController cc = go.AddComponent<CharacterController>();
            cc.height = esGuardian ? 2.7f : 1.8f;
            cc.radius = esGuardian ? 0.75f : 0.4f;
            cc.center = new Vector3(0f, cc.height * 0.5f, 0f);
            cc.stepOffset = esGuardian ? 0.6f : 0.4f;
            cc.slopeLimit = 50f;

            go.AddComponent<EntradaPersonaje>();
            return go;
        }

        private SistemaSaludYTraje CrearDestructor(string nombre, bool esLocal, Vector3 posicion, DescripcionArena arena,
            TipoArquetipo tipoArquetipo, TipoArmaModular tipoArma, ObjetivoBot objetivo, float dificultad, Vector3 fuente)
        {
            GameObject go = CrearBasePersonaje(nombre, false, posicion, arena);
            DatosArquetipo arquetipo = DatosArquetipo.CrearPorDefecto(tipoArquetipo);
            DatosArmaModular arma = DatosArmaModular.CrearPorDefecto(tipoArma);

            ConstructorModelosPersonajes.ConstruirHumano(go.transform, arquetipo, arma, esLocal);

            SistemaSaludYTraje salud = go.AddComponent<SistemaSaludYTraje>();
            salud.ConfigurarIdentidad(nombre, BandoJuego.Destructor, esLocal);
            salud.AsignarArquetipo(arquetipo);

            ControladorMovimientoAssaultCube mov = go.AddComponent<ControladorMovimientoAssaultCube>();
            go.AddComponent<SistemaEstadosAlterados>();

            SistemaCombateArquetipo combate = go.AddComponent<SistemaCombateArquetipo>();
            combate.Configurar(arquetipo, arma);
            go.AddComponent<AnimadorProcedural>();

            if (esLocal) go.AddComponent<EntradaJugadorLocal>();
            else go.AddComponent<ControladorBotIA>().Configurar(objetivo, dificultad, fuente);

            EtiquetaNombre.Crear(salud, 2.25f);
            salud.EstablecerPuntoAparicion(posicion, go.transform.rotation);
            salud.RefrescarCacheVisual();
            return salud;
        }

        private SistemaSaludYTraje CrearGuardian(string nombre, bool esLocal, Vector3 posicion, DescripcionArena arena, TipoElemento elemento,
            RolGuardian rol, bool rotaElementos, bool modoVersus, BiomaPlaneta bioma, ObjetivoBot objetivo, float dificultad, Vector3 fuente)
        {
            GameObject go = CrearBasePersonaje(nombre, true, posicion, arena);
            DatosGuardianElemental datos = rotaElementos ? DatosGuardianElemental.CrearNexus(elemento, rol) : DatosGuardianElemental.CrearVariante(elemento, rol);
            ConstructorModelosPersonajes.ConstruirGuardian(go.transform, elemento, rol, datos.colorPrimario, datos.colorSecundario);

            SistemaSaludYTraje salud = go.AddComponent<SistemaSaludYTraje>();
            salud.ConfigurarIdentidad(nombre, BandoJuego.Cuidador, esLocal);

            ControladorMovimientoAssaultCube mov = go.AddComponent<ControladorMovimientoAssaultCube>();
            mov.ConfigurarAlturaOjos(2.35f);
            go.AddComponent<SistemaEstadosAlterados>();

            ControladorGuardianElemental guardian = go.AddComponent<ControladorGuardianElemental>();
            PuntoDebil.Configurar(go, elemento);
            salud.ConfigurarAguante(rol == RolGuardian.Vanguardia ? 100f : rol == RolGuardian.Control ? 70f : 50f);
            guardian.EstablecerRotacionElementos(rotaElementos);
            guardian.ConfigurarGuardian(elemento, rol);
            guardian.EstablecerModoVersus(modoVersus);
            DatosBioma db = DatosBioma.Obtener(bioma);
            bool escenarioConvergente = bioma == BiomaPlaneta.NexusPrimordial || bioma == BiomaPlaneta.ConvergenciaTotal;
            bool golemEnSuForja = elemento == TipoElemento.Tierra && bioma == BiomaPlaneta.CalderaForjas;
            guardian.EstablecerAfinidadBioma(escenarioConvergente || golemEnSuForja || (db.TieneElemento && db.Elemento == elemento));

            if (esLocal) go.AddComponent<EntradaJugadorLocal>();
            else go.AddComponent<ControladorBotIA>().Configurar(objetivo, dificultad, fuente);

            go.AddComponent<AnimadorProcedural>();
            EtiquetaNombre.Crear(salud, 3.2f);
            salud.EstablecerPuntoAparicion(posicion, go.transform.rotation);
            salud.RefrescarCacheVisual();
            return salud;
        }

        // ---------------- Cámara ----------------

        private void ConfigurarCamaraJugador(ContextoPartida ctx)
        {
            // Se desactiva antes de destruir: Destroy es diferido y, si su LateUpdate llegara a ejecutarse
            // en este fotograma, movería la cámara ya emparentada al jugador.
            OrbitaCamaraMenu orbita = camaraPrincipal.GetComponent<OrbitaCamaraMenu>();
            if (orbita != null)
            {
                orbita.enabled = false;
                Destroy(orbita);
            }

            SistemaSaludYTraje local = ctx.JugadorLocal;
            if (local == null) return;
            ControladorMovimientoAssaultCube mov = local.GetComponent<ControladorMovimientoAssaultCube>();

            camaraPrincipal.transform.SetParent(mov.PivoteCamara, false);
            if (ctx.JugadorEsDestructor)
            {
                // Perspectiva Destructor: primera persona, campo de visión amplio de arena shooter.
                foreach (Renderer r in local.GetComponentsInChildren<Renderer>())
                {
                    r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.ShadowsOnly;
                }
                camaraPrincipal.transform.localPosition = Vector3.zero;
                camaraPrincipal.transform.localRotation = Quaternion.identity;
                camaraPrincipal.fieldOfView = 80f;
                camaraPrincipal.nearClipPlane = 0.05f;
                if (ctx.CombateLocal != null) ModeloArmaPrimeraPersona.Crear(camaraPrincipal.transform, ctx.CombateLocal, mov);
            }
            else
            {
                // Perspectiva Guardián: tercera persona sobre el hombro del coloso elemental.
                camaraPrincipal.transform.localPosition = new Vector3(1.2f, 0.7f, -6.2f);
                camaraPrincipal.transform.localRotation = Quaternion.Euler(6f, 0f, 0f);
                camaraPrincipal.fieldOfView = 70f;
                camaraPrincipal.nearClipPlane = 0.1f;
            }
            local.RefrescarCacheVisual();

            ctx.Espectador = raizPartida.AddComponent<CamaraEspectador>();
            ctx.Espectador.Configurar(camaraPrincipal, () => ObtenerCompanerosVivos(ctx));
        }

        private List<SistemaSaludYTraje> ObtenerCompanerosVivos(ContextoPartida ctx)
        {
            List<SistemaSaludYTraje> lista = new List<SistemaSaludYTraje>();
            List<SistemaSaludYTraje> equipo = ctx.JugadorEsDestructor ? ctx.EquipoDestructores : ctx.EquipoCuidadores;
            foreach (SistemaSaludYTraje m in equipo)
            {
                if (m != null && !m.EstaMuerto && m != ctx.JugadorLocal) lista.Add(m);
            }
            return lista;
        }

        private void ManejarEspectador(bool activo)
        {
            CamaraEspectador espectador = PartidaActual != null ? PartidaActual.Espectador : null;
            if (espectador == null) return;
            if (activo) espectador.Activar();
            else espectador.Desactivar();
        }

        // =====================================================================
        // Nivel 3: final de partida, resultados y limpieza
        // =====================================================================

        private void ManejarFinVersus(ResultadoEnfrentamiento resultado, string mensaje)
        {
            if (resultadoEnProceso || PartidaActual == null) return;
            resultadoEnProceso = true;

            ResultadoPartida r = CrearResultadoBase(resultado, mensaje);
            if (configuracion.modo == ModoJuego.Modo4vs4)
            {
                bool salaConBots = configuracion.tipoSala == TipoSalaConexion.ConBots;
                UltimoCambioCopas = Clasificacion.RegistrarResultado4vs4(r.GanoJugadorLocal, r.FueEmpate, salaConBots);
                r.detalle = UltimoCambioCopas.Value.Explicacion;
            }
            else
            {
                ConquistaDetalle(r);
            }
            rutinaResultados = StartCoroutine(RutinaMostrarResultados(r));
        }

        private void ConquistaDetalle(ResultadoPartida r)
        {
            GestorConquista5Bases c = PartidaActual.Conquista;
            if (c == null) return;
            r.detalle = $"Tiempo de control acumulado — Destructores: {Mathf.RoundToInt(c.TiempoControlDestructores)} s · " +
                        $"Cuidadores: {Mathf.RoundToInt(c.TiempoControlCuidadores)} s.";
        }

        private void ManejarFinHistoria(ResultadoEnfrentamiento resultado, string mensaje, bool avanza)
        {
            if (resultadoEnProceso || PartidaActual == null) return;
            resultadoEnProceso = true;
            UltimoNivelPermiteAvanzar = avanza;

            ResultadoPartida r = CrearResultadoBase(resultado, mensaje);
            r.nivelHistoria = configuracion.nivelHistoria;
            NivelNarrativo n = DatosNarrativaHistoria.ObtenerNivel(configuracion.nivelHistoria);
            r.detalle = $"{n.Titulo}\nIntegridad del ecosistema al final: {Mathf.RoundToInt(r.integridadEcosistemaFinal)}%.\n" +
                        $"Reflexión: {n.ReflexionSocial}";
            rutinaResultados = StartCoroutine(RutinaMostrarResultados(r));
        }

        private void ManejarHistoriaCompletada(string desenlace)
        {
            UltimoDesenlace = desenlace;
        }

        private ResultadoPartida CrearResultadoBase(ResultadoEnfrentamiento resultado, string mensaje)
        {
            return new ResultadoPartida
            {
                modo = configuracion.modo,
                resultado = resultado,
                bandoJugadorLocal = configuracion.bandoJugador,
                mensajePrincipal = mensaje,
                integridadEcosistemaFinal = PartidaActual != null && PartidaActual.Ecosistema != null ? PartidaActual.Ecosistema.Integridad : 100f
            };
        }

        private IEnumerator RutinaMostrarResultados(ResultadoPartida r)
        {
            BusEventosJuego.PublicarMensaje(r.mensajePrincipal, r.GanoJugadorLocal ? TipoMensajeSistema.Exito : TipoMensajeSistema.Critico);
            yield return new WaitForSecondsRealtime(esperaAntesDeResultados);

            r.historiaCompletada = !string.IsNullOrEmpty(UltimoDesenlace);
            UltimoResultado = r;
            BusEventosJuego.PublicarFinPartida(r);
            AlResultadosListos?.Invoke(r);
            rutinaResultados = null;
            IrA(EstadoFlujoJuego.Resultados);
        }

        private void DestruirPartida()
        {
            if (rutinaResultados != null)
            {
                StopCoroutine(rutinaResultados);
                rutinaResultados = null;
            }

            Historia?.CancelarNivel();

            if (PartidaActual != null)
            {
                PartidaActual.Espectador?.Desactivar();
                if (PartidaActual.Conquista != null) PartidaActual.Conquista.AlFinalizarConResultado -= ManejarFinVersus;
                if (PartidaActual.Modo4vs4 != null)
                {
                    PartidaActual.Modo4vs4.AlFinalizarPartida4vs4 -= ManejarFinVersus;
                    PartidaActual.Modo4vs4.AlCambiarModoEspectador -= ManejarEspectador;
                }
            }

            // La cámara se separa ANTES de destruir la partida para no destruirla con el jugador.
            if (camaraPrincipal != null)
            {
                camaraPrincipal.transform.SetParent(null, true);
                for (int i = camaraPrincipal.transform.childCount - 1; i >= 0; i--)
                {
                    Destroy(camaraPrincipal.transform.GetChild(i).gameObject);
                }
            }

            if (raizPartida != null)
            {
                Destroy(raizPartida);
                raizPartida = null;
                if (PartidaActual != null) AlPartidaDestruida?.Invoke();
            }

            PartidaActual = null;
            SistemaSaludYTraje.BloqueoGlobalDeDano = false;
            resultadoEnProceso = false;
        }

        private void CrearFondoMenu()
        {
            DestruirFondoMenu();
            if (!mostrarFondoAnimadoEnMenus || camaraPrincipal == null) return;

            raizFondoMenu = new GameObject("Fondo animado del menú");
            BiomaPlaneta bioma = (BiomaPlaneta)UnityEngine.Random.Range(0, 6);
            DescripcionArena arena = GeneradorBiomas.Generar(raizFondoMenu.transform, bioma, Environment.TickCount);
            camaraPrincipal.backgroundColor = arena.Datos.ColorCielo;
            camaraPrincipal.fieldOfView = 60f;
            camaraPrincipal.nearClipPlane = 0.1f;
            camaraPrincipal.transform.SetParent(null, true);
            if (camaraPrincipal.GetComponent<OrbitaCamaraMenu>() == null)
            {
                camaraPrincipal.gameObject.AddComponent<OrbitaCamaraMenu>();
            }
        }

        private void DestruirFondoMenu()
        {
            if (raizFondoMenu != null)
            {
                Destroy(raizFondoMenu);
                raizFondoMenu = null;
            }
        }
    }
}
