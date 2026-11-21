using System.Collections.Generic;
using UnityEngine;

namespace VoidformBioForge
{
    /// <summary>
    /// Resultado de generar una arena: dónde aparecen los equipos, dónde van las 5 bases y la fuente.
    /// </summary>
    public class DescripcionArena
    {
        public Transform Raiz;
        public DatosBioma Datos;
        public readonly List<Vector3> AparicionDestructores = new List<Vector3>();
        public readonly List<Vector3> AparicionCuidadores = new List<Vector3>();
        public Vector3[] PosicionesBases = new Vector3[5];
        public FuenteEnergiaViva Fuente;
        public Vector3 Centro = Vector3.zero;

        public Quaternion RotacionHaciaCentro(Vector3 desde)
        {
            Vector3 d = Centro - desde;
            d.y = 0f;
            return d.sqrMagnitude < 0.01f ? Quaternion.identity : Quaternion.LookRotation(d);
        }
    }

    /// <summary>
    /// Genera por código los 5 biomas del exoplaneta, jugables sin importar ningún asset.
    /// -----------------------------------------------------------------
    /// Nivel 1: suelo, muros perimetrales, luz, niebla y zonas de aparición de cada bando
    ///          (zona de aterrizaje humana al sur y santuario elemental al norte).
    /// Nivel 2: coberturas específicas de cada bioma colocadas con simetría central (justa para
    ///          ambos equipos), posiciones de las 5 bases y la Fuente de Energía Viva.
    /// Nivel 3: peligros ambientales que tratan distinto a cada facción y DEGRADACIÓN VISUAL en tiempo
    ///          real: cuando baja la integridad del ecosistema el bioma se decolora, la vegetación se
    ///          marchita, la niebla se espesa y la luz se apaga (la problemática social se ve en pantalla).
    /// </summary>
    public class GeneradorBiomas : MonoBehaviour
    {
        private const float MitadArena = 55f;

        private struct PiezaDegradable
        {
            public Renderer Renderizador;
            public Color ColorSano;
            public Color ColorDegradado;
            public Color EmisionSana;
        }

        private readonly List<PiezaDegradable> piezasDegradables = new List<PiezaDegradable>();
        private DatosBioma datos;
        private Light sol;
        private GestorEquilibrioEcosistema ecosistemaConectado;
        private System.Random aleatorio;
        private float ultimaDegradacionAplicada = -1f;

        // =====================================================================
        // Punto de entrada
        // =====================================================================

        public static DescripcionArena Generar(Transform padre, BiomaPlaneta bioma, int semilla)
        {
            GameObject raiz = new GameObject($"Entorno — {DatosBioma.Obtener(bioma).Nombre}");
            raiz.transform.SetParent(padre, false);
            GeneradorBiomas generador = raiz.AddComponent<GeneradorBiomas>();
            return generador.Construir(bioma, semilla);
        }

        private DescripcionArena Construir(BiomaPlaneta bioma, int semilla)
        {
            datos = DatosBioma.Obtener(bioma);
            aleatorio = new System.Random(semilla);

            DescripcionArena arena = new DescripcionArena { Raiz = transform, Datos = datos };

            // ---------- Nivel 1 ----------
            ConstruirSueloYMuros();
            ConfigurarIluminacionYNiebla();
            ConstruirZonaAterrizajeHumana(new Vector3(0f, 0f, -44f));
            ConstruirSantuarioElemental(new Vector3(0f, 0f, 44f));

            for (int i = 0; i < 8; i++)
            {
                float x = -7f + (i % 4) * 4.6f;
                float z = i < 4 ? 0f : 3f;
                arena.AparicionDestructores.Add(new Vector3(x, 1.1f, -45f + z));
                arena.AparicionCuidadores.Add(new Vector3(-x, 1.1f, 45f - z));
            }

            // ---------- Nivel 2 ----------
            arena.PosicionesBases[0] = new Vector3(-26f, 0f, -16f);
            arena.PosicionesBases[1] = new Vector3(26f, 0f, -16f);
            arena.PosicionesBases[2] = new Vector3(0f, 0f, 0f);
            arena.PosicionesBases[3] = new Vector3(-26f, 0f, 16f);
            arena.PosicionesBases[4] = new Vector3(26f, 0f, 16f);

            arena.Fuente = FuenteEnergiaViva.Crear(transform, new Vector3(0f, 0f, 0f), datos.ColorAcento, datos.Bioma);
            ColocarCoberturas(arena);

            // ---------- Nivel 3 ----------
            ColocarPeligrosAmbientales(arena);
            FabricaEfectosVisuales.CrearAmbienteBioma(transform, new Vector3(0f, 6f, 0f), datos.ColorAcento, 45f,
                bioma == BiomaPlaneta.CalderaForjas ? -0.02f : 0.005f);

            // Gravedad del escenario (el Nexus la reduce: parábolas de salto más altas y caídas lentas).
            ControladorMovimientoAssaultCube.MultiplicadorGravedadAmbiente = datos.MultiplicadorGravedad;

            // Auroras multicolores sobre el Nexus y la Convergencia.
            if (bioma == BiomaPlaneta.NexusPrimordial || bioma == BiomaPlaneta.ConvergenciaTotal)
            {
                AuroraCielo.Crear(transform, 4);
            }

            return arena;
        }

