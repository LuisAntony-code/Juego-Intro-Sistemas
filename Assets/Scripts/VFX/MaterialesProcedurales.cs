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
