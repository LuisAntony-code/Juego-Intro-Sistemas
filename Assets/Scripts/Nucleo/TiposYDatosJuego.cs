using System;
using UnityEngine;

namespace VoidformBioForge
{
    // =====================================================================
    // TiposYDatosJuego.cs
    // ---------------------------------------------------------------------
    // Archivo raíz de tipos compartidos. Todo sistema del juego (combate,
    // guardianes, modos, salas, clasificación, biomas e interfaz) depende de
    // estas enumeraciones, por eso se ordenan de lo más simple a lo más
    // complejo:
    //   Nivel 1: identidad básica (bando, elemento, modo, sala).
    //   Nivel 2: reglas y recursos (daño, trajes, armas, estados alterados).
    //   Nivel 3: estados avanzados (bases, ligas, biomas, resultados, flujo).
    // =====================================================================

    // ---------------------------------------------------------------------
    // NIVEL 1 — Identidad básica
    // ---------------------------------------------------------------------

    /// <summary>
    /// Bandos jugables en Voidform: Bio-Forge.
    /// Destructor = expedición humana extractora. Cuidador = guardianes elementales nativos.
    /// </summary>
    public enum BandoJuego
    {
        Destructor,
        Cuidador
    }

    /// <summary>
    /// Elementos asociados a los biomas y a los personajes guardianes de los Cuidadores.
    /// </summary>
    public enum TipoElemento
    {
        Arbol,
        Agua,
        Tierra,
        Fuego
    }

    /// <summary>
    /// Modos principales disponibles en el juego.
    /// </summary>
    public enum ModoJuego
    {
        Historia,
        Conquista5Bases,
        Modo4vs4
    }

    /// <summary>
    /// Formas de entrar o configurar una partida en los modos que lo permiten.
    /// </summary>
    public enum TipoSalaConexion
    {
        CrearSala,
        ConBots,
        ConAmigos,
        SalaPublicaAleatoria
    }

    // ---------------------------------------------------------------------
    // NIVEL 2 — Reglas, recursos y condiciones
    // ---------------------------------------------------------------------

    /// <summary>
    /// Tipo de daño. Permite que cada traje resista o sea débil frente a un elemento
    /// concreto y que las armas humanas (cinéticas) se distingan de las explosiones.
    /// </summary>
    public enum TipoDano
    {
        Cinetico,
        Explosivo,
        Arbol,
        Agua,
        Tierra,
        Fuego
    }

    /// <summary>
    /// Trajes especializados de los Destructores. Cada uno contrarresta un elemento
    /// y es vulnerable a otro, formando un ciclo de balance.
    /// </summary>
    public enum TipoTraje
    {
        CineticoAsalto,
        TermicoIgnifugo,
        HidrostaticoAnclado,
        SismicoBlindado,
        PodadorBiotecnico
    }

    /// <summary>
    /// Armas reestructuradas a partir del ciclo clásico de arena shooter
    /// (pistola, subfusil, rifle, escopeta y precisión) con nombres e identidad propios.
    /// </summary>
    public enum TipoArma
    {
        PistolaAuxiliar,
        SubfusilRafaga,
        RifleExtractor,
        EscopetaDispersora,
        RiflePrecision
    }

    /// <summary>
    /// Estados alterados que pueden aplicar las habilidades elementales o el entorno.
    /// </summary>
    public enum TipoEstadoAlterado
    {
        Ralentizado,
        Enraizado,
        Quemadura,
        Empujado
    }

    /// <summary>
    /// Estados por los que pasa una sala de juego desde que se crea hasta que se cierra.
    /// </summary>
    public enum EstadoSala
    {
        SinSala,
        Creando,
        BuscandoPartida,
        EsperandoJugadores,
        Lista,
        EnPartida,
        Cerrada
    }

    // ---------------------------------------------------------------------
    // NIVEL 3 — Estados avanzados y de cierre
    // ---------------------------------------------------------------------

    /// <summary>
    /// Estados de control de cada una de las 5 bases en Modo Conquista.
    /// </summary>
    public enum EstadoControlBase
    {
        Neutral,
        EnDisputa,
        DominadaDestructores,
        DominadaCuidadores
    }

    /// <summary>
    /// Ligas competitivas del modo 4vs4.
    /// </summary>
    public enum LigaCompetitiva
    {
        Bronce,
        Plata,
        Oro,
        Platino,
        Diamante,
        NucleoSupremo
    }

