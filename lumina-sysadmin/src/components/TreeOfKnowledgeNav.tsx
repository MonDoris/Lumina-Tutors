import { AnimatePresence, motion } from 'framer-motion';
import { useState } from 'react';
import { BRANCHES, type Branch } from '../data';
import { useDive } from '../dive-context';

export function TreeOfKnowledgeNav() {
  return (
    <nav
      aria-label="Cây tri thức"
      className="absolute bottom-0 left-0 top-16 z-20 hidden w-60 overflow-y-auto border-r border-bronze-500/15 bg-obsidian-950/70 px-5 py-6 backdrop-blur lg:block"
    >
      <p className="font-tech text-[11px] uppercase tracking-[.25em] text-slate-500">Cây tri thức</p>
      <div className="relative mt-5 pl-5">
        <span aria-hidden className="absolute bottom-2 left-1 top-2 w-px bg-bronze-500/35" />
        {BRANCHES.map(b => (
          <BranchNode key={b.label} branch={b} />
        ))}
      </div>
    </nav>
  );
}

function BranchNode({ branch }: { branch: Branch }) {
  const { dive, diveTo, surface } = useDive();
  const [open, setOpen] = useState(false);
  const active = dive.module === branch.id;

  return (
    <div onMouseEnter={() => setOpen(true)} onMouseLeave={() => setOpen(false)}>
      <button
        onClick={e => (branch.id === null ? surface() : diveTo(branch.id, e))}
        className={`relative flex w-full items-center py-2 text-left text-sm transition-colors duration-300
                    ${active ? 'text-ivory' : 'text-slate-400 hover:text-bronze-300'}`}
      >
        <span
          aria-hidden
          className={`absolute -left-4 h-2 w-2 -translate-x-1/2 rounded-full transition-colors duration-300
                      ${active ? 'bg-jade-400' : 'border border-bronze-500/60 bg-transparent'}`}
        />
        {branch.label}
      </button>
      <AnimatePresence initial={false}>
        {open && branch.leaves.length > 0 && (
          <motion.ul
            initial={{ height: 0, opacity: 0 }}
            animate={{ height: 'auto', opacity: 1 }}
            exit={{ height: 0, opacity: 0 }}
            transition={{ duration: 0.3, ease: 'easeOut' }}
            className="overflow-hidden pl-4"
          >
            {branch.leaves.map(l => (
              <li key={l} className="py-1 font-tech text-[11px] tracking-wider text-slate-500">
                ◦ {l}
              </li>
            ))}
          </motion.ul>
        )}
      </AnimatePresence>
    </div>
  );
}
