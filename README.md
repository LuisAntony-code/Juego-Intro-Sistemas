# Voidform: Bio-Forge

**Shooter de arena asimétrico en Unity (C#), con todo el código y la documentación en español.**

La Tierra agotó su energía. La nave-arca *Bio-Forge* llega a Voidform, un planeta cuya fuente de energía es una **red viva** que une bosques, humedales, montañas y volcanes.

- Los **Destructores** son humanos de 5 arquetipos (Cazador, Paladín, Bastión, Tirador y Ciborg) con 5 armas modulares combinables. Se juegan en primera persona, con un ritmo ágil de arena shooter.
- Los **Cuidadores** son guardianes elementales que nacen del propio planeta. Se juegan en tercera persona, con maná y habilidades.

Cada extracción **marchita el bioma en pantalla**. Puedes ganar vaciando el planeta o ganar dejándolo vivo, y el final cambia.

## Inicio rápido (5 minutos)

1. Crea un proyecto **3D** en Unity **2022.3 LTS** o **Unity 6** (Built-in o URP).
2. Copia las carpetas `Assets/Scripts` y `Assets/Resources` (modelos 3D y texturas) de este repositorio dentro de `Assets/` del proyecto.
3. **Edit → Project Settings → Player → Active Input Handling → "Both"**.
4. Pulsa **Play** en cualquier escena: el juego arranca solo. También puedes usar el menú **Voidform → 1. Crear escena jugable**.
5. Haz clic en la ventana *Game* para capturar el ratón y juega.

La guía completa de montaje, Visual Studio, pruebas, compilación y problemas frecuentes está en [`Docs/01_Montaje_Unity_y_VisualStudio.md`](Docs/01_Montaje_Unity_y_VisualStudio.md).

## Qué incluye

| Sistema | Contenido |
|---|---|
| **Bandos** | 5 arquetipos humanos con resistencias y 3 habilidades de armadura (Z X C). 5 armas modulares (arco, mandoble, maza y escudo, rifle largo, arsenal biomecánico) con combos, bloqueo, balística y 3 habilidades (E Q F). Puntos débiles con daño crítico, aguante y tambaleo, y habilidades de guardián telegrafiadas que se pueden interrumpir. 5 linajes de guardianes (Madera, Agua, Fuego, Metal y Tierra-Forja) × 3 variantes (corta, media y larga distancia) más los Custodios del Nexus. Los guardianes tienen más vida que los humanos y son un poco más lentos. |
| **Reglas clave** | El traje humano **explota** al llegar a 0 (daño en área). Los guardianes usan **maná** en los modos versus. Hay estados alterados (raíces, ralentización, quemaduras, empujes). |
| **Modo Historia** | 6 niveles y 6 escenarios, cada uno con su fuente de energía, y dos perspectivas narrativas y jugables. Como humano, escuadrón de 4 (rellenado con bots/mods) contra 3 guardianes, y 5 en el nivel final. Como guardián, tú y un aliado contra 4 humanos: debes vencerlos para avanzar. 4 desenlaces. |
| **Conquista** | 5 bases y 5:00. Dominar las 5 da la victoria inmediata. Al llegar a 0:00 gana la mayoría, con desempate por tiempo de control y por influencia. Se reaparece al morir. |
| **4vs4** | 3 rondas, modo espectador al morir, empate por muerte simultánea y empate de partida. Clasificatoria con copas, rangos y ligas, y escudo de liga. |
| **Salas** | Crear sala, Con bots, Con amigos y Sala pública, con un proveedor de red intercambiable (`IProveedorRed`). |
| **Mundo** | 6 escenarios procedurales (Bosque de los Susurros, Abismos de las Mareas, Caldera de las Forjas, Agujas de Aleación, Nexus Primordial y Convergencia Total) con peligros por facción, fuentes de energía con forma propia, gravedad reducida y auroras en el Nexus, y degradación visual según la integridad del ecosistema. |
| **Presentación** | 17 modelos 3D generados en Blender (5 humanos, 5 guardianes, 7 armas) con 19 texturas PBR, animación procedural (locomoción, combos en tres tiempos, tambaleo), VFX procedurales, interfaz completa con menús, HUD y resultados, y bots con IA. |

## Documentación

| Documento | Contenido |
|---|---|
| [`01_Montaje_Unity_y_VisualStudio.md`](Docs/01_Montaje_Unity_y_VisualStudio.md) | Instalación, Visual Studio, recorrido de prueba de 18 puntos, compilación, URP, Input System, red real y problemas frecuentes |
| [`02_Historia_Narrativa_y_Problematica_Social.md`](Docs/02_Historia_Narrativa_y_Problematica_Social.md) | Historia completa, los 6 niveles, desenlaces y **justificación formal** de la problemática social |
| [`03_Diseno_de_Juego_y_Reglas.md`](Docs/03_Diseno_de_Juego_y_Reglas.md) | Todas las cifras: trajes, armas, guardianes, maná, estados, peligros, modos, casos límite, copas y salas |
| [`04_Arte_Biomas_VFX_e_Interfaz.md`](Docs/04_Arte_Biomas_VFX_e_Interfaz.md) | Biblia de arte, biomas, VFX, degradación, pantallas y HUD, y cómo sustituir el arte provisional |
| [`05_Arquitectura_Scripts_y_Eventos.md`](Docs/05_Arquitectura_Scripts_y_Eventos.md) | Mapa de los scripts, construcción de una partida, orden de ejecución, eventos y guía para ampliar el juego |
| [`06_Direccion_Realista_Materiales_y_Animacion.md`](Docs/06_Direccion_Realista_Materiales_y_Animacion.md) | Paso de bloques a arte realista (madera, plasma, magma, metal, transiciones del Nexus) y animación fluida (locomoción, IK, ataques en 5 fases, habilidades) |
| [`07_Reglas_Historia_Atributos_y_Lobby.md`](Docs/07_Reglas_Historia_Atributos_y_Lobby.md) | Reglas de enfrentamiento (3 o 5 contra 4 y 2 contra 4), tácticas de la IA, atributos guardián frente a humano y lobby |
| [`08_Niveles_Escenarios_Fuentes_y_Guardianes.md`](Docs/08_Niveles_Escenarios_Fuentes_y_Guardianes.md) | Los 6 niveles: escenario, fuente de energía y cada guardián con su diseño, animaciones por rango, punto débil, estabilidad, sobrecarga y sinergia; Gólem de Chimenea |
| [`09_Arquetipos_Humanos_y_Armas_Modulares.md`](Docs/09_Arquetipos_Humanos_y_Armas_Modulares.md) | Los 5 arquetipos humanos y las 5 armas modulares con sus habilidades (implementados) |
| [`10_Modelos_Blender_y_Animacion_Procedural.md`](Docs/10_Modelos_Blender_y_Animacion_Procedural.md) | Cómo se generan los modelos 3D y las texturas (Blender y Python), cómo llegan a Unity y cómo se animan |

## Notas importantes

- **Arte: prototipo avanzado, no arte final.** Personajes y armas usan modelos 3D generados por código en Blender, con texturas PBR y animación procedural (Doc 10). Los biomas, los VFX y la interfaz siguen construyéndose con primitivas y partículas. Para calidad comercial hace falta un artista 3D (esculpido, rostros, UV, rig con animaciones capturadas), escenarios modelados, audio y pruebas con jugadores; los Docs 06, 08 y 10 explican cómo integrar ese arte sin tocar la lógica.
- **Red simulada.** Las salas funcionan con un proveedor local que simula el servidor: todos los demás puestos los ocupan bots. Para multijugador real hay que implementar `IProveedorRed`, por ejemplo con Unity Lobby + Relay + Netcode; los pasos están en la guía de montaje.
- **AssaultCube como referencia de ritmo.** Se reinterpretaron en C# sus sensaciones de juego (movimiento ágil, ciclo de armas, dispersión, rondas). No se copió ni se tradujo su código.
- **Sin audio todavía.** El juego no tiene música ni efectos de sonido.
- **Pruebas.** El código C# se ha verificado compilándolo contra los módulos de Unity fuera del editor; los modelos, contra un reimportado del FBX en Blender. Conviene hacer el recorrido de prueba del Doc 01 dentro de Unity antes de cualquier publicación.
- **Belerick como arquetipo.** Los guardianes toman la idea de "tanque elemental de la naturaleza" con diseño, nombres y habilidades propios.

---

## BugBlaster: Operación Compilar (nuevo)

Juego 2D de acción para **aprender Python** (correr, saltar, disparar y programar al robot). Es independiente de Voidform y vive en [`BugBlaster/`](BugBlaster/README.md): se abre con `BugBlaster/index.html`, sin instalar nada. El informe técnico con capturas está en [`BugBlaster/docs/Informe_BugBlaster.docx`](BugBlaster/docs/Informe_BugBlaster.docx).
