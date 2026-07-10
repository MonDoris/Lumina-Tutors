const PARTICLES = Array.from({ length: 26 }, (_, i) => ({
  left: (i * 37 + 11) % 100,
  top: (i * 53 + 7) % 100,
  size: 1 + (i % 3),
  dur: 14 + (i % 7) * 3,
  delay: -(i * 1.7),
  color: i % 3 === 0 ? 'rgba(201,162,39,.55)' : 'rgba(127,231,203,.45)',
}));

export function AmbientLayer() {
  return (
    <div aria-hidden className="pointer-events-none absolute inset-0">
      <div
        className="absolute inset-0"
        style={{
          background: `radial-gradient(1100px 700px at 72% 18%, rgba(31,165,136,.06), transparent 60%),
                       radial-gradient(900px 650px at 22% 82%, rgba(201,162,39,.05), transparent 60%),
                       #05080F`,
        }}
      />
      {PARTICLES.map((p, i) => (
        <span
          key={i}
          className="absolute rounded-full"
          style={{
            left: `${p.left}%`,
            top: `${p.top}%`,
            width: p.size,
            height: p.size,
            background: p.color,
            animation: `drift ${p.dur}s linear infinite`,
            animationDelay: `${p.delay}s`,
          }}
        />
      ))}
    </div>
  );
}
