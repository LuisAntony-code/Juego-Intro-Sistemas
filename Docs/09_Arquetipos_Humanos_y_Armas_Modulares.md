# 09 · Los 5 arquetipos humanos, sus armaduras y las 5 armas modulares

> **Estado: implementado.** Los 5 arquetipos (`DatosArquetipo.cs`) y las 5 armas modulares (`DatosArmaModular.cs`) son el sistema humano jugable: `SistemaCombateArquetipo.cs` ejecuta el ataque básico, el clic derecho y las 6 habilidades (Z X C de la armadura, E Q F del arma) para el jugador y para los bots; la personalización, el HUD (6 ranuras), el briefing y la pantalla de controles los muestran; y cada arquetipo y cada arma tienen modelo 3D de Blender (Doc 10). El apartado 3.3 conserva el plan de sustitución como registro de cómo se hizo; los antiguos trajes y armas de fuego (`DatosTraje`, `DatosArma`, `SistemaArmasYTrajeDestructor`) se eliminaron.
>
> **Escala de distancias** (común a armaduras y armas): **corta** de 0 a 5 m · **media** de 5 a 20 m · **larga** a más de 20 m.
> **Regla de atributos:** todo arquetipo humano tiene **menos vida** que cualquier guardián (de 100 a 125, frente a 140–220) y es **un poco más rápido** (×0,95 a ×1,10, frente a ×0,87–0,92).
> **La regla de explosión se mantiene:** al llegar a 0, la armadura del arquetipo **explota** (en el Ciborg, el núcleo magenta; en los demás, la célula de energía extractora del cinturón).

---

## 3.1 Los 5 arquetipos humanos y sus habilidades de armadura

Cada arquetipo tiene **3 habilidades de armadura**: corta (tecla Z), media (tecla X) y larga (tecla C). Son independientes de las 3 habilidades del arma (Doc 09, 3.2), así que cada combinación de arquetipo + arma suma 6 habilidades.

### 3.1.1 Cazador / Explorador Forestal

**Atributos:** vida 100 · blindaje 40 · velocidad ×1,10 (el más rápido) · estabilidad baja.

**Apariencia realista, paso a paso**
1. **Gambesón** (túnica acolchada) **verde bosque**:
   - Lino exterior con trama visible en el mapa de detalle y algodón acolchado interior.
   - **Costuras en rombos** de 6 cm, modeladas como hundimientos de 4 mm en la alta resolución, con hilo claro.
   - **Ribetes claros** de cuero curtido en cuello, mangas y bajo.
   - Simulación de tela en el faldón del gambesón (rigidez alta: es acolchado).
2. **Yelmo cerrado** con **visor anguloso** de una sola ranura horizontal en V que **solo deja ver los ojos**:
   - Acero mate con pátina.
   - Remaches en la cresta.
   - Dentro, los ojos con *eye shader* (refracción de córnea) y un leve reflejo de la luz.
3. **Hombreras redondeadas de acero pulido:** suavidad de 0,85, arañazos finos y abolladuras leves. Dos láminas solapadas que se deslizan al levantar el brazo (huesos de corrección).
4. **Malla en los brazos:** cota de anillas como textura de anillos en relieve (normal + alfa en los bordes), anisotrópica. En los primeros planos, malla real con *instancing*.
5. **Guanteletes y botas altas de cuero marrón oscuro:** cuero con poros y arrugas en las articulaciones, desgaste más claro en las puntas y hebillas de latón.
6. **Equipo:**
   - **Arco curvo** de madera clara en la mano derecha (en reposo, colgado del hombro si usa otra arma).
   - **Carcaj** con flechas a la espalda: plumas con movimiento secundario.
   - **Machete** a la espalda.
   - **Espada recta** en la cintura.
   - Cada arma tiene su anclaje (*socket*) en el esqueleto.

**Animación de personalidad:** postura agachada y alerta, cabeza que explora (*Look At* aleatorio cada 2–4 s en reposo), pasos silenciosos rodando del talón a la punta, y **ráfagas cortas de aceleración**.

