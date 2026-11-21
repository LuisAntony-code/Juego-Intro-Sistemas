using System;
using UnityEngine;

namespace VoidformBioForge
{
    /// <summary>
    /// Reestructura en C# la lógica de movimiento ágil, salto, gravedad y control de cámara
    /// inspirada en el ritmo de arena shooter de AssaultCube, adaptada a CharacterController
    /// de Unity con arquitectura y nomenclatura propias (no es una copia de su código).
    /// -----------------------------------------------------------------
    /// Nivel 1: desplazamiento directo, salto, gravedad constante y rotación de cámara.
    /// Nivel 2: aceleración/frenado rápidos, control aéreo medido, "tiempo de coyote" y
    ///          "búfer de salto" para que el salto responda aunque se pulse un instante antes
    ///          de tocar el suelo o justo después de dejarlo.
    /// Nivel 3: multiplicadores combinados (traje × habilidad × estados alterados), enraizado,
    ///          empujes externos que decaen, retroceso de armas, apuntado automático para bots
    ///          y protección contra caída fuera del mapa.
    /// </summary>
    [RequireComponent(typeof(CharacterController))]
    [RequireComponent(typeof(EntradaPersonaje))]
    public class ControladorMovimientoAssaultCube : MonoBehaviour
    {
        /// <summary>
        /// Multiplicador de gravedad del escenario (1 = normal). El Nexus Primordial usa 0,6: los saltos
        /// describen parábolas más altas y largas y las caídas son más lentas. Lo fija GeneradorBiomas.
        /// </summary>
        public static float MultiplicadorGravedadAmbiente { get; set; } = 1f;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ReiniciarGravedad()
        {
            MultiplicadorGravedadAmbiente = 1f;
        }

        [Header("1. Parámetros Básicos de Movimiento")]
        [SerializeField] private float velocidadBase = 10.5f;
        [SerializeField] private float fuerzaSalto = 7.8f;
        [SerializeField] private float gravedad = -21f;

        [Header("2. Ritmo Ágil y Respuesta Estilo Shooter")]
        [SerializeField] private float aceleracionSuelo = 32f;
        [SerializeField] private float desaceleracionSuelo = 24f;
        [SerializeField] [Range(0.1f, 1f)] private float controlEnAire = 0.7f;
        [SerializeField] private float tiempoCoyote = 0.1f;
        [SerializeField] private float bufferSalto = 0.12f;

        [Header("3. Cámara y Límites")]
        [SerializeField] private Transform pivoteCamara;
        [SerializeField] private float alturaOjos = 1.6f;
        [SerializeField] private float anguloMinimoVertical = -85f;
        [SerializeField] private float anguloMaximoVertical = 85f;
        [SerializeField] private float friccionEmpuje = 9f;
        [SerializeField] private float alturaLimiteCaida = -40f;

        private CharacterController controladorPersonaje;
        private EntradaPersonaje entrada;
        private SistemaSaludYTraje salud;

        private Vector3 velocidadHorizontal;
        private Vector3 impulsoExterno;
        private float velocidadVertical;
        private float rotacionVerticalCamara;
        private float ultimoMomentoEnSuelo = -10f;
        private float ultimoSaltoSolicitado = -10f;
        private bool estabaEnSuelo;

        private float multiplicadorHabilidad = 1f;
        private float multiplicadorEstados = 1f;
        private float multiplicadorTraje = 1f;
        private bool enraizado;
        private bool movimientoHabilitado = true;

        public event Action AlSaltar;
        public event Action<float> AlAterrizar;

        public Transform PivoteCamara => pivoteCamara;
        public bool EnSuelo => controladorPersonaje != null && controladorPersonaje.isGrounded;
        public bool EstaEnraizado => enraizado;
        public float VelocidadBase => velocidadBase;
        public float MultiplicadorTotal => multiplicadorHabilidad * multiplicadorEstados * multiplicadorTraje;
        public float RotacionVertical => rotacionVerticalCamara;

        public float VelocidadActual
        {
            get
            {
                if (controladorPersonaje == null) return 0f;
                Vector3 v = controladorPersonaje.velocity;
                return new Vector3(v.x, 0f, v.z).magnitude;
            }
        }

        /// <summary>Porcentaje [0..1] de velocidad respecto a la base (lo usa la dispersión de armas).</summary>
        public float FactorMovimiento => Mathf.Clamp01(VelocidadActual / Mathf.Max(0.1f, velocidadBase));

        private void Awake()
        {
            controladorPersonaje = GetComponent<CharacterController>();
            entrada = GetComponent<EntradaPersonaje>();
            salud = GetComponent<SistemaSaludYTraje>();
            AsegurarPivoteCamara();
        }

