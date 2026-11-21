using System;
using UnityEngine;

namespace VoidformBioForge
{
    /// <summary>Detalle de un cambio de clasificación para la pantalla de resultados.</summary>
    public struct CambioClasificacion
    {
        public int CopasAntes;
        public int CopasDespues;
        public int Diferencia;
        public LigaCompetitiva LigaAntes;
        public LigaCompetitiva LigaDespues;
        public bool Ascenso;
        public bool Descenso;
        public bool EscudoConsumido;
        public int Racha;
        public string Explicacion;
    }

    /// <summary>
    /// Gestiona la clasificación competitiva en 4vs4: ganar otorga copas para ascender de rango y liga.
    /// -----------------------------------------------------------------
    /// Nivel 1: copas acumuladas; 100 copas por liga (Bronce 0, Plata 100, Oro 200, Platino 300,
    ///          Diamante 400, Núcleo Supremo 500+); 4 divisiones de 25 copas por liga (I a IV).
    /// Nivel 2: CÁLCULO DE COPAS:
    ///   • Victoria: +30 − 2 × (índice de liga), mínimo +20, más bonificación de racha de +5 por
    ///     victoria consecutiva previa (máximo +15).
    ///   • Derrota: −(15 + 2 × índice de liga) y la racha vuelve a 0. Nunca se baja de 0 copas.
    ///   • Empate: 0 copas y la racha se conserva.
    ///   • Partidas en salas con bots: se aplica el 50% (redondeado hacia arriba) de la variación.
    /// Nivel 3: ASCENSO Y DESCENSO con "escudo de liga": al ascender se obtiene un escudo; la primera
    ///          derrota que te haría descender se queda en el mínimo de tu liga y consume el escudo.
    ///          Todo se guarda en PlayerPrefs y se notifica a la interfaz.
    /// </summary>
    public class SistemaClasificacionYCopas : MonoBehaviour
    {
        [SerializeField] private int copasActuales = 0;
        [SerializeField] private int copasPorVictoria = 30;
        [SerializeField] private int minimoCopasVictoria = 20;
        [SerializeField] private int penalizacionCopasDerrota = 15;
        [SerializeField] private int bonificacionPorRacha = 5;
        [SerializeField] private int bonificacionRachaMaxima = 15;
        [SerializeField] [Range(0.1f, 1f)] private float multiplicadorSalaConBots = 0.5f;
        [SerializeField] private bool cargarDesdeGuardado = true;

        private int racha;
        private bool escudoLiga;

        public int CopasActuales => copasActuales;
        public int Racha => racha;
        public bool TieneEscudoLiga => escudoLiga;
        public LigaCompetitiva LigaActual => CalcularLiga(copasActuales);
        public string RangoActual => CalcularNombreRango(copasActuales);

        public event Action<int, string, LigaCompetitiva> AlActualizarClasificacion;
        public event Action<CambioClasificacion> AlRegistrarCambio;

        private void Awake()
        {
            if (cargarDesdeGuardado)
            {
                copasActuales = GuardadoProgreso.ObtenerCopas();
                racha = GuardadoProgreso.ObtenerRacha();
                escudoLiga = GuardadoProgreso.ObtenerEscudoLiga();
            }
        }

        public void ForzarNotificacion()
        {
            AlActualizarClasificacion?.Invoke(copasActuales, RangoActual, LigaActual);
        }

        /// <summary>Versión básica (compatibilidad): sin bots.</summary>
        public void RegistrarResultado4vs4(bool gano, bool fueEmpate)
        {
            RegistrarResultado4vs4(gano, fueEmpate, false);
        }

