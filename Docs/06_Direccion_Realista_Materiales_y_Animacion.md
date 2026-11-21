# 06 · Dirección realista: materiales, anatomía, entorno y animación fluida

> **Alcance de este documento.** Es la guía de producción para pasar de la geometría de bloques actual (primitivas generadas por código) a un juego de aspecto realista con animaciones fluidas. La geometría, las texturas y las animaciones realistas **no se pueden generar por código**: hay que producirlas en herramientas de arte (Blender, Maya, ZBrush, Substance 3D Painter y Designer, Houdini para VFX, captura de movimiento o animación por fotogramas clave) e integrarlas en Unity siguiendo estos pasos.
>
> El código ya está preparado para recibirlas. Personajes: `ModeloVisualPersonaje`. Explosión del traje: `efectoVisualExplosionTraje`. Escenarios: funciones `GeneradorBiomas.Crear*`. Efectos: `FabricaEfectosVisuales`. Las rutas de sustitución están en `Docs/04_Arte_Biomas_VFX_e_Interfaz.md`.

---

## 0. Preparación técnica común (hacer una sola vez, antes de cualquier asset)

**Paso 0.1 — Canal de render.**
- Para máximo realismo, usa **HDRP** (High Definition Render Pipeline): dispersión bajo la superficie real (*Diffusion Profiles*), sistema de agua, niebla volumétrica, nubes volumétricas y *Screen Space Global Illumination*.
- Si el objetivo incluye equipos modestos, usa **URP**. En ese caso la dispersión y el agua se aproximan con *Shader Graph* (se indica en cada sección).
- El código del proyecto funciona con los dos: `MaterialesProcedurales` detecta el canal.

**Paso 0.2 — Escala.** 1 unidad de Unity = 1 metro, sin excepciones.
- Humano: 1,80 m. Su `CharacterController` mide 1,8 m de alto y 0,4 m de radio.
- Guardián: entre 2,6 y 3,0 m. Su `CharacterController` mide 2,7 m de alto y 0,75 m de radio.
- Puerta o paso estrecho jugable: 1,2 m de ancho como mínimo para humanos y 2 m para guardianes.
- Escalón: 0,4 m como máximo para humanos y 0,6 m para guardianes (`stepOffset`).
- Antes de exportar, modela siempre con un maniquí de referencia de 1,80 m en la escena de Blender o Maya.

**Paso 0.3 — Densidad de texel.**
- Personajes: 1024 px/m en primera persona (arma y manos) y 512 px/m en tercera persona.
- Escenario: 512 px/m en superficies cercanas al jugador y 256 px/m en fondos.
- Comprueba la densidad con un mapa de damero antes de texturizar.

**Paso 0.4 — Mapas PBR por material.**
- Base Color (sin luz ni sombras horneadas).
- Normal (OpenGL).
- Máscara empaquetada:
  - HDRP: R = Metalness, G = AO, B = Detail mask, A = Smoothness.
  - URP: Metallic con Smoothness en el canal alfa + AO.
- Height, para paralaje o teselado.
- Emission en HDR.
- Mapas adicionales según el material: *Thickness* (dispersión), *Flow* (magma y agua) y *Curvature* (desgaste).

**Paso 0.5 — Iluminación base de cada escenario.**
1. Un *Directional Light* físico (lux reales: 80 000 a pleno sol, 20 000 nublado y unos 2 en el Nexus nocturno con auroras).
2. Un *Sky and Fog Volume* con *Physically Based Sky*.
3. *Volumetric Fog* activada, con la densidad de cada escenario (ver Doc 08).
4. *Reflection Probes* por zona (como mínimo una por cada base y otra en la fuente).
5. *Light Probes* en una rejilla de 4 m a la altura de personajes.
6. *Adaptive Probe Volumes* en HDRP o URP 2023+.

**Paso 0.6 — Postprocesado.** Exposición automática limitada, *Bloom* de umbral alto (1,1) para que solo brillen las emisiones reales, *Tonemapping* ACES, *Color Adjustments* por escenario, *Motion Blur* bajo (solo objeto), *Depth of Field* desactivado en juego y activo en cinemáticas, y *Film Grain* leve.

---

## 1.1 De bloques a diseño realista: materiales, anatomía, escala y entorno

Cada elemento sigue el mismo método en **5 fases**, y cada fase empieza cuando termina la anterior:

| Fase | Qué se hace | Herramienta | Entregable |
|---|---|---|---|
| A. Bloqueo | Se sustituye la primitiva por una silueta de baja resolución a escala real y se valida en el juego (coberturas, alturas, líneas de visión). | Blender o ProBuilder | Malla de bloqueo con colisión |
| B. Alta resolución | Esculpido de formas medias y microdetalle (fibras, poros, fracturas, remaches). | ZBrush o Blender Sculpt | Malla de alta resolución |
| C. Retopología + UV | Malla jugable con LODs (LOD0 a LOD3) y UVs sin estirar, siguiendo la dirección del detalle. | Blender, RizomUV | Malla de juego + LODs |
| D. Horneado + textura | Normal, AO, curvatura y grosor horneados desde la alta resolución. Materiales por capas. | Substance Painter o Designer | Juego de mapas PBR |
| E. Shader + integración | Material en Unity (Lit, StackLit o Shader Graph), colisión simplificada, LOD Group, probes y prueba de rendimiento. | Unity | Prefab final |

### 1.1.1 Madera y flora (Bosque de los Susurros Bioluminiscentes)

