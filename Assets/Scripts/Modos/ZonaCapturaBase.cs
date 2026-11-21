using System;
using System.Collections.Generic;
using UnityEngine;

namespace VoidformBioForge
{
    /// <summary>
    /// Representa una de las 5 bases del Modo Conquista.
    /// -----------------------------------------------------------------
    /// Nivel 1: zona circular con un progreso de captura entre -100 (Destructores) y +100 (Cuidadores).
    /// Nivel 2: reglas de captura y disputa:
    ///   • Solo un bando dentro → el progreso avanza hacia su lado (más rápido cuantos más haya, máx. ×2).
    ///   • Ambos bandos dentro → el progreso se congela y la base queda "en disputa".
    ///   • Nadie dentro → si la base no tiene dueño, el progreso vuelve lentamente a 0 (neutral).
    ///   • Llegar a ±100 → la base pasa a estar dominada por ese bando.
    ///   • Una base dominada solo se pierde cuando el rival la neutraliza (progreso cruza 0).
    /// Nivel 3: cuenta entidades vivas sin duplicados, notifica cambios de estado y de progreso para el HUD,
    ///          genera su propio indicador visual (anillo + faro de color) y se registra en una lista
    ///          estática para que bots, ecosistema y gestor de Conquista la encuentren sin búsquedas costosas.
    /// </summary>
    public class ZonaCapturaBase : MonoBehaviour
    {
        private static readonly List<ZonaCapturaBase> basesActivas = new List<ZonaCapturaBase>();
        public static IReadOnlyList<ZonaCapturaBase> BasesActivas => basesActivas;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ReiniciarEstadoEstatico()
        {
            basesActivas.Clear();
        }

        [SerializeField] private int indiceBase;
        [SerializeField] private float radioCaptura = 8f;
        [SerializeField] private float velocidadCapturaPorSegundo = 25f;
        [SerializeField] private float velocidadNeutralizacionPasiva = 6f;
        [SerializeField] private bool crearVisualProcedural = true;

        private BandoJuego? duenoActual;
        private Renderer renderAnillo;
        private Renderer renderFaro;
        private Light luzFaro;
        private float ultimoProgresoNotificado = float.MinValue;

        public int IndiceBase => indiceBase;
        public string Letra => UtilidadesTipos.LetraBase(indiceBase);
        public float RadioCaptura => radioCaptura;
        public float ProgresoCaptura { get; private set; }
        public EstadoControlBase EstadoActual { get; private set; } = EstadoControlBase.Neutral;
        public bool SiendoDisputada { get; private set; }
        public int DestructoresDentro { get; private set; }
        public int CuidadoresDentro { get; private set; }

        public event Action<ZonaCapturaBase> AlCambiarEstadoBase;
        public event Action<ZonaCapturaBase> AlCambiarProgreso;

        public static ZonaCapturaBase Crear(Transform padre, Vector3 posicion, int indice)
        {
            GameObject go = new GameObject($"Base {UtilidadesTipos.LetraBase(indice)}");
            go.transform.SetParent(padre, false);
            go.transform.position = posicion;
            ZonaCapturaBase zona = go.AddComponent<ZonaCapturaBase>();
            zona.indiceBase = indice;
            zona.ConstruirVisual();
            return zona;
        }

        private void OnEnable()
        {
            if (!basesActivas.Contains(this)) basesActivas.Add(this);
        }

        private void OnDisable()
        {
            basesActivas.Remove(this);
        }

        private void Start()
        {
            if (crearVisualProcedural && renderAnillo == null) ConstruirVisual();
        }

        /// <summary>Vuelve la base a neutral (al reiniciar una partida).</summary>
        public void Reiniciar()
        {
            ProgresoCaptura = 0f;
            duenoActual = null;
            SiendoDisputada = false;
            EstadoActual = EstadoControlBase.Neutral;
            AlCambiarEstadoBase?.Invoke(this);
            AlCambiarProgreso?.Invoke(this);
            ActualizarVisual();
        }