        // =====================================================================
        // Nivel 1: estructura básica
        // =====================================================================

        private void ConstruirSueloYMuros()
        {
            GameObject suelo = CrearBloque("Suelo", PrimitiveType.Cube, new Vector3(0f, -0.5f, 0f), new Vector3(MitadArena * 2f, 1f, MitadArena * 2f), Vector3.zero);
            RegistrarDegradable(suelo.GetComponent<Renderer>(), datos.ColorSuelo, datos.ColorSueloDegradado, Color.black);

            // Muros perimetrales con franja de runas luminosas.
            Vector3[] posiciones =
            {
                new Vector3(0f, 4f, MitadArena), new Vector3(0f, 4f, -MitadArena),
                new Vector3(MitadArena, 4f, 0f), new Vector3(-MitadArena, 4f, 0f)
            };
            Vector3[] escalas =
            {
                new Vector3(MitadArena * 2f + 2f, 8f, 2f), new Vector3(MitadArena * 2f + 2f, 8f, 2f),
                new Vector3(2f, 8f, MitadArena * 2f + 2f), new Vector3(2f, 8f, MitadArena * 2f + 2f)
            };
            for (int i = 0; i < 4; i++)
            {
                GameObject muro = CrearBloque($"Muro {i + 1}", PrimitiveType.Cube, posiciones[i], escalas[i], Vector3.zero);
                RegistrarDegradable(muro.GetComponent<Renderer>(), datos.ColorRoca, Color.Lerp(datos.ColorRoca, Color.gray, 0.5f), Color.black);

                GameObject franja = CrearBloque($"Franja rúnica {i + 1}", PrimitiveType.Cube, posiciones[i] + Vector3.up * 2.5f,
                    new Vector3(escalas[i].x * 1.001f, 0.25f, escalas[i].z * 1.001f), Vector3.zero, false);
                RegistrarDegradable(franja.GetComponent<Renderer>(), datos.ColorAcento * 0.5f, Color.gray * 0.3f, datos.ColorAcento * 1.5f);
            }
        }

        private void ConfigurarIluminacionYNiebla()
        {
            // Cada bioma trae su propio sol; cualquier otra luz direccional de la escena (por ejemplo, la
            // "Directional Light" que Unity crea por defecto o el sol de una partida anterior) se apaga
            // para no duplicar la iluminación.
            GameObject objetoSol = new GameObject("Sol del Exoplaneta");
            objetoSol.transform.SetParent(transform, false);
            objetoSol.transform.rotation = Quaternion.Euler(48f, -35f, 0f);
            sol = objetoSol.AddComponent<Light>();
            sol.type = LightType.Directional;
            foreach (Light otra in FindObjectsByType<Light>(FindObjectsSortMode.None))
            {
                if (otra != sol && otra.type == LightType.Directional) otra.enabled = false;
            }
            sol.color = datos.ColorLuz;
            sol.intensity = datos.IntensidadLuz;
            sol.shadows = LightShadows.Soft;

            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
            RenderSettings.ambientLight = datos.ColorAmbiente;
            RenderSettings.fog = true;
            RenderSettings.fogMode = FogMode.ExponentialSquared;
            RenderSettings.fogColor = datos.ColorNiebla;
            RenderSettings.fogDensity = datos.DensidadNiebla;
        }

