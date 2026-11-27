/* ============================================================================
 * BugBlaster — arranque, menús, guardado de progreso y opciones.
 * ========================================================================== */
(function (root) {
  'use strict';
  const { Game, VW, VH, computePowers, POWER_TEXT } = root.BBGame;
  const Lv = root.Levels, Ch = root.Challenges, Snd = root.Sound;
  const $ = (s, el) => (el || document).querySelector(s);

  // ------------------------------------------------------------- guardado
  const KEY = 'bugblaster.v1';
  const DEFAULTS = () => ({
    solved: {}, worlds: {}, adapt: 0,
    settings: { action: 'normal', code: 0, assist: false, shake: true, sound: true, volume: 0.6, unlockAll: false },
  });
  const Save = {
    data: DEFAULTS(),
    load() {
      try {
        const raw = localStorage.getItem(KEY);
        if (raw) {
          const d = JSON.parse(raw), base = DEFAULTS();
          this.data = Object.assign(base, d, { settings: Object.assign(base.settings, d.settings || {}) });
        }
      } catch (e) { /* sin almacenamiento: se juega igual */ }
    },
    store() { try { localStorage.setItem(KEY, JSON.stringify(this.data)); } catch (e) { /* ok */ } },
    reset() { const s = this.data.settings; this.data = DEFAULTS(); this.data.settings = s; this.store(); },
  };

  // ------------------------------------------------------------- arranque
  const canvas = document.getElementById('game');
  const screens = document.getElementById('screens');
  Save.load();

  const game = new Game(canvas, {
    openChallenge: (ch, o) => root.ChallengeUI.open(ch, o),
    saveProgress(id, r, adapt) {
      const cur = Save.data.solved[id] || { best: 0, attempts: 0 };
      const stars = r.usedSolution ? 1 : (r.hints === 0 && r.attempts === 1 ? 3 : r.hints <= 1 ? 2 : 1);
      Save.data.solved[id] = { best: Math.max(cur.best, stars), attempts: cur.attempts + r.attempts };
      Save.data.adapt = adapt;
      Save.store();
    },
    onComplete(rep) {
      const w = Save.data.worlds[rep.world] || { stars: 0, chips: 0, time: 0 };
      Save.data.worlds[rep.world] = { stars: Math.max(w.stars, rep.stars), chips: Math.max(w.chips, rep.chips), time: w.time ? Math.min(w.time, rep.time) : rep.time };
      Save.store();
      setTimeout(() => showScreen('complete', rep), 900);
    },
  });
  applyData();

  function applyData() {
    game.settings = Save.data.settings;
    game.solved = new Set(Object.keys(Save.data.solved));
    game.adapt = Save.data.adapt || 0;
    game.powers = computePowers(game.solved);
    Snd.setMuted(!Save.data.settings.sound);
    Snd.setVolume(Save.data.settings.volume);
  }

  function fit() {
    const s = Math.min(window.innerWidth / VW, window.innerHeight / VH);
    canvas.style.width = Math.floor(VW * s) + 'px';
    canvas.style.height = Math.floor(VH * s) + 'px';
  }
  window.addEventListener('resize', fit); fit();

  function loop(now) { game.frame(now); requestAnimationFrame(loop); }
  requestAnimationFrame(loop);

  // -------------------------------------------------------------- pantallas
  const stars = (n) => '★'.repeat(n) + '☆'.repeat(3 - n);
  const worldUnlocked = (i) => i === 0 || Save.data.settings.unlockAll || (Save.data.worlds[Lv.WORLDS[i - 1].id] || { stars: 0 }).stars > 0;
  const fmtTime = (s) => `${Math.floor(s / 60)}:${String(Math.floor(s % 60)).padStart(2, '0')}`;
  const esc = (s) => String(s).replace(/&/g, '&amp;').replace(/</g, '&lt;').replace(/>/g, '&gt;');

  function hide() { screens.innerHTML = ''; screens.className = ''; }
  function rain() {
    let h = '<div class="rain" aria-hidden="true">';
    for (let i = 0; i < 38; i++) h += `<span style="left:${(i * 2.7) % 100}%;animation-delay:${-(i * 1.3) % 9}s;animation-duration:${7 + (i % 5) * 2}s">${i % 2}${(i * 7) % 2}${(i * 3) % 2}</span>`;
    return h + '</div>';
  }

  function robotCode() {
    const pw = computePowers(game.solved);
    const L = [`<span class="k">robot</span> = {`, `  <span class="s">"municion"</span>: <span class="n">${pw.ammoMax}</span>,`, `  <span class="s">"vida"</span>: <span class="n">${pw.hpMax}</span>,`];
    if (pw.energyMax) L.push(`  <span class="s">"energia"</span>: <span class="n">${pw.energyMax}</span>,`);
    const ab = [];
    if (pw.shotgun) ab.push('escopeta'); if (pw.rifle) ab.push('rifle'); if (pw.shield) ab.push('escudo');
    if (pw.burst) ab.push('ráfaga×' + pw.burst); if (pw.dash) ab.push('dash'); if (pw.doubleJump) ab.push('doble salto'); if (pw.crit) ab.push('crítico');
    L.push(`  <span class="s">"poderes"</span>: [${ab.length ? ab.map((a) => `<span class="s">"${a}"</span>`).join(', ') : ''}]`, '}');
    return L.join('\n');
  }

  function showScreen(name, data) {
    screens.className = 'on';
    const t = {
      title() {
        return `${rain()}<div class="menu title">
          <div class="logo"><span class="bug">🐞</span><h1>Bug<span>Blaster</span></h1><p class="sub">Operación Compilar</p></div>
          <p class="tag">Corre, salta y dispara… y <b>programa en Python</b> para volverte más fuerte.</p>
          <div class="btns">
            <button class="btn primary big" data-go="worlds">▶ Jugar</button>
            <button class="btn" data-go="help">❓ Cómo se juega</button>
            <button class="btn" data-go="options">⚙ Opciones</button>
          </div>
          <p class="foot">Todo el código que escribes se ejecuta de verdad dentro del juego · sin instalar nada</p></div>`;
      },
      worlds() {
        const cards = Lv.WORLDS.map((w, i) => {
          const rec = Save.data.worlds[w.id], ok = worldUnlocked(i), st = rec ? rec.stars : 0;
          return `<div class="wcard ${ok ? '' : 'locked'}" style="--accent:${w.theme.accent}">
            <div class="wicon">${w.icon}</div>
            <div class="wname">Mundo ${w.id}<b>${w.name}</b></div>
            <div class="wconcept">${w.concept}</div>
            <div class="wstars">${stars(st)}</div>
            <div class="wmeta">${rec ? `🪙 ${rec.chips} · ⏱ ${fmtTime(rec.time)}` : (ok ? 'Sin jugar' : '🔒 Completa el mundo anterior')}</div>
            <button class="btn ${ok ? 'primary' : ''}" data-world="${i}" ${ok ? '' : 'disabled'}>${rec ? 'Jugar otra vez' : 'Jugar'}</button>
          </div>`;
        }).join('');
        return `${rain()}<div class="menu wide"><h2>Elige un mundo</h2><div class="wgrid">${cards}</div>
          <div class="robotbox"><div class="label">🤖 Tu robot (se programa con los retos que resuelves)</div><pre class="code py">${robotCode()}</pre></div>
          <div class="btns row"><button class="btn" data-go="title">← Menú</button><button class="btn" data-go="options">⚙ Opciones</button></div></div>`;
      },
      intro() {
        const w = Lv.WORLDS[data];
        return `${rain()}<div class="menu" style="--accent:${w.theme.accent}"><div class="wicon big">${w.icon}</div>
          <h2>Mundo ${w.id}: ${w.name}</h2><p class="concept">Aprenderás: <b>${w.concept}</b></p>
          ${w.intro.map((p) => `<p class="story">${p}</p>`).join('')}
          <pre class="code py">${root.ChallengeUI.hl(w.sample)}</pre>
          <p class="hint2">Hackea ${w.terminals.length} terminales, derrota al jefe y cruza el portal. Hay <b>chips</b> y una terminal secreta por si quieres más.</p>
          <div class="btns row"><button class="btn" data-go="worlds">← Mundos</button><button class="btn primary big" data-start="${data}">▶ ¡Empezar!</button></div></div>`;
      },
      help() {
        return `${rain()}<div class="menu wide"><h2>Cómo se juega</h2>
          <div class="helpgrid">
            <div><h3>🎮 Controles</h3><table>
              <tr><td><kbd>A</kbd> <kbd>D</kbd> / <kbd>←</kbd> <kbd>→</kbd></td><td>Moverse</td></tr>
              <tr><td><kbd>W</kbd> / <kbd>↑</kbd> / <kbd>Espacio</kbd></td><td>Saltar (mantén para saltar más alto)</td></tr>
              <tr><td><kbd>Ratón</kbd> + clic · o <kbd>J</kbd></td><td>Apuntar y disparar</td></tr>
              <tr><td><kbd>E</kbd> / <kbd>Enter</kbd></td><td>Hackear una terminal</td></tr>
              <tr><td><kbd>F</kbd> / clic derecho</td><td>Ráfaga (Mundo 3)</td></tr>
              <tr><td><kbd>Shift</kbd></td><td>Dash (Mundo 4)</td></tr>
              <tr><td><kbd>S</kbd> / <kbd>↓</kbd></td><td>Bajar de una plataforma</td></tr>
              <tr><td><kbd>Esc</kbd></td><td>Pausa</td></tr></table></div>
            <div><h3>💻 La idea</h3>
              <p>Los <b>poderes de tu robot son código</b>. En cada terminal resuelves un reto de Python y, al lograrlo, ese código <b>se instala</b>: más munición, escopeta automática, ráfagas, dash…</p>
              <p>Cada jefe tiene un <b>escudo</b>: solo cae si hackeas su terminal. El reto se adapta a ti: con pistas y sin castigo por equivocarte.</p>
              <h3>⭐ Estrellas</h3><p>★ completar · ★★ sin ver soluciones · ★★★ todos los chips y la terminal secreta.</p></div>
          </div><div class="btns row"><button class="btn primary" data-go="title">← Volver</button></div></div>`;
      },
      options() {
        const s = Save.data.settings;
        const seg = (key, opts) => `<div class="seg" data-key="${key}">${opts.map(([v, l]) => `<button class="${String(s[key]) === String(v) ? 'sel' : ''}" data-v="${v}">${l}</button>`).join('')}</div>`;
        const tog = (key) => `<button class="toggle ${s[key] ? 'on' : ''}" data-tog="${key}"><i></i></button>`;
        return `${rain()}<div class="menu wide"><h2>Opciones</h2><div class="opts">
          <div class="opt"><div><b>Dificultad de acción</b><small>Reflejos: daño y vida de los enemigos</small></div>${seg('action', [['facil', 'Fácil'], ['normal', 'Normal'], ['dificil', 'Difícil']])}</div>
          <div class="opt"><div><b>Dificultad de código</b><small>Guiado: huecos para completar · Experto: escribes todo. Se adapta mientras juegas.</small></div>${seg('code', [[0, 'Guiado'], [1, 'Normal'], [2, 'Experto']])}</div>
          <div class="opt"><div><b>Modo asistido</b><small>Menos daño, apuntado ayudado y el tiempo se detiene al hackear</small></div>${tog('assist')}</div>
          <div class="opt"><div><b>Sacudida de pantalla</b><small>Efecto de impactos</small></div>${tog('shake')}</div>
          <div class="opt"><div><b>Sonido</b><small>Efectos de audio</small></div>${tog('sound')}</div>
          <div class="opt"><div><b>Volumen</b></div><input type="range" min="0" max="1" step="0.05" value="${s.volume}" id="vol"></div>
          <div class="opt"><div><b>Mundos libres (modo docente)</b><small>Desbloquea todos los mundos</small></div>${tog('unlockAll')}</div>
          <div class="opt"><div><b>Borrar progreso</b><small>Reinicia estrellas y poderes</small></div><button class="btn danger" id="wipe">🗑 Borrar</button></div>
        </div><div class="btns row"><button class="btn primary" data-go="${data || 'title'}">← Volver</button></div></div>`;
      },
      pause() {
        return `<div class="menu small"><h2>⏸ Pausa</h2><div class="btns">
          <button class="btn primary big" data-resume>▶ Continuar</button>
          <button class="btn" data-restart>↺ Reiniciar mundo</button>
          <button class="btn" data-go="worlds" data-quit>🗺 Salir a los mundos</button></div></div>`;
      },
      complete() {
        const r = data, last = r.world === Lv.WORLDS.length;
        return `${rain()}<div class="menu wide win" style="--accent:${Lv.WORLDS[r.world - 1].theme.accent}">
          <h2>¡Mundo ${r.world} completado!</h2><div class="bigstars">${stars(r.stars)}</div>
          <div class="stats"><div><b>${r.chips}/${r.chipTotal}</b><small>chips</small></div><div><b>${fmtTime(r.time)}</b><small>tiempo</small></div><div><b>${r.deaths}</b><small>derrotas</small></div><div><b>${r.kills}</b><small>bugs vencidos</small></div><div><b>${r.hints}</b><small>pistas</small></div></div>
          <div class="cols"><div><div class="label">Código que instalaste</div><ul class="inst">${r.installed.map((i) => `<li>✔ <b>${esc(i.title)}</b><code>${esc((POWER_TEXT[i.id] || [''])[0])}</code></li>`).join('')}</ul></div>
          <div><div class="label">Lo aprendido</div><pre class="code py">${root.ChallengeUI.hl(r.sample)}</pre>
          <p class="hint2">${r.noSolutions ? '' : '⭐ Para la 2ª estrella, intenta resolver los retos sin ver la solución.<br>'}${r.allChips ? '' : '⭐ Para la 3ª, recoge todos los chips y la terminal secreta.'}</p></div></div>
          <div class="btns row"><button class="btn" data-go="worlds">🗺 Mundos</button><button class="btn" data-start="${r.world - 1}">↺ Repetir</button>
          ${last ? '<button class="btn primary big" data-go="title">🏆 ¡Terminaste BugBlaster!</button>' : `<button class="btn primary big" data-intro="${r.world}">Siguiente mundo ▶</button>`}</div></div>`;
      },
    };
    screens.innerHTML = t[name](data);
    bind(name, data);
  }

  function bind(name, data) {
    screens.querySelectorAll('[data-go]').forEach((b) => { b.onclick = () => { Snd.resume(); Snd.play('click'); if (b.hasAttribute('data-quit')) { game.state = 'idle'; game.paused = false; } showScreen(b.dataset.go, b.dataset.go === 'options' ? name : undefined); }; });
    screens.querySelectorAll('[data-world]').forEach((b) => { b.onclick = () => { Snd.resume(); Snd.play('click'); showScreen('intro', +b.dataset.world); }; });
    screens.querySelectorAll('[data-intro]').forEach((b) => { b.onclick = () => showScreen('intro', +b.dataset.intro); });
    screens.querySelectorAll('[data-start]').forEach((b) => { b.onclick = () => startWorld(+b.dataset.start); });
    const res = $('[data-resume]', screens); if (res) res.onclick = resume;
    const rs = $('[data-restart]', screens); if (rs) rs.onclick = () => startWorld(game.worldIdx);
    if (name === 'options') {
      screens.querySelectorAll('.seg').forEach((seg) => seg.querySelectorAll('button').forEach((b) => {
        b.onclick = () => { const k = seg.dataset.key, v = b.dataset.v; Save.data.settings[k] = k === 'code' ? +v : v; Save.store(); applyData(); Snd.play('click'); showScreen('options', data); };
      }));
      screens.querySelectorAll('[data-tog]').forEach((b) => {
        b.onclick = () => { const k = b.dataset.tog; Save.data.settings[k] = !Save.data.settings[k]; Save.store(); applyData(); Snd.play('click'); showScreen('options', data); };
      });
      const vol = $('#vol', screens); vol.oninput = () => { Save.data.settings.volume = +vol.value; Save.store(); applyData(); Snd.play('click'); };
      $('#wipe', screens).onclick = () => { if (confirm('¿Seguro que quieres borrar todo tu progreso?')) { Save.reset(); applyData(); showScreen('options', data); } };
    }
  }

  function startWorld(i) {
    hide(); Snd.resume();
    applyData();
    game.start(i);
  }
  function resume() { hide(); game.paused = false; game.last = 0; }

  window.addEventListener('keydown', (e) => {
    if ((e.code === 'Escape' || e.code === 'KeyP') && game.state === 'play' && !document.activeElement.matches('input,textarea')) {
      if (game.paused) resume(); else { game.paused = true; showScreen('pause'); }
    }
  });
  // Pausa automática al cambiar de pestaña
  document.addEventListener('visibilitychange', () => { if (document.hidden && game.state === 'play' && !game.paused) { game.paused = true; showScreen('pause'); } });

  showScreen('title');
  root.BB = { game, Save, showScreen, startWorld };
})(typeof self !== 'undefined' ? self : this);
