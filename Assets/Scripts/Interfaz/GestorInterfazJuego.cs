using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace VoidformBioForge
{
    /// <summary>
    /// Interfaz completa de Voidform: Bio-Forge, construida por código y conectada por eventos.
    /// Este archivo contiene los MENÚS; el HUD de combate está en GestorInterfazJuego.HUD.cs (clase parcial).
    /// -----------------------------------------------------------------
    /// Nivel 1: menú principal, selector de bando (Destructor / Cuidador), selector de modo (Historia,
    ///          Conquista, 4vs4), selección de sala (Crear sala, Con bots, Con amigos, Sala pública).
    /// Nivel 2: personalización (traje y arma o elemento), selector de los 5 niveles con bloqueo, briefing
    ///          narrativo con perspectiva y reglas, pausa y pantalla de resultados con copas/rango/liga.
    /// Nivel 3: la interfaz nunca toca la lógica directamente: llama a GestorFlujoPartida y escucha sus
    ///          eventos, los de GestorSalasYEmparejamiento, SistemaClasificacionYCopas y BusEventosJuego.
    /// </summary>
    public partial class GestorInterfazJuego : MonoBehaviour
    {
        private GestorFlujoPartida flujo;
        private Canvas lienzo;
        private RectTransform raiz;
        private readonly Dictionary<EstadoFlujoJuego, GameObject> pantallas = new Dictionary<EstadoFlujoJuego, GameObject>();

        // ---------- Referencias de menús ----------
        private Text textoAvisoModo;
        private Button[] botonesModo;
        private Button[] botonesNivel;
        private Text textoDescripcionNivel;
        private RectTransform grupoTrajesArmas;
        private RectTransform grupoElementos;
        private Button[] botonesTraje;
        private Button[] botonesArma;
        private Button[] botonesElemento;
        private Button[] botonesVariante;
        private Text textoArchivo;
        private Button[] botonesArchivo;
        private Text textoDetalleTraje;
        private Text textoDetalleArma;
        private Text textoDetalleElemento;
        private Button[] botonesSala;
        private Text textoEstadoSala;
        private Text textoJugadoresSala;
        private Text textoDescripcionSala;
        private Button botonPrivacidad;
        private Button botonBioma;
        private Button botonDificultad;
        private Button botonIniciarSala;
        private InputField campoCodigoAmigo;
        private bool salaPrivadaElegida = true;
        private Text textoBriefingTitulo;
        private Text textoBriefingCuerpo;
        private Text textoClasificacionMenu;
        private Text textoResultadoTitulo;
        private Text textoResultadoCuerpo;
        private Text textoResultadoCopas;
        private Text textoResultadoDesenlace;
        private Button botonSiguienteNivel;

        private static readonly string[] NombresDificultad = { "Fácil", "Normal", "Difícil", "Experta" };
        private static readonly float[] ValoresDificultad = { 0.2f, 0.5f, 0.75f, 0.95f };
        private int indiceDificultad = 1;

        // =====================================================================
        // Ciclo de vida
        // =====================================================================

        private void Awake()
        {
            flujo = GetComponent<GestorFlujoPartida>();
            if (flujo == null) flujo = FindFirstObjectByType<GestorFlujoPartida>();

            FabricaInterfaz.AsegurarSistemaDeEventos();
            lienzo = FabricaInterfaz.CrearLienzo("Interfaz Voidform", 10);
            raiz = (RectTransform)lienzo.transform;

            ConstruirHUD();
            ConstruirMenuPrincipal();
            ConstruirSeleccionBando();
            ConstruirSeleccionModo();
            ConstruirSeleccionNivel();
            ConstruirPersonalizacion();
            ConstruirSeleccionSala();
            ConstruirBriefing();
            ConstruirPausa();
            ConstruirResultados();
            ConstruirHistoriaYProblematica();
            ConstruirControles();
            ConstruirClasificacion();
        }

        private void OnEnable()
        {
            if (flujo != null)
            {
                flujo.AlCambiarEstado += MostrarPantalla;
                flujo.AlPartidaConstruida += VincularPartida;
                flujo.AlPartidaDestruida += DesvincularPartida;
                flujo.AlResultadosListos += MostrarResultados;
            }
            BusEventosJuego.AlMensajeSistema += ManejarMensajeSistema;
            BusEventosJuego.AlRegistrarBaja += ManejarBaja;
        }

        private void Start()
        {
            // Las suscripciones a salas y clasificación se hacen en Start porque el GestorFlujoPartida
            // crea esos componentes en su Awake.
            if (flujo != null)
            {
                flujo.Salas.AlCambiarEstadoSala += ManejarEstadoSala;
                flujo.Salas.AlActualizarJugadores += ManejarJugadoresSala;
                flujo.Salas.AlConfigurarSala += ManejarSalaConfigurada;
                flujo.Clasificacion.AlActualizarClasificacion += ManejarClasificacion;
                MostrarPantalla(flujo.Estado);
            }
        }

        private void OnDisable()
        {
            if (flujo != null)
            {
                flujo.AlCambiarEstado -= MostrarPantalla;
                flujo.AlPartidaConstruida -= VincularPartida;
                flujo.AlPartidaDestruida -= DesvincularPartida;
                flujo.AlResultadosListos -= MostrarResultados;
                if (flujo.Salas != null)
                {
                    flujo.Salas.AlCambiarEstadoSala -= ManejarEstadoSala;
                    flujo.Salas.AlActualizarJugadores -= ManejarJugadoresSala;
                    flujo.Salas.AlConfigurarSala -= ManejarSalaConfigurada;
                }
                if (flujo.Clasificacion != null) flujo.Clasificacion.AlActualizarClasificacion -= ManejarClasificacion;
            }
            BusEventosJuego.AlMensajeSistema -= ManejarMensajeSistema;
            BusEventosJuego.AlRegistrarBaja -= ManejarBaja;
            DesvincularPartida();
        }

        private void Update()
        {
            if (flujo == null) return;

            if (Input.GetKeyDown(KeyCode.Escape))
            {
                if (flujo.Estado == EstadoFlujoJuego.EnPartida || flujo.Estado == EstadoFlujoJuego.Pausa) flujo.AlternarPausa();
                else if (flujo.Estado != EstadoFlujoJuego.MenuPrincipal && flujo.Estado != EstadoFlujoJuego.Resultados) flujo.Volver();
            }

            ActualizarHUDTemporal();
        }

        // =====================================================================
        // Navegación entre pantallas
        // =====================================================================

        private void MostrarPantalla(EstadoFlujoJuego estado)
        {
            foreach (KeyValuePair<EstadoFlujoJuego, GameObject> par in pantallas)
            {
                par.Value.SetActive(par.Key == estado);
            }
            bool hudVisible = estado == EstadoFlujoJuego.EnPartida || estado == EstadoFlujoJuego.Pausa;
            if (hud != null) hud.SetActive(hudVisible);

            switch (estado)
            {
                case EstadoFlujoJuego.SeleccionModo: RefrescarSeleccionModo(); break;
                case EstadoFlujoJuego.SeleccionNivelHistoria: RefrescarSeleccionNivel(); break;
                case EstadoFlujoJuego.Personalizacion: RefrescarPersonalizacion(); break;
                case EstadoFlujoJuego.SeleccionSala: RefrescarSeleccionSala(); break;
                case EstadoFlujoJuego.Briefing: RefrescarBriefing(); break;
                case EstadoFlujoJuego.Clasificacion: ManejarClasificacion(flujo.Clasificacion.CopasActuales, flujo.Clasificacion.RangoActual, flujo.Clasificacion.LigaActual); break;
            }
        }

        // =====================================================================
        // Plantillas de pantalla
        // =====================================================================

        private RectTransform CrearPantalla(EstadoFlujoJuego estado, string titulo, string subtitulo, bool conVolver)
        {
            RectTransform pantalla = FabricaInterfaz.CrearRect(raiz, $"Pantalla {estado}");
            FabricaInterfaz.Anclar(pantalla, 0f, 0f, 1f, 1f);
            Image velo = pantalla.gameObject.AddComponent<Image>();
            velo.color = new Color(0.01f, 0.02f, 0.05f, 0.55f);

            Image tarjeta = FabricaInterfaz.CrearPanel(pantalla, "Tarjeta", PaletaVoidform.FondoVacio);
            FabricaInterfaz.Anclar((RectTransform)tarjeta.transform, 0.07f, 0.07f, 0.93f, 0.93f);

            Image franja = FabricaInterfaz.CrearPanel(tarjeta.transform, "Franja de acento", PaletaVoidform.AcentoVoid, false);
            FabricaInterfaz.Anclar((RectTransform)franja.transform, 0f, 1f, 1f, 1f, 18f, -6f, 18f, 0f);

            Text t = FabricaInterfaz.CrearTexto(tarjeta.transform, "Título", titulo, 50, PaletaVoidform.TextoPrincipal, TextAnchor.UpperLeft, FontStyle.Bold);
            FabricaInterfaz.Anclar((RectTransform)t.transform, 0f, 1f, 1f, 1f, 50f, -95f, 50f, 28f);
            Text s = FabricaInterfaz.CrearTexto(tarjeta.transform, "Subtítulo", subtitulo, 24, PaletaVoidform.TextoSecundario, TextAnchor.UpperLeft);
            FabricaInterfaz.Anclar((RectTransform)s.transform, 0f, 1f, 1f, 1f, 52f, -140f, 50f, 96f);

            if (conVolver)
            {
                Button volver = FabricaInterfaz.CrearBoton(tarjeta.transform, "Volver", "◄  Volver", () => flujo.Volver(), PaletaVoidform.PanelClaro, 24);
                FabricaInterfaz.Colocar((RectTransform)volver.transform, new Vector2(0f, 0f), new Vector2(0f, 0f), new Vector2(40f, 30f), new Vector2(220f, 62f));
            }

            RectTransform contenido = FabricaInterfaz.CrearRect(tarjeta.transform, "Contenido");
            FabricaInterfaz.Anclar(contenido, 0f, 0f, 1f, 1f, 50f, 110f, 50f, 160f);

            pantallas[estado] = pantalla.gameObject;
            pantalla.gameObject.SetActive(false);
            return contenido;
        }

        /// <summary>Tarjeta pulsable con título, cuerpo y color de acento (bandos, modos, niveles, salas).</summary>
        private Button CrearTarjeta(Transform padre, string titulo, string cuerpo, Color acento, UnityEngine.Events.UnityAction accion)
        {
            Button b = FabricaInterfaz.CrearBoton(padre, $"Tarjeta {titulo}", "", accion, PaletaVoidform.PanelProfundo, 20);
            Text etiqueta = b.GetComponentInChildren<Text>();
            etiqueta.alignment = TextAnchor.UpperLeft;
            etiqueta.fontStyle = FontStyle.Normal;
            etiqueta.fontSize = 21;
            etiqueta.text = $"<size=32><b><color={PaletaVoidform.Hex(acento)}>{titulo}</color></b></size>\n\n{cuerpo}";
            FabricaInterfaz.Anclar((RectTransform)etiqueta.transform, 0f, 0f, 1f, 1f, 26f, 20f, 26f, 22f);

            Image borde = FabricaInterfaz.CrearPanel(b.transform, "Borde de acento", acento, false);
            FabricaInterfaz.Anclar((RectTransform)borde.transform, 0f, 0f, 0f, 1f, 0f, 10f, -6f, 10f);
            borde.raycastTarget = false;
            return b;
        }

        // =====================================================================
        // Nivel 1: menú principal, bando y modo
        // =====================================================================

        private void ConstruirMenuPrincipal()
        {
            RectTransform pantalla = FabricaInterfaz.CrearRect(raiz, "Pantalla MenuPrincipal");
            FabricaInterfaz.Anclar(pantalla, 0f, 0f, 1f, 1f);

            Image lateral = FabricaInterfaz.CrearPanel(pantalla, "Panel lateral", new Color(0.02f, 0.03f, 0.07f, 0.86f), false);
            FabricaInterfaz.Anclar((RectTransform)lateral.transform, 0f, 0f, 0.38f, 1f);

            Text titulo = FabricaInterfaz.CrearTexto(lateral.transform, "Título", "VOIDFORM", 96, PaletaVoidform.TextoPrincipal, TextAnchor.UpperLeft, FontStyle.Bold);
            FabricaInterfaz.Anclar((RectTransform)titulo.transform, 0f, 1f, 1f, 1f, 70f, -200f, 40f, 90f);
            Text subtitulo = FabricaInterfaz.CrearTexto(lateral.transform, "Subtítulo", $"<color={PaletaVoidform.Hex(PaletaVoidform.Cuidador)}>BIO</color>-<color={PaletaVoidform.Hex(PaletaVoidform.Destructor)}>FORGE</color>",
                54, PaletaVoidform.TextoPrincipal, TextAnchor.UpperLeft, FontStyle.Bold);
            FabricaInterfaz.Anclar((RectTransform)subtitulo.transform, 0f, 1f, 1f, 1f, 74f, -270f, 40f, 200f);
            Text lema = FabricaInterfaz.CrearTexto(lateral.transform, "Lema",
                "La energía de un planeta vivo no se conquista:\nse cuida o se pierde.", 24, PaletaVoidform.TextoSecundario, TextAnchor.UpperLeft, FontStyle.Italic);
            FabricaInterfaz.Anclar((RectTransform)lema.transform, 0f, 1f, 1f, 1f, 76f, -350f, 40f, 280f);

            RectTransform columna = FabricaInterfaz.CrearColumna(lateral.transform, "Botones", 16f, 0);
            FabricaInterfaz.Anclar(columna, 0f, 0f, 1f, 1f, 70f, 90f, 70f, 400f);

            FabricaInterfaz.Medidas(FabricaInterfaz.CrearBoton(columna, "Jugar", "JUGAR", () => flujo.IrA(EstadoFlujoJuego.SeleccionBando), PaletaVoidform.AcentoVoid, 32), 84f);
            FabricaInterfaz.Medidas(FabricaInterfaz.CrearBoton(columna, "Historia", "Archivo del mundo (lore, escenarios, guardianes)", () => flujo.IrA(EstadoFlujoJuego.HistoriaYProblematica), PaletaVoidform.PanelClaro), 66f);
            FabricaInterfaz.Medidas(FabricaInterfaz.CrearBoton(columna, "Clasificación", "Clasificación 4vs4 (copas · rango · liga)", () => flujo.IrA(EstadoFlujoJuego.Clasificacion), PaletaVoidform.PanelClaro), 66f);
            FabricaInterfaz.Medidas(FabricaInterfaz.CrearBoton(columna, "Controles", "Controles", () => flujo.IrA(EstadoFlujoJuego.Controles), PaletaVoidform.PanelClaro), 66f);
            FabricaInterfaz.Medidas(FabricaInterfaz.CrearBoton(columna, "Salir", "Salir", () => flujo.SalirDelJuego(), new Color(0.35f, 0.12f, 0.14f, 0.95f)), 66f);

            Text pie = FabricaInterfaz.CrearTexto(pantalla, "Pie", "Unity · C# · Prototipo jugable generado por código · Todos los textos en español",
                18, PaletaVoidform.TextoSecundario, TextAnchor.LowerRight);
            FabricaInterfaz.Anclar((RectTransform)pie.transform, 0.4f, 0f, 1f, 0f, 0f, 20f, 30f, -60f);

            pantallas[EstadoFlujoJuego.MenuPrincipal] = pantalla.gameObject;
            pantalla.gameObject.SetActive(false);
        }

        private void ConstruirSeleccionBando()
        {
            RectTransform c = CrearPantalla(EstadoFlujoJuego.SeleccionBando, "Elige tu bando",
                "Nadie es malvado. Todos tienen razones. ¿Salvarás a la humanidad o al planeta que la acoge?", true);
            RectTransform fila = FabricaInterfaz.CrearFila(c, "Bandos", 40f, 0);
            FabricaInterfaz.Anclar(fila, 0f, 0f, 1f, 1f, 0f, 20f, 0f, 0f);

            CrearTarjeta(fila, "DESTRUCTORES · Expedición humana",
                "Operativos de la nave-arca Bio-Forge con exotrajes y armas extractoras. Ritmo ágil de arena shooter en PRIMERA PERSONA.\n\n" +
                "• 5 trajes con habilidad propia (tecla E) y resistencias elementales.\n" +
                "• 5 armas con cargador, recarga, retroceso y habilidad (tecla Q).\n" +
                "• REGLA: si la vida del traje llega a 0, el traje EXPLOTA y el humano muere (daña a los guardianes cercanos).\n\n" +
                "Su dilema: la Tierra se apaga y esta energía podría salvarla... ¿a cualquier precio?",
                PaletaVoidform.Destructor, () => flujo.ElegirBando(BandoJuego.Destructor));

            CrearTarjeta(fila, "CUIDADORES · Guardianes elementales",
                "Guardianes nativos que NACEN elementales (no se transforman). Colosos en TERCERA PERSONA de cinco linajes: Coloso Silvestre (Madera), " +
                "Leviatán Abisal (Agua), Titán Ígneo (Fuego), Androide de Aleación (Metal) y los Custodios del Nexus, más el Gólem de Chimenea.\n\n" +
                "• Más vida que cualquier humano y un poco menos de velocidad.\n" +
                "• Golpe básico + 3 habilidades temáticas con enfriamiento.\n" +
                "• REGLA: en modos versus usan MANÁ; sin maná suficiente no pueden lanzar la habilidad.\n" +
                "• Más fuertes en su bioma (afinidad).\n\n" +
                "Su dilema: defender el equilibrio sin convertirse en aquello que combaten.",
                PaletaVoidform.Cuidador, () => flujo.ElegirBando(BandoJuego.Cuidador));
        }

        private void ConstruirSeleccionModo()
        {
            RectTransform c = CrearPantalla(EstadoFlujoJuego.SeleccionModo, "Elige el modo de juego", "", true);
            RectTransform fila = FabricaInterfaz.CrearFila(c, "Modos", 30f, 0);
            FabricaInterfaz.Anclar(fila, 0f, 0.12f, 1f, 1f);

            botonesModo = new Button[3];
            botonesModo[0] = CrearTarjeta(fila, "MODO HISTORIA",
                "Campaña de 6 niveles: Bosque, Abismos, Caldera, Agujas, Nexus y la Convergencia Total.\n\n" +
                "• Como Humano: escuadrón de 4 (bots/mods rellenan) contra 3 guardianes del escenario; 5 en el nivel final.\n" +
                "• Como Guardián: tú y un guardián aliado contra 4 humanos; debes ganarles para avanzar.\n" +
                "• El final depende del daño ecológico que causes.",
                PaletaVoidform.AcentoVoid, () => flujo.ElegirModo(ModoJuego.Historia));
            botonesModo[1] = CrearTarjeta(fila, "CONQUISTA · 5 bases / 5:00",
                "4 contra 4 en un mapa con 5 bases y reloj de 5 minutos. Al morir (o explotar tu traje) reapareces.\n\n" +
                "• Dominar las 5 bases a la vez = VICTORIA INMEDIATA.\n" +
                "• Al llegar a 0:00 gana quien controle MÁS bases.\n" +
                "• Desempate: tiempo de control acumulado → influencia parcial → empate.",
                PaletaVoidform.Disputa, () => flujo.ElegirModo(ModoJuego.Conquista5Bases));
            botonesModo[2] = CrearTarjeta(fila, "4 VS 4 · 3 rondas",
                "Eliminación por equipos a 3 rondas. Si mueres no revives hasta la siguiente ronda: mientras tanto ESPECTEAS a tus compañeros.\n\n" +
                "• Muerte simultánea de ambos equipos = ronda empatada.\n" +
                "• Igualdad en rondas ganadas = partida empatada.\n" +
                "• Clasificatoria: ganas copas, subes de rango y de liga.",
                PaletaVoidform.Alerta, () => flujo.ElegirModo(ModoJuego.Modo4vs4));

            textoAvisoModo = FabricaInterfaz.CrearTexto(c, "Aviso", "", 22, PaletaVoidform.TextoSecundario, TextAnchor.MiddleLeft);
            FabricaInterfaz.Anclar((RectTransform)textoAvisoModo.transform, 0f, 0f, 1f, 0.1f, 240f, 0f, 0f, 0f);
        }

        private void RefrescarSeleccionModo()
        {
            BandoJuego b = flujo.Configuracion.bandoJugador;
            textoAvisoModo.text = b == BandoJuego.Destructor
                ? "Juegas como DESTRUCTOR: primera persona, trajes y armas. En Historia te acompaña un escuadrón de 4."
                : "Juegas como CUIDADOR: tercera persona, maná y habilidades elementales. En Historia eres el único Guardián.";
        }

        // =====================================================================
        // Nivel 2: niveles de historia
        // =====================================================================

        private void ConstruirSeleccionNivel()
        {
            RectTransform c = CrearPantalla(EstadoFlujoJuego.SeleccionNivelHistoria, "Modo Historia — Elige un nivel",
                "Cinco biomas alrededor de la Fuente de Energía Viva. Cada victoria desbloquea el siguiente nivel.", true);
            RectTransform fila = FabricaInterfaz.CrearFila(c, "Niveles", 18f, 0);
            FabricaInterfaz.Anclar(fila, 0f, 0.3f, 1f, 1f);

            botonesNivel = new Button[GuardadoProgreso.NivelesTotales];
            for (int i = 0; i < GuardadoProgreso.NivelesTotales; i++)
            {
                int nivel = i + 1;
                NivelNarrativo n = DatosNarrativaHistoria.ObtenerNivel(nivel);
                DatosBioma db = DatosBioma.Obtener(n.Bioma);
                botonesNivel[i] = CrearTarjeta(fila, $"Nivel {nivel}", "", db.ColorAcento, () => flujo.ElegirNivelHistoria(nivel));
            }

            textoDescripcionNivel = FabricaInterfaz.CrearTexto(c, "Descripción", "", 21, PaletaVoidform.TextoPrincipal, TextAnchor.UpperLeft);
            FabricaInterfaz.Anclar((RectTransform)textoDescripcionNivel.transform, 0f, 0f, 1f, 0.27f, 240f, 0f, 0f, 0f);
        }

        private void RefrescarSeleccionNivel()
        {
            BandoJuego b = flujo.Configuracion.bandoJugador;
            int desbloqueado = GuardadoProgreso.ObtenerNivelDesbloqueado(b);
            for (int i = 0; i < GuardadoProgreso.NivelesTotales; i++)
            {
                int nivel = i + 1;
                NivelNarrativo n = DatosNarrativaHistoria.ObtenerNivel(nivel);
                DatosBioma db = DatosBioma.Obtener(n.Bioma);
                bool abierto = nivel <= desbloqueado;
                float integridad = GuardadoProgreso.ObtenerIntegridadNivel(b, nivel);
                string estado = !abierto ? "<color=#FF6B6B>BLOQUEADO</color>"
                    : integridad >= 0f ? $"<color=#7CFFB0>Superado · ecosistema {Mathf.RoundToInt(integridad)}%</color>"
                    : "<color=#FFE36B>Disponible</color>";
                int numeroGuardianes = b == BandoJuego.Destructor ? GestorModoHistoria.GuardianesDefensores(nivel).Count : 2;
                string guardian = nivel == 6 ? "Los 5 linajes reunidos"
                    : nivel == 5 ? "Custodios del Nexus"
                    : GestorModoHistoria.GuardianesDefensores(nivel)[0].CrearDatos().linaje.Replace("Linaje del ", "");

                Text t = botonesNivel[i].GetComponentInChildren<Text>();
                t.text = $"<size=30><b><color={PaletaVoidform.Hex(db.ColorAcento)}>Nivel {nivel}</color></b></size>\n{n.Titulo.Replace($"Nivel {nivel} — ", "")}\n\n" +
                         $"<color=#A9B4C8>{db.Nombre}</color>\n<size=17>Fuente: {db.NombreFuente}\nGuardianes: {guardian} ({numeroGuardianes})</size>\n\n{estado}";
                botonesNivel[i].interactable = abierto;
            }

            textoDescripcionNivel.text = b == BandoJuego.Destructor
                ? "<b>Perspectiva Destructor:</b> escuadrón de 4 humanos (los huecos se rellenan con bots/mods) contra 3 guardianes del escenario " +
                  "(vanguardia, control y artillería) y contra 5 —uno de cada escenario— en el nivel 6. Ganas derrotándolos o extrayendo el 100% de la " +
                  "fuente, pero cada punto extraído daña el ecosistema y cambia el final."
                : "<b>Perspectiva Guardián:</b> tú y un guardián aliado (IA) contra 4 humanos bot que aprenden: desde el nivel 2 llevan el traje que " +
                  "resiste tu elemento. Los guardianes tienen más vida pero algo menos de velocidad. Para pasar al siguiente nivel DEBES ganarles.";
        }

        // =====================================================================
        // Nivel 2: personalización
        // =====================================================================

        private void ConstruirPersonalizacion()
        {
            RectTransform c = CrearPantalla(EstadoFlujoJuego.Personalizacion, "Personaliza tu equipamiento",
                "Cada traje y cada elemento tienen fortalezas y debilidades. Adáptate al bioma y al rival.", true);

            // ---- Destructor: trajes y armas ----
            grupoTrajesArmas = FabricaInterfaz.CrearRect(c, "Trajes y armas");
            FabricaInterfaz.Anclar(grupoTrajesArmas, 0f, 0f, 1f, 1f);

            Text rotuloTraje = FabricaInterfaz.CrearTexto(grupoTrajesArmas, "Rótulo traje", "TRAJE (habilidad: tecla E)", 24, PaletaVoidform.Destructor, TextAnchor.MiddleLeft, FontStyle.Bold);
            FabricaInterfaz.Anclar((RectTransform)rotuloTraje.transform, 0f, 0.92f, 1f, 1f);
            RectTransform filaTrajes = FabricaInterfaz.CrearFila(grupoTrajesArmas, "Fila trajes", 12f, 0);
            FabricaInterfaz.Anclar(filaTrajes, 0f, 0.8f, 1f, 0.92f);
            botonesTraje = new Button[5];
            for (int i = 0; i < 5; i++)
            {
                TipoTraje tipo = (TipoTraje)i;
                DatosTraje d = DatosTraje.CrearPorDefecto(tipo);
                botonesTraje[i] = FabricaInterfaz.CrearBoton(filaTrajes, d.nombreTraje, d.nombreTraje, () => { flujo.ElegirTraje(tipo); RefrescarPersonalizacion(); }, PaletaVoidform.PanelProfundo, 19);
            }
            textoDetalleTraje = FabricaInterfaz.CrearTexto(grupoTrajesArmas, "Detalle traje", "", 21, PaletaVoidform.TextoPrincipal, TextAnchor.UpperLeft);
            FabricaInterfaz.Anclar((RectTransform)textoDetalleTraje.transform, 0f, 0.52f, 1f, 0.78f, 4f, 0f, 4f, 0f);

            Text rotuloArma = FabricaInterfaz.CrearTexto(grupoTrajesArmas, "Rótulo arma", "ARMA PRINCIPAL (habilidad: tecla Q · secundaria: Pistola Auxiliar)", 24, PaletaVoidform.Destructor, TextAnchor.MiddleLeft, FontStyle.Bold);
            FabricaInterfaz.Anclar((RectTransform)rotuloArma.transform, 0f, 0.44f, 1f, 0.52f);
            RectTransform filaArmas = FabricaInterfaz.CrearFila(grupoTrajesArmas, "Fila armas", 12f, 0);
            FabricaInterfaz.Anclar(filaArmas, 0f, 0.32f, 1f, 0.44f);
            TipoArma[] armas = { TipoArma.RifleExtractor, TipoArma.SubfusilRafaga, TipoArma.EscopetaDispersora, TipoArma.RiflePrecision, TipoArma.PistolaAuxiliar };
            botonesArma = new Button[armas.Length];
            for (int i = 0; i < armas.Length; i++)
            {
                TipoArma tipo = armas[i];
                DatosArma d = DatosArma.CrearPorDefecto(tipo);
                botonesArma[i] = FabricaInterfaz.CrearBoton(filaArmas, d.nombreArma, d.nombreArma, () => { flujo.ElegirArma(tipo); RefrescarPersonalizacion(); }, PaletaVoidform.PanelProfundo, 19);
            }
            textoDetalleArma = FabricaInterfaz.CrearTexto(grupoTrajesArmas, "Detalle arma", "", 21, PaletaVoidform.TextoPrincipal, TextAnchor.UpperLeft);
            FabricaInterfaz.Anclar((RectTransform)textoDetalleArma.transform, 0f, 0.02f, 1f, 0.3f, 4f, 0f, 4f, 0f);

            // ---- Cuidador: elementos ----
            grupoElementos = FabricaInterfaz.CrearRect(c, "Elementos");
            FabricaInterfaz.Anclar(grupoElementos, 0f, 0f, 1f, 1f);
            Text rotuloElem = FabricaInterfaz.CrearTexto(grupoElementos, "Rótulo elemento", "GUARDIÁN ELEMENTAL (en modos versus)", 24, PaletaVoidform.Cuidador, TextAnchor.MiddleLeft, FontStyle.Bold);
            FabricaInterfaz.Anclar((RectTransform)rotuloElem.transform, 0f, 0.92f, 1f, 1f);
            RectTransform filaElem = FabricaInterfaz.CrearFila(grupoElementos, "Fila elementos", 14f, 0);
            FabricaInterfaz.Anclar(filaElem, 0f, 0.8f, 1f, 0.92f);
            TipoElemento[] elementos = UtilidadesTipos.ElementosJugables;
            botonesElemento = new Button[elementos.Length];
            for (int i = 0; i < elementos.Length; i++)
            {
                TipoElemento e = elementos[i];
                DatosGuardianElemental g = DatosGuardianElemental.CrearPorDefecto(e);
                botonesElemento[i] = FabricaInterfaz.CrearBoton(filaElem, g.linaje, $"{UtilidadesTipos.NombreElemento(e)}\n<size=16>{g.linaje.Replace("Linaje del ", "")}</size>",
                    () => { flujo.ElegirElemento(e); RefrescarPersonalizacion(); }, PaletaVoidform.PanelProfundo, 22);
            }
            RectTransform filaVariante = FabricaInterfaz.CrearFila(grupoElementos, "Fila variantes", 14f, 0);
            FabricaInterfaz.Anclar(filaVariante, 0f, 0.68f, 1f, 0.78f);
            botonesVariante = new Button[3];
            for (int i = 0; i < 3; i++)
            {
                RolGuardian rol = (RolGuardian)i;
                botonesVariante[i] = FabricaInterfaz.CrearBoton(filaVariante, $"Variante {rol}", UtilidadesTipos.NombreRol(rol),
                    () => { flujo.ElegirVarianteGuardian(rol); RefrescarPersonalizacion(); }, PaletaVoidform.PanelProfundo, 20);
            }
            textoDetalleElemento = FabricaInterfaz.CrearTexto(grupoElementos, "Detalle elemento", "", 19, PaletaVoidform.TextoPrincipal, TextAnchor.UpperLeft);
            FabricaInterfaz.Anclar((RectTransform)textoDetalleElemento.transform, 0f, 0.02f, 1f, 0.66f, 4f, 0f, 4f, 0f);

            // ---- Continuar ----
            Transform tarjeta = c.parent;
            Button continuar = FabricaInterfaz.CrearBoton(tarjeta, "Continuar", "Continuar  ►", () => flujo.ContinuarDesdePersonalizacion(), PaletaVoidform.AcentoVoid, 26);
            FabricaInterfaz.Colocar((RectTransform)continuar.transform, new Vector2(1f, 0f), new Vector2(1f, 0f), new Vector2(-40f, 30f), new Vector2(280f, 62f));
        }

        private void RefrescarPersonalizacion()
        {
            ConfiguracionPartida cfg = flujo.Configuracion;
            bool esDestructor = cfg.bandoJugador == BandoJuego.Destructor;
            grupoTrajesArmas.gameObject.SetActive(esDestructor);
            grupoElementos.gameObject.SetActive(!esDestructor);

            if (esDestructor)
            {
                for (int i = 0; i < botonesTraje.Length; i++)
                {
                    FabricaInterfaz.ResaltarBoton(botonesTraje[i], (TipoTraje)i == cfg.trajeDestructor, PaletaVoidform.DestructorOscuro, PaletaVoidform.PanelProfundo);
                }
                DatosTraje t = DatosTraje.CrearPorDefecto(cfg.trajeDestructor);
                textoDetalleTraje.text = $"<b>{t.nombreTraje}</b> — Vida {t.vidaMaxima} · Blindaje {t.blindajeMaximo} · Velocidad ×{t.multiplicadorVelocidad:0.00}\n" +
                                         $"{t.descripcion}\n<color=#FFB37A>Habilidad «{t.nombreHabilidad}» ({t.enfriamientoHabilidad} s):</color> {t.descripcionHabilidad}\n" +
                                         $"<color=#A9B4C8>{t.DescribirResistencias()}</color>";

                TipoArma[] orden = { TipoArma.RifleExtractor, TipoArma.SubfusilRafaga, TipoArma.EscopetaDispersora, TipoArma.RiflePrecision, TipoArma.PistolaAuxiliar };
                for (int i = 0; i < botonesArma.Length; i++)
                {
                    FabricaInterfaz.ResaltarBoton(botonesArma[i], orden[i] == cfg.armaPrincipal, PaletaVoidform.DestructorOscuro, PaletaVoidform.PanelProfundo);
                }
                DatosArma a = DatosArma.CrearPorDefecto(cfg.armaPrincipal);
                string perdigones = a.perdigonesPorDisparo > 1 ? $" ×{a.perdigonesPorDisparo} perdigones" : "";
                textoDetalleArma.text = $"<b>{a.nombreArma}</b> — Daño {a.danoPorImpacto}{perdigones} · Cadencia {a.cadenciaDisparosPorSegundo}/s · Cargador {a.tamanoCargador} " +
                                        $"(reserva {a.municionReservaMaxima}) · Recarga {a.tiempoRecarga} s · Alcance {a.alcanceMaximo} m · {(a.esAutomatica ? "Automática" : "Semiautomática")}\n" +
                                        $"{a.descripcion}\n<color=#FFB37A>Habilidad «{a.nombreHabilidad}» ({a.enfriamientoHabilidad} s):</color> {a.descripcionHabilidad}";
            }
            else
            {
                for (int i = 0; i < botonesElemento.Length; i++)
                {
                    FabricaInterfaz.ResaltarBoton(botonesElemento[i], UtilidadesTipos.ElementosJugables[i] == cfg.elementoGuardian, PaletaVoidform.CuidadorOscuro, PaletaVoidform.PanelProfundo);
                }
                for (int i = 0; i < botonesVariante.Length; i++)
                {
                    FabricaInterfaz.ResaltarBoton(botonesVariante[i], (RolGuardian)i == cfg.varianteGuardian, PaletaVoidform.CuidadorOscuro, PaletaVoidform.PanelProfundo);
                }
                DatosGuardianElemental g = DatosGuardianElemental.CrearVariante(cfg.elementoGuardian, cfg.varianteGuardian);
                textoDetalleElemento.text = $"<size=28><b><color={PaletaVoidform.Hex(g.colorPrimario)}>{g.NombreCompleto}</color></b></size>  <color=#A9B4C8>{g.linaje}</color>\n" +
                                            $"Vida {Mathf.RoundToInt(g.vidaMaxima)} · Blindaje {Mathf.RoundToInt(g.blindajeMaximo)} · Velocidad ×{g.multiplicadorVelocidad:0.00} " +
                                            $"· Radio de habilidades {g.radioEfecto:0.0} m\n" +
                                            $"<i>{g.descripcionVisual}</i>\n" +
                                            $"<color=#A9B4C8>Punto débil: {g.nucleoPuntoDebil}</color>\n" +
                                            $"<b>Clic</b> {g.nombreAtaqueBasico} (sin maná)\n" +
                                            $"<b>E</b> {g.nombreHabilidad1} — 25 maná · 4 s — {g.descripcionHabilidad1}\n" +
                                            $"<b>R</b> {g.nombreHabilidad2} — 35 maná · 7 s — {g.descripcionHabilidad2}\n" +
                                            $"<b>Q</b> {g.nombreDefinitiva} — 65 maná · 16 s — {g.descripcionDefinitiva}";
            }
        }

        // =====================================================================
        // Nivel 2-3: selección de sala
        // =====================================================================

        private void ConstruirSeleccionSala()
        {
            RectTransform c = CrearPantalla(EstadoFlujoJuego.SeleccionSala, "¿Cómo quieres jugar?",
                "Crear sala, con bots, con amigos o sala pública. Los puestos libres siempre se cubren con bots.", true);

            RectTransform fila = FabricaInterfaz.CrearFila(c, "Tipos de sala", 18f, 0);
            FabricaInterfaz.Anclar(fila, 0f, 0.62f, 1f, 1f);
            botonesSala = new Button[4];
            botonesSala[0] = CrearTarjeta(fila, "Crear sala", "Configura visibilidad (privada/pública), bioma y dificultad. Recibes un código para compartir.",
                PaletaVoidform.AcentoVoid, () => SeleccionarSala(TipoSalaConexion.CrearSala));
            botonesSala[1] = CrearTarjeta(fila, "Con bots", "Partida inmediata: todos los demás puestos los ocupan bots/mods con la dificultad elegida.",
                PaletaVoidform.Cuidador, () => SeleccionarSala(TipoSalaConexion.ConBots));
            botonesSala[2] = CrearTarjeta(fila, "Con amigos", "Crea una sala privada para invitar o escribe abajo el código de un amigo (VBF-1234) para unirte.",
                PaletaVoidform.Disputa, () => SeleccionarSala(TipoSalaConexion.ConAmigos));
            botonesSala[3] = CrearTarjeta(fila, "Sala pública", "Entra en una sala pública aleatoria del modo elegido.",
                PaletaVoidform.Destructor, () => SeleccionarSala(TipoSalaConexion.SalaPublicaAleatoria));

            RectTransform opciones = FabricaInterfaz.CrearFila(c, "Opciones", 14f, 0);
            FabricaInterfaz.Anclar(opciones, 0f, 0.5f, 1f, 0.6f);
            botonPrivacidad = FabricaInterfaz.CrearBoton(opciones, "Privacidad", "", () =>
            {
                salaPrivadaElegida = !salaPrivadaElegida;
                RefrescarSeleccionSala();
            }, PaletaVoidform.PanelClaro, 20);
            botonBioma = FabricaInterfaz.CrearBoton(opciones, "Bioma", "", () =>
            {
                int siguiente = flujo.Configuracion.indiceBiomaVersus + 1;
                if (siguiente > 5) siguiente = -1;
                flujo.ElegirBiomaVersus(siguiente);
                RefrescarSeleccionSala();
            }, PaletaVoidform.PanelClaro, 20);
            botonDificultad = FabricaInterfaz.CrearBoton(opciones, "Dificultad", "", () =>
            {
                indiceDificultad = (indiceDificultad + 1) % NombresDificultad.Length;
                flujo.ElegirDificultadBots(ValoresDificultad[indiceDificultad]);
                RefrescarSeleccionSala();
            }, PaletaVoidform.PanelClaro, 20);
            campoCodigoAmigo = FabricaInterfaz.CrearCampoTexto(opciones, "Código de amigo", "Código de amigo (VBF-1234)", 8);

            textoEstadoSala = FabricaInterfaz.CrearTexto(c, "Estado de sala", "Elige un tipo de sala.", 23, PaletaVoidform.Disputa, TextAnchor.UpperLeft, FontStyle.Bold);
            FabricaInterfaz.Anclar((RectTransform)textoEstadoSala.transform, 0f, 0.36f, 0.6f, 0.48f);
            textoDescripcionSala = FabricaInterfaz.CrearTexto(c, "Descripción", "", 20, PaletaVoidform.TextoSecundario, TextAnchor.UpperLeft);
            FabricaInterfaz.Anclar((RectTransform)textoDescripcionSala.transform, 0f, 0.05f, 0.6f, 0.35f);
            textoJugadoresSala = FabricaInterfaz.CrearTexto(c, "Jugadores", "", 20, PaletaVoidform.TextoPrincipal, TextAnchor.UpperLeft);
            FabricaInterfaz.Anclar((RectTransform)textoJugadoresSala.transform, 0.62f, 0.02f, 1f, 0.48f);

            Transform tarjeta = c.parent;
            botonIniciarSala = FabricaInterfaz.CrearBoton(tarjeta, "Iniciar", "Iniciar  ►", () => flujo.ContinuarDesdeSala(), PaletaVoidform.AcentoVoid, 26);
            FabricaInterfaz.Colocar((RectTransform)botonIniciarSala.transform, new Vector2(1f, 0f), new Vector2(1f, 0f), new Vector2(-40f, 30f), new Vector2(280f, 62f));
        }

        private void SeleccionarSala(TipoSalaConexion tipo)
        {
            string codigo = tipo == TipoSalaConexion.ConAmigos && campoCodigoAmigo != null ? campoCodigoAmigo.text : "";
            bool privada = tipo == TipoSalaConexion.CrearSala ? salaPrivadaElegida : tipo != TipoSalaConexion.SalaPublicaAleatoria;
            flujo.SeleccionarSala(tipo, codigo, privada);
            RefrescarSeleccionSala();
        }

        private void RefrescarSeleccionSala()
        {
            ConfiguracionPartida cfg = flujo.Configuracion;
            TipoSalaConexion[] tipos = { TipoSalaConexion.CrearSala, TipoSalaConexion.ConBots, TipoSalaConexion.ConAmigos, TipoSalaConexion.SalaPublicaAleatoria };
            string motivos = "";
            for (int i = 0; i < 4; i++)
            {
                bool valido = GestorSalasYEmparejamiento.EsCombinacionValida(cfg.modo, cfg.bandoJugador, tipos[i], out string motivo);
                botonesSala[i].interactable = valido;
                FabricaInterfaz.ResaltarBoton(botonesSala[i], valido && flujo.Salas.Estado != EstadoSala.SinSala && flujo.Salas.TipoSalaSeleccionado == tipos[i],
                    PaletaVoidform.PanelClaro, PaletaVoidform.PanelProfundo);
                if (!valido && !motivos.Contains(motivo)) motivos += motivo + "\n";
            }

            FabricaInterfaz.CambiarTextoBoton(botonPrivacidad, salaPrivadaElegida ? "Crear sala: PRIVADA" : "Crear sala: PÚBLICA");
            string bioma = cfg.indiceBiomaVersus < 0 ? "Aleatorio" : DatosBioma.Obtener((BiomaPlaneta)cfg.indiceBiomaVersus).Nombre;
            FabricaInterfaz.CambiarTextoBoton(botonBioma, cfg.modo == ModoJuego.Historia ? "Bioma: el del nivel" : $"Bioma: {bioma}");
            botonBioma.interactable = cfg.modo != ModoJuego.Historia;
            FabricaInterfaz.CambiarTextoBoton(botonDificultad, $"Bots: {NombresDificultad[indiceDificultad]}");

            textoDescripcionSala.text =
                $"<b>{UtilidadesTipos.NombreModo(cfg.modo)}</b> como <b>{UtilidadesTipos.NombreBandoSingular(cfg.bandoJugador)}</b>" +
                (cfg.modo == ModoJuego.Historia ? $" · Nivel {cfg.nivelHistoria}" : "") +
                $"\nProveedor de red: {flujo.Salas.NombreProveedor}.\n" +
                (cfg.modo == ModoJuego.Modo4vs4 ? "Clasificatoria: las salas «Con bots» otorgan el 50% de las copas.\n" : "") +
                (motivos.Length > 0 ? $"<color=#FF8A7A>{motivos}</color>" : "");

            botonIniciarSala.interactable = flujo.Salas.PuedeIniciar;
        }

        private void ManejarEstadoSala(EstadoSala estado, string mensaje)
        {
            if (textoEstadoSala == null) return;
            textoEstadoSala.text = $"{UtilidadesTipos.NombreEstadoSala(estado)}{(string.IsNullOrEmpty(mensaje) ? "" : " — " + mensaje)}";
            if (flujo != null && botonIniciarSala != null) botonIniciarSala.interactable = flujo.Salas.PuedeIniciar;
            if (flujo != null && flujo.Estado == EstadoFlujoJuego.SeleccionSala) RefrescarSeleccionSala();
        }

        private void ManejarJugadoresSala(IReadOnlyList<string> jugadores)
        {
            if (textoJugadoresSala == null) return;
            string lista = $"<b>Sala {flujo.Salas.CodigoSalaActual}</b> ({(flujo.Salas.SalaPrivada ? "privada" : "pública")})\n";
            foreach (string j in jugadores) lista += $"• {j}\n";
            textoJugadoresSala.text = lista;
        }

        private void ManejarSalaConfigurada(TipoSalaConexion tipo, string codigo)
        {
            if (campoCodigoAmigo != null && tipo != TipoSalaConexion.ConAmigos) campoCodigoAmigo.text = "";
        }

        // =====================================================================
        // Nivel 2: briefing del Modo Historia
        // =====================================================================

        private void ConstruirBriefing()
        {
            RectTransform c = CrearPantalla(EstadoFlujoJuego.Briefing, "Briefing", "", true);
            textoBriefingTitulo = FabricaInterfaz.CrearTexto(c, "Título del nivel", "", 34, PaletaVoidform.TextoPrincipal, TextAnchor.UpperLeft, FontStyle.Bold);
            FabricaInterfaz.Anclar((RectTransform)textoBriefingTitulo.transform, 0f, 0.9f, 1f, 1f);
            textoBriefingCuerpo = FabricaInterfaz.CrearTexto(c, "Cuerpo", "", 21, PaletaVoidform.TextoPrincipal, TextAnchor.UpperLeft);
            FabricaInterfaz.Anclar((RectTransform)textoBriefingCuerpo.transform, 0f, 0f, 1f, 0.88f);

            Button comenzar = FabricaInterfaz.CrearBoton(c.parent, "Comenzar", "Comenzar nivel  ►", () => flujo.ComenzarDesdeBriefing(), PaletaVoidform.AcentoVoid, 26);
            FabricaInterfaz.Colocar((RectTransform)comenzar.transform, new Vector2(1f, 0f), new Vector2(1f, 0f), new Vector2(-40f, 30f), new Vector2(320f, 62f));
        }

        private void RefrescarBriefing()
        {
            ConfiguracionPartida cfg = flujo.Configuracion;
            NivelNarrativo n = DatosNarrativaHistoria.ObtenerNivel(cfg.nivelHistoria);
            DatosBioma db = DatosBioma.Obtener(n.Bioma);
            bool esDestructor = cfg.bandoJugador == BandoJuego.Destructor;
            ComposicionNivelHistoria comp = flujo.Historia.CalcularComposicion(cfg.nivelHistoria, cfg.bandoJugador, cfg.jugadoresHumanosReales);

            string perspectiva = esDestructor
                ? $"PERSPECTIVA DESTRUCTOR · primera persona · escuadrón de 4 = {comp.HumanosReales} humano(s) real(es) + {comp.BotsHumanos} bot(s)/mod(s). " +
                  $"Rivales: {comp.Guardianes.Count} guardianes ({comp.DescribirGuardianes()}), vida ×{comp.MultiplicadorVidaGuardianesIA:0.00} y daño ×{comp.MultiplicadorDanoGuardianesIA:0.00}."
                : $"PERSPECTIVA GUARDIÁN · tercera persona · 2 guardianes: {comp.DescribirGuardianes()}. Rival: 4 humanos bot (precisión {Mathf.RoundToInt(comp.DificultadBots * 100)}%, " +
                  $"traje {DatosTraje.CrearPorDefecto(comp.TrajeBotsHumanos).nombreTraje}). Para avanzar DEBES ganarles.";

            textoBriefingTitulo.text = $"<color={PaletaVoidform.Hex(db.ColorAcento)}>{n.Titulo}</color>";
            textoBriefingCuerpo.text =
                $"<b>{db.Nombre}.</b> {db.Descripcion}\n<color=#A9B4C8>Peligro del bioma — {db.NombrePeligro}: {db.DescripcionPeligro}</color>\n\n" +
                $"<color=#FFE36B>{perspectiva}</color>\n\n" +
                $"{(esDestructor ? n.IntroDestructor : n.IntroCuidador)}\n\n" +
                $"<b>Objetivo:</b> {(esDestructor ? n.ObjetivoDestructor : n.ObjetivoCuidador)}\n" +
                "<b>Reglas:</b> en Historia nadie reaparece (si caes, espectea a tu equipo). Caída simultánea de ambos bandos = empate y se repite el nivel." +
                (cfg.nivelHistoria >= 5 && !esDestructor ? " Pulsa F para rotar entre los cuatro elementos." : "") +
                (cfg.nivelHistoria == 5 ? " Gravedad reducida: saltos más altos y caídas lentas." : "") + "\n\n" +
                $"<color=#7CFFB0><i>Reflexión: {n.ReflexionSocial}</i></color>";
        }

        // =====================================================================
        // Nivel 2: pausa y resultados
        // =====================================================================

        private void ConstruirPausa()
        {
            RectTransform pantalla = FabricaInterfaz.CrearRect(raiz, "Pantalla Pausa");
            FabricaInterfaz.Anclar(pantalla, 0f, 0f, 1f, 1f);
            Image velo = pantalla.gameObject.AddComponent<Image>();
            velo.color = new Color(0f, 0f, 0f, 0.6f);

            Image tarjeta = FabricaInterfaz.CrearPanel(pantalla, "Tarjeta", PaletaVoidform.FondoVacio);
            FabricaInterfaz.Colocar((RectTransform)tarjeta.transform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(620f, 430f));
            Text t = FabricaInterfaz.CrearTexto(tarjeta.transform, "Título", "PAUSA", 52, PaletaVoidform.TextoPrincipal, TextAnchor.UpperCenter, FontStyle.Bold);
            FabricaInterfaz.Anclar((RectTransform)t.transform, 0f, 1f, 1f, 1f, 0f, -100f, 0f, 30f);

            RectTransform col = FabricaInterfaz.CrearColumna(tarjeta.transform, "Botones", 16f, 0);
            FabricaInterfaz.Anclar(col, 0f, 0f, 1f, 1f, 80f, 50f, 80f, 130f);
            FabricaInterfaz.Medidas(FabricaInterfaz.CrearBoton(col, "Continuar", "Continuar", () => flujo.AlternarPausa(), PaletaVoidform.AcentoVoid, 28), 70f);
            FabricaInterfaz.Medidas(FabricaInterfaz.CrearBoton(col, "Reintentar", "Reiniciar partida", () => flujo.Reintentar(), PaletaVoidform.PanelClaro, 24), 62f);
            FabricaInterfaz.Medidas(FabricaInterfaz.CrearBoton(col, "Abandonar", "Abandonar y volver al menú", () => flujo.VolverAlMenuPrincipal(), new Color(0.35f, 0.12f, 0.14f, 0.95f), 24), 62f);

            pantallas[EstadoFlujoJuego.Pausa] = pantalla.gameObject;
            pantalla.gameObject.SetActive(false);
        }

        private void ConstruirResultados()
        {
            RectTransform c = CrearPantalla(EstadoFlujoJuego.Resultados, "Resultados", "", false);
            textoResultadoTitulo = FabricaInterfaz.CrearTexto(c, "Resultado", "", 60, PaletaVoidform.TextoPrincipal, TextAnchor.UpperLeft, FontStyle.Bold);
            FabricaInterfaz.Anclar((RectTransform)textoResultadoTitulo.transform, 0f, 0.86f, 1f, 1f);
            textoResultadoCuerpo = FabricaInterfaz.CrearTexto(c, "Cuerpo", "", 23, PaletaVoidform.TextoPrincipal, TextAnchor.UpperLeft);
            FabricaInterfaz.Anclar((RectTransform)textoResultadoCuerpo.transform, 0f, 0.5f, 0.55f, 0.85f);
            textoResultadoCopas = FabricaInterfaz.CrearTexto(c, "Copas", "", 24, PaletaVoidform.Disputa, TextAnchor.UpperLeft, FontStyle.Bold);
            FabricaInterfaz.Anclar((RectTransform)textoResultadoCopas.transform, 0.58f, 0.5f, 1f, 0.85f);
            textoResultadoDesenlace = FabricaInterfaz.CrearTexto(c, "Desenlace", "", 20, PaletaVoidform.TextoSecundario, TextAnchor.UpperLeft);
            FabricaInterfaz.Anclar((RectTransform)textoResultadoDesenlace.transform, 0f, 0.02f, 1f, 0.48f);

            Transform tarjeta = c.parent;
            Button menu = FabricaInterfaz.CrearBoton(tarjeta, "Menú", "Volver al menú", () => flujo.VolverAlMenuPrincipal(), PaletaVoidform.PanelClaro, 24);
            FabricaInterfaz.Colocar((RectTransform)menu.transform, new Vector2(0f, 0f), new Vector2(0f, 0f), new Vector2(40f, 30f), new Vector2(280f, 62f));
            Button reintentar = FabricaInterfaz.CrearBoton(tarjeta, "Reintentar", "Jugar de nuevo", () => flujo.Reintentar(), PaletaVoidform.PanelClaro, 24);
            FabricaInterfaz.Colocar((RectTransform)reintentar.transform, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 30f), new Vector2(280f, 62f));
            botonSiguienteNivel = FabricaInterfaz.CrearBoton(tarjeta, "Siguiente", "Siguiente nivel  ►", () => flujo.SiguienteNivel(), PaletaVoidform.AcentoVoid, 24);
            FabricaInterfaz.Colocar((RectTransform)botonSiguienteNivel.transform, new Vector2(1f, 0f), new Vector2(1f, 0f), new Vector2(-40f, 30f), new Vector2(300f, 62f));
        }

        private void MostrarResultados(ResultadoPartida r)
        {
            string titulo;
            Color color;
            if (r.FueEmpate) { titulo = "EMPATE"; color = PaletaVoidform.Disputa; }
            else if (r.GanoJugadorLocal) { titulo = "VICTORIA"; color = PaletaVoidform.Cuidador; }
            else { titulo = "DERROTA"; color = PaletaVoidform.Alerta; }

            textoResultadoTitulo.text = $"<color={PaletaVoidform.Hex(color)}>{titulo}</color>  <size=28><color=#A9B4C8>{UtilidadesTipos.NombreModo(r.modo)}</color></size>";
            textoResultadoCuerpo.text = $"{r.mensajePrincipal}\n\n{r.detalle}\n\nIntegridad final del ecosistema: <b>{Mathf.RoundToInt(r.integridadEcosistemaFinal)}%</b>";

            if (r.modo == ModoJuego.Modo4vs4 && flujo.UltimoCambioCopas.HasValue)
            {
                CambioClasificacion c = flujo.UltimoCambioCopas.Value;
                string signo = c.Diferencia > 0 ? "+" : "";
                textoResultadoCopas.text =
                    $"COPAS: {c.CopasAntes} → {c.CopasDespues}  ({signo}{c.Diferencia})\n" +
                    $"Rango: {SistemaClasificacionYCopas.CalcularNombreRango(c.CopasDespues)}\n" +
                    $"Liga: {UtilidadesTipos.NombreLiga(c.LigaDespues)}" +
                    (c.Ascenso ? "  ▲ ¡ASCENSO!" : c.Descenso ? "  ▼ Descenso" : "") +
                    $"\nRacha de victorias: {c.Racha}" +
                    (c.EscudoConsumido ? "\nEscudo de liga consumido" : flujo.Clasificacion.TieneEscudoLiga ? "\nEscudo de liga activo" : "");
            }
            else if (r.modo == ModoJuego.Historia)
            {
                textoResultadoCopas.text = $"Nivel {r.nivelHistoria} de {GuardadoProgreso.NivelesTotales}\n" +
                    (flujo.UltimoNivelPermiteAvanzar ? $"Nivel {r.nivelHistoria + 1} desbloqueado" : r.GanoJugadorLocal ? "¡Campaña completada!" : "Repite el nivel para avanzar");
            }
            else
            {
                textoResultadoCopas.text = "La Conquista no modifica las copas\n(solo el 4vs4 es clasificatorio).";
            }

            textoResultadoDesenlace.text = r.historiaCompletada ? flujo.UltimoDesenlace : "";
            botonSiguienteNivel.gameObject.SetActive(r.modo == ModoJuego.Historia && flujo.UltimoNivelPermiteAvanzar);
        }

        // =====================================================================
        // Nivel 3: historia, controles y clasificación
        // =====================================================================

        /// <summary>
        /// Archivo del mundo (apartado de Historia del lobby): lore, escenarios y fuentes, guardianes,
        /// arquetipos humanos y reglas de la campaña, organizados en pestañas.
        /// </summary>
        private void ConstruirHistoriaYProblematica()
        {
            RectTransform c = CrearPantalla(EstadoFlujoJuego.HistoriaYProblematica, "Archivo del mundo",
                "Lore, escenarios, fuentes de energía, guardianes, arquetipos humanos y reglas de la campaña.", true);

            RectTransform pestanas = FabricaInterfaz.CrearFila(c, "Pestañas", 10f, 0);
            FabricaInterfaz.Anclar(pestanas, 0f, 0.9f, 1f, 1f);
            string[] titulos = DatosArchivoMundo.TitulosSecciones;
            botonesArchivo = new Button[titulos.Length];
            for (int i = 0; i < titulos.Length; i++)
            {
                int indice = i;
                botonesArchivo[i] = FabricaInterfaz.CrearBoton(pestanas, titulos[i], titulos[i], () => MostrarSeccionArchivo(indice), PaletaVoidform.PanelProfundo, 19);
            }

            Image fondo = FabricaInterfaz.CrearPanel(c, "Fondo del archivo", new Color(0.03f, 0.04f, 0.08f, 0.7f));
            FabricaInterfaz.Anclar((RectTransform)fondo.transform, 0f, 0f, 1f, 0.88f);
            textoArchivo = FabricaInterfaz.CrearTexto(fondo.transform, "Texto del archivo", "", 18, PaletaVoidform.TextoPrincipal, TextAnchor.UpperLeft);
            FabricaInterfaz.Anclar((RectTransform)textoArchivo.transform, 0f, 0f, 1f, 1f, 22f, 16f, 22f, 16f);
            MostrarSeccionArchivo(0);
        }

        private void MostrarSeccionArchivo(int indice)
        {
            textoArchivo.text = DatosArchivoMundo.ObtenerSeccion(indice);
            for (int i = 0; i < botonesArchivo.Length; i++)
            {
                FabricaInterfaz.ResaltarBoton(botonesArchivo[i], i == indice, PaletaVoidform.PanelClaro, PaletaVoidform.PanelProfundo);
            }
        }

        private void ConstruirControles()
        {
            RectTransform c = CrearPantalla(EstadoFlujoJuego.Controles, "Controles", "Teclado y ratón (Input Manager clásico de Unity).", true);
            Text izquierda = FabricaInterfaz.CrearTexto(c, "Destructor",
                $"<size=30><b><color={PaletaVoidform.Hex(PaletaVoidform.Destructor)}>DESTRUCTOR (1.ª persona)</color></b></size>\n\n" +
                "WASD · moverse\nRatón · apuntar\nEspacio · saltar (con margen de coyote y búfer)\nClic izquierdo · disparar\nR · recargar\n" +
                "E · habilidad del traje\nQ · habilidad del arma\n1 / 2 / rueda · arma principal / pistola\nClic o Tab (muerto en 4vs4) · cambiar a quién espectear\nEsc · pausa",
                24, PaletaVoidform.TextoPrincipal, TextAnchor.UpperLeft);
            FabricaInterfaz.Anclar((RectTransform)izquierda.transform, 0f, 0f, 0.48f, 1f);
            Text derecha = FabricaInterfaz.CrearTexto(c, "Guardián",
                $"<size=30><b><color={PaletaVoidform.Hex(PaletaVoidform.Cuidador)}>GUARDIÁN (3.ª persona)</color></b></size>\n\n" +
                "WASD · moverse\nRatón · girar\nEspacio · saltar\nClic izquierdo · golpe elemental básico (sin maná)\nE · habilidad 1 (25 maná)\n" +
                "R · habilidad 2 (35 maná)\nQ · definitiva (65 maná)\nF · rotar elemento (solo Aethernum, nivel 5)\nEsc · pausa\n\n" +
                "<color=#A9B4C8>El maná solo se usa en Conquista y 4vs4. Si no tienes maná suficiente, la habilidad no se lanza y el HUD lo avisa.</color>",
                24, PaletaVoidform.TextoPrincipal, TextAnchor.UpperLeft);
            FabricaInterfaz.Anclar((RectTransform)derecha.transform, 0.52f, 0f, 1f, 1f);
        }

        private void ConstruirClasificacion()
        {
            RectTransform c = CrearPantalla(EstadoFlujoJuego.Clasificacion, "Clasificación 4vs4", "Copas, rango y liga. Solo el 4vs4 es clasificatorio.", true);
            textoClasificacionMenu = FabricaInterfaz.CrearTexto(c, "Estado", "", 26, PaletaVoidform.TextoPrincipal, TextAnchor.UpperLeft);
            FabricaInterfaz.Anclar((RectTransform)textoClasificacionMenu.transform, 0f, 0.55f, 0.5f, 1f);
            Text reglas = FabricaInterfaz.CrearTexto(c, "Reglas",
                "<b>Ligas (100 copas cada una, 4 divisiones de 25):</b>\nBronce 0 · Plata 100 · Oro 200 · Platino 300 · Diamante 400 · Núcleo Supremo 500+\n\n" +
                "<b>Cálculo de copas:</b>\n• Victoria: +30 − 2×(índice de liga), mínimo +20, +5 por cada victoria de racha previa (máx. +15).\n" +
                "• Derrota: −(15 + 2×índice de liga) y se pierde la racha. Nunca por debajo de 0.\n• Empate: 0 copas, la racha se conserva.\n" +
                "• Salas «Con bots»: 50% de la variación.\n\n<b>Escudo de liga:</b> al ascender lo obtienes; tu primera derrota que te haría descender " +
                "te deja en el mínimo de la liga y consume el escudo.",
                21, PaletaVoidform.TextoSecundario, TextAnchor.UpperLeft);
            FabricaInterfaz.Anclar((RectTransform)reglas.transform, 0.52f, 0f, 1f, 1f);

            Button reiniciar = FabricaInterfaz.CrearBoton(c, "Reiniciar progreso", "Borrar todo el progreso", () =>
            {
                GuardadoProgreso.BorrarTodo();
                flujo.Clasificacion.ReiniciarClasificacion();
            }, new Color(0.35f, 0.12f, 0.14f, 0.95f), 22);
            FabricaInterfaz.Colocar((RectTransform)reiniciar.transform, new Vector2(0f, 0f), new Vector2(0f, 0f), new Vector2(0f, 10f), new Vector2(360f, 58f));
        }

        private void ManejarClasificacion(int copas, string rango, LigaCompetitiva liga)
        {
            if (textoClasificacionMenu != null)
            {
                SistemaClasificacionYCopas c = flujo.Clasificacion;
                textoClasificacionMenu.text =
                    $"<size=44><b>{copas}</b></size> copas\n\nRango: <b>{rango}</b>\nLiga: <b>{UtilidadesTipos.NombreLiga(liga)}</b>\n" +
                    $"Racha actual: {c.Racha}\nEscudo de liga: {(c.TieneEscudoLiga ? "activo" : "no")}\n" +
                    (liga != LigaCompetitiva.NucleoSupremo ? $"Faltan {c.CopasParaSiguienteDivision()} copas para la siguiente división." : "Estás en la cima: Núcleo Supremo.");
            }
            ActualizarPanelCopasHUD(copas, rango, liga);
        }
    }
}
