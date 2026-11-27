#!/usr/bin/env python3
"""Genera páginas HTML con fragmentos de código resaltados (estilo editor) para el informe.
Uso: python3 capturar_codigo.py <carpeta_salida>      (después: node capturar_codigo.js <carpeta>)"""
import json, os, re, sys, html
from pygments import highlight
from pygments.lexers import get_lexer_by_name
from pygments.formatters import HtmlFormatter

ROOT = os.path.abspath(os.path.join(os.path.dirname(__file__), '..'))
OUT = os.path.abspath(sys.argv[1] if len(sys.argv) > 1 else os.path.join(ROOT, 'docs', 'codigo'))
os.makedirs(OUT, exist_ok=True)

# (clave, archivo, texto donde empieza, nº de líneas, título corto)
S = [
 ('index_html', 'index.html', '<!doctype html>', 28, 'Página principal: carga los scripts en orden'),
 ('interp_tokenizer', 'js/pyinterp.js', 'function tokenize(src)', 34, 'Tokenizador: sangría, números, textos y operadores'),
 ('interp_parser_if', 'js/pyinterp.js', 'parseCond() {', 22, 'Analizador: errores amistosos en if/while'),
 ('interp_binop', 'js/pyinterp.js', 'function binop(op, a, b, line)', 28, 'Operaciones aritméticas con mensajes de error en español'),
 ('interp_exec', 'js/pyinterp.js', '    exec(s, scope) {', 36, 'Intérprete: ejecución de instrucciones'),
 ('interp_lookup', 'js/pyinterp.js', '    lookup(name, scope, line) {', 18, '«¿Quisiste decir…?»: sugerencias ante un NameError'),
 ('interp_run', 'js/pyinterp.js', '  function run(code, opts) {', 34, 'API pública: run() y evalExpr()'),
 ('retos_datos', 'js/challenges.js', "id: 'w2c', world: 2, type: 'escribir'", 30, 'Definición de un reto (jefe del mundo 2)'),
 ('retos_validar', 'js/challenges.js', "case 'escribir': {", 36, 'Validación: se ejecuta el código contra casos de prueba'),
 ('niveles_builder', 'js/levels.js', 'function build(width, fn)', 32, 'Constructor de niveles (suelo, huecos, plataformas, pinchos…)'),
 ('niveles_mundo1', 'js/levels.js', "id: 1, name: 'Núcleo de Datos'", 36, 'Definición del Mundo 1: tema, jefe, retos y mapa'),
 ('juego_poderes', 'js/game.js', 'function computePowers(solved)', 14, 'Los poderes del robot salen de los retos resueltos'),
 ('juego_colision', 'js/game.js', '    moveBody(e, dt) {', 34, 'Física: colisión con el mapa por ejes (incluye plataformas)'),
 ('juego_jugador', 'js/game.js', '      if (p.onGround) { p.coyote = 0.1;', 32, 'Control del jugador: coyote time, salto variable, dash y doble salto'),
 ('juego_disparo', 'js/game.js', '    fireWeapon() {', 24, 'Armas: pistola, rifle y escopeta según la distancia'),
 ('juego_jefe', 'js/game.js', '      // movimiento base: se acerca/aleja', 34, 'Inteligencia artificial del jefe (patrones por mundo)'),
 ('juego_resuelto', 'js/game.js', '    onSolved(t, r) {', 34, 'Al resolver un reto: instala el poder y adapta la dificultad'),
 ('render_jugador', 'js/render.js', '    drawPlayer() {', 30, 'Dibujo procedural del robot'),
 ('render_hud', 'js/render.js', '    drawHUD() {', 32, 'HUD: el estado del robot se muestra como código'),
 ('ui_ordenar', 'js/challenge-ui.js', "progEl.querySelectorAll('.inprog').forEach((d) => {", 22, 'Reto «ordenar»: arrastrar y soltar tarjetas'),
 ('ui_validar', 'js/challenge-ui.js', '  function run() {', 34, 'Comprobar: ejecuta, muestra consola y casos de prueba'),
 ('main_guardado', 'js/main.js', "const KEY = 'bugblaster.v1';", 30, 'Guardado de progreso y opciones (localStorage)'),
 ('css_panel', 'css/style.css', '/* ---------- terminal de hackeo (retos) ---------- */', 22, 'Estilos de la terminal de hackeo'),
 ('test_interprete', 'tests/interp.test.js', "t('errores amigables'", 22, 'Pruebas automáticas del intérprete'),
 ('test_bot', 'tests/bot.e2e.js', 'while (G.state', 34, 'Bot de prueba que juega los niveles completos'),
]

CSS = HtmlFormatter(style='monokai').get_style_defs('.hl')
PAGE = """<!doctype html><html><head><meta charset="utf-8"><style>
body{margin:0;background:#1b1d23;font-family:'DejaVu Sans Mono',monospace}
.win{display:block;width:820px;margin:0;background:#272822;border:1px solid #3a3d46;border-radius:10px;overflow:hidden}
.bar{background:#1e1f1c;color:#a6a69c;font:13px 'DejaVu Sans',sans-serif;padding:8px 14px;display:flex;gap:8px;align-items:center;border-bottom:1px solid #3a3d46}
.dot{width:11px;height:11px;border-radius:50%%;display:inline-block}.r{background:#ff5f56}.y{background:#ffbd2e}.g{background:#27c93f}
.bar b{color:#e6e6e6;margin-left:8px}.bar span.rng{margin-left:auto;color:#8f908a}
.hl{font-size:14px;line-height:1.5;padding:10px 0}
.hl pre{margin:0;padding:0 18px 0 0;white-space:pre-wrap;overflow-wrap:anywhere;color:#f8f8f2;background:transparent !important}
.hl .linenos,.hl .lineno{color:#75715e;display:inline-block;width:3.2em;padding-right:1em;text-align:right;user-select:none}
.hl .special{background:transparent!important}
%s
</style></head><body><div class="win" id="w"><div class="bar"><i class="dot r"></i><i class="dot y"></i><i class="dot g"></i><b>%s</b><span class="rng">%s</span></div><div class="hl">%s</div></div></body></html>"""

LEX = {'.js': 'javascript', '.html': 'html', '.css': 'css', '.py': 'python'}
manifest = []
for key, rel, start, n, caption in S:
    path = os.path.join(ROOT, rel)
    lines = open(path, encoding='utf-8').read().split('\n')
    idx = next((i for i, l in enumerate(lines) if start in l), None)
    if idx is None:
        print('✗ no se encontró', key, start); continue
    chunk = lines[idx: idx + n]
    code = '\n'.join(chunk)
    lex = get_lexer_by_name(LEX.get(os.path.splitext(rel)[1], 'text'))
    fmt = HtmlFormatter(nowrap=False, noclasses=False, linenos='inline', linenostart=idx + 1, cssclass='hlx', style='monokai')
    body = highlight(code, lex, fmt)
    page = PAGE % (CSS, 'BugBlaster/' + rel, f'líneas {idx+1}–{idx+len(chunk)}', body)
    open(os.path.join(OUT, key + '.html'), 'w', encoding='utf-8').write(page)
    manifest.append({'key': key, 'file': 'BugBlaster/' + rel, 'from': idx + 1, 'to': idx + len(chunk), 'caption': caption})
    print(f'✔ {key:18s} {rel}:{idx+1}-{idx+len(chunk)}')
json.dump(manifest, open(os.path.join(OUT, 'manifest.json'), 'w', encoding='utf-8'), ensure_ascii=False, indent=1)