**Habilidad corta — Paso de maleza (enfriamiento de 7 s)**
1. *Anticipación (0,1 s):* se agacha y apoya una mano en el suelo.
2. *Ejecución:* esquiva rodando o deslizándose 5 m en la dirección del movimiento (o hacia atrás si está quieto). Ignora el Enraizado y la Ralentización durante el gesto (inmunidad de 0,6 s) y deja una nube de hojas y fibras que **bloquea la línea de visión** 1,5 s.
3. *Seguimiento (0,25 s):* termina en guardia baja con una mano en el suelo y se incorpora. Se puede disparar a partir del 50%.
4. *Uso:* escapar del Puño de Corteza o del Pulso Magnético que atrae.

**Habilidad media — Camuflaje de fibras (enfriamiento de 16 s)**
1. *Anticipación (0,3 s):* tira de un cordón del gambesón y las fibras se erizan.
2. *Ejecución:* durante 6 s, el gambesón adopta el color y el patrón del entorno.
   - Shader de camuflaje: mezcla con la textura de la escena detrás del personaje (*scene color*), con un borde de refracción.
   - Los bots y la IA rival pierden su rastro a más de **12 m**.
   - El jugador rival ve una silueta distorsionada.
   - Disparar o recibir daño rompe el camuflaje.
3. *Seguimiento:* el patrón "se seca" de vuelta al verde en 0,5 s.
4. *Uso:* llegar a la fuente sin ser visto por el control y la artillería.

**Habilidad larga — Estabilizador de mira (enfriamiento de 14 s)**
1. *Anticipación (0,4 s):* se arrodilla y apoya el codo en la rodilla (con IK de la mano de soporte).
2. *Ejecución:* durante 5 s, la dispersión del arma es mínima, el balanceo de mira desaparece y el daño a más de 20 m sube un 25%. Mientras dura, se mueve al 40% de su velocidad.
3. *Seguimiento:* se levanta en 0,3 s.
4. *Uso:* castigar al Oráculo de la Estática o al Autómata de Precisión desde lejos.

### 3.1.2 Paladín / Templario del Alba

**Atributos:** vida 115 · blindaje 70 · velocidad ×0,98 · estabilidad alta.

**Apariencia realista, paso a paso**
1. **Complexión robusta y porte noble:** hombros anchos y postura erguida. Proporción de 7,5 cabezas.
2. **Cabello oscuro corto**, **rapado a los lados**: cabello en tarjetas con alfa y degradado al rapado con *tangent shader*.
3. **Placas blancas desgastadas:**
   - Esmalte blanco sobre acero, con desconchones en los bordes que muestran el acero (curvatura + ruido).
   - Suciedad en las juntas.
   - Suavidad del esmalte: 0,55.
4. **Bordes e incrustaciones doradas:** oro con metalness 1 y suavidad 0,8, con el desgaste dorado más brillante en las zonas de roce.
5. **Emblema solar o brújula dorada en el pecho:** relieve real (malla), rosa de 16 puntas con sol central. Pulsa de luz al usar habilidades.
6. **Hombreras articuladas multicapa:** 4 láminas superpuestas por hombro, con huesos de corrección que las deslizan.
7. **Guanteletes blancos y dorados** con dedos articulados (3 falanges de placa por dedo).
8. **Capa clara sobre túnica gris:** capa de lana clara con simulación de tela y colisión con piernas y espada; túnica gris de lino visible entre las placas.
9. **Mandoble** (espada larga a dos manos) con **empuñadura y guarda doradas**: hoja de acero pulido con canal central y empuñadura de cuero con alambre dorado.

**Animación de personalidad:** pasos firmes y medidos, la capa ondea con retraso y en reposo apoya las dos manos en el pomo del mandoble.

**Habilidad corta — Aura de repulsión (enfriamiento de 10 s)**
1. *Anticipación (0,25 s):* clava la espada o planta el arma en el suelo y el emblema solar brilla.
2. *Ejecución:* onda de luz dorada de **4,5 m** que **empuja 10 m/s** a los enemigos y anula los estados alterados que el Paladín tenga encima.
3. *Seguimiento (0,4 s):* la onda se disipa en motas doradas y él recoge el arma.
4. *Uso:* romper el cuerpo a cuerpo de una vanguardia (Coloso o Titán).

