/**
 * Lumina3DEngine — "Lumina Research Facility" (Clinical Luxury)
 *
 *   • Scene PBR: thép không gỉ / kính / quartz, môi trường PMREM studio,
 *     sàn phản chiếu mờ, pedestal, panel LED trần đổ bóng mềm.
 *   • Specimen đa bộ phận: Highlight · Explode · Interactive Labels (CSS2D).
 *   • Teacher xoay specimen (turntable) -> đồng bộ; Student nội suy (SLERP/LERP).
 *   • Laser Pointer + Spatial Audio: ở LuminaInteraction.js.
 *
 * ESM, dùng importmap dự án ("three", "three/addons/").
 */
import * as THREE from 'three';
import { OrbitControls } from 'three/addons/controls/OrbitControls.js';
import { RoomEnvironment } from 'three/addons/environments/RoomEnvironment.js';
import { RectAreaLightUniformsLib } from 'three/addons/lights/RectAreaLightUniformsLib.js';
import { CSS2DRenderer, CSS2DObject } from 'three/addons/renderers/CSS2DRenderer.js';
// ?v=2 phải KHỚP với phiên bản trong nexus-init.js để dùng chung 1 instance module.
import { LuminaInteraction } from './LuminaInteraction.js?v=3';
import { LuminaChemLab } from './LuminaChemLab.js?v=3';
import { LuminaSimLab, SIMS } from './LuminaSimLab.js?v=3';

const GOLD = 0xc9a96a, PLATINUM = 0xc8cdd6, PEARL = 0xf4f5f7;
// Hologram palette: nền không gian tối + accent cyan
const HOLO = 0x7fe9ff, HOLO_DEEP = 0x1aa3e0, VOID = 0x05070f;

export class Lumina3DEngine {
  /** @param {HTMLCanvasElement} canvas @param {{isTeacher:boolean,hub:any,roomId:string,scene?:string}} opts */
  constructor(canvas, { isTeacher, hub, roomId, scene }) {
    this.canvas = canvas; this.hub = hub; this.isTeacher = isTeacher; this.roomId = roomId;
    this.initialScene = scene || 'cell';
    this.reduced = matchMedia('(prefers-reduced-motion: reduce)').matches;

    // ── Renderer: clinical / physically based ────────────────────────────
    this.renderer = new THREE.WebGLRenderer({ canvas, antialias: true });
    this.renderer.setPixelRatio(Math.min(devicePixelRatio, 2));
    this.renderer.outputColorSpace = THREE.SRGBColorSpace;
    this.renderer.toneMapping = THREE.ACESFilmicToneMapping;
    this.renderer.toneMappingExposure = 1.05;
    this.renderer.shadowMap.enabled = true;
    this.renderer.shadowMap.type = THREE.PCFSoftShadowMap;

    // CSS2D label layer (overlay trong suốt)
    this.labelRenderer = new CSS2DRenderer();
    this.labelRenderer.domElement.className = 'lumina-label-layer';
    this.labelRenderer.domElement.style.cssText = 'position:absolute;inset:0;pointer-events:none;z-index:3';
    canvas.parentElement.appendChild(this.labelRenderer.domElement);

    this.scene = new THREE.Scene();
    this.scene.background = new THREE.Color(VOID);
    this.scene.fog = new THREE.FogExp2(VOID, 0.045);

    this.camera = new THREE.PerspectiveCamera(50, 1, 0.1, 200);
    this.camera.position.set(0, 2.6, 6.8);

    this.listener = new THREE.AudioListener();
    this.camera.add(this.listener);

    RectAreaLightUniformsLib.init();
    this._buildEnvironment();
    this._buildRoom();

    // Specimen là 1 Group cố định trên pedestal; setScene() chỉ thay các bộ phận
    // con bên trong -> Laser (con của group) và trạng thái xoay được giữ nguyên.
    this.specimen = new THREE.Group();
    this.specimen.position.y = this.pedestalTop + 1.05;
    this.scene.add(this.specimen);
    this.parts = {}; this.labels = {}; this.onSceneLabel = null;
    this.setScene(this.initialScene, false);

    // OrbitControls: camera navigation (zoom quan trọng cho spatial audio)
    this.controls = new OrbitControls(this.camera, canvas);
    this.controls.enableDamping = true; this.controls.dampingFactor = 0.07;
    this.controls.target.set(0, 1.55, 0);
    this.controls.minDistance = 2.4; this.controls.maxDistance = 13;
    this.controls.maxPolarAngle = Math.PI * 0.92;
    // Teacher: kéo chuột = xoay SPECIMEN (turntable) -> tắt xoay camera, giữ zoom/pan.
    this.controls.enableRotate = !isTeacher;

    // ── State sync ───────────────────────────────────────────────────────
    this.SEND_HZ = 20; this.SEND_INTERVAL = 1000 / this.SEND_HZ;
    this.INTERP_DELAY = 100; this.EPS = 0.0008;
    this._lastQuat = new THREE.Quaternion(); this._lastSendAt = 0; this._seq = 0;
    this._tBuf = [];
    this._explode = 0; this._explodeTarget = 0;
    this.onTelemetry = null; this._frames = 0; this._fpsAt = performance.now();

    this._wireHub();
    this.interaction = new LuminaInteraction(this); // laser + (teacher turntable)
    this._onResize = () => this._resize();
    addEventListener('resize', this._onResize);
    this._resize();
    this.renderer.setAnimationLoop(() => this._frame());
  }

