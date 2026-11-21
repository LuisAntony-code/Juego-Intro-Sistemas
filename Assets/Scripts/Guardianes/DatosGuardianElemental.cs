using UnityEngine;

namespace VoidformBioForge
{
    /// <summary>
    /// Ficha de diseño de cada guardián elemental de los Cuidadores, organizada en LINAJES con 3 variantes:
    ///   Vanguardia (corta distancia) · Control (media distancia) · Artillería (larga distancia).
    /// Los guardianes son elementales DESDE SU ORIGEN (no se transforman desde un humano). Su arquetipo de
    /// "tanque elemental de la naturaleza" se inspira en Belerick, con siluetas, runas, nombres y kits propios.
    /// -----------------------------------------------------------------
    /// Nivel 1: identidad (linaje, variante, rol), vida, blindaje y velocidad.
    /// Nivel 2: ataque básico y tres habilidades del kit elemental, con radio y daño modulados por el rol.
    /// Nivel 3: núcleo o punto débil, estabilidad, sobrecarga y sinergia ambiental (para arte, IA y el Archivo).
    ///
    /// REGLA DE ATRIBUTOS (Modo Historia y versus): todo guardián tiene MÁS VIDA que cualquier humano
    /// (mínimo 140 frente a 120 del traje humano más resistente) y una velocidad UN POCO MENOR
    /// (multiplicador de 0,87 a 0,92 frente a 0,95–1,10 de los trajes humanos). Comparten la misma aceleración,
    /// así que no se sienten lentos ni torpes: solo pierden las carreras largas.
    /// </summary>
    [CreateAssetMenu(fileName = "NuevoGuardian", menuName = "Voidform/Datos de Guardián Elemental")]
    public class DatosGuardianElemental : ScriptableObject
    {
        [Header("1. Identidad")]
        public TipoElemento elemento = TipoElemento.Arbol;
        public RolGuardian rol = RolGuardian.Vanguardia;
        public string linaje = "Linaje del Coloso Silvestre";
        public string nombreGuardian = "Coloso Silvestre";
        public string titulo = "Vanguardia del Bosque de los Susurros";
        [TextArea(3, 8)] public string descripcionVisual = "";
        [TextArea(2, 5)] public string personalidad = "";
        public Color colorPrimario = PaletaVoidform.Arbol;
        public Color colorSecundario = new Color(0.35f, 0.22f, 0.12f);
        public bool esDelNexus;

        [Header("2. Estadísticas (siempre más vida y un poco menos de velocidad que los humanos)")]
        public float vidaMaxima = 190f;
        public float blindajeMaximo = 70f;
        [Range(0.8f, 0.93f)] public float multiplicadorVelocidad = 0.88f;
        public float danoBaseHabilidad = 28f;
        public float radioEfecto = 6.5f;

        [Header("3. Ataque Básico (clic izquierdo, sin maná)")]
        public string nombreAtaqueBasico = "Puño de Corteza";
        public float danoAtaqueBasico = 16f;
        public float alcanceAtaqueBasico = 3.2f;
        public float enfriamientoAtaqueBasico = 0.65f;

        [Header("4. Habilidades (E media · R utilidad · Q larga/área)")]
        public string nombreHabilidad1 = "Raíces Atrapantes";
        [TextArea(2, 4)] public string descripcionHabilidad1 = "";
        public string nombreHabilidad2 = "Savia Regeneradora";
        [TextArea(2, 4)] public string descripcionHabilidad2 = "";
        public string nombreDefinitiva = "Espinar del Bosque Primordial";
        [TextArea(2, 4)] public string descripcionDefinitiva = "";

        [Header("5. Combate avanzado (diseño, IA y Archivo del mundo)")]
        [TextArea(2, 4)] public string nucleoPuntoDebil = "";
        [TextArea(2, 4)] public string estabilidad = "";
        [TextArea(2, 4)] public string sobrecarga = "";
        [TextArea(2, 4)] public string sinergia = "";

        public string NombreCompleto => $"{nombreGuardian}, {titulo}";

