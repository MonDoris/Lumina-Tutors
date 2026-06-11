/**
 * LuminaChemLab — Bàn phản ứng hóa học TƯƠNG TÁC (Holographic Nexus).
 *
 *   • Giáo viên chọn hóa chất (chip trên kệ hoặc click lọ 3D) -> lọ bay lên,
 *     nghiêng rót vào cốc -> kiểm tra DB phản ứng -> hiệu ứng: đổi màu,
 *     kết tủa lắng, sủi bọt khí, tỏa nhiệt, cháy (Na + H₂O), khói.
 *   • Đồng bộ: teacher emit ChemAdd/ChemReset qua hub; student nhận
 *     RemoteChemAdd/RemoteChemReset và phát lại đúng animation.
 *   • Người vào sau: restore(contents) phát lại tức thì (không animation),
 *     giữ màu dung dịch + kết tủa + phương trình cuối.
 *
 * Tự quản lý mesh/labels/FX trong this.group (con của engine.specimen),
 * không đụng engine.parts -> Highlight/Explode của scene tĩnh không ảnh hưởng.
 */
import * as THREE from 'three';
import { CSS2DObject } from 'three/addons/renderers/CSS2DRenderer.js';

// ── Danh mục hóa chất (theo SGK KHTN 8-9 & Hóa học 10-12 Việt Nam) ──────────
// cat: solvent | acid | base | salt | metal | indicator (nhóm hiển thị trên kệ)
// float: chất rắn nhẹ nổi trên mặt nước (Na, K)
export const CHEMICALS = [
  // Dung môi
  { id: 'h2o',    f: 'H₂O',      name: 'Nước cất',             color: 0x9fd8ff, kind: 'liquid', cat: 'solvent' },
  // Axit
  { id: 'hcl',    f: 'HCl',      name: 'Axit clohiđric',       color: 0xbfe8ff, kind: 'liquid', cat: 'acid' },
  { id: 'h2so4',  f: 'H₂SO₄',    name: 'Axit sunfuric loãng',  color: 0xcfe8e2, kind: 'liquid', cat: 'acid' },
  { id: 'hno3',   f: 'HNO₃',     name: 'Axit nitric loãng',    color: 0xdce8c8, kind: 'liquid', cat: 'acid' },
  { id: 'ch3cooh',f: 'CH₃COOH',  name: 'Axit axetic (giấm)',   color: 0xf4ecd2, kind: 'liquid', cat: 'acid' },
  // Bazơ
  { id: 'naoh',   f: 'NaOH',     name: 'Natri hiđroxit',       color: 0xd6f5ff, kind: 'liquid', cat: 'base' },
  { id: 'caoh2',  f: 'Ca(OH)₂',  name: 'Nước vôi trong',       color: 0xe6f4ee, kind: 'liquid', cat: 'base' },
  // Muối
  { id: 'cuso4',  f: 'CuSO₄',    name: 'Đồng(II) sunfat',      color: 0x35a0ff, kind: 'liquid', cat: 'salt' },
  { id: 'agno3',  f: 'AgNO₃',    name: 'Bạc nitrat',           color: 0xe8f4ff, kind: 'liquid', cat: 'salt' },
  { id: 'nacl',   f: 'NaCl',     name: 'Natri clorua',         color: 0xdef0ff, kind: 'liquid', cat: 'salt' },
  { id: 'na2co3', f: 'Na₂CO₃',   name: 'Natri cacbonat',       color: 0xd8f0f4, kind: 'liquid', cat: 'salt' },
  { id: 'bacl2',  f: 'BaCl₂',    name: 'Bari clorua',          color: 0xe4f0ff, kind: 'liquid', cat: 'salt' },
  { id: 'fecl3',  f: 'FeCl₃',    name: 'Sắt(III) clorua',      color: 0xd9933b, kind: 'liquid', cat: 'salt' },
  { id: 'feso4',  f: 'FeSO₄',    name: 'Sắt(II) sunfat',       color: 0xa8d8b0, kind: 'liquid', cat: 'salt' },
  { id: 'ki',     f: 'KI',       name: 'Kali iotua',           color: 0xf0eccc, kind: 'liquid', cat: 'salt' },
  { id: 'pbno32', f: 'Pb(NO₃)₂', name: 'Chì(II) nitrat',       color: 0xeef0f4, kind: 'liquid', cat: 'salt' },
  { id: 'nh4cl',  f: 'NH₄Cl',    name: 'Amoni clorua',         color: 0xe8eef4, kind: 'liquid', cat: 'salt' },
  { id: 'mgcl2',  f: 'MgCl₂',    name: 'Magie clorua',         color: 0xe4f0f0, kind: 'liquid', cat: 'salt' },
  { id: 'kmno4',  f: 'KMnO₄',    name: 'Kali pemanganat',      color: 0xb24ddb, kind: 'liquid', cat: 'salt' },
  { id: 'caco3',  f: 'CaCO₃',    name: 'Đá vôi (viên)',        color: 0xd8dce2, kind: 'solid',  cat: 'salt' },
  // Kim loại
  { id: 'zn',     f: 'Zn',       name: 'Kẽm (viên)',           color: 0x9aa6b4, kind: 'solid',  cat: 'metal' },
  { id: 'fe',     f: 'Fe',       name: 'Sắt (đinh)',           color: 0x767c88, kind: 'solid',  cat: 'metal' },
  { id: 'mg',     f: 'Mg',       name: 'Magie (dải)',          color: 0xc8d0da, kind: 'solid',  cat: 'metal' },
  { id: 'al',     f: 'Al',       name: 'Nhôm (lá)',            color: 0xb8c2cc, kind: 'solid',  cat: 'metal' },
  { id: 'cu',     f: 'Cu',       name: 'Đồng (lá)',            color: 0xd97f4a, kind: 'solid',  cat: 'metal' },
  { id: 'na',     f: 'Na',       name: 'Natri (mẩu nhỏ)',      color: 0xe8edf4, kind: 'solid',  cat: 'metal', float: true },
  { id: 'k',      f: 'K',        name: 'Kali (mẩu nhỏ)',       color: 0xd0c8e8, kind: 'solid',  cat: 'metal', float: true },
  // Chỉ thị
  { id: 'quy',    f: 'Quỳ',      name: 'Quỳ tím',              color: 0x9b8ad6, kind: 'liquid', cat: 'indicator' },
  { id: 'phenol', f: 'PP',       name: 'Phenolphtalein',       color: 0xf2e8ff, kind: 'liquid', cat: 'indicator' },
];
export const CHEM_BY_ID = Object.fromEntries(CHEMICALS.map((c) => [c.id, c]));
export const CHEM_CATS = [
  ['solvent', 'DUNG MÔI'], ['acid', 'AXIT'], ['base', 'BAZƠ'],
  ['salt', 'MUỐI'], ['metal', 'KIM LOẠI'], ['indicator', 'CHỈ THỊ'],
];

