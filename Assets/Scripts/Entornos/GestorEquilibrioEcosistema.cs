using System;
using UnityEngine;

namespace VoidformBioForge
{
    /// <summary>
    /// Mide la salud del ecosistema del exoplaneta durante una partida: la PROBLEMÁTICA SOCIAL
    /// (extractivismo vs. preservación) convertida en una mecánica visible para ambos bandos.
    /// -----------------------------------------------------------------
    /// Nivel 1: integridad del ecosistema de 0 a 100 y energía extraída de 0 a 100.
    /// Nivel 2: la extracción (Fuente de Energía, Descarga del Rifle Extractor, bases dominadas por
    ///          Destructores) resta integridad; la restauración (habilidades curativas, bases de los
    ///          Cuidadores) y la regeneración natural la devuelven; las explosiones de traje contaminan.
    /// Nivel 3: umbrales narrativos (Floreciente, Estable, Herido, Crítico, Colapsado), aviso único
    ///          de colapso y registro del resultado para el desenlace del Modo Historia.
    /// </summary>
    public class GestorEquilibrioEcosistema : MonoBehaviour
    {
        public static GestorEquilibrioEcosistema Instancia { get; private set; }

        [Header("1. Valores Iniciales")]
        [SerializeField] private float integridadInicial = 100f;

        [Header("2. Ritmos de Cambio")]
        [Tooltip("Integridad perdida por cada unidad de energía extraída.")]
        [SerializeField] private float costoIntegridadPorEnergia = 0.6f;
        [SerializeField] private float regeneracionNaturalPorSegundo = 0.12f;
        [SerializeField] private float degradacionPorBaseDestructoraPorSegundo = 0.06f;
        [SerializeField] private float restauracionPorBaseCuidadoraPorSegundo = 0.04f;

        [Header("3. Umbrales")]
        [SerializeField] private float umbralColapso = 15f;

        private bool colapsoNotificado;
        private float ultimaIntegridadNotificada = -1f;
        private float ultimaEnergiaNotificada = -1f;

        public float Integridad { get; private set; }
        public float EnergiaExtraida { get; private set; }
        public bool EcosistemaColapsado => Integridad <= umbralColapso;

        public event Action<float> AlCambiarIntegridad;
        public event Action<float> AlCambiarEnergiaExtraida;
        public event Action AlColapsarEcosistema;

        private void Awake()
        {
            Instancia = this;
            Integridad = integridadInicial;
            EnergiaExtraida = 0f;
        }

        private void OnDestroy()
        {
            if (Instancia == this) Instancia = null;
        }

        private void Update()
        {
            float cambio = regeneracionNaturalPorSegundo;

            foreach (ZonaCapturaBase baseZona in ZonaCapturaBase.BasesActivas)
            {
                if (baseZona == null) continue;
                if (baseZona.EstadoActual == EstadoControlBase.DominadaDestructores) cambio -= degradacionPorBaseDestructoraPorSegundo;
                else if (baseZona.EstadoActual == EstadoControlBase.DominadaCuidadores) cambio += restauracionPorBaseCuidadoraPorSegundo;
            }

            ModificarIntegridad(cambio * Time.deltaTime);
            NotificarSiCambio();
        }

        // ================== API pública ==================

        public void RegistrarExtraccion(float cantidadEnergia, string origen)
        {
            if (cantidadEnergia <= 0f) return;
            EnergiaExtraida = Mathf.Clamp(EnergiaExtraida + cantidadEnergia, 0f, 100f);
            ModificarIntegridad(-cantidadEnergia * costoIntegridadPorEnergia);
            NotificarSiCambio();
        }

        public void RegistrarRestauracion(float cantidad, string origen)
        {
            if (cantidad <= 0f) return;
            ModificarIntegridad(cantidad);
            NotificarSiCambio();
        }

        public void RegistrarContaminacion(float cantidad, string origen)
        {
            if (cantidad <= 0f) return;
            ModificarIntegridad(-cantidad);
            NotificarSiCambio();
        }

        public string ObtenerEstadoTexto()
        {
            if (Integridad >= 85f) return "Floreciente";
            if (Integridad >= 60f) return "Estable";
            if (Integridad >= 35f) return "Herido";
            if (Integridad > umbralColapso) return "Crítico";
            return "Colapsado";
        }

        public void ForzarNotificacionCompleta()
        {
            AlCambiarIntegridad?.Invoke(Integridad);
            AlCambiarEnergiaExtraida?.Invoke(EnergiaExtraida);
        }

        // ================== Lógica interna ==================

        private void ModificarIntegridad(float delta)
        {
            Integridad = Mathf.Clamp(Integridad + delta, 0f, 100f);

            if (!colapsoNotificado && EcosistemaColapsado)
            {
                colapsoNotificado = true;
                BusEventosJuego.PublicarMensaje("¡El ecosistema ha colapsado! La fuente de energía viva se apaga.", TipoMensajeSistema.Critico);
                AlColapsarEcosistema?.Invoke();
            }
        }

        private void NotificarSiCambio()
        {
            // Los extremos (0% de integridad y 100% de energía) se notifican SIEMPRE, aunque el último
            // salto sea menor que el umbral, porque de ellos dependen victorias y desenlaces.
            bool extremoIntegridad = Integridad <= 0f && ultimaIntegridadNotificada > 0f;
            bool extremoEnergia = EnergiaExtraida >= 100f && ultimaEnergiaNotificada < 100f;
            if (Mathf.Abs(Integridad - ultimaIntegridadNotificada) >= 0.1f || extremoIntegridad)
            {
                ultimaIntegridadNotificada = Integridad;
                AlCambiarIntegridad?.Invoke(Integridad);
            }
            if (Mathf.Abs(EnergiaExtraida - ultimaEnergiaNotificada) >= 0.1f || extremoEnergia)
            {
                ultimaEnergiaNotificada = EnergiaExtraida;
                AlCambiarEnergiaExtraida?.Invoke(EnergiaExtraida);
            }
        }
    }
}
