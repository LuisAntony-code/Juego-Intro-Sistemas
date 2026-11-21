using UnityEngine;

namespace VoidformBioForge
{
    /// <summary>
    /// Fábrica de efectos visuales (VFX) generados por código con ParticleSystem, LineRenderer y luces.
    /// Permite que el juego tenga explosiones de traje, habilidades elementales, trazadores y auras
    /// sin necesidad de prefabs. Si el equipo de arte crea prefabs propios, pueden asignarse en
    /// SistemaSaludYTraje.efectoVisualExplosionTraje o sustituir estos métodos.
    /// -----------------------------------------------------------------
    /// Nivel 1: impactos y destellos de disparo.
    /// Nivel 2: trazadores, anillos de onda y efectos por elemento (Árbol, Agua, Tierra, Fuego).
    /// Nivel 3: explosión de traje en capas (núcleo, humo, escombros, onda, luz y sacudida de
    ///          cámara) y auras elementales persistentes que siguen al guardián.
    /// </summary>
    public static class FabricaEfectosVisuales
    {
        /// <summary>Permite desactivar todos los VFX (por ejemplo, en pruebas automáticas).</summary>
        public static bool EfectosActivos { get; set; } = true;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void Reiniciar()
        {
            EfectosActivos = true;
        }

        // =====================================================================
        // Nivel 1: base de sistemas de partículas
        // =====================================================================

        private struct ParametrosParticulas
        {
            public string Nombre;
            public Color ColorInicio;
            public Color ColorFin;
            public float VidaMin;
            public float VidaMax;
            public float VelocidadMin;
            public float VelocidadMax;
            public float TamanoMin;
            public float TamanoMax;
            public int Rafaga;
            public float TasaContinua;
            public float RadioForma;
            public float Gravedad;
            public bool Bucle;
            public bool HaciaArriba;
            public bool EspacioLocal;
        }

        private static ParticleSystem CrearSistema(ParametrosParticulas p, Vector3 posicion, Transform padre)
        {
            GameObject go = new GameObject(p.Nombre);
            if (padre != null)
            {
                go.transform.SetParent(padre, false);
                go.transform.localPosition = posicion;
            }
            else
            {
                go.transform.position = posicion;
            }

            ParticleSystem ps = go.AddComponent<ParticleSystem>();
            ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

            ParticleSystem.MainModule principal = ps.main;
            principal.duration = p.Bucle ? 2f : 0.5f;
            principal.loop = p.Bucle;
            principal.startLifetime = new ParticleSystem.MinMaxCurve(p.VidaMin, p.VidaMax);
            principal.startSpeed = new ParticleSystem.MinMaxCurve(p.VelocidadMin, p.VelocidadMax);
            principal.startSize = new ParticleSystem.MinMaxCurve(p.TamanoMin, p.TamanoMax);
            principal.startColor = p.ColorInicio;
            principal.gravityModifier = p.Gravedad;
            principal.simulationSpace = p.EspacioLocal ? ParticleSystemSimulationSpace.Local : ParticleSystemSimulationSpace.World;
            principal.maxParticles = Mathf.Max(64, p.Rafaga * 2 + Mathf.CeilToInt(p.TasaContinua * 3f));
            principal.playOnAwake = false;

            ParticleSystem.EmissionModule emision = ps.emission;
            emision.rateOverTime = p.TasaContinua;
            if (p.Rafaga > 0)
            {
                emision.SetBursts(new[] { new ParticleSystem.Burst(0f, (short)p.Rafaga) });
            }

            ParticleSystem.ShapeModule forma = ps.shape;
            forma.enabled = true;
            if (p.HaciaArriba)
            {
                forma.shapeType = ParticleSystemShapeType.Cone;
                forma.angle = 25f;
                forma.radius = Mathf.Max(0.05f, p.RadioForma);
                forma.rotation = new Vector3(-90f, 0f, 0f);
            }
            else
            {
                forma.shapeType = ParticleSystemShapeType.Sphere;
                forma.radius = Mathf.Max(0.05f, p.RadioForma);
            }

            ParticleSystem.ColorOverLifetimeModule colorVida = ps.colorOverLifetime;
            colorVida.enabled = true;
            Gradient gradiente = new Gradient();
            gradiente.SetKeys(
                new[] { new GradientColorKey(p.ColorInicio, 0f), new GradientColorKey(p.ColorFin, 1f) },
                new[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(0.8f, 0.4f), new GradientAlphaKey(0f, 1f) });
            colorVida.color = new ParticleSystem.MinMaxGradient(gradiente);

            ParticleSystem.SizeOverLifetimeModule tamanoVida = ps.sizeOverLifetime;
            tamanoVida.enabled = true;
            tamanoVida.size = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.EaseInOut(0f, 1f, 1f, 0.2f));

