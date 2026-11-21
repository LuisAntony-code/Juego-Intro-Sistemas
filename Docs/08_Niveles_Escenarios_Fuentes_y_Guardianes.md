# 08 · Los 6 niveles: escenarios, fuentes de energía y guardianes

> **Qué está implementado y qué es producción artística.**
> - **Implementado en código:** los 6 escenarios (con aproximación procedural de cada uno), las 5 fuentes con forma propia, la gravedad reducida del Nexus, las auroras, los peligros por facción, los 5 linajes × 3 variantes de guardián (vida, blindaje, velocidad, radio y alcance por rol), la composición de cada nivel, la coordinación de la IA por roles y la rotación de elementos del Nexus.
> - **Producción artística (este documento lo especifica):** los modelos y materiales realistas, las animaciones por rango y los efectos finales.
> - **Diseño todavía no implementado como mecánica:** el **núcleo o punto débil**, la **estabilidad** y la **sobrecarga** de cada guardián se muestran como texto en el Archivo del mundo, pero aún no alteran el daño. El apartado 2.7 explica cómo implementarlos.
>
> **Kit de habilidades por linaje.** Las 3 variantes de un linaje comparten el kit elemental (golpe básico, E, R y Q). El rol cambia su vida, blindaje, radio y alcance:

| Rol | Vida | Blindaje | Radio de habilidades (base 6,5 m) | Definitiva (radio × 1,5) | Alcance del golpe básico |
|---|---|---|---|---|---|
| Vanguardia (corta) | ×1,10 | ×1,20 | 5,85 m | 8,8 m | Base del linaje |
| Control (media) | ×1,00 | ×1,00 | 7,15 m | 10,7 m | Base del linaje |
| Artillería (larga) | ×0,88 | ×1,00 | 8,78 m | 13,2 m | Base + 3 m |

> En cada guardián, **corta distancia** es el golpe básico (clic), **media distancia** son la habilidad 1 (E) y la 2 (R), y **larga distancia** es la definitiva (Q).

---

## Regla de distribución de guardianes (todos los niveles)

| Nivel | Contra el bando humano (4 humanos) | Contra el bando guardián (jugador + aliado) |
|---|---|---|
| 1–5 | **3 guardianes** del elemento del escenario: vanguardia, control y artillería | **2 guardianes** del linaje del escenario frente a 4 humanos |
| 6 | **5 guardianes**, uno de cada escenario anterior | Aethernum (jugador) + Androide de Aleación frente a 4 humanos. Los demás linajes están presentes en la Convergencia (ver nivel 6) |

**Pasos comunes a todos los niveles:**
1. **Posicionamiento:** guardianes en abanico a 11 m al norte de la fuente; humanos en la zona de aterrizaje del sur.
2. **Primer contacto:** la vanguardia sale al encuentro y el control y la artillería esperan a que los humanos entren en el radio de la fuente.
3. **Dinámica:** el humano decide si **extrae** (rápido, pero daña el ecosistema y le expone ante 2 guardianes defensivos) o si **caza guardianes** (lento, pero sostenible).

---

## Nivel 1 — El Bosque de los Susurros Bioluminiscentes (Madera)

### 2.1.1 Descripción visual realista, paso a paso

1. **Suelo:** tierra **violeta** (albedo 0,22; 0,12; 0,30) cubierta de hojarasca translúcida. Mezcla por altura con musgo turquesa en las zonas bajas y húmedas. Las **vetas de mineral orgánico** afloran del suelo como lomos cristalinos violetas de 2 a 6 m de largo, con fracturas emisivas.
2. **Árboles colosales:** troncos de **corteza translúcida** de 8 a 30 m de diámetro en el fondo (fuera de la zona jugable) y de 1,5 a 3 m en la jugable. Se construyen como en el Doc 06, 1.1.1: fibras longitudinales, poros, microfisuras y savia que late en **verde esmeralda y turquesa** por dentro.
3. **Raíces sin enterrar:** cada tronco despliega de 3 a 6 raíces que se **enroscan sobre las vetas de mineral**, formando arcos bajo los que se puede pasar (coberturas naturales de 1,2 a 2 m de alto).
4. **Hojas-panel solar:** copas a 15–40 m de altura con hojas de 0,6 a 1,2 m. En la cara superior, un brillo especular anisótropo y un patrón hexagonal casi invisible. En la inferior, translucidez turquesa.
5. **Iluminación filtrada:** sol alto (60°) con intensidad de 40 000 lux que atraviesa las copas. **Rayos volumétricos** (niebla volumétrica de densidad media) con manchas de luz (*gobos* de hojas) en el suelo.
6. **Bruma:** niebla baja de 0 a 2 m que se desplaza lentamente (*Local Volumetric Fog* con textura 3D desplazándose a 0,2 m/s).
7. **Esporas bioluminiscentes:** de 2 000 a 5 000 partículas (VFX Graph) de 1 a 3 mm que flotan entre 0,5 y 6 m de altura con *curl noise* lento. Se **iluminan al acercarse** un personaje (parámetro de posición enviado al VFX) y se apagan al alejarse.
8. **Sonido:** un susurro de hojas que forma "voces" ininteligibles en estéreo: es el nombre del bosque.

**Aproximación procedural actual:** `GeneradorBiomas.CrearArbolBioluminiscente` (tronco emisivo, 3 raíces, veta violeta y copa de hojas-panel) y el suelo violeta.

### 2.1.2 La Fuente de Energía: el Árbol Madre

