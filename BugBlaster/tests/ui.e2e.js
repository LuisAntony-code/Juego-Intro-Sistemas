// Prueba de la interfaz de retos: resuelve TODOS los retos con el DOM real
// (clics, tarjetas, escritura) en los 3 niveles de dificultad, y prueba errores y pistas.
const { chromium } = require('playwright');
const path = require('path');

(async () => {
  const browser = await chromium.launch();
  const page = await browser.newPage({ viewport: { width: 1280, height: 720 } });
  const errors = [];
  page.on('pageerror', (e) => errors.push(e.message));
  page.on('console', (m) => { if (m.type() === 'error') errors.push(m.text()); });
  await page.goto('file://' + path.resolve(__dirname, '../index.html'));
  await page.evaluate(() => { document.getElementById('screens').innerHTML = ''; document.getElementById('screens').className = ''; });

  const ids = await page.evaluate(() => Challenges.LIST.map((c) => c.id));
  let passed = 0, failed = 0;
  const fail = (m) => { failed++; console.error('✗', m); };

  for (const id of ids) {
    for (const skill of [0, 1, 2]) {
      await page.evaluate(([id, skill]) => {
        window.__res = null;
        ChallengeUI.open(Challenges.byId[id], { skill, world: Levels.WORLDS[0], done: (r) => { window.__res = r; } });
      }, [id, skill]);
      const ch = await page.evaluate((id) => { const c = Challenges.byId[id]; return { type: c.type, solution: c.solution, expect: Challenges.expectedOutput(c) }; }, id);
      try {
        if (ch.type === 'completar' || ch.type === 'predecir') {
          const val = ch.type === 'predecir' ? ch.expect : ch.solution;
          const choice = page.locator(`.choice[data-v="${val.replace(/"/g, '\\"')}"]`);
          if (await choice.count()) await choice.first().click(); else await page.fill('#ans', val);
        } else if (ch.type === 'ordenar') {
          const lines = await page.evaluate((id) => Challenges.byId[id].solution, id);
          for (const line of lines) {
            const idx = await page.evaluate((line) => {
              const cards = [...document.querySelectorAll('#bank .card')];
              return cards.findIndex((c) => c.textContent.replace(/ /g, ' ') === line);
            }, line);
            if (idx < 0) throw new Error('no se encontró la tarjeta: ' + line);
            await page.locator('#bank .card').nth(idx).click();
          }
        } else {
          const sol = await page.evaluate((id) => Challenges.byId[id].solution, id);
          await page.fill('#ed', sol);
        }
        await page.click('#runBtn');
        await page.waitForSelector('.cfeedback.ok', { timeout: 2000 });
        await page.click('#okBtn');
        const res = await page.evaluate(() => window.__res);
        if (!res || !res.solved) throw new Error('el resultado no marcó solved');
        passed++;
      } catch (e) { fail(`${id} (modo ${skill}): ${e.message.split('\n')[0]}`); await page.evaluate(() => { document.getElementById('challenge').classList.add('hidden'); }); }
    }
  }

  // Cada opción (incluidas las que llevan comillas) debe poder elegirse y quedar escrita en el hueco
  for (const id of ids) {
    const info = await page.evaluate((id) => { const c = Challenges.byId[id]; return { type: c.type, choices: c.choices || [] }; }, id);
    if (info.type !== 'completar' || !info.choices.length) continue;
    await page.evaluate((id) => { ChallengeUI.open(Challenges.byId[id], { skill: 0, world: Levels.WORLDS[0], done: () => {} }); }, id);
    for (let i = 0; i < info.choices.length; i++) {
      await page.locator('.choice').nth(i).click();
      const [picked, shown] = await page.evaluate(() => [document.querySelector('.choice.sel').dataset.v, document.querySelector('#blank').textContent.trim()]);
      if (picked !== shown || !info.choices.includes(picked)) fail(`${id}: la opción «${picked}» no se reflejó bien en el hueco («${shown}»)`); else passed++;
    }
    await page.keyboard.press('Escape');
  }

  // Camino de error + pistas + solución en un reto de escribir
  await page.evaluate(() => { window.__res = null; ChallengeUI.open(Challenges.byId.w2c, { skill: 1, world: Levels.WORLDS[1], done: (r) => { window.__res = r; } }); });
  await page.fill('#ed', 'print("peligro")');
  await page.click('#runBtn');
  await page.waitForSelector('.cfeedback.bad');
  if (!/if/.test(await page.textContent('.cfeedback .msg'))) fail('sin if debe pedir usar if');
  await page.fill('#ed', 'if vida < 30:\n    print("peligro")');
  await page.click('#runBtn'); await page.waitForTimeout(100);
  if ((await page.locator('.tests li.fail').count()) < 1) fail('debía mostrar casos de prueba fallidos');
  await page.fill('#ed', 'if vida < 30\n    print("peligro")');
  await page.click('#runBtn');
  await page.waitForTimeout(100);
  if (!/:/.test(await page.textContent('.cfeedback'))) fail('el error de sintaxis debe mencionar «:»');
  // 3er intento fallido → pista automática
  await page.click('#runBtn'); await page.waitForTimeout(100);
  if ((await page.locator('.hint').count()) < 1) fail('al 3er fallo debía aparecer una pista automática');
  await page.click('#hintBtn'); await page.click('#hintBtn');
  if ((await page.locator('.hint').count()) !== 3 || (await page.locator('.code.sol').count()) !== 1) fail('las 3 pistas y la solución deben mostrarse');
  await page.keyboard.press('Escape');
  const res = await page.evaluate(() => window.__res);
  if (!res || res.solved || res.hints !== 3 || !res.usedSolution) fail('Esc debe cerrar sin resolver, reportando pistas y solución: ' + JSON.stringify(res)); else passed++;

  console.log(`Retos resueltos por la interfaz: ${passed} OK, ${failed} con fallos (${ids.length} retos × 3 modos + flujo de errores)`);
  console.log('Errores JS:', errors.length ? errors : 'ninguno');
  await browser.close();
  process.exit(failed || errors.length ? 1 : 0);
})().catch((e) => { console.error(e); process.exit(1); });