**Habilidad media — Égida refractaria (enfriamiento de 15 s)**
1. *Anticipación (0,3 s):* levanta el brazo libre y la luz se condensa en un hexágono.
2. *Ejecución:* barrera frontal de luz dorada (3 × 2,5 m) a 2 m delante de él durante 4 s.
   - Bloquea proyectiles y habilidades de área **frontales**.
   - **Refracta** el 30% del daño bloqueado hacia el atacante como un rayo dorado.
   - Los aliados pueden cubrirse tras ella.
3. *Seguimiento:* la barrera se agrieta y se rompe en esquirlas de luz (0,3 s).
4. *Uso:* aguantar la definitiva de un guardián mientras el escuadrón extrae.

**Habilidad larga — Faro de vanguardia (enfriamiento de 22 s)**
1. *Anticipación (0,5 s):* alza el mandoble al cielo y el emblema dispara un haz vertical.
2. *Ejecución:* coloca un faro de luz a **hasta 30 m** (en el punto apuntado) durante 8 s.
   - Los aliados en 6 m del faro ganan un 15% de velocidad y regeneran 4 de blindaje por segundo.
   - Los guardianes dentro quedan **revelados** (contorno visible a través de las paredes).
3. *Seguimiento:* el haz se estrecha y se apaga.
4. *Uso:* marcar el punto de reunión antes de asaltar la fuente.

### 3.1.3 Caballero Bastión / Guardia de Hierro

**Atributos:** vida 125 (el humano más resistente) · blindaje 90 · velocidad ×0,95 (el humano más lento, aun así más rápido que cualquier guardián) · estabilidad muy alta.

**Apariencia realista, paso a paso**
1. **Complexión muy pesada:** tronco ancho, cuello grueso y una proporción de 7 cabezas.
2. **Cabello castaño y barba cerrada:** barba con tarjetas densas y pelo facial de mapa; las cejas también.
3. **Armadura completa de acero oscuro ornamentado:**
   - Acero pavonado (albedo 0,12, suavidad 0,6).
   - **Grabados en relieve** (follaje y escenas) modelados en la alta resolución y horneados.
   - El brillo de los grabados está en las crestas desgastadas.
4. **Hombreras altas y escalonadas:** 3 escalones que suben hacia el cuello y protegen la cabeza de lado.
5. **Correas de cuero cruzadas en el pecho:** cuero grueso con costuras y hebillas de hierro. Las correas tienen huesos y se tensan al girar el torso.
6. **Faldón de tela clara desgastada** bajo las placas: lino crudo deshilachado en el bajo (alfa), sucio. Simulación de tela.
7. **Grebas robustas** con rodilleras de placa.
8. **Maza con púas** en la mano derecha y **escudo ovalado** de acero con **relieves barrocos** en el brazo izquierdo (volutas, una cabeza de león y el borde reforzado).

**Animación de personalidad:** pasos pesados que transmiten peso (pequeño temblor de cámara cercano), el escudo siempre ligeramente adelantado y giros lentos del torso.

**Habilidad corta — Anclaje sísmico (enfriamiento de 12 s)**
1. *Anticipación (0,3 s):* levanta el pie y lo pisa con fuerza.
2. *Ejecución:* una onda de 4 m ralentiza a los enemigos al 60% durante 2 s. Durante 4 s, el Caballero queda **anclado**: inmune al empuje, a la atracción magnética y al tambaleo.
3. *Seguimiento:* las grietas brillan y se cierran (0,5 s).
4. *Uso:* no ser movido por el Pulso Magnético ni por la Corriente de Marea.

**Habilidad media — Absorción de impacto (enfriamiento de 16 s)**
1. *Anticipación (0,2 s):* afianza el escudo con las dos manos.
2. *Ejecución:* durante 3 s, el escudo absorbe el 80% del daño **frontal** y acumula "carga".
3. *Seguimiento:* al terminar, el escudo **descarga** la carga en un golpe de 6 m frontal (daño = 40% de lo absorbido).
4. *Uso:* aguantar la Ráfaga Ígnea y devolverla.

