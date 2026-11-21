using UnityEngine;

namespace VoidformBioForge
{
    /// <summary>
    /// Describe un golpe completo: cuánto daño, de qué tipo, quién lo causó y desde dónde.
    /// Nivel 1: cantidad. Nivel 2: tipo (para resistencias de traje). Nivel 3: atacante y
    /// origen (para el registro de bajas, el fuego amigo y los casos límite de rondas).
    /// </summary>
    public struct InformacionDano
    {
        public float Cantidad;
        public TipoDano Tipo;
        public GameObject Atacante;
        public bool TieneAtacante;
        public BandoJuego BandoAtacante;
        public string NombreAtacante;
        public Vector3 Punto;
        public string Origen;
        public bool EsExplosionDeTraje;
        /// <summary>Impacto en un punto débil (daño crítico).</summary>
        public bool EsCritico;
        /// <summary>Aguante que resta el golpe (0 = se calcula como el 30% del daño).</summary>
        public float ImpactoAguante;

        public static InformacionDano Crear(float cantidad, TipoDano tipo, SistemaSaludYTraje atacante, string origen)
        {
            InformacionDano info = new InformacionDano
            {
                Cantidad = cantidad,
                Tipo = tipo,
                Origen = origen,
                TieneAtacante = atacante != null
            };

            if (atacante != null)
            {
                info.Atacante = atacante.gameObject;
                info.BandoAtacante = atacante.Bando;
                info.NombreAtacante = atacante.NombreEntidad;
                info.Punto = atacante.transform.position;
            }

            return info;
        }

        /// <summary>Daño del entorno (lava, corrientes, etc.) sin atacante.</summary>
        public static InformacionDano Ambiental(float cantidad, TipoDano tipo, string origen, Vector3 punto)
        {
            return new InformacionDano
            {
                Cantidad = cantidad,
                Tipo = tipo,
                Origen = origen,
                Punto = punto,
                TieneAtacante = false,
                NombreAtacante = "Entorno"
            };
        }
    }
}
