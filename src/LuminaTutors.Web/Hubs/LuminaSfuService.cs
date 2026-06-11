using System.Collections.Concurrent;
using System.Net;
using System.Text.Json;
using Microsoft.AspNetCore.SignalR;
using SIPSorcery.Net;
using SIPSorceryMedia.Abstractions;

namespace LuminaTutors.Web.Hubs;

/// <summary>
/// Selective Forwarding Unit (SFU) thuần C# cho "Lumina Holographic Nexus".
///
/// Mỗi publisher : 1 RTCPeerConnection RecvOnly (server NHẬN audio + video).
/// Mỗi subscription: 1 RTCPeerConnection SendOnly (server GỬI track của 1 publisher).
/// Forward gói RTP thô (OnRtpPacketReceived -> SendRtpRaw), KHÔNG transcode.
///
/// Đường tín hiệu (SDP/ICE) đi qua <see cref="LuminaRtcHub"/>; media đi thẳng
/// browser &lt;-&gt; server qua DTLS-SRTP do SIPSorcery tự terminate.
/// </summary>
public interface ILuminaSfuService
{
    Task<string> CreatePublisherAsync(string roomId, string connId, string sdpOffer);
    Task CreateSubscriptionAsync(string roomId, string subscriberConnId, string targetPeerId);
    void SetSubscriptionAnswer(string subscriptionId, string sdpAnswer);
    void AddIceCandidate(string pcKey, string candidateJson);
    void RemovePeer(string roomId, string connId);
}

public sealed class LuminaSfuService : ILuminaSfuService
{
    private readonly IHubContext<LuminaRtcHub> _hub;
    private readonly ILogger<LuminaSfuService> _logger;

    private static readonly JsonSerializerOptions JsonOpts = new() { PropertyNameCaseInsensitive = true };

    // Định dạng cố định: Opus (audio, PT 111, 48 kHz stereo) + VP8 (video, PT 96, 90 kHz)
    private static AudioFormat OpusFormat() => new(AudioCodecsEnum.OPUS, 111, 48000, 2, "");
    private static VideoFormat Vp8Format()  => new(VideoCodecsEnum.VP8, 96, 90000, "");

    private sealed class Publisher
    {
        public required string ConnId { get; init; }
        public required string RoomId { get; init; }
        public required RTCPeerConnection Pc { get; init; }
        public readonly ConcurrentDictionary<string, Subscription> Subscribers = new(); // subscriberConnId -> sub
    }

    private sealed class Subscription
    {
        public required string Id { get; init; }
        public required string SubscriberConnId { get; init; }
        public required string TargetPeerId { get; init; }
        public required RTCPeerConnection Pc { get; init; }
    }

    private readonly ConcurrentDictionary<string, Publisher> _publishers = new();        // key = connId
    private readonly ConcurrentDictionary<string, Subscription> _subscriptions = new();   // key = subscriptionId
    private readonly ConcurrentDictionary<string, RTCPeerConnection> _pcByKey = new();    // cho trickle ICE

    public LuminaSfuService(IHubContext<LuminaRtcHub> hub, ILogger<LuminaSfuService> logger)
    {
        _hub = hub;
        _logger = logger;
    }

    private static RTCPeerConnection NewPc() => new(new RTCConfiguration
    {
        iceServers = new List<RTCIceServer>
        {
            new() { urls = "stun:stun.l.google.com:19302" }
            // Production: thêm TURN server riêng để mobile sau NAT đối xứng kết nối được.
        }
    });

    // ── PUBLISHER: client offer -> SFU answer ─────────────────────────────────
    public async Task<string> CreatePublisherAsync(string roomId, string connId, string sdpOffer)
    {
        var pc = NewPc();
        var pub = new Publisher { ConnId = connId, RoomId = roomId, Pc = pc };
        _publishers[connId] = pub;
        var pcKey = $"publisher:{connId}";
        _pcByKey[pcKey] = pc;

        // Server NHẬN audio + video từ client.
        pc.addTrack(new MediaStreamTrack(OpusFormat(), MediaStreamStatusEnum.RecvOnly));
        pc.addTrack(new MediaStreamTrack(Vp8Format(),  MediaStreamStatusEnum.RecvOnly));

        // FORWARD: nhận gói RTP nào, đẩy ngay tới mọi subscriber của publisher này.
        pc.OnRtpPacketReceived += (IPEndPoint rep, SDPMediaTypesEnum media, RTPPacket pkt) =>
        {
            foreach (var sub in pub.Subscribers.Values)
            {
                if (sub.Pc.connectionState != RTCPeerConnectionState.connected) continue;
                try
                {
                    sub.Pc.SendRtpRaw(media, pkt.Payload,
                        pkt.Header.Timestamp, pkt.Header.MarkerBit, pkt.Header.PayloadType);
                }
                catch (Exception ex)
                {
                    _logger.LogDebug(ex, "Forward RTP failed sub={Sub}", sub.Id);
                }
            }
        };

        pc.onicecandidate += c => PushIce(connId, pcKey, c);
        pc.onconnectionstatechange += s =>
        {
            _logger.LogDebug("Publisher {Conn} -> {State}", connId, s);
            if (s is RTCPeerConnectionState.failed or RTCPeerConnectionState.closed)
                RemovePeer(roomId, connId);
        };

        pc.setRemoteDescription(new RTCSessionDescriptionInit { type = RTCSdpType.offer, sdp = sdpOffer });
        var answer = pc.createAnswer(null);
        await pc.setLocalDescription(answer);

        _logger.LogInformation("SFU publisher created conn={Conn} room={Room}", connId, roomId);
        return answer.sdp;
    }

