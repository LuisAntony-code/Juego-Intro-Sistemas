/* Efectos de sonido sintetizados con WebAudio (sin archivos externos). */
(function (root) {
  'use strict';
  let ctx = null;
  let master = null;
  const S = { volume: 0.6, muted: false };

  function ensure() {
    if (ctx) return ctx;
    try {
      const AC = root.AudioContext || root.webkitAudioContext;
      if (!AC) return null;
      ctx = new AC();
      master = ctx.createGain();
      master.gain.value = S.volume;
      master.connect(ctx.destination);
    } catch (e) { ctx = null; }
    return ctx;
  }

  function tone(freq, dur, type, vol, slideTo, delay) {
    if (S.muted || !ensure()) return;
    try {
      const t0 = ctx.currentTime + (delay || 0);
      const o = ctx.createOscillator();
      const g = ctx.createGain();
      o.type = type || 'square';
      o.frequency.setValueAtTime(freq, t0);
      if (slideTo) o.frequency.exponentialRampToValueAtTime(Math.max(20, slideTo), t0 + dur);
      g.gain.setValueAtTime(0.0001, t0);
      g.gain.exponentialRampToValueAtTime(vol || 0.15, t0 + 0.01);
      g.gain.exponentialRampToValueAtTime(0.0001, t0 + dur);
      o.connect(g); g.connect(master);
      o.start(t0); o.stop(t0 + dur + 0.02);
    } catch (e) { /* sin audio */ }
  }

  function noise(dur, vol, delay) {
    if (S.muted || !ensure()) return;
    try {
      const t0 = ctx.currentTime + (delay || 0);
      const n = Math.floor(ctx.sampleRate * dur);
      const buf = ctx.createBuffer(1, n, ctx.sampleRate);
      const d = buf.getChannelData(0);
      for (let i = 0; i < n; i++) d[i] = (Math.random() * 2 - 1) * (1 - i / n);
      const src = ctx.createBufferSource();
      const g = ctx.createGain();
      src.buffer = buf;
      g.gain.value = vol || 0.2;
      src.connect(g); g.connect(master);
      src.start(t0);
    } catch (e) { /* sin audio */ }
  }

  const SFX = {
    shoot: () => tone(880, 0.09, 'square', 0.07, 300),
    rifle: () => tone(1100, 0.07, 'square', 0.07, 420),
    shotgun: () => { noise(0.12, 0.18); tone(200, 0.15, 'sawtooth', 0.1, 70); },
    burst: () => tone(1000, 0.06, 'square', 0.06, 500),
    empty: () => tone(180, 0.06, 'square', 0.05),
    jump: () => tone(300, 0.14, 'square', 0.07, 620),
    djump: () => tone(480, 0.14, 'triangle', 0.09, 900),
    dash: () => { noise(0.1, 0.12); tone(240, 0.12, 'sawtooth', 0.06, 700); },
    hit: () => tone(260, 0.07, 'square', 0.08, 140),
    kill: () => { noise(0.18, 0.2); tone(420, 0.16, 'square', 0.1, 90); },
    hurt: () => { tone(200, 0.25, 'sawtooth', 0.15, 60); noise(0.1, 0.12); },
    pickup: () => { tone(660, 0.07, 'square', 0.08); tone(990, 0.1, 'square', 0.08, null, 0.06); },
    chip: () => { tone(880, 0.06, 'triangle', 0.09); tone(1320, 0.1, 'triangle', 0.09, null, 0.05); },
    terminal: () => { tone(520, 0.08, 'square', 0.07); tone(780, 0.08, 'square', 0.07, null, 0.08); },
    good: () => { tone(523, 0.1, 'square', 0.09); tone(659, 0.1, 'square', 0.09, null, 0.1); tone(784, 0.18, 'square', 0.09, null, 0.2); },
    bad: () => { tone(220, 0.16, 'sawtooth', 0.1, 150); },
    win: () => { [523, 659, 784, 1047].forEach((f, i) => tone(f, 0.22, 'square', 0.1, null, i * 0.13)); },
    checkpoint: () => { tone(600, 0.1, 'triangle', 0.1); tone(900, 0.16, 'triangle', 0.1, null, 0.09); },
    shield: () => tone(150, 0.12, 'triangle', 0.1, 120),
    boom: () => { noise(0.5, 0.3); tone(120, 0.5, 'sawtooth', 0.15, 30); },
    click: () => tone(700, 0.04, 'square', 0.05),
    die: () => { tone(400, 0.4, 'sawtooth', 0.12, 50); noise(0.3, 0.15); },
  };

  root.Sound = {
    play(name) { try { if (SFX[name]) SFX[name](); } catch (e) { /* sin audio */ } },
    resume() { if (ensure() && ctx.state === 'suspended') ctx.resume().catch(() => {}); },
    setVolume(v) { S.volume = v; if (master) master.gain.value = v; },
    setMuted(m) { S.muted = !!m; },
    get muted() { return S.muted; },
  };
})(typeof self !== 'undefined' ? self : this);
