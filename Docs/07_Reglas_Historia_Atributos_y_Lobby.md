# 07 · Reglas de enfrentamiento del Modo Historia, atributos base y lobby

> **Estado:** todo lo de este documento **está implementado en el código**. Composición: `GestorModoHistoria.GuardianesDefensores` y `ParejaDelJugadorGuardian`. Atributos: `DatosGuardianElemental` y `DatosArquetipo`. Coordinación: `ControladorBotIA.DestinoGuardianPorRol`. Lobby: `GestorInterfazJuego` y `DatosArchivoMundo`.

---

## 1.3 Reglas de enfrentamiento y estadísticas base

### 1.3.1 Modo Historia, bando humano: 4 humanos contra 3 guardianes (5 en el nivel 6)

**Composición exacta por nivel**

| Nivel | Escenario | Guardianes rivales (rol) |
|---|---|---|
| 1 | Bosque de los Susurros Bioluminiscentes | Coloso Silvestre (vanguardia) · Guardián de Corteza (control) · Centinela de las Esporas (artillería) |
| 2 | Abismos de las Mareas de Plasma | Leviatán Abisal (vanguardia) · Señor de las Mareas (control) · Oráculo de la Estática (artillería) |
| 3 | Caldera de las Forjas Magmáticas | Titán Ígneo (vanguardia) · Demonio de Magma (control) · Gólem de Chimenea · Mortero (artillería, sub-arquetipo Tierra-Forja nacido en la Caldera) |
| 4 | Agujas de Aleación Magnética | Androide de Aleación (vanguardia) · Centinela de Resonancia (control) · Autómata de Precisión (artillería) |
| 5 | Nexus Primordial | Eco Prismático (vanguardia) · Aethernum (control) · Vigía Aural (artillería). Los tres rotan entre los 4 elementos. |
| 6 | Convergencia Total | Coloso Silvestre · Leviatán Abisal · Titán Ígneo · Androide de Aleación · Aethernum (uno de cada escenario anterior) |

**Paso 1 — Distribución inicial.**
- Los guardianes aparecen en **abanico** alrededor de la fuente del escenario, del lado norte: 11 m de radio, repartidos entre −60° y +60° (`GestorFlujoPartida.PosicionDefensiva`).
- El escuadrón humano aparece en la zona de aterrizaje del sur, a unos 45 m.
- Con 3 guardianes, el reparto queda: vanguardia a la izquierda (−60°), control en el centro (0°) y artillería a la derecha (+60°).
- Con 5 guardianes, los cinco se reparten cada 30°.

**Paso 2 — Desplazamiento.**
- La **vanguardia** recibe el objetivo *Cazar enemigos* y sale a buscar al humano más cercano.
- El **control** y la **artillería** reciben *Defender la fuente*: no se alejan de ella salvo para atacar a quien entra en su radio de extracción (7 m) y dan prioridad a quien esté extrayendo (la puntuación de amenaza resta 25 si el humano está a menos de 9 m de la fuente).

**Paso 3 — Cobertura de flancos.**
- Al entrar en combate, la vanguardia **no se planta delante del enemigo**: se abre 1,5 m hacia un flanco (izquierdo o derecho según su "fase" de IA). Así no tapona la línea de ataque de sus aliados y obliga al escuadrón a girarse.
- El control se coloca **entre el enemigo y la fuente**, a la distancia de su radio de habilidad × 0,7. Funciona como portero.
- La artillería mantiene el radio × 0,85 y se mueve en **arco** (±40° alrededor del enemigo, con un periodo de unos 20 s) para buscar un ángulo distinto al de sus aliados.

**Paso 4 — Ataque.**
- Golpe básico cuando el enemigo está a menos del alcance básico + 1,2 m y en un cono de 50°.
- Habilidad 1 cuando hay al menos 1 enemigo dentro del 85% del radio.
- Definitiva cuando hay **2 o más** enemigos dentro del 140% del radio.
- Habilidad 2: curación (Madera) si la vida baja del 70%, o daño y control (resto) si hay un enemigo cerca.
- La frecuencia de decisión depende de la dificultad: cada 1,2 s en fácil y cada 0,45 s en experto.