    // ── SUBSCRIPTION: SFU offer -> đẩy xuống client (client trả Answer) ────────
    public async Task CreateSubscriptionAsync(string roomId, string subscriberConnId, string targetPeerId)
    {
        if (!_publishers.TryGetValue(targetPeerId, out var pub))
        {
            _logger.LogWarning("Subscribe target {Target} chưa publish", targetPeerId);
            return;
        }

        var pc = NewPc();
        var subId = Guid.NewGuid().ToString("N");
        var sub = new Subscription
        {
            Id = subId, SubscriberConnId = subscriberConnId, TargetPeerId = targetPeerId, Pc = pc
        };
        var pcKey = $"sub:{subId}";
        _subscriptions[subId] = sub;
        _pcByKey[pcKey] = pc;
        pub.Subscribers[subscriberConnId] = sub;

        // Server GỬI track của publisher xuống subscriber.
        pc.addTrack(new MediaStreamTrack(OpusFormat(), MediaStreamStatusEnum.SendOnly));
        pc.addTrack(new MediaStreamTrack(Vp8Format(),  MediaStreamStatusEnum.SendOnly));

        pc.onicecandidate += c => PushIce(subscriberConnId, pcKey, c);
        pc.onconnectionstatechange += s =>
        {
            if (s is RTCPeerConnectionState.failed or RTCPeerConnectionState.closed)
                CloseSubscription(subId);
        };

        var offer = pc.createOffer(null);
        await pc.setLocalDescription(offer);

        await _hub.Clients.Client(subscriberConnId).SendAsync("RtcOffer", new
        {
            subscriptionId = subId,
            fromPeerId = targetPeerId,
            sdp = offer.sdp
        });
    }

    public void SetSubscriptionAnswer(string subscriptionId, string sdpAnswer)
    {
        if (_subscriptions.TryGetValue(subscriptionId, out var sub))
        {
            sub.Pc.setRemoteDescription(new RTCSessionDescriptionInit
            {
                type = RTCSdpType.answer, sdp = sdpAnswer
            });
        }
    }

    // ── TRICKLE ICE (client -> server) ────────────────────────────────────────
    public void AddIceCandidate(string pcKey, string candidateJson)
    {
        if (string.IsNullOrWhiteSpace(candidateJson)) return;
        if (!_pcByKey.TryGetValue(pcKey, out var pc)) return;

        try
        {
            var init = JsonSerializer.Deserialize<RTCIceCandidateInit>(candidateJson, JsonOpts);
            if (init is not null && !string.IsNullOrEmpty(init.candidate))
                pc.addIceCandidate(init);
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "addIceCandidate failed key={Key}", pcKey);
        }
    }

    // ── server -> client ICE ──────────────────────────────────────────────────
    private void PushIce(string connId, string pcKey, RTCIceCandidate? c)
    {
        if (c is null) return;
        _ = _hub.Clients.Client(connId).SendAsync("RtcIceCandidate", new
        {
            pcKey,
            candidate = new
            {
                candidate = c.candidate,
                sdpMid = c.sdpMid,
                sdpMLineIndex = c.sdpMLineIndex
            }
        });
    }

    // ── CLEANUP ───────────────────────────────────────────────────────────────
    public void RemovePeer(string roomId, string connId)
    {
        if (_publishers.TryRemove(connId, out var pub))
        {
            foreach (var sub in pub.Subscribers.Values.ToList())
                CloseSubscription(sub.Id);
            SafeClose(pub.Pc);
            _pcByKey.TryRemove($"publisher:{connId}", out _);
        }

        // Đóng mọi subscription mà connId này đang xem.
        foreach (var kv in _subscriptions.Where(s => s.Value.SubscriberConnId == connId).ToList())
            CloseSubscription(kv.Key);
    }

    private void CloseSubscription(string subId)
    {
        if (_subscriptions.TryRemove(subId, out var sub))
        {
            if (_publishers.TryGetValue(sub.TargetPeerId, out var pub))
                pub.Subscribers.TryRemove(sub.SubscriberConnId, out _);
            SafeClose(sub.Pc);
            _pcByKey.TryRemove($"sub:{subId}", out _);
        }
    }

    private static void SafeClose(RTCPeerConnection pc)
    {
        try { pc.close(); } catch { /* already closed */ }
    }
}
