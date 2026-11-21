using UnityEngine;

namespace VoidformBioForge
{
    /// <summary>Una habilidad de armadura o de arma: nombre, descripción y enfriamiento.</summary>
    [System.Serializable]
    public struct FichaHabilidad
    {
        public string Nombre;
        [TextArea(2, 4)] public string Descripcion;
        public float Enfriamiento;

        public FichaHabilidad(string nombre, string descripcion, float enfriamiento)
        {
            Nombre = nombre;
            Descripcion = descripcion;
            Enfriamiento = enfriamiento;
        }
    }

    /// <summary>
    /// Arquetipo humano de la expedición: su armadura (vida, blindaje, velocidad, aguante y resistencias)
    /// y sus 3 habilidades de armadura (corta Z, media X, larga C).
    /// -----------------------------------------------------------------
    /// Nivel 1: identidad y aspecto. Nivel 2: estadísticas. Nivel 3: resistencias elementales, inmunidades
    /// y habilidades. Regla: todo humano tiene MENOS vida que cualquier guardián (100–125 frente a 140–220)
    /// y es UN POCO MÁS RÁPIDO (×0,95–1,10 frente a ×0,87–0,92).
    /// La armadura explota al llegar su vida a cero (regla de explosión del traje).
    /// </summary>
    [CreateAssetMenu(fileName = "NuevoArquetipo", menuName = "Voidform/Datos de Arquetipo Humano")]
    public class DatosArquetipo : ScriptableObject
    {
        [Header("1. Identidad")]
        public TipoArquetipo tipo = TipoArquetipo.Cazador;
        public string nombre = "Cazador / Explorador Forestal";
        [TextArea(2, 5)] public string descripcion = "";
        [TextArea(3, 8)] public string descripcionVisual = "";
        public Color colorPrimario = new Color(0.2f, 0.36f, 0.2f);
        public Color colorSecundario = new Color(0.3f, 0.2f, 0.12f);
        public Color colorAcento = new Color(0.8f, 0.82f, 0.85f);

        [Header("2. Estadísticas de la armadura")]
        public float vidaMaxima = 100f;
        public float blindajeMaximo = 40f;
        [Range(0.9f, 1.15f)] public float multiplicadorVelocidad = 1.1f;
        [Tooltip("Aguante (estabilidad): al agotarse, el personaje se tambalea.")]
        public float aguanteMaximo = 40f;

        [Header("3. Resistencias (1 = normal, <1 resiste, >1 vulnerable)")]
        public float resistenciaCinetico = 1f;
        public float resistenciaExplosivo = 1f;
        public float resistenciaArbol = 1f;
        public float resistenciaAgua = 1f;
        public float resistenciaTierra = 1f;
        public float resistenciaFuego = 1f;
        public float resistenciaMetal = 1f;
        public bool inmuneEmpuje = false;
        public bool inmuneRalentizacionAgua = false;
        [Range(0f, 0.9f)] public float reduccionDuracionEstados = 0f;

        [Header("4. Habilidades de armadura (Z corta · X media · C larga)")]
        public FichaHabilidad habilidadCorta;
        public FichaHabilidad habilidadMedia;
        public FichaHabilidad habilidadLarga;

        public FichaHabilidad ObtenerHabilidad(int indice)
        {
            switch (indice)
            {
                case 0: return habilidadCorta;
                case 1: return habilidadMedia;
                default: return habilidadLarga;
            }
        }

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

        public string DescribirResistencias()
        {
            string texto = "";
            Agregar(ref texto, "Madera", resistenciaArbol);
            Agregar(ref texto, "Agua", resistenciaAgua);
            Agregar(ref texto, "Fuego", resistenciaFuego);
            Agregar(ref texto, "Metal", resistenciaMetal);
            Agregar(ref texto, "Tierra", resistenciaTierra);
            Agregar(ref texto, "Explosivo", resistenciaExplosivo);
            if (inmuneEmpuje) texto += "Inmune al empuje. ";
            if (inmuneRalentizacionAgua) texto += "No se ralentiza en el plasma. ";
            if (reduccionDuracionEstados > 0f) texto += $"Estados alterados −{Mathf.RoundToInt(reduccionDuracionEstados * 100f)}%. ";
            return string.IsNullOrEmpty(texto) ? "Sin resistencias especiales." : texto.Trim();
        }

        private static void Agregar(ref string texto, string nombre, float valor)
        {
            if (valor < 0.99f) texto += $"Resiste {nombre} ({Mathf.RoundToInt((1f - valor) * 100f)}%). ";
            else if (valor > 1.01f) texto += $"Débil a {nombre} (+{Mathf.RoundToInt((valor - 1f) * 100f)}%). ";
        }