  getListener() { return this.listener; }

  // ── HDRI / PMREM (studio indoor) ───────────────────────────────────────
  _buildEnvironment() {
    const pmrem = new THREE.PMREMGenerator(this.renderer);
    // RoomEnvironment = studio không cần file .hdr. Giữ cường độ rất thấp để nền tối
    // (hologram tự phát sáng bằng emissive, không dựa vào IBL).
    this.scene.environment = pmrem.fromScene(new RoomEnvironment(), 0.012).texture;
  }

  // ── Sân khấu hologram: sàn lưới phát sáng + bệ emitter + chùm chiếu ─────
  _buildRoom() {
    // Sàn tối + lưới hologram mờ phát sáng toả ra xa
    const floor = new THREE.Mesh(
      new THREE.CircleGeometry(40, 64),
      new THREE.MeshStandardMaterial({ color: 0x060b14, metalness: 0.4, roughness: 0.7 })
    );
    floor.rotation.x = -Math.PI / 2; floor.receiveShadow = true;
    this.scene.add(floor);

    const grid = new THREE.GridHelper(44, 44, HOLO_DEEP, 0x0b2436);
    grid.position.y = 0.002;
    grid.material.transparent = true; grid.material.opacity = 0.28; grid.material.depthWrite = false;
    this.scene.add(grid);

    // Bệ phát hologram (emitter) — kim loại tối, miệng phát sáng cyan
    const base = new THREE.Mesh(
      new THREE.CylinderGeometry(1.2, 1.38, 0.42, 64),
      new THREE.MeshStandardMaterial({ color: 0x0a1622, metalness: 0.9, roughness: 0.34 })
    );
    base.position.y = 0.21; base.castShadow = true; base.receiveShadow = true;
    const emitterRing = new THREE.Mesh(
      new THREE.TorusGeometry(1.16, 0.035, 16, 96),
      new THREE.MeshBasicMaterial({ color: HOLO, transparent: true, opacity: 0.9, blending: THREE.AdditiveBlending, depthWrite: false })
    );
    emitterRing.position.y = 0.43; emitterRing.rotation.x = Math.PI / 2;
    const emitterDisc = new THREE.Mesh(
      new THREE.CircleGeometry(1.12, 64),
      new THREE.MeshBasicMaterial({ color: HOLO_DEEP, transparent: true, opacity: 0.32, blending: THREE.AdditiveBlending, depthWrite: false })
    );
    emitterDisc.position.y = 0.435; emitterDisc.rotation.x = -Math.PI / 2;
    this.scene.add(base, emitterRing, emitterDisc);
    this.pedestalTop = 0.44;

    // Chùm chiếu hologram toả lên specimen (additive, rất mờ, xoay nhẹ)
    this.beam = new THREE.Mesh(
      new THREE.CylinderGeometry(1.45, 1.02, 2.7, 48, 1, true),
      new THREE.MeshBasicMaterial({ color: HOLO, transparent: true, opacity: 0.06, side: THREE.DoubleSide, blending: THREE.AdditiveBlending, depthWrite: false })
    );
    this.beam.position.y = this.pedestalTop + 1.32;
    this.scene.add(this.beam);

    // Ánh sáng lạnh dịu — chỉ tạo khối nhẹ, hologram tự phát sáng
    this.scene.add(new THREE.HemisphereLight(0x9fd8ff, 0x070b14, 0.35));
    const rimL = new THREE.RectAreaLight(HOLO, 2.0, 5, 3); rimL.position.set(-3.5, 4.6, 2.6); rimL.lookAt(0, 1.5, 0);
    const rimR = new THREE.RectAreaLight(0xbfeaff, 1.6, 5, 3); rimR.position.set(3.5, 4.6, -1.6); rimR.lookAt(0, 1.5, 0);
    const key = new THREE.DirectionalLight(0xcfeeff, 0.5); key.position.set(3, 7, 4);
    key.castShadow = true; key.shadow.mapSize.set(1024, 1024);
    key.shadow.camera.near = 1; key.shadow.camera.far = 28;
    key.shadow.bias = -0.0003; key.shadow.radius = 5;
    this.scene.add(rimL, rimR, key);
  }

