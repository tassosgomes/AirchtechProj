import type { ReactNode } from 'react';
import { Clock, RefreshCw, Search, CheckCircle2, XCircle, Zap } from 'lucide-react';
import { Badge } from './Badge';
import { normalizeRequestStatus, type RequestStatus } from '../types/analysis';

type StatusBadgeProps = {
  status: RequestStatus | string;
};

const STATUS_MAP: Record<
  RequestStatus,
  { label: string; tone: 'success' | 'warning' | 'danger' | 'info' | 'neutral'; icon: ReactNode }
> = {
  queued: { label: 'Queued', tone: 'neutral', icon: <Clock size={12} /> },
  discovery_running: { label: 'Discovery Running', tone: 'info', icon: <Search size={12} /> },
  analysis_running: { label: 'Analysis Running', tone: 'success', icon: <Zap size={12} /> },
  consolidating: { label: 'Consolidating', tone: 'warning', icon: <RefreshCw size={12} /> },
  completed: { label: 'Completed', tone: 'success', icon: <CheckCircle2 size={12} /> },
  failed: { label: 'Failed', tone: 'danger', icon: <XCircle size={12} /> },
};

export function StatusBadge({ status }: StatusBadgeProps) {
  const normalized = normalizeRequestStatus(String(status));
  const mapped = STATUS_MAP[normalized];

  return (
    <Badge tone={mapped.tone}>
      {mapped.icon}
      {mapped.label}
    </Badge>
  );
}
