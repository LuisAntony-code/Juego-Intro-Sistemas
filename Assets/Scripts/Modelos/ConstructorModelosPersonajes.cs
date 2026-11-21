using System.Collections.Generic;
using UnityEngine;

namespace VoidformBioForge
{
    /// <summary>
    /// Referencias a las piezas visuales de un personaje para poder recolorearlas
    /// (por ejemplo, cuando Aethernum cambia de elemento o un traje cambia de color).
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
            foreach (Renderer r in piezasPrimarias) MaterialesProcedurales.AplicarColor(r, primario);
            foreach (Renderer r in piezasSecundarias) MaterialesProcedurales.AplicarColor(r, secundario);
            foreach (Renderer r in piezasEmisivas) MaterialesProcedurales.AplicarColor(r, primario, primario * 2.2f);
        }
    }

    /// <summary>
    /// Construye modelos provisionales ("greybox" estilizado) con primitivas de Unity para que el
    /// juego sea jugable desde el primer minuto. Cada modelo respeta la silueta de diseño:
    /// - Destructor: humano con exotraje, visor luminoso, mochila extractora y arma.
    /// - Guardián: coloso elemental 1,4 veces más grande, hombros masivos con runas emisivas,
    ///   corona o cresta según el elemento y núcleo brillante en el pecho.
    /// Los artistas pueden sustituirlos por modelos definitivos manteniendo el componente
    /// ModeloVisualPersonaje (ver Docs/04_Arte_Biomas_VFX_e_Interfaz.md).
    /// </summary>
    public static class ConstructorModelosPersonajes
    {
        private const int CapaPrimaria = 0;
        private const int CapaSecundaria = 1;
        private const int CapaEmisiva = 2;

        // ---------------- Nivel 1: pieza básica ----------------

        private static Renderer CrearPieza(Transform padre, PrimitiveType tipo, Vector3 posicion, Vector3 escala,
            Vector3 rotacion, ModeloVisualPersonaje modelo, int capa)
        {
            GameObject pieza = GameObject.CreatePrimitive(tipo);
            pieza.name = $"Pieza {tipo}";
            pieza.transform.SetParent(padre, false);
            pieza.transform.localPosition = posicion;
            pieza.transform.localScale = escala;
            pieza.transform.localRotation = Quaternion.Euler(rotacion);

            // Las piezas son solo visuales: el CharacterController ya es el colisionador del personaje.
            // Se usa DestroyImmediate para que el colisionador no exista ni un solo fotograma
            // (evita que el propio personaje choque con sus piezas o bloquee sus disparos).
            Collider c = pieza.GetComponent<Collider>();
            if (c != null) Object.DestroyImmediate(c);

            Renderer r = pieza.GetComponent<Renderer>();
            modelo.RegistrarPieza(r, capa);
            return r;
        }

        // ---------------- Nivel 2: humano Destructor ----------------

        public static ModeloVisualPersonaje ConstruirDestructor(Transform raiz, Color colorTraje, bool esJugadorLocal)
        {
            GameObject contenedor = new GameObject("Modelo Destructor");
            contenedor.transform.SetParent(raiz, false);
            ModeloVisualPersonaje modelo = contenedor.AddComponent<ModeloVisualPersonaje>();
            Transform t = contenedor.transform;

            Color oscuro = Color.Lerp(colorTraje, Color.black, 0.65f);

            // Torso, piernas y brazos del exotraje.
            CrearPieza(t, PrimitiveType.Capsule, new Vector3(0f, 1.15f, 0f), new Vector3(0.62f, 0.5f, 0.42f), Vector3.zero, modelo, CapaPrimaria);
            CrearPieza(t, PrimitiveType.Capsule, new Vector3(-0.17f, 0.45f, 0f), new Vector3(0.22f, 0.45f, 0.22f), Vector3.zero, modelo, CapaSecundaria);
            CrearPieza(t, PrimitiveType.Capsule, new Vector3(0.17f, 0.45f, 0f), new Vector3(0.22f, 0.45f, 0.22f), Vector3.zero, modelo, CapaSecundaria);
            CrearPieza(t, PrimitiveType.Capsule, new Vector3(-0.42f, 1.15f, 0.05f), new Vector3(0.17f, 0.36f, 0.17f), new Vector3(15f, 0f, 8f), modelo, CapaSecundaria);
            CrearPieza(t, PrimitiveType.Capsule, new Vector3(0.42f, 1.15f, 0.05f), new Vector3(0.17f, 0.36f, 0.17f), new Vector3(15f, 0f, -8f), modelo, CapaSecundaria);

            // Casco y visor luminoso (emisivo).
            CrearPieza(t, PrimitiveType.Sphere, new Vector3(0f, 1.68f, 0f), new Vector3(0.38f, 0.4f, 0.4f), Vector3.zero, modelo, CapaPrimaria);
            CrearPieza(t, PrimitiveType.Cube, new Vector3(0f, 1.7f, 0.17f), new Vector3(0.3f, 0.09f, 0.1f), Vector3.zero, modelo, CapaEmisiva);

            // Mochila extractora con tanque de energía (emisivo) — símbolo del extractivismo.
            CrearPieza(t, PrimitiveType.Cube, new Vector3(0f, 1.2f, -0.3f), new Vector3(0.45f, 0.55f, 0.22f), Vector3.zero, modelo, CapaSecundaria);
            CrearPieza(t, PrimitiveType.Cylinder, new Vector3(0f, 1.25f, -0.43f), new Vector3(0.16f, 0.22f, 0.16f), Vector3.zero, modelo, CapaEmisiva);

            // Arma (solo visible para otros; el jugador local ve en primera persona y no la necesita).
            if (!esJugadorLocal)
            {
                CrearPieza(t, PrimitiveType.Cube, new Vector3(0.3f, 1.2f, 0.45f), new Vector3(0.09f, 0.12f, 0.75f), Vector3.zero, modelo, CapaSecundaria);
            }

            modelo.AplicarPaleta(colorTraje, oscuro);
            return modelo;
        }

        // ---------------- Nivel 3: guardián elemental ----------------

        public static ModeloVisualPersonaje ConstruirGuardian(Transform raiz, TipoElemento elemento, Color primario, Color secundario)
        {
            GameObject contenedor = new GameObject("Modelo Guardián Elemental");
            contenedor.transform.SetParent(raiz, false);
            ModeloVisualPersonaje modelo = contenedor.AddComponent<ModeloVisualPersonaje>();
            Transform t = contenedor.transform;

            // Cuerpo masivo (más ancho arriba que abajo: silueta de coloso).
            CrearPieza(t, PrimitiveType.Capsule, new Vector3(0f, 1.45f, 0f), new Vector3(1.05f, 0.75f, 0.75f), Vector3.zero, modelo, CapaSecundaria);
            CrearPieza(t, PrimitiveType.Capsule, new Vector3(-0.3f, 0.55f, 0f), new Vector3(0.38f, 0.55f, 0.38f), Vector3.zero, modelo, CapaSecundaria);
            CrearPieza(t, PrimitiveType.Capsule, new Vector3(0.3f, 0.55f, 0f), new Vector3(0.38f, 0.55f, 0.38f), Vector3.zero, modelo, CapaSecundaria);

            // Hombros con runas emisivas.
            CrearPieza(t, PrimitiveType.Sphere, new Vector3(-0.72f, 2f, 0f), new Vector3(0.7f, 0.6f, 0.7f), Vector3.zero, modelo, CapaPrimaria);
            CrearPieza(t, PrimitiveType.Sphere, new Vector3(0.72f, 2f, 0f), new Vector3(0.7f, 0.6f, 0.7f), Vector3.zero, modelo, CapaPrimaria);
            CrearPieza(t, PrimitiveType.Cube, new Vector3(-0.72f, 2.05f, 0.3f), new Vector3(0.3f, 0.08f, 0.08f), new Vector3(0f, 0f, 25f), modelo, CapaEmisiva);
            CrearPieza(t, PrimitiveType.Cube, new Vector3(0.72f, 2.05f, 0.3f), new Vector3(0.3f, 0.08f, 0.08f), new Vector3(0f, 0f, -25f), modelo, CapaEmisiva);

            // Brazos largos con "mazas" elementales.
            CrearPieza(t, PrimitiveType.Capsule, new Vector3(-0.95f, 1.35f, 0.1f), new Vector3(0.32f, 0.6f, 0.32f), new Vector3(10f, 0f, 10f), modelo, CapaSecundaria);
            CrearPieza(t, PrimitiveType.Capsule, new Vector3(0.95f, 1.35f, 0.1f), new Vector3(0.32f, 0.6f, 0.32f), new Vector3(10f, 0f, -10f), modelo, CapaSecundaria);
            CrearPieza(t, PrimitiveType.Sphere, new Vector3(-1.05f, 0.7f, 0.2f), new Vector3(0.48f, 0.48f, 0.48f), Vector3.zero, modelo, CapaPrimaria);
            CrearPieza(t, PrimitiveType.Sphere, new Vector3(1.05f, 0.7f, 0.2f), new Vector3(0.48f, 0.48f, 0.48f), Vector3.zero, modelo, CapaPrimaria);

            // Cabeza y núcleo del pecho (corazón elemental).
            CrearPieza(t, PrimitiveType.Sphere, new Vector3(0f, 2.35f, 0.05f), new Vector3(0.5f, 0.5f, 0.5f), Vector3.zero, modelo, CapaSecundaria);
            CrearPieza(t, PrimitiveType.Sphere, new Vector3(-0.1f, 2.4f, 0.26f), new Vector3(0.09f, 0.09f, 0.05f), Vector3.zero, modelo, CapaEmisiva);
            CrearPieza(t, PrimitiveType.Sphere, new Vector3(0.1f, 2.4f, 0.26f), new Vector3(0.09f, 0.09f, 0.05f), Vector3.zero, modelo, CapaEmisiva);
            CrearPieza(t, PrimitiveType.Sphere, new Vector3(0f, 1.6f, 0.38f), new Vector3(0.32f, 0.32f, 0.2f), Vector3.zero, modelo, CapaEmisiva);

            // Rasgo distintivo de cada elemento.
            switch (elemento)
            {
                case TipoElemento.Arbol:
                    // Cuernos-rama asimétricos y corona de musgo.
                    CrearPieza(t, PrimitiveType.Cylinder, new Vector3(-0.25f, 2.75f, 0f), new Vector3(0.08f, 0.35f, 0.08f), new Vector3(0f, 0f, 25f), modelo, CapaSecundaria);
                    CrearPieza(t, PrimitiveType.Cylinder, new Vector3(0.28f, 2.85f, 0f), new Vector3(0.07f, 0.45f, 0.07f), new Vector3(0f, 0f, -30f), modelo, CapaSecundaria);
                    CrearPieza(t, PrimitiveType.Sphere, new Vector3(0f, 2.62f, 0f), new Vector3(0.6f, 0.15f, 0.6f), Vector3.zero, modelo, CapaEmisiva);
                    break;
                case TipoElemento.Agua:
                    // Cascada invertida en la cabeza y anillo de nácar.
                    CrearPieza(t, PrimitiveType.Capsule, new Vector3(0f, 2.9f, -0.1f), new Vector3(0.25f, 0.45f, 0.25f), new Vector3(-15f, 0f, 0f), modelo, CapaEmisiva);
                    CrearPieza(t, PrimitiveType.Cylinder, new Vector3(0f, 1.45f, 0f), new Vector3(1.3f, 0.03f, 1.3f), Vector3.zero, modelo, CapaPrimaria);
                    break;
                case TipoElemento.Tierra:
                    // Estratos de acantilado en la espalda.
                    CrearPieza(t, PrimitiveType.Cube, new Vector3(0f, 1.9f, -0.45f), new Vector3(1.1f, 0.35f, 0.3f), new Vector3(10f, 0f, 0f), modelo, CapaPrimaria);
                    CrearPieza(t, PrimitiveType.Cube, new Vector3(0f, 1.5f, -0.5f), new Vector3(0.9f, 0.3f, 0.3f), new Vector3(-8f, 0f, 0f), modelo, CapaPrimaria);
                    CrearPieza(t, PrimitiveType.Cube, new Vector3(0f, 1.7f, -0.62f), new Vector3(0.6f, 0.05f, 0.05f), Vector3.zero, modelo, CapaEmisiva);
                    break;
                default:
                    // Melena de llamas y grietas de magma.
                    CrearPieza(t, PrimitiveType.Sphere, new Vector3(0f, 2.65f, -0.15f), new Vector3(0.65f, 0.5f, 0.55f), Vector3.zero, modelo, CapaEmisiva);
                    CrearPieza(t, PrimitiveType.Cube, new Vector3(0.2f, 1.3f, 0.36f), new Vector3(0.05f, 0.5f, 0.05f), new Vector3(0f, 0f, 20f), modelo, CapaEmisiva);
                    CrearPieza(t, PrimitiveType.Cube, new Vector3(-0.25f, 1.2f, 0.36f), new Vector3(0.05f, 0.4f, 0.05f), new Vector3(0f, 0f, -15f), modelo, CapaEmisiva);
                    break;
            }

            modelo.AplicarPaleta(primario, secundario);
            return modelo;
        }
    }
}