1. **Ubicación:** en el centro exacto del bosque y del mapa (coordenadas 0, 0).
2. **Tronco central:** 4,4 m de diámetro jugable (en producción final, 12 m con la base esculpida como raíces entrelazadas). La savia **late al unísono**: un pulso cada 1,2 s, que sube de la base a la copa en 0,8 s y que comparten **todos** los árboles del bosque, con un retraso proporcional a su distancia (la onda viaja a 25 m/s por la red de raíces).
3. **Red de filamentos luminosos:** de las ramas bajan de 6 a 12 filamentos de energía, de 5 cm de grosor, que se clavan en el suelo y en los demás árboles. Por cada filamento viajan **paquetes de luz** (partículas en cinta) hacia el ecosistema. Implementación provisional: 6 filamentos emisivos en `FuenteEnergiaViva.ConstruirFormaDelEscenario`.
4. **Cúpula bioeléctrica:** semiesfera de 7 m de radio (el radio de extracción) con shader de fresnel turquesa casi invisible, que se hace visible con ondas hexagonales cuando:
   - una tormenta externa (relámpago lejano) la golpea;
   - un humano entra a extraer: aparecen ondas allí donde cruza;
   - un guardián la defiende: la cúpula se ilumina en su lado.
   La base de la cúpula ya existe como disco emisivo.
5. **Función de juego:** los humanos dentro del radio extraen 0,6 de energía por humano y por segundo, si no hay guardianes. Cada unidad cuesta 0,6 de integridad del ecosistema. A medida que baja la integridad, el Árbol Madre **se apaga** (pulso más lento y débil) y el bosque entero se marchita (degradación ya implementada).

### 2.1.3 Guardianes del nivel 1: linaje del Coloso Silvestre (Madera)

**Atributos comunes:** vida de 167 a 209 (más que cualquier humano), velocidad ×0,88 (9,24 m/s, frente a los 9,98–11,55 m/s de los humanos), kit Puño de Corteza · Raíces Atrapantes · Savia Regeneradora · Espinar del Bosque Primordial.

#### Coloso Silvestre (vanguardia, corta distancia)
- **Diseño realista:**
  - Altura de 2,9 m. Hombros anchos como troncos milenarios.
  - Corteza translúcida en placas gruesas que dejan ver la savia esmeralda.
  - Runas ámbar talladas en los hombros que laten con la savia.
  - **Corona de musgo bioluminiscente** y cuernos-rama asimétricos.
  - Manos como nudos de raíz, de 50 cm.
  - **Semilla-corazón** verde en el esternón, protegida por dos placas de corteza que se abren al respirar.
- **Locomoción:** pasos lentos y amplios con transferencia de peso marcada. Cada pisada levanta hojarasca y deja un brote que se marchita en 3 s (o al instante si el ecosistema está por debajo del 35%). Inclinación corporal baja (pesa) y giros con pivote completo.
- **Corta (Puño de Corteza):**
  - Anticipación: echa atrás el brazo y la corteza del codo cruje (0,15 s).
  - Ejecución: puñetazo descendente en un cono de 65° y 3,2 m; impacto con astillas.
  - Seguimiento: el puño queda clavado 0,1 s y sale con raíces que se rompen.
- **Media (Raíces Atrapantes, radio de 5,85 m):**
  - Anticipación: hunde ambas manos en el suelo y aparece el círculo verde (0,3 s).
  - Ejecución: raíces y espinas brotan en el área y **enraízan 1,8 s**.
  - Seguimiento: las raíces se retraen lentamente mientras el Coloso se incorpora.
- **Media (Savia Regeneradora):** se golpea el pecho, la semilla-corazón brilla y una onda de savia cura +32 propia y +20 a los aliados a menos de 12 m.
- **Larga (Espinar del Bosque Primordial, radio de 8,8 m):**
  - Anticipación: alza los brazos, la corona se ilumina y el suelo tiembla (0,6 s).
  - Ejecución: un bosque de espinas brota en anillo desde él hacia fuera, con daño masivo, enraizado de 2,5 s y curación propia de 40.
  - Seguimiento: las espinas florecen y se convierten en esporas que flotan 2 s.
- **Núcleo / punto débil:** la semilla-corazón, visible entre las placas mientras respira (cada 3 s, durante 0,8 s).
- **Estabilidad:** muy alta, porque sus raíces se anclan; solo se tambalea con explosiones de traje o impactos cargados.
- **Sobrecarga:** tras la definitiva, la corteza se vuelve opaca 3 s y no regenera.
- **Sinergia:** cerca del Árbol Madre y de los claros de esporas se cura y regenera maná más rápido (afinidad: +15% de daño y +25% de maná en modos versus).

#### Guardián de Corteza (control, media distancia)
- **Diseño realista:**
  - Altura de 2,7 m, más esbelto.
  - Láminas de corteza superpuestas como escamas que se abren al moverse, como una piña.
  - Antebrazos recorridos por **vetas violetas de mineral orgánico**.
  - Dedos largos que se despliegan en raíces flexibles de hasta 2 m.
- **Locomoción:** zancada más larga y ligera. Las escamas de corteza tienen movimiento secundario (pequeños muelles por escama).
- **Corta:** latigazo de raíz de los dedos (mismo kit, alcance de 3,2 m), con un barrido lateral de 100°.
- **Media (Raíces, radio de 7,15 m):** su variante teje una **red de raíces** en forma de rejilla. Úsala sobre las rutas hacia el Árbol Madre.
- **Larga (Espinar, radio de 10,7 m):** el espinar crece siguiendo las vetas de mineral cercanas: las espinas brotan primero sobre las vetas y luego rellenan.
- **Núcleo:** semilla-corazón bajo las escamas del pecho; las escamas se abren al lanzar habilidades.
- **Estabilidad:** alta. **Sobrecarga y sinergia:** como el Coloso.
- **Papel táctico:** se queda entre los humanos y la fuente a 5 m (radio × 0,7) y corta el paso.

#### Centinela de las Esporas (artillería, larga distancia)
- **Diseño realista:**
  - Altura de 3 m, cuerpo alargado.
  - Coronado por un **dosel de hojas-panel** que se orienta al sol.
  - **Vainas de esporas** turquesa en la espalda que se inflan antes de disparar.
