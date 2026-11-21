using System.Collections.Generic;
using UnityEngine;

namespace VoidformBioForge
{
    /// <summary>
    /// Referencias a las piezas visuales de un personaje para poder recolorearlas
    /// (por ejemplo, cuando un guardián del Nexus cambia de elemento).
    /// </summary>
    public class ModeloVisualPersonaje : MonoBehaviour
    {
        private readonly List<Renderer> piezasPrimarias = new List<Renderer>();
        private readonly List<Renderer> piezasSecundarias = new List<Renderer>();
        private readonly List<Renderer> piezasEmisivas = new List<Renderer>();

        public void RegistrarPieza(Renderer r, int capa)
        {
            if (r == null) return;
            switch (capa)
            {
                case 0: piezasPrimarias.Add(r); break;
                case 1: piezasSecundarias.Add(r); break;
                default: piezasEmisivas.Add(r); break;
            }
        }

        public void AplicarPaleta(Color primario, Color secundario)
        {
            foreach (Renderer r in piezasPrimarias) MaterialesProcedurales.AplicarMaterialRealista(r, primario, Color.black);
            foreach (Renderer r in piezasSecundarias) MaterialesProcedurales.AplicarMaterialRealista(r, secundario, Color.black);
            foreach (Renderer r in piezasEmisivas) if (r != null) MaterialesProcedurales.AplicarColor(r, primario, primario * 2.2f);
        }
    }

    /// <summary>
    /// Esqueleto procedural: articulaciones con pivote real que el AnimadorProcedural rota.
    /// Los modelos de Blender usan los MISMOS nombres de objeto (Cadera, Torso, Cabeza, Brazo_I, Antebrazo_I,
    /// Mano_I, Brazo_D, Antebrazo_D, Mano_D, Muslo_I, Pierna_I, Pie_I, Muslo_D, Pierna_D, Pie_D) para que la
    /// animación funcione igual con el modelo de Blender que con el de primitivas.
    /// </summary>
    public class EsqueletoProcedural : MonoBehaviour
    {
        public Transform Cadera, Torso, Cabeza;
        public Transform BrazoI, AntebrazoI, ManoI, BrazoD, AntebrazoD, ManoD;
        public Transform MusloI, PiernaI, PieI, MusloD, PiernaD, PieD;
        public Transform Capa;
        public bool EsGuardian;
        public float Escala = 1f;

        /// <summary>Busca los huesos por nombre (para modelos importados de Blender).</summary>
        public bool AsignarPorNombre(Transform raiz)
        {
            Dictionary<string, Transform> mapa = new Dictionary<string, Transform>();
            foreach (Transform t in raiz.GetComponentsInChildren<Transform>(true))
            {
                string nombre = t.name;
                int punto = nombre.IndexOf('.');
                if (punto > 0) nombre = nombre.Substring(0, punto); // "Torso.001" → "Torso"
                if (!mapa.ContainsKey(nombre)) mapa[nombre] = t;
            }
            Transform B(string n) => mapa.TryGetValue(n, out Transform t) ? t : null;
            Cadera = B("Cadera"); Torso = B("Torso"); Cabeza = B("Cabeza");
            BrazoI = B("Brazo_I"); AntebrazoI = B("Antebrazo_I"); ManoI = B("Mano_I");
            BrazoD = B("Brazo_D"); AntebrazoD = B("Antebrazo_D"); ManoD = B("Mano_D");
            MusloI = B("Muslo_I"); PiernaI = B("Pierna_I"); PieI = B("Pie_I");
            MusloD = B("Muslo_D"); PiernaD = B("Pierna_D"); PieD = B("Pie_D");
            Capa = B("Capa");
            return Cadera != null && Torso != null && BrazoD != null && MusloI != null && MusloD != null;
        }
    }

