/**
 * nexus-init.js — bootstrap "Lumina Research Facility".
 * Ráp Lumina3DEngine (PBR) + LuminaInteraction (laser) + LuminaStreamManager (WebRTC)
 * + Clinical Luxury tablet UI + Dual-Comm video widget + spatial audio.
 */
// NOTE: các ESM nội bộ import nhau KHÔNG qua asp-append-version, dễ bị trình
// duyệt giữ bản cache cũ. Dùng ?v=<NEXUS_VER> đồng nhất để buộc tải lại khi sửa.
// Bump số này mỗi khi đổi 1 trong các module /js/nexus/*.js.
import { Lumina3DEngine } from './Lumina3DEngine.js?v=3';
import { attachPositionalAudio } from './LuminaInteraction.js?v=3';
import { LuminaStreamManager } from './LuminaStreamManager.js?v=3';
import { CHEMICALS, CHEM_BY_ID, CHEM_CATS } from './LuminaChemLab.js?v=3';
import { SIMS } from './LuminaSimLab.js?v=3';

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
  rosterToggle: $('rosterToggle'), rosterCount: $('rosterCount'), rosterHeadCount: $('rosterHeadCount'),
  rosterPanel: $('rosterPanel'), rosterList: $('rosterList'), rosterToast: $('rosterToast'),
  tablet: document.querySelector('.tablet'), tabletCollapse: $('tabletCollapse'),
  nxMic: $('nxMic'), nxMicIc: $('nxMicIc'), nxCam: $('nxCam'), nxCamIc: $('nxCamIc'),
  nxHand: $('nxHand'), nxRec: $('nxRec'), nxRecIc: $('nxRecIc'), nxLeave: $('nxLeave'),
  nxRecBadge: $('nxRecBadge'), nxRecTime: $('nxRecTime'),
};

// ── Hộp thoại xác nhận đẹp (tông tối) — thay cho confirm() mặc định ───────────
function nexusConfirm(opts) {
  opts = opts || {};
  const isDanger = !!opts.danger;
  return new Promise(resolve => {
    if (!document.getElementById('nx-confirm-style')) {
      const st = document.createElement('style');
      st.id = 'nx-confirm-style';
      st.textContent =
        '#nx-confirm{position:fixed;inset:0;z-index:99999;display:flex;align-items:center;justify-content:center;' +
        'background:rgba(5,10,22,.64);backdrop-filter:blur(5px);opacity:0;pointer-events:none;transition:opacity .18s}' +
        '#nx-confirm.show{opacity:1;pointer-events:auto}' +
        '.nxc-card{width:min(92vw,380px);background:linear-gradient(160deg,#0e1830,#0a1322);' +
        'border:1px solid rgba(99,160,255,.22);border-radius:18px;padding:26px 24px 20px;text-align:center;color:#dbe7fb;' +
        'box-shadow:0 30px 70px rgba(0,0,0,.55);transform:scale(.9) translateY(10px);transition:transform .2s cubic-bezier(.34,1.3,.64,1)}' +
        '#nx-confirm.show .nxc-card{transform:none}' +
        '.nxc-ic{width:58px;height:58px;border-radius:50%;display:grid;place-items:center;margin:0 auto 16px;font-size:1.5rem}' +
        '.nxc-ic.danger{background:rgba(239,68,68,.16)}.nxc-ic.info{background:rgba(59,130,246,.16)}' +
        '.nxc-title{font-size:1.1rem;font-weight:800;color:#fff;margin:0 0 6px}' +
        '.nxc-msg{font-size:.88rem;color:#9fb3cf;line-height:1.55;margin:0 0 22px}' +
        '.nxc-actions{display:flex;gap:10px}' +
        '.nxc-btn{flex:1;padding:11px 16px;border-radius:12px;font-size:.9rem;font-weight:700;cursor:pointer;border:1px solid transparent;transition:.15s}' +
        '.nxc-cancel{background:rgba(255,255,255,.07);color:#cdd9ea;border-color:rgba(255,255,255,.16)}' +
        '.nxc-cancel:hover{background:rgba(255,255,255,.14)}' +
        '.nxc-ok{background:#dc2626;color:#fff}.nxc-ok:hover{background:#b91c1c}' +
        '.nxc-ok.info{background:#2563eb}.nxc-ok.info:hover{background:#1d4ed8}';
      document.head.appendChild(st);
    }
    const ov = document.createElement('div');
    ov.id = 'nx-confirm';
    ov.innerHTML =
      '<div class="nxc-card" role="dialog" aria-modal="true">' +
        '<div class="nxc-ic ' + (isDanger ? 'danger' : 'info') + '">' + (isDanger ? '⚠️' : 'ℹ️') + '</div>' +
        '<div class="nxc-title">' + (opts.title || 'Xác nhận') + '</div>' +
        '<div class="nxc-msg">' + (opts.message || '') + '</div>' +
        '<div class="nxc-actions">' +
          '<button class="nxc-btn nxc-cancel" type="button">' + (opts.cancelText || 'Hủy') + '</button>' +
          '<button class="nxc-btn nxc-ok ' + (isDanger ? '' : 'info') + '" type="button">' + (opts.okText || 'Đồng ý') + '</button>' +
        '</div>' +
      '</div>';
    document.body.appendChild(ov);
    requestAnimationFrame(() => ov.classList.add('show'));
    const close = (val) => { ov.classList.remove('show'); setTimeout(() => ov.remove(), 180); resolve(val); };
    ov.querySelector('.nxc-cancel').addEventListener('click', () => close(false));
    ov.querySelector('.nxc-ok').addEventListener('click', () => close(true));
    ov.addEventListener('click', e => { if (e.target === ov) close(false); });
    document.addEventListener('keydown', function esc(e) {
      if (e.key === 'Escape') { close(false); document.removeEventListener('keydown', esc); }
    });
    requestAnimationFrame(() => ov.querySelector('.nxc-ok').focus());
  });
}