// ── DB phản ứng: key = 2 id sắp xếp alphabet nối '|' ────────────────────────
// fx: color (màu dd mới) · precip (màu kết tủa) · bubbles (khí) · glow (tỏa nhiệt)
//     flame/flameColor/smoke (cháy) · dissolve (id chất rắn tan)
//     plate: [idKimLoại, màu] (lớp kim loại mới sinh bám lên bề mặt)
const RX = (eq, desc, fx = {}) => ({ eq, desc, fx });
const rxKey = (a, b) => [a, b].sort().join('|');
export const REACTIONS = {
  // ── Trung hòa axit-bazơ (KHTN 8/9) ──────────────────────────────────────
  'hcl|naoh':         RX('HCl + NaOH → NaCl + H₂O', 'Phản ứng trung hòa — tỏa nhiệt, dung dịch mất màu chỉ thị.', { color: 0xcfe8ff, glow: true }),
  'caoh2|hcl':        RX('Ca(OH)₂ + 2HCl → CaCl₂ + 2H₂O', 'Trung hòa giữa nước vôi trong và axit — tỏa nhiệt.', { color: 0xcfe8ff, glow: true }),
  'caoh2|h2so4':      RX('Ca(OH)₂ + H₂SO₄ → CaSO₄ + 2H₂O', 'Trung hòa; CaSO₄ ít tan tạo vẩn đục trắng nhẹ.', { precip: 0xf0f2f4, glow: true }),
  'hno3|naoh':        RX('HNO₃ + NaOH → NaNO₃ + H₂O', 'Phản ứng trung hòa — tỏa nhiệt.', { color: 0xcfe8ff, glow: true }),
  'ch3cooh|naoh':     RX('CH₃COOH + NaOH → CH₃COONa + H₂O', 'Axit hữu cơ (giấm) trung hòa bazơ — tỏa nhiệt nhẹ.', { color: 0xcfe8ff, glow: true }),
  // ── Kết tủa hiđroxit kim loại (KHTN 9 / Hóa 12) ─────────────────────────
  'cuso4|naoh':       RX('CuSO₄ + 2NaOH → Cu(OH)₂↓ + Na₂SO₄', 'Xuất hiện kết tủa Cu(OH)₂ màu XANH LAM.', { precip: 0x3aa0ff }),
  'fecl3|naoh':       RX('FeCl₃ + 3NaOH → Fe(OH)₃↓ + 3NaCl', 'Kết tủa Fe(OH)₃ màu NÂU ĐỎ — nhận biết ion Fe³⁺.', { precip: 0xb5532a, color: 0xe8d8b0 }),
  'feso4|naoh':       RX('FeSO₄ + 2NaOH → Fe(OH)₂↓ + Na₂SO₄', 'Kết tủa Fe(OH)₂ TRẮNG XANH, hóa nâu ngoài không khí — nhận biết Fe²⁺.', { precip: 0xb8e0cc }),
  'mgcl2|naoh':       RX('MgCl₂ + 2NaOH → Mg(OH)₂↓ + 2NaCl', 'Kết tủa Mg(OH)₂ trắng, không tan trong kiềm dư.', { precip: 0xf4f6f8 }),
  // ── Kết tủa muối — nhận biết ion (KHTN 9 / Hóa 11) ──────────────────────
  'agno3|nacl':       RX('AgNO₃ + NaCl → AgCl↓ + NaNO₃', 'Kết tủa AgCl trắng, hóa đen ngoài ánh sáng — nhận biết ion Cl⁻.', { precip: 0xf2f6ff }),
  'agno3|hcl':        RX('AgNO₃ + HCl → AgCl↓ + HNO₃', 'Kết tủa AgCl trắng — nhận biết ion Cl⁻.', { precip: 0xf2f6ff }),
  'agno3|bacl2':      RX('2AgNO₃ + BaCl₂ → 2AgCl↓ + Ba(NO₃)₂', 'Kết tủa AgCl trắng.', { precip: 0xf2f6ff }),
  'agno3|fecl3':      RX('3AgNO₃ + FeCl₃ → 3AgCl↓ + Fe(NO₃)₃', 'Kết tủa AgCl trắng trong dung dịch vàng nâu.', { precip: 0xf2f6ff }),
  'agno3|ki':         RX('AgNO₃ + KI → AgI↓ + KNO₃', 'Kết tủa AgI màu VÀNG đậm — nhận biết ion I⁻.', { precip: 0xf5d442 }),
  'ki|pbno32':        RX('Pb(NO₃)₂ + 2KI → PbI₂↓ + 2KNO₃', '"MƯA VÀNG" — kết tủa PbI₂ vàng tươi óng ánh, thí nghiệm kinh điển!', { precip: 0xffd84d }),
  'bacl2|h2so4':      RX('H₂SO₄ + BaCl₂ → BaSO₄↓ + 2HCl', 'Kết tủa BaSO₄ trắng — nhận biết gốc sunfat SO₄²⁻.', { precip: 0xffffff }),
  'bacl2|cuso4':      RX('CuSO₄ + BaCl₂ → BaSO₄↓ + CuCl₂', 'Kết tủa BaSO₄ trắng lắng trong dung dịch xanh.', { precip: 0xffffff }),
  'bacl2|na2co3':     RX('Na₂CO₃ + BaCl₂ → BaCO₃↓ + 2NaCl', 'Kết tủa BaCO₃ trắng — nhận biết gốc cacbonat.', { precip: 0xf8fafc }),
  'caoh2|na2co3':     RX('Na₂CO₃ + Ca(OH)₂ → CaCO₃↓ + 2NaOH', 'Kết tủa CaCO₃ trắng — phản ứng tái tạo xút trong công nghiệp.', { precip: 0xf8fafc }),
  // ── Axit + muối cacbonat → CO₂ (KHTN 8/9) ───────────────────────────────
  'hcl|na2co3':       RX('2HCl + Na₂CO₃ → 2NaCl + H₂O + CO₂↑', 'Sủi bọt khí CO₂ không màu, không mùi.', { bubbles: true }),
  'h2so4|na2co3':     RX('H₂SO₄ + Na₂CO₃ → Na₂SO₄ + H₂O + CO₂↑', 'Sủi bọt khí CO₂ thoát ra mạnh.', { bubbles: true }),
  'ch3cooh|na2co3':   RX('2CH₃COOH + Na₂CO₃ → 2CH₃COONa + H₂O + CO₂↑', 'Giấm + soda: sủi bọt CO₂ — thí nghiệm "núi lửa" tại nhà.', { bubbles: true }),
  'caco3|hcl':        RX('CaCO₃ + 2HCl → CaCl₂ + H₂O + CO₂↑', 'Đá vôi sủi bọt mạnh và tan dần trong axit.', { bubbles: true, dissolve: 'caco3' }),
  'caco3|hno3':       RX('CaCO₃ + 2HNO₃ → Ca(NO₃)₂ + H₂O + CO₂↑', 'Đá vôi tan trong axit nitric, sủi bọt CO₂.', { bubbles: true, dissolve: 'caco3' }),
  'caco3|ch3cooh':    RX('CaCO₃ + 2CH₃COOH → (CH₃COO)₂Ca + H₂O + CO₂↑', 'Giấm ăn mòn đá vôi — giải thích hang động đá vôi & mưa axit.', { bubbles: true, dissolve: 'caco3' }),
  // ── Axit + kim loại → H₂ (KHTN 9 / Hóa 12) ──────────────────────────────
  'hcl|zn':           RX('Zn + 2HCl → ZnCl₂ + H₂↑', 'Viên kẽm tan dần, sủi bọt khí H₂ trên bề mặt.', { bubbles: true, dissolve: 'zn' }),
  'h2so4|zn':         RX('Zn + H₂SO₄ → ZnSO₄ + H₂↑', 'Sủi bọt khí H₂, kẽm tan dần.', { bubbles: true, dissolve: 'zn' }),
  'fe|hcl':           RX('Fe + 2HCl → FeCl₂ + H₂↑', 'Sắt tan chậm, sủi bọt khí H₂, dung dịch lục nhạt.', { bubbles: true, dissolve: 'fe', color: 0xc8e8d0 }),
  'fe|h2so4':         RX('Fe + H₂SO₄ → FeSO₄ + H₂↑', 'Sắt tan trong axit loãng, giải phóng khí H₂.', { bubbles: true, dissolve: 'fe', color: 0xc8e8d0 }),
  'hcl|mg':           RX('Mg + 2HCl → MgCl₂ + H₂↑', 'Magie phản ứng MẠNH, sủi bọt H₂ dữ dội và tỏa nhiệt.', { bubbles: true, dissolve: 'mg', glow: true }),
  'h2so4|mg':         RX('Mg + H₂SO₄ → MgSO₄ + H₂↑', 'Phản ứng mạnh, tỏa nhiệt, khí H₂ thoát nhanh.', { bubbles: true, dissolve: 'mg', glow: true }),
  'al|hcl':           RX('2Al + 6HCl → 2AlCl₃ + 3H₂↑', 'Nhôm tan sau khi lớp oxit bảo vệ bị phá, sủi bọt H₂.', { bubbles: true, dissolve: 'al' }),
  // ── Kim loại + muối: dãy hoạt động hóa học (KHTN 9 / Hóa 12) ────────────
  'cuso4|fe':         RX('Fe + CuSO₄ → FeSO₄ + Cu', 'Lớp ĐỒNG ĐỎ bám lên đinh sắt, màu xanh nhạt dần — Fe đứng trước Cu.', { color: 0x9fe8c8, plate: ['fe', 0xd96a3b] }),
  'cuso4|zn':         RX('Zn + CuSO₄ → ZnSO₄ + Cu', 'Đồng đỏ bám lên viên kẽm, dung dịch nhạt màu dần.', { color: 0xbfe8e0, plate: ['zn', 0xd96a3b] }),
  'cuso4|mg':         RX('Mg + CuSO₄ → MgSO₄ + Cu', 'Đồng đỏ bám lên dải magie rất nhanh — Mg hoạt động mạnh.', { color: 0xbfe8e0, plate: ['mg', 0xd96a3b] }),
  'al|cuso4':         RX('2Al + 3CuSO₄ → Al₂(SO₄)₃ + 3Cu', 'Đồng đỏ bám lên lá nhôm, dung dịch mất màu xanh.', { color: 0xcfeee8, plate: ['al', 0xd96a3b] }),
  'agno3|cu':         RX('Cu + 2AgNO₃ → Cu(NO₃)₂ + 2Ag', 'Lớp BẠC trắng sáng bám lên lá đồng, dung dịch hóa xanh — Cu đứng trước Ag.', { color: 0x4db8e8, plate: ['cu', 0xd8dde6] }),
  'cu|fecl3':         RX('Cu + 2FeCl₃ → CuCl₂ + 2FeCl₂', 'Đồng tan trong dung dịch Fe³⁺, dung dịch chuyển xanh lục (Hóa 12).', { color: 0x3fae9e, dissolve: 'cu' }),
  // ── Kim loại kiềm + nước (Hóa 12 — thí nghiệm biểu diễn) ────────────────
  'h2o|na':           RX('2Na + 2H₂O → 2NaOH + H₂↑', 'Natri phản ứng MÃNH LIỆT: chạy trên mặt nước, nóng chảy và bốc cháy vàng!', { bubbles: true, flame: true, smoke: true, glow: true, dissolve: 'na' }),
  'h2o|k':            RX('2K + 2H₂O → 2KOH + H₂↑', 'Kali phản ứng DỮ DỘI hơn Na — bốc cháy ngọn lửa TÍM đặc trưng!', { bubbles: true, flame: true, flameColor: 0xc77dff, smoke: true, glow: true, dissolve: 'k' }),
  // ── Muối amoni + kiềm → NH₃ (Hóa 11) ────────────────────────────────────
  'naoh|nh4cl':       RX('NH₄Cl + NaOH → NaCl + NH₃↑ + H₂O', 'Khí NH₃ MÙI KHAI bay lên — nhận biết muối amoni.', { bubbles: true, smoke: true }),
  'caoh2|nh4cl':      RX('2NH₄Cl + Ca(OH)₂ → CaCl₂ + 2NH₃↑ + 2H₂O', 'Muối amoni + vôi: khí NH₃ mùi khai (điều chế NH₃ trong PTN).', { bubbles: true, smoke: true }),
  // ── Hòa tan & màu sắc ───────────────────────────────────────────────────
  'h2o|kmno4':        RX('KMnO₄ tan trong H₂O', 'Dung dịch chuyển màu TÍM đặc trưng của thuốc tím.', { color: 0xb24ddb }),
};
// Chỉ thị màu: sinh tự động cho mọi cặp axit/bazơ × quỳ tím / phenolphtalein
const ACIDS = ['hcl', 'h2so4', 'hno3', 'ch3cooh'];
const BASES = ['naoh', 'caoh2'];
for (const a of ACIDS)
  REACTIONS[rxKey(a, 'quy')] = RX(`Quỳ tím + ${CHEM_BY_ID[a].f}`, 'Quỳ tím hóa ĐỎ — nhận biết môi trường axit.', { color: 0xff5252 });
