// Genera el informe técnico en Word (docx) de BugBlaster con capturas de código y del juego.
// Uso:  node BugBlaster/tools/generar_informe.js [salida.docx]
const fs = require('fs');
const path = require('path');
const D = require('docx');
const {
  Document, Packer, Paragraph, TextRun, ImageRun, HeadingLevel, AlignmentType, Table, TableRow, TableCell,
  WidthType, ShadingType, BorderStyle, PageBreak, Header, Footer, PageNumber, LevelFormat, TabStopType,
} = D;

const ROOT = path.resolve(__dirname, '..');
const IMG = path.join(ROOT, 'docs', 'img');
const OUT = path.resolve(process.argv[2] || path.join(ROOT, 'docs', 'Informe_BugBlaster.docx'));
const MAN = JSON.parse(fs.readFileSync(path.join(IMG, 'manifest.json'), 'utf8'));
const CODE = JSON.parse(fs.readFileSync(path.join(IMG, 'codigo_manifest.json'), 'utf8'));
const CODEMAP = Object.fromEntries(CODE.map((c) => [c.key, c]));

// ----------------------------------------------------------------- estilo
const FONT = 'Arial', MONO = 'Courier New';
const C = { navy: '0B3A66', blue: '1D6FB0', gray: '5A6B7D', light: 'EAF3FB', line: 'B8C7D6', green: '2E7D32', amber: 'A06A10', rowAlt: 'F6F9FC' };
const W = 9360; // ancho útil (Letter, márgenes de 1")

let figN = 0, tabN = 0;
const lines = (rel) => fs.readFileSync(path.join(ROOT, rel), 'utf8').split('\n').length - 1;

// Texto enriquecido: **negrita**, {{código}}
function rich(text, base = {}) {
  const out = [];
  const re = /(\*\*[^*]+\*\*|\{\{[^}]+\}\})/g;
  let last = 0, m;
  while ((m = re.exec(text))) {
    if (m.index > last) out.push(new TextRun({ text: text.slice(last, m.index), font: FONT, ...base }));
    const tok = m[0];
    if (tok.startsWith('**')) out.push(new TextRun({ text: tok.slice(2, -2), bold: true, font: FONT, ...base }));
    else out.push(new TextRun({ text: tok.slice(2, -2), font: MONO, size: (base.size || 22) - 2, color: '8A1C4E', shading: { type: ShadingType.CLEAR, fill: 'F3F0F5' }, ...base, bold: base.bold }));
    last = m.index + tok.length;
  }
  if (last < text.length) out.push(new TextRun({ text: text.slice(last), font: FONT, ...base }));
  return out;
}
const P = (text, o = {}) => new Paragraph({ children: rich(text, o.run), spacing: { before: o.before ?? 0, after: o.after ?? 120, line: 288 }, alignment: o.align || AlignmentType.LEFT, keepNext: o.keepNext, indent: o.indent, pageBreakBefore: o.pageBreakBefore });
const H1 = (t, pb = true) => new Paragraph({ heading: HeadingLevel.HEADING_1, pageBreakBefore: pb, children: [new TextRun({ text: t, font: FONT })] });
const H2 = (t) => new Paragraph({ heading: HeadingLevel.HEADING_2, children: [new TextRun({ text: t, font: FONT })] });
const H3 = (t) => new Paragraph({ heading: HeadingLevel.HEADING_3, children: [new TextRun({ text: t, font: FONT })] });
const bullet = (t, level = 0) => new Paragraph({ numbering: { reference: 'bul', level }, children: rich(t), spacing: { after: 60, line: 276 } });
const num = (t, ref = 'num') => new Paragraph({ numbering: { reference: ref, level: 0 }, children: rich(t), spacing: { after: 60, line: 276 } });
const spacer = (h = 120) => new Paragraph({ children: [], spacing: { after: h } });
const pageBreak = () => new Paragraph({ children: [new PageBreak()] });

const border = { style: BorderStyle.SINGLE, size: 4, color: C.line };
const borders = { top: border, bottom: border, left: border, right: border };
function cell(text, w, o = {}) {
  const paras = (Array.isArray(text) ? text : [text]).map((t) => new Paragraph({
    children: rich(String(t), { size: o.size || 19, bold: o.bold, color: o.color }), spacing: { after: 40 }, alignment: o.align || AlignmentType.LEFT,
  }));
  return new TableCell({ children: paras, width: { size: w, type: WidthType.DXA }, borders, margins: { top: 70, bottom: 70, left: 110, right: 110 }, verticalAlign: D.VerticalAlign.CENTER,
    shading: o.fill ? { type: ShadingType.CLEAR, fill: o.fill, color: 'auto' } : undefined, columnSpan: o.span });
}
function table(headers, rows, widths, caption) {
  const total = widths.reduce((a, b) => a + b, 0);
  const t = new Table({
    width: { size: total, type: WidthType.DXA }, columnWidths: widths,
    rows: [
      new TableRow({ tableHeader: true, children: headers.map((h, i) => cell(h, widths[i], { bold: true, fill: 'D9E8F5', color: C.navy })) }),
      ...rows.map((r, ri) => new TableRow({ cantSplit: true, children: r.map((c, i) => cell(c, widths[i], { fill: ri % 2 ? C.rowAlt : undefined })) })),
    ],
  });
  const out = [];
  if (caption) { tabN++; out.push(new Paragraph({ keepNext: true, spacing: { before: 120, after: 80 }, children: [new TextRun({ text: `Tabla ${tabN}. `, bold: true, font: FONT, size: 19, color: C.navy }), new TextRun({ text: caption, font: FONT, size: 19, color: C.gray, italics: true })] })); }
  out.push(t, spacer(160));
  return out;
}
function callout(title, text, fill = C.light, color = C.blue) {
  const b = { style: BorderStyle.SINGLE, size: 4, color };
  return [new Table({
    width: { size: W, type: WidthType.DXA }, columnWidths: [W],
    rows: [new TableRow({ children: [new TableCell({
      width: { size: W, type: WidthType.DXA }, shading: { type: ShadingType.CLEAR, fill, color: 'auto' },
      borders: { top: { style: BorderStyle.NONE, size: 0, color: 'FFFFFF' }, bottom: { style: BorderStyle.NONE, size: 0, color: 'FFFFFF' }, right: { style: BorderStyle.NONE, size: 0, color: 'FFFFFF' }, left: { style: BorderStyle.SINGLE, size: 24, color } },
      margins: { top: 100, bottom: 100, left: 180, right: 160 },
      children: [new Paragraph({ spacing: { after: 40 }, children: [new TextRun({ text: title, bold: true, font: FONT, size: 20, color })] }), ...[].concat(text).map((t) => new Paragraph({ spacing: { after: 40, line: 276 }, children: rich(t, { size: 20 }) }))],
    })] })],
  }), spacer(160)];
}

// Figuras
function figure(key, caption, o = {}) {
  const m = MAN[key];
  if (!m) throw new Error('imagen no encontrada: ' + key);
  const data = fs.readFileSync(path.join(IMG, m.path));
  const maxIn = o.maxIn || 6.3, maxH = o.maxH || 6.0;
  const wIn = Math.min(maxIn, maxH * m.w / m.h);
  const hIn = wIn * m.h / m.w;
  figN++;
  const type = m.path.endsWith('.png') ? 'png' : 'jpg';
  return [
    new Paragraph({ keepNext: true, alignment: AlignmentType.CENTER, spacing: { before: 120, after: 60 }, children: [new ImageRun({
      type, data, transformation: { width: Math.round(wIn * 96), height: Math.round(hIn * 96) },
      altText: { title: `Figura ${figN}`, description: caption, name: key },
    })] }),
    new Paragraph({ alignment: AlignmentType.CENTER, spacing: { after: 200 }, children: [
      new TextRun({ text: `Figura ${figN}. `, bold: true, font: FONT, size: 18, color: C.navy }),
      new TextRun({ text: caption, font: FONT, size: 18, color: C.gray, italics: true })] }),
  ];
}
const cap = (k, extra) => figure('cap_' + k, extra);
// Código: explicación + imagen
function codeFig(key, texto, extra = {}) {
  const c = CODEMAP[key];
  const out = [];
  if (texto) out.push(P(texto, { keepNext: true }));
  out.push(...figure('img_' + key, `${c.file}, líneas ${c.from}–${c.to}. ${c.caption}.`, { maxH: 5.9, ...extra }));
  return out;
}

