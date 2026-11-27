/* ============================================================================
 * BugBlaster — dibujo (todo procedural con Canvas 2D: sin imágenes externas).
 * Se añade a Game.prototype para mantener la lógica separada del arte.
 * ========================================================================== */
(function (root) {
  'use strict';
  const { Game, TILE, VW, VH, clamp } = root.BBGame;
  const MONO = '14px Consolas, "Courier New", monospace';

  function rr(c, x, y, w, h, r) {
    c.beginPath();
    c.moveTo(x + r, y); c.arcTo(x + w, y, x + w, y + h, r); c.arcTo(x + w, y + h, x, y + h, r);
    c.arcTo(x, y + h, x, y, r); c.arcTo(x, y, x + w, y, r); c.closePath();
  }
  const hash = (x, y) => (((x * 73856093) ^ (y * 19349663)) >>> 0) % 997;

  function wrap(c, text, maxW) {
    const words = text.split(' '), lines = [];
    let cur = '';
    for (const w of words) {
      const t = cur ? cur + ' ' + w : w;
      if (c.measureText(t).width > maxW && cur) { lines.push(cur); cur = w; } else cur = t;
    }
    if (cur) lines.push(cur);
    return lines;
  }

  Object.assign(Game.prototype, {
    render() {
      const c = this.ctx, cam = this.cam;
      c.save();
      c.clearRect(0, 0, VW, VH);
      this.drawBackground();
      const sx = this.shake ? (Math.random() - 0.5) * this.shake : 0;
      const sy = this.shake ? (Math.random() - 0.5) * this.shake : 0;
      c.translate(-Math.round(cam.x) + sx, sy);
      this.drawTiles();
      this.drawProps();
      this.drawPickups();
      for (const e of this.enemies) this.drawEnemy(e);
      this.drawBullets();
      if (!this.p.dead) this.drawPlayer();
      this.drawParticles();
      c.restore();
      this.drawHUD();
    },

    // ----------------------------------------------------------- fondo
    drawBackground() {
      const c = this.ctx, th = this.world.theme, cam = this.cam;
      const g = c.createLinearGradient(0, 0, 0, VH);
      g.addColorStop(0, th.sky1); g.addColorStop(1, th.sky2);
      c.fillStyle = g; c.fillRect(0, 0, VW, VH);
      // torres de servidores (parallax lento)
      const off = cam.x * 0.25;
      const first = Math.floor(off / 70);
      for (let i = -1; i < 16; i++) {
        const idx = first + i;
        const x = idx * 70 - off;
        const h = 110 + (((idx * 7919) % 11) + 11) % 11 * 17;
        c.fillStyle = th.far; c.fillRect(x, VH - h - 40, 56, h + 40);
        c.fillStyle = 'rgba(255,255,255,0.06)';
        for (let yy = VH - h - 30; yy < VH - 50; yy += 16) c.fillRect(x + 6, yy, 44, 4);
        c.fillStyle = th.accent;
        c.globalAlpha = 0.25 + 0.25 * Math.sin(this.time * 2 + idx);
        c.fillRect(x + 46, VH - h - 34, 4, 4);
        c.globalAlpha = 1;
      }
      // lluvia de bits
      c.font = '12px Consolas, monospace';
      c.fillStyle = th.accent;
      for (let i = 0; i < 46; i++) {
        const bx = (((i * 97 - cam.x * 0.5) % (VW + 60)) + VW + 60) % (VW + 60) - 30;
        const by = (i * 53 + this.time * (14 + (i % 4) * 9)) % (VH + 20);
        c.globalAlpha = 0.07 + (i % 5) * 0.025;
        c.fillText(((i * 31 + Math.floor(this.time * 1.5 + i)) % 2).toString(), bx, by);
      }
      c.globalAlpha = 1;
    },

    // ----------------------------------------------------------- mapa
    drawTiles() {
      const c = this.ctx, th = this.world.theme, cam = this.cam;
      const x0 = Math.max(0, Math.floor(cam.x / TILE)), x1 = Math.min(this.W - 1, Math.floor((cam.x + VW) / TILE) + 1);
      for (let ty = 0; ty < this.Hh; ty++) {
        for (let tx = x0; tx <= x1; tx++) {
          const ch = this.grid[ty][tx];
          if (ch === '.') continue;
          const x = tx * TILE, y = ty * TILE;
          if (ch === '#') {
            if (this.gate.has(ty * 10000 + tx)) {
              c.fillStyle = 'rgba(255,60,90,0.18)'; c.fillRect(x, y, TILE, TILE);
              c.fillStyle = `rgba(255,90,120,${0.5 + 0.3 * Math.sin(this.time * 8 + ty)})`; c.fillRect(x + 12, y, 8, TILE);
              continue;
            }
            const up = this.grid[ty - 1] ? this.grid[ty - 1][tx] : '.';
            const deep = ty > 14;
            c.fillStyle = deep ? th.tile : th.tile; c.fillRect(x, y, TILE, TILE);
            c.fillStyle = 'rgba(0,0,0,0.22)'; c.fillRect(x, y + TILE - 2, TILE, 2); c.fillRect(x + TILE - 2, y, 2, TILE);
            c.fillStyle = 'rgba(255,255,255,0.05)'; c.fillRect(x, y, TILE, 2);
            const h = hash(tx, ty);
            if (h % 5 === 0) { c.strokeStyle = 'rgba(255,255,255,0.10)'; c.beginPath(); c.moveTo(x + 6, y + 10); c.lineTo(x + 18, y + 10); c.lineTo(x + 18, y + 22); c.lineTo(x + 26, y + 22); c.stroke(); c.fillStyle = 'rgba(255,255,255,0.18)'; c.fillRect(x + 25, y + 21, 3, 3); }
            if (up !== '#') { c.fillStyle = th.tileTop; c.fillRect(x, y, TILE, 4); c.fillStyle = 'rgba(255,255,255,0.35)'; c.fillRect(x, y, TILE, 1); }
          } else if (ch === '=') {
            c.fillStyle = th.tileTop; c.globalAlpha = 0.9; c.fillRect(x, y, TILE, 6); c.globalAlpha = 1;
            c.fillStyle = 'rgba(0,0,0,0.35)'; c.fillRect(x, y + 6, TILE, 3);
            c.fillStyle = th.tile; c.fillRect(x + 4, y + 9, 4, 4); c.fillRect(x + TILE - 8, y + 9, 4, 4);
          } else if (ch === '^') {
            c.fillStyle = '#d7dde6';
            for (let i = 0; i < 3; i++) { c.beginPath(); c.moveTo(x + i * 11 - 1, y + TILE); c.lineTo(x + i * 11 + 5.5, y + 12); c.lineTo(x + i * 11 + 12, y + TILE); c.closePath(); c.fill(); }
            c.fillStyle = '#ff5d73'; for (let i = 0; i < 3; i++) c.fillRect(x + i * 11 + 4, y + 12, 3, 3);
          }
        }
      }
    },

    drawProps() {
      const c = this.ctx, th = this.world.theme, p = this.p;
      // carteles
      for (const s of this.signs) {
        c.fillStyle = '#6b4f2a'; c.fillRect(s.x + 14, s.y - 16, 4, 16);
        c.fillStyle = '#c9a15c'; rr(c, s.x + 3, s.y - 34, 26, 18, 3); c.fill();
        c.fillStyle = '#5a3d17'; c.font = 'bold 12px monospace'; c.fillText('i', s.x + 14, s.y - 21);
      }
      // checkpoints
      for (const k of this.checkpoints) {
        c.fillStyle = '#334'; c.fillRect(k.x + 14, k.y, 4, 48);
        c.fillStyle = k.on ? th.glow : '#667';
        c.beginPath(); c.moveTo(k.x + 18, k.y + 2); c.lineTo(k.x + 34, k.y + 9 + Math.sin(this.time * 4) * (k.on ? 2 : 0)); c.lineTo(k.x + 18, k.y + 18); c.fill();
        if (k.on) { c.globalAlpha = 0.25; c.fillRect(k.x + 8, k.y + 40, 16, 8); c.globalAlpha = 1; }
      }
      // terminales
      for (const t of this.terminals) this.drawTerminal(t, t === this.nearTerminal);
      // portal
      const po = this.portal;
      if (po) {
        const cx = po.x + po.w / 2, cy = po.y + po.h / 2;
        c.save(); c.translate(cx, cy);
        for (let i = 0; i < 4; i++) {
          c.strokeStyle = po.open ? `hsla(${180 + i * 25 + this.time * 60}, 100%, 70%, 0.8)` : 'rgba(255,90,110,0.5)';
          c.lineWidth = 3; c.beginPath();
          c.ellipse(0, 0, 20 - i * 3, 38 - i * 5, this.time * (po.open ? 1.5 : 0.2) * (i % 2 ? 1 : -1), 0, Math.PI * 2); c.stroke();
        }
        if (po.open) { c.globalAlpha = 0.18 + 0.1 * Math.sin(this.time * 5); c.fillStyle = '#9ff'; c.beginPath(); c.ellipse(0, 0, 20, 38, 0, 0, Math.PI * 2); c.fill(); c.globalAlpha = 1; }
        else { c.fillStyle = 'rgba(255,90,110,0.9)'; c.font = 'bold 18px monospace'; c.textAlign = 'center'; c.fillText('🔒', 0, 6); c.textAlign = 'left'; }
        c.restore();
      }
      // globos de los carteles
      c.font = '13px "Segoe UI", sans-serif';
      for (const s of this.signs) {
        if (Math.abs(p.x + 10 - (s.x + 16)) < 90) {
          const lines = wrap(c, s.text, 250);
          const w = Math.max(...lines.map((l) => c.measureText(l).width)) + 20, h = lines.length * 17 + 14;
          const bx = s.x + 16 - w / 2, by = s.y - 44 - h;
          c.fillStyle = 'rgba(8,14,26,0.92)'; rr(c, bx, by, w, h, 8); c.fill();
          c.strokeStyle = th.accent; c.lineWidth = 1.5; c.stroke();
          c.fillStyle = '#e8f4ff'; lines.forEach((l, i) => c.fillText(l, bx + 10, by + 19 + i * 17));
        }
      }
    },

    drawTerminal(t, near) {
      const c = this.ctx, x = t.x, y = t.y;
      const accent = t.bonus ? '#ffd36b' : this.world.theme.accent;
      c.fillStyle = '#1b2433'; rr(c, x + 2, y + 6, 28, 34, 4); c.fill();
      c.fillStyle = '#2b374d'; c.fillRect(x + 8, y + 38, 16, 6);
      c.fillStyle = t.solved ? '#06210f' : '#04111a'; rr(c, x + 5, y + 9, 22, 20, 3); c.fill();
      c.strokeStyle = t.solved ? '#7dff9b' : accent; c.lineWidth = 2; rr(c, x + 2, y + 6, 28, 34, 4); c.stroke();
      c.font = 'bold 14px Consolas, monospace';
      if (t.solved) { c.fillStyle = '#7dff9b'; c.fillText('✓', x + 10, y + 25); }
      else {
        c.fillStyle = accent;
        c.fillText(Math.floor(this.time * 2) % 2 ? '>_' : '> ', x + 8, y + 24);
        c.globalAlpha = 0.18 + 0.12 * Math.sin(this.time * 4); c.fillRect(x - 4, y + 2, 40, 42); c.globalAlpha = 1;
      }
      if (t.boss && !t.solved) { c.fillStyle = '#ff5d73'; c.fillText('!', x + 14, y + 2); }
      if (near) {
        c.font = 'bold 13px "Segoe UI", sans-serif';
        const txt = t.solved ? 'Terminal hackeada ✓' : '[E] Hackear terminal';
        const w = c.measureText(txt).width + 18;
        c.fillStyle = 'rgba(8,14,26,0.92)'; rr(c, x + 16 - w / 2, y - 30, w, 22, 6); c.fill();
        c.strokeStyle = accent; c.lineWidth = 1.2; c.stroke();
        c.fillStyle = '#fff'; c.fillText(txt, x + 16 - w / 2 + 9, y - 14);
      }
    },

    drawPickups() {
      const c = this.ctx;
      for (const k of this.pickups) {
        const bob = Math.sin(k.ph) * 3;
        if (k.t === 'chip') {
          const x = k.x, y = k.y + bob;
          c.fillStyle = '#d4a017'; c.fillRect(x - 3, y + 2, 3, 3); c.fillRect(x + 16, y + 2, 3, 3); c.fillRect(x - 3, y + 10, 3, 3); c.fillRect(x + 16, y + 10, 3, 3);
          c.fillStyle = '#ffd36b'; rr(c, x, y, 16, 16, 3); c.fill();
          c.fillStyle = '#7a5a00'; c.fillRect(x + 4, y + 4, 8, 8);
          c.globalAlpha = 0.3 + 0.2 * Math.sin(k.ph * 2); c.fillStyle = '#fff6a8'; c.fillRect(x - 5, y - 5, 26, 26); c.globalAlpha = 1;
        } else if (k.t === 'ammo') {
          c.fillStyle = '#e0b341'; rr(c, k.x, k.y + bob, 20, 16, 3); c.fill();
          c.fillStyle = '#6b4e00'; for (let i = 0; i < 3; i++) c.fillRect(k.x + 4 + i * 5, k.y + 3 + bob, 3, 10);
        } else {
          c.fillStyle = '#f4f7fb'; rr(c, k.x, k.y + bob, 20, 18, 3); c.fill();
          c.fillStyle = '#e63946'; c.fillRect(k.x + 8, k.y + 3 + bob, 4, 12); c.fillRect(k.x + 4, k.y + 7 + bob, 12, 4);
        }
      }
    },

    // -------------------------------------------------------- jugador
    drawPlayer() {
      const c = this.ctx, p = this.p;
      if (p.inv > 0 && Math.floor(this.time * 20) % 2 === 0 && p.dashT <= 0) return;
      const cx = p.x + p.w / 2, by = p.y + p.h;
      const run = p.onGround && Math.abs(p.vx) > 25;
      const ph = Math.sin(p.anim * 16) * (run ? 4 : 0);
      c.save(); c.translate(cx, by); c.scale(p.face, 1);
      if (p.dashT > 0) { c.globalAlpha = 0.35; c.fillStyle = '#9ff'; for (let i = 1; i <= 3; i++) c.fillRect(-10 - i * 12, -26, 18, 22); c.globalAlpha = 1; }
      // piernas
      c.fillStyle = '#243a55';
      c.fillRect(-7 + ph, -8, 5, 8); c.fillRect(2 - ph, -8, 5, 8);
      c.fillStyle = '#ffd36b'; c.fillRect(-8 + ph, -2, 7, 2); c.fillRect(1 - ph, -2, 7, 2);
      // torso
      c.fillStyle = '#5ad1ff'; rr(c, -9, -21, 18, 15, 4); c.fill();
      c.fillStyle = '#2f8fb8'; c.fillRect(-9, -9, 18, 3);
      c.fillStyle = '#ffd36b'; c.fillRect(-3, -17, 6, 4);
      // cabeza
      c.fillStyle = '#8ae2ff'; rr(c, -8, -30, 16, 11, 4); c.fill();
      c.fillStyle = '#0b1d33'; rr(c, -6, -28, 12, 6, 2); c.fill();
      c.fillStyle = this.shieldActive() ? '#ff9' : '#7dffb0'; c.fillRect(0, -27, 3, 3); c.fillRect(-5, -27, 3, 3);
      c.strokeStyle = '#8ae2ff'; c.lineWidth = 2; c.beginPath(); c.moveTo(0, -30); c.lineTo(0, -34); c.stroke();
      c.fillStyle = Math.floor(this.time * 3) % 2 ? '#ff5d73' : '#7a2634'; c.fillRect(-2, -37, 4, 4);
      c.restore();
      // arma
      c.save(); c.translate(cx, by - 14); c.rotate(p.aim);
      if (Math.cos(p.aim) < 0) c.scale(1, -1);
      const w = this.currentWeapon();
      c.fillStyle = '#394b66'; c.fillRect(2, -3, w === 'escopeta' ? 18 : w === 'rifle' ? 20 : 14, 6);
      c.fillStyle = w === 'escopeta' ? '#ff9a3c' : w === 'rifle' ? '#7dffb0' : '#cfe3ff'; c.fillRect(w === 'escopeta' ? 16 : 12, -2, 7, 4);
      c.restore();
      if (this.shieldActive()) {
        c.strokeStyle = `rgba(255,236,120,${0.5 + 0.3 * Math.sin(this.time * 10)})`; c.lineWidth = 3;
        c.beginPath(); c.arc(cx, by - 16, 24, 0, Math.PI * 2); c.stroke();
      }
    },

    // -------------------------------------------------------- enemigos
    drawEnemy(e) {
      const c = this.ctx;
      if (e.dead) {
        if (e.type === 'looper' && e.respawn > 0) {
          c.globalAlpha = 0.5; c.fillStyle = '#9dff6b'; c.font = 'bold 20px monospace';
          c.fillText('↻', e.home.x + 6, e.home.y + 16);
          c.font = '11px monospace'; c.fillText(e.respawn.toFixed(1), e.home.x + 4, e.home.y + 30); c.globalAlpha = 1;
        }
        return;
      }
      const cx = e.x + e.w / 2, cy = e.y + e.h / 2;
      const fl = e.flash > 0;
      if (e.type === 'walker') {
        c.save(); c.translate(cx, e.y + e.h); c.scale(e.dir, 1);
        const lp = Math.sin(e.t * 12) * 3;
        c.strokeStyle = '#3a1520'; c.lineWidth = 2;
        for (let i = -1; i <= 1; i++) { c.beginPath(); c.moveTo(i * 7, -6); c.lineTo(i * 7 + lp * (i % 2 ? 1 : -1), 0); c.stroke(); }
        c.fillStyle = fl ? '#fff' : '#d1495b'; c.beginPath(); c.ellipse(0, -11, 13, 10, 0, Math.PI, 0); c.lineTo(13, -6); c.lineTo(-13, -6); c.fill();
        c.fillStyle = 'rgba(0,0,0,0.25)'; c.fillRect(-1, -21, 2, 14); c.fillRect(-8, -18, 2, 10); c.fillRect(6, -18, 2, 10);
        c.fillStyle = '#fff'; c.fillRect(6, -13, 5, 5); c.fillStyle = '#111'; c.fillRect(9, -12, 2, 3);
        c.strokeStyle = '#3a1520'; c.beginPath(); c.moveTo(9, -20); c.lineTo(13, -26); c.stroke();
        c.restore();
        c.fillStyle = 'rgba(255,255,255,0.6)'; c.font = 'bold 9px monospace'; c.fillText('bug', cx - 9, e.y - 2);
      } else if (e.type === 'looper') {
        c.save(); c.translate(e.x, e.y + e.h);
        for (let i = 4; i >= 0; i--) {
          const sx = e.dir > 0 ? i * 6 : e.w - 8 - i * 6, sy = -8 + Math.sin(e.t * 8 + i) * 3 - (i === 0 ? 2 : 0);
          c.fillStyle = fl ? '#fff' : (i === 0 ? '#58c93a' : '#3fa028'); c.beginPath(); c.arc(sx + 4, sy, i === 0 ? 7 : 5.5, 0, 7); c.fill();
        }
        const hx = e.dir > 0 ? 4 : e.w - 8;
        c.fillStyle = '#fff'; c.fillRect(hx + 2 + e.dir * 3, -14, 4, 4); c.fillStyle = '#111'; c.fillRect(hx + 4 + e.dir * 3, -13, 2, 2);
        c.restore();
        c.fillStyle = '#9dff6b'; c.font = 'bold 11px monospace'; c.fillText('∞', cx - 4, e.y - 3);
      } else if (e.type === 'flyer') {
        const wob = Math.sin(e.t * 3) * 2;
        c.globalAlpha = fl ? 1 : 0.88;
        c.fillStyle = fl ? '#fff' : '#d8ecff';
        c.beginPath(); c.arc(cx, cy - 2 + wob, 12, Math.PI, 0);
        c.lineTo(cx + 12, cy + 12 + wob);
        for (let i = 0; i < 4; i++) c.lineTo(cx + 12 - (i + 1) * 6, cy + (i % 2 ? 12 : 6) + wob);
        c.closePath(); c.fill(); c.globalAlpha = 1;
        c.fillStyle = '#16233a'; c.fillRect(cx - 7 + e.dir * 2, cy - 6 + wob, 4, 6); c.fillRect(cx + 2 + e.dir * 2, cy - 6 + wob, 4, 6);
        c.fillStyle = 'rgba(30,50,80,0.8)'; c.font = 'bold 9px monospace'; c.fillText('null', cx - 9, cy + 6 + wob);
      } else if (e.type === 'shooter') {
        c.fillStyle = '#3a4252'; c.beginPath(); c.moveTo(e.x, e.y + e.h); c.lineTo(e.x + 4, e.y + 14); c.lineTo(e.x + e.w - 4, e.y + 14); c.lineTo(e.x + e.w, e.y + e.h); c.fill();
        c.fillStyle = fl ? '#fff' : '#566178'; c.beginPath(); c.arc(cx, e.y + 14, 12, Math.PI, 0); c.fill();
        const a = Math.atan2(this.p.y + 14 - (e.y + 8), this.p.x + 10 - cx);
        c.save(); c.translate(cx, e.y + 10); c.rotate(a); c.fillStyle = '#2a303d'; c.fillRect(0, -3, 16, 6); c.restore();
        const hot = e.tele > 0;
        c.fillStyle = hot ? '#ffe44d' : '#ff3b52'; c.beginPath(); c.arc(cx, e.y + 10, 4.5, 0, 7); c.fill();
        if (hot) { c.globalAlpha = 0.4; c.beginPath(); c.arc(cx, e.y + 10, 10, 0, 7); c.fill(); c.globalAlpha = 1; }
        c.fillStyle = '#ff7a8a'; c.font = 'bold 9px monospace'; c.fillText('ERR', cx - 10, e.y + 27);
      } else if (e.type === 'boss') this.drawBoss(e);
      if (e.hp < e.maxhp && e.type !== 'boss') {
        c.fillStyle = 'rgba(0,0,0,0.5)'; c.fillRect(e.x, e.y - 9, e.w, 4);
        c.fillStyle = '#ff5d73'; c.fillRect(e.x, e.y - 9, e.w * clamp(e.hp / e.maxhp, 0, 1), 4);
      }
    },

    drawBoss(b) {
      const c = this.ctx, cx = b.x + b.w / 2, cy = b.y + b.h / 2;
      if (b.dead) return;
      const fl = b.flash > 0;
      const pulse = b.tele > 0 ? Math.floor(this.time * 20) % 2 : 0;
      c.save(); c.translate(cx, cy);
      if (b.hop && !b.onGround) c.scale(0.95, 1.08);
      // cuerpo
      c.fillStyle = fl ? '#fff' : (pulse ? '#fff' : b.color);
      rr(c, -26, -27, 52, 54, 12); c.fill();
      c.fillStyle = 'rgba(0,0,0,0.28)'; rr(c, -20, -4, 40, 26, 6); c.fill();
      // pinchos
      c.fillStyle = fl ? '#fff' : b.color;
      for (let i = -2; i <= 2; i++) { c.beginPath(); c.moveTo(i * 10 - 5, -26); c.lineTo(i * 10, -37 + Math.abs(i) * 3); c.lineTo(i * 10 + 5, -26); c.fill(); }
      // ojos
      const look = b.dir;
      c.fillStyle = '#fff'; c.fillRect(-16, -18, 12, 9); c.fillRect(4, -18, 12, 9);
      c.fillStyle = '#111'; c.fillRect(-12 + look * 2, -17, 5, 7); c.fillRect(8 + look * 2, -17, 5, 7);
      c.strokeStyle = '#111'; c.lineWidth = 3; c.beginPath(); c.moveTo(-18, -22); c.lineTo(-4, -16); c.moveTo(18, -22); c.lineTo(4, -16); c.stroke();
      // pecho: símbolo del concepto
      c.fillStyle = b.color; c.font = 'bold 15px Consolas, monospace'; c.textAlign = 'center';
      c.fillText({ var: 'x = ?', cond: 'if ?', loop: '∞', func: 'f()' }[b.style] || '?', 0, 14);
      c.textAlign = 'left';
      c.restore();
      // escudo
      if (b.shield) {
        const a = b.active ? 0.45 : 0.25;
        c.strokeStyle = `rgba(110,200,255,${a + 0.25 * Math.sin(this.time * 6)})`; c.lineWidth = 4;
        c.beginPath(); c.arc(cx, cy, 46, 0, Math.PI * 2); c.stroke();
        c.fillStyle = `rgba(110,200,255,${a * 0.35})`; c.fill();
        c.fillStyle = 'rgba(190,235,255,0.6)'; c.font = 'bold 11px monospace'; c.textAlign = 'center'; c.fillText('ESCUDO', cx, cy - 52); c.textAlign = 'left';
      }
      if (b.tele > 0) { c.fillStyle = 'rgba(255,255,255,0.8)'; c.font = 'bold 20px monospace'; c.fillText('!', cx - 4, b.y - 42); }
    },

    drawBullets() {
      const c = this.ctx;
      for (const b of this.bullets) {
        c.fillStyle = b.crit ? '#ffd36b' : b.kind === 'burst' ? '#9ff6ff' : b.kind === 'rifle' ? '#7dffb0' : '#fff7a8';
        const a = Math.atan2(b.vy, b.vx);
        c.save(); c.translate(b.x, b.y); c.rotate(a); c.fillRect(-7, -2, 14, 4); c.globalAlpha = 0.35; c.fillRect(-18, -1, 12, 2); c.restore(); c.globalAlpha = 1;
      }
      for (const b of this.ebullets) {
        c.fillStyle = b.color; c.beginPath(); c.arc(b.x, b.y, b.r, 0, 7); c.fill();
        c.globalAlpha = 0.3; c.beginPath(); c.arc(b.x, b.y, b.r + 4, 0, 7); c.fill(); c.globalAlpha = 1;
      }
    },

    drawParticles() {
      const c = this.ctx;
      for (const q of this.parts) {
        c.globalAlpha = clamp(q.life / q.max, 0, 1); c.fillStyle = q.color;
        c.fillRect(q.x - q.size / 2, q.y - q.size / 2, q.size, q.size);
      }
      c.globalAlpha = 1;
      for (const q of this.pops) {
        c.globalAlpha = clamp(q.t / 0.5, 0, 1);
        c.font = q.big ? 'bold 17px "Segoe UI", sans-serif' : 'bold 13px "Segoe UI", sans-serif';
        c.fillStyle = '#000'; c.fillText(q.text, q.x + 1, q.y + 1);
        c.fillStyle = q.color; c.fillText(q.text, q.x, q.y);
      }
      c.globalAlpha = 1;
    },

    // ------------------------------------------------------------- HUD
    drawHUD() {
      const c = this.ctx, p = this.p, pw = this.powers, th = this.world.theme;
      c.save();
      // Panel de "código" con el estado del robot
      const lines = [];
      lines.push(['vida', String(Math.ceil(Math.max(0, p.hp))), '#7dff9b', p.hp / pw.hpMax, '#e63946']);
      lines.push(['municion', String(Math.floor(p.ammo)), '#ffd36b', p.ammo / pw.ammoMax, '#e0b341']);
      if (pw.energyMax > 0) lines.push(['energia', String(Math.floor(p.energy)), '#6cd0ff', p.energy / pw.energyMax, '#39a6ff']);
      lines.push(['arma', '"' + this.currentWeapon() + '"', '#ff9ad5']);
      if (pw.shotgun) {
        const d = this.nearestEnemyDist() / TILE;
        lines.push(['distancia', isFinite(d) ? Math.min(d, 9.9).toFixed(1) : 'None', '#c7b8ff']);
      }
      const h = lines.length * 22 + 14;
      c.fillStyle = 'rgba(5,12,24,0.74)'; rr(c, 10, 10, 232, h, 8); c.fill();
      c.strokeStyle = th.accent; c.globalAlpha = 0.6; c.lineWidth = 1.5; c.stroke(); c.globalAlpha = 1;
      c.font = MONO;
      lines.forEach((l, i) => {
        const y = 32 + i * 22;
        c.fillStyle = '#9fb7d4'; c.fillText(l[0], 20, y);
        c.fillStyle = '#e8f1ff'; c.fillText('=', 20 + c.measureText(l[0]).width + 6, y);
        c.fillStyle = l[2]; c.fillText(l[1], 20 + c.measureText(l[0] + ' = ').width + 6, y);
        if (l[3] !== undefined) {
          c.fillStyle = 'rgba(255,255,255,0.12)'; c.fillRect(150, y - 9, 80, 7);
          c.fillStyle = l[4]; c.fillRect(150, y - 9, 80 * clamp(l[3], 0, 1), 7);
        }
      });
      // Panel derecho
      const req = this.terminals.filter((t) => !t.bonus);
      const rows = [`Mundo ${this.world.id} · ${this.world.name}`, `terminales: ${req.filter((t) => t.solved).length}/${req.length}`, `chips: ${this.chips}/${this.chipTotal}`];
      c.font = 'bold 13px "Segoe UI", sans-serif';
      const rw = Math.max(...rows.map((r) => c.measureText(r).width)) + 24;
      c.fillStyle = 'rgba(5,12,24,0.74)'; rr(c, VW - rw - 10, 10, rw, 72, 8); c.fill();
      c.strokeStyle = th.accent; c.globalAlpha = 0.6; c.stroke(); c.globalAlpha = 1;
      rows.forEach((r, i) => { c.fillStyle = i === 0 ? th.accent : '#dfeaff'; c.fillText(r, VW - rw + 2, 32 + i * 21); });
      // Habilidades (teclas)
      const keys = [];
      if (pw.burst) keys.push(['F', 'ráfaga', p.energy >= 25]);
      if (pw.dash) keys.push(['Shift', 'dash', p.energy >= 20]);
      if (pw.doubleJump) keys.push(['2×↑', 'doble salto', true]);
      keys.forEach((k, i) => {
        c.font = 'bold 12px "Segoe UI", sans-serif';
        c.fillStyle = k[2] ? 'rgba(125,255,155,0.9)' : 'rgba(255,255,255,0.35)';
        c.fillText(`[${k[0]}] ${k[1]}`, 14 + i * 110, VH - 14);
      });
      // Barra del jefe
      const b = this.boss;
      if (b && b.active && !b.dead) {
        const w = 380, x = VW / 2 - w / 2;
        c.fillStyle = 'rgba(0,0,0,0.6)'; rr(c, x - 4, 14, w + 8, 26, 6); c.fill();
        c.fillStyle = '#3a1020'; c.fillRect(x, 20, w, 14);
        c.fillStyle = b.shield ? '#6cc8ff' : b.color; c.fillRect(x, 20, w * clamp(b.hp / b.maxhp, 0, 1), 14);
        c.fillStyle = '#fff'; c.font = 'bold 12px "Segoe UI", sans-serif'; c.textAlign = 'center';
        c.fillText(b.name + (b.shield ? '  🛡 escudo activo: hackea la terminal' : ''), VW / 2, 56); c.textAlign = 'left';
      }
      // Avisos
      c.textAlign = 'center';
      this.toasts.forEach((t, i) => {
        c.globalAlpha = clamp(t.t / 0.6, 0, 1);
        c.font = 'bold 15px "Segoe UI", sans-serif';
        const w = c.measureText(t.text).width + 26, y = 92 + i * 32;
        c.fillStyle = 'rgba(5,12,24,0.85)'; rr(c, VW / 2 - w / 2, y - 18, w, 27, 7); c.fill();
        c.fillStyle = t.color; c.fillText(t.text, VW / 2, y);
      });
      c.globalAlpha = 1;
      // Banner de inicio / jefe
      if (this.banner && this.banner.t > 0) {
        const a = clamp(this.banner.t / 0.6, 0, 1) * clamp((3.2 - this.banner.t) / 0.4 + 0.2, 0, 1);
        c.globalAlpha = Math.min(1, a);
        c.fillStyle = 'rgba(0,0,0,0.55)'; c.fillRect(0, VH / 2 - 70, VW, 120);
        c.font = 'bold 40px "Segoe UI", sans-serif'; c.fillStyle = th.accent; c.fillText(this.banner.text, VW / 2, VH / 2 - 14);
        c.font = '18px "Segoe UI", sans-serif'; c.fillStyle = '#fff'; c.fillText(this.banner.sub, VW / 2, VH / 2 + 24);
        c.globalAlpha = 1;
      }
      c.textAlign = 'left';
      // Mira
      if (this.aimMode === 'mouse' && this.state === 'play' && !p.dead) {
        const mx = this.mouse.x, my = this.mouse.y;
        c.strokeStyle = 'rgba(255,255,255,0.85)'; c.lineWidth = 1.5;
        c.beginPath(); c.arc(mx, my, 8, 0, 7); c.moveTo(mx - 13, my); c.lineTo(mx - 4, my); c.moveTo(mx + 4, my); c.lineTo(mx + 13, my); c.moveTo(mx, my - 13); c.lineTo(mx, my - 4); c.moveTo(mx, my + 4); c.lineTo(mx, my + 13); c.stroke();
      }
      if (p.dead) {
        c.fillStyle = 'rgba(80,0,10,0.35)'; c.fillRect(0, 0, VW, VH);
        c.textAlign = 'center'; c.font = 'bold 34px "Segoe UI", sans-serif'; c.fillStyle = '#fff'; c.fillText('💥 ¡Bug aplastado!', VW / 2, VH / 2); c.textAlign = 'left';
      }
      // Daño reciente
      if (p.inv > 0.7) { c.fillStyle = `rgba(255,0,0,${(p.inv - 0.7) * 0.5})`; c.fillRect(0, 0, VW, VH); }
      c.restore();
    },
  });
})(typeof self !== 'undefined' ? self : this);