        // =====================================================================
        // Fábricas
        // =====================================================================

        /// <summary>Compatibilidad: variante de vanguardia del elemento.</summary>
        public static DatosGuardianElemental CrearPorDefecto(TipoElemento elemento)
        {
            return CrearVariante(elemento, RolGuardian.Vanguardia);
        }

        /// <summary>Guardián del linaje del elemento con el rol indicado.</summary>
        public static DatosGuardianElemental CrearVariante(TipoElemento elemento, RolGuardian rol)
        {
            DatosGuardianElemental d = CreateInstance<DatosGuardianElemental>();
            d.elemento = elemento;
            d.rol = rol;
            ConfigurarKitElemental(d, elemento);
            ConfigurarIdentidadDeVariante(d, elemento, rol);
            AplicarModificadoresDeRol(d, rol);
            d.name = d.nombreGuardian;
            return d;
        }

        /// <summary>
        /// Guardián del Nexus Primordial: usa el kit del elemento ACTIVO (rota entre Madera, Agua, Fuego y Metal),
        /// pero conserva su identidad prismática y su rol.
        /// </summary>
        public static DatosGuardianElemental CrearNexus(TipoElemento elementoActivo, RolGuardian rol)
        {
            DatosGuardianElemental d = CrearVariante(elementoActivo, rol);
            d.esDelNexus = true;
            d.linaje = "Custodios del Nexus Primordial";
            switch (rol)
            {
                case RolGuardian.Vanguardia:
                    d.nombreGuardian = "Eco Prismático";
                    break;
                case RolGuardian.Control:
                    d.nombreGuardian = "Aethernum";
                    d.vidaMaxima *= 1.1f;
                    break;
                default:
                    d.nombreGuardian = "Vigía Aural";
                    break;
            }
            d.titulo = $"{UtilidadesTipos.NombreRol(rol)} del Nexus · forma de {UtilidadesTipos.NombreElemento(elementoActivo)}";
            d.descripcionVisual = "Cuerpo de piedra translúcida atravesado por cuatro corrientes: savia esmeralda, plasma añil, magma " +
                                  "naranja y filamentos plateados. Los anillos que orbitan su torso cambian de material según la forma activa.";
            d.personalidad = "La voz del planeta. No odia: recuerda. Su pregunta final es si la humanidad aprendió algo del viaje.";
            d.nucleoPuntoDebil = "Gema-nexo en el esternón: durante 1,5 s tras rotar de elemento queda expuesta y sin blindaje elemental.";
            d.estabilidad = "Alta en su forma de Metal y Madera; baja en la de Agua (los empujes la desplazan más).";
            d.sobrecarga = "Si rota dos veces en menos de 10 s, la gema se sobrecalienta y su siguiente habilidad pierde un 30% de radio.";
            d.sinergia = "Junto al Corazón del Planeta regenera maná un 25% más rápido; las auroras revelan su próxima forma.";
            d.name = d.nombreGuardian;
            return d;
        }

        /// <summary>Compatibilidad con la versión anterior.</summary>
        public static DatosGuardianElemental CrearAethernum(TipoElemento elementoInicial)
        {
            return CrearNexus(elementoInicial, RolGuardian.Control);
        }

        // =====================================================================
        // Nivel 1-2: kit común de cada elemento
        // =====================================================================

