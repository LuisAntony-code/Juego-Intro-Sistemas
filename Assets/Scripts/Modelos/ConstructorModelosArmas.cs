using UnityEngine;

namespace VoidformBioForge
{
    /// <summary>
    /// Modelos de las 5 armas modulares y del escudo.
    /// Convención: la empuñadura está en el origen y el arma "apunta" hacia +Z local (hoja, cañón o flecha).
    /// En la mano de un personaje se gira 90° en X para seguir la dirección del antebrazo; en primera persona
    /// se coloca sin giro delante de la cámara.
    /// Si existe un modelo de Blender en Resources/Modelos/Armas/{Tipo} (o Escudo / CanonPulso), se usa ese;
    /// si no, se arma con primitivas. Los FBX los genera Herramientas/Blender/generar_modelos_voidform.py.
    /// </summary>
    public static class ConstructorModelosArmas
    {
        public static GameObject Construir(TipoArmaModular tipo, Transform padre, bool primeraPersona)
        {
            GameObject raiz = new GameObject($"Arma {tipo}");
            raiz.transform.SetParent(padre, false);
            raiz.transform.localRotation = primeraPersona ? Quaternion.identity : Quaternion.Euler(90f, 0f, 0f);

            if (InstanciarImportado($"Modelos/Armas/{tipo}", raiz, primeraPersona)) return raiz;

            switch (tipo)
            {
                case TipoArmaModular.ArcoCompuesto:
                    Pieza(raiz.transform, PrimitiveType.Cylinder, new Vector3(0f, 0f, 0.02f), new Vector3(0.035f, 0.1f, 0.035f), Vector3.zero, "cuero");
                    Pieza(raiz.transform, PrimitiveType.Capsule, new Vector3(0f, 0.32f, 0.07f), new Vector3(0.04f, 0.27f, 0.04f), new Vector3(-14f, 0f, 0f), "madera");
                    Pieza(raiz.transform, PrimitiveType.Capsule, new Vector3(0f, -0.32f, 0.07f), new Vector3(0.04f, 0.27f, 0.04f), new Vector3(14f, 0f, 0f), "madera");
                    Pieza(raiz.transform, PrimitiveType.Cylinder, new Vector3(0f, 0f, -0.06f), new Vector3(0.006f, 0.58f, 0.006f), Vector3.zero, "cuerda");
                    break;

                case TipoArmaModular.Mandoble:
                    Pieza(raiz.transform, PrimitiveType.Cylinder, new Vector3(0f, 0f, 0.05f), new Vector3(0.04f, 0.16f, 0.04f), new Vector3(90f, 0f, 0f), "cuero");
                    Pieza(raiz.transform, PrimitiveType.Sphere, new Vector3(0f, 0f, -0.13f), Vector3.one * 0.06f, Vector3.zero, "oro");
                    Pieza(raiz.transform, PrimitiveType.Cube, new Vector3(0f, 0f, 0.22f), new Vector3(0.3f, 0.035f, 0.04f), Vector3.zero, "oro");
                    Pieza(raiz.transform, PrimitiveType.Cube, new Vector3(0f, 0f, 0.82f), new Vector3(0.06f, 0.012f, 1.15f), Vector3.zero, "acero");
                    break;

                case TipoArmaModular.MazaEscudo:
                    Pieza(raiz.transform, PrimitiveType.Cylinder, new Vector3(0f, 0f, 0.22f), new Vector3(0.04f, 0.32f, 0.04f), new Vector3(90f, 0f, 0f), "madera");
                    Pieza(raiz.transform, PrimitiveType.Sphere, new Vector3(0f, 0f, 0.58f), Vector3.one * 0.2f, Vector3.zero, "hierro");
                    for (int i = 0; i < 6; i++)
                    {
                        Vector3 dir = Quaternion.Euler(0f, 0f, i * 60f) * Vector3.up;
                        Pieza(raiz.transform, PrimitiveType.Cube, new Vector3(0f, 0f, 0.58f) + dir * 0.1f, new Vector3(0.04f, 0.09f, 0.04f), new Vector3(0f, 0f, i * 60f), "hierro");
                    }
                    break;

                case TipoArmaModular.RifleLargo:
                    Pieza(raiz.transform, PrimitiveType.Cube, new Vector3(0f, -0.03f, -0.22f), new Vector3(0.05f, 0.12f, 0.32f), Vector3.zero, "madera");
                    Pieza(raiz.transform, PrimitiveType.Cube, new Vector3(0f, 0f, 0.12f), new Vector3(0.06f, 0.09f, 0.42f), Vector3.zero, "acero");
                    Pieza(raiz.transform, PrimitiveType.Cylinder, new Vector3(0f, 0.015f, 0.72f), new Vector3(0.028f, 0.42f, 0.028f), new Vector3(90f, 0f, 0f), "acero");
                    Pieza(raiz.transform, PrimitiveType.Cylinder, new Vector3(0f, 0.085f, 0.12f), new Vector3(0.035f, 0.17f, 0.035f), new Vector3(90f, 0f, 0f), "laton");
                    Pieza(raiz.transform, PrimitiveType.Cylinder, new Vector3(0f, 0.015f, 0.4f), new Vector3(0.045f, 0.06f, 0.045f), new Vector3(90f, 0f, 0f), "laton");
                    break;

                default:
                    // Cuchilla de antebrazo con filo emisivo magenta.
                    Pieza(raiz.transform, PrimitiveType.Cube, new Vector3(0f, 0f, -0.08f), new Vector3(0.09f, 0.07f, 0.28f), Vector3.zero, "acero");
                    GameObject filo = Pieza(raiz.transform, PrimitiveType.Cube, new Vector3(0.03f, 0f, 0.32f), new Vector3(0.015f, 0.07f, 0.6f), new Vector3(0f, 6f, 0f), "acero");
                    MaterialesProcedurales.AplicarColor(filo.GetComponent<Renderer>(), new Color(0.8f, 0.82f, 0.86f), new Color(1f, 0.2f, 0.65f) * 0.6f);
                    break;
            }

            PrepararSombras(raiz, primeraPersona);
            return raiz;
        }

