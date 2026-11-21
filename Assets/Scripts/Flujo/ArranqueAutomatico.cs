using UnityEngine;

namespace VoidformBioForge
{
    /// <summary>
    /// Arranque automático: al pulsar Play en CUALQUIER escena (incluida la "SampleScene" vacía que
    /// crea Unity), si no existe un GestorFlujoPartida se crea uno y el juego completo se construye solo.
    /// Para desactivarlo (por ejemplo, si integra estos scripts en otro proyecto con escenas propias),
    /// ponga <see cref="Habilitado"/> en false.
    /// </summary>
    public static class ArranqueAutomatico
    {
        /// <summary>Cambie a false para que el juego solo arranque en escenas con GestorFlujoPartida.</summary>
        public static readonly bool Habilitado = true;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void ArrancarSiHaceFalta()
        {
            if (!Habilitado) return;
            if (Object.FindFirstObjectByType<GestorFlujoPartida>() != null) return;

            GameObject director = new GameObject("Voidform Bio-Forge (GestorFlujoPartida)");
            director.AddComponent<GestorFlujoPartida>();
            Debug.Log("[Voidform] Arranque automático: se creó GestorFlujoPartida en la escena actual.");
        }
    }
}