**Paso 5 — Coordinación.**
- Como la vanguardia presiona por un flanco, el control defiende el centro y la artillería ataca desde un tercer ángulo, el escuadrón humano **no puede concentrar el fuego** sin exponer la espalda.
- Los Custodios del Nexus (nivel 5) y Aethernum (nivel 6) **rotan al elemento que más daño hace** a los trajes del escuadrón (evalúan las resistencias de los trajes vivos cada 10 a 16 s).
- En el nivel 6, cuatro vanguardias presionan desde cuatro direcciones mientras Aethernum defiende el Corazón.

**Escalado por nivel de los guardianes rivales:** vida × (1 + 0,06·(n−1)), daño × (1 + 0,05·(n−1)) y blindaje × 1,5 por ser defensores del escenario.

**Victoria y derrota:** el escuadrón gana si derrota a **todos** los guardianes o extrae el 100% de la fuente. Pierde si caen sus 4 humanos. En Historia **nadie reaparece**: quien cae espectea a sus compañeros.

### 1.3.2 Modo Historia, bando guardián: 2 guardianes contra 4 humanos

**Pareja por nivel** (el primero es el jugador y el segundo, su aliado IA):

| Nivel | Jugador (vanguardia) | Aliado IA |
|---|---|---|
| 1 | Coloso Silvestre | Guardián de Corteza (control) |
| 2 | Leviatán Abisal | Señor de las Mareas (control) |
| 3 | Titán Ígneo | Demonio de Magma (control) |
| 4 | Androide de Aleación | Centinela de Resonancia (control) |
| 5 | Eco Prismático (rota, F) | Aethernum (control, rota) |
| 6 | Aethernum (control, rota, F) | Androide de Aleación (vanguardia) |

**Paso 1 — Reparto de la presión.** El jugador ocupa el rol de choque y el aliado IA el de control, que se queda **cerca de la fuente**. Los humanos tienen dos objetivos (matar guardianes o extraer) y la pareja los cubre así:
- El jugador caza a los humanos que avanzan por los flancos.
- El aliado intercepta a cualquiera que entre en el radio de extracción (7 m). Su sola presencia **detiene la extracción**: la fuente queda "disputada" mientras haya un guardián vivo dentro.

**Paso 2 — Control del espacio.** Las habilidades de área (raíces, mareas, sismos, pulsos magnéticos) se lanzan sobre las **rutas** hacia la fuente, no sobre humanos aislados. Una habilidad de control bien colocada inmoviliza a 2 o 3 humanos a la vez.

**Paso 3 — Defensa de la fuente.**
- Si 2 o más humanos llegan a la fuente, el aliado lanza su definitiva (umbral de 2 enemigos en su radio ampliado).
- Si es la Savia (Madera), cura al jugador si está a menos de 12 m.

**Paso 4 — Encadenamiento.** El orden recomendado es **control → daño**:
1. El aliado ralentiza o enraíza.
2. El jugador entra con el golpe y la definitiva mientras los humanos no pueden alejarse.

Como en Historia **no hay maná** (solo enfriamientos), se pueden encadenar habilidades sin quedarse "seco". La disciplina la imponen los enfriamientos: 4, 7 y 16 s.

**Paso 5 — No verse superados.**
- La mayor vida (de 140 a 220 frente a 100–120) y el blindaje permiten **aguantar el fuego cruzado**.
- La afinidad con el escenario (daño +15%) y los peligros ambientales (que curan a los guardianes y dañan o ralentizan a los humanos) compensan la desventaja numérica.
- **Retirarse hacia un peligro propio** (claro de esporas, corriente de plasma, río de magma, campo de inducción) es una táctica válida: el guardián se cura y el humano que lo persigue sufre.

