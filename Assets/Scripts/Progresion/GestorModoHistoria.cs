using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace VoidformBioForge
{
    /// <summary>
    /// Cuántos personajes y de qué tipo forman un nivel de Historia según la perspectiva elegida.
    /// </summary>
    public struct ComposicionNivelHistoria
    {
        public bool JugadorEsGuardian;
        public int HumanosReales;
        public int BotsHumanos;
        public int GuardianesIA;
        public TipoElemento ElementoGuardian;
        public bool GuardianRotaElementos;
        public float MultiplicadorVidaJefe;
        public float MultiplicadorDanoJefe;
        public float DificultadBots;
        public TipoTraje TrajeBotsHumanos;
    }

    /// <summary>
    /// Gestiona el Modo Historia de 5 niveles con cambio de bioma, elección de bando y distintas perspectivas.
    /// -----------------------------------------------------------------
    /// Nivel 1: 5 niveles (Bosque, Humedales, Cordillera, Caldera y Núcleo) y elección de bando.
    /// Nivel 2: REGLAS DE PARTICIPACIÓN:
    ///   • Humano (Destructor): siempre se juega con un escuadrón de 4 humanos; los puestos que no ocupen
    ///     jugadores reales se rellenan automáticamente con bots/mods. Su rival es el guardián del bioma.
    ///   • Guardián (Cuidador): SOLO el jugador puede ser Guardián en ese nivel; se enfrenta a 4 humanos
    ///     controlados por bots y, para pasar al siguiente nivel, DEBE GANARLES.
    ///   Perspectiva jugable: Destructor en primera persona (ritmo de arena shooter); Guardián en tercera
    ///   persona (coloso elemental). Perspectiva narrativa: textos distintos para cada bando.
    /// Nivel 3: validación de victoria y avance:
    ///   • Victoria Cuidador = los 4 humanos eliminados. Derrota = el guardián cae o los humanos extraen el 100%.
    ///   • Victoria Destructor = guardián derrotado o extracción al 100% (con coste ecológico).
    ///     Derrota = los 4 humanos eliminados (los humanos no reaparecen en Historia; el jugador caído espectea).
    ///   • Muerte simultánea del último humano y del guardián (ventana de 0,25 s) = EMPATE: se repite el nivel.
    ///   • Escalado por nivel: el jefe guardián gana vida y daño; los bots humanos ganan precisión y, desde el
    ///     nivel 2, eligen el traje que contrarresta al elemento del bioma (los humanos "aprenden").
    ///   • Se guarda la integridad ecológica de cada nivel; al completar el nivel 5 se muestra el desenlace.
    /// </summary>
    public class GestorModoHistoria : MonoBehaviour
    {
        [SerializeField] private int totalNiveles = 5;
        [SerializeField] private int cupoHumanosEnModoHistoria = 4;
        [SerializeField] private int jugadoresHumanosRealesConectados = 1;
        [SerializeField] private float ventanaMuerteSimultaneaSegundos = 0.25f;

        private readonly List<SistemaSaludYTraje> humanos = new List<SistemaSaludYTraje>();
        private readonly List<SistemaSaludYTraje> guardianes = new List<SistemaSaludYTraje>();
        private bool nivelEnCurso;
        private bool resolviendo;

        public int NivelActual { get; private set; } = 1;
        public BandoJuego BandoElegido { get; private set; } = BandoJuego.Destructor;
        public TipoElemento ElementoNivelActual { get; private set; } = TipoElemento.Arbol;
        public BiomaPlaneta BiomaActual { get; private set; } = BiomaPlaneta.BosqueAncestral;
        public int BotsModsRellenados { get; private set; }
        public string DescripcionPerspectiva { get; private set; } = "";
        public NivelNarrativo Narrativa { get; private set; }
        public int TotalNiveles => totalNiveles;
        public bool NivelEnCurso => nivelEnCurso;

        public event Action<int, TipoElemento, BandoJuego, string> AlIniciarNivelHistoria;
        public event Action<string> AlCambiarObjetivo;
        public event Action<ResultadoEnfrentamiento, string, bool> AlFinalizarNivel;
        public event Action<string> AlCompletarHistoria;
        public event Action<bool> AlCambiarModoEspectador;

        // =====================================================================
        // Nivel 1-2: preparación del nivel
        // =====================================================================

        public static TipoElemento ObtenerElementoPorNivel(int nivel)
        {
            switch (nivel)
            {
                case 1: return TipoElemento.Arbol;
                case 2: return TipoElemento.Agua;
                case 3: return TipoElemento.Tierra;
                case 4: return TipoElemento.Fuego;
                default: return TipoElemento.Arbol; // Nivel 5: Aethernum empieza en forma de Árbol y rota.
            }
        }

        /// <summary>Calcula quién juega el nivel sin crear nada (la usa la interfaz y el GestorFlujoPartida).</summary>
        public ComposicionNivelHistoria CalcularComposicion(int nivel, BandoJuego bando, int humanosRealesConectados)
        {
            nivel = Mathf.Clamp(nivel, 1, totalNiveles);
            ComposicionNivelHistoria c = new ComposicionNivelHistoria
            {
                ElementoGuardian = ObtenerElementoPorNivel(nivel),
                GuardianRotaElementos = nivel == 5,
                GuardianesIA = 0,
                MultiplicadorVidaJefe = 1f,
                MultiplicadorDanoJefe = 1f,
                DificultadBots = Mathf.Clamp01(0.35f + 0.12f * (nivel - 1)),
                TrajeBotsHumanos = nivel == 1 ? TipoTraje.CineticoAsalto : DatosTraje.TrajeContrario(ObtenerElementoPorNivel(nivel))
            };

            if (bando == BandoJuego.Destructor)
            {
                c.JugadorEsGuardian = false;
                c.HumanosReales = Mathf.Clamp(humanosRealesConectados, 1, cupoHumanosEnModoHistoria);
                c.BotsHumanos = cupoHumanosEnModoHistoria - c.HumanosReales;
                c.GuardianesIA = 1;
                c.MultiplicadorVidaJefe = 2.4f + 0.3f * (nivel - 1);
                c.MultiplicadorDanoJefe = 1f + 0.1f * (nivel - 1);
            }
            else
            {
                // Regla: solo el jugador puede ser Guardián; los demás jugadores reales no ocupan ese rol.
                c.JugadorEsGuardian = true;
                c.HumanosReales = 0;
                c.BotsHumanos = cupoHumanosEnModoHistoria;
                c.GuardianesIA = 0;
            }
            return c;
        }

        public void IniciarNivel(int numeroNivel, BandoJuego bando)
        {
            IniciarNivel(numeroNivel, bando, jugadoresHumanosRealesConectados);
        }

        public void IniciarNivel(int numeroNivel, BandoJuego bando, int humanosReales)
        {
            NivelActual = Mathf.Clamp(numeroNivel, 1, totalNiveles);
            BandoElegido = bando;
            jugadoresHumanosRealesConectados = humanosReales;
            ElementoNivelActual = ObtenerElementoPorNivel(NivelActual);
            BiomaActual = (BiomaPlaneta)(NivelActual - 1);
            Narrativa = DatosNarrativaHistoria.ObtenerNivel(NivelActual);

            ComposicionNivelHistoria c = CalcularComposicion(NivelActual, bando, humanosReales);
            BotsModsRellenados = c.BotsHumanos;
            string nombreGuardian = NivelActual == 5 ? "Aethernum" : DatosGuardianElemental.CrearPorDefecto(ElementoNivelActual).nombreGuardian;

            if (BandoElegido == BandoJuego.Destructor)
            {
                DescripcionPerspectiva =
                    $"Perspectiva Destructor (Nivel {NivelActual}/{totalNiveles} - {DatosBioma.Obtener(BiomaActual).Nombre}): " +
                    $"escuadrón humano de {cupoHumanosEnModoHistoria} ({c.HumanosReales} humano(s) + {c.BotsHumanos} bot(s)/mod(s)) " +
                    $"extrayendo energía frente a {nombreGuardian}. Vista en primera persona.";
            }
            else
            {
                DescripcionPerspectiva =
                    $"Perspectiva Guardián Cuidador (Nivel {NivelActual}/{totalNiveles} - {nombreGuardian}): solo tú juegas como " +
                    $"Guardián; debes derrotar a los {c.BotsHumanos} humanos para pasar al siguiente nivel. Vista en tercera persona.";
            }

            humanos.Clear();
            guardianes.Clear();
            nivelEnCurso = false;
            resolviendo = false;

            AlIniciarNivelHistoria?.Invoke(NivelActual, ElementoNivelActual, BandoElegido, DescripcionPerspectiva);
        }

        /// <summary>Registra a los participantes una vez creados y arranca la evaluación del nivel.</summary>
        public void RegistrarParticipantes(List<SistemaSaludYTraje> listaHumanos, List<SistemaSaludYTraje> listaGuardianes)
        {
            humanos.Clear();
            guardianes.Clear();
            humanos.AddRange(listaHumanos);
            guardianes.AddRange(listaGuardianes);

            foreach (SistemaSaludYTraje h in humanos) h.AlMorir += ManejarMuerte;
            foreach (SistemaSaludYTraje g in guardianes) g.AlMorir += ManejarMuerte;

            if (GestorEquilibrioEcosistema.Instancia != null)
            {
                GestorEquilibrioEcosistema.Instancia.AlCambiarEnergiaExtraida += ManejarExtraccion;
            }

            SistemaSaludYTraje.BloqueoGlobalDeDano = false;
            nivelEnCurso = true;
            AlCambiarObjetivo?.Invoke(BandoElegido == BandoJuego.Destructor ? Narrativa.ObjetivoDestructor : Narrativa.ObjetivoCuidador);
            BusEventosJuego.PublicarMensaje(Narrativa.Titulo, TipoMensajeSistema.Narrativa);
        }

        /// <summary>
        /// Cancela el nivel en curso sin registrar resultado (al abandonar, reintentar o volver al menú).
        /// Detiene la ventana de simultaneidad pendiente para que no "gane" un nivel cuyos personajes
        /// ya se están destruyendo.
        /// </summary>
        public void CancelarNivel()
        {
            StopAllCoroutines();
            nivelEnCurso = false;
            resolviendo = false;
            foreach (SistemaSaludYTraje h in humanos) if (h != null) h.AlMorir -= ManejarMuerte;
            foreach (SistemaSaludYTraje g in guardianes) if (g != null) g.AlMorir -= ManejarMuerte;
            if (GestorEquilibrioEcosistema.Instancia != null)
            {
                GestorEquilibrioEcosistema.Instancia.AlCambiarEnergiaExtraida -= ManejarExtraccion;
            }
            humanos.Clear();
            guardianes.Clear();
        }

        private void OnDestroy()
        {
            foreach (SistemaSaludYTraje h in humanos) if (h != null) h.AlMorir -= ManejarMuerte;
            foreach (SistemaSaludYTraje g in guardianes) if (g != null) g.AlMorir -= ManejarMuerte;
            if (GestorEquilibrioEcosistema.Instancia != null)
            {
                GestorEquilibrioEcosistema.Instancia.AlCambiarEnergiaExtraida -= ManejarExtraccion;
            }
        }

        // =====================================================================
        // Nivel 3: evaluación de victoria
        // =====================================================================

        private void ManejarMuerte(SistemaSaludYTraje caido)
        {
            if (!nivelEnCurso) return;

            if (caido.EsJugadorLocal && caido.Bando == BandoJuego.Destructor && !TodosMuertos(humanos))
            {
                AlCambiarModoEspectador?.Invoke(true);
            }

            if (resolviendo) return;
            if (TodosMuertos(humanos) || TodosMuertos(guardianes))
            {
                resolviendo = true;
                StartCoroutine(RutinaResolverConVentana());
            }
        }

        private void ManejarExtraccion(float energia)
        {
            if (!nivelEnCurso || resolviendo) return;
            if (energia >= 99.99f)
            {
                resolviendo = true;
                if (BandoElegido == BandoJuego.Destructor)
                {
                    CerrarNivel(ResultadoEnfrentamiento.VictoriaDestructores, "¡Extracción completada al 100%! " + Narrativa.VictoriaDestructor);
                }
                else
                {
                    CerrarNivel(ResultadoEnfrentamiento.VictoriaDestructores, "Los humanos han vaciado la Fuente. " + Narrativa.DerrotaCuidador);
                }
            }
        }

        private IEnumerator RutinaResolverConVentana()
        {
            yield return new WaitForSeconds(ventanaMuerteSimultaneaSegundos);

            bool humanosCaidos = TodosMuertos(humanos);
            bool guardianesCaidos = TodosMuertos(guardianes);

            if (humanosCaidos && guardianesCaidos)
            {
                CerrarNivel(ResultadoEnfrentamiento.Empate, "Caída simultánea de ambos bandos: el nivel queda en EMPATE y debe repetirse.");
            }
            else if (humanosCaidos)
            {
                string texto = BandoElegido == BandoJuego.Cuidador ? Narrativa.VictoriaCuidador : Narrativa.DerrotaDestructor;
                CerrarNivel(ResultadoEnfrentamiento.VictoriaCuidadores, texto);
            }
            else if (guardianesCaidos)
            {
                string texto = BandoElegido == BandoJuego.Destructor ? Narrativa.VictoriaDestructor : Narrativa.DerrotaCuidador;
                CerrarNivel(ResultadoEnfrentamiento.VictoriaDestructores, texto);
            }
            else
            {
                resolviendo = false;
            }
        }

        private void CerrarNivel(ResultadoEnfrentamiento resultado, string mensaje)
        {
            nivelEnCurso = false;
            SistemaSaludYTraje.BloqueoGlobalDeDano = true;

            bool gano = resultado == UtilidadesTipos.VictoriaDe(BandoElegido);
            float integridad = GestorEquilibrioEcosistema.Instancia != null ? GestorEquilibrioEcosistema.Instancia.Integridad : 100f;

            if (gano)
            {
                GuardadoProgreso.GuardarIntegridadNivel(BandoElegido, NivelActual, integridad);
                if (BandoElegido == BandoJuego.Cuidador)
                {
                    RegistrarVictoriaDelGuardianSobreHumanos();
                }
                else if (NivelActual < totalNiveles)
                {
                    GuardadoProgreso.DesbloquearNivel(BandoElegido, NivelActual + 1);
                }
            }

            bool avanza = gano && NivelActual < totalNiveles;
            AlFinalizarNivel?.Invoke(resultado, mensaje, avanza);

            if (gano && NivelActual >= totalNiveles)
            {
                GuardadoProgreso.MarcarHistoriaCompletada(BandoElegido);
                string desenlace = DatosNarrativaHistoria.ObtenerDesenlace(BandoElegido, GuardadoProgreso.ObtenerPromedioIntegridad(BandoElegido));
                AlCompletarHistoria?.Invoke(desenlace);
            }
        }

        /// <summary>
        /// Regla del Guardián: solo avanza de nivel si les ganó a los humanos. Desbloquea el siguiente
        /// nivel (el GestorFlujoPartida lo inicia cuando el jugador pulsa "Siguiente nivel").
        /// </summary>
        public void RegistrarVictoriaDelGuardianSobreHumanos()
        {
            if (BandoElegido != BandoJuego.Cuidador) return;
            if (NivelActual < totalNiveles)
            {
                GuardadoProgreso.DesbloquearNivel(BandoJuego.Cuidador, NivelActual + 1);
            }
        }

        private static bool TodosMuertos(List<SistemaSaludYTraje> lista)
        {
            if (lista.Count == 0) return false;
            foreach (SistemaSaludYTraje e in lista)
            {
                if (e != null && !e.EstaMuerto) return false;
            }
            return true;
        }

        /// <summary>Compañeros humanos vivos (para la cámara de espectador en perspectiva Destructor).</summary>
        public List<SistemaSaludYTraje> ObtenerHumanosVivos()
        {
            List<SistemaSaludYTraje> vivos = new List<SistemaSaludYTraje>();
            foreach (SistemaSaludYTraje h in humanos) if (h != null && !h.EstaMuerto) vivos.Add(h);
            return vivos;
        }
    }
}
