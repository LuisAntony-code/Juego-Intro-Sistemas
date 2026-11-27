#!/usr/bin/env python3
"""Ejecuta comandos reales y genera páginas HTML con aspecto de terminal (para el informe).
Uso: python3 capturar_terminal.py <carpeta_salida>   (luego: node capturar_codigo.js <carpeta>)"""
import html, os, subprocess, sys
ROOT = os.path.abspath(os.path.join(os.path.dirname(__file__), '..', '..'))
OUT = os.path.abspath(sys.argv[1])
os.makedirs(OUT, exist_ok=True)

PAGE = """<!doctype html><html><head><meta charset="utf-8"><style>
body{margin:0;background:#12141a;font-family:'DejaVu Sans Mono',monospace}
.win{display:inline-block;background:#0c0e13;border:1px solid #2b2f3a;border-radius:10px;overflow:hidden;min-width:820px}
.bar{background:#1a1d26;color:#9aa3b5;font:13px 'DejaVu Sans',sans-serif;padding:8px 14px;display:flex;gap:8px;align-items:center;border-bottom:1px solid #2b2f3a}
.dot{width:11px;height:11px;border-radius:50%%;display:inline-block}.r{background:#ff5f56}.y{background:#ffbd2e}.g{background:#27c93f}.bar b{margin-left:8px;color:#dfe5f2}
pre{margin:0;padding:14px 18px;color:#d6deeb;font-size:14px;line-height:1.5;white-space:pre-wrap;overflow-wrap:anywhere;max-width:880px}
.p{color:#7fdbca}.c{color:#ffd580}.ok{color:#7dff9b}.bad{color:#ff6b7d}
</style></head><body><div class="win" id="w"><div class="bar"><i class="dot r"></i><i class="dot y"></i><i class="dot g"></i><b>%s</b></div><pre>%s</pre></div></body></html>"""

def colorize(text):
    out = []
    for l in text.split('\n'):
        e = html.escape(l)
        if '✔' in l or 'OK' in l: e = '<span class="ok">%s</span>' % e
        elif '✗' in l or '✖' in l or 'fallos' in l: e = '<span class="bad">%s</span>' % e
        out.append(e)
    return '\n'.join(out)

def run(key, title, cmds):
    body = []
    for c in cmds:
        r = subprocess.run(c, shell=True, cwd=ROOT, capture_output=True, text=True, timeout=600)
        body.append('<span class="p">$</span> <span class="c">%s</span>' % html.escape(c))
        body.append(colorize((r.stdout + r.stderr).rstrip()))
        body.append('')
    open(os.path.join(OUT, key + '.html'), 'w', encoding='utf-8').write(PAGE % (title, '\n'.join(body).rstrip()))
    print('✔', key)

run('term_unit', 'Pruebas unitarias (node)', [
    'node BugBlaster/tests/interp.test.js',
    'node BugBlaster/tests/challenges.test.js',
    'node BugBlaster/tests/levels.test.js'])
run('term_bot', 'Bot que juega los 4 mundos completos (Chromium headless)', ['node BugBlaster/tests/bot.e2e.js'])
run('term_ui', 'Prueba de la interfaz de retos (DOM real)', ['node BugBlaster/tests/ui.e2e.js'])
run('term_arbol', 'Estructura del proyecto (líneas por archivo)', [
    'cd BugBlaster && ls && wc -l index.html css/*.css js/*.js tests/*.js'])
