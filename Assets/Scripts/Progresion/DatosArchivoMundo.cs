using System.Text;
using UnityEngine;

namespace VoidformBioForge
{
    /// <summary>
    /// Contenido del "Archivo del mundo", el apartado de Historia del lobby.
    /// Nivel 1: lore general. Nivel 2: escenarios, fuentes y guardianes, generados desde los datos del juego
    /// (así nunca se desincronizan del código). Nivel 3: arquetipos humanos y reglas de la campaña.
    /// La versión completa, paso a paso, está en Docs/06 a Docs/09.
    /// </summary>
    public static class DatosArchivoMundo
    {
        public static readonly string[] TitulosSecciones =
        {
            "Lore y problemática", "Escenarios y fuentes", "Guardianes", "Arquetipos humanos", "Reglas de la campaña"
        };

        public static string ObtenerSeccion(int indice)
        {
            switch (indice)
            {
                case 0: return SeccionLore();
                case 1: return SeccionEscenarios();
                case 2: return SeccionGuardianes();
                case 3: return SeccionArquetipos();
                default: return SeccionReglas();
            }
        }

        private static string SeccionLore()
        {
            return DatosNarrativaHistoria.Prologo + "\n\n" +
                   "<b><color=#7CFFB0>Problemática social.</color></b> Extractivismo frente a preservación: la barra de INTEGRIDAD DEL ECOSISTEMA baja " +
                   "con cada extracción y el escenario se marchita en pantalla. Trabajo en equipo y empatía: ningún personaje gana solo, y jugar " +
                   "los dos bandos obliga a entender las razones del otro. El final de la campaña depende de cuánto planeta dejes en pie.";
        }

        private static string SeccionEscenarios()
        {
            StringBuilder sb = new StringBuilder();
            for (int i = 0; i < GuardadoProgreso.NivelesTotales; i++)
            {
                DatosBioma d = DatosBioma.Obtener((BiomaPlaneta)i);
                sb.Append($"<b><color={PaletaVoidform.Hex(d.ColorAcento)}>Nivel {i + 1} · {d.Nombre}</color></b>");
                sb.Append(d.TieneElemento ? $"  <color=#A9B4C8>(elemento {UtilidadesTipos.NombreElemento(d.Elemento)})</color>\n" : "  <color=#A9B4C8>(confluencia)</color>\n");
                sb.Append($"<size=16>Fuente — <b>{d.NombreFuente}</b>: {d.DescripcionFuente}\n");
                sb.Append($"Peligro — {d.NombrePeligro}: {d.DescripcionPeligro}");
                if (d.MultiplicadorGravedad < 0.99f) sb.Append($" Gravedad ×{d.MultiplicadorGravedad:0.0}.");
                sb.Append("</size>\n");
            }
            return sb.ToString();
        }

        private static string SeccionGuardianes()
        {
            StringBuilder sb = new StringBuilder();
            sb.Append("<b>Regla de atributos:</b> todo guardián tiene MÁS VIDA que cualquier humano y un POCO MENOS de velocidad " +
                      "(misma aceleración, así que se siente ágil, pero pierde las carreras largas).\n\n");
            foreach (TipoElemento e in UtilidadesTipos.ElementosJugables)
            {
                DatosGuardianElemental v = DatosGuardianElemental.CrearVariante(e, RolGuardian.Vanguardia);
                DatosGuardianElemental c = DatosGuardianElemental.CrearVariante(e, RolGuardian.Control);
                DatosGuardianElemental a = DatosGuardianElemental.CrearVariante(e, RolGuardian.Artilleria);
                sb.Append($"<b><color={PaletaVoidform.Hex(v.colorPrimario)}>{v.linaje}</color></b> — {UtilidadesTipos.NombreElemento(e)}\n");
                sb.Append($"<size=16>Corta: {v.nombreGuardian} ({Mathf.RoundToInt(v.vidaMaxima)} vida) · Media: {c.nombreGuardian} ({Mathf.RoundToInt(c.vidaMaxima)}) · " +
                          $"Larga: {a.nombreGuardian} ({Mathf.RoundToInt(a.vidaMaxima)}) · Velocidad ×{v.multiplicadorVelocidad:0.00}\n");
                sb.Append($"Punto débil: {v.nucleoPuntoDebil}\nSobrecarga: {v.sobrecarga}</size>\n");
            }
            DatosGuardianElemental nexo = DatosGuardianElemental.CrearNexus(TipoElemento.Arbol, RolGuardian.Control);
            sb.Append($"<b><color={PaletaVoidform.Hex(PaletaVoidform.AcentoVoid)}>{nexo.linaje}</color></b> — Eco Prismático, Aethernum y Vigía Aural; " +
                      $"rotan entre Madera, Agua, Fuego y Metal (tecla F).\n<size=16>Punto débil: {nexo.nucleoPuntoDebil}</size>");
            return sb.ToString();
        }

