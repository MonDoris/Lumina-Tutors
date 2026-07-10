import { motion } from 'framer-motion';
import { useRef, useState } from 'react';
import type { SysAlert } from '../data';

interface Ripple {
  id: number;
  x: number;
  y: number;
}

export function AlertLotusCard({ alert }: { alert: SysAlert }) {
  const [ripples, setRipples] = useState<Ripple[]>([]);
  const lastDrop = useRef(0);

  const drop = (e: React.PointerEvent<HTMLDivElement>) => {
    const now = performance.now();
    if (now - lastDrop.current < 160) return; /* nhỏ giọt, không xối xả */
    lastDrop.current = now;
    const rect = e.currentTarget.getBoundingClientRect();
    setRipples(rs => [...rs.slice(-5), { id: now, x: e.clientX - rect.left, y: e.clientY - rect.top }]);
  };

  const sev = alert.severity === 'critical'
    ? { dot: 'bg-son-500', ring: 'border-son-400/40', text: 'text-son-400' }
    : { dot: 'bg-bronze-500', ring: 'border-bronze-400/40', text: 'text-bronze-400' };

  return (
    <div
      onPointerEnter={drop}
      onPointerMove={drop}
      className="group relative overflow-hidden rounded-lg border border-bronze-500/20
                 bg-obsidian-800/70 px-5 py-4 backdrop-blur
                 transition-colors duration-500 hover:border-jade-500/40"
    >
      {/* mặt hồ — mỗi giọt là 3 vành sóng lệch pha */}
      {ripples.map(r => (
        <span key={r.id} className="pointer-events-none absolute" style={{ left: r.x, top: r.y }}>
          {[0, 0.16, 0.32].map((delay, i) => (
            <motion.span
              key={i}
              className={`absolute -translate-x-1/2 -translate-y-1/2 rounded-full border ${sev.ring}`}
              initial={{ width: 6, height: 6, opacity: 0.5 - i * 0.12 }}
              animate={{ width: 240, height: 240, opacity: 0 }}
              transition={{ duration: 1.5, delay, ease: [0.16, 1, 0.3, 1] }}
              onAnimationComplete={i === 2 ? () => setRipples(rs => rs.filter(x => x.id !== r.id)) : undefined}
            />
          ))}
        </span>
      ))}

      <div className="relative flex items-start gap-4">
        <span className="relative mt-1.5 flex h-2.5 w-2.5 shrink-0">
          <span className={`absolute h-full w-full animate-ping rounded-full ${sev.dot} opacity-60`} />
          <span className={`h-2.5 w-2.5 rounded-full ${sev.dot}`} />
        </span>
        <div className="min-w-0">
          <h3 className="font-display text-lg text-ivory transition-colors duration-500 group-hover:text-bronze-300">
            {alert.title}
          </h3>
          <p className="mt-0.5 truncate text-sm text-slate-400">{alert.detail}</p>
          <p className={`mt-2 font-tech text-xs tracking-widest ${sev.text}`}>
            {alert.source} · {alert.timestamp}
          </p>
        </div>
      </div>
    </div>
  );
}
