// ════════════════════════════════════════════════════════════════════════════
//  Bảng vẽ Toán 3D cộng tác — engine client
//  • Render hình học bằng Three.js, camera độc lập từng máy (OrbitControls).
//  • Đồng bộ real-time qua SignalR theo mô hình TRUYỀN LỆNH (op), không truyền mesh.
//  • Optimistic: vẽ hiện ngay tại máy mình, gửi op cho server phát cho người khác.
//  • Người vào trễ nhận snapshot toàn bộ scene.
// ════════════════════════════════════════════════════════════════════════════
import * as THREE from 'three';
import { OrbitControls } from 'three/addons/controls/OrbitControls.js';
import { CSS2DRenderer, CSS2DObject } from 'three/addons/renderers/CSS2DRenderer.js';

export function initMathLab(cfg) {
    const signalR = window.signalR;
    const canvas  = document.getElementById('ml-canvas');

    // ── State ────────────────────────────────────────────────────────────────
    let isHost      = !!cfg.isHost;
    let studentDraw = false;
    let cameraMode  = 'free';            // 'free' | 'follow'
    let tool        = 'orbit';
    let color       = '#38bdf8';
    let level       = 0;                 // cao độ mặt phẳng vẽ (trục Y)
    let pending     = null;              // điểm neo cho công cụ 2-điểm (đoạn/vector)

    const objects   = new Map();         // id → { spec, root }
    const pickables = [];                // root Object3D (cho raycast xóa)

    const canDraw = () => isHost || studentDraw;

    // ── Renderer / Scene / Camera ────────────────────────────────────────────
    const renderer = new THREE.WebGLRenderer({ canvas, antialias: true });
    renderer.setPixelRatio(Math.min(window.devicePixelRatio, 2)); // ghìm DPR → mượt trên mobile
    renderer.setSize(window.innerWidth, window.innerHeight);

    // Lớp render nhãn/đo đạc bằng HTML (CSS2D) phủ lên canvas, không chặn chuột
    const labelRenderer = new CSS2DRenderer();
    labelRenderer.setSize(window.innerWidth, window.innerHeight);
    Object.assign(labelRenderer.domElement.style, { position: 'absolute', top: '0', left: '0', pointerEvents: 'none', zIndex: '5' });
    document.body.appendChild(labelRenderer.domElement);

    const scene = new THREE.Scene();
    scene.background = new THREE.Color(0x0a0f1e);

    const camera = new THREE.PerspectiveCamera(55, window.innerWidth / window.innerHeight, 0.1, 200);
    const HOME = { p: new THREE.Vector3(9, 8, 12), t: new THREE.Vector3(0, 0, 0) };
    camera.position.copy(HOME.p);

    const controls = new OrbitControls(camera, renderer.domElement);
    controls.enableDamping = true;
    controls.dampingFactor = 0.08;
    controls.target.copy(HOME.t);
    controls.update();

    // ── Lights ───────────────────────────────────────────────────────────────
    scene.add(new THREE.AmbientLight(0xb8c6e0, 1.0));
    const key = new THREE.DirectionalLight(0xffffff, 1.4);
    key.position.set(6, 12, 8);
    scene.add(key);
    scene.add(new THREE.HemisphereLight(0x8090b0, 0x202840, 0.5));

    // ── Lưới + trục Oxyz ─────────────────────────────────────────────────────
    const grid = new THREE.GridHelper(20, 20, 0x2a3a5a, 0x18233a);
    scene.add(grid);
    const axes = new THREE.AxesHelper(5);   // X đỏ · Y xanh lá · Z xanh dương
    scene.add(axes);

    // Mặt phẳng vẽ (di chuyển theo cao độ) — gợi ý nơi nét sẽ rơi xuống
    const planeHelper = new THREE.Mesh(
        new THREE.PlaneGeometry(20, 20),
        new THREE.MeshBasicMaterial({ color: 0x38bdf8, transparent: true, opacity: 0.05, side: THREE.DoubleSide })
    );
    planeHelper.rotation.x = -Math.PI / 2;
    scene.add(planeHelper);

    // Con trỏ laser của giáo viên (nhìn từ máy người khác)
    const laser = new THREE.Mesh(
        new THREE.SphereGeometry(0.18, 16, 16),
        new THREE.MeshBasicMaterial({ color: 0xef4444 })
    );
    laser.visible = false;
    scene.add(laser);
    let laserHideTimer = null;

    // ── Tiện ích ─────────────────────────────────────────────────────────────
    const uuid = () => (crypto.randomUUID ? crypto.randomUUID()
                        : 'id-' + Date.now() + '-' + Math.random().toString(16).slice(2));
    const v3   = (a) => new THREE.Vector3(a[0], a[1], a[2]);
    const arr  = (v) => [+v.x.toFixed(3), +v.y.toFixed(3), +v.z.toFixed(3)];

    // ── Dựng mesh từ spec (chân lý hình học = THAM SỐ, không phải mesh) ───────
    function buildMesh(spec) {
        const g = new THREE.Group();
        g.userData.objId = spec.id;
        const col = spec.color || color;
        const std = (o) => new THREE.MeshStandardMaterial(Object.assign({ color: col, roughness: .5, metalness: .1 }, o));

        switch (spec.kind) {
            case 'point': {
                const m = new THREE.Mesh(new THREE.SphereGeometry(0.13, 18, 18), std({}));
                m.position.copy(v3(spec.p));
                g.add(m);
                break;
            }
            case 'segment': {
                g.add(tube(v3(spec.a), v3(spec.b), col, 0.035));
                break;
            }
            case 'vector': {
                const a = v3(spec.a), b = v3(spec.b);
                const dir = new THREE.Vector3().subVectors(b, a);
                const len = Math.max(dir.length(), 0.0001);
                g.add(new THREE.ArrowHelper(dir.normalize(), a, len, col, Math.min(0.5, len * 0.25), 0.22));
                break;
            }
            case 'sphere': {
                const m = new THREE.Mesh(new THREE.SphereGeometry(spec.r, 32, 24),
                    std({ transparent: true, opacity: 0.55 }));
                m.position.copy(v3(spec.c));
                g.add(m);
                break;
            }
            case 'box': {
                const m = new THREE.Mesh(new THREE.BoxGeometry(spec.s[0], spec.s[1], spec.s[2]),
                    std({ transparent: true, opacity: 0.5 }));
                m.position.copy(v3(spec.c));
                const edges = new THREE.LineSegments(
                    new THREE.EdgesGeometry(m.geometry),
                    new THREE.LineBasicMaterial({ color: col }));
                edges.position.copy(m.position);
                g.add(m); g.add(edges);
                break;
            }
            case 'plane': {
                const m = new THREE.Mesh(new THREE.PlaneGeometry(spec.size, spec.size),
                    std({ transparent: true, opacity: 0.3, side: THREE.DoubleSide }));
                m.rotation.x = -Math.PI / 2;
                m.position.copy(v3(spec.c));
                g.add(m);
                break;
            }
            case 'freehand': {
                const pts = spec.pts.map(v3);
                g.add(new THREE.Line(
                    new THREE.BufferGeometry().setFromPoints(pts),
                    new THREE.LineBasicMaterial({ color: col })));
                break;
            }
            case 'label': {
                g.add(makeLabel(v3(spec.p), spec.text || '', col));
                break;
            }
            case 'measure': {
                const a = v3(spec.a), b = v3(spec.b);
                g.add(tube(a, b, col, 0.02));
                const mid = a.clone().add(b).multiplyScalar(0.5);
                g.add(makeLabel(mid, a.distanceTo(b).toFixed(2), col));  // độ dài đoạn
                break;
            }
        }
        return g;
    }

    // Nhãn/số đo dạng HTML neo vào một điểm 3D (luôn quay về phía người xem)
    function makeLabel(pos, text, col) {
        const div = document.createElement('div');
        div.className = 'ml-label3d';
        div.textContent = text;
        div.style.color = col;
        const obj = new CSS2DObject(div);
        obj.position.copy(pos);
        return obj;
    }

    function tube(a, b, col, r) {
        const dir = new THREE.Vector3().subVectors(b, a);
        const len = Math.max(dir.length(), 0.0001);
        const mesh = new THREE.Mesh(
            new THREE.CylinderGeometry(r, r, len, 10),
            new THREE.MeshStandardMaterial({ color: col, roughness: .5 }));
        mesh.position.copy(a).add(b).multiplyScalar(0.5);
        mesh.quaternion.setFromUnitVectors(new THREE.Vector3(0, 1, 0), dir.clone().normalize());
        return mesh;
    }

    // ── Quản lý đối tượng cục bộ ──────────────────────────────────────────────
    function addOrReplace(spec) {
        removeLocal(spec.id, true);
        const root = buildMesh(spec);
        scene.add(root);
        pickables.push(root);
        objects.set(spec.id, { spec, root });
        updateCount();
    }

    function removeLocal(id, silent) {
        const o = objects.get(id);
        if (!o) return;
        scene.remove(o.root);
        const i = pickables.indexOf(o.root);
        if (i >= 0) pickables.splice(i, 1);
        o.root.traverse(n => { if (n.isCSS2DObject && n.element) n.element.remove(); n.geometry?.dispose?.(); n.material?.dispose?.(); });
        objects.delete(id);
        if (!silent) updateCount();
    }

    function clearLocal() {
        [...objects.keys()].forEach(id => removeLocal(id, true));
        updateCount();
    }

    // ── SignalR ───────────────────────────────────────────────────────────────
    const conn = new signalR.HubConnectionBuilder()
        .withUrl('/hubs/lab')
        .withAutomaticReconnect()
        .build();

    function emitOp(kind, objectId, spec) {
        const op = { id: uuid(), kind, objectId: objectId ?? null, json: spec ? JSON.stringify(spec) : null };
        conn.invoke('SendOp', cfg.sessionId, op).catch(() => {});
    }

    conn.on('Snapshot', d => {
        isHost      = d.isHost;
        studentDraw = d.studentDraw;
        cameraMode  = d.cameraMode || 'free';
        (d.objects || []).forEach(j => { try { addOrReplace(JSON.parse(j)); } catch {} });
        applyPerms(); applyModeUI();
        setStatus(isHost ? 'Bạn là chủ phòng — vẽ thoải mái.' : (canDraw() ? 'Bạn được phép vẽ.' : 'Chế độ xem — chờ giáo viên mở quyền vẽ.'));
    });
    conn.on('Op', op => {
        if (op.kind === 'upsert' && op.json) { try { addOrReplace(JSON.parse(op.json)); } catch {} }
        else if (op.kind === 'delete') removeLocal(op.objectId);
        else if (op.kind === 'clear')  clearLocal();
    });
    conn.on('OpRejected', () => setStatus('Thao tác bị từ chối (bạn chưa được phép vẽ).'));
    conn.on('Camera', j => { try { applyRemoteCamera(JSON.parse(j)); } catch {} });
    conn.on('CameraMode', m => { cameraMode = m; applyModeUI(); });
    conn.on('StudentDraw', a => { studentDraw = a; applyPerms();
        setStatus(canDraw() ? 'Giáo viên đã MỞ quyền vẽ cho học sinh.' : 'Giáo viên đã KHÓA quyền vẽ.'); });
    conn.on('Pointer', p => showRemoteLaser(p));
    conn.on('PeerJoined', () => bumpPeers(+1));
    conn.on('PeerLeft',   () => bumpPeers(-1));
    conn.on('LabEnded', () => { setStatus('Phòng đã được giáo viên kết thúc.'); setTimeout(() => location.href = '/VirtualLab', 1500); });

    conn.start()
        .then(() => conn.invoke('JoinLab', cfg.sessionId))
        .catch(err => setStatus('Lỗi kết nối real-time: ' + err));

    // ── Camera follow (đồng bộ góc nhìn) ──────────────────────────────────────
    let lastCamSent = 0;
    let applyingRemote = false;
    controls.addEventListener('change', () => {
        if (!isHost || cameraMode !== 'follow' || applyingRemote) return;
        const now = performance.now();
        if (now - lastCamSent < 90) return;          // ghìm ~11 lần/giây
        lastCamSent = now;
        conn.invoke('SyncCamera', cfg.sessionId,
            JSON.stringify({ p: arr(camera.position), t: arr(controls.target) })).catch(() => {});
    });

    function applyRemoteCamera(c) {
        if (isHost || cameraMode !== 'follow') return;
        applyingRemote = true;
        camera.position.copy(v3(c.p));
        controls.target.copy(v3(c.t));
        controls.update();
        applyingRemote = false;
    }

    // ── Tương tác chuột/cảm ứng ───────────────────────────────────────────────
    const ray = new THREE.Raycaster();
    const ndc = new THREE.Vector2();
    const drawPlane = new THREE.Plane(new THREE.Vector3(0, 1, 0), 0);  // y = level
    const activePointers = new Set();
    let downPt = null, moved = false, freePts = null, freePreview = null;

    function setNdc(e) {
        const r = canvas.getBoundingClientRect();
        ndc.x = ((e.clientX - r.left) / r.width) * 2 - 1;
        ndc.y = -((e.clientY - r.top) / r.height) * 2 + 1;
    }
    function planePoint(e) {
        setNdc(e);
        ray.setFromCamera(ndc, camera);
        drawPlane.constant = -level;
        const hit = new THREE.Vector3();
        return ray.ray.intersectPlane(drawPlane, hit) ? hit : null;
    }
    function pickId(e) {
        setNdc(e);
        ray.setFromCamera(ndc, camera);
        const hits = ray.intersectObjects(pickables, true);
        for (const h of hits) {
            let o = h.object;
            while (o && o.userData.objId == null) o = o.parent;
            if (o) return o.userData.objId;
        }
        return null;
    }

    canvas.addEventListener('pointerdown', e => {
        activePointers.add(e.pointerId);
        if (activePointers.size > 1) { cancelFreehand(); return; }  // đa chạm → để OrbitControls lo
        downPt = { x: e.clientX, y: e.clientY }; moved = false;

        if (tool === 'laser' && isHost) { sendLaser(e, true); return; }
        if (tool === 'freehand' && canDraw() && e.button === 0) startFreehand(e);
    });

    canvas.addEventListener('pointermove', e => {
        if (downPt && (Math.abs(e.clientX - downPt.x) + Math.abs(e.clientY - downPt.y)) > 6) moved = true;
        if (tool === 'laser' && isHost && activePointers.has(e.pointerId)) sendLaser(e, true);
        if (freePts && activePointers.size === 1) extendFreehand(e);
    });

    canvas.addEventListener('pointerup', e => {
        activePointers.delete(e.pointerId);
        if (tool === 'laser' && isHost) { sendLaser(e, false); cleanup(); return; }
        if (freePts) { finishFreehand(); cleanup(); return; }
        if (!moved && downPt && activePointers.size === 0) handleClick(e);  // coi như một cú "nhấp"
        cleanup();
    });
    canvas.addEventListener('pointercancel', e => { activePointers.delete(e.pointerId); cancelFreehand(); cleanup(); });
    function cleanup() { downPt = null; moved = false; }

    function handleClick(e) {
        if (tool === 'delete' && canDraw()) {
            const id = pickId(e);
            if (id) { removeLocal(id); emitOp('delete', id, null); }
            return;
        }
        if (!canDraw() || tool === 'orbit' || tool === 'laser') return;
        const p = planePoint(e);
        if (!p) return;

        if (tool === 'point')  return create({ kind: 'point', p: arr(p) });
        if (tool === 'sphere') return create({ kind: 'sphere', c: arr(p), r: 0.7 });
        if (tool === 'box')    return create({ kind: 'box', c: arr(p.clone().setY(p.y + 0.5)), s: [1, 1, 1] });
        if (tool === 'plane')  return create({ kind: 'plane', c: arr(p), size: 4 });

        if (tool === 'label') {
            const text = (window.prompt('Nội dung nhãn:') || '').trim();
            if (text) create({ kind: 'label', p: arr(p), text });
            return;
        }

        if (tool === 'segment' || tool === 'vector' || tool === 'measure') {
            if (!pending) {
                pending = p.clone();
                setStatus(tool === 'measure' ? 'Đã đặt điểm đầu — chạm điểm thứ hai để đo.' : 'Đã đặt điểm đầu — chạm điểm thứ hai.');
                markPending(p); return;
            }
            create({ kind: tool, a: arr(pending), b: arr(p) });
            pending = null; clearPending();
        }
    }

    function create(partial) {
        const spec = Object.assign({ id: uuid(), color }, partial);
        addOrReplace(spec);            // optimistic
        emitOp('upsert', spec.id, spec);
    }

    // điểm neo tạm cho công cụ 2-điểm
    let pendingMarker = null;
    function markPending(p) {
        clearPending();
        pendingMarker = new THREE.Mesh(new THREE.SphereGeometry(0.16, 12, 12),
            new THREE.MeshBasicMaterial({ color: 0xfacc15 }));
        pendingMarker.position.copy(p);
        scene.add(pendingMarker);
    }
    function clearPending() { if (pendingMarker) { scene.remove(pendingMarker); pendingMarker = null; } }

    // ── Vẽ tay ────────────────────────────────────────────────────────────────
    function startFreehand(e) {
        const p = planePoint(e); if (!p) return;
        freePts = [p];
        freePreview = new THREE.Line(new THREE.BufferGeometry().setFromPoints(freePts),
            new THREE.LineBasicMaterial({ color }));
        scene.add(freePreview);
    }
    function extendFreehand(e) {
        const p = planePoint(e); if (!p || !freePts) return;
        if (freePts[freePts.length - 1].distanceTo(p) < 0.06) return;  // ghìm số điểm
        freePts.push(p);
        freePreview.geometry.setFromPoints(freePts);
    }
    function finishFreehand() {
        if (freePreview) { scene.remove(freePreview); freePreview.geometry.dispose(); }
        if (freePts && freePts.length >= 2) create({ kind: 'freehand', pts: freePts.map(arr) });
        freePts = null; freePreview = null;
    }
    function cancelFreehand() {
        if (freePreview) { scene.remove(freePreview); freePreview.geometry.dispose(); }
        freePts = null; freePreview = null;
    }

    // ── Laser ─────────────────────────────────────────────────────────────────
    function sendLaser(e, on) {
        const p = planePoint(e);
        if (!p) { if (!on) conn.invoke('Pointer', cfg.sessionId, 0, 0, 0, false).catch(() => {}); return; }
        conn.invoke('Pointer', cfg.sessionId, p.x, p.y, p.z, on).catch(() => {});
    }
    function showRemoteLaser(p) {
        if (!p.on) { laser.visible = false; return; }
        laser.position.set(p.x, p.y, p.z);
        laser.visible = true;
        clearTimeout(laserHideTimer);
        laserHideTimer = setTimeout(() => (laser.visible = false), 2000);
    }

    // ── Wiring toolbar (theo data-attribute, không cần inline onclick) ─────────
    function applyControlMode() {
        const drawing = tool !== 'orbit' && canDraw();
        const useLaser = tool === 'laser';
        if (drawing || useLaser) {
            controls.mouseButtons = { LEFT: null, MIDDLE: THREE.MOUSE.DOLLY, RIGHT: THREE.MOUSE.ROTATE };
            controls.touches = { ONE: null, TWO: THREE.TOUCH.DOLLY_PAN };
        } else {
            controls.mouseButtons = { LEFT: THREE.MOUSE.ROTATE, MIDDLE: THREE.MOUSE.DOLLY, RIGHT: THREE.MOUSE.PAN };
            controls.touches = { ONE: THREE.TOUCH.ROTATE, TWO: THREE.TOUCH.DOLLY_PAN };
        }
    }

    function setTool(name) {
        if (name !== 'orbit' && name !== 'laser' && name !== 'delete' && !canDraw()) {
            setStatus('Bạn chưa được phép vẽ.'); return;
        }
        tool = name;
        pending = null; clearPending();
        document.querySelectorAll('.ml-tool').forEach(b =>
            b.classList.toggle('active', b.dataset.tool === name));
        applyControlMode();
    }

    document.querySelectorAll('.ml-tool').forEach(b =>
        b.addEventListener('click', () => setTool(b.dataset.tool)));

    document.querySelectorAll('.ml-color').forEach(b =>
        b.addEventListener('click', () => {
            color = b.dataset.color;
            document.querySelectorAll('.ml-color').forEach(x => x.classList.toggle('active', x === b));
        }));

    document.querySelectorAll('[data-action]').forEach(b =>
        b.addEventListener('click', () => doAction(b.dataset.action)));

    function doAction(a) {
        if (a === 'level-up')   { level += 0.5; planeHelper.position.y = level; setLevelLabel(); }
        else if (a === 'level-down') { level -= 0.5; planeHelper.position.y = level; setLevelLabel(); }
        else if (a === 'fit')   { camera.position.copy(HOME.p); controls.target.copy(HOME.t); controls.update(); }
        else if (a === 'clear' && isHost) { clearLocal(); emitOp('clear', null, null); }
        else if (a === 'toggle-follow' && isHost) {
            cameraMode = cameraMode === 'follow' ? 'free' : 'follow';
            conn.invoke('SetCameraMode', cfg.sessionId, cameraMode).catch(() => {});
            applyModeUI();
        }
        else if (a === 'toggle-studentdraw' && isHost) {
            studentDraw = !studentDraw;
            conn.invoke('SetStudentDraw', cfg.sessionId, studentDraw).catch(() => {});
            applyPerms();
        }
        else if (a === 'end' && isHost) {
            conn.invoke('EndLab', cfg.sessionId).catch(() => {});
        }
    }

    // ── Cập nhật UI ───────────────────────────────────────────────────────────
    let peers = 1;
    const $ = (id) => document.getElementById(id);
    function setStatus(t) { const el = $('ml-status'); if (el) el.textContent = t; }
    function setLevelLabel() { const el = $('ml-level'); if (el) el.textContent = 'z = ' + level.toFixed(1); }
    function updateCount() { const el = $('ml-count'); if (el) el.textContent = objects.size + ' đối tượng'; }
    function bumpPeers(d) { peers = Math.max(1, peers + d); const el = $('ml-peers'); if (el) el.textContent = peers + ' người'; }
    function applyModeUI() {
        const el = $('btn-follow');
        if (el) { el.classList.toggle('active', cameraMode === 'follow');
            el.querySelector('span').textContent = cameraMode === 'follow' ? 'Đang dẫn nhìn' : 'Dẫn góc nhìn'; }
        if (!isHost && cameraMode === 'follow') setStatus('Đang xem theo góc nhìn của giáo viên.');
    }
    function applyPerms() {
        const allow = canDraw();
        document.querySelectorAll('.ml-tool').forEach(b => {
            const t = b.dataset.tool;
            const locked = (t !== 'orbit' && t !== 'laser' && t !== 'delete') && !allow;
            b.classList.toggle('disabled', locked);
        });
        const el = $('btn-studentdraw');
        if (el) { el.classList.toggle('active', studentDraw);
            el.querySelector('span').textContent = studentDraw ? 'HS đang được vẽ' : 'Cho HS vẽ'; }
        if (tool !== 'orbit' && tool !== 'laser' && tool !== 'delete' && !allow) setTool('orbit');
    }

    // ── Vòng lặp render + resize ──────────────────────────────────────────────
    function onResize() {
        camera.aspect = window.innerWidth / window.innerHeight;
        camera.updateProjectionMatrix();
        renderer.setSize(window.innerWidth, window.innerHeight);
        labelRenderer.setSize(window.innerWidth, window.innerHeight);
    }
    window.addEventListener('resize', onResize);
    document.addEventListener('visibilitychange', () => { running = !document.hidden; if (running) loop(); });

    let running = true;
    function loop() {
        if (!running) return;
        controls.update();
        renderer.render(scene, camera);
        labelRenderer.render(scene, camera);
        requestAnimationFrame(loop);
    }

    // Khởi tạo nhãn + chế độ ban đầu
    setLevelLabel(); updateCount(); applyControlMode();
    window.addEventListener('beforeunload', () => { conn.invoke('LeaveLab', cfg.sessionId).catch(() => {}); });
    loop();
}
