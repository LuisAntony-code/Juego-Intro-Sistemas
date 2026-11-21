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
    /// Nivel 1: prólogo del viaje espacial. Nivel 2: los 5 niveles con sus dos perspectivas.
    /// Nivel 3: desenlaces que dependen del bando y de la integridad ecológica lograda en la campaña.
    /// </summary>
    public static class DatosNarrativaHistoria
    {
        public const string Prologo =
            "AÑO 2291. La Tierra agotó su último yacimiento energético. Las ciudades se apagan por turnos, los océanos " +
            "están cansados y los bosques son un recuerdo en los museos. La sonda Heliadna detecta, a 41 años luz, un " +
            "planeta que emite energía como si respirara: VOIDFORM.\n\n" +
            "La nave-arca Bio-Forge parte con diez mil personas y una promesa: traer energía suficiente para encender " +
            "la Tierra otra vez. Al descender descubren que la fuente no es un mineral inerte, sino una red viva que " +
            "une bosques, humedales, montañas y volcanes. Cada metro extraído se siente en todo el planeta.\n\n" +
            "Los DESTRUCTORES —operativos con exotrajes y armas extractoras— ven la salvación de la humanidad. Los " +
            "CUIDADORES —guardianes elementales nacidos del propio planeta— ven una herida que se abre. Nadie es " +
            "malvado. Todos tienen razones. Y la fuente no alcanza para todos si se la vacía.";

        public static NivelNarrativo ObtenerNivel(int nivel)
        {
            switch (Mathf.Clamp(nivel, 1, 5))
            {
                case 1:
                    return new NivelNarrativo
                    {
                        Numero = 1,
                        Titulo = "Nivel 1 — Raíces que Escuchan",
                        Bioma = BiomaPlaneta.BosqueAncestral,
                        IntroDestructor = "Escuadrón Alba, aterrizaje confirmado en el Bosque Ancestral. Los sensores marcan una " +
                                          "concentración energética bajo las raíces. Instalad la primera extracción. Mando insiste: " +
                                          "«la Tierra no puede esperar». Pero el bosque parece... observaros.",
                        IntroCuidador = "Eres SYLVARAN, Custodio del Bosque Ancestral. Algo metálico cayó del cielo y ya " +
                                        "corta tus raíces. No los odias: nunca habías visto criaturas tan asustadas. Pero si no " +
                                        "los detienes, el bosque dejará de cantar.",
                        ObjetivoDestructor = "Derrotad al guardián Sylvaran o extraed el 100% de la energía de la Fuente (cuanta más " +
                                             "extraigáis, más se dañará el ecosistema).",
                        ObjetivoCuidador = "Derrota a los 4 humanos del Escuadrón Alba antes de que vacíen la Fuente. Solo tú puedes " +
                                           "ser el Guardián en este nivel.",
                        VictoriaDestructor = "La primera reserva fluye hacia la órbita. Algunos árboles se apagan a vuestro paso. " +
                                             "Un técnico pregunta en voz baja si el bosque «duele».",
                        VictoriaCuidador = "Los humanos se repliegan a su cápsula. Sylvaran no los persigue: deja crecer un brote " +
                                           "junto a la plataforma de aterrizaje, como quien deja una puerta abierta.",
                        DerrotaDestructor = "El escuadrón cae entre raíces que se cierran. La Bio-Forge ordena reintentar.",
                        DerrotaCuidador = "Las perforadoras rugen. El canto del bosque se debilita... Inténtalo de nuevo.",
                        ReflexionSocial = "La deforestación no es solo talar árboles: rompe redes vivas invisibles. Lo que se extrae " +
                                          "en un punto se pierde en todo el sistema."
                    };

                case 2:
                    return new NivelNarrativo
                    {
                        Numero = 2,
                        Titulo = "Nivel 2 — El Espejo que se Enturbia",
                        Bioma = BiomaPlaneta.HumedalesEspejo,
                        IntroDestructor = "Los Humedales Espejo canalizan la energía como venas de agua. La extracción aquí es " +
                                          "rápida... y contamina los ríos que alimentan medio continente. Los trajes Hidrostáticos " +
                                          "están listos.",
                        IntroCuidador = "Eres NERIVALE, Custodia de las Mareas. El agua te contó lo que pasó en el bosque. Los " +
                                        "humanos vienen mejor preparados: han aprendido. Tú también.",
                        ObjetivoDestructor = "Derrotad a Nerivale o completad la extracción. Cuidado con las corrientes profundas.",
                        ObjetivoCuidador = "Expulsa a los 4 humanos de los humedales usando corrientes y mareas.",
                        VictoriaDestructor = "Los depósitos se llenan. Río abajo, los reflejos de las lunas desaparecen del agua.",
                        VictoriaCuidador = "Las lagunas recuperan su quietud. Nerivale devuelve a la orilla un casco humano intacto: " +
                                           "un mensaje de que no busca matar, sino proteger.",
                        DerrotaDestructor = "Las mareas os arrastran. Reorganizad el escuadrón.",
                        DerrotaCuidador = "El agua se vuelve turbia. Vuelve a intentarlo, Custodia.",
                        ReflexionSocial = "El agua conecta territorios: contaminarla en un lugar afecta a comunidades enteras aguas abajo."
                    };

                case 3:
                    return new NivelNarrativo
                    {
                        Numero = 3,
                        Titulo = "Nivel 3 — Cicatrices de la Cordillera",
                        Bioma = BiomaPlaneta.CordilleraFracturada,
                        IntroDestructor = "La Cordillera ya fue perforada por la primera oleada. Las vetas de cuarzo son la energía " +
                                          "más densa del planeta. Mando aumenta la cuota: «extracción máxima». Algunos del escuadrón " +
                                          "dudan.",
                        IntroCuidador = "Eres KORVHAL, Coloso de la Cordillera. Las perforadoras abandonadas aún humean en tus " +
                                        "laderas. Recuerdas cada montaña vaciada. Esta vez, la montaña responde.",
                        ObjetivoDestructor = "Derrotad a Korvhal o completad la extracción. Los Trajes Sísmicos resisten la tierra.",
                        ObjetivoCuidador = "Derriba a los 4 humanos con muros y ondas sísmicas antes de que vacíen la veta.",
                        VictoriaDestructor = "La veta principal se apaga. Un derrumbe sepulta una de vuestras perforadoras: la montaña " +
                                             "ya no aguanta más.",
                        VictoriaCuidador = "Los cañones vuelven al silencio. Korvhal levanta un muro alrededor de la perforadora " +
                                           "abandonada: no para destruirla, sino para que nadie olvide la herida.",
                        DerrotaDestructor = "La montaña se cierra sobre vosotros. Reintentad.",
                        DerrotaCuidador = "Las vetas se apagan una a una. Vuelve a intentarlo, Coloso.",
                        ReflexionSocial = "La minería sin límites deja cicatrices que duran siglos. La pregunta no es solo cuánto " +
                                          "podemos extraer, sino cuánto debemos."
                    };

                case 4:
                    return new NivelNarrativo
                    {
                        Numero = 4,
                        Titulo = "Nivel 4 — La Caldera Despierta",
                        Bioma = BiomaPlaneta.CalderaIgnea,
                        IntroDestructor = "La Caldera Ígnea regula el clima de todo Voidform. La extracción térmica la está " +
                                          "despertando: la lava avanza hacia los campamentos. Algunos colonos ya no quieren seguir.",
                        IntroCuidador = "Eres IGNARA, Centinela de la Caldera. Eres el último aviso del planeta. Si los humanos " +
                                        "vacían el volcán, el clima entero colapsará... también para ellos.",
                        ObjetivoDestructor = "Derrotad a Ignara o completad la extracción. Los Trajes Térmicos resisten la lava.",
                        ObjetivoCuidador = "Derrota a los 4 humanos antes de que desestabilicen el volcán.",
                        VictoriaDestructor = "Obtenéis la energía térmica, pero el cielo se oscurece de ceniza. Las lecturas climáticas " +
                                             "del planeta entran en rojo.",
                        VictoriaCuidador = "La Caldera vuelve a dormir. Ignara apaga su furia y, por primera vez, deja que un humano " +
                                           "herido escape hacia su cápsula.",
                        DerrotaDestructor = "La lava os rodea. Reintentad con otro equipo.",
                        DerrotaCuidador = "El volcán ruge fuera de control. Inténtalo de nuevo, Centinela.",
                        ReflexionSocial = "Alterar los grandes reguladores naturales (volcanes, océanos, glaciares) desencadena crisis " +
                                          "climáticas que no distinguen bandos."
                    };

                default:
                    return new NivelNarrativo
                    {
                        Numero = 5,
                        Titulo = "Nivel 5 — El Corazón del Núcleo",
                        Bioma = BiomaPlaneta.CorazonDelNucleo,
                        IntroDestructor = "Coordenadas finales: el Corazón del Núcleo, origen de toda la energía de Voidform. Con él " +
                                          "se encendería la Tierra durante un siglo. Sin él, Voidform moriría. AETHERNUM, el planeta " +
                                          "hecho guardián, os espera. La decisión final es vuestra.",
                        IntroCuidador = "Eres AETHERNUM, el Corazón Vivo del Planeta. Llevas dentro a Sylvaran, Nerivale, Korvhal e " +
                                        "Ignara. Puedes adoptar cualquiera de sus formas (tecla F). Los humanos llegan con todo lo que " +
                                        "aprendieron. Defiende el origen de la vida.",
                        ObjetivoDestructor = "Derrotad a Aethernum (que cambia de elemento) o extraed el Núcleo. Vuestra integridad " +
                                             "ecológica acumulada decidirá el final.",
                        ObjetivoCuidador = "Derrota a los 4 humanos en el Núcleo rotando entre los cuatro elementos.",
                        VictoriaDestructor = "El Núcleo responde. Lo que ocurra ahora depende de cómo habéis tratado al planeta.",
                        VictoriaCuidador = "Los cuatro elementos se calman. Aethernum habla por primera vez a los humanos: «Hay " +
                                           "suficiente para vivir, nunca para vaciar».",
                        DerrotaDestructor = "Aethernum os expulsa del Núcleo. Reintentad.",
                        DerrotaCuidador = "El Núcleo parpadea. Vuelve a intentarlo, Corazón del Planeta.",
                        ReflexionSocial = "Los recursos de un planeta son finitos. La sostenibilidad es aprender a vivir de lo que se " +
                                          "renueva, no de lo que se agota."
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
