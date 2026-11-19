/* Candy Balls — browser edition. Pure canvas + Web Audio, no assets. */
(function () {
'use strict';

const COLS = 8, ROWS = 8, COLORS = 6;
const MOVES = 30, TARGET = 2000, POINTS = 20;
const PALETTE = [
  { base: '#f54358', light: '#ff96a4', dark: '#a31230' },
  { base: '#ff9e29', light: '#ffd08a', dark: '#b85c05' },
  { base: '#ffdb47', light: '#fff3a8', dark: '#bd8c05' },
  { base: '#5bd35f', light: '#b4f0a9', dark: '#1f8a30' },
  { base: '#40a9f5', light: '#a2d9ff', dark: '#1259a8' },
  { base: '#b46cf2', light: '#dfb6ff', dark: '#6b27ae' }
];

const canvas = document.getElementById('board');
const ctx = canvas.getContext('2d');
const el = id => document.getElementById(id);
const scoreEl = el('score'), movesEl = el('moves'), bestEl = el('best');
const targetEl = el('target'), progressEl = el('progressFill');
const overlay = el('overlay'), ovIcon = el('ovIcon'), ovTitle = el('ovTitle'), ovText = el('ovText');

let board = [], score = 0, moves = MOVES, best = 0;
let busy = false, ended = false, selected = null, gen = 0;
let particles = [], floaters = [], hint = null, lastInput = 0;
let cell = 56, pad = 12, cw = 560, ch = 560;
let muted = false, audio = null;
let dragStart = null, dragPos = null;

try { best = parseInt(localStorage.getItem('candyballs.best') || '0', 10) || 0; } catch (e) {}

const clamp = (v, a, b) => v < a ? a : v > b ? b : v;
const lerp = (a, b, t) => a + (b - a) * t;
const ease = t => 1 - Math.pow(1 - clamp(t, 0, 1), 3);
const rnd = n => Math.floor(Math.random() * n);
const wait = ms => new Promise(r => setTimeout(r, ms));
const valid = (c, r) => c >= 0 && c < COLS && r >= 0 && r < ROWS;
const at = (c, r) => valid(c, r) ? board[r][c] : null;

function makeBall(c, r, color) {
  return { c, r, color, x: c, y: r - ROWS - 1, scale: 0, alpha: 0,
           dying: false, die: 0, pop: 0 };
}

function fillBoard() {
  board = [];
  for (let r = 0; r < ROWS; r++) {
    const row = [];
    for (let c = 0; c < COLS; c++) {
      let color, guard = 0;
      do {
        color = rnd(COLORS);
        guard++;
      } while (guard < 40 && (
        (c >= 2 && row[c - 1] && row[c - 2] && row[c - 1].color === color && row[c - 2].color === color) ||
        (r >= 2 && board[r - 1][c] && board[r - 2][c] &&
          board[r - 1][c].color === color && board[r - 2][c].color === color)
      ));
      const b = makeBall(c, r, color);
      b.y = r; b.x = c; b.scale = 1; b.alpha = 1;
      row.push(b);
    }
    board.push(row);
  }
}

// ---------------------------------------------------------------- rendering

function resize() {
  const rect = canvas.getBoundingClientRect();
  const dpr = Math.min(window.devicePixelRatio || 1, 2);
  cw = Math.max(240, rect.width); ch = Math.max(240, rect.height);
  canvas.width = Math.round(cw * dpr);
  canvas.height = Math.round(ch * dpr);
  ctx.setTransform(dpr, 0, 0, dpr, 0, 0);
  cell = (Math.min(cw, ch) - pad * 2) / COLS;
}

function px(c) { return pad + (c + 0.5) * cell; }
function py(r) { return pad + (r + 0.5) * cell; }

function roundRect(x, y, w, h, r) {
  r = Math.min(r, w / 2, h / 2);
  ctx.beginPath();
  ctx.moveTo(x + r, y);
  ctx.arcTo(x + w, y, x + w, y + h, r);
  ctx.arcTo(x + w, y + h, x, y + h, r);
  ctx.arcTo(x, y + h, x, y, r);
  ctx.arcTo(x, y, x + w, y, r);
  ctx.closePath();
}

function drawBall(b, t) {
  const x = px(b.x), y = py(b.y);
  const dying = b.dying ? clamp(1 - b.die / .18, 0, 1) : 1;
  const rad = cell * 0.40 * b.scale * dying *
              (b.pop ? 1 + 0.22 * Math.sin(b.pop * Math.PI) : 1);
  if (rad < 1 || b.alpha <= 0 || dying <= 0) return;
  const p = PALETTE[b.color % PALETTE.length];

  ctx.save();
  ctx.globalAlpha = b.alpha * dying;
  ctx.translate(x, y);

  ctx.shadowColor = 'rgba(0,0,0,.45)';
  ctx.shadowBlur = cell * 0.16;
  ctx.shadowOffsetY = cell * 0.06;

  const g = ctx.createRadialGradient(-rad * .34, -rad * .40, rad * .08, 0, 0, rad);
  g.addColorStop(0, p.light);
  g.addColorStop(.5, p.base);
  g.addColorStop(1, p.dark);
  ctx.fillStyle = g;
  ctx.beginPath(); ctx.arc(0, 0, rad, 0, Math.PI * 2); ctx.fill();

  ctx.shadowColor = 'transparent';
  ctx.lineWidth = Math.max(1, rad * .07);
  ctx.strokeStyle = 'rgba(255,255,255,.35)';
  ctx.beginPath(); ctx.arc(0, 0, rad * .93, Math.PI * 1.05, Math.PI * 1.75); ctx.stroke();

  ctx.fillStyle = 'rgba(255,255,255,.75)';
  ctx.beginPath();
  ctx.ellipse(-rad * .30, -rad * .38, rad * .22, rad * .13, -0.6, 0, Math.PI * 2);
  ctx.fill();

  ctx.fillStyle = 'rgba(255,255,255,.22)';
  ctx.beginPath();
  ctx.ellipse(rad * .30, rad * .40, rad * .16, rad * .09, -0.6, 0, Math.PI * 2);
  ctx.fill();
  ctx.restore();
}

function draw(t, dt) {
  ctx.clearRect(0, 0, cw, ch);
  const bg = ctx.createLinearGradient(0, 0, 0, ch);
  bg.addColorStop(0, '#2b1a55'); bg.addColorStop(1, '#180e36');
  ctx.fillStyle = bg; roundRect(0, 0, cw, ch, 20); ctx.fill();

  // board cells
  for (let r = 0; r < ROWS; r++) for (let c = 0; c < COLS; c++) {
    const x = pad + c * cell + cell * .06, y = pad + r * cell + cell * .06;
    const s = cell * .88;
    ctx.fillStyle = (r + c) % 2 ? 'rgba(255,255,255,.035)' : 'rgba(255,255,255,.065)';
    roundRect(x, y, s, s, cell * .17); ctx.fill();
  }

  // hint pulse
  if (hint && !busy && !ended) {
    const pulse = .55 + .45 * Math.sin(t * 7);
    [hint.a, hint.b].forEach(p => {
      ctx.save();
      ctx.strokeStyle = 'rgba(255,209,102,' + (0.35 + pulse * .55) + ')';
      ctx.lineWidth = 3 + pulse * 2;
      ctx.shadowColor = '#ffd166'; ctx.shadowBlur = 16 * pulse;
      roundRect(px(p.c) - cell * .45, py(p.r) - cell * .45, cell * .9, cell * .9, cell * .2);
      ctx.stroke(); ctx.restore();
    });
  }

  // selected ring
  if (selected && !ended) {
    ctx.save();
    ctx.strokeStyle = '#fff'; ctx.lineWidth = 2.5;
    ctx.shadowColor = '#fff'; ctx.shadowBlur = 14;
    roundRect(px(selected.c) - cell * .46, py(selected.r) - cell * .46, cell * .92, cell * .92, cell * .2);
    ctx.stroke(); ctx.restore();
  }

  for (let r = 0; r < ROWS; r++) for (let c = 0; c < COLS; c++) {
    const b = at(c, r); if (!b) continue;
    b.x = lerp(b.x, b.c, Math.min(1, dt * 13));
    b.y = lerp(b.y, b.r, Math.min(1, dt * 13));
    b.scale = lerp(b.scale, 1, Math.min(1, dt * 11));
    b.alpha = lerp(b.alpha, 1, Math.min(1, dt * 11));
    if (b.dying) b.die += dt;
    if (b.pop > 0) b.pop = Math.max(0, b.pop - dt * 5);
    drawBall(b, t);
  }

  // particles
  for (let i = particles.length - 1; i >= 0; i--) {
    const p = particles[i];
    p.life -= dt; p.x += p.vx * dt; p.y += p.vy * dt; p.vy += 260 * dt;
    if (p.life <= 0) { particles.splice(i, 1); continue; }
    ctx.save(); ctx.globalAlpha = clamp(p.life / p.max, 0, 1);
    ctx.fillStyle = p.color;
    ctx.beginPath(); ctx.arc(p.x, p.y, p.size * (p.life / p.max), 0, Math.PI * 2); ctx.fill();
    ctx.restore();
  }

  // floating score labels
  for (let i = floaters.length - 1; i >= 0; i--) {
    const f = floaters[i];
    f.life -= dt; f.y -= 42 * dt;
    if (f.life <= 0) { floaters.splice(i, 1); continue; }
    ctx.save();
    ctx.globalAlpha = clamp(f.life / f.max, 0, 1);
    ctx.font = '800 ' + Math.round(cell * .42) + 'px system-ui, sans-serif';
    ctx.textAlign = 'center'; ctx.textBaseline = 'middle';
    ctx.shadowColor = 'rgba(0,0,0,.65)'; ctx.shadowBlur = 8;
    ctx.fillStyle = f.color; ctx.fillText(f.text, f.x, f.y);
    ctx.restore();
  }
}

function burst(c, r, color, n) {
  const x = px(c), y = py(r);
  for (let i = 0; i < n; i++) {
    const a = Math.random() * Math.PI * 2, sp = 40 + Math.random() * 170;
    particles.push({ x, y, vx: Math.cos(a) * sp, vy: Math.sin(a) * sp - 40,
      size: cell * (.06 + Math.random() * .10), color,
      life: .45 + Math.random() * .3, max: .75 });
  }
}
function floatText(c, r, text, color) {
  floaters.push({ x: px(c), y: py(r), text, color, life: .95, max: .95 });
}

// ---------------------------------------------------------------- game logic

function findMatches() {
  const set = new Set();
  for (let r = 0; r < ROWS; r++) {
    let c = 0;
    while (c < COLS) {
      const b = at(c, r);
      if (!b) { c++; continue; }
      let e = c + 1;
      while (e < COLS && at(e, r) && at(e, r).color === b.color) e++;
      if (e - c >= 3) for (let k = c; k < e; k++) set.add(r * COLS + k);
      c = e;
    }
  }
  for (let c = 0; c < COLS; c++) {
    let r = 0;
    while (r < ROWS) {
      const b = at(c, r);
      if (!b) { r++; continue; }
      let e = r + 1;
      while (e < ROWS && at(c, e) && at(c, e).color === b.color) e++;
      if (e - r >= 3) for (let k = r; k < e; k++) set.add(k * COLS + c);
      r = e;
    }
  }
  return Array.from(set).map(v => ({ c: v % COLS, r: Math.floor(v / COLS) }));
}

function hasValidMove() {
  for (let r = 0; r < ROWS; r++) for (let c = 0; c < COLS; c++) {
    for (const d of [[1, 0], [0, 1]]) {
      const nc = c + d[0], nr = r + d[1];
      if (!valid(nc, nr)) continue;
      const a = at(c, r), b = at(nc, nr);
      if (!a || !b || a.color === b.color) continue;
      board[r][c] = b; board[nr][nc] = a;
      const ok = findMatches().length > 0;
      board[r][c] = a; board[nr][nc] = b;
      if (ok) return { a: { c, r }, b: { c: nc, r: nr } };
    }
  }
  return null;
}

function updateHud() {
  scoreEl.textContent = score;
  movesEl.textContent = moves;
  bestEl.textContent = best;
  targetEl.textContent = TARGET;
  progressEl.style.width = clamp(score / TARGET * 100, 0, 100) + '%';
}

function finish(won) {
  ended = true; busy = false; selected = null; hint = null;
  if (score > best) {
    best = score;
    try { localStorage.setItem('candyballs.best', String(best)); } catch (e) {}
    updateHud();
  }
  ovIcon.textContent = won ? '🏆' : '💜';
  ovTitle.textContent = won ? 'Sweet victory!' : 'Out of moves';
  ovText.textContent = won
    ? 'You smashed ' + score + ' / ' + TARGET + ' points.'
    : 'You scored ' + score + ' / ' + TARGET + '. One more go?';
  overlay.classList.remove('hidden');
  sound(won ? 'win' : 'lose');
}

async function resolve(g) {
  let cascade = 0;
  while (true) {
    if (g !== gen) return;
    const matches = findMatches();
    if (!matches.length) break;

    cascade++;
    const points = POINTS * matches.length * cascade;
    score += points;
    if (score > best) best = score;
    updateHud();
    sound(cascade > 1 ? 'combo' : 'match', cascade);

    let cx = 0, cy = 0;
    for (const m of matches) {
      const b = at(m.c, m.r);
      if (b) {
        b.dying = true; b.die = 0; b.pop = 1;
        burst(m.c, m.r, PALETTE[b.color % PALETTE.length].base, 7);
      }
      cx += m.c; cy += m.r;
    }
    cx /= matches.length; cy /= matches.length;
    floatText(cx, cy, '+' + points + (cascade > 1 ? '  ×' + cascade : ''),
      cascade > 1 ? '#ffd166' : '#ffffff');

    await wait(cascade > 1 ? 210 : 170);
    if (g !== gen) return;

    for (const m of matches) board[m.r][m.c] = null;

    // gravity
    for (let c = 0; c < COLS; c++) {
      let write = ROWS - 1;
      for (let r = ROWS - 1; r >= 0; r--) {
        const b = board[r][c];
        if (!b) continue;
        if (write !== r) { board[write][c] = b; board[r][c] = null; b.r = write; }
        write--;
      }
      for (let r = write; r >= 0; r--) {
        const b = makeBall(c, r, rnd(COLORS));
        b.y = r - ROWS - 1 + (write - r) * .2;
        board[r][c] = b;
      }
    }
    await wait(230);
    if (g !== gen) return;
  }

  if (score >= TARGET) { finish(true); return; }
  if (moves <= 0) { finish(false); return; }

  if (!hasValidMove()) {
    floatText(3.5, 3.5, 'SHUFFLE!', '#ffd166');
    const colors = [];
    for (let r = 0; r < ROWS; r++) for (let c = 0; c < COLS; c++) colors.push(board[r][c].color);
    for (let i = colors.length - 1; i > 0; i--) {
      const j = rnd(i + 1); const t = colors[i]; colors[i] = colors[j]; colors[j] = t;
    }
    let i = 0;
    for (let r = 0; r < ROWS; r++) for (let c = 0; c < COLS; c++) {
      const b = board[r][c];
      if (b) { b.color = colors[i++]; b.pop = 1; }
    }
    await wait(200);
    if (g !== gen) return;
    if (!hasValidMove()) { finish(false); return; }
    resolve(g);
    return;
  }

  busy = false;
  lastInput = performance.now();
}

async function trySwap(a, b) {
  if (!valid(b.c, b.r) || busy || ended) return;
  const g = gen;
  const ba = at(a.c, a.r), bb = at(b.c, b.r);
  if (!ba || !bb || ba.color === bb.color) { sound('bad'); return; }

  busy = true; selected = null; hint = null; lastInput = performance.now();
  board[a.r][a.c] = bb; board[b.r][b.c] = ba;
  ba.c = b.c; ba.r = b.r; bb.c = a.c; bb.r = a.r;
  sound('swap');

  await wait(190);
  if (g !== gen) return;

  if (!findMatches().length) {
    board[a.r][a.c] = ba; board[b.r][b.c] = bb;
    ba.c = a.c; ba.r = a.r; bb.c = b.c; bb.r = b.r;
    sound('bad');
    await wait(190);
    if (g !== gen) return;
    busy = false;
    return;
  }

  moves = Math.max(0, moves - 1);
  updateHud();
  resolve(g);
}

// ---------------------------------------------------------------- input

function cellAt(ev) {
  const rect = canvas.getBoundingClientRect();
  const x = ev.clientX - rect.left, y = ev.clientY - rect.top;
  const c = Math.floor((x - pad) / cell), r = Math.floor((y - pad) / cell);
  return valid(c, r) ? { c, r } : null;
}
const adjacent = (a, b) => Math.abs(a.c - b.c) + Math.abs(a.r - b.r) === 1;

canvas.addEventListener('pointerdown', ev => {
  if (busy || ended) return;
  try { canvas.setPointerCapture(ev.pointerId); } catch (e) {}
  const p = cellAt(ev); if (!p) return;
  hint = null; lastInput = performance.now();
  if (selected && adjacent(selected, p)) {
    trySwap(selected, p);
  } else {
    selected = p;
  }
});

canvas.addEventListener('pointermove', ev => {
  if (busy || ended || !selected) return;
  const p = cellAt(ev); if (!p) return;
  if (p.c === selected.c && p.r === selected.r) return;
  if (adjacent(selected, p)) {
    const from = selected;
    selected = null;
    trySwap(from, p);
  }
});

canvas.addEventListener('pointerup', () => { /* selection stays for tap-tap */ });
canvas.addEventListener('pointercancel', () => { selected = null; });

document.addEventListener('keydown', ev => {
  if (ev.key === 'r' || ev.key === 'R') restart();
  if (ev.key === 'm' || ev.key === 'M') toggleSound();
  if (ev.key === 'Escape') selected = null;
});
el('restartBtn').addEventListener('click', restart);
el('soundBtn').addEventListener('click', toggleSound);
el('ovBtn').addEventListener('click', restart);
window.addEventListener('resize', resize);

// ---------------------------------------------------------------- audio

function initAudio() {
  if (audio) return;
  try { audio = new (window.AudioContext || window.webkitAudioContext)(); } catch (e) {}
}
function tone(freq, dur, type, vol, delay) {
  if (!audio || muted) return;
  const t = audio.currentTime + (delay || 0);
  const osc = audio.createOscillator(), gain = audio.createGain();
  osc.type = type || 'sine';
  osc.frequency.setValueAtTime(freq, t);
  osc.frequency.exponentialRampToValueAtTime(Math.max(40, freq * 1.5), t + dur);
  gain.gain.setValueAtTime(0.0001, t);
  gain.gain.exponentialRampToValueAtTime(vol || .12, t + .015);
  gain.gain.exponentialRampToValueAtTime(0.0001, t + dur);
  osc.connect(gain); gain.connect(audio.destination);
  osc.start(t); osc.stop(t + dur + .02);
}
function sound(kind, n) {
  initAudio();
  if (!audio || muted) return;
  if (audio.state === 'suspended') audio.resume();
  if (kind === 'swap') tone(380, .10, 'sine', .10);
  else if (kind === 'bad') { tone(190, .16, 'sawtooth', .06); tone(150, .18, 'sine', .05, .03); }
  else if (kind === 'match') tone(620 + Math.min(n, 4) * 80, .16, 'triangle', .12);
  else if (kind === 'combo') { tone(700, .14, 'triangle', .12); tone(950, .18, 'sine', .10, .09); }
  else if (kind === 'win') [523, 659, 784, 1047].forEach((f, i) => tone(f, .35, 'sine', .11, i * .12));
  else if (kind === 'lose') [392, 330, 262].forEach((f, i) => tone(f, .38, 'triangle', .10, i * .14));
  else if (kind === 'start') [440, 660].forEach((f, i) => tone(f, .18, 'sine', .09, i * .09));
}
function toggleSound() {
  muted = !muted;
  el('soundBtn').textContent = muted ? '🔇' : '🔊';
  if (!muted) sound('swap');
}

// ---------------------------------------------------------------- lifecycle

function restart() {
  gen++;
  score = 0; moves = MOVES; busy = false; ended = false;
  selected = null; hint = null; particles = []; floaters = [];
  dragStart = null;
  overlay.classList.add('hidden');
  fillBoard();
  updateHud();
  lastInput = performance.now();
  sound('start');
}

let last = performance.now();
function loop(now) {
  const dt = Math.min(.05, (now - last) / 1000);
  last = now;
  const t = now / 1000;

  if (!busy && !ended && !hint && now - lastInput > 4500 && !selected) {
    hint = hasValidMove();
  }

  draw(t, dt);
  requestAnimationFrame(loop);
}

resize();
restart();
requestAnimationFrame(loop);
})();