**Habilidad larga — Intercepción de artillería (enfriamiento de 20 s)**
1. *Anticipación (0,4 s):* gira el escudo hacia un aliado (indicador en arco hasta 25 m).
2. *Ejecución:* salta (con impulso de embestida) hacia la posición del aliado designado, o hacia un punto a 25 m si no hay aliado. Durante 4 s, **intercepta el 50% del daño** que recibe ese aliado.
3. *Seguimiento:* aterrizaje con rodilla y escudo al suelo (0,4 s).
4. *Uso:* proteger al Tirador del Autómata de Precisión o del Heraldo de Ceniza.

### 3.1.4 Tirador / Cazador de Reliquias

**Atributos:** vida 100 · blindaje 45 · velocidad ×1,03 · estabilidad media.

**Apariencia realista, paso a paso**
1. **Complexión atlética.** Cabello castaño **peinado hacia atrás** (tarjetas con brillo de pomada) y **bigote grueso**.
2. **Abrigo largo azul oscuro y marrón:**
   - Lana azul con forro marrón.
   - **Solapas de cuero**.
   - Faldones con simulación de tela (abiertos detrás para correr).
   - Bordes gastados.
3. **Hombrera metálica plateada izquierda**, con grabado y remaches.
4. **Bandoleras** cruzadas con cartuchos de latón (instanciados, con brillo individual).
5. **Cinturón ancho con hebilla metálica** grande y bolsas de cuero.
6. **Rodilleras reforzadas** y **botas altas de cuero con correas**.
7. **Armas:** **rifle largo de precisión con latón dorado** (cañón alargado, mira telescópica con lente que refleja), **dos armas largas a la espalda** y una **empuñadura en la cadera** (revólver o pistola).

**Animación de personalidad:** postura relajada y confiada, comprueba la mira en reposo y se recoloca el abrigo.

**Habilidad corta — Propulsión de retroceso (enfriamiento de 8 s)**
1. *Anticipación (0,1 s):* apunta el arma hacia abajo y en sentido contrario al deseado.
2. *Ejecución:* dispara un cartucho de propulsión: **salto de 7 m** en la dirección contraria al cañón, con humo y fogonazo. Daña 15 a quien esté en el cono del cartucho (2 m).
3. *Seguimiento:* aterriza rodando y recupera la mira (0,3 s).
4. *Uso:* escapar del cuerpo a cuerpo o alcanzar una plataforma (excelente en la gravedad reducida del Nexus).

**Habilidad media — Disipador térmico (enfriamiento de 15 s)**
1. *Anticipación (0,3 s):* tira de una palanca del rifle y las aletas del disipador se despliegan.
2. *Ejecución:* durante 5 s, el arma **no se calienta ni recarga**: cargador infinito y +20% de cadencia. El disipador expulsa vapor.
3. *Seguimiento:* las aletas se recogen con un siseo; si el arma usa cargador, queda recargada.
4. *Uso:* ventana de daño sostenido contra un guardián con la Égida activa.

**Habilidad larga — Visor telemétrico (enfriamiento de 18 s)**
1. *Anticipación (0,4 s):* baja la lente telemétrica del sombrero o la mira.
2. *Ejecución:* durante 6 s:
   - Revela a los guardianes a menos de **60 m** (contorno).
   - Muestra la **distancia** y el **punto débil** de cada guardián (marcador sobre su núcleo).
   - Los disparos al punto débil hacen ×1,5 (requiere el sistema de puntos débiles del Doc 08, 2.7).
3. *Seguimiento:* sube la lente.
4. *Uso:* coordinar al escuadrón contra la artillería guardiana.

### 3.1.5 Ciborg / Vanguardia Biomecánica

**Atributos:** vida 110 · blindaje 60 · velocidad ×1,00 · estabilidad alta.

**Apariencia realista, paso a paso**
1. **Implantes cibernéticos en la mitad izquierda del rostro:**
   - **Lente óptico rojo en la frente**: emisivo, con anillo de enfoque animado.
   - **Placa craneal plateada** atornillada, con la juntura de piel inflamada (dispersión rojiza en el borde).
   - El resto de la cara, piel con shader de dispersión.
