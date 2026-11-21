using UnityEngine;

namespace VoidformBioForge
{
    /// <summary>
    /// Datos de un exotraje Destructor. Puede crearse como asset desde
    /// Assets > Create > Voidform > Datos de Traje, o generarse en tiempo de ejecución con
    /// CrearPorDefecto(), de modo que el juego funciona sin ningún asset creado a mano.
    /// -----------------------------------------------------------------
    /// Nivel 1: vida, blindaje y velocidad.
    /// Nivel 2: habilidad activa con enfriamiento.
    /// Nivel 3: tabla de resistencias por tipo de daño (contramedidas frente a cada elemento),
    ///          inmunidad al empuje y reducción de estados alterados.
    /// </summary>
    [CreateAssetMenu(fileName = "NuevoTraje", menuName = "Voidform/Datos de Traje")]
    public class DatosTraje : ScriptableObject
    {
        [Header("1. Identidad y Estadísticas Base")]
        public TipoTraje tipo = TipoTraje.CineticoAsalto;
        public string nombreTraje = "Traje Cinético de Asalto";
        [TextArea(2, 5)] public string descripcion = "";
        public float vidaMaxima = 100f;
        public float blindajeMaximo = 50f;
        [Range(0.6f, 1.4f)] public float multiplicadorVelocidad = 1f;
        public Color colorTraje = new Color(0.85f, 0.45f, 0.2f);

        [Header("2. Habilidad Activa del Traje (tecla E)")]
        public string nombreHabilidad = "Impulso Táctico";
        [TextArea(2, 4)] public string descripcionHabilidad = "";
        public float enfriamientoHabilidad = 6f;

        [Header("3. Resistencias (1 = normal, <1 resiste, >1 vulnerable)")]
        public float resistenciaCinetico = 1f;
        public float resistenciaExplosivo = 1f;
        public float resistenciaArbol = 1f;
        public float resistenciaAgua = 1f;
        public float resistenciaTierra = 1f;
        public float resistenciaFuego = 1f;
        public float resistenciaMetal = 1f;
        [Tooltip("Si es verdadero, el traje no puede ser empujado por corrientes u ondas.")]
        public bool inmuneEmpuje = false;
        [Tooltip("Porcentaje en que se reduce la duración de los estados alterados recibidos.")]
        [Range(0f, 0.9f)] public float reduccionDuracionEstados = 0f;

        public float ObtenerMultiplicador(TipoDano tipoDano)
        {
            switch (tipoDano)
            {
                case TipoDano.Cinetico: return resistenciaCinetico;
                case TipoDano.Explosivo: return resistenciaExplosivo;
                case TipoDano.Arbol: return resistenciaArbol;
                case TipoDano.Agua: return resistenciaAgua;
                case TipoDano.Tierra: return resistenciaTierra;
                case TipoDano.Metal: return resistenciaMetal;
                default: return resistenciaFuego;
            }
        }

        /// <summary>Texto corto de fortalezas y debilidades para la interfaz.</summary>
        public string DescribirResistencias()
        {
            string texto = "";
            AgregarLinea(ref texto, "Cinético", resistenciaCinetico);
            AgregarLinea(ref texto, "Explosivo", resistenciaExplosivo);
            AgregarLinea(ref texto, "Madera", resistenciaArbol);
            AgregarLinea(ref texto, "Agua", resistenciaAgua);
            AgregarLinea(ref texto, "Tierra", resistenciaTierra);
            AgregarLinea(ref texto, "Fuego", resistenciaFuego);
            AgregarLinea(ref texto, "Metal", resistenciaMetal);
            if (inmuneEmpuje) texto += "Inmune al empuje. ";
            if (reduccionDuracionEstados > 0f) texto += $"Estados alterados -{Mathf.RoundToInt(reduccionDuracionEstados * 100f)}%. ";
            return string.IsNullOrEmpty(texto) ? "Sin resistencias especiales (todoterreno)." : texto.Trim();
        }

        private static void AgregarLinea(ref string texto, string nombre, float valor)
        {
            if (valor < 0.99f) texto += $"Resiste {nombre} ({Mathf.RoundToInt((1f - valor) * 100f)}%). ";
            else if (valor > 1.01f) texto += $"Débil a {nombre} (+{Mathf.RoundToInt((valor - 1f) * 100f)}%). ";
        }

