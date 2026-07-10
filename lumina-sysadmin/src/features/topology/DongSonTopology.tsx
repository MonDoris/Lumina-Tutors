import { motion } from 'framer-motion';
import { useMemo } from 'react';
import type { DrumNode, NodeStatus } from '../../data';

type Pt = { x: number; y: number };

const CX = 500;
const CY = 500;
const RING_R = [0, 160, 260, 360];

const polar = (r: number, deg: number): Pt => {
  const rad = ((deg - 90) * Math.PI) / 180;
  return { x: CX + r * Math.cos(rad), y: CY + r * Math.sin(rad) };
};

const STATUS: Record<NodeStatus, { core: string; halo: string; period: number }> = {
  healthy: { core: '#1FA588', halo: 'rgba(31,165,136,.45)', period: 2.6 },
  degraded: { core: '#C9A227', halo: 'rgba(201,162,39,.50)', period: 1.3 },
  critical: { core: '#C43D2E', halo: 'rgba(196,61,46,.55)', period: 0.65 },
};

/* Mặt trời 14 tia — tâm trống Ngọc Lũ */
const sunPoints = (rays = 14, rIn = 52, rOut = 96) =>
  Array.from({ length: rays * 2 }, (_, i) => {
    const p = polar(i % 2 === 0 ? rOut : rIn, (i * 360) / (rays * 2));
    return `${p.x.toFixed(1)},${p.y.toFixed(1)}`;
  }).join(' ');

export function DongSonTopology({ nodes, flows, onDive }: {
  nodes: DrumNode[];
  flows: [string, string][];
  onDive: (node: DrumNode, e: React.MouseEvent) => void;
}) {
  const pos = useMemo(
    () => new Map(nodes.map(n => [n.id, polar(RING_R[n.ring], n.angle)])),
    [nodes],
  );

  return (
    <svg viewBox="0 0 1000 1000" role="img" aria-label="Sơ đồ mạng lưới trống đồng Đông Sơn"
         className="h-full max-h-[860px] w-auto max-w-full select-none">
      <title>Trống đồng hệ thống — mỗi node là một dịch vụ đang đập nhịp</title>
      <defs>
        <radialGradient id="sun-grad">
          <stop offset="0%" stopColor="#F0D48A" />
          <stop offset="55%" stopColor="#C9A227" />
          <stop offset="100%" stopColor="#6E5615" />
        </radialGradient>
      </defs>

      {/* Khai trống: các vòng lan ra như tiếng trống vang */}
      {RING_R.slice(1).map((r, i) => (
        <motion.g key={r}
          initial={{ scale: 0.6, opacity: 0 }}
          animate={{ scale: 1, opacity: 1 }}
          transition={{ delay: 0.35 + i * 0.22, duration: 1.1, ease: [0.22, 1, 0.36, 1] }}
          style={{ transformOrigin: '500px 500px' }}
        >
          <circle cx={CX} cy={CY} r={r} fill="none" stroke="#C9A227" strokeOpacity={0.16} />
          <circle cx={CX} cy={CY} r={r - 12} fill="none" stroke="#C9A227"
                  strokeOpacity={0.07} strokeDasharray="3 9" />
          <Ticks r={r + 8} count={72 + i * 24} />
        </motion.g>
      ))}

      <ChimLacFlock />

      {flows.map(([a, b]) => (
        <FlowArc key={`${a}-${b}`} from={pos.get(a)!} to={pos.get(b)!} />
      ))}

      {/* Mặt trời trung tâm — sức khỏe tổng thể */}
      <motion.polygon points={sunPoints()} fill="url(#sun-grad)"
        initial={{ scale: 0, opacity: 0 }}
        animate={{ scale: [1, 1.04, 1], opacity: [0.9, 1, 0.9] }}
        transition={{
          scale: { duration: 3.4, repeat: Infinity, ease: 'easeInOut', delay: 0.15 },
          opacity: { duration: 3.4, repeat: Infinity, ease: 'easeInOut', delay: 0.15 },
        }}
        style={{ transformOrigin: '500px 500px', filter: 'drop-shadow(0 0 28px rgba(201,162,39,.45))' }}
      />

      {nodes.map((n, i) => (
        <DrumNodeGlyph key={n.id} node={n} at={pos.get(n.id)!} delay={0.9 + i * 0.06} onDive={onDive} />
      ))}
    </svg>
  );
}

