import { createContext, useCallback, useContext, useState } from 'react';

export interface Dive {
  module: string | null;
  /** Tọa độ % viewport — tâm "ống kính hiển vi" khi zoom */
  origin: { x: number; y: number };
}

interface DiveApi {
  dive: Dive;
  diveTo: (module: string, e: React.MouseEvent) => void;
  surface: () => void;
}

const DiveCtx = createContext<DiveApi | null>(null);

export function DiveProvider({ children }: { children: React.ReactNode }) {
  const [dive, setDive] = useState<Dive>({ module: null, origin: { x: 50, y: 50 } });

  const diveTo = useCallback((module: string, e: React.MouseEvent) => {
    setDive({
      module,
      origin: {
        x: (e.clientX / window.innerWidth) * 100,
        y: (e.clientY / window.innerHeight) * 100,
      },
    });
  }, []);

  const surface = useCallback(() => setDive(d => ({ ...d, module: null })), []);

  return <DiveCtx.Provider value={{ dive, diveTo, surface }}>{children}</DiveCtx.Provider>;
}

export function useDive(): DiveApi {
  const ctx = useContext(DiveCtx);
  if (!ctx) throw new Error('useDive phải nằm trong <DiveProvider>');
  return ctx;
}
