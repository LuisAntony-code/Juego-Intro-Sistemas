using UnityEngine;

namespace VoidformBioForge
{
    /// <summary>
    /// Datos de un arma Destructor, reinterpretando el ciclo clásico de arena shooter
    /// (cadencia, cargador, reserva, recarga, retroceso, dispersión y alcance) con valores
    /// e identidad propios de Voidform: Bio-Forge.
    /// -----------------------------------------------------------------
    /// Nivel 1: daño, alcance y cadencia.
    /// Nivel 2: cargador, reserva y tiempo de recarga.
    /// Nivel 3: dispersión dinámica (moverse o saltar abre el cono), retroceso, caída de daño
    ///          por distancia, perdigones y habilidad especial del arma.
    /// </summary>
    [CreateAssetMenu(fileName = "NuevaArma", menuName = "Voidform/Datos de Arma")]
    public class DatosArma : ScriptableObject
    {
        [Header("1. Identidad y Disparo Básico")]
        public TipoArma tipo = TipoArma.RifleExtractor;
        public string nombreArma = "Rifle Extractor Mk.I";
        [TextArea(2, 4)] public string descripcion = "";
        public float danoPorImpacto = 18f;
        public float alcanceMaximo = 120f;
        public float cadenciaDisparosPorSegundo = 8f;
        public bool esAutomatica = true;

        [Header("2. Munición y Recarga")]
        public int tamanoCargador = 30;
        public int municionReservaMaxima = 120;
        public float tiempoRecarga = 1.4f;

        [Header("3. Balística Avanzada")]
        [Tooltip("Proyectiles por disparo (escopeta > 1).")]
        public int perdigonesPorDisparo = 1;
        [Tooltip("Distancia desde la que el daño empieza a caer.")]
        public float distanciaInicioCaidaDano = 40f;
        [Range(0.1f, 1f)] public float porcentajeDanoMinimo = 0.55f;
        [Tooltip("Dispersión mínima en grados (quieto, en el suelo).")]
        public float dispersionBase = 0.6f;
        public float dispersionMaxima = 5f;
        public float dispersionPorDisparo = 0.45f;
        public float recuperacionDispersionPorSegundo = 6f;
        public float multiplicadorDispersionMovimiento = 1.8f;
        public float multiplicadorDispersionAire = 2.6f;
        [Tooltip("Grados que sube la mirada por disparo.")]
        public float retrocesoVertical = 0.7f;
        public Color colorTrazador = new Color(1f, 0.75f, 0.35f);

        [Header("4. Habilidad Especial del Arma (tecla Q)")]
        public string nombreHabilidad = "Descarga Energética de Extracción";
        [TextArea(2, 4)] public string descripcionHabilidad = "";
        public float enfriamientoHabilidad = 10f;

        public float IntervaloEntreDisparos => 1f / Mathf.Max(0.1f, cadenciaDisparosPorSegundo);

        /// <summary>Daño final aplicando la caída por distancia.</summary>
        public float CalcularDanoPorDistancia(float distancia)
        {
            if (distancia <= distanciaInicioCaidaDano) return danoPorImpacto;
            float t = Mathf.InverseLerp(distanciaInicioCaidaDano, alcanceMaximo, distancia);
            return danoPorImpacto * Mathf.Lerp(1f, porcentajeDanoMinimo, t);
        }