            ParticleSystemRenderer render = go.GetComponent<ParticleSystemRenderer>();
            render.sharedMaterial = MaterialesProcedurales.ObtenerMaterialParticulas();
            render.renderMode = ParticleSystemRenderMode.Billboard;

            ps.Play();

            if (!p.Bucle)
            {
                AutodestruirTrasTiempo.Programar(go, p.VidaMax + 0.6f);
            }
            return ps;
        }

        /// <summary>Chispas pequeñas donde impacta una bala.</summary>
        public static void CrearImpacto(Vector3 punto, Vector3 normal, Color color)
        {
            if (!EfectosActivos) return;
            CrearSistema(new ParametrosParticulas
            {
                Nombre = "VFX Impacto",
                ColorInicio = color,
                ColorFin = new Color(color.r, color.g * 0.5f, color.b * 0.3f, 0f),
                VidaMin = 0.12f, VidaMax = 0.3f,
                VelocidadMin = 2f, VelocidadMax = 6f,
                TamanoMin = 0.05f, TamanoMax = 0.14f,
                Rafaga = 8,
                RadioForma = 0.05f,
                Gravedad = 1f
            }, punto + normal * 0.03f, null);
        }

        /// <summary>Destello en la boca del arma con un pulso de luz brevísimo.</summary>
        public static void CrearDestelloDisparo(Vector3 punto, Vector3 direccion, Color color)
        {
            if (!EfectosActivos) return;
            CrearSistema(new ParametrosParticulas
            {
                Nombre = "VFX Destello",
                ColorInicio = Color.Lerp(color, Color.white, 0.5f),
                ColorFin = color,
                VidaMin = 0.04f, VidaMax = 0.08f,
                VelocidadMin = 0.5f, VelocidadMax = 2f,
                TamanoMin = 0.15f, TamanoMax = 0.3f,
                Rafaga = 4,
                RadioForma = 0.02f
            }, punto, null);
            LuzEfimera.Crear(punto, color, 2.5f, 4f, 0.05f);
        }

        // =====================================================================
        // Nivel 2: trazadores, ondas y efectos elementales
        // =====================================================================

        public static void CrearTrazador(Vector3 origen, Vector3 destino, Color color)
        {
            if (!EfectosActivos) return;
            GameObject go = new GameObject("VFX Trazador");
            LineRenderer linea = go.AddComponent<LineRenderer>();
            linea.positionCount = 2;
            linea.SetPosition(0, origen);
            linea.SetPosition(1, destino);
            linea.startWidth = 0.035f;
            linea.endWidth = 0.015f;
            linea.useWorldSpace = true;
            linea.sharedMaterial = MaterialesProcedurales.ObtenerMaterialLinea(color);
            linea.startColor = color;
            linea.endColor = new Color(color.r, color.g, color.b, 0.2f);
            linea.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            linea.receiveShadows = false;
            go.AddComponent<LineaDesvanecida>().Configurar(0.07f);
        }

        public static void CrearAnilloOnda(Vector3 centro, float radio, Color color)
        {
            if (!EfectosActivos) return;
            GameObject go = new GameObject("VFX Anillo de Onda");
            go.transform.position = centro;
            LineRenderer linea = go.AddComponent<LineRenderer>();
            linea.loop = true;
            linea.useWorldSpace = false;
            linea.startWidth = 0.18f;
            linea.endWidth = 0.18f;
            linea.sharedMaterial = MaterialesProcedurales.ObtenerMaterialLinea(color);
            linea.startColor = color;
            linea.endColor = color;
            linea.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            go.AddComponent<AnilloOndaExpansivo>().Configurar(radio, 0.4f, color);
        }