        private static string SeccionArquetipos()
        {
            return "<b>Los 5 arquetipos humanos</b> <color=#A9B4C8>(diseño de producción; el prototipo jugable usa aún los 5 trajes y armas de la " +
                   "versión 1. Ficha completa en Docs/09_Arquetipos_Humanos_y_Armas_Modulares.md)</color>\n\n" +
                   "<b><color=#7CFFB0>Cazador / Explorador Forestal</color></b> — gambesón verde, yelmo de visor anguloso, arco curvo, carcaj y machete.\n" +
                   "<size=16>Paso de maleza (corta) · Camuflaje de fibras (media) · Estabilizador de mira (larga)</size>\n" +
                   "<b><color=#FFE36B>Paladín / Templario del Alba</color></b> — placas blancas y doradas, emblema solar, capa clara y mandoble.\n" +
                   "<size=16>Aura de repulsión (corta) · Égida refractaria (media) · Faro de vanguardia (larga)</size>\n" +
                   "<b><color=#A9B4C8>Caballero Bastión / Guardia de Hierro</color></b> — acero oscuro grabado, faldón claro, maza con púas y escudo barroco.\n" +
                   "<size=16>Anclaje sísmico (corta) · Absorción de impacto (media) · Intercepción de artillería (larga)</size>\n" +
                   "<b><color=#7AB8FF>Tirador / Cazador de Reliquias</color></b> — abrigo largo azul y marrón, bandoleras, rifle largo de latón con mira.\n" +
                   "<size=16>Propulsión de retroceso (corta) · Disipador térmico (media) · Visor telemétrico (larga)</size>\n" +
                   "<b><color=#FF6EC7>Ciborg / Vanguardia Biomecánica</color></b> — implantes faciales, placas plateadas, núcleo magenta en el pecho.\n" +
                   "<size=16>Sobrecarga del núcleo (corta) · Escudo de frecuencia (media) · Enlace de rastreo (larga)</size>\n\n" +
                   "<b>Armas modulares (combinables con cualquier arquetipo):</b> Arco Compuesto · Mandoble / Espada Larga · Maza con Púas y Escudo · " +
                   "Rifle Largo con Mira · Arsenal Biomecánico Acoplado.";
        }

        private static string SeccionReglas()
        {
            StringBuilder sb = new StringBuilder();
            sb.Append("<b>Bando humano:</b> escuadrón de 4 (jugadores reales + bots/mods) contra 3 guardianes del escenario en los niveles 1 a 5 " +
                      "(vanguardia, control y artillería) y contra 5 guardianes, uno de cada escenario, en el nivel 6.\n");
            sb.Append("<b>Bando guardián:</b> 2 guardianes (tú y un aliado IA) contra 4 humanos; para avanzar debes ganarles.\n");
            sb.Append("<b>Victoria humana:</b> derrotar a todos los guardianes o extraer el 100% de la fuente (con coste ecológico). " +
                      "<b>Victoria guardiana:</b> eliminar a los 4 humanos. En Historia nadie reaparece: quien cae espectea a su equipo. " +
                      "Caída simultánea de los últimos de cada bando = empate y se repite el nivel.\n\n");
            for (int nivel = 1; nivel <= GuardadoProgreso.NivelesTotales; nivel++)
            {
                ComposicionNivelHistoria humano = new ComposicionNivelHistoria { Guardianes = GestorModoHistoria.GuardianesDefensores(nivel) };
                ComposicionNivelHistoria guardian = new ComposicionNivelHistoria { Guardianes = GestorModoHistoria.ParejaDelJugadorGuardian(nivel) };
                sb.Append($"<b>Nivel {nivel}</b> <size=16>— contra los humanos: {humano.DescribirGuardianes()}. Como guardián: {guardian.DescribirGuardianes()}.</size>\n");
            }
            return sb.ToString();
        }
    }
}