2. **Implantes en torso y brazos:** **placas metálicas plateadas texturizadas** (patrón de micro-hexágonos en la normal) que se integran en la carne con bordes de silicona.
3. **Hombrera ancha con detalles dorados:** filetes de latón en los bordes.
4. **Segmentos tipo espina en la espalda:** 7 vértebras externas que se flexionan con la columna (un hueso por segmento).
5. **Núcleo circular luminoso magenta o rosado en el pecho:** emisión HDR y pulso cardíaco (1 Hz en reposo, 2,5 Hz en combate). Es el **punto que explota** al llegar a 0.
6. **Sistemas integrados** listos para armamento acoplado: puertos en los antebrazos y hombros (anclajes del Arsenal Biomecánico).

**Animación de personalidad:** movimientos precisos con micro-correcciones mecánicas (paradas de 1 fotograma en articulaciones), escaneo con el ojo y la lente enfocando.

**Habilidad corta — Sobrecarga del núcleo (enfriamiento de 14 s)**
1. *Anticipación (0,3 s):* el núcleo pasa de rosa a blanco y las vértebras se iluminan en cadena.
2. *Ejecución:* durante 4 s, +30% de daño cuerpo a cuerpo y +20% de velocidad de ataque. Al terminar, pulso de 3,5 m que daña 25.
3. *Seguimiento:* vapor por las vértebras y el núcleo vuelve a rosa (0,4 s). Durante 2 s **recibe un 15% más de daño** (sobrecalentamiento).
4. *Uso:* explosión de daño contra el Titán con el caparazón enfriado.

**Habilidad media — Escudo de frecuencia (enfriamiento de 16 s)**
1. *Anticipación (0,2 s):* el ojo rojo parpadea y las placas vibran.
2. *Ejecución:* durante 3 s, esfera de interferencia de 3 m:
   - **Anula los efectos de control** (Enraizado, Ralentización, atracción) sobre él y sus aliados dentro.
   - Reduce un 40% el daño elemental recibido.
3. *Seguimiento:* la esfera parpadea y se desvanece.
4. *Uso:* atravesar la Tormenta de Agujas o el Espinar sin quedar bloqueado.

**Habilidad larga — Enlace de rastreo (enfriamiento de 20 s)**
1. *Anticipación (0,3 s):* la lente apunta y marca un objetivo (a menos de 40 m).
2. *Ejecución:* durante 8 s, el objetivo queda **marcado**: visible para todo el escuadrón, recibe un 10% más de daño y sus habilidades se ven con 0,2 s más de anticipación en el HUD humano.
3. *Seguimiento:* la marca se disuelve en píxeles.
4. *Uso:* concentrar el fuego del escuadrón en la vanguardia guardiana.

---

## 3.2 Las 5 armas modulares

### 3.2.0 Combinación con cualquier arquetipo (cómo se integra)

1. **Anclajes estándar en el esqueleto humanoide:** `Mano_Derecha_Arma`, `Mano_Izquierda_Soporte`, `Espalda_Arma_Larga`, `Cadera_Derecha`, `Antebrazo_Izq_Puerto` y `Antebrazo_Der_Puerto`. Cada arma define en su prefab el `AgarrePrincipal` y el `AgarreSoporte`. La IK lleva la mano de soporte allí.
2. **Capas de animación:**
   - Cada arma trae su **Animator Override Controller**, que sustituye la capa superior: guardia, encare, ataque o disparo y recarga.
   - La capa base de locomoción es **la misma para todos**.
   - Así **cualquier** arquetipo usa **cualquier** arma sin animaciones duplicadas.