- **Locomoción:** pasos medidos. El dosel se balancea con retraso (movimiento secundario).
- **Corta (golpe con alcance de 6,2 m):** lanza un **chorro de esporas comprimidas** en lugar de un puño.
  - Anticipación: la vaina se infla (0,15 s).
  - Ejecución: la vaina se contrae y sale el chorro.
- **Media (Raíces, radio de 8,78 m):** dispara una vaina que estalla en el área y hace brotar las raíces.
- **Larga (Espinar, radio de 13,2 m):** lluvia de vainas desde el dosel. Las espinas brotan donde caen.
- **Núcleo:** la vaina mayor de la espalda (expuesta al disparar).
- **Estabilidad:** media. **Papel táctico:** mantiene unos 7,5 m de distancia y se mueve en arco.

### 2.1.4 Dinámica del nivel 1

- **Contra el humano:**
  - El Coloso sale por un flanco entre las raíces.
  - El Guardián de Corteza teje redes en los accesos al Árbol Madre.
  - El Centinela castiga desde atrás de los arcos de raíz.
  - Los claros de esporas curan a los guardianes y frenan a los humanos: los humanos deben **evitar pelear dentro de ellos**.
- **Contra el guardián (jugador Coloso + aliado Corteza):**
  - El jugador presiona a los humanos que se acercan por los flancos.
  - El Guardián de Corteza se queda en el Árbol Madre (lo bloquea con su presencia) y teje redes.
  - El jugador puede retirarse a un claro de esporas para curarse.

---

## Nivel 2 — Los Abismos de las Mareas de Plasma (Agua)

### 2.2.1 Descripción visual realista, paso a paso

1. **Océano de plasma:** líquido denso y bioluminiscente, **azul profundo y añil**, hasta el horizonte. Materiales del Doc 06, 1.1.2: absorción de Beer-Lambert, refracción, cuatro ondas de Gerstner lentas y plancton luminoso en profundidad.
2. **Cielo:** nubes bajas, densas y oscuras, **cargadas de estática**. Relámpagos internos cada 3 a 8 s y descargas al mar cada 10 a 20 s. Dos lunas visibles (una grande y pálida, otra pequeña y azulada) que **mueven las mareas**: la dirección de las olas sigue la suma de sus posiciones.
3. **Estructuras flotantes:** plataformas de **coral sintético** (ramificado, poroso, en azul pálido con puntas emisivas) y **cristal marino** (prismas translúcidos de 1 a 6 m que refractan la luz del plasma). Las plataformas son el suelo jugable y suben y bajan de 5 a 15 cm con el oleaje.
4. **Puentes colgantes flexibles:** tablones de coral prensado sobre cables de fibra. Se mecen con las olas (balanceo de ±3° y cabeceo de ±1,5°) y crujen. Los personajes que los cruzan los hunden ligeramente (deformación con huesos).
5. **Iluminación:** luz principal fría y tenue (20 000 lux, cielo cubierto), relleno azul desde abajo (el plasma) y destellos de los relámpagos.
6. **Partículas:** espuma espesa en las crestas, gotas que saltan, niebla salina baja y arcos de estática entre las crestas.

**Aproximación procedural actual:** `CrearEstructuraDeCoral` (plataforma, ramas de coral, cristal marino y puente con `BalanceoPorOleaje`) y el suelo añil.

### 2.2.2 La Fuente de Energía: la Turbina Abisal

1. **Estructura:** un **anillo gigantesco** (en la versión final, 120 m de diámetro) sumergido en el abismo en vertical. Desde la superficie se ve su borde superior emergiendo y su luz bajo el plasma. En el prototipo, dos anillos verticales que giran sobre un pozo emisivo.
2. **Funcionamiento:**
   - Las **corrientes térmicas** del fondo y las **mareas de plasma** (movidas por la gravedad de las dos lunas) atraviesan el anillo.
   - Los álabes internos (80 aletas de aleación y coral) giran con la corriente.
   - La velocidad de giro sigue la marea: más rápida cuando las dos lunas están alineadas, cada 4 minutos de juego.
3. **Energía hidrocinética constante:** cada 2 s el anillo emite un **pulso** (un anillo de luz que se expande por la superficie del plasma hasta el horizonte), la energía que se irradia a otros sectores.
4. **Función de juego:** extracción como en el resto de fuentes. Al bajar la integridad, el giro se ralentiza y los pulsos se espacian y apagan.

### 2.2.3 Guardianes del nivel 2: linaje del Leviatán Abisal (Agua)

**Atributos:** vida de 141 a 176, velocidad ×0,92 (9,66 m/s, el linaje más rápido sin superar a ningún humano), kit Latigazo de Corriente · Pulso Fluvial Restaurador · Corriente de Marea · Diluvio de Plasma.

#### Leviatán Abisal (vanguardia, corta distancia)
- **Diseño realista:**
  - Torso ancho de **plasma denso añil** contenido por placas de **coral sintético**.
  - Mandíbula de **cristal marino**.
  - Aletas dorsales que chisporrotean **estática azul-blanca**.
  - Por dentro se ven corrientes en movimiento (shader de plasma con absorción) y burbujas.
- **Locomoción:** ondulante: el cuerpo se mueve como una ola (desfase de la columna). Deja huellas de plasma que se evaporan.
- **Corta (Latigazo de Corriente, 5,5 m):**
  - Anticipación: el brazo se licúa (0,15 s).
  - Ejecución: látigo de agua.
  - Seguimiento: las gotas caen con la gravedad del plasma (×1,3).