  // ── Nhãn hiển thị của từng thí nghiệm ───────────────────────────────────
  static SCENE_LABELS = {
    cell: 'Tế bào nhân thực', dna: 'Cấu trúc DNA', photosynthesis: 'Lục lạp · Quang hợp',
    reaction: 'Bàn phản ứng hóa học',
    titration: 'Chuẩn độ axit-bazơ', electrolysis: 'Điện phân nước', distillation: 'Chưng cất đơn giản',
    molecule: 'Phân tử nước H₂O',
    pendulum: 'Con lắc đơn', spring: 'Con lắc lò xo', optics: 'Lăng kính · Khúc xạ',
    polyhedron: 'Đa diện đều', vectors: 'Vector 3D',
  };
  sceneLabel(scene) { return SIMS[scene]?.label || Lumina3DEngine.SCENE_LABELS[scene] || scene; }

  /**
   * Đổi học cụ theo thí nghiệm. Chỉ thay các bộ phận con trong this.specimen
   * (Laser và trạng thái xoay được giữ nguyên).
   */
  setScene(scene, emit = true) {
    this.scene2 = scene;
    if (this.chemlab) { this.chemlab.dispose(); this.chemlab = null; }
    if (this.simlab) { this.simlab.dispose(); this.simlab = null; }
    this._clearSpecimen();
    if (scene === 'reaction') {
      // Bàn phản ứng hóa học tương tác — tự quản lý mesh/FX, không dùng parts
      this.chemlab = new LuminaChemLab(this);
    } else if (SIMS[scene]) {
      // Thí nghiệm tham số tương tác (Lý/Sinh/Toán)
      this.simlab = new LuminaSimLab(this, scene);
    } else {
      const specs = this._specimenSpecs(scene);
      specs.forEach(({ id, mesh, dir, label }) => this._register(id, mesh, dir, label));
      if (this._labelsOn) Object.values(this.labels).forEach((l) => (l.visible = true));
    }
    this._explode = 0; this._explodeTarget = 0;
    if (this.onSceneLabel) this.onSceneLabel(this.sceneLabel(scene));
    if (this.onChemState) this.onChemState(this.chemlab ? this.chemlab.state() : null);
    if (this.onSimState) this.onSimState(this.simlab ? this.simlab.readout() : null);
    if (emit && this.isTeacher) this.hub.invoke('SetScene', scene).catch(() => {});
  }

  _clearSpecimen() {
    Object.values(this.parts).forEach((m) => {
      this.specimen.remove(m);
      // Dispose cả mesh lẫn các con (cạnh hologram phát sáng)
      m.traverse((c) => { c.geometry?.dispose?.(); c.material?.dispose?.(); });
    });
    // Mesh trang trí ngoài parts (vd. wireframe đa diện) cũng phải dọn
    [...this.specimen.children].forEach((c) => {
      if (c.userData.decor) { this.specimen.remove(c); c.geometry?.dispose?.(); c.material?.dispose?.(); }
    });
    Object.values(this.labels).forEach((l) => l.element?.remove?.());
    this.parts = {}; this.labels = {};
  }

