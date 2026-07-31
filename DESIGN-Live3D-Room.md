# Kỹ thuật xây dựng Phòng học Live 3D (Real-time 3D Classroom)

> Tài liệu kiến trúc kỹ thuật cho **Lumina Holographic Nexus** — phòng học 3D real-time của Lumina Tutors.
> Cập nhật: 31/07/2026 · Đối chiếu với code thực tế tại `src/LuminaTutors.Web/Hubs/` và `wwwroot/js/nexus/`.

---

## Mục lục

1. [Mô hình 5 lớp của một phòng live 3D](#1-mô-hình-5-lớp-của-một-phòng-live-3d)
2. [Lớp Render — dựng hình 3D trên trình duyệt](#2-lớp-render--dựng-hình-3d-trên-trình-duyệt)
3. [Lớp Netcode — đồng bộ trạng thái 3D](#3-lớp-netcode--đồng-bộ-trạng-thái-3d)
4. [Lớp Media — WebRTC audio/video real-time](#4-lớp-media--webrtc-audiovideo-real-time)
5. [Lớp Presence — spatial audio, avatar, hiện diện](#5-lớp-presence--spatial-audio-avatar-hiện-diện)
6. [Lớp Sư phạm — công cụ giảng dạy trong không gian 3D](#6-lớp-sư-phạm--công-cụ-giảng-dạy-trong-không-gian-3d)
7. [Tích hợp ASP.NET Core & vấn đề scale-out](#7-tích-hợp-aspnet-core--vấn-đề-scale-out)
8. [Asset pipeline & ngân sách hiệu năng](#8-asset-pipeline--ngân-sách-hiệu-năng)
9. [Bảo mật & multi-tenancy](#9-bảo-mật--multi-tenancy)
10. [Hiện trạng Lumina Nexus — bảng đối chiếu](#10-hiện-trạng-lumina-nexus--bảng-đối-chiếu)
11. [Roadmap nâng cấp](#11-roadmap-nâng-cấp)
12. [Tham khảo](#12-tham-khảo)

---

## 1. Mô hình 5 lớp của một phòng live 3D

Một "phòng room live 3D" **không phải một công nghệ đơn lẻ** — nó là 5 hệ thống độc lập chạy song song, mỗi hệ có mô hình dữ liệu và ngân sách độ trễ riêng. Sai lầm phổ biến nhất là gộp chung media và state vào một đường truyền.

```
┌─────────────────────────────────────────────────────────────────┐
│  1. RENDER      Three.js / WebGL2 · WebGPU        60 fps, local  │
│  2. NETCODE     SignalR / WebSocket · DataChannel   20 Hz, ~80ms │
│  3. MEDIA       WebRTC SFU (DTLS-SRTP)            30 fps, ~150ms │
│  4. PRESENCE    Web Audio PannerNode · avatar      20 Hz         │
│  5. SƯ PHẠM     Raycast · laser · highlight        event-driven  │
└─────────────────────────────────────────────────────────────────┘
        ↓ tất cả hội tụ tại 1 canvas + 1 audio graph ở client
```

**Nguyên tắc vàng: tách control-plane khỏi media-plane.**
SignalR/WebSocket chỉ mang *tín hiệu* (SDP, ICE, transform, sự kiện). Media (audio/video) đi đường riêng qua WebRTC/DTLS-SRTP thẳng browser ↔ SFU. Nếu đẩy video qua SignalR Hub, băng thông và GC pressure sẽ giết server ở ~10 người dùng.

Trong Lumina Nexus, nguyên tắc này đã được thực thi đúng — trích `LuminaRtcHub.cs`:

```csharp
/// Media KHÔNG đi qua Hub. Hub chỉ là control-plane + signaling.
```

---

## 2. Lớp Render — dựng hình 3D trên trình duyệt

### 2.1 Chọn engine

| Engine | Điểm mạnh | Điểm yếu | Phù hợp khi |
|---|---|---|---|
| **Three.js** | Hệ sinh thái lớn nhất, ESM thuần, kích thước gọn, kiểm soát tối đa | Không có sẵn physics/ECS, tự viết nhiều | Scene tuỳ biến cao, tích hợp DOM overlay (đang dùng) |
| **Babylon.js** | Physics + inspector + WebXR đóng gói sẵn, TypeScript-first | Bundle nặng hơn, "opinionated" hơn | Cần physics và WebXR ngay từ đầu |
| **PlayCanvas** | Editor trực quan, streaming asset tốt | Ràng buộc vào editor/cloud | Team có designer làm scene |
| **React Three Fiber** | Khai báo, state React tự nhiên | Thêm một lớp trừu tượng, khó debug perf | Front-end đã là React |

Lumina Tutors là ASP.NET Core MVC + Razor, không React → **Three.js ESM thuần là lựa chọn đúng**.

### 2.2 WebGL2 vs WebGPU (2026)

`WebGPURenderer` đã ở trạng thái production-ready từ **r171**, và bản ổn định hiện tại là **r184** (04/2026). Điểm quan trọng:

- Đổi renderer chỉ tốn **một dòng import**; Three.js tự fallback về WebGL2 trên thiết bị cũ.
- **TSL (Three Shader Language)** cho phép viết shader một lần, compile ra cả WGSL lẫn GLSL. Từ r161 trở đi các material mới là **TSL-first** → code GLSL thuần dần trở thành nợ kỹ thuật.
- r184 đã sửa lỗi cấp phát object mỗi frame (trước đó sinh 240.000–500.000 object/giây khi render 1.000 mesh ở 60fps) — đây là lợi ích hiệu năng "miễn phí" chỉ bằng việc nâng version.

> ⚠️ **Lumina Nexus đang dùng r160** (`wwwroot/js/three/three.module.js`, `REVISION = '160'`). Tức là đứng trước cả mốc WebGPU production và toàn bộ cải tiến TSL. Đây là hạng mục nâng cấp ưu tiên cao.

### 2.3 Kỹ thuật ánh sáng & vật liệu

| Kỹ thuật | Mô tả | Trạng thái trong Nexus |
|---|---|---|
| **PBR (Physically Based Rendering)** | `MeshStandardMaterial` với metalness/roughness — vật liệu phản ứng đúng vật lý với ánh sáng | ✅ đang dùng |
| **IBL + PMREM** | `PMREMGenerator.fromScene(new RoomEnvironment())` — ánh sáng môi trường studio không cần file `.hdr` (tiết kiệm 2–8 MB tải) | ✅ đang dùng, intensity 0.012 |
| **ACES Filmic tone mapping** | Ánh xạ HDR → sRGB giữ chi tiết vùng sáng, cho cảm giác "điện ảnh" | ✅ `toneMappingExposure = 1.05` |
| **PCF Soft Shadow** | Bóng mềm, chi phí trung bình | ✅ đang dùng |
| **RectAreaLight** | Đèn panel LED trần — ánh sáng vùng thực tế | ✅ qua `RectAreaLightUniformsLib` |
| **Additive blending + emissive** | Hiệu ứng hologram tự phát sáng, không phụ thuộc IBL | ✅ ring emitter, beam |
| **FogExp2** | Sương mũ mờ dần theo khoảng cách, che culling xa | ✅ `density 0.045` |
| **Post-processing (Bloom/SSAO)** | Tăng chất lượng thị giác đáng kể cho scene hologram | ❌ chưa có — cơ hội nâng cấp |

### 2.4 Overlay DOM trong không gian 3D

Vấn đề kinh điển: nhãn (label) chú thích bộ phận cần **chữ sắc nét + chọn được bằng chuột**, nhưng texture 3D thì mờ và không tương tác được.

**Giải pháp: `CSS2DRenderer`** — render một lớp `<div>` trong suốt phủ lên canvas, mỗi label là một DOM node được định vị theo phép chiếu camera mỗi frame.

```js
this.labelRenderer.domElement.style.cssText =
  'position:absolute;inset:0;pointer-events:none;z-index:3';
// ...
this.renderer.render(this.scene, this.camera);
this.labelRenderer.render(this.scene, this.camera);  // 2 lần render mỗi frame
```

Ưu: chữ nét ở mọi zoom, dùng được CSS/animation, accessible (screen reader đọc được).
Nhược: >100 label sẽ gây layout thrashing → khi đó chuyển sang `CSS3DRenderer` hoặc SDF text (`troika-three-text`).

### 2.5 Tối ưu render

- **`setPixelRatio(Math.min(devicePixelRatio, 2))`** — chặn retina 3x/4x đốt 9–16× fillrate. ✅ đã áp dụng.
- **Instancing** (`InstancedMesh`) — nhiều avatar/hạt giống nhau chỉ tốn 1 draw call. Bắt buộc khi >30 học sinh.
- **LOD** (`THREE.LOD`) — giảm polygon theo khoảng cách camera.
- **Frustum culling** — Three.js bật mặc định; đừng tắt.
- **`prefers-reduced-motion`** — tắt animation nền cho người nhạy cảm tiền đình. ✅ đã có (`this.reduced`).
- **`setAnimationLoop`** thay vì `requestAnimationFrame` — bắt buộc để tương thích WebXR sau này. ✅ đã dùng.
- **Service Worker cache-first** cho thư viện 3D — lần vào sau tải từ cache (<5 ms) thay vì mạng. ✅ `wwwroot/sw-3d.js`, cache `lumina-3d-v3`.

---

## 3. Lớp Netcode — đồng bộ trạng thái 3D

Đây là phần khó nhất và cũng là phần quyết định "phòng live" có mượt hay không.

### 3.1 Mô hình thẩm quyền (Authority)

| Mô hình | Cách hoạt động | Dùng khi |
|---|---|---|
| **Server-authoritative** | Server mô phỏng, client chỉ gửi input | Game cạnh tranh, chống gian lận |
| **Host-authoritative** | Một client (giáo viên) là nguồn chân lý, server chỉ relay | **Lớp học** — đúng ngữ nghĩa sư phạm |
| **Distributed/CRDT** | Ai cũng ghi được, hợp nhất tự động | Bảng trắng cộng tác |

Lumina Nexus dùng **host-authoritative** với giáo viên là host, và **kiểm tra vai trò ở server** — đây là điểm làm đúng:

```csharp
public async Task SyncTransform(TransformPayload payload)
{
    if (!Participants.TryGetValue(Context.ConnectionId, out var p) || p.Role != "teacher") return;
    payload.ServerTime = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
    await Clients.OthersInGroup(p.RoomId).SendAsync("RemoteTransform", payload);
}
```

Hai chi tiết quan trọng: (1) học sinh giả mạo gói `SyncTransform` sẽ bị chặn ở server, không tin client; (2) **server đóng dấu `ServerTime`**, không dùng `Date.now()` của client — nếu dùng đồng hồ client thì lệch giờ máy học sinh sẽ phá vỡ nội suy.

### 3.2 Giảm tải đường truyền: Throttle + Dead-band

Gửi transform mỗi frame = 60 Hz × N học sinh = bão tin nhắn. Hai kỹ thuật kết hợp:

```js
this.SEND_HZ = 20;                    // throttle: tối đa 20 gói/giây
this.SEND_INTERVAL = 1000 / this.SEND_HZ;
this.EPS = 0.0008;                    // dead-band: bỏ qua thay đổi nhỏ hơn ngưỡng
```

- **Throttle 20 Hz** — dưới ngưỡng cảm nhận khi có nội suy, giảm 3× lưu lượng so với 60 Hz.
- **Dead-band `EPS`** — vật thể đứng yên thì **không gửi gì cả**. Trong lớp học, phần lớn thời gian mô hình bất động → tiết kiệm 90%+ băng thông.

### 3.3 Snapshot Interpolation — chống giật

Mạng không đều: gói đến lúc 95 ms, lúc 140 ms. Nếu áp trực tiếp vị trí nhận được, vật thể sẽ giật.

**Kỹ thuật: đệm và phát chậm lại.** Client giữ buffer snapshot và render **quá khứ 100 ms**, luôn nội suy giữa hai snapshot đã có:

```js
this.INTERP_DELAY = 100;   // render trạng thái của 100ms trước
this._tBuf = [];           // buffer tối đa 60 snapshot

_interpolateTransform() {
  const rt = Date.now() - this.INTERP_DELAY;
  // tìm cặp (a, b) sao cho a.t <= rt <= b.t
  const tt = (rt - a.t) / (b.t - a.t);
  this.specimen.quaternion.copy(a.quat).slerp(b.quat, tt);   // SLERP cho rotation
  this.specimen.position.copy(a.pos).lerp(b.pos, tt);        // LERP cho position
}
```

**Vì sao SLERP chứ không LERP cho quaternion:** LERP trên quaternion cho tốc độ góc **không đều** (nhanh ở giữa, chậm ở hai đầu) và kết quả chưa chuẩn hoá. SLERP nội suy trên mặt cầu đơn vị → xoay đều, đúng vật lý. Đây là lý do transform 3D **luôn** truyền quaternion `[x,y,z,w]` chứ không phải góc Euler — Euler bị gimbal lock và không nội suy được liên tục.

**Đánh đổi:** học sinh luôn thấy chậm hơn giáo viên 100 ms + RTT/2. Với lớp học, đây là đánh đổi đúng — mượt quan trọng hơn tức thời. Với game FPS thì phải thêm client-side prediction + rollback.

### 3.4 Kênh truyền: chọn đúng ống

| Kênh | Giao thức | Độ trễ | Mất gói | Dùng cho |
|---|---|---|---|---|
| **WebSocket / SignalR** | TCP | Trung bình | Không (retransmit → head-of-line blocking) | Sự kiện rời rạc, chat, điều khiển ✅ |
| **WebRTC DataChannel** | UDP/SCTP (cấu hình được unreliable) | Thấp nhất | Có thể chấp nhận | Transform tần số cao, >30 Hz |
| **WebTransport (HTTP/3)** | QUIC | Thấp | Cấu hình được | Thay thế hiện đại cho cả hai, hỗ trợ đang mở rộng |

Khác biệt cốt lõi: **WebSocket dùng TCP nên gói trễ sẽ chặn toàn bộ gói sau** (head-of-line blocking). Với dữ liệu transform, gói cũ đến muộn là **vô giá trị** — ta muốn vứt nó đi, không phải chờ nó.

Ở 20 Hz với dead-band, SignalR/WebSocket là **đủ tốt và đơn giản hơn nhiều**. Chỉ chuyển sang DataChannel unreliable khi nâng lên 30–60 Hz hoặc đồng bộ nhiều avatar di chuyển liên tục.

### 3.5 State reconciliation — người vào sau

Học sinh vào muộn phải thấy đúng trạng thái phòng. Kỹ thuật: **server giữ state phòng, phát lại khi join**.

```csharp
private static readonly ConcurrentDictionary<string, string> RoomScenes = new();
private static readonly ConcurrentDictionary<string, ConcurrentQueue<string>> RoomChems = new();
private static readonly ConcurrentDictionary<string, ConcurrentDictionary<string, double>> RoomSims = new();

await Clients.Caller.SendAsync("RoomJoined", new {
    roomId, selfId = Context.ConnectionId, role = p.Role,
    scene = RoomScenes.GetValueOrDefault(roomId),
    chem  = ..., sims = ..., peers, roster
});
```

Ba loại state được phân biệt rõ:
- **Scalar state** (scene hiện tại, tham số sim) → lưu giá trị cuối, gửi thẳng.
- **Event log** (hoá chất đã đổ vào cốc) → `ConcurrentQueue`, phát lại theo thứ tự. Có giới hạn 30 phần tử chống spam.
- **Continuous state** (transform) → **không cần lưu**, snapshot kế tiếp (≤50 ms) sẽ tự sửa.

Điểm tinh tế trong code: khi giáo viên F5 và gửi lại **cùng** scene thì state cốc được **giữ nguyên**; chỉ khi đổi sang scene **khác** mới reset.

```csharp
var changed = !RoomScenes.TryGetValue(p.RoomId, out var cur) || cur != scene;
RoomScenes[p.RoomId] = scene;
if (changed) { RoomChems.TryRemove(p.RoomId, out _); RoomSims.TryRemove(p.RoomId, out _); }
```

> ⚠️ Toàn bộ state đang nằm trong `static ConcurrentDictionary` **in-process**. Restart app = mất phòng; chạy 2 instance = hai phòng khác nhau cùng mã. Xem [§7.2](#72-scale-out).

---

## 4. Lớp Media — WebRTC audio/video real-time

### 4.1 Ba topology

```
MESH (P2P)              SFU (Selective Forwarding)      MCU (Multipoint Control)
  A ─── B                    A ──┐                          A ──┐
  │ ╲ ╱ │                    B ──┼──► [SFU] ──► tất cả       B ──┼──► [MCU] ──► 1 luồng trộn
  │ ╳   │                    C ──┘   (forward RTP)           C ──┘   (decode+mix+encode)
  D ─── C
n×(n-1) kết nối          n up + n×(n-1) down            n up + n down
CPU server: 0            CPU server: thấp                CPU server: rất cao
Trần: 4–6 người          Trần: 50–200/node               Trần: giới hạn bởi CPU
```

- **Mesh**: chỉ dùng cho 1-1 hoặc nhóm ≤4. Mỗi client phải encode video n-1 lần → điện thoại nóng và hết pin.
- **SFU**: chuẩn công nghiệp cho lớp học. Server **chỉ chuyển tiếp gói RTP, không giải mã** → CPU thấp, chất lượng không suy giảm.
- **MCU**: chỉ hợp lý khi cần một luồng duy nhất (ghi hình, phát ra RTMP/YouTube, thiết bị yếu).

Lumina Nexus dùng **SFU thuần C# tự viết trên SIPSorcery**:

```csharp
/// Mỗi publisher : 1 RTCPeerConnection RecvOnly (server NHẬN audio + video).
/// Mỗi subscription: 1 RTCPeerConnection SendOnly (server GỬI track của 1 publisher).
/// Forward gói RTP thô (OnRtpPacketReceived -> SendRtpRaw), KHÔNG transcode.
```

### 4.2 Tự viết SFU vs dùng SFU có sẵn

| | SFU tự viết (SIPSorcery) | mediasoup | LiveKit |
|---|---|---|---|
| Ngôn ngữ | C# — cùng process với ASP.NET Core | C++ core, điều khiển từ Node.js | Go (trên Pion) |
| Triển khai | Zero — chung 1 app | Thêm process Node + worker | Thêm service, có Helm chart chính thức |
| **Simulcast** | ❌ | ✅ | ✅ mặc định cho VP8/H.264 |
| **SVC (VP9/AV1)** | ❌ | ✅ (AV1 SVC vào core sớm) | ✅ tự chuyển sang SVC cho VP9/AV1 |
| Bandwidth estimation | ❌ | ✅ | ✅ + **Dynacast** (tự dừng layer không ai xem) |
| Ghi hình / egress | Tự làm | Plugin | Có sẵn |
| Kiểm soát byte path | Toàn bộ | Cao | Trung bình |

**Đánh giá thực tế cho Lumina Nexus:** SFU tự viết là lựa chọn hợp lý ở quy mô hiện tại (1 giáo viên phát, N học sinh xem — mô hình *một chiều*, không phải hội nghị đối xứng). Nó loại bỏ hoàn toàn một service phải vận hành. Nhưng cần biết rõ trần của nó:

- **Codec cứng: VP8 + Opus.** VP8 không có phần cứng tăng tốc trên phần lớn thiết bị → encode bằng CPU, tốn pin điện thoại. Không có AV1/H.264.
  ```csharp
  private static AudioFormat OpusFormat() => new(AudioCodecsEnum.OPUS, 111, 48000, 2, "");
  private static VideoFormat Vp8Format()  => new(VideoCodecsEnum.VP8, 96, 90000, "");
  ```
- **Không simulcast** → học sinh mạng 3G và học sinh mạng cáp quang nhận **cùng một** luồng. Ai yếu hơn sẽ giật. Đây là giới hạn cảm nhận được rõ nhất trong lớp học thật.
- **Không bandwidth estimation** → không tự hạ bitrate khi mạng xấu.

Ngưỡng nên chuyển sang LiveKit/mediasoup: khi cần **>2 người phát đồng thời**, hoặc khi phàn nàn về giật ở học sinh mạng yếu trở thành vấn đề vận hành.

### 4.3 Signaling & NAT traversal

**Luồng bắt tay** (đã triển khai đúng chuẩn trong Nexus):

```
Publisher (giáo viên)                Subscriber (học sinh)
  createOffer                          SFU gọi CreateSubscriptionAsync
  → hub.invoke('Publish', sdp)         → SFU tạo offer, đẩy 'RtcOffer' xuống client
  ← answer SDP từ SFU                  → client createAnswer
  setRemoteDescription                 → hub.invoke('Answer', subId, sdp)
        ↕ trickle ICE hai chiều qua hub.invoke('SendIceCandidate', pcKey, json)
```

**Trickle ICE** — gửi ICE candidate ngay khi tìm được thay vì chờ gom đủ. Rút ngắn thời gian kết nối từ ~10 s xuống <1 s. Nexus dùng `pcKey` dạng `"publisher:{connId}"` / `"sub:{subId}"` để định tuyến candidate về đúng PeerConnection — cách đặt khoá đơn giản và hiệu quả.

**STUN vs TURN** — điểm rủi ro vận hành lớn nhất:

- **STUN** chỉ *cho biết* IP public của bạn. Đủ cho phần lớn NAT gia đình.
- **TURN** *chuyển tiếp* toàn bộ media qua server relay. **Bắt buộc** khi: NAT đối xứng (rất phổ biến ở mạng 4G/5G Việt Nam), tường lửa doanh nghiệp/trường học chặn UDP, hoặc mạng chỉ cho ra port 443.

> ⚠️ `appsettings.json` hiện chỉ có `stun:stun.l.google.com:19302`. Nghĩa là **một tỉ lệ học sinh dùng 4G sẽ không nhận được video** và không có cách nào chẩn đoán từ phía họ. Đây là hạng mục P0. Dựng **coturn** (`turn:host:3478` + `turns:host:443` cho mạng chặn UDP) và điền vào `Webrtc:IceServers` — cấu hình đã sẵn sàng nhận, chỉ thiếu server.

**Bảo mật media:** WebRTC bắt buộc mã hoá **DTLS-SRTP** — không có chế độ plaintext. SIPSorcery tự terminate DTLS, nên media giữa browser và server luôn được mã hoá mà không cần cấu hình gì thêm.

### 4.4 Cạm bẫy đã xử lý trong code

Hai gotcha kinh điển đã được xử lý đúng — đáng ghi lại để không bị "tối ưu" bay mất:

```js
// 1. Preview camera phải độc lập với SFU: getLocalMedia() tách khỏi publishLocal().
//    Giáo viên phải thấy mình NGAY, kể cả khi bắt tay SFU thất bại.
async publishLocal() { /* BEST-EFFORT: lỗi ở đây KHÔNG được làm mất preview */ }

// 2. Mic bị từ chối → thử lại CHỈ camera, thay vì fail toàn bộ.
catch (err) {
  if (video && audio) this.localStream = await getUserMedia({ video: vc, audio: false });
}
```

### 4.5 Ghi hình buổi học: client-side vs server-side egress

Có hai cách ghi lại một phòng live 3D:

| | **Client-side** (`MediaRecorder`) | **Server-side egress** |
|---|---|---|
| Cách làm | `canvas.captureStream()` + trộn audio track → `MediaRecorder` → upload `.webm` | SFU decode + compose + encode ở server |
| CPU server | 0 | Rất cao (giống MCU) |
| Ghi được scene 3D | ✅ đúng những gì giáo viên thấy | Cần renderer headless |
| Rủi ro | Mất bản ghi nếu máy giáo viên crash/mất mạng; tốn upload của giáo viên | Không |
| Chi phí | Gần như 0 | Đáng kể |

Nexus dùng **client-side** — hợp lý cho ngữ cảnh, vì thứ cần ghi chính là **canvas 3D** chứ không phải lưới video:

```js
const mime = ['video/webm;codecs=vp9,opus', 'video/webm;codecs=vp8,opus', 'video/webm']
  .find((m) => MediaRecorder.isTypeSupported(m)) || '';
_nxRec.mr = new MediaRecorder(mixed, { mimeType: mime, videoBitsPerSecond: 3000000 });
// ... kết thúc → fetch('/Recording/Save', { method: 'POST', body: fd })
```

`RecordingController.Save` nhận file tối đa ~600 MB (`RequestSizeLimit`). Ở 3 Mbps, 600 MB ≈ **26 phút**. Một tiết học 45 phút sẽ **vượt giới hạn và mất bản ghi**. Hai hướng xử lý: hạ `videoBitsPerSecond` xuống ~1,5 Mbps (đủ cho scene 3D vốn ít nhiễu), hoặc chuyển sang **chunked upload** — `MediaRecorder.start(timeslice)` rồi upload từng mảnh, vừa bỏ trần dung lượng vừa không mất trắng khi trình duyệt crash giữa buổi.

---

## 5. Lớp Presence — spatial audio, avatar, hiện diện

### 5.1 Spatial audio: nối WebRTC vào Web Audio

Đây là kỹ thuật tạo cảm giác "ở cùng một phòng": tiếng nói phát ra **từ vị trí** của người nói trong không gian 3D, nhỏ dần theo khoảng cách.

Chuỗi kết nối: `MediaStream (WebRTC)` → `MediaStreamAudioSourceNode` → `PannerNode` (bọc trong `THREE.PositionalAudio`) → `AudioListener` gắn ở camera.

```js
const source = this.audioCtx.createMediaStreamSource(stream);
const positional = new THREE.PositionalAudio(this.listener);
positional.setNodeSource(source);
positional.setRefDistance(2.5);        // khoảng cách bắt đầu suy giảm
positional.setMaxDistance(40);
positional.setRolloffFactor(2.2);      // tốc độ suy giảm
positional.setDistanceModel('exponential');
```

**Gotcha quan trọng nhất — đã được ghi chú trong code:**

```js
// GOTCHA Chrome: stream WebRTC phải được "kéo" bởi 1 sink, nếu không
// createMediaStreamSource sẽ câm. Gắn vào <audio muted> ẩn để mồi pipeline.
const sink = new Audio();
sink.muted = true; sink.srcObject = stream; sink.play().catch(() => {});
```

Chrome không "bơm" MediaStream từ WebRTC vào Web Audio API trừ khi có một sink đang tiêu thụ nó. Không có `<audio muted>` ẩn này, spatial audio **im lặng hoàn toàn** mà không có lỗi nào. Đây là bug tốn hàng giờ debug nếu chưa biết.

**Autoplay policy:** `AudioContext` khởi tạo ở trạng thái `suspended` cho tới khi có tương tác người dùng. Code đã gọi `resume()` — nhưng cần đảm bảo nó chạy sau một click thật (nút "Vào phòng"), không phải lúc load trang.

### 5.2 Audio-reactive visual

`AnalyserNode` (`fftSize = 256`) đọc mức âm lượng theo thời gian thực để làm avatar phát sáng khi nói:

```js
getLevel(peerId) {
  const buf = new Uint8Array(s.analyser.frequencyBinCount);
  s.analyser.getByteFrequencyData(buf);
  let sum = 0; for (let i = 0; i < buf.length; i++) sum += buf[i];
  return sum / buf.length / 255;   // 0..1
}
```

Chi tiết hay: có cả `attachLocalAnalyser()` để giáo viên **tự thấy** glow của mình — phản hồi tức thì xác nhận mic đang hoạt động, giảm hẳn câu hỏi "em có nghe thầy không?".

### 5.3 Roster vs Publisher list

Một phân biệt dễ bỏ sót nhưng quan trọng, đã làm đúng trong `JoinRoom`:

- `peers` — chỉ những người **đang phát media** (để client subscribe).
- `roster` — **mọi người trong phòng**, kể cả học sinh chỉ ngồi xem (để hiển thị danh sách + điểm danh).

Gộp hai khái niệm này là lỗi thiết kế phổ biến, dẫn tới "điểm danh thiếu học sinh không bật camera".

---

## 6. Lớp Sư phạm — công cụ giảng dạy trong không gian 3D

Đây là phần **tạo khác biệt** giữa "một scene 3D đẹp" và "một phòng học 3D dùng được".

| Công cụ | Kỹ thuật nền | Payload đồng bộ |
|---|---|---|
| **Laser pointer** | `Raycaster` từ chuột → điểm giao với mesh, chuyển sang **local-space của specimen**; gửi 30 Hz, học sinh buffer + LERP với delay 100 ms | `LaserPayload { Point[3], Visible, ServerTime }` |
| **Turntable xoay mẫu vật** | Tắt `controls.enableRotate` cho giáo viên, kéo chuột = xoay object thay vì camera | `TransformPayload` |
| **Highlight bộ phận** | Đổi `emissive` của material theo `partId` | `{ partId, on }` |
| **Explode view** | Mỗi part có `userData.base` + `userData.dir`, dịch theo hệ số 0..1 | `{ factor }` |
| **Interactive labels** | `CSS2DObject` gắn vào từng part | `{ on }` |
| **Đổi thí nghiệm** | Thay các mesh con trong `specimen` Group | `{ scene }` + lưu server-side |
| **Giơ tay** | Sự kiện thuần | `{ peerId, displayName }` |

**Chi tiết thiết kế xuất sắc — laser toạ độ local-space:** nếu truyền toạ độ world, mẫu vật xoay là điểm laser trôi khỏi vị trí giáo viên trỏ. Lưu ở local-space của specimen → laser **dính** vào bộ phận được trỏ dù mẫu vật xoay thế nào, và tự động đúng ngay cả khi transform chưa kịp đồng bộ.

**Chi tiết thứ hai — specimen là Group cố định:**

```js
// Specimen là 1 Group cố định trên pedestal; setScene() chỉ thay các bộ phận
// con bên trong -> Laser (con của group) và trạng thái xoay được giữ nguyên.
```

Đổi thí nghiệm không phá vỡ laser đang bật hay góc xoay hiện tại. Đây là kiểu quyết định kiến trúc nhỏ nhưng tiết kiệm rất nhiều bug về sau.

**Explode với easing ở client:**

```js
this._explode += (this._explodeTarget - this._explode) * 0.12;   // exponential smoothing
```

Server chỉ gửi giá trị đích; client tự nội suy mượt. Nguyên tắc chung: **truyền target, animate ở client** — vừa giảm băng thông vừa mượt hơn truyền từng frame.

---

## 7. Tích hợp ASP.NET Core & vấn đề scale-out

### 7.1 Cấu trúc hiện tại

```
LuminaNexusController  →  View Index.cshtml (Layout=null, fullscreen canvas)
                          ├─ importmap: "three" → /js/three/three.module.js
                          ├─ NEXUS_CONFIG.iceServers  (server bơm xuống, đồng bộ với SFU)
                          └─ SignalR client → /hubs/lumina-rtc
                                                   ↓
LuminaRtcHub  [Authorize(Policy = "LabAccess")]
   ├─ signaling: Publish / Subscribe / Answer / SendIceCandidate  →  ILuminaSfuService
   ├─ state sync: SyncTransform / SyncLaser / SyncHighlight / SyncExplode / SyncLabels
   ├─ lab state:  SetScene / SyncSim / SimReset / ChemAdd / ChemReset
   └─ presence:   UpdateAvatar / RaiseHand / LowerHand / SwitchMode / EndRoom
                                                   ↓
LuminaSfuService (SIPSorcery)  →  RTP forwarding, DTLS-SRTP
```

Điểm làm đúng: **ICE servers do server bơm xuống client** (`BuildClientIceServersJson()`), đảm bảo SFU và browser luôn dùng **cùng một** bộ STUN/TURN. Cấu hình lệch nhau giữa hai bên là nguồn lỗi kết nối rất khó chẩn đoán.

### 7.2 Scale-out

Hiện tại **toàn bộ hệ thống ràng buộc vào một process**:

```csharp
private static readonly ConcurrentDictionary<string, Participant> Participants = new();
private static readonly ConcurrentDictionary<string, ConcurrentDictionary<string, byte>> Rooms = new();
```

Hệ quả khi chạy nhiều instance:

| Vấn đề | Nguyên nhân |
|---|---|
| Hai người cùng mã phòng vào hai instance khác nhau → hai phòng riêng biệt | State in-memory không chia sẻ |
| Restart/deploy = mọi phòng biến mất | Không persist |
| SFU giữ `RTCPeerConnection` trong RAM của đúng instance đó | Media không thể fail-over |

**Hướng xử lý theo thứ tự chi phí tăng dần:**

1. **Sticky session (ARR affinity) + 1 instance/phòng.** Rẻ nhất, đủ cho quy mô trung tâm nhỏ. Nhược: không HA.
2. **Redis backplane cho SignalR.** SignalR không chia sẻ state giữa các server; Redis pub/sub chuyển tiếp tin nhắn qua các node. Cần lưu ý: **backplane forward mọi tin nhắn tới mọi node** → nó chính là điểm nghẽn, và throughput thấp hơn khi chỉ có một node. Với tin nhắn 20 Hz × số phòng, cần đo tải trước khi cam kết. Redis **phải cùng datacenter** với app.
3. **Room affinity / routing layer.** Hash `roomId` → chọn node cố định. Giữ được lợi ích locality của SFU, tránh backplane cho traffic tần số cao. Đây là hướng đúng về dài hạn cho hệ có media.
4. **Azure SignalR Service** nếu deploy trên Azure — thay thế backplane, nhưng lưu ý giới hạn theo tier (free: 20 kết nối đồng thời/unit, 20.000 tin nhắn/ngày; standard: 1.000 kết nối/unit, tối đa 100 unit).

**Tối ưu payload trước khi nghĩ tới scale:** bật **MessagePack** thay JSON cho SignalR. `TransformPayload` (3 float + 4 float + int + long) ở JSON tốn ~180 byte; MessagePack còn ~60 byte. Ở 20 Hz × 30 học sinh × 100 phòng, đây là khác biệt giữa 10 MB/s và 3,6 MB/s.

---

## 8. Asset pipeline & ngân sách hiệu năng

### 8.1 Pipeline mô hình 3D

```
Blender/CAD  →  glTF 2.0 (.glb)  →  nén hình học  →  nén texture  →  CDN
                                     Draco / Meshopt   KTX2 + Basis
```

| Kỹ thuật | Giảm | Đánh đổi |
|---|---|---|
| **Draco** | 90–95% kích thước hình học | Tốn ~50–200 ms giải nén trên main thread (dùng worker) |
| **Meshopt** | 60–80%, giải nén nhanh hơn Draco nhiều | Tỉ lệ nén thấp hơn |
| **KTX2 + Basis Universal** | Texture giữ nguyên dạng nén **trên GPU** — giảm VRAM 4–6× | Cần transcoder, chất lượng thấp hơn PNG chút |
| **Instancing** | n draw call → 1 | Chỉ áp dụng cho mesh giống nhau |

Với scene hologram tự sinh bằng code (như Nexus hiện tại), pipeline này chưa cần. Nó trở nên bắt buộc khi nhập mô hình giải phẫu/phân tử thật từ file ngoài.

### 8.2 Ngân sách hiệu năng đề xuất

| Chỉ số | Mục tiêu desktop | Mục tiêu mobile tầm trung |
|---|---|---|
| FPS | 60 | ≥30 |
| Draw calls | <150 | <80 |
| Triangles | <500K | <150K |
| VRAM texture | <256 MB | <96 MB |
| Tải lần đầu | <3 s (4G) | <5 s |
| Băng thông state | <5 KB/s/client | <5 KB/s |
| Băng thông video | 1,2 Mbps @720p | 600 kbps @480p (cần simulcast) |
| Độ trễ end-to-end | <200 ms | <300 ms |

Nexus đã có sẵn cơ chế đo — `onTelemetry` phát FPS mỗi 500 ms. Nên mở rộng để log thêm draw calls (`renderer.info.render.calls`) và số byte đã gửi (`RTCPeerConnection.getStats()`).

---

## 9. Bảo mật & multi-tenancy

### 9.1 Nguyên tắc

1. **Không tin client.** Mọi hành động thay đổi state chung phải kiểm tra vai trò **ở server**. ✅ Nexus làm đúng — mọi `Sync*` đều có `if (p.Role != "teacher") return;`
2. **Validate mọi payload.** ✅ Có giới hạn độ dài, `double.IsFinite`, clamp giá trị:
   ```csharp
   if (key.Length is 0 or > 20 || !double.IsFinite(value)) return;
   var clamped = Math.Clamp(factor, 0f, 1f);
   if (q.Count >= 30) return; // chống spam / cốc đầy
   ```
3. **Rate limiting per-connection.** ⚠️ Chưa có. Một client sửa JS có thể spam `SyncTransform` ở 1000 Hz và làm ngập cả phòng. Cần token bucket theo `ConnectionId`.
4. **TURN credentials ngắn hạn.** Không hard-code user/pass TURN; dùng cơ chế `timestamp:username` + HMAC theo chuẩn coturn REST API, hạn 1 giờ.

### 9.2 Ba lỗ hổng cần vá (ưu tiên cao)

**(a) Mã phòng 6 chữ số không có rate limit → brute-force được.**

```csharp
private static string GenerateRoomCode() => Random.Shared.Next(100000, 1000000).ToString();
```

900.000 tổ hợp. Một script thử 100 mã/giây tìm được một phòng đang mở trong vài phút. Cần: rate limit theo IP/user trên endpoint join, và/hoặc nâng lên mã 8 ký tự alphanumeric cho phòng nhạy cảm. Lưu ý mã 6 số là quyết định UX có chủ đích (gõ nhanh trên bàn phím số điện thoại) — nên **giữ mã ngắn nhưng thêm rate limit + hạn dùng**, thay vì kéo dài mã.

**(b) Phòng không được lưu server-side → không có `SchoolId` để kiểm tra.**

Gốc rễ nằm ở `Create`: nó **không tạo bản ghi phòng nào cả**, chỉ sinh mã rồi redirect kèm query string.

```csharp
public IActionResult Create(string? subject = null)
{
    if (!(User.IsInRole("TEACHER") || User.IsInRole("ADMIN"))) return RedirectToAction(nameof(Index));
    return RedirectToAction(nameof(Index), new { room = GenerateRoomCode(), subject });
}
```

Phòng chỉ "tồn tại" khi có người gọi `JoinRoom` — và Hub nhận **bất kỳ chuỗi `roomId` nào**, không đối chiếu `SchoolId`. `SchoolId` trong `LuminaNexusController` hiện chỉ dùng để tra hồ sơ giáo viên đoán môn học, không dùng để phân vùng phòng.

CLAUDE.md quy định: *"Every `TenantEntity` carries `SchoolId`. When querying, always filter by `SchoolId` from the authenticated user's claims."* Hub đang là ngoại lệ → người dùng đã đăng nhập ở **trường A** biết mã phòng của **trường B** sẽ vào được.

Cách vá: tạo entity `NexusRoom { Code, SchoolId, OwnerUserId, SubjectTag, ExpiresAt }` (kế thừa `TenantEntity`), ghi khi `Create`, và trong `JoinRoom` đối chiếu `room.SchoolId` với claim `SchoolId`. Việc này giải quyết đồng thời lỗ hổng (b) và (c), đồng thời mở đường cho persist state ở P1.

**(c) `IsTeacher` chỉ dựa trên role claim, không dựa trên "ai tạo phòng".**

```csharp
private bool IsTeacher => Context.User?.IsInRole("TEACHER") == true
                       || Context.User?.IsInRole("ADMIN") == true;
```

**Bất kỳ** giáo viên nào vào phòng đều có toàn quyền điều khiển scene — kể cả giáo viên không dạy lớp đó. Cần lưu `roomId → ownerUserId` và chỉ cấp quyền `teacher` cho chủ phòng (hoặc người được chủ phòng uỷ quyền).

---

## 10. Hiện trạng Lumina Nexus — bảng đối chiếu

| Lớp | Kỹ thuật | Trạng thái |
|---|---|---|
| **Render** | Three.js ESM + importmap | ✅ nhưng r160 (mới nhất r184) |
| | PBR + PMREM + ACES tone mapping | ✅ |
| | CSS2DRenderer cho label | ✅ |
| | Service Worker cache thư viện | ✅ `sw-3d.js` |
| | `prefers-reduced-motion` | ✅ |
| | WebGPURenderer / TSL | ❌ |
| | Post-processing (Bloom/SSAO) | ❌ |
| | Instancing / LOD | ❌ (chưa cần ở quy mô hiện tại) |
| **Netcode** | Host-authoritative + kiểm tra role server-side | ✅ |
| | Throttle 20 Hz + dead-band | ✅ |
| | Snapshot interpolation 100 ms + SLERP | ✅ |
| | Server timestamp | ✅ |
| | State reconciliation người vào sau | ✅ |
| | MessagePack | ❌ đang dùng JSON |
| | DataChannel unreliable | ❌ (chưa cần ở 20 Hz) |
| **Media** | SFU thuần C# (SIPSorcery), RTP forwarding | ✅ |
| | Trickle ICE hai chiều | ✅ |
| | DTLS-SRTP | ✅ tự động |
| | Fallback mic-denied | ✅ |
| | **TURN server** | ❌ **P0 — chỉ có STUN** |
| | Simulcast / SVC / BWE | ❌ giới hạn kiến trúc SIPSorcery |
| | Ghi hình buổi học | ✅ client-side (xem §4.5) |
| **Presence** | Spatial audio (PositionalAudio + sink priming) | ✅ |
| | AnalyserNode audio-reactive | ✅ (cả remote lẫn local) |
| | Roster tách khỏi publisher list | ✅ |
| | Avatar 3D có hình thể | ❌ hiện là chip chữ cái 2D; `UpdateAvatar` trong Hub **chưa được client gọi** (dead code) |
| **Sư phạm** | Laser local-space, highlight, explode, labels | ✅ |
| | Scene switching + replay hoá chất/tham số | ✅ |
| | Giơ tay | ✅ |
| **Vận hành** | Scale-out (Redis / room affinity) | ❌ static dict in-process |
| | Rate limiting per-connection | ❌ |
| | `SchoolId` isolation trong Hub | ❌ **lỗ hổng tenancy** |
| | Room ownership | ❌ |
| | Telemetry FPS | ✅ mở rộng được |

---

## 11. Roadmap nâng cấp

### P0 — Chặn vận hành thật (1–2 tuần)

1. **Dựng coturn + điền `Webrtc:IceServers`.** Không có TURN thì học sinh dùng 4G/mạng trường sẽ không xem được video. Cấu hình phía code đã sẵn sàng.
2. **Entity `NexusRoom` (`TenantEntity`)** với `Code` + `OwnerUserId` + `ExpiresAt`; `Create` ghi bản ghi, `JoinRoom` đối chiếu `SchoolId` và cấp quyền `teacher` chỉ cho chủ phòng. Vá cùng lúc lỗ hổng (b) và (c).
3. **Rate limit** `SyncTransform`/`ChemAdd`/join-by-code.
4. **Sửa trần ghi hình**: hạ bitrate về ~1,5 Mbps hoặc chuyển sang chunked upload — hiện tiết học >26 phút sẽ mất bản ghi.

### P1 — Chất lượng trải nghiệm (3–6 tuần)

5. **Nâng Three.js r160 → r184.** Đọc migration guide từng bản (breaking changes về color space và material đáng kể quanh r152–r155). Lợi ích: sửa lỗi cấp phát mỗi frame, mở đường cho TSL.
6. **Bật MessagePack cho SignalR** — giảm ~3× payload, một dòng cấu hình.
7. **Persist room state ra Redis** — sống sót qua restart, chuẩn bị cho scale-out.
8. **Telemetry mở rộng**: draw calls, `getStats()` bitrate/packet loss, hiển thị cảnh báo mạng yếu cho học sinh.
9. **Post-processing bloom** cho hologram — chi phí thấp, hiệu quả thị giác cao.

### P2 — Mở rộng quy mô (2–3 tháng)

10. **Đánh giá chuyển sang LiveKit** nếu cần simulcast/nhiều người phát. Cân nhắc: mất tính "zero-deployment" của SFU hiện tại, đổi lấy simulcast + Dynacast + egress ghi hình.
11. **Room affinity routing** thay vì backplane cho traffic 20 Hz.
12. **WebGPURenderer + TSL** — đo A/B trên thiết bị thật trước khi mặc định.
13. **Avatar 3D có hình thể + instancing** khi phòng >20 người.
14. **WebXR** (`setAnimationLoop` đã tương thích sẵn) nếu có nhu cầu kính VR.

---

## 12. Tham khảo

**Three.js & WebGPU**

- [What's New in Three.js (2026): WebGPU, New Workflows & Beyond — Utsubo](https://www.utsubo.com/blog/threejs-2026-what-changed)
- [Migrate Three.js to WebGPU (2026) — The Complete Checklist](https://www.utsubo.com/blog/webgpu-threejs-migration-guide)
- [Three.js vs WebGPU in 2026: What Changed for Large-Scale Viewers — AlterSquare](https://altersquare.io/blog/three-js-vs-webgpu-2026-large-scale-construction-viewers)
- [The Complete Guide to Three.js Post-Processing in 2026](https://threejsroadmap.com/blog/the-complete-guide-to-threejs-post-processing-in-2026)

**WebRTC & SFU**

- [mediasoup, Janus, LiveKit, Jitsi Videobridge, Pion: Choosing an SFU — Forasoft](https://www.forasoft.com/learn/video-streaming/articles-streaming/sfu-comparison-mediasoup-janus-livekit-jitsi-pion)
- [Simulcast and SVC: How the SFU Serves a Heterogeneous Audience — Forasoft](https://www.forasoft.com/learn/video-streaming/articles-streaming/simulcast-svc-sfu)
- [WebRTC Architecture for Production: SFU, MCU, MoQ Guide — Forasoft](https://www.forasoft.com/learn/webrtc-architecture-production-systems)
- [LiveKit Architecture Deep Dive: SFU, Media Routing, and Scaling — SheerBit](https://sheerbit.com/livekit-architecture-deep-dive-sfu-media-routing-and-scaling/)
- [LiveKit vs Mediasoup vs Janus: Best WebRTC SFU (2026) — Trembit](https://trembit.com/blog/choosing-the-right-sfu-janus-vs-mediasoup-vs-livekit-for-telemedicine-platforms/)

**Netcode**

- [Netcode Architectures Part 3: Snapshot Interpolation — SnapNet](https://snapnet.dev/blog/netcode-architectures-part-3-snapshot-interpolation/)
- [Client-side prediction — Wikipedia](https://en.wikipedia.org/wiki/Client-side_prediction)
- [geckos.io snapshot-interpolation (thư viện tham khảo)](https://github.com/geckosio/snapshot-interpolation)
- [WebRTC — Web Game Dev](https://www.webgamedev.com/backend/webrtc)

**SignalR & scale-out**

- [ASP.NET Core SignalR production hosting and scaling — Microsoft Learn](https://learn.microsoft.com/en-us/aspnet/core/signalr/scale)
- [Redis backplane for ASP.NET Core SignalR scale-out — Microsoft Learn](https://learn.microsoft.com/en-us/aspnet/core/signalr/redis-backplane)
- [Scaling SignalR: Scaleout strategies, limits & alternatives — Ably](https://ably.com/topic/scaling-signalr)
- [Scaling SignalR With a Redis Backplane — Milan Jovanović](https://milanjovanovic.tech/blog/scaling-signalr-with-redis-backplane)

**File nguồn trong dự án**

- `src/LuminaTutors.Web/Hubs/LuminaRtcHub.cs` — signaling + state sync
- `src/LuminaTutors.Web/Hubs/LuminaSfuService.cs` — SFU SIPSorcery
- `src/LuminaTutors.Web/wwwroot/js/nexus/Lumina3DEngine.js` — render + interpolation
- `src/LuminaTutors.Web/wwwroot/js/nexus/LuminaStreamManager.js` — WebRTC client + spatial audio
- `src/LuminaTutors.Web/wwwroot/js/nexus/LuminaInteraction.js` — laser + turntable
- `src/LuminaTutors.Web/wwwroot/sw-3d.js` — cache thư viện 3D
