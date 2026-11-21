using UnityEngine;

namespace VoidformBioForge
{
    /// <summary>
    /// Dirección de arte de cada uno de los 5 biomas del exoplaneta.
    /// Nivel 1: nombre y elemento. Nivel 2: paleta, niebla y luz. Nivel 3: peligros del entorno
    /// y su interacción con las facciones (lava, corrientes, esporas, polvo de cuarzo, pulsos del núcleo).
    /// </summary>
    public struct DatosBioma
    {
        public BiomaPlaneta Bioma;
        public string Nombre;
        public string Descripcion;
        public bool TieneElemento;
        public TipoElemento Elemento;

        public Color ColorSuelo;
        public Color ColorSueloDegradado;
        public Color ColorVegetacion;
        public Color ColorRoca;
        public Color ColorAcento;
        public Color ColorCielo;
        public Color ColorNiebla;
        public float DensidadNiebla;
        public Color ColorLuz;
        public float IntensidadLuz;
        public Color ColorAmbiente;

        public string NombrePeligro;
        public string DescripcionPeligro;

        public static DatosBioma Obtener(BiomaPlaneta bioma)
        {
            switch (bioma)
            {
                case BiomaPlaneta.BosqueAncestral:
                    return new DatosBioma
                    {
                        Bioma = bioma,
                        Nombre = "Bosque Ancestral de Sylvaran",
                        Descripcion = "Árboles de cien metros con troncos que respiran luz. El suelo es una red de raíces conectadas a la " +
                                      "fuente de energía: cada tala se siente a kilómetros.",
                        TieneElemento = true,
                        Elemento = TipoElemento.Arbol,
                        ColorSuelo = new Color(0.16f, 0.3f, 0.14f),
                        ColorSueloDegradado = new Color(0.3f, 0.25f, 0.18f),
                        ColorVegetacion = new Color(0.25f, 0.7f, 0.3f),
                        ColorRoca = new Color(0.32f, 0.24f, 0.16f),
                        ColorAcento = new Color(0.65f, 1f, 0.45f),
                        ColorCielo = new Color(0.05f, 0.12f, 0.1f),
                        ColorNiebla = new Color(0.12f, 0.25f, 0.18f),
                        DensidadNiebla = 0.018f,
                        ColorLuz = new Color(0.85f, 1f, 0.75f),
                        IntensidadLuz = 1.05f,
                        ColorAmbiente = new Color(0.22f, 0.35f, 0.25f),
                        NombrePeligro = "Claros de esporas",
                        DescripcionPeligro = "Las esporas curan a los Cuidadores y ralentizan a los Destructores (sus filtros se saturan)."
                    };

                case BiomaPlaneta.HumedalesEspejo:
                    return new DatosBioma
                    {
                        Bioma = bioma,
                        Nombre = "Humedales Espejo de Nerivale",
                        Descripcion = "Lagunas tan quietas que reflejan las dos lunas. Ríos que fluyen en espiral hacia la fuente " +
                                      "transportan su energía como venas de agua.",
                        TieneElemento = true,
                        Elemento = TipoElemento.Agua,
                        ColorSuelo = new Color(0.14f, 0.24f, 0.3f),
                        ColorSueloDegradado = new Color(0.27f, 0.26f, 0.22f),
                        ColorVegetacion = new Color(0.3f, 0.6f, 0.55f),
                        ColorRoca = new Color(0.4f, 0.45f, 0.5f),
                        ColorAcento = new Color(0.45f, 0.8f, 1f),
                        ColorCielo = new Color(0.06f, 0.1f, 0.18f),
                        ColorNiebla = new Color(0.25f, 0.35f, 0.45f),
                        DensidadNiebla = 0.022f,
                        ColorLuz = new Color(0.8f, 0.9f, 1f),
                        IntensidadLuz = 0.95f,
                        ColorAmbiente = new Color(0.25f, 0.32f, 0.42f),
                        NombrePeligro = "Corrientes profundas",
                        DescripcionPeligro = "El agua profunda ralentiza a los Destructores (salvo el Traje Hidrostático) y cura a los Cuidadores."
                    };

                case BiomaPlaneta.CordilleraFracturada:
                    return new DatosBioma
                    {
                        Bioma = bioma,
                        Nombre = "Cordillera Fracturada de Korvhal",
                        Descripcion = "Cañones abiertos por la primera oleada de perforadoras humanas. Las vetas de cuarzo aún laten, " +
                                      "pero las cicatrices de la minería atraviesan la montaña.",
                        TieneElemento = true,
                        Elemento = TipoElemento.Tierra,
                        ColorSuelo = new Color(0.36f, 0.28f, 0.2f),
                        ColorSueloDegradado = new Color(0.3f, 0.29f, 0.28f),
                        ColorVegetacion = new Color(0.55f, 0.5f, 0.3f),
                        ColorRoca = new Color(0.45f, 0.36f, 0.28f),
                        ColorAcento = new Color(1f, 0.75f, 0.4f),
                        ColorCielo = new Color(0.16f, 0.11f, 0.08f),
                        ColorNiebla = new Color(0.42f, 0.32f, 0.22f),
                        DensidadNiebla = 0.014f,
                        ColorLuz = new Color(1f, 0.85f, 0.65f),
                        IntensidadLuz = 1.15f,
                        ColorAmbiente = new Color(0.38f, 0.3f, 0.24f),
                        NombrePeligro = "Polvo de cuarzo",
                        DescripcionPeligro = "Las vetas expuestas dañan a los Destructores (salvo el Traje Sísmico) y fortalecen a los Cuidadores."
                    };

                case BiomaPlaneta.CalderaIgnea:
                    return new DatosBioma
                    {
                        Bioma = bioma,
                        Nombre = "Caldera Ígnea de Ignara",
                        Descripcion = "Un volcán dormido que regula el clima de todo el planeta. La extracción térmica humana lo está " +
                                      "despertando: ríos de lava avanzan hacia los campamentos.",
                        TieneElemento = true,
                        Elemento = TipoElemento.Fuego,
                        ColorSuelo = new Color(0.14f, 0.1f, 0.1f),
                        ColorSueloDegradado = new Color(0.22f, 0.2f, 0.2f),
                        ColorVegetacion = new Color(0.35f, 0.15f, 0.1f),
                        ColorRoca = new Color(0.1f, 0.09f, 0.1f),
                        ColorAcento = new Color(1f, 0.45f, 0.1f),
                        ColorCielo = new Color(0.16f, 0.05f, 0.03f),
                        ColorNiebla = new Color(0.3f, 0.12f, 0.06f),
                        DensidadNiebla = 0.02f,
                        ColorLuz = new Color(1f, 0.6f, 0.4f),
                        IntensidadLuz = 1f,
                        ColorAmbiente = new Color(0.35f, 0.18f, 0.12f),
                        NombrePeligro = "Lagos de lava",
                        DescripcionPeligro = "La lava quema a los Destructores (el Traje Térmico la resiste) y cura a los Cuidadores."
                    };

                default:
                    return new DatosBioma
                    {
                        Bioma = BiomaPlaneta.CorazonDelNucleo,
                        Nombre = "Corazón del Núcleo de Aethernum",
                        Descripcion = "El interior del planeta, donde los cuatro elementos se trenzan alrededor de la fuente original. " +
                                      "Aquí se decide si el viaje de la humanidad fue una salvación o un saqueo.",
                        TieneElemento = false,
                        Elemento = TipoElemento.Arbol,
                        ColorSuelo = new Color(0.12f, 0.1f, 0.2f),
                        ColorSueloDegradado = new Color(0.2f, 0.2f, 0.2f),
                        ColorVegetacion = new Color(0.55f, 0.45f, 1f),
                        ColorRoca = new Color(0.2f, 0.18f, 0.3f),
                        ColorAcento = new Color(0.75f, 0.6f, 1f),
                        ColorCielo = new Color(0.04f, 0.03f, 0.1f),
                        ColorNiebla = new Color(0.15f, 0.1f, 0.3f),
                        DensidadNiebla = 0.016f,
                        ColorLuz = new Color(0.85f, 0.8f, 1f),
                        IntensidadLuz = 0.9f,
                        ColorAmbiente = new Color(0.25f, 0.2f, 0.4f),
                        NombrePeligro = "Pulsos elementales",
                        DescripcionPeligro = "Cuatro cuadrantes, uno por elemento: esporas, corrientes, cuarzo y lava conviven en la misma arena."
                    };
            }
        }
    }
}