// ----------------------------------------------------------- datos reales
const L = {
  pyinterp: lines('js/pyinterp.js'), game: lines('js/game.js'), render: lines('js/render.js'), challenges: lines('js/challenges.js'),
  ui: lines('js/challenge-ui.js'), levels: lines('js/levels.js'), main: lines('js/main.js'), audio: lines('js/audio.js'),
  css: lines('css/style.css'), html: lines('index.html'),
  tests: ['interp.test.js', 'challenges.test.js', 'levels.test.js', 'bot.e2e.js', 'ui.e2e.js'].reduce((a, f) => a + lines('tests/' + f), 0),
};
L.total = L.pyinterp + L.game + L.render + L.challenges + L.ui + L.levels + L.main + L.audio + L.css + L.html;

// ===================================================================== CONTENIDO
const body = [];

// ---------- Portada
body.push(
  new Paragraph({ spacing: { before: 700, after: 0 }, children: [new TextRun({ text: 'INFORME TÉCNICO DE DESARROLLO', font: FONT, size: 22, bold: true, color: C.blue, characterSpacing: 60 })] }),
  new Paragraph({ spacing: { before: 120, after: 60 }, children: [new TextRun({ text: 'BugBlaster', font: FONT, size: 92, bold: true, color: C.navy })] }),
  new Paragraph({ spacing: { after: 200 }, children: [new TextRun({ text: 'Operación Compilar', font: FONT, size: 40, color: 'C98A00' })] }),
  new Paragraph({ spacing: { after: 360 }, border: { bottom: { style: BorderStyle.SINGLE, size: 12, color: C.blue, space: 8 } }, children: [new TextRun({ text: 'Un juego de acción 2D (correr, saltar y disparar) para aprender a programar en Python', font: FONT, size: 26, color: C.gray })] }),
);
body.push(...figure('cap_menu_titulo', 'Pantalla de título del juego, tal como se ve al abrir index.html en el navegador.', { maxIn: 6.5 }));
body.push(
  ...table(['Dato', 'Detalle'], [
    ['Proyecto', 'BugBlaster: Operación Compilar (carpeta {{BugBlaster/}} del repositorio Juego-Intro-Sistemas)'],
    ['Tipo de juego', 'Acción 2D de plataformas y disparos con retos de programación integrados'],
    ['Lenguaje enseñado', 'Python (ejecutado dentro del juego con un mini-intérprete propio)'],
    ['Tecnología', 'HTML5 + Canvas 2D + JavaScript puro. Sin librerías ni instalación: se abre con doble clic'],
    ['Fecha del informe', '27 de noviembre de 2026'],
  ], [2300, 7060]),
);

// ---------- Contenido (lista estática)
body.push(H1('Contenido'));
[
  ['1.', 'Resumen ejecutivo', 'qué es BugBlaster y qué se logró, en cifras'],
  ['2.', 'Problemática y objetivos', 'por qué un juego para aprender a programar'],
  ['3.', 'Del concepto al proyecto', 'de las ideas iniciales a la decisión tecnológica'],
  ['4.', 'Diseño del juego', 'mundos, poderes, retos, jefes, accesibilidad y dificultad adaptativa'],
  ['5.', 'Arquitectura', 'módulos, flujo de datos y decisiones técnicas'],
  ['6.', 'Desarrollo paso a paso', 'cómo se construyó cada pieza, con capturas del código'],
  ['7.', 'Pruebas y calidad', 'pruebas automáticas, bot jugador y problemas encontrados'],
  ['8.', 'El juego en ejecución', 'galería de capturas: menús, retos, combate, jefes y mundos'],
  ['9.', 'Cómo ejecutarlo y jugarlo', 'requisitos, controles, comandos de prueba'],
  ['10.', 'Limitaciones y próximos pasos', 'qué falta y cómo seguir'],
  ['A.', 'Anexos', 'lista de retos, archivos del proyecto y glosario'],
].forEach(([n, t, d]) => body.push(new Paragraph({ spacing: { after: 110 }, tabStops: [{ type: TabStopType.LEFT, position: 700 }], children: [
  new TextRun({ text: n + '\t', bold: true, font: FONT, size: 24, color: C.blue }), new TextRun({ text: t, bold: true, font: FONT, size: 24, color: C.navy }), new TextRun({ text: '  —  ' + d, font: FONT, size: 21, color: C.gray })] })));

body.push(spacer(200), ...callout('Sobre este informe', 'Documenta **cómo se hizo todo**: desde la idea y las decisiones de diseño hasta el código de cada módulo, las pruebas y el juego ejecutándose. Incluye capturas del código fuente (resaltadas como en un editor) y capturas reales del juego, tomadas de forma automática.'));

// ---------- 1. Resumen
body.push(H1('1. Resumen ejecutivo'));
body.push(
  P('**BugBlaster: Operación Compilar** es un videojuego 2D de acción en el que el jugador controla a un robot-programador que atraviesa una computadora infectada por «bugs». El robot corre, salta y dispara, pero su verdadera fuerza está en el código: **cada poder del robot es un reto de Python que el jugador resuelve en una terminal**. Al lograrlo, ese código se «instala» y cambia el juego de verdad: más munición, una escopeta que se activa sola según la distancia, ráfagas, dash, doble salto, golpes críticos.'),
  P('El proyecto responde a una problemática concreta: el **rechazo hacia la programación y la deserción** de quienes se inician en ella. Para atacarla, el juego está pensado para que un **novato siempre pueda completarlo**: los retos empiezan guiados (con huecos para rellenar), tienen tres niveles de pista, no castigan el error, explican los fallos en español y adaptan su dificultad al desempeño de cada jugador.'),
);
body.push(...callout('Idea central del diseño', 'Tu código es tu arma. Lo que el jugador aprende (variables, condicionales, bucles y funciones) no es un adorno: es lo que le permite ganar. El estado del robot incluso se muestra en pantalla como si fuera un programa: {{vida = 120}}, {{municion = 12}}, {{arma = "escopeta"}}.'));
body.push(H2('BugBlaster en cifras'));
body.push(...table(['Aspecto', 'Valor'], [
  ['Mundos jugables', '4 — Variables, Condicionales, Bucles y Funciones, cada uno con un jefe'],
  ['Retos de programación', '16 (12 obligatorios + 4 terminales secretas), en 4 tipos: completar, ordenar, predecir y escribir/corregir'],
  ['Poderes que se instalan con código', '12 (munición, vida, energía, escopeta, rifle, escudo, ráfaga ×2, romper bucles, dash, doble salto, críticos)'],
  ['Enemigos', '5 tipos (caminante, fantasma volador, torreta, bicho-bucle y jefes) y 4 jefes con escudo que se rompe hackeando su terminal'],
  ['Dificultad', 'Dos «perillas» independientes (acción y código) + modo asistido + adaptación automática'],
  ['Tamaño del código del juego', `${L.total.toLocaleString('es')} líneas (JS, CSS y HTML) · ${L.pyinterp.toLocaleString('es')} corresponden al intérprete de Python`],
  ['Pruebas automáticas', `${L.tests} líneas de pruebas: intérprete (10 grupos), retos (60 comprobaciones), mapas (16), interfaz (61) y un bot que completa los 4 mundos`],
  ['Dependencias externas', 'Ninguna. Funciona sin internet, sin servidor y sin instalar nada'],
], [3000, 6360], 'Resumen cuantitativo del proyecto'));

