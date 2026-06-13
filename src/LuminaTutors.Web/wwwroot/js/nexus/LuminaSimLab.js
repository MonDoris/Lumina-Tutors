/**
 * LuminaSimLab — Thí nghiệm TƯƠNG TÁC theo tham số (Vật lý · Sinh học · Toán).
 *
 *   • Giáo viên kéo thanh trượt (chiều dài con lắc, góc tới, cường độ ánh sáng,
 *     thành phần vector…) -> mô phỏng 3D chạy theo ĐÚNG công thức -> bảng kết
 *     quả cập nhật trực tiếp (chu kỳ, góc khúc xạ, tốc độ thải O₂, tích có hướng…).
 *   • Mọi tham số đều là SỐ -> đồng bộ real-time qua hub: teacher emit SyncSim,
 *     student nhận RemoteSim. Người vào sau khôi phục qua restore().
 *
 * Mỗi thí nghiệm là 1 "definition" trong SIMS: params (thanh trượt/lựa chọn),
 * build (dựng 3D), update (mô phỏng mỗi frame), readout (bảng kết quả).
 */
import * as THREE from 'three';
import { CSS2DObject } from 'three/addons/renderers/CSS2DRenderer.js';

const UP = new THREE.Vector3(0, 1, 0);
const deg = (d) => THREE.MathUtils.degToRad(d);

// ── Vật liệu hologram dùng chung ────────────────────────────────────────────
const holoSolid = (c, o = 0.55) => new THREE.MeshStandardMaterial({
  color: 0x0a1c2a, emissive: c, emissiveIntensity: 0.7, metalness: 0, roughness: 1,
  transparent: true, opacity: o, depthWrite: false, side: THREE.FrontSide,
});
const holoMetal = () => new THREE.MeshStandardMaterial({
  color: 0x0a1c2a, emissive: 0x7fbfe0, emissiveIntensity: 0.4, metalness: 0.3, roughness: 0.6,
  transparent: true, opacity: 0.6, depthWrite: false,
});
const glowMat = (c, o = 0.9) => new THREE.MeshBasicMaterial({ color: c, transparent: true, opacity: o, blending: THREE.AdditiveBlending, depthWrite: false });
const faint = (c, o) => new THREE.MeshBasicMaterial({ color: c, transparent: true, opacity: o, depthWrite: false });

// Tia sáng/đoạn thẳng pivot tại gốc, vươn 1 đầu theo trục Y (sign = +/-)
function beam(color, len, sign, r = 0.022) {
  const geo = new THREE.CylinderGeometry(r, r, len, 10);
  geo.translate(0, (sign * len) / 2, 0);
  return new THREE.Mesh(geo, glowMat(color, 0.92));
}

// Texture chấm sáng cho particle (bọt khí)
let _tex;
function glowTex() {
  if (_tex) return _tex;
  const c = document.createElement('canvas'); c.width = c.height = 32;
  const x = c.getContext('2d'); const g = x.createRadialGradient(16, 16, 0, 16, 16, 16);
  g.addColorStop(0, 'rgba(255,255,255,1)'); g.addColorStop(.5, 'rgba(255,255,255,.45)'); g.addColorStop(1, 'rgba(255,255,255,0)');
  x.fillStyle = g; x.fillRect(0, 0, 32, 32);
  _tex = new THREE.CanvasTexture(c); _tex.colorSpace = THREE.SRGBColorSpace; return _tex;
}

// Pool hạt nổi lên (bọt O₂ trong quang hợp)
class RisingPoints {
  constructor(group, { n = 90, size = 0.05, color = 0xbff4ff } = {}) {
    this.n = n; this.pos = new Float32Array(n * 3).fill(-999); this.vy = new Float32Array(n); this.life = new Float32Array(n).fill(-1);
    this.geo = new THREE.BufferGeometry(); this.geo.setAttribute('position', new THREE.BufferAttribute(this.pos, 3));
    this.mat = new THREE.PointsMaterial({ size, color, map: glowTex(), transparent: true, opacity: 0.85, depthWrite: false, blending: THREE.AdditiveBlending, sizeAttenuation: true });
    this.obj = new THREE.Points(this.geo, this.mat); this.obj.frustumCulled = false; group.add(this.obj); this._i = 0;
  }
  spawn(x, y, z, vy, life) { const j = (this._i = (this._i + 1) % this.n), i = j * 3; this.pos[i] = x; this.pos[i + 1] = y; this.pos[i + 2] = z; this.vy[j] = vy; this.life[j] = life; }
  update(dt, topY) {
    for (let j = 0; j < this.n; j++) {
      if (this.life[j] <= 0) continue;
      this.life[j] -= dt; const i = j * 3; this.pos[i + 1] += this.vy[j] * dt; this.vy[j] += 0.4 * dt;
      if (this.life[j] <= 0 || this.pos[i + 1] > topY) { this.life[j] = -1; this.pos[i + 1] = -999; }
    }
    this.geo.attributes.position.needsUpdate = true;
  }
  clear() { this.life.fill(-1); for (let j = 0; j < this.n; j++) this.pos[j * 3 + 1] = -999; this.geo.attributes.position.needsUpdate = true; }
}

