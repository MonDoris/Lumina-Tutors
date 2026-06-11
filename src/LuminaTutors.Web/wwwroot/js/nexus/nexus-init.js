/**
 * nexus-init.js — bootstrap "Lumina Research Facility".
 * Ráp Lumina3DEngine (PBR) + LuminaInteraction (laser) + LuminaStreamManager (WebRTC)
 * + Clinical Luxury tablet UI + Dual-Comm video widget + spatial audio.
 */
// NOTE: các ESM nội bộ import nhau KHÔNG qua asp-append-version, dễ bị trình
// duyệt giữ bản cache cũ. Dùng ?v=<NEXUS_VER> đồng nhất để buộc tải lại khi sửa.
// Bump số này mỗi khi đổi 1 trong các module /js/nexus/*.js.
import { Lumina3DEngine } from './Lumina3DEngine.js?v=2';
import { attachPositionalAudio } from './LuminaInteraction.js?v=2';
import { LuminaStreamManager } from './LuminaStreamManager.js?v=2';
import { CHEMICALS, CHEM_BY_ID, CHEM_CATS } from './LuminaChemLab.js?v=2';
import { SIMS } from './LuminaSimLab.js?v=2';

const CFG = window.NEXUS_CONFIG || { roomId: 'nexus-demo', isTeacher: false, displayName: 'Học viên' };

const $ = (id) => document.getElementById(id);
const els = {
  boot: $('nexusBoot'), bootLn: $('nexusBootLines'), canvas: $('nexusCanvas'),
  statusDot: $('statusDot'), statusTxt: $('statusTxt'),
  hudFps: $('hudFps'), hudExplode: $('hudExplode'),
  btnHighlight: $('btnHighlight'), btnLabels: $('btnLabels'), btnReset: $('btnReset'),
  explode: $('explode'), explodeVal: $('explodeVal'),
  subjectSel: $('subjectSel'), sceneSel: $('sceneSel'), specimenLabel: $('specimenLabel'),
  chemTray: $('chemTray'), chemGrid: $('chemGrid'), chemPanel: $('chemPanel'),
  chemContents: $('chemContents'), chemEq: $('chemEq'), chemDesc: $('chemDesc'),
  simControls: $('simControls'), simCtrlBody: $('simCtrlBody'),
  simPanel: $('simPanel'), simTitle: $('simTitle'), simLines: $('simLines'),
  feed: $('teacherFeed'), camPlaceholder: $('camPlaceholder'), commName: $('commName'),
  commGlow: document.querySelector('.commwidget__glow'),
};

// ── Thí nghiệm theo môn học (đồng bộ nhãn với Lumina3DEngine.SCENE_LABELS) ─────
const SUBJECTS = [
  ['chemistry', '⚗️ Hóa học'],
  ['physics',   '⚙️ Vật lý'],
  ['biology',   '🧬 Sinh học'],
  ['math',      '📐 Toán học'],
];
const SCENES_BY_SUBJECT = {
  chemistry: [
    ['reaction', 'Bàn phản ứng · TƯƠNG TÁC'],
    ['titration', 'Chuẩn độ axit-bazơ'], ['electrolysis', 'Điện phân nước'],
    ['distillation', 'Chưng cất đơn giản'], ['molecule', 'Phân tử nước H₂O'],
  ],
  physics: [
    ['pendulum-lab', 'Con lắc đơn · TƯƠNG TÁC'],
    ['optics-lab', 'Khúc xạ ánh sáng · TƯƠNG TÁC'],
    ['pendulum', 'Con lắc đơn'], ['spring', 'Con lắc lò xo'], ['optics', 'Lăng kính · Khúc xạ'],
  ],
  biology: [
    ['photosynthesis-lab', 'Quang hợp · TƯƠNG TÁC'],
    ['cell', 'Tế bào nhân thực'], ['dna', 'Cấu trúc DNA'], ['photosynthesis', 'Lục lạp · Quang hợp'],
  ],
  math: [
    ['vectors-lab', 'Vector 3D · TƯƠNG TÁC'],
    ['polyhedron', 'Đa diện đều'], ['vectors', 'Vector 3D'],
  ],
};
const subjectOfScene = (scene) =>
  Object.keys(SCENES_BY_SUBJECT).find((s) => SCENES_BY_SUBJECT[s].some(([v]) => v === scene)) || null;