// ---------- 2. Problemática
body.push(H1('2. Problemática y objetivos'));
body.push(H2('2.1 La problemática'));
body.push(
  P('Quienes empiezan a programar suelen toparse con barreras que no tienen que ver con su capacidad: una sintaxis que no perdona, mensajes de error en inglés y difíciles de interpretar, ejercicios abstractos sin una razón visible para hacerlos y el miedo a equivocarse frente a un compilador que «solo dice que no». Cuando esa frustración se acumula, se instala el **rechazo** y, en el peor caso, el **abandono** del curso.'),
  P('La hipótesis de este proyecto es que un juego bien diseñado puede reducir esas barreras: da un propósito inmediato a cada línea de código, convierte el error en parte normal del juego y mantiene la motivación con progreso, recompensas y variedad.'),
);
body.push(H2('2.2 Objetivos'));
body.push(P('**Objetivo general.** Construir un videojuego 2D, divertido y rejugable, que enseñe los fundamentos de la programación en Python a principiantes y que cualquiera pueda completar.', { keepNext: true }));
body.push(P('**Objetivos específicos:**', { keepNext: true, after: 60 }));
[
  'Integrar la programación en la jugabilidad (el código da poderes), en lugar de ponerla como un examen aparte.',
  'Ejecutar el código del estudiante de verdad, con mensajes de error claros y en español.',
  'Garantizar que un novato pueda completar cada reto: andamiaje, pistas, sin penalización y dificultad adaptativa.',
  'Ofrecer variedad de mecánicas: plataformas, disparos, armas, habilidades, jefes, coleccionables y estrellas.',
  'Que funcione en cualquier computadora de un laboratorio escolar: sin instalar nada y sin internet.',
  'Dejar el proyecto probado automáticamente y documentado.',
].forEach((t) => body.push(bullet(t)));
body.push(H2('2.3 Principios pedagógicos aplicados'));
body.push(...table(['Principio', 'Cómo se aplica en BugBlaster'], [
  ['Imposible quedarse atascado', 'Tres pistas por reto; la tercera muestra la solución. Al tercer fallo aparece una pista automática.'],
  ['De lo guiado a lo libre', 'Cada reto tiene tres versiones: Guiado (huecos «___»), Normal (esqueleto con comentarios) y Experto (editor vacío).'],
  ['Sin castigo por equivocarse', 'Fallar un reto no quita vida ni progreso: se explica el error y se reintenta al instante.'],
  ['Retroalimentación inmediata', 'La terminal muestra la salida del programa y una lista de casos de prueba en verde o rojo.'],
  ['Errores comprensibles', 'Los errores de Python se traducen y se explican; los nombres mal escritos sugieren «¿Quisiste decir…?».'],
  ['Aprender haciendo', 'El concepto se usa de inmediato: un {{for}} se convierte en una ráfaga, un {{if}} en un cambio de arma.'],
  ['Repaso natural', 'Cada partida repite las terminales del mundo y las terminales secretas refuerzan con retos de predecir/ordenar.'],
  ['Motivación', 'Estrellas, chips coleccionables, jefes, poderes que se acumulan entre mundos y una nueva sensación de dominio.'],
], [2900, 6460], 'Principios pedagógicos y su implementación'));

// ---------- 3. Concepto
body.push(H1('3. Del concepto al proyecto'));
body.push(H2('3.1 Cronología de decisiones'));
[
  '**Lluvia de ideas.** Se propusieron 10 ideas de juegos 2D que resuelven problemáticas comunes (reciclaje, finanzas personales, salud mental, ciberseguridad, primeros auxilios, agua, matemáticas, tráfico, nutrición y acoso escolar).',
  '**Elección del tema.** Se eligió la línea educativa «aprender a programar» y se concretó en dos juegos: un RPG de exploración con criaturas que representan lenguajes (CodeMon) y un juego de acción de correr, saltar y disparar (BugBlaster).',
  '**Plan detallado.** Se definieron los principios pedagógicos comunes, las mecánicas de ambos juegos, una comparación y las opciones técnicas.',
  '**Construcción.** Se decidió construir **BugBlaster**, en web (Canvas + JavaScript) y con Python como lenguaje enseñado.',
].forEach((t) => body.push(num(t)));
body.push(H2('3.2 Los dos juegos propuestos'));
body.push(...table(['', 'CodeMon (RPG)', 'BugBlaster (acción) — construido'], [
  ['Idea', 'Explorar un mundo con criaturas-lenguaje (Python = serpiente, Java = dragón…); combates que son puzzles de código', 'Correr, saltar y disparar; los poderes del robot son código que se escribe en terminales'],
  ['Fortaleza', 'Profundidad de aprendizaje y coleccionismo', 'Adrenalina y adicción inmediata'],
  ['Ritmo', 'Pausado, de pensar', 'Rápido, de reflejos + pausas de código'],
  ['Público ideal', 'Quien se abruma con la programación', 'Quien se aburre con lo «escolar»'],
], [1500, 3930, 3930], 'Comparación de los dos juegos propuestos'));
body.push(P('La parte pedagógica (intérprete, motor de retos, interfaz de la terminal) se construyó **independiente del juego**, de modo que pueda reutilizarse para construir CodeMon más adelante.'));
body.push(H2('3.3 Decisión tecnológica'));
body.push(...table(['Opción', 'Ventajas', 'Inconvenientes', 'Decisión'], [
  ['Web: HTML5 + Canvas + JS', 'Sin instalación; corre en el laboratorio de la escuela; se comparte con un enlace; el código del jugador puede ejecutarse dentro del navegador', 'Hay que escribir el motor 2D a mano', '**Elegida**'],
  ['Godot 2D', 'Muy bueno para 2D; ligero', 'Ejecutar código del usuario es más trabajoso; requiere exportar', 'Descartada'],
  ['Unity 2D (como el repositorio anterior)', 'Misma herramienta que el juego previo del repositorio', 'Pesado; difícil ejecutar código de usuario; requiere instalar', 'Descartada'],
], [2000, 3000, 2700, 1660], 'Opciones tecnológicas evaluadas'));
body.push(...callout('¿Por qué un mini-intérprete propio y no Python real?', [
  'Se consideró cargar un Python real en el navegador (Pyodide), pero implica descargar varios megabytes, depende de internet y ofrece mensajes de error en inglés poco amigables.',
  'Se escribió **PyMini**, un intérprete de un subconjunto de Python: arranca al instante, funciona sin conexión, permite detener bucles infinitos y **traduce los errores al español con sugerencias**. El costo: no soporta todo Python (ver sección 10).',
]));
body.push(P('El repositorio ya contenía otro proyecto (**Voidform: Bio-Forge**, un juego 3D en Unity). Para no mezclarlos, BugBlaster vive en su propia carpeta {{BugBlaster/}} y no modifica nada de lo anterior.'));

