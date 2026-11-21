using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace VoidformBioForge
{
    /// <summary>
    /// Barra de progreso de interfaz (vida, blindaje, maná, ecosistema, captura de base, enfriamientos).
    /// Usa anclajes en lugar de Image.fillAmount para no depender de ningún sprite importado.
    /// </summary>
    public class BarraInterfaz
    {
        public RectTransform Raiz;
        public RectTransform Relleno;
        public Image ImagenRelleno;
        public Text Etiqueta;
        private bool desdeDerecha;

        public BarraInterfaz(RectTransform raiz, RectTransform relleno, Image imagen, Text etiqueta, bool rellenarDesdeDerecha)
        {
            Raiz = raiz;
            Relleno = relleno;
            ImagenRelleno = imagen;
            Etiqueta = etiqueta;
            desdeDerecha = rellenarDesdeDerecha;
        }

        public void Establecer(float valor01, string texto = null)
        {
            float v = Mathf.Clamp01(valor01);
            if (desdeDerecha)
            {
                Relleno.anchorMin = new Vector2(1f - v, 0f);
                Relleno.anchorMax = new Vector2(1f, 1f);
            }
            else
            {
                Relleno.anchorMin = new Vector2(0f, 0f);
                Relleno.anchorMax = new Vector2(v, 1f);
            }
            if (texto != null && Etiqueta != null) Etiqueta.text = texto;
        }

        public void EstablecerColor(Color color)
        {
            if (ImagenRelleno != null) ImagenRelleno.color = color;
        }

        public void EstablecerVisible(bool visible)
        {
            if (Raiz != null) Raiz.gameObject.SetActive(visible);
        }
    }

    /// <summary>
    /// Fábrica de elementos de interfaz (UGUI) creados por código con el estilo visual de Voidform:
    /// paneles translúcidos de esquinas redondeadas, acentos de color por bando y tipografía con sombra.
    /// Nivel 1: lienzo, paneles y textos. Nivel 2: botones con estados de color, barras y campos de texto.
    /// Nivel 3: sprite redondeado de 9 cortes generado por código y fuente integrada compatible con
    ///          cualquier versión de Unity (LegacyRuntime.ttf, Arial.ttf o fuente del sistema).
    /// </summary>
    public static class FabricaInterfaz
    {
        private static Font fuente;
        private static Sprite spriteRedondeado;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void Reiniciar()
        {
            fuente = null;
            spriteRedondeado = null;
        }

        // =====================================================================
        // Nivel 3: recursos generados
        // =====================================================================

        public static Font ObtenerFuente()
        {
            if (fuente != null) return fuente;
            string[] candidatas = { "LegacyRuntime.ttf", "Arial.ttf" };
            foreach (string nombre in candidatas)
            {
                try
                {
                    fuente = Resources.GetBuiltinResource<Font>(nombre);
                }
                catch (System.Exception)
                {
                    fuente = null;
                }
                if (fuente != null) return fuente;
            }
            fuente = Font.CreateDynamicFontFromOSFont(new[] { "Segoe UI", "Arial", "Helvetica", "Liberation Sans" }, 16);
            return fuente;
        }

        public static Sprite ObtenerSpriteRedondeado()
        {
            if (spriteRedondeado != null) return spriteRedondeado;
            const int lado = 64;
            const int radio = 14;
            Texture2D tex = new Texture2D(lado, lado, TextureFormat.RGBA32, false)
            {
                name = "PanelRedondeadoVoidform",
                wrapMode = TextureWrapMode.Clamp,
                filterMode = FilterMode.Bilinear
            };
            for (int y = 0; y < lado; y++)
            {
                for (int x = 0; x < lado; x++)
                {
                    float dx = Mathf.Max(0f, Mathf.Max(radio - x - 0.5f, x + 0.5f - (lado - radio)));
                    float dy = Mathf.Max(0f, Mathf.Max(radio - y - 0.5f, y + 0.5f - (lado - radio)));
                    float d = Mathf.Sqrt(dx * dx + dy * dy);
                    float a = Mathf.Clamp01(radio - d + 0.5f);
                    tex.SetPixel(x, y, new Color(1f, 1f, 1f, a));
                }
            }
            tex.Apply();
            spriteRedondeado = Sprite.Create(tex, new Rect(0, 0, lado, lado), new Vector2(0.5f, 0.5f), 100f, 0,
                SpriteMeshType.FullRect, new Vector4(radio + 2, radio + 2, radio + 2, radio + 2));
            spriteRedondeado.name = "SpriteRedondeadoVoidform";
            return spriteRedondeado;
        }

        // =====================================================================
        // Nivel 1: lienzo, paneles y textos
        // =====================================================================

        public static Canvas CrearLienzo(string nombre, int orden)
        {
            GameObject go = new GameObject(nombre);
            Canvas lienzo = go.AddComponent<Canvas>();
            lienzo.renderMode = RenderMode.ScreenSpaceOverlay;
            lienzo.sortingOrder = orden;
            CanvasScaler escalador = go.AddComponent<CanvasScaler>();
            escalador.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            escalador.referenceResolution = new Vector2(1920f, 1080f);
            escalador.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            escalador.matchWidthOrHeight = 0.5f;
            go.AddComponent<GraphicRaycaster>();
            return lienzo;
        }

        public static void AsegurarSistemaDeEventos()
        {
            if (Object.FindFirstObjectByType<EventSystem>() != null) return;
            GameObject go = new GameObject("EventSystem");
            go.AddComponent<EventSystem>();
            // Requiere el Input Manager clásico ("Both" en Active Input Handling). Con el nuevo
            // Input System, sustituir por InputSystemUIInputModule (ver documentación de montaje).
            go.AddComponent<StandaloneInputModule>();
        }

        public static RectTransform CrearRect(Transform padre, string nombre)
        {
            GameObject go = new GameObject(nombre, typeof(RectTransform));
            go.transform.SetParent(padre, false);
            return (RectTransform)go.transform;
        }

        /// <summary>Fija anclajes (0..1) y márgenes en píxeles de referencia.</summary>
        public static RectTransform Anclar(RectTransform rt, float minX, float minY, float maxX, float maxY,
            float izquierda = 0f, float abajo = 0f, float derecha = 0f, float arriba = 0f)
        {
            rt.anchorMin = new Vector2(minX, minY);
            rt.anchorMax = new Vector2(maxX, maxY);
            rt.offsetMin = new Vector2(izquierda, abajo);
            rt.offsetMax = new Vector2(-derecha, -arriba);
            return rt;
        }

        /// <summary>Coloca un rectángulo de tamaño fijo respecto a un punto de anclaje.</summary>
        public static RectTransform Colocar(RectTransform rt, Vector2 ancla, Vector2 pivote, Vector2 posicion, Vector2 tamano)
        {
            rt.anchorMin = ancla;
            rt.anchorMax = ancla;
            rt.pivot = pivote;
            rt.anchoredPosition = posicion;
            rt.sizeDelta = tamano;
            return rt;
        }

        public static Image CrearPanel(Transform padre, string nombre, Color color, bool redondeado = true)
        {
            RectTransform rt = CrearRect(padre, nombre);
            Image img = rt.gameObject.AddComponent<Image>();
            img.color = color;
            if (redondeado)
            {
                img.sprite = ObtenerSpriteRedondeado();
                img.type = Image.Type.Sliced;
                img.pixelsPerUnitMultiplier = 1f;
            }
            return img;
        }

        public static Text CrearTexto(Transform padre, string nombre, string contenido, int tamano, Color color,
            TextAnchor alineacion = TextAnchor.MiddleLeft, FontStyle estilo = FontStyle.Normal, bool sombra = true)
        {
            RectTransform rt = CrearRect(padre, nombre);
            Text t = rt.gameObject.AddComponent<Text>();
            t.font = ObtenerFuente();
            t.text = contenido;
            t.fontSize = tamano;
            t.color = color;
            t.alignment = alineacion;
            t.fontStyle = estilo;
            t.supportRichText = true;
            t.horizontalOverflow = HorizontalWrapMode.Wrap;
            t.verticalOverflow = VerticalWrapMode.Overflow;
            t.raycastTarget = false;
            t.lineSpacing = 1.05f;
            if (sombra)
            {
                Shadow s = rt.gameObject.AddComponent<Shadow>();
                s.effectColor = new Color(0f, 0f, 0f, 0.65f);
                s.effectDistance = new Vector2(1.5f, -1.5f);
            }
            return t;
        }

        // =====================================================================
        // Nivel 2: botones, barras y campos
        // =====================================================================

        public static Button CrearBoton(Transform padre, string nombre, string etiqueta, UnityAction alPulsar, Color colorBase, int tamanoTexto = 26)
        {
            Image fondo = CrearPanel(padre, nombre, colorBase);
            Button boton = fondo.gameObject.AddComponent<Button>();
            boton.targetGraphic = fondo;
            boton.transition = Selectable.Transition.ColorTint;
            // El tinte multiplica el color del fondo: en reposo se oscurece un poco y al pasar el ratón
            // se muestra el color pleno, lo que da una respuesta visual clara sin sprites adicionales.
            ColorBlock colores = ColorBlock.defaultColorBlock;
            colores.normalColor = new Color(0.82f, 0.82f, 0.82f, 1f);
            colores.highlightedColor = Color.white;
            colores.pressedColor = new Color(0.62f, 0.62f, 0.62f, 1f);
            colores.selectedColor = new Color(0.92f, 0.92f, 0.92f, 1f);
            colores.disabledColor = new Color(0.35f, 0.35f, 0.35f, 0.55f);
            colores.colorMultiplier = 1f;
            colores.fadeDuration = 0.08f;
            boton.colors = colores;
            if (alPulsar != null) boton.onClick.AddListener(alPulsar);

            Text t = CrearTexto(fondo.transform, "Etiqueta", etiqueta, tamanoTexto, PaletaVoidform.TextoPrincipal, TextAnchor.MiddleCenter, FontStyle.Bold);
            Anclar((RectTransform)t.transform, 0f, 0f, 1f, 1f, 14f, 6f, 14f, 6f);
            return boton;
        }

        public static void CambiarTextoBoton(Button boton, string texto)
        {
            Text t = boton.GetComponentInChildren<Text>();
            if (t != null) t.text = texto;
        }

        public static void ResaltarBoton(Button boton, bool resaltado, Color colorResalte, Color colorBase)
        {
            Image img = boton.GetComponent<Image>();
            if (img != null) img.color = resaltado ? colorResalte : colorBase;
            Outline o = boton.GetComponent<Outline>();
            if (o == null) o = boton.gameObject.AddComponent<Outline>();
            o.effectColor = resaltado ? Color.white : new Color(0f, 0f, 0f, 0f);
            o.effectDistance = new Vector2(2.5f, -2.5f);
        }

        public static BarraInterfaz CrearBarra(Transform padre, string nombre, Color colorFondo, Color colorRelleno, int tamanoTexto = 18, bool desdeDerecha = false)
        {
            Image fondo = CrearPanel(padre, nombre, colorFondo);
            RectTransform raiz = (RectTransform)fondo.transform;

            Image relleno = CrearPanel(raiz, "Relleno", colorRelleno);
            RectTransform rtRelleno = (RectTransform)relleno.transform;
            rtRelleno.offsetMin = Vector2.zero;
            rtRelleno.offsetMax = Vector2.zero;

            Text etiqueta = CrearTexto(raiz, "Texto", "", tamanoTexto, PaletaVoidform.TextoPrincipal, TextAnchor.MiddleCenter, FontStyle.Bold);
            Anclar((RectTransform)etiqueta.transform, 0f, 0f, 1f, 1f, 8f, 0f, 8f, 0f);

            BarraInterfaz barra = new BarraInterfaz(raiz, rtRelleno, relleno, etiqueta, desdeDerecha);
            barra.Establecer(1f);
            return barra;
        }

        public static InputField CrearCampoTexto(Transform padre, string nombre, string marcador, int limite)
        {
            Image fondo = CrearPanel(padre, nombre, new Color(0.05f, 0.07f, 0.12f, 0.95f));
            InputField campo = fondo.gameObject.AddComponent<InputField>();
            campo.targetGraphic = fondo;

            Text texto = CrearTexto(fondo.transform, "Texto", "", 24, PaletaVoidform.TextoPrincipal, TextAnchor.MiddleLeft, FontStyle.Normal, false);
            Anclar((RectTransform)texto.transform, 0f, 0f, 1f, 1f, 14f, 4f, 14f, 4f);
            texto.supportRichText = false;

            Text placeholder = CrearTexto(fondo.transform, "Marcador", marcador, 22, PaletaVoidform.TextoSecundario, TextAnchor.MiddleLeft, FontStyle.Italic, false);
            Anclar((RectTransform)placeholder.transform, 0f, 0f, 1f, 1f, 14f, 4f, 14f, 4f);

            campo.textComponent = texto;
            campo.placeholder = placeholder;
            campo.characterLimit = limite;
            campo.lineType = InputField.LineType.SingleLine;
            return campo;
        }

        /// <summary>Contenedor con disposición vertical automática.</summary>
        public static RectTransform CrearColumna(Transform padre, string nombre, float espaciado, int margen)
        {
            RectTransform rt = CrearRect(padre, nombre);
            VerticalLayoutGroup v = rt.gameObject.AddComponent<VerticalLayoutGroup>();
            v.spacing = espaciado;
            v.padding = new RectOffset(margen, margen, margen, margen);
            v.childAlignment = TextAnchor.UpperCenter;
            v.childControlWidth = true;
            v.childControlHeight = true;
            v.childForceExpandWidth = true;
            v.childForceExpandHeight = false;
            return rt;
        }

        /// <summary>Contenedor con disposición horizontal automática.</summary>
        public static RectTransform CrearFila(Transform padre, string nombre, float espaciado, int margen)
        {
            RectTransform rt = CrearRect(padre, nombre);
            HorizontalLayoutGroup h = rt.gameObject.AddComponent<HorizontalLayoutGroup>();
            h.spacing = espaciado;
            h.padding = new RectOffset(margen, margen, margen, margen);
            h.childAlignment = TextAnchor.MiddleCenter;
            h.childControlWidth = true;
            h.childControlHeight = true;
            h.childForceExpandWidth = true;
            h.childForceExpandHeight = true;
            return rt;
        }

        /// <summary>Fija la altura (y opcionalmente la anchura) preferida dentro de un layout.</summary>
        public static T Medidas<T>(T componente, float alto, float ancho = -1f, float flexibleAncho = -1f) where T : Component
        {
            LayoutElement le = componente.GetComponent<LayoutElement>();
            if (le == null) le = componente.gameObject.AddComponent<LayoutElement>();
            le.preferredHeight = alto;
            le.minHeight = alto;
            if (ancho > 0f)
            {
                le.preferredWidth = ancho;
                le.minWidth = ancho;
            }
            if (flexibleAncho >= 0f) le.flexibleWidth = flexibleAncho;
            return componente;
        }
    }
}
