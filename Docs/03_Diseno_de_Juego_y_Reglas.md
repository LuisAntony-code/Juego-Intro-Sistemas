# 03 · Diseño de juego y reglas completas

Todas las cifras de este documento salen del código. Si cambias un valor en el Inspector o en los scripts, actualiza también esta tabla.

---

## Nivel 1 — Fundamentos

### 1.1 Movimiento (ambos bandos) · `ControladorMovimientoAssaultCube.cs`

| Parámetro | Valor | Efecto |
|---|---|---|
| Velocidad base | 10,5 m/s | Ritmo ágil de arena shooter. Se multiplica por el arquetipo o el guardián, por las habilidades y por los estados alterados. |
| Salto | 7,8 m/s | Salto responsivo. |
| Gravedad | −21 m/s² | Caída rápida y controlable. |
| Aceleración / frenado en suelo | 32 / 24 m/s² | Respuesta inmediata, sin inercia pesada. |
| Control en el aire | 70% | Permite corregir el salto sin "volar". |
| Tiempo de coyote | 0,10 s | Se puede saltar un instante después de dejar el borde. |
| Búfer de salto | 0,12 s | Si pulsas un poco antes de aterrizar, el salto se ejecuta al tocar suelo. |
| Ángulo vertical | −85° a 85° | Rotación limpia de cámara. |
| Caída fuera del mapa | y < −40 | Muerte inmediata ("Caída al vacío"). |

**Perspectivas jugables:** Destructor en **1.ª persona** (FOV 80, arma visible). Guardián en **3.ª persona** sobre el hombro (FOV 70).

### 1.2 Salud · `SistemaSaludYTraje.cs`

- **Vida y blindaje.** Mientras queda blindaje, absorbe el **60%** de cada golpe.
- **Resistencias.** El daño final es `daño × multiplicador del arquetipo (o guardián) según el tipo × multiplicador de jefe` (× 1,5 si acierta en un punto débil).
- **Fuego amigo:** desactivado por defecto (`FuegoAmigoActivo = false`).

---

## Nivel 2 — Facciones, recursos y reglas

### 2.1 Arquetipos de los Destructores (armadura: habilidades Z · X · C) · `DatosArquetipo.cs`

El humano elige **un arquetipo** (su armadura) y **un arma modular** (2.2). Cualquier combinación es válida, así que cada operativo lleva **6 habilidades**. La especificación completa (aspecto, animaciones y matices) está en el **Doc 09**; aquí van las cifras que usa el código.

| Arquetipo | Vida / Blindaje | Velocidad | Aguante | Resiste | Débil a | Z (corta) | X (media) | C (larga) |
|---|---|---|---|---|---|---|---|---|
| Cazador / Explorador Forestal | 100 / 40 | ×1,10 | 40 | Árbol −30%, estados −25% de duración | Fuego +15% | **Paso de maleza** (7 s) | **Camuflaje de fibras** (16 s) | **Estabilizador de mira** (14 s) |
| Paladín / Templario del Alba | 115 / 70 | ×0,98 | 80 | Fuego −25% | Agua +10% | **Aura de repulsión** (10 s) | **Égida refractaria** (15 s) | **Faro de vanguardia** (22 s) |
| Caballero Bastión / Guardia de Hierro | 125 / 90 | ×0,95 | 120 | Tierra −30%, Metal −30%, Explosivo −20% | Árbol +15% | **Anclaje sísmico** (12 s) | **Absorción de impacto** (16 s) | **Intercepción de artillería** (20 s) |
| Tirador / Cazador de Reliquias | 100 / 45 | ×1,03 | 60 | Agua −20%, inmune a la ralentización del agua | Metal +10% | **Propulsión de retroceso** (8 s) | **Disipador térmico** (15 s) | **Visor telemétrico** (18 s) |
| Ciborg / Vanguardia Biomecánica | 110 / 60 | ×1,00 | 80 | Metal −25% | Agua +20% | **Sobrecarga del núcleo** (14 s) | **Escudo de frecuencia** (16 s) | **Enlace de rastreo** (20 s) |

