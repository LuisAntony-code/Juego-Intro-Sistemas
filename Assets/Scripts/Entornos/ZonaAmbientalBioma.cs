using UnityEngine;

namespace VoidformBioForge
{
    /// <summary>
    /// Zona del entorno que interactúa de forma distinta con cada facción (interacción facción–bioma).
    /// Nivel 1: área circular con nombre. Nivel 2: daño o ralentización a Destructores y curación a
    /// Cuidadores en "pulsos" de medio segundo. Nivel 3: el tipo de daño respeta las resistencias del
    /// traje (por ejemplo, el Traje Térmico casi ignora la lava; el Hidrostático no se ralentiza en el agua).
    /// </summary>
    public class ZonaAmbientalBioma : MonoBehaviour
    {
        [SerializeField] private string nombreZona = "Zona elemental";
        [SerializeField] private float radio = 4f;
        [SerializeField] private TipoDano tipoDano = TipoDano.Fuego;
        [SerializeField] private float danoPorSegundoDestructores = 0f;
        [SerializeField] [Range(0.2f, 1f)] private float factorVelocidadDestructores = 1f;
        [SerializeField] private float curacionPorSegundoCuidadores = 0f;
        [SerializeField] private float intervaloPulso = 0.5f;

        private float proximoPulso;

        public void Configurar(string nombre, float nuevoRadio, TipoDano tipo, float danoDps, float factorVelocidad, float curacionDps)
        {
            nombreZona = nombre;
            radio = nuevoRadio;
            tipoDano = tipo;
            danoPorSegundoDestructores = danoDps;
            factorVelocidadDestructores = factorVelocidad;
            curacionPorSegundoCuidadores = curacionDps;
        }

        private void Update()
        {
            if (Time.time < proximoPulso) return;
            proximoPulso = Time.time + intervaloPulso;

            foreach (SistemaSaludYTraje entidad in SistemaSaludYTraje.Entidades)
            {
                if (entidad == null || entidad.EstaMuerto) continue;
                Vector3 delta = entidad.transform.position - transform.position;
                delta.y = 0f;
                if (delta.magnitude > radio) continue;

                if (entidad.Bando == BandoJuego.Destructor)
                {
                    if (danoPorSegundoDestructores > 0f)
                    {
                        entidad.RecibirDano(InformacionDano.Ambiental(danoPorSegundoDestructores * intervaloPulso, tipoDano, nombreZona, transform.position));
                    }

                    if (factorVelocidadDestructores < 0.99f)
                    {
                        bool inmuneAlAgua = tipoDano == TipoDano.Agua && entidad.Traje != null && entidad.Traje.tipo == TipoTraje.HidrostaticoAnclado;
                        if (!inmuneAlAgua)
                        {
                            entidad.GetComponent<SistemaEstadosAlterados>()?.AplicarRalentizacion(factorVelocidadDestructores, intervaloPulso + 0.15f);
                        }
                    }
                }
                else if (curacionPorSegundoCuidadores > 0f)
                {
                    entidad.Curar(curacionPorSegundoCuidadores * intervaloPulso);
                }
            }
        }
    }
}