// ---------- 4. Diseño
body.push(H1('4. Diseño del juego'));
body.push(H2('4.1 Bucle principal'));
body.push(P('Cada mundo se juega así: se avanza por un nivel de plataformas eliminando bugs, recogiendo chips y munición, y **hackeando terminales** (tres obligatorias y una secreta). Al final aparece un jefe protegido por un **escudo** que solo cae al resolver el reto de su terminal; después hay que derrotarlo para abrir el portal de salida.'));
body.push(...figure('img_diag_flujo', 'Qué ocurre cuando el jugador hackea una terminal: del reto al poder instalado.', { maxIn: 6.5 }));
body.push(H2('4.2 Los cuatro mundos'));
body.push(...table(['Mundo', 'Concepto', 'Enemigos', 'Jefe', 'Retos'], [
  ['1 · Núcleo de Datos', 'Variables', 'Bichos caminantes', 'Bug Variable (dispara orbes)', 'Completar, ordenar, escribir + secreto de predecir'],
  ['2 · Sala de Decisiones', 'Condicionales (if/elif/else)', 'Caminantes, fantasmas «null» voladores y torretas «ERR»', 'Rey Si-Entonces (abanico de 3 balas)', 'Completar, corregir, escribir + secreto de ordenar'],
  ['3 · Fábrica de Repeticiones', 'Bucles (for/while)', 'Bichos-gusano que reaparecen en bucle', 'Bucle Maestro (ráfagas e invoca gusanos)', 'Completar, corregir bucle infinito, escribir + secreto de predecir'],
  ['4 · Ciudad de Funciones', 'Funciones (def/return)', 'Todos los anteriores', 'Función Maestra (embestida y abanico de 5)', 'Completar, ordenar, escribir + secreto de predecir'],
], [1900, 1500, 2160, 1900, 1900], 'Los cuatro mundos del juego'));
body.push(H2('4.3 Los poderes son código'));
body.push(P('La idea más importante del diseño: **el progreso del jugador es la lista de retos que resolvió**, y de esa lista se calculan los poderes del robot. Así lo aprendido tiene un efecto real y visible.'));
body.push(...table(['Reto', 'Código que se «instala»', 'Efecto en el juego'], [
  ['Mundo 1 · completar', '{{municion = 12}}', 'El cargador pasa de 6 a 12 balas'],
  ['Mundo 1 · ordenar', '{{vida = vida - dano}}', 'La armadura aguanta 120 puntos de vida (antes 100)'],
  ['Mundo 1 · jefe', '{{energia = energia + 25}}', 'Aparece el medidor de energía (75) que alimenta las habilidades'],
  ['Mundo 2 · completar', '{{if distancia < 3: arma = "escopeta"}}', 'Con un enemigo a menos de 3 casillas el robot usa la escopeta (5 perdigones) automáticamente'],
  ['Mundo 2 · corregir', '{{else: arma = "rifle"}}', 'Con enemigos lejos usa el rifle: más rápido y con mayor alcance'],
  ['Mundo 2 · jefe', '{{if vida < 30}}', 'Escudo automático: con poca vida el daño recibido se reduce a la mitad'],
  ['Mundo 3 · completar', '{{for i in range(3)}}', 'Ráfaga de 3 disparos con F (cuesta 25 de energía)'],
  ['Mundo 3 · corregir', '{{contador = contador + 1}}', 'Se «rompe el bucle»: los bichos-gusano ya no reaparecen'],
  ['Mundo 3 · jefe', '{{for n in range(1, 6)}}', 'La ráfaga mejora a 5 disparos'],
  ['Mundo 4 · completar', '{{saludar()}}', 'Dash con Shift: invulnerable un instante (20 de energía)'],
  ['Mundo 4 · ordenar', '{{def cuadrado(n)}}', 'Doble salto'],
  ['Mundo 4 · jefe', '{{def danio(base, critico)}}', 'Golpes críticos: 20 % de los disparos hacen el doble de daño'],
], [2100, 3300, 3960], 'Retos, código instalado y poderes que se obtienen'));
body.push(H2('4.4 Los cuatro tipos de reto'));
body.push(...table(['Tipo', 'Qué hace el jugador', 'Cómo se valida'], [
  ['Completar', 'Rellena el hueco «___» de un código (eligiendo una opción o escribiendo)', 'Se sustituye el hueco y se **ejecuta** el programa; se compara su salida con la esperada'],
  ['Ordenar', 'Arrastra o toca tarjetas con líneas de código para armar el programa', 'Se comprueba que use exactamente las tarjetas necesarias y se ejecuta el resultado'],
  ['Predecir', 'Indica qué imprimirá un código', 'La respuesta correcta se **calcula ejecutando** el código (no está escrita a mano)'],
  ['Escribir / corregir', 'Escribe una solución o arregla un código con un error escondido', 'Se ejecuta contra varios **casos de prueba**; algunos retos exigen usar el concepto (por ejemplo, un bucle)'],
], [1700, 3700, 3960], 'Tipos de reto y su validación'));
body.push(H2('4.5 Accesibilidad y dificultad adaptativa'));
[
  '**Dos perillas separadas.** «Dificultad de acción» (Fácil/Normal/Difícil: daño y vida de enemigos) y «Dificultad de código» (Guiado/Normal/Experto). Alguien flojo en reflejos y bueno en código, o al revés, puede avanzar igual.',
  '**Modo asistido.** Reduce el daño a 30 %, ayuda al apuntar (los disparos se curvan hacia el enemigo) y **pausa el tiempo** al hackear una terminal.',
  '**Adaptación automática.** Si el jugador resuelve retos sin ayudas, el siguiente sube de modo; si necesita la solución o falla muchas veces, baja a uno más guiado. Se guarda entre sesiones.',
  '**Tiempo lento en las terminales.** El mundo sigue a 15 % de velocidad mientras se hackea: hay tensión, pero se puede pensar.',
  '**Estrellas.** ★ por completar, ★★ si no se usó ninguna solución, ★★★ si además se recogieron todos los chips y la terminal secreta.',
  '**Teclado o ratón.** Se puede jugar solo con teclado (J para disparar) o apuntar con el ratón.',
].forEach((t) => body.push(bullet(t)));

// ---------- 5. Arquitectura
body.push(H1('5. Arquitectura'));
body.push(P('BugBlaster está formado por módulos pequeños y con una sola responsabilidad, cargados con etiquetas {{<script>}} clásicas (no módulos ES), lo que permite abrir {{index.html}} directamente desde el disco sin servidor.'));
body.push(...figure('img_diag_arq', 'Módulos del proyecto agrupados en tres capas.', { maxIn: 6.5 }));
body.push(...table(['Archivo', 'Responsabilidad', 'Líneas'], [
  ['{{js/pyinterp.js}}', 'Mini-intérprete de Python: tokenizador, analizador y evaluador; errores en español', String(L.pyinterp)],
  ['{{js/challenges.js}}', 'Los 16 retos (datos) y la validación que ejecuta el código del jugador', String(L.challenges)],
  ['{{js/challenge-ui.js}}', 'Interfaz de la terminal de hackeo: los 4 tipos de reto, pistas, consola y casos de prueba', String(L.ui)],
  ['{{js/levels.js}}', 'Constructor de mapas y definición de los 4 mundos', String(L.levels)],
  ['{{js/game.js}}', 'Motor: física, jugador, armas, enemigos, jefes, poderes, cámara, estrellas', String(L.game)],
  ['{{js/render.js}}', 'Todo el dibujo (procedural) en Canvas 2D y el HUD', String(L.render)],
  ['{{js/audio.js}}', 'Efectos de sonido sintetizados con WebAudio', String(L.audio)],
  ['{{js/main.js}}', 'Menús, opciones, guardado de progreso, pausa y arranque', String(L.main)],
  ['{{css/style.css}}', 'Estilos de menús y de las terminales de hackeo', String(L.css)],
  ['{{index.html}}', 'Página principal; carga los scripts en el orden correcto', String(L.html)],
], [2700, 5760, 900], 'Módulos del proyecto'));
body.push(H3('Decisiones de arquitectura'));
[
  '**Sin frameworks ni dependencias.** Menos cosas que fallen, cero instalación y todo el código es legible por un estudiante.',
  '**Arte 100 % procedural.** Robot, bichos, jefes, mapas y fondos se dibujan con formas de Canvas: sin imágenes que descargar y fácil de modificar.',
  '**Separación datos / lógica / interfaz.** Los retos son datos; la validación no conoce el DOM; el juego solo conoce a la interfaz por tres «hooks» ({{openChallenge}}, {{saveProgress}}, {{onComplete}}).',
  '**Estado = retos resueltos.** El progreso se guarda como una lista de identificadores y los poderes se recalculan ({{computePowers}}), lo que evita estados inconsistentes.',
  '**Guardado tolerante.** Si el navegador bloquea el almacenamiento, el juego funciona igual (solo no recuerda el progreso).',
].forEach((t) => body.push(bullet(t)));
body.push(...codeFig('index_html', 'El orden de carga importa: primero el intérprete, luego los retos, el audio, los mundos, el motor, el dibujo, la interfaz y, al final, el arranque.'));

// ---------- 6. Desarrollo paso a paso
body.push(H1('6. Desarrollo paso a paso'));
body.push(P('A continuación se describe, en el orden en que se construyó, cada pieza del juego con capturas del código real. Cada paso se probó antes de pasar al siguiente.'));
body.push(...figure('img_term_arbol', 'Estructura final del proyecto y líneas de cada archivo (comando ejecutado de verdad).', { maxIn: 6.0 }));

