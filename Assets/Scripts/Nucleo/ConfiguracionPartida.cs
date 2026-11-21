using System;
using UnityEngine;

namespace VoidformBioForge
{
    /// <summary>
    /// Agrupa TODAS las decisiones que el jugador toma en los menús antes de jugar:
    /// bando, modo, sala, nivel de historia, traje, arma, elemento y bioma.
    /// La interfaz la rellena paso a paso y el GestorFlujoPartida la lee para construir la partida.
    /// </summary>
    [Serializable]
    public class ConfiguracionPartida
    {
        // -------------------------------------------------------------
        // Nivel 1 — Elecciones básicas
        // -------------------------------------------------------------
        [Tooltip("Bando con el que juega el jugador local.")]
        public BandoJuego bandoJugador = BandoJuego.Destructor;

        [Tooltip("Modo de juego elegido en el selector de modo.")]
        public ModoJuego modo = ModoJuego.Conquista5Bases;

        [Tooltip("Forma de entrar a la partida (solo en modos que admiten salas).")]
        public TipoSalaConexion tipoSala = TipoSalaConexion.ConBots;

        // -------------------------------------------------------------
        // Nivel 2 — Personalización y reglas
        // -------------------------------------------------------------
        [Tooltip("Traje elegido cuando el jugador es Destructor.")]
        public TipoTraje trajeDestructor = TipoTraje.CineticoAsalto;

        [Tooltip("Arma principal elegida cuando el jugador es Destructor.")]
        public TipoArma armaPrincipal = TipoArma.RifleExtractor;

        [Tooltip("Elemento elegido cuando el jugador es Cuidador en modos versus.")]
        public TipoElemento elementoGuardian = TipoElemento.Arbol;

        [Tooltip("Nivel de Historia (1 a 5) que se va a jugar.")]
        [Range(1, 5)] public int nivelHistoria = 1;

        [Tooltip("Jugadores humanos reales en el equipo del jugador (incluido él).")]
        [Range(1, 4)] public int jugadoresHumanosReales = 1;

        [Tooltip("Integrantes por equipo en modos versus (4 en 4vs4 y Conquista).")]
        [Range(1, 4)] public int jugadoresPorEquipo = 4;

        // -------------------------------------------------------------
        // Nivel 3 — Datos de sala y entorno
        // -------------------------------------------------------------
        [Tooltip("Código de la sala actual (creada, de amigos o pública).")]
        public string codigoSala = "";

        [Tooltip("Si la sala creada es privada (solo con código) o visible públicamente.")]
        public bool salaPrivada = true;

        [Tooltip("Bioma para modos versus. -1 significa aleatorio.")]
        public int indiceBiomaVersus = -1;

        [Tooltip("Dificultad de los bots (0 = fácil, 1 = experta).")]
        [Range(0f, 1f)] public float dificultadBots = 0.5f;

        /// <summary>Indica si este modo permite elegir tipo de sala.</summary>
        public bool ModoAdmiteSalas => modo != ModoJuego.Historia || bandoJugador == BandoJuego.Destructor;

        /// <summary>Bioma real que se usará en la partida según modo, nivel y elección de sala.</summary>
        public BiomaPlaneta ObtenerBiomaEfectivo()
        {
            if (modo == ModoJuego.Historia)
            {
                return (BiomaPlaneta)Mathf.Clamp(nivelHistoria - 1, 0, 4);
            }

            if (indiceBiomaVersus >= 0 && indiceBiomaVersus <= 4)
            {
                return (BiomaPlaneta)indiceBiomaVersus;
            }

            return (BiomaPlaneta)UnityEngine.Random.Range(0, 5);
        }

        public ConfiguracionPartida Clonar()
        {
            return (ConfiguracionPartida)MemberwiseClone();
        }

        public override string ToString()
        {
            return $"[{UtilidadesTipos.NombreModo(modo)} | {UtilidadesTipos.NombreBandoSingular(bandoJugador)} | " +
                   $"{UtilidadesTipos.NombreSala(tipoSala)} | Sala {codigoSala} | Nivel {nivelHistoria}]";
        }
    }

    /// <summary>
    /// Resultado final que se publica al terminar cualquier partida o nivel.
    /// La interfaz de resultados y la clasificación lo consumen.
    /// </summary>
    [Serializable]
    public class ResultadoPartida
    {
        public ModoJuego modo;
        public ResultadoEnfrentamiento resultado = ResultadoEnfrentamiento.Pendiente;
        public BandoJuego bandoJugadorLocal;
        public string mensajePrincipal = "";
        public string detalle = "";
        public int nivelHistoria;
        public bool historiaCompletada;
        public float integridadEcosistemaFinal = 100f;

        public bool GanoJugadorLocal => resultado == UtilidadesTipos.VictoriaDe(bandoJugadorLocal);
        public bool FueEmpate => resultado == ResultadoEnfrentamiento.Empate;
    }
}