// ── Mobile chrome: thu gọn bảng điều khiển để vùng 3D thoáng, dễ nhìn thí nghiệm.
// Trên màn hình hẹp (≤760px) mặc định gập; chạm chevron để mở/đóng.
(function initMobileChrome() {
  const tablet = els.tablet;
  if (!tablet) return;
  const mq = window.matchMedia('(max-width: 760px)');
  const apply = (matches) => tablet.classList.toggle('is-collapsed', matches);
  apply(mq.matches);
  const onChange = (e) => apply(e.matches);
  if (mq.addEventListener) mq.addEventListener('change', onChange);
  else if (mq.addListener) mq.addListener(onChange);
  els.tabletCollapse?.addEventListener('click', () => tablet.classList.toggle('is-collapsed'));
})();

// ── Danh sách người trong phòng (roster) ──────────────────────────────────────
// peerId -> { name, role }. Dựng từ RoomJoined.roster (+ self), cập nhật realtime
// qua PeerJoined / PeerLeft. Cả giáo viên lẫn học sinh đều thấy.
const roster = new Map();
let selfPeerId = null;
let joinToastTimer = null;

// ── Thí nghiệm theo môn học (đồng bộ nhãn với Lumina3DEngine.SCENE_LABELS) ─────
const SUBJECTS = [
  ['chemistry', '⚗️ Hóa học'],
  ['physics',   '⚙️ Vật lý'],
  ['biology',   '🧬 Sinh học'],
  ['math',      '📐 Toán học'],
];
const SCENES_BY_SUBJECT = {
  chemistry: [
    ['reaction', 'Bàn phản ứng hóa học'],
  ],
  physics: [
    ['pendulum-lab', 'Con lắc đơn'],
    ['spring-lab', 'Con lắc lò xo'],
    ['optics-lab', 'Khúc xạ ánh sáng'],
    ['circuit-lab', 'Định luật Ôm'],
  ],
  biology: [
    ['photosynthesis-lab', 'Quang hợp'],
    ['osmosis-lab', 'Thẩm thấu tế bào'],
  ],
  math: [
    ['vectors-lab', 'Vector 3D'],
    ['parabola-lab', 'Đồ thị Parabol'],
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
  // Chỉ mở thí nghiệm TƯƠNG TÁC (mô hình tĩnh đã bỏ khỏi danh sách chọn).
  const startScene = (() => {
    const valid = new Set(Object.values(SCENES_BY_SUBJECT).flat().map(([v]) => v));
    if (CFG.initialScene && valid.has(CFG.initialScene)) return CFG.initialScene;
    return firstSceneOf(CFG.subject) || 'reaction';
  })();
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
  let lastRemoteAudio = null;   // tiếng giáo viên (để học sinh ghi hình kèm âm thanh)
  const streams = new LuminaStreamManager(hub, engine.getListener(), {
    // Audio remote (giáo viên) -> gắn PositionalAudio vào specimen (zoom gần = to hơn)
    onRemoteAudio: (peerId, stream) => { lastRemoteAudio = stream; attachPositionalAudio(engine.specimen, engine.getListener(), stream); },
    // Video remote (giáo viên) -> đổ vào comm widget
    onPeerVideo: (peerId, _el, stream) => showFeed(stream),
  });

  // ── Roster ───────────────────────────────────────────────────────────────
  hub.on('RoomJoined', ({ selfId, peers, roster: initialRoster, role, scene, chem, sims }) => {
    setStatus(true, 'SYNC 20Hz · ' + role.toUpperCase());
    // Dựng lại danh sách người trong phòng: bản thân + những người đã có sẵn.
    selfPeerId = selfId;
    roster.clear();
    roster.set(selfId, { name: CFG.displayName, role });
    (initialRoster || []).forEach((p) => roster.set(p.peerId, { name: p.displayName, role: p.role }));
    renderRoster();
    // Người vào sau (học sinh): đồng bộ đúng thí nghiệm giáo viên đang chiếu
    if (scene && !CFG.isTeacher) engine.setScene(scene, false);
    // Bàn phản ứng: phát lại các hóa chất đã cho vào cốc (tức thì, không animation)
    if (Array.isArray(chem) && chem.length) engine.chemlab?.restore(chem);
    // Thí nghiệm tham số: khôi phục các giá trị thanh trượt giáo viên đang đặt
    if (sims && Object.keys(sims).length) engine.simlab?.restore(sims);
    peers.forEach((p) => streams.subscribe(p.peerId));
  });
  hub.on('NewPublisher', (peer) => streams.subscribe(peer.peerId));

  // ── Có người vào / rời phòng -> cập nhật danh sách + thông báo ───────────
  hub.on('PeerJoined', (peer) => {
    roster.set(peer.peerId, { name: peer.displayName, role: peer.role });
    renderRoster();
    showJoinToast(peer.displayName, peer.role);
  });
  hub.on('PeerLeft', (peerId) => {
    roster.delete(peerId);
    renderRoster();
  });

  // ── Giơ tay / kết thúc phòng (realtime) ──────────────────────────────────
  hub.on('HandRaised', ({ peerId, displayName }) => {
    const e = roster.get(peerId); if (e) e.hand = true;
    renderRoster(); showHandToast(displayName);
  });
  hub.on('HandLowered', ({ peerId }) => {
    const e = roster.get(peerId); if (e) e.hand = false;
    renderRoster();
  });
  hub.on('RoomEnded', () => { alert('Giáo viên đã kết thúc phòng học.'); leaveNexus(hub); });

  // ── Nút mở / đóng danh sách người trong phòng ──────────────────────────
  els.rosterToggle?.addEventListener('click', () => {
    const open = els.rosterPanel?.classList.toggle('show');
    els.rosterToggle.classList.toggle('is-open', open);
  });

  // ── Thanh điều khiển: mic / camera / giơ tay / ghi hình / rời phòng ─────
  let micOn = true, camOn = true, handUp = false;
  els.nxMic?.addEventListener('click', () => {
    micOn = !micOn;
    streams.localStream?.getAudioTracks().forEach((t) => (t.enabled = micOn));
    els.nxMic.classList.toggle('off', !micOn);
    if (els.nxMicIc) els.nxMicIc.textContent = micOn ? '🎙️' : '🔇';
  });
  els.nxCam?.addEventListener('click', () => {
    camOn = !camOn;
    streams.localStream?.getVideoTracks().forEach((t) => (t.enabled = camOn));
    els.nxCam.classList.toggle('off', !camOn);
    if (els.nxCamIc) els.nxCamIc.textContent = camOn ? '🎥' : '📷';
    if (els.feed) els.feed.style.opacity = camOn ? '1' : '0';
  });
  els.nxHand?.addEventListener('click', () => {
    handUp = !handUp;
    els.nxHand.classList.toggle('is-active', handUp);
    hub.invoke(handUp ? 'RaiseHand' : 'LowerHand').catch(() => {});
    const me = roster.get(selfPeerId); if (me) { me.hand = handUp; renderRoster(); }
  });
  els.nxRec?.addEventListener('click', () => toggleNexusRecord(streams, () => lastRemoteAudio));
  els.nxLeave?.addEventListener('click', async () => {
    const teacher = CFG.isTeacher;
    const ok = await nexusConfirm({
      danger:  teacher,
      title:   teacher ? 'Kết thúc phòng học?' : 'Rời khỏi phòng?',
      message: teacher
        ? 'Phòng sẽ đóng cho tất cả mọi người đang tham gia.'
        : 'Bạn sẽ rời khỏi phòng học này.',
      okText:  teacher ? 'Kết thúc phòng' : 'Rời phòng'
    });
    if (!ok) return;
    if (teacher) hub.invoke('EndRoom').catch(() => {});
    leaveNexus(hub);
  });
  hub.onreconnecting(() => setStatus(false, 'RECONNECTING'));
  hub.onreconnected(() => setStatus(true, 'SYNC 20Hz'));
  hub.onclose(() => setStatus(false, 'OFFLINE'));

  try {
    await hub.start();
    await hub.invoke('JoinRoom', CFG.roomId);
    // Đăng ký thí nghiệm khởi tạo lên server (lúc wirePickers gọi thì hub chưa start)
    if (CFG.isTeacher) hub.invoke('SetScene', engine.scene2).catch(() => {});
  } catch (err) { console.error('[Nexus] Hub start failed:', err); setStatus(false, 'HUB ERROR'); }

  // ── Teacher: hiện camera NGAY, rồi mới đẩy lên SFU (best-effort) ────────
  if (CFG.isTeacher) {
    try {
      const local = await streams.getLocalMedia({ video: true, audio: true });
      showFeed(local);                       // camera lên hình ngay, không chờ SFU
      streams.attachLocalAnalyser();
      pumpGlow(() => streams.getLocalLevel());
      // Đẩy lên SFU để học sinh xem — nếu SFU lỗi vẫn KHÔNG mất preview của giáo viên
      streams.publishLocal().catch((err) =>
        console.warn('[Nexus] SFU publish lỗi (camera vẫn hiển thị):', err.message));
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

// ── Roster: render danh sách + thông báo người vào phòng ──────────────────────
const escapeHtml = (s) =>
  String(s).replace(/[&<>"']/g, (c) => ({ '&': '&amp;', '<': '&lt;', '>': '&gt;', '"': '&quot;', "'": '&#39;' }[c]));

/** Chữ cái viết tắt cho avatar (chữ đầu của tên đầu + tên cuối). */
function initials(name) {
  const parts = (name || '').trim().split(/\s+/).filter(Boolean);
  if (!parts.length) return '?';
  if (parts.length === 1) return parts[0].slice(0, 1).toUpperCase();
  return (parts[0][0] + parts[parts.length - 1][0]).toUpperCase();
}

const roleLabel = (role) => (role === 'teacher' ? 'Giáo viên' : 'Học sinh');

/** Vẽ lại danh sách người trong phòng (giáo viên trước, rồi A→Z theo tên). */
function renderRoster() {
  const total = roster.size;
  if (els.rosterCount) els.rosterCount.textContent = String(total);
  if (els.rosterHeadCount) els.rosterHeadCount.textContent = String(total);
  if (!els.rosterList) return;

  const entries = [...roster.entries()].sort((a, b) => {
    if (a[1].role !== b[1].role) return a[1].role === 'teacher' ? -1 : 1;
    return (a[1].name || '').localeCompare(b[1].name || '', 'vi');
  });

  els.rosterList.innerHTML = entries.map(([id, p]) => {
    const self = id === selfPeerId ? ' is-self' : '';
    const youTag = id === selfPeerId ? ' <i>(bạn)</i>' : '';
    const badgeMod = p.role === 'teacher' ? 'roster-badge--t' : 'roster-badge--s';
    const handIc = p.hand ? '<span style="font-size:.9rem;margin-right:2px" title="Đang giơ tay">✋</span>' : '';
    return `<li class="roster-item${self}">
        <span class="roster-ava" data-role="${p.role}">${escapeHtml(initials(p.name))}</span>
        <span class="roster-name">${escapeHtml(p.name || 'Học viên')}${youTag}</span>
        ${handIc}
        <span class="roster-badge ${badgeMod}">${roleLabel(p.role)}</span>
      </li>`;
  }).join('');
}

/** Thông báo nổi khi có người mới vào phòng. */
function showJoinToast(name, role) {
  if (!els.rosterToast) return;
  els.rosterToast.innerHTML =
    `<i class="roster-toast__dot"></i><span><b>${escapeHtml(name || 'Học viên')}</b> · ${roleLabel(role)} đã vào phòng</span>`;
  els.rosterToast.classList.add('show');
  clearTimeout(joinToastTimer);
  joinToastTimer = setTimeout(() => els.rosterToast.classList.remove('show'), 3200);
}

/** Thông báo khi có người giơ tay. */
function showHandToast(name) {
  if (!els.rosterToast) return;
  els.rosterToast.innerHTML =
    `<i class="roster-toast__dot" style="background:#f5d76e;box-shadow:0 0 8px rgba(245,215,110,.6)"></i><span>✋ <b>${escapeHtml(name || 'Học viên')}</b> giơ tay</span>`;
  els.rosterToast.classList.add('show');
  clearTimeout(joinToastTimer);
  joinToastTimer = setTimeout(() => els.rosterToast.classList.remove('show'), 3200);
}

// ── GHI HÌNH PHÒNG 3D (canvas WebGL + trộn audio → tải .webm) ─────────────────
let _nxRec = { mr: null, chunks: [], on: false, timer: 0, secs: 0, actx: null };
function toggleNexusRecord(streams, getRemote) { _nxRec.on ? stopNexusRecord() : startNexusRecord(streams, getRemote); }
async function startNexusRecord(streams, getRemote) {
  if (!window.MediaRecorder || !els.canvas || !els.canvas.captureStream) { alert('Trình duyệt không hỗ trợ ghi hình'); return; }
  try {
    const vStream = els.canvas.captureStream(25);            // canvas hologram WebGL
    _nxRec.actx = new (window.AudioContext || window.webkitAudioContext)();
    try { await _nxRec.actx.resume(); } catch (e) {}
    const dest = _nxRec.actx.createMediaStreamDestination();
    let na = 0;
    const addAudio = (s) => { const a = s && s.getAudioTracks && s.getAudioTracks(); if (a && a.length) { try { _nxRec.actx.createMediaStreamSource(new MediaStream([a[0]])).connect(dest); na++; } catch (e) {} } };
    addAudio(streams && streams.localStream);               // mic giáo viên (nếu có)
    addAudio(getRemote && getRemote());                     // tiếng giáo viên (phía học sinh)
    const audioTracks = na > 0 ? dest.stream.getAudioTracks() : [];
    const mixed = new MediaStream([vStream.getVideoTracks()[0], ...audioTracks]);
    const mime = ['video/webm;codecs=vp9,opus', 'video/webm;codecs=vp8,opus', 'video/webm'].find((m) => MediaRecorder.isTypeSupported(m)) || '';
    _nxRec.mr = new MediaRecorder(mixed, mime ? { mimeType: mime, videoBitsPerSecond: 3000000 } : undefined);
    _nxRec.chunks = [];
    _nxRec.mr.ondataavailable = (e) => { if (e.data && e.data.size) _nxRec.chunks.push(e.data); };
    _nxRec.mr.onstop = finalizeNexusRecord;
    _nxRec.mr.start(1000);
    _nxRec.on = true; _nxRec.secs = 0; _nxRec.startTs = Date.now(); updateNxRecUI(true);
    _nxRec.timer = setInterval(() => { _nxRec.secs++; updateNxRecTime(); }, 1000);
  } catch (e) { alert('Không ghi hình được: ' + e.message); cleanupNxRec(); }
}
function stopNexusRecord() { if (_nxRec.mr && _nxRec.mr.state !== 'inactive') { try { _nxRec.mr.stop(); } catch (e) { cleanupNxRec(); } } else cleanupNxRec(); }
function finalizeNexusRecord() {
  const blob = new Blob(_nxRec.chunks, { type: 'video/webm' });
  if (blob.size) {
    const url = URL.createObjectURL(blob), a = document.createElement('a');
    const ts = new Date().toISOString().slice(0, 19).replace(/[:T]/g, '-');
    a.href = url; a.download = `lumina-3d-${ts}.webm`;
    document.body.appendChild(a); a.click(); a.remove();
    setTimeout(() => URL.revokeObjectURL(url), 15000);
    uploadNexusRecording(blob);                 // lưu lên server (DB + trang Admin)
  }
  cleanupNxRec();
}
// Gửi bản ghi phòng 3D + metadata lên server
function uploadNexusRecording(blob) {
  try {
    const students = [...roster.values()].filter((p) => p.role !== 'teacher').length;
    const fd = new FormData();
    fd.append('file', blob, 'rec.webm');
    fd.append('source', 'Lab3D');
    fd.append('roomLabel', 'Phòng 3D · ' + (CFG.roomId || ''));
    fd.append('startedAtMs', String(_nxRec.startTs || Date.now()));
    fd.append('endedAtMs', String(Date.now()));
    fd.append('participantCount', String(students));
    fetch('/Recording/Save', { method: 'POST', body: fd }).catch(() => {});
  } catch (e) {}
}
function cleanupNxRec() {
  _nxRec.on = false; clearInterval(_nxRec.timer);
  if (_nxRec.actx) { _nxRec.actx.close().catch(() => {}); _nxRec.actx = null; }
  _nxRec.mr = null; _nxRec.chunks = [];
  updateNxRecUI(false);
}
function updateNxRecUI(on) {
  els.nxRec?.classList.toggle('rec-on', on);
  if (els.nxRecIc) els.nxRecIc.textContent = on ? '⏹️' : '⏺️';
  els.nxRecBadge?.classList.toggle('show', on);
  if (!on) updateNxRecTime(true);
}
function updateNxRecTime(reset) {
  const s = reset ? 0 : _nxRec.secs;
  if (els.nxRecTime) els.nxRecTime.textContent = String(Math.floor(s / 60)).padStart(2, '0') + ':' + String(s % 60).padStart(2, '0');
}

/** Rời / kết thúc phòng: dừng ghi (nếu đang ghi), ngắt hub, về trang chủ. */
function leaveNexus(hub) {
  try { if (_nxRec.on) stopNexusRecord(); } catch (e) {}
  try { hub && hub.stop && hub.stop(); } catch (e) {}
  window.location.href = '/Dashboard';
}

function showFeed(stream) {
  if (!els.feed) return;
  els.feed.srcObject = stream; els.feed.style.display = 'block';
  if (els.camPlaceholder) els.camPlaceholder.style.display = 'none';
  // Trên mobile, camera bị ẩn cho tới khi có luồng thật — đánh dấu để hiện thumbnail.
  document.body.classList.add('nexus-has-feed');
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
