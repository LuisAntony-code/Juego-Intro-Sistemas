using UnityEngine;

namespace VoidformBioForge
{
    /// <summary>
    /// Muro de roca temporal que levanta Korvhal (Guardián de Tierra) con "Muralla y Temblor".
    /// Nivel 1: bloquea balas y paso. Nivel 2: emerge del suelo con animación y dura unos segundos.
    /// Nivel 3: al expirar se hunde de nuevo en el suelo y se destruye, liberando el paso.
    /// </summary>
    public class MuroDeRocaElemental : MonoBehaviour
    {
        [SerializeField] private float duracion = 6f;
        [SerializeField] private float velocidadEmergencia = 6f;
        [SerializeField] private Vector3 tamano = new Vector3(5f, 3.2f, 0.9f);

        private float tiempoCreacion;
        private float alturaObjetivo;
        private bool hundiendose;

        public static MuroDeRocaElemental Crear(Vector3 posicion, Quaternion rotacion, float duracionSegundos)
        {
            GameObject muro = GameObject.CreatePrimitive(PrimitiveType.Cube);
            muro.name = "Muro de Roca Elemental";
            muro.transform.SetPositionAndRotation(posicion, Quaternion.Euler(0f, rotacion.eulerAngles.y, 0f));

            MuroDeRocaElemental componente = muro.AddComponent<MuroDeRocaElemental>();
            componente.duracion = duracionSegundos;
            muro.transform.localScale = componente.tamano;

            Renderer r = muro.GetComponent<Renderer>();
            MaterialesProcedurales.AplicarColor(r, new Color(0.42f, 0.34f, 0.27f), new Color(0.9f, 0.6f, 0.25f) * 0.4f);

            FabricaEfectosVisuales.CrearEfectoElemental(TipoElemento.Tierra, posicion, 2.5f);
            return componente;
        }

        private void Start()
        {
            tiempoCreacion = Time.time;
            alturaObjetivo = transform.position.y + tamano.y * 0.5f;
            transform.position = new Vector3(transform.position.x, alturaObjetivo - tamano.y, transform.position.z);
        }

        private void Update()
        {
            Vector3 p = transform.position;
            if (!hundiendose)
            {
                p.y = Mathf.MoveTowards(p.y, alturaObjetivo, velocidadEmergencia * Time.deltaTime);
                transform.position = p;
                if (Time.time - tiempoCreacion >= duracion) hundiendose = true;
            }
            else
            {
                p.y -= velocidadEmergencia * 0.5f * Time.deltaTime;
                transform.position = p;
                if (p.y < alturaObjetivo - tamano.y) Destroy(gameObject);
            }
        }
    }
}