// ── Boot typing ─────────────────────────────────────────────────────────────
const bootLines = [
  'CALIBRATE EMITTERS .......... <span class="ok">OK</span>',
  'ALIGN PROJECTION FIELD ...... <span class="ok">OK</span>',
  'RENDER HOLOGRAM ............. <span class="ok">OK</span>',
];
let bi = 0;
(function typeBoot() {
  if (bi < bootLines.length && els.bootLn) { els.bootLn.innerHTML += bootLines[bi++] + '<br/>'; setTimeout(typeBoot, 300); }
})();

async function main() {
  // ── SignalR ────────────────────────────────────────────────────────────
  const hub = new signalR.HubConnectionBuilder()
    .withUrl('/hubs/lumina-rtc').withAutomaticReconnect()
    .configureLogging(signalR.LogLevel.Warning).build();

  // ── 3D Engine (tự tạo LuminaInteraction bên trong) ──────────────────────
  const startScene = CFG.initialScene || firstSceneOf(CFG.subject) || 'cell';
  const engine = new Lumina3DEngine(els.canvas, {
    isTeacher: CFG.isTeacher, hub, roomId: CFG.roomId, scene: startScene,
  });
  window.__nexus = { engine, hub }; // debug handle (console/DevTools)
  engine.onTelemetry = ({ fps, explode }) => {
    if (els.hudFps) els.hudFps.textContent = String(fps).padStart(2, '0');
    if (els.hudExplode) els.hudExplode.textContent = explode + '%';
  };
  // Nhãn hologram hiện tại -> readout SPECIMEN/HOLOGRAM trên tablet
  // + bật/tắt panel theo loại scene: bàn phản ứng | thí nghiệm tham số | tĩnh
  engine.onSceneLabel = (label) => {
    if (els.specimenLabel) els.specimenLabel.textContent = label;
    const isRx = engine.scene2 === 'reaction';
    const isSim = !!SIMS[engine.scene2];
    els.chemTray?.classList.toggle('show', isRx);
    els.chemPanel?.classList.toggle('show', isRx);
    els.simControls?.classList.toggle('show', isSim && CFG.isTeacher);
    els.simPanel?.classList.toggle('show', isSim);
    if (isSim && CFG.isTeacher) buildSimControls(engine);
  };
  // Bảng kết quả: phản ứng hóa học / thí nghiệm tham số (cả lớp đều thấy)
  engine.onChemState = (st) => renderChemPanel(st);
  engine.onSimState = (st) => renderSimPanel(st);
  engine.onSceneLabel(engine.sceneLabel(startScene)); // đồng bộ ngay lần đầu
  engine.onChemState(engine.chemlab ? engine.chemlab.state() : null);
  engine.onSimState(engine.simlab ? engine.simlab.readout() : null);

  // ── Giáo viên: bộ chọn môn học + thí nghiệm + kệ hóa chất ───────────────
  if (CFG.isTeacher) { wirePickers(engine, startScene); buildChemTray(engine); }

  // ── Stream manager (WebRTC + spatial audio) ─────────────────────────────
  const streams = new LuminaStreamManager(hub, engine.getListener(), {
    // Audio remote (giáo viên) -> gắn PositionalAudio vào specimen (zoom gần = to hơn)
    onRemoteAudio: (peerId, stream) => attachPositionalAudio(engine.specimen, engine.getListener(), stream),
    // Video remote (giáo viên) -> đổ vào comm widget
    onPeerVideo: (peerId, _el, stream) => showFeed(stream),
  });

  // ── Roster ───────────────────────────────────────────────────────────────
  hub.on('RoomJoined', ({ peers, role, scene, chem, sims }) => {
    setStatus(true, 'SYNC 20Hz · ' + role.toUpperCase());
    // Người vào sau (học sinh): đồng bộ đúng thí nghiệm giáo viên đang chiếu
    if (scene && !CFG.isTeacher) engine.setScene(scene, false);
    // Bàn phản ứng: phát lại các hóa chất đã cho vào cốc (tức thì, không animation)
    if (Array.isArray(chem) && chem.length) engine.chemlab?.restore(chem);
    // Thí nghiệm tham số: khôi phục các giá trị thanh trượt giáo viên đang đặt
    if (sims && Object.keys(sims).length) engine.simlab?.restore(sims);
    peers.forEach((p) => streams.subscribe(p.peerId));
  });
  hub.on('NewPublisher', (peer) => streams.subscribe(peer.peerId));
  hub.onreconnecting(() => setStatus(false, 'RECONNECTING'));
  hub.onreconnected(() => setStatus(true, 'SYNC 20Hz'));
  hub.onclose(() => setStatus(false, 'OFFLINE'));

  try {
    await hub.start();
    await hub.invoke('JoinRoom', CFG.roomId);
    // Đăng ký thí nghiệm khởi tạo lên server (lúc wirePickers gọi thì hub chưa start)
    if (CFG.isTeacher) hub.invoke('SetScene', engine.scene2).catch(() => {});
  } catch (err) { console.error('[Nexus] Hub start failed:', err); setStatus(false, 'HUB ERROR'); }

  // ── Teacher: publish camera + glow theo âm lượng ────────────────────────
  if (CFG.isTeacher) {
    try {
      const local = await streams.publish({ video: true, audio: true });
      showFeed(local);
      streams.attachLocalAnalyser();
      pumpGlow(() => streams.getLocalLevel());
    } catch (err) {
      console.warn('[Nexus] Camera/mic không khả dụng:', err.message);
      if (els.camPlaceholder) els.camPlaceholder.textContent = 'CAMERA OFFLINE';
    }
  }

  // ── Tablet tools (teacher) ───────────────────────────────────────────────
  if (CFG.isTeacher) {
    els.btnLabels?.addEventListener('click', () => {
      const on = !els.btnLabels.classList.contains('is-on');
      els.btnLabels.classList.toggle('is-on', on);
      engine.setLabels(on, true);
    });
    els.btnHighlight?.addEventListener('click', () => {
      // Tô sáng bộ phận đầu tiên của thí nghiệm hiện tại + nhắc click trực tiếp lên bộ phận.
      els.btnHighlight.classList.toggle('is-on');
      const firstPart = Object.keys(engine.parts)[0];
      if (firstPart) engine.toggleHighlight(firstPart);
    });
    els.btnReset?.addEventListener('click', () => {
      engine.reset();
      els.explode.value = 0; els.explodeVal.textContent = '0%';
      els.btnLabels?.classList.remove('is-on'); els.btnHighlight?.classList.remove('is-on');
      if (engine.simlab) buildSimControls(engine); // đồng bộ thanh trượt về mặc định
    });
    els.explode?.addEventListener('input', () => {
      const v = parseInt(els.explode.value, 10);
      els.explodeVal.textContent = v + '%';
      engine.setExplode(v / 100, true);
    });
  }

  setTimeout(() => els.boot?.classList.add('done'), 1500);
}