**Victoria:** eliminar a los 4 humanos (obligatorio para avanzar). **Derrota:** caen los dos guardianes o los humanos extraen el 100%. Si cae el jugador y su aliado sigue vivo, el jugador **espectea al aliado** y la partida continúa.

### 1.3.3 Atributos base: guardianes frente a humanos

**Regla:** los guardianes tienen **más vida** que los humanos y una velocidad **un poquito menor**, nada más.

| Personaje | Vida | Blindaje | Multiplicador de velocidad | Velocidad máxima |
|---|---|---|---|---|
| Humano: Traje Cinético | 100 | 40 | 1,10 | 11,55 m/s |
| Humano: Traje Podador | 100 | 50 | 1,02 | 10,71 m/s |
| Humano: Traje Térmico | 105 | 55 | 0,98 | 10,29 m/s |
| Humano: Traje Hidrostático | 105 | 50 | 0,97 | 10,19 m/s |
| Humano: Traje Sísmico | 120 | 80 | 0,95 | 9,98 m/s |
| Guardián Agua (vanguardia / control / artillería) | 176 / 160 / 141 | 54 / 45 / 45 | 0,92 | 9,66 m/s |
| Guardián Fuego | 182 / 165 / 145 | 48 / 40 / 40 | 0,91 | 9,56 m/s |
| Guardián Metal | 192 / 175 / 154 | 90 / 75 / 75 | 0,90 | 9,45 m/s |
| Guardián Madera | 209 / 190 / 167 | 84 / 70 / 70 | 0,88 | 9,24 m/s |
| Gólem de Chimenea (Tierra-Forja) | 220 / 200 / 176 | 108 / 90 / 90 | 0,87 | 9,14 m/s |

- El guardián **menos resistente** (Oráculo de la Estática, 141) tiene más vida que el humano **más resistente** (Sísmico, 120).
- El guardián **más rápido** (9,66 m/s) es más lento que el humano **más lento** (9,98 m/s).
- La diferencia media ronda el 10%.

**Cómo se siente y por qué no resultan lentos ni torpes:**
1. **Misma aceleración y frenado** (32 y 24 m/s²) para todos. El guardián arranca, frena y cambia de dirección con la misma respuesta inmediata que el humano: solo pierde en una carrera recta larga, y por poco (un humano Cinético gana unos 2 m por segundo de persecución).
2. **Mismo control aéreo, mismo salto y mismo "tiempo de coyote" y búfer de salto.** La fluidez del mando es idéntica.
3. **Aguante para sostener posiciones:** con un 40–100% más de vida y el blindaje que absorbe el 60% de cada golpe, un guardián puede **quedarse en la fuente** recibiendo fuego del escuadrón durante el tiempo que tardan sus habilidades en volver.
4. **Compensación por herramientas, no por velocidad:** lo que la velocidad no alcanza lo alcanzan las habilidades. Atraer (Pulso Magnético), empujar (Corriente de Marea), enraizar (Raíces Atrapantes) y lanzar (Golpe de Escoria) traen al humano o le impiden huir.
5. **Animación:** el guardián se anima con peso (pisadas con temblor de cámara cercano y polvo), pero con **los mismos tiempos de mezcla** que el humano (Doc 06, 1.2). Su pesadez es visual, no de control.

---

## 1.4 Lobby, los 3 modos y el apartado de Historia

### 1.4.1 Organización visual

1. **Fondo vivo:** detrás del menú se genera uno de los **6 escenarios** al azar y la cámara orbita lentamente (4°/s) alrededor de su fuente de energía (`OrbitaCamaraMenu`). Con arte realista, este fondo se sustituye por un **diorama cinematográfico** de cada escenario: cámara sobre raíl, iluminación de hora mágica y personajes en poses de reposo animadas.
2. **Panel lateral izquierdo** (38% del ancho, oscuro y translúcido): título **VOIDFORM** (96 pt), subtítulo **BIO-FORGE** con BIO en verde Cuidador y FORGE en naranja Destructor, y el lema.
3. **Botones** (columna, de arriba abajo, por orden de uso): **JUGAR** (destacado en violeta Void), **Archivo del mundo**, **Clasificación 4vs4**, **Controles** y **Salir** (rojo).
4. **Pie de página:** créditos técnicos, discretos.