**Regla de atributos:** todo humano tiene **menos vida** que cualquier guardián (100–125 frente a 140–220) y es **un poco más rápido** (×0,95–1,10 frente a ×0,87–0,92).
**Contramedidas de la IA en Historia:** desde el nivel 2 los bots humanos llevan el arquetipo que resiste el elemento del nivel: Árbol → Cazador, Agua → Tirador, Fuego → Paladín, Metal → Ciborg, Tierra-Forja → Bastión.

**Aguante (estabilidad).** Cada golpe resta aguante además de vida. Si el aguante llega a 0, el personaje se **tambalea 0,8 s** (no puede actuar y pierde lo que estuviera cargando). Se regenera a 25 por segundo tras 1 s sin recibir golpes.

### 2.2 Armas modulares (clic izquierdo, clic derecho y habilidades E · Q · F) · `DatosArmaModular.cs`

| Arma | Ataque básico | Clic derecho | Munición | E (corta) | Q (media) | F (larga) |
|---|---|---|---|---|---|---|
| Arco Compuesto | Flecha balística de 45 (cae con la distancia de vuelo; tensar 0,8 s = daño máximo) | Tensar y apuntar (zoom ×1,6) | 20 / 40, sin recarga | **Disparo a quemarropa y retroceso** (8 s) | **Flecha de amarre** (12 s) | **Tiro de núcleo** (18 s) |
| Mandoble / Espada Larga | Combo de 3 tajos de 30 (el 3.º ×1,35), 2,8 m | Bloqueo frontal del 60% | — | **Barrido ascendente** (7 s) | **Onda de filo** (11 s) | **Estocada de impulso** (16 s) |
| Maza con Púas y Escudo | Combo de 2 mazazos de 34, 2,4 m, gran daño de aguante | Bloqueo frontal del 75% | — | **Golpe de yunque** (8 s) | **Lanzamiento con cadena** (12 s) | **Postura de bastión** (20 s) |
| Rifle Largo con Mira | Disparo de 60 (rayo, 220 m) | Mira ×4 (muy impreciso en movimiento) | 6 / 36, recarga 2,2 s | **Disparo de dispersión** (7 s) | **Tiro a articulación** (12 s) | **Calibre perforante** (18 s) |
| Arsenal Biomecánico Acoplado | Combo de 4 cuchilladas de 22 (0,26 s) | Cañón de pulso del antebrazo | Cañón: 24, recarga 1,2 s | **Cuchillas de antebrazo** (6 s) | **Cable arpón** (10 s) | **Pulso de riel** (18 s) |

**Reglas del combate cuerpo a cuerpo:** cada golpe tiene **anticipación, impacto y seguimiento**; el daño se aplica en el *momento de impacto* del arma (no al pulsar), en un cono frente al atacante. El último golpe del combo hace ×1,35.
**Puntos débiles:** los impactos cerca del núcleo luminoso de un guardián hacen **daño crítico ×1,5** (marcador amarillo en el HUD); algunos núcleos se exponen más tras ciertas acciones (gema-nexo tras rotar de elemento).

### 2.3 Regla de daño y explosión del traje humano

1. El traje recibe daño (blindaje primero, vida después).
2. Por debajo del **25%** de vida se activa la **alerta de traje crítico** en el HUD.
3. Al llegar a **0**, el traje **explota**:
   - Radio de **5,5 m**. Daño explosivo de **45** (100% en el centro y 40% en el borde) a los **enemigos**.
   - Empuje de 9 m/s hacia fuera y un poco hacia arriba.
   - Contamina el ecosistema (**−2,5** de integridad).
   - Efecto visual en capas, sacudida de cámara y aviso en el registro de combate.
4. El humano muere. Según el modo, **reaparece** (Conquista), **pasa a espectador** (4vs4 y Historia) o pierde el nivel (Historia, si cae todo el escuadrón).

### 2.4 Guardianes elementales · `DatosGuardianElemental.cs`, `ControladorGuardianElemental.cs`

