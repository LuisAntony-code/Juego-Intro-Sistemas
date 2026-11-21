using UnityEngine;

namespace VoidformBioForge
{
    /// <summary>
    /// Textos narrativos completos de un nivel del Modo Historia, desde las dos perspectivas.
    /// </summary>
    public struct NivelNarrativo
    {
        public int Numero;
        public string Titulo;
        public BiomaPlaneta Bioma;
        public string IntroDestructor;
        public string IntroCuidador;
        public string ObjetivoDestructor;
        public string ObjetivoCuidador;
        public string VictoriaDestructor;
        public string VictoriaCuidador;
        public string DerrotaDestructor;
        public string DerrotaCuidador;
        public string ReflexionSocial;
    }

    /// <summary>
    /// Guion del Modo Historia de Voidform: Bio-Forge.
    /// Nivel 1: prólogo del viaje espacial. Nivel 2: los 6 niveles con sus dos perspectivas.
    /// Nivel 3: desenlaces que dependen del bando y de la integridad ecológica lograda en la campaña.
    /// </summary>
    public static class DatosNarrativaHistoria
    {
        public const string Prologo =
            "AÑO 2291. La Tierra agotó su último yacimiento energético. Las ciudades se apagan por turnos, los océanos " +
            "están cansados y los bosques son un recuerdo en los museos. La sonda Heliadna detecta, a 41 años luz, un " +
            "planeta que emite energía como si respirara: VOIDFORM.\n\n" +
            "La nave-arca Bio-Forge parte con diez mil personas y una promesa: traer energía suficiente para encender " +
            "la Tierra otra vez. Al descender descubren que la energía no es un mineral inerte, sino una red viva con cinco " +
            "fuentes: el Árbol Madre del Bosque de los Susurros, la Turbina Abisal de las Mareas de Plasma, el Núcleo Térmico " +
            "de la Caldera, el Monolito de Resonancia de las Agujas de Aleación y, en el centro de todo, el Corazón del Planeta " +
            "en el Nexus Primordial. Cada metro extraído se siente en todo el planeta.\n\n" +
            "Los DESTRUCTORES —cazadores, paladines, caballeros, tiradores y ciborgs de la expedición— ven la salvación de la " +
            "humanidad. Los CUIDADORES —guardianes elementales nacidos del propio planeta— ven una herida que se abre. Nadie es " +
            "malvado. Todos tienen razones. Y la fuente no alcanza para todos si se la vacía.";