**Fibras longitudinales**
1. En la alta resolución del tronco, esculpe la corteza con pinceles direccionales que sigan el eje de crecimiento, de raíz a copa, nunca en horizontal.
2. Haz que las fibras se abran alrededor de las ramas y nudos, como un río alrededor de una piedra. Usa una máscara de flujo pintada en Blender (atributo de vértice) para orientar el pincel.
3. Espaciado: surcos de 2 a 5 mm de ancho y 1 a 3 mm de profundidad en corteza joven, y grietas de 1 a 3 cm en corteza madura.
4. Desenvuelve las UV con la V alineada al eje del tronco. Así la textura de veta, que es repetible, nunca se estira en diagonal.
5. En Substance Designer, crea un material procedural de "corteza fibrosa" con un generador de vetas (Directional Noise + Warp) y tres parámetros: densidad de fibra, apertura de grieta y madurez.

**Poros**
1. Añade una capa de microdetalle de 0,3 a 0,8 mm con ruido celular invertido en el mapa *Height*.
2. Hornéalo como *Detail Normal* (HDRP: *Detail Map*; URP: segunda normal en Shader Graph) con una repetición de 8× a 16×. Así el poro es nítido de cerca sin gastar textura principal.
3. Oscurece ligeramente los poros con la AO del detalle y súbeles la rugosidad (0,85).

**Microfisuras**
1. En el mapa de curvatura horneado, aísla las concavidades finas (*cavity*) y úsalas como máscara.
2. Dentro de esa máscara, pinta fisuras ramificadas de 0,2 a 1 mm (pincel de grieta en Substance Painter) que sigan las fibras en un 80% de los casos y las crucen en un 20%.
3. En las fisuras: albedo un 20% más oscuro y rugosidad de 0,9. Si son "fisuras de savia" (bioluminiscentes), dales además una emisión débil (ver translucidez).

**Corteza translúcida y dispersión bajo la superficie**
1. Modela la corteza como una **capa exterior** de 2 a 4 cm sobre un **núcleo interior** de savia luminosa. No hace falta geometría doble: se simula con el mapa *Thickness*.
2. Hornea el *Thickness* (grosor) desde la alta resolución: las crestas de corteza son finas (más claras en el mapa) y los valles gruesos.
3. **HDRP:**
   - Material *Lit* con *Material Type = Translucent* o *Subsurface Scattering*.
   - Crea un *Diffusion Profile* llamado "CortezaTranslucida": dispersión verde esmeralda (R 0,1, G 0,9, B 0,6) con radio de 6 mm.
   - Asigna el mapa *Thickness*.
4. **URP:**
   - En Shader Graph, calcula la transmisión como `pow(saturate(dot(V, -(L + N·0,4))), 4) × (1 − thickness) × colorSavia`.
   - Súmala a la emisión. Es la aproximación de "iluminación envolvente" (*wrap lighting*).
5. **Pulso de savia:** en el mismo shader, una máscara vertical de franjas (`frac(posY × 0,25 − tiempo × 0,6)`) modula la emisión entre 0,4 y 1,2 nits relativos. Con eso la savia sube por el tronco a 0,6 m/s. El Árbol Madre usa el mismo pulso, sincronizado.
6. **Degradación:** conecta la integridad del ecosistema (de 0 a 1, ya existe en `GestorEquilibrioEcosistema`) a un parámetro global del shader (`Shader.SetGlobalFloat("_IntegridadEcosistema", x)`). Con integridad baja, la translucidez se apaga y el albedo vira a gris.

**Raíces sin enterrar**
1. Constrúyelas como **curvas** en Blender: un *spline* con radio decreciente, de 40 cm en el tronco a 3 cm en la punta.
2. Que se enrosquen sobre las vetas de mineral siguiendo su relieve. Usa *Shrinkwrap* sobre la malla de la veta con un desplazamiento de 2 cm, para que parezca que las abraza.
3. En el punto donde la raíz entra al suelo, haz una **mezcla por color de vértice**: el último 15% de la raíz se mezcla con el material del suelo (tierra violeta y musgo), para que no haya un corte duro.
4. Aplícales el mismo material de corteza (fibras longitudinales) más una capa de musgo en la cara superior: máscara de "normal hacia arriba" (`saturate(N.y)`) con ruido.

**Hojas con nervaduras y bordes orgánicos**
1. Modela 4 o 5 hojas maestras en alta resolución con la nervadura central, las secundarias en espina de pez y el borde irregular (dientes, mordiscos y roturas). Las hojas son **anchas, de 0,6 a 1,2 m**: funcionan como paneles solares naturales.
2. Hornéalas a *texture cards* de 2048² con alfa recortado, normal (nervaduras en relieve), translucidez (las nervaduras son más gruesas y dejan pasar menos luz) y un *Thickness*.
3. Material de doble cara con *Translucency*. En la cara inferior la luz pasa con un tono turquesa.
4. **Viento:** un *Vertex Shader* con tres frecuencias.
   - Tronco: 0,1 Hz, amplitud de 0,5°.
   - Rama: 0,4 Hz, ponderada por el color de vértice rojo.
   - Hoja: 2 a 3 Hz con aleteo, ponderada por el verde.
5. **"Paneles solares":** en la cara superior añade un brillo especular anisótropo leve (rugosidad de 0,35 en la dirección de la nervadura) y un patrón celular hexagonal casi invisible en la emisión. Que reaccione al sol: más emisión cuanto mayor sea `dot(N, L)`.

**Transición a minerales incrustados**
1. Las vetas de mineral orgánico (violeta) **afloran** del suelo: son mallas propias que atraviesan el terreno.
2. Mezcla corteza → mineral con una **máscara de altura**. Allí donde la raíz toca la veta, el material de la raíz se cristaliza (sube la suavidad hasta 0,8 y el albedo vira a violeta) en una franja de 10 a 20 cm. La transición se calcula con `heightBlend(alturaCorteza, alturaMineral, contraste 0,2)`.
3. El mineral tiene dispersión propia (perfil "MineralVioleta": rojo y azul altos, verde bajo) y emisión en sus fracturas. La emisión de la veta "respira" en contrafase con la savia: cuando una sube, la otra baja.

