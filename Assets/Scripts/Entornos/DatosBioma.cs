using UnityEngine;

namespace VoidformBioForge
{
    /// <summary>
    /// Dirección de arte de cada uno de los 6 escenarios del exoplaneta y de su Fuente de Energía.
    /// Nivel 1: nombre y elemento. Nivel 2: paleta, niebla, luz y gravedad. Nivel 3: peligro ambiental y
    /// su interacción con las facciones, y la Fuente de Energía propia del escenario.
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
        public float MultiplicadorGravedad;

        public string NombreFuente;
        public string DescripcionFuente;
        public string NombrePeligro;
        public string DescripcionPeligro;

        public static DatosBioma Obtener(BiomaPlaneta bioma)
        {
            switch (bioma)
            {
                case BiomaPlaneta.BosqueSusurros:
                    return new DatosBioma
                    {
                        Bioma = bioma,
                        Nombre = "El Bosque de los Susurros Bioluminiscentes",
                        Descripcion = "Árboles colosales de corteza translúcida que brillan en verde esmeralda y turquesa; raíces sin enterrar " +
                                      "enroscadas sobre vetas de mineral orgánico que afloran del suelo violeta; hojas anchas que funcionan como " +
                                      "paneles solares naturales, bruma y esporas bioluminiscentes.",
                        TieneElemento = true,
                        Elemento = TipoElemento.Arbol,
                        ColorSuelo = new Color(0.22f, 0.12f, 0.3f),
                        ColorSueloDegradado = new Color(0.3f, 0.26f, 0.22f),
                        ColorVegetacion = new Color(0.15f, 0.85f, 0.6f),
                        ColorRoca = new Color(0.3f, 0.22f, 0.16f),
                        ColorAcento = new Color(0.35f, 1f, 0.8f),
                        ColorCielo = new Color(0.04f, 0.1f, 0.1f),
                        ColorNiebla = new Color(0.12f, 0.24f, 0.22f),
                        DensidadNiebla = 0.02f,
                        ColorLuz = new Color(0.75f, 1f, 0.85f),
                        IntensidadLuz = 0.95f,
                        ColorAmbiente = new Color(0.2f, 0.3f, 0.3f),
                        MultiplicadorGravedad = 1f,
                        NombreFuente = "El Árbol Madre",
                        DescripcionFuente = "Tronco central que late al unísono con una savia de energía pura; sus ramas emiten una red de " +
                                            "filamentos luminosos hacia todo el bosque y una cúpula bioeléctrica protege la zona de las tormentas.",
                        NombrePeligro = "Claros de esporas",
                        DescripcionPeligro = "Las esporas curan a los Cuidadores y ralentizan a los Destructores (sus filtros se saturan)."
                    };

                case BiomaPlaneta.AbismosMareas:
                    return new DatosBioma
                    {
                        Bioma = bioma,
                        Nombre = "Los Abismos de las Mareas de Plasma",
                        Descripcion = "Océano vastísimo de líquido denso y bioluminiscente azul profundo y añil; cielo surcado por nubes cargadas " +
                                      "de estática; estructuras flotantes de coral sintético y cristal marino unidas por puentes colgantes flexibles.",
                        TieneElemento = true,
                        Elemento = TipoElemento.Agua,
                        ColorSuelo = new Color(0.1f, 0.14f, 0.35f),
                        ColorSueloDegradado = new Color(0.25f, 0.25f, 0.24f),
                        ColorVegetacion = new Color(0.4f, 0.7f, 0.95f),
                        ColorRoca = new Color(0.55f, 0.65f, 0.8f),
                        ColorAcento = new Color(0.35f, 0.55f, 1f),
                        ColorCielo = new Color(0.04f, 0.05f, 0.16f),
                        ColorNiebla = new Color(0.16f, 0.2f, 0.42f),
                        DensidadNiebla = 0.022f,
                        ColorLuz = new Color(0.7f, 0.8f, 1f),
                        IntensidadLuz = 0.9f,
                        ColorAmbiente = new Color(0.2f, 0.24f, 0.42f),
                        MultiplicadorGravedad = 1f,
                        NombreFuente = "La Turbina Abisal",
                        DescripcionFuente = "Anillo gigantesco sumergido que aprovecha las corrientes térmicas y las mareas de plasma movidas por " +
                                            "dos lunas; genera energía hidrocinética constante e irradia pulsos hacia otros sectores.",
                        NombrePeligro = "Corrientes de plasma",
                        DescripcionPeligro = "El plasma profundo ralentiza a los Destructores (salvo el Traje Hidrostático) y cura a los Cuidadores."
                    };

                case BiomaPlaneta.CalderaForjas:
                    return new DatosBioma
                    {
                        Bioma = bioma,
                        Nombre = "La Caldera de las Forjas Magmáticas",
                        Descripcion = "Valle desértico de obsidiana negra con gargantas profundas por las que corren ríos de magma naranja y " +
                                      "carmesí; aire denso que brilla por el calor y formaciones cristalinas en las paredes de los cañones.",
                        TieneElemento = true,
                        Elemento = TipoElemento.Fuego,
                        ColorSuelo = new Color(0.08f, 0.07f, 0.08f),
                        ColorSueloDegradado = new Color(0.22f, 0.2f, 0.2f),
                        ColorVegetacion = new Color(0.45f, 0.18f, 0.1f),
                        ColorRoca = new Color(0.05f, 0.05f, 0.06f),
                        ColorAcento = new Color(1f, 0.45f, 0.1f),
                        ColorCielo = new Color(0.16f, 0.05f, 0.03f),
                        ColorNiebla = new Color(0.32f, 0.12f, 0.06f),
                        DensidadNiebla = 0.02f,
                        ColorLuz = new Color(1f, 0.62f, 0.42f),
                        IntensidadLuz = 1f,
                        ColorAmbiente = new Color(0.35f, 0.17f, 0.12f),
                        MultiplicadorGravedad = 1f,
                        NombreFuente = "El Núcleo Térmico",
                        DescripcionFuente = "Super-fumarola artificial sobre un géiser de magma a alta presión; columnas de fuego controlado " +
                                            "impulsan generadores de vapor masivos. Es la fuente más volátil y de mayor potencia calórica del planeta.",
                        NombrePeligro = "Ríos de magma",
                        DescripcionPeligro = "El magma quema a los Destructores (el Traje Térmico lo resiste) y cura a los Cuidadores."
                    };

                case BiomaPlaneta.AgujasAleacion:
                    return new DatosBioma
                    {
                        Bioma = bioma,
                        Nombre = "Las Agujas de Aleación Magnética",
                        Descripcion = "Llanura de placas tectónicas artificiales que encajan con precisión y reflejan el cielo como un espejo " +
                                      "gigante; torres monolíticas y puntiagudas de aleación plateada que levitan a pocos centímetros del suelo.",
                        TieneElemento = true,
                        Elemento = TipoElemento.Metal,
                        ColorSuelo = new Color(0.42f, 0.46f, 0.52f),
                        ColorSueloDegradado = new Color(0.32f, 0.27f, 0.22f),
                        ColorVegetacion = new Color(0.75f, 0.82f, 0.92f),
                        ColorRoca = new Color(0.62f, 0.66f, 0.72f),
                        ColorAcento = new Color(0.6f, 0.85f, 1f),
                        ColorCielo = new Color(0.1f, 0.08f, 0.16f),
                        ColorNiebla = new Color(0.3f, 0.3f, 0.38f),
                        DensidadNiebla = 0.012f,
                        ColorLuz = new Color(0.95f, 0.95f, 1f),
                        IntensidadLuz = 1.15f,
                        ColorAmbiente = new Color(0.32f, 0.33f, 0.4f),
                        MultiplicadorGravedad = 1f,
                        NombreFuente = "El Monolito Central de Resonancia",
                        DescripcionFuente = "Núcleo flotante de metal puro que gira sobre su eje sin tocar nada; usa el campo magnético del " +
                                            "planeta para generar energía cinética y electromagnética y emite un zumbido que estabiliza la ciudad.",
                        NombrePeligro = "Campos de inducción",
                        DescripcionPeligro = "Las placas inductoras dañan y frenan a los Destructores (el Traje Sísmico lo resiste) y recargan a los Cuidadores."
                    };

                case BiomaPlaneta.NexusPrimordial:
                    return new DatosBioma
                    {
                        Bioma = bioma,
                        Nombre = "El Nexus Primordial",
                        Descripcion = "Santuario colosal donde convergen los cuatro biomas: piedra translúcida, canales de plasma, enredaderas de " +
                                      "madera petrificada y filamentos metálicos conductores bajo auroras multicolores y gravedad reducida.",
                        TieneElemento = false,
                        Elemento = TipoElemento.Arbol,
                        ColorSuelo = new Color(0.3f, 0.3f, 0.38f),
                        ColorSueloDegradado = new Color(0.22f, 0.22f, 0.22f),
                        ColorVegetacion = new Color(0.55f, 0.85f, 0.75f),
                        ColorRoca = new Color(0.7f, 0.72f, 0.8f),
                        ColorAcento = new Color(0.78f, 0.62f, 1f),
                        ColorCielo = new Color(0.04f, 0.03f, 0.1f),
                        ColorNiebla = new Color(0.15f, 0.12f, 0.3f),
                        DensidadNiebla = 0.014f,
                        ColorLuz = new Color(0.9f, 0.85f, 1f),
                        IntensidadLuz = 0.9f,
                        ColorAmbiente = new Color(0.27f, 0.23f, 0.42f),
                        MultiplicadorGravedad = 0.6f,
                        NombreFuente = "El Corazón del Planeta",
                        DescripcionFuente = "Esfera masiva de energía pura flotando en el centro del santuario; absorbe la savia del Bosque, el " +
                                            "plasma de los Abismos, el calor de la Caldera y la conductividad de las Agujas y los unifica.",
                        NombrePeligro = "Confluencias elementales",
                        DescripcionPeligro = "Cuatro cuadrantes, uno por elemento: esporas, plasma, magma e inducción conviven en el santuario."
                    };

                default:
                    return new DatosBioma
                    {
                        Bioma = BiomaPlaneta.ConvergenciaTotal,
                        Nombre = "La Convergencia Total",
                        Descripcion = "Los cinco escenarios fusionados: cuadrantes del Bosque, los Abismos, la Caldera y las Agujas alrededor de " +
                                      "un anillo de piedra translúcida del Nexus, con el Corazón del Planeta latiendo en el centro.",
                        TieneElemento = false,
                        Elemento = TipoElemento.Arbol,
                        ColorSuelo = new Color(0.2f, 0.18f, 0.26f),
                        ColorSueloDegradado = new Color(0.2f, 0.2f, 0.2f),
                        ColorVegetacion = new Color(0.5f, 0.8f, 0.8f),
                        ColorRoca = new Color(0.5f, 0.5f, 0.6f),
                        ColorAcento = new Color(1f, 0.85f, 0.55f),
                        ColorCielo = new Color(0.05f, 0.03f, 0.09f),
                        ColorNiebla = new Color(0.18f, 0.12f, 0.24f),
                        DensidadNiebla = 0.016f,
                        ColorLuz = new Color(1f, 0.92f, 0.85f),
                        IntensidadLuz = 0.95f,
                        ColorAmbiente = new Color(0.28f, 0.22f, 0.35f),
                        MultiplicadorGravedad = 0.8f,
                        NombreFuente = "El Corazón del Planeta (Convergencia)",
                        DescripcionFuente = "El Corazón del Planeta, abierto por completo: recibe a la vez las cuatro corrientes y responde a " +
                                            "cada extracción con pulsos visibles en los cinco escenarios.",
                        NombrePeligro = "Todos los peligros",
                        DescripcionPeligro = "Esporas, plasma, magma e inducción en sus cuadrantes y gravedad algo reducida en todo el campo."
                    };
            }
        }
    }
}
