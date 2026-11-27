// Verifica que cada reto sea resoluble (su solución pasa) y que las respuestas
// incorrectas típicas NO pasen. Ejecutar:  node BugBlaster/tests/challenges.test.js
const assert = require('assert');
const C = require('../js/challenges.js');
const PyMini = require('../js/pyinterp.js');

let n = 0;
function t(name, fn) { try { fn(); n++; } catch (e) { console.error('✗', name, '\n  ', e.message); process.exitCode = 1; } }

for (const ch of C.LIST) {
  t(`${ch.id}: la solución pasa`, () => {
    const r = C.validate(ch, C.solutionSubmission(ch));
    assert.ok(r.ok, `${ch.id} no valida su propia solución: ${r.message}`);
  });
  t(`${ch.id}: tiene 3 pistas, historia y explicación`, () => {
    assert.strictEqual(ch.hints.length, 3);
    assert.ok(ch.story && ch.explain && ch.title);
  });
  if (ch.type === 'escribir') {
    t(`${ch.id}: código vacío NO pasa`, () => {
      assert.ok(!C.validate(ch, { code: '' }).ok);
      assert.ok(!C.validate(ch, { code: '# nada' }).ok);
    });
    t(`${ch.id}: los 3 starters existen y NO son ya la solución`, () => {
      for (const s of [0, 1, 2]) {
        const st = C.starterFor(ch, s);
        assert.strictEqual(typeof st, 'string');
        if (ch.kind !== 'corregir' || s === 0) assert.ok(!C.validate(ch, { code: st }).ok, `starter ${s} ya pasa sin tocarlo`);
      }
    });
  }
  if (ch.type === 'completar' || ch.type === 'predecir') {
    t(`${ch.id}: las opciones incorrectas NO pasan y la correcta está presente`, () => {
      assert.ok(ch.choices.includes(ch.type === 'predecir' ? C.expectedOutput(ch) : ch.solution), 'la solución no está en las opciones');
      for (const c of ch.choices) {
        const ok = C.validate(ch, { value: c }).ok;
        const should = c === (ch.type === 'predecir' ? C.expectedOutput(ch) : ch.solution);
        assert.strictEqual(ok, should, `opción «${c}»`);
      }
    });
  }
  if (ch.type === 'ordenar') {
    t(`${ch.id}: ordenar — desordenado y con distractor NO pasan`, () => {
      const rev = ch.lines.slice().reverse();
      assert.ok(!C.validate(ch, { lines: rev }).ok);
      assert.ok(!C.validate(ch, { lines: ch.lines.concat(ch.extra) }).ok);
      assert.ok(!C.validate(ch, { lines: ch.lines.slice(1) }).ok);
    });
  }
}

t('w1c acepta variantes válidas', () => {
  assert.ok(C.validate(C.byId.w1c, { code: 'energia = 75\nprint(energia)' }).ok === true);
  assert.ok(!C.validate(C.byId.w1c, { code: 'print(75)' }).ok); // debe usar la variable
});
t('w2c: una solución con casos fijos NO pasa', () => {
  assert.ok(!C.validate(C.byId.w2c, { code: 'print("peligro")' }).ok);
  assert.ok(C.validate(C.byId.w2c, { code: 'if vida < 30:\n    print("peligro")\nelse:\n    print("ok")' }).ok);
  assert.ok(C.validate(C.byId.w2c, { code: 'if vida >= 30:\n    print("ok")\nelse:\n    print("peligro")' }).ok);
});
t('w3b: el bucle infinito original falla, y arreglos válidos pasan', () => {
  const ch = C.byId.w3b;
  assert.ok(!C.validate(ch, { code: C.starterFor(ch, 1) }).ok);
  assert.ok(C.validate(ch, { code: 'contador = 0\nwhile contador < 3:\n    print("pew")\n    contador += 1' }).ok);
  assert.ok(C.validate(ch, { code: 'for i in range(3):\n    print("pew")' }).ok);
});
t('w3c: cinco prints seguidos NO pasan; while sí', () => {
  const ch = C.byId.w3c;
  assert.ok(!C.validate(ch, { code: 'print(1)\nprint(2)\nprint(3)\nprint(4)\nprint(5)' }).ok);
  assert.ok(C.validate(ch, { code: 'n = 1\nwhile n <= 5:\n    print(n)\n    n += 1' }).ok);
});
t('w4c: errores se informan con línea correcta', () => {
  const r = C.validate(C.byId.w4c, { code: 'def danio(base, critico):\n    return basee * 2' });
  assert.ok(!r.ok);
  assert.ok(r.results[0].isError && r.results[0].got.includes('Línea 2'), r.results[0].got);
});
t('w2b: el starter original reporta el error de «==»', () => {
  const r = C.validate(C.byId.w2b, { code: C.starterFor(C.byId.w2b, 1) });
  assert.ok(!r.ok && /==/.test(r.message), r.message);
});

console.log(`Retos: ${n} comprobaciones OK (${C.LIST.length} retos)` + (process.exitCode ? ' (con fallos)' : ''));
