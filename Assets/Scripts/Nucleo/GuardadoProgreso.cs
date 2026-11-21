using UnityEngine;

namespace VoidformBioForge
{
    /// <summary>
    /// Guarda y carga el progreso persistente del jugador con PlayerPrefs:
    /// - Nivel 1: nivel de historia desbloqueado por bando.
    /// - Nivel 2: copas, racha de victorias y escudo de liga del modo 4vs4.
    /// - Nivel 3: integridad del ecosistema lograda en cada nivel (para el desenlace).
    /// Todas las claves llevan el prefijo "VBF_" para no chocar con otros proyectos.
    /// </summary>
    public static class GuardadoProgreso
    {
        private const string Prefijo = "VBF_";

        /// <summary>Número de niveles del Modo Historia (6: cinco escenarios y la Convergencia Total).</summary>
        public const int NivelesTotales = 6;

        // ---------------- Nivel 1: progreso de historia ----------------

        public static int ObtenerNivelDesbloqueado(BandoJuego bando)
        {
            return Mathf.Clamp(PlayerPrefs.GetInt($"{Prefijo}NivelDesbloqueado_{bando}", 1), 1, NivelesTotales);
        }

        public static void DesbloquearNivel(BandoJuego bando, int nivel)
        {
            int actual = ObtenerNivelDesbloqueado(bando);
            if (nivel > actual)
            {
                PlayerPrefs.SetInt($"{Prefijo}NivelDesbloqueado_{bando}", Mathf.Clamp(nivel, 1, NivelesTotales));
                PlayerPrefs.Save();
            }
        }

        public static bool HistoriaCompletada(BandoJuego bando)
        {
            return PlayerPrefs.GetInt($"{Prefijo}HistoriaCompletada_{bando}", 0) == 1;
        }

        public static void MarcarHistoriaCompletada(BandoJuego bando)
        {
            PlayerPrefs.SetInt($"{Prefijo}HistoriaCompletada_{bando}", 1);
            PlayerPrefs.Save();
        }

        // ---------------- Nivel 2: clasificación 4vs4 ----------------

        public static int ObtenerCopas()
        {
            return Mathf.Max(0, PlayerPrefs.GetInt($"{Prefijo}Copas", 0));
        }

        public static int ObtenerRacha()
        {
            return Mathf.Max(0, PlayerPrefs.GetInt($"{Prefijo}Racha", 0));
        }

        public static bool ObtenerEscudoLiga()
        {
            return PlayerPrefs.GetInt($"{Prefijo}EscudoLiga", 0) == 1;
        }

        public static void GuardarClasificacion(int copas, int racha, bool escudoLiga)
        {
            PlayerPrefs.SetInt($"{Prefijo}Copas", Mathf.Max(0, copas));
            PlayerPrefs.SetInt($"{Prefijo}Racha", Mathf.Max(0, racha));
            PlayerPrefs.SetInt($"{Prefijo}EscudoLiga", escudoLiga ? 1 : 0);
            PlayerPrefs.Save();
        }

        // ---------------- Nivel 3: memoria ecológica de la campaña ----------------

        public static float ObtenerIntegridadNivel(BandoJuego bando, int nivel)
        {
            return PlayerPrefs.GetFloat($"{Prefijo}Integridad_{bando}_{nivel}", -1f);
        }

        public static void GuardarIntegridadNivel(BandoJuego bando, int nivel, float integridad)
        {
            PlayerPrefs.SetFloat($"{Prefijo}Integridad_{bando}_{nivel}", Mathf.Clamp(integridad, 0f, 100f));
            PlayerPrefs.Save();
        }

        /// <summary>
        /// Promedio de integridad ecológica de los niveles jugados por un bando.
        /// Devuelve 100 si todavía no se ha jugado ninguno.
        /// </summary>
        public static float ObtenerPromedioIntegridad(BandoJuego bando)
        {
            float suma = 0f;
            int jugados = 0;
            for (int nivel = 1; nivel <= NivelesTotales; nivel++)
            {
                float valor = ObtenerIntegridadNivel(bando, nivel);
                if (valor >= 0f)
                {
                    suma += valor;
                    jugados++;
                }
            }
            return jugados == 0 ? 100f : suma / jugados;
        }

        /// <summary>Borra todo el progreso (botón de reinicio en el menú de clasificación).</summary>
        public static void BorrarTodo()
        {
            foreach (BandoJuego bando in new[] { BandoJuego.Destructor, BandoJuego.Cuidador })
            {
                PlayerPrefs.DeleteKey($"{Prefijo}NivelDesbloqueado_{bando}");
                PlayerPrefs.DeleteKey($"{Prefijo}HistoriaCompletada_{bando}");
                for (int nivel = 1; nivel <= NivelesTotales; nivel++)
                {
                    PlayerPrefs.DeleteKey($"{Prefijo}Integridad_{bando}_{nivel}");
                }
            }
            PlayerPrefs.DeleteKey($"{Prefijo}Copas");
            PlayerPrefs.DeleteKey($"{Prefijo}Racha");
            PlayerPrefs.DeleteKey($"{Prefijo}EscudoLiga");
            PlayerPrefs.Save();
        }
    }
}