Hay **5 linajes** (Madera, Agua, Fuego, Metal y el sub-arquetipo Tierra-Forja) con **3 variantes** cada uno (vanguardia de corta distancia, control de media y artillería de larga), más los **Custodios del Nexus**, que rotan entre los 4 elementos primarios. Todos tienen **más vida que cualquier humano y un poco menos de velocidad**. La tabla completa está en `07_Reglas_Historia_Atributos_y_Lobby.md` y las fichas, en `08_Niveles_Escenarios_Fuentes_y_Guardianes.md`.

| Linaje | Vida base | Blindaje base | Velocidad | Daño base | Golpe básico (clic, sin maná, 0,65 s) |
|---|---|---|---|---|---|
| Coloso Silvestre (Madera) | 190 | 70 | ×0,88 | 28 | Puño de Corteza: 16 de daño, 3,2 m |
| Leviatán Abisal (Agua) | 160 | 45 | ×0,92 | 26 | Latigazo de Corriente: 12 de daño, 5,5 m |
| Titán Ígneo (Fuego) | 165 | 40 | ×0,91 | 40 | Zarpa Ígnea: 14 de daño + Quemadura de 4/s durante 1,5 s |
| Androide de Aleación (Metal) | 175 | 75 | ×0,90 | 32 | Filo de Aleación: 17 de daño, 3,4 m |
| Gólem de Chimenea (Tierra-Forja) | 200 | 90 | ×0,87 | 34 | Puño de Yunque: 20 de daño, 3,0 m |
| Custodios del Nexus | Los del elemento activo (Aethernum ×1,1) | — | — | — | Rotan con la tecla F |

**Modificadores por rol:** vanguardia (vida ×1,1, blindaje ×1,2, radio ×0,9, golpe ×1,15) · control (radio ×1,1) · artillería (vida ×0,88, radio ×1,35, alcance del golpe +3 m).

**Habilidades** (radio de efecto base = 6,5 m, modificado por el rol):

| Elemento | E · Habilidad 1 | R · Habilidad 2 | Q · Definitiva (radio ×1,5, daño ×1,8) |
|---|---|---|---|
| Madera | **Raíces Atrapantes**: daño en área + Enraizado 1,8 s | **Savia Regeneradora**: +32 de vida propia, +20 a aliados en 12 m, +2 de ecosistema | **Espinar del Bosque Primordial**: Enraizado 2,5 s, +40 de vida, +3 de ecosistema |
| Agua | **Pulso Fluvial Restaurador**: +28 de vida, daño ×0,6 en radio ×0,7, +1,5 de ecosistema | **Corriente de Marea**: daño, empuje de 14 y Ralentizado al 60% durante 2,5 s | **Diluvio de Plasma**: Ralentizado al 40% durante 3,5 s, empuje, +40 de vida, +3 de ecosistema |
| Fuego | **Ráfaga Ígnea Abrasadora**: cono de 70°, daño ×1,3 + Quemadura de 8/s durante 3 s | **Combustión de Área**: daño ×1,4 + Quemadura de 10/s durante 3 s | **Erupción del Núcleo Térmico**: Quemadura de 12/s durante 4 s + empuje |
| Metal | **Pulso Magnético**: daño, **atracción** de 9 m/s hacia el guardián y Ralentizado al 75% durante 1,5 s | **Égida de Aleación**: +35 de blindaje temporal y descarga (daño ×0,7 en radio ×0,6) | **Tormenta de Agujas**: bloqueo magnético (Enraizado 1,2 s) y Ralentizado al 60% durante 2,5 s |
| Tierra-Forja | **Golpe de Escoria**: daño ×1,15, Ralentizado al 70% durante 1,5 s, elevación | **Muralla de Escoria**: muro de roca de 6 s, +25 de blindaje, +22 de vida, daño | **Erupción de Chimenea**: lanza a los enemigos y los Ralentiza al 50% durante 2 s |

### 2.5 Regla del maná (solo en modos versus)

| Parámetro | Valor |
|---|---|
| Maná máximo | 100 |
| Regeneración | 12/s (×1,25 con afinidad de bioma) |
| Costes | E = 25 · R = 35 · Q = 65 · Clic = 0 |
| Enfriamientos | Clic 0,65 s · E 4 s · R 7 s · Q 16 s |

