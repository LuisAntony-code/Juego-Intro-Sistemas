using UnityEngine;

namespace VoidformBioForge
{
    /// <summary>
    /// "Intenciones" de un personaje en el fotograma actual: moverse, mirar, saltar, disparar,
    /// recargar y lanzar habilidades.
    /// -----------------------------------------------------------------
    /// Nivel 1: es un simple contenedor de valores.
    /// Nivel 2: lo rellena EntradaJugadorLocal (teclado y ratón) o ControladorBotIA (IA),
    ///          de modo que movimiento, armas y guardianes funcionan igual para personas y bots.
    /// Nivel 3: los pulsos de un solo fotograma (Presionado) se limpian en LateUpdate, después
    ///          de que todos los sistemas los hayan leído en Update. Los productores de entrada
    ///          usan [DefaultExecutionOrder] negativo para escribir antes de que se lea.
    /// </summary>
    [DefaultExecutionOrder(-200)]
    public class EntradaPersonaje : MonoBehaviour
    {
        // ---------- Ejes continuos ----------
        /// <summary>Movimiento deseado: x = lateral, y = adelante/atrás. Rango [-1, 1].</summary>
        public Vector2 Movimiento { get; set; }

        /// <summary>Giro de cámara solicitado este fotograma (grados): x = horizontal, y = vertical.</summary>
        public Vector2 GiroMirada { get; set; }

        // ---------- Botones mantenidos ----------
        public bool AtaquePrincipalMantenido { get; set; }

        /// <summary>Clic derecho mantenido: bloquear (espada, escudo), apuntar (arco, rifle) o cañón de pulso.</summary>
        public bool AtaqueSecundarioMantenido { get; set; }

        // ---------- Pulsos de un fotograma ----------
        public bool SaltoPresionado { get; set; }
        public bool AtaquePrincipalPresionado { get; set; }
        public bool RecargarPresionado { get; set; }
        public bool Habilidad1Presionada { get; set; }
        public bool Habilidad2Presionada { get; set; }
        public bool HabilidadEspecialPresionada { get; set; }
        public bool CambiarElementoPresionado { get; set; }

        // Humanos: habilidades de armadura (Z corta, X media, C larga).
        public bool HabilidadArmaduraCorta { get; set; }
        public bool HabilidadArmaduraMedia { get; set; }
        public bool HabilidadArmaduraLarga { get; set; }

        // Humanos: habilidades del arma. Comparten teclas con las de los guardianes (E, Q, F).
        public bool HabilidadArmaCorta => Habilidad1Presionada;
        public bool HabilidadArmaMedia => HabilidadEspecialPresionada;
        public bool HabilidadArmaLarga => CambiarElementoPresionado;

        private float bloqueadaHasta = -1f;

        /// <summary>Verdadero mientras un tambaleo impide actuar.</summary>
        public bool BloqueadaPorTambaleo => Time.time < bloqueadaHasta;

        public void BloquearDurante(float segundos)
        {
            bloqueadaHasta = Mathf.Max(bloqueadaHasta, Time.time + segundos);
        }

        /// <summary>Índice de arma solicitado (0 principal, 1 secundaria) o -1 si no se pidió cambio.</summary>
        public int CambioArmaSolicitado { get; set; } = -1;

        /// <summary>Si es falso, todos los sistemas ignoran la entrada (menús, muerte, pausa).</summary>
        public bool EntradaHabilitada { get; set; } = true;

        private void LateUpdate()
        {
            LimpiarPulsos();
            GiroMirada = Vector2.zero;
        }

        public void LimpiarPulsos()
        {
            SaltoPresionado = false;
            AtaquePrincipalPresionado = false;
            RecargarPresionado = false;
            Habilidad1Presionada = false;
            Habilidad2Presionada = false;
            HabilidadEspecialPresionada = false;
            CambiarElementoPresionado = false;
            HabilidadArmaduraCorta = false;
            HabilidadArmaduraMedia = false;
            HabilidadArmaduraLarga = false;
            CambioArmaSolicitado = -1;
        }

        public void LimpiarTodo()
        {
            LimpiarPulsos();
            Movimiento = Vector2.zero;
            GiroMirada = Vector2.zero;
            AtaquePrincipalMantenido = false;
            AtaqueSecundarioMantenido = false;
        }
    }
}