for (const b of BASES) {
  REACTIONS[rxKey(b, 'quy')] = RX(`Quỳ tím + ${CHEM_BY_ID[b].f}`, 'Quỳ tím hóa XANH — nhận biết môi trường bazơ (kiềm).', { color: 0x3f6fff });
  REACTIONS[rxKey(b, 'phenol')] = RX(`Phenolphtalein + ${CHEM_BY_ID[b].f}`, 'Phenolphtalein chuyển màu HỒNG trong môi trường bazơ.', { color: 0xff5fa8 });
}

// ── Texture chấm sáng dùng chung cho particle ───────────────────────────────
const TEX = (() => {
  let t;
  return () => {
    if (t) return t;
    const c = document.createElement('canvas'); c.width = c.height = 32;
    const x = c.getContext('2d'); const g = x.createRadialGradient(16, 16, 0, 16, 16, 16);
    g.addColorStop(0, 'rgba(255,255,255,1)'); g.addColorStop(.5, 'rgba(255,255,255,.45)'); g.addColorStop(1, 'rgba(255,255,255,0)');
    x.fillStyle = g; x.fillRect(0, 0, 32, 32);
    t = new THREE.CanvasTexture(c); t.colorSpace = THREE.SRGBColorSpace; return t;
  };
})();

/** Pool particle dùng THREE.Points; step(i,dt,pos,vel) trả false để giết hạt. */
class PointFX {
  constructor(parent, { n = 90, size = 0.05, color = 0xffffff, additive = true, opacity = 0.9, step = null }) {
    this.n = n; this.step = step;
    this.pos = new Float32Array(n * 3).fill(-999);
    this.vel = new Float32Array(n * 3);
    this.life = new Float32Array(n).fill(-1);
    this.geo = new THREE.BufferGeometry();
    this.geo.setAttribute('position', new THREE.BufferAttribute(this.pos, 3));
    this.mat = new THREE.PointsMaterial({
      size, color, map: TEX(), transparent: true, opacity, depthWrite: false, sizeAttenuation: true,
      blending: additive ? THREE.AdditiveBlending : THREE.NormalBlending,
    });
    this.obj = new THREE.Points(this.geo, this.mat);
    this.obj.renderOrder = 5; this.obj.frustumCulled = false;
    parent.add(this.obj);
    this._i = 0;
  }
  spawn(px, py, pz, vx, vy, vz, life) {
    const j = (this._i = (this._i + 1) % this.n), i = j * 3;
    this.pos[i] = px; this.pos[i + 1] = py; this.pos[i + 2] = pz;
    this.vel[i] = vx; this.vel[i + 1] = vy; this.vel[i + 2] = vz;
    this.life[j] = life;
  }
  update(dt) {
    const { pos, vel, life, n } = this;
    for (let j = 0; j < n; j++) {
      if (life[j] <= 0) continue;
      life[j] -= dt;
      const i = j * 3;
      if (life[j] <= 0 || (this.step && !this.step(i, dt, pos, vel))) { life[j] = -1; pos[i + 1] = -999; continue; }
      pos[i] += vel[i] * dt; pos[i + 1] += vel[i + 1] * dt; pos[i + 2] += vel[i + 2] * dt;
    }
    this.geo.attributes.position.needsUpdate = true;
  }
  clear() { this.life.fill(-1); for (let j = 0; j < this.n; j++) this.pos[j * 3 + 1] = -999; this.geo.attributes.position.needsUpdate = true; }
  dispose() { this.obj.parent?.remove(this.obj); this.geo.dispose(); this.mat.dispose(); }
}