// ── Helpers ──────────────────────────────────────────────────────────────────
const firstSceneOf = (subject) => (SCENES_BY_SUBJECT[subject] || [])[0]?.[0] || null;

/** Bộ chọn Môn học + Thí nghiệm (giáo viên). Đổi -> engine.setScene(emit) -> đồng bộ phòng. */
function wirePickers(engine, startScene) {
  const subjSel = els.subjectSel, sceneSel = els.sceneSel;
  if (!subjSel || !sceneSel) return;

  subjSel.innerHTML = SUBJECTS.map(([v, l]) => `<option value="${v}">${l}</option>`).join('');
  const startSubject = subjectOfScene(startScene) || CFG.subject || 'biology';
  subjSel.value = startSubject;

  // Đổi môn -> nạp danh sách thí nghiệm của môn đó (giữ thí nghiệm khởi tạo nếu khớp)
  const fillScenes = (subject, preferred) => {
    const list = SCENES_BY_SUBJECT[subject] || [];
    sceneSel.innerHTML = list.map(([v, l]) => `<option value="${v}">${l}</option>`).join('');
    const chosen = (preferred && list.some(([v]) => v === preferred)) ? preferred : list[0]?.[0];
    if (chosen) { sceneSel.value = chosen; engine.setScene(chosen, true); }
  };

  fillScenes(startSubject, startScene);
  subjSel.addEventListener('change', () => fillScenes(subjSel.value));
  sceneSel.addEventListener('change', () => { engine.setScene(sceneSel.value, true); resetTabletUI(); });
}

/** Đồng bộ trạng thái UI tablet với specimen vừa dựng lại (explode về 0, tắt highlight). */
function resetTabletUI() {
  if (els.explode) els.explode.value = 0;
  if (els.explodeVal) els.explodeVal.textContent = '0%';
  els.btnHighlight?.classList.remove('is-on');
}