        private void Update()
        {
            ContarEntidadesDentro();

            EstadoControlBase estadoAnterior = EstadoActual;
            float progresoAnterior = ProgresoCaptura;

            int d = DestructoresDentro;
            int c = CuidadoresDentro;
            SiendoDisputada = d > 0 && c > 0;

            if (d > 0 && c == 0)
            {
                float velocidad = velocidadCapturaPorSegundo * Mathf.Min(2f, 1f + (d - 1) * 0.35f);
                ProgresoCaptura = Mathf.Max(-100f, ProgresoCaptura - velocidad * Time.deltaTime);
            }
            else if (c > 0 && d == 0)
            {
                float velocidad = velocidadCapturaPorSegundo * Mathf.Min(2f, 1f + (c - 1) * 0.35f);
                ProgresoCaptura = Mathf.Min(100f, ProgresoCaptura + velocidad * Time.deltaTime);
            }
            else if (d == 0 && c == 0 && duenoActual == null)
            {
                ProgresoCaptura = Mathf.MoveTowards(ProgresoCaptura, 0f, velocidadNeutralizacionPasiva * Time.deltaTime);
            }

            // Cambios de dueño.
            if (ProgresoCaptura <= -99.9f) duenoActual = BandoJuego.Destructor;
            else if (ProgresoCaptura >= 99.9f) duenoActual = BandoJuego.Cuidador;
            else if (duenoActual == BandoJuego.Destructor && ProgresoCaptura >= 0f) duenoActual = null;
            else if (duenoActual == BandoJuego.Cuidador && ProgresoCaptura <= 0f) duenoActual = null;

            if (duenoActual == BandoJuego.Destructor) EstadoActual = EstadoControlBase.DominadaDestructores;
            else if (duenoActual == BandoJuego.Cuidador) EstadoActual = EstadoControlBase.DominadaCuidadores;
            else if (SiendoDisputada || Mathf.Abs(ProgresoCaptura) > 0.5f) EstadoActual = EstadoControlBase.EnDisputa;
            else EstadoActual = EstadoControlBase.Neutral;

            if (EstadoActual != estadoAnterior)
            {
                AlCambiarEstadoBase?.Invoke(this);
                if (EstadoActual == EstadoControlBase.DominadaDestructores || EstadoActual == EstadoControlBase.DominadaCuidadores)
                {
                    BandoJuego bando = EstadoActual == EstadoControlBase.DominadaDestructores ? BandoJuego.Destructor : BandoJuego.Cuidador;
                    BusEventosJuego.PublicarMensaje($"Base {Letra} dominada por los {UtilidadesTipos.NombreBando(bando)}.",
                        bando == BandoJuego.Destructor ? TipoMensajeSistema.Advertencia : TipoMensajeSistema.Exito);
                }
            }

            if (Mathf.Abs(ProgresoCaptura - ultimoProgresoNotificado) > 0.5f || (progresoAnterior != ProgresoCaptura && Mathf.Abs(ProgresoCaptura) >= 99.9f))
            {
                ultimoProgresoNotificado = ProgresoCaptura;
                AlCambiarProgreso?.Invoke(this);
            }

            ActualizarVisual();
        }

        private void ContarEntidadesDentro()
        {
            int d = 0;
            int c = 0;
            // La lista estática no contiene duplicados (una entidad = un registro), a diferencia de
            // OverlapSphere, que devolvería un resultado por cada colisionador del personaje.
            foreach (SistemaSaludYTraje entidad in SistemaSaludYTraje.Entidades)
            {
                if (entidad == null || entidad.EstaMuerto) continue;
                Vector3 delta = entidad.transform.position - transform.position;
                delta.y = 0f;
                if (delta.magnitude > radioCaptura) continue;
                if (entidad.Bando == BandoJuego.Destructor) d++;
                else c++;
            }
            DestructoresDentro = d;
            CuidadoresDentro = c;
        }

        // ================== Visual ==================

        private void ConstruirVisual()
        {
            if (!crearVisualProcedural || renderAnillo != null) return;

            GameObject anillo = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            anillo.name = "Anillo de Captura";
            anillo.transform.SetParent(transform, false);
            anillo.transform.localPosition = new Vector3(0f, 0.04f, 0f);
            anillo.transform.localScale = new Vector3(radioCaptura * 2f, 0.02f, radioCaptura * 2f);
            DestroyImmediate(anillo.GetComponent<Collider>());
            renderAnillo = anillo.GetComponent<Renderer>();

            GameObject faro = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            faro.name = "Faro de la Base";
            faro.transform.SetParent(transform, false);
            faro.transform.localPosition = new Vector3(0f, 6f, 0f);
            faro.transform.localScale = new Vector3(0.35f, 6f, 0.35f);
            DestroyImmediate(faro.GetComponent<Collider>());
            renderFaro = faro.GetComponent<Renderer>();

            GameObject luz = new GameObject("Luz de la Base");
            luz.transform.SetParent(transform, false);
            luz.transform.localPosition = new Vector3(0f, 3f, 0f);
            luzFaro = luz.AddComponent<Light>();
            luzFaro.type = LightType.Point;
            luzFaro.range = radioCaptura * 1.8f;
            luzFaro.intensity = 2f;
            luzFaro.shadows = LightShadows.None;

            ActualizarVisual();
        }

        private void ActualizarVisual()
        {
            if (renderAnillo == null) return;
            Color color;
            if (EstadoActual == EstadoControlBase.DominadaDestructores) color = PaletaVoidform.Destructor;
            else if (EstadoActual == EstadoControlBase.DominadaCuidadores) color = PaletaVoidform.Cuidador;
            else if (ProgresoCaptura < 0f) color = Color.Lerp(PaletaVoidform.Neutral, PaletaVoidform.Destructor, -ProgresoCaptura / 100f);
            else color = Color.Lerp(PaletaVoidform.Neutral, PaletaVoidform.Cuidador, ProgresoCaptura / 100f);

            float parpadeo = SiendoDisputada ? 0.5f + Mathf.PingPong(Time.time * 4f, 1f) : 1f;
            MaterialesProcedurales.AplicarColor(renderAnillo, color * 0.5f, color * 0.6f * parpadeo);
            MaterialesProcedurales.AplicarColor(renderFaro, color, color * 1.8f * parpadeo);
            if (luzFaro != null)
            {
                luzFaro.color = color;
                luzFaro.intensity = 2f * parpadeo;
            }
        }

        private void OnDrawGizmos()
        {
            Gizmos.color = PaletaVoidform.ColorEstadoBase(EstadoActual);
            Gizmos.DrawWireSphere(transform.position, radioCaptura);
        }
    }
}
