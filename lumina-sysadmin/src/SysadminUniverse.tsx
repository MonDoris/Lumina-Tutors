import { DiveProvider } from './dive-context';
import { AmbientLayer } from './components/AmbientLayer';
import { CitadelHeader } from './components/CitadelHeader';
import { TreeOfKnowledgeNav } from './components/TreeOfKnowledgeNav';
import { TelemetryRail } from './components/TelemetryRail';
import { DiveStage } from './DiveStage';

export default function SysadminUniverse() {
  return (
    <DiveProvider>
      <div className="relative h-screen overflow-hidden bg-obsidian-950 font-body text-ivory">
        <AmbientLayer />
        <DiveStage />
        <CitadelHeader />
        <TreeOfKnowledgeNav />
        <TelemetryRail />
      </div>
    </DiveProvider>
  );
}
