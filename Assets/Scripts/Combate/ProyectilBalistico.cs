using System;
using System.Collections.Generic;
using UnityEngine;

namespace VoidformBioForge
{
    /// <summary>Aspecto del proyectil.</summary>
    public enum FormaProyectil
    {
        Flecha,
        OndaDeFilo,
        Cadena,
        Arpon
    }

    /// <summary>
    /// Proyectil físico (flechas, onda de filo, cadena de la maza, cable arpón).
    /// Nivel 1: avanza con velocidad y gravedad y se dibuja con su forma.
    /// Nivel 2: detección continua (barrido entre la posición anterior y la nueva) para no atravesar nada a gran velocidad,
    ///          con radio opcional para ondas anchas; daño, críticos en el punto débil y perforación.
    /// Nivel 3: callbacks al tocar una entidad o una superficie (amarres, arrastres, impulsos), atraviesa a los aliados y
    ///          las barreras de su bando, y la cadena o el cable se dibujan unidos al lanzador.
    /// </summary>
    public class ProyectilBalistico : MonoBehaviour
    {
        private Vector3 velocidad;
        private float gravedad;
        private float alcanceRestante;
        private float radio;
        private int perforacionesRestantes;
        private float dano;
        private float impactoAguante;
        private float multiplicadorCritico = 1f;
        private TipoDano tipoDano;
        private string origen;
        private SistemaSaludYTraje lanzador;
        private Color color;
        private FormaProyectil forma;
        private Action<SistemaSaludYTraje, Vector3> alImpactarEntidad;
        private Action<Vector3, Vector3> alImpactarSuperficie;
        private Action<bool, bool> alConfirmarImpacto;
        private readonly HashSet<SistemaSaludYTraje> yaAlcanzados = new HashSet<SistemaSaludYTraje>();
        private LineRenderer cuerda;

        public static ProyectilBalistico Lanzar(SistemaSaludYTraje lanzador, Vector3 posicion, Vector3 direccion, float rapidez, float gravedad,
            float alcance, float dano, TipoDano tipo, string origen, Color color, FormaProyectil forma, int perforaciones = 1, float radio = 0f,
            float multiplicadorCritico = 1f, float impactoAguante = 0f,
            Action<SistemaSaludYTraje, Vector3> alImpactarEntidad = null, Action<Vector3, Vector3> alImpactarSuperficie = null,
            Action<bool, bool> alConfirmarImpacto = null)
        {
            GameObject go = new GameObject($"Proyectil {forma}");
            go.transform.position = posicion;
            ProyectilBalistico p = go.AddComponent<ProyectilBalistico>();
            p.lanzador = lanzador;
            p.velocidad = direccion.normalized * rapidez;
            p.gravedad = gravedad;
            p.alcanceRestante = alcance;
            p.dano = dano;
            p.tipoDano = tipo;
            p.origen = origen;
            p.color = color;
            p.forma = forma;
            p.perforacionesRestantes = Mathf.Max(1, perforaciones);
            p.radio = radio;
            p.multiplicadorCritico = multiplicadorCritico;
            p.impactoAguante = impactoAguante;
            p.alImpactarEntidad = alImpactarEntidad;
            p.alImpactarSuperficie = alImpactarSuperficie;
            p.alConfirmarImpacto = alConfirmarImpacto;
            p.ConstruirVisual();
            return p;
        }

        private void ConstruirVisual()
        {
            GameObject cuerpo = GameObject.CreatePrimitive(forma == FormaProyectil.OndaDeFilo ? PrimitiveType.Cube : PrimitiveType.Capsule);
            DestroyImmediate(cuerpo.GetComponent<Collider>());
            cuerpo.transform.SetParent(transform, false);
            Renderer r = cuerpo.GetComponent<Renderer>();
            r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            switch (forma)
            {
                case FormaProyectil.Flecha:
                    cuerpo.transform.localScale = new Vector3(0.03f, 0.45f, 0.03f);
                    cuerpo.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
                    MaterialesProcedurales.AplicarColor(r, new Color(0.55f, 0.42f, 0.28f), color * 0.8f);
                    break;
                case FormaProyectil.OndaDeFilo:
                    cuerpo.transform.localScale = new Vector3(Mathf.Max(1f, radio * 2f), 0.5f, 0.12f);
                    r.sharedMaterial = MaterialesProcedurales.ObtenerMaterialLinea(color);
                    break;
                default:
                    cuerpo.transform.localScale = new Vector3(0.18f, 0.18f, 0.18f);
                    MaterialesProcedurales.AplicarColor(r, new Color(0.35f, 0.33f, 0.32f), color * 0.5f);
                    break;
            }

            TrailRenderer estela = gameObject.AddComponent<TrailRenderer>();
            estela.time = forma == FormaProyectil.OndaDeFilo ? 0.25f : 0.12f;
            estela.startWidth = forma == FormaProyectil.OndaDeFilo ? radio : 0.04f;
            estela.endWidth = 0f;
            estela.sharedMaterial = MaterialesProcedurales.ObtenerMaterialLinea(color);
            estela.startColor = color;
            estela.endColor = new Color(color.r, color.g, color.b, 0f);
            estela.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;

            if ((forma == FormaProyectil.Cadena || forma == FormaProyectil.Arpon) && lanzador != null)
            {
                cuerda = gameObject.AddComponent<LineRenderer>();
                cuerda.positionCount = 2;
                cuerda.startWidth = 0.03f;
                cuerda.endWidth = 0.03f;
                cuerda.sharedMaterial = MaterialesProcedurales.ObtenerMaterialLinea(new Color(0.7f, 0.7f, 0.72f));
                cuerda.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            }
        }