    /// <summary>
    /// Construye los modelos de personaje.
    /// 1) Si existe un modelo de Blender en Resources (Modelos/Humanos/{Arquetipo} o Modelos/Guardianes/{Elemento}),
    ///    lo instancia, lo normaliza a la altura correcta y localiza sus huesos por nombre.
    /// 2) Si no existe, construye un modelo articulado con primitivas (mismo esqueleto) para que el juego
    ///    sea jugable siempre. Ver Docs/10_Modelos_Blender_y_Animacion_Procedural.md.
    /// </summary>
    public static class ConstructorModelosPersonajes
    {
        private const int CapaPrimaria = 0;
        private const int CapaSecundaria = 1;
        private const int CapaEmisiva = 2;

        // =====================================================================
        // Utilidades
        // =====================================================================

        private static Transform Articulacion(Transform padre, string nombre, Vector3 posicionLocal)
        {
            GameObject go = new GameObject(nombre);
            go.transform.SetParent(padre, false);
            go.transform.localPosition = posicionLocal;
            return go.transform;
        }

        private static Renderer Pieza(Transform padre, PrimitiveType tipo, Vector3 posicion, Vector3 escala, Vector3 rotacion,
            ModeloVisualPersonaje modelo, int capa, string nombre = null)
        {
            GameObject pieza = GameObject.CreatePrimitive(tipo);
            pieza.name = nombre ?? $"Pieza {tipo}";
            pieza.transform.SetParent(padre, false);
            pieza.transform.localPosition = posicion;
            pieza.transform.localScale = escala;
            pieza.transform.localRotation = Quaternion.Euler(rotacion);
            Collider c = pieza.GetComponent<Collider>();
            if (c != null) Object.DestroyImmediate(c);
            Renderer r = pieza.GetComponent<Renderer>();
            modelo.RegistrarPieza(r, capa);
            return r;
        }

        /// <summary>Intenta cargar un modelo de Blender; lo escala a la altura deseada y quita sus colisionadores.</summary>
        private static GameObject InstanciarModeloImportado(string ruta, Transform raiz, float alturaDeseada, ModeloVisualPersonaje modelo)
        {
            GameObject prefab = Resources.Load<GameObject>(ruta);
            if (prefab == null) return null;
            GameObject instancia = Object.Instantiate(prefab, raiz, false);
            instancia.name = $"Modelo {prefab.name}";
            foreach (Collider c in instancia.GetComponentsInChildren<Collider>(true)) Object.DestroyImmediate(c);

            // 1) Orientación por marcadores (frente = +Z, arriba = +Y), independiente de la conversión de ejes del FBX.
            AlineadorModeloImportado.Alinear(instancia, raiz);

            // 2) Normalización: sea cual sea la escala del FBX, el modelo mide lo que debe y apoya los pies en el suelo.
            Bounds limites = CalcularLimites(instancia);
            if (limites.size.y > 0.01f)
            {
                float factor = alturaDeseada / limites.size.y;
                instancia.transform.localScale *= factor;
                limites = CalcularLimites(instancia);
                instancia.transform.position += new Vector3(0f, raiz.position.y - limites.min.y, 0f);
            }

            // 3) Esqueleto con ejes limpios para el AnimadorProcedural (si no tiene articulaciones, se descarta).
            Transform reconstruido = AlineadorModeloImportado.ReconstruirEsqueleto(instancia, raiz);
            if (reconstruido == null)
            {
                Object.Destroy(instancia);
                return null;
            }
            instancia = reconstruido.gameObject;
            instancia.name = $"Modelo {prefab.name}";
            MaterialesProcedurales.AplicarMaterialesPorNombre(instancia);
            // Las piezas emisivas (material "VBF_emisivo_*") se registran para que el brillo siga el color del
            // elemento actual (p. ej., el guardián del Nexus al rotar de linaje). El resto conserva sus materiales.
            foreach (Renderer r in instancia.GetComponentsInChildren<Renderer>(true))
            {
                if (r.sharedMaterial != null && r.sharedMaterial.name.ToLowerInvariant().Contains("emisivo")) modelo.RegistrarPieza(r, CapaEmisiva);
            }
            return instancia;
        }

        private static Bounds CalcularLimites(GameObject go)
        {
            Renderer[] rs = go.GetComponentsInChildren<Renderer>();
            if (rs.Length == 0) return new Bounds(go.transform.position, Vector3.zero);
            Bounds b = rs[0].bounds;
            for (int i = 1; i < rs.Length; i++) b.Encapsulate(rs[i].bounds);
            return b;
        }

