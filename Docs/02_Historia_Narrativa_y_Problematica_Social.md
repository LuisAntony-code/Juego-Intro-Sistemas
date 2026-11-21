# 02 · Historia, narrativa y problemática social

## Nivel 1 — Identidad del juego y premisa central

| Elemento | Definición |
|---|---|
| **Título** | Voidform: Bio-Forge |
| **Plataforma y motor** | Unity con C#, desarrollado en Visual Studio y ampliado con Claude Code |
| **Género** | Shooter de arena asimétrico (Destructores en 1.ª persona) contra guardianes elementales (Cuidadores en 3.ª persona) |
| **Premisa** | La Tierra agotó su energía. La nave-arca *Bio-Forge* viaja hasta Voidform, un exoplaneta cuya fuente de energía es una **red viva** repartida en biomas (Madera, Agua, Fuego y Metal). Los **Destructores** quieren extraerla para salvar a la humanidad. Los **Cuidadores**, guardianes elementales nativos, quieren preservarla porque sostiene toda la vida del planeta. |

### Los dos bandos

- **Destructores (humanos).** Operativos con exotrajes y armas extractoras. Su ritmo de juego, ágil y directo, se inspira en la sensación de partida de los arena shooters clásicos como AssaultCube, reinterpretada en C# con identidad propia. Cada traje y cada arma dan una habilidad.
- **Cuidadores (guardianes elementales).** No son humanos transformados: **nacen elementales**. Como arquetipo toman la contundencia y el vínculo con la naturaleza de un guardián tanque (inspiración en Belerick), pero sus siluetas, runas, nombres y habilidades son propios:
  - **Linaje del Coloso Silvestre** (Madera): Coloso Silvestre, Guardián de Corteza y Centinela de las Esporas.
  - **Linaje del Leviatán Abisal** (Agua): Leviatán Abisal, Señor de las Mareas y Oráculo de la Estática.
  - **Linaje del Titán Ígneo** (Fuego): Titán Ígneo, Demonio de Magma y Heraldo de Ceniza, más el **Gólem de Chimenea** (sub-arquetipo Tierra-Forja nacido en la Caldera).
  - **Linaje del Androide de Aleación** (Metal): Androide de Aleación, Centinela de Resonancia y Autómata de Precisión.
  - **Custodios del Nexus**: Eco Prismático, **Aethernum** (el Corazón Vivo del Planeta) y Vigía Aural, que reúnen los cuatro elementos.
  - Fichas completas en `08_Niveles_Escenarios_Fuentes_y_Guardianes.md`.

---

## Nivel 2 — Narrativa completa: del viaje espacial al clímax

### 2.1 Prólogo: el colapso y la partida

> **Año 2291.** La Tierra agotó su último yacimiento energético. Las ciudades se apagan por turnos, los océanos están cansados y los bosques son un recuerdo de museo. La sonda *Heliadna* detecta, a 41 años luz, un planeta que emite energía como si respirara: **Voidform**.
>
> La nave-arca *Bio-Forge* parte con diez mil personas y una promesa: traer energía suficiente para volver a encender la Tierra.

### 2.2 El descubrimiento

Al entrar en órbita y descender, la expedición descubre que la fuente no es un mineral inerte. Es una **red energética viva y sensible**, unida a los ecosistemas:

- **El Bosque de los Susurros Bioluminiscentes (Madera):** raíces que conducen energía como nervios hasta el Árbol Madre.
- **Los Abismos de las Mareas de Plasma (Agua):** un océano denso cuya Turbina Abisal reparte la energía en pulsos.
- **La Caldera de las Forjas Magmáticas (Fuego):** el Núcleo Térmico, regulador térmico y fuente más volátil del planeta.
- **Las Agujas de Aleación Magnética (Metal):** el Monolito Central de Resonancia, que estabiliza el campo magnético.
- **El Nexus Primordial:** el Corazón del Planeta, que unifica las cuatro corrientes.

Cada metro que se extrae se siente en todo el planeta.

### 2.3 El choque y el detonante

Los **Destructores** ven en esa energía la salvación de la humanidad y despliegan exotrajes y armas para extraerla, aunque la extracción destruya el hábitat. Los **Cuidadores**, guardianes nativos del equilibrio, se levantan para frenar la destrucción. **Nadie es malvado. Todos tienen razones.** La fuente alcanza para todos, pero solo si no se vacía.

### 2.4 Los seis niveles y sus perspectivas