### 1.1.2 Agua y plasma (Abismos de las Mareas de Plasma)

**Densidad del líquido**
1. El plasma es **más denso que el agua**: las olas son más lentas y altas, la espuma es escasa y espesa y las salpicaduras caen antes.
2. En la simulación de olas (sistema de agua de HDRP en Unity 2023+/6, o Gerstner en Shader Graph): velocidad de fase un 30% menor que la del agua y "chop" (picudez) de 0,6.
3. Las salpicaduras de los VFX llevan gravedad ×1,3 y menos gotas (unas 60 en lugar de 150) de mayor tamaño.

**Absorción de luz según la profundidad (Beer-Lambert)**
1. Calcula la profundidad del agua por píxel: `profundidad = profundidadEscena − profundidadSuperficie`.
2. Aplica la transmitancia por canal: `T = exp(−coeficienteAbsorcion × profundidad)`, con coeficientes RGB de (0,45; 0,12; 0,06) por metro. El rojo desaparece a 2 m, el verde a 8 m y el azul añil persiste hasta 20 m.
3. Color final bajo la superficie: `lerp(colorProfundo añil (0,05; 0,07; 0,25), colorEscenaRefractada, T)`.
4. Añade una **bioluminiscencia en profundidad**: una emisión azul que aumenta con la profundidad (plancton) y sube a la superficie en vetas animadas.

**Refracción**
1. Desplaza las UV de la textura de escena según la normal de la superficie: `uvRefractada = uvPantalla + normal.xz × 0,03 × saturate(profundidad)`. En zonas poco profundas casi no hay distorsión.
2. Para evitar que "entren" objetos que están por delante de la superficie, vuelve a comprobar la profundidad en la UV desplazada y, si es menor, usa la UV original.

**Micro-ondulaciones y ondas de gran escala**
1. **Gran escala:** suma de 4 ondas de Gerstner con longitudes de 24, 15, 9 y 5,5 m y amplitudes de 0,9; 0,5; 0,25 y 0,12 m, en direcciones a ±35° de la dirección de la marea. La marea cambia de dirección lentamente porque la arrastran **dos lunas**: modula la dirección con dos senos de periodo 90 y 140 s.
2. **Media escala:** dos mapas normales repetibles que se desplazan en direcciones distintas (velocidades de 0,05 y 0,03).
3. **Micro-ondulaciones:** un tercer mapa normal a escala ×8 que solo se ve cerca (se atenúa con la distancia), más ondas de impacto locales cuando un personaje pisa o una habilidad golpea el agua. Usa un *render texture* de ondas, que se dibuja con partículas.
4. **Puentes colgantes:** leen la altura de las olas de gran escala en sus puntos de anclaje y se mecen con ellas. En el prototipo ya existe un balanceo provisional: `BalanceoPorOleaje`.

**Descargas de estática azul-blanca**
1. Las nubes cargadas tienen un *Volumetric Cloud* oscuro con relámpagos internos: emisión de pulsos en la densidad de la nube.
2. Las descargas al suelo o al agua se hacen con **VFX Graph**:
   - Un rayo principal: tira de 8 a 14 segmentos con desplazamiento aleatorio fractal.
   - Entre 2 y 4 ramas secundarias.
   - Una vida de 0,08 a 0,15 s con 2 o 3 parpadeos.
   - Color azul-blanco (0,7; 0,85; 1) × 20 de intensidad.
3. Cada descarga enciende una *Point Light* de 0,1 s de alcance 30 m y deja una marca de vapor en el agua.
4. En la superficie, "arcos de estática" pequeños saltan entre crestas de ola. Usa partículas de cinta (*strip*) de 0,5 a 2 m que nacen en la normal de las crestas más altas.

**Brillos especulares en zonas húmedas**
1. Todo material del escenario recibe una **máscara de humedad**: un *decal* de proyección en zonas de salpicadura, más una máscara de altura respecto al nivel del agua.
2. Donde está húmedo: albedo × 0,7 (más oscuro), suavidad 0,9 y, en HDRP, *Coat* (capa transparente) de 0,5. Así aparecen los reflejos especulares del cielo cargado.
3. Gotas: un mapa normal de gotas animado (desplazamiento de UV hacia abajo) en superficies verticales húmedas.

### 1.1.3 Fuego, magma y roca volcánica (Caldera de las Forjas Magmáticas)

**Obsidiana negra: fractura concoidea y bordes vítreos**
1. Esculpe la fractura **concoidea**: superficies curvas en forma de concha, con ondas concéntricas (rizos de percusión) que irradian del punto de impacto. Pincel de "capas concéntricas" o *Booleans* de esferas aplastadas.
2. Los **bordes** son afilados y finos (1 a 3 mm de chaflán) y dejan pasar la luz. Dales el mismo truco de dispersión que la corteza, con un perfil marrón oscuro y un radio de 1 mm. En contraluz, los bordes brillan en marrón-ámbar.
3. Material: albedo casi negro (0,02), suavidad de 0,92 a 0,96, especular por defecto (F0 de 0,04 a 0,05) y una normal con ondas concoideas muy suaves. **Lo que la hace realista es el reflejo**: necesita *Reflection Probes* correctas y *SSR* activado.
4. Polvo de ceniza en las caras horizontales: máscara de "normal hacia arriba" con albedo gris (0,15) y rugosidad de 0,8, interrumpida por el viento (ruido direccional).

