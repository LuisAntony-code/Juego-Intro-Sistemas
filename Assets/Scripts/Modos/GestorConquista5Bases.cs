using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace VoidformBioForge
{
    /// <summary>
    /// Gestiona el Modo Conquista: 5 bases y duración de 5 minutos (300 segundos).
    /// -----------------------------------------------------------------
    /// Nivel 1: mapa con 5 bases y temporizador de 5:00.
    /// Nivel 2: las bases se capturan y disputan; quien muere (incluido el humano cuyo traje explota)
    ///          reaparece tras unos segundos en un punto de aparición de su bando, con protección breve,
    ///          hasta que termina la partida.
    /// Nivel 3: reglas de victoria exactas y su orden de evaluación:
    ///   1) VICTORIA INMEDIATA: si un bando domina las 5 bases a la vez, gana al instante.
    ///   2) AL LLEGAR A 0:00: gana el bando con MÁS bases dominadas.
    ///   3) DESEMPATE 1: si tienen las mismas bases, gana quien acumuló más "tiempo de control"
    ///      (suma de segundos que cada una de sus bases estuvo dominada durante la partida).
    ///   4) DESEMPATE 2: si siguen iguales, gana quien tenga más "influencia" (suma del progreso
    ///      parcial a su favor en las bases no dominadas).
    ///   5) Si todo es idéntico, la partida termina en EMPATE.
    ///   Casos límite: en el fotograma en que el reloj llega a 0 se comprueba primero la dominación
    ///   total; tras finalizar se bloquea el daño global (una explosión de traje tardía no cambia nada)
    ///   y se cancelan las reapariciones pendientes.
    /// </summary>
    public class GestorConquista5Bases : MonoBehaviour
    {
        [SerializeField] private ZonaCapturaBase[] cincoBases = new ZonaCapturaBase[5];
        [SerializeField] private float duracionPartidaSegundos = 300f;
        [SerializeField] private float tiempoReaparicionSegundos = 3.5f;
        [SerializeField] private float proteccionReaparicionSegundos = 1.5f;
        [SerializeField] private bool iniciarAutomaticamente = true;

        private readonly List<SistemaSaludYTraje> participantes = new List<SistemaSaludYTraje>();
        private readonly Dictionary<BandoJuego, List<Vector3>> puntosAparicion = new Dictionary<BandoJuego, List<Vector3>>();
        private readonly List<Coroutine> reaparicionesPendientes = new List<Coroutine>();
        private float tiempoRestante;
        private bool partidaIniciada;
        private bool partidaFinalizada;
        private float tiempoControlDestructores;
        private float tiempoControlCuidadores;

        public float TiempoRestante => tiempoRestante;
        public float DuracionTotal => duracionPartidaSegundos;
        public bool PartidaFinalizada => partidaFinalizada;
        public IReadOnlyList<ZonaCapturaBase> Bases => cincoBases;
        public ResultadoEnfrentamiento Resultado { get; private set; } = ResultadoEnfrentamiento.Pendiente;
        public float TiempoControlDestructores => tiempoControlDestructores;
        public float TiempoControlCuidadores => tiempoControlCuidadores;

        public event Action<float> AlActualizarReloj5Minutos;
        public event Action<int, int> AlActualizarConteoBases;
        public event Action<ZonaCapturaBase> AlCambiarBase;
        public event Action<SistemaSaludYTraje, float> AlProgramarReaparicion;
        public event Action<string> AlFinalizarConquista;
        public event Action<ResultadoEnfrentamiento, string> AlFinalizarConResultado;

        // ================== Configuración ==================

        public void ConfigurarBases(ZonaCapturaBase[] bases)
        {
            cincoBases = bases;
        }

        public void ConfigurarPuntosAparicion(BandoJuego bando, List<Vector3> puntos)
        {
            puntosAparicion[bando] = puntos;
        }

        public void RegistrarParticipante(SistemaSaludYTraje entidad)
        {
            if (entidad == null || participantes.Contains(entidad)) return;
            participantes.Add(entidad);
            entidad.AlMorir += ProgramarReaparicionEnConquista;
        }

        private void Start()
        {
            if (iniciarAutomaticamente && !partidaIniciada) IniciarPartida();
        }

        public void IniciarPartida()
        {
            partidaIniciada = true;
            partidaFinalizada = false;
            Resultado = ResultadoEnfrentamiento.Pendiente;
            tiempoRestante = duracionPartidaSegundos;
            tiempoControlDestructores = 0f;
            tiempoControlCuidadores = 0f;
            SistemaSaludYTraje.BloqueoGlobalDeDano = false;

            // Modo de escena manual: si no hay bases asignadas se buscan las existentes.
            if (cincoBases == null || cincoBases.Length == 0 || cincoBases[0] == null)
            {
                cincoBases = FindObjectsByType<ZonaCapturaBase>(FindObjectsSortMode.None);
            }

            foreach (ZonaCapturaBase baseZona in cincoBases)
            {
                if (baseZona == null) continue;
                baseZona.AlCambiarEstadoBase -= ManejarCambioEnBase;
                baseZona.AlCambiarEstadoBase += ManejarCambioEnBase;
                baseZona.Reiniciar();
            }

            if (participantes.Count == 0)
            {
                foreach (SistemaSaludYTraje ent in FindObjectsByType<SistemaSaludYTraje>(FindObjectsSortMode.None))
                {
                    RegistrarParticipante(ent);
                }
            }

            EvaluarBases(false);
            BusEventosJuego.PublicarMensaje("¡Conquista iniciada! Dominad las 5 bases o controlad la mayoría al terminar los 5 minutos.", TipoMensajeSistema.Narrativa);
        }

        private void OnDestroy()
        {
            foreach (SistemaSaludYTraje p in participantes)
            {
                if (p != null) p.AlMorir -= ProgramarReaparicionEnConquista;
            }
            foreach (ZonaCapturaBase b in cincoBases)
            {
                if (b != null) b.AlCambiarEstadoBase -= ManejarCambioEnBase;
            }
        }

        // ================== Nivel 1-2: reloj y reaparición ==================

        private void Update()
        {
            if (!partidaIniciada || partidaFinalizada) return;

            AcumularTiempoDeControl();

            tiempoRestante = Mathf.Max(0f, tiempoRestante - Time.deltaTime);
            AlActualizarReloj5Minutos?.Invoke(tiempoRestante);

            if (tiempoRestante <= 0f)
            {
                // Caso límite: dominación total alcanzada en el mismo fotograma del final.
                if (!EvaluarBases(true))
                {
                    ResolverGanadorAlTerminar5Minutos();
                }
            }
        }

        private void AcumularTiempoDeControl()
        {
            foreach (ZonaCapturaBase b in cincoBases)
            {
                if (b == null) continue;
                if (b.EstadoActual == EstadoControlBase.DominadaDestructores) tiempoControlDestructores += Time.deltaTime;
                else if (b.EstadoActual == EstadoControlBase.DominadaCuidadores) tiempoControlCuidadores += Time.deltaTime;
            }
        }

        private void ProgramarReaparicionEnConquista(SistemaSaludYTraje entidadMuerta)
        {
            if (partidaFinalizada || entidadMuerta == null) return;
            AlProgramarReaparicion?.Invoke(entidadMuerta, tiempoReaparicionSegundos);
            reaparicionesPendientes.Add(StartCoroutine(RutinaReaparecer(entidadMuerta)));
        }

        private IEnumerator RutinaReaparecer(SistemaSaludYTraje entidad)
        {
            yield return new WaitForSeconds(tiempoReaparicionSegundos);
            if (partidaFinalizada || entidad == null || !entidad.EstaMuerto) yield break;

            if (puntosAparicion.TryGetValue(entidad.Bando, out List<Vector3> puntos) && puntos != null && puntos.Count > 0)
            {
                Vector3 punto = puntos[UnityEngine.Random.Range(0, puntos.Count)];
                Vector3 haciaCentro = -punto;
                haciaCentro.y = 0f;
                entidad.EstablecerPuntoAparicion(punto, haciaCentro.sqrMagnitude > 0.01f ? Quaternion.LookRotation(haciaCentro) : Quaternion.identity);
            }

            entidad.RevivirEnPuntoAparicion(proteccionReaparicionSegundos);
        }

        // ================== Nivel 3: victoria ==================

        private void ManejarCambioEnBase(ZonaCapturaBase baseModificada)
        {
            AlCambiarBase?.Invoke(baseModificada);
            if (partidaFinalizada) return;
            EvaluarBases(true);
        }

        /// <summary>Cuenta bases y, si se pide, aplica la victoria inmediata. Devuelve verdadero si terminó.</summary>
        private bool EvaluarBases(bool verificarVictoriaInmediataPorCincoBases)
        {
            ContarBases(out int basesDestructores, out int basesCuidadores, out _, out _);
            AlActualizarConteoBases?.Invoke(basesDestructores, basesCuidadores);

            if (!verificarVictoriaInmediataPorCincoBases) return false;

            int total = ContarBasesValidas();
            if (total > 0 && basesDestructores >= total)
            {
                FinalizarPartida(ResultadoEnfrentamiento.VictoriaDestructores, "¡Victoria inmediata de los Destructores por dominar las 5 bases!");
                return true;
            }
            if (total > 0 && basesCuidadores >= total)
            {
                FinalizarPartida(ResultadoEnfrentamiento.VictoriaCuidadores, "¡Victoria inmediata de los Cuidadores por dominar las 5 bases!");
                return true;
            }
            return false;
        }

        private int ContarBasesValidas()
        {
            int n = 0;
            foreach (ZonaCapturaBase b in cincoBases) if (b != null) n++;
            return n;
        }

        private void ContarBases(out int destructores, out int cuidadores, out float influenciaDestructores, out float influenciaCuidadores)
        {
            destructores = 0;
            cuidadores = 0;
            influenciaDestructores = 0f;
            influenciaCuidadores = 0f;
            foreach (ZonaCapturaBase baseZona in cincoBases)
            {
                if (baseZona == null) continue;
                if (baseZona.EstadoActual == EstadoControlBase.DominadaDestructores) destructores++;
                else if (baseZona.EstadoActual == EstadoControlBase.DominadaCuidadores) cuidadores++;
                else if (baseZona.ProgresoCaptura < 0f) influenciaDestructores += -baseZona.ProgresoCaptura;
                else influenciaCuidadores += baseZona.ProgresoCaptura;
            }
        }

        private void ResolverGanadorAlTerminar5Minutos()
        {
            ContarBases(out int basesD, out int basesC, out float influenciaD, out float influenciaC);

            if (basesD > basesC)
            {
                FinalizarPartida(ResultadoEnfrentamiento.VictoriaDestructores, $"¡Fin de los 5 minutos! Ganan los Destructores por mayoría ({basesD} contra {basesC} bases).");
            }
            else if (basesC > basesD)
            {
                FinalizarPartida(ResultadoEnfrentamiento.VictoriaCuidadores, $"¡Fin de los 5 minutos! Ganan los Cuidadores por mayoría ({basesC} contra {basesD} bases).");
            }
            else if (Mathf.Abs(tiempoControlDestructores - tiempoControlCuidadores) >= 0.5f)
            {
                bool ganaD = tiempoControlDestructores > tiempoControlCuidadores;
                FinalizarPartida(ganaD ? ResultadoEnfrentamiento.VictoriaDestructores : ResultadoEnfrentamiento.VictoriaCuidadores,
                    $"¡Empate a {basesD} bases! Desempate por tiempo de control: ganan los {(ganaD ? "Destructores" : "Cuidadores")} " +
                    $"({Mathf.RoundToInt(Mathf.Max(tiempoControlDestructores, tiempoControlCuidadores))} s contra {Mathf.RoundToInt(Mathf.Min(tiempoControlDestructores, tiempoControlCuidadores))} s).");
            }
            else if (Mathf.Abs(influenciaD - influenciaC) >= 1f)
            {
                bool ganaD = influenciaD > influenciaC;
                FinalizarPartida(ganaD ? ResultadoEnfrentamiento.VictoriaDestructores : ResultadoEnfrentamiento.VictoriaCuidadores,
                    $"¡Empate en bases y tiempo de control! Desempate por influencia parcial: ganan los {(ganaD ? "Destructores" : "Cuidadores")}.");
            }
            else
            {
                FinalizarPartida(ResultadoEnfrentamiento.Empate, $"¡Fin de los 5 minutos! Empate total ({basesD} a {basesC} bases, mismo tiempo de control e influencia).");
            }
        }

        private void FinalizarPartida(ResultadoEnfrentamiento resultado, string mensajeFinal)
        {
            if (partidaFinalizada) return;
            partidaFinalizada = true;
            Resultado = resultado;
            SistemaSaludYTraje.BloqueoGlobalDeDano = true;

            foreach (Coroutine c in reaparicionesPendientes)
            {
                if (c != null) StopCoroutine(c);
            }
            reaparicionesPendientes.Clear();

            AlFinalizarConquista?.Invoke(mensajeFinal);
            AlFinalizarConResultado?.Invoke(resultado, mensajeFinal);
        }
    }
}