        private void ConstruirZonaAterrizajeHumana(Vector3 centro)
        {
            // Plataforma metálica, cápsula de descenso y focos: la huella industrial humana.
            GameObject plataforma = CrearBloque("Plataforma de Aterrizaje", PrimitiveType.Cube, centro + new Vector3(0f, 0.1f, 0f), new Vector3(18f, 0.2f, 9f), Vector3.zero);
            MaterialesProcedurales.AplicarColor(plataforma.GetComponent<Renderer>(), new Color(0.3f, 0.32f, 0.35f));

            GameObject capsula = CrearBloque("Cápsula de Descenso", PrimitiveType.Capsule, centro + new Vector3(0f, 2.5f, -7f), new Vector3(4f, 3f, 4f), Vector3.zero);
            MaterialesProcedurales.AplicarColor(capsula.GetComponent<Renderer>(), new Color(0.75f, 0.45f, 0.2f), PaletaVoidform.Destructor * 0.3f);

            for (int i = -1; i <= 1; i += 2)
            {
                GameObject torre = CrearBloque("Torre de Focos", PrimitiveType.Cylinder, centro + new Vector3(i * 10f, 2.5f, -3f), new Vector3(0.4f, 2.5f, 0.4f), Vector3.zero);
                MaterialesProcedurales.AplicarColor(torre.GetComponent<Renderer>(), new Color(0.25f, 0.25f, 0.28f));
                GameObject foco = CrearBloque("Foco", PrimitiveType.Cube, centro + new Vector3(i * 10f, 5.2f, -3f), new Vector3(1.2f, 0.5f, 0.6f), Vector3.zero, false);
                MaterialesProcedurales.AplicarColor(foco.GetComponent<Renderer>(), Color.white, new Color(1f, 0.85f, 0.6f) * 2f);
            }
        }

        private void ConstruirSantuarioElemental(Vector3 centro)
        {
            // Círculo de piedras rúnicas: el hogar de los guardianes.
            for (int i = 0; i < 7; i++)
            {
                // Arco por DETRÁS de los puntos de aparición (z de 47 a 52) para no bloquearlos.
                float angulo = (i / 6f) * Mathf.PI;
                Vector3 p = centro + new Vector3(Mathf.Cos(angulo) * 12f, 2f, 3f + Mathf.Sin(angulo) * 5f);
                GameObject piedra = CrearBloque("Piedra Rúnica", PrimitiveType.Cube, p, new Vector3(1.2f, 4f, 0.9f), new Vector3(0f, i * 25f, 0f));
                RegistrarDegradable(piedra.GetComponent<Renderer>(), datos.ColorRoca, Color.gray * 0.5f, Color.black);
                GameObject runa = CrearBloque("Runa", PrimitiveType.Cube, p + Vector3.up * 1f, new Vector3(1.25f, 0.2f, 0.95f), new Vector3(0f, i * 25f, 0f), false);
                RegistrarDegradable(runa.GetComponent<Renderer>(), datos.ColorAcento, Color.gray * 0.4f, datos.ColorAcento * 2f);
            }
        }

        // =====================================================================
        // Nivel 2: coberturas del bioma con simetría central
        // =====================================================================

        private void ColocarCoberturas(DescripcionArena arena)
        {
            int cantidadPorMitad = 16;
            int intentos = 0;
            int colocadas = 0;

            while (colocadas < cantidadPorMitad && intentos < 400)
            {
                intentos++;
                Vector3 p = new Vector3(Rango(-48f, 48f), 0f, Rango(-40f, -3f));
                if (!PosicionLibre(p, arena)) continue;

                CrearCoberturaBioma(p);
                CrearCoberturaBioma(new Vector3(-p.x, 0f, -p.z)); // Punto simétrico para el otro equipo.
                colocadas++;
            }
        }

        private bool PosicionLibre(Vector3 p, DescripcionArena arena)
        {
            if (Mathf.Abs(p.z) > 36f && Mathf.Abs(p.x) < 14f) return false;  // Zonas de aparición.
            if (new Vector2(p.x, p.z).magnitude < 9f) return false;           // Fuente central.
            foreach (Vector3 b in arena.PosicionesBases)
            {
                // Las bases son simétricas, así que comprobar el punto original basta para su reflejo.
                if (Vector3.Distance(new Vector3(b.x, 0f, b.z), p) < 10f) return false;
            }
            return true;
        }

        /// <summary>
        /// Cuadrante elemental de un punto en el Nexus y la Convergencia:
        /// suroeste = Bosque, sureste = Abismos, noroeste = Caldera, noreste = Agujas.
        /// </summary>
        private static BiomaPlaneta CuadranteDe(Vector3 p)
        {
            int cuadrante = (p.x >= 0f ? 1 : 0) + (p.z >= 0f ? 2 : 0);
            return (BiomaPlaneta)cuadrante;
        }