**Ríos de magma naranja brillante y carmesí**
1. Geometría: un canal con fondo cóncavo y una superficie de magma un 10% por debajo del borde.
2. Shader con **flowmap**: pinta la dirección del flujo en Houdini o con una herramienta de flowmap y avanza dos fases de UV desfasadas medio ciclo, para evitar saltos.
3. **Corteza enfriada:** un ruido de Voronoi que fluye con el magma decide dónde hay costra (umbral 0,55). Donde hay costra, el material es roca oscura agrietada. Donde no hay, magma emisivo.
4. **Temperatura:** la emisión sigue un gradiente de cuerpo negro.
   - Núcleo amarillo-blanco: unos 1200 °C, emisión × 40.
   - Naranja: × 15.
   - Carmesí: × 4, en los bordes y bajo la costra fina.
   - Negro: costra.
   - El gradiente se indexa con `temperatura = 1 − (costra × 0,8 + distanciaAlBorde × 0,4)`.
5. **Burbujas:** partículas que se hinchan, estallan y dejan un anillo de corteza que se rompe.
6. Luz: tira de *Area Lights* (HDRP) o varias *Point Lights* naranjas sin sombra a lo largo del río, con la intensidad modulada por el flujo.

**Distorsión térmica del aire**
1. Pon un **volumen de distorsión** sobre cada río y sobre el Núcleo Térmico: partículas grandes y transparentes con un shader de refracción que desplaza la textura de escena con una normal de ruido ascendente (velocidad de 0,4 m/s).
2. Intensidad: la máxima (0,02 de desplazamiento de UV) justo encima del magma, decreciendo con la altura hasta desaparecer a 6 m.
3. Debe verse incluso a través de los personajes: el efecto se dibuja después de lo opaco y antes de lo transparente.

**Ascuas incandescentes**
1. VFX Graph: entre 200 y 2000 partículas por zona, de 2 a 8 mm, con vida de 3 a 7 s.
2. Fuerzas: empuje térmico ascendente (+1,5 m/s²), *curl noise* para trayectorias espirales y una velocidad límite de 2 m/s.
3. Color por edad: amarillo, naranja, rojo oscuro y apagado (con un parpadeo final).
4. Colisión con el suelo: se apagan y dejan un punto rojo que se enfría en 1 s.

**Formaciones cristalinas por enfriamiento rápido**
1. Ubicación: las paredes de los cañones, en franjas horizontales (estratos donde el mineral se enfrió de golpe).
2. Geometría: grupos de prismas hexagonales y agujas de 5 a 60 cm que crecen **perpendiculares a la pared** con ±25° de desviación.
3. Material: cristal translúcido ámbar con refracción leve, suavidad de 0,95 y emisión débil en la base (calor residual). Con luz rasante del magma proyectan cáusticas: añade un *decal* de cáusticas animado bajo cada grupo.
4. Mezcla con la roca: la base de cada cristal se funde con la obsidiana mediante una máscara de altura de 3 cm.

**Columnas de vapor presurizado**
1. Respiraderos en el suelo y en los generadores del Núcleo Térmico.
2. Ciclo de presión de 6 a 10 s:
   - Acumulación (1,5 s): silbido, temblor de la tapa y unas pocas volutas.
   - Expulsión (0,8 s): un chorro vertical a 25 m/s que se abre en un cono de 12° y llega a 15 m de altura.
   - Disipación (3 s): la columna se ensancha y la arrastra el viento.
3. Render: partículas de humo con *flipbooks* de 8×8 simulados en Houdini (iluminación de 6 direcciones para que la luz del magma los ilumine desde abajo) y niebla volumétrica local (*Local Volumetric Fog* con una textura 3D de densidad).

### 1.1.4 Metal y tecnología (Agujas de Aleación Magnética y equipo humano)

**Placas tectónicas artificiales**
1. Losas hexagonales o romboidales de 4 a 12 m que **encajan con precisión**. La junta mide 2 cm de ancho y 5 cm de profundidad, con un bisel de 3 mm.
2. Usa una **hoja de recortes** (*trim sheet*) de 4096² con juntas, biseles, remaches y paneles, y aplícala con UVs a lo largo de los bordes. Así una sola textura sirve para cientos de placas.
3. Superficie: aleación plateada pulida como **espejo gigante**.
   - Metalness 1, suavidad de 0,95 con variación sutil de ±0,03 (micro-ondulación del pulido).
   - Albedo plateado (0,91; 0,92; 0,93).
   - El cielo alienígena debe reflejarse: *Planar Reflection Probe* (HDRP) sobre la llanura, o SSR + *Reflection Probe* de alta resolución en URP.

**Desgaste**
1. Hornea la **curvatura** y úsala como máscara de bordes. En las aristas convexas, el pulido se desgasta (suavidad 0,7) y aparece un tono más cálido de aleación base.
2. En las concavidades (juntas): suciedad oscura y polvo de óxido azulado (no rojizo: es una aleación alienígena). Albedo × 0,6 y suavidad 0,4.
3. **Ley de uso:** las zonas por donde se camina (centro de las placas y rutas entre torres) tienen un pulido más mate, en forma de sendero. Pinta una máscara de tránsito a gran escala.

**Micro-arañazos**
1. Mapa de detalle con 3 capas de arañazos lineales en direcciones dominantes (tráfico) y otras aleatorias.
2. Cada arañazo es una línea de 0,1 a 0,5 mm en la normal **y** en la suavidad (más mate dentro del arañazo).
3. En HDRP usa material **anisotrópico** (*StackLit*, o *Lit* con *Anisotropy* de 0,6). El reflejo se estira perpendicular a los arañazos y produce las "estelas" de luz típicas del metal cepillado.

**Reflejos especulares del entorno**
1. Una *Reflection Probe* por cada grupo de torres, con proyección de caja ajustada al volumen.
2. Una *Planar Reflection* para el suelo espejo, limitada a 1/2 resolución y actualizada cada 2 fotogramas.
3. *Specular occlusion* desde la AO horneada, para que el metal en recovecos no refleje el cielo.