        public static void CrearEfectoElemental(TipoElemento elemento, Vector3 centro, float radio)
        {
            if (!EfectosActivos) return;
            Color c = PaletaVoidform.ColorElemento(elemento);
            float escala = Mathf.Clamp(radio, 0.5f, 12f);
            int cantidad = Mathf.Clamp(Mathf.RoundToInt(12 * escala), 10, 140);

            switch (elemento)
            {
                case TipoElemento.Arbol:
                    // Esporas luminosas que ascienden + astillas de raíz.
                    CrearSistema(new ParametrosParticulas
                    {
                        Nombre = "VFX Árbol Esporas", ColorInicio = new Color(0.6f, 1f, 0.45f), ColorFin = new Color(0.9f, 1f, 0.5f, 0f),
                        VidaMin = 0.8f, VidaMax = 1.6f, VelocidadMin = 1f, VelocidadMax = 3f, TamanoMin = 0.1f, TamanoMax = 0.25f,
                        Rafaga = cantidad, RadioForma = escala * 0.8f, Gravedad = -0.15f, HaciaArriba = true
                    }, centro, null);
                    CrearSistema(new ParametrosParticulas
                    {
                        Nombre = "VFX Árbol Raíces", ColorInicio = new Color(0.45f, 0.3f, 0.15f), ColorFin = new Color(0.3f, 0.2f, 0.1f, 0f),
                        VidaMin = 0.4f, VidaMax = 0.8f, VelocidadMin = 3f, VelocidadMax = 7f, TamanoMin = 0.15f, TamanoMax = 0.35f,
                        Rafaga = cantidad / 2, RadioForma = escala * 0.6f, Gravedad = 1.5f, HaciaArriba = true
                    }, centro, null);
                    break;

                case TipoElemento.Agua:
                    // Salpicadura radial con gravedad + neblina.
                    CrearSistema(new ParametrosParticulas
                    {
                        Nombre = "VFX Agua Salpicadura", ColorInicio = new Color(0.55f, 0.85f, 1f), ColorFin = new Color(0.2f, 0.5f, 1f, 0f),
                        VidaMin = 0.5f, VidaMax = 1f, VelocidadMin = 4f, VelocidadMax = 9f, TamanoMin = 0.12f, TamanoMax = 0.3f,
                        Rafaga = cantidad, RadioForma = 0.4f, Gravedad = 1.8f
                    }, centro + Vector3.up * 0.5f, null);
                    CrearSistema(new ParametrosParticulas
                    {
                        Nombre = "VFX Agua Neblina", ColorInicio = new Color(0.7f, 0.9f, 1f, 0.6f), ColorFin = new Color(0.7f, 0.9f, 1f, 0f),
                        VidaMin = 1f, VidaMax = 1.8f, VelocidadMin = 0.3f, VelocidadMax = 1f, TamanoMin = 0.8f, TamanoMax = 1.6f,
                        Rafaga = cantidad / 3, RadioForma = escala * 0.7f
                    }, centro + Vector3.up * 0.3f, null);
                    break;

                case TipoElemento.Tierra:
                    // Rocas que saltan con gravedad + polvo ocre.
                    CrearSistema(new ParametrosParticulas
                    {
                        Nombre = "VFX Tierra Rocas", ColorInicio = new Color(0.5f, 0.4f, 0.3f), ColorFin = new Color(0.35f, 0.28f, 0.2f, 0f),
                        VidaMin = 0.6f, VidaMax = 1.1f, VelocidadMin = 5f, VelocidadMax = 10f, TamanoMin = 0.2f, TamanoMax = 0.5f,
                        Rafaga = cantidad / 2, RadioForma = escala * 0.5f, Gravedad = 2.2f, HaciaArriba = true
                    }, centro, null);
                    CrearSistema(new ParametrosParticulas
                    {
                        Nombre = "VFX Tierra Polvo", ColorInicio = new Color(0.8f, 0.65f, 0.45f, 0.7f), ColorFin = new Color(0.6f, 0.5f, 0.35f, 0f),
                        VidaMin = 1f, VidaMax = 2f, VelocidadMin = 1f, VelocidadMax = 3f, TamanoMin = 0.8f, TamanoMax = 1.8f,
                        Rafaga = cantidad / 2, RadioForma = escala * 0.8f, Gravedad = -0.05f
                    }, centro + Vector3.up * 0.3f, null);
                    break;

                default:
                    // Llamarada + brasas ascendentes.
                    CrearSistema(new ParametrosParticulas
                    {
                        Nombre = "VFX Fuego Llamarada", ColorInicio = new Color(1f, 0.85f, 0.35f), ColorFin = new Color(1f, 0.2f, 0.05f, 0f),
                        VidaMin = 0.35f, VidaMax = 0.7f, VelocidadMin = 4f, VelocidadMax = 9f, TamanoMin = 0.4f, TamanoMax = 0.9f,
                        Rafaga = cantidad, RadioForma = escala * 0.5f, Gravedad = -0.4f
                    }, centro + Vector3.up * 0.6f, null);
                    CrearSistema(new ParametrosParticulas
                    {
                        Nombre = "VFX Fuego Brasas", ColorInicio = new Color(1f, 0.6f, 0.15f), ColorFin = new Color(0.6f, 0.1f, 0.02f, 0f),
                        VidaMin = 1f, VidaMax = 2f, VelocidadMin = 1f, VelocidadMax = 4f, TamanoMin = 0.05f, TamanoMax = 0.14f,
                        Rafaga = cantidad, RadioForma = escala * 0.7f, Gravedad = -0.3f, HaciaArriba = true
                    }, centro, null);
                    LuzEfimera.Crear(centro + Vector3.up, c, 4f, escala * 2.5f, 0.35f);
                    break;
            }

            if (radio >= 2f) CrearAnilloOnda(centro + Vector3.up * 0.15f, radio, c);
        }

