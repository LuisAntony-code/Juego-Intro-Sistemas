using UnityEngine;

namespace VoidformBioForge
{
    /// <summary>
    /// Núcleo o punto débil de un guardián (semilla-corazón, vejiga de plasma, grietas de magma,
    /// giroscopio de la nuca, núcleo del antebrazo, gema-nexo).
    /// Nivel 1: posición local y radio. Nivel 2: los impactos que pasan cerca hacen daño crítico (×1,5).
    /// Nivel 3: algunos núcleos solo quedan expuestos un tiempo (gema-nexo tras rotar de elemento: ×2).
    /// </summary>
    public class PuntoDebil : MonoBehaviour
    {
        [SerializeField] private Vector3 posicionLocal = new Vector3(0f, 1.6f, 0.4f);
        [SerializeField] private float radio = 0.45f;
        [SerializeField] private float multiplicador = 1.5f;

        private float expuestoExtraHasta = -1f;

        public Vector3 PosicionMundo => transform.TransformPoint(posicionLocal);
        public float Multiplicador => Time.time < expuestoExtraHasta ? multiplicador * 1.35f : multiplicador;

        public static PuntoDebil Configurar(GameObject guardian, TipoElemento elemento)
        {
            PuntoDebil p = guardian.GetComponent<PuntoDebil>();
            if (p == null) p = guardian.AddComponent<PuntoDebil>();
            switch (elemento)
            {
                case TipoElemento.Arbol: p.posicionLocal = new Vector3(0f, 1.6f, 0.42f); break;     // Semilla-corazón.
                case TipoElemento.Agua: p.posicionLocal = new Vector3(0f, 1.6f, -0.45f); break;     // Vejiga de plasma (espalda).
                case TipoElemento.Fuego: p.posicionLocal = new Vector3(0f, 1.35f, 0.4f); break;     // Grietas de magma.
                case TipoElemento.Metal: p.posicionLocal = new Vector3(0f, 2.3f, -0.3f); break;     // Giroscopio de la nuca.
                default: p.posicionLocal = new Vector3(1.05f, 1.05f, 0.32f); break;                 // Núcleo del antebrazo.
            }
            return p;
        }

        /// <summary>Expone más el núcleo durante un tiempo (gema-nexo tras rotar, núcleo al rojo tras golpear).</summary>
        public void Exponer(float segundos)
        {
            expuestoExtraHasta = Time.time + segundos;
        }

        /// <summary>Multiplicador crítico para un disparo que recorre el rayo (1 si no pasa cerca del núcleo).</summary>
        public static float EvaluarRayo(SistemaSaludYTraje objetivo, Ray rayo, out bool critico)
        {
            critico = false;
            PuntoDebil p = objetivo != null ? objetivo.GetComponent<PuntoDebil>() : null;
            if (p == null) return 1f;
            Vector3 aPunto = p.PosicionMundo - rayo.origin;
            float t = Vector3.Dot(aPunto, rayo.direction);
            if (t < 0f) return 1f;
            float distancia = Vector3.Distance(rayo.origin + rayo.direction * t, p.PosicionMundo);
            if (distancia > p.radio) return 1f;
            critico = true;
            return p.Multiplicador;
        }

        /// <summary>Multiplicador crítico para un impacto en un punto (proyectiles y golpes).</summary>
        public static float EvaluarPunto(SistemaSaludYTraje objetivo, Vector3 punto, out bool critico)
        {
            critico = false;
            PuntoDebil p = objetivo != null ? objetivo.GetComponent<PuntoDebil>() : null;
            if (p == null) return 1f;
            if (Vector3.Distance(punto, p.PosicionMundo) > p.radio * 1.6f) return 1f;
            critico = true;
            return p.Multiplicador;
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(transform.TransformPoint(posicionLocal), radio);
        }
    }
}