**Levitación magnética a pocos centímetros del suelo**
1. Las torres levitan entre **3 y 8 cm**. No basta con elevarlas: el jugador solo lo cree si ve:
   - Una **sombra de contacto** nítida que se ensancha cuando la torre sube (*Contact Shadows* en HDRP, o un decal de sombra en URP).
   - Una **distorsión del aire** en el hueco (refracción débil, como el calor pero sin movimiento ascendente: ondas concéntricas).
   - **Chispas o arcos** ocasionales entre la torre y la placa (cada 4 a 9 s, 0,05 s de duración).
   - Un **zumbido** espacializado de 60 a 120 Hz que sube de tono cuando la torre baja.
2. Movimiento: oscilación senoidal de periodo 2 a 4 s y amplitud de 1 a 3 cm, más una precesión lenta (inclinación de 0,3°). En el prototipo ya existe `LevitacionMagnetica`, con altura de 0,25, amplitud de 0,06 y frecuencia de 0,35 en las torres.

**Remaches, pistones, cables y articulaciones mecánicas**
1. **Remaches:** cúpulas de 8 a 20 mm en filas regulares (paso de 4 a 8 cm), con un anillo de suciedad alrededor y un brillo en la cúpula. Van en el *trim sheet* como normal + AO, salvo los de primer plano (arma y hombreras), que se modelan.
2. **Pistones:** cilindro exterior mate, vástago interior pulido cromado (suavidad 0,98) con un anillo de grasa y un fuelle de goma opcional. En la animación, el vástago **siempre** sigue la distancia entre los dos anclajes. Se implementa con *Aim Constraint* + *Position Constraint* (Animation Rigging), sin animarlo a mano.
3. **Cables:** se cuelgan con un *Line Renderer* o una malla con hueso por segmento y simulación de cadena (*Damped Transform* en cascada). Cada 30 cm, un conector con brida.
4. **Articulaciones:** rótulas visibles con juego mecánico. Las placas exteriores se solapan como escamas y **se deslizan unas sobre otras** al flexionar, con huesos auxiliares controlados por *Twist Correction* y *Multi-Rotation Constraints*.

### 1.1.5 Transiciones y Nexus Primordial

**Mapas de mezcla (blend masks) suaves y coherentes**
1. Toda frontera entre dos materiales usa **mezcla por altura**: `peso = saturate((alturaB + máscara − alturaA) / contraste)`. Así, en la frontera musgo → piedra, el musgo ocupa primero las grietas (zonas bajas) y después las crestas.
2. Pinta la máscara base con **color de vértice** (R, G, B y A = hasta 4 capas) en las mallas del terreno y de la arquitectura del Nexus, más un ruido triplanar de ruptura para evitar curvas demasiado limpias.
3. **Coherencia con el entorno:** las máscaras obedecen a la física del lugar.
   - Agua: sube por capilaridad entre 20 y 40 cm por encima de los canales de plasma. Allí la piedra se oscurece y brilla.
   - Calor: ennegrece y cristaliza la piedra entre 0,5 y 1 m alrededor de las grietas de magma.
   - Metal: deja vetas conductoras que siguen la dirección del filamento.
   - Madera petrificada: aparece donde las enredaderas tocan la piedra.

**Gravedad reducida: parábolas de salto, caída y péndulos**
1. En el Nexus la gravedad es **×0,6**. Ya está implementado: `ControladorMovimientoAssaultCube.MultiplicadorGravedadAmbiente = 0,6`, que fija `GeneradorBiomas`.
2. Efectos físicos (con la misma velocidad de salto, 7,8 m/s):
   - Altura máxima = v²/2g → ×1,67 (de 1,45 a 2,41 m).
   - Tiempo hasta la cima = v/g → ×1,67 (de 0,37 a 0,62 s).
   - Tiempo total en el aire → ×1,67.
   - Alcance horizontal → ×1,67.
   - Periodo de un péndulo (lámparas, cadenas, cables) = 2π√(L/g) → ×1,29: todo oscila más despacio.
3. **Animación:** el bucle de "en el aire" dura más. Crea una variante *Float* con brazos que se abren ligeramente para equilibrar y piernas recogidas. Se mezcla según el tiempo de vuelo: por encima de 0,5 s pasa a *Float*.
4. **VFX:** partículas con gravedad ×0,6. El polvo y las ascuas quedan suspendidos más tiempo y las gotas de plasma caen como en cámara lenta. Los cables y las cadenas usan amortiguación más baja.

**Auroras multicolores**
1. Cintas de aurora en la cúpula celeste. Shader de cielo con 2 o 3 capas de ruido desplazado en franjas verticales (cortinas), con una paleta que recorre los colores elementales:
   - Verde esmeralda (Madera).
   - Añil (Agua).
   - Naranja (Fuego).
   - Plata azulada (Metal).
2. Movimiento: las cortinas ondulan con un periodo de 20 a 40 s y cambian de matiz en ciclos de 2 min. En el prototipo ya existe una versión provisional, `AuroraCielo`.
3. **Interacción con la luz:** las auroras son la principal luz de relleno del Nexus. Añade un *Directional Light* secundario sin sombras cuyo color sigue la media del color de las auroras (cálculo cada 0,5 s) con intensidad de 0,3 lux. Así la piedra translúcida toma su tono.

**Luz, sombras, rugosidad y partículas en cada transición**

