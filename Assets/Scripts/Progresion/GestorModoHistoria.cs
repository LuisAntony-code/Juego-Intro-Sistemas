using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace VoidformBioForge
{
    /// <summary>Un guardián concreto que participa en un nivel de Historia.</summary>
    public struct EspecGuardian
    {
        public TipoElemento Elemento;
        public RolGuardian Rol;
        public bool RotaElementos;
        public bool EsJugador;

        public EspecGuardian(TipoElemento elemento, RolGuardian rol, bool rotaElementos)
        {
            Elemento = elemento;
            Rol = rol;
            RotaElementos = rotaElementos;
            EsJugador = false;
        }

        public DatosGuardianElemental CrearDatos()
        {
            return RotaElementos ? DatosGuardianElemental.CrearNexus(Elemento, Rol) : DatosGuardianElemental.CrearVariante(Elemento, Rol);
        }

        public string Nombre => CrearDatos().nombreGuardian;
    }

    /// <summary>
    /// Cuántos personajes y de qué tipo forman un nivel de Historia según la perspectiva elegida.
    /// </summary>
    public struct ComposicionNivelHistoria
    {
        public bool JugadorEsGuardian;
        public int HumanosReales;
        public int BotsHumanos;
        public List<EspecGuardian> Guardianes;
        public float MultiplicadorVidaGuardianesIA;
        public float MultiplicadorDanoGuardianesIA;
        public float DificultadBots;
        public TipoTraje TrajeBotsHumanos;

        public string DescribirGuardianes()
        {
            if (Guardianes == null || Guardianes.Count == 0) return "";
            string texto = "";
            foreach (EspecGuardian g in Guardianes)
            {
                if (texto.Length > 0) texto += ", ";
                texto += g.Nombre + (g.EsJugador ? " (tú)" : "");
            }
            return texto;
        }
    }

    /// <summary>
    /// Gestiona el Modo Historia de 6 niveles con cambio de escenario, elección de bando y distintas perspectivas.
    /// -----------------------------------------------------------------
    /// Nivel 1: 6 niveles (Bosque de los Susurros, Abismos de las Mareas, Caldera de las Forjas, Agujas de Aleación,
    ///          Nexus Primordial y Convergencia Total) y elección de bando.
    /// Nivel 2: REGLAS DE ENFRENTAMIENTO:
    ///   • Bando humano (Destructor): escuadrón de 4 humanos (jugadores reales + bots/mods hasta completar 4)
    ///     contra 3 GUARDIANES del elemento del escenario en los niveles 1 a 5 (una variante de cada rol:
    ///     vanguardia, control y artillería), y contra 5 GUARDIANES en el nivel 6 (uno de cada escenario).
    ///   • Bando guardián (Cuidador): 2 GUARDIANES (el jugador + un aliado de su linaje controlado por la IA)
    ///     contra 4 humanos bot. Para pasar de nivel hay que ganarles.
    ///   • Atributos: todo guardián tiene más vida que cualquier humano y un poco menos de velocidad.
    ///   Perspectiva jugable: Destructor en primera persona; Guardián en tercera persona.
    /// Nivel 3: validación de victoria y avance:
    ///   • Victoria Cuidador = los 4 humanos eliminados. Derrota = caen todos los guardianes o los humanos extraen el 100%.
    ///   • Victoria Destructor = todos los guardianes derrotados o extracción al 100% (con coste ecológico).
    ///     Derrota = los 4 humanos eliminados (en Historia nadie reaparece; quien cae espectea a su equipo).
    ///   • Muerte simultánea del último de cada bando (ventana de 0,25 s) = EMPATE: se repite el nivel.
    ///   • Escalado por nivel: los guardianes IA ganan vida y daño; los bots humanos ganan precisión y, desde el
    ///     nivel 2, eligen el traje que contrarresta al elemento del escenario (los humanos "aprenden").
    ///   • Se guarda la integridad ecológica de cada nivel; al completar el nivel 6 se muestra el desenlace.
    /// </summary>
    public class GestorModoHistoria : MonoBehaviour
    {
        [SerializeField] private int totalNiveles = GuardadoProgreso.NivelesTotales;
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
        public BiomaPlaneta BiomaActual { get; private set; } = BiomaPlaneta.BosqueSusurros;
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

        /// <summary>Elemento principal del escenario (en el Nexus y la Convergencia, los guardianes rotan).</summary>
        public static TipoElemento ObtenerElementoPorNivel(int nivel)
        {
            switch (nivel)
            {
                case 1: return TipoElemento.Arbol;
                case 2: return TipoElemento.Agua;
                case 3: return TipoElemento.Fuego;
                case 4: return TipoElemento.Metal;
                default: return TipoElemento.Arbol;
            }
        }

        /// <summary>
        /// Guardianes que defienden cada escenario cuando el jugador es HUMANO:
        /// niveles 1-5 → 3 guardianes del linaje del escenario (vanguardia, control y artillería);
        /// nivel 6 → 5 guardianes, uno de cada escenario anterior.
        /// </summary>
        public static List<EspecGuardian> GuardianesDefensores(int nivel)
        {
            List<EspecGuardian> lista = new List<EspecGuardian>();
            switch (Mathf.Clamp(nivel, 1, GuardadoProgreso.NivelesTotales))
            {
                case 3:
                    // La Caldera: dos variantes del linaje Fuego y el Gólem de Chimenea (sub-arquetipo Tierra-Forja
                    // nacido en las forjas del valle) ocupando la plaza de artillería.
                    lista.Add(new EspecGuardian(TipoElemento.Fuego, RolGuardian.Vanguardia, false));
                    lista.Add(new EspecGuardian(TipoElemento.Fuego, RolGuardian.Control, false));
                    lista.Add(new EspecGuardian(TipoElemento.Tierra, RolGuardian.Artilleria, false));
                    break;
                case 5:
                    lista.Add(new EspecGuardian(TipoElemento.Arbol, RolGuardian.Vanguardia, true));
                    lista.Add(new EspecGuardian(TipoElemento.Agua, RolGuardian.Control, true));
                    lista.Add(new EspecGuardian(TipoElemento.Fuego, RolGuardian.Artilleria, true));
                    break;
                case 6:
                    lista.Add(new EspecGuardian(TipoElemento.Arbol, RolGuardian.Vanguardia, false));
                    lista.Add(new EspecGuardian(TipoElemento.Agua, RolGuardian.Vanguardia, false));
                    lista.Add(new EspecGuardian(TipoElemento.Fuego, RolGuardian.Vanguardia, false));
                    lista.Add(new EspecGuardian(TipoElemento.Metal, RolGuardian.Vanguardia, false));
                    lista.Add(new EspecGuardian(TipoElemento.Metal, RolGuardian.Control, true));
                    break;
                default:
                    TipoElemento e = ObtenerElementoPorNivel(nivel);
                    lista.Add(new EspecGuardian(e, RolGuardian.Vanguardia, false));
                    lista.Add(new EspecGuardian(e, RolGuardian.Control, false));
                    lista.Add(new EspecGuardian(e, RolGuardian.Artilleria, false));
                    break;
            }
            return lista;
        }

        /// <summary>
        /// Pareja de guardianes cuando el jugador es GUARDIÁN: el primero es el jugador y el segundo su aliado IA.
        /// </summary>
        public static List<EspecGuardian> ParejaDelJugadorGuardian(int nivel)
        {
            List<EspecGuardian> lista = new List<EspecGuardian>();
            switch (Mathf.Clamp(nivel, 1, GuardadoProgreso.NivelesTotales))
            {
                case 5:
                    lista.Add(new EspecGuardian(TipoElemento.Arbol, RolGuardian.Vanguardia, true));
                    lista.Add(new EspecGuardian(TipoElemento.Agua, RolGuardian.Control, true));
                    break;
                case 6:
                    lista.Add(new EspecGuardian(TipoElemento.Arbol, RolGuardian.Control, true));
                    lista.Add(new EspecGuardian(TipoElemento.Metal, RolGuardian.Vanguardia, false));
                    break;
                default:
                    TipoElemento e = ObtenerElementoPorNivel(nivel);
                    lista.Add(new EspecGuardian(e, RolGuardian.Vanguardia, false));
                    lista.Add(new EspecGuardian(e, RolGuardian.Control, false));
                    break;
            }
            EspecGuardian jugador = lista[0];
            jugador.EsJugador = true;
            lista[0] = jugador;
            return lista;
        }

        /// <summary>Calcula quién juega el nivel sin crear nada (la usa la interfaz y el GestorFlujoPartida).</summary>
        public ComposicionNivelHistoria CalcularComposicion(int nivel, BandoJuego bando, int humanosRealesConectados)
        {
            nivel = Mathf.Clamp(nivel, 1, totalNiveles);
            TipoElemento principal = ObtenerElementoPorNivel(nivel);
            ComposicionNivelHistoria c = new ComposicionNivelHistoria
            {
                MultiplicadorVidaGuardianesIA = 1f,
                MultiplicadorDanoGuardianesIA = 1f,
                DificultadBots = Mathf.Clamp01(0.35f + 0.1f * (nivel - 1)),
                TrajeBotsHumanos = nivel == 1 || nivel >= 5 ? TipoTraje.CineticoAsalto : DatosTraje.TrajeContrario(principal)
            };

            if (bando == BandoJuego.Destructor)
            {
                c.JugadorEsGuardian = false;
                c.HumanosReales = Mathf.Clamp(humanosRealesConectados, 1, cupoHumanosEnModoHistoria);
                c.BotsHumanos = cupoHumanosEnModoHistoria - c.HumanosReales;
                c.Guardianes = GuardianesDefensores(nivel);
                // 3 guardianes con más vida que los humanos ya equilibran el 4 contra 3; el escalado es suave.
                c.MultiplicadorVidaGuardianesIA = 1f + 0.06f * (nivel - 1);
                c.MultiplicadorDanoGuardianesIA = 1f + 0.05f * (nivel - 1);
            }
            else
            {
                c.JugadorEsGuardian = true;
                c.HumanosReales = 0;
                c.BotsHumanos = cupoHumanosEnModoHistoria;
                c.Guardianes = ParejaDelJugadorGuardian(nivel);
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

            if (BandoElegido == BandoJuego.Destructor)
            {
                DescripcionPerspectiva =
                    $"Perspectiva Destructor (Nivel {NivelActual}/{totalNiveles} - {DatosBioma.Obtener(BiomaActual).Nombre}): " +
                    $"escuadrón de {cupoHumanosEnModoHistoria} ({c.HumanosReales} humano(s) + {c.BotsHumanos} bot(s)/mod(s)) contra " +
                    $"{c.Guardianes.Count} guardianes: {c.DescribirGuardianes()}. Vista en primera persona.";
            }
            else
            {
                DescripcionPerspectiva =
                    $"Perspectiva Guardián (Nivel {NivelActual}/{totalNiveles} - {DatosBioma.Obtener(BiomaActual).Nombre}): " +
                    $"2 guardianes ({c.DescribirGuardianes()}) contra {c.BotsHumanos} humanos. Debes ganarles para avanzar. " +
                    "Vista en tercera persona.";
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

            // Quien cae en Historia no reaparece: si su equipo sigue en pie, espectea a sus compañeros
            // (humanos de su escuadrón o el guardián aliado).
            List<SistemaSaludYTraje> equipoPropio = caido.Bando == BandoJuego.Destructor ? humanos : guardianes;
            if (caido.EsJugadorLocal && !TodosMuertos(equipoPropio))
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