        /// <summary>Escudo ovalado de acero con relieve (mano izquierda de la Maza con Escudo).</summary>
        public static GameObject ConstruirEscudo(Transform padre, bool primeraPersona)
        {
            GameObject raiz = new GameObject("Escudo barroco");
            raiz.transform.SetParent(padre, false);
            raiz.transform.localRotation = primeraPersona ? Quaternion.identity : Quaternion.Euler(90f, 0f, 0f);
            raiz.transform.localPosition = primeraPersona ? Vector3.zero : new Vector3(0f, -0.05f, 0f);
            if (InstanciarImportado("Modelos/Armas/Escudo", raiz, primeraPersona)) return raiz;
            Pieza(raiz.transform, PrimitiveType.Cylinder, new Vector3(0f, 0f, 0.06f), new Vector3(0.55f, 0.02f, 0.72f), new Vector3(90f, 0f, 0f), "acero");
            Pieza(raiz.transform, PrimitiveType.Cylinder, new Vector3(0f, 0f, 0.075f), new Vector3(0.6f, 0.012f, 0.77f), new Vector3(90f, 0f, 0f), "oro");
            Pieza(raiz.transform, PrimitiveType.Sphere, new Vector3(0f, 0f, 0.09f), new Vector3(0.16f, 0.16f, 0.07f), Vector3.zero, "oro");
            PrepararSombras(raiz, primeraPersona);
            return raiz;
        }