| Transición | Luz | Sombras | Rugosidad | Partículas |
|---|---|---|---|---|
| Madera → piedra translúcida | La savia se ve a través de ambas: la dispersión se continúa (mismo *Diffusion Profile* en la frontera) | Suaves, por el musgo | 0,8 → 0,35 | Esporas que se posan en la piedra |
| Piedra → canal de plasma | La piedra húmeda refleja el plasma; luz azul en movimiento (cáusticas) | Cáusticas proyectadas | 0,35 → 0,05 (húmeda) | Gotas y niebla baja |
| Piedra → grieta de magma | La emisión naranja tiñe la piedra hasta 1 m | Sombras recortadas por la luz rasante del magma | 0,35 → 0,6 (requemada) | Ascuas y distorsión térmica |
| Piedra → filamento metálico | Reflejos especulares finos y pulsos de energía que recorren el filamento | Muy definidas | 0,35 → 0,1 | Chispas en los nodos |
| Todas → Corazón del Planeta | Luz blanca pulsante que domina (2 Hz cuando el ecosistema está sano) | Sombras que "respiran" con el pulso | Sin cambio | Partículas de los 4 elementos orbitando |

---

## 1.2 Fluidez total de animaciones: mezcla, cinética y continuidad

### 1.2.0 Arquitectura de animación en Unity

1. **Rig:**
   - Humanos: *Humanoid* (*Avatar*), para reutilizar animaciones entre los 5 arquetipos.
   - Guardianes: *Generic* con su propio esqueleto (más huesos: placas, raíces, aletas y chimenea), o *Humanoid* con huesos adicionales si su proporción lo permite.
2. **Animator Controller** con estas capas:
   1. *Base — Locomoción* (cuerpo completo).
   2. *Parte superior — Armas* (máscara de avatar de cintura hacia arriba, mezcla *Override*).
   3. *Aditiva — Retroceso e impactos* (*Additive*).
   4. *Cara y respiración* (*Additive*).
   5. *Habilidades* (cuerpo completo, con peso que entra y sale).
3. **Parámetros** que debe escribir un `ControladorAnimacionPersonaje` (script nuevo, que lee `ControladorMovimientoAssaultCube` y los sistemas de combate): `VelocidadPlano`, `VelocidadX`, `VelocidadZ` (locales), `VelocidadVertical`, `EnSuelo`, `TiempoEnAire`, `Inclinacion`, `GiroAngular`, `Apuntando`, `Recargando`, `IndiceArma`, triggers `Disparar`, `Golpear`, `Habilidad1/2/3`, `Recibir`, `Morir`, `Revivir`.
4. **Animation Rigging** (paquete oficial): *Two Bone IK* para pies y manos, *Multi-Aim* para cabeza y columna, *Damped Transform* para movimiento secundario y *Override Transform* para la pelvis.
5. **Regla de oro:** ninguna transición es instantánea. La mezcla mínima es de **0,08 s** y la habitual, de 0,15 a 0,25 s. En el código, toda la jugabilidad está separada de la animación (lee intenciones de `EntradaPersonaje`), así que la animación nunca "congela" la jugabilidad ni al revés.

### 1.2.1 Desplazamiento

**Estados y velocidades.** El código actual no tiene tecla de esprint: el personaje acelera de 0 a su velocidad máxima (10,5 m/s × multiplicadores) en unos 0,33 s. El árbol de locomoción elige la marcha **por la velocidad real**, de modo que caminar y trotar aparecen al arrancar, frenar o moverse con el stick a media inclinación. La animación se ajusta con *stride warping*, que se explica más abajo:

| Estado | Humano (m/s) | Guardián (m/s) | Clip base | Duración de ciclo |
|---|---|---|---|---|
| Reposo activo | 0 | 0 | Respiración, guardia | 3–4 s |
| Caminar | 1,5–3 | 1,3–2,6 | Caminar 8 direcciones | 1,1 s |
| Trotar | 3–6 | 2,6–5,2 | Trote 8 direcciones | 0,75 s |
| Esprintar | 6–11,6 | 5,2–9,7 | Carrera 8 direcciones | 0,6 s |
| Nado / plasma profundo | 2,5 | 2,2 | Brazada + patada | 1,6 s |

1. **Árbol de mezcla 2D direccional libre** (*2D Freeform Directional*) con `VelocidadX` y `VelocidadZ` para cada marcha. Los tres árboles (caminar, trotar y esprintar) se anidan en un árbol 1D por `VelocidadPlano`. Las direcciones intermedias se mezclan sin "patinar" porque todos los clips comparten **fase de pie**: el pie izquierdo toca el suelo al 0% y el derecho al 50%. Hay que marcarlo en los clips con *Foot IK* y con curvas `PieIzquierdo` y `PieDerecho`.
2. **Transferencia de peso:**
   - Al **arrancar**: clip de arranque de 0,3 a 0,4 s (el peso va primero a la pierna de apoyo, el torso se inclina 8 a 12° hacia la dirección y el primer paso es corto). Se reproduce si la velocidad pasa de menos de 0,5 a más de 3 m/s en menos de 0,2 s.
   - Al **frenar**: clip de parada elegido según la **fase del pie** en ese momento (dos variantes, con el pie izquierdo o el derecho delante). El cuerpo sobrepasa el punto de parada 5 a 10 cm y vuelve (asentamiento).
   - En el código, el frenado de 24 m/s² hace que una frenada desde la carrera dure 0,45 s: el clip de parada debe durar eso.
3. **Inclinación corporal (lean):**
   - `inclinacionLateral = clamp(velocidadAngular(grados/s) × velocidadPlano × 0,004, −15°, 15°)`. Se aplica a columna (60%) y pelvis (40%) con un *Override Transform* aditivo, suavizado con un muelle de constante 12.
   - Inclinación frontal: con la aceleración, el torso se adelanta hasta 10° al acelerar y se retrasa hasta 8° al frenar.