  /**
   * Biến học cụ PBR thành hình chiếu hologram: phát sáng (emissive), bán trong
   * suốt, cộng màu (additive). Giữ tông màu gốc để vẫn mã hoá theo bộ phận.
   * Cạnh sắc (EdgesGeometry) phát sáng để rõ khối — mặt cong trơn sẽ tự bỏ qua.
   */
  _holoize(mesh) {
    const src = mesh.material;
    const isGlow = !!(src.emissive && (src.emissiveIntensity ?? 0) > 0.1);
    const tint = new THREE.Color(isGlow ? src.emissive : (src.color ?? HOLO));
    // Vỏ trong suốt: ép tông thành màu cyan/màu bão hoà tầm trung để KHÔNG cháy
    // trắng (tông gốc như kính 0xbfe0ff gần trắng -> sẽ loé trắng qua tone-map).
    if (!isGlow) {
      const hsl = {}; tint.getHSL(hsl);
      tint.setHSL(hsl.h, Math.max(hsl.s, 0.5), THREE.MathUtils.clamp(hsl.l, 0.4, 0.56));
    }

    // Bộ phận "nguồn sáng" (tia, photon…) dùng additive để rực; vỏ trong suốt
    // (màng, bình, cốc…) dùng alpha thường + 1 mặt để KHÔNG cháy trắng khi chồng
    // lớp, nhờ vậy thấy xuyên qua và lộ được bộ phận bên trong.
    const holo = new THREE.MeshStandardMaterial({
      color: isGlow ? 0x0a1c2a : 0x06101c, emissive: tint,
      emissiveIntensity: isGlow ? 1.2 : 0.7,
      metalness: 0, roughness: 1, transparent: true, opacity: isGlow ? 0.85 : 0.32,
      blending: isGlow ? THREE.AdditiveBlending : THREE.NormalBlending,
      depthWrite: false, side: isGlow ? THREE.DoubleSide : THREE.FrontSide,
    });
    src.dispose?.();
    mesh.material = holo;
    mesh.castShadow = false; // hologram không đổ bóng đặc

    const edges = new THREE.LineSegments(
      new THREE.EdgesGeometry(mesh.geometry, 24),
      new THREE.LineBasicMaterial({ color: tint, transparent: true, opacity: 0.55, blending: THREE.AdditiveBlending, depthWrite: false })
    );
    edges.userData.holoEdge = true;
    mesh.add(edges);
  }

  _register(id, mesh, dir, labelText) {
    mesh.name = id;
    this._holoize(mesh);
    mesh.userData.base = mesh.position.clone();
    mesh.userData.dir = dir.clone().normalize();
    mesh.userData.emissiveBase = mesh.material.emissive ? mesh.material.emissive.clone() : new THREE.Color(0);
    mesh.userData.emissiveIntensityBase = mesh.material.emissiveIntensity ?? 0;
    mesh.userData.hot = false;
    this.parts[id] = mesh; this.specimen.add(mesh);
    if (labelText) {
      const el = document.createElement('div'); el.className = 'lumina-label'; el.textContent = labelText;
      const obj = new CSS2DObject(el); obj.visible = false; mesh.add(obj); this.labels[id] = obj;
    }
  }

