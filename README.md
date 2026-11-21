# Voidform: Bio-Forge

**Shooter de arena asimétrico en Unity (C#), con todo el código y la documentación en español.**

La Tierra agotó su energía. La nave-arca *Bio-Forge* llega a Voidform, un planeta cuya fuente de energía es una **red viva** que une bosques, humedales, montañas y volcanes.

- Los **Destructores** son humanos con exotrajes y armas extractoras. Se juegan en primera persona, con un ritmo ágil de arena shooter.
- Los **Cuidadores** son guardianes elementales que nacen del propio planeta. Se juegan en tercera persona, con maná y habilidades.

Cada extracción **marchita el bioma en pantalla**. Puedes ganar vaciando el planeta o ganar dejándolo vivo, y el final cambia.

## Inicio rápido (5 minutos)

1. Crea un proyecto **3D** en Unity **2022.3 LTS** o **Unity 6** (Built-in o URP).
2. Copia la carpeta `Assets/Scripts` de este repositorio dentro de `Assets/` del proyecto.
3. **Edit → Project Settings → Player → Active Input Handling → "Both"**.
4. Pulsa **Play** en cualquier escena: el juego arranca solo. También puedes usar el menú **Voidform → 1. Crear escena jugable**.
5. Haz clic en la ventana *Game* para capturar el ratón y juega.

La guía completa de montaje, Visual Studio, pruebas, compilación y problemas frecuentes está en [`Docs/01_Montaje_Unity_y_VisualStudio.md`](Docs/01_Montaje_Unity_y_VisualStudio.md).

## Qué incluye

| Sistema | Contenido |
|---|---|
| **Bandos** | 5 trajes con resistencias cruzadas y habilidad (E). 5 armas con cargador, recarga, dispersión, retroceso y habilidad (Q). 4 guardianes elementales y Aethernum, con golpe básico y 3 habilidades. |
| **Reglas clave** | El traje humano **explota** al llegar a 0 (daño en área). Los guardianes usan **maná** en los modos versus. Hay estados alterados (raíces, ralentización, quemaduras, empujes). |
| **Modo Historia** | 5 niveles y 5 biomas, dos perspectivas narrativas y jugables. Como humano, escuadrón de 4 rellenado con bots/mods. Como guardián juegas solo y debes vencer a los humanos para avanzar. 4 desenlaces. |
| **Conquista** | 5 bases y 5:00. Dominar las 5 da la victoria inmediata. Al llegar a 0:00 gana la mayoría, con desempate por tiempo de control y por influencia. Se reaparece al morir. |
| **4vs4** | 3 rondas, modo espectador al morir, empate por muerte simultánea y empate de partida. Clasificatoria con copas, rangos y ligas, y escudo de liga. |
| **Salas** | Crear sala, Con bots, Con amigos y Sala pública, con un proveedor de red intercambiable (`IProveedorRed`). |
| **Mundo** | 5 biomas procedurales con peligros que afectan distinto a cada facción, una Fuente de Energía Viva y degradación visual según la integridad del ecosistema. |
| **Presentación** | VFX procedurales (explosión de traje en 6 capas, efectos por elemento, auras, trazadores), interfaz completa con menús, HUD y resultados, y bots con IA. |

## Documentación

| Documento | Contenido |
|---|---|
| [`01_Montaje_Unity_y_VisualStudio.md`](Docs/01_Montaje_Unity_y_VisualStudio.md) | Instalación, Visual Studio, recorrido de prueba de 18 puntos, compilación, URP, Input System, red real y problemas frecuentes |
| [`02_Historia_Narrativa_y_Problematica_Social.md`](Docs/02_Historia_Narrativa_y_Problematica_Social.md) | Historia completa, los 5 niveles, desenlaces y **justificación formal** de la problemática social |
| [`03_Diseno_de_Juego_y_Reglas.md`](Docs/03_Diseno_de_Juego_y_Reglas.md) | Todas las cifras: trajes, armas, guardianes, maná, estados, peligros, modos, casos límite, copas y salas |
| [`04_Arte_Biomas_VFX_e_Interfaz.md`](Docs/04_Arte_Biomas_VFX_e_Interfaz.md) | Biblia de arte, biomas, VFX, degradación, pantallas y HUD, y cómo sustituir el arte provisional |
| [`05_Arquitectura_Scripts_y_Eventos.md`](Docs/05_Arquitectura_Scripts_y_Eventos.md) | Mapa de los scripts, construcción de una partida, orden de ejecución, eventos y guía para ampliar el juego |

## Notas importantes

- **Arte provisional generado por código.** Personajes, biomas, VFX e interfaz se construyen con primitivas y partículas para que el juego sea jugable sin importar assets. La documentación explica cómo sustituirlos por arte final sin tocar la lógica.
- **Red simulada.** Las salas funcionan con un proveedor local que simula el servidor: todos los demás puestos los ocupan bots. Para multijugador real hay que implementar `IProveedorRed`, por ejemplo con Unity Lobby + Relay + Netcode; los pasos están en la guía de montaje.
- **AssaultCube como referencia de ritmo.** Se reinterpretaron en C# sus sensaciones de juego (movimiento ágil, ciclo de armas, dispersión, rondas). No se copió ni se tradujo su código.
- **Belerick como arquetipo.** Los guardianes toman la idea de "tanque elemental de la naturaleza" con diseño, nombres y habilidades propios.