        // =====================================================================
        // Nivel 3: explosión de traje y auras persistentes
        // =====================================================================

        /// <summary>Explosión del exotraje humano al llegar su vida a cero.</summary>
        public static void CrearExplosionTraje(Vector3 centro, float radio)
        {
            if (!EfectosActivos) return;

            // 1) Núcleo incandescente.
            CrearSistema(new ParametrosParticulas
            {
                Nombre = "VFX Explosión Núcleo", ColorInicio = new Color(1f, 0.95f, 0.7f), ColorFin = new Color(1f, 0.35f, 0.05f, 0f),
                VidaMin = 0.25f, VidaMax = 0.55f, VelocidadMin = 6f, VelocidadMax = 16f, TamanoMin = 0.6f, TamanoMax = 1.4f,
                Rafaga = 70, RadioForma = 0.3f
            }, centro, null);

            // 2) Humo denso y oscuro (contaminación).
            CrearSistema(new ParametrosParticulas
            {
                Nombre = "VFX Explosión Humo", ColorInicio = new Color(0.25f, 0.22f, 0.2f, 0.8f), ColorFin = new Color(0.1f, 0.1f, 0.1f, 0f),
                VidaMin = 1.5f, VidaMax = 2.8f, VelocidadMin = 1f, VelocidadMax = 3.5f, TamanoMin = 1.5f, TamanoMax = 3f,
                Rafaga = 25, RadioForma = 0.8f, Gravedad = -0.12f
            }, centro, null);

            // 3) Escombros del traje con gravedad.
            CrearSistema(new ParametrosParticulas
            {
                Nombre = "VFX Explosión Escombros", ColorInicio = new Color(0.9f, 0.5f, 0.2f), ColorFin = new Color(0.2f, 0.2f, 0.2f, 0f),
                VidaMin = 0.8f, VidaMax = 1.5f, VelocidadMin = 6f, VelocidadMax = 13f, TamanoMin = 0.08f, TamanoMax = 0.22f,
                Rafaga = 40, RadioForma = 0.3f, Gravedad = 2.5f
            }, centro, null);

            // 4) Onda expansiva, 5) luz y 6) sacudida de cámara.
            CrearAnilloOnda(centro - Vector3.up * 0.8f, radio, PaletaVoidform.Destructor);
            LuzEfimera.Crear(centro, new Color(1f, 0.55f, 0.2f), 9f, radio * 3.5f, 0.6f);
            SacudidaCamara.SacudirDesde(centro, 0.35f, 0.45f, radio * 4f);
        }

