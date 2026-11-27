/* ============================================================================
 * Interfaz de las terminales de hackeo (retos de código).
 * Se abre sobre el juego (que sigue "en cámara lenta") y devuelve el resultado
 * { solved, attempts, hints, usedSolution } a quien la abrió.
 * ========================================================================== */
(function (root) {
  'use strict';
  const C = root.Challenges, Snd = root.Sound;
  const SKILLS = ['Guiado', 'Normal', 'Experto'];
  const $ = (sel, el) => (el || document).querySelector(sel);

  const esc = (s) => String(s).replace(/&/g, '&amp;').replace(/</g, '&lt;').replace(/>/g, '&gt;').replace(/"/g, '&quot;');

  // Resaltado de sintaxis sencillo para mostrar código Python.
  function hl(code) {
    const re = /(#[^\n]*)|("(?:[^"\\\n]|\\.)*"|'(?:[^'\\\n]|\\.)*')|\b(\d+(?:\.\d+)?)\b|\b(def|if|elif|else|for|while|in|return|and|or|not|True|False|None|break|continue|pass)\b|\b(print|range|len|int|str|float|sum|min|max|abs)\b|(___)/g;
    let out = '', last = 0, m;
    while ((m = re.exec(code))) {
      out += esc(code.slice(last, m.index));
      const cls = m[1] ? 'c' : m[2] ? 's' : m[3] ? 'n' : m[4] ? 'k' : m[5] ? 'f' : 'b';
      out += `<span class="t-${cls}">${esc(m[0])}</span>`;
      last = re.lastIndex;
    }
    return out + esc(code.slice(last));
  }

  function shuffle(a) {
    const r = a.slice();
    for (let i = r.length - 1; i > 0; i--) { const j = Math.floor(Math.random() * (i + 1)); [r[i], r[j]] = [r[j], r[i]]; }
    return r;
  }

  let S = null; // estado del reto abierto

  function open(ch, opts) {
    const host = document.getElementById('challenge');
    S = {
      ch, skill: opts.skill, done: opts.done, attempts: 0, hints: 0, usedSolution: false, solved: false,
      get: () => ({}), host,
    };
    const accent = (opts.world && opts.world.theme.accent) || '#39d0ff';
    host.innerHTML = `
      <div class="cpanel" role="dialog" aria-label="Terminal de hackeo" style="--accent:${accent}">
        <div class="chead">
          <span class="cbug">🐞 ${esc(ch.bug)}</span>
          <span class="ctitle">${esc(ch.title)}${ch.bonus ? ' · <em>bonus</em>' : ''}</span>
          <span class="cskill" title="La dificultad se adapta a tu desempeño">Modo ${SKILLS[S.skill]}</span>
          <button class="cclose" title="Salir (Esc)" aria-label="Salir">✕</button>
        </div>
        <div class="cstory">${ch.story}</div>
        <div class="cbody"></div>
        <div class="chints"></div>
        <div class="cfeedback" aria-live="polite"></div>
        <div class="cfoot">
          <button class="btn ghost" id="hintBtn">💡 Pista <span class="count">(0/3)</span></button>
          <button class="btn ghost hidden" id="resetBtn">↺ Reiniciar código</button>
          <span class="spacer"></span>
          <span class="tip">Ctrl + Enter para comprobar</span>
          <button class="btn primary" id="runBtn">▶ Comprobar</button>
        </div>
      </div>`;
    host.classList.remove('hidden');
    renderBody();
    $('#hintBtn', host).onclick = giveHint;
    $('#runBtn', host).onclick = run;
    $('.cclose', host).onclick = () => close(false);
    document.addEventListener('keydown', onKey, true);
    Snd.resume();
    setTimeout(() => { const f = $('textarea, input, .choice, .card', host); if (f) f.focus(); }, 30);
  }

  function onKey(e) {
    if (!S) return;
    if (e.key === 'Escape') { e.preventDefault(); e.stopPropagation(); close(false); }
    else if (e.key === 'Enter' && (e.ctrlKey || e.metaKey)) { e.preventDefault(); if (S.solved) finish(); else run(); }
  }

  function close(solved) {
    if (!S) return;
    document.removeEventListener('keydown', onKey, true);
    const st = S; S = null;
    st.host.classList.add('hidden'); st.host.innerHTML = '';
    st.done({ solved: !!solved, attempts: st.attempts, hints: st.hints, usedSolution: st.usedSolution, id: st.ch.id });
  }
  function finish() { close(true); }

  // ---------------------------------------------------------- cuerpos
  function renderBody() {
    const { ch, skill } = S, body = $('.cbody', S.host);
    if (ch.type === 'completar') bodyCompletar(ch, skill, body);
    else if (ch.type === 'predecir') bodyPredecir(ch, skill, body);
    else if (ch.type === 'ordenar') bodyOrdenar(ch, skill, body);
    else bodyEscribir(ch, skill, body);
  }

  function choicesHTML(list) {
    return `<div class="choices">${shuffle(list).map((c) => `<button class="choice" data-v="${esc(c)}"><code>${esc(c)}</code></button>`).join('')}</div>`;
  }

  function bodyCompletar(ch, skill, body) {
    const parts = ch.code.split(C.BLANK);
    const typed = skill >= 2 || !ch.choices;
    const slot = typed ? '<input class="blank-input" id="ans" spellcheck="false" autocomplete="off" placeholder="…">' : '<span class="blank" id="blank">&nbsp;&nbsp;?&nbsp;&nbsp;</span>';
    body.innerHTML = `<div class="label">Completa el hueco:</div><pre class="code">${hl(parts[0])}${slot}${hl(parts[1] || '')}</pre>` + (typed ? '' : choicesHTML(ch.choices)) + '<div class="out hidden" id="out"></div>';
    let val = '';
    if (typed) {
      const inp = $('#ans', body);
      inp.oninput = () => { val = inp.value; };
      inp.onkeydown = (e) => { if (e.key === 'Enter') { e.preventDefault(); run(); } };
    } else {
      body.querySelectorAll('.choice').forEach((b) => {
        b.onclick = () => {
          body.querySelectorAll('.choice').forEach((x) => x.classList.remove('sel'));
          b.classList.add('sel'); val = b.dataset.v; $('#blank', body).textContent = ' ' + val + ' '; Snd.play('click');
        };
      });
    }
    S.get = () => ({ value: val });
  }

  function bodyPredecir(ch, skill, body) {
    const typed = skill >= 2 || !ch.choices;
    body.innerHTML = `<div class="label">Tu respuesta:</div><pre class="code">${hl(ch.code)}</pre>` +
      (typed ? '<div class="typed"><span>Imprimirá:</span> <input class="blank-input" id="ans" spellcheck="false" autocomplete="off" placeholder="escribe la salida"></div>' : choicesHTML(ch.choices));
    let val = '';
    if (typed) {
      const inp = $('#ans', body);
      inp.oninput = () => { val = inp.value; };
      inp.onkeydown = (e) => { if (e.key === 'Enter') { e.preventDefault(); run(); } };
    } else {
      body.querySelectorAll('.choice').forEach((b) => {
        b.onclick = () => { body.querySelectorAll('.choice').forEach((x) => x.classList.remove('sel')); b.classList.add('sel'); val = b.dataset.v; Snd.play('click'); };
      });
    }
    S.get = () => ({ value: val });
  }

  function bodyOrdenar(ch, skill, body) {
    const all = ch.lines.concat(skill >= 2 && ch.extra ? ch.extra : []);
    let bank = shuffle(all.map((l, i) => i));
    if (bank.length > 1 && bank.every((v, i) => all[v] === ch.lines[i])) bank = bank.reverse();
    let prog = [];
    body.innerHTML = `<div class="ordenar">
        <div class="zone"><div class="label">Tarjetas disponibles <small>(toca una para usarla)</small></div><div class="bank" id="bank"></div></div>
        <div class="zone"><div class="label">Tu programa <small>(arrastra o usa ▲ ▼ para ordenar)</small></div><div class="prog" id="prog"></div></div>
      </div><div class="out hidden" id="out"></div>`;
    const bankEl = $('#bank', body), progEl = $('#prog', body);
    let dragIdx = null;
    function draw() {
      bankEl.innerHTML = bank.map((i) => `<button class="card" data-i="${i}"><code>${hl(all[i]).replace(/^( +)/, (m) => '&nbsp;'.repeat(m.length))}</code></button>`).join('') || '<em class="muted">(vacío)</em>';
      progEl.innerHTML = prog.map((i, k) => `<div class="card inprog" draggable="true" data-k="${k}"><span class="ln">${k + 1}</span><code>${hl(all[i]).replace(/^( +)/, (m) => '&nbsp;'.repeat(m.length))}</code><span class="mv"><button data-up="${k}" title="Subir">▲</button><button data-dn="${k}" title="Bajar">▼</button><button data-rm="${k}" title="Quitar">✕</button></span></div>`).join('') || '<em class="muted">Aquí armarás tu programa…</em>';
      bankEl.querySelectorAll('.card').forEach((b) => { b.onclick = () => { const i = +b.dataset.i; bank = bank.filter((x) => x !== i); prog.push(i); Snd.play('click'); draw(); }; });
      progEl.querySelectorAll('[data-rm]').forEach((b) => { b.onclick = () => { const k = +b.dataset.rm; bank.push(prog.splice(k, 1)[0]); draw(); }; });
      progEl.querySelectorAll('[data-up]').forEach((b) => { b.onclick = () => { const k = +b.dataset.up; if (k > 0) { [prog[k - 1], prog[k]] = [prog[k], prog[k - 1]]; draw(); } }; });
      progEl.querySelectorAll('[data-dn]').forEach((b) => { b.onclick = () => { const k = +b.dataset.dn; if (k < prog.length - 1) { [prog[k + 1], prog[k]] = [prog[k], prog[k + 1]]; draw(); } }; });
      progEl.querySelectorAll('.inprog').forEach((d) => {
        d.ondragstart = (e) => { dragIdx = +d.dataset.k; e.dataTransfer.effectAllowed = 'move'; try { e.dataTransfer.setData('text/plain', String(dragIdx)); } catch (x) { /* ok */ } d.classList.add('drag'); };
        d.ondragend = () => d.classList.remove('drag');
        d.ondragover = (e) => { e.preventDefault(); d.classList.add('over'); };
        d.ondragleave = () => d.classList.remove('over');
        d.ondrop = (e) => {
          e.preventDefault();
          const to = +d.dataset.k;
          if (dragIdx !== null && dragIdx !== to) { const [m] = prog.splice(dragIdx, 1); prog.splice(to, 0, m); }
          dragIdx = null; draw();
        };
      });
    }
    draw();
    S.get = () => ({ lines: prog.map((i) => all[i]) });
  }

  function bodyEscribir(ch, skill, body) {
    const start = C.starterFor(ch, skill);
    const label = ch.kind === 'corregir' ? 'Corrige el código (el error está escondido):' : 'Escribe tu código:';
    body.innerHTML = `<div class="label">${label}</div>
      <textarea class="editor" id="ed" spellcheck="false" autocapitalize="off" autocomplete="off" rows="8"></textarea>
      <div class="out hidden" id="out"></div>`;
    const ed = $('#ed', body);
    ed.value = start;
    ed.rows = Math.max(7, Math.min(11, start.split('\n').length + 2));
    ed.addEventListener('keydown', (e) => {
      if (e.key === 'Tab') {
        e.preventDefault();
        const s = ed.selectionStart, t = ed.selectionEnd;
        ed.value = ed.value.slice(0, s) + '    ' + ed.value.slice(t);
        ed.selectionStart = ed.selectionEnd = s + 4;
      }
    });
    const reset = $('#resetBtn', S.host);
    reset.classList.remove('hidden');
    reset.onclick = () => { ed.value = start; ed.focus(); };
    S.get = () => ({ code: ed.value });
    S.editor = ed;
  }

  // ------------------------------------------------------- validación
  function run() {
    if (!S || S.solved) return;
    const { ch } = S, host = S.host;
    const sub = S.get();
    const r = C.validate(ch, sub);
    const fb = $('.cfeedback', host), out = $('#out', host);
    S.attempts++;
    if (out) {
      const txt = r.output !== undefined && r.output !== null ? String(r.output) : '';
      if (ch.type === 'escribir' || ch.type === 'ordenar' || ch.type === 'completar') {
        out.classList.remove('hidden');
        if (r.error) out.innerHTML = `<div class="olabel">Consola</div><pre class="err">${esc(C.friendlyError(r.error))}</pre>` + (txt ? `<pre>${esc(txt)}</pre>` : '');
        else out.innerHTML = `<div class="olabel">Salida de tu programa</div><pre>${esc(txt) || '<span class="muted">(no imprimió nada)</span>'}</pre>`;
      }
    }
    if (r.ok) {
      S.solved = true; Snd.play('good');
      fb.className = 'cfeedback ok';
      fb.innerHTML = `<div class="big">✔ ¡Correcto! <span class="stars">${S.usedSolution ? '' : (S.hints === 0 && S.attempts === 1 ? '★★★' : S.hints <= 1 ? '★★' : '★')}</span></div><div class="exp">${ch.explain}</div>`;
      renderResults(r, fb, true);
      const foot = $('.cfoot', host);
      foot.innerHTML = '<span class="spacer"></span><span class="tip">Ctrl + Enter</span><button class="btn primary big" id="okBtn">Instalar y continuar ▶</button>';
      const ok = $('#okBtn', host); ok.onclick = finish; ok.focus();
      host.querySelectorAll('textarea, input, .choice, .card, .mv button').forEach((x) => { x.disabled = true; });
    } else {
      Snd.play('bad');
      fb.className = 'cfeedback bad';
      fb.innerHTML = `<div class="big">✖ Todavía no. <span class="muted">¡Sin penalización, vuelve a intentarlo!</span></div><div class="msg">${r.message ? esc(r.message).replace(/«([^»]+)»/g, '<code>$1</code>') : ''}</div>`;
      renderResults(r, fb, false);
      if (S.attempts >= 3 && S.hints === 0) { giveHint(true); }
      else if (S.attempts >= 2) $('#hintBtn', host).classList.add('pulse');
      host.querySelector('.cpanel').classList.remove('shake'); void host.offsetWidth; host.querySelector('.cpanel').classList.add('shake');
    }
  }

  function renderResults(r, fb, ok) {
    if (!r.results || !r.results.length) return;
    const li = r.results.map((t) => `<li class="${t.ok ? 'pass' : 'fail'}"><b>${t.ok ? '✔' : '✖'}</b> <span>${esc(t.label || '')}</span>${t.ok ? '' : `<small>${t.isError ? esc(t.got) : `esperado <code>${esc(t.want)}</code> · obtuvo <code>${esc(t.got || '(nada)')}</code>`}</small>`}</li>`).join('');
    fb.innerHTML += `<div class="olabel">Casos de prueba</div><ul class="tests">${li}</ul>`;
  }

  // ---------------------------------------------------------- pistas
  function giveHint(auto) {
    if (!S || S.solved) return;
    if (S.hints >= 3) return;
    const { ch, host } = S;
    const n = S.hints;
    S.hints++;
    const box = $('.chints', host);
    let html = `<div class="hint"><b>Pista ${n + 1}:</b> ${ch.hints[n]}</div>`;
    if (n === 2) {
      S.usedSolution = true;
      let sol = '';
      if (ch.type === 'escribir') sol = ch.solution;
      else if (ch.type === 'ordenar') sol = ch.solution.join('\n');
      else if (ch.type === 'completar') sol = ch.code.replace(C.BLANK, ch.solution);
      if (sol) html += `<pre class="code sol">${hl(sol)}</pre><div class="muted">Esta es la solución. Escríbela tú mismo para practicar.</div>`;
    }
    box.insertAdjacentHTML('beforeend', html);
    const btn = $('#hintBtn', host);
    btn.classList.remove('pulse');
    btn.querySelector('.count').textContent = `(${S.hints}/3)`;
    if (S.hints >= 3) { btn.disabled = true; } else if (S.hints === 2) btn.innerHTML = '🔓 Ver solución <span class="count">(2/3)</span>';
    if (!auto) Snd.play('click');
  }

  root.ChallengeUI = { open, hl };
})(typeof self !== 'undefined' ? self : this);
