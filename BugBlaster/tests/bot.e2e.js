// Prueba de extremo a extremo: un "bot" juega cada mundo completo en Chromium
// (simulando el juego a máxima velocidad) para comprobar que TODOS son completables.
// Requiere Playwright:  node BugBlaster/tests/bot.e2e.js
const { chromium } = require('playwright');
const path = require('path');

(async () => {
  const browser = await chromium.launch();
  const page = await browser.newPage({ viewport: { width: 1280, height: 720 } });
  const errors = [];
  page.on('pageerror', (e) => errors.push(e.message));
  page.on('console', (m) => { if (m.type() === 'error') errors.push(m.text()); });
  await page.goto('file://' + path.resolve(__dirname, '../index.html'));

  const results = await page.evaluate(() => {
    const out = [];
    const G = BB.game;
    for (let w = 0; w < 4; w++) {
      G.settings.assist = true;             // el bot no esquiva: daño muy reducido
      G.start(w);
      G.banner = null;
      let t = 0, stuck = 0, lastX = -1, jumps = 0;
      const T = 32;
      while (G.state === 'play' && t < 400) {
        const p = G.p;
        G.keys.clear();
        if (!p.dead) {
          // ¿terminal cerca sin hackear? (modo asistido: el bot "resuelve" por API)
          if (G.nearTerminal && !G.nearTerminal.solved && !(G.nearTerminal.bonus)) {
            G.onSolved(G.nearTerminal, { solved: true, attempts: 1, hints: 0 });
          }
          const b = G.boss;
          let dir = 1;
          const bossFight = b && b.active && !b.dead;
          if (bossFight) {
            const term = G.terminals.find((x) => x.boss);
            if (!term.solved) dir = (term.x + 16 > p.x + 10) ? 1 : -1;
            else dir = (b.x + 26 > p.x + 10) ? 1 : -1;
            if (term.solved && Math.abs(b.x + 26 - (p.x + 10)) < 140) dir = -dir; // mantener distancia
          }
          G.keys.add(dir > 0 ? 'KeyD' : 'KeyA');
          G.aimMode = 'key'; p.face = dir;
          G.keys.add('KeyJ');
          // salto: pared, hueco, pinchos o enemigo cerca
          const ahead = (dx) => Math.floor((p.x + p.w / 2 + dir * dx) / T);
          const feet = Math.floor((p.y + p.h - 2) / T);
          const wall = G.tileAt(ahead(18), feet) === '#' || G.tileAt(ahead(18), feet - 1) === '#';
          const gap = p.onGround && G.tileAt(ahead(22), feet + 1) === '.' && G.tileAt(ahead(22), feet + 2) === '.';
          const spike = G.tileAt(ahead(30), feet) === '^' || G.tileAt(ahead(14), feet) === '^';
          const foe = G.enemies.some((e) => !e.dead && e.type !== 'boss' && Math.abs(e.x + e.w / 2 - (p.x + 10)) < 70 && Math.abs(e.y - p.y) < 60);
          if (!bossFight && p.onGround && (wall || gap || spike || foe)) { G.keys.add('Space'); G.pressed.add('Space'); jumps++; }
          if (bossFight && p.onGround && wall) { G.keys.add('Space'); G.pressed.add('Space'); }
          if (p.onGround === false && !p.dead) G.keys.add('Space');       // mantener salto
          if (Math.abs(p.x - lastX) < 0.3 && !bossFight) stuck++; else stuck = 0;
          if (stuck > 90 && p.onGround) { G.keys.add('Space'); G.pressed.add('Space'); stuck = 0; }
          lastX = p.x;
          // ammo infinito para el bot
          p.ammo = G.powers.ammoMax; p.energy = G.powers.energyMax;
        }
        G.update(1 / 60); t += 1 / 60;
      }
      out.push({ world: w + 1, state: G.state, time: +t.toFixed(1), x: Math.round(G.p.x / 32), of: G.W, deaths: G.deaths,
        kills: G.kills, chips: G.chips + '/' + G.chipTotal, boss: G.boss ? Math.round(G.boss.hp) + '/' + Math.round(G.boss.maxhp) : '-',
        terminals: G.terminals.filter((x) => x.solved).length + '/' + G.terminals.length, stars: G.report && G.report.stars });
    }
    return out;
  });
  console.table(results);
  console.log('Errores JS:', errors.length ? errors : 'ninguno');
  const allOk = results.every((r) => r.state === 'complete') && !errors.length;
  console.log(allOk ? '✔ Los 4 mundos se pueden completar' : '✖ Algún mundo no se completó');
  await browser.close();
  process.exit(allOk ? 0 : 1);
})().catch((e) => { console.error(e); process.exit(1); });