- **Media (Pulso Fluvial, radio de 4,1 m; Corriente de Marea, radio de 5,85 m):** el Pulso cura +28 y daña cerca. La Marea es una ola que **empuja 14 m/s** y ralentiza 2,5 s; la anticipación es un retroceso del agua alrededor de sus pies, como antes de un tsunami.
- **Larga (Diluvio de Plasma, radio de 8,8 m):** levanta los brazos, el plasma sube en columna y cae en un anillo. Ralentiza al 40% durante 3,5 s, empuja y cura 40.
- **Núcleo:** la **vejiga de plasma comprimido** de la espalda, que brilla al cargar habilidades.
- **Estabilidad:** media; los impactos sísmicos lo dispersan un instante.
- **Sobrecarga:** sin maná, su cuerpo pierde densidad 2 s.
- **Sinergia:** la Turbina Abisal y las corrientes de plasma lo curan.

#### Señor de las Mareas (control, media distancia)
- **Diseño realista:**
  - Figura esbelta de agua translúcida con armadura de **nácar** (material iridiscente: HDRP *Iridescence*).
  - **Cascada que cae hacia arriba** en lugar de cabello.
  - Dos lunas diminutas orbitando sus manos.
- **Locomoción:** se desliza. Los pies apenas tocan el suelo y dejan anillos de onda.
- **Corta:** latigazo doble con las dos manos, en arco.
- **Media (radio de 7,15 m):** la Corriente de Marea sale en la **dirección de las lunas de sus manos**. Úsala para empujar a los humanos fuera del radio de la Turbina.
- **Larga (radio de 10,7 m):** el Diluvio dibuja un remolino que atrae ligeramente hacia el centro antes de caer (anticipación visual de 0,6 s).
- **Núcleo:** la luna mayor de su mano izquierda. **Papel:** portero de la Turbina.

#### Oráculo de la Estática (artillería, larga distancia)
- **Diseño realista:** silueta que **flota** sobre una columna de plasma, con un halo de nubes cargadas que descargan arcos eléctricos.
- **Corta (8,5 m):** descarga eléctrica en lugar de látigo. Anticipación: el halo se ilumina (0,15 s).
- **Media (radio de 8,78 m):** el Pulso y la Marea vienen precedidos por un relámpago que marca el centro.
- **Larga (radio de 13,2 m):** el Diluvio cae acompañado de 3 relámpagos sobre los humanos más cercanos al centro.
- **Núcleo:** la columna de plasma que lo sostiene; si se interrumpe, cae.
- **Estabilidad:** baja. **Papel:** castiga desde las plataformas lejanas.

### 2.2.4 Dinámica del nivel 2

- **Contra el humano:** los puentes son cuellos de botella. El Leviatán los cierra, el Señor de las Mareas empuja hacia las corrientes de plasma (que ralentizan) y el Oráculo dispara a quien cruza. Los humanos deben llevar el **Traje Hidrostático** (inmune al empuje y a la ralentización del agua).
- **Contra el guardián (Leviatán + Señor de las Mareas):** el jugador cierra los puentes y el aliado expulsa de la Turbina a quien llega.

---

## Nivel 3 — La Caldera de las Forjas Magmáticas (Fuego)

### 2.3.1 Descripción visual realista, paso a paso

1. **Valle desértico** de **obsidiana negra** con fractura concoidea y bordes vítreos (Doc 06, 1.1.3). Polvo de ceniza gris en las superficies horizontales.
2. **Gargantas profundas** de 10 a 40 m con **ríos de magma** naranja brillante en el centro y carmesí en los bordes. Costra que se forma y se rompe. Burbujas.
3. **Aire denso y brillante:** distorsión térmica sobre los ríos y alrededor del Núcleo, más una neblina rojiza (niebla de color 0,32; 0,12; 0,06).
4. **Formaciones cristalinas** en las paredes de los cañones: franjas de prismas ámbar por enfriamiento rápido que proyectan cáusticas con la luz del magma.
5. **Columnas de vapor presurizado** en ciclos de 6 a 10 s.
6. **Iluminación:** sol bajo, rojizo y filtrado por ceniza (30 000 lux, temperatura de 3 500 K) más la luz del magma desde abajo, que crea sombras invertidas.
7. **Partículas:** ascuas por todo el valle, ceniza cayendo como nieve gris, chispas en los respiraderos.

**Aproximación procedural actual:** `CrearAgujaDeObsidiana` (aguja negra, grieta de magma, cristales y respiraderos de vapor) y peligros "Río de magma".

### 2.3.2 La Fuente de Energía: el Núcleo Térmico

1. **Estructura:** una **super-fumarola artificial** (cilindro de roca y aleación de 5,2 m de diámetro y 4 m de altura en el prototipo; en la versión final, 15 m) construida sobre un **géiser de magma de alta presión**. En su boca hay un disco de magma emisivo.
2. **Columnas de fuego controlado:** cada 5 s una columna de fuego sale de la boca (anticipación: rugido y vibración de 1 s; erupción: 2 s hasta 20 m de altura; cola de humo).
3. **Generadores de vapor masivos:** dos o cuatro torres a los lados reciben el calor por conductos y expulsan vapor por arriba. Sus turbinas giran y zumban.
4. **La fuente más volátil y de mayor potencia calórica del planeta:**
   - Visualmente, el magma pulsa a 1,5 Hz (más rápido que ninguna otra fuente).
   - Si la integridad baja del 35%, las columnas se vuelven **irregulares** (erupciones fuera de ciclo).
   - Extensión opcional: una erupción fuera de ciclo que hace daño a todos en 7 m.

### 2.3.3 Guardianes del nivel 3: linaje del Titán Ígneo (Fuego) y Gólem de Chimenea (Tierra-Forja)

#### Titán Ígneo (vanguardia, corta distancia)
- **Diseño realista:**
  - 2,9 m de **obsidiana negra** con fractura concoidea.
  - **Núcleo de magma** visible entre las placas del torso.
  - Puños que gotean roca fundida, que se solidifica en el suelo en 2 s.