Cada nivel tiene dos guiones completos: perspectiva Destructor y perspectiva Cuidador. Están escritos en `DatosNarrativaHistoria.cs` y se muestran en el *briefing* y en los resultados. La ficha visual y de combate de cada nivel está en `08_Niveles_Escenarios_Fuentes_y_Guardianes.md`.

| Nivel | Título | Escenario y fuente | Guardianes (bando humano) | Reflexión social |
|---|---|---|---|---|
| 1 | Raíces que Escuchan | Bosque de los Susurros · Árbol Madre | Coloso Silvestre, Guardián de Corteza, Centinela de las Esporas | La deforestación rompe redes vivas invisibles. |
| 2 | El Abismo que Respira | Abismos de las Mareas de Plasma · Turbina Abisal | Leviatán Abisal, Señor de las Mareas, Oráculo de la Estática | El agua conecta territorios: contaminar aguas arriba afecta a quien vive aguas abajo. |
| 3 | La Forja Despierta | Caldera de las Forjas Magmáticas · Núcleo Térmico | Titán Ígneo, Demonio de Magma, Gólem de Chimenea | Alterar los grandes reguladores naturales provoca crisis climáticas. |
| 4 | La Ciudad que Levita | Agujas de Aleación Magnética · Monolito Central de Resonancia | Androide de Aleación, Centinela de Resonancia, Autómata de Precisión | Las infraestructuras invisibles que nos protegen solo se notan cuando fallan. |
| 5 | El Santuario de la Confluencia | Nexus Primordial · Corazón del Planeta | Eco Prismático, Aethernum, Vigía Aural (rotan de elemento) | Dañar un sistema natural desequilibra a todos los demás. |
| 6 | Convergencia Total | Los 5 escenarios combinados · Corazón del Planeta | 5 guardianes, uno de cada escenario | Los recursos son finitos: hay que vivir de lo que se renueva. |

### 2.5 Reglas de participación del Modo Historia

- **Si eliges Humano (Destructor):** se juega siempre con un escuadrón de **4 humanos** (los puestos libres se rellenan con **bots/mods**) contra **3 guardianes** del escenario en los niveles 1 a 5 y contra **5 guardianes**, uno de cada escenario, en el nivel 6.
- **Si eliges Guardián (Cuidador):** **2 guardianes** (tú y un aliado de tu linaje controlado por la IA) contra 4 humanos bot. **Para avanzar al siguiente nivel, debes ganarles.** Desde el nivel 2 los humanos se adaptan: llevan el traje que resiste tu elemento.
- **Atributos:** los guardianes tienen más vida que los humanos y una velocidad un poco menor. Detalle en `07_Reglas_Historia_Atributos_y_Lobby.md`.

---

## Nivel 3 — Desenlaces y problemática social resuelta

### 3.1 Desenlaces según el bando

El final del Modo Historia depende del bando y de la **integridad ecológica media** que dejaste en los 6 niveles ganados (se guarda con `GuardadoProgreso`):

| Bando | Integridad media | Desenlace | Mensaje |
|---|---|---|---|
| Cuidador | Cualquiera | **«Equilibrio Restaurado»** | Aethernum ofrece un pacto: la humanidad toma solo la energía que el planeta regenera y ayuda a curar las cicatrices. Defender el ecosistema también era defender el futuro de quienes llegaron a destruirlo. |
| Destructor | ≥ 60% | **«Pacto de Extracción Responsable»** | El escuadrón desobedece la orden de extracción máxima. Energía renovable, nunca más de la que el planeta regenera. La Tierra se ilumina despacio, pero se ilumina. |
| Destructor | 35%–59% | **«La Victoria Amarga»** | Hay energía para una generación, pero dos biomas no se recuperarán. La humanidad ganó tiempo... y la misma pregunta que la obligó a partir. |
| Destructor | < 35% | **«El Planeta Vacío»** | El Núcleo se apaga. Voidform queda en silencio, igual que la Tierra. ¿Habrá un tercer planeta? |

**Perspectiva Destructor:** muestra la urgencia de la supervivencia humana, la tentación de extraer rápido y el coste real de vaciar una fuente viva cuando se ignora el límite ecológico.
**Perspectiva Cuidador:** muestra la defensa del ecosistema, la resistencia de cada elemento ante la explotación y la preservación de la fuente como patrimonio vivo.

### 3.2 Cómo se integra la problemática en las mecánicas