4. **Giros:**
   - **Giro en el sitio:** si la diferencia entre el cuerpo y la cámara supera 70° estando quieto, clip de giro de 90° o 180° con desplazamiento real de los pies (no se desliza).
   - **Giro en carrera** a más de 120°: clip de pivote de 180° (el pie de apoyo planta y el cuerpo rota sobre él en 0,35 s).
   - Por debajo de esos umbrales, el giro lo absorben la inclinación y el árbol direccional.
5. **Ajuste de zancada (stride warping):** si el personaje se mueve más rápido que el clip (por los multiplicadores de traje o habilidad), en lugar de acelerar la reproducción (que produce "piernas de dibujo animado") se **alarga la zancada** escalando la distancia de los pies respecto a la pelvis en la dirección del movimiento (IK), hasta un ×1,3. A partir de ahí se acelera la reproducción.
6. **IK de pies y pelvis sobre superficies irregulares o inclinadas:**
   1. Para cada pie, lanza un rayo de 0,5 m por encima del tobillo hacia abajo, de 1,2 m de longitud.
   2. Si impacta, coloca el objetivo de IK del pie en el punto de impacto + altura del tobillo, y orienta el pie a la normal (limitado a ±35°).
   3. Desplazamiento de la pelvis: `pelvis.y −= max(diferenciaPieIzquierdo, diferenciaPieDerecho)` (baja la pelvis la distancia que necesita el pie más bajo), suavizado a 10 m/s. Así la pierna del lado alto se flexiona y la otra se estira, sin quedar ningún pie en el aire.
   4. **Pendientes:** el torso se inclina hacia la pendiente un 30% del ángulo (subiendo, hacia delante). La zancada se acorta un 15% al subir y se alarga un 10% al bajar.
   5. El peso de la IK se desactiva en el aire (`EnSuelo = false`) y vuelve en 0,1 s al aterrizar.
7. **Aterrizaje:** clip aditivo de absorción proporcional a la velocidad de caída. El código ya emite `AlAterrizar(velocidad)`. Por encima de 9 m/s, aterrizaje con rodilla flexionada de 0,25 s. En gravedad reducida, la absorción es menor.
8. **Nado y desplazamiento en plasma:** en zonas de plasma profundo (Abismos) se activa la sub-máquina *Nado*. La transición entra en 0,3 s al superar el agua el 60% de la altura del personaje. El cuerpo pasa a horizontal y la cabeza se mantiene fuera con *Look At*. La densidad del plasma hace las brazadas lentas y amplias.
9. **Gravedad reducida:** ver 1.1.5. Además, en el Nexus el salto añade un giro corporal suave (rotación de 5 a 10° del torso en la cima) y el aterrizaje es más largo y suave.
10. **Movimiento secundario encadenado en capas:**

| Elemento | Técnica | Parámetros |
|---|---|---|
| Capas y faldones de tela | Unity Cloth o simulación de tela de terceros (MagicaCloth), con colisionadores en piernas | Rigidez 0,6, amortiguación 0,2, colisión con cápsulas de las piernas |
| Cabello | Cadenas de huesos con *Damped Transform* (o físicas de muelle) | Amortiguación 0,4, retardo de 2–3 fotogramas en la punta |
| Placas de armadura | Hueso por placa con muelle angular limitado (±3–8°) | Rebotan al aterrizar y al recibir golpes |
| Remaches colgantes, cadenas y cables | Cadena de *Damped Transform* + gravedad | Retardo creciente hacia el extremo; gravedad ×0,6 en el Nexus |
| Bandoleras y carcaj | Hueso con muelle vertical | Golpean el cuerpo en cada paso (pico en el contacto del pie) |
| Raíces, aletas y chimenea (guardianes) | Cadenas con *Damped Transform* y ruido | Ondulación propia en reposo; la chimenea expulsa humo según el esfuerzo |

El orden de evaluación es **cuerpo → placas → cadenas → tela**, para que cada capa reaccione a la anterior y nada vibre.

### 1.2.2 Disparos y ataques: las 5 fases

Toda acción ofensiva, a distancia o cuerpo a cuerpo, pasa por **encare → carga → ejecución → retroceso o reacción → recuperación**. Los tiempos de cada arma están en el Doc 09. Aquí se explica la técnica común.

1. **Encare (apuntado):**
   - Hombros: la capa superior gira la columna en 3 huesos (30%, 30% y 40%) hacia el punto de mira con *Multi-Aim Constraint*. La pelvis sigue a la cámara con un retardo de hasta 45°; si se supera, se dispara el giro en el sitio.
   - Brazos y manos: *Two Bone IK* lleva la mano de soporte al punto de agarre del arma (un `Transform` llamado `AgarreSoporte` dentro de cada prefab de arma). La mano principal está fija a la empuñadura.
   - Vista: en primera persona, la cámara sale del hueso de la cabeza estabilizado (sin el bamboleo completo: el 20%). En tercera persona, la cabeza mira al objetivo con *Look At* limitado a ±70°.
   - Transición desde la locomoción: 0,12 s. Al integrar las animaciones, apuntar debe limitar la velocidad al rango de trote (6 m/s) para que el árbol de esprint nunca se mezcle con el encare. Es un cambio pequeño en `ControladorMovimientoAssaultCube`: un multiplicador más, como los que ya existen para trajes y estados.
2. **Carga o tensión:**
   - Arco: tensar la cuerda de 0,4 a 0,8 s (el hombro retrocede, la escápula se junta y el codo trasero sube hasta la línea de la oreja). La flecha vibra al pasar de 0,6 s (tensión máxima).
   - Mandoble y maza: levantar el arma (anticipación) en 0,15 a 0,35 s. El peso pasa a la pierna trasera y el torso rota en contra del golpe.
   - Rifle: contener la respiración (el balanceo de mira baja un 70% en 0,5 s).
   - Arsenal biomecánico: las placas del antebrazo se abren y el núcleo brilla de 0,2 a 0,4 s.
   - **Señal visual:** el color de la carga crece con el tiempo (emisión del arma o del núcleo), para que el rival pueda leerla.