        /// <summary>Cañón de pulso acoplado al antebrazo izquierdo (Arsenal Biomecánico).</summary>
        public static GameObject ConstruirCanonPulso(Transform padre, bool primeraPersona)
        {
            GameObject raiz = new GameObject("Cañón de pulso");
            raiz.transform.SetParent(padre, false);
            raiz.transform.localRotation = primeraPersona ? Quaternion.identity : Quaternion.Euler(90f, 0f, 0f);
            raiz.transform.localPosition = primeraPersona ? Vector3.zero : new Vector3(0f, -0.15f, 0.06f);
            if (InstanciarImportado("Modelos/Armas/CanonPulso", raiz, primeraPersona)) return raiz;
            Pieza(raiz.transform, PrimitiveType.Cylinder, new Vector3(0f, 0f, 0.1f), new Vector3(0.09f, 0.16f, 0.09f), new Vector3(90f, 0f, 0f), "acero");
            GameObject boca = Pieza(raiz.transform, PrimitiveType.Cylinder, new Vector3(0f, 0f, 0.27f), new Vector3(0.07f, 0.02f, 0.07f), new Vector3(90f, 0f, 0f), "acero");
            MaterialesProcedurales.AplicarColor(boca.GetComponent<Renderer>(), new Color(1f, 0.3f, 0.75f), new Color(1f, 0.25f, 0.7f) * 2f);
            PrepararSombras(raiz, primeraPersona);
            return raiz;
        }

        /// <summary>Instancia un modelo de Blender (si existe) bajo la raíz del arma, sin colisionadores y con materiales realistas.</summary>
        private static bool InstanciarImportado(string ruta, GameObject raiz, bool primeraPersona)
        {
            GameObject prefab = Resources.Load<GameObject>(ruta);
            if (prefab == null) return false;
            GameObject modelo = Object.Instantiate(prefab, raiz.transform, false);
            foreach (Collider c in modelo.GetComponentsInChildren<Collider>(true)) Object.DestroyImmediate(c);
            AlineadorModeloImportado.Alinear(modelo, raiz.transform);
            // Seguridad de unidades: si el FBX llegó en centímetros (arma de decenas de metros), se corrige a metros.
            Renderer[] rs = modelo.GetComponentsInChildren<Renderer>();
            if (rs.Length > 0)
            {
                Bounds b = rs[0].bounds;
                for (int i = 1; i < rs.Length; i++) b.Encapsulate(rs[i].bounds);
                float escalaMundo = Mathf.Max(0.0001f, raiz.transform.lossyScale.x);
                if (b.size.magnitude / escalaMundo > 10f) modelo.transform.localScale *= 0.01f;
            }
            MaterialesProcedurales.AplicarMaterialesPorNombre(modelo);
            PrepararSombras(raiz, primeraPersona);
            return true;
        }

        private static GameObject Pieza(Transform padre, PrimitiveType tipo, Vector3 posicion, Vector3 escala, Vector3 rotacion, string superficie)
        {
            GameObject p = GameObject.CreatePrimitive(tipo);
            p.name = superficie;
            p.transform.SetParent(padre, false);
            p.transform.localPosition = posicion;
            p.transform.localScale = escala;
            p.transform.localRotation = Quaternion.Euler(rotacion);
            Object.DestroyImmediate(p.GetComponent<Collider>());
            MaterialesProcedurales.AplicarMaterialRealista(p.GetComponent<Renderer>(), ColorDeSuperficie(superficie), Color.black, superficie);
            return p;
        }

        private static Color ColorDeSuperficie(string superficie)
        {
            switch (superficie)
            {
                case "madera": return new Color(0.62f, 0.46f, 0.3f);
                case "cuero": return new Color(0.25f, 0.15f, 0.09f);
                case "oro": return new Color(1f, 0.78f, 0.36f);
                case "laton": return new Color(0.85f, 0.65f, 0.3f);
                case "hierro": return new Color(0.28f, 0.28f, 0.3f);
                case "cuerda": return new Color(0.85f, 0.82f, 0.72f);
                default: return new Color(0.78f, 0.8f, 0.84f);
            }
        }

        private static void PrepararSombras(GameObject raiz, bool primeraPersona)
        {
            if (!primeraPersona) return;
            foreach (Renderer r in raiz.GetComponentsInChildren<Renderer>())
            {
                r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                r.receiveShadows = false;
            }
        }
    }
}
