using System;
using System.Collections.Generic;
using UnityEngine;

namespace VoidformBioForge
{
    /// <summary>
    /// Cámara de espectador del Modo 4vs4 (y del Modo Historia como Destructor).
    /// Nivel 1: al activarse, sigue a un compañero vivo desde detrás y por encima.
    /// Nivel 2: clic izquierdo / Tab cambia al siguiente compañero vivo; si el observado muere, salta solo.
    /// Nivel 3: al desactivarse devuelve la cámara a su padre y posición originales sin saltos visibles.
    /// </summary>
    public class CamaraEspectador : MonoBehaviour
    {
        [SerializeField] private Vector3 desplazamientoSeguimiento = new Vector3(0f, 3.2f, -6f);
        [SerializeField] private float suavizado = 6f;

        private Camera camara;
        private Transform padreOriginal;
        private Vector3 posicionLocalOriginal;
        private Quaternion rotacionLocalOriginal;
        private Func<List<SistemaSaludYTraje>> proveedorCompaneros;
        private SistemaSaludYTraje observado;
        private int indiceObservado;

        public bool Activa { get; private set; }
        public SistemaSaludYTraje Observado => observado;

        public event Action<string> AlCambiarObservado;

        public void Configurar(Camera camaraPrincipal, Func<List<SistemaSaludYTraje>> obtenerCompanerosVivos)
        {
            camara = camaraPrincipal;
            proveedorCompaneros = obtenerCompanerosVivos;
        }

        public void Activar()
        {
            if (Activa || camara == null) return;
            Activa = true;
            padreOriginal = camara.transform.parent;
            posicionLocalOriginal = camara.transform.localPosition;
            rotacionLocalOriginal = camara.transform.localRotation;
            camara.transform.SetParent(null, true);
            indiceObservado = 0;
            SeleccionarSiguiente(0);
        }

        public void Desactivar()
        {
            if (!Activa || camara == null) return;
            Activa = false;
            observado = null;
            camara.transform.SetParent(padreOriginal, false);
            camara.transform.localPosition = posicionLocalOriginal;
            camara.transform.localRotation = rotacionLocalOriginal;
            AlCambiarObservado?.Invoke("");
        }

        private void Update()
        {
            if (!Activa) return;

            bool cambiar = !EntradaJugadorLocal.EntradaBloqueadaPorInterfaz && (Input.GetMouseButtonDown(0) || Input.GetKeyDown(KeyCode.Tab));
            if (cambiar || observado == null || observado.EstaMuerto)
            {
                SeleccionarSiguiente(cambiar ? 1 : 0);
            }
        }

        private void LateUpdate()
        {
            if (!Activa || camara == null || observado == null) return;
            Transform t = observado.transform;
            Vector3 destino = t.position + t.rotation * desplazamientoSeguimiento;
            camara.transform.position = Vector3.Lerp(camara.transform.position, destino, 1f - Mathf.Exp(-suavizado * Time.deltaTime));
            Quaternion mirar = Quaternion.LookRotation((t.position + Vector3.up * 1.5f) - camara.transform.position);
            camara.transform.rotation = Quaternion.Slerp(camara.transform.rotation, mirar, 1f - Mathf.Exp(-suavizado * Time.deltaTime));
        }

        private void SeleccionarSiguiente(int paso)
        {
            List<SistemaSaludYTraje> vivos = proveedorCompaneros != null ? proveedorCompaneros() : new List<SistemaSaludYTraje>();
            if (vivos.Count == 0)
            {
                observado = null;
                AlCambiarObservado?.Invoke("Ningún compañero vivo");
                return;
            }
            indiceObservado = (indiceObservado + paso) % vivos.Count;
            if (indiceObservado < 0) indiceObservado += vivos.Count;
            observado = vivos[indiceObservado];
            AlCambiarObservado?.Invoke(observado.NombreEntidad);
        }
    }
}