        private static void AnclarArma(EsqueletoProcedural esqueleto, DatosArmaModular arma)
        {
            if (esqueleto.ManoD == null || arma == null) return;
            ConstructorModelosArmas.Construir(arma.tipo, esqueleto.ManoD, false);
            if (arma.tipo == TipoArmaModular.MazaEscudo && esqueleto.ManoI != null)
            {
                ConstructorModelosArmas.ConstruirEscudo(esqueleto.ManoI, false);
            }
            if (arma.tipo == TipoArmaModular.ArsenalBiomecanico && esqueleto.AntebrazoI != null)
            {
                ConstructorModelosArmas.ConstruirCanonPulso(esqueleto.AntebrazoI, false);
            }
        }

        // =====================================================================
        // Humanos (los 5 arquetipos)
        // =====================================================================

        public static ModeloVisualPersonaje ConstruirHumano(Transform raiz, DatosArquetipo arquetipo, DatosArmaModular arma, bool esJugadorLocal)
        {
            GameObject contenedor = new GameObject("Modelo Humano");
            contenedor.transform.SetParent(raiz, false);
            ModeloVisualPersonaje modelo = contenedor.AddComponent<ModeloVisualPersonaje>();
            EsqueletoProcedural esqueleto = contenedor.AddComponent<EsqueletoProcedural>();

            GameObject importado = InstanciarModeloImportado($"Modelos/Humanos/{arquetipo.tipo}", contenedor.transform, 1.8f, modelo);
            if (importado == null || !esqueleto.AsignarPorNombre(importado.transform))
            {
                if (importado != null) Object.DestroyImmediate(importado);
                ConstruirHumanoPrimitivo(contenedor.transform, esqueleto, modelo, arquetipo);
            }

            AnclarArma(esqueleto, arma);
            return modelo;
        }

