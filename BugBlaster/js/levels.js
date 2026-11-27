/* ============================================================================
 * Mundos y mapas de BugBlaster.
 *
 * Los mapas se construyen con un pequeño "constructor" (suelo, huecos,
 * plataformas, pinchos…) para evitar errores a mano y garantizar que todo
 * sea alcanzable con las habilidades base: saltar 3 casillas de alto y
 * cruzar huecos de hasta 4 casillas. Dash y doble salto solo abren rutas
 * OPCIONALES (chips extra).
 *
 * Caracteres en el mapa final:
 *   #  bloque sólido      =  plataforma (se cruza desde abajo)    ^  pinchos
 *   P  inicio             E  portal de salida     C  checkpoint
 *   T  terminal (reto)    b  terminal bonus (chip secreto)
 *   w  bicho caminante    f  fantasma volador     s  torreta
 *   l  bicho-bucle        B  jefe
 *   o  chip de datos      a  munición             h  botiquín
 * ========================================================================== */
(function (root) {
  'use strict';
  const H = 17;
  const GROUND = 14; // fila superior del suelo

  function build(width, fn) {
    const g = Array.from({ length: H }, () => Array(width).fill('.'));
    const signs = [];
    let arena = null;
    const set = (x, y, c) => { if (x >= 0 && x < width && y >= 0 && y < H) g[y][x] = c; };
    const api = {
      ground(x, w, top = GROUND) { for (let i = x; i < x + w; i++) for (let y = top; y < H; y++) set(i, y, '#'); },
      carve(x, w) { for (let i = x; i < x + w; i++) for (let y = GROUND; y < H; y++) set(i, y, '.'); },
      block(x, top, w, h = 1) { for (let i = x; i < x + w; i++) for (let y = top; y < top + h; y++) set(i, y, '#'); },
      plat(x, row, w) { for (let i = x; i < x + w; i++) set(i, row, '='); },
      spikes(x, w, row = GROUND - 1) { for (let i = x; i < x + w; i++) set(i, row, '^'); },
      ent(c, x, row = GROUND - 1) { set(x, row, c); },
      sign(x, row, text) { signs.push({ x, row, text }); },
      // Arena del jefe: [x0, x0+w) con muro derecho. La puerta izquierda se cierra al entrar.
      arena(x0, w) {
        arena = { x0, x1: x0 + w };
        for (let y = 0; y < GROUND; y++) set(x0 + w, y, '#');
      },
    };
    api.ground(0, width);
    fn(api);
    return { rows: g.map((r) => r.join('')), signs, arena, width, height: H };
  }

  const WORLDS = [
    // ----------------------------------------------------------------- 1
    {
      id: 1, name: 'Núcleo de Datos', concept: 'Variables', icon: '📦',
      theme: { sky1: '#071a2e', sky2: '#0f3a57', tile: '#1c4a68', tileTop: '#39d0ff', accent: '#39d0ff', glow: '#7df9ff', far: '#0c2c47' },
      intro: [
        'La computadora central fue infectada por el <b>Bug Variable</b>.',
        'Tu robot perdió su munición y su energía. Hackea las <b>terminales</b> para reprogramarlo: aquí aprenderás <b>variables</b>, cajitas con nombre donde guardar datos.',
      ],
      boss: { name: 'Bug Variable', style: 'var', hp: 24, color: '#ff5d73' },
      terminals: ['w1a', 'w1b', 'w1c'], bonus: ['w1x'],
      sample: 'municion = 10\nvida = vida - dano\nenergia = energia + 25',
      level: build(137, (b) => {
        b.ent('P', 3);
        b.sign(5, 13, 'A / D (o ← →) para moverte · W, ↑ o Espacio para saltar');
        b.sign(11, 13, 'Apunta con el ratón y haz clic (o pulsa J) para disparar');
        b.ent('w', 16); b.ent('o', 19); b.ent('o', 20);
        b.sign(22, 13, 'Acércate a una terminal y pulsa E para hackearla');
        b.ent('T', 24); b.ent('a', 27);
        b.carve(30, 3); b.ent('o', 31, 10);
        b.ent('w', 36); b.ent('C', 40);
        b.block(44, 13, 3, 1); b.plat(49, 12, 4); b.ent('o', 50, 11); b.ent('o', 51, 11);
        b.ent('w', 54); b.ent('o', 56); b.spikes(58, 2); b.ent('a', 62);
        b.carve(65, 3); b.ent('o', 66, 10);
        b.ent('h', 70); b.ent('T', 72); b.ent('w', 77); b.ent('w', 81);
        b.block(85, 12, 3, 2); b.ent('o', 86, 11);
        b.plat(90, 10, 4); b.ent('b', 91, 9);
        b.ent('w', 95); b.ent('o', 97); b.ent('w', 99); b.ent('a', 100); b.ent('h', 102);
        b.ent('C', 104);
        // Arena del jefe
        b.arena(108, 28);
        b.ent('C', 109); b.ent('T', 113); b.ent('B', 128); b.ent('E', 133);
      }),
    },
    // ----------------------------------------------------------------- 2
    {
      id: 2, name: 'Sala de Decisiones', concept: 'Condicionales', icon: '🔀',
      theme: { sky1: '#1c0a2e', sky2: '#4a1550', tile: '#5b2a6e', tileTop: '#ff7ad9', accent: '#ff7ad9', glow: '#ffd36b', far: '#2d1042' },
      intro: [
        'El <b>Rey Si-Entonces</b> gobierna esta sala: todo depende de una condición.',
        'Aprenderás a tomar decisiones con <code>if</code>, <code>elif</code> y <code>else</code>. Tu robot podrá cambiar de arma solo, según la distancia al enemigo.',
      ],
      boss: { name: 'Rey Si-Entonces', style: 'cond', hp: 30, color: '#ffb347' },
      terminals: ['w2a', 'w2b', 'w2c'], bonus: ['w2x'],
      sample: 'if distancia < 3:\n    arma = "escopeta"\nelse:\n    arma = "rifle"',
      level: build(148, (b) => {
        b.ent('P', 3);
        b.sign(5, 13, 'Un «if» decide: si se cumple una condición, haz esto; si no, haz lo otro.');
        b.ent('f', 14, 8); b.ent('w', 20); b.ent('o', 22);
        b.ent('T', 26); b.ent('a', 29); b.ent('C', 31);
        b.carve(35, 3); b.plat(39, 12, 3); b.ent('o', 40, 11);
        b.sign(44, 13, 'Dos caminos: abajo (pinchos y bichos) o arriba (plataformas y chips).');
        // Camino inferior
        b.spikes(48, 2); b.ent('w', 53); b.ent('s', 60);
        // Camino superior
        b.plat(47, 12, 4); b.plat(53, 11, 4); b.plat(59, 12, 4);
        b.ent('o', 48, 11); b.ent('o', 54, 10); b.ent('o', 55, 10); b.ent('o', 60, 11);
        b.ent('f', 51, 7);
        b.ent('T', 67); b.ent('h', 70);
        b.carve(74, 4); b.ent('o', 75, 10);
        b.ent('w', 81); b.ent('f', 85, 8);
        b.block(88, 12, 3, 2); b.ent('s', 89, 11);
        b.ent('w', 93); b.ent('o', 96); b.ent('a', 95);
        b.block(98, 12, 3, 2); b.plat(103, 10, 4); b.ent('b', 104, 9);
        b.ent('w', 108); b.ent('f', 111, 8); b.ent('h', 113);
        b.ent('C', 115);
        b.arena(119, 28);
        b.ent('C', 120); b.ent('T', 124); b.ent('B', 139); b.ent('E', 144);
      }),
    },
    // ----------------------------------------------------------------- 3
    {
      id: 3, name: 'Fábrica de Repeticiones', concept: 'Bucles', icon: '🔁',
      theme: { sky1: '#0a2210', sky2: '#1c4a1c', tile: '#2d5a2a', tileTop: '#9dff6b', accent: '#9dff6b', glow: '#fff36b', far: '#10331a' },
      intro: [
        'En la <b>Fábrica de Repeticiones</b> todo se repite… ¡y los bichos reaparecen en bucle!',
        'Con <code>for</code> y <code>while</code> harás que tu robot dispare ráfagas y romperás el bucle infinito.',
      ],
      boss: { name: 'Bucle Maestro', style: 'loop', hp: 34, color: '#7dff7a' },
      terminals: ['w3a', 'w3b', 'w3c'], bonus: ['w3x'],
      sample: 'for i in range(3):\n    disparar()',
      level: build(150, (b) => {
        b.ent('P', 3);
        b.sign(5, 13, 'Los bichos-gusano reaparecen en bucle… hasta que lo rompas con código.');
        b.ent('w', 10); b.ent('T', 16); b.ent('a', 19); b.ent('h', 20);
        // Tres tramos que se repiten (como un bucle)
        const seg = (x0, extra) => {
          b.ent('l', x0 + 2); b.ent('o', x0 + 4);
          b.carve(x0 + 7, 3); b.ent('o', x0 + 8, 10);
          b.plat(x0 + 12, 12, 4); b.ent('o', x0 + 13, 11);
          b.spikes(x0 + 18, 2);
          b.ent('w', x0 + 22);
          if (extra) extra(x0);
        };
        seg(24, (x0) => { b.ent('f', x0 + 15, 8); });
        b.ent('C', 48); b.ent('T', 52);
        seg(56, (x0) => { b.ent('f', x0 + 10, 8); b.plat(x0 + 17, 10, 3); b.ent('b', x0 + 18, 9); b.ent('s', x0 + 24); });
        b.ent('h', 83); b.ent('C', 85);
        seg(88, (x0) => { b.ent('f', x0 + 6, 8); b.ent('f', x0 + 16, 7); b.ent('s', x0 + 21); });
        b.ent('a', 113); b.ent('h', 114); b.ent('C', 117);
        b.arena(121, 28);
        b.ent('C', 122); b.ent('T', 126); b.ent('B', 141); b.ent('E', 146);
      }),
    },
    // ----------------------------------------------------------------- 4
    {
      id: 4, name: 'Ciudad de Funciones', concept: 'Funciones', icon: '🏙️',
      theme: { sky1: '#12102e', sky2: '#2b2a6e', tile: '#3a3a8a', tileTop: '#8f9bff', accent: '#8f9bff', glow: '#ff9bd5', far: '#1a1848' },
      intro: [
        'La <b>Ciudad de Funciones</b> funciona con recetas reutilizables… hasta que la <b>Función Maestra</b> las corrompió.',
        'Aprenderás a <b>definir</b> y <b>llamar</b> funciones. ¡Y ganarás dash, doble salto y golpes críticos!',
      ],
      boss: { name: 'Función Maestra', style: 'func', hp: 40, color: '#c77dff' },
      terminals: ['w4a', 'w4b', 'w4c'], bonus: ['w4x'],
      sample: 'def danio(base, critico):\n    if critico:\n        return base * 2\n    return base',
      level: build(163, (b) => {
        b.ent('P', 3);
        b.sign(5, 13, 'Una función es una receta con nombre: la defines una vez y la usas muchas veces.');
        b.ent('T', 12); b.ent('w', 18); b.ent('a', 20);
        b.sign(22, 13, 'Con DASH (Shift) llegas a chips lejanos. Con doble salto, a los más altos.');
        b.carve(26, 4); b.ent('o', 28, 9);
        b.plat(33, 12, 3); b.plat(37, 10, 3); b.plat(41, 8, 3);
        b.ent('o', 34, 11); b.ent('o', 38, 9); b.ent('b', 42, 7);
        b.ent('s', 40); b.ent('o', 47); b.ent('C', 45);
        b.ent('w', 50); b.ent('f', 54, 9); b.ent('s', 60);
        b.carve(64, 3); b.ent('o', 65, 10);
        b.ent('T', 71); b.ent('h', 73); b.ent('C', 75);
        b.spikes(79, 3); b.ent('o', 80, 11);
        b.ent('w', 86); b.ent('w', 89); b.ent('f', 92, 8);
        b.carve(96, 4); b.ent('o', 98, 9);
        b.block(103, 12, 3, 2); b.ent('s', 104, 11);
        b.ent('f', 108, 8); b.ent('w', 112); b.ent('a', 114);
        b.plat(117, 12, 3); b.plat(121, 10, 3); b.ent('o', 122, 9);
        b.ent('w', 126); b.ent('h', 128); b.ent('C', 130);
        b.arena(134, 28);
        b.ent('C', 135); b.ent('T', 139); b.ent('B', 154); b.ent('E', 159);
      }),
    },
  ];

  root.Levels = { WORLDS, H, GROUND };
  if (typeof module === 'object' && module.exports) module.exports = root.Levels;
})(typeof self !== 'undefined' ? self : this);
