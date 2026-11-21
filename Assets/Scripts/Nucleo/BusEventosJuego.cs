using System;
using UnityEngine;

namespace VoidformBioForge
{
    /// <summary>
    /// Información de una baja (muerte) para el registro de combate en pantalla.
    /// </summary>
    public struct InformacionBaja
    {
        public string NombreVictima;
        public BandoJuego BandoVictima;
        public string NombreAtacante;
        public bool TieneAtacante;
        public BandoJuego BandoAtacante;
        public bool FueExplosionDeTraje;
        public string Origen;
    }

    /// <summary>
    /// Tipo de mensaje para que la interfaz elija color e importancia.
    /// </summary>
    public enum TipoMensajeSistema
    {
        Informacion,
        Exito,
        Advertencia,
        Critico,
        Narrativa
    }

    /// <summary>
    /// Bus de eventos global del juego.
    /// -----------------------------------------------------------------
    /// Nivel 1: es un punto único donde cualquier sistema "publica" algo que pasó.
    /// Nivel 2: los oyentes (interfaz, clasificación, historia) se suscriben sin conocer
    ///          a quien publica, evitando referencias cruzadas entre scripts.
    /// Nivel 3: se limpia automáticamente al iniciar el juego, incluso si el editor tiene
    ///          desactivada la recarga de dominio ("Enter Play Mode Options"), para que no
    ///          queden suscripciones fantasma de una ejecución anterior.
    /// </summary>
    public static class BusEventosJuego
    {
        /// <summary>Mensaje de texto para el registro de eventos del HUD.</summary>
        public static event Action<string, TipoMensajeSistema> AlMensajeSistema;

        /// <summary>Se publicó una baja (para el registro de combate).</summary>
        public static event Action<InformacionBaja> AlRegistrarBaja;

        /// <summary>Un traje humano explotó al llegar su vida a cero.</summary>
        public static event Action<SistemaSaludYTraje> AlExplotarTraje;

        /// <summary>Cambió la pantalla o fase del flujo general.</summary>
        public static event Action<EstadoFlujoJuego> AlCambiarEstadoFlujo;

        /// <summary>Una partida acaba de construirse y comienza.</summary>
        public static event Action<ConfiguracionPartida> AlIniciarPartida;

        /// <summary>Una partida terminó con un resultado definitivo.</summary>
        public static event Action<ResultadoPartida> AlFinalizarPartida;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void LimpiarAlIniciarJuego()
        {
            AlMensajeSistema = null;
            AlRegistrarBaja = null;
            AlExplotarTraje = null;
            AlCambiarEstadoFlujo = null;
            AlIniciarPartida = null;
            AlFinalizarPartida = null;
        }

        public static void PublicarMensaje(string mensaje, TipoMensajeSistema tipo = TipoMensajeSistema.Informacion)
        {
            Debug.Log($"[Voidform] {mensaje}");
            AlMensajeSistema?.Invoke(mensaje, tipo);
        }

        public static void PublicarBaja(InformacionBaja baja)
        {
            AlRegistrarBaja?.Invoke(baja);
        }

        public static void PublicarExplosionTraje(SistemaSaludYTraje traje)
        {
            AlExplotarTraje?.Invoke(traje);
        }

        public static void PublicarEstadoFlujo(EstadoFlujoJuego estado)
        {
            AlCambiarEstadoFlujo?.Invoke(estado);
        }

        public static void PublicarInicioPartida(ConfiguracionPartida configuracion)
        {
            AlIniciarPartida?.Invoke(configuracion);
        }

        public static void PublicarFinPartida(ResultadoPartida resultado)
        {
            Debug.Log($"[Voidform] Fin de partida: {UtilidadesTipos.NombreResultado(resultado.resultado)} — {resultado.mensajePrincipal}");
            AlFinalizarPartida?.Invoke(resultado);
        }
    }
}