- **Sin maná suficiente, la habilidad no se lanza**, **no entra en enfriamiento** y el HUD avisa: "Maná insuficiente para X: necesita N (tienes M)".
- **En Historia no hay maná**: el guardián solo depende de los enfriamientos, porque la regla del maná es exclusiva de los modos versus.
- **Afinidad de bioma:** el guardián cuyo elemento coincide con el bioma (cualquiera en el Nexus y la Convergencia, y el Gólem de Chimenea en la Caldera) hace +15% de daño y regenera +25% de maná.

### 2.6 Estados alterados · `SistemaEstadosAlterados.cs`

| Estado | Efecto | Lo provoca | Contramedida |
|---|---|---|---|
| Ralentizado | Multiplica la velocidad (se aplica el más fuerte) | Agua, Tierra, peligros de bioma, Tiro a articulación | Paso de maleza (Cazador) y Anclaje sísmico (Bastión) dan inmunidad |
| Enraizado | No puedes desplazarte (sí apuntar y atacar) | Árbol | Paso de maleza y Anclaje sísmico (inmunidad); Aura de repulsión (lo quita) |
| Quemadura | Daño por segundo en pulsos de 0,25 s | Fuego | Aura de repulsión del Paladín (la quita) |
| Empujado | Impulso externo que se va frenando | Agua, Tierra, explosiones, Disparo de dispersión | Anclaje sísmico del Bastión (inmune 4 s) |

Los estados **no se acumulan sin límite**: se conserva la duración más larga y la intensidad más fuerte. El Cazador reduce un 25% su duración y el Escudo de frecuencia del Ciborg hace inmunes al control a los aliados. Al morir se limpian todos.

### 2.7 Peligros de cada bioma (interacción entre facciones y bioma) · `ZonaAmbientalBioma.cs`

| Bioma | Peligro | Destructores | Cuidadores |
|---|---|---|---|
| Bosque de los Susurros | Claros de esporas | Ralentizados al 75% | +4 de vida/s |
| Abismos de las Mareas | Corrientes de plasma | Ralentizados al 60% (excepto el Tirador) | +3 de vida/s |
| Caldera de las Forjas | Ríos de magma | 12 de daño/s de Fuego (el Paladín lo resiste un 25%) | +3 de vida/s |
| Agujas de Aleación | Campos de inducción | 5 de daño/s de Metal (Bastión −30%, Ciborg −25%) y Ralentizados al 80% | +2 de vida/s |
| Nexus Primordial (gravedad ×0,6) y Convergencia Total (gravedad ×0,8) | Los cuatro anteriores, uno por cuadrante | Según el cuadrante | Según el cuadrante |

---

## Nivel 3 — Modos de juego, casos límite y clasificación

### 3.1 Modo Historia · `GestorModoHistoria.cs`

- **6 niveles:** Bosque de los Susurros, Abismos de las Mareas, Caldera de las Forjas, Agujas de Aleación, Nexus Primordial y Convergencia Total.
- **Composición:**
  - *Destructor:* 4 humanos (tú + amigos reales + bots/mods hasta completar 4) contra **3 guardianes** del escenario (vanguardia, control y artillería) en los niveles 1 a 5 y contra **5 guardianes** (uno de cada escenario) en el nivel 6. Los guardianes rivales escalan: vida ×(1 + 0,06·(n−1)), daño ×(1 + 0,05·(n−1)) y blindaje ×1,5.
  - *Cuidador:* **2 guardianes** (tú + un aliado IA de tu linaje) contra 4 humanos bot con precisión de 0,35 + 0,1·(n−1). Desde el nivel 2, los bots llevan el **arquetipo que contrarresta** el elemento del escenario.
  - Tabla nivel a nivel en `07_Reglas_Historia_Atributos_y_Lobby.md`.