  // ── Thư viện học cụ theo thí nghiệm ────────────────────────────────────
  _specimenSpecs(scene) {
    const V = (x, y, z) => new THREE.Vector3(x, y, z);
    const M = (geo, mat, pos) => { const m = new THREE.Mesh(geo, mat); if (pos) m.position.copy(pos); return m; };
    const P = (id, mesh, dir, label) => ({ id, mesh, dir, label });
    // Vật liệu PBR
    const glass = (c = 0xbfe0ff, o = 0.5) => new THREE.MeshPhysicalMaterial({ color: c, roughness: 0.05, transmission: 0.9, thickness: 0.6, ior: 1.4, transparent: true, opacity: o, clearcoat: 1 });
    const quartz = (c) => new THREE.MeshPhysicalMaterial({ color: c, roughness: 0.25, clearcoat: 1 });
    const metal = (c = PLATINUM) => new THREE.MeshStandardMaterial({ color: c, metalness: 0.92, roughness: 0.32 });
    const gold = () => new THREE.MeshStandardMaterial({ color: GOLD, metalness: 0.9, roughness: 0.3 });
    const plastic = (c) => new THREE.MeshStandardMaterial({ color: c, roughness: 0.5, metalness: 0 });
    const liquid = (c) => new THREE.MeshPhysicalMaterial({ color: c, roughness: 0.1, transmission: 0.55, transparent: true, opacity: 0.78, ior: 1.33 });
    const glow = (c) => new THREE.MeshStandardMaterial({ color: c, emissive: c, emissiveIntensity: 0.7, roughness: 0.4 });
    const tube = (pts, r) => new THREE.TubeGeometry(new THREE.CatmullRomCurve3(pts), 80, r, 10, false);

    switch (scene) {
      // ── SINH HỌC ──────────────────────────────────────────────────────
      case 'dna': {
        const A = [], B = [];
        for (let i = 0; i <= 16; i++) { const a = i * 0.7, y = -1.1 + i * 0.14;
          A.push(V(Math.cos(a) * 0.5, y, Math.sin(a) * 0.5)); B.push(V(Math.cos(a + Math.PI) * 0.5, y, Math.sin(a + Math.PI) * 0.5)); }
        const rung = M(new THREE.CylinderGeometry(0.03, 0.03, 1.0, 8), gold()); rung.position.set(0, 0.1, 0); rung.rotation.z = Math.PI / 2;
        return [
          P('strand-a', M(tube(A, 0.07), quartz(0x4f8bff)), V(1, 0, 0), 'Mạch đơn 5′→3′'),
          P('strand-b', M(tube(B, 0.07), quartz(0xc9a96a)), V(-1, 0, 0), 'Mạch bổ sung 3′→5′'),
          P('base-pairs', rung, V(0, 1, 0.4), 'Cặp base · A-T / G-C'),
        ];
      }
      case 'photosynthesis': {
        const chl = M(new THREE.SphereGeometry(1.0, 40, 32), glass(0x7ed17e, 0.45)); chl.scale.set(1.15, 0.72, 0.85);
        return [
          P('chloroplast', chl, V(0, 1, 0), 'Lục lạp'),
          P('grana', M(new THREE.CylinderGeometry(0.3, 0.3, 0.5, 24), plastic(0x2e9b57), V(0, 0, 0)), V(0.4, 0, 0.6), 'Grana · thylakoid'),
          P('sunlight', M(new THREE.SphereGeometry(0.22, 24, 24), glow(0xffd66b), V(1.4, 0.9, 0)), V(1, 0.6, 0), 'Ánh sáng · photon'),
        ];
      }
      // ── HOÁ HỌC ───────────────────────────────────────────────────────
      case 'titration': {
        const flask = M(new THREE.ConeGeometry(0.75, 1.1, 32, 1, true), glass(0xffffff, 0.35)); flask.position.y = -0.25;
        const sol = M(new THREE.ConeGeometry(0.6, 0.6, 32), liquid(0xff7eb0)); sol.position.y = -0.5;
        const bur = M(new THREE.CylinderGeometry(0.07, 0.07, 1.4, 20), glass(0xffffff, 0.3)); bur.position.y = 0.95;
        const drop = M(new THREE.SphereGeometry(0.05, 16, 16), liquid(0x9ad0ff)); drop.position.y = 0.18;
        return [
          P('burette', bur, V(0, 1, 0), 'Buret · dung dịch chuẩn'),
          P('drop', drop, V(0.6, 0.4, 0), 'Giọt chuẩn độ'),
          P('flask', flask, V(-1, -0.3, 0), 'Bình tam giác'),
          P('analyte', sol, V(0, -1, 0.3), 'Dung dịch cần định lượng'),
        ];
      }
      case 'electrolysis': {
        const beaker = M(new THREE.CylinderGeometry(0.75, 0.75, 1.2, 40, 1, true), glass(0xffffff, 0.3));
        const water = M(new THREE.CylinderGeometry(0.72, 0.72, 0.95, 40), liquid(0x9ad0ff)); water.position.y = -0.1;
        const anode = M(new THREE.CylinderGeometry(0.05, 0.05, 1.3, 16), metal(0xcf7b3a)); anode.position.set(-0.3, 0.2, 0);
        const cathode = M(new THREE.CylinderGeometry(0.05, 0.05, 1.3, 16), metal(0xb8bcc4)); cathode.position.set(0.3, 0.2, 0);
        return [
          P('anode', anode, V(-1, 0.3, 0), 'Cực dương (+) · O₂'),
          P('cathode', cathode, V(1, 0.3, 0), 'Cực âm (−) · H₂'),
          P('beaker', beaker, V(0, 1, 0), 'Cốc điện phân'),
          P('electrolyte', water, V(0, -1, 0), 'Dung dịch điện ly'),
        ];
      }
      case 'distillation': {
        const flask = M(new THREE.SphereGeometry(0.62, 32, 32), glass(0xffffff, 0.35)); flask.position.y = -0.5;
        const neck = M(new THREE.CylinderGeometry(0.12, 0.12, 0.7, 20), glass(0xffffff, 0.3)); neck.position.y = 0.05;
        const cond = M(new THREE.CylinderGeometry(0.1, 0.1, 1.1, 20), glass(0xbfe0ff, 0.4)); cond.position.set(0.5, 0.45, 0); cond.rotation.z = -Math.PI / 4;
        const boil = M(new THREE.SphereGeometry(0.42, 24, 24), liquid(0xffcf6b)); boil.position.y = -0.55;
        return [
          P('condenser', cond, V(1, 0.5, 0), 'Ống sinh hàn'),
          P('neck', neck, V(0, 1, 0), 'Cổ bình'),
          P('flask', flask, V(-0.4, -0.6, 0), 'Bình cầu chưng cất'),
          P('mixture', boil, V(0, -1, 0), 'Hỗn hợp đun sôi'),
        ];
      }
      case 'molecule': {
        const o = M(new THREE.SphereGeometry(0.5, 40, 40), quartz(0xff5a5a));
        const h1 = M(new THREE.SphereGeometry(0.28, 32, 32), quartz(0xf4f5f7)); h1.position.set(0.6, 0.45, 0);
        const h2 = M(new THREE.SphereGeometry(0.28, 32, 32), quartz(0xf4f5f7)); h2.position.set(-0.6, 0.45, 0);
        return [
          P('oxygen', o, V(0, -1, 0), 'Nguyên tử Oxy'),
          P('hydrogen-1', h1, V(1, 0.7, 0), 'Hydro'),
          P('hydrogen-2', h2, V(-1, 0.7, 0), 'Hydro'),
        ];
      }
      // ── VẬT LÝ ────────────────────────────────────────────────────────
      case 'pendulum': {
        const bar = M(new THREE.BoxGeometry(1.4, 0.08, 0.08), metal()); bar.position.y = 1.1;
        const str = M(new THREE.CylinderGeometry(0.012, 0.012, 1.6, 8), plastic(0x8a93a6)); str.position.y = 0.3;
        const bob = M(new THREE.SphereGeometry(0.26, 40, 40), metal(0xb8bcc4)); bob.position.y = -0.55;
        return [
          P('support', bar, V(0, 1, 0), 'Giá đỡ'),
          P('string', str, V(0.6, 0, 0), 'Dây treo · chiều dài ℓ'),
          P('bob', bob, V(0, -1, 0.4), 'Vật nặng · khối lượng m'),
        ];
      }
      case 'spring': {
        const pts = []; for (let i = 0; i <= 60; i++) { const a = i * 0.7; pts.push(V(Math.cos(a) * 0.28, 0.7 - i * 0.018, Math.sin(a) * 0.28)); }
        const spring = M(tube(pts, 0.04), metal(0xc8cdd6));
        const mass = M(new THREE.BoxGeometry(0.5, 0.5, 0.5), metal(0xb8bcc4)); mass.position.y = -0.7;
        return [
          P('spring', spring, V(0.6, 0.4, 0), 'Lò xo · độ cứng k'),
          P('mass', mass, V(0, -1, 0), 'Vật nặng · khối lượng m'),
        ];
      }
      case 'optics': {
        const prism = M(new THREE.CylinderGeometry(0.7, 0.7, 0.9, 3), glass(0xeaf2ff, 0.4)); prism.rotation.x = Math.PI / 2;
        const beam = M(new THREE.CylinderGeometry(0.03, 0.03, 1.8, 12), glow(0xffe066)); beam.position.set(-0.9, 0.1, 0); beam.rotation.z = Math.PI / 2.4;
        return [
          P('prism', prism, V(0, 1, 0), 'Lăng kính thuỷ tinh'),
          P('ray', beam, V(-1, 0.3, 0), 'Tia sáng tới'),
        ];
      }
      // ── TOÁN HỌC ──────────────────────────────────────────────────────
      case 'vectors': {
        const arrow = (color, rot) => { const m = M(new THREE.ConeGeometry(0.13, 1.4, 24), quartz(color)); m.position.set(0, 0, 0); if (rot) rot(m); return m; };
        const vx = arrow(0xff5a5a, (m) => { m.rotation.z = -Math.PI / 2; m.position.x = 0.7; });
        const vy = arrow(0x27c08a, (m) => { m.position.y = 0.7; });
        const vz = arrow(0x4f8bff, (m) => { m.rotation.x = Math.PI / 2; m.position.z = 0.7; });
        return [
          P('vec-x', vx, V(1, 0, 0), 'Vector i⃗ (Ox)'),
          P('vec-y', vy, V(0, 1, 0), 'Vector j⃗ (Oy)'),
          P('vec-z', vz, V(0, 0, 1), 'Vector k⃗ (Oz)'),
        ];
      }
      case 'polyhedron': {
        const solid = M(new THREE.DodecahedronGeometry(1.0, 0), quartz(0x6f8bff));
        const wire = new THREE.LineSegments(new THREE.WireframeGeometry(new THREE.DodecahedronGeometry(1.02, 0)),
          new THREE.LineBasicMaterial({ color: HOLO, transparent: true, opacity: 0.5, blending: THREE.AdditiveBlending, depthWrite: false }));
        wire.name = 'wire'; wire.userData.base = wire.position.clone(); wire.userData.dir = new THREE.Vector3(0, 1, 0);
        wire.userData.decor = true;
        this.specimen.add(wire); // trang trí, không phải part
        return [ P('solid', solid, V(0, 1, 0), 'Khối mười hai mặt đều') ];
      }
      // ── SINH HỌC (mặc định) ───────────────────────────────────────────
      case 'cell':
      default: {
        const mem = M(new THREE.SphereGeometry(1.0, 48, 48), glass(0xbfe0ff, 0.5));
        const nuc = M(new THREE.SphereGeometry(0.42, 40, 40), quartz(0x9a7bd6));
        const mito = (c) => new THREE.MeshStandardMaterial({ color: GOLD, metalness: 0.6, roughness: 0.35 });
        const m1 = M(new THREE.CapsuleGeometry(0.12, 0.28, 6, 12), mito()); m1.position.set(0.45, 0.2, 0.3);
        const m2 = M(new THREE.CapsuleGeometry(0.1, 0.22, 6, 12), mito()); m2.position.set(0.2, -0.35, -0.4);
        return [
          P('membrane', mem, V(0, 1, 0), 'Màng tế bào'),
          P('nucleus', nuc, V(-1, 0.4, 0.3), 'Nhân · chứa DNA'),
          P('mito-1', m1, V(0.9, 0.2, 0.6), 'Ti thể · ATP'),
          P('mito-2', m2, V(0.5, -0.6, -0.8), 'Ti thể · ATP'),
        ];
      }
    }
  }