        /// <summary>Catálogo de las 5 armas oficiales.</summary>
        public static DatosArma CrearPorDefecto(TipoArma tipoArma)
        {
            DatosArma datos = CreateInstance<DatosArma>();
            datos.tipo = tipoArma;

            switch (tipoArma)
            {
                case TipoArma.PistolaAuxiliar:
                    datos.nombreArma = "Pistola Auxiliar Vk-9";
                    datos.descripcion = "Arma secundaria semiautomática, precisa y de recarga veloz.";
                    datos.danoPorImpacto = 20f;
                    datos.alcanceMaximo = 80f;
                    datos.cadenciaDisparosPorSegundo = 4.5f;
                    datos.esAutomatica = false;
                    datos.tamanoCargador = 10;
                    datos.municionReservaMaxima = 60;
                    datos.tiempoRecarga = 1.1f;
                    datos.distanciaInicioCaidaDano = 25f;
                    datos.dispersionBase = 0.4f;
                    datos.dispersionMaxima = 3.5f;
                    datos.dispersionPorDisparo = 0.8f;
                    datos.retrocesoVertical = 1.1f;
                    datos.colorTrazador = new Color(1f, 0.9f, 0.6f);
                    datos.nombreHabilidad = "Disparo Sobrecargado";
                    datos.descripcionHabilidad = "El siguiente disparo inflige el triple de daño.";
                    datos.enfriamientoHabilidad = 8f;
                    break;

                case TipoArma.SubfusilRafaga:
                    datos.nombreArma = "Subfusil Ráfaga Hx-3";
                    datos.descripcion = "Cadencia altísima a corta distancia; su daño cae rápido con la distancia.";
                    datos.danoPorImpacto = 13f;
                    datos.alcanceMaximo = 70f;
                    datos.cadenciaDisparosPorSegundo = 12f;
                    datos.esAutomatica = true;
                    datos.tamanoCargador = 32;
                    datos.municionReservaMaxima = 160;
                    datos.tiempoRecarga = 1.6f;
                    datos.distanciaInicioCaidaDano = 18f;
                    datos.porcentajeDanoMinimo = 0.45f;
                    datos.dispersionBase = 1.2f;
                    datos.dispersionMaxima = 6f;
                    datos.dispersionPorDisparo = 0.35f;
                    datos.retrocesoVertical = 0.45f;
                    datos.colorTrazador = new Color(1f, 0.6f, 0.3f);
                    datos.nombreHabilidad = "Ráfaga Supresora";
                    datos.descripcionHabilidad = "Durante 3 s la cadencia se duplica.";
                    datos.enfriamientoHabilidad = 11f;
                    break;

                case TipoArma.RifleExtractor:
                    datos.nombreArma = "Rifle Extractor Mk.I";
                    datos.descripcion = "Rifle de asalto equilibrado. Su núcleo canaliza energía del planeta... a costa del ecosistema.";
                    datos.danoPorImpacto = 18f;
                    datos.alcanceMaximo = 120f;
                    datos.cadenciaDisparosPorSegundo = 8f;
                    datos.esAutomatica = true;
                    datos.tamanoCargador = 30;
                    datos.municionReservaMaxima = 120;
                    datos.tiempoRecarga = 1.4f;
                    datos.distanciaInicioCaidaDano = 40f;
                    datos.dispersionBase = 0.6f;
                    datos.dispersionMaxima = 5f;
                    datos.dispersionPorDisparo = 0.45f;
                    datos.retrocesoVertical = 0.7f;
                    datos.colorTrazador = new Color(1f, 0.75f, 0.35f);
                    datos.nombreHabilidad = "Descarga Energética de Extracción";
                    datos.descripcionHabilidad = "Recarga el cargador y cura 15 de vida extrayendo energía viva del suelo (reduce la integridad del ecosistema).";
                    datos.enfriamientoHabilidad = 10f;
                    break;

                case TipoArma.EscopetaDispersora:
                    datos.nombreArma = "Escopeta Dispersora Kr-12";
                    datos.descripcion = "Ocho perdigones devastadores a quemarropa; inútil a larga distancia.";
                    datos.danoPorImpacto = 11f;
                    datos.alcanceMaximo = 40f;
                    datos.cadenciaDisparosPorSegundo = 1.3f;
                    datos.esAutomatica = false;
                    datos.tamanoCargador = 7;
                    datos.municionReservaMaxima = 35;
                    datos.tiempoRecarga = 2.2f;
                    datos.perdigonesPorDisparo = 8;
                    datos.distanciaInicioCaidaDano = 8f;
                    datos.porcentajeDanoMinimo = 0.25f;
                    datos.dispersionBase = 4.5f;
                    datos.dispersionMaxima = 7f;
                    datos.dispersionPorDisparo = 1.2f;
                    datos.retrocesoVertical = 3f;
                    datos.colorTrazador = new Color(1f, 0.55f, 0.25f);
                    datos.nombreHabilidad = "Cartucho Conmocionador";
                    datos.descripcionHabilidad = "El siguiente disparo empuja y ralentiza a los objetivos alcanzados.";
                    datos.enfriamientoHabilidad = 9f;
                    break;

                case TipoArma.RiflePrecision:
                    datos.nombreArma = "Rifle de Precisión Lanza-7";
                    datos.descripcion = "Un disparo, un impacto decisivo. Muy impreciso si se dispara en movimiento.";
                    datos.danoPorImpacto = 70f;
                    datos.alcanceMaximo = 220f;
                    datos.cadenciaDisparosPorSegundo = 0.8f;
                    datos.esAutomatica = false;
                    datos.tamanoCargador = 5;
                    datos.municionReservaMaxima = 25;
                    datos.tiempoRecarga = 2.4f;
                    datos.distanciaInicioCaidaDano = 150f;
                    datos.porcentajeDanoMinimo = 0.8f;
                    datos.dispersionBase = 0.05f;
                    datos.dispersionMaxima = 6f;
                    datos.dispersionPorDisparo = 3f;
                    datos.recuperacionDispersionPorSegundo = 4f;
                    datos.multiplicadorDispersionMovimiento = 6f;
                    datos.multiplicadorDispersionAire = 10f;
                    datos.retrocesoVertical = 4f;
                    datos.colorTrazador = new Color(0.7f, 0.9f, 1f);
                    datos.nombreHabilidad = "Bala Perforante";
                    datos.descripcionHabilidad = "El siguiente disparo atraviesa objetivos e inflige el doble de daño.";
                    datos.enfriamientoHabilidad = 12f;
                    break;
            }

            datos.name = datos.nombreArma;
            return datos;
        }
    }
}