        private void AsegurarPivoteCamara()
        {
            if (pivoteCamara != null) return;
            Transform existente = transform.Find("PivoteCamara");
            if (existente != null)
            {
                pivoteCamara = existente;
                return;
            }
            GameObject pivote = new GameObject("PivoteCamara");
            pivote.transform.SetParent(transform, false);
            pivote.transform.localPosition = new Vector3(0f, alturaOjos, 0f);
            pivoteCamara = pivote.transform;
        }

        /// <summary>Permite ajustar la altura de ojos para personajes grandes (guardianes).</summary>
        public void ConfigurarAlturaOjos(float nuevaAltura)
        {
            alturaOjos = nuevaAltura;
            AsegurarPivoteCamara();
            pivoteCamara.localPosition = new Vector3(0f, alturaOjos, 0f);
        }

        private void Update()
        {
            if (transform.position.y < alturaLimiteCaida)
            {
                ProcesarCaidaFueraDelMapa();
                return;
            }

            if (!movimientoHabilitado) return;

            if (entrada.EntradaHabilitada)
            {
                ProcesarRotacionCamara(entrada.GiroMirada);
            }

            ProcesarDesplazamientoYSalto();
        }

        // ================== NIVEL 1: Fundamentos ==================

        private void ProcesarRotacionCamara(Vector2 giro)
        {
            if (giro.sqrMagnitude < 0.000001f) return;
            transform.Rotate(Vector3.up * giro.x);
            AplicarRotacionVertical(rotacionVerticalCamara - giro.y);
        }

        private void AplicarRotacionVertical(float nuevoAngulo)
        {
            rotacionVerticalCamara = Mathf.Clamp(nuevoAngulo, anguloMinimoVertical, anguloMaximoVertical);
            if (pivoteCamara != null)
            {
                pivoteCamara.localRotation = Quaternion.Euler(rotacionVerticalCamara, 0f, 0f);
            }
        }

        // ================== NIVEL 2: Ritmo ágil ==================

        private void ProcesarDesplazamientoYSalto()
        {
            bool enSuelo = controladorPersonaje.isGrounded;
            if (enSuelo) ultimoMomentoEnSuelo = Time.time;

            Vector2 ejes = entrada.EntradaHabilitada ? entrada.Movimiento : Vector2.zero;
            if (enraizado) ejes = Vector2.zero;

            Vector3 direccionDeseada = transform.right * ejes.x + transform.forward * ejes.y;
            if (direccionDeseada.sqrMagnitude > 1f) direccionDeseada.Normalize();

            Vector3 velocidadObjetivo = direccionDeseada * (velocidadBase * MultiplicadorTotal);

            float tasaCambio = enSuelo
                ? (direccionDeseada.sqrMagnitude > 0.01f ? aceleracionSuelo : desaceleracionSuelo)
                : aceleracionSuelo * controlEnAire;

            velocidadHorizontal = Vector3.MoveTowards(velocidadHorizontal, velocidadObjetivo, tasaCambio * Time.deltaTime);

            // Búfer de salto: se recuerda la pulsación durante "bufferSalto" segundos.
            if (entrada.EntradaHabilitada && entrada.SaltoPresionado)
            {
                ultimoSaltoSolicitado = Time.time;
            }

            bool puedeSaltarPorCoyote = Time.time - ultimoMomentoEnSuelo <= tiempoCoyote;
            bool saltoPendiente = Time.time - ultimoSaltoSolicitado <= bufferSalto;

            if (enSuelo && velocidadVertical < 0f)
            {
                velocidadVertical = -2f; // Mantiene pegado al suelo en rampas.
            }

            if (saltoPendiente && puedeSaltarPorCoyote && !enraizado)
            {
                velocidadVertical = fuerzaSalto;
                ultimoSaltoSolicitado = -10f;
                ultimoMomentoEnSuelo = -10f;
                AlSaltar?.Invoke();
            }
            else if (!enSuelo)
            {
                velocidadVertical += gravedad * MultiplicadorGravedadAmbiente * Time.deltaTime;
            }

            // ================== NIVEL 3: Fuerzas externas ==================
            impulsoExterno = Vector3.MoveTowards(impulsoExterno, Vector3.zero, friccionEmpuje * Time.deltaTime);

            Vector3 movimientoFinal = velocidadHorizontal + impulsoExterno + Vector3.up * velocidadVertical;
            float velocidadVerticalPrevia = velocidadVertical;
            controladorPersonaje.Move(movimientoFinal * Time.deltaTime);

            // Techo: si chocamos arriba se anula la subida.
            if ((controladorPersonaje.collisionFlags & CollisionFlags.Above) != 0 && velocidadVertical > 0f)
            {
                velocidadVertical = 0f;
            }

            bool ahoraEnSuelo = controladorPersonaje.isGrounded;
            if (ahoraEnSuelo && !estabaEnSuelo)
            {
                AlAterrizar?.Invoke(Mathf.Abs(velocidadVerticalPrevia));
            }
            estabaEnSuelo = ahoraEnSuelo;
        }

