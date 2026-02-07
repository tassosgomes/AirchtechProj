export type RequestStatus =
  | 'queued'
  | 'discovery_running'
  | 'analysis_running'
  | 'consolidating'
  | 'completed'
  | 'failed';
