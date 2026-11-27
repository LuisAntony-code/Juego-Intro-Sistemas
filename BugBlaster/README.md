# BugBlaster: Operación Compilar

**Juego 2D de acción (correr, saltar y disparar) para aprender Python.** Los poderes del robot son código: en cada terminal el jugador resuelve un reto de programación y, al lograrlo, ese código se instala y cambia el juego (más munición, escopeta automática, ráfagas, dash, doble salto, críticos…).

- 4 mundos: **Variables**, **Condicionales**, **Bucles** y **Funciones**, cada uno con un jefe protegido por un escudo.
- 16 retos en 4 tipos: completar, ordenar, predecir y escribir/corregir. El código se **ejecuta de verdad** con un mini-intérprete de Python propio (errores en español, «¿quisiste decir…?»).
- Pensado para que un novato siempre pueda completarlo: pistas, sin castigo por fallar, dificultad de código **guiada/normal/experta** y adaptativa, modo asistido.
- Sin dependencias ni instalación: HTML5 + Canvas + JavaScript.

## Jugar

Abre `BugBlaster/index.html` en un navegador (Chrome, Edge, Firefox o Safari). Sin internet ni servidor.
Opcional: `cd BugBlaster && python3 -m http.server 8000` y abre <http://localhost:8000>.

| Tecla | Acción |
|---|---|
| `A` `D` / `←` `→` | Moverse |
| `W` / `↑` / `Espacio` | Saltar (doble salto con el poder del mundo 4) |
| Ratón + clic, o `J` | Apuntar y disparar |
| `E` | Hackear una terminal |
| `F` / clic derecho | Ráfaga (mundo 3) |
| `Shift` | Dash (mundo 4) |
| `Esc` / `P` | Pausa |

## Estructura

```
BugBlaster/
├─ index.html          página principal
├─ css/style.css       menús y terminal de hackeo
├─ js/
│  ├─ pyinterp.js      mini-intérprete de Python (tokenizador, analizador, evaluador)
│  ├─ challenges.js    los 16 retos + validación por ejecución
│  ├─ challenge-ui.js  interfaz de la terminal de hackeo
│  ├─ levels.js        constructor de mapas y los 4 mundos
│  ├─ game.js          motor: física, combate, jefes, poderes
│  ├─ render.js        dibujo procedural en Canvas + HUD
│  ├─ audio.js         efectos con WebAudio
│  └─ main.js          menús, opciones, guardado y arranque
├─ tests/              pruebas automáticas
├─ tools/              capturas de pantalla y generador del informe
└─ docs/Informe_BugBlaster.docx   informe técnico con capturas
```

## Pruebas

```bash
node BugBlaster/tests/interp.test.js       # intérprete de Python
node BugBlaster/tests/challenges.test.js   # los 16 retos (solución válida / incorrectas inválidas)
node BugBlaster/tests/levels.test.js       # estructura de los mapas
node BugBlaster/tests/ui.e2e.js            # interfaz de retos con el DOM real (requiere Playwright)
node BugBlaster/tests/bot.e2e.js           # un bot completa los 4 mundos (requiere Playwright)
```

## Regenerar las capturas y el informe

```bash
node BugBlaster/tools/capturar.js <carpeta>                 # capturas del juego (Playwright)
python3 BugBlaster/tools/capturar_codigo.py <carpeta>       # fragmentos de código resaltados
python3 BugBlaster/tools/capturar_terminal.py <carpeta>     # salidas de terminal
node BugBlaster/tools/capturar_codigo.js <carpeta>          # convierte las páginas HTML en imágenes
node BugBlaster/tools/generar_informe.js                    # genera docs/Informe_BugBlaster.docx
```
El generador lee las imágenes de `BugBlaster/docs/img/` (no se versiona: se regenera con los comandos anteriores).
