using UnityEngine;

namespace VoidformBioForge
{
    /// <summary>
    /// Arma visible en primera persona para el Destructor local (vista estilo arena shooter).
    /// Nivel 1: modelo simple pegado a la cámara. Nivel 2: forma y color distintos por arma, retroceso
    /// visual al disparar y balanceo al caminar. Nivel 3: animación de recarga (el arma baja y sube).
    /// </summary>
    public class ModeloArmaPrimeraPersona : MonoBehaviour
    {
        private SistemaArmasYTrajeDestructor armas;
        private ControladorMovimientoAssaultCube movimiento;
        private Transform contenedor;
        private Transform canon;
        private Transform cuerpo;
        private Renderer renderNucleo;
        private Vector3 posicionReposo = new Vector3(0.28f, -0.26f, 0.55f);
        private float retroceso;
        private float faseBalanceo;

        public static ModeloArmaPrimeraPersona Crear(Transform camara, SistemaArmasYTrajeDestructor sistemaArmas, ControladorMovimientoAssaultCube mov)
        {
            GameObject go = new GameObject("Arma en Primera Persona");
            go.transform.SetParent(camara, false);
            ModeloArmaPrimeraPersona m = go.AddComponent<ModeloArmaPrimeraPersona>();
            m.armas = sistemaArmas;
            m.movimiento = mov;
            m.Construir();
            sistemaArmas.AlDisparar += m.ManejarDisparo;
            sistemaArmas.AlCambiarArma += m.ManejarCambioArma;
            m.ManejarCambioArma(sistemaArmas.ArmaActiva);
            return m;
        }

        private void OnDestroy()
        {
            if (armas != null)
            {
                armas.AlDisparar -= ManejarDisparo;
                armas.AlCambiarArma -= ManejarCambioArma;
            }
        }

        private void Construir()
        {
            contenedor = new GameObject("Pivote Arma").transform;
            contenedor.SetParent(transform, false);
            contenedor.localPosition = posicionReposo;

            cuerpo = CrearPieza(PrimitiveType.Cube, new Vector3(0f, 0f, 0f), new Vector3(0.09f, 0.12f, 0.42f), new Color(0.22f, 0.23f, 0.26f), Color.black).transform;
            canon = CrearPieza(PrimitiveType.Cylinder, new Vector3(0f, 0.02f, 0.3f), new Vector3(0.035f, 0.14f, 0.035f), new Color(0.15f, 0.15f, 0.17f), Color.black).transform;
            canon.localRotation = Quaternion.Euler(90f, 0f, 0f);
            CrearPieza(PrimitiveType.Cube, new Vector3(0f, -0.1f, -0.06f), new Vector3(0.06f, 0.12f, 0.07f), new Color(0.18f, 0.18f, 0.2f), Color.black);
            renderNucleo = CrearPieza(PrimitiveType.Cube, new Vector3(0f, 0.075f, -0.02f), new Vector3(0.05f, 0.03f, 0.18f), PaletaVoidform.Destructor, PaletaVoidform.Destructor * 2f);
        }

        private Renderer CrearPieza(PrimitiveType tipo, Vector3 posicion, Vector3 escala, Color color, Color emision)
        {
            GameObject p = GameObject.CreatePrimitive(tipo);
            p.transform.SetParent(contenedor, false);
            p.transform.localPosition = posicion;
            p.transform.localScale = escala;
            DestroyImmediate(p.GetComponent<Collider>());
            Renderer r = p.GetComponent<Renderer>();
            r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            MaterialesProcedurales.AplicarColor(r, color, emision);
            return r;
        }

        private void ManejarDisparo()
        {
            retroceso = Mathf.Min(1f, retroceso + 0.6f);
        }

        private void ManejarCambioArma(DatosArma arma)
        {
            if (arma == null || cuerpo == null) return;
            float largo;
            float grosor;
            switch (arma.tipo)
            {
                case TipoArma.PistolaAuxiliar: largo = 0.22f; grosor = 0.08f; break;
                case TipoArma.SubfusilRafaga: largo = 0.34f; grosor = 0.1f; break;
                case TipoArma.EscopetaDispersora: largo = 0.5f; grosor = 0.13f; break;
                case TipoArma.RiflePrecision: largo = 0.7f; grosor = 0.09f; break;
                default: largo = 0.46f; grosor = 0.11f; break;
            }
            cuerpo.localScale = new Vector3(grosor * 0.85f, grosor, largo);
            canon.localPosition = new Vector3(0f, 0.02f, largo * 0.5f + 0.08f);
            MaterialesProcedurales.AplicarColor(renderNucleo, arma.colorTrazador, arma.colorTrazador * 2f);
        }

        private void LateUpdate()
        {
            if (contenedor == null) return;
            retroceso = Mathf.MoveTowards(retroceso, 0f, Time.deltaTime * 6f);

            float velocidad = movimiento != null ? movimiento.FactorMovimiento : 0f;
            faseBalanceo += Time.deltaTime * Mathf.Lerp(1.5f, 9f, velocidad);
            Vector3 balanceo = new Vector3(Mathf.Sin(faseBalanceo) * 0.012f, Mathf.Abs(Mathf.Cos(faseBalanceo)) * 0.012f, 0f) * (0.3f + velocidad);

            float bajadaRecarga = armas != null && armas.EstaRecargando ? 0.18f : 0f;
            Vector3 objetivo = posicionReposo + balanceo + new Vector3(0f, -bajadaRecarga, -retroceso * 0.08f);
            contenedor.localPosition = Vector3.Lerp(contenedor.localPosition, objetivo, 1f - Mathf.Exp(-18f * Time.deltaTime));
            contenedor.localRotation = Quaternion.Euler(-retroceso * 8f + bajadaRecarga * 120f, 0f, 0f);
        }
    }
}