body.push(H2('Paso 1 · El mini-intérprete de Python (PyMini)'));
body.push(P('Lo primero fue lo más arriesgado: poder **ejecutar el código del jugador dentro del juego**. PyMini tiene las tres etapas clásicas de un intérprete: tokenizador, analizador sintáctico y evaluador. Se ocupó de que cada error se explique con palabras sencillas.'));
body.push(...codeFig('interp_tokenizer', '**Tokenizador.** Recorre el programa línea por línea. Calcula la sangría con una pila (como hace Python) y genera los tokens INDENT/DEDENT; reconoce números, textos, f-strings y operadores. Si la sangría no coincide, avisa en español.'));
body.push(...codeFig('interp_parser_if', '**Analizador.** Es de descenso recursivo. Un detalle pedagógico: si el estudiante escribe {{if x = 3:}}, el error no es genérico, explica que para comparar se usan dos signos {{==}}.'));
body.push(...codeFig('interp_binop', '**Operaciones.** Respetan las reglas de Python (la división {{/}} siempre da decimal, {{//}} es entera, {{%}} sigue el signo del divisor) y los errores sugieren la solución; por ejemplo, al sumar un texto con un número recomienda {{str(...)}}.'));
body.push(...codeFig('interp_exec', '**Evaluador.** Ejecuta el árbol de instrucciones. Cada instrucción consume un «paso» ({{tick}}): así un {{while}} que nunca termina se detiene con un mensaje claro en lugar de congelar el juego. {{for}} recorre listas, textos o {{range}} de forma perezosa.'));
body.push(...codeFig('interp_lookup', '**«¿Quisiste decir…?»** Cuando un nombre no existe, se calcula la distancia de edición (Levenshtein) con los nombres conocidos. Así {{vidda}} sugiere {{vida}}, un error típico de principiante.'));
body.push(...codeFig('interp_run', '**API pública.** {{run(código)}} devuelve la salida, el error traducido (tipo, mensaje y línea) y {{evalExpr()}}, que permite probar llamadas como {{danio(10, True)}} sobre el estado final. {{lineOffset}} corrige el número de línea cuando se antepone código de preparación.'));
body.push(...codeFig('test_interprete', 'Las pruebas del intérprete comprueban programas correctos y los mensajes de error esperados (nombre mal escrito, falta de «:», bucle infinito, división entre cero…).'));

body.push(H2('Paso 2 · Los retos y su validación'));
body.push(P('Cada reto es un objeto de datos con su historia, su tipo, el código inicial para cada dificultad, sus casos de prueba, tres pistas y la explicación que se muestra al acertar. **Agregar un reto es agregar un objeto**; no hay que tocar el motor.'));
body.push(...codeFig('retos_datos', '**Un reto (jefe del mundo 2).** Fíjese en los tres {{starter}} (0 = guiado con huecos, 1 = normal, 2 = experto), los {{tests}} con valores previos ({{prelude}}) y el requisito conceptual ({{require}}).'));
body.push(...codeFig('retos_validar', '**Validación de «escribir».** Comprueba el requisito (por ejemplo, usar {{if}}), ejecuta el código del jugador contra cada caso de prueba y devuelve el detalle. Se valida el **comportamiento**, no el texto: así valen soluciones distintas pero correctas (por ejemplo, {{vida >= 30}} o {{vida < 30}} invertido).'));
body.push(...callout('Respuestas calculadas, no escritas', 'En los retos de «predecir» la respuesta correcta no se escribe a mano: se obtiene ejecutando el código con PyMini. Así es imposible que el dato y el código discrepen.'));

body.push(H2('Paso 3 · Los mundos y sus mapas'));
body.push(P('Dibujar mapas a mano en texto es propenso a errores, así que se creó un **constructor**: se declaran suelo, huecos, plataformas, pinchos y entidades, y la herramienta genera el mapa de 17 filas. Se acotaron las distancias para que **todo sea alcanzable con las habilidades básicas** (saltar 3 casillas de alto y cruzar huecos de hasta 4); dash y doble salto solo abren rutas opcionales con chips.'));
body.push(...codeFig('niveles_builder', '**Constructor de niveles.** Funciones como {{ground}}, {{carve}} (hueco), {{block}}, {{plat}} (plataforma que se atraviesa desde abajo), {{spikes}}, {{ent}} (entidad) y {{arena}} (zona del jefe).'));
body.push(...codeFig('niveles_mundo1', '**Definición del Mundo 1.** Tema de colores, introducción, jefe, retos y la secuencia de llamadas que levantan el mapa: tutorial, enemigos, terminales, checkpoints y la arena del jefe.'));
body.push(P('Los cuatro mapas completos, generados por el propio juego (cada uno dividido en dos mitades para verlos legibles):', { keepNext: true }));
const mapNames = { 1: 'Núcleo de Datos', 2: 'Sala de Decisiones', 3: 'Fábrica de Repeticiones', 4: 'Ciudad de Funciones' };
for (let n = 1; n <= 4; n++) {
  body.push(...figure(`mapa${n}_1`, `Mundo ${n} · ${mapNames[n]} — primera mitad del mapa (inicio, enemigos y terminales).`, { maxIn: 6.5, maxH: 3 }));
  body.push(...figure(`mapa${n}_2`, `Mundo ${n} · ${mapNames[n]} — segunda mitad (zona final y arena del jefe).`, { maxIn: 6.5, maxH: 3 }));
}

body.push(H2('Paso 4 · El motor del juego'));
body.push(P('El corazón de BugBlaster es {{game.js}}: físicas de plataformas, control del robot, armas, enemigos, jefes y la conexión con los retos. Se usó un paso de tiempo **fijo de 1/60 s**, lo que hace que el juego sea idéntico en cualquier equipo y permite a las pruebas simularlo a máxima velocidad.'));
body.push(...codeFig('juego_poderes', '**De retos a poderes.** {{computePowers}} recibe el conjunto de retos resueltos y devuelve todas las capacidades del robot. El guardado es solo una lista de identificadores.'));
body.push(...codeFig('juego_colision', '**Colisión con el mapa.** Se mueve primero en X y luego en Y, corrigiendo contra las casillas sólidas. Las plataformas {{=}} solo detienen al caer desde arriba y se pueden atravesar con ↓.'));
body.push(...codeFig('juego_jugador', '**Sensación de control.** «Coyote time» (se puede saltar un instante después de dejar el borde), memoria de salto, salto de altura variable, aceleración distinta en el aire, dash con invulnerabilidad y doble salto.'));
body.push(...codeFig('juego_disparo', '**Armas según la distancia.** Con un enemigo a menos de 3 casillas y el poder instalado, el robot usa la escopeta; si no, el rifle (si se instaló) o la pistola. Cada arma gasta una munición diferente y la munición se regenera con el tiempo.'));
body.push(...codeFig('juego_jefe', '**Inteligencia del jefe.** Mantiene la distancia, salta y ataca según su «estilo»: {{var}} dispara un orbe; {{cond}}, un abanico de 3 balas; {{loop}}, una ráfaga de 3 y fabrica gusanos; {{func}}, una embestida avisada y un abanico de 5.'));
body.push(...codeFig('juego_resuelto', '**Al resolver un reto.** Se guarda el progreso, se recalculan los poderes, se cura y recarga al jugador, se anuncia el código instalado y, si es la terminal del jefe, **se rompe su escudo**. También se ajusta la dificultad adaptativa.'));

body.push(H2('Paso 5 · El dibujo y el HUD'));
body.push(P('Todo el arte se genera con código: el robot, los bichos, los jefes, los edificios del fondo, la «lluvia» de ceros y unos y hasta los carteles. Cada mundo tiene su paleta de colores. Esto evita depender de imágenes y hace el juego muy ligero.'));
body.push(...codeFig('render_jugador', '**El robot.** Se dibuja con rectángulos redondeados; las piernas se animan con un seno, el arma gira hacia el ratón y aparece un anillo amarillo cuando el escudo automático está activo.'));
body.push(...codeFig('render_hud', '**HUD como código.** El estado del robot se muestra como líneas de un programa ({{vida = 120}}, {{municion = 12}}, {{arma = "escopeta"}}, {{distancia = 2.1}}), de modo que el jugador lee código mientras juega.'));