3. **Ajuste por arquetipo:** cada arquetipo aplica una **pose aditiva** encima (Paladín más erguido, Cazador más agachado, Bastión con el escudo adelantado si lo lleva).
4. **Visibilidad:** el arma equipada va en la mano. Las demás que el arquetipo lleva por diseño (el arco del Cazador, el rifle del Tirador) van en sus anclajes de espalda o cadera.
5. **Arsenal Biomecánico en no-ciborgs:** se monta como **guantelete exoesquelético** sobre el antebrazo (un "kit de acople" con correas y placas). En el Ciborg, se acopla directamente a sus puertos sin piezas extra.
6. **Fluidez:** cambiar de arma lleva 0,35 s (enfunda 0,15 s + desenfunda 0,2 s) con mezcla de capa. Esprintar baja el arma a una pose de carrera en 0,15 s.

Las 3 habilidades de arma usan las teclas **E** (corta), **Q** (media) y **F** (larga; en los guardianes, F sigue siendo rotar elemento), con el mismo sistema de enfriamientos que ya tienen las habilidades de arma del prototipo.

### 3.2.1 Arco Compuesto

**Base:** daño 45 con carga completa (25 sin carga), alcance de 70 m, flecha con trayectoria balística (gravedad 9,8 m/s² × 0,6), carga de 0,8 s y carcaj de 20 flechas.
- **Animación:**
  - Encare: flecha del carcaj a la cuerda (0,3 s).
  - Carga: tensar, con el codo trasero a la altura de la oreja y la escápula juntándose (0,8 s).
  - Disparo: soltar con un temblor de la cuerda (secundario).
  - Retroceso: el brazo del arco sube 2° y los dedos se abren.
  - Recuperación: nueva flecha (0,35 s) y vuelta a guardia o locomoción.
- **Corta — Disparo a quemarropa y retroceso (8 s):**
  - Anticipación: gira el arco en horizontal (0,15 s).
  - Ejecución: dispara a menos de 3 m (daño 40 + empuje de 8 m/s al objetivo) y **salta 4 m hacia atrás** por el impulso.
  - Seguimiento: aterriza en guardia con una nueva flecha.
- **Media — Flecha de amarre (12 s):**
  - Anticipación: flecha con cuerda (0,3 s).
  - Ejecución: hasta 20 m. Si impacta a un guardián, lo **amarra** al suelo: ralentización al 50% durante 3 s y no puede usar el desplazamiento de sus habilidades. Si impacta una superficie, el arquero **se impulsa** hacia ella.
  - Seguimiento: la cuerda se tensa y vibra.
- **Larga — Tiro de núcleo (18 s):**
  - Anticipación: carga de 1,5 s con la flecha brillando.
  - Ejecución: flecha perforante a más de 20 m sin caída, que **atraviesa** hasta 3 objetivos y hace ×2,5 si golpea un **punto débil**.
  - Seguimiento: estela de luz y sonido de chasquido.

### 3.2.2 Mandoble / Espada Larga

**Base:** daño 30 por tajo, alcance de 2,6 m, combo de 3 golpes (horizontal, diagonal y estocada) en 1,4 s y bloqueo con clic derecho (−60% de daño frontal).
- **Animación:**
  - Encare: guardia alta con las dos manos.
  - Golpe: anticipación de 0,15 s (peso atrás), ejecución de 4 fotogramas activos con pausa de impacto de 60 ms y seguimiento de 0,3 s (el peso termina delante).
  - Al impactar contra algo duro (placas de Metal u obsidiana), **rebote** del 20%.
  - Encadenado: el siguiente golpe del combo empieza desde la pose final del anterior (sin volver a la guardia).
- **Corta — Barrido ascendente (7 s):**
  - Anticipación: baja la espada a la cadera (0,2 s).
  - Ejecución: tajo vertical ascendente de 3 m con daño 45 y **lanzamiento** (empuje de 7 m/s hacia arriba).
  - Seguimiento: la espada termina sobre la cabeza.
- **Media — Onda de filo (11 s):**
  - Anticipación: carga con la hoja brillando (0,4 s).
  - Ejecución: tajo horizontal que lanza una **onda de energía** de 4 m de ancho que avanza 15 m (daño 35 a todo lo que cruza).
  - Seguimiento: la onda se desvanece en partículas y la espada queda extendida.
- **Larga — Estocada de impulso (16 s):**
  - Anticipación: retrocede la hoja (0,3 s).
  - Ejecución: **embestida de 12 m** en línea recta con la espada delante (daño 50 al primer objetivo y empuje). Los guardianes con estabilidad baja se tambalean.
  - Seguimiento: frenada larga con el pie delantero (0,4 s).

