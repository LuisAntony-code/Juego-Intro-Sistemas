// Pruebas del mini-intérprete de Python. Ejecutar:  node BugBlaster/tests/interp.test.js
const assert = require('assert');
const PyMini = require('../js/pyinterp.js');

let passed = 0;
function out(code) { const r = PyMini.run(code); assert.ok(r.ok, `Debía funcionar:\n${code}\n→ ${r.error && r.error.msg}`); return r.output.trim(); }
function err(code, type, frag) {
  const r = PyMini.run(code);
  assert.ok(!r.ok, `Debía fallar:\n${code}`);
  assert.strictEqual(r.error.type, type, `Tipo de error: ${r.error.type} (${r.error.msg})`);
  if (frag) assert.ok(r.error.msg.includes(frag), `Mensaje «${r.error.msg}» no contiene «${frag}»`);
  return r.error;
}
function t(name, fn) { try { fn(); passed++; } catch (e) { console.error('✗', name, '\n  ', e.message); process.exitCode = 1; } }

t('variables y aritmética', () => {
  assert.strictEqual(out('vida = 100\ndano = 30\nvida = vida - dano\nprint(vida)'), '70');
  assert.strictEqual(out('print(7 // 2, 7 % 2, 2 ** 10, 7 / 2)'), '3 1 1024 3.5');
  assert.strictEqual(out('print(4 / 2)'), '2.0');
  assert.strictEqual(out('print(-7 // 2, -7 % 3)'), '-4 2');
  assert.strictEqual(out('x = 5\nx += 3\nx *= 2\nprint(x)'), '16');
});
t('textos y f-strings', () => {
  assert.strictEqual(out('n = "Robo"\nprint("Hola " + n)'), 'Hola Robo');
  assert.strictEqual(out('v = 3\nprint(f"vida: {v * 2} y {v:.2f}")'), 'vida: 6 y 3.00');
  assert.strictEqual(out('print("ab" * 3)'), 'ababab');
  assert.strictEqual(out('print(len("hola"), "hola".upper())'), '4 HOLA');
});
t('if / elif / else', () => {
  const c = (e) => `energia = ${e}\nif energia > 80:\n    print("turbo")\nelif energia > 30:\n    print("normal")\nelse:\n    print("ahorro")`;
  assert.strictEqual(out(c(90)), 'turbo');
  assert.strictEqual(out(c(50)), 'normal');
  assert.strictEqual(out(c(10)), 'ahorro');
  assert.strictEqual(out('x = 3\nif x > 1 and not x > 5: print("ok")'), 'ok');
  assert.strictEqual(out('print("a" if 2 > 1 else "b")'), 'a');
});
t('bucles', () => {
  assert.strictEqual(out('for i in range(3):\n    print("pew")'), 'pew\npew\npew');
  assert.strictEqual(out('total = 0\nfor i in range(4):\n    total = total + 2\nprint(total)'), '8');
  assert.strictEqual(out('c = 0\nwhile c < 3:\n    c = c + 1\nprint(c)'), '3');
  assert.strictEqual(out('for n in range(1, 6):\n    print(n)'), '1\n2\n3\n4\n5');
  assert.strictEqual(out('for i in range(10):\n    if i == 2:\n        break\n    print(i)'), '0\n1');
  assert.strictEqual(out('for i in range(3):\n    if i == 1:\n        continue\n    print(i)'), '0\n2');
  assert.strictEqual(out('for i in range(3, 0, -1):\n    print(i)'), '3\n2\n1');
});
t('funciones', () => {
  assert.strictEqual(out('def doble(n):\n    return n * 2\n\nprint(doble(4) + 1)'), '9');
  assert.strictEqual(out('def saludar():\n    print("hola")\n\nsaludar()'), 'hola');
  assert.strictEqual(out('def f(a, b=10):\n    return a + b\nprint(f(1), f(1, 2), f(1, b=5))'), '11 3 6');
  assert.strictEqual(out('def fact(n):\n    if n <= 1:\n        return 1\n    return n * fact(n - 1)\nprint(fact(5))'), '120');
  assert.strictEqual(out('x = 1\ndef f():\n    global x\n    x = 5\nf()\nprint(x)'), '5');
  const r = PyMini.run('def triple(n):\n    return n * 3');
  assert.strictEqual(r.evalExpr('triple(4)').value, '12');
});
t('listas y diccionarios', () => {
  assert.strictEqual(out('a = [3, 1, 2]\na.append(9)\na.sort()\nprint(a, len(a), a[0], a[-1])'), '[1, 2, 3, 9] 4 1 9');
  assert.strictEqual(out('d = {"vida": 100}\nd["vida"] = 90\nprint(d, d["vida"])'), "{'vida': 90} 90");
  assert.strictEqual(out('print(sum([1, 2, 3]), max(4, 9, 2), min([5, 3]), sorted([3, 1, 2]))'), '6 9 3 [1, 2, 3]');
  assert.strictEqual(out('a = [1, 2, 3, 4]\nprint(a[1:3], "hola"[1:])'), '[2, 3] ola');
  assert.strictEqual(out('print(2 in [1, 2], "a" in "casa", "x" not in "casa")'), 'True True True');
  assert.strictEqual(out('print(True, None, 1 == 1.0, "a" < "b")'), 'True None True True');
});
t('print con sep/end y repr', () => {
  assert.strictEqual(out('print(1, 2, sep="-", end="!")'), '1-2!');
  assert.strictEqual(out('print(["a", 1.5, None])'), "['a', 1.5, None]");
});
t('errores amigables', () => {
  assert.ok(err('print(vidda)', 'NameError').msg.length > 0);
  assert.ok(err('vida = 5\nprint(vidda)', 'NameError', 'vida').msg.includes('Quisiste decir'));
  err('if x = 3:\n    pass', 'SyntaxError', '==');
  err('if True\n    pass', 'SyntaxError', ':');
  err('if True:\nprint(1)', 'SyntaxError', 'sangría');
  err('print("hola)', 'SyntaxError', 'comillas');
  err('print("a" + 1)', 'TypeError', 'str(');
  err('print(1 / 0)', 'ZeroDivisionError');
  err('a = [1]\nprint(a[5])', 'IndexError');
  err('while True:\n    pass', 'Timeout');
  err('contador = 0\nwhile contador < 3:\n    print("pew")', 'Timeout');
  err('def f():\n    return f()\nf()', 'RecursionError');
  err('x = 5 +', 'SyntaxError');
  err('5 = x', 'SyntaxError');
  err('import os', 'SyntaxError');
  err('return 3', 'SyntaxError');
  assert.strictEqual(err('x = 1\nprint(y)', 'NameError').line, 2);
});
t('lineOffset', () => {
  const r = PyMini.run('vida = 20\nprint(zzz)', { lineOffset: 1 });
  assert.strictEqual(r.error.line, 1);
});
t('límite de salida', () => {
  const r = PyMini.run('for i in range(100000):\n    print("xxxxxxxxxx")');
  assert.ok(!r.ok);
});

console.log(`Intérprete: ${passed} grupos de pruebas OK` + (process.exitCode ? ' (con fallos)' : ''));