| Problemática | Mecánica que la hace visible | Script |
|---|---|---|
| Extractivismo vs. preservación | **Integridad del ecosistema** (0–100%): baja al extraer en la Fuente, al usar la *Descarga de Extracción* del Rifle Extractor, por cada base dominada por Destructores y con cada explosión de traje (contaminación). Sube con la regeneración natural, las habilidades curativas de los guardianes y las bases de los Cuidadores. | `GestorEquilibrioEcosistema.cs` |
| Impacto visible del daño | El bioma **se marchita en tiempo real**: colores apagados, vegetación gris, niebla más densa, luz más débil y una Fuente cada vez más opaca y lenta. | `GeneradorBiomas.AplicarDegradacion`, `FuenteEnergiaViva.ActualizarVisual` |
| Decisión moral | En Historia, los Destructores pueden ganar **extrayendo** (más rápido y seguro, pero destruye el ecosistema) o **derrotando al guardián** (más difícil y sostenible). El final cambia. | `GestorModoHistoria`, `DatosNarrativaHistoria.ObtenerDesenlace` |
| Trabajo en equipo | Trajes que contrarrestan elementos, curación de aliados, 4vs4 sin reaparición (cada baja pesa en el equipo) y Conquista de 5 bases, imposible de ganar en solitario. | `DatosTraje`, `ControladorGuardianElemental`, `GestorModo4vs4`, `GestorConquista5Bases` |
| Empatía de roles | Ambos bandos tienen narrativa completa y razones legítimas. Jugar los dos obliga a entender al otro. | `DatosNarrativaHistoria` |
| Aprendizaje de los demás | El **modo espectador** del 4vs4 permite observar cómo juegan los compañeros mientras se espera la siguiente ronda. | `CamaraEspectador` |

### 3.3 Justificación formal (para exposición, tesis o defensa del proyecto)

**Título del proyecto.** *Voidform: Bio-Forge* — videojuego de acción asimétrica como herramienta de reflexión sobre el extractivismo, la sostenibilidad y la cooperación.

**Planteamiento del problema.** La explotación desmedida de recursos naturales y energéticos degrada ecosistemas de los que dependen comunidades enteras. Al mismo tiempo, muchos entornos competitivos (incluidos los videojuegos) premian el individualismo y la victoria inmediata sin considerar sus consecuencias. Hacen falta experiencias interactivas que permitan *vivir* —y no solo leer— la tensión entre necesidad, explotación y límites planetarios, y que premien la coordinación.

**Objetivo general.** Diseñar e implementar en Unity (C#) un videojuego jugable que integre en sus reglas, su narrativa y su interfaz la problemática del extractivismo y la importancia del trabajo en equipo, de modo que el jugador perciba las consecuencias ecológicas de sus decisiones.

**Objetivos específicos.**
1. Representar la energía del planeta como un sistema vivo y medible (*integridad del ecosistema*) que reaccione en tiempo real a las acciones de ambos bandos.
2. Ofrecer dos perspectivas narrativas y jugables completas (Destructor y Cuidador) que permitan comprender las motivaciones de cada parte.
3. Hacer depender el desenlace de la campaña de la sostenibilidad de las decisiones del jugador, y no solo de la victoria en combate.
4. Fomentar la coordinación mediante modos de equipo (4vs4 por rondas con espectador y Conquista de 5 bases) y roles complementarios (trajes y elementos con fortalezas y debilidades cruzadas).
5. Documentar la arquitectura en español para que el proyecto pueda estudiarse, mantenerse y ampliarse con fines educativos.

**Justificación.** El videojuego es un medio de alto alcance entre jóvenes y permite el aprendizaje por experiencia: el jugador ve marchitarse un bosque *porque* eligió extraer. Las reglas convierten una idea abstracta (los límites planetarios) en una consecuencia concreta y visible. La asimetría entre bandos evita una moral simplista: los humanos tienen una necesidad real y los guardianes defienden algo real. La solución propuesta —el pacto de extracción responsable— modela una salida colaborativa en lugar de la aniquilación de una de las partes.

**Metodología de diseño.** Desarrollo incremental en tres niveles de complejidad por sistema (fundamentos → reglas y recursos → casos límite y sinergias), arquitectura desacoplada por eventos y validación mediante el recorrido de pruebas de `01_Montaje_Unity_y_VisualStudio.md`.

**Indicadores observables durante el juego.**
- Integridad final del ecosistema por nivel, que se guarda y se muestra en el selector de niveles.
- Desenlace alcanzado (cuatro finales posibles).
- Proporción de victorias por extracción frente a victorias por derrotar al guardián.
- En 4vs4: rondas ganadas, empates y evolución de copas, rango y liga.

**Conclusión.** *Voidform: Bio-Forge* no castiga la necesidad humana de energía: muestra su precio. Propone que la única victoria completa es la que deja el planeta vivo, y que esa victoria solo se consigue cooperando.