        private void CrearCoberturaBioma(Vector3 p)
        {
            BiomaPlaneta bioma = datos.Bioma;
            if (bioma == BiomaPlaneta.NexusPrimordial || bioma == BiomaPlaneta.ConvergenciaTotal)
            {
                BiomaPlaneta cuadrante = CuadranteDe(p);
                // En el Nexus domina la arquitectura del santuario; en la Convergencia, los biomas originales.
                bool piezaDelNexus = bioma == BiomaPlaneta.NexusPrimordial ? Rango(0f, 1f) < 0.6f : new Vector2(p.x, p.z).magnitude < 20f;
                if (piezaDelNexus)
                {
                    CrearColumnaNexus(p, ElementoDeBioma(cuadrante));
                    return;
                }
                bioma = cuadrante;
            }

            switch (bioma)
            {
                case BiomaPlaneta.BosqueSusurros: CrearArbolBioluminiscente(p); break;
                case BiomaPlaneta.AbismosMareas: CrearEstructuraDeCoral(p); break;
                case BiomaPlaneta.CalderaForjas: CrearAgujaDeObsidiana(p); break;
                default: CrearTorreDeAleacion(p); break;
            }
        }

        private static TipoElemento ElementoDeBioma(BiomaPlaneta b)
        {
            switch (b)
            {
                case BiomaPlaneta.BosqueSusurros: return TipoElemento.Arbol;
                case BiomaPlaneta.AbismosMareas: return TipoElemento.Agua;
                case BiomaPlaneta.CalderaForjas: return TipoElemento.Fuego;
                default: return TipoElemento.Metal;
            }
        }

        /// <summary>Árbol colosal de corteza translúcida, raíces sin enterrar, veta mineral violeta y hojas-panel.</summary>
        private void CrearArbolBioluminiscente(Vector3 p)
        {
            Color vegetacion = DatosBioma.Obtener(BiomaPlaneta.BosqueSusurros).ColorVegetacion;
            Color acento = DatosBioma.Obtener(BiomaPlaneta.BosqueSusurros).ColorAcento;
            float altura = Rango(7f, 12f);
            float grosor = Rango(1.4f, 2.3f);

            GameObject tronco = CrearBloque("Tronco de Corteza Translúcida", PrimitiveType.Cylinder, p + Vector3.up * (altura * 0.5f), new Vector3(grosor, altura * 0.5f, grosor), Vector3.zero);
            RegistrarDegradable(tronco.GetComponent<Renderer>(), new Color(0.12f, 0.35f, 0.3f), new Color(0.25f, 0.22f, 0.2f), acento * 0.45f);

            for (int i = 0; i < 3; i++)
            {
                float angulo = i * 120f + Rango(-20f, 20f);
                Vector3 dir = Quaternion.Euler(0f, angulo, 0f) * Vector3.forward;
                GameObject raiz = CrearBloque("Raíz sin enterrar", PrimitiveType.Capsule, p + dir * (grosor * 0.9f) + Vector3.up * 0.35f,
                    new Vector3(0.5f, 1.5f, 0.5f), new Vector3(70f, angulo, 0f));
                RegistrarDegradable(raiz.GetComponent<Renderer>(), new Color(0.28f, 0.2f, 0.14f), new Color(0.22f, 0.2f, 0.18f), Color.black);
            }

            GameObject veta = CrearBloque("Veta de mineral orgánico", PrimitiveType.Sphere, p + new Vector3(Rango(-2f, 2f), 0.1f, Rango(-2f, 2f)),
                new Vector3(Rango(2f, 3.5f), 0.5f, Rango(1f, 2f)), new Vector3(0f, Rango(0f, 180f), 0f));
            RegistrarDegradable(veta.GetComponent<Renderer>(), new Color(0.45f, 0.2f, 0.65f), new Color(0.3f, 0.28f, 0.28f), new Color(0.6f, 0.25f, 1f) * 0.8f);

            GameObject copa = CrearBloque("Copa de hojas-panel", PrimitiveType.Sphere, p + Vector3.up * (altura + 1.5f), new Vector3(Rango(6f, 9f), Rango(2.5f, 4f), Rango(6f, 9f)), Vector3.zero, false);
            RegistrarDegradable(copa.GetComponent<Renderer>(), vegetacion, new Color(0.35f, 0.3f, 0.22f), acento * 0.4f);
        }

