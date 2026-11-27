// Genera las capturas de pantalla del juego en funcionamiento (para el informe).
// Uso:  node BugBlaster/tools/capturar.js <carpeta_salida>
const { chromium } = require('playwright');
const path = require('path');
const fs = require('fs');

const OUT = path.resolve(process.argv[2] || path.join(__dirname, '../docs/capturas'));
fs.mkdirSync(OUT, { recursive: true });
let n = 0;

(async () => {
  const browser = await chromium.launch();
  const ctx = await browser.newContext({ viewport: { width: 1280, height: 720 }, deviceScaleFactor: 2 });
  const page = await ctx.newPage();
  const errors = [];
  page.on('pageerror', (e) => errors.push(e.message));
  page.on('console', (m) => { if (m.type() === 'error') errors.push(m.text()); });
  await page.goto('file://' + path.resolve(__dirname, '../index.html'));
  await page.waitForTimeout(500);

  const shot = async (name, wait = 150) => { await page.waitForTimeout(wait); n++; await page.screenshot({ path: path.join(OUT, `${String(n).padStart(2, '0')}_${name}.png`) }); console.log('  ·', name); };
  const ev = (fn, arg) => page.evaluate(fn, arg);
  // Avanza la simulación n fotogramas y redibuja (con el juego en pausa para que sea determinista)
  const step = (frames) => ev((f) => { const G = BB.game; for (let i = 0; i < f; i++) G.update(1 / 60); G.render(); }, frames);
  const prep = (w, solved) => ev(([w, solved]) => {
    const G = BB.game; document.getElementById('screens').innerHTML = ''; document.getElementById('screens').className = '';
    G.solved = new Set(solved); G.settings.assist = false; G.settings.code = 0; G.adapt = 0;
    G.start(w); G.paused = true; G.banner = null; G.toasts = []; G.render();
  }, [w, solved]);
  const put = (tile, row = 13, dx = 0) => ev(([t, r, dx]) => { const G = BB.game, p = G.p; p.x = t * 32 + dx; p.y = r * 32 - 28 + 32; p.vx = p.vy = 0; p.inv = 0; G.cam.x = Math.max(0, Math.min(G.levelW - 960, p.x - 480)); }, [tile, row, dx]);

  // ---------------- Menús ----------------
  await shot('menu_titulo');
  await page.click('[data-go="help"]'); await shot('menu_ayuda');
  await page.click('[data-go="title"]'); await page.click('[data-go="options"]'); await shot('menu_opciones');
  await page.click('[data-go="title"]'); await page.click('[data-go="worlds"]'); await shot('menu_mundos');
  await page.click('[data-world="0"]'); await shot('intro_mundo1');
  await page.click('[data-start="0"]'); await page.waitForTimeout(1300); await shot('inicio_banner', 0);

  // ---------------- Mundo 1: movimiento y combate ----------------
  await prep(0, []);
  await put(5); await step(3); await shot('mundo1_cartel_controles');
  await put(13); await ev(() => { const G = BB.game; G.aimMode = 'key'; G.p.face = 1; G.keys.add('KeyJ'); });
  await step(22); await shot('mundo1_combate_disparos');
  await ev(() => BB.game.keys.clear());
  await put(23, 13, -4); await step(2); await shot('mundo1_terminal_aviso');

  // ---------------- Retos (interfaz) ----------------
  const open = (id, skill) => ev(([id, skill]) => {
    const G = BB.game; G.settings.code = skill; G.adapt = 0; G.state = 'play'; G.paused = true;
    const t = G.terminals.find((t) => t.id === id);
    if (t) { G.nearTerminal = t; G.openChallenge(t); }
    else ChallengeUI.open(Challenges.byId[id], { skill, world: Levels.WORLDS[+id[1] - 1], done: () => {} });   // reto de otro mundo
  }, [id, skill]);
  await open('w1a', 0); await shot('reto_completar_guiado');
  await page.locator('.choice', { hasText: 'balas' }).click(); await page.click('#runBtn'); await shot('reto_completar_error');
  await page.click('.choice[data-v="10"]'); await page.click('#runBtn'); await shot('reto_completar_correcto');
  await page.click('#okBtn'); await step(2); await shot('mundo1_poder_instalado_toast');

  await open('w1b', 0); await shot('reto_ordenar_inicio');
  const clickCard = async (text) => { const idx = await ev((t) => [...document.querySelectorAll('#bank .card')].findIndex((c) => c.textContent.replace(/ /g, ' ') === t), text); await page.locator('#bank .card').nth(idx).click(); };
  await clickCard('dano = 30'); await clickCard('vida = vida - dano'); await clickCard('vida = 100'); await clickCard('print(vida)');
  await page.click('#runBtn'); await shot('reto_ordenar_orden_incorrecto');
  await page.locator('[data-up="2"]').click(); await page.locator('[data-up="1"]').click(); await page.locator('[data-up="2"]').click().catch(() => {});
  // dejamos el orden correcto exacto
  await ev(() => document.querySelector('.cclose').click());
  await open('w1b', 0);
  for (const l of ['vida = 100', 'dano = 30', 'vida = vida - dano', 'print(vida)']) await clickCard(l);
  await shot('reto_ordenar_listo');
  await page.click('#runBtn'); await shot('reto_ordenar_correcto'); await page.click('#okBtn');

  await open('w1c', 0); await shot('reto_escribir_guiado');
  await ev(() => document.querySelector('#ed').value = 'energia = 50\nenergia = energia + 25\nprint(energi)');
  await page.click('#runBtn'); await shot('reto_escribir_error_nombre');
  await page.click('#hintBtn'); await page.click('#hintBtn'); await shot('reto_pistas');
  await ev(() => document.querySelector('#ed').value = 'energia = 50\nenergia = energia + 25\nprint(energia)');
  await page.click('#runBtn'); await shot('reto_escribir_correcto'); await page.click('#okBtn');
  await ev(() => { const G = BB.game; G.render(); }); await shot('mundo1_hud_energia');

  await open('w1x', 0); await page.click('.choice[data-v="16"]'); await shot('reto_predecir_bonus');
  await page.click('#runBtn'); await page.click('#okBtn');

  // modo experto: escribir desde cero
  await open('w3c', 2); await shot('reto_escribir_experto');
  await ev(() => document.querySelector('#ed').value = 'print(1)\nprint(2)\nprint(3)\nprint(4)\nprint(5)');
  await page.click('#runBtn'); await shot('reto_escribir_requisito_bucle');
  await ev(() => document.querySelector('.cclose').click());
  await open('w2b', 1); await page.click('#runBtn'); await shot('reto_corregir_sintaxis');
  await ev(() => document.querySelector('.cclose').click());
  await open('w3b', 1); await page.click('#runBtn'); await shot('reto_corregir_bucle_infinito');
  await ev(() => document.querySelector('.cclose').click());
  await open('w4c', 1); await ev(() => document.querySelector('#ed').value = 'def danio(base, critico):\n    if critico:\n        return base * 2\n    return base');
  await page.click('#runBtn'); await shot('reto_funcion_pruebas');
  await ev(() => document.querySelector('.cclose').click());

  // ---------------- Mundo 1: jefe ----------------
  await prep(0, ['w1a', 'w1b']);
  await ev(() => { const G = BB.game; const t = G.terminals.find((x) => x.id === 'w1a'); t.solved = true; G.terminals.find((x) => x.id === 'w1b').solved = true; });
  await put(114); await step(90); await shot('jefe1_escudo_activo');
  await ev(() => { const G = BB.game; G.onSolved(G.terminals.find((x) => x.boss), { solved: true, attempts: 1, hints: 0 }); });
  await step(20); await shot('jefe1_escudo_roto');
  await ev(() => { const G = BB.game; G.aimMode = 'key'; G.p.face = 1; G.keys.add('KeyJ'); G.p.ammo = 12; G.boss.hp = 8; });
  await step(90); await shot('jefe1_combate');
  await ev(() => { const G = BB.game; G.keys.clear(); G.boss.hp = 0.5; G.p.ammo = 12; G.keys.add('KeyJ'); });
  await step(60); await ev(() => BB.game.keys.clear());
  await step(30); await shot('jefe1_derrotado_portal');

  // ---------------- Mundo 2 ----------------
  await prep(1, ['w1a', 'w1b', 'w1c', 'w2a', 'w2b']);
  await put(18); await ev(() => { const G = BB.game; G.aimMode = 'key'; G.p.face = 1; G.keys.add('KeyJ'); G.p.ammo = 12; });
  await step(2); await shot('mundo2_escopeta_distancia');
  await ev(() => BB.game.keys.clear());
  await put(57); await step(40); await shot('mundo2_bifurcacion');
  await put(86, 13); await step(40); await shot('mundo2_torreta');

  // ---------------- Mundo 3 ----------------
  await prep(2, ['w1a', 'w1b', 'w1c', 'w2a', 'w2b', 'w2c', 'w3a']);
  await put(14); await ev(() => { const G = BB.game; G.aimMode = 'key'; G.p.face = 1; G.p.energy = 75; G.pressed.add('KeyF'); });
  await step(16); await shot('mundo3_rafaga');
  await ev(() => { const G = BB.game; G.keys.clear(); const l = G.enemies.find((e) => e.type === 'looper'); l.hp = 0.1; G.damageEnemy(l, 1, false); });
  await step(10); await shot('mundo3_bicho_bucle_reaparece');

  // ---------------- Mundo 4 ----------------
  await prep(3, ['w1a', 'w1b', 'w1c', 'w2a', 'w2b', 'w2c', 'w3a', 'w3b', 'w3c', 'w4a', 'w4b']);
  await put(30, 5); await step(3); await ev(() => { const G = BB.game; G.p.energy = 75; G.aimMode = 'key'; G.p.face = 1; G.pressed.add('ShiftLeft'); });
  await step(6); await shot('mundo4_dash');
  await prep(3, ['w1a', 'w1b', 'w1c', 'w2a', 'w2b', 'w2c', 'w3a', 'w3b', 'w3c', 'w4a', 'w4b', 'w4c']);
  await ev(() => { const G = BB.game; G.terminals.forEach((t) => { if (!t.boss) t.solved = true; }); });
  await put(141); await step(70); await ev(() => { const G = BB.game; G.onSolved(G.terminals.find((x) => x.boss), { solved: true, attempts: 1, hints: 0 }); G.aimMode = 'key'; G.p.face = 1; G.p.ammo = 12; G.keys.add('KeyJ'); });
  await step(100); await shot('jefe4_funcion_maestra');

  await prep(1, ['w1a', 'w1b', 'w1c', 'w2a', 'w2b', 'w2c']);
  await ev(() => { const G = BB.game; G.terminals.forEach((t) => { if (!t.boss) t.solved = true; }); });
  await put(125); await step(150); await ev(() => { const G = BB.game; G.onSolved(G.terminals.find((x) => x.boss), { solved: true, attempts: 1, hints: 0 }); G.aimMode = 'key'; G.p.face = 1; G.p.ammo = 12; G.keys.add('KeyJ'); });
  await step(60); await shot('jefe2_rey_si_entonces');
  await prep(2, ['w1a', 'w1b', 'w1c', 'w2a', 'w2b', 'w2c', 'w3a', 'w3b', 'w3c']);
  await ev(() => { const G = BB.game; G.terminals.forEach((t) => { if (!t.boss) t.solved = true; }); G.loopBroken = true; });
  await put(127); await step(60); await ev(() => { const G = BB.game; G.onSolved(G.terminals.find((x) => x.boss), { solved: true, attempts: 1, hints: 0 }); G.aimMode = 'key'; G.p.face = 1; G.p.ammo = 12; G.p.energy = 75; G.keys.add('KeyJ'); G.boss.spawnT = 0.1; });
  await step(240); await shot('jefe3_bucle_maestro');

  // ---------------- Fin de nivel y pausa ----------------
  await prep(0, ['w1a', 'w1b', 'w1c']);
  await ev(() => { const G = BB.game; G.paused = false; G.terminals.forEach((t) => { if (!t.bonus) { t.solved = true; t.usedSolution = false; } }); G.chips = 12; G.bossDead = true; G.refreshPortal(); G.completeLevel(); });
  await page.waitForTimeout(1400); await shot('fin_nivel');
  await ev(() => { const s = document.getElementById('screens'); s.innerHTML = ''; s.className = ''; BB.game.start(0); });
  await page.waitForTimeout(300);
  await page.keyboard.press('Escape'); await shot('menu_pausa');

  // ---------------- Mapas completos de los niveles ----------------
  const maps = await ev(() => {
    const G = BB.game, res = [];
    document.getElementById('screens').innerHTML = ''; document.getElementById('screens').className = '';
    for (let w = 0; w < 4; w++) {
      G.solved = new Set(); G.start(w); G.paused = true; G.banner = null;
      const big = document.createElement('canvas'); big.width = G.levelW; big.height = 544;
      const c = big.getContext('2d'); const old = G.ctx; G.ctx = c;
      const segs = Math.ceil(G.levelW / 960);
      for (let s = 0; s < segs; s++) {
        G.cam.x = s * 960;
        c.save(); c.beginPath(); c.rect(s * 960, 0, 960, 544); c.clip();
        c.save(); c.translate(s * 960, 0); G.drawBackground(); c.restore();
        G.drawTiles(); G.drawProps(); G.drawPickups(); G.enemies.forEach((e) => G.drawEnemy(e));
        c.restore();
      }
      // jugador en el inicio
      G.p.x = G.start0.x; G.p.y = G.start0.y; G.p.aim = 0; G.drawPlayer();
      G.ctx = old;
      res.push({ w: w + 1, name: G.world.name, w_px: G.levelW, url: big.toDataURL('image/png') });
    }
    return res;
  });
  for (const m of maps) fs.writeFileSync(path.join(OUT, `mapa_mundo${m.w}.png`), Buffer.from(m.url.split(',')[1], 'base64'));
  console.log('  · mapas de nivel:', maps.map((m) => `${m.w}:${m.w_px}px`).join(' '));

  console.log('Capturas:', n, '→', OUT);
  console.log('Errores JS:', errors.length ? errors : 'ninguno');
  await browser.close();
})().catch((e) => { console.error(e); process.exit(1); });
