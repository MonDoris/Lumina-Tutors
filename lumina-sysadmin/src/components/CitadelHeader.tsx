import { useEffect, useState } from 'react';
import { MODULES } from '../data';
import { useDive } from '../dive-context';

export function CitadelHeader() {
  const { dive } = useDive();
  const [now, setNow] = useState(() => new Date());

  useEffect(() => {
    const t = setInterval(() => setNow(new Date()), 1000);
    return () => clearInterval(t);
  }, []);

  const moduleTitle = dive.module ? MODULES[dive.module]?.title : null;

  return (
    <header className="absolute inset-x-0 top-0 z-20 flex h-16 items-center justify-between border-b border-bronze-500/20 bg-obsidian-950/80 px-6 backdrop-blur">
      <div className="flex items-baseline gap-3">
        <h1 className="font-display text-2xl font-semibold tracking-wide text-ivory">Lumina · Đài quan sát</h1>
        <span className="rounded border border-bronze-500/40 px-1.5 py-0.5 font-tech text-[11px] text-bronze-500">
          SYSADMIN
        </span>
      </div>
      <p className="hidden font-tech text-xs tracking-[.2em] text-slate-500 md:block">
        Rễ ▸ Mặt trống
        {moduleTitle ? <span className="text-jade-400"> ▸ {moduleTitle}</span> : null}
      </p>
      <div className="flex items-center gap-5 font-tech text-xs text-slate-400">
        <span>
          {now.toLocaleDateString('vi-VN')} — {now.toLocaleTimeString('vi-VN', { hour12: false })}
        </span>
        <span className="flex items-center gap-2 text-jade-400">
          <span className="h-1.5 w-1.5 rounded-full bg-jade-400" />
          Mạch ổn định
        </span>
      </div>
    </header>
  );
}