const ease = (t) => t < 0.5 ? 2 * t * t : 1 - Math.pow(-2 * t + 2, 2) / 2;

export class LuminaChemLab {
  /** @param {import('./Lumina3DEngine.js').Lumina3DEngine} engine */
  constructor(engine) {
    this.engine = engine; this.hub = engine.hub;
    this.group = new THREE.Group();
    engine.specimen.add(this.group);

    // Hình học cốc (local-space của specimen)
    this.BK = { bottom: -0.78, h: 1.05, r: 0.5, liqR: 0.44, liqH: 0.88 };

    this.contents = [];        // các id hóa chất đã cho vào cốc (theo thứ tự)
    this.lastRx = null;        // phản ứng gần nhất {eq, desc, fx}
    this.level = 0;            // mức dung dịch 0..0.85
    this.chunks = {};          // id chất rắn -> mesh viên/mẩu trong cốc
    this._liqTarget = new THREE.Color(0x9fd8ff);
    this._pourQueue = []; this._pour = null;
    this._bubbleT = 0; this._precipT = 0; this._smokeT = 0; this._flameT = 0; this._glowT = 0;
    this._flameAnchor = null;
    this._disposed = false;

    this._buildBench();
    this._buildFX();
  }

  // ── Dựng bàn thí nghiệm: cốc + kệ lọ hóa chất ─────────────────────────────
  _buildBench() {
    const { BK } = this;
    const holoGlass = (c, o) => new THREE.MeshStandardMaterial({
      color: 0x06101c, emissive: c, emissiveIntensity: 0.55, metalness: 0, roughness: 1,
      transparent: true, opacity: o, depthWrite: false, side: THREE.FrontSide,
    });

    // Cốc thủy tinh hologram
    this.beaker = new THREE.Mesh(new THREE.CylinderGeometry(BK.r, BK.r * 0.92, BK.h, 40, 1, true), holoGlass(0x49c3e8, 0.16));
    this.beaker.position.y = BK.bottom + BK.h / 2;
    this.beaker.renderOrder = 3;
    const bottomDisc = new THREE.Mesh(new THREE.CircleGeometry(BK.r * 0.92, 40), holoGlass(0x49c3e8, 0.22));
    bottomDisc.rotation.x = -Math.PI / 2; bottomDisc.position.y = BK.bottom + 0.01;
    const rim = new THREE.Mesh(
      new THREE.TorusGeometry(BK.r, 0.012, 12, 64),
      new THREE.MeshBasicMaterial({ color: 0x7fe9ff, transparent: true, opacity: 0.8, blending: THREE.AdditiveBlending, depthWrite: false })
    );
    rim.position.y = BK.bottom + BK.h; rim.rotation.x = Math.PI / 2;
    this.group.add(this.beaker, bottomDisc, rim);

    // Dung dịch trong cốc (scale.y theo level)
    this.liquid = new THREE.Mesh(
      new THREE.CylinderGeometry(BK.liqR, BK.liqR * 0.95, BK.liqH, 36),
      new THREE.MeshStandardMaterial({
        color: 0x06101c, emissive: 0x9fd8ff, emissiveIntensity: 0.7, metalness: 0, roughness: 1,
        transparent: true, opacity: 0.42, depthWrite: false,
      })
    );
    this.liquid.renderOrder = 2; this.liquid.scale.y = 0.001; this.liquid.visible = false;
    this.group.add(this.liquid);

    // Kết tủa lắng đáy cốc
    this.sediment = new THREE.Mesh(
      new THREE.CylinderGeometry(BK.liqR * 0.96, BK.liqR * 0.96, 0.07, 32),
      new THREE.MeshStandardMaterial({ color: 0x06101c, emissive: 0xffffff, emissiveIntensity: 0.8, transparent: true, opacity: 0.85, depthWrite: false })
    );
    this.sediment.position.y = BK.bottom + 0.05;
    this.sediment.scale.y = 0.001; this.sediment.visible = false; this.sediment.renderOrder = 2;
    this.group.add(this.sediment);

    // Kệ lọ hóa chất: HAI hàng cung phía sau cốc (29 lọ — xen kẽ để không chạm)
    this.bottles = {};
    const rows = [{ r: 1.38, items: [] }, { r: 1.74, items: [] }];
    CHEMICALS.forEach((c, i) => rows[i % 2].items.push(c));
    rows.forEach(({ r, items }) => {
      const span = 205, start = -span / 2;
      items.forEach((chem, idx) => {
        const a = THREE.MathUtils.degToRad(start + (span / (items.length - 1)) * idx);
        const g = new THREE.Group();
        g.position.set(Math.sin(a) * r, BK.bottom, -Math.cos(a) * r);
        g.userData.chemId = chem.id;

        const body = new THREE.Mesh(
          new THREE.CylinderGeometry(0.095, 0.11, 0.28, 18),
          new THREE.MeshStandardMaterial({
            color: 0x06101c, emissive: chem.color, emissiveIntensity: 0.75, metalness: 0, roughness: 1,
            transparent: true, opacity: 0.55, depthWrite: false,
          })
        );
        body.position.y = 0.14;
        const neck = new THREE.Mesh(
          new THREE.CylinderGeometry(0.032, 0.042, 0.11, 12),
          new THREE.MeshStandardMaterial({ color: 0x0a1c2a, emissive: 0x7fe9ff, emissiveIntensity: 0.4, transparent: true, opacity: 0.5, depthWrite: false })
        );
        neck.position.y = 0.34;
        g.add(body, neck);

        // Nhãn công thức nhỏ luôn hiện trên mỗi lọ
        const el = document.createElement('div');
        el.className = 'lumina-label lumina-label--chem';
        el.textContent = chem.f;
        const lbl = new CSS2DObject(el);
        lbl.position.y = 0.48;
        g.add(lbl);

        this.group.add(g);
        this.bottles[chem.id] = g;
        g.userData.home = g.position.clone();
      });
    });
  }