  // ── Network ────────────────────────────────────────────────────────────
  _wireHub() {
    this.hub.on('RemoteTransform', (p) => {
      this._tBuf.push({ t: p.serverTime, quat: new THREE.Quaternion().fromArray(p.quaternion), pos: new THREE.Vector3().fromArray(p.position) });
      if (this._tBuf.length > 60) this._tBuf.shift();
    });
    this.hub.on('RemoteHighlight', ({ partId, on }) => this.highlight(partId, on, false));
    this.hub.on('RemoteExplode', ({ factor }) => { this._explodeTarget = factor; });
    this.hub.on('RemoteLabels', ({ on }) => this.setLabels(on, false));
    // Giáo viên đổi thí nghiệm -> học sinh đổi theo
    this.hub.on('RemoteScene', ({ scene }) => this.setScene(scene, false));
    // Bàn phản ứng: giáo viên thêm hóa chất / rửa cốc -> học sinh phát lại
    this.hub.on('RemoteChemAdd', ({ chemicalId }) => this.chemlab?.applyChem(chemicalId, { emit: false }));
    this.hub.on('RemoteChemReset', () => this.chemlab?.resetBench(false));
    // Thí nghiệm tham số: giáo viên kéo thanh trượt -> học sinh cập nhật
    this.hub.on('RemoteSim', ({ key, value }) => this.simlab?.setParam(key, value, false));
    this.hub.on('RemoteSimReset', () => this.simlab?.resetDefaults(false));
  }