        public static NivelNarrativo ObtenerNivel(int nivel)
        {
            switch (Mathf.Clamp(nivel, 1, 6))
            {
                case 1:
                    return new NivelNarrativo
                    {
                        Numero = 1,
                        Titulo = "Nivel 1 — Raíces que Escuchan",
                        Bioma = BiomaPlaneta.BosqueSusurros,
                        IntroDestructor = "Escuadrón Alba, aterrizaje confirmado en el Bosque de los Susurros Bioluminiscentes. Bajo la corteza " +
                                          "translúcida late el Árbol Madre. Mando insiste: «la Tierra no puede esperar». Tres guardianes de corteza " +
                                          "se mueven entre los troncos... y el bosque entero parece observaros.",
                        IntroCuidador = "Eres un Coloso Silvestre. Algo metálico cayó del cielo y ya corta las raíces que alimentan al Árbol " +
                                        "Madre. Con el Guardián de Corteza a tu lado, dos contra cuatro, debes demostrar que el bosque sabe defenderse.",
                        ObjetivoDestructor = "Derrotad a los 3 guardianes de Madera o extraed el 100% de la energía del Árbol Madre (cuanta más " +
                                             "extraigáis, más se dañará el ecosistema).",
                        ObjetivoCuidador = "Junto a tu aliado, derrota a los 4 humanos antes de que vacíen el Árbol Madre. Para pasar de nivel debes ganarles.",
                        VictoriaDestructor = "La savia fluye hacia la órbita. Algunos árboles se apagan a vuestro paso. Un técnico pregunta en voz " +
                                             "baja si el bosque «duele».",
                        VictoriaCuidador = "Los humanos se repliegan a su cápsula. El Coloso no los persigue: deja crecer un brote junto a la " +
                                           "plataforma de aterrizaje, como quien deja una puerta abierta.",
                        DerrotaDestructor = "El escuadrón cae entre raíces que se cierran. La Bio-Forge ordena reintentar.",
                        DerrotaCuidador = "Las perforadoras rugen junto al Árbol Madre. El susurro del bosque se debilita... Inténtalo de nuevo.",
                        ReflexionSocial = "La deforestación no es solo talar árboles: rompe redes vivas invisibles. Lo que se extrae en un " +
                                          "punto se pierde en todo el sistema."
                    };

                case 2:
                    return new NivelNarrativo
                    {
                        Numero = 2,
                        Titulo = "Nivel 2 — El Abismo que Respira",
                        Bioma = BiomaPlaneta.AbismosMareas,
                        IntroDestructor = "Los Abismos de las Mareas de Plasma esconden la Turbina Abisal, que irradia energía a medio planeta. " +
                                          "Extraerla es rápido... y envenena las corrientes que mueven dos lunas. Tres guardianes de las mareas " +
                                          "patrullan los puentes colgantes.",
                        IntroCuidador = "Eres un Leviatán Abisal. El agua te contó lo que pasó en el bosque. Los humanos vienen mejor " +
                                        "preparados: han aprendido. Tú y el Señor de las Mareas también.",
                        ObjetivoDestructor = "Derrotad a los 3 guardianes de Agua o completad la extracción de la Turbina. Cuidado con las corrientes de plasma.",
                        ObjetivoCuidador = "Con tu aliado, expulsa a los 4 humanos usando corrientes y mareas antes de que agoten la Turbina.",
                        VictoriaDestructor = "Los depósitos se llenan. Río abajo, los reflejos de las dos lunas desaparecen del plasma.",
                        VictoriaCuidador = "El océano recupera su ritmo. El Señor de las Mareas devuelve a la orilla un casco humano intacto: " +
                                           "no busca matar, sino proteger.",
                        DerrotaDestructor = "Las mareas os arrastran al abismo. Reorganizad el escuadrón.",
                        DerrotaCuidador = "La Turbina se detiene. Las mareas se vuelven turbias. Vuelve a intentarlo.",
                        ReflexionSocial = "El agua conecta territorios: contaminarla en un lugar afecta a comunidades enteras aguas abajo."
                    };

                case 3:
                    return new NivelNarrativo
                    {
                        Numero = 3,
                        Titulo = "Nivel 3 — La Forja Despierta",
                        Bioma = BiomaPlaneta.CalderaForjas,
                        IntroDestructor = "La Caldera de las Forjas Magmáticas aloja el Núcleo Térmico, la fuente más volátil del planeta. Mando " +
                                          "exige «extracción máxima». Un Titán Ígneo, un Demonio de Magma y un Gólem de Chimenea —la forja viva del " +
                                          "valle— guardan la fumarola.",
                        IntroCuidador = "Eres un Titán Ígneo. La extracción térmica está despertando la Caldera: si vacían el Núcleo, el " +
                                        "clima entero colapsará... también para ellos. El Demonio de Magma pelea a tu lado.",
                        ObjetivoDestructor = "Derrotad a los 3 guardianes de la Caldera o completad la extracción. Los Trajes Térmicos resisten el magma.",
                        ObjetivoCuidador = "Con tu aliado, derrota a los 4 humanos antes de que desestabilicen el Núcleo Térmico.",
                        VictoriaDestructor = "Obtenéis la energía térmica, pero el cielo se oscurece de ceniza y las lecturas climáticas entran en rojo.",
                        VictoriaCuidador = "La Caldera vuelve a dormir. El Titán apaga su furia y deja que un humano herido escape hacia su cápsula.",
                        DerrotaDestructor = "El magma os rodea. Reintentad con otro escuadrón.",
                        DerrotaCuidador = "El Núcleo Térmico ruge fuera de control. Inténtalo de nuevo.",
                        ReflexionSocial = "Alterar los grandes reguladores naturales (volcanes, océanos, glaciares) desencadena crisis climáticas " +
                                          "que no distinguen bandos."
                    };

                case 4:
                    return new NivelNarrativo
                    {
                        Numero = 4,
                        Titulo = "Nivel 4 — La Ciudad que Levita",
                        Bioma = BiomaPlaneta.AgujasAleacion,
                        IntroDestructor = "Las Agujas de Aleación Magnética son una ciudad sin habitantes que levita sobre placas espejo. Su " +
                                          "Monolito Central de Resonancia estabiliza el campo magnético del planeta. Tres autómatas de aleación lo " +
                                          "custodian con precisión mecánica.",
                        IntroCuidador = "Eres un Androide de Aleación. Si los humanos detienen el Monolito, las torres caerán y el campo " +
                                        "magnético que protege Voidform de su sol se debilitará. El Centinela de Resonancia te cubre.",
                        ObjetivoDestructor = "Derrotad a los 3 guardianes de Metal o completad la extracción del Monolito.",
                        ObjetivoCuidador = "Con tu aliado, derrota a los 4 humanos antes de que silencien el zumbido del Monolito.",
                        VictoriaDestructor = "El Monolito se ralentiza. Una torre cae a lo lejos y su eco metálico recorre la llanura espejo.",
                        VictoriaCuidador = "El zumbido del Monolito vuelve a ser estable. Las torres retoman su levitación perfecta.",
                        DerrotaDestructor = "El campo magnético os aplasta contra las placas. Reintentad.",
                        DerrotaCuidador = "El zumbido se apaga y las torres tiemblan. Inténtalo de nuevo.",
                        ReflexionSocial = "Las infraestructuras invisibles que nos protegen (campos, atmósfera, capa de ozono) solo se notan " +
                                          "cuando fallan."
                    };

                case 5:
                    return new NivelNarrativo
                    {
                        Numero = 5,
                        Titulo = "Nivel 5 — El Santuario de la Confluencia",
                        Bioma = BiomaPlaneta.NexusPrimordial,
                        IntroDestructor = "El Nexus Primordial une los cuatro biomas bajo auroras multicolores y gravedad reducida. El Corazón " +
                                          "del Planeta flota en su centro. Tres Custodios del Nexus, capaces de adoptar cualquier elemento, os esperan.",
                        IntroCuidador = "Eres un Eco Prismático del Nexus. Llevas dentro la savia, el plasma, el magma y el metal (tecla F para " +
                                        "rotar de elemento). Aethernum, el Corazón Vivo, combate a tu lado.",
                        ObjetivoDestructor = "Derrotad a los 3 Custodios del Nexus o extraed el Corazón del Planeta. La gravedad reducida cambia los saltos.",
                        ObjetivoCuidador = "Con Aethernum, derrota a los 4 humanos rotando entre los cuatro elementos.",
                        VictoriaDestructor = "El Corazón del Planeta parpadea. Las auroras pierden un color.",
                        VictoriaCuidador = "Los cuatro elementos se calman y las auroras recuperan todos sus colores.",
                        DerrotaDestructor = "Los Custodios os expulsan del santuario. Reintentad.",
                        DerrotaCuidador = "El Nexus pierde el equilibrio. Vuelve a intentarlo.",
                        ReflexionSocial = "Los sistemas naturales están conectados: dañar uno desequilibra a todos los demás."
                    };

                default:
                    return new NivelNarrativo
                    {
                        Numero = 6,
                        Titulo = "Nivel 6 — Convergencia Total",
                        Bioma = BiomaPlaneta.ConvergenciaTotal,
                        IntroDestructor = "Los cinco escenarios se funden alrededor del Corazón del Planeta. Por primera vez, todos los linajes " +
                                          "se reúnen: el Coloso Silvestre, el Leviatán Abisal, el Titán Ígneo, el Androide de Aleación y Aethernum. " +
                                          "Cinco guardianes contra vuestro escuadrón. Vuestra integridad ecológica acumulada decidirá el final.",
                        IntroCuidador = "Eres Aethernum, el Corazón Vivo del Planeta. Todos los linajes se han reunido en la Convergencia y " +
                                        "sostienen el campo; tú y el Androide de Aleación sois quienes combatís. Defiende el origen de la vida.",
                        ObjetivoDestructor = "Derrotad a los 5 guardianes reunidos o extraed el Corazón en plena Convergencia.",
                        ObjetivoCuidador = "Con el Androide de Aleación, derrota a los 4 humanos en la Convergencia rotando entre los cuatro elementos.",
                        VictoriaDestructor = "El Corazón responde. Lo que ocurra ahora depende de cómo habéis tratado al planeta.",
                        VictoriaCuidador = "Los cinco escenarios se calman. Aethernum habla por primera vez a los humanos: «Hay suficiente para " +
                                           "vivir, nunca para vaciar».",
                        DerrotaDestructor = "La Convergencia os expulsa. Reintentad.",
                        DerrotaCuidador = "El Corazón parpadea. Vuelve a intentarlo, Corazón del Planeta.",
                        ReflexionSocial = "Los recursos de un planeta son finitos. La sostenibilidad es aprender a vivir de lo que se renueva, " +
                                          "no de lo que se agota."
                    };
            }
        }