- **Coordinación de la IA guardiana por roles:** la vanguardia caza abriéndose a un flanco, el control se interpone entre el enemigo y la fuente, y la artillería mantiene la distancia moviéndose en arco.
- **Fuente de energía** (Árbol Madre, Turbina Abisal, Núcleo Térmico, Monolito de Resonancia o Corazón del Planeta): los humanos vivos en un radio de 7 m extraen **0,6 de energía por humano y por segundo**, siempre que no haya un guardián dentro. Cada punto de energía extraído cuesta **0,6 de integridad**.
- **Victoria y derrota:**
  - Cuidador gana = los 4 humanos eliminados. Cuidador pierde = caen todos los guardianes o los humanos extraen el 100%.
  - Destructor gana = todos los guardianes derrotados **o** extracción al 100%. Destructor pierde = cae todo el escuadrón.
  - En Historia **nadie reaparece**. Quien cae espectea a su equipo (también un guardián, que mira a su aliado).
- **Caso límite:** si los últimos de cada bando caen en una ventana de **0,25 s**, el nivel queda **empatado** y se repite.
- **Progreso:** ganar desbloquea el siguiente nivel y guarda la integridad ecológica. Al ganar el nivel 6 se muestra el desenlace. Abandonar cancela el nivel sin registrar resultado.

### 3.2 Modo Conquista (5 bases / 5 minutos) · `GestorConquista5Bases.cs`, `ZonaCapturaBase.cs`

**Captura de una base** (radio 8 m, progreso de −100 = Destructores a +100 = Cuidadores):

| Situación dentro de la base | Qué ocurre |
|---|---|
| Solo un bando | El progreso avanza 25/s hacia su lado, ×(1 + 0,35 por cada jugador extra), con un máximo de ×2 |
| Ambos bandos | Progreso congelado: **en disputa** |
| Nadie y la base sin dueño | El progreso vuelve a 0 a 6/s |
| Se llega a ±100 | La base pasa a estar **dominada** por ese bando |
| Base dominada | Solo se pierde cuando el rival la **neutraliza** (el progreso cruza el 0) |

**Reaparición:** 3,5 s después de morir (también si el traje explotó), en un punto aleatorio de su bando, con **1,5 s de protección**.

**Victoria, en este orden exacto:**
1. **Victoria inmediata:** un bando domina las **5 bases a la vez** → gana sin esperar al reloj.
2. **0:00 por mayoría:** gana el bando con más bases dominadas.
3. **Desempate 1 — tiempo de control:** gana quien acumuló más segundos de base dominada durante la partida (suma de todas las bases).
4. **Desempate 2 — influencia:** gana quien tenga más progreso parcial a su favor en las bases no dominadas.
5. **Empate total** si todo coincide.

**Casos límite:** si en el último fotograma un bando completa las 5 bases, gana por dominación (se comprueba antes que la mayoría). Al terminar se bloquea el daño global y se cancelan las reapariciones pendientes. Conquista **no cambia las copas**.

### 3.3 Modo 4vs4 (eliminación a 3 rondas) · `GestorModo4vs4.cs`

- **2 equipos de 4.** La ronda termina cuando un equipo queda completamente eliminado.
- **Muerte y espectador:** quien muere no revive hasta la siguiente ronda. Mientras tanto **espectea** a sus compañeros vivos (clic o Tab para cambiar de compañero).
- **Muerte simultánea:** cuando cae el último de un equipo, se abre una ventana de **0,25 s** con el daño todavía activo. Si en ella cae también el último rival, la ronda es **EMPATE**.
- **Tiempo límite de ronda (180 s):** gana quien tenga más supervivientes. Si hay igualdad, quien tenga más % de vida sumada. Si sigue igual, la ronda es empate.
- **Entre rondas:** pausa de 4 s con el daño bloqueado. Después todos reaparecen en sus puntos.
- **Final:** gana quien tenga más rondas ganadas. Con igualdad (1-1 y un empate, 0-0 y tres empates...) la partida es **EMPATE**. Con 2-0 la serie termina antes, porque el tercer resultado ya no cambia nada.

### 3.4 Clasificación: copas, rango y liga · `SistemaClasificacionYCopas.cs`