- **Locomoción:** cada paso deja una huella incandescente que se enfría (emisión decreciente). La distorsión térmica le rodea.
- **Corta (Zarpa Ígnea):** zarpazo diagonal de 3,2 m que deja una **Quemadura** de 4/s durante 1,5 s. Las garras dejan estelas de fuego.
- **Media:**
  - **Ráfaga Ígnea Abrasadora:** cono de 70° y 8,2 m. Anticipación: inspira y el pecho se ilumina (0,3 s). Quemadura de 8/s durante 3 s.
  - **Combustión de Área:** radio de 6,4 m y Quemadura de 10/s. El Titán se encoge y estalla hacia fuera.
- **Larga (Erupción del Núcleo Térmico, radio de 8,8 m):** golpea el suelo con los dos puños y el magma sale en anillo. Quemadura de 12/s durante 4 s y empuje.
- **Núcleo / punto débil:** las grietas de magma del torso, que se ensanchan al cargar la definitiva.
- **Estabilidad:** media; el agua lo enfría y lo hace tambalear.
- **Sobrecarga:** tras la definitiva, el magma se enfría 3 s (caparazón gris).
- **Sinergia:** sobre los ríos de magma y junto al Núcleo Térmico se cura.

#### Demonio de Magma (control, media distancia)
- **Diseño realista:**
  - Cuerpo de **magma carmesí semilíquido** contenido por costras que se rompen y se vuelven a formar (shader con umbral de costra animado por esfuerzo).
  - **Melena de llamas** que pasa de naranja a azul al enfurecerse (al lanzar la definitiva o con vida por debajo del 30%).
- **Locomoción:** fluida, casi líquida. Las costras crujen con cada movimiento.
- **Corta:** zarpazo con salpicadura de magma.
- **Media (radio de 7,15 m):** su Combustión **deja el suelo ardiendo** visualmente 2 s.
- **Larga (radio de 10,7 m):** la Erupción sale en espiral desde él.
- **Núcleo:** la melena; cuando arde en azul, queda expuesto. **Papel:** portero del Núcleo Térmico.

#### Gólem de Chimenea · Mortero (artillería, sub-arquetipo Tierra-Forja)
Ver la ficha completa en la sección 2.6. En la Caldera ocupa la plaza de artillería: es la **forja viva** del valle.

### 2.3.4 Dinámica del nivel 3

- **Contra el humano:**
  - El Titán sale al cruce de las gargantas.
  - El Demonio defiende el Núcleo.
  - El Gólem bombardea desde detrás de las agujas de obsidiana.
  - Los ríos de magma (12 de daño/s a los humanos; el Traje Térmico los resiste) parten el mapa.
- **Contra el guardián (Titán + Demonio):** el Titán presiona y el Demonio llena de quemaduras la zona de extracción.

---

## Nivel 4 — Las Agujas de Aleación Magnética (Metal)

### 2.4.1 Descripción visual realista, paso a paso

1. **Llanura metálica** de **placas tectónicas artificiales** que encajan con precisión (Doc 06, 1.1.4): juntas de 2 cm, biseles de 3 mm y superficie pulida **como un espejo gigante** que refleja el cielo alienígena (violeta oscuro con dos lunas y estrellas).
2. **Torres monolíticas puntiagudas** de aleación plateada desconocida, de 6 a 60 m. Secciones romboidales y aristas perfectas. **Levitan de 3 a 8 cm** sobre las placas mediante campos magnéticos: sombra de contacto, distorsión del aire en el hueco, chispas ocasionales y zumbido.
3. **Juntas de resonancia** en las torres: anillos emisivos azul pálido que pulsan con el zumbido del Monolito.
4. **Iluminación:** luz fría y nítida (70 000 lux), sombras duras y reflejos especulares por todas partes.
5. **Partículas:** limaduras metálicas que se alinean con las líneas de campo cerca de las torres (partículas atraídas por un campo vectorial), chispas y destellos.
6. **Sonido:** zumbido de 60 Hz base con armónicos; cada torre añade su nota.

**Aproximación procedural actual:** `CrearTorreDeAleacion` (placa espejo + torre levitante con `LevitacionMagnetica` + junta emisiva) y campos de inducción.

### 2.4.2 La Fuente de Energía: el Monolito Central de Resonancia

1. **Estructura:** un **núcleo flotante de metal puro**: un bloque romboidal (cubo en el prototipo) que **gira sobre su eje** (30°/s con el ecosistema sano) **sin tocar ninguna superficie**, suspendido a 4 m y oscilando ±15 cm.
2. **Funcionamiento:** usa el **campo magnético natural del planeta** para generar energía cinética y electromagnética. Visualmente, líneas de campo (cintas de partículas) entran por sus polos y salen por su ecuador.
3. **Zumbido constante que estabiliza la ciudad:**
   - El Monolito emite el tono base y todas las torres resuenan con él (sus juntas pulsan en fase).
   - Si la integridad baja, el giro se ralentiza, el zumbido desafina y las torres **pierden levitación** (en la versión final, las torres más lejanas se posan con un golpe metálico).

### 2.4.3 Guardianes del nivel 4: linaje del Androide de Aleación (Metal)

**Atributos:** vida de 154 a 192, blindaje de 75 a 90 (el más alto entre los elementos primarios), velocidad ×0,90 (9,45 m/s), kit Filo de Aleación · Pulso Magnético · Égida de Aleación · Tormenta de Agujas.

#### Androide de Aleación (vanguardia, corta distancia)
- **Diseño realista:**
  - Humanoide de 2,8 m de **aleación plateada** con placas tectónicas en miniatura que se **reajustan** al moverse (se deslizan unas sobre otras).
  - Articulaciones con pistones y cables visibles.
  - **Levita a pocos centímetros** del suelo (ya implementado: `LevitacionMagnetica` en el modelo).
  - Los antebrazos se despliegan en filos.
