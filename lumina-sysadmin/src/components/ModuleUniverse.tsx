import { motion, type Variants } from 'framer-motion';
import { MODULES, type Tone } from '../data';
import { useDive } from '../dive-context';
import { AlertLotusCard } from './AlertLotusCard';

const panelVariants: Variants = {
  hidden: { opacity: 0, y: 24, scale: 0.96 },
  show: { opacity: 1, y: 0, scale: 1, transition: { duration: 0.6, ease: [0.22, 1, 0.36, 1] } },
};

const toneText: Record<Tone, string> = {
  jade: 'text-jade-300',
  bronze: 'text-bronze-300',
  son: 'text-son-400',
  ivory: 'text-ivory',
};

const PANEL = 'rounded-xl border border-bronze-500/15 bg-obsidian-900/60 p-5 backdrop-blur';

export function ModuleUniverse({ id }: { id: string }) {
  const { surface } = useDive();
  const mod = MODULES[id];
  if (!mod) return null;

  return (
    <div className="h-full overflow-y-auto px-10 pb-10 pt-8">
      <header className="flex flex-wrap items-end justify-between gap-4">
        <div>
          <p className="font-tech text-xs uppercase tracking-[.35em] text-jade-400">node://{mod.code}</p>
          <h1 className="mt-1 font-display text-5xl font-semibold text-ivory">{mod.title}</h1>
          <p className="mt-2 max-w-xl text-sm text-slate-400">{mod.intro}</p>
        </div>
        <button
          onClick={surface}
          className="group shrink-0 rounded border border-bronze-500/30 px-4 py-2 font-tech text-sm
                     text-bronze-400 transition-colors hover:border-bronze-400/60 hover:text-bronze-300"
        >
          <span className="mr-2 inline-block transition-transform group-hover:-translate-y-0.5">↑</span>
          Trồi lên mặt trống
        </button>
      </header>

      <motion.div
        initial="hidden"
        animate="show"
        variants={{ hidden: {}, show: { transition: { staggerChildren: 0.08, delayChildren: 0.35 } } }}
        className="mt-8 grid grid-cols-12 gap-5"
      >
        {mod.stats.map(s => (
          <motion.section key={s.label} variants={panelVariants} className={`col-span-6 lg:col-span-3 ${PANEL}`}>
            <p className="font-tech text-[11px] uppercase tracking-[.2em] text-slate-500">{s.label}</p>
            <p className={`mt-2 font-tech text-3xl ${toneText[s.tone ?? 'ivory']}`}>{s.value}</p>
          </motion.section>
        ))}

        {mod.spark && (
          <motion.section variants={panelVariants} className={`col-span-12 lg:col-span-7 ${PANEL}`}>
            <h2 className="font-display text-xl text-bronze-300">{mod.spark.title}</h2>
            <div className="mt-4">
              <Sparkline points={mod.spark.points} />
            </div>
            <p className="mt-1 text-right font-tech text-[11px] text-slate-500">đơn vị: {mod.spark.unit}</p>
          </motion.section>
        )}

        {mod.lists.map((list, idx) => (
          <motion.section
            key={list.title}
            variants={panelVariants}
            className={`col-span-12 ${idx === 0 && mod.spark ? 'lg:col-span-5' : 'lg:col-span-6'} ${PANEL}`}
          >
            <h2 className="font-display text-xl text-bronze-300">{list.title}</h2>
            <ul className="mt-4 space-y-3">
              {list.rows.map(row => (
                <li key={row.primary}
                    className="flex items-baseline justify-between gap-4 border-b border-bronze-500/10 pb-2 last:border-0 last:pb-0">
                  <span className={`text-sm ${toneText[row.tone ?? 'ivory']}`}>{row.primary}</span>
                  <span className="shrink-0 font-tech text-xs text-slate-500">{row.secondary}</span>
                </li>
              ))}
            </ul>
          </motion.section>
        ))}

        {mod.alerts.length > 0 && (
          <motion.section variants={panelVariants} className="col-span-12 lg:col-span-6">
            <h2 className="font-display text-xl text-bronze-300">Hồ cảnh báo</h2>
            <div className="mt-4 space-y-4">
              {mod.alerts.map(a => (
                <AlertLotusCard key={a.title} alert={a} />
              ))}
            </div>
          </motion.section>
        )}
      </motion.div>
    </div>
  );
}

function Sparkline({ points }: { points: number[] }) {
  const W = 560;
  const H = 120;
  const max = Math.max(...points);
  const min = Math.min(...points);
  const span = max - min || 1;
  const coords = points.map((p, i) => ({
    x: (i / (points.length - 1)) * W,
    y: H - ((p - min) / span) * (H - 16) - 8,
  }));
  const d = coords.map((c, i) => `${i === 0 ? 'M' : 'L'} ${c.x.toFixed(1)} ${c.y.toFixed(1)}`).join(' ');
  const last = coords[coords.length - 1];

  return (
    <svg viewBox={`0 0 ${W} ${H}`} className="w-full" role="img" aria-label="Biểu đồ nhịp 24 giờ">
      <path d={`${d} L ${W} ${H} L 0 ${H} Z`} fill="rgba(31,165,136,.08)" stroke="none" />
      <path d={d} fill="none" stroke="#1FA588" strokeWidth={2} />
      <motion.circle
        cx={last.x}
        cy={last.y}
        r={4}
        fill="#7FE7CB"
        animate={{ scale: [1, 1.5, 1], opacity: [1, 0.6, 1] }}
        transition={{ duration: 1.8, repeat: Infinity, ease: 'easeInOut' }}
        style={{ transformBox: 'fill-box', transformOrigin: 'center', filter: 'drop-shadow(0 0 6px rgba(127,231,203,.8))' }}
      />
    </svg>
  );
}