| Liga | Copas | Divisiones |
|---|---|---|
| Bronce | 0–99 | I (0–24), II (25–49), III (50–74), IV (75–99) |
| Plata | 100–199 | I–IV |
| Oro | 200–299 | I–IV |
| Platino | 300–399 | I–IV |
| Diamante | 400–499 | I–IV |
| Núcleo Supremo | 500+ | Sin divisiones: se muestran las copas |

**Cálculo** (índice de liga: Bronce 0 ... Núcleo Supremo 5):
- **Victoria:** `max(20, 30 − 2·índice) + min(15, 5·racha_previa)`. La racha aumenta en 1.
- **Derrota:** `−(15 + 2·índice)`. La racha vuelve a 0. Nunca se baja de 0 copas.
- **Empate:** 0. La racha se conserva.
- **Sala "Con bots":** se aplica el 50% de la variación, redondeado hacia arriba en valor absoluto.
- **Escudo de liga:** al ascender se obtiene. La primera derrota que te haría descender te deja en el mínimo de tu liga y consume el escudo.

**Ejemplo:** tienes 95 copas (Bronce IV) y llevas 2 victorias de racha. Ganas: +30 +10 = **+40 → 135 copas, Plata II, ¡ascenso!** y escudo activo. Después pierdes: −(15 + 2) = −17 → 118 (sigues en Plata). Vuelves a perder: −17 → 101. Pierdes otra vez: −17 → 84, que sería Bronce, pero el escudo te deja en **100** y se consume.

### 3.5 Salas y modalidades · `GestorSalasYEmparejamiento.cs`

| Tipo | Comportamiento | Permitido en |
|---|---|---|
| **Crear sala** | Elegir privada o pública, bioma y dificultad. Genera el código `VBF-####`. Estado "Esperando jugadores". | Conquista, 4vs4, Historia (Destructor) |
| **Con bots** | Inicio inmediato. Todos los demás puestos son bots. | Conquista, 4vs4, Historia (Destructor) |
| **Con amigos** | Sin código: crea una sala privada para invitar. Con código: se une a la sala de un amigo. | Conquista, 4vs4, Historia (Destructor) |
| **Sala pública** | Entra en una sala pública aleatoria del modo. | Conquista y 4vs4 |
| — | **Historia como Guardián:** individual, sin sala (solo el jugador es Guardián). | — |

**Estados de una sala:** Sin sala → Creando o Buscando → Esperando jugadores → Lista → En partida → Cerrada. Los huecos siempre se rellenan con bots. Con el proveedor local simulado, los amigos y los jugadores públicos **también son bots** (ver la sección "Red real" en `01_...md`).

### 3.6 Equivalencias con el ritmo de AssaultCube

El encargo pedía tomar la base y el ritmo de partida de AssaultCube (shooter libre en C++ sobre el motor Cube) como punto de partida y cambiar su lenguaje a C# en Unity. **No se ha copiado ni traducido su código línea a línea:** se reinterpretaron sus *sensaciones de juego* con una arquitectura propia en español, que es justo lo que indicaba el propio encargo ("sin plagio ni copia literal").

| Rasgo de un arena shooter clásico | Reinterpretación en Voidform |
|---|---|
| Movimiento rápido, frenado seco y control aéreo | `ControladorMovimientoAssaultCube`: aceleración de 32, frenado de 24, control aéreo del 70%, tiempo de coyote y búfer de salto |
| Arsenal con identidad propia por arma | `DatosArmaModular`: 5 armas modulares (arco, mandoble, maza y escudo, rifle largo, arsenal biomecánico) con 3 habilidades cada una |
| Dispersión que crece al moverse o saltar y retroceso vertical | `SistemaCombateArquetipo`: dispersión por movimiento y en el aire, retroceso del rifle y del cañón de pulso |
| Blindaje que absorbe parte del daño | `SistemaSaludYTraje`: el blindaje absorbe el 60% |
| Modos por equipos, rondas y espectador | `GestorModo4vs4` (3 rondas, espectador) y `GestorConquista5Bases` |
| Interfaz espartana | Sustituida por un HUD completo: barras, ranuras de habilidad, 5 bases, ecosistema, registro de combate y banners |
