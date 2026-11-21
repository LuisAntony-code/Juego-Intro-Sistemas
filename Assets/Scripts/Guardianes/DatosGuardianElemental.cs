using UnityEngine;

namespace VoidformBioForge
{
    /// <summary>
    /// Ficha de diseño de cada guardián elemental de los Cuidadores.
    /// Son personajes elementales DESDE SU ORIGEN (no se transforman desde un humano).
    /// Toman como arquetipo la contundencia elemental y el vínculo con el entorno de un
    /// guardián tanque de la naturaleza (como Belerick), pero su silueta, proporciones, runas,
    /// nombres y habilidades son propios de Voidform: Bio-Forge.
    /// -----------------------------------------------------------------
    /// Nivel 1: identidad, vida, blindaje y velocidad.
    /// Nivel 2: ataque básico y tres habilidades con costo de maná y enfriamiento.
    /// Nivel 3: descripción visual completa para arte/modelado y colores para VFX.
    /// </summary>
    [CreateAssetMenu(fileName = "NuevoGuardian", menuName = "Voidform/Datos de Guardián Elemental")]
    public class DatosGuardianElemental : ScriptableObject
    {
        [Header("1. Identidad")]
        public TipoElemento elemento = TipoElemento.Arbol;
        public string nombreGuardian = "Sylvaran";
        public string titulo = "Custodio del Bosque Ancestral";
        [TextArea(3, 8)] public string descripcionVisual = "";
        [TextArea(2, 5)] public string personalidad = "";
        public Color colorPrimario = PaletaVoidform.Arbol;
        public Color colorSecundario = new Color(0.35f, 0.22f, 0.12f);

        [Header("2. Estadísticas")]
        public float vidaMaxima = 190f;
        public float blindajeMaximo = 70f;
        [Range(0.6f, 1.3f)] public float multiplicadorVelocidad = 0.9f;
        public float danoBaseHabilidad = 28f;
        public float radioEfecto = 6.5f;

        [Header("3. Ataque Básico (clic izquierdo, sin maná)")]
        public string nombreAtaqueBasico = "Puño de Corteza";
        public float danoAtaqueBasico = 16f;
        public float alcanceAtaqueBasico = 3.2f;
        public float enfriamientoAtaqueBasico = 0.65f;

        [Header("4. Habilidades (E / R / Q)")]
        public string nombreHabilidad1 = "Raíces Atrapantes";
        [TextArea(2, 4)] public string descripcionHabilidad1 = "";
        public string nombreHabilidad2 = "Savia Regeneradora";
        [TextArea(2, 4)] public string descripcionHabilidad2 = "";
        public string nombreDefinitiva = "Espinar del Bosque Primordial";
        [TextArea(2, 4)] public string descripcionDefinitiva = "";

        public string NombreCompleto => $"{nombreGuardian}, {titulo}";

