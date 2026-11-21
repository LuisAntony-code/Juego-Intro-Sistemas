#if UNITY_EDITOR
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace VoidformBioForge.Herramientas
{
    /// <summary>
    /// Utilidades del editor de Unity (menú superior "Voidform").
    /// - Crear escena jugable: genera Assets/Escenas/VoidformBioForge.unity con el GestorFlujoPartida,
    ///   la guarda y la añade a File > Build Settings.
    /// - Crear assets de datos: genera ScriptableObjects editables de los 5 trajes, 5 armas y 15 variantes de guardián
    ///   para que el equipo de diseño ajuste valores sin tocar código.
    /// - Borrar progreso guardado: limpia PlayerPrefs del juego (niveles, copas, integridad ecológica).
    /// </summary>
    public static class MenuVoidformEditor
    {
        private const string CarpetaEscenas = "Assets/Escenas";
        private const string RutaEscena = "Assets/Escenas/VoidformBioForge.unity";
        private const string CarpetaDatos = "Assets/Datos";

        [MenuItem("Voidform/1. Crear escena jugable")]
        public static void CrearEscenaJugable()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;

            if (!Directory.Exists(CarpetaEscenas)) Directory.CreateDirectory(CarpetaEscenas);

            var escena = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);
            GameObject director = new GameObject("Voidform Bio-Forge (GestorFlujoPartida)");
            director.AddComponent<GestorFlujoPartida>();

            EditorSceneManager.SaveScene(escena, RutaEscena);

            List<EditorBuildSettingsScene> escenas = new List<EditorBuildSettingsScene>(EditorBuildSettings.scenes);
            if (!escenas.Exists(e => e.path == RutaEscena))
            {
                escenas.Insert(0, new EditorBuildSettingsScene(RutaEscena, true));
                EditorBuildSettings.scenes = escenas.ToArray();
            }

            AssetDatabase.Refresh();
            Debug.Log($"[Voidform] Escena creada en {RutaEscena} y añadida a Build Settings. Pulsa Play para jugar.");
        }

        [MenuItem("Voidform/2. Crear assets de datos (arquetipos, armas y guardianes)")]
        public static void CrearAssetsDeDatos()
        {
            if (!AssetDatabase.IsValidFolder(CarpetaDatos)) AssetDatabase.CreateFolder("Assets", "Datos");
            foreach (string sub in new[] { "Arquetipos", "Armas", "Guardianes" })
            {
                if (!AssetDatabase.IsValidFolder($"{CarpetaDatos}/{sub}")) AssetDatabase.CreateFolder(CarpetaDatos, sub);
            }

            for (int i = 0; i < 5; i++)
            {
                DatosArquetipo t = DatosArquetipo.CrearPorDefecto((TipoArquetipo)i);
                AssetDatabase.CreateAsset(t, AssetDatabase.GenerateUniqueAssetPath($"{CarpetaDatos}/Arquetipos/{(TipoArquetipo)i}.asset"));
                DatosArmaModular a = DatosArmaModular.CrearPorDefecto((TipoArmaModular)i);
                AssetDatabase.CreateAsset(a, AssetDatabase.GenerateUniqueAssetPath($"{CarpetaDatos}/Armas/{(TipoArmaModular)i}.asset"));
            }
            // Un asset por cada variante (vanguardia, control y artillería) de los 5 linajes jugables.
            foreach (TipoElemento elemento in UtilidadesTipos.ElementosJugables)
            {
                for (int r = 0; r < 3; r++)
                {
                    DatosGuardianElemental g = DatosGuardianElemental.CrearVariante(elemento, (RolGuardian)r);
                    AssetDatabase.CreateAsset(g, AssetDatabase.GenerateUniqueAssetPath($"{CarpetaDatos}/Guardianes/{elemento}_{(RolGuardian)r}.asset"));
                }
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log($"[Voidform] Assets de datos creados en {CarpetaDatos}.");
        }

        [MenuItem("Voidform/3. Borrar progreso guardado")]
        public static void BorrarProgreso()
        {
            if (EditorUtility.DisplayDialog("Voidform: Bio-Forge", "¿Borrar niveles desbloqueados, copas y registros ecológicos?", "Borrar", "Cancelar"))
            {
                GuardadoProgreso.BorrarTodo();
                Debug.Log("[Voidform] Progreso borrado.");
            }
        }
    }
}
#endif