const vecTxt = (v) => `(${v.x}, ${v.y}, ${v.z})`;

// ════════════════════════════════════════════════════════════════════════════
//  ĐỊNH NGHĨA CÁC THÍ NGHIỆM
// ════════════════════════════════════════════════════════════════════════════
export const SIMS = {
  // ── VẬT LÝ: Con lắc đơn ───────────────────────────────────────────────────
  'pendulum-lab': {
    label: 'Con lắc đơn (tương tác)', subject: 'physics',
    params: [
      { key: 'length', label: 'Chiều dài ℓ', min: 0.6, max: 2.4, step: 0.1, value: 1.4, unit: 'm' },
      { key: 'amplitude', label: 'Biên độ θ₀', min: 5, max: 55, step: 1, value: 28, unit: '°' },
      { key: 'mass', label: 'Khối lượng m', min: 0.2, max: 3, step: 0.1, value: 1, unit: 'kg' },
      { key: 'gravity', label: 'Trọng trường g', type: 'select', value: 9.8, unit: 'm/s²',
        options: [{ label: 'Trái Đất (9.8)', value: 9.8 }, { label: 'Mặt Trăng (1.6)', value: 1.6 }, { label: 'Sao Mộc (24.8)', value: 24.8 }] },
    ],
    build(c) {
      const g = c.group, s = c.state; s.pivotY = 1.2;
      const bar = new THREE.Mesh(new THREE.BoxGeometry(1.5, 0.07, 0.07), holoMetal()); bar.position.y = s.pivotY + 0.05;
      const mount = new THREE.Mesh(new THREE.CylinderGeometry(0.05, 0.05, 0.12, 12), holoMetal()); mount.position.y = s.pivotY;
      s.string = new THREE.Mesh(new THREE.CylinderGeometry(0.012, 0.012, 1, 8), glowMat(0x7fe9ff, 0.7));
      s.bob = new THREE.Mesh(new THREE.SphereGeometry(1, 32, 24), holoSolid(0x9fd8ff, 0.6));
      g.add(bar, mount, s.string, s.bob); s.phase = 0;
    },
    update(dt, t, c) {
      const s = c.state, p = c.params;
      const T = 2 * Math.PI * Math.sqrt(p.length / p.gravity), omega = (2 * Math.PI) / T;
      s.phase += omega * dt;
      const theta = deg(p.amplitude) * Math.cos(s.phase);
      const Lvis = 0.5 + p.length * 0.5;
      const bx = Lvis * Math.sin(theta), by = s.pivotY - Lvis * Math.cos(theta);
      s.bob.position.set(bx, by, 0); s.bob.scale.setScalar(0.12 + p.mass * 0.05);
      const pivot = new THREE.Vector3(0, s.pivotY, 0), bob = new THREE.Vector3(bx, by, 0);
      const dir = bob.clone().sub(pivot), len = dir.length();
      s.string.position.copy(pivot).add(bob).multiplyScalar(0.5);
      s.string.quaternion.setFromUnitVectors(UP, dir.normalize());
      s.string.scale.set(1, len, 1);
    },
    readout(c) {
      const p = c.params, T = 2 * Math.PI * Math.sqrt(p.length / p.gravity);
      return { title: 'Con lắc đơn', lines: [
        { k: 'Chu kỳ T', v: T.toFixed(2) + ' s' },
        { k: 'Tần số f', v: (1 / T).toFixed(2) + ' Hz' },
        { k: 'Công thức', v: 'T = 2π√(ℓ/g)' },
        { k: 'Ghi chú', v: 'T không phụ thuộc m' },
      ] };
    },
  },

  // ── VẬT LÝ: Khúc xạ ánh sáng ──────────────────────────────────────────────
  'optics-lab': {
    label: 'Khúc xạ ánh sáng (tương tác)', subject: 'physics',
    params: [
      { key: 'angle', label: 'Góc tới θ₁', min: 0, max: 85, step: 1, value: 42, unit: '°' },
      { key: 'n2', label: 'Môi trường 2', type: 'select', value: 1.33,
        options: [{ label: 'Nước (1.33)', value: 1.33 }, { label: 'Thủy tinh (1.50)', value: 1.50 }, { label: 'Kim cương (2.42)', value: 2.42 }] },
    ],
    build(c) {
      const g = c.group, s = c.state;
      const upper = new THREE.Mesh(new THREE.BoxGeometry(2.8, 1.3, 0.04), faint(0xbfe8ff, 0.05)); upper.position.y = 0.65;
      s.lower = new THREE.Mesh(new THREE.BoxGeometry(2.8, 1.3, 0.04), faint(0x4fb8e6, 0.16)); s.lower.position.y = -0.65;
      const iface = new THREE.Mesh(new THREE.BoxGeometry(2.8, 0.014, 0.05), glowMat(0xffffff, 0.7));
      const normal = new THREE.Mesh(new THREE.CylinderGeometry(0.006, 0.006, 2.4, 6), glowMat(0x9fc4e0, 0.5));
      s.incident = beam(0xffe066, 1.35, +1);
      s.reflected = beam(0x9fd8ff, 1.15, +1);
      s.refracted = beam(0x66ff9f, 1.35, -1);
      g.add(upper, s.lower, iface, normal, s.incident, s.refracted, s.reflected);
      c.addLabel('Không khí · n₁=1', 1.0, 1.05, 0);
      s.medLabel = c.addLabel('Nước · n₂', 1.0, -1.05, 0);
    },
    update(dt, t, c) {
      const p = c.params, s = c.state;
      const th1 = deg(p.angle), th2 = Math.asin(Math.min(Math.sin(th1) / p.n2, 1));
      s.incident.rotation.z = th1; s.reflected.rotation.z = -th1; s.refracted.rotation.z = th2;
      const nm = p.n2 === 1.33 ? 'Nước' : p.n2 === 1.50 ? 'Thủy tinh' : 'Kim cương';
      if (s.medLabel) s.medLabel.element.textContent = `${nm} · n₂=${p.n2}`;
    },
    readout(c) {
      const p = c.params, th2 = Math.asin(Math.min(Math.sin(deg(p.angle)) / p.n2, 1)) * 180 / Math.PI;
      return { title: 'Khúc xạ ánh sáng', lines: [
        { k: 'Góc tới θ₁', v: p.angle.toFixed(0) + '°' },
        { k: 'Góc khúc xạ θ₂', v: th2.toFixed(1) + '°' },
        { k: 'Chiết suất n₂', v: p.n2.toFixed(2) },
        { k: 'Định luật', v: 'sinθ₁ = n₂·sinθ₂' },
      ] };
    },
  },

  // ── SINH HỌC: Quang hợp (đo bọt khí O₂) ──────────────────────────────────
  'photosynthesis-lab': {
    label: 'Quang hợp (tương tác)', subject: 'biology',
    params: [
      { key: 'light', label: 'Cường độ ánh sáng', min: 0, max: 100, step: 5, value: 60, unit: '%' },
      { key: 'co2', label: 'Nồng độ CO₂', min: 0, max: 100, step: 5, value: 55, unit: '%' },
    ],
    build(c) {
      const g = c.group, s = c.state;
      const beaker = new THREE.Mesh(new THREE.CylinderGeometry(0.72, 0.68, 1.5, 32, 1, true), holoSolid(0x9fd8ff, 0.13));
      s.water = new THREE.Mesh(new THREE.CylinderGeometry(0.68, 0.65, 1.3, 32), holoSolid(0x4fd0e0, 0.28)); s.water.position.y = -0.05;
      s.waterTop = 0.6;
      s.plant = new THREE.Group();
      const stem = new THREE.Mesh(new THREE.CylinderGeometry(0.03, 0.045, 1.1, 8), holoSolid(0x49b257)); stem.position.y = 0;
      s.plant.add(stem); s.leafTips = [];
      for (let i = 0; i < 7; i++) {
        const side = i % 2 ? 1 : -1, yy = -0.5 + i * 0.16;
        const leaf = new THREE.Mesh(new THREE.SphereGeometry(0.13, 12, 10), holoSolid(0x5fcf6a)); leaf.scale.set(1, 0.4, 0.7);
        leaf.position.set(side * 0.18, yy, 0); leaf.rotation.z = side * 0.7; s.plant.add(leaf);
        s.leafTips.push(new THREE.Vector3(side * 0.34, yy + 0.04, 0));
      }
      s.plant.position.set(-0.05, -0.55, 0);
      s.sun = new THREE.Sprite(new THREE.SpriteMaterial({ map: glowTex(), color: 0xffe066, transparent: true, opacity: 0.8, blending: THREE.AdditiveBlending, depthWrite: false }));
      s.sun.position.set(1.45, 0.95, 0.2); s.sun.scale.setScalar(0.8);
      g.add(beaker, s.water, s.plant, s.sun);
      s.bubbles = new RisingPoints(g, { n: 110, size: 0.05, color: 0xc8f6ff });
      s.acc = 0;
      c.addLabel('Rong đuôi chó', -0.05, 0.7, 0);
    },
    rate(p) { return 0.3 + 5 * (p.light / 100) * (0.4 + 0.6 * (p.co2 / 100)); },
    update(dt, t, c) {
      const p = c.params, s = c.state, rate = SIMS['photosynthesis-lab'].rate(p);
      s.acc += rate * dt;
      while (s.acc >= 1) {
        s.acc -= 1; const tip = s.leafTips[(Math.random() * s.leafTips.length) | 0];
        s.bubbles.spawn(s.plant.position.x + tip.x + (Math.random() - .5) * 0.05, s.plant.position.y + tip.y, tip.z + (Math.random() - .5) * 0.05, 0.22 + Math.random() * 0.12, 6);
      }
      s.bubbles.update(dt, s.waterTop);
      const lf = p.light / 100;
      s.sun.material.opacity = 0.28 + 0.6 * lf; s.sun.scale.setScalar(0.5 + 0.45 * lf);
      s.plant.children.forEach((ch) => { if (ch.material) ch.material.emissiveIntensity = 0.45 + 0.5 * lf; });
    },
    readout(c) {
      const p = c.params, perMin = Math.round(SIMS['photosynthesis-lab'].rate(p) * 60);
      return { title: 'Quang hợp ở cây xanh', lines: [
        { k: 'Ánh sáng', v: p.light + '%' },
        { k: 'CO₂ (NaHCO₃)', v: p.co2 + '%' },
        { k: 'Tốc độ thải O₂', v: '~' + perMin + ' bọt/phút' },
        { k: 'Yếu tố hạn chế', v: p.light <= p.co2 ? 'Ánh sáng' : 'CO₂' },
        { k: 'Phương trình', v: '6CO₂+6H₂O→C₆H₁₂O₆+6O₂', wide: true },
      ] };
    },
  },

  // ── TOÁN: Vector 3D (tổng & tích có hướng) ───────────────────────────────
  'vectors-lab': {
    label: 'Vector 3D (tương tác)', subject: 'math',
    params: [
      { key: 'ax', label: 'a⃗ · x', min: -2, max: 2, step: 0.5, value: 1.5, unit: '' },
      { key: 'ay', label: 'a⃗ · y', min: -2, max: 2, step: 0.5, value: 1, unit: '' },
      { key: 'az', label: 'a⃗ · z', min: -2, max: 2, step: 0.5, value: 0, unit: '' },
      { key: 'bx', label: 'b⃗ · x', min: -2, max: 2, step: 0.5, value: 0, unit: '' },
      { key: 'by', label: 'b⃗ · y', min: -2, max: 2, step: 0.5, value: 1, unit: '' },
      { key: 'bz', label: 'b⃗ · z', min: -2, max: 2, step: 0.5, value: 1.5, unit: '' },
    ],
    build(c) {
      const g = c.group, s = c.state;
      const axis = (dir, col) => { const a = new THREE.ArrowHelper(dir, new THREE.Vector3(), 1.7, col, 0.16, 0.09); a.line.material.transparent = true; a.line.material.opacity = 0.35; return a; };
      g.add(axis(new THREE.Vector3(1, 0, 0), 0xff8a8a), axis(new THREE.Vector3(0, 1, 0), 0x8affa0), axis(new THREE.Vector3(0, 0, 1), 0x8ab4ff));
      const mk = (col) => { const a = new THREE.ArrowHelper(new THREE.Vector3(0, 1, 0), new THREE.Vector3(), 1, col, 0.24, 0.13); return a; };
      s.aArr = mk(0xff5a5a); s.bArr = mk(0x27c08a); s.sumArr = mk(0xc9a96a); s.crossArr = mk(0xb24ddb);
      g.add(s.aArr, s.bArr, s.sumArr, s.crossArr);
      s.la = c.addLabel('a⃗', 0, 0, 0); s.lb = c.addLabel('b⃗', 0, 0, 0);
      s.lsum = c.addLabel('a⃗+b⃗', 0, 0, 0); s.lcr = c.addLabel('a⃗×b⃗', 0, 0, 0);
    },
    update(dt, t, c) {
      const p = c.params, s = c.state;
      const a = new THREE.Vector3(p.ax, p.ay, p.az), b = new THREE.Vector3(p.bx, p.by, p.bz);
      const sum = a.clone().add(b), cross = new THREE.Vector3().crossVectors(a, b);
      const setA = (arr, lbl, v, clamp) => {
        const len = v.length();
        if (len < 1e-4) { arr.visible = false; lbl.visible = false; return; }
        arr.visible = lbl.visible = true;
        const dl = clamp ? Math.min(len, clamp) : len;
        arr.setDirection(v.clone().normalize()); arr.setLength(dl, Math.min(0.24, dl * 0.35), Math.min(0.13, dl * 0.2));
        lbl.position.copy(v.clone().normalize().multiplyScalar(dl + 0.14));
      };
      setA(s.aArr, s.la, a); setA(s.bArr, s.lb, b); setA(s.sumArr, s.lsum, sum, 4); setA(s.crossArr, s.lcr, cross, 4);
    },
    readout(c) {
      const p = c.params;
      const a = new THREE.Vector3(p.ax, p.ay, p.az), b = new THREE.Vector3(p.bx, p.by, p.bz);
      const sum = a.clone().add(b), cross = new THREE.Vector3().crossVectors(a, b);
      const la = a.length(), lb = b.length(), dot = a.dot(b);
      const ang = (la < 1e-4 || lb < 1e-4) ? '—' : (Math.acos(THREE.MathUtils.clamp(dot / (la * lb), -1, 1)) * 180 / Math.PI).toFixed(1) + '°';
      return { title: 'Vector trong không gian', lines: [
        { k: 'a⃗', v: vecTxt(a) }, { k: 'b⃗', v: vecTxt(b) },
        { k: '|a⃗|', v: la.toFixed(2) }, { k: '|b⃗|', v: lb.toFixed(2) },
        { k: 'a⃗·b⃗', v: dot.toFixed(2) }, { k: 'Góc (a⃗,b⃗)', v: ang },
        { k: 'a⃗+b⃗', v: vecTxt(sum) },
        { k: 'a⃗×b⃗', v: vecTxt(cross) },
        { k: '|a⃗×b⃗| = S', v: cross.length().toFixed(2), wide: true },
      ] };
    },
  },

  // ── VẬT LÝ: Con lắc lò xo ─────────────────────────────────────────────────
  'spring-lab': {
    label: 'Con lắc lò xo (tương tác)', subject: 'physics',
    params: [
      { key: 'k', label: 'Độ cứng k', min: 5, max: 60, step: 1, value: 20, unit: 'N/m' },
      { key: 'mass', label: 'Khối lượng m', min: 0.1, max: 2, step: 0.1, value: 0.5, unit: 'kg' },
      { key: 'amp', label: 'Biên độ A', min: 0.05, max: 0.4, step: 0.05, value: 0.25, unit: 'm' },
    ],
    build(c) {
      const g = c.group, s = c.state; s.topY = 1.15; s.baseLen = 0.85;
      const bar = new THREE.Mesh(new THREE.BoxGeometry(1.1, 0.07, 0.3), holoMetal()); bar.position.y = s.topY + 0.04;
      // Lò xo: đường xoắn ốc đơn vị (cao 1, hướng xuống), scale.y theo chiều dài
      const pts = []; const turns = 9, seg = 18;
      for (let i = 0; i <= turns * seg; i++) { const a = i * (Math.PI * 2 / seg), y = -i / (turns * seg);
        pts.push(new THREE.Vector3(Math.cos(a) * 0.14, y, Math.sin(a) * 0.14)); }
      s.spring = new THREE.Line(new THREE.BufferGeometry().setFromPoints(pts), glowMat(0x7fe9ff, 0.75));
      s.spring.position.y = s.topY;
      s.mass = new THREE.Mesh(new THREE.BoxGeometry(0.4, 0.4, 0.4), holoSolid(0x9fd8ff, 0.6));
      g.add(bar, s.spring, s.mass); s.phase = 0;
    },
    update(dt, t, c) {
      const s = c.state, p = c.params;
      const T = 2 * Math.PI * Math.sqrt(p.mass / p.k), omega = (2 * Math.PI) / T;
      s.phase += omega * dt;
      const disp = p.amp * 1.6 * Math.cos(s.phase);          // 1.6 = hệ số phóng đại cho dễ nhìn
      const len = s.baseLen + disp;
      s.spring.scale.y = len;
      s.mass.position.y = s.topY - len - 0.2;
    },
    readout(c) {
      const p = c.params, T = 2 * Math.PI * Math.sqrt(p.mass / p.k);
      return { title: 'Con lắc lò xo', lines: [
        { k: 'Chu kỳ T', v: T.toFixed(2) + ' s' },
        { k: 'Tần số f', v: (1 / T).toFixed(2) + ' Hz' },
        { k: 'Công thức', v: 'T = 2π√(m/k)' },
        { k: 'Lực kéo về', v: 'F = −k·x' },
      ] };
    },
  },

  // ── VẬT LÝ: Định luật Ôm (mạch điện) ─────────────────────────────────────
  'circuit-lab': {
    label: 'Định luật Ôm (tương tác)', subject: 'physics',
    params: [
      { key: 'u', label: 'Hiệu điện thế U', min: 1, max: 24, step: 1, value: 9, unit: 'V' },
      { key: 'r', label: 'Điện trở R', min: 1, max: 100, step: 1, value: 30, unit: 'Ω' },
    ],
    build(c) {
      const g = c.group, s = c.state; s.W = 1.7; s.H = 1.3;
      const W = s.W, H = s.H, hw = W / 2, hh = H / 2;
      // Dây dẫn = khung chữ nhật phát sáng
      const wire = (x1, y1, x2, y2) => { const m = new THREE.Mesh(new THREE.CylinderGeometry(0.022, 0.022, Math.hypot(x2-x1, y2-y1), 8), glowMat(0x7fbfe0, 0.6));
        m.position.set((x1+x2)/2, (y1+y2)/2, 0); m.rotation.z = Math.atan2(y2-y1, x2-x1) - Math.PI/2; return m; };
      g.add(wire(-hw,-hh, hw,-hh), wire(hw,-hh, hw,hh), wire(hw,hh, -hw,hh), wire(-hw,hh, -hw,-hh));
      // Pin (trái) · Điện trở (phải) · Bóng đèn (trên)
      const batt = new THREE.Mesh(new THREE.BoxGeometry(0.22, 0.5, 0.22), holoSolid(0xffc24d, 0.7)); batt.position.set(-hw, 0, 0);
      const res = new THREE.Mesh(new THREE.BoxGeometry(0.5, 0.22, 0.22), holoSolid(0xff8a8a, 0.7)); res.position.set(hw, 0, 0);
      s.bulb = new THREE.Mesh(new THREE.SphereGeometry(0.2, 24, 20), new THREE.MeshStandardMaterial({
        color: 0x1a1206, emissive: 0xffe066, emissiveIntensity: 0.4, transparent: true, opacity: 0.85, depthWrite: false }));
      s.bulb.position.set(0, hh, 0);
      s.halo = new THREE.Sprite(new THREE.SpriteMaterial({ map: glowTex(), color: 0xffe066, transparent: true, opacity: 0, blending: THREE.AdditiveBlending, depthWrite: false }));
      s.halo.position.copy(s.bulb.position); s.halo.scale.setScalar(0.9);
      g.add(batt, res, s.bulb, s.halo);
      c.addLabel('Pin', -hw, -hh - 0.18, 0); c.addLabel('R', hw + 0.16, 0, 0); c.addLabel('Đèn', 0, hh + 0.22, 0);
      // Electron chạy quanh mạch
      s.N = 24; s.flow = 0;
      const pos = new Float32Array(s.N * 3).fill(-999);
      s.eGeo = new THREE.BufferGeometry(); s.eGeo.setAttribute('position', new THREE.BufferAttribute(pos, 3));
      s.electrons = new THREE.Points(s.eGeo, new THREE.PointsMaterial({ size: 0.07, map: glowTex(), color: 0x9fe8ff,
        transparent: true, opacity: 0.95, depthWrite: false, blending: THREE.AdditiveBlending })); s.electrons.frustumCulled = false;
      g.add(s.electrons);
    },
    _perim(s, t) {   // t∈[0,1) → điểm trên chu vi khung W×H
      const W = s.W, H = s.H, hw = W/2, hh = H/2, P = 2*(W+H), d = (t % 1) * P;
      if (d < W)            return [-hw + d, -hh];
      if (d < W + H)        return [hw, -hh + (d - W)];
      if (d < 2*W + H)      return [hw - (d - W - H), hh];
      return [-hw, hh - (d - 2*W - H)];
    },
    update(dt, t, c) {
      const s = c.state, p = c.params, I = p.u / p.r;
      s.flow = (s.flow + I * 0.06 * dt) % 1;
      const arr = s.eGeo.attributes.position.array;
      for (let i = 0; i < s.N; i++) { const [x, y] = SIMS['circuit-lab']._perim(s, s.flow + i / s.N); arr[i*3] = x; arr[i*3+1] = y; arr[i*3+2] = 0.02; }
      s.eGeo.attributes.position.needsUpdate = true;
      const bright = Math.min(I * 0.5, 2.2);                 // đèn sáng theo dòng điện
      s.bulb.material.emissiveIntensity = 0.3 + bright;
      s.halo.material.opacity = Math.min(0.15 + bright * 0.4, 0.9);
      s.halo.scale.setScalar(0.7 + bright * 0.35);
    },
    readout(c) {
      const p = c.params, I = p.u / p.r;
      return { title: 'Định luật Ôm', lines: [
        { k: 'Hiệu điện thế U', v: p.u + ' V' },
        { k: 'Điện trở R', v: p.r + ' Ω' },
        { k: 'Cường độ I = U/R', v: I.toFixed(2) + ' A' },
        { k: 'Công suất P = U·I', v: (p.u * I).toFixed(1) + ' W' },
      ] };
    },
  },

  // ── SINH HỌC: Thẩm thấu tế bào ───────────────────────────────────────────
  'osmosis-lab': {
    label: 'Thẩm thấu (tương tác)', subject: 'biology',
    params: [
      { key: 'conc', label: 'Nồng độ dung dịch', min: 0, max: 100, step: 5, value: 50, unit: '%' },
    ],
    build(c) {
      const g = c.group, s = c.state;
      const beaker = new THREE.Mesh(new THREE.CylinderGeometry(1.05, 1.0, 1.7, 40, 1, true), holoSolid(0x9fd8ff, 0.12));
      const water = new THREE.Mesh(new THREE.CylinderGeometry(1.0, 0.97, 1.5, 40), holoSolid(0x4fd0e0, 0.22)); water.position.y = -0.05;
      s.cell = new THREE.Group();
      s.mem = new THREE.Mesh(new THREE.SphereGeometry(0.55, 40, 32), holoSolid(0xbfe0ff, 0.4));
      const nuc = new THREE.Mesh(new THREE.SphereGeometry(0.2, 28, 22), holoSolid(0x9a7bd6, 0.6));
      s.cell.add(s.mem, nuc); g.add(beaker, water, s.cell);
      s.flow = new RisingPoints(g, { n: 70, size: 0.05, color: 0xbff4ff }); // tái dùng cho dòng nước
      s.acc = 0; s.scale = 1;
      c.addLabel('Tế bào', 0, 0.75, 0);
    },
    update(dt, t, c) {
      const s = c.state, p = c.params;
      // <50 nhược trương (nước vào, trương) · >50 ưu trương (nước ra, co)
      const target = THREE.MathUtils.clamp(1.35 - (p.conc / 100) * 0.85, 0.6, 1.4);
      s.scale += (target - s.scale) * Math.min(dt * 2.5, 1);
      s.cell.scale.setScalar(s.scale);
      // Dòng nước: vào (nhược) hay ra (ưu); đẳng trương ~ đứng yên
      const inward = p.conc < 48, outward = p.conc > 52;
      s.acc += dt * 14;
      while (s.acc >= 1 && (inward || outward)) {
        s.acc -= 1; const a = Math.random() * Math.PI * 2, rr = 0.95;
        const ex = Math.cos(a) * rr, ez = Math.sin(a) * rr, ey = (Math.random() - .5) * 0.8;
        if (inward) s.flow.spawn(ex, ey, ez, 0, 4); else s.flow.spawn(s.mem.position.x, 0, s.mem.position.z, 0, 4);
      }
      // RisingPoints bay lên — ở đây chỉ cần hiệu ứng hạt mờ quanh tế bào
      s.flow.update(dt, 0.7);
    },
    readout(c) {
      const p = c.params;
      const env = p.conc < 48 ? 'Nhược trương' : p.conc > 52 ? 'Ưu trương' : 'Đẳng trương';
      const flow = p.conc < 48 ? 'Nước đi VÀO tế bào' : p.conc > 52 ? 'Nước đi RA ngoài' : 'Cân bằng';
      const state = p.conc < 48 ? 'Tế bào TRƯƠNG (có thể vỡ)' : p.conc > 52 ? 'Tế bào CO nguyên sinh' : 'Bình thường';
      return { title: 'Hiện tượng thẩm thấu', lines: [
        { k: 'Nồng độ ngoài', v: p.conc + '%' },
        { k: 'Môi trường', v: env },
        { k: 'Chiều di chuyển', v: flow, wide: true },
        { k: 'Trạng thái tế bào', v: state, wide: true },
      ] };
    },
  },

  // ── TOÁN: Đồ thị hàm số bậc hai (Parabol) ────────────────────────────────
  'parabola-lab': {
    label: 'Đồ thị Parabol (tương tác)', subject: 'math',
    params: [
      { key: 'a', label: 'Hệ số a', min: -3, max: 3, step: 0.5, value: 1, unit: '' },
      { key: 'b', label: 'Hệ số b', min: -4, max: 4, step: 0.5, value: 0, unit: '' },
      { key: 'c', label: 'Hệ số c', min: -3, max: 3, step: 0.5, value: -1, unit: '' },
    ],
    build(c) {
      const g = c.group, s = c.state; s.SX = 0.62; s.SY = 0.42; s.N = 100;
      const axis = (x1, y1, x2, y2, col) => { const m = new THREE.Mesh(new THREE.CylinderGeometry(0.008, 0.008, Math.hypot(x2-x1, y2-y1), 6), glowMat(col, 0.4));
        m.position.set((x1+x2)/2, (y1+y2)/2, 0); m.rotation.z = Math.atan2(y2-y1, x2-x1) - Math.PI/2; return m; };
      g.add(axis(-2.1, 0, 2.1, 0, 0x8ab4ff), axis(0, -1.7, 0, 2.2, 0x8affa0));
      c.addLabel('x', 2.2, 0, 0); c.addLabel('y', 0, 2.3, 0);
      const pos = new Float32Array((s.N + 1) * 3);
      s.curveGeo = new THREE.BufferGeometry(); s.curveGeo.setAttribute('position', new THREE.BufferAttribute(pos, 3));
      s.curve = new THREE.Line(s.curveGeo, glowMat(0xffc24d, 0.95));
      s.vertex = new THREE.Mesh(new THREE.SphereGeometry(0.06, 16, 14), glowMat(0x35f5b0, 1));
      s.root1 = new THREE.Mesh(new THREE.SphereGeometry(0.055, 14, 12), glowMat(0xff5a8a, 1));
      s.root2 = new THREE.Mesh(new THREE.SphereGeometry(0.055, 14, 12), glowMat(0xff5a8a, 1));
      g.add(s.curve, s.vertex, s.root1, s.root2);
      s.vlabel = c.addLabel('I', 0, 0, 0);
    },
    update(dt, t, c) {
      const s = c.state, p = c.params, arr = s.curveGeo.attributes.position.array;
      const X0 = -3.2, X1 = 3.2, clampY = (yw) => THREE.MathUtils.clamp(yw, -1.7, 2.2);
      for (let i = 0; i <= s.N; i++) { const x = X0 + (X1 - X0) * (i / s.N), y = p.a*x*x + p.b*x + p.c;
        arr[i*3] = THREE.MathUtils.clamp(x * s.SX, -2.05, 2.05); arr[i*3+1] = clampY(y * s.SY); arr[i*3+2] = 0; }
      s.curveGeo.attributes.position.needsUpdate = true;
      if (Math.abs(p.a) > 1e-6) {                            // đỉnh
        const xv = -p.b / (2 * p.a), yv = p.c - p.b*p.b / (4*p.a);
        s.vertex.visible = true; s.vertex.position.set(THREE.MathUtils.clamp(xv*s.SX,-2.05,2.05), clampY(yv*s.SY), .01);
        s.vlabel.visible = true; s.vlabel.position.copy(s.vertex.position).add(new THREE.Vector3(.14,.12,0));
      } else { s.vertex.visible = false; s.vlabel.visible = false; }
      const D = p.b*p.b - 4*p.a*p.c;                          // nghiệm
      if (Math.abs(p.a) > 1e-6 && D >= 0) {
        const x1 = (-p.b - Math.sqrt(D)) / (2*p.a), x2 = (-p.b + Math.sqrt(D)) / (2*p.a);
        s.root1.visible = Math.abs(x1) <= 3.3; s.root1.position.set(x1*s.SX, 0, .01);
        s.root2.visible = Math.abs(x2) <= 3.3; s.root2.position.set(x2*s.SX, 0, .01);
      } else { s.root1.visible = s.root2.visible = false; }
    },
    readout(c) {
      const p = c.params;
      const term = (v, sym, first) => { if (v === 0) return ''; const sign = v < 0 ? '−' : (first ? '' : '+'); const av = Math.abs(v); return ` ${sign} ${av === 1 && sym ? '' : av}${sym}`; };
      let eq = 'y =' + (term(p.a,'x²',true) || ' 0') + term(p.b,'x') + term(p.c,'');
      const lines = [{ k: 'Hàm số', v: eq.replace('y =  ','y = ').trim(), wide: true }];
      if (Math.abs(p.a) < 1e-6) { lines.push({ k: 'Lưu ý', v: 'a = 0 → đường thẳng', wide: true }); }
      else {
        const xv = -p.b/(2*p.a), yv = p.c - p.b*p.b/(4*p.a), D = p.b*p.b - 4*p.a*p.c;
        lines.push({ k: 'Đỉnh I', v: `(${xv.toFixed(2)} ; ${yv.toFixed(2)})` });
        lines.push({ k: 'Trục đối xứng', v: 'x = ' + xv.toFixed(2) });
        lines.push({ k: 'Biệt thức Δ', v: D.toFixed(2) });
        lines.push({ k: 'Nghiệm', v: D < 0 ? 'Vô nghiệm' : D === 0 ? 'x = ' + (-p.b/(2*p.a)).toFixed(2)
          : `${((-p.b-Math.sqrt(D))/(2*p.a)).toFixed(2)} ; ${((-p.b+Math.sqrt(D))/(2*p.a)).toFixed(2)}`, wide: true });
        lines.push({ k: 'Bề lõm', v: p.a > 0 ? 'Hướng lên' : 'Hướng xuống' });
      }
      return { title: 'Hàm số bậc hai', lines };
    },
  },
};

