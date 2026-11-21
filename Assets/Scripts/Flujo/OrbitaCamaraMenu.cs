using UnityEngine;

namespace VoidformBioForge
{
    /// <summary>
    /// Mueve la cámara en una órbita lenta alrededor de la Fuente de Energía mientras se navega por los
    /// menús, mostrando el bioma generado como fondo vivo de la interfaz.
    /// </summary>
    public class OrbitaCamaraMenu : MonoBehaviour
    {
        [SerializeField] private Vector3 centro = new Vector3(0f, 3f, 0f);
        [SerializeField] private float radio = 30f;
        [SerializeField] private float altura = 13f;
        [SerializeField] private float velocidadGrados = 4f;

        private float angulo;

        private void LateUpdate()
        {
            angulo += velocidadGrados * Time.unscaledDeltaTime;
            float rad = angulo * Mathf.Deg2Rad;
            transform.position = centro + new Vector3(Mathf.Cos(rad) * radio, altura, Mathf.Sin(rad) * radio);
            transform.LookAt(centro);
        }
    }
}
