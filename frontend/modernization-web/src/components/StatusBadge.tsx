import { Badge } from './Badge';
import type { RequestStatus } from '../types/analysis';

type StatusBadgeProps = {
  status: RequestStatus | string;
};

const STATUS_MAP: Record<string, { label: string; tone: 'success' | 'warning' | 'danger' | 'info' | 'neutral' }> = {
  queued: { label: 'Queued', tone: 'neutral' },
  discovery_running: { label: 'Discovery', tone: 'info' },
  analysis_running: { label: 'Analysis', tone: 'warning' },
  consolidating: { label: 'Consolidating', tone: 'info' },
  completed: { label: 'Completed', tone: 'success' },
  failed: { label: 'Failed', tone: 'danger' },
};

export function StatusBadge({ status }: StatusBadgeProps) {
  const normalized = status.toLowerCase();
  const mapped = STATUS_MAP[normalized] ?? { label: status, tone: 'neutral' };

  return <Badge tone={mapped.tone}>{mapped.label}</Badge>;
}