body.push(H2('Paso 6 · Interfaz de retos, menús y guardado'));
body.push(P('La terminal de hackeo es una ventana HTML que se abre sobre el juego, que sigue «en cámara lenta» detrás. Incluye resaltado de sintaxis, editor con tabulador, consola, casos de prueba y pistas.'));
body.push(...codeFig('ui_ordenar', '**Reto «ordenar».** Dos zonas: el banco de tarjetas y «tu programa». Las tarjetas se pueden tocar, arrastrar con el ratón (la parte que muestra la captura) o mover con los botones ▲ ▼, útil sin ratón.'));
body.push(...codeFig('ui_validar', '**Comprobar.** Valida, muestra la consola, marca los casos en verde/rojo, ofrece una pista automática al tercer fallo y hace «temblar» el panel. Nunca resta nada.'));
body.push(...codeFig('main_guardado', '**Guardado de progreso.** Se usa {{localStorage}} dentro de {{try/catch}}: si el navegador lo bloquea, se juega igual. Se guardan retos resueltos, estrellas por mundo, dificultad adaptativa y opciones.'));
body.push(...codeFig('css_panel', '**Estilos de la terminal.** Borde del color del mundo, animación de aparición y de «sacudida» cuando el reto falla.'));

// ---------- 7. Pruebas
body.push(H1('7. Pruebas y calidad'));
body.push(P('El proyecto se probó en cuatro niveles, todos automáticos y reproducibles con un comando:'));
body.push(...table(['Prueba', 'Qué comprueba', 'Resultado'], [
  ['{{interp.test.js}}', 'El intérprete: aritmética, textos, f-strings, if/elif/else, bucles, funciones, listas, diccionarios y 20 errores amistosos', '10 grupos OK'],
  ['{{challenges.test.js}}', 'Cada reto: su solución pasa; las respuestas incorrectas no; los 3 {{starter}} no son ya la solución; hay 3 pistas', '60 comprobaciones OK (16 retos)'],
  ['{{levels.test.js}}', 'Los mapas: filas del mismo largo, 1 inicio, 1 salida, 1 jefe, terminales = retos, suelo bajo las entidades, huecos ≤ 4', '16 comprobaciones OK'],
  ['{{ui.e2e.js}}', 'Resuelve los 16 retos con el DOM real (clics, tarjetas, escritura) en los 3 modos, más caminos de error y pistas', '61 OK, 0 fallos'],
  ['{{bot.e2e.js}}', 'Un bot corre, salta, dispara y hackea para completar cada mundo en Chromium', '4 de 4 mundos completados, 0 derrotas'],
], [2300, 5160, 1900], 'Pruebas automáticas'));
body.push(...figure('img_term_unit', 'Ejecución de las pruebas unitarias (intérprete, retos y mapas).', { maxIn: 6.3 }));
body.push(...codeFig('test_bot', '**El bot jugador.** Corre a la derecha, salta ante paredes, huecos, pinchos y enemigos, dispara, hackea las terminales y, en la arena, rompe el escudo y derrota al jefe. Simula el juego llamando directamente a {{update(1/60)}} sin esperar tiempo real.'));
body.push(...figure('img_term_bot', 'El bot completa los 4 mundos (columna {{state}} = complete) sin una sola derrota y sin errores de JavaScript.', { maxIn: 6.3 }));
body.push(...figure('img_term_ui', 'Resultado de la prueba de la interfaz: todos los retos se resuelven por la interfaz real en los tres modos.', { maxIn: 6.3 }));
body.push(H2('Problemas encontrados y cómo se resolvieron'));
body.push(...table(['#', 'Problema', 'Cómo se detectó', 'Solución'], [
  ['1', 'Las entidades del jefe y el portal no aparecían: el ancho del mapa no incluía la arena', 'Revisión al montar los mapas y prueba de estructura', 'Se corrigieron los anchos; {{levels.test.js}} exige 1 jefe, 1 salida y arena al final'],
  ['2', 'Tres mundos tenían muy pocos chips', 'Prueba de mapas', 'Se añadieron chips; la prueba exige un mínimo'],
  ['3', 'Plataformas a 3 casillas de altura, al límite del salto', 'Análisis de la física (salto máximo ≈ 3.5 casillas)', 'Se bajaron a 2 casillas para novatos'],
  ['4', 'Un error al llamar a una función en un caso de prueba no se marcaba como error', 'Prueba de los retos', 'Se agregó la marca {{isError}} y se propaga el mensaje'],
  ['5', 'Las opciones con comillas (por ejemplo {{"balas"}}) rompían el atributo HTML y no se podían elegir', 'Al fotografiar el juego: el clic en esa opción no respondía', 'Se escapan las comillas en la interfaz y se añadió una prueba que elige cada opción'],
], [450, 3300, 2700, 2910], 'Problemas encontrados durante el desarrollo'));

// ---------- 8. Galería
body.push(H1('8. El juego en ejecución'));
body.push(P('Todas las capturas de esta sección son **capturas reales del juego** generadas automáticamente con el script {{tools/capturar.js}} (Chromium sin interfaz), que monta cada escena y la fotografía. Siguen el orden natural de una partida.'));
body.push(H2('8.1 Menús'));
body.push(...cap('menu_titulo', 'Pantalla de título con lluvia de bits animada.'));
body.push(...cap('menu_mundos', 'Selección de mundo con estrellas y el panel «Tu robot», que muestra en código los poderes instalados.'));
body.push(...cap('intro_mundo1', 'Introducción del Mundo 1: historia, concepto que se aprenderá y un adelanto del código.'));
body.push(...cap('menu_ayuda', 'Pantalla «Cómo se juega»: controles, idea del juego y estrellas.'));
body.push(...cap('menu_opciones', 'Opciones: dificultad de acción y de código, modo asistido, sonido y mundos libres para docentes.'));

body.push(H2('8.2 Mundo 1: moverse, disparar y hackear'));
body.push(...cap('inicio_banner', 'Inicio del Mundo 1 con su banner. A la izquierda, el HUD en forma de código; a la derecha, el progreso del nivel.'));
body.push(...cap('mundo1_cartel_controles', 'Los carteles enseñan los controles cuando el robot se acerca.'));
body.push(...cap('mundo1_combate_disparos', 'Combate: disparos hacia un bicho caminante con la pistola inicial.'));
body.push(...cap('mundo1_terminal_aviso', 'Al acercarse a una terminal aparece el aviso «[E] Hackear terminal».'));

body.push(H2('8.3 Los retos de código'));
body.push(H3('Completar el hueco (modo Guiado)'));
body.push(...cap('reto_completar_guiado', 'Reto «completar»: el hueco se rellena eligiendo una opción. Se ven el tipo de error, el modo y la pista (0/3).'));
body.push(...cap('reto_completar_error', 'Respuesta incorrecta: se explica sin penalización; se muestra también la consola con la salida del programa.'));
body.push(...cap('reto_completar_correcto', 'Respuesta correcta: aparece la explicación del concepto y el botón «Instalar y continuar».'));
body.push(...cap('mundo1_poder_instalado_toast', 'Tras resolverlo, el juego anuncia lo instalado y la munición sube a 12 (HUD).'));
body.push(H3('Ordenar las líneas'));
body.push(...cap('reto_ordenar_inicio', 'Reto «ordenar»: tarjetas desordenadas a la izquierda y el programa vacío a la derecha.'));
body.push(...cap('reto_ordenar_orden_incorrecto', 'Orden equivocado: Python lo ejecuta y explica que {{vida}} no existe todavía; el jugador lo entiende viendo el error.'));
body.push(...cap('reto_ordenar_listo', 'Programa ordenado correctamente antes de comprobarlo.'));
body.push(...cap('reto_ordenar_correcto', 'Comprobado: la salida es 70 y se muestra la explicación («Python lee el código de arriba hacia abajo»).'));
body.push(H3('Escribir código'));
body.push(...cap('reto_escribir_guiado', 'Reto «escribir» en modo Guiado: el editor ya trae el esqueleto con huecos «___».'));
body.push(...cap('reto_escribir_error_nombre', 'Error de nombre mal escrito ({{energi}}): el mensaje señala la línea y sugiere «¿Quisiste decir «energia»?».'));
body.push(...cap('reto_pistas', 'Las pistas aparecen de forma progresiva; la tercera ofrece la solución.'));
body.push(...cap('reto_escribir_correcto', 'Solución correcta: se ve la salida del programa, las pistas usadas y la valoración en estrellas.'));
body.push(...cap('reto_escribir_experto', 'Modo Experto: el editor empieza completamente vacío.'));
body.push(...cap('reto_escribir_requisito_bucle', 'Requisito conceptual: cinco {{print}} seguidos producen la salida correcta, pero el reto exige usar un bucle.'));
body.push(H3('Corregir errores'));
body.push(...cap('reto_corregir_sintaxis', 'Corregir: un {{=}} dentro de un {{if}}; el mensaje indica usar {{==}} y señala la línea.'));
body.push(...cap('reto_corregir_bucle_infinito', 'Corregir un bucle infinito: el intérprete lo detiene y lo explica en vez de congelar el juego.'));
body.push(H3('Funciones con casos de prueba'));
body.push(...cap('reto_funcion_pruebas', 'Reto de funciones: aparecen los casos de prueba ({{danio(10, True)}}, etc.) en verde.'));
body.push(H3('Terminal secreta'));
body.push(...cap('reto_predecir_bonus', 'Reto «predecir» de una terminal secreta: recompensa con un chip y ayuda a conseguir la tercera estrella.'));

