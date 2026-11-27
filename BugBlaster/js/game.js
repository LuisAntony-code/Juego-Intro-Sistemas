/* ============================================================================
 * BugBlaster — motor del juego (lógica). El dibujo está en render.js.
 *
 * Idea central: los PODERES del robot son el código que el jugador resuelve
 * en las terminales. Cada reto superado instala una habilidad real:
 *   variables → munición/vida/energía · if → escopeta, rifle, escudo
 *   bucles → ráfagas · funciones → dash, doble salto y golpes críticos
 * ========================================================================== */
(function (root) {
  'use strict';

  const TILE = 32, VW = 960, VH = 544;
  const GRAV = 1500, SPEED = 215, JUMP = 585;
  const Lv = root.Levels, Ch = root.Challenges, Snd = root.Sound;

  const ACT = {
    facil:   { dmg: 0.6, hp: 0.7, spd: 0.85 },
    normal:  { dmg: 1.0, hp: 1.0, spd: 1.0 },
    dificil: { dmg: 1.4, hp: 1.3, spd: 1.12 },
  };

  const clamp = (v, a, b) => Math.max(a, Math.min(b, v));
  const approach = (v, t, d) => (v < t ? Math.min(t, v + d) : Math.max(t, v - d));
  const rnd = (a, b) => a + Math.random() * (b - a);
  const overlap = (a, b) => a.x < b.x + b.w && a.x + a.w > b.x && a.y < b.y + b.h && a.y + a.h > b.y;

  // Poderes derivados de los retos resueltos (persisten en el guardado).
  function computePowers(solved) {
    const has = (id) => solved.has(id);
    return {
      ammoMax: has('w1a') ? 12 : 6,
      hpMax: has('w1b') ? 120 : 100,
      energyMax: has('w1c') ? 75 : 0,
      shotgun: has('w2a'), rifle: has('w2b'), shield: has('w2c'),
      burst: has('w3c') ? 5 : has('w3a') ? 3 : 0,
      dash: has('w4a'), doubleJump: has('w4b'), crit: has('w4c'),
    };
  }

  const POWER_TEXT = {
    w1a: ['municion = 12', 'Tu cargador ahora guarda 12 balas.'],
    w1b: ['vida = 120', 'Tu armadura aguanta 120 puntos de vida.'],
    w1c: ['energia = 75', 'Nuevo medidor de energía para tus habilidades.'],
    w2a: ['if distancia < 3: arma = "escopeta"', '¡Cambio automático a escopeta con enemigos cerca!'],
    w2b: ['else: arma = "rifle"', 'Con enemigos lejos, tu robot usa el rifle (más rápido).'],
    w2c: ['if vida < 30: escudo = True', 'Con poca vida se activa un escudo que reduce el daño a la mitad.'],
    w3a: ['for i in range(3): disparar()', 'Ráfaga de 3 disparos con F (cuesta 25 de energía).'],
    w3b: ['contador = contador + 1', '¡Bucle roto! Los bichos-gusano ya no reaparecen.'],
    w3c: ['for n in range(1, 6): disparar()', 'Ráfaga mejorada: ¡5 disparos!'],
    w4a: ['saludar()  →  dash()', 'DASH con Shift: te vuelves invulnerable un instante (20 de energía).'],
    w4b: ['def salto(): ...', 'DOBLE SALTO: pulsa saltar otra vez en el aire.'],
    w4c: ['danio(base, critico)', 'Golpes críticos: 20% de tus disparos hacen doble daño.'],
  };

  class Game {
    constructor(canvas, hooks) {
      this.canvas = canvas;
      this.ctx = canvas.getContext('2d');
      this.hooks = hooks || {};
      this.settings = { action: 'normal', code: 0, assist: false, shake: true };
      this.solved = new Set();
      this.adapt = 0;
      this.state = 'idle';
      this.time = 0; this.acc = 0; this.last = 0; this.ts = 1;
      this.keys = new Set(); this.pressed = new Set();
      this.mouse = { x: VW / 2, y: VH / 2, down: false, rdown: false };
      this.aimMode = 'mouse';
      this.cam = { x: 0, y: 0 };
      this.shake = 0;
      this.toasts = [];
      this.paused = false;
      this.powers = computePowers(this.solved);
      this.bindInput();
    }

    // ------------------------------------------------------------ entrada
    typing() {
      const a = document.activeElement;
      return a && (a.tagName === 'INPUT' || a.tagName === 'TEXTAREA' || a.tagName === 'SELECT');
    }
    bindInput() {
      const blockKeys = ['Space', 'ArrowUp', 'ArrowDown', 'ArrowLeft', 'ArrowRight'];
      window.addEventListener('keydown', (e) => {
        if (this.typing()) return;
        if (this.state !== 'play' && this.state !== 'dead') return;
        if (blockKeys.includes(e.code)) e.preventDefault();
        if (!e.repeat) this.pressed.add(e.code);
        this.keys.add(e.code);
        if (e.code === 'KeyJ' || e.code === 'KeyL') this.aimMode = 'key';
        Snd.resume();
      });
      window.addEventListener('keyup', (e) => { this.keys.delete(e.code); });
      window.addEventListener('blur', () => { this.keys.clear(); this.mouse.down = false; this.mouse.rdown = false; });
      const c = this.canvas;
      const pos = (e) => {
        const r = c.getBoundingClientRect();
        this.mouse.x = (e.clientX - r.left) * (VW / r.width);
        this.mouse.y = (e.clientY - r.top) * (VH / r.height);
      };
      c.addEventListener('mousemove', (e) => { pos(e); this.aimMode = 'mouse'; });
      c.addEventListener('mousedown', (e) => {
        pos(e); this.aimMode = 'mouse'; Snd.resume();
        if (e.button === 0) this.mouse.down = true;
        if (e.button === 2) this.mouse.rdown = true;
        e.preventDefault();
      });
      window.addEventListener('mouseup', (e) => {
        if (e.button === 0) this.mouse.down = false;
        if (e.button === 2) this.mouse.rdown = false;
      });
      c.addEventListener('contextmenu', (e) => e.preventDefault());
    }

    // ------------------------------------------------------------ niveles
    start(worldIdx) {
      const W = Lv.WORLDS[worldIdx];
      this.worldIdx = worldIdx;
      this.world = W;
      const lv = W.level;
      this.W = lv.width; this.Hh = lv.height;
      this.levelW = lv.width * TILE;
      this.grid = lv.rows.map((r) => r.split(''));
      this.gate = new Set();
      this.enemies = []; this.bullets = []; this.ebullets = []; this.parts = []; this.pops = [];
      this.pickups = []; this.terminals = []; this.checkpoints = []; this.signs = [];
      this.portal = null; this.boss = null; this.arena = null;
      this.runSolved = new Set();
      this.chips = 0; this.chipTotal = 0; this.deaths = 0; this.kills = 0;
      this.hintsUsed = 0; this.solutionsUsed = 0; this.attempts = 0;
      this.bossDead = false; this.loopBroken = false;
      this.runTime = 0; this.toasts = []; this.banner = { t: 3.2, text: W.name, sub: 'Mundo ' + W.id + ' · ' + W.concept };
      const act = ACT[this.settings.action] || ACT.normal;
      this.act = act;

      const tIds = W.terminals.slice(), bIds = W.bonus.slice();
      const found = [];
      for (let y = 0; y < this.Hh; y++) for (let x = 0; x < this.W; x++) {
        const c = this.grid[y][x];
        if ('PTbCEwflsBoah'.includes(c)) found.push({ c, x, y });
      }
      found.sort((a, b) => a.x - b.x || a.y - b.y);
      for (const { c, x, y } of found) {
        this.grid[y][x] = '.';
        const bx = x * TILE, by = (y + 1) * TILE;
        switch (c) {
          case 'P': this.start0 = { x: bx + 6, y: by - 28 }; break;
          case 'T': { const id = tIds.shift(); this.terminals.push({ x: bx, y: by - 44, w: 32, h: 44, id, ch: Ch.byId[id], solved: false, bonus: false, boss: this.terminals.filter((t) => !t.bonus).length === W.terminals.length - 1 }); break; }
          case 'b': { const id = bIds.shift(); this.terminals.push({ x: bx, y: by - 44, w: 32, h: 44, id, ch: Ch.byId[id], solved: false, bonus: true, boss: false }); this.chipTotal++; break; }
          case 'C': this.checkpoints.push({ x: bx, y: by - 48, w: 32, h: 48, on: false, sx: bx + 6, sy: by - 28 }); break;
          case 'E': this.portal = { x: bx - 8, y: by - 80, w: 48, h: 80, open: false }; break;
          case 'w': this.addEnemy('walker', bx, by, 26, 22, 2, 55); break;
          case 'l': this.addEnemy('looper', bx, by, 30, 20, 3, 70); break;
          case 'f': this.addEnemy('flyer', bx + 4, y * TILE + 4, 24, 24, 2, 70); break;
          case 's': this.addEnemy('shooter', bx, by, 28, 28, 3, 0); break;
          case 'B': this.makeBoss(bx - 10, by); break;
          case 'o': this.pickups.push({ t: 'chip', x: bx + 8, y: y * TILE + 8, w: 16, h: 16, ph: Math.random() * 6 }); this.chipTotal++; break;
          case 'a': this.pickups.push({ t: 'ammo', x: bx + 6, y: by - 18, w: 20, h: 16, ph: Math.random() * 6 }); break;
          case 'h': this.pickups.push({ t: 'hp', x: bx + 6, y: by - 18, w: 20, h: 18, ph: Math.random() * 6 }); break;
          default: break;
        }
      }
      this.terminals.sort((a, b) => a.x - b.x);
      this.signs = lv.signs.map((s) => ({ x: s.x * TILE, y: (s.row + 1) * TILE, text: s.text }));
      if (lv.arena) this.arena = { x0: lv.arena.x0, x1: lv.arena.x1, active: false };
      // Estrellas de fondo deterministas
      this.rng = 1337 + worldIdx * 77;

      this.powers = computePowers(this.solved);
      this.p = {
        x: this.start0.x, y: this.start0.y, w: 20, h: 28, vx: 0, vy: 0, onGround: false, face: 1,
        coyote: 0, jumpBuf: 0, djUsed: false, dropT: 0, hp: this.powers.hpMax, ammo: this.powers.ammoMax,
        energy: this.powers.energyMax, inv: 0, shootCd: 0, dashT: 0, dashCd: 0, burstQ: [], burstT: 0,
        dead: false, deadT: 0, anim: 0, ammoAcc: 0, aim: 0,
      };
      this.checkpoint = { x: this.start0.x, y: this.start0.y };
      this.cam.x = clamp(this.p.x - VW / 2, 0, this.levelW - VW); this.cam.y = 0;
      this.state = 'play'; this.ts = 1; this.paused = false; this.acc = 0;
      this.keys.clear(); this.pressed.clear();
      this.toast('Mundo ' + W.id + ': ' + W.concept, W.theme.accent);
    }

    addEnemy(type, x, bottom, w, h, hp, spd) {
      const m = this.act;
      this.enemies.push({
        type, x, y: bottom - h, w, h, hp: hp * m.hp, maxhp: hp * m.hp, vx: 0, vy: 0, dir: Math.random() < 0.5 ? -1 : 1,
        spd: spd * m.spd, home: { x, y: bottom - h }, t: Math.random() * 5, dead: false, respawn: 0, flash: 0, cd: rnd(0.5, 2), tele: 0,
      });
    }

    makeBoss(x, bottom) {
      const def = this.world.boss, m = this.act;
      this.boss = {
        type: 'boss', style: def.style, name: def.name, color: def.color, x, y: bottom - 54, w: 52, h: 54,
        vx: 0, vy: 0, hp: def.hp * m.hp, maxhp: def.hp * m.hp, shield: true, active: false, dead: false, dir: -1,
        t: 0, atk: 2, dashT: 0, tele: 0, flash: 0, hop: 5, spawnT: 6, home: { x, y: bottom - 54 }, burst: 0, burstT: 0,
      };
      this.enemies.push(this.boss);
    }

    // --------------------------------------------------------- utilidades
    tileAt(tx, ty) {
      if (tx < 0 || tx >= this.W) return '#';
      if (ty < 0 || ty >= this.Hh) return '.';
      return this.grid[ty][tx];
    }
    toast(text, color) { this.toasts.push({ text, t: 3.2, color: color || '#fff' }); if (this.toasts.length > 4) this.toasts.shift(); }
    popup(x, y, text, color, big) { this.pops.push({ x, y, text, color: color || '#fff', t: 0.9, big: !!big }); }
    burst(x, y, color, n, spd, grav) {
      for (let i = 0; i < n; i++) {
        const a = Math.random() * Math.PI * 2, s = rnd(spd * 0.3, spd);
        this.parts.push({ x, y, vx: Math.cos(a) * s, vy: Math.sin(a) * s - spd * 0.2, life: rnd(0.3, 0.7), max: 0.7, color, size: rnd(2, 5), grav: grav === undefined ? 600 : grav });
      }
    }
    doShake(n) { if (this.settings.shake) this.shake = Math.max(this.shake, n); }

    // Mueve un cuerpo con colisión contra el mapa.
    moveBody(e, dt) {
      const res = { hitX: false, landed: false, ceil: false };
      e.onGround = false;
      const dx = Math.sign(e.vx);
      e.x += e.vx * dt;
      let l = Math.floor(e.x / TILE), r = Math.floor((e.x + e.w - 0.01) / TILE);
      let t = Math.floor(e.y / TILE), b = Math.floor((e.y + e.h - 0.01) / TILE);
      for (let ty = t; ty <= b; ty++) for (let tx = l; tx <= r; tx++) {
        if (this.tileAt(tx, ty) === '#') {
          if (dx > 0) e.x = tx * TILE - e.w; else if (dx < 0) e.x = (tx + 1) * TILE; else continue;
          res.hitX = true; e.vx = 0;
        }
      }
      const dy = Math.sign(e.vy);
      e.py = e.y;
      e.y += e.vy * dt;
      l = Math.floor(e.x / TILE); r = Math.floor((e.x + e.w - 0.01) / TILE);
      t = Math.floor(e.y / TILE); b = Math.floor((e.y + e.h - 0.01) / TILE);
      for (let ty = t; ty <= b; ty++) for (let tx = l; tx <= r; tx++) {
        const c = this.tileAt(tx, ty);
        if (c === '#') {
          if (dy > 0) { e.y = ty * TILE - e.h; res.landed = true; } else if (dy < 0) { e.y = (ty + 1) * TILE; res.ceil = true; } else continue;
          e.vy = 0;
        } else if (c === '=' && dy >= 0 && !(e.dropT > 0) && e.py + e.h <= ty * TILE + 1.5 && e.y + e.h >= ty * TILE) {
          e.y = ty * TILE - e.h; e.vy = 0; res.landed = true;
        }
      }
      if (res.landed) e.onGround = true;
      return res;
    }

    // -------------------------------------------------------------- bucle
    frame(now) {
      if (!this.last) this.last = now;
      const raw = Math.min(0.1, (now - this.last) / 1000);
      this.last = now;
      if (!this.paused && this.state !== 'idle') {
        this.acc += raw;
        let steps = 0;
        while (this.acc >= 1 / 60 && steps < 5) { this.update(1 / 60); this.acc -= 1 / 60; steps++; }
        if (steps === 5) this.acc = 0;
      }
      if (this.state !== 'idle') this.render();
    }

    update(rdt) {
      this.time += rdt;
      const dt = rdt * this.ts;
      if (this.state === 'complete') { this.updateFx(dt); this.pressed.clear(); return; }
      this.runTime += rdt;
      if (this.banner && this.banner.t > 0) this.banner.t -= rdt;
      for (const t of this.toasts) t.t -= rdt;
      this.toasts = this.toasts.filter((t) => t.t > 0);

      if (this.state === 'play' || this.state === 'dead') this.updatePlayer(dt);
      else this.p.vx = 0;
      this.updateEnemies(dt);
      this.updateBullets(dt);
      this.updatePickups(dt);
      this.updateWorldTriggers(dt);
      this.updateFx(dt);
      this.updateCamera(dt);
      this.pressed.clear();
      if (this.mouse.rdown) this.pressed.add('Mouse2');
    }

    // ------------------------------------------------------------ jugador
    aimAngle() {
      const p = this.p, ox = p.x + p.w / 2, oy = p.y + 14;
      if (this.aimMode === 'mouse') {
        const mx = this.mouse.x + this.cam.x, my = this.mouse.y + this.cam.y;
        let a = Math.atan2(my - oy, mx - ox);
        if (this.settings.assist) {
          let best = null, bd = 0.45;
          for (const e of this.enemies) {
            if (e.dead || (e.type === 'boss' && !e.active)) continue;
            const ea = Math.atan2(e.y + e.h / 2 - oy, e.x + e.w / 2 - ox);
            let d = Math.abs(ea - a); if (d > Math.PI) d = Math.PI * 2 - d;
            if (d < bd && Math.hypot(e.x - ox, e.y - oy) < 520) { bd = d; best = ea; }
          }
          if (best !== null) a = best;
        }
        return a;
      }
      return p.face > 0 ? 0 : Math.PI;
    }

    nearestEnemyDist() {
      const p = this.p, cx = p.x + p.w / 2, cy = p.y + p.h / 2;
      let best = Infinity;
      for (const e of this.enemies) {
        if (e.dead || (e.type === 'boss' && !e.active)) continue;
        const d = Math.hypot(e.x + e.w / 2 - cx, e.y + e.h / 2 - cy);
        if (d < best) best = d;
      }
      return best;
    }

    currentWeapon() {
      const d = this.nearestEnemyDist() / TILE;
      if (this.powers.shotgun && d < 3) return 'escopeta';
      if (this.powers.rifle) return 'rifle';
      return 'pistola';
    }

    updatePlayer(dt) {
      const p = this.p, pw = this.powers, k = this.keys;
      if (p.dead) {
        p.deadT -= dt;
        if (p.deadT <= 0) this.respawn();
        return;
      }
      const left = k.has('KeyA') || k.has('ArrowLeft'), right = k.has('KeyD') || k.has('ArrowRight');
      const jumpHeld = k.has('KeyW') || k.has('ArrowUp') || k.has('Space');
      const jumpPressed = this.pressed.has('KeyW') || this.pressed.has('ArrowUp') || this.pressed.has('Space');
      const downPressed = this.pressed.has('KeyS') || this.pressed.has('ArrowDown');
      const interact = this.pressed.has('KeyE') || this.pressed.has('Enter');
      const dir = (right ? 1 : 0) - (left ? 1 : 0);
      const inChallenge = this.state === 'challenge';

      if (p.onGround) { p.coyote = 0.1; p.djUsed = false; } else p.coyote -= dt;
      p.jumpBuf = jumpPressed ? 0.12 : p.jumpBuf - dt;
      p.inv = Math.max(0, p.inv - dt);
      p.shootCd = Math.max(0, p.shootCd - dt);
      p.dashCd = Math.max(0, p.dashCd - dt);
      p.dropT = Math.max(0, p.dropT - dt);
      p.anim += dt;

      if (p.dashT > 0) {
        p.dashT -= dt; p.vy = 0;
        if (Math.random() < 0.6) this.parts.push({ x: p.x + 10, y: p.y + 14, vx: 0, vy: 0, life: 0.25, max: 0.25, color: '#9ff', size: 6, grav: 0 });
        if (p.dashT <= 0) p.vx *= 0.35;
      } else {
        const target = dir * SPEED;
        if (dir !== 0) p.vx = approach(p.vx, target, (p.onGround ? 2400 : 1500) * dt);
        else p.vx = approach(p.vx, 0, (p.onGround ? 2800 : 500) * dt);
        p.vy = Math.min(900, p.vy + GRAV * dt);
        if (p.jumpBuf > 0 && p.coyote > 0) {
          p.vy = -JUMP; p.jumpBuf = 0; p.coyote = 0; Snd.play('jump');
          this.burst(p.x + 10, p.y + 28, '#9bd', 5, 90, 200);
        } else if (jumpPressed && !p.onGround && p.coyote <= 0 && pw.doubleJump && !p.djUsed) {
          p.vy = -JUMP * 0.92; p.djUsed = true; Snd.play('djump');
          this.burst(p.x + 10, p.y + 28, '#ffd36b', 10, 140, 100);
        }
        if (!jumpHeld && p.vy < -230) p.vy = -230;
        if (downPressed && p.onGround) {
          const below = this.tileAt(Math.floor((p.x + p.w / 2) / TILE), Math.floor((p.y + p.h + 1) / TILE));
          if (below === '=') { p.dropT = 0.22; p.y += 2; }
        }
      }

      // Disparos y habilidades
      const mouseMode = this.aimMode === 'mouse';
      if (mouseMode) p.face = (this.mouse.x + this.cam.x) >= p.x + p.w / 2 ? 1 : -1;
      else if (dir !== 0) p.face = dir;
      p.aim = this.aimAngle();
      if (!inChallenge) {
        if (this.mouse.down || k.has('KeyJ')) this.fireWeapon();
        if ((this.pressed.has('KeyF') || this.pressed.has('KeyL') || this.pressed.has('Mouse2')) && pw.burst > 0) this.startBurst();
        if ((this.pressed.has('ShiftLeft') || this.pressed.has('ShiftRight') || this.pressed.has('KeyK')) && pw.dash) this.startDash(dir);
        if (interact) this.tryInteract();
      }
      if (p.burstQ.length) {
        p.burstT -= dt;
        if (p.burstT <= 0) { p.burstQ.pop(); p.burstT = 0.08; this.spawnBullet(p.aim, 720, 1.3, 0.9, 'burst'); Snd.play('burst'); }
      }

      // Recarga
      if (p.ammo < pw.ammoMax) { p.ammoAcc += dt * 1.6; while (p.ammoAcc >= 1 && p.ammo < pw.ammoMax) { p.ammo++; p.ammoAcc -= 1; } } else p.ammoAcc = 0;
      if (pw.energyMax > 0) p.energy = Math.min(pw.energyMax, p.energy + 7 * dt);

      this.moveBody(p, dt);
      if (p.y > this.Hh * TILE + 60) this.killPlayer('pit');
      this.checkHazards();
    }

    spawnBullet(ang, speed, dmg, life, kind) {
      const p = this.p, ox = p.x + p.w / 2 + Math.cos(ang) * 14, oy = p.y + 14 + Math.sin(ang) * 14;
      let crit = false, d = dmg;
      if (this.powers.crit && Math.random() < 0.2) { crit = true; d *= 2; }
      this.bullets.push({ x: ox, y: oy, vx: Math.cos(ang) * speed, vy: Math.sin(ang) * speed, life, dmg: d, crit, kind: kind || 'n', r: 3 });
      this.parts.push({ x: ox, y: oy, vx: Math.cos(ang) * 60, vy: Math.sin(ang) * 60, life: 0.1, max: 0.1, color: '#fff7a8', size: 6, grav: 0 });
    }

    fireWeapon() {
      const p = this.p;
      if (p.shootCd > 0) return;
      const w = this.currentWeapon();
      const cfg = { pistola: { cd: 0.28, cost: 1, sp: 580, dmg: 1, life: 0.85 }, rifle: { cd: 0.15, cost: 1, sp: 780, dmg: 1.2, life: 1.0 }, escopeta: { cd: 0.6, cost: 2, sp: 540, dmg: 1, life: 0.28 } }[w];
      if (p.ammo < cfg.cost) { p.shootCd = 0.18; Snd.play('empty'); if (!this.noAmmoMsg || this.time - this.noAmmoMsg > 1.5) { this.popup(p.x + 10, p.y - 8, 'sin munición…', '#ff9'); this.noAmmoMsg = this.time; } return; }
      p.ammo -= cfg.cost; p.shootCd = cfg.cd;
      const a = p.aim;
      if (w === 'escopeta') {
        for (let i = -2; i <= 2; i++) this.spawnBullet(a + i * 0.13 + rnd(-0.03, 0.03), cfg.sp * rnd(0.9, 1.1), cfg.dmg, cfg.life, 'pellet');
        Snd.play('shotgun'); this.doShake(3);
        p.vx -= Math.cos(a) * 60;
      } else {
        this.spawnBullet(a, cfg.sp, cfg.dmg, cfg.life, w);
        Snd.play(w === 'rifle' ? 'rifle' : 'shoot');
      }
    }

    startBurst() {
      const p = this.p;
      if (p.burstQ.length || p.energy < 25) { if (p.energy < 25) this.popup(p.x + 10, p.y - 8, 'poca energía', '#9cf'); return; }
      p.energy -= 25;
      p.burstQ = new Array(this.powers.burst).fill(1); p.burstT = 0;
    }

    startDash(dir) {
      const p = this.p;
      if (p.dashCd > 0 || p.dashT > 0) return;
      if (p.energy < 20) { this.popup(p.x + 10, p.y - 8, 'poca energía', '#9cf'); return; }
      p.energy -= 20; p.dashT = 0.17; p.dashCd = 0.45; p.inv = Math.max(p.inv, 0.3);
      p.vx = (dir || p.face) * 560; p.vy = 0;
      Snd.play('dash');
    }

    shieldActive() { return this.powers.shield && this.p.hp < 30; }

    hurtPlayer(dmg, fromX) {
      const p = this.p;
      if (p.dead || p.inv > 0 || p.dashT > 0 || this.state === 'complete') return;
      let d = dmg * this.act.dmg;
      if (this.settings.assist) d *= 0.3;
      if (this.shieldActive()) { d *= 0.5; Snd.play('shield'); }
      p.hp -= d; p.inv = 1.0;
      const s = fromX !== undefined && fromX < p.x + p.w / 2 ? 1 : -1;
      p.vx = s * 220; p.vy = -260;
      this.popup(p.x + 10, p.y - 4, '-' + Math.ceil(d), '#ff6b6b');
      this.burst(p.x + 10, p.y + 14, '#ff6b6b', 8, 160);
      Snd.play('hurt'); this.doShake(6);
      if (p.hp <= 0) {
        if (this.settings.assist) { p.hp = 1; } else this.killPlayer('dmg');
      }
    }

    killPlayer() {
      const p = this.p;
      if (p.dead) return;
      p.dead = true; p.deadT = 1.1; p.hp = 0; p.vx = 0; p.vy = 0;
      this.deaths++;
      this.burst(p.x + 10, p.y + 14, '#5ad1ff', 26, 300);
      Snd.play('die'); this.doShake(10);
    }

    respawn() {
      const p = this.p;
      p.x = this.checkpoint.x; p.y = this.checkpoint.y; p.vx = 0; p.vy = 0;
      p.hp = this.powers.hpMax; p.ammo = this.powers.ammoMax; p.energy = this.powers.energyMax;
      p.inv = 1.5; p.dead = false; p.burstQ = []; p.dashT = 0;
      this.ebullets = []; this.bullets = [];
      if (this.arena && this.arena.active) this.resetBoss();
      this.toast('Reiniciando desde el último checkpoint', '#9cf');
    }

    checkHazards() {
      const p = this.p;
      const l = Math.floor(p.x / TILE), r = Math.floor((p.x + p.w) / TILE), t = Math.floor(p.y / TILE), b = Math.floor((p.y + p.h) / TILE);
      for (let ty = t; ty <= b; ty++) for (let tx = l; tx <= r; tx++) {
        if (this.tileAt(tx, ty) === '^') {
          const spike = { x: tx * TILE + 4, y: ty * TILE + 14, w: 24, h: 18 };
          if (overlap(p, spike)) { this.hurtPlayer(20, tx * TILE + 16); if (!p.dead) p.vy = -330; }
        }
      }
    }

    // --------------------------------------------------------- enemigos
    updateEnemies(dt) {
      const p = this.p;
      for (const e of this.enemies) {
        if (e.type === 'boss') { this.updateBoss(e, dt); continue; }
        if (e.dead) {
          if (e.type === 'looper' && !this.loopBroken && e.respawn > 0) {
            e.respawn -= dt;
            if (e.respawn <= 0) { e.dead = false; e.hp = e.maxhp; e.x = e.home.x; e.y = e.home.y; e.vx = 0; e.vy = 0; this.burst(e.x + 15, e.y + 10, '#8f8', 10, 120); }
          }
          continue;
        }
        e.t += dt; e.flash = Math.max(0, e.flash - dt);
        const px = p.x + p.w / 2, py = p.y + p.h / 2, ex = e.x + e.w / 2, ey = e.y + e.h / 2;
        if (e.type === 'walker' || e.type === 'looper') {
          const near = Math.abs(px - ex) < 260 && Math.abs(py - ey) < 90 && !p.dead;
          if (near) e.dir = px > ex ? 1 : -1;
          e.vx = e.dir * e.spd * (near ? 1.35 : 1);
          e.vy = Math.min(900, e.vy + GRAV * dt);
          const res = this.moveBody(e, dt);
          const fx = e.dir > 0 ? e.x + e.w + 3 : e.x - 3;
          const ahead = this.tileAt(Math.floor(fx / TILE), Math.floor((e.y + e.h + 3) / TILE));
          const spikeAhead = this.tileAt(Math.floor(fx / TILE), Math.floor((e.y + e.h - 4) / TILE)) === '^';
          if (e.onGround && (res.hitX || ahead === '.' || ahead === '^' || spikeAhead)) { e.dir *= -1; e.x += e.dir * 3; }
          if (e.y > this.Hh * TILE + 60) { e.dead = true; e.respawn = 0; }
        } else if (e.type === 'flyer') {
          const dx = px - ex;
          if (Math.abs(dx) < 340 && !p.dead) {
            e.x += Math.sign(dx) * e.spd * dt;
            e.y += clamp(py - 20 - ey, -1, 1) * 38 * dt;
          }
          e.y += Math.sin(e.t * 2.4) * 22 * dt;
          e.x = clamp(e.x, TILE, this.levelW - TILE - e.w);
          e.y = clamp(e.y, 0, (this.Hh - 4) * TILE);
          e.dir = dx > 0 ? 1 : -1;
        } else if (e.type === 'shooter') {
          e.cd -= dt;
          const inRange = Math.abs(px - ex) < 440 && Math.abs(py - ey) < 240 && !p.dead;
          e.tele = inRange && e.cd < 0.35 ? e.cd : 0;
          if (inRange && e.cd <= 0) {
            e.cd = 2.1 / this.act.spd;
            const a = Math.atan2(py - ey, px - ex);
            this.ebullets.push({ x: ex, y: ey - 4, vx: Math.cos(a) * 190 * this.act.spd, vy: Math.sin(a) * 190 * this.act.spd, life: 3, dmg: 10, r: 5, color: '#ff7a59' });
            Snd.play('shoot');
          }
          e.dir = px > ex ? 1 : -1;
        }
        // Contacto con el jugador
        if (!p.dead && overlap(p, { x: e.x + 3, y: e.y + 3, w: e.w - 6, h: e.h - 6 })) this.hurtPlayer(e.type === 'flyer' ? 10 : 12, ex);
      }
    }

    damageEnemy(e, dmg, crit) {
      if (e.dead) return;
      if (e.type === 'boss' && (e.shield || !e.active)) {
        if (this.time - (e.shieldMsg || -9) > 1.4) { this.popup(e.x + e.w / 2, e.y - 6, e.active ? '¡Escudo! Hackea la terminal' : '…', '#9cf'); e.shieldMsg = this.time; }
        Snd.play('shield'); this.burst(e.x + e.w / 2, e.y + e.h / 2, '#6cf', 3, 100, 0);
        return;
      }
      e.hp -= dmg; e.flash = 0.1; Snd.play('hit');
      this.popup(e.x + e.w / 2, e.y - 2, crit ? '¡CRÍTICO! ' + dmg.toFixed(1) : dmg.toFixed(1), crit ? '#ffd36b' : '#fff', crit);
      if (e.hp <= 0) this.killEnemy(e);
    }

    killEnemy(e) {
      e.dead = true; this.kills++;
      const cx = e.x + e.w / 2, cy = e.y + e.h / 2;
      this.burst(cx, cy, e.type === 'flyer' ? '#cfe8ff' : '#ff6b6b', 16, 240);
      Snd.play('kill'); this.doShake(4);
      if (e.type === 'looper' && !this.loopBroken) e.respawn = 4.5;
      if (e.type === 'boss') return;
      const r = Math.random();
      if (r < 0.28) this.pickups.push({ t: 'ammo', x: cx - 10, y: e.y + e.h - 18, w: 20, h: 16, ph: 0 });
      else if (r < 0.4) this.pickups.push({ t: 'hp', x: cx - 10, y: e.y + e.h - 18, w: 20, h: 18, ph: 0 });
    }

    // -------------------------------------------------------------- jefe
    activateArena() {
      const a = this.arena;
      a.active = true;
      for (let y = 0; y < 14; y++) { this.grid[y][a.x0] = '#'; this.gate.add(y * 10000 + a.x0); }
      this.boss.active = true; this.boss.atk = 1.6;
      this.banner = { t: 2.6, text: this.boss.name, sub: 'JEFE · ' + (this.boss.shield ? 'tiene un escudo: ¡hackea su terminal!' : 'sin escudo') };
      this.doShake(6);
    }

    resetBoss() {
      const a = this.arena, b = this.boss;
      a.active = false;
      for (let y = 0; y < 14; y++) { this.grid[y][a.x0] = '.'; }
      this.gate.clear();
      b.active = false; b.hp = b.maxhp; b.x = b.home.x; b.y = b.home.y; b.vx = 0; b.vy = 0; b.dashT = 0; b.tele = 0; b.burst = 0;
      this.enemies = this.enemies.filter((e) => !e.spawnedByBoss);
    }

    updateBoss(b, dt) {
      const p = this.p, a = this.arena;
      b.flash = Math.max(0, b.flash - dt);
      if (b.dead) return;
      b.t += dt;
      b.vy = Math.min(900, b.vy + GRAV * dt);
      if (!b.active) { b.vx = 0; this.moveBody(b, dt); return; }
      const minX = (a.x0 + 1) * TILE, maxX = (a.x1 - 1) * TILE - b.w;
      const px = p.x + p.w / 2, ex = b.x + b.w / 2, py = p.y + p.h / 2, ey = b.y + b.h / 2;
      const spd = this.act.spd;
      const aimA = Math.atan2(py - ey, px - ex);
      const shoot = (ang, sp, color) => {
        this.ebullets.push({ x: ex, y: ey, vx: Math.cos(ang) * sp * spd, vy: Math.sin(ang) * sp * spd, life: 4, dmg: 15, r: 7, color: color || '#ff5d73' });
        Snd.play('shoot');
      };
      // movimiento base: se acerca/aleja manteniendo distancia
      if (b.dashT > 0) {
        b.dashT -= dt; b.vx = b.dir * 400 * spd;
      } else if (b.tele > 0) {
        b.tele -= dt; b.vx = 0;
        if (b.tele <= 0) { b.dashT = 0.75; b.dir = px > ex ? 1 : -1; Snd.play('dash'); }
      } else {
        const want = Math.abs(px - ex) > 230 ? Math.sign(px - ex) : -Math.sign(px - ex) * 0.4;
        b.dir = px > ex ? 1 : -1;
        b.vx = want * 70 * spd;
      }
      b.atk -= dt; b.hop -= dt;
      if (b.hop <= 0 && b.onGround) { b.vy = -520; b.hop = rnd(4, 6); }
      if (b.style === 'var') {
        if (b.atk <= 0) { b.atk = 1.7 / spd; shoot(aimA, 190, '#ff7a90'); }
      } else if (b.style === 'cond') {
        if (b.atk <= 0) { b.atk = 2.0 / spd; for (let i = -1; i <= 1; i++) shoot(aimA + i * 0.32, 185, i === 0 ? '#ffd36b' : '#ffb347'); }
      } else if (b.style === 'loop') {
        if (b.atk <= 0) { b.atk = 2.6 / spd; b.burst = 3; b.burstT = 0; }
        if (b.burst > 0) { b.burstT -= dt; if (b.burstT <= 0) { b.burstT = 0.22; b.burst--; shoot(Math.atan2(py - ey, px - ex), 230, '#9dff6b'); } }
        b.spawnT -= dt;
        if (b.spawnT <= 0 && this.enemies.filter((e) => e.spawnedByBoss && !e.dead).length < 2) {
          b.spawnT = 9;
          this.addEnemy('looper', b.x + 10, b.y + b.h, 30, 20, 2, 80);
          const ne = this.enemies[this.enemies.length - 1]; ne.spawnedByBoss = true; ne.home = { x: b.x, y: b.y };
          this.loopBrokenForSpawned = true;
        }
      } else if (b.style === 'func') {
        if (b.atk <= 0) {
          b.atk = 3.2 / spd;
          if (Math.random() < 0.5 && b.onGround) { b.tele = 0.6; }
          else for (let i = -2; i <= 2; i++) shoot(aimA + i * 0.26, 190, '#d7a2ff');
        }
      }
      this.moveBody(b, dt);
      b.x = clamp(b.x, minX, maxX);
      if (b.dashT > 0 && (b.x <= minX + 1 || b.x >= maxX - 1)) b.dashT = 0;
      if (!p.dead && overlap(p, { x: b.x + 4, y: b.y + 4, w: b.w - 8, h: b.h - 8 })) this.hurtPlayer(20, ex);
    }

    defeatBoss() {
      const b = this.boss;
      b.dead = true; this.bossDead = true;
      this.burst(b.x + b.w / 2, b.y + b.h / 2, b.color, 60, 380);
      this.burst(b.x + b.w / 2, b.y + b.h / 2, '#fff', 30, 260);
      Snd.play('boom'); this.doShake(16);
      this.enemies.forEach((e) => { if (e.spawnedByBoss && !e.dead) this.killEnemy(e); });
      this.ebullets = [];
      for (let i = 0; i < 3; i++) this.pickups.push({ t: 'chip', x: b.x + 10 + i * 14, y: b.y + 10, w: 16, h: 16, ph: i });
      this.chipTotal += 3;
      this.banner = { t: 2.5, text: '¡' + b.name + ' derrotado!', sub: 'Cruza el portal para terminar el mundo' };
      const a = this.arena;
      for (let y = 0; y < 14; y++) this.grid[y][a.x0] = '.';
      this.gate.clear();
      this.refreshPortal();
    }

    refreshPortal() {
      const need = this.terminals.filter((t) => !t.bonus).every((t) => t.solved);
      this.portal.open = need && this.bossDead;
    }

    // ------------------------------------------------------ proyectiles
    updateBullets(dt) {
      for (const b of this.bullets) {
        b.x += b.vx * dt; b.y += b.vy * dt; b.life -= dt;
        if (this.settings.assist && b.kind !== 'pellet') {
          // ligera ayuda: curva hacia el enemigo más cercano
          let best = null, bd = 140;
          for (const e of this.enemies) {
            if (e.dead || (e.type === 'boss' && !e.active)) continue;
            const d = Math.hypot(e.x + e.w / 2 - b.x, e.y + e.h / 2 - b.y);
            if (d < bd) { bd = d; best = e; }
          }
          if (best) {
            const sp = Math.hypot(b.vx, b.vy), a = Math.atan2(b.vy, b.vx), ta = Math.atan2(best.y + best.h / 2 - b.y, best.x + best.w / 2 - b.x);
            let da = ta - a; while (da > Math.PI) da -= Math.PI * 2; while (da < -Math.PI) da += Math.PI * 2;
            const na = a + clamp(da, -3 * dt, 3 * dt);
            b.vx = Math.cos(na) * sp; b.vy = Math.sin(na) * sp;
          }
        }
        if (this.tileAt(Math.floor(b.x / TILE), Math.floor(b.y / TILE)) === '#') { b.life = 0; this.burst(b.x, b.y, '#ffe9a0', 3, 90, 0); continue; }
        for (const e of this.enemies) {
          if (e.dead || b.life <= 0) continue;
          if (b.x > e.x - 2 && b.x < e.x + e.w + 2 && b.y > e.y - 2 && b.y < e.y + e.h + 2) {
            this.damageEnemy(e, b.dmg, b.crit);
            if (e.type === 'boss' && !e.shield && e.hp <= 0 && !this.bossDead) this.defeatBoss();
            b.life = 0;
          }
        }
      }
      this.bullets = this.bullets.filter((b) => b.life > 0);
      const p = this.p;
      for (const b of this.ebullets) {
        b.x += b.vx * dt; b.y += b.vy * dt; b.life -= dt;
        if (this.tileAt(Math.floor(b.x / TILE), Math.floor(b.y / TILE)) === '#') { b.life = 0; this.burst(b.x, b.y, b.color, 3, 80, 0); continue; }
        if (!p.dead && b.x > p.x && b.x < p.x + p.w && b.y > p.y && b.y < p.y + p.h) { this.hurtPlayer(b.dmg, b.x - b.vx); b.life = 0; }
      }
      this.ebullets = this.ebullets.filter((b) => b.life > 0);
    }

    updatePickups(dt) {
      const p = this.p;
      if (p.dead) return;
      for (const k of this.pickups) {
        k.ph += dt * 3;
        if (k.got) continue;
        if (overlap(p, k)) {
          if (k.t === 'chip') { this.chips++; Snd.play('chip'); this.burst(k.x + 8, k.y + 8, '#ffd36b', 8, 120, 0); this.popup(k.x, k.y - 6, '+chip', '#ffd36b'); k.got = true; }
          else if (k.t === 'ammo') { if (p.ammo >= this.powers.ammoMax) continue; p.ammo = Math.min(this.powers.ammoMax, p.ammo + 5); Snd.play('pickup'); this.popup(k.x, k.y - 6, '+munición', '#ffe27a'); k.got = true; }
          else if (k.t === 'hp') { if (p.hp >= this.powers.hpMax) continue; p.hp = Math.min(this.powers.hpMax, p.hp + 30); Snd.play('pickup'); this.popup(k.x, k.y - 6, '+vida', '#7dff9b'); k.got = true; }
        }
      }
      this.pickups = this.pickups.filter((k) => !k.got);
    }

    // ----------------------------------------------- triggers del mundo
    updateWorldTriggers(dt) {
      const p = this.p;
      if (p.dead) return;
      for (const c of this.checkpoints) {
        if (!c.on && overlap(p, c)) {
          c.on = true; this.checkpoint = { x: c.sx, y: c.sy };
          Snd.play('checkpoint'); this.toast('Checkpoint guardado', '#7df9ff');
          this.burst(c.x + 16, c.y + 10, '#7df9ff', 14, 160, 0);
        }
      }
      if (this.arena && !this.arena.active && !this.bossDead && p.x > (this.arena.x0 + 4) * TILE) this.activateArena();
      const portal = this.portal;
      if (portal && overlap(p, portal) && this.state === 'play') {
        if (portal.open) this.completeLevel();
        else if (this.time - (this.portalMsg || -9) > 2.5) {
          this.portalMsg = this.time;
          const missing = this.terminals.filter((t) => !t.bonus && !t.solved).length;
          this.toast(missing ? `El portal está bloqueado: faltan ${missing} terminal(es) por hackear` : 'El portal se abre al derrotar al jefe', '#ff9');
        }
      }
      // Cerca de terminal
      this.nearTerminal = null;
      for (const t of this.terminals) {
        if (Math.abs(t.x + 16 - (p.x + p.w / 2)) < 52 && Math.abs(t.y + 22 - (p.y + p.h / 2)) < 56) this.nearTerminal = t;
      }
    }

    tryInteract() {
      const t = this.nearTerminal;
      if (!t || this.state !== 'play') return;
      if (t.solved) { this.toast('Esta terminal ya está hackeada ✓', '#7dff9b'); return; }
      this.openChallenge(t);
    }

    effectiveSkill() {
      const base = this.settings.code;
      return clamp(base + (this.adapt >= 2 ? 1 : this.adapt <= -2 ? -1 : 0), 0, 2);
    }

    openChallenge(t) {
      this.state = 'challenge';
      this.ts = this.settings.assist ? 0 : 0.15;
      Snd.play('terminal');
      const done = (result) => {
        this.state = 'play'; this.ts = 1;
        this.keys.clear(); this.pressed.clear();
        if (result && result.solved) this.onSolved(t, result);
        else if (result) { this.hintsUsed += result.hints || 0; this.attempts += result.attempts || 0; }
      };
      this.hooks.openChallenge(t.ch, { skill: this.effectiveSkill(), world: this.world, done });
    }

    onSolved(t, r) {
      t.solved = true;
      this.runSolved.add(t.id);
      const newly = !this.solved.has(t.id);
      this.solved.add(t.id);
      this.hintsUsed += r.hints || 0; this.attempts += r.attempts || 0;
      if (r.usedSolution) this.solutionsUsed++;
      t.usedSolution = !!r.usedSolution;
      // Adaptación de dificultad: pocas ayudas → sube; muchas → baja.
      if (r.usedSolution || r.attempts >= 4) this.adapt = clamp(this.adapt - 1, -3, 3);
      else if (r.attempts <= 1 && !r.hints) this.adapt = clamp(this.adapt + 1, -3, 3);
      if (this.hooks.saveProgress) this.hooks.saveProgress(t.id, r, this.adapt);
      const before = this.powers;
      this.powers = computePowers(this.solved);
      const p = this.p;
      p.hp = Math.min(this.powers.hpMax, Math.max(p.hp, this.powers.hpMax * 0.6) + (this.powers.hpMax - before.hpMax));
      p.ammo = this.powers.ammoMax;
      if (this.powers.energyMax > before.energyMax) p.energy = this.powers.energyMax;
      Snd.play('good');
      this.burst(t.x + 16, t.y + 12, '#7dff9b', 24, 220, 0);
      this.doShake(5);
      if (t.bonus) {
        this.chips++;
        this.popup(t.x, t.y - 12, '+chip secreto', '#ffd36b', true);
      } else if (POWER_TEXT[t.id]) {
        this.toast('✔ Instalado: ' + POWER_TEXT[t.id][0], '#7dff9b');
        this.toast(POWER_TEXT[t.id][1], '#cfe');
        if (t.id === 'w3b') {
          this.loopBroken = true;
          this.enemies.forEach((e) => { if (e.type === 'looper') { e.respawn = 0; } });
        }
      }
      if (t.boss && this.boss) {
        this.boss.shield = false;
        this.burst(this.boss.x + 26, this.boss.y + 27, '#6cf', 40, 320, 0);
        Snd.play('boom');
        this.toast('¡Escudo del jefe roto! ¡Dispárale!', '#ffd36b');
      }
      if (newly && !t.bonus) { /* poder nuevo */ }
      this.refreshPortal();
    }

    completeLevel() {
      if (this.state === 'complete') return;
      this.state = 'complete';
      Snd.play('win');
      const req = this.terminals.filter((t) => !t.bonus);
      const bonus = this.terminals.filter((t) => t.bonus);
      const noSolutions = req.every((t) => !t.usedSolution);
      const allChips = this.chips >= this.chipTotal && bonus.every((t) => t.solved);
      const stars = 1 + (noSolutions ? 1 : 0) + (allChips ? 1 : 0);
      this.report = {
        world: this.world.id, stars, chips: this.chips, chipTotal: this.chipTotal, deaths: this.deaths, kills: this.kills,
        time: this.runTime, hints: this.hintsUsed, solutions: this.solutionsUsed, noSolutions, allChips,
        installed: req.map((t) => ({ id: t.id, title: t.ch.title })), sample: this.world.sample,
      };
      if (this.hooks.onComplete) this.hooks.onComplete(this.report);
    }

    // ------------------------------------------------ efectos y cámara
    updateFx(dt) {
      for (const q of this.parts) { q.life -= dt; q.vy += q.grav * dt; q.x += q.vx * dt; q.y += q.vy * dt; }
      this.parts = this.parts.filter((q) => q.life > 0);
      if (this.parts.length > 400) this.parts.splice(0, this.parts.length - 400);
      for (const q of this.pops) { q.t -= dt; q.y -= 28 * dt; }
      this.pops = this.pops.filter((q) => q.t > 0);
      this.shake = Math.max(0, this.shake - dt * 30);
    }

    updateCamera(dt) {
      const p = this.p;
      let tx;
      if (this.arena && this.arena.active && !this.bossDead) {
        const mid = ((this.arena.x0 + this.arena.x1) * TILE) / 2;
        tx = mid - VW / 2;
      } else tx = p.x + p.w / 2 - VW / 2 + p.face * 40 + clamp(p.vx, -200, 200) * 0.15;
      tx = clamp(tx, 0, Math.max(0, this.levelW - VW));
      this.cam.x += (tx - this.cam.x) * Math.min(1, dt * 7 + (this.state === 'play' ? 0 : 0));
      if (Math.abs(tx - this.cam.x) < 0.3) this.cam.x = tx;
      this.cam.y = 0;
    }

    // Para pruebas automáticas y depuración.
    debugSolveAll() {
      for (const t of this.terminals) if (!t.solved) { t.usedSolution = false; this.onSolved(t, { solved: true, attempts: 1, hints: 0 }); }
    }
  }

  root.BBGame = { Game, TILE, VW, VH, computePowers, POWER_TEXT, ACT, clamp, rnd };
})(typeof self !== 'undefined' ? self : this);
