export type InventorySeverity = 'Critical' | 'High' | 'Medium' | 'Low' | 'Informative';

export type InventoryRepositorySummary = {
  id: string;
  url: string;
  provider?: string;
  technologies: string[];
  lastAnalysisAt?: string | null;
  findingsBySeverity: Record<string, number>;
};

export type InventoryPagination = {
  page: number;
  size: number;
  total: number;
  totalPages: number;
};

export type InventoryRepositoryList = {
  data: InventoryRepositorySummary[];
  pagination: InventoryPagination;
};

export type RepositoryTimelineEntry = {
  id: string;
  analyzedAt: string;
  findingsBySeverity: Record<string, number>;
  requestId?: string | null;
};

export type RepositoryTimeline = {
  repositoryId: string;
  repositoryUrl?: string;
  entries: RepositoryTimelineEntry[];
};