        private static void ConstruirHumanoPrimitivo(Transform t, EsqueletoProcedural e, ModeloVisualPersonaje m, DatosArquetipo a)
        {
            bool pesado = a.tipo == TipoArquetipo.Bastion;
            float ancho = pesado ? 1.18f : a.tipo == TipoArquetipo.Paladin ? 1.08f : 1f;

            e.Cadera = Articulacion(t, "Cadera", new Vector3(0f, 0.95f, 0f));
            Pieza(e.Cadera, PrimitiveType.Capsule, new Vector3(0f, 0.02f, 0f), new Vector3(0.36f * ancho, 0.14f, 0.24f), new Vector3(0f, 0f, 90f), m, CapaSecundaria, "Pelvis");

            e.Torso = Articulacion(e.Cadera, "Torso", new Vector3(0f, 0.06f, 0f));
            Pieza(e.Torso, PrimitiveType.Capsule, new Vector3(0f, 0.28f, 0f), new Vector3(0.42f * ancho, 0.3f, 0.26f), Vector3.zero, m, CapaPrimaria, "Pecho");
            Pieza(e.Torso, PrimitiveType.Cube, new Vector3(0f, 0.36f, 0.12f), new Vector3(0.36f * ancho, 0.3f, 0.06f), Vector3.zero, m, CapaPrimaria, "Peto");
            e.Cabeza = Articulacion(e.Torso, "Cabeza", new Vector3(0f, 0.6f, 0f));
            Pieza(e.Cabeza, PrimitiveType.Cylinder, new Vector3(0f, 0.04f, 0f), new Vector3(0.1f, 0.05f, 0.1f), Vector3.zero, m, CapaSecundaria, "Cuello");
            Pieza(e.Cabeza, PrimitiveType.Sphere, new Vector3(0f, 0.17f, 0.01f), new Vector3(0.22f, 0.26f, 0.24f), Vector3.zero, m, CapaPrimaria, "Craneo");

            foreach (int lado in new[] { -1, 1 })
            {
                Transform brazo = Articulacion(e.Torso, lado < 0 ? "Brazo_I" : "Brazo_D", new Vector3(0.23f * ancho * lado, 0.5f, 0f));
                Pieza(brazo, PrimitiveType.Sphere, new Vector3(0.03f * lado, 0.02f, 0f), new Vector3(0.2f, 0.16f, 0.2f) * (pesado ? 1.3f : 1f), Vector3.zero, m, CapaSecundaria, "Hombrera");
                Pieza(brazo, PrimitiveType.Capsule, new Vector3(0f, -0.15f, 0f), new Vector3(0.11f, 0.16f, 0.11f), Vector3.zero, m, CapaPrimaria, "Biceps");
                Transform antebrazo = Articulacion(brazo, lado < 0 ? "Antebrazo_I" : "Antebrazo_D", new Vector3(0f, -0.3f, 0f));
                Pieza(antebrazo, PrimitiveType.Capsule, new Vector3(0f, -0.13f, 0f), new Vector3(0.1f, 0.14f, 0.1f), Vector3.zero, m, CapaSecundaria, "Antebrazo");
                Transform mano = Articulacion(antebrazo, lado < 0 ? "Mano_I" : "Mano_D", new Vector3(0f, -0.28f, 0f));
                Pieza(mano, PrimitiveType.Cube, new Vector3(0f, -0.04f, 0f), new Vector3(0.08f, 0.1f, 0.06f), Vector3.zero, m, CapaSecundaria, "Guantelete");
                if (lado < 0) { e.BrazoI = brazo; e.AntebrazoI = antebrazo; e.ManoI = mano; }
                else { e.BrazoD = brazo; e.AntebrazoD = antebrazo; e.ManoD = mano; }
            }

            foreach (int lado in new[] { -1, 1 })
            {
                Transform muslo = Articulacion(e.Cadera, lado < 0 ? "Muslo_I" : "Muslo_D", new Vector3(0.11f * ancho * lado, -0.02f, 0f));
                Pieza(muslo, PrimitiveType.Capsule, new Vector3(0f, -0.22f, 0f), new Vector3(0.15f, 0.23f, 0.15f), Vector3.zero, m, CapaSecundaria, "Muslo");
                Transform pierna = Articulacion(muslo, lado < 0 ? "Pierna_I" : "Pierna_D", new Vector3(0f, -0.45f, 0f));
                Pieza(pierna, PrimitiveType.Capsule, new Vector3(0f, -0.21f, 0f), new Vector3(0.13f, 0.22f, 0.13f), Vector3.zero, m, CapaSecundaria, "Greba");
                Pieza(pierna, PrimitiveType.Sphere, new Vector3(0f, 0f, 0.05f), new Vector3(0.12f, 0.1f, 0.1f), Vector3.zero, m, CapaPrimaria, "Rodillera");
                Transform pie = Articulacion(pierna, lado < 0 ? "Pie_I" : "Pie_D", new Vector3(0f, -0.44f, 0f));
                Pieza(pie, PrimitiveType.Cube, new Vector3(0f, -0.03f, 0.05f), new Vector3(0.11f, 0.08f, 0.26f), Vector3.zero, m, CapaSecundaria, "Bota");
                if (lado < 0) { e.MusloI = muslo; e.PiernaI = pierna; e.PieI = pie; }
                else { e.MusloD = muslo; e.PiernaD = pierna; e.PieD = pie; }
            }

            Color acento = a.colorAcento;
            List<Renderer> acentos = new List<Renderer>();
            switch (a.tipo)
            {
                case TipoArquetipo.Cazador:
                    acentos.Add(Pieza(e.Cabeza, PrimitiveType.Cube, new Vector3(0f, 0.19f, 0.12f), new Vector3(0.16f, 0.03f, 0.03f), Vector3.zero, m, CapaSecundaria, "Visor"));
                    Pieza(e.Torso, PrimitiveType.Cylinder, new Vector3(0.08f, 0.38f, -0.2f), new Vector3(0.1f, 0.25f, 0.1f), new Vector3(0f, 0f, -15f), m, CapaSecundaria, "Carcaj");
                    Pieza(e.Torso, PrimitiveType.Cube, new Vector3(0f, -0.12f, 0f), new Vector3(0.46f, 0.25f, 0.3f), Vector3.zero, m, CapaPrimaria, "Faldon del gambeson");
                    break;
                case TipoArquetipo.Paladin:
                    acentos.Add(Pieza(e.Torso, PrimitiveType.Cylinder, new Vector3(0f, 0.38f, 0.16f), new Vector3(0.14f, 0.01f, 0.14f), new Vector3(90f, 0f, 0f), m, CapaSecundaria, "Emblema solar"));
                    e.Capa = Articulacion(e.Torso, "Capa", new Vector3(0f, 0.55f, -0.15f));
                    Pieza(e.Capa, PrimitiveType.Cube, new Vector3(0f, -0.48f, -0.02f), new Vector3(0.5f, 0.95f, 0.03f), Vector3.zero, m, CapaPrimaria, "Capa clara");
                    break;
                case TipoArquetipo.Bastion:
                    Pieza(e.Torso, PrimitiveType.Cube, new Vector3(0f, 0.3f, 0.16f), new Vector3(0.06f, 0.5f, 0.02f), new Vector3(0f, 0f, 35f), m, CapaSecundaria, "Correa cruzada");
                    Pieza(e.Torso, PrimitiveType.Cube, new Vector3(0f, -0.15f, 0f), new Vector3(0.5f, 0.3f, 0.34f), Vector3.zero, m, CapaSecundaria, "Faldon claro");
                    Pieza(e.Cabeza, PrimitiveType.Sphere, new Vector3(0f, 0.08f, 0.1f), new Vector3(0.2f, 0.14f, 0.12f), Vector3.zero, m, CapaSecundaria, "Barba");
                    break;
                case TipoArquetipo.Tirador:
                    Pieza(e.Torso, PrimitiveType.Cube, new Vector3(0f, -0.22f, -0.02f), new Vector3(0.46f, 0.55f, 0.3f), Vector3.zero, m, CapaPrimaria, "Faldones del abrigo");
                    Pieza(e.Torso, PrimitiveType.Cube, new Vector3(0f, 0.3f, 0.15f), new Vector3(0.05f, 0.55f, 0.02f), new Vector3(0f, 0f, -30f), m, CapaSecundaria, "Bandolera");
                    Pieza(e.Cabeza, PrimitiveType.Cylinder, new Vector3(0f, 0.31f, 0f), new Vector3(0.36f, 0.02f, 0.36f), Vector3.zero, m, CapaSecundaria, "Ala del sombrero");
                    break;
                default:
                    acentos.Add(Pieza(e.Torso, PrimitiveType.Sphere, new Vector3(0f, 0.36f, 0.15f), new Vector3(0.12f, 0.12f, 0.06f), Vector3.zero, m, CapaSecundaria, "Nucleo magenta"));
                    acentos.Add(Pieza(e.Cabeza, PrimitiveType.Sphere, new Vector3(-0.07f, 0.24f, 0.1f), new Vector3(0.05f, 0.05f, 0.03f), Vector3.zero, m, CapaSecundaria, "Lente optico"));
                    for (int i = 0; i < 5; i++)
                    {
                        Pieza(e.Torso, PrimitiveType.Cube, new Vector3(0f, 0.08f + i * 0.11f, -0.15f), new Vector3(0.07f, 0.06f, 0.06f), new Vector3(20f, 0f, 0f), m, CapaSecundaria, "Espina");
                    }
                    break;
            }

            m.AplicarPaleta(a.colorPrimario, a.colorSecundario);
            foreach (Renderer r in acentos) MaterialesProcedurales.AplicarColor(r, acento, acento * 2.5f);
        }

