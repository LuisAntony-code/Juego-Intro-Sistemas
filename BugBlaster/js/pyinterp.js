/* ============================================================================
 * PyMini — un mini-intérprete de Python escrito en JavaScript.
 *
 * Sirve para que el código que escribe el jugador se EJECUTE DE VERDAD dentro
 * del juego, sin instalar nada ni depender de internet. Cubre lo que necesita
 * un principiante: variables, números, textos, listas, diccionarios, if/elif/
 * else, while, for, range, def/return, print, f-strings y funciones básicas.
 *
 * Los mensajes de error están en español y explican qué pasó y cómo arreglarlo.
 * Funciona en el navegador (window.PyMini) y en Node (require).
 * ========================================================================== */
(function (root, factory) {
  if (typeof module === 'object' && module.exports) module.exports = factory();
  else root.PyMini = factory();
})(typeof self !== 'undefined' ? self : this, function () {
  'use strict';

  class PyError extends Error {
    constructor(type, msg, line) { super(msg); this.pyType = type; this.pyLine = line || null; }
  }
  // Los decimales van "envueltos" para distinguir 2 (int) de 2.0 (float).
  class Fl { constructor(v) { this.v = v; } }

  const KEYWORDS = new Set(['if', 'elif', 'else', 'while', 'for', 'in', 'def', 'return', 'pass',
    'break', 'continue', 'and', 'or', 'not', 'True', 'False', 'None', 'is', 'global']);
  const UNSUPPORTED = new Set(['import', 'from', 'class', 'try', 'except', 'finally', 'with', 'lambda',
    'yield', 'del', 'raise', 'assert', 'async', 'await', 'nonlocal']);

  // ------------------------------------------------------------------ valores
  const isFl = (v) => v instanceof Fl;
  const isNumber = (v) => typeof v === 'number' || isFl(v) || typeof v === 'boolean';
  const nv = (v) => (isFl(v) ? v.v : typeof v === 'boolean' ? (v ? 1 : 0) : v);
  const isRange = (v) => v !== null && typeof v === 'object' && v.kind === 'range';

  function typeName(v) {
    if (v === null) return 'NoneType';
    if (typeof v === 'boolean') return 'bool';
    if (typeof v === 'number') return 'int';
    if (isFl(v)) return 'float';
    if (typeof v === 'string') return 'str';
    if (Array.isArray(v)) return 'list';
    if (v instanceof Map) return 'dict';
    if (isRange(v)) return 'range';
    return 'function';
  }
  const TYPE_ES = {
    int: 'un número entero', float: 'un número decimal', str: 'un texto', list: 'una lista',
    dict: 'un diccionario', bool: 'un valor True/False', NoneType: 'None (nada)',
    function: 'una función', range: 'un rango',
  };
  const tes = (v) => TYPE_ES[typeName(v)];

  function fmtFloat(x) {
    if (Number.isNaN(x)) return 'nan';
    if (!Number.isFinite(x)) return x > 0 ? 'inf' : '-inf';
    if (Number.isInteger(x) && Math.abs(x) < 1e16) return x.toFixed(1);
    return String(x);
  }

  function repr(v) {
    if (v === null) return 'None';
    if (v === true) return 'True';
    if (v === false) return 'False';
    if (typeof v === 'number') return String(v);
    if (isFl(v)) return fmtFloat(v.v);
    if (typeof v === 'string') {
      const q = v.includes("'") && !v.includes('"') ? '"' : "'";
      return q + v.replace(/\\/g, '\\\\').replace(/\n/g, '\\n').replace(/\t/g, '\\t').split(q).join('\\' + q) + q;
    }
    if (Array.isArray(v)) return '[' + v.map(repr).join(', ') + ']';
    if (v instanceof Map) return '{' + [...v].map(([k, x]) => repr(k) + ': ' + repr(x)).join(', ') + '}';
    if (isRange(v)) return `range(${v.start}, ${v.stop}` + (v.step !== 1 ? `, ${v.step})` : ')');
    if (v && v.name) return `<función ${v.name}>`;
    return String(v);
  }
  const str = (v) => (typeof v === 'string' ? v : repr(v));

  function truthy(v) {
    if (v === null || v === false) return false;
    if (v === true) return true;
    if (typeof v === 'number') return v !== 0;
    if (isFl(v)) return v.v !== 0;
    if (typeof v === 'string') return v.length > 0;
    if (Array.isArray(v)) return v.length > 0;
    if (v instanceof Map) return v.size > 0;
    if (isRange(v)) return rangeLen(v) > 0;
    return true;
  }

  const typeErr = (msg, line) => new PyError('TypeError', msg, line);
  const num = (res, a, b) => (isFl(a) || isFl(b) ? new Fl(res) : res);

  function binop(op, a, b, line) {
    if (op === '+') {
      if (typeof a === 'string' && typeof b === 'string') return a + b;
      if (Array.isArray(a) && Array.isArray(b)) return a.concat(b);
      if (isNumber(a) && isNumber(b)) return num(nv(a) + nv(b), a, b);
      let extra = '';
      if ((typeof a === 'string' && isNumber(b)) || (typeof b === 'string' && isNumber(a))) {
        extra = ' Para unir un texto con un número, convierte el número con str(...).';
      }
      throw typeErr(`No se puede sumar ${tes(a)} con ${tes(b)}.${extra}`, line);
    }
    if (op === '*') {
      if (isNumber(a) && isNumber(b)) return num(nv(a) * nv(b), a, b);
      if (typeof a === 'string' && typeof b === 'number') return b > 0 ? a.repeat(b) : '';
      if (typeof b === 'string' && typeof a === 'number') return a > 0 ? b.repeat(a) : '';
      if (Array.isArray(a) && typeof b === 'number') { let r = []; for (let i = 0; i < b; i++) r = r.concat(a); return r; }
      throw typeErr(`No se puede multiplicar ${tes(a)} por ${tes(b)}.`, line);
    }
    if (!isNumber(a) || !isNumber(b)) {
      throw typeErr(`No se puede usar «${op}» entre ${tes(a)} y ${tes(b)}.`, line);
    }
    const x = nv(a), y = nv(b);
    switch (op) {
      case '-': return num(x - y, a, b);
      case '/':
        if (y === 0) throw new PyError('ZeroDivisionError', 'No se puede dividir entre cero.', line);
        return new Fl(x / y);
      case '//':
        if (y === 0) throw new PyError('ZeroDivisionError', 'No se puede dividir entre cero.', line);
        return num(Math.floor(x / y), a, b);
      case '%':
        if (y === 0) throw new PyError('ZeroDivisionError', 'No se puede sacar el resto de una división entre cero.', line);
        return num(((x % y) + y) % y, a, b);
      case '**':
        if (y < 0) return new Fl(Math.pow(x, y));
        return num(Math.pow(x, y), a, b);
      default:
        throw new PyError('SyntaxError', `Operador desconocido «${op}».`, line);
    }
  }

  function pyEq(a, b) {
    if (isNumber(a) && isNumber(b)) return nv(a) === nv(b);
    if (typeof a === 'string' && typeof b === 'string') return a === b;
    if (a === null || b === null) return a === b;
    if (Array.isArray(a) && Array.isArray(b)) return a.length === b.length && a.every((x, i) => pyEq(x, b[i]));
    if (a instanceof Map && b instanceof Map) {
      if (a.size !== b.size) return false;
      for (const [k, v] of a) if (!b.has(k) || !pyEq(v, b.get(k))) return false;
      return true;
    }
    return a === b;
  }

  function compare(op, a, b, line) {
    if (isNumber(a) && isNumber(b)) {
      const x = nv(a), y = nv(b);
      return op === '<' ? x < y : op === '>' ? x > y : op === '<=' ? x <= y : x >= y;
    }
    if (typeof a === 'string' && typeof b === 'string') {
      return op === '<' ? a < b : op === '>' ? a > b : op === '<=' ? a <= b : a >= b;
    }
    throw typeErr(`No se puede comparar ${tes(a)} con ${tes(b)} usando «${op}».`, line);
  }

  function contains(container, item, line) {
    if (typeof container === 'string') {
      if (typeof item !== 'string') throw typeErr('Con «in» sobre un texto, lo que buscas también debe ser un texto.', line);
      return container.includes(item);
    }
    if (Array.isArray(container)) return container.some((x) => pyEq(x, item));
    if (container instanceof Map) return container.has(dictKey(item, line));
    throw typeErr(`No se puede usar «in» con ${tes(container)}.`, line);
  }

  function dictKey(k, line) {
    if (isFl(k)) return k.v;
    if (typeof k === 'boolean') return k ? 1 : 0;
    if (Array.isArray(k) || k instanceof Map) throw typeErr('Una lista o diccionario no puede ser llave de un diccionario.', line);
    return k;
  }

  function rangeLen(r) {
    if (r.step > 0) return Math.max(0, Math.ceil((r.stop - r.start) / r.step));
    return Math.max(0, Math.ceil((r.start - r.stop) / -r.step));
  }

  function* iterate(v, line) {
    if (Array.isArray(v)) { yield* v.slice(); return; }
    if (typeof v === 'string') { yield* v; return; }
    if (v instanceof Map) { yield* [...v.keys()]; return; }
    if (isRange(v)) {
      if (v.step > 0) for (let i = v.start; i < v.stop; i += v.step) yield i;
      else for (let i = v.start; i > v.stop; i += v.step) yield i;
      return;
    }
    throw typeErr(`No se puede recorrer ${tes(v)} con un for. Usa una lista, un texto o range(...).`, line);
  }

  function lev(a, b) {
    const m = a.length, n = b.length;
    const d = Array.from({ length: m + 1 }, (_, i) => [i, ...Array(n).fill(0)]);
    for (let j = 0; j <= n; j++) d[0][j] = j;
    for (let i = 1; i <= m; i++) for (let j = 1; j <= n; j++) {
      d[i][j] = Math.min(d[i - 1][j] + 1, d[i][j - 1] + 1, d[i - 1][j - 1] + (a[i - 1] === b[j - 1] ? 0 : 1));
    }
    return d[m][n];
  }

  // ---------------------------------------------------------------- tokenizador
  function readStr(line, i, ln) {
    const q = line[i];
    if (line.substr(i, 3) === q + q + q) {
      throw new PyError('SyntaxError', 'Las comillas triples no están disponibles en este mini-Python. Usa comillas simples o dobles.', ln);
    }
    let j = i + 1, out = '';
    while (j < line.length && line[j] !== q) {
      if (line[j] === '\\' && j + 1 < line.length) {
        const n = line[j + 1];
        out += n === 'n' ? '\n' : n === 't' ? '\t' : n === '\\' ? '\\' : n === '"' ? '"' : n === "'" ? "'" : '\\' + n;
        j += 2;
      } else { out += line[j]; j++; }
    }
    if (j >= line.length) throw new PyError('SyntaxError', 'Falta cerrar las comillas de un texto.', ln);
    return { value: out, end: j + 1 };
  }

  function tokenize(src) {
    const lines = src.replace(/\r\n?/g, '\n').split('\n');
    const toks = [];
    const indents = [0];
    let depth = 0;
    for (let li = 0; li < lines.length; li++) {
      const line = lines[li];
      const ln = li + 1;
      let i = 0;
      if (depth === 0) {
        let col = 0;
        while (i < line.length && (line[i] === ' ' || line[i] === '\t')) { col += line[i] === '\t' ? 4 : 1; i++; }
        if (i >= line.length || line[i] === '#') continue;
        const top = indents[indents.length - 1];
        if (col > top) { indents.push(col); toks.push({ t: 'INDENT', line: ln }); }
        else if (col < top) {
          while (col < indents[indents.length - 1]) { indents.pop(); toks.push({ t: 'DEDENT', line: ln }); }
          if (col !== indents[indents.length - 1]) {
            throw new PyError('IndentationError', 'La sangría (los espacios al inicio de la línea) no coincide con la de ningún bloque anterior.', ln);
          }
        }
      }
      let any = false;
      while (i < line.length) {
        const c = line[i];
        if (c === ' ' || c === '\t') { i++; continue; }
        if (c === '#') break;
        any = true;
        if (/[0-9]/.test(c) || (c === '.' && /[0-9]/.test(line[i + 1] || ''))) {
          let j = i;
          while (j < line.length && /[0-9_]/.test(line[j])) j++;
          let isF = false;
          if (line[j] === '.' && /[0-9]/.test(line[j + 1] || '')) { isF = true; j++; while (j < line.length && /[0-9]/.test(line[j])) j++; }
          else if (c === '.') { isF = true; j = i + 1; while (j < line.length && /[0-9]/.test(line[j])) j++; }
          if (/[A-Za-z_]/.test(line[j] || '')) throw new PyError('SyntaxError', 'Un número no puede ir pegado a letras.', ln);
          const txt = line.slice(i, j).replace(/_/g, '');
          toks.push({ t: 'NUM', v: isF ? new Fl(parseFloat(txt)) : parseInt(txt, 10), line: ln });
          i = j; continue;
        }
        if (c === '"' || c === "'") {
          const s = readStr(line, i, ln);
          toks.push({ t: 'STR', v: s.value, line: ln }); i = s.end; continue;
        }
        if (/[A-Za-z_À-ɏ]/.test(c)) {
          if ((c === 'f' || c === 'F') && (line[i + 1] === '"' || line[i + 1] === "'")) {
            const s = readStr(line, i + 1, ln);
            toks.push({ t: 'FSTR', v: s.value, line: ln }); i = s.end; continue;
          }
          let j = i;
          while (j < line.length && /[A-Za-z0-9_À-ɏ]/.test(line[j])) j++;
          toks.push({ t: 'NAME', v: line.slice(i, j), line: ln }); i = j; continue;
        }
        const three = line.substr(i, 3), two = line.substr(i, 2);
        if (three === '**=' || three === '//=') { toks.push({ t: 'OP', v: three, line: ln }); i += 3; continue; }
        if (['==', '!=', '<=', '>=', '+=', '-=', '*=', '/=', '%=', '//', '**'].includes(two)) {
          toks.push({ t: 'OP', v: two, line: ln }); i += 2; continue;
        }
        if ('+-*/%<>=()[]{},:.'.includes(c)) {
          if ('([{'.includes(c)) depth++;
          if (')]}'.includes(c)) depth = Math.max(0, depth - 1);
          toks.push({ t: 'OP', v: c, line: ln }); i++; continue;
        }
        throw new PyError('SyntaxError', `No entiendo el símbolo «${c}».`, ln);
      }
      if (any && depth === 0) toks.push({ t: 'NEWLINE', line: ln });
    }
    if (depth > 0) throw new PyError('SyntaxError', 'Falta cerrar un paréntesis, corchete o llave.', lines.length);
    while (indents.length > 1) { indents.pop(); toks.push({ t: 'DEDENT', line: lines.length }); }
    toks.push({ t: 'EOF', line: lines.length });
    return toks;
  }

  // ------------------------------------------------------------------- parser
  const AUG = new Set(['+=', '-=', '*=', '/=', '%=', '//=', '**=']);

  class Parser {
    constructor(toks) { this.toks = toks; this.p = 0; }
    get cur() { return this.toks[this.p]; }
    get prev() { return this.toks[Math.max(0, this.p - 1)]; }
    next() { return this.toks[this.p++]; }
    isOp(v) { const t = this.cur; return t.t === 'OP' && t.v === v; }
    isKw(v) { const t = this.cur; return t.t === 'NAME' && t.v === v; }
    err(msg, line) { throw new PyError('SyntaxError', msg, line || this.cur.line || this.prev.line); }
    tokText(t) {
      if (t.t === 'NEWLINE') return 'el final de la línea';
      if (t.t === 'EOF') return 'el final del programa';
      if (t.t === 'INDENT' || t.t === 'DEDENT') return 'la sangría';
      if (t.t === 'STR' || t.t === 'FSTR') return 'un texto';
      if (t.t === 'NUM') return isFl(t.v) ? fmtFloat(t.v.v) : String(t.v);
      return t.v;
    }
    unexpected() {
      const t = this.cur;
      if (t.t === 'NEWLINE' || t.t === 'EOF') this.err('La línea termina antes de tiempo: parece que falta algo (un valor, un paréntesis…).', this.prev.line);
      this.err(`Algo no cuadra cerca de «${this.tokText(t)}». Revisa que esté bien escrito.`, t.line);
    }

    parseProgram() {
      const out = [];
      while (this.cur.t !== 'EOF') out.push(this.parseStatement());
      return out;
    }

    parseStatement() {
      const t = this.cur;
      if (t.t === 'NAME') {
        if (UNSUPPORTED.has(t.v)) this.err(`«${t.v}» todavía no está disponible en este mini-Python del juego.`, t.line);
        if (t.v === 'if') return this.parseIf();
        if (t.v === 'while') return this.parseWhile();
        if (t.v === 'for') return this.parseFor();
        if (t.v === 'def') return this.parseDef();
        if (t.v === 'else' || t.v === 'elif') this.err(`«${t.v}» solo puede ir justo después de un bloque «if» (con la misma sangría).`, t.line);
      }
      if (t.t === 'INDENT') this.err('Sangría inesperada: esta línea tiene espacios de más al inicio.', t.line);
      const s = this.parseSimple();
      if (this.cur.t === 'NEWLINE') this.next();
      else if (this.cur.t !== 'EOF') this.unexpected();
      return s;
    }

    parseBlock() {
      if (!this.isOp(':')) this.err('Falta el signo «:» al final de la línea.', this.prev.line);
      const colon = this.next();
      if (this.cur.t === 'NEWLINE') {
        this.next();
        if (this.cur.t !== 'INDENT') this.err('Después de «:» hace falta escribir el bloque en las líneas siguientes, con sangría (4 espacios).', colon.line);
        this.next();
        const body = [];
        while (this.cur.t !== 'DEDENT' && this.cur.t !== 'EOF') body.push(this.parseStatement());
        if (this.cur.t === 'DEDENT') this.next();
        return body;
      }
      const s = this.parseSimple();
      if (this.cur.t === 'NEWLINE') this.next();
      return [s];
    }

    parseCond() {
      const e = this.parseExpr();
      if (this.isOp('=')) this.err('Para comparar se usan dos signos «==». Un solo «=» sirve para guardar un valor en una variable.', this.cur.line);
      return e;
    }

    parseIf() {
      const line = this.next().line;
      const cond = this.parseCond();
      const body = this.parseBlock();
      let orelse = [];
      if (this.isKw('elif')) orelse = [this.parseIf()];
      else if (this.isKw('else')) { this.next(); orelse = this.parseBlock(); }
      return { type: 'If', cond, body, orelse, line };
    }

    parseWhile() {
      const line = this.next().line;
      const cond = this.parseCond();
      return { type: 'While', cond, body: this.parseBlock(), line };
    }

    parseFor() {
      const line = this.next().line;
      const v = this.cur;
      if (v.t !== 'NAME' || KEYWORDS.has(v.v)) this.err('Después de «for» va el nombre de la variable que cambia en cada vuelta. Ejemplo: for i in range(3):', line);
      this.next();
      if (!this.isKw('in')) this.err('Falta la palabra «in». Ejemplo: for i in range(3):', line);
      this.next();
      const iter = this.parseExpr();
      return { type: 'For', name: v.v, iter, body: this.parseBlock(), line };
    }

    parseDef() {
      const line = this.next().line;
      const nameTok = this.cur;
      if (nameTok.t !== 'NAME' || KEYWORDS.has(nameTok.v)) this.err('Después de «def» va el nombre de la función. Ejemplo: def saludar():', line);
      this.next();
      if (!this.isOp('(')) this.err('Después del nombre de la función van paréntesis: def nombre():', line);
      this.next();
      const params = [];
      while (!this.isOp(')')) {
        const p = this.cur;
        if (p.t !== 'NAME' || KEYWORDS.has(p.v)) this.err('Dentro de los paréntesis de def van los nombres de los parámetros, separados por comas.', line);
        this.next();
        let def = null;
        if (this.isOp('=')) { this.next(); def = this.parseExpr(); }
        params.push({ name: p.v, def });
        if (this.isOp(',')) this.next(); else if (!this.isOp(')')) this.err('Faltan comas o paréntesis en la definición de la función.', line);
      }
      this.next();
      return { type: 'Def', name: nameTok.v, params, body: this.parseBlock(), line };
    }

    parseSimple() {
      const t = this.cur;
      const line = t.line;
      if (t.t === 'NAME') {
        if (t.v === 'pass') { this.next(); return { type: 'Pass', line }; }
        if (t.v === 'break') { this.next(); return { type: 'Break', line }; }
        if (t.v === 'continue') { this.next(); return { type: 'Continue', line }; }
        if (t.v === 'return') {
          this.next();
          const value = (this.cur.t === 'NEWLINE' || this.cur.t === 'EOF') ? null : this.parseExpr();
          return { type: 'Return', value, line };
        }
        if (t.v === 'global') {
          this.next(); const names = [];
          while (this.cur.t === 'NAME') { names.push(this.next().v); if (this.isOp(',')) this.next(); }
          return { type: 'Global', names, line };
        }
      }
      const e = this.parseExpr();
      if (this.isOp('=')) {
        const parts = [e];
        while (this.isOp('=')) { this.next(); parts.push(this.parseExpr()); }
        const value = parts.pop();
        for (const tg of parts) {
          if (tg.type === 'Const') this.err('Al lado izquierdo del «=» debe ir el nombre de una variable (por ejemplo: vida = 100), no un valor.', line);
          if (tg.type !== 'Name' && tg.type !== 'Subscript') this.err('Al lado izquierdo del «=» debe ir el nombre de una variable.', line);
        }
        return { type: 'Assign', targets: parts, value, line };
      }
      if (this.cur.t === 'OP' && AUG.has(this.cur.v)) {
        const op = this.next().v.slice(0, -1);
        if (e.type !== 'Name' && e.type !== 'Subscript') this.err('Solo se puede usar «+=», «-=»… con el nombre de una variable.', line);
        return { type: 'AugAssign', op, target: e, value: this.parseExpr(), line };
      }
      return { type: 'ExprStmt', expr: e, line };
    }

    parseExpr() {
      const e = this.parseOr();
      if (this.isKw('if')) {
        const line = this.next().line;
        const cond = this.parseOr();
        if (!this.isKw('else')) this.err('Una expresión «a if condicion else b» necesita su «else».', line);
        this.next();
        return { type: 'Cond', cond, a: e, b: this.parseExpr(), line };
      }
      return e;
    }
    parseOr() {
      let l = this.parseAnd();
      while (this.isKw('or')) { const line = this.next().line; l = { type: 'BoolOp', op: 'or', a: l, b: this.parseAnd(), line }; }
      return l;
    }
    parseAnd() {
      let l = this.parseNot();
      while (this.isKw('and')) { const line = this.next().line; l = { type: 'BoolOp', op: 'and', a: l, b: this.parseNot(), line }; }
      return l;
    }
    parseNot() {
      if (this.isKw('not')) { const line = this.next().line; return { type: 'Not', e: this.parseNot(), line }; }
      return this.parseCompare();
    }
    parseCompare() {
      const left = this.parseArith();
      const ops = [];
      const line = this.cur.line;
      for (;;) {
        const t = this.cur;
        let op = null;
        if (t.t === 'OP' && ['==', '!=', '<', '>', '<=', '>='].includes(t.v)) { op = t.v; this.next(); }
        else if (this.isKw('in')) { op = 'in'; this.next(); }
        else if (this.isKw('not') && this.toks[this.p + 1] && this.toks[this.p + 1].t === 'NAME' && this.toks[this.p + 1].v === 'in') { op = 'not in'; this.next(); this.next(); }
        else if (this.isKw('is')) {
          this.next();
          if (this.isKw('not')) { this.next(); op = 'is not'; } else op = 'is';
        } else break;
        ops.push({ op, right: this.parseArith() });
      }
      return ops.length ? { type: 'Compare', left, ops, line } : left;
    }
    parseArith() {
      let l = this.parseTerm();
      while (this.cur.t === 'OP' && (this.cur.v === '+' || this.cur.v === '-')) {
        const o = this.next(); l = { type: 'Bin', op: o.v, a: l, b: this.parseTerm(), line: o.line };
      }
      return l;
    }
    parseTerm() {
      let l = this.parseFactor();
      while (this.cur.t === 'OP' && ['*', '/', '//', '%'].includes(this.cur.v)) {
        const o = this.next(); l = { type: 'Bin', op: o.v, a: l, b: this.parseFactor(), line: o.line };
      }
      return l;
    }
    parseFactor() {
      if (this.cur.t === 'OP' && (this.cur.v === '-' || this.cur.v === '+')) {
        const o = this.next(); return { type: 'Unary', op: o.v, e: this.parseFactor(), line: o.line };
      }
      return this.parsePower();
    }
    parsePower() {
      const base = this.parsePostfix();
      if (this.isOp('**')) { const o = this.next(); return { type: 'Bin', op: '**', a: base, b: this.parseFactor(), line: o.line }; }
      return base;
    }
    parsePostfix() {
      let e = this.parseAtom();
      for (;;) {
        if (this.isOp('(')) {
          const line = this.next().line;
          const args = [], kwargs = [];
          while (!this.isOp(')')) {
            if (this.cur.t === 'NAME' && this.toks[this.p + 1] && this.toks[this.p + 1].t === 'OP' && this.toks[this.p + 1].v === '=') {
              const k = this.next().v; this.next(); kwargs.push([k, this.parseExpr()]);
            } else args.push(this.parseExpr());
            if (this.isOp(',')) this.next(); else if (!this.isOp(')')) this.err('Faltan comas o falta cerrar el paréntesis «)».', line);
          }
          this.next();
          e = { type: 'Call', func: e, args, kwargs, line };
        } else if (this.isOp('[')) {
          const line = this.next().line;
          let a = null, b = null, slice = false;
          if (!this.isOp(':')) a = this.parseExpr();
          if (this.isOp(':')) { slice = true; this.next(); if (!this.isOp(']')) b = this.parseExpr(); }
          if (!this.isOp(']')) this.err('Falta cerrar el corchete «]».', line);
          this.next();
          e = slice ? { type: 'Slice', obj: e, a, b, line } : { type: 'Subscript', obj: e, index: a, line };
        } else if (this.isOp('.')) {
          const line = this.next().line;
          const n = this.cur;
          if (n.t !== 'NAME') this.err('Después del punto va el nombre de un método (por ejemplo: lista.append(3)).', line);
          this.next();
          e = { type: 'Attr', obj: e, name: n.v, line };
        } else break;
      }
      return e;
    }
    parseAtom() {
      const t = this.cur;
      if (t.t === 'NUM') { this.next(); return { type: 'Const', v: t.v, line: t.line }; }
      if (t.t === 'STR') {
        this.next(); let s = t.v;
        while (this.cur.t === 'STR') s += this.next().v;
        return { type: 'Const', v: s, line: t.line };
      }
      if (t.t === 'FSTR') { this.next(); return this.parseFString(t.v, t.line); }
      if (t.t === 'NAME') {
        if (t.v === 'True') { this.next(); return { type: 'Const', v: true, line: t.line }; }
        if (t.v === 'False') { this.next(); return { type: 'Const', v: false, line: t.line }; }
        if (t.v === 'None') { this.next(); return { type: 'Const', v: null, line: t.line }; }
        if (KEYWORDS.has(t.v)) this.err(`«${t.v}» es una palabra reservada de Python y aquí no puede ir.`, t.line);
        if (UNSUPPORTED.has(t.v)) this.err(`«${t.v}» todavía no está disponible en este mini-Python del juego.`, t.line);
        this.next(); return { type: 'Name', name: t.v, line: t.line };
      }
      if (t.t === 'OP') {
        if (t.v === '(') {
          this.next();
          if (this.isOp(')')) this.err('Los paréntesis están vacíos: falta un valor adentro.', t.line);
          const e = this.parseExpr();
          if (this.isOp(',')) this.err('Las tuplas (valores separados por coma entre paréntesis) todavía no están disponibles. Usa una lista con [ ].', t.line);
          if (!this.isOp(')')) this.err('Falta cerrar el paréntesis «)».', t.line);
          this.next(); return e;
        }
        if (t.v === '[') {
          this.next(); const items = [];
          while (!this.isOp(']')) {
            items.push(this.parseExpr());
            if (this.isOp(',')) this.next(); else if (!this.isOp(']')) this.err('Faltan comas o falta cerrar el corchete «]» de la lista.', t.line);
          }
          this.next(); return { type: 'List', items, line: t.line };
        }
        if (t.v === '{') {
          this.next(); const pairs = [];
          while (!this.isOp('}')) {
            const k = this.parseExpr();
            if (!this.isOp(':')) this.err('En un diccionario cada llave lleva «:» y su valor. Ejemplo: {"vida": 100}', t.line);
            this.next();
            pairs.push([k, this.parseExpr()]);
            if (this.isOp(',')) this.next(); else if (!this.isOp('}')) this.err('Faltan comas o falta cerrar la llave «}» del diccionario.', t.line);
          }
          this.next(); return { type: 'Dict', pairs, line: t.line };
        }
      }
      this.unexpected();
      return null;
    }
    parseFString(text, line) {
      const parts = [];
      let lit = '', i = 0;
      while (i < text.length) {
        const c = text[i];
        if (c === '{' && text[i + 1] === '{') { lit += '{'; i += 2; continue; }
        if (c === '}' && text[i + 1] === '}') { lit += '}'; i += 2; continue; }
        if (c === '{') {
          const end = text.indexOf('}', i);
          if (end < 0) throw new PyError('SyntaxError', 'En un f-string falta cerrar una llave «}».', line);
          let inner = text.slice(i + 1, end), spec = null;
          const m = inner.match(/^(.*):(\.\d+f)$/);
          if (m) { inner = m[1]; spec = m[2]; }
          if (!inner.trim()) throw new PyError('SyntaxError', 'En un f-string hay unas llaves {} vacías.', line);
          const sub = new Parser(tokenize(inner));
          const expr = sub.parseExpr();
          if (sub.cur.t !== 'NEWLINE' && sub.cur.t !== 'EOF') throw new PyError('SyntaxError', 'Algo no cuadra dentro de las llaves del f-string.', line);
          if (lit) { parts.push({ lit }); lit = ''; }
          parts.push({ expr, spec });
          i = end + 1; continue;
        }
        lit += c; i++;
      }
      if (lit) parts.push({ lit });
      return { type: 'FString', parts, line };
    }
  }

  // ------------------------------------------------------------- intérprete
  class Scope {
    constructor(parent) { this.vars = new Map(); this.parent = parent || null; this.globals = null; }
  }

  class Interp {
    constructor(opts) {
      this.out = [];
      this.outLen = 0;
      this.steps = 0;
      this.maxSteps = opts.maxSteps || 40000;
      this.depth = 0;
      this.globalScope = new Scope(null);
      this.builtins = this.makeBuiltins();
    }

    write(s) {
      this.outLen += s.length;
      if (this.outLen > 20000) throw new PyError('Timeout', 'Tu programa imprimió demasiado texto: puede haber un bucle que nunca termina.', null);
      this.out.push(s);
    }
    tick(line) {
      if (++this.steps > this.maxSteps) {
        throw new PyError('Timeout', 'Tu programa tardó demasiado y se detuvo: puede haber un bucle que nunca termina. Revisa que la condición del while llegue a ser falsa.', line);
      }
    }

    lookup(name, scope, line) {
      for (let s = scope; s; s = s.parent) if (s.vars.has(name)) return s.vars.get(name);
      if (Object.prototype.hasOwnProperty.call(this.builtins, name)) return this.builtins[name];
      const cands = [];
      for (let s = scope; s; s = s.parent) for (const k of s.vars.keys()) cands.push(k);
      for (const k of Object.keys(this.builtins)) cands.push(k);
      let best = null, bd = 99;
      for (const c of new Set(cands)) {
        const d = lev(name.toLowerCase(), c.toLowerCase());
        if (d < bd) { bd = d; best = c; }
      }
      const tip = best && bd <= Math.max(1, Math.floor(name.length / 3)) ? ` ¿Quisiste decir «${best}»?` : ' ¿La creaste antes con «=»? Las mayúsculas y minúsculas importan.';
      throw new PyError('NameError', `No existe nada llamado «${name}».${tip}`, line);
    }

    assign(t, v, scope) {
      if (t.type === 'Name') {
        const s = scope.globals && scope.globals.has(t.name) ? this.globalScope : scope;
        s.vars.set(t.name, v);
      } else if (t.type === 'Subscript') {
        const obj = this.eval(t.obj, scope);
        const idx = this.eval(t.index, scope);
        if (Array.isArray(obj)) {
          const i = this.listIndex(obj, idx, t.line);
          obj[i] = v;
        } else if (obj instanceof Map) obj.set(dictKey(idx, t.line), v);
        else throw typeErr(`No se puede asignar con [ ] sobre ${tes(obj)}.`, t.line);
      }
    }

    listIndex(arr, idx, line) {
      if (typeof idx !== 'number') throw typeErr('El número de posición (índice) debe ser un entero.', line);
      const i = idx < 0 ? arr.length + idx : idx;
      if (i < 0 || i >= arr.length) throw new PyError('IndexError', `La posición ${idx} no existe: la lista tiene ${arr.length} elemento(s) y empieza en la posición 0.`, line);
      return i;
    }

    execBlock(stmts, scope) {
      for (const s of stmts) { const r = this.exec(s, scope); if (r) return r; }
      return null;
    }

    exec(s, scope) {
      this.tick(s.line);
      switch (s.type) {
        case 'ExprStmt': this.eval(s.expr, scope); return null;
        case 'Pass': return null;
        case 'Assign': {
          const v = this.eval(s.value, scope);
          for (const t of s.targets) this.assign(t, v, scope);
          return null;
        }
        case 'AugAssign': {
          const cur = this.eval(s.target, scope);
          const v = this.eval(s.value, scope);
          this.assign(s.target, binop(s.op, cur, v, s.line), scope);
          return null;
        }
        case 'If': return this.execBlock(truthy(this.eval(s.cond, scope)) ? s.body : s.orelse, scope);
        case 'While':
          while (truthy(this.eval(s.cond, scope))) {
            this.tick(s.line);
            const r = this.execBlock(s.body, scope);
            if (r) { if (r.t === 'break') break; if (r.t === 'continue') continue; return r; }
          }
          return null;
        case 'For': {
          const it = iterate(this.eval(s.iter, scope), s.line);
          for (const x of it) {
            this.tick(s.line);
            this.assign({ type: 'Name', name: s.name }, x, scope);
            const r = this.execBlock(s.body, scope);
            if (r) { if (r.t === 'break') break; if (r.t === 'continue') continue; return r; }
          }
          return null;
        }
        case 'Def': {
          const params = s.params.map((p) => ({ name: p.name, hasDef: !!p.def, def: p.def ? this.eval(p.def, scope) : undefined }));
          scope.vars.set(s.name, { kind: 'func', name: s.name, params, body: s.body, closure: scope });
          return null;
        }
        case 'Return':
          if (this.depth === 0) throw new PyError('SyntaxError', '«return» solo se puede usar dentro de una función (dentro de un def).', s.line);
          return { t: 'return', v: s.value ? this.eval(s.value, scope) : null };
        case 'Break': return { t: 'break' };
        case 'Continue': return { t: 'continue' };
        case 'Global':
          if (!scope.globals) scope.globals = new Set();
          s.names.forEach((n) => scope.globals.add(n));
          return null;
        default: throw new PyError('SyntaxError', 'Instrucción no soportada.', s.line);
      }
    }

    eval(n, scope) {
      switch (n.type) {
        case 'Const': return n.v;
        case 'Name': return this.lookup(n.name, scope, n.line);
        case 'List': return n.items.map((e) => this.eval(e, scope));
        case 'Dict': {
          const m = new Map();
          for (const [k, v] of n.pairs) m.set(dictKey(this.eval(k, scope), n.line), this.eval(v, scope));
          return m;
        }
        case 'Bin': return binop(n.op, this.eval(n.a, scope), this.eval(n.b, scope), n.line);
        case 'Unary': {
          const v = this.eval(n.e, scope);
          if (!isNumber(v)) throw typeErr(`No se puede usar «${n.op}» delante de ${tes(v)}.`, n.line);
          if (n.op === '+') return typeof v === 'boolean' ? nv(v) : v;
          return isFl(v) ? new Fl(-v.v) : -nv(v);
        }
        case 'BoolOp': {
          const a = this.eval(n.a, scope);
          if (n.op === 'and') return truthy(a) ? this.eval(n.b, scope) : a;
          return truthy(a) ? a : this.eval(n.b, scope);
        }
        case 'Not': return !truthy(this.eval(n.e, scope));
        case 'Cond': return truthy(this.eval(n.cond, scope)) ? this.eval(n.a, scope) : this.eval(n.b, scope);
        case 'Compare': {
          let left = this.eval(n.left, scope);
          for (const { op, right: re } of n.ops) {
            const right = this.eval(re, scope);
            let ok;
            if (op === '==') ok = pyEq(left, right);
            else if (op === '!=') ok = !pyEq(left, right);
            else if (op === 'in') ok = contains(right, left, n.line);
            else if (op === 'not in') ok = !contains(right, left, n.line);
            else if (op === 'is') ok = left === right || (isNumber(left) && isNumber(right) && pyEq(left, right));
            else if (op === 'is not') ok = !(left === right || (isNumber(left) && isNumber(right) && pyEq(left, right)));
            else ok = compare(op, left, right, n.line);
            if (!ok) return false;
            left = right;
          }
          return true;
        }
        case 'FString': {
          let out = '';
          for (const p of n.parts) {
            if (p.lit !== undefined) out += p.lit;
            else {
              const v = this.eval(p.expr, scope);
              if (p.spec) {
                if (!isNumber(v)) throw typeErr('El formato «.2f» solo sirve con números.', n.line);
                out += nv(v).toFixed(parseInt(p.spec.slice(1), 10));
              } else out += str(v);
            }
          }
          return out;
        }
        case 'Subscript': {
          const obj = this.eval(n.obj, scope);
          const idx = this.eval(n.index, scope);
          if (Array.isArray(obj)) return obj[this.listIndex(obj, idx, n.line)];
          if (typeof obj === 'string') {
            if (typeof idx !== 'number') throw typeErr('El número de posición (índice) debe ser un entero.', n.line);
            const i = idx < 0 ? obj.length + idx : idx;
            if (i < 0 || i >= obj.length) throw new PyError('IndexError', `La posición ${idx} no existe: el texto tiene ${obj.length} letra(s).`, n.line);
            return obj[i];
          }
          if (obj instanceof Map) {
            const k = dictKey(idx, n.line);
            if (!obj.has(k)) throw new PyError('KeyError', `La llave ${repr(idx)} no existe en el diccionario.`, n.line);
            return obj.get(k);
          }
          throw typeErr(`No se puede usar [ ] sobre ${tes(obj)}.`, n.line);
        }
        case 'Slice': {
          const obj = this.eval(n.obj, scope);
          if (!Array.isArray(obj) && typeof obj !== 'string') throw typeErr(`No se puede cortar con [a:b] ${tes(obj)}.`, n.line);
          const a = n.a ? this.eval(n.a, scope) : 0;
          const b = n.b ? this.eval(n.b, scope) : obj.length;
          if (typeof a !== 'number' || typeof b !== 'number') throw typeErr('Los límites de un corte [a:b] deben ser enteros.', n.line);
          return obj.slice(a, b);
        }
        case 'Attr': throw typeErr(`«.${n.name}» debe usarse con paréntesis, como una acción: algo.${n.name}(...)`, n.line);
        case 'Call': {
          if (n.func.type === 'Attr') {
            const obj = this.eval(n.func.obj, scope);
            const args = n.args.map((a) => this.eval(a, scope));
            return this.callMethod(obj, n.func.name, args, n.line);
          }
          const f = this.eval(n.func, scope);
          const args = n.args.map((a) => this.eval(a, scope));
          const kw = {};
          for (const [k, e] of n.kwargs) kw[k] = this.eval(e, scope);
          return this.call(f, args, kw, n.line);
        }
        default: throw new PyError('SyntaxError', 'Expresión no soportada.', n.line);
      }
    }

    call(f, args, kw, line) {
      this.tick(line);
      if (f && f.kind === 'builtin') return f.fn(args, kw, line);
      if (f && f.kind === 'func') return this.callUser(f, args, kw, line);
      throw typeErr(`No se puede «llamar» a ${tes(f)} como si fuera una función. ¿Pusiste paréntesis de más?`, line);
    }

    callUser(f, args, kw, line) {
      if (this.depth > 120) throw new PyError('RecursionError', 'La función se llama a sí misma demasiadas veces sin parar (recursión sin fin).', line);
      const p = f.params;
      if (args.length > p.length) throw typeErr(`${f.name}() recibe ${p.length} dato(s) pero le pasaste ${args.length}.`, line);
      const local = new Scope(f.closure);
      for (let i = 0; i < p.length; i++) {
        const name = p[i].name;
        let v;
        if (i < args.length) v = args[i];
        else if (Object.prototype.hasOwnProperty.call(kw, name)) v = kw[name];
        else if (p[i].hasDef) v = p[i].def;
        else throw typeErr(`Falta un dato al llamar a ${f.name}(): no le diste valor a «${name}».`, line);
        local.vars.set(name, v);
      }
      for (const k of Object.keys(kw)) if (!p.some((q) => q.name === k)) throw typeErr(`${f.name}() no tiene ningún parámetro llamado «${k}».`, line);
      this.depth++;
      try {
        const r = this.execBlock(f.body, local);
        return r && r.t === 'return' ? r.v : null;
      } finally { this.depth--; }
    }

    callMethod(obj, name, args, line) {
      const bad = () => typeErr(`${tes(obj)} no tiene la acción «.${name}()».`, line);
      if (typeof obj === 'string') {
        switch (name) {
          case 'upper': return obj.toUpperCase();
          case 'lower': return obj.toLowerCase();
          case 'strip': return obj.trim();
          case 'replace': return obj.split(String(args[0])).join(String(args[1]));
          case 'split': return args.length ? obj.split(String(args[0])) : obj.trim().split(/\s+/).filter((x) => x !== '');
          case 'join': return [...iterate(args[0], line)].map((x) => {
            if (typeof x !== 'string') throw typeErr('join() solo une textos. Convierte los números con str(...).', line);
            return x;
          }).join(obj);
          case 'startswith': return obj.startsWith(String(args[0]));
          case 'endswith': return obj.endsWith(String(args[0]));
          case 'count': return obj.split(String(args[0])).length - 1;
          default: throw bad();
        }
      }
      if (Array.isArray(obj)) {
        switch (name) {
          case 'append': if (args.length !== 1) throw typeErr('append() recibe exactamente 1 dato.', line); obj.push(args[0]); return null;
          case 'pop': {
            if (!obj.length) throw new PyError('IndexError', 'No se puede sacar un elemento de una lista vacía.', line);
            return args.length ? obj.splice(this.listIndex(obj, args[0], line), 1)[0] : obj.pop();
          }
          case 'insert': obj.splice(Math.max(0, args[0]), 0, args[1]); return null;
          case 'reverse': obj.reverse(); return null;
          case 'sort': obj.sort((a, b) => (compare('<', a, b, line) ? -1 : compare('<', b, a, line) ? 1 : 0)); return null;
          case 'count': return obj.filter((x) => pyEq(x, args[0])).length;
          case 'index': {
            const i = obj.findIndex((x) => pyEq(x, args[0]));
            if (i < 0) throw new PyError('ValueError', `${repr(args[0])} no está en la lista.`, line);
            return i;
          }
          default: throw bad();
        }
      }
      if (obj instanceof Map) {
        switch (name) {
          case 'keys': return [...obj.keys()];
          case 'values': return [...obj.values()];
          case 'get': { const k = dictKey(args[0], line); return obj.has(k) ? obj.get(k) : (args.length > 1 ? args[1] : null); }
          default: throw bad();
        }
      }
      throw bad();
    }

    makeBuiltins() {
      const B = Object.create(null);
      const self = this;
      const def = (name, fn) => { B[name] = { kind: 'builtin', name, fn }; };
      const need = (args, n, name, line) => {
        if (args.length < n) throw typeErr(`${name}() necesita al menos ${n} dato(s).`, line);
      };
      def('print', (args, kw) => {
        const sep = kw.sep !== undefined ? str(kw.sep) : ' ';
        const end = kw.end !== undefined ? str(kw.end) : '\n';
        self.write(args.map(str).join(sep) + end);
        return null;
      });
      def('len', (a, kw, line) => {
        need(a, 1, 'len', line);
        const v = a[0];
        if (typeof v === 'string' || Array.isArray(v)) return v.length;
        if (v instanceof Map) return v.size;
        if (isRange(v)) return rangeLen(v);
        throw typeErr(`len() no funciona con ${tes(v)}.`, line);
      });
      def('range', (a, kw, line) => {
        if (!a.length || a.length > 3) throw typeErr('range() recibe de 1 a 3 números.', line);
        if (!a.every((x) => typeof x === 'number')) throw typeErr('range() solo acepta números enteros.', line);
        const r = a.length === 1 ? { start: 0, stop: a[0], step: 1 } : { start: a[0], stop: a[1], step: a[2] === undefined ? 1 : a[2] };
        if (r.step === 0) throw new PyError('ValueError', 'El paso de range() no puede ser 0.', line);
        return { kind: 'range', ...r };
      });
      def('int', (a, kw, line) => {
        if (!a.length) return 0;
        const v = a[0];
        if (typeof v === 'string') {
          if (!/^\s*[+-]?\d+\s*$/.test(v)) throw new PyError('ValueError', `No se puede convertir ${repr(v)} en un número entero.`, line);
          return parseInt(v, 10);
        }
        if (isNumber(v)) return Math.trunc(nv(v));
        throw typeErr(`int() no funciona con ${tes(v)}.`, line);
      });
      def('float', (a, kw, line) => {
        if (!a.length) return new Fl(0);
        const v = a[0];
        if (typeof v === 'string') {
          if (!/^\s*[+-]?(\d+\.?\d*|\.\d+)\s*$/.test(v)) throw new PyError('ValueError', `No se puede convertir ${repr(v)} en un número decimal.`, line);
          return new Fl(parseFloat(v));
        }
        if (isNumber(v)) return new Fl(nv(v));
        throw typeErr(`float() no funciona con ${tes(v)}.`, line);
      });
      def('str', (a) => (a.length ? str(a[0]) : ''));
      def('bool', (a) => (a.length ? truthy(a[0]) : false));
      def('abs', (a, kw, line) => {
        need(a, 1, 'abs', line);
        if (!isNumber(a[0])) throw typeErr(`abs() no funciona con ${tes(a[0])}.`, line);
        return isFl(a[0]) ? new Fl(Math.abs(a[0].v)) : Math.abs(nv(a[0]));
      });
      def('round', (a, kw, line) => {
        need(a, 1, 'round', line);
        const x = nv(a[0]);
        if (a.length > 1) return new Fl(Number(x.toFixed(a[1])));
        const r = Math.round(x);
        return Math.abs(x % 1) === 0.5 ? 2 * Math.round(x / 2) : r;
      });
      const pick = (name, better) => (a, kw, line) => {
        const items = a.length === 1 ? [...iterate(a[0], line)] : a;
        if (!items.length) throw new PyError('ValueError', `${name}() no puede trabajar con una lista vacía.`, line);
        return items.reduce((m, x) => (better(x, m, line) ? x : m));
      };
      def('max', pick('max', (x, m, line) => compare('>', x, m, line)));
      def('min', pick('min', (x, m, line) => compare('<', x, m, line)));
      def('sum', (a, kw, line) => {
        need(a, 1, 'sum', line);
        return [...iterate(a[0], line)].reduce((s, x) => binop('+', s, x, line), a.length > 1 ? a[1] : 0);
      });
      def('list', (a, kw, line) => (a.length ? [...iterate(a[0], line)] : []));
      def('sorted', (a, kw, line) => {
        need(a, 1, 'sorted', line);
        const arr = [...iterate(a[0], line)];
        arr.sort((x, y) => (compare('<', x, y, line) ? -1 : compare('<', y, x, line) ? 1 : 0));
        if (kw.reverse && truthy(kw.reverse)) arr.reverse();
        return arr;
      });
      def('input', (a, kw, line) => { throw new PyError('RuntimeError', 'input() no está disponible dentro del juego: los datos ya vienen dados en cada reto.', line); });
      return B;
    }
  }

  // --------------------------------------------------------------- API pública
  function run(code, opts) {
    opts = opts || {};
    const interp = new Interp(opts);
    const off = opts.lineOffset || 0;
    let error = null;
    const wrap = (e) => {
      if (e instanceof PyError) {
        return { type: e.pyType, msg: e.message, line: e.pyLine ? Math.max(1, e.pyLine - off) : null };
      }
      if (e instanceof RangeError) return { type: 'RecursionError', msg: 'La función se llama a sí misma demasiadas veces sin parar (recursión sin fin).', line: null };
      throw e;
    };
    try {
      const prog = new Parser(tokenize(String(code))).parseProgram();
      const r = interp.execBlock(prog, interp.globalScope);
      if (r && (r.t === 'break' || r.t === 'continue')) {
        throw new PyError('SyntaxError', `«${r.t}» solo se puede usar dentro de un bucle (for o while).`, null);
      }
    } catch (e) { error = wrap(e); }
    return {
      ok: !error,
      output: interp.out.join(''),
      error,
      globals: interp.globalScope.vars,
      // Evalúa una expresión en el estado final del programa y devuelve su repr().
      evalExpr(src) {
        try {
          const p = new Parser(tokenize(String(src)));
          const e = p.parseExpr();
          return { ok: true, value: repr(interp.eval(e, interp.globalScope)) };
        } catch (e) { return { ok: false, error: wrap(e) }; }
      },
    };
  }

  // Comprueba solo la sintaxis (sin ejecutar).
  function check(code) {
    try { new Parser(tokenize(String(code))).parseProgram(); return { ok: true }; }
    catch (e) {
      if (e instanceof PyError) return { ok: false, error: { type: e.pyType, msg: e.message, line: e.pyLine } };
      throw e;
    }
  }

  return { run, check, repr, PyError };
});
