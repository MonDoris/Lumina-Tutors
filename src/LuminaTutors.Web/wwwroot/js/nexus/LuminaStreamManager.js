/**
 * LuminaStreamManager
 * Quản lý publish/subscribe tới SFU thuần C# qua SignalR, gắn MediaStream vào
 * <video>, và móc nối audio WebRTC vào THREE.PositionalAudio (spatial audio).
 *
 * ESM. Phụ thuộc global `signalR` (load qua <script> CDN) được truyền vào
 * dạng `hub` (HubConnection) ở constructor.
 */
import * as THREE from 'three';

const ICE = { iceServers: [{ urls: 'stun:stun.l.google.com:19302' }] };

export class LuminaStreamManager {
  /**
   * @param {any} hub  signalR HubConnection đã start
   * @param {THREE.AudioListener} listener  gắn ở camera
   * @param {{ onPeerVideo?:Function, onLevel?:Function }} [cb]
   */
  constructor(hub, listener, cb = {}) {
    this.hub = hub;
    this.listener = listener;
    this.audioCtx = listener.context;
    this.cb = cb;

    this.publisherPc = null;
    this.localStream = null;
    this.subscriptions = new Map();   // subscriptionId -> { pc, peerId }
    this.spatialByPeer = new Map();    // peerId -> { positional, analyser, sink, source }

    this._wireSignaling();
  }

  _wireSignaling() {
    // SFU gửi offer cho 1 subscription -> tạo PC, trả Answer
    this.hub.on('RtcOffer', async ({ subscriptionId, fromPeerId, sdp }) => {
      const pc = this._newPc(`sub:${subscriptionId}`);
      this.subscriptions.set(subscriptionId, { pc, peerId: fromPeerId });
      pc.ontrack = (e) => this._onRemoteTrack(fromPeerId, e.streams[0] || new MediaStream([e.track]), e.track);
      await pc.setRemoteDescription({ type: 'offer', sdp });
      const answer = await pc.createAnswer();
      await pc.setLocalDescription(answer);
      await this.hub.invoke('Answer', subscriptionId, answer.sdp);
    });

    // ICE do server trickle xuống
    this.hub.on('RtcIceCandidate', async ({ pcKey, candidate }) => {
      const pc = this._pcByKey(pcKey);
      if (pc && candidate && candidate.candidate) {
        try { await pc.addIceCandidate(candidate); } catch (_) { /* ignore */ }
      }
    });

    this.hub.on('PeerLeft', (peerId) => this._teardownPeer(peerId));
  }

  _pcByKey(pcKey) {
    if (pcKey.startsWith('publisher:')) return this.publisherPc;
    const id = pcKey.slice(4); // sau "sub:"
    return this.subscriptions.get(id)?.pc ?? null;
  }

  _newPc(pcKey) {
    const pc = new RTCPeerConnection(ICE);
    pc.onicecandidate = (e) => {
      if (e.candidate) this.hub.invoke('SendIceCandidate', pcKey, JSON.stringify(e.candidate)).catch(() => {});
    };
    return pc;
  }

  // ── LẤY camera/mic local ────────────────────────────────────────────────
  // Tách riêng khỏi SFU: giáo viên phải thấy được camera của mình NGAY, không
  // phụ thuộc việc bắt tay với SFU có thành công hay không. Nếu xin cả mic+cam
  // thất bại (thường do không có/không cấp mic) thì thử lại CHỈ camera.
  async getLocalMedia({ video = true, audio = true } = {}) {
    const vc = video ? { width: { ideal: 1280 }, height: { ideal: 720 } } : false;
    try {
      this.localStream = await navigator.mediaDevices.getUserMedia({
        video: vc,
        audio: audio ? { echoCancellation: true, noiseSuppression: true } : false,
      });
    } catch (err) {
      if (video && audio) {
        // Mic có thể bị từ chối → vẫn cho giáo viên lên hình bằng camera.
        this.localStream = await navigator.mediaDevices.getUserMedia({ video: vc, audio: false });
      } else {
        throw err;
      }
    }
    return this.localStream;
  }

  // ── ĐẨY luồng local đã lấy lên SFU (để học sinh xem) ────────────────────
  // BEST-EFFORT: lỗi ở đây KHÔNG được làm mất preview camera của giáo viên.
  async publishLocal() {
    if (!this.localStream) throw new Error('Chưa có luồng local để publish');
    this.publisherPc = this._newPc('publisher:self');
    this.localStream.getTracks().forEach((t) => this.publisherPc.addTrack(t, this.localStream));

    const offer = await this.publisherPc.createOffer();
    await this.publisherPc.setLocalDescription(offer);
    const answerSdp = await this.hub.invoke('Publish', offer.sdp);
    if (!answerSdp) throw new Error('SFU không trả answer SDP');
    await this.publisherPc.setRemoteDescription({ type: 'answer', sdp: answerSdp });
  }