        // =====================================================================
        // Guardianes (linajes de Madera, Agua, Fuego, Metal y Tierra-Forja; Nexus)
        // =====================================================================

        public static ModeloVisualPersonaje ConstruirGuardian(Transform raiz, TipoElemento elemento, RolGuardian rol, Color primario, Color secundario)
        {
            GameObject contenedor = new GameObject("Modelo Guardián Elemental");
            contenedor.transform.SetParent(raiz, false);
            ModeloVisualPersonaje modelo = contenedor.AddComponent<ModeloVisualPersonaje>();
            EsqueletoProcedural esqueleto = contenedor.AddComponent<EsqueletoProcedural>();
            esqueleto.EsGuardian = true;
            esqueleto.Escala = 1.45f;
            float altura = rol == RolGuardian.Artilleria ? 2.85f : rol == RolGuardian.Vanguardia ? 2.75f : 2.65f;

            GameObject importado = InstanciarModeloImportado($"Modelos/Guardianes/{elemento}", contenedor.transform, altura, modelo);
            if (importado == null || !esqueleto.AsignarPorNombre(importado.transform))
            {
                if (importado != null) Object.DestroyImmediate(importado);
                ConstruirGuardianPrimitivo(contenedor.transform, esqueleto, modelo, elemento, rol);
                modelo.AplicarPaleta(primario, secundario);
            }

            if (elemento == TipoElemento.Metal)
            {
                contenedor.AddComponent<LevitacionMagnetica>();
            }
            return modelo;
        }

