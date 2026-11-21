using System.Collections.Generic;
using UnityEngine;

namespace VoidformBioForge
{
    /// <summary>
    /// Prepara un modelo FBX generado en Blender para el juego, sin depender de la conversión de ejes del importador.
    /// -----------------------------------------------------------------
    /// Nivel 1 (orientación): cada FBX trae dos marcadores vacíos, "Marca_Frente" (1 m delante) y "Marca_Arriba"
    ///   (1 m encima). Se gira el modelo para que apunten a +Z y +Y del contenedor, haga lo que haga el importador
    ///   (rotación de -90° en X, escala 100, etc.).
    /// Nivel 2 (esqueleto limpio): las articulaciones importadas pueden traer rotaciones de conversión en sus ejes
    ///   locales, y el AnimadorProcedural gira cada hueso sobre sus ejes locales. Por eso se crean articulaciones
    ///   NUEVAS con rotación identidad (las mismas que las del modelo de primitivas) en las posiciones de las
    ///   importadas y se mueven a ellas las mallas, conservando su posición en el mundo.
    /// </summary>
    public static class AlineadorModeloImportado
    {
        /// <summary>Articulaciones en orden padre → hijo, con su padre (null = raíz del modelo).</summary>
        private static readonly string[,] Jerarquia =
        {
            { "Cadera", null }, { "Torso", "Cadera" }, { "Cabeza", "Torso" }, { "Capa", "Torso" },
            { "Brazo_I", "Torso" }, { "Antebrazo_I", "Brazo_I" }, { "Mano_I", "Antebrazo_I" },
            { "Brazo_D", "Torso" }, { "Antebrazo_D", "Brazo_D" }, { "Mano_D", "Antebrazo_D" },
            { "Muslo_I", "Cadera" }, { "Pierna_I", "Muslo_I" }, { "Pie_I", "Pierna_I" },
            { "Muslo_D", "Cadera" }, { "Pierna_D", "Muslo_D" }, { "Pie_D", "Pierna_D" }
        };

        private static string NombreBase(string nombre)
        {
            int punto = nombre.IndexOf('.');
            return punto > 0 ? nombre.Substring(0, punto) : nombre; // "Torso.001" → "Torso"
        }

        private static Transform Buscar(Transform raiz, string nombre)
        {
            foreach (Transform t in raiz.GetComponentsInChildren<Transform>(true))
            {
                if (NombreBase(t.name) == nombre) return t;
            }
            return null;
        }

        /// <summary>Gira la instancia para que su frente mire a referencia.forward y su arriba a referencia.up.</summary>
        public static void Alinear(GameObject instancia, Transform referencia)
        {
            Transform raiz = instancia.transform;
            Transform frente = Buscar(raiz, "Marca_Frente");
            Transform arriba = Buscar(raiz, "Marca_Arriba");
            if (frente == null || arriba == null) return;

            Vector3 f = frente.position - raiz.position;
            Vector3 u = arriba.position - raiz.position;
            if (f.sqrMagnitude > 1e-8f && u.sqrMagnitude > 1e-8f)
            {
                Quaternion actual = Quaternion.LookRotation(f.normalized, u.normalized);
                raiz.rotation = referencia.rotation * Quaternion.Inverse(actual) * raiz.rotation;
            }
            Object.Destroy(frente.gameObject);
            Object.Destroy(arriba.gameObject);
        }

        /// <summary>
        /// Crea bajo 'contenedor' un esqueleto nuevo con ejes limpios en las posiciones de las articulaciones
        /// importadas, le pasa todas las mallas y destruye la jerarquía importada. Devuelve la raíz nueva, o null si el
        /// modelo no tiene articulaciones (en ese caso no toca nada).
        /// </summary>
        public static Transform ReconstruirEsqueleto(GameObject instancia, Transform contenedor)
        {
            Dictionary<string, Transform> viejas = new Dictionary<string, Transform>();
            foreach (Transform t in instancia.GetComponentsInChildren<Transform>(true))
            {
                string n = NombreBase(t.name);
                if (!viejas.ContainsKey(n)) viejas[n] = t;
            }
            if (!viejas.ContainsKey("Cadera")) return null;

            GameObject raizNueva = new GameObject(instancia.name);
            raizNueva.transform.SetParent(contenedor, false);

            Dictionary<Transform, Transform> equivalencia = new Dictionary<Transform, Transform>();
            Dictionary<string, Transform> nuevas = new Dictionary<string, Transform>();
            for (int i = 0; i < Jerarquia.GetLength(0); i++)
            {
                string nombre = Jerarquia[i, 0];
                string padre = Jerarquia[i, 1];
                if (!viejas.TryGetValue(nombre, out Transform vieja)) continue;
                Transform contenedorPadre = padre != null && nuevas.TryGetValue(padre, out Transform p) ? p : raizNueva.transform;
                Transform nueva = new GameObject(nombre).transform;
                nueva.SetParent(contenedorPadre, false);
                nueva.position = vieja.position;
                nueva.rotation = contenedor.rotation;
                nueva.localScale = Vector3.one;
                nuevas[nombre] = nueva;
                equivalencia[vieja] = nueva;
            }

            // Cada malla pasa a la articulación nueva equivalente a su articulación importada más cercana.
            foreach (Renderer r in instancia.GetComponentsInChildren<Renderer>(true))
            {
                Transform destino = raizNueva.transform;
                for (Transform a = r.transform.parent; a != null; a = a.parent)
                {
                    if (equivalencia.TryGetValue(a, out Transform n)) { destino = n; break; }
                }
                r.transform.SetParent(destino, true);
            }

            instancia.SetActive(false);
            instancia.transform.SetParent(null, false);
            Object.Destroy(instancia);
            return raizNueva.transform;
        }
    }
}