  // Tương thích ngược: lấy media rồi publish luôn.
  async publish(opts) { await this.getLocalMedia(opts); await this.publishLocal(); return this.localStream; }

  async subscribe(peerId) {
    await this.hub.invoke('Subscribe', peerId); // SFU sẽ gửi 'RtcOffer'
  }

  // ── Nhận track remote ───────────────────────────────────────────────────
  _onRemoteTrack(peerId, stream, track) {
    if (track.kind === 'video') {
      let el = document.getElementById(`video-${peerId}`);
      if (!el) {
        el = document.createElement('video');
        el.id = `video-${peerId}`; el.autoplay = true; el.playsInline = true; el.muted = true;
        document.getElementById('nexus-video-pool')?.appendChild(el);
      }
      el.srcObject = stream;
      if (this.cb.onPeerVideo) this.cb.onPeerVideo(peerId, el, stream);
    } else if (track.kind === 'audio') {
      // Research Facility: ủy thác cho engine gắn PositionalAudio vào specimen.
      if (this.cb.onRemoteAudio) this.cb.onRemoteAudio(peerId, stream);
      else this._attachSpatialAudio(peerId, stream);
    }
  }

  /**
   * Gắn audio WebRTC vào THREE.PositionalAudio (PannerNode) đặt tại vị trí peer.
   * Trả về node để engine add vào avatar object3D.
   */
  _attachSpatialAudio(peerId, stream) {
    if (this.audioCtx.state === 'suspended') this.audioCtx.resume();

    // GOTCHA Chrome: stream WebRTC phải được "kéo" bởi 1 sink, nếu không
    // createMediaStreamSource sẽ câm. Gắn vào <audio muted> ẩn để mồi pipeline.
    const sink = new Audio();
    sink.muted = true; sink.srcObject = stream; sink.play().catch(() => {});

    const source = this.audioCtx.createMediaStreamSource(stream);

    const positional = new THREE.PositionalAudio(this.listener);
    positional.setNodeSource(source);
    positional.setRefDistance(2.5);
    positional.setMaxDistance(40);
    positional.setRolloffFactor(2.2);
    positional.setDistanceModel('exponential');

    const analyser = this.audioCtx.createAnalyser();
    analyser.fftSize = 256;
    source.connect(analyser);

    this.spatialByPeer.set(peerId, { positional, analyser, sink, source });
    if (this.cb.onSpatialReady) this.cb.onSpatialReady(peerId, positional);
    return positional;
  }

  getPositionalAudio(peerId) { return this.spatialByPeer.get(peerId)?.positional ?? null; }

  /** Mức âm lượng 0..1 của một peer (cho audio-visualizer glow). */
  getLevel(peerId) {
    const s = this.spatialByPeer.get(peerId);
    if (!s) return 0;
    const buf = new Uint8Array(s.analyser.frequencyBinCount);
    s.analyser.getByteFrequencyData(buf);
    let sum = 0;
    for (let i = 0; i < buf.length; i++) sum += buf[i];
    return sum / buf.length / 255;
  }

  /** Mức âm lượng từ chính mic local (teacher tự nghe glow của mình). */
  attachLocalAnalyser() {
    if (!this.localStream) return;
    const audioTrack = this.localStream.getAudioTracks()[0];
    if (!audioTrack) return;
    const src = this.audioCtx.createMediaStreamSource(new MediaStream([audioTrack]));
    this._localAnalyser = this.audioCtx.createAnalyser();
    this._localAnalyser.fftSize = 256;
    src.connect(this._localAnalyser);
  }
  getLocalLevel() {
    if (!this._localAnalyser) return 0;
    const buf = new Uint8Array(this._localAnalyser.frequencyBinCount);
    this._localAnalyser.getByteFrequencyData(buf);
    let sum = 0; for (let i = 0; i < buf.length; i++) sum += buf[i];
    return sum / buf.length / 255;
  }

  _teardownPeer(peerId) {
    const s = this.spatialByPeer.get(peerId);
    if (s) { try { s.positional.disconnect(); } catch (_) {} s.sink.srcObject = null; this.spatialByPeer.delete(peerId); }
    document.getElementById(`video-${peerId}`)?.remove();
    for (const [id, sub] of this.subscriptions) {
      if (sub.peerId === peerId) { sub.pc.close(); this.subscriptions.delete(id); }
    }
  }

  stop() {
    this.localStream?.getTracks().forEach((t) => t.stop());
    this.publisherPc?.close();
    this.subscriptions.forEach((s) => s.pc.close());
    this.subscriptions.clear();
  }
}
