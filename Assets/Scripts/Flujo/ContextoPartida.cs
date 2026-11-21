using System.Collections.Generic;
using UnityEngine;

namespace VoidformBioForge
{
    /// <summary>
    /// Todo lo que existe en la partida en curso. El GestorFlujoPartida lo construye y lo entrega a la
    /// interfaz mediante un evento, de forma que el HUD se suscribe solo a los sistemas que realmente
    /// existen en ese modo (por ejemplo, no hay bases en 4vs4 ni rondas en Conquista).
    /// </summary>
    public class ContextoPartida
    {
        public ConfiguracionPartida Configuracion;
        public DescripcionArena Arena;
        public GestorEquilibrioEcosistema Ecosistema;

        public SistemaSaludYTraje JugadorLocal;
        public SistemaArmasYTrajeDestructor ArmasLocal;
        public ControladorGuardianElemental GuardianLocal;
        public SistemaEstadosAlterados EstadosLocal;
        public CamaraEspectador Espectador;

        public GestorConquista5Bases Conquista;
        public GestorModo4vs4 Modo4vs4;
        public GestorModoHistoria Historia;

        public readonly List<SistemaSaludYTraje> EquipoDestructores = new List<SistemaSaludYTraje>();
        public readonly List<SistemaSaludYTraje> EquipoCuidadores = new List<SistemaSaludYTraje>();
        public readonly List<ZonaCapturaBase> Bases = new List<ZonaCapturaBase>();

        public bool JugadorEsDestructor => Configuracion != null && Configuracion.bandoJugador == BandoJuego.Destructor;
    }
}