        /// <summary>
        /// Partículas ambientales del bioma (esporas, bruma, polvo o ceniza) flotando sobre toda la arena.
        /// </summary>
        public static GameObject CrearAmbienteBioma(Transform padre, Vector3 centro, Color color, float radio, float gravedad)
        {
            if (!EfectosActivos) return null;
            ParticleSystem ps = CrearSistema(new ParametrosParticulas
            {
                Nombre = "Ambiente del Bioma",
                ColorInicio = new Color(color.r, color.g, color.b, 0.8f),
                ColorFin = new Color(color.r, color.g, color.b, 0f),
                VidaMin = 5f, VidaMax = 9f,
                VelocidadMin = 0.1f, VelocidadMax = 0.5f,
                TamanoMin = 0.08f, TamanoMax = 0.25f,
                TasaContinua = 45f,
                RadioForma = radio,
                Gravedad = gravedad,
                Bucle = true
            }, centro, null);
            if (padre != null) ps.transform.SetParent(padre, true);
            return ps.gameObject;
        }

        /// <summary>
        /// Aura persistente del guardián: hojas y esporas (Árbol), gotas en órbita (Agua),
        /// polvo y guijarros flotantes (Tierra) o brasas (Fuego). Se emparenta al personaje.
        /// </summary>
        public static GameObject CrearAuraElemental(Transform padre, TipoElemento elemento)
        {
            if (!EfectosActivos || padre == null) return null;
            Color c = PaletaVoidform.ColorElemento(elemento);
            ParametrosParticulas p = new ParametrosParticulas
            {
                Nombre = $"Aura {UtilidadesTipos.NombreElemento(elemento)}",
                ColorInicio = Color.Lerp(c, Color.white, 0.25f),
                ColorFin = new Color(c.r, c.g, c.b, 0f),
                VidaMin = 1f, VidaMax = 1.8f,
                VelocidadMin = 0.2f, VelocidadMax = 0.8f,
                TamanoMin = 0.08f, TamanoMax = 0.22f,
                TasaContinua = 18f,
                RadioForma = 1.1f,
                Gravedad = elemento == TipoElemento.Tierra ? 0.05f : -0.08f,
                Bucle = true,
                EspacioLocal = false
            };
            ParticleSystem ps = CrearSistema(p, new Vector3(0f, 1.4f, 0f), padre);
            return ps.gameObject;
        }
    }

    // =========================================================================
    // Componentes auxiliares de VFX
    // =========================================================================

    /// <summary>Destruye el objeto tras un tiempo (para efectos de un solo uso).</summary>
    public class AutodestruirTrasTiempo : MonoBehaviour
    {
        [SerializeField] private float segundos = 2f;

        public static void Programar(GameObject objetivo, float segundosHastaDestruir)
        {
            AutodestruirTrasTiempo a = objetivo.AddComponent<AutodestruirTrasTiempo>();
            a.segundos = segundosHastaDestruir;
        }

        private void Start()
        {
            Destroy(gameObject, segundos);
        }
    }

    /// <summary>Luz puntual que se apaga rápidamente (destellos, explosiones, fuego).</summary>
    public class LuzEfimera : MonoBehaviour
    {
        private Light luz;
        private float intensidadInicial;
        private float duracion;
        private float inicio;

        public static void Crear(Vector3 posicion, Color color, float intensidad, float alcance, float duracionSegundos)
        {
            GameObject go = new GameObject("VFX Luz Efímera");
            go.transform.position = posicion;
            Light l = go.AddComponent<Light>();
            l.type = LightType.Point;
            l.color = color;
            l.intensity = intensidad;
            l.range = alcance;
            l.shadows = LightShadows.None;
            LuzEfimera e = go.AddComponent<LuzEfimera>();
            e.luz = l;
            e.intensidadInicial = intensidad;
            e.duracion = Mathf.Max(0.01f, duracionSegundos);
            e.inicio = Time.time;
        }

        private void Update()
        {
            float t = (Time.time - inicio) / duracion;
            if (t >= 1f)
            {
                Destroy(gameObject);
                return;
            }
            luz.intensity = Mathf.Lerp(intensidadInicial, 0f, t);
        }
    }

    /// <summary>Hace desaparecer una línea (trazador) en pocos centésimos de segundo.</summary>
    public class LineaDesvanecida : MonoBehaviour
    {
        private LineRenderer linea;
        private float duracion = 0.07f;
        private float inicio;
        private Color colorInicio;
        private Color colorFin;

        public void Configurar(float segundos)
        {
            linea = GetComponent<LineRenderer>();
            duracion = Mathf.Max(0.01f, segundos);
            inicio = Time.time;
            colorInicio = linea.startColor;
            colorFin = linea.endColor;
        }

        private void Update()
        {
            float t = (Time.time - inicio) / duracion;
            if (t >= 1f)
            {
                Destroy(gameObject);
                return;
            }
            float a = 1f - t;
            linea.startColor = new Color(colorInicio.r, colorInicio.g, colorInicio.b, colorInicio.a * a);
            linea.endColor = new Color(colorFin.r, colorFin.g, colorFin.b, colorFin.a * a);
        }
    }

    /// <summary>Anillo que se expande y se desvanece (ondas sísmicas, mareas, explosiones).</summary>
    public class AnilloOndaExpansivo : MonoBehaviour
    {
        private const int Segmentos = 48;
        private LineRenderer linea;
        private float radioFinal;
        private float duracion;
        private float inicio;
        private Color color;

        public void Configurar(float radio, float segundos, Color colorAnillo)
        {
            linea = GetComponent<LineRenderer>();
            linea.positionCount = Segmentos;
            radioFinal = Mathf.Max(0.3f, radio);
            duracion = Mathf.Max(0.05f, segundos);
            inicio = Time.time;
            color = colorAnillo;
            Dibujar(0.2f, 1f);
        }

        private void Update()
        {
            float t = (Time.time - inicio) / duracion;
            if (t >= 1f)
            {
                Destroy(gameObject);
                return;
            }
            float suavizado = 1f - (1f - t) * (1f - t);
            Dibujar(Mathf.Lerp(0.2f, radioFinal, suavizado), 1f - t);
        }

        private void Dibujar(float radio, float alfa)
        {
            for (int i = 0; i < Segmentos; i++)
            {
                float angulo = i / (float)Segmentos * Mathf.PI * 2f;
                linea.SetPosition(i, new Vector3(Mathf.Cos(angulo) * radio, 0f, Mathf.Sin(angulo) * radio));
            }
            Color c = new Color(color.r, color.g, color.b, alfa);
            linea.startColor = c;
            linea.endColor = c;
        }
    }

    /// <summary>
    /// Sacudida de la cámara principal ante explosiones cercanas. Se añade sola a la cámara
    /// la primera vez que se necesita y se atenúa con la distancia a la explosión.
    /// </summary>
    public class SacudidaCamara : MonoBehaviour
    {
        private float intensidad;
        private float finSacudida;
        private float duracionTotal;
        private Vector3 desplazamientoAplicado;

        public static void SacudirDesde(Vector3 origen, float intensidadMaxima, float duracion, float alcance)
        {
            Camera cam = Camera.main;
            if (cam == null) return;
            float distancia = Vector3.Distance(cam.transform.position, origen);
            float factor = 1f - Mathf.Clamp01(distancia / Mathf.Max(1f, alcance));
            if (factor <= 0.01f) return;
            SacudidaCamara s = cam.GetComponent<SacudidaCamara>();
            if (s == null) s = cam.gameObject.AddComponent<SacudidaCamara>();
            s.Sacudir(intensidadMaxima * factor, duracion);
        }

        public void Sacudir(float nuevaIntensidad, float duracion)
        {
            intensidad = Mathf.Max(intensidad, nuevaIntensidad);
            duracionTotal = Mathf.Max(0.05f, duracion);
            finSacudida = Time.time + duracionTotal;
        }

        private void LateUpdate()
        {
            // Se deshace el desplazamiento del fotograma anterior para no acumular deriva.
            transform.localPosition -= desplazamientoAplicado;
            desplazamientoAplicado = Vector3.zero;

            if (Time.time >= finSacudida)
            {
                intensidad = 0f;
                return;
            }

            float restante = (finSacudida - Time.time) / duracionTotal;
            desplazamientoAplicado = Random.insideUnitSphere * intensidad * restante;
            transform.localPosition += desplazamientoAplicado;
        }
    }
}
