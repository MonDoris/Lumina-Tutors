/**
 * LuminaInteraction — Laser Pointer 3D đồng bộ + Teacher turntable + Spatial Audio.
 *
 *   • Teacher:
 *       - Kéo chuột (trái)  -> xoay SPECIMEN (turntable), engine tự đồng bộ transform.
 *       - Di chuột (hover)  -> Raycaster lấy điểm chạm -> gửi điểm LOCAL-space (30Hz).
 *   • Student: nhận điểm -> buffer -> LERP laser tới đúng toạ độ (mượt, hết jitter).
 *
 * Laser là CON của specimen => toạ độ local, bám đúng bề mặt khi mô hình xoay/explode.
 */
import * as THREE from 'three';

export class LuminaInteraction {
  /** @param {import('./Lumina3DEngine.js').Lumina3DEngine} engine */
  constructor(engine) {
    this.engine = engine; this.hub = engine.hub; this.isTeacher = engine.isTeacher;
    this.raycaster = new THREE.Raycaster();
    this.ndc = new THREE.Vector2();

    this._laserBuf = [];                    // student: snapshot {t, p(local)}
    this._sendAt = 0; this.SEND = 1000 / 30; this.DELAY = 100;
    this._visible = false;

    this._dragging = false; this._lastX = 0; this._lastY = 0;
    this._worldUp = new THREE.Vector3(0, 1, 0);
    this._right = new THREE.Vector3();
    this._qy = new THREE.Quaternion(); this._qx = new THREE.Quaternion();

    this._buildLaser();
    if (this.isTeacher) this._bindTeacher();
    else this._bindStudent();
  }

  // ── Chấm laser: lõi sáng + halo sprite + vòng nhịp ─────────────────────
  _buildLaser() {
    this.laser = new THREE.Group(); this.laser.visible = false;
    const core = new THREE.Mesh(
      new THREE.SphereGeometry(0.035, 16, 16),
      new THREE.MeshBasicMaterial({ color: 0xffe9b0 })
    );
    const halo = new THREE.Sprite(new THREE.SpriteMaterial({
      map: this._glowTexture(), color: 0xc9a96a, blending: THREE.AdditiveBlending, depthWrite: false, transparent: true,
    }));
    halo.scale.setScalar(0.34);
    this.ring = new THREE.Mesh(
      new THREE.RingGeometry(0.06, 0.075, 32),
      new THREE.MeshBasicMaterial({ color: 0xc9a96a, transparent: true, opacity: 0.85, side: THREE.DoubleSide, depthWrite: false })
    );
    this.laser.add(core, halo, this.ring);
    this.engine.specimen.add(this.laser); // local-space của specimen
  }
  _glowTexture() {
    const c = document.createElement('canvas'); c.width = c.height = 64;
    const x = c.getContext('2d'); const g = x.createRadialGradient(32, 32, 0, 32, 32, 32);
    g.addColorStop(0, 'rgba(255,240,200,1)'); g.addColorStop(.4, 'rgba(201,169,106,.6)'); g.addColorStop(1, 'rgba(201,169,106,0)');
    x.fillStyle = g; x.fillRect(0, 0, 64, 64);
    const t = new THREE.CanvasTexture(c); t.colorSpace = THREE.SRGBColorSpace; return t;
  }

  // ── TEACHER ─────────────────────────────────────────────────────────────
  _bindTeacher() {
    const dom = this.engine.canvas;
    dom.addEventListener('pointerdown', (e) => {
      if (e.button !== 0) return;            // chỉ chuột trái = turntable
      this._dragging = true; this._dragMoved = 0; this._lastX = e.clientX; this._lastY = e.clientY;
    });
    addEventListener('pointerup', () => { this._dragging = false; });
    dom.addEventListener('pointermove', (e) => {
      if (this._dragging) { this._turntable(e); return; }
      this._aimLaser(e);                      // hover = laser
    });
    dom.addEventListener('pointerleave', () => {
      if (this._visible) { this.laser.visible = false; this._visible = false; this._sendLaser(this.laser.position, false); }
    });
    // Click (không kéo): bộ phận = highlight; lọ hóa chất = cho vào cốc.
    dom.addEventListener('click', (e) => {
      if (this._dragMoved > 6) return;        // đó là thao tác xoay, bỏ qua
      const hit = this._pickAt(e);
      if (!hit) return;
      if (hit.kind === 'chem') this.engine.chemlab?.requestAdd(hit.id);
      else if (hit.kind === 'part') this.engine.toggleHighlight(hit.id);
    });
  }