        public CambioClasificacion RegistrarResultado4vs4(bool gano, bool fueEmpate, bool salaConBots)
        {
            CambioClasificacion cambio = new CambioClasificacion
            {
                CopasAntes = copasActuales,
                LigaAntes = LigaActual
            };

            int indiceLiga = (int)LigaActual;
            int diferencia;
            string explicacion;

            if (fueEmpate)
            {
                diferencia = 0;
                explicacion = "Empate: no se ganan ni se pierden copas y la racha se conserva.";
            }
            else if (gano)
            {
                int baseVictoria = Mathf.Max(minimoCopasVictoria, copasPorVictoria - 2 * indiceLiga);
                int bonoRacha = Mathf.Min(bonificacionRachaMaxima, racha * bonificacionPorRacha);
                diferencia = baseVictoria + bonoRacha;
                explicacion = $"Victoria: +{baseVictoria} base" + (bonoRacha > 0 ? $" +{bonoRacha} por racha" : "");
                racha++;
            }
            else
            {
                diferencia = -(penalizacionCopasDerrota + 2 * indiceLiga);
                explicacion = $"Derrota: {diferencia} copas; la racha se reinicia";
                racha = 0;
            }

            if (salaConBots && diferencia != 0)
            {
                diferencia = Mathf.RoundToInt(Mathf.Sign(diferencia) * Mathf.Ceil(Mathf.Abs(diferencia) * multiplicadorSalaConBots));
                explicacion += $" (sala con bots: {Mathf.RoundToInt(multiplicadorSalaConBots * 100f)}%)";
            }

            int nuevasCopas = Mathf.Max(0, copasActuales + diferencia);
            LigaCompetitiva ligaNueva = CalcularLiga(nuevasCopas);

            // Escudo de liga: evita un descenso inmediato tras ascender.
            if (ligaNueva < cambio.LigaAntes && escudoLiga)
            {
                nuevasCopas = UmbralLiga(cambio.LigaAntes);
                ligaNueva = cambio.LigaAntes;
                escudoLiga = false;
                cambio.EscudoConsumido = true;
                explicacion += ". ¡El escudo de liga evitó el descenso!";
            }

            copasActuales = nuevasCopas;
            cambio.CopasDespues = nuevasCopas;
            cambio.Diferencia = nuevasCopas - cambio.CopasAntes;
            cambio.LigaDespues = ligaNueva;
            cambio.Ascenso = ligaNueva > cambio.LigaAntes;
            cambio.Descenso = ligaNueva < cambio.LigaAntes;
            cambio.Racha = racha;

            if (cambio.Ascenso)
            {
                escudoLiga = true;
                explicacion += $". ¡ASCENSO a {UtilidadesTipos.NombreLiga(ligaNueva)}! (escudo de liga activado)";
            }
            else if (cambio.Descenso)
            {
                explicacion += $". Descenso a {UtilidadesTipos.NombreLiga(ligaNueva)}.";
            }

            cambio.Explicacion = explicacion;

            GuardadoProgreso.GuardarClasificacion(copasActuales, racha, escudoLiga);
            AlRegistrarCambio?.Invoke(cambio);
            AlActualizarClasificacion?.Invoke(copasActuales, RangoActual, LigaActual);
            return cambio;
        }

        public void ReiniciarClasificacion()
        {
            copasActuales = 0;
            racha = 0;
            escudoLiga = false;
            GuardadoProgreso.GuardarClasificacion(0, 0, false);
            ForzarNotificacion();
        }

        public static LigaCompetitiva CalcularLiga(int copas)
        {
            if (copas >= 500) return LigaCompetitiva.NucleoSupremo;
            if (copas >= 400) return LigaCompetitiva.Diamante;
            if (copas >= 300) return LigaCompetitiva.Platino;
            if (copas >= 200) return LigaCompetitiva.Oro;
            if (copas >= 100) return LigaCompetitiva.Plata;
            return LigaCompetitiva.Bronce;
        }

        public static int UmbralLiga(LigaCompetitiva liga)
        {
            return (int)liga * 100;
        }

        public static string CalcularNombreRango(int copas)
        {
            LigaCompetitiva liga = CalcularLiga(copas);
            if (liga == LigaCompetitiva.NucleoSupremo) return $"{UtilidadesTipos.NombreLiga(liga)} · {copas} copas";
            int division = (copas % 100) / 25 + 1;
            string[] romanos = { "I", "II", "III", "IV" };
            return $"{UtilidadesTipos.NombreLiga(liga)} {romanos[Mathf.Clamp(division - 1, 0, 3)]}";
        }

        /// <summary>Copas que faltan para la siguiente división o liga.</summary>
        public int CopasParaSiguienteDivision()
        {
            if (LigaActual == LigaCompetitiva.NucleoSupremo) return 0;
            int siguiente = (copasActuales / 25 + 1) * 25;
            return siguiente - copasActuales;
        }
    }
}