        private static void ConfigurarKitElemental(DatosGuardianElemental d, TipoElemento elemento)
        {
            switch (elemento)
            {
                case TipoElemento.Arbol:
                    d.linaje = "Linaje del Coloso Silvestre";
                    d.colorPrimario = new Color(0.25f, 0.95f, 0.65f);
                    d.colorSecundario = new Color(0.33f, 0.22f, 0.14f);
                    d.vidaMaxima = 190f;
                    d.blindajeMaximo = 70f;
                    d.multiplicadorVelocidad = 0.88f;
                    d.danoBaseHabilidad = 28f;
                    d.nombreAtaqueBasico = "Puño de Corteza";
                    d.danoAtaqueBasico = 16f;
                    d.alcanceAtaqueBasico = 3.2f;
                    d.nombreHabilidad1 = "Raíces Atrapantes";
                    d.descripcionHabilidad1 = "Raíces y espinas brotan alrededor: daño en área y Enraizado 1,8 s.";
                    d.nombreHabilidad2 = "Savia Regeneradora";
                    d.descripcionHabilidad2 = "Cura 32 de vida propia y 20 a los aliados cercanos; restaura el ecosistema.";
                    d.nombreDefinitiva = "Espinar del Bosque Primordial";
                    d.descripcionDefinitiva = "Un bosque de espinas en gran área: daño masivo, Enraizado 2,5 s y curación de 40.";
                    d.nucleoPuntoDebil = "Semilla-corazón del pecho, visible entre dos placas de corteza translúcida: los impactos allí ignoran el blindaje de corteza.";
                    d.estabilidad = "Muy alta: sus raíces se anclan al suelo; solo se tambalea con explosiones de traje o impactos cargados.";
                    d.sobrecarga = "Tras la definitiva, la corteza se vuelve opaca 3 s (sin regeneración) mientras la savia se recarga.";
                    d.sinergia = "Cerca del Árbol Madre y de los claros de esporas se cura y su maná se regenera más rápido.";
                    break;

                case TipoElemento.Agua:
                    d.linaje = "Linaje del Leviatán Abisal";
                    d.colorPrimario = new Color(0.2f, 0.45f, 1f);
                    d.colorSecundario = new Color(0.78f, 0.9f, 1f);
                    d.vidaMaxima = 160f;
                    d.blindajeMaximo = 45f;
                    d.multiplicadorVelocidad = 0.92f;
                    d.danoBaseHabilidad = 26f;
                    d.nombreAtaqueBasico = "Latigazo de Corriente";
                    d.danoAtaqueBasico = 12f;
                    d.alcanceAtaqueBasico = 5.5f;
                    d.nombreHabilidad1 = "Pulso Fluvial Restaurador";
                    d.descripcionHabilidad1 = "Se cura 28 y lanza un pulso que daña a los humanos cercanos.";
                    d.nombreHabilidad2 = "Corriente de Marea";
                    d.descripcionHabilidad2 = "Una ola empuja lejos a los enemigos, los daña y los Ralentiza 2,5 s.";
                    d.nombreDefinitiva = "Diluvio de Plasma";
                    d.descripcionDefinitiva = "Marea de plasma en gran área: daño masivo, Ralentización fuerte 3,5 s, empuje y curación de 40.";
                    d.nucleoPuntoDebil = "Vejiga de plasma comprimido en la espalda: brilla al cargar habilidades y recibe daño doble mientras brilla.";
                    d.estabilidad = "Media: su cuerpo líquido absorbe los empujes, pero los impactos sísmicos lo dispersan un instante.";
                    d.sobrecarga = "Si gasta todo el maná, su cuerpo pierde densidad 2 s: se mueve más rápido pero recibe más daño.";
                    d.sinergia = "Junto a la Turbina Abisal y en corrientes profundas se cura; las tormentas de estática amplían su alcance.";
                    break;

                case TipoElemento.Tierra:
                    d.linaje = "Sub-arquetipo Tierra-Forja (nacido en la Caldera)";
                    d.colorPrimario = new Color(0.82f, 0.76f, 0.66f);
                    d.colorSecundario = new Color(0.16f, 0.14f, 0.13f);
                    d.vidaMaxima = 200f;
                    d.blindajeMaximo = 90f;
                    d.multiplicadorVelocidad = 0.87f;
                    d.danoBaseHabilidad = 34f;
                    d.nombreAtaqueBasico = "Puño de Yunque";
                    d.danoAtaqueBasico = 20f;
                    d.alcanceAtaqueBasico = 3f;
                    d.nombreHabilidad1 = "Golpe de Escoria";
                    d.descripcionHabilidad1 = "El antebrazo incandescente golpea el suelo: onda sísmica, Ralentización 1,5 s y elevación.";
                    d.nombreHabilidad2 = "Muralla de Escoria";
                    d.descripcionHabilidad2 = "Levanta un muro de roca de forja, gana 25 de blindaje, se cura 22 y daña alrededor.";
                    d.nombreDefinitiva = "Erupción de Chimenea";
                    d.descripcionDefinitiva = "La chimenea expulsa una columna de humo y llamas: el suelo se parte, lanza y Ralentiza a los enemigos.";
                    d.nucleoPuntoDebil = "Núcleo incandescente del antebrazo derecho: tras cada golpe queda al rojo 1 s y recibe daño ×1,5.";
                    d.estabilidad = "Máxima del juego: placas claras remachadas y centro de gravedad bajo; casi nunca se tambalea.";
                    d.sobrecarga = "Si la chimenea expulsa humo negro (tres habilidades seguidas), se ahoga 2 s y no puede atacar.";
                    d.sinergia = "En la Caldera y junto al Núcleo Térmico su chimenea tira mejor: daño +15% y maná más rápido.";
                    break;

                case TipoElemento.Metal:
                    d.linaje = "Linaje del Androide de Aleación";
                    d.colorPrimario = PaletaVoidform.Metal;
                    d.colorSecundario = new Color(0.22f, 0.24f, 0.3f);
                    d.vidaMaxima = 175f;
                    d.blindajeMaximo = 75f;
                    d.multiplicadorVelocidad = 0.9f;
                    d.danoBaseHabilidad = 32f;
                    d.nombreAtaqueBasico = "Filo de Aleación";
                    d.danoAtaqueBasico = 17f;
                    d.alcanceAtaqueBasico = 3.4f;
                    d.nombreHabilidad1 = "Pulso Magnético";
                    d.descripcionHabilidad1 = "Atrae a los humanos hacia sí, los daña y los Ralentiza 1,5 s.";
                    d.nombreHabilidad2 = "Égida de Aleación";
                    d.descripcionHabilidad2 = "Placas flotantes lo envuelven: +35 de blindaje y descarga eléctrica alrededor.";
                    d.nombreDefinitiva = "Tormenta de Agujas";
                    d.descripcionDefinitiva = "Agujas magnéticas caen en gran área: daño masivo, bloqueo magnético (Enraizado 1,2 s) y Ralentización.";
                    d.nucleoPuntoDebil = "Giroscopio de resonancia en la nuca: si recibe un impacto de precisión, el androide pierde la levitación 1,5 s.";
                    d.estabilidad = "Alta en el suelo; durante la Égida es inamovible, pero los pulsos de agua cortocircuitan sus placas.";
                    d.sobrecarga = "Tras la Tormenta de Agujas su campo magnético se invierte 2 s: deja de levitar y frena un 15%.";
                    d.sinergia = "Cerca del Monolito Central de Resonancia sus placas se recargan; las torres levitantes amplían su pulso.";
                    break;

                default:
                    d.linaje = "Linaje del Titán Ígneo";
                    d.colorPrimario = PaletaVoidform.Fuego;
                    d.colorSecundario = new Color(0.1f, 0.09f, 0.11f);
                    d.vidaMaxima = 165f;
                    d.blindajeMaximo = 40f;
                    d.multiplicadorVelocidad = 0.91f;
                    d.danoBaseHabilidad = 40f;
                    d.nombreAtaqueBasico = "Zarpa Ígnea";
                    d.danoAtaqueBasico = 14f;
                    d.alcanceAtaqueBasico = 3.2f;
                    d.nombreHabilidad1 = "Ráfaga Ígnea Abrasadora";
                    d.descripcionHabilidad1 = "Cono de fuego frontal: daño alto y Quemadura 3 s.";
                    d.nombreHabilidad2 = "Combustión de Área";
                    d.descripcionHabilidad2 = "Explosión térmica alrededor: daño muy alto y Quemadura 3 s.";
                    d.nombreDefinitiva = "Erupción del Núcleo Térmico";
                    d.descripcionDefinitiva = "Erupción en gran área: daño masivo, Quemadura 4 s y empuje.";
                    d.nucleoPuntoDebil = "Grietas de magma del torso: al cargar la definitiva se ensanchan y reciben daño ×1,5.";
                    d.estabilidad = "Media: su caparazón de obsidiana resiste, pero el agua lo enfría y lo hace tambalear.";
                    d.sobrecarga = "Tras la definitiva su magma se enfría 3 s (caparazón gris): menos daño, pero más blindaje.";
                    d.sinergia = "Sobre ríos de magma y junto al Núcleo Térmico se cura y su maná se regenera más rápido.";
                    break;
            }
        }

