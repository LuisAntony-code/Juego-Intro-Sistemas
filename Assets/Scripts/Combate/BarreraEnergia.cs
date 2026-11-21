using System.Collections.Generic;
using UnityEngine;

namespace VoidformBioForge
{
    /// <summary>
    /// Barrera física de energía (Égida refractaria del Paladín, Postura de bastión de la Maza con Escudo).
    /// Nivel 1: muro con colisión que detiene balas, proyectiles y personajes.
    /// Nivel 2: protege a los aliados del daño que llega desde el otro lado (también el daño en área de los
    ///          guardianes, comprobando si la barrera se interpone entre el atacante y el aliado).
    /// Nivel 3: refleja un porcentaje del daño bloqueado hacia el atacante y deja pasar los disparos aliados.
    /// </summary>
    public class BarreraEnergia : MonoBehaviour
    {
        private static readonly List<BarreraEnergia> activas = new List<BarreraEnergia>();

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void Reiniciar() => activas.Clear();

        private BandoJuego bando;
        private SistemaSaludYTraje dueno;
        private float porcentajeReflejo;
        private float finTiempo;
        private float duracion;
        private BoxCollider caja;
        private Renderer render;
        private Color color;

        public BandoJuego Bando => bando;

        public static BarreraEnergia Crear(Vector3 posicion, Quaternion rotacion, Vector2 tamano, float duracionSegundos,
            float reflejo, SistemaSaludYTraje propietario, Color colorBarrera)
        {
            GameObject go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.name = "Barrera de Energía";
            go.transform.SetPositionAndRotation(posicion + Vector3.up * (tamano.y * 0.5f), Quaternion.Euler(0f, rotacion.eulerAngles.y, 0f));
            go.transform.localScale = new Vector3(tamano.x, tamano.y, 0.15f);
            BarreraEnergia b = go.AddComponent<BarreraEnergia>();
            b.bando = propietario != null ? propietario.Bando : BandoJuego.Destructor;
            b.dueno = propietario;
            b.porcentajeReflejo = reflejo;
            b.duracion = Mathf.Max(0.1f, duracionSegundos);
            b.finTiempo = Time.time + b.duracion;
            b.caja = go.GetComponent<BoxCollider>();
            b.render = go.GetComponent<Renderer>();
            b.color = colorBarrera;
            b.render.sharedMaterial = MaterialesProcedurales.ObtenerMaterialLinea(colorBarrera);
            b.render.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            FabricaEfectosVisuales.CrearAnilloOnda(posicion + Vector3.up * 0.1f, Mathf.Max(tamano.x, 2f), colorBarrera);
            return b;
        }

        private void OnEnable() { if (!activas.Contains(this)) activas.Add(this); }
        private void OnDisable() { activas.Remove(this); }

        private void Update()
        {
            float restante = finTiempo - Time.time;
            if (restante <= 0f)
            {
                FabricaEfectosVisuales.CrearImpacto(transform.position, transform.forward, color);
                Destroy(gameObject);
                return;
            }
            // Parpadeo de aviso en el último segundo.
            float pulso = restante < 1f ? (Mathf.PingPong(Time.time * 8f, 1f) * 0.6f + 0.4f) : 0.85f + Mathf.Sin(Time.time * 3f) * 0.15f;
            transform.localScale = new Vector3(transform.localScale.x, transform.localScale.y, 0.15f * pulso);
        }

        /// <summary>
        /// Devuelve verdadero si una barrera del bando de la víctima se interpone entre el origen del daño y la víctima.
        /// Aplica además el reflejo de daño sobre el atacante.
        /// </summary>
        public static bool Intercepta(Vector3 origen, Vector3 destino, BandoJuego bandoVictima, InformacionDano info)
        {
            if (activas.Count == 0) return false;
            Vector3 direccion = destino - origen;
            float distancia = direccion.magnitude;
            if (distancia < 0.01f) return false;
            Ray rayo = new Ray(origen, direccion / distancia);

            foreach (BarreraEnergia b in activas)
            {
                if (b == null || b.bando != bandoVictima || b.caja == null) continue;
                if (!b.caja.Raycast(rayo, out RaycastHit impacto, distancia)) continue;

                FabricaEfectosVisuales.CrearImpacto(impacto.point, impacto.normal, b.color);
                if (b.porcentajeReflejo > 0f && info.Atacante != null)
                {
                    SistemaSaludYTraje atacante = info.Atacante.GetComponent<SistemaSaludYTraje>();
                    if (atacante != null && !atacante.EstaMuerto && atacante.Bando != b.bando)
                    {
                        InformacionDano reflejo = InformacionDano.Crear(info.Cantidad * b.porcentajeReflejo, TipoDano.Cinetico, b.dueno, "Égida refractaria");
                        atacante.RecibirDano(reflejo);
                        FabricaEfectosVisuales.CrearTrazador(impacto.point, atacante.transform.position + Vector3.up * 1.4f, b.color);
                    }
                }
                return true;
            }
            return false;
        }
    }
}
