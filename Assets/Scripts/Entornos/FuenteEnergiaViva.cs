using System;
using UnityEngine;

namespace VoidformBioForge
{
    /// <summary>
    /// La Fuente de Energía Viva del planeta: corazón luminoso en el centro de cada bioma.
    /// -----------------------------------------------------------------
    /// Nivel 1: objeto central con núcleo brillante, anillos giratorios y luz pulsante.
    /// Nivel 2: si está activa la extracción (Modo Historia), los Destructores vivos dentro del
    ///          radio extraen energía; si hay un Cuidador vivo dentro, la extracción se detiene (disputa).
    /// Nivel 3: su color, brillo y pulso reflejan la integridad del ecosistema: florece cuando el
    ///          planeta está sano y se apaga, gris y lenta, cuando la extracción lo ha vaciado.
    /// </summary>
    public class FuenteEnergiaViva : MonoBehaviour
    {
        [SerializeField] private float radioExtraccion = 7f;
        [SerializeField] private float energiaPorDestructorPorSegundo = 0.6f;
        [SerializeField] private bool extraccionActiva = false;

        private Transform nucleo;
        private Transform anilloA;
        private Transform anilloB;
        private Light luzNucleo;
        private Renderer renderNucleo;
        private Color colorSano = new Color(0.55f, 1f, 0.85f);
        private readonly Color colorAgotado = new Color(0.35f, 0.33f, 0.32f);
        private string ultimoEstado = "";

        public float RadioExtraccion => radioExtraccion;
        public bool ExtraccionActiva => extraccionActiva;
        public string EstadoExtraccion { get; private set; } = "Inactiva";

        public event Action<string> AlCambiarEstadoExtraccion;

        /// <summary>Crea la fuente con su modelo procedural en la posición indicada.</summary>
        public static FuenteEnergiaViva Crear(Transform padre, Vector3 posicion, Color colorBioma)
        {
            GameObject raiz = new GameObject("Fuente de Energía Viva");
            raiz.transform.SetParent(padre, false);
            raiz.transform.position = posicion;
            FuenteEnergiaViva fuente = raiz.AddComponent<FuenteEnergiaViva>();
            fuente.colorSano = Color.Lerp(colorBioma, Color.white, 0.35f);
            fuente.ConstruirModelo();
            return fuente;
        }

        public void EstablecerExtraccionActiva(bool activa)
        {
            extraccionActiva = activa;
            ActualizarEstado(activa ? "Sin extractores" : "Inactiva");
        }

        private void ConstruirModelo()
        {
            // Pedestal de roca con raíces de cristal.
            GameObject pedestal = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            pedestal.name = "Pedestal";
            pedestal.transform.SetParent(transform, false);
            pedestal.transform.localPosition = new Vector3(0f, 0.4f, 0f);
            pedestal.transform.localScale = new Vector3(3.2f, 0.4f, 3.2f);
            MaterialesProcedurales.AplicarColor(pedestal.GetComponent<Renderer>(), new Color(0.2f, 0.22f, 0.26f));

            GameObject esfera = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            esfera.name = "Núcleo de Energía";
            esfera.transform.SetParent(transform, false);
            esfera.transform.localPosition = new Vector3(0f, 2.6f, 0f);
            esfera.transform.localScale = Vector3.one * 1.6f;
            Destroy(esfera.GetComponent<Collider>());
            nucleo = esfera.transform;
            renderNucleo = esfera.GetComponent<Renderer>();

            anilloA = CrearAnillo("Anillo A", 2.6f);
            anilloB = CrearAnillo("Anillo B", 3.3f);

            GameObject luz = new GameObject("Luz del Núcleo");
            luz.transform.SetParent(transform, false);
            luz.transform.localPosition = new Vector3(0f, 2.6f, 0f);
            luzNucleo = luz.AddComponent<Light>();
            luzNucleo.type = LightType.Point;
            luzNucleo.range = 18f;
            luzNucleo.intensity = 3f;
            luzNucleo.shadows = LightShadows.None;

            ActualizarVisual(100f);
        }

        private Transform CrearAnillo(string nombre, float diametro)
        {
            GameObject anillo = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            anillo.name = nombre;
            anillo.transform.SetParent(transform, false);
            anillo.transform.localPosition = new Vector3(0f, 2.6f, 0f);
            anillo.transform.localScale = new Vector3(diametro, 0.03f, diametro);
            Destroy(anillo.GetComponent<Collider>());
            MaterialesProcedurales.AplicarColor(anillo.GetComponent<Renderer>(), colorSano * 0.6f, colorSano);
            return anillo.transform;
        }

        private void Update()
        {
            float integridad = GestorEquilibrioEcosistema.Instancia != null ? GestorEquilibrioEcosistema.Instancia.Integridad : 100f;
            ActualizarVisual(integridad);

            if (!extraccionActiva) return;

            int destructores = 0;
            int cuidadores = 0;
            foreach (SistemaSaludYTraje entidad in SistemaSaludYTraje.Entidades)
            {
                if (entidad == null || entidad.EstaMuerto) continue;
                if (Vector3.Distance(entidad.transform.position, transform.position) > radioExtraccion) continue;
                if (entidad.Bando == BandoJuego.Destructor) destructores++;
                else cuidadores++;
            }

            if (destructores > 0 && cuidadores == 0)
            {
                GestorEquilibrioEcosistema.Instancia?.RegistrarExtraccion(destructores * energiaPorDestructorPorSegundo * Time.deltaTime, "Extracción en la Fuente");
                ActualizarEstado($"Extrayendo ({destructores} humano(s))");
            }
            else if (destructores > 0 && cuidadores > 0)
            {
                ActualizarEstado("Disputada: el guardián bloquea la extracción");
            }
            else
            {
                ActualizarEstado("Sin extractores");
            }
        }

        private void ActualizarVisual(float integridad)
        {
            float salud = Mathf.Clamp01(integridad / 100f);
            float velocidadPulso = Mathf.Lerp(0.6f, 2.2f, salud);
            float pulso = 0.85f + Mathf.Sin(Time.time * velocidadPulso * Mathf.PI) * 0.15f;
            Color color = Color.Lerp(colorAgotado, colorSano, salud);

            if (nucleo != null) nucleo.localScale = Vector3.one * (1.3f + 0.3f * salud) * pulso;
            if (anilloA != null) anilloA.Rotate(Vector3.up, 40f * salud * Time.deltaTime, Space.Self);
            if (anilloB != null)
            {
                anilloB.Rotate(Vector3.forward, 25f * salud * Time.deltaTime, Space.Self);
            }
            if (renderNucleo != null) MaterialesProcedurales.AplicarColor(renderNucleo, color, color * (0.6f + 2.2f * salud) * pulso);
            if (luzNucleo != null)
            {
                luzNucleo.color = color;
                luzNucleo.intensity = Mathf.Lerp(0.6f, 4f, salud) * pulso;
            }
        }

        private void ActualizarEstado(string estado)
        {
            if (estado == ultimoEstado) return;
            ultimoEstado = estado;
            EstadoExtraccion = estado;
            AlCambiarEstadoExtraccion?.Invoke(estado);
        }
    }
}