body.push(H2('8.4 Poderes instalados'));
body.push(...cap('mundo1_hud_energia', 'Tras el reto del jefe aparece el medidor {{energia = 75}} en el HUD.'));

body.push(H2('8.5 El jefe del Mundo 1'));
body.push(...cap('jefe1_escudo_activo', 'Al entrar en la arena se cierra la compuerta (barrera roja) y aparece el jefe con su escudo. La barra de vida es azul mientras el escudo esté activo.'));
body.push(...cap('jefe1_escudo_roto', 'Se resuelve la terminal del jefe: el escudo se rompe y el juego anuncia el código instalado.'));
body.push(...cap('jefe1_combate', 'Combate contra el jefe ya sin escudo; su barra de vida baja con cada disparo.'));
body.push(...cap('jefe1_derrotado_portal', 'Jefe derrotado: se abre la compuerta y el portal se activa.'));

body.push(H2('8.6 Mundo 2: Sala de Decisiones'));
body.push(...cap('mundo2_escopeta_distancia', 'Con enemigos cerca el HUD cambia a {{arma = "escopeta"}} ({{distancia = 2.1}}) y se disparan perdigones en abanico.'));
body.push(...cap('mundo2_bifurcacion', 'Bifurcación: camino bajo con pinchos y bichos, o camino alto con plataformas y chips.'));
body.push(...cap('mundo2_torreta', 'Torretas «ERR» que disparan hacia el jugador.'));
body.push(...cap('jefe2_rey_si_entonces', 'El Rey Si-Entonces dispara abanicos de 3 balas; el escudo automático del robot se ve en el mensaje de poder instalado.'));

body.push(H2('8.7 Mundo 3: Fábrica de Repeticiones'));
body.push(...cap('mundo3_rafaga', 'Ráfaga de 3 disparos con F: las balas cian vuelan en fila y la energía baja de 75 a 51.'));
body.push(...cap('mundo3_bicho_bucle_reaparece', 'Un bicho-gusano derrotado deja un contador «↻» con el tiempo que falta para reaparecer… hasta que el jugador «rompe el bucle» con código.'));
body.push(...cap('jefe3_bucle_maestro', 'El Bucle Maestro dispara ráfagas de orbes verdes.'));

body.push(H2('8.8 Mundo 4: Ciudad de Funciones'));
body.push(...cap('mundo4_dash', 'Dash: la estela azul detrás del robot. En el HUD inferior aparecen las habilidades [F] ráfaga, [Shift] dash y [2×↑] doble salto.'));
body.push(...cap('jefe4_funcion_maestra', 'La Función Maestra, último jefe, tras instalar los golpes críticos.'));

body.push(H2('8.9 Fin de nivel y pausa'));
body.push(...cap('fin_nivel', 'Pantalla de nivel completado: estrellas, estadísticas, el código instalado y lo aprendido.'));
body.push(...cap('menu_pausa', 'Menú de pausa (Esc o P). El juego también se pausa solo al cambiar de pestaña.'));

// ---------- 9. Cómo ejecutarlo
body.push(H1('9. Cómo ejecutarlo y jugarlo'));
body.push(H2('9.1 Requisitos'));
[
  'Un navegador moderno (Chrome, Edge, Firefox o Safari). No requiere internet, servidor ni instalación.',
  'Teclado; el ratón es opcional (se puede disparar con la tecla J).',
].forEach((t) => body.push(bullet(t)));
body.push(H2('9.2 Para jugar'));
body.push(num('Abrir el archivo {{BugBlaster/index.html}} con doble clic.', 'num2'));
body.push(num('Pulsar «Jugar», elegir el Mundo 1 y empezar.', 'num2'));
body.push(P('Opcionalmente, para servirlo localmente: {{cd BugBlaster && python3 -m http.server 8000}} y abrir {{http://localhost:8000}}.', { before: 60 }));
body.push(H2('9.3 Controles'));
body.push(...table(['Tecla', 'Acción'], [
  ['{{A}} {{D}} o {{←}} {{→}}', 'Moverse'],
  ['{{W}}, {{↑}} o {{Espacio}}', 'Saltar (mantener para saltar más alto); en el aire, doble salto si se instaló'],
  ['Ratón + clic izquierdo, o {{J}}', 'Apuntar y disparar'],
  ['{{E}} o {{Enter}}', 'Hackear una terminal'],
  ['{{F}} o clic derecho', 'Ráfaga (desde el Mundo 3)'],
  ['{{Shift}}', 'Dash (desde el Mundo 4)'],
  ['{{S}} o {{↓}}', 'Bajar de una plataforma'],
  ['{{Esc}} o {{P}}', 'Pausa'],
  ['{{Ctrl}} + {{Enter}}', 'Comprobar un reto (dentro de la terminal)'],
], [3400, 5960], 'Controles del juego'));
body.push(H2('9.4 Para el docente'));
body.push(P('En Opciones se puede activar **«Mundos libres»** para desbloquear los cuatro mundos y practicar un tema concreto, y subir o bajar la dificultad de código. Las estrellas por mundo permiten ver el avance de cada estudiante en su navegador.'));
body.push(H2('9.5 Ejecutar las pruebas y regenerar este informe'));
body.push(...table(['Acción', 'Comando'], [
  ['Pruebas del intérprete, retos y mapas', '{{node BugBlaster/tests/interp.test.js}} · {{challenges.test.js}} · {{levels.test.js}}'],
  ['Prueba de la interfaz de retos', '{{node BugBlaster/tests/ui.e2e.js}}'],
  ['Bot que completa los 4 mundos', '{{node BugBlaster/tests/bot.e2e.js}}'],
  ['Capturas del juego', '{{node BugBlaster/tools/capturar.js carpeta}}'],
  ['Capturas del código y la terminal', '{{python3 BugBlaster/tools/capturar_codigo.py carpeta}} y {{node BugBlaster/tools/capturar_codigo.js carpeta}}'],
  ['Este informe', '{{node BugBlaster/tools/generar_informe.js}}'],
], [3300, 6060], 'Comandos de prueba y de generación (las pruebas e2e requieren Playwright)'));

