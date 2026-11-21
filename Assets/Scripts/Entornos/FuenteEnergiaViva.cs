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
        private BiomaPlaneta biomaFuente = BiomaPlaneta.NexusPrimordial;
        private float alturaNucleo = 2.6f;

        public string NombreFuente { get; private set; } = "Fuente de Energía Viva";

        public float RadioExtraccion => radioExtraccion;
        public bool ExtraccionActiva => extraccionActiva;
        public string EstadoExtraccion { get; private set; } = "Inactiva";

        public event Action<string> AlCambiarEstadoExtraccion;

        /// <summary>Crea la fuente con su modelo procedural en la posición indicada.</summary>
        public static FuenteEnergiaViva Crear(Transform padre, Vector3 posicion, Color colorBioma)
        {
            return Crear(padre, posicion, colorBioma, BiomaPlaneta.NexusPrimordial);
        }

        /// <summary>
        /// Crea la fuente con la forma propia del escenario: Árbol Madre (Bosque), Turbina Abisal (Abismos),
        /// Núcleo Térmico (Caldera), Monolito Central de Resonancia (Agujas) o Corazón del Planeta (Nexus).
        /// </summary>
        public static FuenteEnergiaViva Crear(Transform padre, Vector3 posicion, Color colorBioma, BiomaPlaneta bioma)
        {
            GameObject raiz = new GameObject(DatosBioma.Obtener(bioma).NombreFuente);
            raiz.transform.SetParent(padre, false);
            raiz.transform.position = posicion;
            FuenteEnergiaViva fuente = raiz.AddComponent<FuenteEnergiaViva>();
            fuente.colorSano = Color.Lerp(colorBioma, Color.white, 0.35f);
            fuente.biomaFuente = bioma;
            fuente.NombreFuente = DatosBioma.Obtener(bioma).NombreFuente;
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

            ConstruirFormaDelEscenario();

            bool esMonolito = biomaFuente == BiomaPlaneta.AgujasAleacion;
            GameObject esfera = GameObject.CreatePrimitive(esMonolito ? PrimitiveType.Cube : PrimitiveType.Sphere);
            esfera.name = esMonolito ? "Monolito de metal puro" : "Núcleo de Energía";
            esfera.transform.SetParent(transform, false);
            esfera.transform.localPosition = new Vector3(0f, alturaNucleo, 0f);
            esfera.transform.localScale = Vector3.one * 1.6f;
            Destroy(esfera.GetComponent<Collider>());
            nucleo = esfera.transform;
            renderNucleo = esfera.GetComponent<Renderer>();

            anilloA = CrearAnillo("Anillo A", biomaFuente == BiomaPlaneta.AbismosMareas ? 6f : 2.6f);
            anilloB = CrearAnillo("Anillo B", biomaFuente == BiomaPlaneta.AbismosMareas ? 7f : 3.3f);
            if (biomaFuente == BiomaPlaneta.AbismosMareas)
            {
                // Turbina Abisal: los anillos se ponen en vertical, como una turbina de marea.
                anilloA.localRotation = Quaternion.Euler(90f, 0f, 0f);
                anilloB.localRotation = Quaternion.Euler(90f, 90f, 0f);
            }

            GameObject luz = new GameObject("Luz del Núcleo");
            luz.transform.SetParent(transform, false);
            luz.transform.localPosition = new Vector3(0f, alturaNucleo, 0f);
            luzNucleo = luz.AddComponent<Light>();
            luzNucleo.type = LightType.Point;
            luzNucleo.range = 18f;
            luzNucleo.intensity = 3f;
            luzNucleo.shadows = LightShadows.None;

            ActualizarVisual(100f);
        }

        private GameObject CrearPiezaFuente(string nombre, PrimitiveType tipo, Vector3 posicion, Vector3 escala, Color color, Color emision, bool conColision)
        {
            GameObject pieza = GameObject.CreatePrimitive(tipo);
            pieza.name = nombre;
            pieza.transform.SetParent(transform, false);
            pieza.transform.localPosition = posicion;
            pieza.transform.localScale = escala;
            if (!conColision) DestroyImmediate(pieza.GetComponent<Collider>());
            MaterialesProcedurales.AplicarColor(pieza.GetComponent<Renderer>(), color, emision);
            return pieza;
        }

        private void ConstruirFormaDelEscenario()
        {
            switch (biomaFuente)
            {
                case BiomaPlaneta.BosqueSusurros:
                    // Árbol Madre: tronco central que late, copa, red de filamentos y cúpula bioeléctrica.
                    alturaNucleo = 7.5f;
                    CrearPiezaFuente("Tronco del Árbol Madre", PrimitiveType.Cylinder, new Vector3(0f, 4f, 0f), new Vector3(2.2f, 4f, 2.2f),
                        new Color(0.12f, 0.35f, 0.3f), colorSano * 0.5f, true);
                    for (int i = 0; i < 6; i++)
                    {
                        GameObject filamento = CrearPiezaFuente("Filamento luminoso", PrimitiveType.Cylinder, Vector3.zero, new Vector3(0.05f, 6f, 0.05f),
                            colorSano, colorSano * 2f, false);
                        filamento.transform.localPosition = Quaternion.Euler(0f, i * 60f, 0f) * new Vector3(0f, 6.5f, 4.5f);
                        filamento.transform.localRotation = Quaternion.Euler(70f, i * 60f, 0f);
                    }
                    CrearPiezaFuente("Base de la cúpula bioeléctrica", PrimitiveType.Cylinder, new Vector3(0f, 0.05f, 0f), new Vector3(14f, 0.02f, 14f),
                        colorSano * 0.2f, colorSano * 0.6f, false);
                    break;

                case BiomaPlaneta.AbismosMareas:
                    alturaNucleo = 4f;
                    CrearPiezaFuente("Pozo abisal", PrimitiveType.Cylinder, new Vector3(0f, 0.06f, 0f), new Vector3(9f, 0.05f, 9f),
                        new Color(0.05f, 0.1f, 0.35f), new Color(0.2f, 0.35f, 1f), false);
                    break;

                case BiomaPlaneta.CalderaForjas:
                    // Núcleo Térmico: super-fumarola con columna de fuego controlado y generadores de vapor.
                    alturaNucleo = 5f;
                    CrearPiezaFuente("Fumarola", PrimitiveType.Cylinder, new Vector3(0f, 2f, 0f), new Vector3(2.6f, 2f, 2.6f),
                        new Color(0.08f, 0.07f, 0.08f), Color.black, true);
                    CrearPiezaFuente("Boca de magma", PrimitiveType.Cylinder, new Vector3(0f, 4.05f, 0f), new Vector3(2f, 0.05f, 2f),
                        colorSano, new Color(1f, 0.45f, 0.1f) * 3f, false);
                    for (int i = -1; i <= 1; i += 2)
                    {
                        CrearPiezaFuente("Generador de vapor", PrimitiveType.Cylinder, new Vector3(i * 5f, 1.8f, 0f), new Vector3(1.6f, 1.8f, 1.6f),
                            new Color(0.3f, 0.28f, 0.27f), Color.black, true);
                        FabricaEfectosVisuales.CrearAmbienteBioma(transform, transform.position + new Vector3(i * 5f, 3.8f, 0f), new Color(0.9f, 0.88f, 0.85f), 0.5f, -0.3f);
                    }
                    FabricaEfectosVisuales.CrearAmbienteBioma(transform, transform.position + new Vector3(0f, 4.2f, 0f), new Color(1f, 0.55f, 0.15f), 0.8f, -0.4f);
                    break;

                case BiomaPlaneta.AgujasAleacion:
                    alturaNucleo = 4f;
                    break;

                default:
                    // Corazón del Planeta: esfera masiva flotante sobre un anillo de piedra translúcida.
                    alturaNucleo = 4.5f;
                    CrearPiezaFuente("Anillo de piedra translúcida", PrimitiveType.Cylinder, new Vector3(0f, 0.2f, 0f), new Vector3(6f, 0.2f, 6f),
                        new Color(0.72f, 0.74f, 0.82f), colorSano * 0.3f, true);
                    break;
            }
        }

        private Transform CrearAnillo(string nombre, float diametro)
        {
            GameObject anillo = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            anillo.name = nombre;
            anillo.transform.SetParent(transform, false);
            anillo.transform.localPosition = new Vector3(0f, alturaNucleo, 0f);
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

            if (nucleo != null)
            {
                nucleo.localScale = Vector3.one * (1.3f + 0.3f * salud) * pulso;
                if (biomaFuente == BiomaPlaneta.AgujasAleacion)
                {
                    // El Monolito gira sobre su eje sin tocar ninguna superficie.
                    nucleo.Rotate(Vector3.up, 30f * Mathf.Max(0.2f, salud) * Time.deltaTime, Space.World);
                    nucleo.localPosition = new Vector3(0f, alturaNucleo + Mathf.Sin(Time.time) * 0.15f, 0f);
                }
            }
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
