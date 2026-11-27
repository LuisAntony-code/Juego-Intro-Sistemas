// Fotografía las páginas HTML generadas por capturar_codigo.py (y otras páginas .html de la carpeta).
// Uso: node capturar_codigo.js <carpeta>
const { chromium } = require('playwright');
const fs = require('fs'), path = require('path');
(async () => {
  const dir = path.resolve(process.argv[2]);
  const b = await chromium.launch();
  const ctx = await b.newContext({ viewport: { width: 1400, height: 900 }, deviceScaleFactor: 2 });
  const page = await ctx.newPage();
  for (const f of fs.readdirSync(dir).filter((f) => f.endsWith('.html'))) {
    await page.goto('file://' + path.join(dir, f));
    await page.locator('#w').screenshot({ path: path.join(dir, f.replace('.html', '.png')) });
  }
  console.log('listo');
  await b.close();
})();