    /// <summary>
    /// Los 5 biomas del exoplaneta. Los cuatro primeros pertenecen a un elemento;
    /// el quinto (Corazón del Núcleo) une los cuatro y es el escenario del clímax.
    /// </summary>
    public enum BiomaPlaneta
    {
        BosqueAncestral,
        HumedalesEspejo,
        CordilleraFracturada,
        CalderaIgnea,
        CorazonDelNucleo
    }

    /// <summary>
    /// Resultado de una ronda, de una partida de Conquista o de un nivel de Historia.
    /// </summary>
    public enum ResultadoEnfrentamiento
    {
        Pendiente,
        VictoriaDestructores,
        VictoriaCuidadores,
        Empate
    }

    /// <summary>
    /// Pantallas y fases del flujo general del juego (menús, partida, resultados).
    /// </summary>
    public enum EstadoFlujoJuego
    {
        MenuPrincipal,
        SeleccionBando,
        SeleccionModo,
        Personalizacion,
        SeleccionSala,
        SeleccionNivelHistoria,
        Briefing,
        EnPartida,
        Pausa,
        Resultados,
        HistoriaYProblematica,
        Controles,
        Clasificacion
    }

    /// <summary>
    /// Métodos auxiliares de conversión entre tipos, usados en todo el proyecto para que
    /// los textos visibles estén siempre en español correcto (con tildes y eñes).
    /// </summary>
    public static class UtilidadesTipos
    {
        public static BandoJuego BandoContrario(BandoJuego bando)
        {
            return bando == BandoJuego.Destructor ? BandoJuego.Cuidador : BandoJuego.Destructor;
        }

        public static TipoDano ElementoATipoDano(TipoElemento elemento)
        {
            switch (elemento)
            {
                case TipoElemento.Arbol: return TipoDano.Arbol;
                case TipoElemento.Agua: return TipoDano.Agua;
                case TipoElemento.Tierra: return TipoDano.Tierra;
                default: return TipoDano.Fuego;
            }
        }

        public static string NombreBando(BandoJuego bando)
        {
            return bando == BandoJuego.Destructor ? "Destructores" : "Cuidadores";
        }

        public static string NombreBandoSingular(BandoJuego bando)
        {
            return bando == BandoJuego.Destructor ? "Destructor (Humano)" : "Cuidador (Guardián elemental)";
        }

        public static string NombreElemento(TipoElemento elemento)
        {
            switch (elemento)
            {
                case TipoElemento.Arbol: return "Árbol";
                case TipoElemento.Agua: return "Agua";
                case TipoElemento.Tierra: return "Tierra";
                default: return "Fuego";
            }
        }

        public static string NombreModo(ModoJuego modo)
        {
            switch (modo)
            {
                case ModoJuego.Historia: return "Modo Historia";
                case ModoJuego.Conquista5Bases: return "Modo Conquista (5 bases / 5 minutos)";
                default: return "Modo 4vs4 (3 rondas)";
            }
        }

        public static string NombreSala(TipoSalaConexion sala)
        {
            switch (sala)
            {
                case TipoSalaConexion.CrearSala: return "Crear sala";
                case TipoSalaConexion.ConBots: return "Con bots";
                case TipoSalaConexion.ConAmigos: return "Con amigos";
                default: return "Sala pública";
            }
        }

        public static string NombreLiga(LigaCompetitiva liga)
        {
            switch (liga)
            {
                case LigaCompetitiva.Bronce: return "Bronce";
                case LigaCompetitiva.Plata: return "Plata";
                case LigaCompetitiva.Oro: return "Oro";
                case LigaCompetitiva.Platino: return "Platino";
                case LigaCompetitiva.Diamante: return "Diamante";
                default: return "Núcleo Supremo";
            }
        }

        public static string NombreEstadoAlterado(TipoEstadoAlterado estado)
        {
            switch (estado)
            {
                case TipoEstadoAlterado.Ralentizado: return "Ralentizado";
                case TipoEstadoAlterado.Enraizado: return "Enraizado";
                case TipoEstadoAlterado.Quemadura: return "Quemadura";
                default: return "Empujado";
            }
        }

        public static string NombreEstadoSala(EstadoSala estado)
        {
            switch (estado)
            {
                case EstadoSala.SinSala: return "Sin sala";
                case EstadoSala.Creando: return "Creando sala...";
                case EstadoSala.BuscandoPartida: return "Buscando sala pública...";
                case EstadoSala.EsperandoJugadores: return "Esperando jugadores";
                case EstadoSala.Lista: return "Sala lista";
                case EstadoSala.EnPartida: return "En partida";
                default: return "Sala cerrada";
            }
        }