  _buildFX() {
    const { BK } = this;
    this._surfaceY = () => BK.bottom + 0.05 + this.level * BK.liqH;

    // Bọt khí: nổi lên, chết tại mặt dung dịch
    this.fxBubble = new PointFX(this.group, {
      n: 110, size: 0.045, color: 0xbff4ff, opacity: 0.85,
      step: (i, dt, pos, vel) => { vel[i + 1] += 1.6 * dt; return pos[i + 1] < this._surfaceY(); },
    });
    // Kết tủa: rơi xuống, chết tại đáy
    this.fxPrecip = new PointFX(this.group, {
      n: 130, size: 0.04, color: 0xffffff, additive: false, opacity: 0.95,
      step: (i, dt, pos, vel) => { vel[i + 1] = Math.max(vel[i + 1] - 1.2 * dt, -0.5); return pos[i + 1] > BK.bottom + 0.08; },
    });
    // Khói: bay lên chậm
    this.fxSmoke = new PointFX(this.group, { n: 50, size: 0.16, color: 0x9fb6c8, opacity: 0.28, step: null });
    // Dòng rót từ lọ xuống cốc
    this.fxStream = new PointFX(this.group, {
      n: 70, size: 0.04, color: 0xbfe8ff, opacity: 0.95,
      step: (i, dt, pos, vel) => { vel[i + 1] -= 5 * dt; return pos[i + 1] > Math.max(this._surfaceY(), BK.bottom + 0.06); },
    });

    // Ngọn lửa (Na + H₂O): 3 sprite cộng màu nhấp nháy — màu đỏ cam đậm dần
    // ra ngoài để nổi rõ trên nền dung dịch sáng.
    this.flames = [];
    const FLAME_COLORS = [0xfff3b0, 0xff9a3d, 0xff5a1f];
    for (let k = 0; k < 3; k++) {
      const s = new THREE.Sprite(new THREE.SpriteMaterial({
        map: TEX(), color: FLAME_COLORS[k], transparent: true, opacity: 0,
        blending: THREE.AdditiveBlending, depthWrite: false,
      }));
      s.scale.setScalar(0.34 + k * 0.12); s.renderOrder = 6;
      this.group.add(s); this.flames.push(s);
    }
  }