const Ticks = ({ r, count }: { r: number; count: number }) => (
  <g stroke="#C9A227" strokeOpacity={0.22}>
    {Array.from({ length: count }, (_, i) => {
      const a = (i * 360) / count;
      const p1 = polar(r - 4, a);
      const p2 = polar(r + 4, a);
      return <line key={i} x1={p1.x} y1={p1.y} x2={p2.x} y2={p2.y} />;
    })}
  </g>
);

function DrumNodeGlyph({ node, at, delay, onDive }: {
  node: DrumNode;
  at: Pt;
  delay: number;
  onDive: (n: DrumNode, e: React.MouseEvent) => void;
}) {
  const s = STATUS[node.status];
  return (
    <motion.g
      transform={`translate(${at.x} ${at.y})`}
      className="cursor-pointer"
      data-node-id={node.id}
      initial={{ opacity: 0 }}
      animate={{ opacity: 1 }}
      transition={{ delay, duration: 0.6 }}
      onClick={e => onDive(node, e)}
    >
      <title>{`${node.label} — ${node.status === 'healthy' ? 'khỏe mạnh' : node.status === 'degraded' ? 'suy giảm' : 'nguy kịch'}`}</title>
      {/* hào quang lan như sóng nước */}
      <motion.circle r={16} fill="none" stroke={s.core} strokeWidth={1.5}
        animate={{ r: [14, 30], opacity: [0.65, 0] }}
        transition={{ duration: s.period, repeat: Infinity, ease: 'easeOut' }} />
      {/* lõi đập như nhịp tim */}
      <motion.circle r={10} fill={s.core}
        animate={{ scale: [1, 1.18, 1] }}
        whileHover={{ scale: 1.45 }}
        transition={{ duration: s.period, repeat: Infinity, ease: 'easeInOut' }}
        style={{ transformBox: 'fill-box', transformOrigin: 'center', filter: `drop-shadow(0 0 10px ${s.halo})` }} />
      <text y={36} textAnchor="middle"
        className="pointer-events-none fill-bronze-300/80 font-tech text-[13px] uppercase tracking-[.25em]">
        {node.label}
      </text>
    </motion.g>
  );
}

function FlowArc({ from, to }: { from: Pt; to: Pt }) {
  /* mạch cong về tâm — mọi dữ liệu đều chảy qua "mặt trời" */
  const d = `M ${from.x.toFixed(1)} ${from.y.toFixed(1)} Q ${CX} ${CY} ${to.x.toFixed(1)} ${to.y.toFixed(1)}`;
  const dur = 3 + ((from.x + to.y) % 30) / 10;
  return (
    <g>
      <path d={d} fill="none" stroke="#1FA588" strokeOpacity={0.14} strokeWidth={1.2} />
      <circle r={3} fill="#7FE7CB" style={{ filter: 'drop-shadow(0 0 6px rgba(127,231,203,.8))' }}>
        <animateMotion dur={`${dur}s`} repeatCount="indefinite" path={d} />
      </circle>
    </g>
  );
}

const LAC_BIRD = 'M0 0 Q10 -14 30 -16 Q22 -8 26 -5 Q38 -8 46 -2 Q32 2 24 8 Q12 16 -6 12 Q6 6 0 0 Z';

const ChimLacFlock = () => (
  <motion.g
    animate={{ rotate: -360 }}
    transition={{ duration: 540, repeat: Infinity, ease: 'linear' }}
    style={{ transformOrigin: '500px 500px' }}
  >
    {[0, 60, 120, 180, 240, 300].map(a => {
      const p = polar(415, a);
      return (
        <path key={a} d={LAC_BIRD} fill="#C9A227" fillOpacity={0.28}
              transform={`translate(${p.x.toFixed(1)} ${p.y.toFixed(1)}) rotate(${a})`} />
      );
    })}
  </motion.g>
);
