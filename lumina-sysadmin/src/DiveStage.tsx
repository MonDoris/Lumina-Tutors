import { AnimatePresence, motion, useReducedMotion } from 'framer-motion';
import { FLOWS, NODES } from './data';
import { useDive } from './dive-context';
import { DongSonTopology } from './features/topology/DongSonTopology';
import { ModuleUniverse } from './components/ModuleUniverse';

const EASE_DIVE: [number, number, number, number] = [0.83, 0, 0.17, 1];
const DUR = 0.95;

export function DiveStage() {
  const { dive, diveTo } = useDive();
  const reduced = useReducedMotion();
  const origin = `${dive.origin.x}% ${dive.origin.y}%`;

  const overviewHidden = reduced ? { opacity: 0 } : { scale: 4.5, opacity: 0, filter: 'blur(16px)' };
  const overviewExit = reduced ? { opacity: 0 } : { scale: 5.5, opacity: 0, filter: 'blur(16px)' };
  const moduleHidden = reduced ? { opacity: 0 } : { scale: 0.1, opacity: 0, filter: 'blur(12px)' };
  const moduleExit = reduced ? { opacity: 0 } : { scale: 0.08, opacity: 0, filter: 'blur(12px)' };

  return (
    <div className="absolute inset-0">
      {/* mode="wait": exit của Overview (lao vào node) chạy xong
          rồi Module mới nở ra — một nhịp trống, một cảnh mở */}
      <AnimatePresence mode="wait">
        {dive.module === null ? (
          <motion.main
            key="overview"
            className="absolute inset-0 flex items-center justify-center px-4 pb-4 pt-20 lg:pl-60 xl:pr-72"
            style={{ transformOrigin: origin }}
            initial={overviewHidden}
            animate={{ scale: 1, opacity: 1, filter: 'blur(0px)' }}
            exit={overviewExit}
            transition={{ duration: DUR, ease: EASE_DIVE }}
          >
            <DongSonTopology nodes={NODES} flows={FLOWS} onDive={(n, e) => diveTo(n.module, e)} />
          </motion.main>
        ) : (
          <motion.main
            key={dive.module}
            className="absolute inset-0 pt-16 lg:pl-60 xl:pr-72"
            style={{ transformOrigin: origin }}
            initial={moduleHidden}
            animate={{ scale: 1, opacity: 1, filter: 'blur(0px)' }}
            exit={moduleExit}
            transition={{ duration: DUR, ease: EASE_DIVE }}
          >
            <ModuleUniverse id={dive.module} />
          </motion.main>
        )}
      </AnimatePresence>
      {!reduced && <Shockwave key={dive.module ?? 'overview'} origin={dive.origin} />}
    </div>
  );
}

/* Vòng sóng đồng — mỗi lần chuyển cảnh là một nhịp trống */
function Shockwave({ origin }: { origin: { x: number; y: number } }) {
  return (
    <motion.div
      className="pointer-events-none absolute z-30 rounded-full border border-bronze-400/50"
      style={{ left: `${origin.x}%`, top: `${origin.y}%`, x: '-50%', y: '-50%' }}
      initial={{ width: 0, height: 0, opacity: 0.8 }}
      animate={{ width: '250vmax', height: '250vmax', opacity: 0 }}
      transition={{ duration: 1.2, ease: 'easeOut' }}
    />
  );
}