  // ── API: giáo viên yêu cầu thêm hóa chất ──────────────────────────────────
  requestAdd(id) { if (this.engine.isTeacher) this.applyChem(id, { emit: true }); }

  /** Thêm hóa chất vào cốc. emit=true (teacher) sẽ phát lên hub. */
  applyChem(id, { emit = false, instant = false } = {}) {
    const chem = CHEM_BY_ID[id];
    if (!chem || this._disposed) return;
    if (this.contents.length >= 16) { this._emitState('Cốc đã đầy — hãy Rửa cốc trước khi tiếp tục.'); return; }

    if (emit) this.hub.invoke('ChemAdd', id).catch(() => {});
    const prev = [...this.contents];
    this.contents.push(id);
    const rx = this._findReaction(id, prev);

    const fire = () => {
      this._mixIn(chem, instant);
      if (rx) this._react(rx, instant);
      this._emitState();
    };
    if (instant) fire();
    else { this._emitState(); this._pourQueue.push({ chem, fire }); }
  }

  _findReaction(id, prev) {
    for (let k = prev.length - 1; k >= 0; k--) {
      const rx = REACTIONS[rxKey(id, prev[k])];
      if (rx) return rx;
    }
    return null;
  }

  _mixIn(chem, instant) {
    if (chem.kind === 'liquid') {
      const first = this.level <= 0.001;
      this.level = Math.min(this.level + 0.13, 0.85);
      const c = new THREE.Color(chem.color);
      if (first) this._liqTarget.copy(c); else this._liqTarget.lerp(c, 0.45);
      if (instant) this.liquid.material.emissive.copy(this._liqTarget);
    } else {
      this._spawnChunk(chem, instant);
    }
  }