3. **Ejecución:**
   - Disparo: el proyectil o el rayo sale en el **fotograma de contacto** marcado con un *Animation Event* (`EventoDisparo`), nunca antes de que el arma esté alineada.
   - Golpe cuerpo a cuerpo: ventana activa de 2 a 6 fotogramas en la que una cápsula de impacto (en el arma, no en el cuerpo) detecta enemigos. Al conectar, **pausa de impacto** (*hitstop*) de 40 a 80 ms en atacante y víctima, y un temblor de cámara proporcional.
4. **Retroceso o reacción:**
   - Armas de fuego: clip **aditivo** de retroceso (hombro atrás 3–6 cm, cañón arriba 2–6°) más un muelle de recuperación (constante 20, amortiguación 0,6). Las ráfagas suman retrocesos sin cortar el anterior. En el código, `AplicarRetroceso` ya sube la mirada; la animación debe coincidir con esos grados.
   - Cuerpo a cuerpo: si el golpe impacta contra algo duro (escudo o placa), **rebote**: el arma vuelve un 20% de su arco y el atacante retrocede medio paso.
   - Víctima: reacción direccional (4 direcciones × 3 intensidades), aditiva sobre lo que esté haciendo, con un peso según el daño. Por encima de la estabilidad del personaje, tambaleo completo (ver Doc 08, "estabilidad").
5. **Recuperación:**
   - Ventana de **cancelación** al 60–70% del clip: a partir de ahí, moverse, saltar u otra acción interrumpe la recuperación con una mezcla de 0,15 s.
   - Sin entrada, el clip termina y vuelve a la guardia (0,2 s) o a la locomoción en curso (la capa superior baja su peso en 0,25 s mientras las piernas siguen el árbol de locomoción). Así se puede caminar mientras se recupera el arma.

### 1.2.3 Poderes y habilidades

Cada habilidad (de humanos y de guardianes) se anima en **tres tiempos** obligatorios:

1. **Anticipación legible (0,25–0,6 s, según la potencia):**
   - Postura clara y reconocible a distancia (silueta). Por ejemplo, el guardián planta un pie y levanta los brazos para un golpe sísmico.
   - Señal visual: un círculo o cono en el suelo (*decal*) con el color del elemento, que se llena durante la anticipación. Su radio es el radio real de la habilidad: en el código, `radioEfecto` × el multiplicador de cada habilidad.
   - Señal sonora: un sonido de carga ascendente.
   - En el prototipo, el efecto se lanza al instante. Al integrar la animación, mueve la llamada a `LanzarHabilidad*` al *Animation Event* del fotograma de impacto y reserva el maná y el enfriamiento al **inicio** de la anticipación. Así no se puede pulsar varias veces durante la carga, y si el guardián es interrumpido (tambaleo), la habilidad se pierde.
2. **Ejecución potente (2–8 fotogramas):**
   - Descarga, barrera, onda, impulso o proyección con **masa visible**: el cuerpo empuja y el suelo responde (polvo, grietas, salpicadura).
   - La cámara recibe impulso y temblor: `SacudidaCamara` ya existe y escala con la distancia.
   - La luz cambia: un destello del color del elemento (`LuzEfimera`).
   - El impulso físico se aplica en ese fotograma a los objetivos (`AplicarEmpuje`).
3. **Seguimiento (follow-through), de 0,4 a 0,8 s:**
   - **Inercia residual:** el cuerpo sigue la dirección del esfuerzo y vuelve con un sobrepaso de 5 a 10%.
   - **Partículas que persisten y se asientan:** el polvo cae, las ascuas se apagan y el agua se escurre.
   - **Asentamiento gradual:** las capas secundarias (placas, cadenas, raíces) siguen moviéndose 0,3 s más.
   - **Regreso fluido a la postura activa:** mezcla de 0,2 s a la guardia o a la locomoción. A partir del 50% del seguimiento se permite moverse (cancelación de piernas), y a partir del 80%, lanzar otra habilidad.

**Tabla de tiempos de referencia** (aplicable a los kits de guardianes de los Docs 08 y 03):

| Habilidad | Anticipación | Ejecución | Seguimiento | Cancelación posible desde |
|---|---|---|---|---|
| Golpe básico | 0,15 s | 3 fotogramas | 0,25 s | 60% |
| Habilidad 1 (E, media distancia) | 0,3 s | 4 fotogramas | 0,45 s | 70% |
| Habilidad 2 (R, utilidad) | 0,25 s | 4 fotogramas | 0,4 s | 70% |
| Definitiva (Q, larga o área) | 0,6 s | 8 fotogramas | 0,8 s | 85% |

### 1.2.4 Lista de comprobación de fluidez (QA)

1. Graba en vídeo a 60 fps cada transición de la máquina de estados y revisa fotograma a fotograma que no haya saltos de pose, pies deslizando ni manos atravesando el arma.
2. Mantén pulsadas a la vez combinaciones absurdas (esprintar + recargar + saltar + habilidad) y verifica que siempre hay un estado válido y nunca una "pose T" ni un congelamiento.
3. Activa `Animator.cullingMode = CullUpdateTransforms` en los bots lejanos y comprueba que, al volver a verlos, no saltan.
4. Comprueba que la fase de los pies se conserva al cambiar de marcha: el pie que estaba en el aire sigue en el aire.
5. En el Nexus, verifica el bucle *Float* y que el aterrizaje no se adelanta (gravedad ×0,6).
