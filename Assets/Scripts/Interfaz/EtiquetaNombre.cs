using UnityEngine;

namespace VoidformBioForge
{
    /// <summary>
    /// Etiqueta flotante con el nombre y la vida de cada personaje (color del bando).
    /// Nivel 1: texto 3D sobre la cabeza. Nivel 2: siempre mira a la cámara y muestra la vida.
    /// Nivel 3: se oculta al morir, a gran distancia o si es el propio jugador local.
    /// </summary>
    public class EtiquetaNombre : MonoBehaviour
    {
        private TextMesh texto;
        private SistemaSaludYTraje salud;
        private float alturaEtiqueta;
        private string nombreBase;

        public static void Crear(SistemaSaludYTraje entidad, float altura)
        {
            GameObject go = new GameObject("Etiqueta de Nombre");
            go.transform.SetParent(entidad.transform, false);
            go.transform.localPosition = new Vector3(0f, altura, 0f);
            EtiquetaNombre e = go.AddComponent<EtiquetaNombre>();
            e.salud = entidad;
            e.alturaEtiqueta = altura;
            e.nombreBase = entidad.NombreEntidad;

            e.texto = go.AddComponent<TextMesh>();
            e.texto.anchor = TextAnchor.MiddleCenter;
            e.texto.alignment = TextAlignment.Center;
            e.texto.characterSize = 0.06f;
            e.texto.fontSize = 48;
            e.texto.color = PaletaVoidform.ColorBando(entidad.Bando);
            Font fuente = FabricaInterfaz.ObtenerFuente();
            if (fuente != null)
            {
                e.texto.font = fuente;
                go.GetComponent<MeshRenderer>().sharedMaterial = fuente.material;
            }
        }

        private void LateUpdate()
        {
            if (texto == null || salud == null) return;
            Camera cam = Camera.main;
            MeshRenderer mr = texto.GetComponent<MeshRenderer>();
            bool visible = cam != null && !salud.EstaMuerto && !salud.EsJugadorLocal
                           && Vector3.Distance(cam.transform.position, transform.position) < 60f;
            mr.enabled = visible;
            if (!visible) return;

            transform.rotation = Quaternion.LookRotation(transform.position - cam.transform.position);
            texto.text = $"{nombreBase}\n{Mathf.CeilToInt(salud.VidaActual)} / {Mathf.CeilToInt(salud.VidaMaxima)}";
        }
    }
}