        /// <summary>Catálogo oficial de los 5 arquetipos (valores del Doc 09).</summary>
        public static DatosArquetipo CrearPorDefecto(TipoArquetipo tipo)
        {
            DatosArquetipo d = CreateInstance<DatosArquetipo>();
            d.tipo = tipo;
            switch (tipo)
            {
                case TipoArquetipo.Cazador:
                    d.nombre = "Cazador / Explorador Forestal";
                    d.descripcion = "Explorador veloz y sigiloso. El más rápido de la expedición; frágil si lo atrapan.";
                    d.descripcionVisual = "Gambesón verde bosque con costuras en rombos y ribetes claros, yelmo cerrado de visor anguloso, hombreras " +
                                          "redondeadas de acero pulido, malla en los brazos, guanteletes y botas altas de cuero marrón oscuro.";
                    d.colorPrimario = new Color(0.17f, 0.32f, 0.18f);
                    d.colorSecundario = new Color(0.28f, 0.18f, 0.1f);
                    d.colorAcento = new Color(0.82f, 0.84f, 0.86f);
                    d.vidaMaxima = 100f; d.blindajeMaximo = 40f; d.multiplicadorVelocidad = 1.10f; d.aguanteMaximo = 40f;
                    d.resistenciaArbol = 0.7f; d.resistenciaFuego = 1.15f;
                    d.reduccionDuracionEstados = 0.25f;
                    d.habilidadCorta = new FichaHabilidad("Paso de maleza", "Esquiva de 5 m con inmunidad a raíces y ralentización; deja una nube de fibras.", 7f);
                    d.habilidadMedia = new FichaHabilidad("Camuflaje de fibras", "6 s casi invisible: los guardianes de la IA no te detectan a más de 12 m.", 16f);
                    d.habilidadLarga = new FichaHabilidad("Estabilizador de mira", "5 s de precisión máxima y +25% de daño a más de 20 m (te mueves al 40%).", 14f);
                    break;

                case TipoArquetipo.Paladin:
                    d.nombre = "Paladín / Templario del Alba";
                    d.descripcion = "Protector de porte noble. Repele, protege al escuadrón y marca el punto de asalto.";
                    d.descripcionVisual = "Placas blancas desgastadas con bordes e incrustaciones doradas, emblema solar en el pecho, hombreras " +
                                          "articuladas multicapa, guanteletes blancos y dorados, capa clara sobre túnica gris.";
                    d.colorPrimario = new Color(0.88f, 0.87f, 0.83f);
                    d.colorSecundario = new Color(0.45f, 0.45f, 0.47f);
                    d.colorAcento = new Color(0.95f, 0.75f, 0.3f);
                    d.vidaMaxima = 115f; d.blindajeMaximo = 70f; d.multiplicadorVelocidad = 0.98f; d.aguanteMaximo = 80f;
                    d.resistenciaFuego = 0.75f; d.resistenciaAgua = 1.1f;
                    d.habilidadCorta = new FichaHabilidad("Aura de repulsión", "Onda dorada de 4,5 m que empuja a los enemigos y te limpia de estados alterados.", 10f);
                    d.habilidadMedia = new FichaHabilidad("Égida refractaria", "Barrera frontal de 4 s que bloquea el daño y devuelve el 30% al atacante.", 15f);
                    d.habilidadLarga = new FichaHabilidad("Faro de vanguardia", "Faro a 30 m durante 8 s: aliados +15% de velocidad y blindaje; revela guardianes.", 22f);
                    break;

                case TipoArquetipo.Bastion:
                    d.nombre = "Caballero Bastión / Guardia de Hierro";
                    d.descripcion = "El humano más resistente. Se ancla, absorbe y protege a los aliados en peligro.";
                    d.descripcionVisual = "Armadura completa de acero oscuro grabado en relieve, hombreras altas y escalonadas, correas de cuero " +
                                          "cruzadas, faldón de tela clara desgastada y grebas robustas. Cabello castaño y barba cerrada.";
                    d.colorPrimario = new Color(0.16f, 0.16f, 0.18f);
                    d.colorSecundario = new Color(0.76f, 0.72f, 0.62f);
                    d.colorAcento = new Color(0.45f, 0.3f, 0.18f);
                    d.vidaMaxima = 125f; d.blindajeMaximo = 90f; d.multiplicadorVelocidad = 0.95f; d.aguanteMaximo = 120f;
                    d.resistenciaTierra = 0.7f; d.resistenciaMetal = 0.7f; d.resistenciaExplosivo = 0.8f; d.resistenciaArbol = 1.15f;
                    d.habilidadCorta = new FichaHabilidad("Anclaje sísmico", "Pisotón que ralentiza en 4 m; 4 s inmune a empuje, raíces y ralentización.", 12f);
                    d.habilidadMedia = new FichaHabilidad("Absorción de impacto", "3 s absorbiendo el 80% del daño frontal; al terminar, descarga el 40% en un cono.", 16f);
                    d.habilidadLarga = new FichaHabilidad("Intercepción de artillería", "Salta junto a un aliado (25 m) y recibe la mitad de su daño durante 4 s.", 20f);
                    break;

                case TipoArquetipo.Tirador:
                    d.nombre = "Tirador / Cazador de Reliquias";
                    d.descripcion = "Especialista a distancia. Se reposiciona con retroceso y revela los puntos débiles.";
                    d.descripcionVisual = "Abrigo largo azul oscuro y marrón con solapas de cuero, hombrera metálica plateada izquierda, bandoleras, " +
                                          "cinturón ancho con hebilla, rodilleras reforzadas y botas altas con correas. Bigote grueso.";
                    d.colorPrimario = new Color(0.12f, 0.16f, 0.3f);
                    d.colorSecundario = new Color(0.35f, 0.22f, 0.13f);
                    d.colorAcento = new Color(0.78f, 0.62f, 0.3f);
                    d.vidaMaxima = 100f; d.blindajeMaximo = 45f; d.multiplicadorVelocidad = 1.03f; d.aguanteMaximo = 60f;
                    d.resistenciaAgua = 0.8f; d.resistenciaMetal = 1.1f;
                    d.inmuneRalentizacionAgua = true;
                    d.habilidadCorta = new FichaHabilidad("Propulsión de retroceso", "Cartucho que te lanza 7 m hacia atrás y daña a quien tengas delante.", 8f);
                    d.habilidadMedia = new FichaHabilidad("Disipador térmico", "5 s sin gastar munición ni sobrecalentar, con +20% de cadencia.", 15f);
                    d.habilidadLarga = new FichaHabilidad("Visor telemétrico", "6 s: revela guardianes a 60 m y tus impactos críticos hacen +25%.", 18f);
                    break;

                default:
                    d.nombre = "Ciborg / Vanguardia Biomecánica";
                    d.descripcion = "Luchador aumentado. Sobrecarga su núcleo, anula el control enemigo y marca objetivos.";
                    d.descripcionVisual = "Lente óptico rojo en la frente y placa craneal plateada, placas plateadas texturizadas en torso y brazos, " +
                                          "hombrera ancha con detalles dorados, segmentos tipo espina en la espalda y núcleo magenta en el pecho.";
                    d.colorPrimario = new Color(0.62f, 0.64f, 0.68f);
                    d.colorSecundario = new Color(0.2f, 0.2f, 0.23f);
                    d.colorAcento = new Color(1f, 0.25f, 0.7f);
                    d.vidaMaxima = 110f; d.blindajeMaximo = 60f; d.multiplicadorVelocidad = 1.0f; d.aguanteMaximo = 80f;
                    d.resistenciaMetal = 0.75f; d.resistenciaAgua = 1.2f;
                    d.habilidadCorta = new FichaHabilidad("Sobrecarga del núcleo", "4 s: +30% de daño cuerpo a cuerpo y velocidad de ataque; pulso final de 3,5 m.", 14f);
                    d.habilidadMedia = new FichaHabilidad("Escudo de frecuencia", "Esfera de 3 m durante 3 s: aliados inmunes al control y −40% de daño elemental.", 16f);
                    d.habilidadLarga = new FichaHabilidad("Enlace de rastreo", "Marca a un guardián (40 m) 8 s: revelado y recibe +10% de daño.", 20f);
                    break;
            }
            d.name = d.nombre;
            return d;
        }

        /// <summary>Arquetipo que mejor resiste a un elemento (los bots de Historia "aprenden").</summary>
        public static TipoArquetipo ArquetipoContrario(TipoElemento elemento)
        {
            switch (elemento)
            {
                case TipoElemento.Arbol: return TipoArquetipo.Cazador;
                case TipoElemento.Agua: return TipoArquetipo.Tirador;
                case TipoElemento.Fuego: return TipoArquetipo.Paladin;
                case TipoElemento.Metal: return TipoArquetipo.Ciborg;
                default: return TipoArquetipo.Bastion;
            }
        }
    }
}