  // ── Phản ứng: áp hiệu ứng ─────────────────────────────────────────────────
  _react(rx, instant) {
    this.lastRx = rx;
    const fx = rx.fx;
    if (fx.color !== undefined) {
      this._liqTarget.set(fx.color);
      if (instant) this.liquid.material.emissive.copy(this._liqTarget);
    }
    if (fx.precip !== undefined) {
      this.fxPrecip.mat.color.set(fx.precip);
      this.sediment.material.emissive.set(fx.precip);
      this.sediment.visible = true;
      if (instant) this.sediment.scale.y = 1;
      else this._precipT = 2.8;
    }
    if (fx.plate !== undefined) {
      // plate: [idKimLoại, màu] — lớp kim loại mới sinh bám lên bề mặt
      const [targetId, plateColor] = fx.plate;
      const m = this.chunks[targetId];
      if (m) { m.material.color.set(plateColor); m.material.emissive.set(plateColor); }
    }
    if (fx.dissolve) this._dissolve(fx.dissolve, instant);
    if (!instant) {
      if (fx.bubbles) this._bubbleT = 3.6;
      if (fx.flame) {
        this._flameT = 3.2;
        this._flameAnchor = fx.dissolve || null; // lửa bám theo mẩu kim loại đang cháy
        // Màu lửa theo kim loại: Na vàng cam (mặc định), K tím…
        this.flames[1].material.color.set(fx.flameColor || 0xff9a3d);
        this.flames[2].material.color.set(fx.flameColor || 0xff5a1f);
      }
      if (fx.smoke) this._smokeT = 3.8;
      if (fx.glow) this._glowT = 2.2;
    }
  }

  _dissolve(id, instant) {
    const m = this.chunks[id];
    if (!m) return;
    if (instant) { this.group.remove(m); m.geometry.dispose(); m.material.dispose(); delete this.chunks[id]; }
    else m.userData.dieT = 3.2;
  }

  _spawnChunk(chem, instant) {
    // Mỗi chất rắn chỉ 1 viên trong cốc (thêm lần nữa = thay viên mới)
    if (this.chunks[chem.id]) this._dissolve(chem.id, true);
    const m = new THREE.Mesh(
      new THREE.DodecahedronGeometry(0.075),
      new THREE.MeshStandardMaterial({ color: chem.color, metalness: 0.5, roughness: 0.45, emissive: chem.color, emissiveIntensity: 0.3 })
    );
    m.userData.chemId2 = chem.id;
    if (chem.float) m.userData.float = true; // kim loại kiềm nhẹ (Na, K) nổi trên mặt nước
    const jx = (Math.random() - 0.5) * 0.25, jz = (Math.random() - 0.5) * 0.25;
    if (instant) {
      m.position.set(jx, m.userData.float && this.level > 0.001 ? this._surfaceY() : this.BK.bottom + 0.11, jz);
    } else {
      m.position.set(jx * 0.4, 0.55, jz * 0.4);
      m.userData.falling = true; m.userData.vy = 0;
    }
    this.group.add(m);
    this.chunks[chem.id] = m;
  }

  // ── Reset / restore ───────────────────────────────────────────────────────
  resetBench(emit) {
    if (emit && this.engine.isTeacher) this.hub.invoke('ChemReset').catch(() => {});
    this._resetLocal();
    this._emitState();
  }
  _resetLocal() {
    this.contents = []; this.lastRx = null; this.level = 0;
    this._liqTarget.set(0x9fd8ff);
    this._pourQueue = [];
    if (this._pour) { this._endPour(); }
    this._bubbleT = this._precipT = this._smokeT = this._flameT = this._glowT = 0;
    this._flameAnchor = null;
    this.fxBubble.clear(); this.fxPrecip.clear(); this.fxSmoke.clear(); this.fxStream.clear();
    this.sediment.visible = false; this.sediment.scale.y = 0.001;
    Object.values(this.chunks).forEach((m) => { this.group.remove(m); m.geometry.dispose(); m.material.dispose(); });
    this.chunks = {};
    this.flames.forEach((s) => (s.material.opacity = 0));
  }
  /** Người vào sau: phát lại danh sách hóa chất tức thì (không animation). */
  restore(list) {
    this._resetLocal();
    list.forEach((id) => this.applyChem(id, { instant: true }));
    this._emitState();
  }

  state() {
    return { contents: [...this.contents], last: this.lastRx ? { eq: this.lastRx.eq, desc: this.lastRx.desc } : null };
  }
  _emitState(note) { this.engine.onChemState?.({ ...this.state(), note: note || null }); }

  // ── Animation rót ─────────────────────────────────────────────────────────
  _startPour(job) {
    const bottle = this.bottles[job.chem.id];
    if (!bottle) { job.fire(); return; }
    this._pour = { job, bottle, t: 0, from: bottle.userData.home.clone(), fired: false };
    this.fxStream.mat.color.set(job.chem.color);
  }
  _endPour() {
    if (!this._pour) return;
    const b = this._pour.bottle;
    b.position.copy(b.userData.home); b.rotation.set(0, 0, 0);
    this._pour = null;
  }
  _stepPour(dt) {
    const P = this._pour; P.t += dt;
    const b = P.bottle, T1 = 0.35, T2 = 1.15, T3 = 1.5;
    const target = new THREE.Vector3(0.1, 0.62, 0);
    if (P.t < T1) {
      const k = ease(P.t / T1);
      b.position.lerpVectors(P.from, target, k);
      b.rotation.z = -1.9 * k;
    } else if (P.t < T2) {
      b.position.copy(target); b.rotation.z = -1.9;
      // Dòng rót: hạt rơi từ miệng lọ xuống cốc (chất rắn = thả viên thay vì dòng)
      if (P.job.chem.kind === 'liquid') {
        for (let k = 0; k < 3; k++)
          this.fxStream.spawn(-0.06 + (Math.random() - 0.5) * 0.03, 0.52, (Math.random() - 0.5) * 0.03,
            (Math.random() - 0.5) * 0.1, -1.4, (Math.random() - 0.5) * 0.1, 1.2);
      }
      if (!P.fired && P.t > T1 + 0.3) { P.fired = true; P.job.fire(); }
    } else if (P.t < T3) {
      const k = ease((P.t - T2) / (T3 - T2));
      b.position.lerpVectors(target, P.from, k);
      b.rotation.z = -1.9 * (1 - k);
    } else {
      if (!P.fired) P.job.fire();
      this._endPour();
    }
  }