        private void ProcesarCaidaFueraDelMapa()
        {
            if (salud != null && !salud.EstaMuerto)
            {
                salud.RecibirDano(InformacionDano.Ambiental(99999f, TipoDano.Explosivo, "Caída al vacío", transform.position));
            }
        }

        // ================== API pública para otros sistemas ==================

        public void EstablecerMovimientoHabilitado(bool habilitado)
        {
            movimientoHabilitado = habilitado;
            if (!habilitado)
            {
                velocidadHorizontal = Vector3.zero;
                impulsoExterno = Vector3.zero;
                velocidadVertical = 0f;
            }
        }

        /// <summary>Multiplicador temporal de habilidades (por ejemplo, Impulso Táctico).</summary>
        public void EstablecerMultiplicadorVelocidad(float multiplicador)
        {
            multiplicadorHabilidad = Mathf.Max(0.1f, multiplicador);
        }

        /// <summary>Multiplicador procedente de estados alterados (ralentización).</summary>
        public void EstablecerMultiplicadorEstados(float multiplicador)
        {
            multiplicadorEstados = Mathf.Clamp(multiplicador, 0.05f, 2f);
        }

        /// <summary>Multiplicador permanente del traje o del guardián.</summary>
        public void EstablecerMultiplicadorTraje(float multiplicador)
        {
            multiplicadorTraje = Mathf.Clamp(multiplicador, 0.3f, 2f);
        }

        public void EstablecerEnraizado(bool valor)
        {
            enraizado = valor;
        }

        /// <summary>Empuje externo (corrientes de agua, ondas sísmicas, explosiones).</summary>
        public void AplicarImpulso(Vector3 impulso)
        {
            impulsoExterno += new Vector3(impulso.x, 0f, impulso.z);
            if (impulso.y > 0f)
            {
                velocidadVertical = Mathf.Max(velocidadVertical, impulso.y);
            }
        }

        /// <summary>Retroceso del arma: eleva la mirada unos grados.</summary>
        public void AplicarRetroceso(float grados)
        {
            AplicarRotacionVertical(rotacionVerticalCamara - grados);
        }

        /// <summary>
        /// Gira el cuerpo (eje Y) y el pivote (eje X) hacia un punto. Lo usan los bots para apuntar.
        /// Devuelve el ángulo restante hasta el objetivo.
        /// </summary>
        public float MirarHaciaPunto(Vector3 puntoObjetivo, float gradosPorSegundo)
        {
            Vector3 origen = pivoteCamara != null ? pivoteCamara.position : transform.position;
            Vector3 direccion = puntoObjetivo - origen;
            if (direccion.sqrMagnitude < 0.0001f) return 0f;

            Vector3 plano = new Vector3(direccion.x, 0f, direccion.z);
            if (plano.sqrMagnitude > 0.0001f)
            {
                Quaternion rotacionObjetivo = Quaternion.LookRotation(plano);
                transform.rotation = Quaternion.RotateTowards(transform.rotation, rotacionObjetivo, gradosPorSegundo * Time.deltaTime);
            }

            float anguloVerticalDeseado = -Mathf.Atan2(direccion.y, plano.magnitude) * Mathf.Rad2Deg;
            AplicarRotacionVertical(Mathf.MoveTowards(rotacionVerticalCamara, anguloVerticalDeseado, gradosPorSegundo * Time.deltaTime));

            Vector3 adelante = pivoteCamara != null ? pivoteCamara.forward : transform.forward;
            return Vector3.Angle(adelante, direccion);
        }

        /// <summary>Mueve al personaje sin que el CharacterController lo bloquee.</summary>
        public void Teletransportar(Vector3 posicion, Quaternion rotacion)
        {
            bool estabaActivo = controladorPersonaje.enabled;
            controladorPersonaje.enabled = false;
            transform.SetPositionAndRotation(posicion, rotacion);
            controladorPersonaje.enabled = estabaActivo;
            velocidadHorizontal = Vector3.zero;
            impulsoExterno = Vector3.zero;
            velocidadVertical = 0f;
            AplicarRotacionVertical(0f);
        }
    }
}