        /// <summary>Estructura flotante de coral sintético y cristal marino con un tramo de puente colgante.</summary>
        private void CrearEstructuraDeCoral(Vector3 p)
        {
            Color acento = DatosBioma.Obtener(BiomaPlaneta.AbismosMareas).ColorAcento;
            GameObject plataforma = CrearBloque("Plataforma de coral sintético", PrimitiveType.Cylinder, p + Vector3.up * 0.4f, new Vector3(Rango(4f, 6f), 0.4f, Rango(4f, 6f)), Vector3.zero);
            RegistrarDegradable(plataforma.GetComponent<Renderer>(), new Color(0.45f, 0.55f, 0.75f), new Color(0.35f, 0.33f, 0.3f), Color.black);

            for (int i = 0; i < 3; i++)
            {
                float alto = Rango(2f, 4.5f);
                GameObject coral = CrearBloque("Rama de coral", PrimitiveType.Capsule, p + new Vector3(Rango(-1.5f, 1.5f), 0.8f + alto * 0.5f, Rango(-1.5f, 1.5f)),
                    new Vector3(0.5f, alto * 0.5f, 0.5f), new Vector3(Rango(-15f, 15f), 0f, Rango(-15f, 15f)));
                RegistrarDegradable(coral.GetComponent<Renderer>(), new Color(0.3f, 0.6f, 0.95f), new Color(0.3f, 0.3f, 0.3f), acento * 0.6f);
            }

            GameObject cristal = CrearBloque("Cristal marino", PrimitiveType.Cube, p + new Vector3(0f, 2.2f, 0f), new Vector3(0.7f, 2.8f, 0.7f), new Vector3(15f, 45f, 10f), false);
            RegistrarDegradable(cristal.GetComponent<Renderer>(), new Color(0.6f, 0.85f, 1f), Color.gray * 0.5f, new Color(0.4f, 0.7f, 1f) * 1.3f);

            if (Rango(0f, 1f) > 0.5f)
            {
                GameObject puente = CrearBloque("Puente colgante flexible", PrimitiveType.Cube, p + new Vector3(4.5f, 0.9f, 0f), new Vector3(4.5f, 0.12f, 1.3f), new Vector3(0f, 0f, 4f));
                RegistrarDegradable(puente.GetComponent<Renderer>(), new Color(0.35f, 0.3f, 0.26f), new Color(0.25f, 0.23f, 0.2f), Color.black);
                puente.isStatic = false;
                puente.AddComponent<BalanceoPorOleaje>();
            }
        }

        /// <summary>Aguja de obsidiana con fractura vítrea, grieta de magma y cristales por enfriamiento rápido.</summary>
        private void CrearAgujaDeObsidiana(Vector3 p)
        {
            Color acento = DatosBioma.Obtener(BiomaPlaneta.CalderaForjas).ColorAcento;
            float altura = Rango(4f, 8f);
            GameObject aguja = CrearBloque("Aguja de Obsidiana", PrimitiveType.Cube, p + Vector3.up * (altura * 0.45f), new Vector3(Rango(1.5f, 2.6f), altura, Rango(1.5f, 2.6f)), new Vector3(Rango(-12f, 12f), Rango(0f, 90f), Rango(-12f, 12f)));
            RegistrarDegradable(aguja.GetComponent<Renderer>(), new Color(0.05f, 0.05f, 0.06f), new Color(0.25f, 0.23f, 0.23f), Color.black);

            GameObject grieta = CrearBloque("Grieta de Magma", PrimitiveType.Cube, p + Vector3.up * (altura * 0.35f), new Vector3(0.15f, altura * 0.7f, 0.15f), new Vector3(0f, 30f, 8f), false);
            RegistrarDegradable(grieta.GetComponent<Renderer>(), acento, new Color(0.3f, 0.2f, 0.15f), acento * 2.5f);

            for (int i = 0; i < 2; i++)
            {
                GameObject cristal = CrearBloque("Cristal de enfriamiento rápido", PrimitiveType.Cube, p + new Vector3(Rango(-1.5f, 1.5f), Rango(1f, 3f), Rango(-1.5f, 1.5f)),
                    new Vector3(0.3f, Rango(0.8f, 1.5f), 0.3f), new Vector3(Rango(-40f, 40f), Rango(0f, 90f), Rango(-40f, 40f)), false);
                RegistrarDegradable(cristal.GetComponent<Renderer>(), new Color(0.9f, 0.55f, 0.4f), Color.gray * 0.4f, new Color(1f, 0.5f, 0.3f) * 0.6f);
            }

            if (Rango(0f, 1f) > 0.6f)
            {
                // Respiradero de vapor presurizado.
                FabricaEfectosVisuales.CrearAmbienteBioma(transform, p + new Vector3(2.5f, 0.2f, 0f), new Color(0.85f, 0.8f, 0.75f), 0.6f, -0.25f);
            }
        }