  // ── Frame update (gọi từ engine) ──────────────────────────────────────────
  update(dt, t) {
    if (this._disposed) return;
    const { BK } = this;

    if (!this._pour && this._pourQueue.length) this._startPour(this._pourQueue.shift());
    if (this._pour) this._stepPour(dt);

    // Dung dịch: mức + màu mượt
    const targetScale = Math.max(this.level, 0.001);
    this.liquid.scale.y += (targetScale - this.liquid.scale.y) * Math.min(dt * 5, 1);
    this.liquid.position.y = BK.bottom + 0.05 + (BK.liqH * this.liquid.scale.y) / 2;
    this.liquid.visible = this.level > 0.001;
    this.liquid.material.emissive.lerp(this._liqTarget, Math.min(dt * 3, 1));

    // Tỏa nhiệt: dung dịch loé sáng rồi dịu lại (giữ vừa phải để lửa nổi rõ)
    if (this._glowT > 0) { this._glowT -= dt; }
    this.liquid.material.emissiveIntensity = 0.7 + Math.max(this._glowT, 0) * 0.3;

    // Kết tủa: rơi + sediment dày lên
    if (this._precipT > 0) {
      this._precipT -= dt;
      const sy = this._surfaceY();
      for (let k = 0; k < 2; k++) {
        const r = Math.random() * BK.liqR * 0.8, a = Math.random() * Math.PI * 2;
        this.fxPrecip.spawn(Math.cos(a) * r, sy - 0.05, Math.sin(a) * r, 0, -0.1, 0, 3);
      }
      this.sediment.scale.y = Math.min(this.sediment.scale.y + dt * 0.45, 1);
    }
    // Bọt khí
    if (this._bubbleT > 0) {
      this._bubbleT -= dt;
      for (let k = 0; k < 2; k++) {
        const r = Math.random() * BK.liqR * 0.85, a = Math.random() * Math.PI * 2;
        this.fxBubble.spawn(Math.cos(a) * r, BK.bottom + 0.1, Math.sin(a) * r,
          (Math.random() - 0.5) * 0.12, 0.3 + Math.random() * 0.2, (Math.random() - 0.5) * 0.12, 3);
      }
    }
    // Lửa + khói (vị trí bám theo mẩu kim loại đang cháy nếu có)
    const anchor = this._flameAnchor ? this.chunks[this._flameAnchor] : null;
    const fy = this._surfaceY() + 0.1;
    const fxp = anchor ? anchor.position : { x: 0, z: 0 };
    if (this._flameT > 0) {
      this._flameT -= dt;
      this.flames.forEach((s, i) => {
        s.position.set(fxp.x + Math.sin(t * 17 + i * 2) * 0.05, fy + 0.16 + i * 0.11, fxp.z + Math.cos(t * 13 + i) * 0.05);
        s.material.opacity = Math.min(this._flameT, 1) * (0.8 + Math.sin(t * 23 + i * 1.7) * 0.2);
        s.scale.setScalar(0.34 + i * 0.12 + Math.sin(t * 19 + i) * 0.06);
      });
    } else this.flames.forEach((s) => (s.material.opacity = 0));
    if (this._smokeT > 0) {
      this._smokeT -= dt;
      if (Math.random() < dt * 14)
        this.fxSmoke.spawn(fxp.x, fy + 0.12, fxp.z, (Math.random() - 0.5) * 0.1, 0.4, (Math.random() - 0.5) * 0.1, 2.2);
    }

    // Chất rắn: rơi vào cốc / nổi (Na) / tan dần
    for (const id in this.chunks) {
      const m = this.chunks[id];
      if (m.userData.falling) {
        m.userData.vy -= 4 * dt;
        m.position.y += m.userData.vy * dt;
        const restY = m.userData.float && this.level > 0.001 ? this._surfaceY() : BK.bottom + 0.11;
        if (m.position.y <= restY) { m.position.y = restY; m.userData.falling = false; }
      } else if (m.userData.float && this.level > 0.001) {
        // Na chạy vòng trên mặt nước
        const sp = m.userData.dieT !== undefined ? 2.6 : 0.6;
        m.position.set(Math.cos(t * sp) * 0.2, this._surfaceY(), Math.sin(t * sp) * 0.2);
      }
      if (m.userData.dieT !== undefined) {
        m.userData.dieT -= dt;
        const s = Math.max(m.userData.dieT / 3.2, 0.02);
        m.scale.setScalar(s);
        if (m.userData.dieT <= 0) { this.group.remove(m); m.geometry.dispose(); m.material.dispose(); delete this.chunks[id]; }
      }
      m.rotation.y += dt * 0.6;
    }

    this.fxBubble.update(dt); this.fxPrecip.update(dt); this.fxSmoke.update(dt); this.fxStream.update(dt);
  }

  dispose() {
    this._disposed = true;
    this._resetLocal();
    this.fxBubble.dispose(); this.fxPrecip.dispose(); this.fxSmoke.dispose(); this.fxStream.dispose();
    this.group.traverse((c) => {
      if (c.isCSS2DObject) c.element?.remove?.();
      c.geometry?.dispose?.(); c.material?.dispose?.();
    });
    this.engine.specimen.remove(this.group);
  }
}