        private static void ConstruirGuardianPrimitivo(Transform t, EsqueletoProcedural e, ModeloVisualPersonaje m, TipoElemento elemento, RolGuardian rol)
        {
            float k = rol == RolGuardian.Vanguardia ? 1.12f : rol == RolGuardian.Artilleria ? 0.92f : 1f;

            e.Cadera = Articulacion(t, "Cadera", new Vector3(0f, 1.15f, 0f));
            Pieza(e.Cadera, PrimitiveType.Capsule, Vector3.zero, new Vector3(0.62f * k, 0.22f, 0.45f), new Vector3(0f, 0f, 90f), m, CapaSecundaria, "Pelvis");

            e.Torso = Articulacion(e.Cadera, "Torso", new Vector3(0f, 0.1f, 0f));
            Pieza(e.Torso, PrimitiveType.Capsule, new Vector3(0f, 0.5f, 0f), new Vector3(1.0f * k, 0.6f, 0.7f), Vector3.zero, m, CapaSecundaria, "Tronco");
            Pieza(e.Torso, PrimitiveType.Sphere, new Vector3(0f, 0.55f, 0.33f), new Vector3(0.3f, 0.3f, 0.18f), Vector3.zero, m, CapaEmisiva, "Nucleo del pecho");
            e.Cabeza = Articulacion(e.Torso, "Cabeza", new Vector3(0f, 1.02f, 0.05f));
            Pieza(e.Cabeza, PrimitiveType.Sphere, new Vector3(0f, 0.2f, 0f), new Vector3(0.48f, 0.46f, 0.48f), Vector3.zero, m, CapaSecundaria, "Cabeza");
            Pieza(e.Cabeza, PrimitiveType.Sphere, new Vector3(-0.1f, 0.24f, 0.21f), new Vector3(0.08f, 0.08f, 0.05f), Vector3.zero, m, CapaEmisiva, "Ojo");
            Pieza(e.Cabeza, PrimitiveType.Sphere, new Vector3(0.1f, 0.24f, 0.21f), new Vector3(0.08f, 0.08f, 0.05f), Vector3.zero, m, CapaEmisiva, "Ojo");

            foreach (int lado in new[] { -1, 1 })
            {
                Transform brazo = Articulacion(e.Torso, lado < 0 ? "Brazo_I" : "Brazo_D", new Vector3(0.68f * k * lado, 0.88f, 0f));
                Pieza(brazo, PrimitiveType.Sphere, new Vector3(0.05f * lado, 0.05f, 0f), new Vector3(0.66f, 0.56f, 0.66f) * k, Vector3.zero, m, CapaPrimaria, "Hombro masivo");
                Pieza(brazo, PrimitiveType.Cube, new Vector3(0.05f * lado, 0.12f, 0.3f), new Vector3(0.28f, 0.07f, 0.07f), new Vector3(0f, 0f, -25f * lado), m, CapaEmisiva, "Runa");
                Pieza(brazo, PrimitiveType.Capsule, new Vector3(0f, -0.3f, 0f), new Vector3(0.32f, 0.3f, 0.32f) * k, Vector3.zero, m, CapaSecundaria, "Brazo");
                Transform antebrazo = Articulacion(brazo, lado < 0 ? "Antebrazo_I" : "Antebrazo_D", new Vector3(0f, -0.6f, 0f));
                Pieza(antebrazo, PrimitiveType.Capsule, new Vector3(0f, -0.28f, 0f), new Vector3(0.3f, 0.3f, 0.3f) * k, Vector3.zero, m, CapaSecundaria, "Antebrazo");
                Transform mano = Articulacion(antebrazo, lado < 0 ? "Mano_I" : "Mano_D", new Vector3(0f, -0.58f, 0f));
                Pieza(mano, PrimitiveType.Sphere, new Vector3(0f, -0.08f, 0.05f), new Vector3(0.46f, 0.46f, 0.46f) * k, Vector3.zero, m, CapaPrimaria, "Maza elemental");
                if (lado < 0) { e.BrazoI = brazo; e.AntebrazoI = antebrazo; e.ManoI = mano; }
                else { e.BrazoD = brazo; e.AntebrazoD = antebrazo; e.ManoD = mano; }
            }

            foreach (int lado in new[] { -1, 1 })
            {
                Transform muslo = Articulacion(e.Cadera, lado < 0 ? "Muslo_I" : "Muslo_D", new Vector3(0.28f * k * lado, -0.05f, 0f));
                Pieza(muslo, PrimitiveType.Capsule, new Vector3(0f, -0.27f, 0f), new Vector3(0.36f, 0.3f, 0.36f) * k, Vector3.zero, m, CapaSecundaria, "Muslo");
                Transform pierna = Articulacion(muslo, lado < 0 ? "Pierna_I" : "Pierna_D", new Vector3(0f, -0.55f, 0f));
                Pieza(pierna, PrimitiveType.Capsule, new Vector3(0f, -0.25f, 0f), new Vector3(0.32f, 0.28f, 0.32f) * k, Vector3.zero, m, CapaSecundaria, "Pierna");
                Transform pie = Articulacion(pierna, lado < 0 ? "Pie_I" : "Pie_D", new Vector3(0f, -0.52f, 0f));
                Pieza(pie, PrimitiveType.Cube, new Vector3(0f, -0.02f, 0.1f), new Vector3(0.34f, 0.14f, 0.5f) * k, Vector3.zero, m, CapaPrimaria, "Pie");
                if (lado < 0) { e.MusloI = muslo; e.PiernaI = pierna; e.PieI = pie; }
                else { e.MusloD = muslo; e.PiernaD = pierna; e.PieD = pie; }
            }

            switch (elemento)
            {
                case TipoElemento.Arbol:
                    Pieza(e.Cabeza, PrimitiveType.Cylinder, new Vector3(-0.22f, 0.55f, 0f), new Vector3(0.08f, 0.35f, 0.08f), new Vector3(0f, 0f, 25f), m, CapaSecundaria, "Cuerno rama");
                    Pieza(e.Cabeza, PrimitiveType.Cylinder, new Vector3(0.25f, 0.65f, 0f), new Vector3(0.07f, 0.45f, 0.07f), new Vector3(0f, 0f, -30f), m, CapaSecundaria, "Cuerno rama");
                    Pieza(e.Cabeza, PrimitiveType.Sphere, new Vector3(0f, 0.42f, 0f), new Vector3(0.6f, 0.15f, 0.6f), Vector3.zero, m, CapaEmisiva, "Corona de musgo");
                    break;
                case TipoElemento.Agua:
                    Pieza(e.Cabeza, PrimitiveType.Capsule, new Vector3(0f, 0.7f, -0.1f), new Vector3(0.25f, 0.45f, 0.25f), new Vector3(-15f, 0f, 0f), m, CapaEmisiva, "Cascada invertida");
                    Pieza(e.Torso, PrimitiveType.Cylinder, new Vector3(0f, 0.5f, 0f), new Vector3(1.3f, 0.03f, 1.3f), Vector3.zero, m, CapaPrimaria, "Anillo de nacar");
                    break;
                case TipoElemento.Tierra:
                    Pieza(e.Torso, PrimitiveType.Cube, new Vector3(0f, 0.55f, 0.36f), new Vector3(0.9f, 0.7f, 0.1f), Vector3.zero, m, CapaPrimaria, "Placa clara");
                    for (int r = 0; r < 4; r++)
                    {
                        Pieza(e.Torso, PrimitiveType.Sphere, new Vector3(-0.35f + r * 0.23f, 0.87f, 0.42f), Vector3.one * 0.07f, Vector3.zero, m, CapaSecundaria, "Remache");
                    }
                    Pieza(e.BrazoI, PrimitiveType.Cylinder, new Vector3(0f, 0.55f, -0.2f), new Vector3(0.28f, 0.45f, 0.28f), Vector3.zero, m, CapaSecundaria, "Chimenea");
                    Pieza(e.BrazoI, PrimitiveType.Sphere, new Vector3(0f, 1.02f, -0.2f), new Vector3(0.24f, 0.18f, 0.24f), Vector3.zero, m, CapaEmisiva, "Llama de chimenea");
                    Pieza(e.AntebrazoD, PrimitiveType.Sphere, new Vector3(0f, -0.3f, 0.12f), Vector3.one * 0.26f, Vector3.zero, m, CapaEmisiva, "Nucleo incandescente");
                    break;
                case TipoElemento.Metal:
                    Pieza(e.Torso, PrimitiveType.Cylinder, new Vector3(0f, 0.6f, 0f), new Vector3(1.6f, 0.02f, 1.6f), new Vector3(0f, 0f, 12f), m, CapaEmisiva, "Anillo de resonancia");
                    Pieza(e.Torso, PrimitiveType.Cylinder, new Vector3(0f, 0.3f, 0f), new Vector3(1.3f, 0.02f, 1.3f), new Vector3(0f, 0f, -10f), m, CapaEmisiva, "Anillo de resonancia");
                    Pieza(e.Cabeza, PrimitiveType.Cube, new Vector3(0f, 0.24f, 0.24f), new Vector3(0.36f, 0.06f, 0.05f), Vector3.zero, m, CapaEmisiva, "Lente");
                    break;
                default:
                    Pieza(e.Cabeza, PrimitiveType.Sphere, new Vector3(0f, 0.45f, -0.15f), new Vector3(0.65f, 0.5f, 0.55f), Vector3.zero, m, CapaEmisiva, "Melena de llamas");
                    Pieza(e.Torso, PrimitiveType.Cube, new Vector3(0.2f, 0.4f, 0.36f), new Vector3(0.05f, 0.5f, 0.05f), new Vector3(0f, 0f, 20f), m, CapaEmisiva, "Grieta de magma");
                    break;
            }
        }
    }