### 1.4.2 Navegación (de lo simple a lo complejo)

```
Menú principal
 ├─ JUGAR → Elegir bando (Destructores / Cuidadores)
 │           └─ Elegir modo
 │               ├─ Historia → Elegir nivel (1-6, con bloqueo)
 │               │             ├─ Destructor → Equipamiento → Sala (bots o amigos) → Briefing → Partida
 │               │             └─ Guardián  → Briefing → Partida
 │               ├─ 4 vs 4   → Equipamiento (traje+arma / elemento+variante) → Sala → Partida
 │               └─ Conquista→ Equipamiento → Sala → Partida
 ├─ Archivo del mundo (5 pestañas)
 ├─ Clasificación 4vs4
 └─ Controles
```
- **Esc** retrocede un paso en cualquier menú.
- Cada pantalla tiene **"◄ Volver"** abajo a la izquierda y la acción principal (**Continuar ►**, **Iniciar ►**, **Comenzar nivel ►**) abajo a la derecha. Las acciones siempre están en el mismo sitio.

### 1.4.3 Contenido de cada apartado

1. **Elegir bando:** dos tarjetas grandes. Cada una explica la perspectiva (1.ª o 3.ª persona), los recursos (trajes y armas, o elementos y maná), la regla clave (explosión del traje o maná) y el dilema moral del bando.
2. **Elegir modo:** tres tarjetas.
   - **Historia:** 6 niveles; humano contra 3 guardianes (5 al final); guardián en pareja contra 4.
   - **Conquista:** 5 bases y 5:00; victoria inmediata con las 5; desempates.
   - **4 vs 4:** 3 rondas, espectador, empates y copas.
3. **Elegir nivel:** 6 tarjetas con número, título, escenario, **fuente de energía**, guardianes (linaje y número según tu bando) y estado (bloqueado, disponible o superado con su % de ecosistema).
4. **Equipamiento:**
   - Destructor: 5 trajes y 5 armas con sus estadísticas.
   - Guardián (solo versus): **5 linajes** (Madera, Agua, Fuego, Metal y Tierra-Forja) × **3 variantes** (vanguardia, control y artillería), con vida, blindaje, velocidad, radio, descripción visual, punto débil y habilidades.
5. **Sala:** Crear sala, Con bots, Con amigos o Sala pública. Opciones de privacidad, bioma (6 + aleatorio) y dificultad.
6. **Briefing** (Historia): escenario, peligro, **perspectiva con la composición exacta** (por ejemplo, "3 guardianes: Coloso Silvestre, Guardián de Corteza, Centinela de las Esporas"), introducción narrativa, objetivo, reglas y reflexión social.
7. **Archivo del mundo** (apartado de Historia), con 5 pestañas:
   - **Lore y problemática:** prólogo del viaje, las cinco fuentes y la problemática social.
   - **Escenarios y fuentes:** los 6 escenarios, cada uno con su fuente, su peligro y su gravedad. Se genera desde `DatosBioma`, así que siempre coincide con el juego.
   - **Guardianes:** la regla de atributos; los 5 linajes con sus 3 variantes, su vida, velocidad, punto débil y sobrecarga; y los Custodios del Nexus.
   - **Arquetipos humanos:** los 5 arquetipos con sus 3 habilidades y las 5 armas modulares (ficha completa en el Doc 09).
   - **Reglas de la campaña:** la composición de cada nivel en las dos perspectivas, generada desde `GestorModoHistoria`.
8. **Clasificación:** copas, rango, liga, racha, escudo y la fórmula completa.