        /// <summary>Torre monolítica de aleación plateada levitando sobre una placa tectónica espejo.</summary>
        private void CrearTorreDeAleacion(Vector3 p)
        {
            Color acento = DatosBioma.Obtener(BiomaPlaneta.AgujasAleacion).ColorAcento;
            GameObject placa = CrearBloque("Placa tectónica espejo", PrimitiveType.Cube, p + Vector3.up * 0.05f, new Vector3(Rango(5f, 7f), 0.1f, Rango(5f, 7f)), new Vector3(0f, Rango(0f, 90f), 0f), false);
            RegistrarDegradable(placa.GetComponent<Renderer>(), new Color(0.7f, 0.75f, 0.82f), new Color(0.35f, 0.28f, 0.22f), Color.black);
            Material mPlaca = placa.GetComponent<Renderer>().material;
            if (mPlaca.HasProperty("_Metallic")) mPlaca.SetFloat("_Metallic", 0.9f);
            if (mPlaca.HasProperty("_Glossiness")) mPlaca.SetFloat("_Glossiness", 0.9f);
            if (mPlaca.HasProperty("_Smoothness")) mPlaca.SetFloat("_Smoothness", 0.9f);

            float altura = Rango(6f, 12f);
            GameObject torre = CrearBloque("Torre de aleación levitante", PrimitiveType.Cube, p + Vector3.up * (altura * 0.5f + 0.25f), new Vector3(1.6f, altura, 1.6f), new Vector3(0f, 45f, 0f));
            RegistrarDegradable(torre.GetComponent<Renderer>(), new Color(0.75f, 0.8f, 0.88f), new Color(0.38f, 0.3f, 0.24f), Color.black);
            torre.isStatic = false;
            torre.AddComponent<LevitacionMagnetica>().Configurar(0.25f, 0.06f, 0.35f);

            GameObject junta = CrearBloque("Junta de resonancia", PrimitiveType.Cube, p + Vector3.up * (altura * 0.7f), new Vector3(1.7f, 0.12f, 1.7f), new Vector3(0f, 45f, 0f), false);
            RegistrarDegradable(junta.GetComponent<Renderer>(), acento, Color.gray * 0.4f, acento * 1.5f);
        }

        /// <summary>Columna de piedra translúcida del Nexus con canal de plasma, enredadera petrificada y filamento conductor.</summary>
        private void CrearColumnaNexus(Vector3 p, TipoElemento cuadrante)
        {
            Color c = PaletaVoidform.ColorElemento(cuadrante);
            GameObject columna = CrearBloque("Columna de piedra translúcida", PrimitiveType.Cylinder, p + Vector3.up * 3.5f, new Vector3(1.6f, 3.5f, 1.6f), Vector3.zero);
            RegistrarDegradable(columna.GetComponent<Renderer>(), new Color(0.72f, 0.74f, 0.82f), Color.gray * 0.5f, c * 0.25f);

            GameObject canal = CrearBloque("Canal de plasma", PrimitiveType.Cube, p + new Vector3(0f, 0.08f, 2.2f), new Vector3(0.6f, 0.15f, 3f), Vector3.zero, false);
            RegistrarDegradable(canal.GetComponent<Renderer>(), new Color(0.2f, 0.4f, 1f), Color.gray * 0.3f, new Color(0.3f, 0.5f, 1f) * 1.4f);

            GameObject enredadera = CrearBloque("Enredadera petrificada", PrimitiveType.Capsule, p + new Vector3(0.75f, 3f, 0f), new Vector3(0.2f, 3f, 0.2f), new Vector3(0f, 0f, 8f), false);
            RegistrarDegradable(enredadera.GetComponent<Renderer>(), new Color(0.4f, 0.36f, 0.3f), Color.gray * 0.4f, Color.black);

            GameObject filamento = CrearBloque("Filamento conductor", PrimitiveType.Cylinder, p + new Vector3(-0.8f, 3.5f, 0f), new Vector3(0.06f, 3.5f, 0.06f), Vector3.zero, false);
            RegistrarDegradable(filamento.GetComponent<Renderer>(), PaletaVoidform.Metal, Color.gray * 0.4f, c * 1.8f);
        }

        // =====================================================================
        // Nivel 3: peligros ambientales y degradación
        // =====================================================================