- **Locomoción:** se desliza sin pasos (levitación), con una leve inclinación hacia la dirección del movimiento y placas que vibran.
- **Corta (Filo de Aleación, 3,4 m):** los antebrazos se abren en filos (0,15 s) y hace un corte en X.
- **Media:**
  - **Pulso Magnético** (radio de 5,85 m): extiende las palmas, crea un campo visible (anillos de distorsión) y **atrae a los humanos** hacia sí (empuje −9 m/s), con daño y ralentización al 75% durante 1,5 s.
  - **Égida de Aleación:** las placas se separan del cuerpo y orbitan como escudo (+35 de blindaje) con una descarga eléctrica corta.
- **Larga (Tormenta de Agujas, radio de 8,8 m):** se eleva 1 m (anticipación de 0,6 s) y agujas de aleación caen del cielo. **Bloqueo magnético** (enraizado de 1,2 s) y ralentización.
- **Núcleo:** el **giroscopio de resonancia** de la nuca.
- **Estabilidad:** alta en el suelo e inamovible con la Égida; los pulsos de agua cortocircuitan sus placas.
- **Sobrecarga:** tras la Tormenta, su campo se invierte 2 s y deja de levitar.
- **Sinergia:** el Monolito recarga sus placas.

#### Centinela de Resonancia (control, media distancia)
- **Diseño realista:** cuerpo de **anillos concéntricos** que giran alrededor de un núcleo zumbante, con placas flotantes que forman escudos y redes.
- **Corta:** golpe con un anillo lanzado como disco que vuelve.
- **Media (radio de 7,15 m):** el Pulso Magnético atrae hacia **el Monolito**, no hacia él: aleja a los humanos de sus objetivos.
- **Larga (radio de 10,7 m):** la Tormenta cae en patrón de rejilla.
- **Núcleo:** el núcleo central, cuando los anillos se separan. **Papel:** portero del Monolito.

#### Autómata de Precisión (artillería, larga distancia)
- **Diseño realista:** silueta estilizada con **articulaciones expuestas**, pistones finos y un **brazo-riel** largo. La lente frontal se estrecha al apuntar.
- **Corta (6,4 m):** disparo corto del brazo-riel. Anticipación: la lente se estrecha y suena un pitido (0,15 s).
- **Media (radio de 8,78 m):** el Pulso se proyecta como un disparo que crea el campo al impactar.
- **Larga (radio de 13,2 m):** marca un punto con un láser (0,6 s) y la Tormenta cae allí.
- **Núcleo:** la lente frontal. **Estabilidad:** baja.

### 2.4.4 Dinámica del nivel 4

- **Contra el humano:**
  - Las torres levitantes son coberturas, pero los campos de inducción entre ellas dañan y frenan.
  - El Androide atrae a los humanos fuera de su cobertura.
  - El Centinela los aleja del Monolito.
  - El Autómata remata desde lejos.
  - El **Traje Sísmico** resiste el Metal un 40%.
- **Contra el guardián (Androide + Centinela):** el Androide caza con atracciones y el Centinela mantiene limpio el Monolito.

---

## Nivel 5 — El Nexus Primordial (confluencia de los cuatro elementos)

### 2.5.1 Descripción visual realista, paso a paso

1. **Templo o santuario colosal** en el punto donde se tocan los cuatro biomas: cuatro brazos arquitectónicos, uno por bioma, que convergen en un patio central.
2. **Arquitectura:**
   - Columnas y bóvedas de **piedra translúcida**: blanquecina, con dispersión, dejando ver las vetas de energía internas.
   - **Canales de plasma** tallados en el suelo, que llevan agua luminosa hacia el centro.
   - **Enredaderas de madera petrificada** que trepan por las columnas, con restos de translucidez esmeralda.
   - **Filamentos metálicos conductores** incrustados en la piedra, por los que viajan pulsos plateados.
   - Las transiciones entre materiales siguen el Doc 06, 1.1.5.
3. **Gravedad reducida (×0,6):** saltos 1,67 veces más altos, péndulos más lentos y partículas suspendidas. Ya implementado.
4. **Auroras multicolores** en el cielo nocturno, en ciclo por los colores de los 4 elementos. Implementación provisional: `AuroraCielo`.
5. **Equilibrio naturaleza–tecnología:** ningún elemento domina. La composición es simétrica (cada cuadrante tiene su elemento) y el centro es neutro y blanco.
6. **Iluminación:** cielo nocturno (unos 2 lux de luna más las auroras). Las fuentes principales de luz son el Corazón del Planeta (blanco), las auroras (color cambiante) y los canales de plasma (azul).

**Aproximación procedural actual:** `CrearColumnaNexus` (columna translúcida, canal de plasma, enredadera petrificada y filamento conductor), cuadrantes elementales, gravedad de 0,6 y auroras.

### 2.5.2 La Fuente de Energía: el Corazón del Planeta (el Nexus)

1. **Estructura:** una **esfera masiva de energía pura** (en la versión final, 10 m de diámetro) que **flota** en el centro del santuario, sobre un anillo de piedra translúcida.
2. **Absorción de las cuatro corrientes:**
   - **Savia** (verde esmeralda) llega por las enredaderas.
   - **Plasma** (añil) por los canales.
   - **Calor magmático** (naranja) por grietas del suelo.
   - **Conductividad** (plata) por los filamentos.
   Cada corriente es un flujo de partículas visible que entra en la esfera.
3. **Unificación:** dentro de la esfera, las cuatro corrientes se trenzan y la esfera emite **luz blanca**. Late a 2 Hz con el ecosistema sano y se ralentiza al degradarse (ya implementado en `FuenteEnergiaViva.ActualizarVisual`).
4. **Fuente infinita que mantiene vivo el ecosistema global:** si se extrae, las cuatro corrientes se debilitan **y los cuatro biomas anteriores lo notan**. Narrativamente, es lo que se decide en los niveles 5 y 6.