/** Kệ hóa chất (giáo viên): chip nhóm theo loại + nút rửa cốc. */
function buildChemTray(engine) {
  if (!els.chemGrid) return;
  const hex = (c) => '#' + c.toString(16).padStart(6, '0');
  const chip = (c) =>
    `<button type="button" class="chem-chip" data-chem="${c.id}" style="--cc:${hex(c.color)}">
       <b>${c.f}</b><span>${c.name}</span>
     </button>`;
  els.chemGrid.innerHTML = CHEM_CATS.map(([cat, label]) => {
    const chips = CHEMICALS.filter((c) => c.cat === cat).map(chip).join('');
    return chips ? `<div class="chemtray__cat">${label}</div>${chips}` : '';
  }).join('') +
    `<button type="button" class="chem-chip chem-chip--rinse" data-rinse="1">
       <b>🚿 Rửa cốc</b><span>Đổ bỏ &amp; làm sạch</span>
     </button>`;
  els.chemGrid.addEventListener('click', (e) => {
    const b = e.target.closest('button');
    if (!b || !engine.chemlab) return;
    if (b.dataset.rinse) engine.chemlab.resetBench(true);
    else engine.chemlab.requestAdd(b.dataset.chem);
  });
}

/** Thanh trượt tham số thí nghiệm (giáo viên): kéo -> engine.simlab.setParam(emit). */
function buildSimControls(engine) {
  const sim = engine.simlab, body = els.simCtrlBody;
  if (!sim || !body) return;
  const fmt = (v) => (Math.round(v * 100) / 100).toString();
  body.innerHTML = sim.def.params.map((p) => {
    const val = sim.params[p.key];
    if (p.type === 'select') {
      const opts = p.options.map((o) => `<option value="${o.value}" ${o.value === val ? 'selected' : ''}>${o.label}</option>`).join('');
      return `<label class="simrow"><span class="simrow__lbl">${p.label}</span>
                <select class="holo-sel simctrl-input" data-key="${p.key}">${opts}</select></label>`;
    }
    return `<label class="simrow">
              <span class="simrow__lbl">${p.label} <b data-val="${p.key}">${fmt(val)}${p.unit ? ' ' + p.unit : ''}</b></span>
              <input type="range" class="simctrl-input" data-key="${p.key}" min="${p.min}" max="${p.max}" step="${p.step}" value="${val}"></label>`;
  }).join('');
  body.querySelectorAll('.simctrl-input').forEach((inp) => {
    inp.addEventListener('input', () => {
      const key = inp.dataset.key, v = +inp.value;
      const pdef = sim.def.params.find((p) => p.key === key);
      const vb = body.querySelector(`[data-val="${key}"]`);
      if (vb) vb.textContent = fmt(v) + (pdef?.unit ? ' ' + pdef.unit : '');
      engine.simlab.setParam(key, v, true);
    });
  });
}

/** Bảng kết quả thí nghiệm tham số (mọi vai trò đều thấy). */
function renderSimPanel(st) {
  if (!els.simPanel) return;
  if (!st) { if (els.simTitle) els.simTitle.textContent = '—'; if (els.simLines) els.simLines.innerHTML = ''; return; }
  if (els.simTitle) els.simTitle.textContent = st.title || '';
  if (els.simLines)
    els.simLines.innerHTML = (st.lines || [])
      .map((l) => `<div class="simpanel__row${l.wide ? ' simpanel__row--wide' : ''}"><span>${l.k}</span><b>${l.v}</b></div>`)
      .join('');
}

/** Bảng phương trình phản ứng (mọi vai trò đều thấy). */
function renderChemPanel(st) {
  if (!els.chemPanel) return;
  if (!st) {
    if (els.chemContents) els.chemContents.textContent = 'Cốc rỗng';
    if (els.chemEq) els.chemEq.textContent = '—';
    if (els.chemDesc) els.chemDesc.textContent = 'Chọn hóa chất trên kệ để bắt đầu thí nghiệm.';
    return;
  }
  if (els.chemContents)
    els.chemContents.textContent = st.contents.length
      ? 'Trong cốc: ' + st.contents.map((id) => CHEM_BY_ID[id]?.f || id).join('  +  ')
      : 'Cốc rỗng';
  if (els.chemEq) els.chemEq.textContent = st.last ? st.last.eq : '—';
  if (els.chemDesc)
    els.chemDesc.textContent = st.note || (st.last ? st.last.desc : 'Chưa có phản ứng — chọn hóa chất để bắt đầu.');
}

function showFeed(stream) {
  if (!els.feed) return;
  els.feed.srcObject = stream; els.feed.style.display = 'block';
  if (els.camPlaceholder) els.camPlaceholder.style.display = 'none';
}
function setStatus(online, text) {
  els.statusDot?.classList.toggle('online', online);
  if (els.statusTxt) els.statusTxt.textContent = text;
}
function pumpGlow(getLevel) {
  if (!els.commGlow) return;
  (function loop() {
    document.documentElement.style.setProperty('--glow', getLevel().toFixed(3));
    requestAnimationFrame(loop);
  })();
}

main();
