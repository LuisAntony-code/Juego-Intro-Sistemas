// Valida estructura de los mapas: filas del mismo largo, 1 inicio, 1 salida,
// terminales = retos, un jefe, arena dentro del mapa, checkpoints, suelo bajo los enemigos.
const assert = require('assert');
const L = require('../js/levels.js');
const C = require('../js/challenges.js');
let n = 0;
function t(name, fn) { try { fn(); n++; } catch (e) { console.error('✗', name, '\n  ', e.message); process.exitCode = 1; } }
for (const w of L.WORLDS) {
  const lv = w.level;
  t(`mundo ${w.id}: estructura`, () => {
    assert.strictEqual(lv.rows.length, L.H);
    lv.rows.forEach((r, i) => assert.strictEqual(r.length, lv.width, `fila ${i} mide ${r.length} y debería medir ${lv.width}`));
    const count = (c) => lv.rows.join('').split(c).length - 1;
    assert.strictEqual(count('P'), 1, 'inicio');
    assert.strictEqual(count('E'), 1, 'salida');
    assert.strictEqual(count('B'), 1, 'jefe');
    assert.strictEqual(count('T'), w.terminals.length, 'terminales requeridas');
    assert.strictEqual(count('b'), w.bonus.length, 'terminales bonus');
    assert.ok(count('C') >= 3, 'checkpoints');
    assert.ok(count('o') >= 8, 'chips');
    assert.ok(lv.arena && lv.arena.x1 === lv.width - 1, 'arena al final');
    for (const id of w.terminals.concat(w.bonus)) assert.ok(C.byId[id], `reto ${id}`);
    assert.strictEqual(C.byId[w.terminals[w.terminals.length - 1]].boss, true, 'último terminal = jefe');
  });
  t(`mundo ${w.id}: la última terminal está dentro de la arena y las demás fuera`, () => {
    const pos = [];
    lv.rows.forEach((r) => { for (let x = 0; x < r.length; x++) if (r[x] === 'T') pos.push(x); });
    pos.sort((a, b) => a - b);
    assert.ok(pos[pos.length - 1] >= lv.arena.x0 && pos[pos.length - 1] < lv.arena.x1);
    pos.slice(0, -1).forEach((x) => assert.ok(x < lv.arena.x0));
  });
  t(`mundo ${w.id}: entidades terrestres tienen suelo debajo`, () => {
    for (let y = 0; y < L.H - 1; y++) for (let x = 0; x < lv.width; x++) {
      const c = lv.rows[y][x];
      if ('PTbCwlsBEha'.includes(c) && c !== 'f') {
        const below = lv.rows[y + 1][x];
        assert.ok(below === '#' || below === '=', `«${c}» flotando en (${x},${y}); debajo hay «${below}»`);
      }
    }
  });
  t(`mundo ${w.id}: hay suelo bajo los pinchos y no hay huecos de más de 4`, () => {
    const g = L.GROUND;
    let run = 0, max = 0;
    for (let x = 0; x < lv.width; x++) {
      if (lv.rows[g][x] === '.') { run++; max = Math.max(max, run); } else run = 0;
      if (lv.rows[g - 1][x] === '^') assert.ok(lv.rows[g][x] === '#', `pinchos sin suelo en x=${x}`);
    }
    assert.ok(max <= 4, `hueco de ${max} casillas`);
  });
}
console.log(`Niveles: ${n} comprobaciones OK` + (process.exitCode ? ' (con fallos)' : ''));