### 2.5.3 Guardianes del nivel 5: Custodios del Nexus

Los tres **rotan** entre Madera, Agua, Fuego y Metal (tecla F para el jugador; la IA elige el elemento que más daño hace a los trajes del escuadrón cada 10–16 s). Al rotar, adoptan el kit, la vida máxima y la velocidad del elemento, y **conservan su porcentaje de vida**.

#### Eco Prismático (vanguardia, corta distancia)
- **Diseño realista:**
  - Cuerpo de **piedra translúcida** atravesado por cuatro corrientes de color.
  - Anillos orbitando el torso que **cambian de material** según la forma (corteza, nácar, obsidiana o aleación).
  - Transición de 0,4 s con un destello del nuevo color.
- **Animaciones:** las del linaje activo, con un velo prismático. En gravedad reducida, sus golpes incluyen un pequeño despegue.
- **Rotación:** anticipación de 0,2 s (los anillos se detienen), cambio y onda del nuevo color (`CrearAnilloOnda`).

#### Aethernum (control, media distancia)
- **Diseño realista:** la entidad prismática central, con los cuatro elementos girando en anillos alrededor de un núcleo vivo. Su vida es un 10% mayor.
- **Papel:** el portero del Corazón del Planeta.

#### Vigía Aural (artillería, larga distancia)
- **Diseño realista:** silueta alta y estilizada con un **velo de aurora** a la espalda, que cambia de color con su forma.
- **Papel:** ataca desde los brazos del santuario.

**Núcleo / punto débil común:** la **gema-nexo** del esternón, expuesta 1,5 s tras cada rotación. **Estabilidad:** alta en Metal y Madera, baja en Agua. **Sobrecarga:** dos rotaciones en menos de 10 s sobrecalientan la gema. **Sinergia:** el Corazón del Planeta les regenera maná un 25% más rápido.

### 2.5.4 Dinámica del nivel 5

- **Contra el humano:** cada Custodio puede ser el elemento que más daño hace al escuadrón, así que los humanos deben **variar sus trajes**. La gravedad reducida cambia las coberturas: se puede saltar a las columnas.
- **Contra el guardián (Eco Prismático + Aethernum):** el jugador rota de elemento según el traje de cada humano. Aethernum defiende el Corazón.

---

## Nivel 6 (final) — Convergencia Total

### 2.6.1 Descripción visual y ambiental, paso a paso

1. **Combinación de los 5 escenarios** alrededor del Corazón del Planeta abierto:
   - **Cuadrante suroeste: Bosque de los Susurros.** Corteza translúcida esmeralda y turquesa, raíces sobre vetas violetas, esporas.
   - **Cuadrante sureste: Abismos.** Plataformas de coral sintético y cristal marino sobre plasma añil, puentes que se mecen.
   - **Cuadrante noroeste: Caldera.** Obsidiana negra, ríos de magma y cristales de enfriamiento.
   - **Cuadrante noreste: Agujas.** Placas espejo y torres levitantes.
   - **Anillo central: Nexus.** Piedra translúcida, canales, enredaderas petrificadas y filamentos, **gravedad ×0,8** en todo el campo y **auroras**.
2. **Fronteras entre cuadrantes:** transiciones con mezcla por altura (Doc 06, 1.1.5). La corteza se petrifica al tocar la obsidiana, el plasma se evapora al tocar el magma con columnas de vapor, el metal se cubre de coral al tocar el agua y las raíces trepan por las torres.
3. **Cielo:** dividido. Cada cuadrante tiene la luz de su bioma y en el cénit se mezclan con las auroras.
4. **El Corazón del Planeta** recibe las cuatro corrientes **a la vez** y responde a cada extracción con pulsos visibles en los cinco escenarios.

**Implementado:** `BiomaPlaneta.ConvergenciaTotal` genera los 4 cuadrantes con su arquitectura y sus peligros, un anillo central de columnas del Nexus, gravedad de 0,8 y auroras.

### 2.6.2 Guardianes del nivel 6: todos los linajes reunidos

- **Contra el humano: 5 guardianes, uno de cada escenario anterior.**
  - Coloso Silvestre (Bosque).
  - Leviatán Abisal (Abismos).
  - Titán Ígneo (Caldera).
  - Androide de Aleación (Agujas).
  - Aethernum (Nexus, que rota).
  - Todos mantienen **más vida que los humanos y velocidad algo menor**.
  - Escalado del nivel 6: vida × 1,30, daño × 1,25 y blindaje × 1,5.
- **Despliegue:**
  1. Abanico de 5 guardianes a 11 m del Corazón, cada 30°.
  2. Los cuatro de vanguardia **cazan** desde cuatro direcciones: la IA los lleva al humano más cercano y cada uno se abre hacia un flanco distinto. En la Convergencia **todos** tienen afinidad en todo el campo; además, los peligros de cada cuadrante curan a cualquier guardián y castigan a los humanos, así que los guardianes ganan ventaja peleando sobre ellos.
  3. Aethernum **defiende el Corazón** y rota al elemento que más daño hace al escuadrón.
- **Corta, media y larga distancia, núcleos, estabilidad, sobrecarga y sinergias:** cada uno conserva su ficha (secciones 2.1 a 2.5). La sinergia ambiental se multiplica: en la Convergencia todos tienen afinidad (+15% de daño y +25% de maná).
- **Contra el guardián:** el jugador es **Aethernum** y combate junto al **Androide de Aleación**. Los demás linajes están **presentes** en la Convergencia (en la versión final, como figuras gigantes que sostienen el campo en el horizonte de su cuadrante: arte y cinemáticas, no combatientes). La regla de 2 guardianes contra 4 humanos se mantiene.

### 2.6.3 Dinámica del enfrentamiento definitivo