        private void ColocarPeligrosAmbientales(DescripcionArena arena)
        {
            Vector3[] posiciones =
            {
                new Vector3(-14f, 0f, -8f), new Vector3(14f, 0f, 8f),
                new Vector3(14f, 0f, -26f), new Vector3(-14f, 0f, 26f),
                new Vector3(-40f, 0f, 2f), new Vector3(40f, 0f, -2f)
            };

            foreach (Vector3 posicion in posiciones)
            {
                BiomaPlaneta tipo = datos.Bioma;
                if (tipo == BiomaPlaneta.NexusPrimordial || tipo == BiomaPlaneta.ConvergenciaTotal)
                {
                    tipo = CuadranteDe(posicion);
                }
                CrearPeligro(posicion, tipo);
            }
        }

        private void CrearPeligro(Vector3 posicion, BiomaPlaneta tipo)
        {
            float radio = 4.5f;
            GameObject visual = CrearBloque("Peligro Ambiental", PrimitiveType.Cylinder, posicion + Vector3.up * 0.03f, new Vector3(radio * 2f, 0.03f, radio * 2f), Vector3.zero, false);
            ZonaAmbientalBioma zona = visual.AddComponent<ZonaAmbientalBioma>();

            switch (tipo)
            {
                case BiomaPlaneta.BosqueSusurros:
                    zona.Configurar("Claro de esporas", radio, TipoDano.Arbol, 0f, 0.75f, 4f);
                    RegistrarDegradable(visual.GetComponent<Renderer>(), new Color(0.2f, 0.6f, 0.45f), new Color(0.3f, 0.28f, 0.2f), new Color(0.3f, 1f, 0.75f) * 0.8f);
                    break;
                case BiomaPlaneta.AbismosMareas:
                    zona.Configurar("Corriente de plasma", radio, TipoDano.Agua, 0f, 0.6f, 3f);
                    RegistrarDegradable(visual.GetComponent<Renderer>(), new Color(0.12f, 0.25f, 0.75f), new Color(0.3f, 0.3f, 0.25f), new Color(0.25f, 0.4f, 1f) * 1.1f);
                    break;
                case BiomaPlaneta.CalderaForjas:
                    zona.Configurar("Río de magma", radio, TipoDano.Fuego, 12f, 1f, 3f);
                    RegistrarDegradable(visual.GetComponent<Renderer>(), new Color(1f, 0.35f, 0.05f), new Color(0.35f, 0.25f, 0.2f), new Color(1f, 0.4f, 0.05f) * 2.5f);
                    break;
                default:
                    zona.Configurar("Campo de inducción", radio, TipoDano.Metal, 5f, 0.8f, 2f);
                    RegistrarDegradable(visual.GetComponent<Renderer>(), new Color(0.6f, 0.7f, 0.85f), new Color(0.35f, 0.3f, 0.25f), new Color(0.5f, 0.75f, 1f) * 1.2f);
                    break;
            }
        }

        /// <summary>Conecta la degradación visual a la integridad del ecosistema.</summary>
        public void ConectarEcosistema(GestorEquilibrioEcosistema ecosistema)
        {
            if (ecosistemaConectado != null) ecosistemaConectado.AlCambiarIntegridad -= AplicarDegradacion;
            ecosistemaConectado = ecosistema;
            if (ecosistema != null)
            {
                ecosistema.AlCambiarIntegridad += AplicarDegradacion;
                AplicarDegradacion(ecosistema.Integridad);
            }
        }

        private void OnDestroy()
        {
            if (ecosistemaConectado != null) ecosistemaConectado.AlCambiarIntegridad -= AplicarDegradacion;
        }

        private void AplicarDegradacion(float integridad)
        {
            // 0 = sano, 1 = completamente degradado. Por encima de 85 no se nota ningún daño.
            float degradacion = 1f - Mathf.InverseLerp(15f, 85f, integridad);
            if (Mathf.Abs(degradacion - ultimaDegradacionAplicada) < 0.01f) return;
            ultimaDegradacionAplicada = degradacion;

            foreach (PiezaDegradable pieza in piezasDegradables)
            {
                if (pieza.Renderizador == null) continue;
                Color color = Color.Lerp(pieza.ColorSano, pieza.ColorDegradado, degradacion);
                Color emision = Color.Lerp(pieza.EmisionSana, Color.black, degradacion * 0.85f);
                MaterialesProcedurales.AplicarColor(pieza.Renderizador, color, emision);
            }

            RenderSettings.fogDensity = datos.DensidadNiebla * Mathf.Lerp(1f, 2.2f, degradacion);
            RenderSettings.fogColor = Color.Lerp(datos.ColorNiebla, new Color(0.3f, 0.28f, 0.26f), degradacion);
            if (sol != null)
            {
                sol.intensity = datos.IntensidadLuz * Mathf.Lerp(1f, 0.6f, degradacion);
                sol.color = Color.Lerp(datos.ColorLuz, new Color(0.8f, 0.75f, 0.7f), degradacion);
            }
        }

