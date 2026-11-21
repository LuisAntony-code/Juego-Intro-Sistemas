using UnityEngine;

namespace VoidformBioForge
{
    /// <summary>
    /// Telégrafo de una habilidad de guardián: un disco del color del elemento en el suelo que se llena desde el
    /// centro hasta el radio real de la habilidad durante la anticipación, más un anillo exterior fijo que marca el
    /// límite. Permite a los humanos LEER el ataque y apartarse o interrumpirlo.
    /// </summary>
    public class TelegrafoHabilidad : MonoBehaviour
    {
        private Transform relleno;
        private float radio;
        private float duracion;
        private float inicio;

        public static GameObject Crear(Transform guardian, float radioHabilidad, float duracionAnticipacion, Color color, bool cono)
        {
            GameObject raiz = new GameObject("Telégrafo de habilidad");
            raiz.transform.SetParent(guardian, false);
            float r = cono ? radioHabilidad * 0.6f : radioHabilidad;
            raiz.transform.localPosition = new Vector3(0f, 0.06f, cono ? radioHabilidad * 0.55f : 0f);

            Material m = MaterialesProcedurales.ObtenerMaterialLinea(new Color(color.r, color.g, color.b, 0.55f));
            GameObject borde = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            borde.name = "Límite";
            Object.DestroyImmediate(borde.GetComponent<Collider>());
            borde.transform.SetParent(raiz.transform, false);
            borde.transform.localScale = new Vector3(r * 2f, 0.005f, r * 2f);
            Renderer rb = borde.GetComponent<Renderer>();
            rb.sharedMaterial = m;
            rb.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;

            GameObject relleno = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            relleno.name = "Relleno";
            Object.DestroyImmediate(relleno.GetComponent<Collider>());
            relleno.transform.SetParent(raiz.transform, false);
            relleno.transform.localPosition = new Vector3(0f, 0.01f, 0f);
            Renderer rr = relleno.GetComponent<Renderer>();
            rr.sharedMaterial = MaterialesProcedurales.ObtenerMaterialLinea(color);
            rr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;

            TelegrafoHabilidad t = raiz.AddComponent<TelegrafoHabilidad>();
            t.relleno = relleno.transform;
            t.radio = r;
            t.duracion = Mathf.Max(0.05f, duracionAnticipacion);
            t.inicio = Time.time;
            t.Actualizar();
            return raiz;
        }

        private void Update() => Actualizar();

        private void Actualizar()
        {
            float t = Mathf.Clamp01((Time.time - inicio) / duracion);
            float d = radio * 2f * Mathf.Lerp(0.05f, 1f, t * t);
            relleno.localScale = new Vector3(d, 0.006f, d);
            // El telégrafo no gira con el guardián en horizontal: queda plano sobre el suelo.
            transform.rotation = Quaternion.Euler(0f, transform.parent != null ? transform.parent.eulerAngles.y : 0f, 0f);
        }
    }
}
