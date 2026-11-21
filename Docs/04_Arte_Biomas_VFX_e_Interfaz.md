# 04 · Arte, biomas, VFX e interfaz

El proyecto se juega desde el primer minuto **sin importar ningún asset**: personajes, biomas, efectos e interfaz se generan por código con primitivas, `ParticleSystem`, `LineRenderer`, luces y UGUI. Este documento es la **biblia de arte**: describe cómo debe verse cada cosa y cómo sustituir el arte provisional por arte final sin tocar la lógica.

---

## Nivel 1 — Dirección de arte general

- **Tono visual:** ciencia ficción ecológica. El planeta está vivo (bioluminiscencia, runas, núcleos que laten). La tecnología humana es industrial y cálida (naranja, metal, focos).
- **Lenguaje de color:**
  - Destructores: naranja **#FF7A38** (energía extraída, calor industrial).
  - Cuidadores: verde-turquesa **#4CFFB3** (vida, bioluminiscencia).
  - Neutral **#BFC7D9**, disputa **#FFE64D**, alerta **#FF3326**, maná **#738CFF**, ecosistema **#73F273**, acento Void **#8C73FF**.
  - Todos están centralizados en `PaletaVoidform` (`Nucleo/TiposYDatosJuego.cs`).
- **Regla de legibilidad:** cada bando y cada elemento se reconocen solo por la silueta y el color, a 30 m de distancia.

---

## Nivel 2 — Personajes, biomas y peligros

### 2.1 Destructores (humanos con exotraje)

Silueta humana compacta: torso de exotraje, casco con **visor emisivo horizontal**, **mochila extractora** con un tanque de energía brillante (símbolo del extractivismo) y arma al frente. El color del traje identifica el modelo:

| Traje | Color base | Rasgos de diseño |
|---|---|---|
| Cinético de Asalto | Naranja vivo | Placas ligeras, propulsores dorsales visibles |
| Térmico Ignífugo | Rojo cerámico | Placas refrigeradas con aletas |
| Hidrostático Anclado | Azul acero | Sellos estancos, lastres magnéticos en botas |
| Sísmico Blindado | Gris basalto | Placas gruesas y hombreras amortiguadas |
| Podador Biotécnico | Verde oliva | Cuchillas térmicas en antebrazos, depósitos de biocombustible |

### 2.2 Guardianes elementales (nacen elementales; inspiración tanque-natural, diseño propio)

Silueta de **coloso**: 1,4 veces más grande que un humano, **más ancho arriba que abajo**, hombros masivos con **runas emisivas**, brazos largos acabados en "mazas" elementales, cabeza pequeña con ojos luminosos y un **núcleo brillante en el pecho**.

| Linaje | Descripción visual (detalle completo en el Doc 08) |
|---|---|
| **Coloso Silvestre** (Madera) | Corteza translúcida esmeralda con la savia visible, runas ámbar, corona de musgo bioluminiscente y semilla-corazón en el pecho. Variantes: Guardián de Corteza (escamas de corteza, raíces flexibles) y Centinela de las Esporas (dosel de hojas-panel, vainas de esporas). |
| **Leviatán Abisal** (Agua) | Plasma añil contenido por coral sintético, mandíbula de cristal marino y aletas de estática. Variantes: Señor de las Mareas (nácar, cascada invertida) y Oráculo de la Estática (flota sobre una columna de plasma con un halo de tormenta). |
| **Titán Ígneo** (Fuego) | Obsidiana negra con fractura concoidea y núcleo de magma visible. Variantes: Demonio de Magma (magma carmesí con costras, melena de llamas) y Heraldo de Ceniza (halo de ceniza y cristales de enfriamiento). |
| **Androide de Aleación** (Metal) | Aleación plateada con placas que se reajustan; **levita** a pocos centímetros. Variantes: Centinela de Resonancia (anillos concéntricos) y Autómata de Precisión (brazo-riel y lente que se estrecha). |
| **Gólem de Chimenea** (Tierra-Forja) | Placas claras con remaches oscuros, chimenea humeante con llamas naranjas y antebrazo con núcleo incandescente. |
| **Custodios del Nexus** | Piedra translúcida atravesada por las cuatro corrientes; los anillos cambian de material según la forma activa (Eco Prismático, Aethernum y Vigía Aural). |

