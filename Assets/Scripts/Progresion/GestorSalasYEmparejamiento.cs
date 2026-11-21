using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace VoidformBioForge
{
    /// <summary>
    /// Contrato que debe cumplir cualquier servicio de red (local, Unity Lobby/Relay + Netcode, Photon, etc.).
    /// GestorSalasYEmparejamiento solo habla con esta interfaz, de modo que cambiar de servicio no obliga a
    /// tocar la interfaz de usuario ni los gestores de partida.
    /// </summary>
    public interface IProveedorRed
    {
        string NombreProveedor { get; }
        bool EsSimulado { get; }
        IEnumerator CrearSala(ModoJuego modo, bool privada, string codigoDeseado, Action<bool, string, string> alTerminar);
        IEnumerator UnirsePorCodigo(string codigo, Action<bool, string, int> alTerminar);
        IEnumerator BuscarSalaPublica(ModoJuego modo, Action<bool, string, int> alTerminar);
        void SalirDeSala();
    }

    /// <summary>
    /// Proveedor de red local incluido en el proyecto. Simula la latencia y las respuestas de un servidor de
    /// salas para que todo el flujo (crear sala, con amigos, sala pública) funcione sin conexión. Los puestos
    /// que ocuparían jugadores remotos se rellenan con bots. Para multijugador real, implemente IProveedorRed
    /// con el servicio elegido (ver Docs/01_Montaje_Unity_y_VisualStudio.md, sección "Red real").
    /// </summary>
    public class ProveedorRedLocalSimulado : IProveedorRed
    {
        public string NombreProveedor => "Red local simulada (sin conexión)";
        public bool EsSimulado => true;

        public IEnumerator CrearSala(ModoJuego modo, bool privada, string codigoDeseado, Action<bool, string, string> alTerminar)
        {
            yield return new WaitForSecondsRealtime(UnityEngine.Random.Range(0.4f, 0.9f));
            string codigo = string.IsNullOrWhiteSpace(codigoDeseado)
                ? $"VBF-{UnityEngine.Random.Range(1000, 9999)}"
                : codigoDeseado.Trim().ToUpperInvariant();
            alTerminar?.Invoke(true, codigo, privada ? "Sala privada creada. Comparte el código con tus amigos." : "Sala pública creada y visible.");
        }

        public IEnumerator UnirsePorCodigo(string codigo, Action<bool, string, int> alTerminar)
        {
            yield return new WaitForSecondsRealtime(UnityEngine.Random.Range(0.5f, 1.1f));
            string limpio = (codigo ?? "").Trim().ToUpperInvariant();
            bool formatoValido = limpio.StartsWith("VBF-") && limpio.Length == 8;
            if (!formatoValido)
            {
                alTerminar?.Invoke(false, "Código inválido. Formato esperado: VBF-1234.", 0);
                yield break;
            }
            alTerminar?.Invoke(true, $"Unido a la sala {limpio} (modo local: los amigos ausentes se cubren con bots).", 0);
        }

        public IEnumerator BuscarSalaPublica(ModoJuego modo, Action<bool, string, int> alTerminar)
        {
            yield return new WaitForSecondsRealtime(UnityEngine.Random.Range(0.8f, 1.6f));
            alTerminar?.Invoke(true, $"PUBLICA-{UnityEngine.Random.Range(100, 999)}", 0);
        }

        public void SalirDeSala() { }
    }

    /// <summary>
    /// Gestiona la creación y entrada a salas en los modos versus/multijugador:
    /// crear sala, jugar con bots, jugar con amigos o entrar de forma aleatoria en una sala pública.
    /// -----------------------------------------------------------------
    /// Nivel 1: seleccionar el tipo de sala y obtener un código.
    /// Nivel 2: máquina de estados (Sin sala → Creando/Buscando → Esperando jugadores → Lista → En partida),
    ///          configuración de visibilidad (privada/pública) y lista de jugadores con relleno de bots.
    /// Nivel 3: validación de combinaciones modo/bando/sala (Historia como Guardián es individual; Historia
    ///          como Destructor admite cooperativo con amigos o bots), proveedor de red intercambiable y
    ///          entrega de una ConfiguracionPartida coherente a los gestores de partida sin conflictos.
    /// </summary>
    public class GestorSalasYEmparejamiento : MonoBehaviour
    {
        private static readonly string[] NombresBotsDestructores =
        {
            "Cabo Ibarra", "Teniente Rivas", "Ing. Okafor", "Sgto. Nakamura", "Piloto Duarte", "Téc. Lindqvist", "Cabo Mendoza", "Op. Achterberg"
        };
        private static readonly string[] NombresBotsCuidadores =
        {
            "Brote de Sylvaran", "Eco de Nerivale", "Esquirla de Korvhal", "Chispa de Ignara", "Semilla Ámbar", "Rocío Hondo", "Basalto Joven", "Brasa Errante"
        };

        private IProveedorRed proveedor = new ProveedorRedLocalSimulado();
        private Coroutine operacionEnCurso;
        private readonly List<string> jugadoresEnSala = new List<string>();

        public TipoSalaConexion TipoSalaSeleccionado { get; private set; } = TipoSalaConexion.ConBots;
        public string CodigoSalaActual { get; private set; } = "SALA-BASE";
        public EstadoSala Estado { get; private set; } = EstadoSala.SinSala;
        public bool SalaPrivada { get; private set; } = true;
        public int AmigosConectados { get; private set; }
        public string NombreProveedor => proveedor.NombreProveedor;
        public IReadOnlyList<string> JugadoresEnSala => jugadoresEnSala;

        public event Action<TipoSalaConexion, string> AlConfigurarSala;
        public event Action<EstadoSala, string> AlCambiarEstadoSala;
        public event Action<IReadOnlyList<string>> AlActualizarJugadores;

        /// <summary>Permite sustituir el proveedor local por uno real.</summary>
        public void EstablecerProveedorRed(IProveedorRed nuevoProveedor)
        {
            proveedor = nuevoProveedor ?? new ProveedorRedLocalSimulado();
        }

        // =====================================================================
        // Nivel 3: validación
        // =====================================================================

        public static bool EsCombinacionValida(ModoJuego modo, BandoJuego bando, TipoSalaConexion sala, out string motivo)
        {
            motivo = "";
            if (modo != ModoJuego.Historia) return true;

            if (bando == BandoJuego.Cuidador)
            {
                motivo = "En el Modo Historia como Guardián juegas en solitario: solo tú puedes ser el Guardián del nivel.";
                return false;
            }

            if (sala == TipoSalaConexion.SalaPublicaAleatoria)
            {
                motivo = "El Modo Historia no admite salas públicas: invita a tus amigos o completa el escuadrón con bots.";
                return false;
            }
            return true;
        }

        // =====================================================================
        // Nivel 1-2: selección de sala
        // =====================================================================

        public void SeleccionarTipoSala(TipoSalaConexion tipoSala, string codigoPersonalizado = "")
        {
            SeleccionarTipoSala(tipoSala, codigoPersonalizado, ModoJuego.Conquista5Bases, BandoJuego.Destructor, 4, true);
        }

        public void SeleccionarTipoSala(TipoSalaConexion tipoSala, string codigoPersonalizado, ModoJuego modo, BandoJuego bando,
            int jugadoresPorEquipo, bool privada)
        {
            if (!EsCombinacionValida(modo, bando, tipoSala, out string motivo))
            {
                CambiarEstado(EstadoSala.SinSala, motivo);
                return;
            }

            if (operacionEnCurso != null) StopCoroutine(operacionEnCurso);
            proveedor.SalirDeSala();

            TipoSalaSeleccionado = tipoSala;
            SalaPrivada = privada;
            AmigosConectados = 0;

            switch (tipoSala)
            {
                case TipoSalaConexion.CrearSala:
                    CambiarEstado(EstadoSala.Creando, "Creando sala...");
                    operacionEnCurso = StartCoroutine(proveedor.CrearSala(modo, privada, codigoPersonalizado, (exito, codigo, mensaje) =>
                    {
                        operacionEnCurso = null;
                        if (!exito) { CambiarEstado(EstadoSala.SinSala, mensaje); return; }
                        CodigoSalaActual = codigo;
                        RellenarLista(modo, bando, jugadoresPorEquipo);
                        AlConfigurarSala?.Invoke(TipoSalaSeleccionado, CodigoSalaActual);
                        CambiarEstado(EstadoSala.EsperandoJugadores, $"{mensaje} Código: {codigo}. Pulsa «Iniciar» cuando quieras (los huecos se rellenan con bots).");
                    }));
                    break;

                case TipoSalaConexion.ConBots:
                    CodigoSalaActual = "SALA-BOTS";
                    RellenarLista(modo, bando, jugadoresPorEquipo);
                    AlConfigurarSala?.Invoke(TipoSalaSeleccionado, CodigoSalaActual);
                    CambiarEstado(EstadoSala.Lista, "Sala con bots lista: todos los puestos libres los ocupan bots.");
                    break;

                case TipoSalaConexion.ConAmigos:
                    if (string.IsNullOrWhiteSpace(codigoPersonalizado))
                    {
                        CambiarEstado(EstadoSala.Creando, "Creando sala privada para amigos...");
                        operacionEnCurso = StartCoroutine(proveedor.CrearSala(modo, true, "", (exito, codigo, mensaje) =>
                        {
                            operacionEnCurso = null;
                            if (!exito) { CambiarEstado(EstadoSala.SinSala, mensaje); return; }
                            CodigoSalaActual = codigo;
                            SalaPrivada = true;
                            RellenarLista(modo, bando, jugadoresPorEquipo);
                            AlConfigurarSala?.Invoke(TipoSalaSeleccionado, CodigoSalaActual);
                            CambiarEstado(EstadoSala.EsperandoJugadores, $"Invita a tus amigos con el código {codigo}.");
                        }));
                    }
                    else
                    {
                        CambiarEstado(EstadoSala.BuscandoPartida, $"Uniéndose a la sala {codigoPersonalizado}...");
                        operacionEnCurso = StartCoroutine(proveedor.UnirsePorCodigo(codigoPersonalizado, (exito, mensaje, amigos) =>
                        {
                            operacionEnCurso = null;
                            if (!exito) { CambiarEstado(EstadoSala.SinSala, mensaje); return; }
                            CodigoSalaActual = codigoPersonalizado.Trim().ToUpperInvariant();
                            AmigosConectados = amigos;
                            RellenarLista(modo, bando, jugadoresPorEquipo);
                            AlConfigurarSala?.Invoke(TipoSalaSeleccionado, CodigoSalaActual);
                            CambiarEstado(EstadoSala.Lista, mensaje);
                        }));
                    }
                    break;

                case TipoSalaConexion.SalaPublicaAleatoria:
                    CambiarEstado(EstadoSala.BuscandoPartida, "Buscando una sala pública aleatoria...");
                    operacionEnCurso = StartCoroutine(proveedor.BuscarSalaPublica(modo, (exito, codigo, remotos) =>
                    {
                        operacionEnCurso = null;
                        if (!exito) { CambiarEstado(EstadoSala.SinSala, "No se encontró ninguna sala pública."); return; }
                        CodigoSalaActual = codigo;
                        SalaPrivada = false;
                        RellenarLista(modo, bando, jugadoresPorEquipo);
                        AlConfigurarSala?.Invoke(TipoSalaSeleccionado, CodigoSalaActual);
                        CambiarEstado(EstadoSala.Lista, $"Entraste en la sala pública {codigo}.");
                    }));
                    break;
            }
        }

        private void RellenarLista(ModoJuego modo, BandoJuego bando, int jugadoresPorEquipo)
        {
            jugadoresEnSala.Clear();
            jugadoresEnSala.Add($"Tú ({UtilidadesTipos.NombreBandoSingular(bando)})");

            int aliados;
            int rivales;
            if (modo == ModoJuego.Historia)
            {
                aliados = 4 - 1 - AmigosConectados;
                rivales = 1;
            }
            else
            {
                aliados = jugadoresPorEquipo - 1 - AmigosConectados;
                rivales = jugadoresPorEquipo;
            }

            for (int i = 0; i < AmigosConectados; i++) jugadoresEnSala.Add($"Amigo {i + 1} (aliado)");

            string[] nombresAliados = bando == BandoJuego.Destructor ? NombresBotsDestructores : NombresBotsCuidadores;
            string[] nombresRivales = bando == BandoJuego.Destructor ? NombresBotsCuidadores : NombresBotsDestructores;
            for (int i = 0; i < Mathf.Max(0, aliados); i++) jugadoresEnSala.Add($"[Bot aliado] {nombresAliados[i % nombresAliados.Length]}");
            if (modo == ModoJuego.Historia)
            {
                jugadoresEnSala.Add("[Jefe] Guardián del bioma");
            }
            else
            {
                for (int i = 0; i < rivales; i++) jugadoresEnSala.Add($"[Bot rival] {nombresRivales[i % nombresRivales.Length]}");
            }

            AlActualizarJugadores?.Invoke(jugadoresEnSala);
        }

        public static string ObtenerNombreBot(BandoJuego bando, int indice)
        {
            string[] lista = bando == BandoJuego.Destructor ? NombresBotsDestructores : NombresBotsCuidadores;
            return lista[Mathf.Abs(indice) % lista.Length];
        }

        // =====================================================================
        // Nivel 3: entrega a la partida
        // =====================================================================

        public bool PuedeIniciar => Estado == EstadoSala.Lista || Estado == EstadoSala.EsperandoJugadores || Estado == EstadoSala.EnPartida;

        /// <summary>Vuelca los datos de la sala en la configuración y marca la sala como "En partida".</summary>
        public void AplicarAConfiguracion(ConfiguracionPartida configuracion)
        {
            configuracion.tipoSala = TipoSalaSeleccionado;
            configuracion.codigoSala = CodigoSalaActual;
            configuracion.salaPrivada = SalaPrivada;
            configuracion.jugadoresHumanosReales = Mathf.Clamp(1 + AmigosConectados, 1, 4);
            CambiarEstado(EstadoSala.EnPartida, $"Partida en curso en {CodigoSalaActual}.");
        }

        public void CerrarSala()
        {
            if (operacionEnCurso != null) StopCoroutine(operacionEnCurso);
            operacionEnCurso = null;
            proveedor.SalirDeSala();
            jugadoresEnSala.Clear();
            AmigosConectados = 0;
            CambiarEstado(EstadoSala.SinSala, "");
        }

        private void CambiarEstado(EstadoSala nuevo, string mensaje)
        {
            Estado = nuevo;
            AlCambiarEstadoSala?.Invoke(nuevo, mensaje);
        }
    }
}