  /** Đối tượng nhận raycast (laser + click): parts tĩnh + bàn phản ứng + sim. */
  getRaycastTargets() {
    const t = Object.values(this.parts);
    if (this.chemlab) t.push(this.chemlab.group);
    if (this.simlab) t.push(this.simlab.group);
    return t;
  }

  // ── Hành động giảng dạy (teacher phát, student nhận) ───────────────────
  highlight(partId, on, emit = true) {
    const m = this.parts[partId]; if (!m) return;
    m.userData.hot = on;
    if (m.material.emissive) m.material.emissive.set(on ? GOLD : m.userData.emissiveBase);
    // Tắt highlight phải khôi phục độ phát sáng nền của hologram (không về 0)
    if (m.material.emissiveIntensity !== undefined)
      m.material.emissiveIntensity = on ? 1.4 : m.userData.emissiveIntensityBase;
    if (emit && this.isTeacher) this.hub.invoke('SyncHighlight', partId, on).catch(() => {});
  }
  toggleHighlight(partId) {
    const m = this.parts[partId]; if (!m) return;
    this.highlight(partId, !m.userData.hot, true);
  }
  setExplode(factor, emit = true) {
    this._explodeTarget = THREE.MathUtils.clamp(factor, 0, 1);
    if (emit && this.isTeacher) this.hub.invoke('SyncExplode', this._explodeTarget).catch(() => {});
  }
  setLabels(on, emit = true) {
    this._labelsOn = on;
    Object.values(this.labels).forEach((l) => (l.visible = on));
    if (emit && this.isTeacher) this.hub.invoke('SyncLabels', on).catch(() => {});
  }
  /** Đặt lại: hết xoay, hết bóc tách, tắt highlight + nhãn (đồng bộ cả phòng). */
  reset() {
    if (this.isTeacher) this.specimen.quaternion.identity(); // frame loop sẽ phát transform
    this.setExplode(0, true);
    this.setLabels(false, true);
    Object.keys(this.parts).forEach((id) => this.highlight(id, false, true));
    this.chemlab?.resetBench(true); // bàn phản ứng: đổ cốc, dọn hiệu ứng
    this.simlab?.resetDefaults(true); // thí nghiệm tham số: về giá trị mặc định
  }