### 3.2.3 Maza con Púas y Escudo

**Base:** daño 34 por golpe, alcance de 2,2 m, combo de 2 golpes en 1,2 s, escudo con bloqueo frontal del 75% y aturdimiento corto de 0,2 s al golpear.
- **Animación:**
  - Encare: escudo adelantado y maza alta.
  - Golpe: anticipación de 0,25 s (más pesada que la espada) y ejecución descendente con pausa de impacto de 80 ms.
  - Seguimiento: la maza rebota en el suelo si falla.
  - Bloqueo: el escudo absorbe con retroceso aditivo del brazo.
- **Corta — Golpe de yunque (8 s):**
  - Anticipación: alza la maza con las dos manos (0,35 s).
  - Ejecución: golpe al suelo de 3,5 m con daño 40 y **aturdimiento de 0,8 s** (anula la anticipación de habilidades de los guardianes).
  - Seguimiento: polvo y chispas, y la maza se levanta con esfuerzo.
- **Media — Lanzamiento con cadena (12 s):**
  - Anticipación: suelta la cabeza de la maza, unida por una cadena, y la hace girar (0,3 s).
  - Ejecución: la lanza hasta 12 m. Si impacta, **arrastra** al objetivo 5 m hacia el portador (los guardianes de vanguardia, solo 2 m).
  - Seguimiento: la cadena se recoge con movimiento secundario de los eslabones.
- **Larga — Postura de bastión (20 s):**
  - Anticipación: planta el escudo en el suelo (0,3 s).
  - Ejecución: durante 6 s, el escudo se convierte en una **cobertura fija** de 2 × 1,8 m que bloquea el 100% del daño frontal para él y sus aliados detrás. Se proyecta un **arco de cobertura visible hasta 25 m** (el resto del escuadrón sabe dónde está la protección).
  - Seguimiento: recoge el escudo.

### 3.2.4 Rifle Largo con Mira

**Base:** daño 60 por disparo, cadencia de 1 por segundo, cargador de 6, recarga de 2,2 s, alcance de 220 m y mira con zoom ×4. Muy impreciso en movimiento (como el Rifle de Precisión del prototipo).
- **Animación:**
  - Encare: culata al hombro y ojo a la mira (0,25 s, con un fundido a la vista de la mira).
  - Disparo: retroceso aditivo de 6° con el hombro atrás.
  - Recuperación: manejo del cerrojo (0,6 s) con el casquillo expulsado como objeto físico.
  - Recarga: cartucho a cartucho, que se puede interrumpir.
- **Corta — Disparo de dispersión (7 s):** cartucho de perdigones: cono de 25° y 6 m (8 × 10 de daño) con empuje de 6 m/s. *Anticipación:* cambia el cartucho (0,2 s).
- **Media — Tiro a articulación (12 s):** disparo marcado que, si impacta, **ralentiza al 60% durante 3 s** y **cancela** la habilidad que el guardián estuviera anticipando. *Anticipación:* la mira se tiñe de ámbar (0,3 s).
- **Larga — Calibre perforante (18 s):** bala que **atraviesa** coberturas finas y hasta 3 objetivos, con daño 120 (×1,5 en el punto débil). *Anticipación:* contiene la respiración y el balanceo desaparece (0,6 s).

### 3.2.5 Arsenal Biomecánico Acoplado

**Base:** cuchillas de antebrazo para cuerpo a cuerpo (daño 22, combo de 4 golpes muy rápidos en 1,0 s, alcance de 2 m) y cañón de pulso ligero en la otra mano para media distancia (daño 12, 6 disparos por segundo, sobrecalentamiento a los 4 s).
- **Animación:**
  - Las placas del antebrazo se **despliegan** en cuchillas (0,15 s) con movimiento secundario de las placas.
  - Golpes encadenados alternando brazos, con mezcla de 0,08 s.
  - El cañón se abre como una flor de placas para disparar.
  - Sobrecalentamiento: vapor y placas al rojo.