        // =====================================================================
        // Nivel 2: identidad de cada variante del linaje
        // =====================================================================

        private static void ConfigurarIdentidadDeVariante(DatosGuardianElemental d, TipoElemento elemento, RolGuardian rol)
        {
            switch (elemento)
            {
                case TipoElemento.Arbol:
                    switch (rol)
                    {
                        case RolGuardian.Vanguardia:
                            d.nombreGuardian = "Coloso Silvestre";
                            d.descripcionVisual = "Coloso de casi tres metros con corteza translúcida esmeralda que deja ver la savia latiendo; hombros " +
                                                  "como troncos milenarios con runas ámbar, corona de musgo bioluminiscente y cuernos-rama asimétricos.";
                            d.personalidad = "Paciente y protector. Piensa en siglos y ve a los humanos como semillas extraviadas.";
                            break;
                        case RolGuardian.Control:
                            d.nombreGuardian = "Guardián de Corteza";
                            d.descripcionVisual = "Más esbelto, con láminas de corteza superpuestas como escamas y brazos que se despliegan en raíces " +
                                                  "flexibles; vetas violetas de mineral orgánico recorren sus antebrazos.";
                            d.personalidad = "Metódico. Teje el terreno como una red y deja que el enemigo caiga en ella.";
                            break;
                        default:
                            d.nombreGuardian = "Centinela de las Esporas";
                            d.descripcionVisual = "Cuerpo alargado coronado por un dosel de hojas-panel solar; vainas de esporas turquesa en la espalda " +
                                                  "que dispara como bruma luminosa a larga distancia.";
                            d.personalidad = "Vigilante y distante. Habla con el bosque mediante esporas.";
                            break;
                    }
                    break;

                case TipoElemento.Agua:
                    switch (rol)
                    {
                        case RolGuardian.Vanguardia:
                            d.nombreGuardian = "Leviatán Abisal";
                            d.descripcionVisual = "Torso ancho de plasma denso añil contenido por placas de coral sintético; mandíbula de cristal marino y " +
                                                  "aletas dorsales que chisporrotean de estática azul-blanca.";
                            d.personalidad = "Implacable como la marea alta. No retrocede: empuja.";
                            break;
                        case RolGuardian.Control:
                            d.nombreGuardian = "Señor de las Mareas";
                            d.descripcionVisual = "Figura esbelta de agua translúcida con armadura de nácar; una cascada que cae hacia arriba en lugar de " +
                                                  "cabello y dos lunas diminutas orbitando sus manos.";
                            d.personalidad = "Empático y cambiante. Desvía los conflictos como un río antes de la inundación.";
                            break;
                        default:
                            d.nombreGuardian = "Oráculo de la Estática";
                            d.descripcionVisual = "Silueta flotante sobre una columna de plasma, con un halo de nubes cargadas que descargan arcos " +
                                                  "eléctricos hacia su objetivo.";
                            d.personalidad = "Sereno. Escucha las tormentas y habla con relámpagos.";
                            break;
                    }
                    break;

                case TipoElemento.Tierra:
                    d.nombreGuardian = rol == RolGuardian.Vanguardia ? "Gólem de Chimenea"
                        : rol == RolGuardian.Control ? "Gólem de Chimenea · Fuelle" : "Gólem de Chimenea · Mortero";
                    d.descripcionVisual = "Gólem de placas claras de piedra de forja unidas por remaches oscuros; una chimenea humeante con llamas " +
                                          "naranjas sobresale de su hombro y su antebrazo derecho aloja un núcleo incandescente.";
                    d.personalidad = "Laborioso y terco. Fue la primera forja del planeta y no permitirá que la conviertan en mina.";
                    break;

                case TipoElemento.Metal:
                    switch (rol)
                    {
                        case RolGuardian.Vanguardia:
                            d.nombreGuardian = "Androide de Aleación";
                            d.descripcionVisual = "Humanoide alto de aleación plateada con placas tectónicas que se reajustan al moverse; levita a pocos " +
                                                  "centímetros del suelo y sus antebrazos se despliegan en filos.";
                            d.personalidad = "Preciso y frío, pero leal: protege la ciudad de las agujas como un mecanismo protege su eje.";
                            break;
                        case RolGuardian.Control:
                            d.nombreGuardian = "Centinela de Resonancia";
                            d.descripcionVisual = "Cuerpo de anillos concéntricos que giran alrededor de un núcleo zumbante; placas flotantes forman " +
                                                  "escudos y redes magnéticas.";
                            d.personalidad = "Armónico. Afina el campo magnético del planeta como un instrumento.";
                            break;
                        default:
                            d.nombreGuardian = "Autómata de Precisión";
                            d.descripcionVisual = "Silueta estilizada de articulaciones expuestas, pistones finos y un brazo-riel largo; lente frontal " +
                                                  "que se estrecha al apuntar.";
                            d.personalidad = "Calculador. No falla dos veces.";
                            break;
                    }
                    break;

                default:
                    switch (rol)
                    {
                        case RolGuardian.Vanguardia:
                            d.nombreGuardian = "Titán Ígneo";
                            d.descripcionVisual = "Coloso de obsidiana negra con fractura concoidea y bordes vítreos; un núcleo de magma visible entre " +
                                                  "las placas y puños que gotean roca fundida.";
                            d.personalidad = "Impetuoso. Es el último aviso del planeta.";
                            break;
                        case RolGuardian.Control:
                            d.nombreGuardian = "Demonio de Magma";
                            d.descripcionVisual = "Cuerpo de magma carmesí semilíquido contenido por costras que se rompen y vuelven a formar; melena de " +
                                                  "llamas que pasa de naranja a azul al enfurecerse.";
                            d.personalidad = "Apasionado y volátil, como el Núcleo Térmico.";
                            break;
                        default:
                            d.nombreGuardian = "Heraldo de Ceniza";
                            d.descripcionVisual = "Figura delgada envuelta en un halo de ceniza incandescente y cristales de enfriamiento rápido en los " +
                                                  "hombros; lanza bolas de magma a larga distancia.";
                            d.personalidad = "Ominoso. Donde cae su ceniza, el suelo recuerda que fue volcán.";
                            break;
                    }
                    break;
            }
            d.titulo = UtilidadesTipos.NombreRol(rol);
        }

        // =====================================================================
        // Nivel 3: modificadores por rol (corta / media / larga distancia)
        // =====================================================================

        private static void AplicarModificadoresDeRol(DatosGuardianElemental d, RolGuardian rol)
        {
            switch (rol)
            {
                case RolGuardian.Vanguardia:
                    // Cuerpo a cuerpo: más aguante para sostener la posición y absorber castigo.
                    d.vidaMaxima *= 1.1f;
                    d.blindajeMaximo *= 1.2f;
                    d.radioEfecto *= 0.9f;
                    d.danoBaseHabilidad *= 1.05f;
                    d.danoAtaqueBasico *= 1.15f;
                    break;
                case RolGuardian.Control:
                    d.radioEfecto *= 1.1f;
                    break;
                default:
                    // Larga distancia: menos vida (pero siempre más que un humano), radio y alcance mayores.
                    d.vidaMaxima *= 0.88f;
                    d.radioEfecto *= 1.35f;
                    d.alcanceAtaqueBasico += 3f;
                    d.danoBaseHabilidad *= 0.95f;
                    break;
            }
        }
    }
}