export const SIM_IDS = Object.keys(SIMS);

// ════════════════════════════════════════════════════════════════════════════
export class LuminaSimLab {
  /** @param {import('./Lumina3DEngine.js').Lumina3DEngine} engine */
  constructor(engine, simId) {
    this.engine = engine; this.hub = engine.hub; this.simId = simId;
    this.def = SIMS[simId];
    this.group = new THREE.Group(); engine.specimen.add(this.group);
    this.state = {}; this.params = {};
    this.def.params.forEach((p) => (this.params[p.key] = p.value));
    this._disposed = false;
    this.def.build(this);
    this._emitState();
  }

  addLabel(text, x, y, z) {
    const el = document.createElement('div'); el.className = 'lumina-label lumina-label--sim'; el.textContent = text;
    const o = new CSS2DObject(el); o.position.set(x, y, z); this.group.add(o); return o;
  }

  setParam(key, value, emit = false) {
    if (this._disposed || !(key in this.params)) return;
    value = +value; if (!Number.isFinite(value)) return;
    this.params[key] = value;
    if (emit && this.engine.isTeacher) this.hub.invoke('SyncSim', key, value).catch(() => {});
    this._emitState();
  }

  resetDefaults(emit = false) {
    this.def.params.forEach((p) => (this.params[p.key] = p.value));
    if (emit && this.engine.isTeacher) this.hub.invoke('SimReset').catch(() => {});
    this._emitState();
  }

  /** Người vào sau: khôi phục tham số giáo viên đang đặt. */
  restore(obj) {
    if (!obj) return;
    Object.entries(obj).forEach(([k, v]) => { if (k in this.params) this.params[k] = +v; });
    this._emitState();
  }

  state() { return { ...this.params }; }
  readout() { return this.def.readout(this); }
  _emitState() { this.engine.onSimState?.(this.readout()); }
  update(dt, t) { if (!this._disposed) this.def.update(dt, t, this); }

  dispose() {
    this._disposed = true;
    this.group.traverse((o) => {
      if (o.isCSS2DObject) o.element?.remove?.();
      o.geometry?.dispose?.();
      if (o.material) (Array.isArray(o.material) ? o.material : [o.material]).forEach((m) => m.dispose?.());
    });
    this.engine.specimen.remove(this.group);
  }
}