// ---------- 10. Limitaciones
body.push(H1('10. Limitaciones y próximos pasos'));
body.push(P('Es importante ser claro con lo que **todavía no está hecho o no se ha comprobado**:'));
body.push(H2('10.1 Limitaciones actuales'));
[
  '**Subconjunto de Python.** PyMini no soporta clases, tuplas, imports, comprensiones de listas, excepciones ni cadenas de triple comilla. Es suficiente para los cuatro primeros temas, pero no para un curso completo.',
  '**Solo cuatro mundos.** Faltan listas, diccionarios, clases y algoritmos (ordenar, buscar). La estructura permite añadirlos agregando datos.',
  '**Un solo lenguaje.** Está pensado para Python; Java o JavaScript requerirían otro intérprete o una validación distinta.',
  '**No se ha probado con estudiantes reales.** La dificultad adaptativa y las pistas se basan en heurísticas razonables, pero no están calibradas con datos de uso.',
  '**Control solo con teclado y ratón.** No hay controles táctiles ni de gamepad.',
  '**Sonido y arte sencillos.** Los efectos son sintetizados y el arte es procedural; no hay música.',
  '**El segundo juego (CodeMon) no se construyó.** Quedó como diseño; la parte pedagógica del proyecto es reutilizable.',
  '**Las pruebas de extremo a extremo** necesitan Playwright instalado; el resto de las pruebas solo requiere Node.',
].forEach((t) => body.push(bullet(t)));
body.push(H2('10.2 Próximos pasos propuestos'));
body.push(...table(['Prioridad', 'Mejora', 'Descripción'], [
  ['Alta', 'Probar con estudiantes', 'Sesiones piloto para calibrar dificultad, pistas y tiempos; medir cuántos terminan cada mundo'],
  ['Alta', 'Mundos 5–7', 'Listas, diccionarios y clases/objetos; ampliar el intérprete'],
  ['Media', 'Modo Arena infinita', 'Oleadas aleatorias con una terminal cada cierto tiempo (repaso espaciado)'],
  ['Media', 'Panel docente', 'Exportar el progreso por estudiante y asignar mundos'],
  ['Media', 'Controles táctiles', 'Para tabletas y teléfonos'],
  ['Baja', 'Editor de niveles y cooperativo local', 'Uno juega la acción y otro programa en tiempo real'],
  ['Baja', 'Python real opcional', 'Cargar Pyodide para quien quiera Python completo'],
  ['Baja', 'CodeMon', 'Reutilizar el intérprete y la interfaz de retos para el RPG de criaturas'],
], [1200, 2900, 5260], 'Hoja de ruta'));

// ---------- Anexos
body.push(H1('Anexos'));
body.push(H2('A. Los 16 retos'));
const Ch = require(path.join(ROOT, 'js', 'challenges.js'));
const tipo = { completar: 'Completar', ordenar: 'Ordenar', predecir: 'Predecir', escribir: 'Escribir' };
const mundoN = { 1: 'Variables', 2: 'Condicionales', 3: 'Bucles', 4: 'Funciones' };
body.push(...table(['Id', 'Mundo', 'Título', 'Tipo', 'Rol'], Ch.LIST.map((c) => [
  `{{${c.id}}}`, `${c.world} · ${mundoN[c.world]}`, c.title, c.kind === 'corregir' ? 'Corregir' : tipo[c.type], c.boss ? 'Terminal del jefe' : c.bonus ? 'Secreta (chip)' : 'Obligatoria',
]), [900, 2300, 2760, 1500, 1900], 'Lista de retos del juego'));
body.push(H2('B. Archivos del proyecto'));
body.push(...table(['Ruta', 'Contenido'], [
  ['{{BugBlaster/index.html}}', 'Página principal del juego'],
  ['{{BugBlaster/css/style.css}}', 'Estilos'],
  ['{{BugBlaster/js/*.js}}', 'Código del juego (8 módulos)'],
  ['{{BugBlaster/tests/*.js}}', 'Pruebas automáticas (5 archivos)'],
  ['{{BugBlaster/tools/}}', 'Scripts para capturas del juego, del código y de terminal, y para generar este informe'],
  ['{{BugBlaster/docs/Informe_BugBlaster.docx}}', 'Este informe'],
], [4300, 5060], 'Estructura de carpetas'));
body.push(H2('C. Glosario'));
body.push(...table(['Término', 'Significado'], [
  ['Variable', 'Cajita con nombre donde se guarda un dato: {{vida = 100}}'],
  ['Condicional', 'Instrucción que decide qué hacer según una condición ({{if}}, {{elif}}, {{else}})'],
  ['Bucle', 'Instrucción que repite un bloque ({{for}}, {{while}})'],
  ['Función', 'Receta con nombre que se define una vez ({{def}}) y se usa muchas ({{nombre()}})'],
  ['Intérprete', 'Programa que lee código y lo ejecuta línea a línea'],
  ['Tokenizador / analizador', 'Etapas del intérprete: dividir el texto en piezas y construir un árbol con su estructura'],
  ['Canvas 2D', 'Superficie de dibujo del navegador usada para pintar el juego'],
  ['HUD', 'Información en pantalla (vida, munición…) superpuesta al juego'],
  ['Coyote time', 'Pequeño margen que permite saltar justo después de salir de un borde'],
  ['Hook', 'Función que el juego recibe de la aplicación para avisarle de algo (abrir un reto, guardar, terminar)'],
  ['Prueba e2e', 'Prueba de extremo a extremo: usa el programa completo como lo usaría una persona'],
], [2800, 6560], 'Glosario'));

// ===================================================================== DOCUMENTO
const doc = new Document({
  creator: 'BugBlaster', title: 'BugBlaster — Informe técnico de desarrollo', description: 'Informe de cómo se desarrolló BugBlaster: Operación Compilar',
  styles: {
    default: { document: { run: { font: FONT, size: 22 } } },
    paragraphStyles: [
      { id: 'Heading1', name: 'Heading 1', basedOn: 'Normal', next: 'Normal', quickFormat: true, run: { font: FONT, size: 36, bold: true, color: C.navy }, paragraph: { spacing: { before: 0, after: 200 }, keepNext: true, outlineLevel: 0, border: { bottom: { style: BorderStyle.SINGLE, size: 8, color: C.blue, space: 4 } } } },
      { id: 'Heading2', name: 'Heading 2', basedOn: 'Normal', next: 'Normal', quickFormat: true, run: { font: FONT, size: 28, bold: true, color: C.blue }, paragraph: { spacing: { before: 320, after: 120 }, keepNext: true, outlineLevel: 1 } },
      { id: 'Heading3', name: 'Heading 3', basedOn: 'Normal', next: 'Normal', quickFormat: true, run: { font: FONT, size: 24, bold: true, color: C.navy }, paragraph: { spacing: { before: 220, after: 100 }, keepNext: true, outlineLevel: 2 } },
    ],
  },
  numbering: { config: [
    { reference: 'bul', levels: [{ level: 0, format: LevelFormat.BULLET, text: '•', alignment: AlignmentType.LEFT, style: { paragraph: { indent: { left: 540, hanging: 270 } } } }, { level: 1, format: LevelFormat.BULLET, text: '–', alignment: AlignmentType.LEFT, style: { paragraph: { indent: { left: 1080, hanging: 270 } } } }] },
    ...['num', 'num2'].map((reference) => ({ reference, levels: [{ level: 0, format: LevelFormat.DECIMAL, text: '%1.', alignment: AlignmentType.LEFT, style: { paragraph: { indent: { left: 540, hanging: 360 } } } }] })),
  ] },
  sections: [{
    properties: { page: { size: { width: 12240, height: 15840 }, margin: { top: 1300, right: 1440, bottom: 1200, left: 1440 } } },
    headers: { default: new Header({ children: [new Paragraph({ alignment: AlignmentType.RIGHT, border: { bottom: { style: BorderStyle.SINGLE, size: 4, color: C.line, space: 4 } }, children: [new TextRun({ text: 'BugBlaster · Operación Compilar — Informe técnico de desarrollo', font: FONT, size: 16, color: C.gray })] })] }) },
    footers: { default: new Footer({ children: [new Paragraph({ alignment: AlignmentType.CENTER, children: [new TextRun({ text: 'Página ', font: FONT, size: 18, color: C.gray }), new TextRun({ children: [PageNumber.CURRENT], font: FONT, size: 18, color: C.gray }), new TextRun({ text: ' de ', font: FONT, size: 18, color: C.gray }), new TextRun({ children: [PageNumber.TOTAL_PAGES], font: FONT, size: 18, color: C.gray })] })] }) },
    children: body,
  }],
});

Packer.toBuffer(doc).then((buf) => {
  fs.mkdirSync(path.dirname(OUT), { recursive: true });
  fs.writeFileSync(OUT, buf);
  console.log(`✔ ${OUT} (${(buf.length / 1e6).toFixed(1)} MB) · ${figN} figuras · ${tabN} tablas`);
});
