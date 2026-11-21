using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace VoidformBioForge
{
    /// <summary>
    /// Gestiona el Modo 4vs4 (eliminación por rondas).
    /// -----------------------------------------------------------------
    /// Nivel 1: dos equipos de 4 integrantes; la ronda termina cuando un equipo queda completamente eliminado.
    /// Nivel 2: MUERTE Y ESPECTADOR: quien muere (o cuyo traje explota) no revive hasta que acabe la ronda;
    ///          mientras tanto puede espectear a sus compañeros vivos. La partida se juega a 3 rondas.
    /// Nivel 3: reglas exactas de empate y casos límite:
    ///   • MUERTE SIMULTÁNEA: cuando el último integrante de un equipo cae, se abre una ventana de
    ///     0,25 s. Si en esa ventana cae también el último del otro equipo (por ejemplo, por la
    ///     explosión del traje del humano que acaba de morir), la ronda es EMPATE.
    ///   • TIEMPO LÍMITE DE RONDA (opcional, 180 s): gana quien tenga más vivos; si hay igualdad, más
    ///     vida total en porcentaje; si sigue igual, la ronda es empate.
    ///   • FINAL DE PARTIDA: tras 3 rondas gana quien tenga más rondas ganadas; si están igualados
    ///     (1-1 con un empate, 0-0 con tres empates…), la partida es EMPATE. Si un equipo ya no puede ser
    ///     alcanzado (2-0), la serie termina antes (configurable).
    ///   • Entre rondas se bloquea el daño global, de modo que nada de lo que ocurra durante la
    ///     transición (explosiones tardías, quemaduras) altera el resultado ya decidido.
    /// </summary>
    public class GestorModo4vs4 : MonoBehaviour
    {
        [SerializeField] private int jugadoresPorEquipo = 4;
        [SerializeField] private int totalRondas = 3;
        [SerializeField] private float ventanaMuerteSimultaneaSegundos = 0.25f;
        [SerializeField] private float pausaEntreRondasSegundos = 4f;
        [SerializeField] private bool usarTiempoLimiteRonda = true;
        [SerializeField] private float tiempoLimiteRondaSegundos = 180f;
        [SerializeField] private bool terminarSiGanadorMatematico = true;
        [SerializeField] private bool iniciarAutomaticamente = true;
        [SerializeField] private List<SistemaSaludYTraje> equipoDestructores = new List<SistemaSaludYTraje>();
        [SerializeField] private List<SistemaSaludYTraje> equipoCuidadores = new List<SistemaSaludYTraje>();

        private bool rondaEnCurso;
        private bool resolviendoRonda;
        private bool partidaIniciada;
        private float tiempoRestanteRonda;

        public int RondaActual { get; private set; } = 1;
        public int TotalRondas => totalRondas;
        public int JugadoresPorEquipo => jugadoresPorEquipo;
        public int RondasGanadasDestructores { get; private set; }
        public int RondasGanadasCuidadores { get; private set; }
        public int RondasEmpatadas { get; private set; }
        public bool JugadorLocalEnEspectador { get; private set; }
        public bool PartidaTerminada { get; private set; }
        public float TiempoRestanteRonda => tiempoRestanteRonda;
        public ResultadoEnfrentamiento ResultadoFinal { get; private set; } = ResultadoEnfrentamiento.Pendiente;
        public IReadOnlyList<SistemaSaludYTraje> EquipoDestructores => equipoDestructores;
        public IReadOnlyList<SistemaSaludYTraje> EquipoCuidadores => equipoCuidadores;

        public event Action<int, int, int, int> AlActualizarMarcadorRondas;
        public event Action<bool> AlCambiarModoEspectador;
        public event Action<string> AlFinalizarRondaOPartida;
        public event Action<int, ResultadoEnfrentamiento, string> AlFinalizarRonda;
        public event Action<ResultadoEnfrentamiento, string> AlFinalizarPartida4vs4;
        public event Action<int> AlIniciarRonda;
        public event Action<float> AlActualizarRelojRonda;
        public event Action<int, int> AlActualizarVivos;

        // ================== Configuración ==================

        public void RegistrarEquipos(List<SistemaSaludYTraje> destructores, List<SistemaSaludYTraje> cuidadores)
        {
            DesuscribirJugadores();
            equipoDestructores = new List<SistemaSaludYTraje>(destructores);
            equipoCuidadores = new List<SistemaSaludYTraje>(cuidadores);
        }

        private void Start()
        {
            if (iniciarAutomaticamente && !partidaIniciada) IniciarPartida();
        }

        public void IniciarPartida()
        {
            partidaIniciada = true;
            PartidaTerminada = false;
            ResultadoFinal = ResultadoEnfrentamiento.Pendiente;
            RondaActual = 1;
            RondasGanadasDestructores = 0;
            RondasGanadasCuidadores = 0;
            RondasEmpatadas = 0;

            if (equipoDestructores.Count == 0 && equipoCuidadores.Count == 0)
            {
                foreach (SistemaSaludYTraje e in FindObjectsByType<SistemaSaludYTraje>(FindObjectsSortMode.None))
                {
                    if (e.Bando == BandoJuego.Destructor) equipoDestructores.Add(e);
                    else equipoCuidadores.Add(e);
                }
            }

            if (equipoDestructores.Count != jugadoresPorEquipo || equipoCuidadores.Count != jugadoresPorEquipo)
            {
                Debug.LogWarning($"[Voidform] 4vs4 con equipos de {equipoDestructores.Count} y {equipoCuidadores.Count} (se esperaban {jugadoresPorEquipo}).");
            }

            DesuscribirJugadores();
            SuscribirJugadores(equipoDestructores);
            SuscribirJugadores(equipoCuidadores);

            ComenzarRonda();
        }

        private void SuscribirJugadores(List<SistemaSaludYTraje> equipo)
        {
            foreach (SistemaSaludYTraje jugador in equipo)
            {
                if (jugador != null) jugador.AlMorir += ManejarMuerteEn4vs4;
            }
        }

        private void DesuscribirJugadores()
        {
            foreach (SistemaSaludYTraje j in equipoDestructores) if (j != null) j.AlMorir -= ManejarMuerteEn4vs4;
            foreach (SistemaSaludYTraje j in equipoCuidadores) if (j != null) j.AlMorir -= ManejarMuerteEn4vs4;
        }

        private void OnDestroy()
        {
            DesuscribirJugadores();
        }

        // ================== Nivel 1: ciclo de ronda ==================

        private void ComenzarRonda()
        {
            foreach (SistemaSaludYTraje d in equipoDestructores) d?.RevivirEnPuntoAparicion();
            foreach (SistemaSaludYTraje c in equipoCuidadores) c?.RevivirEnPuntoAparicion();

            SistemaSaludYTraje.BloqueoGlobalDeDano = false;
            rondaEnCurso = true;
            resolviendoRonda = false;
            tiempoRestanteRonda = tiempoLimiteRondaSegundos;

            EstablecerEspectador(false);
            AlIniciarRonda?.Invoke(RondaActual);
            NotificarMarcador();
            NotificarVivos();
            BusEventosJuego.PublicarMensaje($"Ronda {RondaActual} de {totalRondas}: ¡eliminad al equipo rival!", TipoMensajeSistema.Narrativa);
        }

        private void Update()
        {
            if (!rondaEnCurso || resolviendoRonda || PartidaTerminada) return;
            if (!usarTiempoLimiteRonda) return;

            tiempoRestanteRonda = Mathf.Max(0f, tiempoRestanteRonda - Time.deltaTime);
            AlActualizarRelojRonda?.Invoke(tiempoRestanteRonda);
            if (tiempoRestanteRonda <= 0f)
            {
                ResolverRondaPorTiempo();
            }
        }

        // ================== Nivel 2: muertes y espectador ==================

        private void ManejarMuerteEn4vs4(SistemaSaludYTraje jugadorCaido)
        {
            if (PartidaTerminada || jugadorCaido == null || !rondaEnCurso) return;

            NotificarVivos();

            // Solo el jugador local entra en modo espectador (los bots no necesitan cámara).
            if (jugadorCaido.EsJugadorLocal)
            {
                EstablecerEspectador(true);
            }

            if (resolviendoRonda) return; // Ya hay una ventana de simultaneidad abierta.

            if (TodosMuertos(equipoDestructores) || TodosMuertos(equipoCuidadores))
            {
                resolviendoRonda = true;
                StartCoroutine(RutinaResolverRondaConVentana());
            }
        }

        private void EstablecerEspectador(bool activo)
        {
            if (JugadorLocalEnEspectador == activo) return;
            JugadorLocalEnEspectador = activo;
            AlCambiarModoEspectador?.Invoke(activo);
        }

        public bool TodosMuertos(IReadOnlyList<SistemaSaludYTraje> equipo)
        {
            if (equipo.Count == 0) return false;
            foreach (SistemaSaludYTraje miembro in equipo)
            {
                if (miembro != null && !miembro.EstaMuerto) return false;
            }
            return true;
        }

        public int ContarVivos(IReadOnlyList<SistemaSaludYTraje> equipo)
        {
            int vivos = 0;
            foreach (SistemaSaludYTraje m in equipo) if (m != null && !m.EstaMuerto) vivos++;
            return vivos;
        }

        /// <summary>Compañeros vivos del jugador local (los usa la cámara de espectador).</summary>
        public List<SistemaSaludYTraje> ObtenerCompanerosVivos(BandoJuego bando)
        {
            List<SistemaSaludYTraje> lista = new List<SistemaSaludYTraje>();
            foreach (SistemaSaludYTraje m in bando == BandoJuego.Destructor ? equipoDestructores : equipoCuidadores)
            {
                if (m != null && !m.EstaMuerto) lista.Add(m);
            }
            return lista;
        }

        // ================== Nivel 3: resolución de ronda ==================

        private IEnumerator RutinaResolverRondaConVentana()
        {
            // Ventana de simultaneidad: el daño sigue activo para que una explosión de traje pueda
            // alcanzar al último rival y convertir la ronda en empate.
            yield return new WaitForSeconds(ventanaMuerteSimultaneaSegundos);

            bool todosDestructoresMuertos = TodosMuertos(equipoDestructores);
            bool todosCuidadoresMuertos = TodosMuertos(equipoCuidadores);

            if (todosDestructoresMuertos && todosCuidadoresMuertos)
            {
                CerrarRonda(ResultadoEnfrentamiento.Empate, $"Ronda {RondaActual}: EMPATE por eliminación simultánea de ambos equipos.");
            }
            else if (todosDestructoresMuertos)
            {
                CerrarRonda(ResultadoEnfrentamiento.VictoriaCuidadores, $"Ronda {RondaActual}: ganan los Cuidadores.");
            }
            else if (todosCuidadoresMuertos)
            {
                CerrarRonda(ResultadoEnfrentamiento.VictoriaDestructores, $"Ronda {RondaActual}: ganan los Destructores.");
            }
            else
            {
                resolviendoRonda = false; // Caso imposible en reglas normales; se reanuda la ronda.
            }
        }

        private void ResolverRondaPorTiempo()
        {
            resolviendoRonda = true;
            int vivosD = ContarVivos(equipoDestructores);
            int vivosC = ContarVivos(equipoCuidadores);

            if (vivosD != vivosC)
            {
                bool ganaD = vivosD > vivosC;
                CerrarRonda(ganaD ? ResultadoEnfrentamiento.VictoriaDestructores : ResultadoEnfrentamiento.VictoriaCuidadores,
                    $"Ronda {RondaActual}: tiempo agotado. Ganan los {(ganaD ? "Destructores" : "Cuidadores")} por supervivientes ({vivosD} contra {vivosC}).");
                return;
            }

            float vidaD = PorcentajeVidaTotal(equipoDestructores);
            float vidaC = PorcentajeVidaTotal(equipoCuidadores);
            if (Mathf.Abs(vidaD - vidaC) > 0.01f)
            {
                bool ganaD = vidaD > vidaC;
                CerrarRonda(ganaD ? ResultadoEnfrentamiento.VictoriaDestructores : ResultadoEnfrentamiento.VictoriaCuidadores,
                    $"Ronda {RondaActual}: tiempo agotado con {vivosD} vivos por lado. Ganan los {(ganaD ? "Destructores" : "Cuidadores")} por vida restante.");
                return;
            }

            CerrarRonda(ResultadoEnfrentamiento.Empate, $"Ronda {RondaActual}: tiempo agotado en igualdad total. EMPATE.");
        }

        private static float PorcentajeVidaTotal(IReadOnlyList<SistemaSaludYTraje> equipo)
        {
            float suma = 0f;
            foreach (SistemaSaludYTraje m in equipo) if (m != null && !m.EstaMuerto) suma += m.PorcentajeVida;
            return suma;
        }

        private void CerrarRonda(ResultadoEnfrentamiento resultado, string mensaje)
        {
            rondaEnCurso = false;
            SistemaSaludYTraje.BloqueoGlobalDeDano = true;

            if (resultado == ResultadoEnfrentamiento.VictoriaDestructores) RondasGanadasDestructores++;
            else if (resultado == ResultadoEnfrentamiento.VictoriaCuidadores) RondasGanadasCuidadores++;
            else RondasEmpatadas++;

            AlFinalizarRondaOPartida?.Invoke(mensaje);
            AlFinalizarRonda?.Invoke(RondaActual, resultado, mensaje);
            BusEventosJuego.PublicarMensaje(mensaje, resultado == ResultadoEnfrentamiento.Empate ? TipoMensajeSistema.Advertencia : TipoMensajeSistema.Exito);
            NotificarMarcador();

            if (SerieDecidida())
            {
                FinalizarSerie();
            }
            else
            {
                StartCoroutine(RutinaSiguienteRonda());
            }
        }

        private bool SerieDecidida()
        {
            int jugadas = RondaActual;
            int restantes = totalRondas - jugadas;
            if (restantes <= 0) return true;
            if (!terminarSiGanadorMatematico) return false;
            return RondasGanadasDestructores > RondasGanadasCuidadores + restantes
                || RondasGanadasCuidadores > RondasGanadasDestructores + restantes;
        }

        private IEnumerator RutinaSiguienteRonda()
        {
            yield return new WaitForSeconds(pausaEntreRondasSegundos);
            RondaActual++;
            ComenzarRonda();
        }

        private void FinalizarSerie()
        {
            PartidaTerminada = true;
            string mensaje;

            if (RondasGanadasDestructores > RondasGanadasCuidadores)
            {
                ResultadoFinal = ResultadoEnfrentamiento.VictoriaDestructores;
                mensaje = $"¡Partida 4vs4 finalizada! Ganan los Destructores ({RondasGanadasDestructores} - {RondasGanadasCuidadores}, empates: {RondasEmpatadas}).";
            }
            else if (RondasGanadasCuidadores > RondasGanadasDestructores)
            {
                ResultadoFinal = ResultadoEnfrentamiento.VictoriaCuidadores;
                mensaje = $"¡Partida 4vs4 finalizada! Ganan los Cuidadores ({RondasGanadasCuidadores} - {RondasGanadasDestructores}, empates: {RondasEmpatadas}).";
            }
            else
            {
                ResultadoFinal = ResultadoEnfrentamiento.Empate;
                mensaje = $"¡Partida 4vs4 finalizada en EMPATE! (Victorias: {RondasGanadasDestructores}-{RondasGanadasCuidadores}, empates: {RondasEmpatadas}).";
            }

            AlFinalizarRondaOPartida?.Invoke(mensaje);
            AlFinalizarPartida4vs4?.Invoke(ResultadoFinal, mensaje);
        }

        private void NotificarMarcador()
        {
            AlActualizarMarcadorRondas?.Invoke(RondaActual, RondasGanadasDestructores, RondasGanadasCuidadores, RondasEmpatadas);
        }

        private void NotificarVivos()
        {
            AlActualizarVivos?.Invoke(ContarVivos(equipoDestructores), ContarVivos(equipoCuidadores));
        }
    }
}