- **Corta — Cuchillas de antebrazo (6 s):**
  - Anticipación: cruza los brazos (0,15 s).
  - Ejecución: tijera de las dos cuchillas a menos de 2,5 m con daño 50 y **sangrado energético** (6 por segundo durante 3 s).
  - Seguimiento: las cuchillas vibran y se recogen.
- **Media — Cable arpón (10 s):**
  - Anticipación: el antebrazo apunta y se abre (0,2 s).
  - Ejecución: arpón hasta 18 m. Si impacta en una superficie, el portador **se impulsa** hacia ella. Si impacta en un enemigo, lo **atrae 4 m**.
  - Seguimiento: el cable se recoge con física de cadena.
- **Larga — Pulso de riel (18 s):**
  - Anticipación: el antebrazo se convierte en un cañón de riel y se carga durante 1 s, con arcos eléctricos.
  - Ejecución: rayo instantáneo a más de 20 m (hasta 150 m) con daño 90, que **anula la levitación** de los guardianes de Metal durante 2 s.
  - Seguimiento: el cañón expulsa vapor y vuelve a su forma (0,5 s).

---

## 3.3 Implementación paso a paso en el proyecto (registro de la sustitución, ya realizada)

1. **Datos:**
   - Crea `DatosArquetipo` (ScriptableObject): nombre, vida, blindaje, velocidad, estabilidad, 3 habilidades (nombre, descripción, enfriamiento, tipo de efecto y parámetros) y color.
   - Crea `DatosArmaModular`: tipo (Arco, Mandoble, MazaEscudo, Rifle, Arsenal), modo (cuerpo a cuerpo, proyectil o rayo), daño, cadencia o combo, alcance y 3 habilidades.
   - Añade `CrearPorDefecto` con los valores de este documento, como en `DatosTraje` y `DatosArma`.
2. **Sistemas nuevos:**
   - `SistemaCuerpoACuerpo`: cápsula de impacto en el arma, ventanas activas por *Animation Event*, combo con búfer de entrada de 0,3 s, bloqueo y pausa de impacto.
   - `ProyectilBalistico`: flechas y arpones con gravedad, colisión, anclaje y daño.
   - `SistemaHabilidadesArquetipo`: las 3 habilidades de armadura (teclas Z, X y C), con una anticipación que reserva el enfriamiento al inicio y aplica el efecto en el evento de impacto.
3. **Reutilización:**
   - Todos los efectos se construyen con primitivas que ya existen: `SistemaSaludYTraje` (daño, curación, blindaje temporal, protección), `SistemaEstadosAlterados` (ralentizar, enraizar, empujar, inmunidades), `ControladorMovimientoAssaultCube` (impulsos y multiplicadores), `FabricaEfectosVisuales` (VFX), `SacudidaCamara` y `LuzEfimera`.
   - Las habilidades nuevas de "revelar", "marcar" e "interceptar daño" necesitan dos piezas pequeñas: un componente `MarcaRevelado` (contorno) y un redirector de daño en `RecibirDano`.
4. **Sustitución:** en `GestorFlujoPartida.CrearDestructor`, cambia `AsignarTraje` y `ConfigurarArsenal` por `AsignarArquetipo` y `EquiparArmaModular`. La explosión del traje se conserva tal cual.
5. **Interfaz:** en `ConstruirPersonalizacion`, cambia la fila de 5 trajes por 5 arquetipos y la de 5 armas por 5 armas modulares. En el HUD, pasa de 2 a 6 ranuras de habilidad (3 de armadura + 3 de arma).
6. **IA:** en `ControladorBotIA.UsarHabilidadesDestructor`, añade reglas por arquetipo:
   - Cazador: Paso de maleza si está enraizado o a menos de 4 m de una vanguardia.
   - Paladín: Égida si hay un guardián cargando una definitiva en su cono.
   - Bastión: Intercepción sobre el aliado con menos vida.
   - Tirador: Visor al inicio del combate.
   - Ciborg: Escudo de frecuencia si un aliado está bajo control.
7. **Animación:** ver Doc 06, 1.2 (capas, Override Controllers e IK de agarre).