  /**
   * Raycast tổng quát: trả về { kind: 'part'|'chem'|'none', id, point } hoặc null.
   * Chỉ nhận Mesh (bỏ LineSegments cạnh hologram, Points/Sprite hiệu ứng —
   * raycast của Line/Points dùng threshold lớn gây trúng ảo).
   */
  _pickAt(e) {
    const r = this.engine.canvas.getBoundingClientRect();
    this.ndc.x = ((e.clientX - r.left) / r.width) * 2 - 1;
    this.ndc.y = -((e.clientY - r.top) / r.height) * 2 + 1;
    this.raycaster.setFromCamera(this.ndc, this.engine.camera);
    const hit = this.raycaster.intersectObjects(this.engine.getRaycastTargets(), true)
      .find((h) => h.object.isMesh);
    if (!hit) return null;
    let o = hit.object;
    while (o && o !== this.engine.specimen) {
      if (o.userData.chemId) return { kind: 'chem', id: o.userData.chemId, point: hit.point };
      if (o.name && this.engine.parts[o.name]) return { kind: 'part', id: o.name, point: hit.point };
      o = o.parent;
    }
    return { kind: 'none', id: null, point: hit.point };
  }

  // Xoay specimen: yaw quanh world-up theo dx, pitch quanh camera-right theo dy.
  _turntable(e) {
    const dx = e.clientX - this._lastX, dy = e.clientY - this._lastY;
    this._dragMoved += Math.abs(dx) + Math.abs(dy);
    this._lastX = e.clientX; this._lastY = e.clientY;
    this._right.setFromMatrixColumn(this.engine.camera.matrixWorld, 0).normalize();
    this._qy.setFromAxisAngle(this._worldUp, dx * 0.006);
    this._qx.setFromAxisAngle(this._right, dy * 0.006);
    this.engine.specimen.quaternion.premultiply(this._qy).premultiply(this._qx);
    // engine._maybeSendTransform() trong frame loop sẽ tự phát khi quaternion đổi.
  }

  _aimLaser(e) {
    const hit = this._pickAt(e);
    if (hit) {
      const local = this.engine.specimen.worldToLocal(hit.point.clone());
      this.laser.position.copy(local); this.laser.visible = true; this._visible = true;
      this._sendLaser(local, true);
    } else if (this._visible) {
      this.laser.visible = false; this._visible = false; this._sendLaser(this.laser.position, false);
    }
  }
  _sendLaser(localPt, visible) {
    const now = performance.now(); if (now - this._sendAt < this.SEND) return;
    this._sendAt = now;
    this.hub.invoke('SyncLaser', { point: localPt.toArray(), visible, serverTime: 0 }).catch(() => {});
  }

  // ── STUDENT ─────────────────────────────────────────────────────────────
  _bindStudent() {
    this.hub.on('RemoteLaser', (p) => {
      this._visible = p.visible; this.laser.visible = p.visible;
      this._laserBuf.push({ t: p.serverTime, p: new THREE.Vector3().fromArray(p.point) });
      if (this._laserBuf.length > 40) this._laserBuf.shift();
    });
  }

  _faceCamera() { if (this.ring) this.ring.lookAt(this.engine.camera.position); }

  /** Gọi mỗi frame từ engine. */
  update(time) {
    if (this.ring) this.ring.scale.setScalar(1 + Math.sin(time * 6) * 0.12);
    if (this.isTeacher) { this._faceCamera(); return; }
    if (this._laserBuf.length < 2) { this._faceCamera(); return; }
    const rt = Date.now() - this.DELAY;
    let a, b;
    for (let i = 0; i < this._laserBuf.length - 1; i++)
      if (this._laserBuf[i].t <= rt && this._laserBuf[i + 1].t >= rt) { a = this._laserBuf[i]; b = this._laserBuf[i + 1]; break; }
    const target = b ? a.p.clone().lerp(b.p, THREE.MathUtils.clamp((rt - a.t) / ((b.t - a.t) || 1), 0, 1))
                     : this._laserBuf[this._laserBuf.length - 1].p;
    this.laser.position.lerp(target, 0.5); // LERP cuối: hết jitter mạng
    this._faceCamera();
  }
}

/**
 * SPATIAL AUDIO — gắn luồng audio (WebRTC/local) vào một object 3D.
 * Listener ở camera; zoom lại gần object -> nghe to hơn (exponential distance).
 * @returns {THREE.PositionalAudio} (có ._lumiAnalyser cho visualizer)
 */
export function attachPositionalAudio(object3D, listener, mediaStream) {
  const ctx = listener.context; if (ctx.state === 'suspended') ctx.resume();
  // Mồi pipeline Chrome cho stream WebRTC (nếu không sẽ câm)
  const sink = new Audio(); sink.muted = true; sink.srcObject = mediaStream; sink.play().catch(() => {});
  const source = ctx.createMediaStreamSource(mediaStream);

  const pa = new THREE.PositionalAudio(listener);
  pa.setNodeSource(source);
  pa.setRefDistance(1.6);
  pa.setMaxDistance(30);
  pa.setRolloffFactor(2.4);
  pa.setDistanceModel('exponential');
  object3D.add(pa);

  pa._lumiAnalyser = new THREE.AudioAnalyser(pa, 64);
  pa._lumiSink = sink;
  return pa;
}
