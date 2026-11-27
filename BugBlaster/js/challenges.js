/* ============================================================================
 * Retos de programación (Python) y su validación.
 *
 * Tipos de reto:
 *   completar → rellena el hueco «___» (con opciones o escribiendo)
 *   ordenar   → ordena las líneas para que el programa dé la salida pedida
 *   predecir  → ¿qué imprimirá este código? (la respuesta se calcula ejecutándolo)
 *   escribir  → escribe (o corrige) código; se prueba con casos de prueba reales
 *
 * La validación EJECUTA el código con PyMini: nada de respuestas "de memoria".
 * Dificultad (skill): 0 = guiado, 1 = normal, 2 = experto.
 * ========================================================================== */
(function (root, factory) {
  const PM = (typeof module === 'object' && module.exports) ? require('./pyinterp.js') : root.PyMini;
  const api = factory(PM);
  if (typeof module === 'object' && module.exports) module.exports = api;
  else root.Challenges = api;
})(typeof self !== 'undefined' ? self : this, function (PyMini) {
  'use strict';

  const normOut = (s) => String(s).replace(/\r/g, '').split('\n').map((l) => l.replace(/\s+$/, '')).join('\n').replace(/^\n+|\n+$/g, '');
  const BLANK = '___';

  const LIST = [
    // =============================================== MUNDO 1 · VARIABLES ====
    {
      id: 'w1a', world: 1, type: 'completar', bug: 'NameError',
      title: 'Munición',
      story: 'El Bug Variable borró tu munición. ¡Guarda 10 balas en una variable llamada <b>municion</b>!',
      code: 'municion = ___',
      choices: ['10', '"balas"', 'diez'],
      solution: '10',
      expectOut: null, accept: ['10'],
      explain: 'Una <b>variable</b> es una cajita con nombre. Aquí <code>municion</code> guarda el número 10, y tu robot ya puede cargar más balas.',
      hints: [
        'Una variable guarda un valor. ¿Qué valor es un número que puedes contar?',
        'El valor va a la derecha del signo «=». Quieres 10 balas.',
        'Escribe 10 en el hueco: <code>municion = 10</code>',
      ],
      reward: 'municion_max = 12',
    },
    {
      id: 'w1b', world: 1, type: 'ordenar', bug: 'Orden de ejecución',
      title: 'Cálculo de vida',
      story: 'Para curarte, tu robot calcula la vida que le queda tras un golpe. <b>Ordena las líneas</b> para que el programa muestre <code>70</code>.',
      lines: ['vida = 100', 'dano = 30', 'vida = vida - dano', 'print(vida)'],
      extra: ['vida = 0'],
      expectOut: '70',
      solution: ['vida = 100', 'dano = 30', 'vida = vida - dano', 'print(vida)'],
      explain: 'Python lee el código <b>de arriba hacia abajo</b>: primero hay que crear las variables y después usarlas.',
      hints: [
        'Python ejecuta línea por línea, de arriba hacia abajo. Una variable debe existir antes de usarla.',
        'Primero crea <code>vida</code> y <code>dano</code>. Luego resta. Al final, muestra el resultado.',
        'Orden: vida = 100 · dano = 30 · vida = vida - dano · print(vida)',
      ],
      reward: 'vida_max = 120',
    },
    {
      id: 'w1c', world: 1, type: 'escribir', bug: 'Jefe: Bug Variable', boss: true,
      title: 'Terminal del Jefe',
      story: '¡Hackea al jefe! Crea la variable <b>energia</b> con valor 50, súmale 25 y muéstrala con <code>print</code>. Debe imprimir <code>75</code>.',
      starter: {
        0: 'energia = ___\nenergia = energia + ___\nprint(___)',
        1: '# 1) crea energia con valor 50\n# 2) súmale 25\n# 3) imprímela\n',
        2: '',
      },
      tests: [{ label: 'imprime 75', expectOut: '75' }],
      require: [{ re: /energia\s*=/, msg: 'Crea la variable <code>energia</code> con «=».' }, { re: /print\s*\(/, msg: 'Usa <code>print(...)</code> para mostrar el resultado.' }],
      solution: 'energia = 50\nenergia = energia + 25\nprint(energia)',
      explain: 'Las variables pueden cambiar: <code>energia = energia + 25</code> lee el valor viejo, le suma 25 y lo guarda otra vez.',
      hints: [
        'Son tres pasos: crear la variable, sumarle, y mostrarla.',
        'Para sumar a una variable: <code>energia = energia + 25</code>. Para mostrarla: <code>print(energia)</code>.',
        'energia = 50 · energia = energia + 25 · print(energia)',
      ],
      reward: 'energia_max = 75',
    },
    {
      id: 'w1x', world: 1, type: 'predecir', bug: 'Chip secreto', bonus: true,
      title: 'Chip secreto',
      story: 'Un chip de datos se abre si adivinas la salida. <b>¿Qué imprime este código?</b>',
      code: 'energia = 5\nenergia = energia + 3\nenergia = energia * 2\nprint(energia)',
      choices: ['8', '16', '10', '13'],
      solution: '16',
      explain: 'Paso a paso: 5 → 5 + 3 = 8 → 8 × 2 = 16.',
      hints: ['Sigue el código línea por línea y anota el valor de <code>energia</code> en cada paso.', 'Empieza en 5. Luego +3, y después ×2.', 'La respuesta es 16.'],
      reward: 'chip',
    },

    // ========================================= MUNDO 2 · CONDICIONALES ====
    {
      id: 'w2a', world: 2, type: 'completar', bug: 'Fantasma Null',
      title: 'Escopeta cercana',
      story: 'Un Fantasma Null bloquea la puerta. Si está <b>cerca</b> (distancia menor que 3), el robot debe cambiar a escopeta. ¿Qué operador falta?',
      code: 'distancia = 2\nif distancia ___ 3:\n    print("escopeta")\nelse:\n    print("rifle")',
      choices: ['<', '>', '=='],
      expectOut: 'escopeta', solution: '<',
      explain: 'Un <b>if</b> decide con una condición. <code>2 &lt; 3</code> es verdadero, así que se ejecuta el bloque del <code>if</code> y no el del <code>else</code>.',
      hints: [
        'Queremos escopeta cuando la distancia es MENOR que 3.',
        'El símbolo «&lt;» significa «menor que».',
        'Escribe &lt; en el hueco: <code>if distancia &lt; 3:</code>',
      ],
      reward: 'escopeta',
    },
    {
      id: 'w2b', world: 2, type: 'escribir', kind: 'corregir', bug: 'Error de sintaxis',
      title: 'Compuerta rota',
      story: 'El Error de Sintaxis rompió esta compuerta. <b>Corrige el código</b> para que imprima <code>a mitad</code>.',
      starter: {
        0: 'vida = 40\nif vida = 40:   # ← aquí está el error\n    print("a mitad")\nelse:\n    print("otra cosa")',
        1: 'vida = 40\nif vida = 40:\n    print("a mitad")\nelse:\n    print("otra cosa")',
        2: 'vida = 40\nif vida = 40:\n    print("a mitad")\nelse:\n    print("otra cosa")',
      },
      tests: [{ label: 'imprime a mitad', expectOut: 'a mitad' }],
      solution: 'vida = 40\nif vida == 40:\n    print("a mitad")\nelse:\n    print("otra cosa")',
      explain: 'Un solo «=» <b>guarda</b> un valor. Dos «==» <b>comparan</b>. En un <code>if</code> casi siempre quieres comparar.',
      hints: [
        'Lee el mensaje de error: te dice qué signo usar para comparar.',
        'En el <code>if</code> estás comparando, no guardando. Cambia «=» por otra cosa.',
        'Debe ser <code>if vida == 40:</code>',
      ],
      reward: 'rifle',
    },
    {
      id: 'w2c', world: 2, type: 'escribir', bug: 'Jefe: Si-Entonces', boss: true,
      title: 'Terminal del Jefe',
      story: 'La variable <b>vida</b> ya existe. Escribe un <b>if / else</b>: si <code>vida &lt; 30</code> imprime <code>peligro</code>; si no, imprime <code>ok</code>.',
      starter: {
        0: 'if vida ___ 30:\n    print("___")\nelse:\n    print("___")',
        1: '# La variable vida ya existe.\n# Escribe tu if / else aquí:\n',
        2: '',
      },
      tests: [
        { label: 'vida = 20', prelude: 'vida = 20\n', expectOut: 'peligro' },
        { label: 'vida = 90', prelude: 'vida = 90\n', expectOut: 'ok' },
        { label: 'vida = 29', prelude: 'vida = 29\n', expectOut: 'peligro' },
        { label: 'vida = 30', prelude: 'vida = 30\n', expectOut: 'ok' },
      ],
      require: [{ re: /\bif\b/, msg: 'Usa un <code>if</code> para decidir.' }],
      solution: 'if vida < 30:\n    print("peligro")\nelse:\n    print("ok")',
      explain: 'Tu programa funciona con <b>cualquier</b> valor de vida: el robot decide solo. ¡Eso es programar!',
      hints: [
        'Estructura: <code>if condición:</code> y debajo (con 4 espacios) lo que se hace; luego <code>else:</code>.',
        'La condición es <code>vida &lt; 30</code>. Los textos van entre comillas dentro de print.',
        'if vida &lt; 30: ⏎ &nbsp;&nbsp;&nbsp;&nbsp;print("peligro") ⏎ else: ⏎ &nbsp;&nbsp;&nbsp;&nbsp;print("ok")',
      ],
      reward: 'escudo',
    },
    {
      id: 'w2x', world: 2, type: 'ordenar', bug: 'Chip secreto', bonus: true,
      title: 'Chip secreto',
      story: 'Ordena la cadena de decisiones para que el robot imprima <code>normal</code>.',
      lines: ['energia = 50', 'if energia > 80:', '    print("turbo")', 'elif energia > 30:', '    print("normal")', 'else:', '    print("ahorro")'],
      extra: ['    print("max")'],
      expectOut: 'normal',
      solution: ['energia = 50', 'if energia > 80:', '    print("turbo")', 'elif energia > 30:', '    print("normal")', 'else:', '    print("ahorro")'],
      explain: '<code>elif</code> significa «si no, pero si…». Python revisa las condiciones en orden y ejecuta solo la primera verdadera.',
      hints: ['Primero se crea la variable. Luego va el <code>if</code>, después los <code>elif</code>, y al final el <code>else</code>.', 'Cada <code>print</code> va justo debajo de su condición.', 'Sigue el orden en que está en la solución: energia, if, turbo, elif, normal, else, ahorro.'],
      reward: 'chip',
    },

    // ================================================== MUNDO 3 · BUCLES ====
    {
      id: 'w3a', world: 3, type: 'completar', bug: 'Bug Repetidor',
      title: 'Ráfaga de 3',
      story: 'Tu robot debe disparar una <b>ráfaga de 3 balas</b>. ¿Cuántas veces tiene que repetir el bucle?',
      code: 'for i in range(___):\n    print("pew")',
      choices: ['2', '3', '4'],
      expectOut: 'pew\npew\npew', solution: '3',
      explain: 'Un <b>for</b> repite un bloque. <code>range(3)</code> hace que el bloque se ejecute 3 veces: ¡una ráfaga!',
      hints: ['Cuenta cuántas veces quieres ver «pew».', 'range(N) repite exactamente N veces.', 'Escribe 3 en el hueco.'],
      reward: 'rafaga3',
    },
    {
      id: 'w3b', world: 3, type: 'escribir', kind: 'corregir', bug: 'Bucle Infinito',
      title: 'El bucle que no termina',
      story: 'Un <b>Bucle Infinito</b> bloquea la fábrica: este código nunca termina. <b>Arréglalo</b> para que imprima «pew» 3 veces y se detenga.',
      starter: {
        0: 'contador = 0\nwhile contador < 3:\n    print("pew")\n    # ← falta algo aquí que cambie contador',
        1: 'contador = 0\nwhile contador < 3:\n    print("pew")',
        2: 'contador = 0\nwhile contador < 3:\n    print("pew")',
      },
      tests: [{ label: 'imprime pew 3 veces', expectOut: 'pew\npew\npew' }],
      solution: 'contador = 0\nwhile contador < 3:\n    print("pew")\n    contador = contador + 1',
      explain: 'Un <code>while</code> sigue mientras su condición sea verdadera. Si <code>contador</code> nunca cambia, ¡nunca termina! Hay que actualizarlo dentro del bucle.',
      hints: ['La condición es <code>contador &lt; 3</code>. ¿Qué pasa con <code>contador</code> en cada vuelta?', 'Dentro del bucle falta una línea que <b>aumente</b> <code>contador</code>.', 'Agrega, con 4 espacios: <code>contador = contador + 1</code>'],
      reward: 'romper_bucles',
    },
    {
      id: 'w3c', world: 3, type: 'escribir', bug: 'Jefe: Bucle Maestro', boss: true,
      title: 'Terminal del Jefe',
      story: 'Imprime los números del <b>1 al 5</b>, uno por línea, usando un <b>bucle</b>.',
      starter: {
        0: 'for numero in range(___, ___):\n    print(numero)',
        1: '# Usa un bucle for con range(...)\n',
        2: '',
      },
      tests: [{ label: 'imprime 1 al 5', expectOut: '1\n2\n3\n4\n5' }],
      require: [{ re: /\b(for|while)\b/, msg: 'Usa un bucle (<code>for</code> o <code>while</code>) en vez de escribir cinco print.' }],
      solution: 'for n in range(1, 6):\n    print(n)',
      explain: '<code>range(1, 6)</code> empieza en 1 y se detiene <i>antes</i> del 6. ¡Cinco vueltas!',
      hints: ['Un <code>for</code> con <code>range</code> cuenta por ti.', '<code>range(a, b)</code> va desde a hasta b-1. Para llegar al 5, b debe ser 6.', 'for n in range(1, 6): ⏎ &nbsp;&nbsp;&nbsp;&nbsp;print(n)'],
      reward: 'rafaga5',
    },
    {
      id: 'w3x', world: 3, type: 'predecir', bug: 'Chip secreto', bonus: true,
      title: 'Chip secreto',
      story: '¿Qué imprime este bucle?',
      code: 'total = 0\nfor i in range(4):\n    total = total + 2\nprint(total)',
      choices: ['4', '6', '8', '10'],
      solution: '8',
      explain: 'El bucle da 4 vueltas y en cada una suma 2: 2 + 2 + 2 + 2 = 8.',
      hints: ['¿Cuántas vueltas da <code>range(4)</code>?', 'Cada vuelta suma 2 a <code>total</code>.', '4 vueltas × 2 = 8.'],
      reward: 'chip',
    },

    // ================================================= MUNDO 4 · FUNCIONES ====
    {
      id: 'w4a', world: 4, type: 'completar', bug: 'Función sin llamar',
      title: 'Llamar a la función',
      story: 'Definiste una función pero tu robot no obedece: ¡hay que <b>llamarla</b>! ¿Qué va después de <code>saludar</code>?',
      code: 'def saludar():\n    print("hola")\n\nsaludar___',
      choices: ['()', '[]', ':'],
      expectOut: 'hola', solution: '()',
      explain: '<code>def</code> <b>crea</b> la función; los paréntesis <code>()</code> la <b>ejecutan</b>. Una función es una receta que puedes usar muchas veces.',
      hints: ['Definir una función no la ejecuta todavía.', 'Para ejecutarla se escriben paréntesis después de su nombre.', 'Escribe () en el hueco: <code>saludar()</code>'],
      reward: 'dash',
    },
    {
      id: 'w4b', world: 4, type: 'ordenar', bug: 'Función desordenada',
      title: 'Cuadrado',
      story: 'Ordena el código para que imprima el cuadrado de 5: <code>25</code>.',
      lines: ['def cuadrado(n):', '    return n * n', 'print(cuadrado(5))'],
      extra: ['    return n + n'],
      expectOut: '25',
      solution: ['def cuadrado(n):', '    return n * n', 'print(cuadrado(5))'],
      explain: '<code>return</code> devuelve el resultado de la función. Primero se <b>define</b> la función y luego se <b>usa</b>.',
      hints: ['Una función se define antes de usarla.', 'El <code>return</code> va dentro de la función (con sangría).', 'def, return, y por último el print.'],
      reward: 'doble_salto',
    },
    {
      id: 'w4c', world: 4, type: 'escribir', bug: 'Jefe: Función Maestra', boss: true,
      title: 'Terminal del Jefe',
      story: 'Crea la función <b>danio(base, critico)</b>: si <code>critico</code> es <code>True</code> devuelve el <b>doble</b> de <code>base</code>; si no, devuelve <code>base</code>.',
      starter: {
        0: 'def danio(base, critico):\n    if critico:\n        return ___\n    else:\n        return ___',
        1: '# Define la función danio(base, critico)\n',
        2: '',
      },
      tests: [
        { label: 'danio(10, True)', call: 'danio(10, True)', expect: '20' },
        { label: 'danio(10, False)', call: 'danio(10, False)', expect: '10' },
        { label: 'danio(7, True)', call: 'danio(7, True)', expect: '14' },
      ],
      require: [{ re: /def\s+danio\s*\(/, msg: 'Define la función con <code>def danio(base, critico):</code>' }],
      solution: 'def danio(base, critico):\n    if critico:\n        return base * 2\n    else:\n        return base',
      explain: '¡Combinaste <b>función + parámetros + if</b>! Eso es lo que hacen los videojuegos de verdad para calcular el daño.',
      hints: [
        'Una función recibe datos (parámetros) y <code>return</code> devuelve un resultado.',
        'Dentro usa un <code>if critico:</code>. Si es verdadero devuelve <code>base * 2</code>, si no <code>base</code>.',
        'def danio(base, critico): ⏎ &nbsp;&nbsp;&nbsp;&nbsp;if critico: ⏎ &nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;return base * 2 ⏎ &nbsp;&nbsp;&nbsp;&nbsp;return base',
      ],
      reward: 'critico',
    },
    {
      id: 'w4x', world: 4, type: 'predecir', bug: 'Chip secreto', bonus: true,
      title: 'Chip secreto',
      story: '¿Qué imprime este código?',
      code: 'def doble(n):\n    return n * 2\n\nprint(doble(4) + 1)',
      choices: ['8', '9', '10', '12'],
      solution: '9',
      explain: '<code>doble(4)</code> devuelve 8, y luego se le suma 1: 9.',
      hints: ['Resuelve primero lo que está dentro: <code>doble(4)</code>.', 'doble(4) = 8. Después, +1.', 'La respuesta es 9.'],
      reward: 'chip',
    },
  ];

  const byId = {};
  LIST.forEach((c) => { byId[c.id] = c; });

  // ------------------------------------------------------------------ utilidades
  function expectedOutput(ch) {
    if (ch.type === 'predecir') {
      const r = PyMini.run(ch.code);
      return normOut(r.output);
    }
    return ch.expectOut === undefined || ch.expectOut === null ? null : normOut(ch.expectOut);
  }

  function friendlyError(err) {
    if (!err) return '';
    const where = err.line ? `Línea ${err.line}: ` : '';
    return `${where}${err.msg}`;
  }

  function starterFor(ch, skill) {
    const s = ch.starter || {};
    return s[Math.max(0, Math.min(2, skill))] !== undefined ? s[Math.max(0, Math.min(2, skill))] : '';
  }

  // ------------------------------------------------------------------ validación
  // Devuelve { ok, output?, error?, message?, results? }
  function validate(ch, sub) {
    switch (ch.type) {
      case 'completar': {
        const value = String(sub.value === undefined ? '' : sub.value).trim();
        if (!value) return { ok: false, message: 'Elige o escribe algo para rellenar el hueco.' };
        if (ch.expectOut !== null && ch.expectOut !== undefined) {
          const code = ch.code.replace(BLANK, value);
          const r = PyMini.run(code);
          if (!r.ok) return { ok: false, error: r.error, output: r.output, message: friendlyError(r.error) };
          const ok = normOut(r.output) === normOut(ch.expectOut);
          return { ok, output: r.output, message: ok ? '' : `El programa imprimió «${normOut(r.output) || '(nada)'}» pero debía imprimir «${normOut(ch.expectOut)}».` };
        }
        const ok = (ch.accept || []).map(String).includes(value);
        return { ok, message: ok ? '' : 'Esa no es la respuesta. ¡Prueba otra o pide una pista!' };
      }
      case 'predecir': {
        const value = normOut(sub.value === undefined ? '' : sub.value).replace(/^["']|["']$/g, '');
        if (!value) return { ok: false, message: 'Escribe o elige tu respuesta.' };
        const ok = value === normOut(expectedOutput(ch));
        return { ok, message: ok ? '' : 'Casi. Sigue el código línea por línea, anotando cuánto vale cada variable.' };
      }
      case 'ordenar': {
        const used = sub.lines || [];
        if (!used.length) return { ok: false, message: 'Arrastra o toca las líneas para armar tu programa.' };
        const need = ch.lines.slice().sort();
        const got = used.slice().sort();
        if (need.join('\n') !== got.join('\n')) {
          const extra = used.length > ch.lines.length;
          return { ok: false, message: extra ? 'Sobra alguna línea: no todas las tarjetas se usan.' : 'Faltan líneas: usa todas las tarjetas necesarias.' };
        }
        const r = PyMini.run(used.join('\n'));
        if (!r.ok) return { ok: false, error: r.error, output: r.output, message: friendlyError(r.error) };
        const ok = normOut(r.output) === normOut(ch.expectOut);
        return { ok, output: r.output, message: ok ? '' : `Tu programa imprimió «${normOut(r.output) || '(nada)'}» pero debía imprimir «${normOut(ch.expectOut)}». ¡Cambia el orden!` };
      }
      case 'escribir': {
        const code = String(sub.code || '');
        if (!code.replace(/#.*$/gm, '').trim()) return { ok: false, message: 'Escribe tu código en el editor.' };
        if (code.includes(BLANK)) return { ok: false, message: 'Todavía quedan huecos «___» por rellenar.' };
        for (const q of ch.require || []) {
          if (!q.re.test(code)) return { ok: false, message: q.msg };
        }
        const results = [];
        let all = true, firstOut = '', firstErr = null;
        (ch.tests || []).forEach((t, i) => {
          const prelude = t.prelude || '';
          const offset = prelude ? (prelude.match(/\n/g) || []).length : 0;
          const r = PyMini.run(prelude + code, { lineOffset: offset });
          if (i === 0) firstOut = r.output;
          if (!r.ok) {
            all = false; if (!firstErr) firstErr = r.error;
            results.push({ ok: false, label: t.label, got: friendlyError(r.error), isError: true });
            return;
          }
          let ok, got, want, isError = false;
          if (t.call !== undefined) {
            const ev = r.evalExpr(t.call);
            got = ev.ok ? ev.value : friendlyError(ev.error);
            want = t.expect; ok = ev.ok && ev.value === t.expect; isError = !ev.ok;
            if (!ev.ok && !firstErr) firstErr = ev.error;
          } else {
            got = normOut(r.output); want = normOut(t.expectOut); ok = got === want;
          }
          if (!ok) all = false;
          results.push({ ok, label: t.label, got, want, isError });
        });
        return { ok: all, results, output: firstOut, error: firstErr, message: all ? '' : (firstErr ? friendlyError(firstErr) : 'Algún caso de prueba falló. Revisa la lista de abajo.') };
      }
      default: return { ok: false, message: 'Tipo de reto desconocido.' };
    }
  }

  // ¿Cuál fue la mejor solución en el formato que recibe validate()?
  function solutionSubmission(ch) {
    switch (ch.type) {
      case 'completar': return { value: ch.solution };
      case 'predecir': return { value: expectedOutput(ch) };
      case 'ordenar': return { lines: ch.solution };
      case 'escribir': return { code: ch.solution };
      default: return {};
    }
  }

  return { LIST, byId, validate, expectedOutput, starterFor, solutionSubmission, friendlyError, normOut, BLANK };
});