En código (`Modelos/ConstructorModelosPersonajes.cs`) cada guardián tiene su **rasgo distintivo** (cuernos y corona, cascada invertida, estratos, melena de llamas) y un **aura de partículas** de su elemento (`FabricaEfectosVisuales.CrearAuraElemental`).

### 2.3 Los 6 escenarios · `Entornos/DatosBioma.cs`, `Entornos/GeneradorBiomas.cs`

| Escenario | Paleta (suelo · acento · niebla) | Coberturas | Fuente | Peligro |
|---|---|---|---|---|
| **Bosque de los Susurros Bioluminiscentes** | violeta · turquesa · verde bruma | Árboles de corteza translúcida, raíces sin enterrar sobre vetas violetas, hojas-panel | Árbol Madre | Claros de esporas |
| **Abismos de las Mareas de Plasma** | añil · azul eléctrico · azul bruma | Plataformas de coral sintético, cristal marino, puentes colgantes que se mecen | Turbina Abisal | Corrientes de plasma |
| **Caldera de las Forjas Magmáticas** | negro obsidiana · naranja magma · humo rojizo | Agujas de obsidiana con grietas de magma, cristales de enfriamiento rápido, respiraderos de vapor | Núcleo Térmico | Ríos de magma |
| **Agujas de Aleación Magnética** | plata · azul pálido · gris violáceo | Placas tectónicas espejo y torres de aleación que levitan | Monolito Central de Resonancia | Campos de inducción |
| **Nexus Primordial** | piedra clara · lavanda · índigo | Columnas de piedra translúcida, canales de plasma, enredaderas petrificadas, filamentos conductores; gravedad ×0,6 y auroras | Corazón del Planeta | Los cuatro, por cuadrante |
| **Convergencia Total** | mezcla de los cinco | Cuadrantes de los cuatro biomas y anillo central del Nexus; gravedad ×0,8 y auroras | Corazón del Planeta | Los cuatro, por cuadrante |

La guía de materiales realistas de cada escenario está en `06_Direccion_Realista_Materiales_y_Animacion.md` y su ficha completa, en el Doc 08.

**Disposición de la arena (110 × 110 m):** zona de aterrizaje humana al **sur** (plataforma, cápsula de descenso y torres de focos) y santuario elemental al **norte** (arco de piedras rúnicas). Coberturas con **simetría central**, para que ningún equipo tenga ventaja. 5 bases en (±26, ±16) y en el centro, y la **Fuente de Energía Viva** en el centro.

---

## Nivel 3 — VFX, degradación del ecosistema e interfaz

### 3.1 Efectos visuales · `VFX/FabricaEfectosVisuales.cs`

| Efecto | Capas |
|---|---|
| **Explosión del traje** | 1) Núcleo incandescente (70 partículas rápidas). 2) Humo oscuro que sube: la contaminación. 3) Escombros con gravedad. 4) Anillo de onda expansiva. 5) Luz puntual naranja de 0,6 s. 6) Sacudida de cámara que se atenúa con la distancia. |
| **Árbol** | Esporas luminosas que suben + astillas de raíz |
| **Agua** | Salpicadura radial con gravedad + neblina |
| **Tierra** | Rocas que saltan + polvo ocre (y muro de roca físico con *Muralla y Temblor*) |
| **Fuego** | Llamarada + brasas ascendentes + destello de luz |
| **Armas** | Destello en la boca del cañón con luz breve, trazador (`LineRenderer` de 0,07 s) y chispas de impacto (rojas si impactan en un enemigo) |
| **Ambiente** | Partículas flotantes del bioma por toda la arena |
| **Auras** | Partículas persistentes alrededor de cada guardián |

### 3.2 Degradación del ecosistema (la problemática social, visible en pantalla)

Cuando baja la integridad del ecosistema (`GestorEquilibrioEcosistema`):
- Cada pieza registrada con `RegistrarDegradable` pasa de su **color sano** a un **color degradado** (gris o marrón) y pierde emisión. Las copas se marchitan y las runas se apagan.
- La **niebla** se vuelve hasta 2,2 veces más densa y se agrisa.
- El **sol** pierde hasta el 40% de su intensidad y se desatura.
- La **Fuente de Energía Viva** encoge, late más despacio, sus anillos giran más lento y su luz se apaga.

### 3.3 Interfaz de usuario · `Interfaz/GestorInterfazJuego.cs` (+ `.HUD.cs`)