  // ── Frame loop ──────────────────────────────────────────────────────────
  _frame() {
    this.controls.update();

    const nowMs = performance.now();
    const dt = Math.min((nowMs - (this._prevFrameAt ?? nowMs)) / 1000, 0.05);
    this._prevFrameAt = nowMs;
    const t = nowMs / 1000;

    // Chùm chiếu hologram: nhịp sáng nhẹ + xoay chậm
    if (this.beam) {
      this.beam.material.opacity = 0.055 + Math.sin(t * 1.5) * 0.02;
      if (!this.reduced) this.beam.rotation.y = t * 0.12;
    }

    // Bàn phản ứng hóa học: rót, particle, lửa, chất rắn…
    if (this.chemlab) this.chemlab.update(dt, t);
    // Thí nghiệm tham số: con lắc dao động, tia khúc xạ, bọt O₂, vector…
    if (this.simlab) this.simlab.update(dt, t);

    this._explode += (this._explodeTarget - this._explode) * 0.12;
    for (const id in this.parts) {
      const m = this.parts[id];
      m.position.copy(m.userData.base).addScaledVector(m.userData.dir, this._explode * 1.5);
    }

    if (this.isTeacher) this._maybeSendTransform();
    else this._interpolateTransform();

    if (this.interaction) this.interaction.update(performance.now() / 1000);

    this.renderer.render(this.scene, this.camera);
    this.labelRenderer.render(this.scene, this.camera);

    this._frames++;
    const now = performance.now();
    if (now - this._fpsAt >= 500) {
      const fps = Math.round((this._frames * 1000) / (now - this._fpsAt));
      this._frames = 0; this._fpsAt = now;
      if (this.onTelemetry) this.onTelemetry({ fps, explode: Math.round(this._explode * 100) });
    }
  }

  _maybeSendTransform() {
    const now = performance.now();
    if (now - this._lastSendAt < this.SEND_INTERVAL) return;
    const q = this.specimen.quaternion;
    if (q.angleTo(this._lastQuat) < this.EPS) return;
    this._lastSendAt = now; this._lastQuat.copy(q);
    this.hub.invoke('SyncTransform', {
      seq: ++this._seq, position: this.specimen.position.toArray(),
      quaternion: q.toArray(), scale: 1, serverTime: 0,
    }).catch(() => {});
  }
  _interpolateTransform() {
    if (this._tBuf.length < 2) return;
    const rt = Date.now() - this.INTERP_DELAY;
    let a, b;
    for (let i = 0; i < this._tBuf.length - 1; i++)
      if (this._tBuf[i].t <= rt && this._tBuf[i + 1].t >= rt) { a = this._tBuf[i]; b = this._tBuf[i + 1]; break; }
    if (!a) { const l = this._tBuf[this._tBuf.length - 1]; this.specimen.quaternion.copy(l.quat); this.specimen.position.copy(l.pos); return; }
    const tt = THREE.MathUtils.clamp((rt - a.t) / ((b.t - a.t) || 1), 0, 1);
    this.specimen.quaternion.copy(a.quat).slerp(b.quat, tt);
    this.specimen.position.copy(a.pos).lerp(b.pos, tt);
  }

  _resize() {
    const w = this.canvas.clientWidth || innerWidth, h = this.canvas.clientHeight || innerHeight;
    this.renderer.setSize(w, h, false); this.labelRenderer.setSize(w, h);
    this.camera.aspect = w / h; this.camera.updateProjectionMatrix();
  }

  dispose() {
    removeEventListener('resize', this._onResize);
    this.chemlab?.dispose();
    this.simlab?.dispose();
    this.renderer.setAnimationLoop(null); this.renderer.dispose();
  }
}