    /// <summary>
    /// Levitación magnética: eleva el modelo unos centímetros y lo hace oscilar con suavidad.
    /// Lo usan los guardianes de Metal y las torres de las Agujas de Aleación.
    /// </summary>
    public class LevitacionMagnetica : MonoBehaviour
    {
        [SerializeField] private float alturaBase = 0.12f;
        [SerializeField] private float amplitud = 0.04f;
        [SerializeField] private float frecuencia = 0.8f;

        private Vector3 posicionInicial;
        private float fase;
        private float suspendidaHasta = -1f;
        private float alturaActual;

        public void Configurar(float altura, float amplitudOscilacion, float frecuenciaOscilacion)
        {
            alturaBase = altura;
            amplitud = amplitudOscilacion;
            frecuencia = frecuenciaOscilacion;
        }

        /// <summary>Apaga la levitación durante unos segundos (el modelo baja al suelo y vuelve a subir).</summary>
        public void Suspender(float segundos)
        {
            suspendidaHasta = Time.time + segundos;
        }

        private void Start()
        {
            posicionInicial = transform.localPosition;
            fase = Random.Range(0f, Mathf.PI * 2f);
            alturaActual = alturaBase;
        }

        private void Update()
        {
            float objetivo = Time.time < suspendidaHasta ? 0f : alturaBase + Mathf.Sin(Time.time * frecuencia * Mathf.PI * 2f + fase) * amplitud;
            alturaActual = Mathf.MoveTowards(alturaActual, objetivo, Time.deltaTime * 1.5f);
            transform.localPosition = posicionInicial + new Vector3(0f, alturaActual, 0f);
        }
    }
}