1. El escuadrón humano **no puede ganar por fuerza bruta** (5 guardianes con más vida). Tiene tres caminos:
   - Dividir a los guardianes atrayéndolos a cuadrantes ajenos, donde no reciben curación de su peligro.
   - Extraer el Corazón (victoria rápida, pero el final será "La Victoria Amarga" o "El Planeta Vacío").
   - Una victoria táctica, cazándolos uno a uno.
2. La integridad media de la campaña decide el **desenlace** (Doc 02).

---

## Gólem de Chimenea: sub-arquetipo Tierra-Forja (integrado)

**Integración:** es un **sub-arquetipo elemental de Tierra nacido en las forjas de la Caldera**.
- En el **nivel 3** ocupa la plaza de **artillería** del trío de la Caldera, con afinidad en ese escenario.
- En **Conquista y 4 vs 4** se puede jugar como linaje propio ("Tierra-Forja") con sus 3 variantes:
  - **Gólem de Chimenea** (vanguardia).
  - **Fuelle** (control).
  - **Mortero** (artillería).

### Diseño realista, paso a paso
1. **Cuerpo:** gólem de 2,9 m de **placas claras** de piedra de forja (gris arena, albedo 0,82; 0,76; 0,66) con textura de piedra caliza fundida y grietas finas.
2. **Remaches oscuros** (hierro ennegrecido de 3 a 5 cm) en filas a lo largo de las juntas de las placas, con una costra de óxido alrededor.
3. **Chimenea humeante** en el hombro izquierdo: cilindro de ladrillo refractario y hierro de 90 cm. Por su boca salen **llamas naranjas** (VFX de fuego en bucle) y **humo** que se adensa con el esfuerzo: gris claro en reposo y negro al encadenar habilidades.
4. **Antebrazo derecho con núcleo incandescente:** una esfera de 50 cm de piedra fundida visible a través de una rejilla de hierro. Pulsa al ritmo de la respiración y se pone al rojo blanco tras cada golpe.
5. **Escala y peso:** el centro de gravedad más bajo de todos los guardianes. Piernas cortas y gruesas.

En el prototipo, el modelo ya tiene placa frontal, remaches, chimenea con llama emisiva y núcleo en el antebrazo (`ConstructorModelosPersonajes`, caso Tierra).

### Animaciones y habilidades por rango
- **Locomoción:** pasos pesados con temblor de cámara en un radio de 10 m. La chimenea expulsa una bocanada en cada paso.
- **Corta (Puño de Yunque, 3,0 m; 6,0 m en el Mortero):**
  - Anticipación: el núcleo del antebrazo pasa al rojo blanco (0,15 s).
  - Ejecución: golpe como un martillo sobre un yunque, con chispas.
  - Seguimiento: el brazo humea.
- **Media:**
  - **Golpe de Escoria** (radio × 0,85: 4,97, 6,08 o 7,46 m según la variante): golpea el suelo con el antebrazo. Onda sísmica, ralentización al 70% durante 1,5 s y elevación de 6 m/s.
  - **Muralla de Escoria:** levanta un muro de roca de forja (5 × 3,2 m, 6 s) con +25 de blindaje temporal y +22 de vida.
- **Larga (Erupción de Chimenea, radio de 8,8, 10,7 o 13,2 m):**
  - Anticipación: la chimenea se llena de humo negro y ruge (0,6 s).
  - Ejecución: una columna de humo y llamas sale disparada hacia arriba, el suelo se parte en anillo y lanza a los enemigos (empuje de 8 m/s más 9 m/s hacia arriba), con ralentización al 50% durante 2 s.
  - Seguimiento: llueven brasas.
- **Núcleo / punto débil:** el núcleo incandescente del antebrazo (daño × 1,5 durante 1 s tras cada golpe).
- **Estabilidad:** la máxima del juego.
- **Sobrecarga:** 3 habilidades seguidas ahogan la chimenea 2 s y no puede atacar.
- **Sinergia:** en la Caldera y junto al Núcleo Térmico, +15% de daño.
- **Atributos:** vida de 176 a 220 (la más alta) y velocidad ×0,87 (9,14 m/s, la más baja). Sigue siendo ágil porque tiene la misma aceleración que todos.

---

## 2.7 Implementar la estabilidad, los puntos débiles y la sobrecarga (siguiente paso técnico)

Para que los textos de diseño se conviertan en mecánica, en este orden:

1. **Puntos débiles:**
   - Añade a cada modelo de guardián un `Collider` disparador (*trigger*) en la posición del núcleo, con un componente `PuntoDebil` (multiplicador × 1,5 y condición de exposición: siempre, al cargar, tras rotar...).
   - En `SistemaArmasYTrajeDestructor.ProcesarImpactoEnEntidad`, cuando el rayo atraviese ese disparador (usa `QueryTriggerInteraction.Collide` en un segundo `Raycast`), multiplica el daño y muestra un marcador de impacto crítico.
2. **Estabilidad:**
   - Añade a `SistemaSaludYTraje` un contador de "aguante" (*poise*): 100 en los guardianes de vanguardia, 70 en control y 50 en artillería. Cada golpe lo reduce según su tipo (explosivo ×3, cargado ×2) y se regenera a 25 por segundo.
   - Si llega a 0, el personaje **se tambalea** 0,8 s: entrada deshabilitada, animación de tambaleo y **cancela la anticipación** de cualquier habilidad en curso.
3. **Sobrecarga:** en `ControladorGuardianElemental`, tras la definitiva, aplica el estado de sobrecarga de cada linaje (Madera: sin regeneración 3 s; Fuego: −20% de daño y +30 de blindaje 3 s; Metal: sin levitación y velocidad ×0,85 durante 2 s; Agua: tras quedarse sin maná, velocidad ×1,1 y daño recibido ×1,2 durante 2 s; Tierra: tras 3 habilidades en menos de 6 s, sin atacar 2 s). Reutiliza `SistemaEstadosAlterados` para los multiplicadores.
