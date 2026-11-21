using UnityEngine;

namespace VoidformBioForge
{
    /// <summary>
    /// Traduce el teclado y el ratón del jugador local a intenciones de EntradaPersonaje.
    /// -----------------------------------------------------------------
    /// Controles (iguales para ambos bandos; cada sistema interpreta los que necesita):
    ///   WASD / flechas ... moverse            Ratón ........ mirar
    ///   Espacio .......... saltar             Clic izq. .... disparar / golpe elemental
    ///   R ................ recargar (Destructor) / habilidad 2 (Guardián)
    ///   Clic derecho ..... bloquear / apuntar / cañón de pulso (Destructor)
    ///   Z / X / C ........ habilidades de armadura corta / media / larga (Destructor)
    ///   E / Q / F ........ habilidades del arma corta / media / larga (Destructor)
    ///   E / R / Q ........ habilidades 1 / 2 / definitiva (Guardián) · F rota elemento (Nexus)
    ///   Esc .............. pausa (lo gestiona GestorInterfazJuego)
    /// Usa el "Input Manager" clásico de Unity. Si el proyecto usa el nuevo Input System,
    /// en Project Settings > Player > Active Input Handling seleccione "Both".
    /// </summary>
    [DefaultExecutionOrder(-100)]
    [RequireComponent(typeof(EntradaPersonaje))]
    public class EntradaJugadorLocal : MonoBehaviour
    {
        /// <summary>Bloqueo global: la interfaz lo activa mientras hay un menú o la pausa abierta.</summary>
        public static bool EntradaBloqueadaPorInterfaz { get; set; }

        [Header("Sensibilidad del Ratón")]
        [SerializeField] private float sensibilidadRaton = 2.2f;
        [SerializeField] private bool invertirEjeVertical = false;

        private EntradaPersonaje entrada;

        public float SensibilidadRaton
        {
            get => sensibilidadRaton;
            set => sensibilidadRaton = Mathf.Clamp(value, 0.1f, 10f);
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ReiniciarEstadoEstatico()
        {
            EntradaBloqueadaPorInterfaz = false;
        }

        private void Awake()
        {
            entrada = GetComponent<EntradaPersonaje>();
        }

        private void Update()
        {
            if (EntradaBloqueadaPorInterfaz || !entrada.EntradaHabilitada || entrada.BloqueadaPorTambaleo)
            {
                entrada.LimpiarTodo();
                return;
            }

            // Nivel 1: ejes básicos de desplazamiento y mirada.
            entrada.Movimiento = Vector2.ClampMagnitude(
                new Vector2(Input.GetAxisRaw("Horizontal"), Input.GetAxisRaw("Vertical")), 1f);

            float giroVertical = Input.GetAxis("Mouse Y") * sensibilidadRaton;
            if (invertirEjeVertical) giroVertical = -giroVertical;
            entrada.GiroMirada = new Vector2(Input.GetAxis("Mouse X") * sensibilidadRaton, giroVertical);

            // Nivel 2: acciones de combate.
            entrada.SaltoPresionado |= Input.GetButtonDown("Jump");
            entrada.AtaquePrincipalMantenido = Input.GetMouseButton(0);
            entrada.AtaquePrincipalPresionado |= Input.GetMouseButtonDown(0);
            entrada.AtaqueSecundarioMantenido = Input.GetMouseButton(1);
            entrada.HabilidadArmaduraCorta |= Input.GetKeyDown(KeyCode.Z);
            entrada.HabilidadArmaduraMedia |= Input.GetKeyDown(KeyCode.X);
            entrada.HabilidadArmaduraLarga |= Input.GetKeyDown(KeyCode.C);
            entrada.RecargarPresionado |= Input.GetKeyDown(KeyCode.R);
            entrada.Habilidad1Presionada |= Input.GetKeyDown(KeyCode.E);
            entrada.Habilidad2Presionada |= Input.GetKeyDown(KeyCode.R);
            entrada.HabilidadEspecialPresionada |= Input.GetKeyDown(KeyCode.Q);
            entrada.CambiarElementoPresionado |= Input.GetKeyDown(KeyCode.F);

            // Nivel 3: cambio de arma por teclas numéricas o rueda del ratón.
            if (Input.GetKeyDown(KeyCode.Alpha1)) entrada.CambioArmaSolicitado = 0;
            else if (Input.GetKeyDown(KeyCode.Alpha2)) entrada.CambioArmaSolicitado = 1;
            else
            {
                float rueda = Input.GetAxis("Mouse ScrollWheel");
                if (rueda > 0.05f) entrada.CambioArmaSolicitado = 0;
                else if (rueda < -0.05f) entrada.CambioArmaSolicitado = 1;
            }
        }
    }
}