        /// <summary>
        /// Catálogo de los 5 trajes oficiales con su ciclo de contramedidas:
        /// Térmico resiste Fuego y teme al Agua; Hidrostático resiste Agua y teme a la Tierra;
        /// Sísmico resiste Tierra y teme al Árbol; Podador resiste Árbol y teme al Fuego;
        /// Cinético no tiene resistencias pero es el más veloz.
        /// </summary>
        public static DatosTraje CrearPorDefecto(TipoTraje tipoTraje)
        {
            DatosTraje datos = CreateInstance<DatosTraje>();
            datos.tipo = tipoTraje;

            switch (tipoTraje)
            {
                case TipoTraje.CineticoAsalto:
                    datos.nombreTraje = "Traje Cinético de Asalto";
                    datos.descripcion = "Exotraje ligero de la vanguardia extractora. Máxima movilidad, sin protección elemental: ideal para capturar bases rápido.";
                    datos.vidaMaxima = 100f;
                    datos.blindajeMaximo = 40f;
                    datos.multiplicadorVelocidad = 1.1f;
                    datos.colorTraje = new Color(0.95f, 0.5f, 0.2f);
                    datos.nombreHabilidad = "Impulso Táctico";
                    datos.descripcionHabilidad = "Propulsores dorsales: +80% de velocidad durante 0,45 s.";
                    datos.enfriamientoHabilidad = 6f;
                    break;

                case TipoTraje.TermicoIgnifugo:
                    datos.nombreTraje = "Traje Térmico Ignífugo";
                    datos.descripcion = "Capas cerámicas refrigeradas para operar en la Caldera Ígnea. El agua provoca choque térmico en sus placas.";
                    datos.vidaMaxima = 105f;
                    datos.blindajeMaximo = 55f;
                    datos.multiplicadorVelocidad = 0.98f;
                    datos.colorTraje = new Color(0.85f, 0.25f, 0.15f);
                    datos.resistenciaFuego = 0.55f;
                    datos.resistenciaAgua = 1.25f;
                    datos.nombreHabilidad = "Purga Térmica";
                    datos.descripcionHabilidad = "Elimina quemaduras y concede inmunidad a quemaduras durante 3 s.";
                    datos.enfriamientoHabilidad = 9f;
                    break;

                case TipoTraje.HidrostaticoAnclado:
                    datos.nombreTraje = "Traje Hidrostático Anclado";
                    datos.descripcion = "Lastre magnético y sellos estancos para los Humedales Espejo. Las vibraciones sísmicas rompen sus sellos.";
                    datos.vidaMaxima = 105f;
                    datos.blindajeMaximo = 50f;
                    datos.multiplicadorVelocidad = 0.97f;
                    datos.colorTraje = new Color(0.2f, 0.55f, 0.85f);
                    datos.resistenciaAgua = 0.55f;
                    datos.resistenciaTierra = 1.25f;
                    datos.inmuneEmpuje = true;
                    datos.nombreHabilidad = "Ancla Hidráulica";
                    datos.descripcionHabilidad = "Inmunidad a ralentización durante 3 s y +20 de blindaje.";
                    datos.enfriamientoHabilidad = 10f;
                    break;

                case TipoTraje.SismicoBlindado:
                    datos.nombreTraje = "Traje Sísmico Blindado";
                    datos.descripcion = "Placas reactivas pesadas para la Cordillera Fracturada. Las raíces se cuelan entre sus juntas.";
                    datos.vidaMaxima = 120f;
                    datos.blindajeMaximo = 80f;
                    // 0,95: los humanos SIEMPRE son un poco más rápidos que cualquier guardián (máx. 0,92).
                    datos.multiplicadorVelocidad = 0.95f;
                    datos.colorTraje = new Color(0.55f, 0.5f, 0.45f);
                    datos.resistenciaTierra = 0.55f;
                    datos.resistenciaMetal = 0.6f;
                    datos.resistenciaExplosivo = 0.8f;
                    datos.resistenciaArbol = 1.25f;
                    datos.nombreHabilidad = "Placas Reactivas";
                    datos.descripcionHabilidad = "Restaura 35 puntos de blindaje al instante.";
                    datos.enfriamientoHabilidad = 11f;
                    break;

                case TipoTraje.PodadorBiotecnico:
                    datos.nombreTraje = "Traje Podador Biotécnico";
                    datos.descripcion = "Cuchillas térmicas y biocombustible para abrirse paso en el Bosque Ancestral. Su combustible lo hace inflamable.";
                    datos.vidaMaxima = 100f;
                    datos.blindajeMaximo = 50f;
                    datos.multiplicadorVelocidad = 1.02f;
                    datos.colorTraje = new Color(0.6f, 0.65f, 0.2f);
                    datos.resistenciaArbol = 0.55f;
                    datos.resistenciaFuego = 1.25f;
                    datos.reduccionDuracionEstados = 0.35f;
                    datos.nombreHabilidad = "Sierra de Poda";
                    datos.descripcionHabilidad = "Corta raíces (quita Enraizado) y daña a los guardianes cercanos; +50% contra el Guardián de Árbol.";
                    datos.enfriamientoHabilidad = 8f;
                    break;
            }

            datos.name = datos.nombreTraje;
            return datos;
        }

        /// <summary>
        /// Traje recomendado para enfrentar a un guardián de cierto elemento
        /// (los bots del Modo Historia lo usan para "adaptarse" a cada bioma).
        /// </summary>
        public static TipoTraje TrajeContrario(TipoElemento elemento)
        {
            switch (elemento)
            {
                case TipoElemento.Arbol: return TipoTraje.PodadorBiotecnico;
                case TipoElemento.Agua: return TipoTraje.HidrostaticoAnclado;
                case TipoElemento.Tierra: return TipoTraje.SismicoBlindado;
                case TipoElemento.Metal: return TipoTraje.SismicoBlindado;
                default: return TipoTraje.TermicoIgnifugo;
            }
        }
    }
}
