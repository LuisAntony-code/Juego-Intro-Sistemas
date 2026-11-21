using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace VoidformBioForge
{
    /// <summary>
    /// Crea materiales en tiempo de ejecución compatibles con cualquier canal de render
    /// (Built-in, URP o HDRP), para que el prototipo se vea bien sin importar assets.
    /// -----------------------------------------------------------------
    /// Nivel 1: color base sobre el material por defecto de las primitivas.
    /// Nivel 2: emisión (runas, lava, cristales) activando la palabra clave _EMISSION.
    /// Nivel 3: material de partículas aditivo con textura circular suave generada por código
    ///          y caché para no crear materiales repetidos en cada disparo.
    /// IMPORTANTE para compilaciones: si los shaders no aparecen en el build, añádalos en
    /// Project Settings > Graphics > Always Included Shaders (ver Docs/01_Montaje...).
    /// </summary>
    public static class MaterialesProcedurales
    {
        private static Material materialParticulasBase;
        private static Texture2D texturaCirculoSuave;
        private static readonly Dictionary<int, Material> cacheParticulas = new Dictionary<int, Material>();
        private static Shader shaderOpacoCache;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void Reiniciar()
        {
            materialParticulasBase = null;
            texturaCirculoSuave = null;
            cacheParticulas.Clear();
            shaderOpacoCache = null;
            cacheTexturas.Clear();
        }

        /// <summary>Verdadero si el proyecto usa un canal de render programable (URP/HDRP).</summary>
        public static bool UsaCanalProgramable => GraphicsSettings.currentRenderPipeline != null;

        /// <summary>Shader opaco iluminado adecuado al canal de render actual.</summary>
        public static Shader ObtenerShaderOpaco()
        {
            if (shaderOpacoCache != null) return shaderOpacoCache;
            string[] candidatos = UsaCanalProgramable
                ? new[] { "Universal Render Pipeline/Lit", "HDRP/Lit", "Standard" }
                : new[] { "Standard", "Universal Render Pipeline/Lit" };
            foreach (string nombre in candidatos)
            {
                Shader s = Shader.Find(nombre);
                if (s != null)
                {
                    shaderOpacoCache = s;
                    return s;
                }
            }
            return null;
        }

        /// <summary>Material opaco nuevo con color y emisión opcional.</summary>
        public static Material CrearMaterialOpaco(Color color, Color emision)
        {
            Shader shader = ObtenerShaderOpaco();
            Material m = shader != null ? new Material(shader) : new Material(Shader.Find("Hidden/InternalErrorShader"));
            ConfigurarColor(m, color, emision);
            return m;
        }

        /// <summary>Aplica color (y emisión) a un renderizador creando una instancia de material.</summary>
        public static void AplicarColor(Renderer renderizador, Color color, Color emision)
        {
            if (renderizador == null) return;
            Material m = renderizador.material; // Instancia propia del renderizador.
            if (m == null || m.shader == null || m.shader.name.Contains("InternalError"))
            {
                m = CrearMaterialOpaco(color, emision);
                renderizador.material = m;
                return;
            }
            ConfigurarColor(m, color, emision);
        }

        public static void AplicarColor(Renderer renderizador, Color color)
        {
            AplicarColor(renderizador, color, Color.black);
        }

        private static void ConfigurarColor(Material m, Color color, Color emision)
        {
            if (m.HasProperty("_BaseColor")) m.SetColor("_BaseColor", color);
            if (m.HasProperty("_Color")) m.SetColor("_Color", color);
            if (m.HasProperty("_Glossiness")) m.SetFloat("_Glossiness", 0.35f);
            if (m.HasProperty("_Smoothness")) m.SetFloat("_Smoothness", 0.35f);

            bool tieneEmision = emision.maxColorComponent > 0.001f;
            if (m.HasProperty("_EmissionColor"))
            {
                m.SetColor("_EmissionColor", tieneEmision ? emision : Color.black);
                if (tieneEmision)
                {
                    m.EnableKeyword("_EMISSION");
                    m.globalIlluminationFlags = MaterialGlobalIlluminationFlags.RealtimeEmissive;
                }
                else
                {
                    m.DisableKeyword("_EMISSION");
                }
            }
        }

        /// <summary>Textura de círculo con bordes suaves (64×64) para partículas.</summary>
        public static Texture2D ObtenerTexturaCirculoSuave()
        {
            if (texturaCirculoSuave != null) return texturaCirculoSuave;
            const int lado = 64;
            texturaCirculoSuave = new Texture2D(lado, lado, TextureFormat.RGBA32, false)
            {
                name = "CirculoSuaveVoidform",
                wrapMode = TextureWrapMode.Clamp
            };
            Vector2 centro = new Vector2((lado - 1) * 0.5f, (lado - 1) * 0.5f);
            for (int y = 0; y < lado; y++)
            {
                for (int x = 0; x < lado; x++)
                {
                    float d = Vector2.Distance(new Vector2(x, y), centro) / (lado * 0.5f);
                    float a = Mathf.Clamp01(1f - d);
                    a = a * a * (3f - 2f * a); // Suavizado "smoothstep".
                    texturaCirculoSuave.SetPixel(x, y, new Color(1f, 1f, 1f, a));
                }
            }
            texturaCirculoSuave.Apply();
            return texturaCirculoSuave;
        }

        /// <summary>Material de partículas aditivo con textura circular suave.</summary>
        public static Material ObtenerMaterialParticulas()
        {
            if (materialParticulasBase != null) return materialParticulasBase;

            string[] candidatos =
            {
                "Universal Render Pipeline/Particles/Unlit",
                "HDRP/Unlit",
                "Particles/Standard Unlit",
                "Legacy Shaders/Particles/Additive",
                "Sprites/Default"
            };

            Shader shader = null;
            foreach (string nombre in candidatos)
            {
                shader = Shader.Find(nombre);
                if (shader != null) break;
            }

            materialParticulasBase = new Material(shader != null ? shader : Shader.Find("Sprites/Default"))
            {
                name = "ParticulasVoidform"
            };
            Texture2D tex = ObtenerTexturaCirculoSuave();
            if (materialParticulasBase.HasProperty("_BaseMap")) materialParticulasBase.SetTexture("_BaseMap", tex);
            if (materialParticulasBase.HasProperty("_MainTex")) materialParticulasBase.SetTexture("_MainTex", tex);

            // URP Particles/Unlit: superficie transparente con mezcla aditiva.
            if (materialParticulasBase.HasProperty("_Surface"))
            {
                materialParticulasBase.SetFloat("_Surface", 1f);
                materialParticulasBase.SetFloat("_Blend", 2f);
                materialParticulasBase.SetOverrideTag("RenderType", "Transparent");
                materialParticulasBase.SetInt("_SrcBlend", (int)BlendMode.SrcAlpha);
                materialParticulasBase.SetInt("_DstBlend", (int)BlendMode.One);
                materialParticulasBase.SetInt("_ZWrite", 0);
                materialParticulasBase.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
                materialParticulasBase.renderQueue = (int)RenderQueue.Transparent;
            }
            // Built-in Particles/Standard Unlit: modo de mezcla aditivo.
            else if (materialParticulasBase.HasProperty("_Mode"))
            {
                materialParticulasBase.SetFloat("_Mode", 4f);
                materialParticulasBase.SetInt("_SrcBlend", (int)BlendMode.SrcAlpha);
                materialParticulasBase.SetInt("_DstBlend", (int)BlendMode.One);
                materialParticulasBase.SetInt("_ZWrite", 0);
                materialParticulasBase.EnableKeyword("_ALPHABLEND_ON");
                materialParticulasBase.renderQueue = (int)RenderQueue.Transparent;
            }

            return materialParticulasBase;
        }

        // =====================================================================
        // Materiales realistas con texturas PBR (generadas en Blender, en Resources/Texturas)
        // =====================================================================

        private struct PerfilSuperficie
        {
            public float Metalico;
            public float Suavidad;
            public float TamanoMosaico; // metros que cubre una repetición de la textura
        }

        private static readonly Dictionary<string, Texture2D> cacheTexturas = new Dictionary<string, Texture2D>();

        private static PerfilSuperficie Perfil(string superficie)
        {
            switch (superficie)
            {
                case "acero": return new PerfilSuperficie { Metalico = 1f, Suavidad = 0.72f, TamanoMosaico = 0.6f };
                case "hierro": return new PerfilSuperficie { Metalico = 1f, Suavidad = 0.45f, TamanoMosaico = 0.6f };
                case "oro": return new PerfilSuperficie { Metalico = 1f, Suavidad = 0.8f, TamanoMosaico = 0.5f };
                case "laton": return new PerfilSuperficie { Metalico = 1f, Suavidad = 0.7f, TamanoMosaico = 0.5f };
                case "metal_placas": return new PerfilSuperficie { Metalico = 1f, Suavidad = 0.85f, TamanoMosaico = 4f };
                case "obsidiana": return new PerfilSuperficie { Metalico = 0f, Suavidad = 0.93f, TamanoMosaico = 3f };
                case "cristal": return new PerfilSuperficie { Metalico = 0f, Suavidad = 0.95f, TamanoMosaico = 1f };
                case "piedra": return new PerfilSuperficie { Metalico = 0f, Suavidad = 0.4f, TamanoMosaico = 3f };
                case "coral": return new PerfilSuperficie { Metalico = 0f, Suavidad = 0.35f, TamanoMosaico = 2f };
                case "corteza": return new PerfilSuperficie { Metalico = 0f, Suavidad = 0.15f, TamanoMosaico = 2.5f };
                case "madera": return new PerfilSuperficie { Metalico = 0f, Suavidad = 0.35f, TamanoMosaico = 0.6f };
                case "cuero": return new PerfilSuperficie { Metalico = 0f, Suavidad = 0.3f, TamanoMosaico = 0.5f };
                case "tela": return new PerfilSuperficie { Metalico = 0f, Suavidad = 0.08f, TamanoMosaico = 0.5f };
                case "roca": return new PerfilSuperficie { Metalico = 0f, Suavidad = 0.2f, TamanoMosaico = 4f };
                default:
                    if (superficie != null && superficie.StartsWith("suelo")) return new PerfilSuperficie { Metalico = 0f, Suavidad = 0.15f, TamanoMosaico = 6f };
                    return new PerfilSuperficie { Metalico = 0f, Suavidad = 0.35f, TamanoMosaico = 2f };
            }
        }

        /// <summary>Carga una textura de Resources/Texturas (null si no existe; se guarda en caché).</summary>
        public static Texture2D CargarTextura(string nombre)
        {
            if (cacheTexturas.TryGetValue(nombre, out Texture2D t)) return t;
            t = Resources.Load<Texture2D>($"Texturas/{nombre}");
            cacheTexturas[nombre] = t;
            return t;
        }

        /// <summary>
        /// Aplica un material realista: color base, metalicidad y suavidad por superficie y, si existen, las
        /// texturas PBR horneadas en Blender (albedo de detalle en escala de grises + mapa de normales), con el
        /// mosaico ajustado al tamaño real del objeto para que no se estiren.
        /// </summary>
        public static void AplicarMaterialRealista(Renderer r, Color color, Color emision, string superficie = null)
        {
            if (r == null) return;
            AplicarColor(r, color, emision);
            if (string.IsNullOrEmpty(superficie)) superficie = InferirSuperficie(r.name);
            if (string.IsNullOrEmpty(superficie)) return;

            Material m = r.material;
            PerfilSuperficie perfil = Perfil(superficie);
            if (m.HasProperty("_Metallic")) m.SetFloat("_Metallic", perfil.Metalico);
            if (m.HasProperty("_Glossiness")) m.SetFloat("_Glossiness", perfil.Suavidad);
            if (m.HasProperty("_Smoothness")) m.SetFloat("_Smoothness", perfil.Suavidad);

            Texture2D albedo = CargarTextura($"{superficie}_albedo");
            Texture2D normal = CargarTextura($"{superficie}_normal");
            if (albedo == null && normal == null) return;

            Vector3 tamano = r.bounds.size;
            float lado = Mathf.Max(0.05f, Mathf.Max(tamano.x, Mathf.Max(tamano.y, tamano.z)));
            Vector2 mosaico = Vector2.one * Mathf.Max(1f, lado / perfil.TamanoMosaico);

            if (albedo != null)
            {
                if (m.HasProperty("_BaseMap")) { m.SetTexture("_BaseMap", albedo); m.SetTextureScale("_BaseMap", mosaico); }
                if (m.HasProperty("_MainTex")) { m.SetTexture("_MainTex", albedo); m.SetTextureScale("_MainTex", mosaico); }
            }
            if (normal != null && m.HasProperty("_BumpMap"))
            {
                m.SetTexture("_BumpMap", normal);
                m.SetTextureScale("_BumpMap", mosaico);
                if (m.HasProperty("_BumpScale")) m.SetFloat("_BumpScale", 1f);
                m.EnableKeyword("_NORMALMAP");
            }
        }

        /// <summary>
        /// Deduce la superficie a partir del nombre del objeto o del material (los modelos de Blender usan
        /// materiales llamados "VBF_corteza", "VBF_acero", "VBF_emisivo_verde"...).
        /// </summary>
        public static string InferirSuperficie(string nombre)
        {
            if (string.IsNullOrEmpty(nombre)) return null;
            string n = nombre.ToLowerInvariant();
            string[] claves =
            {
                "metal_placas", "obsidiana", "cristal", "corteza", "coral", "madera", "cuero", "tela", "acero", "hierro", "laton", "oro",
                "piedra", "roca", "suelo_bosque", "suelo_abismos", "suelo_caldera", "suelo_agujas", "suelo_nexus"
            };
            foreach (string c in claves) if (n.Contains(c)) return c;
            if (n.Contains("tronco") || n.Contains("raiz") || n.Contains("raíz")) return "corteza";
            if (n.Contains("aguja de obsidiana")) return "obsidiana";
            if (n.Contains("torre") || n.Contains("placa") || n.Contains("monolito")) return "metal_placas";
            if (n.Contains("muro") || n.Contains("pilar") || n.Contains("pedestal") || n.Contains("columna") || n.Contains("fumarola")) return "piedra";
            if (n.Contains("gambes") || n.Contains("capa") || n.Contains("faldon") || n.Contains("abrigo")) return "tela";
            if (n.Contains("bota") || n.Contains("correa") || n.Contains("bandolera") || n.Contains("guante") || n.Contains("carcaj")) return "cuero";
            if (n.Contains("hombrera") || n.Contains("peto") || n.Contains("greba") || n.Contains("rodillera")) return "acero";
            return null;
        }

        /// <summary>Aplica los materiales realistas a todas las piezas de un modelo importado de Blender.</summary>
        public static void AplicarMaterialesPorNombre(GameObject raiz)
        {
            foreach (Renderer r in raiz.GetComponentsInChildren<Renderer>(true))
            {
                Material original = r.sharedMaterial;
                string nombreMaterial = original != null ? original.name : r.name;
                Color color = original != null && original.HasProperty("_Color") ? original.color
                    : original != null && original.HasProperty("_BaseColor") ? original.GetColor("_BaseColor") : Color.gray;
                string n = nombreMaterial.ToLowerInvariant();
                if (n.Contains("emisivo"))
                {
                    AplicarColor(r, color, color * 2.5f);
                    continue;
                }
                AplicarMaterialRealista(r, color, Color.black, InferirSuperficie(nombreMaterial) ?? InferirSuperficie(r.name));
            }
        }

        /// <summary>Material simple sin iluminación para líneas (trazadores, anillos).</summary>
        public static Material ObtenerMaterialLinea(Color color)
        {
            int clave = color.GetHashCode();
            if (cacheParticulas.TryGetValue(clave, out Material existente) && existente != null) return existente;
            Material m = new Material(ObtenerMaterialParticulas());
            if (m.HasProperty("_BaseColor")) m.SetColor("_BaseColor", color);
            if (m.HasProperty("_Color")) m.SetColor("_Color", color);
            if (m.HasProperty("_TintColor")) m.SetColor("_TintColor", color);
            cacheParticulas[clave] = m;
            return m;
        }
    }
}
