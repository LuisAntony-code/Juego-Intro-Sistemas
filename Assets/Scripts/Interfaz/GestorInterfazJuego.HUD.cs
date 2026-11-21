using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace VoidformBioForge
{
    /// <summary>
    /// HUD de combate de GestorInterfazJuego (clase parcial).
    /// -----------------------------------------------------------------
    /// Nivel 1: vida, blindaje/integridad del traje, munición, mira y reloj.
    /// Nivel 2: maná del guardián y enfriamientos (4 ranuras), alerta de explosión del traje al 25%,
    ///          aviso de explosión, estado de las 5 bases, reloj de 5:00, marcador de 3 rondas,
    ///          supervivientes, modo espectador, cuenta atrás de reaparición y panel de copas/rango/liga.
    /// Nivel 3: panel de Historia (nivel, perspectiva, objetivo y extracción), barra de integridad del
    ///          ecosistema, registro de bajas y mensajes, marcador de impacto, viñeta de daño y avisos de
    ///          "maná insuficiente". Todo se vincula y desvincula por eventos al crear/destruir la partida.
    /// </summary>
    public partial class GestorInterfazJuego
    {
        private class RanuraHabilidad
        {
            public Image Fondo;
            public Text Tecla;
            public Text Nombre;
            public Text Detalle;
            public BarraInterfaz Enfriamiento;
            public float CostoMana;
        }

        private struct EntradaRegistro
        {
            public string Texto;
            public float Momento;
        }

        private GameObject hud;
        private readonly List<Action> desuscripciones = new List<Action>();
        private ContextoPartida contexto;

        // Superior
        private Text textoReloj;
        private Text textoSubtituloModo;
        private RectTransform grupoBases;
        private Image[] fondosBases;
        private Text[] letrasBases;
        private BarraInterfaz[] barrasBases;
        private Text textoEspectador;
        private GameObject panelEspectador;

        // Superior derecha / izquierda
        private BarraInterfaz barraEcosistema;
        private BarraInterfaz barraEnergia;
        private Text textoCopasHUD;
        private GameObject panelHistoria;
        private Text textoHistoria;
        private string objetivoHistoria = "";
        private string estadoExtraccion = "";

        // Inferior izquierda
        private Text textoIdentidad;
        private BarraInterfaz barraVida;
        private BarraInterfaz barraBlindaje;
        private BarraInterfaz barraMana;
        private Text textoEstadosAlterados;

        // Inferior derecha
        private GameObject panelArmas;
        private Text textoArma;
        private Text textoMunicion;
        private GameObject panelGuardian;
        private Text textoElemento;
        private RanuraHabilidad[] ranurasDestructor;
        private RanuraHabilidad[] ranurasGuardian;

        // Centro
        private Image[] mira;
        private Text textoMarcadorImpacto;
        private Text textoAlertaTraje;
        private Text textoReaparicion;
        private Text textoBanner;
        private Text textoAvisoMana;
        private Image vinetaDano;
        private Text textoRegistro;

        // Temporizadores visuales
        private readonly List<EntradaRegistro> registro = new List<EntradaRegistro>();
        private float finBanner;
        private float finMarcadorImpacto;
        private float finAvisoMana;
        private float finReaparicion;
        private float intensidadVineta;
        private bool alertaTrajeActiva;

        // =====================================================================
        // Construcción del HUD
        // =====================================================================

        private void ConstruirHUD()
        {
            RectTransform raizHud = FabricaInterfaz.CrearRect(raiz, "HUD de Combate");
            FabricaInterfaz.Anclar(raizHud, 0f, 0f, 1f, 1f);
            hud = raizHud.gameObject;

            vinetaDano = FabricaInterfaz.CrearPanel(raizHud, "Viñeta de daño", new Color(1f, 0f, 0f, 0f), false);
            FabricaInterfaz.Anclar((RectTransform)vinetaDano.transform, 0f, 0f, 1f, 1f);
            vinetaDano.raycastTarget = false;

            ConstruirPanelSuperior(raizHud);
            ConstruirPanelEcosistema(raizHud);
            ConstruirPanelHistoria(raizHud);
            ConstruirPanelVida(raizHud);
            ConstruirPanelArmas(raizHud);
            ConstruirPanelGuardian(raizHud);
            ConstruirCentro(raizHud);

            textoRegistro = FabricaInterfaz.CrearTexto(raizHud, "Registro de combate", "", 19, PaletaVoidform.TextoPrincipal, TextAnchor.LowerLeft);
            FabricaInterfaz.Colocar((RectTransform)textoRegistro.transform, new Vector2(0f, 0.5f), new Vector2(0f, 0f), new Vector2(30f, -40f), new Vector2(640f, 260f));

            hud.SetActive(false);
        }

        private void ConstruirPanelSuperior(RectTransform padre)
        {
            Image panel = FabricaInterfaz.CrearPanel(padre, "Panel superior", PaletaVoidform.PanelProfundo);
            FabricaInterfaz.Colocar((RectTransform)panel.transform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -16f), new Vector2(760f, 112f));
            textoReloj = FabricaInterfaz.CrearTexto(panel.transform, "Reloj", "5:00", 52, PaletaVoidform.TextoPrincipal, TextAnchor.UpperCenter, FontStyle.Bold);
            FabricaInterfaz.Anclar((RectTransform)textoReloj.transform, 0f, 0.4f, 1f, 1f, 0f, 0f, 0f, 4f);
            textoSubtituloModo = FabricaInterfaz.CrearTexto(panel.transform, "Subtítulo", "", 20, PaletaVoidform.TextoSecundario, TextAnchor.MiddleCenter);
            FabricaInterfaz.Anclar((RectTransform)textoSubtituloModo.transform, 0f, 0f, 1f, 0.42f, 10f, 6f, 10f, 0f);

            grupoBases = FabricaInterfaz.CrearRect(padre, "Cinco bases");
            FabricaInterfaz.Colocar(grupoBases, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -138f), new Vector2(560f, 78f));
            fondosBases = new Image[5];
            letrasBases = new Text[5];
            barrasBases = new BarraInterfaz[5];
            for (int i = 0; i < 5; i++)
            {
                Image fondo = FabricaInterfaz.CrearPanel(grupoBases, $"Base {UtilidadesTipos.LetraBase(i)}", PaletaVoidform.PanelProfundo);
                FabricaInterfaz.Colocar((RectTransform)fondo.transform, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(i * 114f, 0f), new Vector2(100f, 76f));
                fondosBases[i] = fondo;
                letrasBases[i] = FabricaInterfaz.CrearTexto(fondo.transform, "Letra", UtilidadesTipos.LetraBase(i), 32, PaletaVoidform.TextoPrincipal, TextAnchor.MiddleCenter, FontStyle.Bold);
                FabricaInterfaz.Anclar((RectTransform)letrasBases[i].transform, 0f, 0.3f, 1f, 1f);
                barrasBases[i] = FabricaInterfaz.CrearBarra(fondo.transform, "Progreso", new Color(0f, 0f, 0f, 0.5f), PaletaVoidform.Neutral, 12);
                FabricaInterfaz.Anclar(barrasBases[i].Raiz, 0f, 0f, 1f, 0f, 8f, 8f, 8f, -20f);
            }

            panelEspectador = FabricaInterfaz.CrearPanel(padre, "Panel espectador", new Color(0.1f, 0.05f, 0.2f, 0.9f)).gameObject;
            FabricaInterfaz.Colocar((RectTransform)panelEspectador.transform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -230f), new Vector2(900f, 70f));
            textoEspectador = FabricaInterfaz.CrearTexto(panelEspectador.transform, "Texto", "", 22, PaletaVoidform.TextoPrincipal, TextAnchor.MiddleCenter, FontStyle.Bold);
            FabricaInterfaz.Anclar((RectTransform)textoEspectador.transform, 0f, 0f, 1f, 1f, 12f, 4f, 12f, 4f);
            panelEspectador.SetActive(false);
        }

        private void ConstruirPanelEcosistema(RectTransform padre)
        {
            Image panel = FabricaInterfaz.CrearPanel(padre, "Panel ecosistema", PaletaVoidform.PanelProfundo);
            FabricaInterfaz.Colocar((RectTransform)panel.transform, new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-20f, -16f), new Vector2(430f, 168f));
            Text titulo = FabricaInterfaz.CrearTexto(panel.transform, "Título", "INTEGRIDAD DEL ECOSISTEMA", 17, PaletaVoidform.Ecosistema, TextAnchor.UpperLeft, FontStyle.Bold);
            FabricaInterfaz.Anclar((RectTransform)titulo.transform, 0f, 1f, 1f, 1f, 16f, -34f, 16f, 10f);
            barraEcosistema = FabricaInterfaz.CrearBarra(panel.transform, "Barra ecosistema", new Color(0f, 0f, 0f, 0.55f), PaletaVoidform.Ecosistema, 16);
            FabricaInterfaz.Anclar(barraEcosistema.Raiz, 0f, 1f, 1f, 1f, 16f, -66f, 16f, 38f);
            barraEnergia = FabricaInterfaz.CrearBarra(panel.transform, "Barra energía", new Color(0f, 0f, 0f, 0.55f), PaletaVoidform.Destructor, 16);
            FabricaInterfaz.Anclar(barraEnergia.Raiz, 0f, 1f, 1f, 1f, 16f, -102f, 16f, 74f);
            textoCopasHUD = FabricaInterfaz.CrearTexto(panel.transform, "Copas", "", 18, PaletaVoidform.Disputa, TextAnchor.UpperLeft, FontStyle.Bold);
            FabricaInterfaz.Anclar((RectTransform)textoCopasHUD.transform, 0f, 0f, 1f, 0f, 16f, 8f, 16f, -56f);
        }

        private void ConstruirPanelHistoria(RectTransform padre)
        {
            Image panel = FabricaInterfaz.CrearPanel(padre, "Panel historia", PaletaVoidform.PanelProfundo);
            FabricaInterfaz.Colocar((RectTransform)panel.transform, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(20f, -16f), new Vector2(560f, 200f));
            textoHistoria = FabricaInterfaz.CrearTexto(panel.transform, "Texto", "", 18, PaletaVoidform.TextoPrincipal, TextAnchor.UpperLeft);
            FabricaInterfaz.Anclar((RectTransform)textoHistoria.transform, 0f, 0f, 1f, 1f, 16f, 10f, 16f, 10f);
            panelHistoria = panel.gameObject;
        }

        private void ConstruirPanelVida(RectTransform padre)
        {
            Image panel = FabricaInterfaz.CrearPanel(padre, "Panel vida", PaletaVoidform.PanelProfundo);
            FabricaInterfaz.Colocar((RectTransform)panel.transform, new Vector2(0f, 0f), new Vector2(0f, 0f), new Vector2(20f, 20f), new Vector2(540f, 190f));
            textoIdentidad = FabricaInterfaz.CrearTexto(panel.transform, "Identidad", "", 20, PaletaVoidform.TextoPrincipal, TextAnchor.UpperLeft, FontStyle.Bold);
            FabricaInterfaz.Anclar((RectTransform)textoIdentidad.transform, 0f, 1f, 1f, 1f, 18f, -40f, 18f, 12f);
            barraVida = FabricaInterfaz.CrearBarra(panel.transform, "Vida", new Color(0f, 0f, 0f, 0.55f), PaletaVoidform.Vida, 18);
            FabricaInterfaz.Anclar(barraVida.Raiz, 0f, 1f, 1f, 1f, 18f, -84f, 18f, 48f);
            barraBlindaje = FabricaInterfaz.CrearBarra(panel.transform, "Blindaje", new Color(0f, 0f, 0f, 0.55f), PaletaVoidform.Blindaje, 16);
            FabricaInterfaz.Anclar(barraBlindaje.Raiz, 0f, 1f, 1f, 1f, 18f, -116f, 18f, 90f);
            barraMana = FabricaInterfaz.CrearBarra(panel.transform, "Maná", new Color(0f, 0f, 0f, 0.55f), PaletaVoidform.Mana, 16);
            FabricaInterfaz.Anclar(barraMana.Raiz, 0f, 1f, 1f, 1f, 18f, -148f, 18f, 122f);
            textoEstadosAlterados = FabricaInterfaz.CrearTexto(panel.transform, "Estados", "", 17, PaletaVoidform.Disputa, TextAnchor.LowerLeft, FontStyle.Bold);
            FabricaInterfaz.Anclar((RectTransform)textoEstadosAlterados.transform, 0f, 0f, 1f, 0f, 18f, 8f, 18f, -30f);
        }

        private RanuraHabilidad CrearRanura(Transform padre, string tecla, Color color, float x)
        {
            RanuraHabilidad r = new RanuraHabilidad();
            r.Fondo = FabricaInterfaz.CrearPanel(padre, $"Ranura {tecla}", new Color(color.r * 0.3f, color.g * 0.3f, color.b * 0.3f, 0.95f));
            FabricaInterfaz.Colocar((RectTransform)r.Fondo.transform, new Vector2(0f, 0f), new Vector2(0f, 0f), new Vector2(x, 14f), new Vector2(128f, 92f));
            r.Enfriamiento = FabricaInterfaz.CrearBarra(r.Fondo.transform, "Enfriamiento", new Color(0f, 0f, 0f, 0f), new Color(0f, 0f, 0f, 0.65f), 20, true);
            FabricaInterfaz.Anclar(r.Enfriamiento.Raiz, 0f, 0f, 1f, 1f);
            r.Enfriamiento.Establecer(0f, "");
            r.Tecla = FabricaInterfaz.CrearTexto(r.Fondo.transform, "Tecla", tecla, 20, color, TextAnchor.UpperLeft, FontStyle.Bold);
            FabricaInterfaz.Anclar((RectTransform)r.Tecla.transform, 0f, 0f, 1f, 1f, 8f, 4f, 8f, 4f);
            r.Nombre = FabricaInterfaz.CrearTexto(r.Fondo.transform, "Nombre", "", 14, PaletaVoidform.TextoPrincipal, TextAnchor.MiddleCenter);
            FabricaInterfaz.Anclar((RectTransform)r.Nombre.transform, 0f, 0.2f, 1f, 0.8f, 6f, 0f, 6f, 0f);
            r.Detalle = FabricaInterfaz.CrearTexto(r.Fondo.transform, "Detalle", "", 13, PaletaVoidform.TextoSecundario, TextAnchor.LowerCenter);
            FabricaInterfaz.Anclar((RectTransform)r.Detalle.transform, 0f, 0f, 1f, 0.3f, 4f, 4f, 4f, 0f);
            return r;
        }

        private void ConstruirPanelArmas(RectTransform padre)
        {
            Image panel = FabricaInterfaz.CrearPanel(padre, "Panel armas", PaletaVoidform.PanelProfundo);
            FabricaInterfaz.Colocar((RectTransform)panel.transform, new Vector2(1f, 0f), new Vector2(1f, 0f), new Vector2(-20f, 20f), new Vector2(560f, 200f));
            panelArmas = panel.gameObject;
            textoArma = FabricaInterfaz.CrearTexto(panel.transform, "Arma", "", 20, PaletaVoidform.TextoSecundario, TextAnchor.UpperRight, FontStyle.Bold);
            FabricaInterfaz.Anclar((RectTransform)textoArma.transform, 0f, 1f, 1f, 1f, 18f, -40f, 18f, 12f);
            textoMunicion = FabricaInterfaz.CrearTexto(panel.transform, "Munición", "", 46, PaletaVoidform.TextoPrincipal, TextAnchor.UpperRight, FontStyle.Bold);
            FabricaInterfaz.Anclar((RectTransform)textoMunicion.transform, 0.45f, 1f, 1f, 1f, 0f, -100f, 18f, 38f);
            ranurasDestructor = new[]
            {
                CrearRanura(panel.transform, "E", PaletaVoidform.Destructor, 18f),
                CrearRanura(panel.transform, "Q", PaletaVoidform.Disputa, 158f)
            };
        }

        private void ConstruirPanelGuardian(RectTransform padre)
        {
            Image panel = FabricaInterfaz.CrearPanel(padre, "Panel guardián", PaletaVoidform.PanelProfundo);
            FabricaInterfaz.Colocar((RectTransform)panel.transform, new Vector2(1f, 0f), new Vector2(1f, 0f), new Vector2(-20f, 20f), new Vector2(580f, 170f));
            panelGuardian = panel.gameObject;
            textoElemento = FabricaInterfaz.CrearTexto(panel.transform, "Elemento", "", 20, PaletaVoidform.TextoPrincipal, TextAnchor.UpperLeft, FontStyle.Bold);
            FabricaInterfaz.Anclar((RectTransform)textoElemento.transform, 0f, 1f, 1f, 1f, 18f, -44f, 18f, 12f);
            ranurasGuardian = new[]
            {
                CrearRanura(panel.transform, "Clic", PaletaVoidform.TextoPrincipal, 14f),
                CrearRanura(panel.transform, "E", PaletaVoidform.Cuidador, 155f),
                CrearRanura(panel.transform, "R", PaletaVoidform.Cuidador, 296f),
                CrearRanura(panel.transform, "Q", PaletaVoidform.Disputa, 437f)
            };
        }

        private void ConstruirCentro(RectTransform padre)
        {
            mira = new Image[5];
            Vector2[] posiciones = { new Vector2(0f, 14f), new Vector2(0f, -14f), new Vector2(14f, 0f), new Vector2(-14f, 0f), Vector2.zero };
            Vector2[] tamanos = { new Vector2(3f, 12f), new Vector2(3f, 12f), new Vector2(12f, 3f), new Vector2(12f, 3f), new Vector2(4f, 4f) };
            for (int i = 0; i < 5; i++)
            {
                mira[i] = FabricaInterfaz.CrearPanel(padre, $"Mira {i}", new Color(1f, 1f, 1f, 0.9f), false);
                FabricaInterfaz.Colocar((RectTransform)mira[i].transform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), posiciones[i], tamanos[i]);
                mira[i].raycastTarget = false;
            }

            textoMarcadorImpacto = FabricaInterfaz.CrearTexto(padre, "Marcador de impacto", "×", 40, new Color(1f, 1f, 1f, 0f), TextAnchor.MiddleCenter, FontStyle.Bold, false);
            FabricaInterfaz.Colocar((RectTransform)textoMarcadorImpacto.transform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(80f, 80f));

            textoAlertaTraje = FabricaInterfaz.CrearTexto(padre, "Alerta de traje", "¡TRAJE CRÍTICO · RIESGO DE EXPLOSIÓN!", 36, PaletaVoidform.Alerta, TextAnchor.MiddleCenter, FontStyle.Bold);
            FabricaInterfaz.Colocar((RectTransform)textoAlertaTraje.transform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, -170f), new Vector2(1000f, 60f));
            textoAlertaTraje.gameObject.SetActive(false);

            textoReaparicion = FabricaInterfaz.CrearTexto(padre, "Reaparición", "", 38, PaletaVoidform.TextoPrincipal, TextAnchor.MiddleCenter, FontStyle.Bold);
            FabricaInterfaz.Colocar((RectTransform)textoReaparicion.transform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, 60f), new Vector2(1000f, 120f));

            textoBanner = FabricaInterfaz.CrearTexto(padre, "Banner", "", 54, PaletaVoidform.TextoPrincipal, TextAnchor.MiddleCenter, FontStyle.Bold);
            FabricaInterfaz.Colocar((RectTransform)textoBanner.transform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, 200f), new Vector2(1500f, 140f));

            textoAvisoMana = FabricaInterfaz.CrearTexto(padre, "Aviso de maná", "", 26, PaletaVoidform.Mana, TextAnchor.MiddleCenter, FontStyle.Bold);
            FabricaInterfaz.Colocar((RectTransform)textoAvisoMana.transform, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 240f), new Vector2(900f, 50f));
        }

        // =====================================================================
        // Vinculación con la partida
        // =====================================================================

        private void VincularPartida(ContextoPartida ctx)
        {
            DesvincularPartida();
            contexto = ctx;
            registro.Clear();
            alertaTrajeActiva = false;
            finReaparicion = 0f;
            intensidadVineta = 0f;

            bool esDestructor = ctx.JugadorEsDestructor;
            ModoJuego modo = ctx.Configuracion.modo;

            panelArmas.SetActive(esDestructor);
            panelGuardian.SetActive(!esDestructor);
            barraMana.EstablecerVisible(!esDestructor && ctx.GuardianLocal != null && ctx.GuardianLocal.UsaMana);
            grupoBases.gameObject.SetActive(modo == ModoJuego.Conquista5Bases);
            panelHistoria.SetActive(modo == ModoJuego.Historia);
            barraEnergia.EstablecerVisible(modo == ModoJuego.Historia);
            panelEspectador.SetActive(false);
            textoAlertaTraje.gameObject.SetActive(false);
            textoReaparicion.text = "";
            textoEstadosAlterados.text = "";
            textoReloj.color = PaletaVoidform.TextoPrincipal;
            foreach (Image m in mira)
            {
                // Se reactivan ambas cosas: una partida anterior pudo terminar en modo espectador (enabled = false).
                m.gameObject.SetActive(true);
                m.enabled = true;
            }
            mira[0].gameObject.SetActive(esDestructor);
            mira[1].gameObject.SetActive(esDestructor);
            mira[2].gameObject.SetActive(esDestructor);
            mira[3].gameObject.SetActive(esDestructor);

            textoIdentidad.text = $"<color={PaletaVoidform.Hex(PaletaVoidform.ColorBando(ctx.Configuracion.bandoJugador))}>{UtilidadesTipos.NombreBandoSingular(ctx.Configuracion.bandoJugador)}</color>" +
                                  $"  ·  {ctx.Arena.Datos.Nombre}";
            textoSubtituloModo.text = UtilidadesTipos.NombreModo(modo);
            textoReloj.text = modo == ModoJuego.Historia ? $"Nivel {ctx.Configuracion.nivelHistoria}/{GuardadoProgreso.NivelesTotales}" : modo == ModoJuego.Conquista5Bases ? "5:00" : "Ronda 1/3";
            ActualizarPanelCopasHUD(flujo.Clasificacion.CopasActuales, flujo.Clasificacion.RangoActual, flujo.Clasificacion.LigaActual);

            VincularJugadorLocal(ctx);
            VincularEcosistema(ctx);
            if (ctx.Conquista != null) VincularConquista(ctx);
            if (ctx.Modo4vs4 != null) Vincular4vs4(ctx);
            if (ctx.Historia != null) VincularHistoria(ctx);
            if (ctx.Espectador != null)
            {
                ctx.Espectador.AlCambiarObservado += ManejarObservado;
                desuscripciones.Add(() => ctx.Espectador.AlCambiarObservado -= ManejarObservado);
            }

            MostrarBanner(modo == ModoJuego.Historia ? DatosNarrativaHistoria.ObtenerNivel(ctx.Configuracion.nivelHistoria).Titulo : UtilidadesTipos.NombreModo(modo), 3f);
        }

        private void DesvincularPartida()
        {
            foreach (Action d in desuscripciones)
            {
                try { d(); }
                catch (Exception) { /* El objeto ya pudo destruirse; no hay nada que desuscribir. */ }
            }
            desuscripciones.Clear();
            contexto = null;
        }

        private void VincularJugadorLocal(ContextoPartida ctx)
        {
            SistemaSaludYTraje s = ctx.JugadorLocal;
            if (s == null) return;

            s.AlCambiarSaludYBlindaje += ManejarSalud;
            s.AlCambiarAlertaTrajeCritico += ManejarAlertaTraje;
            s.AlExplotarTrajeHumano += ManejarExplosionPropia;
            s.AlRecibirDano += ManejarDanoRecibido;
            s.AlMorir += ManejarMuertePropia;
            s.AlRevivir += ManejarRevivirPropio;
            desuscripciones.Add(() =>
            {
                s.AlCambiarSaludYBlindaje -= ManejarSalud;
                s.AlCambiarAlertaTrajeCritico -= ManejarAlertaTraje;
                s.AlExplotarTrajeHumano -= ManejarExplosionPropia;
                s.AlRecibirDano -= ManejarDanoRecibido;
                s.AlMorir -= ManejarMuertePropia;
                s.AlRevivir -= ManejarRevivirPropio;
            });
            ManejarSalud(s.VidaActual, s.VidaMaxima, s.BlindajeActual, s.BlindajeMaximo);

            if (ctx.EstadosLocal != null)
            {
                SistemaEstadosAlterados e = ctx.EstadosLocal;
                e.AlCambiarEstados += ManejarEstadosAlterados;
                desuscripciones.Add(() => e.AlCambiarEstados -= ManejarEstadosAlterados);
            }

            if (ctx.ArmasLocal != null)
            {
                SistemaArmasYTrajeDestructor a = ctx.ArmasLocal;
                a.AlCambiarMunicion += ManejarMunicion;
                a.AlCambiarArma += ManejarCambioArma;
                a.AlCambiarEnfriamientoTraje += ManejarEnfriamientoTraje;
                a.AlCambiarEnfriamientoArma += ManejarEnfriamientoArma;
                a.AlActivarHabilidad += ManejarHabilidadActivada;
                a.AlConfirmarImpacto += ManejarImpactoConfirmado;
                desuscripciones.Add(() =>
                {
                    a.AlCambiarMunicion -= ManejarMunicion;
                    a.AlCambiarArma -= ManejarCambioArma;
                    a.AlCambiarEnfriamientoTraje -= ManejarEnfriamientoTraje;
                    a.AlCambiarEnfriamientoArma -= ManejarEnfriamientoArma;
                    a.AlActivarHabilidad -= ManejarHabilidadActivada;
                    a.AlConfirmarImpacto -= ManejarImpactoConfirmado;
                });
                DatosTraje traje = s.Traje;
                ranurasDestructor[0].Nombre.text = traje != null ? traje.nombreHabilidad : "Traje";
                a.ForzarNotificacionCompleta();
            }

            if (ctx.GuardianLocal != null)
            {
                ControladorGuardianElemental g = ctx.GuardianLocal;
                g.AlCambiarMana += ManejarMana;
                g.AlCambiarEnfriamiento += ManejarEnfriamientoGuardian;
                g.AlCambiarElemento += ManejarCambioElemento;
                g.AlFaltarMana += ManejarFaltaMana;
                g.AlLanzarHabilidadElemental += ManejarHabilidadActivada;
                desuscripciones.Add(() =>
                {
                    g.AlCambiarMana -= ManejarMana;
                    g.AlCambiarEnfriamiento -= ManejarEnfriamientoGuardian;
                    g.AlCambiarElemento -= ManejarCambioElemento;
                    g.AlFaltarMana -= ManejarFaltaMana;
                    g.AlLanzarHabilidadElemental -= ManejarHabilidadActivada;
                });
                g.ForzarNotificacionCompleta();
            }
        }

        private void VincularEcosistema(ContextoPartida ctx)
        {
            GestorEquilibrioEcosistema eco = ctx.Ecosistema;
            if (eco == null) return;
            eco.AlCambiarIntegridad += ManejarIntegridad;
            eco.AlCambiarEnergiaExtraida += ManejarEnergia;
            desuscripciones.Add(() =>
            {
                eco.AlCambiarIntegridad -= ManejarIntegridad;
                eco.AlCambiarEnergiaExtraida -= ManejarEnergia;
            });
            eco.ForzarNotificacionCompleta();
        }

        private void VincularConquista(ContextoPartida ctx)
        {
            GestorConquista5Bases c = ctx.Conquista;
            c.AlActualizarReloj5Minutos += ManejarRelojConquista;
            c.AlActualizarConteoBases += ManejarConteoBases;
            c.AlProgramarReaparicion += ManejarReaparicionProgramada;
            desuscripciones.Add(() =>
            {
                c.AlActualizarReloj5Minutos -= ManejarRelojConquista;
                c.AlActualizarConteoBases -= ManejarConteoBases;
                c.AlProgramarReaparicion -= ManejarReaparicionProgramada;
            });

            foreach (ZonaCapturaBase b in ctx.Bases)
            {
                ZonaCapturaBase baseLocal = b;
                baseLocal.AlCambiarProgreso += ManejarBase;
                baseLocal.AlCambiarEstadoBase += ManejarBase;
                desuscripciones.Add(() =>
                {
                    baseLocal.AlCambiarProgreso -= ManejarBase;
                    baseLocal.AlCambiarEstadoBase -= ManejarBase;
                });
                ManejarBase(baseLocal);
            }
            ManejarConteoBases(0, 0);
        }

        private void Vincular4vs4(ContextoPartida ctx)
        {
            GestorModo4vs4 m = ctx.Modo4vs4;
            m.AlActualizarMarcadorRondas += ManejarMarcador;
            m.AlActualizarRelojRonda += ManejarRelojRonda;
            m.AlActualizarVivos += ManejarVivos;
            m.AlCambiarModoEspectador += ManejarModoEspectador;
            m.AlIniciarRonda += ManejarInicioRonda;
            m.AlFinalizarRonda += ManejarFinRonda;
            desuscripciones.Add(() =>
            {
                m.AlActualizarMarcadorRondas -= ManejarMarcador;
                m.AlActualizarRelojRonda -= ManejarRelojRonda;
                m.AlActualizarVivos -= ManejarVivos;
                m.AlCambiarModoEspectador -= ManejarModoEspectador;
                m.AlIniciarRonda -= ManejarInicioRonda;
                m.AlFinalizarRonda -= ManejarFinRonda;
            });
            ManejarMarcador(m.RondaActual, m.RondasGanadasDestructores, m.RondasGanadasCuidadores, m.RondasEmpatadas);
        }

        private void VincularHistoria(ContextoPartida ctx)
        {
            GestorModoHistoria h = ctx.Historia;
            objetivoHistoria = ctx.JugadorEsDestructor ? h.Narrativa.ObjetivoDestructor : h.Narrativa.ObjetivoCuidador;
            estadoExtraccion = ctx.Arena.Fuente != null ? ctx.Arena.Fuente.EstadoExtraccion : "";
            h.AlCambiarObjetivo += ManejarObjetivo;
            h.AlCambiarModoEspectador += ManejarModoEspectador;
            desuscripciones.Add(() =>
            {
                h.AlCambiarObjetivo -= ManejarObjetivo;
                h.AlCambiarModoEspectador -= ManejarModoEspectador;
            });

            if (ctx.Arena.Fuente != null)
            {
                FuenteEnergiaViva f = ctx.Arena.Fuente;
                f.AlCambiarEstadoExtraccion += ManejarExtraccion;
                desuscripciones.Add(() => f.AlCambiarEstadoExtraccion -= ManejarExtraccion);
            }
            ActualizarTextoHistoria();
        }

        // =====================================================================
        // Manejadores: jugador local
        // =====================================================================

        private void ManejarSalud(float vida, float vidaMax, float blindaje, float blindajeMax)
        {
            bool esDestructor = contexto != null && contexto.JugadorEsDestructor;
            barraVida.Establecer(vidaMax > 0f ? vida / vidaMax : 0f, $"{(esDestructor ? "Vida" : "Vida del guardián")}  {Mathf.CeilToInt(vida)} / {Mathf.CeilToInt(vidaMax)}");
            float maxVisible = Mathf.Max(blindajeMax, blindaje);
            barraBlindaje.Establecer(maxVisible > 0f ? blindaje / maxVisible : 0f,
                $"{(esDestructor ? "Integridad del traje" : "Corteza / blindaje")}  {Mathf.CeilToInt(blindaje)} / {Mathf.CeilToInt(blindajeMax)}");
        }

        private void ManejarAlertaTraje(bool activa)
        {
            alertaTrajeActiva = activa;
            textoAlertaTraje.gameObject.SetActive(activa);
        }

        private void ManejarExplosionPropia(SistemaSaludYTraje _)
        {
            MostrarBanner($"<color={PaletaVoidform.Hex(PaletaVoidform.Alerta)}>¡TU TRAJE HA EXPLOTADO!</color>", 2.5f);
        }

        private void ManejarDanoRecibido(InformacionDano info)
        {
            intensidadVineta = Mathf.Min(0.45f, intensidadVineta + Mathf.Clamp(info.Cantidad / 60f, 0.05f, 0.3f));
        }

        private void ManejarMuertePropia(SistemaSaludYTraje _)
        {
            textoAlertaTraje.gameObject.SetActive(false);
            if (contexto != null && contexto.Configuracion.modo == ModoJuego.Historia && !contexto.JugadorEsDestructor)
            {
                MostrarBanner("El guardián ha caído...", 2.5f);
            }
        }

        private void ManejarRevivirPropio(SistemaSaludYTraje _)
        {
            textoReaparicion.text = "";
            finReaparicion = 0f;
        }

        private void ManejarEstadosAlterados(string descripcion)
        {
            textoEstadosAlterados.text = descripcion;
        }

        private void ManejarMunicion(int cargador, int tamano, int reserva, bool recargando)
        {
            textoMunicion.text = recargando
                ? $"<color={PaletaVoidform.Hex(PaletaVoidform.Disputa)}>RECARGANDO</color>"
                : $"{(cargador <= Mathf.Max(1, tamano / 5) ? $"<color={PaletaVoidform.Hex(PaletaVoidform.Alerta)}>{cargador}</color>" : cargador.ToString())}<size=26> / {tamano}  ·  {reserva}</size>";
        }

        private void ManejarCambioArma(DatosArma arma)
        {
            if (arma == null) return;
            textoArma.text = arma.nombreArma.ToUpperInvariant();
            ranurasDestructor[1].Nombre.text = arma.nombreHabilidad;
        }

        private void ManejarEnfriamientoTraje(float restante, float total)
        {
            ActualizarRanura(ranurasDestructor[0], restante, total, true);
        }

        private void ManejarEnfriamientoArma(float restante, float total)
        {
            ActualizarRanura(ranurasDestructor[1], restante, total, true);
        }

        private void ManejarHabilidadActivada(string nombre)
        {
            AgregarRegistro($"<color={PaletaVoidform.Hex(PaletaVoidform.Disputa)}>» {nombre}</color>");
        }

        private void ManejarImpactoConfirmado(bool fueBaja)
        {
            finMarcadorImpacto = Time.unscaledTime + (fueBaja ? 0.45f : 0.15f);
            textoMarcadorImpacto.color = fueBaja ? PaletaVoidform.Alerta : Color.white;
        }

        private void ManejarMana(float mana, float maximo)
        {
            barraMana.Establecer(maximo > 0f ? mana / maximo : 0f, $"Maná  {Mathf.FloorToInt(mana)} / {Mathf.FloorToInt(maximo)}");
            if (contexto == null || contexto.GuardianLocal == null || ranurasGuardian == null) return;
            ControladorGuardianElemental g = contexto.GuardianLocal;
            for (int i = 1; i < 4; i++)
            {
                bool sinMana = g.UsaMana && mana < ranurasGuardian[i].CostoMana;
                ranurasGuardian[i].Detalle.color = sinMana ? PaletaVoidform.Alerta : PaletaVoidform.TextoSecundario;
            }
        }

        private void ManejarEnfriamientoGuardian(int slot, float restante, float total)
        {
            if (slot < 0 || slot >= ranurasGuardian.Length) return;
            ActualizarRanura(ranurasGuardian[slot], restante, total, slot != ControladorGuardianElemental.SlotAtaqueBasico);
        }

        private void ManejarCambioElemento(TipoElemento elemento)
        {
            if (contexto == null || contexto.GuardianLocal == null) return;
            ControladorGuardianElemental g = contexto.GuardianLocal;
            DatosGuardianElemental d = g.Datos;
            Color c = PaletaVoidform.ColorElemento(elemento);
            textoElemento.text = $"<color={PaletaVoidform.Hex(c)}>{d.nombreGuardian}</color> · {UtilidadesTipos.NombreElemento(elemento)}" +
                                 (g.PuedeRotarElementos ? "  <color=#A9B4C8>(F: rotar elemento)</color>" : "") +
                                 (g.UsaMana ? "" : "  <color=#A9B4C8>(Historia: sin maná)</color>");

            string[] nombres = { d.nombreAtaqueBasico, d.nombreHabilidad1, d.nombreHabilidad2, d.nombreDefinitiva };
            for (int i = 0; i < 4; i++)
            {
                ranurasGuardian[i].Nombre.text = nombres[i];
                ranurasGuardian[i].CostoMana = g.ObtenerCostoMana(i);
                ranurasGuardian[i].Detalle.text = i == 0 ? "sin maná" : g.UsaMana ? $"{g.ObtenerCostoMana(i)} maná" : $"{g.ObtenerEnfriamientoTotal(i)} s";
                ranurasGuardian[i].Fondo.color = i == 0 ? new Color(0.2f, 0.22f, 0.26f, 0.95f) : new Color(c.r * 0.3f, c.g * 0.3f, c.b * 0.3f, 0.95f);
            }
            barraMana.EstablecerVisible(g.UsaMana);
        }

        private void ManejarFaltaMana(string habilidad, float costo, float actual)
        {
            textoAvisoMana.text = $"Maná insuficiente para {habilidad}: necesita {Mathf.RoundToInt(costo)} (tienes {Mathf.FloorToInt(actual)})";
            finAvisoMana = Time.unscaledTime + 1.6f;
        }

        private static void ActualizarRanura(RanuraHabilidad r, float restante, float total, bool mostrarNumero)
        {
            float t = total > 0f ? restante / total : 0f;
            r.Enfriamiento.Establecer(t, restante > 0.05f && mostrarNumero ? $"{restante:0.0}" : "");
        }

        // =====================================================================
        // Manejadores: ecosistema, conquista, 4vs4 e historia
        // =====================================================================

        private void ManejarIntegridad(float integridad)
        {
            string estado = contexto != null && contexto.Ecosistema != null ? contexto.Ecosistema.ObtenerEstadoTexto() : "";
            barraEcosistema.Establecer(integridad / 100f, $"{estado}  {Mathf.RoundToInt(integridad)}%");
            barraEcosistema.EstablecerColor(Color.Lerp(PaletaVoidform.Alerta, PaletaVoidform.Ecosistema, integridad / 100f));
        }

        private void ManejarEnergia(float energia)
        {
            barraEnergia.Establecer(energia / 100f, $"Energía extraída  {Mathf.RoundToInt(energia)}%");
        }

        private void ManejarRelojConquista(float segundos)
        {
            textoReloj.text = UtilidadesTipos.FormatearReloj(segundos);
            textoReloj.color = segundos <= 30f ? PaletaVoidform.Alerta : PaletaVoidform.TextoPrincipal;
        }

        private void ManejarConteoBases(int destructores, int cuidadores)
        {
            textoSubtituloModo.text = $"CONQUISTA · <color={PaletaVoidform.Hex(PaletaVoidform.Destructor)}>Destructores {destructores}</color>  bases  " +
                                      $"<color={PaletaVoidform.Hex(PaletaVoidform.Cuidador)}>{cuidadores} Cuidadores</color> · ¡5 bases = victoria inmediata!";
        }

        private void ManejarBase(ZonaCapturaBase b)
        {
            int i = b.IndiceBase;
            if (i < 0 || i >= 5) return;
            Color color = PaletaVoidform.ColorEstadoBase(b.EstadoActual);
            fondosBases[i].color = new Color(color.r * 0.35f, color.g * 0.35f, color.b * 0.35f, 0.95f);
            letrasBases[i].color = color;
            float progreso = Mathf.Abs(b.ProgresoCaptura) / 100f;
            barrasBases[i].Establecer(progreso, "");
            barrasBases[i].EstablecerColor(b.ProgresoCaptura < 0f ? PaletaVoidform.Destructor : PaletaVoidform.Cuidador);
            letrasBases[i].text = b.SiendoDisputada ? $"{b.Letra}!" : b.Letra;
        }

        private void ManejarReaparicionProgramada(SistemaSaludYTraje entidad, float segundos)
        {
            if (entidad == null || !entidad.EsJugadorLocal) return;
            finReaparicion = Time.time + segundos;
        }

        private void ManejarMarcador(int ronda, int ganadasD, int ganadasC, int empates)
        {
            textoSubtituloModo.text = $"4VS4 · Ronda {ronda}/3 · <color={PaletaVoidform.Hex(PaletaVoidform.Destructor)}>Destructores {ganadasD}</color> — " +
                                      $"<color={PaletaVoidform.Hex(PaletaVoidform.Cuidador)}>{ganadasC} Cuidadores</color> · Empates {empates}";
        }

        private void ManejarRelojRonda(float segundos)
        {
            textoReloj.text = UtilidadesTipos.FormatearReloj(segundos);
            textoReloj.color = segundos <= 20f ? PaletaVoidform.Alerta : PaletaVoidform.TextoPrincipal;
        }

        private void ManejarVivos(int vivosD, int vivosC)
        {
            if (contexto == null || contexto.Modo4vs4 == null) return;
            GestorModo4vs4 m = contexto.Modo4vs4;
            textoSubtituloModo.text = $"4VS4 · Ronda {m.RondaActual}/3 · Vivos: <color={PaletaVoidform.Hex(PaletaVoidform.Destructor)}>{vivosD}</color> vs " +
                                      $"<color={PaletaVoidform.Hex(PaletaVoidform.Cuidador)}>{vivosC}</color> · Rondas {m.RondasGanadasDestructores}-{m.RondasGanadasCuidadores} (empates {m.RondasEmpatadas})";
        }

        private void ManejarModoEspectador(bool activo)
        {
            panelEspectador.SetActive(activo);
            if (activo) textoEspectador.text = "MODO ESPECTADOR — esperando compañero...";
            foreach (Image m in mira) m.enabled = !activo;
        }

        private void ManejarObservado(string nombre)
        {
            if (!panelEspectador.activeSelf) return;
            textoEspectador.text = string.IsNullOrEmpty(nombre)
                ? "MODO ESPECTADOR"
                : $"MODO ESPECTADOR — Observando a: {nombre}   ·   Clic / Tab para cambiar   ·   Revives al terminar la ronda";
        }

        private void ManejarInicioRonda(int ronda)
        {
            MostrarBanner($"RONDA {ronda} DE 3", 2f);
        }

        private void ManejarFinRonda(int ronda, ResultadoEnfrentamiento resultado, string mensaje)
        {
            Color c = resultado == ResultadoEnfrentamiento.Empate ? PaletaVoidform.Disputa
                : resultado == ResultadoEnfrentamiento.VictoriaDestructores ? PaletaVoidform.Destructor : PaletaVoidform.Cuidador;
            MostrarBanner($"<color={PaletaVoidform.Hex(c)}>{mensaje}</color>", 3.5f);
        }

        private void ManejarObjetivo(string objetivo)
        {
            objetivoHistoria = objetivo;
            ActualizarTextoHistoria();
        }

        private void ManejarExtraccion(string estado)
        {
            estadoExtraccion = estado;
            ActualizarTextoHistoria();
        }

        private void ActualizarTextoHistoria()
        {
            if (contexto == null || contexto.Historia == null) return;
            GestorModoHistoria h = contexto.Historia;
            textoHistoria.text = $"<b><color={PaletaVoidform.Hex(DatosBioma.Obtener(h.BiomaActual).ColorAcento)}>{h.Narrativa.Titulo}</color></b>\n" +
                                 $"<size=15><color=#A9B4C8>{h.DescripcionPerspectiva}</color></size>\n" +
                                 $"<b>Objetivo:</b> {objetivoHistoria}\n<b>Fuente:</b> {estadoExtraccion}";
        }

        public void ActualizarPanelCopasHUD(int copas, string rango, LigaCompetitiva liga)
        {
            if (textoCopasHUD == null) return;
            bool mostrar = contexto != null && contexto.Configuracion.modo == ModoJuego.Modo4vs4;
            textoCopasHUD.text = mostrar ? $"Copas {copas} · {rango} · Liga {UtilidadesTipos.NombreLiga(liga)}" : "";
        }

        // =====================================================================
        // Registro de combate, banners y efectos temporales
        // =====================================================================

        private void ManejarMensajeSistema(string mensaje, TipoMensajeSistema tipo)
        {
            Color c;
            switch (tipo)
            {
                case TipoMensajeSistema.Exito: c = PaletaVoidform.Cuidador; break;
                case TipoMensajeSistema.Advertencia: c = PaletaVoidform.Disputa; break;
                case TipoMensajeSistema.Critico: c = PaletaVoidform.Alerta; break;
                case TipoMensajeSistema.Narrativa: c = PaletaVoidform.AcentoVoid; break;
                default: c = PaletaVoidform.TextoSecundario; break;
            }
            AgregarRegistro($"<color={PaletaVoidform.Hex(c)}>{mensaje}</color>");
        }

        private void ManejarBaja(InformacionBaja baja)
        {
            string victima = $"<color={PaletaVoidform.Hex(PaletaVoidform.ColorBando(baja.BandoVictima))}>{baja.NombreVictima}</color>";
            string causa = baja.TieneAtacante
                ? $"<color={PaletaVoidform.Hex(PaletaVoidform.ColorBando(baja.BandoAtacante))}>{baja.NombreAtacante}</color> [{baja.Origen}]"
                : $"[{baja.Origen}]";
            AgregarRegistro($"{causa} × {victima}{(baja.FueExplosionDeTraje ? " (explosión de traje)" : "")}");
        }

        private void AgregarRegistro(string texto)
        {
            registro.Add(new EntradaRegistro { Texto = texto, Momento = Time.unscaledTime });
            while (registro.Count > 7) registro.RemoveAt(0);
        }

        private void MostrarBanner(string texto, float segundos)
        {
            if (textoBanner == null) return;
            textoBanner.text = texto;
            finBanner = Time.unscaledTime + segundos;
        }

        private void ActualizarHUDTemporal()
        {
            if (hud == null || !hud.activeSelf) return;
            float ahora = Time.unscaledTime;

            // Registro de combate: las entradas desaparecen tras 7 segundos.
            registro.RemoveAll(e => ahora - e.Momento > 7f);
            string texto = "";
            foreach (EntradaRegistro e in registro) texto += e.Texto + "\n";
            textoRegistro.text = texto;

            // Banner central con desvanecimiento.
            float alfaBanner = Mathf.Clamp01((finBanner - ahora) / 0.5f);
            textoBanner.color = new Color(1f, 1f, 1f, alfaBanner);

            // Marcador de impacto.
            Color cm = textoMarcadorImpacto.color;
            textoMarcadorImpacto.color = new Color(cm.r, cm.g, cm.b, ahora < finMarcadorImpacto ? 1f : 0f);

            // Alerta de traje crítico parpadeante.
            if (alertaTrajeActiva)
            {
                textoAlertaTraje.color = new Color(1f, 0.2f, 0.15f, 0.45f + Mathf.PingPong(ahora * 3f, 0.55f));
            }

            // Aviso de maná insuficiente.
            textoAvisoMana.color = new Color(PaletaVoidform.Mana.r, PaletaVoidform.Mana.g, PaletaVoidform.Mana.b, Mathf.Clamp01((finAvisoMana - ahora) / 0.4f));

            // Viñeta de daño.
            intensidadVineta = Mathf.MoveTowards(intensidadVineta, 0f, Time.unscaledDeltaTime * 0.8f);
            vinetaDano.color = new Color(0.8f, 0f, 0f, intensidadVineta);

            // Cuenta atrás de reaparición (Conquista).
            if (finReaparicion > 0f)
            {
                float restante = finReaparicion - Time.time;
                textoReaparicion.text = restante > 0f ? $"Reapareciendo en {Mathf.CeilToInt(restante)}..." : "";
                if (restante <= 0f) finReaparicion = 0f;
            }
            else if (contexto != null && contexto.JugadorLocal != null && contexto.JugadorLocal.EstaMuerto && contexto.Configuracion.modo != ModoJuego.Conquista5Bases)
            {
                textoReaparicion.text = contexto.Configuracion.modo == ModoJuego.Modo4vs4 ? "Has caído · revives en la próxima ronda" : "Has caído";
            }
            else if (textoReaparicion.text.Length > 0 && contexto != null && contexto.JugadorLocal != null && !contexto.JugadorLocal.EstaMuerto)
            {
                textoReaparicion.text = "";
            }
        }
    }
}
