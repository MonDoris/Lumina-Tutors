import { useEffect, useState } from 'react';

const clamp = (v: number, lo: number, hi: number) => Math.min(hi, Math.max(lo, v));
const jitter = (v: number, d: number) => v + (Math.random() * 2 - 1) * d;

interface Vitals {
  cpu: number;
  ram: number;
  db: number;
  queue: number;
  rps: number;
  sessions: number;
}

export function TelemetryRail() {
  const [v, setV] = useState<Vitals>({ cpu: 34, ram: 39, db: 8, queue: 240, rps: 1284, sessions: 312 });

  useEffect(() => {
    const t = setInterval(() => {
      setV(p => ({
        cpu: clamp(Math.round(jitter(p.cpu, 4)), 12, 88),
        ram: clamp(Math.round(jitter(p.ram, 2)), 20, 90),
        db: clamp(Math.round(jitter(p.db, 2)), 4, 40),
        queue: clamp(Math.round(jitter(p.queue, 18)), 60, 420),
        rps: clamp(Math.round(jitter(p.rps, 60)), 300, 2200),
        sessions: clamp(Math.round(jitter(p.sessions, 6)), 100, 600),
      }));
    }, 2500);
    return () => clearInterval(t);
  }, []);

  return (
    <aside className="absolute bottom-0 right-0 top-16 z-20 hidden w-72 overflow-y-auto border-l border-bronze-500/15 bg-obsidian-950/70 px-6 py-6 backdrop-blur xl:block">
      <p className="font-tech text-[11px] uppercase tracking-[.25em] text-slate-500">Sinh hiệu</p>
      <div className="mt-5 space-y-5 font-tech">
        <Gauge label="CPU" display={`${v.cpu}%`} pct={v.cpu} warn={v.cpu > 75} />
        <Gauge label="RAM" display={`${(v.ram * 0.16).toFixed(1)}/16 GB`} pct={v.ram} warn={v.ram > 80} />
        <Gauge label="DB latency" display={`${v.db} ms`} pct={(v.db / 40) * 100} warn={v.db > 25} />
        <Gauge label="Hàng đợi" display={`${v.queue} ms`} pct={(v.queue / 420) * 100} warn={v.queue > 200} />
        <Plain label="Req/s" value={v.rps.toLocaleString('vi-VN')} />
        <Plain label="Phiên" value={v.sessions.toLocaleString('vi-VN')} />
        <Plain label="Uptime" value="42 ngày" />
      </div>
    </aside>
  );
}

function Gauge({ label, display, pct, warn }: { label: string; display: string; pct: number; warn: boolean }) {
  return (
    <div>
      <div className="flex items-baseline justify-between text-xs">
        <span className="text-slate-500">{label}</span>
        <span className={warn ? 'text-bronze-300' : 'text-jade-300'}>{display}</span>
      </div>
      <div className="mt-1.5 h-[3px] rounded bg-slate-500/20">
        <div
          className={`h-[3px] rounded transition-all duration-700 ${warn ? 'bg-bronze-500' : 'bg-jade-500'}`}
          style={{ width: `${clamp(pct, 2, 100)}%` }}
        />
      </div>
    </div>
  );
}

function Plain({ label, value }: { label: string; value: string }) {
  return (
    <div className="flex items-baseline justify-between text-xs">
      <span className="text-slate-500">{label}</span>
      <span className="text-ivory">{value}</span>
    </div>
  );
}