**Estilo:** paneles translúcidos azul vacío con **esquinas redondeadas** (sprite de 9 cortes generado por código), franja de acento violeta, títulos grandes en negrita con sombra y textos enriquecidos con colores de bando.

**Pantallas** (el orden de navegación es de lo simple a lo complejo):
1. **Menú principal:** VOIDFORM / BIO-FORGE, lema, Jugar, Historia y problemática social, Clasificación, Controles y Salir, sobre un bioma real con cámara orbital.
2. **Selector de bando:** dos tarjetas (Destructores y Cuidadores) con sus reglas clave.
3. **Selector de modo:** Historia, Conquista y 4vs4, con sus reglas resumidas.
4. **Selector de nivel** (Historia): 5 tarjetas con bioma, guardián, estado (bloqueado, disponible o superado con su % de ecosistema) y la regla de la perspectiva.
5. **Personalización:** 5 trajes y 5 armas con estadísticas, resistencias y habilidades, o 4 guardianes con descripción visual, personalidad y habilidades con sus costes.
6. **Selección de sala:** Crear sala, Con bots, Con amigos y Sala pública. Opciones de privacidad, bioma, dificultad y código de amigo. Estado de la sala, lista de jugadores y bots, y avisos de combinaciones no válidas.
7. **Briefing** (Historia): título, bioma y peligro, **perspectiva y reglas de participación**, introducción narrativa, objetivo y reflexión social.
8. **HUD de combate:**
   - Arriba en el centro: **reloj** (5:00 en Conquista, ronda en 4vs4, nivel en Historia) y subtítulo con el marcador (bases o rondas, supervivientes).
   - Debajo: **5 bases A–E** con el color de su estado, una barra de progreso de captura y "!" si están en disputa.
   - Arriba a la derecha: **integridad del ecosistema**, **energía extraída** (Historia) y **copas, rango y liga** (4vs4).
   - Arriba a la izquierda (Historia): nivel, perspectiva, objetivo y estado de la Fuente.
   - Abajo a la izquierda: identidad y bioma, **vida**, **integridad del traje o corteza**, **maná** (guardián en versus) y estados alterados.
   - Abajo a la derecha: **arma, munición, reserva y RECARGANDO**, más las ranuras E/Q (Destructor), o el **elemento y 4 ranuras** Clic/E/R/Q con coste y enfriamiento (Guardián).
   - Centro: mira (cruz en 1.ª persona, punto en 3.ª), **marcador de impacto** (rojo si hay baja), **alerta de traje crítico**, cuenta atrás de reaparición, banners de ronda y aviso de **maná insuficiente**.
   - Izquierda: **registro de combate** con colores de bando, que indica las bajas por explosión de traje.
   - Panel **MODO ESPECTADOR** cuando estás muerto en 4vs4 o Historia.
   - Viñeta roja al recibir daño.
9. **Pausa:** Continuar, Reiniciar partida o Abandonar.
10. **Resultados:** VICTORIA, DERROTA o EMPATE; mensaje y detalle (desempates, integridad final, reflexión); **copas antes → después, rango, liga, racha y escudo** (4vs4); nivel desbloqueado y **desenlace de la campaña** (Historia); botones Volver al menú, Jugar de nuevo y Siguiente nivel.
11. **Historia y problemática**, **Controles** y **Clasificación** (con el reglamento de copas y el botón para borrar el progreso).

### 3.4 Guía para sustituir el arte provisional

| Pieza | Dónde | Cómo |
|---|---|---|
| Modelos de personajes | `ConstructorModelosPersonajes` | Instanciar el prefab final y registrar sus renderers en `ModeloVisualPersonaje` (capas 0/1/2) |
| Explosión del traje | `SistemaSaludYTraje.efectoVisualExplosionTraje` | Asignar un prefab de VFX (si es nulo, se usa el VFX procedural) |
| Efectos elementales | `FabricaEfectosVisuales.CrearEfectoElemental` | Sustituir cada `case` por un `Instantiate` del prefab de VFX Graph o Shuriken |
| Coberturas de bioma | `GeneradorBiomas.Crear*` | Instanciar prefabs y llamar a `RegistrarDegradable` para que se marchiten |
| Interfaz | `FabricaInterfaz` | Cambiar `ObtenerSpriteRedondeado()` por sprites del equipo de arte y `ObtenerFuente()` por la fuente corporativa |