        private void Update()
        {
            float dt = Time.deltaTime;
            velocidad += Vector3.down * gravedad * dt;
            Vector3 inicio = transform.position;
            Vector3 desplazamiento = velocidad * dt;
            float paso = desplazamiento.magnitude;
            if (paso > 0.0001f) transform.rotation = Quaternion.LookRotation(desplazamiento);

            if (paso > 0f && Barrer(inicio, desplazamiento / paso, paso)) return;

            transform.position = inicio + desplazamiento;
            alcanceRestante -= paso;
            if (cuerda != null && lanzador != null)
            {
                cuerda.SetPosition(0, lanzador.transform.position + Vector3.up * 1.3f);
                cuerda.SetPosition(1, transform.position);
            }
            if (alcanceRestante <= 0f) Destroy(gameObject);
        }

        /// <summary>Comprueba el tramo recorrido este fotograma. Devuelve verdadero si el proyectil terminó.</summary>
        private bool Barrer(Vector3 inicio, Vector3 direccion, float distancia)
        {
            RaycastHit[] impactos = radio > 0.05f
                ? Physics.SphereCastAll(inicio, radio, direccion, distancia, ~0, QueryTriggerInteraction.Ignore)
                : Physics.RaycastAll(inicio, direccion, distancia, ~0, QueryTriggerInteraction.Ignore);
            Array.Sort(impactos, (a, b) => a.distance.CompareTo(b.distance));

            foreach (RaycastHit impacto in impactos)
            {
                SistemaSaludYTraje objetivo = impacto.collider.GetComponentInParent<SistemaSaludYTraje>();
                Vector3 punto = impacto.point == Vector3.zero ? inicio : impacto.point;

                if (objetivo != null)
                {
                    if (objetivo == lanzador || objetivo.EstaMuerto || yaAlcanzados.Contains(objetivo)) continue;
                    if (lanzador != null && objetivo.Bando == lanzador.Bando) continue;
                    yaAlcanzados.Add(objetivo);

                    float critico = PuntoDebil.EvaluarPunto(objetivo, punto, out bool esCritico);
                    if (esCritico) critico = Mathf.Max(critico, multiplicadorCritico);
                    InformacionDano info = InformacionDano.Crear(dano * critico, tipoDano, lanzador, origen);
                    info.EsCritico = esCritico;
                    info.ImpactoAguante = impactoAguante;
                    objetivo.RecibirDano(info);
                    FabricaEfectosVisuales.CrearImpacto(punto, -direccion, esCritico ? PaletaVoidform.Disputa : PaletaVoidform.Alerta);
                    alConfirmarImpacto?.Invoke(objetivo.EstaMuerto, esCritico);
                    alImpactarEntidad?.Invoke(objetivo, punto);

                    perforacionesRestantes--;
                    if (perforacionesRestantes <= 0)
                    {
                        Destroy(gameObject);
                        return true;
                    }
                    continue;
                }

                // Un barrido ancho que ya empieza solapado con el suelo no debe detenerse por él.
                if (radio > 0.05f && impacto.distance <= 0f) continue;

                BarreraEnergia barrera = impacto.collider.GetComponent<BarreraEnergia>();
                if (barrera != null && lanzador != null && barrera.Bando == lanzador.Bando) continue;

                // Superficie: el proyectil se detiene (las ondas se disipan contra la geometría).
                FabricaEfectosVisuales.CrearImpacto(punto, impacto.normal, color);
                alImpactarSuperficie?.Invoke(punto, impacto.normal);
                Destroy(gameObject);
                return true;
            }
            return false;
        }
    }
}