        public static string NombreResultado(ResultadoEnfrentamiento resultado)
        {
            switch (resultado)
            {
                case ResultadoEnfrentamiento.VictoriaDestructores: return "Victoria de los Destructores";
                case ResultadoEnfrentamiento.VictoriaCuidadores: return "Victoria de los Cuidadores";
                case ResultadoEnfrentamiento.Empate: return "Empate";
                default: return "En curso";
            }
        }

        public static ResultadoEnfrentamiento VictoriaDe(BandoJuego bando)
        {
            return bando == BandoJuego.Destructor
                ? ResultadoEnfrentamiento.VictoriaDestructores
                : ResultadoEnfrentamiento.VictoriaCuidadores;
        }

        /// <summary>Convierte segundos a formato de reloj m:ss (por ejemplo 300 → 5:00).</summary>
        public static string FormatearReloj(float segundos)
        {
            int total = Mathf.CeilToInt(Mathf.Max(0f, segundos));
            int minutos = total / 60;
            int resto = total % 60;
            return $"{minutos}:{resto:00}";
        }

        /// <summary>Letra visible de cada base (0 → A, 4 → E).</summary>
        public static string LetraBase(int indice)
        {
            return ((char)('A' + Mathf.Clamp(indice, 0, 25))).ToString();
        }
    }

    /// <summary>
    /// Paleta de colores oficial del juego. Centralizarla evita colores sueltos repartidos
    /// por el código y mantiene coherentes la interfaz, los modelos y los VFX.
    /// </summary>
    public static class PaletaVoidform
    {
        public static readonly Color FondoVacio = new Color(0.035f, 0.05f, 0.09f, 0.92f);
        public static readonly Color PanelProfundo = new Color(0.07f, 0.1f, 0.16f, 0.9f);
        public static readonly Color PanelClaro = new Color(0.13f, 0.18f, 0.27f, 0.95f);
        public static readonly Color TextoPrincipal = new Color(0.93f, 0.96f, 1f, 1f);
        public static readonly Color TextoSecundario = new Color(0.62f, 0.7f, 0.82f, 1f);
        public static readonly Color AcentoVoid = new Color(0.55f, 0.45f, 1f, 1f);

        public static readonly Color Destructor = new Color(1f, 0.48f, 0.22f, 1f);
        public static readonly Color DestructorOscuro = new Color(0.45f, 0.18f, 0.08f, 1f);
        public static readonly Color Cuidador = new Color(0.3f, 1f, 0.7f, 1f);
        public static readonly Color CuidadorOscuro = new Color(0.08f, 0.38f, 0.26f, 1f);
        public static readonly Color Neutral = new Color(0.75f, 0.78f, 0.85f, 1f);
        public static readonly Color Disputa = new Color(1f, 0.9f, 0.3f, 1f);

        public static readonly Color Vida = new Color(0.95f, 0.27f, 0.32f, 1f);
        public static readonly Color Blindaje = new Color(0.35f, 0.75f, 1f, 1f);
        public static readonly Color Mana = new Color(0.45f, 0.55f, 1f, 1f);
        public static readonly Color Alerta = new Color(1f, 0.2f, 0.15f, 1f);
        public static readonly Color Ecosistema = new Color(0.45f, 0.95f, 0.45f, 1f);

        public static readonly Color Arbol = new Color(0.35f, 0.85f, 0.3f, 1f);
        public static readonly Color Agua = new Color(0.25f, 0.65f, 1f, 1f);
        public static readonly Color Tierra = new Color(0.78f, 0.58f, 0.34f, 1f);
        public static readonly Color Fuego = new Color(1f, 0.42f, 0.1f, 1f);

        public static Color ColorBando(BandoJuego bando)
        {
            return bando == BandoJuego.Destructor ? Destructor : Cuidador;
        }

        public static Color ColorElemento(TipoElemento elemento)
        {
            switch (elemento)
            {
                case TipoElemento.Arbol: return Arbol;
                case TipoElemento.Agua: return Agua;
                case TipoElemento.Tierra: return Tierra;
                default: return Fuego;
            }
        }

        public static Color ColorEstadoBase(EstadoControlBase estado)
        {
            switch (estado)
            {
                case EstadoControlBase.DominadaDestructores: return Destructor;
                case EstadoControlBase.DominadaCuidadores: return Cuidador;
                case EstadoControlBase.EnDisputa: return Disputa;
                default: return Neutral;
            }
        }

        public static string Hex(Color color)
        {
            return "#" + ColorUtility.ToHtmlStringRGB(color);
        }
    }
}