        public static DatosGuardianElemental CrearPorDefecto(TipoElemento elemento)
        {
            DatosGuardianElemental d = CreateInstance<DatosGuardianElemental>();
            d.elemento = elemento;

            switch (elemento)
            {
                case TipoElemento.Arbol:
                    d.nombreGuardian = "Sylvaran";
                    d.titulo = "Custodio del Bosque Ancestral";
                    d.descripcionVisual = "Coloso de corteza viva de casi tres metros. Hombros anchos como troncos milenarios con runas ámbar " +
                                          "talladas que laten al ritmo de la savia; corona de musgo bioluminiscente y ramas que forman cuernos asimétricos. " +
                                          "Sus manos son nudos de raíz y en su pecho brilla una semilla-corazón verde. Al caminar deja brotes que se marchitan " +
                                          "si el ecosistema está degradado.";
                    d.personalidad = "Paciente y protector. Habla poco, piensa en siglos. Ve a los humanos como semillas extraviadas, no como enemigos.";
                    d.colorPrimario = PaletaVoidform.Arbol;
                    d.colorSecundario = new Color(0.36f, 0.24f, 0.13f);
                    d.vidaMaxima = 190f;
                    d.blindajeMaximo = 70f;
                    d.multiplicadorVelocidad = 0.9f;
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
                    break;

                case TipoElemento.Agua:
                    d.nombreGuardian = "Nerivale";
                    d.titulo = "Custodia de las Mareas";
                    d.descripcionVisual = "Figura esbelta de agua translúcida contenida por una armadura de nácar y coral azul. Su silueta ondula " +
                                          "como una corriente; en lugar de cabello tiene una cascada que cae hacia arriba. Peces de luz orbitan su torso " +
                                          "y sus pasos dejan anillos en el aire como si caminara sobre un lago.";
                    d.personalidad = "Empática y cambiante. Cree que todo conflicto puede desviarse como un río antes de convertirse en inundación.";
                    d.colorPrimario = PaletaVoidform.Agua;
                    d.colorSecundario = new Color(0.82f, 0.92f, 1f);
                    d.vidaMaxima = 145f;
                    d.blindajeMaximo = 40f;
                    d.multiplicadorVelocidad = 1.05f;
                    d.danoBaseHabilidad = 24f;
                    d.nombreAtaqueBasico = "Latigazo de Corriente";
                    d.danoAtaqueBasico = 12f;
                    d.alcanceAtaqueBasico = 5.5f;
                    d.nombreHabilidad1 = "Pulso Fluvial Restaurador";
                    d.descripcionHabilidad1 = "Se cura 28 y lanza un pulso que daña a los humanos cercanos.";
                    d.nombreHabilidad2 = "Corriente de Marea";
                    d.descripcionHabilidad2 = "Una ola empuja lejos a los enemigos, los daña y los Ralentiza 2,5 s.";
                    d.nombreDefinitiva = "Diluvio del Humedal";
                    d.descripcionDefinitiva = "Inundación en gran área: daño masivo, Ralentización fuerte 3,5 s, empuje y curación de 40.";
                    break;

                case TipoElemento.Tierra:
                    d.nombreGuardian = "Korvhal";
                    d.titulo = "Coloso de la Cordillera";
                    d.descripcionVisual = "Gigante de placas de basalto unidas por vetas de cuarzo que brillan con luz ocre. Espalda cubierta de " +
                                          "estratos como un acantilado, brazos desproporcionados que terminan en mazas de roca y un rostro sin boca con dos " +
                                          "geodas por ojos. Cada paso levanta polvo y pequeñas piedras flotantes.";
                    d.personalidad = "Firme y testarudo. Recuerda cada montaña que la humanidad vació en su planeta de origen.";
                    d.colorPrimario = PaletaVoidform.Tierra;
                    d.colorSecundario = new Color(0.3f, 0.28f, 0.27f);
                    d.vidaMaxima = 185f;
                    d.blindajeMaximo = 80f;
                    d.multiplicadorVelocidad = 0.85f;
                    d.danoBaseHabilidad = 36f;
                    d.nombreAtaqueBasico = "Mazazo de Basalto";
                    d.danoAtaqueBasico = 20f;
                    d.alcanceAtaqueBasico = 3f;
                    d.nombreHabilidad1 = "Golpe Sísmico de Roca";
                    d.descripcionHabilidad1 = "Onda sísmica: daño elevado, Ralentización 1,5 s y elevación de enemigos.";
                    d.nombreHabilidad2 = "Muralla y Temblor";
                    d.descripcionHabilidad2 = "Levanta un muro de roca, gana 25 de blindaje, se cura 22 y daña alrededor.";
                    d.nombreDefinitiva = "Fractura Tectónica";
                    d.descripcionDefinitiva = "El suelo se parte en gran área: daño masivo, lanza a los enemigos y los Ralentiza 2 s.";
                    break;

                default:
                    d.nombreGuardian = "Ignara";
                    d.titulo = "Centinela de la Caldera";
                    d.descripcionVisual = "Cuerpo de obsidiana agrietada por donde asoma un núcleo de magma vivo. Melena de llamas que cambia de " +
                                          "naranja a azul cuando se enfurece; garras de vidrio volcánico y un halo de ceniza incandescente. El aire " +
                                          "a su alrededor ondula por el calor.";
                    d.personalidad = "Impetuosa y apasionada. Es el último aviso del planeta: donde Ignara aparece, la paciencia ya se agotó.";
                    d.colorPrimario = PaletaVoidform.Fuego;
                    d.colorSecundario = new Color(0.12f, 0.1f, 0.12f);
                    d.vidaMaxima = 140f;
                    d.blindajeMaximo = 35f;
                    d.multiplicadorVelocidad = 1f;
                    d.danoBaseHabilidad = 45f;
                    d.nombreAtaqueBasico = "Zarpa Ígnea";
                    d.danoAtaqueBasico = 14f;
                    d.alcanceAtaqueBasico = 3.2f;
                    d.nombreHabilidad1 = "Ráfaga Ígnea Abrasadora";
                    d.descripcionHabilidad1 = "Cono de fuego frontal: daño alto y Quemadura 3 s.";
                    d.nombreHabilidad2 = "Combustión de Área";
                    d.descripcionHabilidad2 = "Explosión térmica alrededor: daño muy alto y Quemadura 3 s.";
                    d.nombreDefinitiva = "Erupción del Corazón Volcánico";
                    d.descripcionDefinitiva = "Erupción en gran área: daño masivo, Quemadura 4 s y empuje.";
                    break;
            }

            d.name = d.nombreGuardian;
            return d;
        }

        /// <summary>
        /// Guardián del clímax (nivel 5): Aethernum, el Corazón Vivo del Planeta.
        /// Usa los datos del elemento activo, pero su nombre y título cambian.
        /// </summary>
        public static DatosGuardianElemental CrearAethernum(TipoElemento elementoInicial)
        {
            DatosGuardianElemental d = CrearPorDefecto(elementoInicial);
            d.nombreGuardian = "Aethernum";
            d.titulo = $"Corazón Vivo del Planeta (forma de {UtilidadesTipos.NombreElemento(elementoInicial)})";
            d.descripcionVisual = "Entidad prismática formada por los cuatro elementos entrelazados: corteza, agua, basalto y magma giran " +
                                  "en anillos alrededor de un núcleo de energía viva. Puede adoptar la forma de cualquiera de los cuatro guardianes.";
            d.personalidad = "La voz del planeta entero. No odia; recuerda. Su pregunta final es si la humanidad aprendió algo del viaje.";
            d.vidaMaxima *= 1.15f;
            d.name = "Aethernum";
            return d;
        }
    }
}