        // =====================================================================
        // Utilidades
        // =====================================================================

        private GameObject CrearBloque(string nombre, PrimitiveType tipo, Vector3 posicion, Vector3 escala, Vector3 rotacion, bool conColision = true)
        {
            GameObject go = GameObject.CreatePrimitive(tipo);
            go.name = nombre;
            go.transform.SetParent(transform, false);
            go.transform.position = posicion;
            go.transform.localScale = escala;
            go.transform.rotation = Quaternion.Euler(rotacion);
            go.isStatic = true;
            if (!conColision)
            {
                Collider c = go.GetComponent<Collider>();
                if (c != null) DestroyImmediate(c);
            }
            return go;
        }

        private void RegistrarDegradable(Renderer r, Color sano, Color degradado, Color emisionSana)
        {
            if (r == null) return;
            MaterialesProcedurales.AplicarColor(r, sano, emisionSana);
            piezasDegradables.Add(new PiezaDegradable
            {
                Renderizador = r,
                ColorSano = sano,
                ColorDegradado = degradado,
                EmisionSana = emisionSana
            });
        }

        private float Rango(float minimo, float maximo)
        {
            return minimo + (float)aleatorio.NextDouble() * (maximo - minimo);
        }
    }

    /// <summary>Balanceo suave de los puentes colgantes de los Abismos al ritmo del oleaje.</summary>
    public class BalanceoPorOleaje : MonoBehaviour
    {
        private Quaternion rotacionInicial;
        private float fase;

        private void Start()
        {
            rotacionInicial = transform.rotation;
            fase = Random.Range(0f, 10f);
        }

        private void Update()
        {
            float balanceo = Mathf.Sin(Time.time * 0.7f + fase) * 3f;
            float cabeceo = Mathf.Sin(Time.time * 0.45f + fase * 1.3f) * 1.5f;
            transform.rotation = rotacionInicial * Quaternion.Euler(cabeceo, 0f, balanceo);
        }
    }

    /// <summary>
    /// Auroras multicolores: cintas emisivas a gran altura que ondulan y cambian de color lentamente
    /// (Nexus Primordial y Convergencia Total).
    /// </summary>
    public class AuroraCielo : MonoBehaviour
    {
        private Renderer[] cintas;
        private Vector3[] posicionesBase;

        public static void Crear(Transform padre, int numeroCintas)
        {
            GameObject raiz = new GameObject("Auroras multicolores");
            raiz.transform.SetParent(padre, false);
            AuroraCielo aurora = raiz.AddComponent<AuroraCielo>();
            aurora.cintas = new Renderer[numeroCintas];
            aurora.posicionesBase = new Vector3[numeroCintas];
            for (int i = 0; i < numeroCintas; i++)
            {
                GameObject cinta = GameObject.CreatePrimitive(PrimitiveType.Cube);
                cinta.name = $"Cinta de aurora {i + 1}";
                cinta.transform.SetParent(raiz.transform, false);
                Vector3 pos = new Vector3(-60f + i * 40f, 70f + i * 6f, 40f - i * 25f);
                cinta.transform.localPosition = pos;
                cinta.transform.localScale = new Vector3(140f, 10f, 0.5f);
                cinta.transform.localRotation = Quaternion.Euler(10f, 20f * i, 0f);
                DestroyImmediate(cinta.GetComponent<Collider>());
                Renderer r = cinta.GetComponent<Renderer>();
                r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                aurora.cintas[i] = r;
                aurora.posicionesBase[i] = pos;
            }
        }

        private void Update()
        {
            if (cintas == null) return;
            for (int i = 0; i < cintas.Length; i++)
            {
                if (cintas[i] == null) continue;
                float t = Time.time * 0.05f + i * 0.23f;
                Color c = Color.HSVToRGB(Mathf.Repeat(t, 1f), 0.65f, 1f);
                MaterialesProcedurales.AplicarColor(cintas[i], c * 0.2f, c * 1.6f);
                cintas[i].transform.localPosition = posicionesBase[i] + new Vector3(Mathf.Sin(Time.time * 0.2f + i) * 6f, Mathf.Sin(Time.time * 0.3f + i * 2f) * 2f, 0f);
            }
        }
    }
}