        /// <summary>Desenlace final según bando y promedio de integridad ecológica de la campaña.</summary>
        public static string ObtenerDesenlace(BandoJuego bando, float promedioIntegridad)
        {
            if (bando == BandoJuego.Cuidador)
            {
                return "DESENLACE CUIDADOR — «EQUILIBRIO RESTAURADO»\n\n" +
                       "Los cinco biomas siguen vivos. Aethernum no expulsa a la humanidad: le ofrece un pacto. La Bio-Forge podrá " +
                       "tomar solo la energía que el planeta regenera cada año —ni un vatio más— a cambio de ayudar a sanar las " +
                       "cicatrices de la Cordillera y los ríos del Humedal. Por primera vez, guardianes y humanos trabajan juntos.\n\n" +
                       "El viaje no terminó en conquista ni en guerra, sino en un acuerdo: defender el ecosistema también era " +
                       "defender el futuro de quienes llegaron a destruirlo.";
            }

            if (promedioIntegridad >= 60f)
            {
                return "DESENLACE DESTRUCTOR — «PACTO DE EXTRACCIÓN RESPONSABLE»\n\n" +
                       $"Integridad ecológica media de la campaña: {Mathf.RoundToInt(promedioIntegridad)}%.\n\n" +
                       "Elegisteis vencer a los guardianes sin vaciar la Fuente. Ante el Núcleo, el escuadrón desobedece la orden " +
                       "de extracción máxima. Aethernum lo percibe y propone un límite: energía renovable, nunca más que la que el " +
                       "planeta puede regenerar. La Tierra se ilumina despacio, pero se ilumina. Voidform sigue respirando.";
            }

            if (promedioIntegridad >= 35f)
            {
                return "DESENLACE DESTRUCTOR — «LA VICTORIA AMARGA»\n\n" +
                       $"Integridad ecológica media de la campaña: {Mathf.RoundToInt(promedioIntegridad)}%.\n\n" +
                       "La Bio-Forge regresa con energía suficiente para una generación. Pero dos biomas ya no se recuperarán y " +
                       "los colonos que se quedan en Voidform conviven con tormentas de ceniza y ríos muertos. La humanidad ganó " +
                       "tiempo... y la misma pregunta que la obligó a partir de la Tierra.";
            }

            return "DESENLACE DESTRUCTOR — «EL PLANETA VACÍO»\n\n" +
                   $"Integridad ecológica media de la campaña: {Mathf.RoundToInt(promedioIntegridad)}%.\n\n" +
                   "Extrajisteis todo. El Núcleo se apaga y con él los bosques, los humedales, las montañas y el volcán. La " +
                   "energía llega a la Tierra, pero Voidform queda en silencio, igual que el planeta que dejasteis atrás. " +
                   "La humanidad repitió su error a 41 años luz de casa. ¿Habrá un tercer planeta?";
        }
    }
}
