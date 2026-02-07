export type AnalysisType = 'Obsolescence' | 'Security' | 'Observability' | 'Documentation';
export type SourceProvider = 'GitHub' | 'AzureDevOps';

export type RequestStatus =
  | 'queued'
  | 'discovery_running'
  | 'analysis_running'
  | 'consolidating'
  | 'completed'
  | 'failed';

export type ApiRequestStatus =
  | 'Queued'
  | 'DiscoveryRunning'
  | 'AnalysisRunning'
  | 'Consolidating'
  | 'Completed'
  | 'Failed';

export type JobStatus = 'pending' | 'running' | 'completed' | 'failed';
export type ApiJobStatus = 'Pending' | 'Running' | 'Completed' | 'Failed';

export type Severity = 'Critical' | 'High' | 'Medium' | 'Low' | 'Informative';

export type AnalysisRequest = {
  id: string;
  repositoryUrl: string;
  provider: SourceProvider;
  status: ApiRequestStatus;
  queuePosition: number | null;
  selectedTypes: AnalysisType[];
  createdAt: string;
  completedAt: string | null;
};

export type AnalysisRequestListResponse = {
  data: AnalysisRequest[];
  pagination: {
    page: number;
    size: number;
    total: number;
    totalPages: number;
  };
};

export type AnalysisJobResult = {
  analysisType: AnalysisType;
  status: ApiJobStatus;
  outputJson: string | null;
  durationMs: number | null;
};

export type AnalysisRequestResults = {
  requestId: string;
  status: ApiRequestStatus;
  jobs: AnalysisJobResult[];
};

export type Finding = {
  id: string;
  severity: Severity | string;
  category: string;
  title: string;
  description: string;
  filePath: string;
};

export type ConsolidatedResult = {
  requestId: string;
  repositoryUrl: string;
  completedAt: string;
  summary: {
    totalFindings: number;
    bySeverity: Record<string, number>;
    byCategory: Record<string, number>;
  };
  findings: Finding[];
};

const REQUEST_STATUS_VALUES: RequestStatus[] = [
  'queued',
  'discovery_running',
  'analysis_running',
  'consolidating',
  'completed',
  'failed',
];

const JOB_STATUS_VALUES: JobStatus[] = ['pending', 'running', 'completed', 'failed'];

export function normalizeRequestStatus(status: string): RequestStatus {
  const normalized = status
    .replace(/([a-z0-9])([A-Z])/g, '$1_$2')
    .replace(/\s+/g, '_')
    .toLowerCase();

  if (REQUEST_STATUS_VALUES.includes(normalized as RequestStatus)) {
    return normalized as RequestStatus;
  }

  return 'queued';
}

export function normalizeJobStatus(status: string): JobStatus {
  const normalized = status
    .replace(/([a-z0-9])([A-Z])/g, '$1_$2')
    .replace(/\s+/g, '_')
    .toLowerCase();

  if (JOB_STATUS_VALUES.includes(normalized as JobStatus)) {
    return normalized as JobStatus;
  }

  return 'pending';
}

export function normalizeSeverity(value: string): string {
  return value.trim().toLowerCase();
}
