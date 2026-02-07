import apiClient from './apiClient';
import type {
  InventoryRepositoryList,
  InventoryRepositorySummary,
  InventoryPagination,
  RepositoryTimeline,
  RepositoryTimelineEntry,
} from '../types/inventory';

const DEFAULT_PAGINATION: InventoryPagination = {
  page: 1,
  size: 12,
  total: 0,
  totalPages: 1,
};

const SEVERITY_KEYS = ['Critical', 'High', 'Medium', 'Low', 'Informative'];

function normalizeSeverityMap(input?: Record<string, number> | null): Record<string, number> {
  const result: Record<string, number> = {};

  SEVERITY_KEYS.forEach((key) => {
    result[key] = 0;
  });

  if (!input) {
    return result;
  }

  Object.entries(input).forEach(([key, value]) => {
    const matched = SEVERITY_KEYS.find(
      (known) => known.toLowerCase() === key.toLowerCase(),
    );
    if (matched) {
      result[matched] = value ?? 0;
    }
  });

  return result;
}

function normalizeRepositorySummary(raw: Record<string, unknown>): InventoryRepositorySummary {
  const url =
    (raw.repositoryUrl as string) ||
    (raw.url as string) ||
    (raw.repoUrl as string) ||
    '';

  const technologies =
    (raw.technologies as string[]) ||
    (raw.languages as string[]) ||
    (raw.stack as string[]) ||
    [];

  const summary = raw.summary as Record<string, unknown> | undefined;
  const severityMap =
    (raw.findingsBySeverity as Record<string, number>) ||
    (raw.summaryBySeverity as Record<string, number>) ||
    (summary?.bySeverity as Record<string, number>) ||
    null;

  return {
    id: String(raw.id ?? url),
    url,
    provider: (raw.provider as string) || (raw.sourceProvider as string),
    technologies,
    lastAnalysisAt:
      (raw.lastAnalysisAt as string) ||
      (raw.lastAnalyzedAt as string) ||
      (raw.lastAnalysisDate as string) ||
      null,
    findingsBySeverity: normalizeSeverityMap(severityMap),
  };
}

function normalizePagination(raw?: Record<string, unknown> | null): InventoryPagination {
  if (!raw) {
    return DEFAULT_PAGINATION;
  }

  return {
    page: Number(raw.page ?? raw.currentPage ?? DEFAULT_PAGINATION.page),
    size: Number(raw.size ?? raw.pageSize ?? DEFAULT_PAGINATION.size),
    total: Number(raw.total ?? raw.totalItems ?? DEFAULT_PAGINATION.total),
    totalPages: Number(raw.totalPages ?? raw.pages ?? DEFAULT_PAGINATION.totalPages),
  };
}

function normalizeRepositoryList(
  payload: InventoryRepositorySummary[] | Record<string, unknown>,
): InventoryRepositoryList {
  if (Array.isArray(payload)) {
    return {
      data: payload.map((item) => normalizeRepositorySummary(item as Record<string, unknown>)),
      pagination: {
        ...DEFAULT_PAGINATION,
        total: payload.length,
        totalPages: 1,
      },
    };
  }

  const data = (payload.data as Record<string, unknown>[]) || [];
  const pagination = normalizePagination(payload.pagination as Record<string, unknown>);

  return {
    data: data.map((item) => normalizeRepositorySummary(item)),
    pagination,
  };
}

function normalizeTimelineEntry(raw: Record<string, unknown>): RepositoryTimelineEntry {
  const summary = raw.summary as Record<string, unknown> | undefined;
  const severityMap =
    (raw.findingsBySeverity as Record<string, number>) ||
    (raw.summaryBySeverity as Record<string, number>) ||
    (summary?.bySeverity as Record<string, number>) ||
    (raw.counts as Record<string, number>) ||
    null;

  const analyzedAt =
    (raw.analyzedAt as string) ||
    (raw.analysisDate as string) ||
    (raw.date as string) ||
    (raw.createdAt as string) ||
    '';

  return {
    id: String(
      raw.id ??
        raw.requestId ??
        raw.analysisRequestId ??
        raw.analysisId ??
        analyzedAt,
    ),
    analyzedAt,
    findingsBySeverity: normalizeSeverityMap(severityMap),
    requestId: (raw.requestId as string) || (raw.analysisRequestId as string) || null,
  };
}

function normalizeTimeline(payload: Record<string, unknown>): RepositoryTimeline {
  const entriesSource =
    (payload.entries as Record<string, unknown>[]) ||
    (payload.timeline as Record<string, unknown>[]) ||
    (payload.history as Record<string, unknown>[]) ||
    (payload.analyses as Record<string, unknown>[]) ||
    [];

  return {
    repositoryId: String(payload.repositoryId ?? payload.id ?? ''),
    repositoryUrl: (payload.repositoryUrl as string) || (payload.url as string),
    entries: entriesSource.map((item) => normalizeTimelineEntry(item)),
  };
}

type InventoryQuery = {
  page?: number;
  size?: number;
  search?: string;
  technology?: string;
  dependency?: string;
  severities?: string[];
  dateFrom?: string;
  dateTo?: string;
};

function buildQueryParams(filters: InventoryQuery) {
  const params: Record<string, string | number | string[] | undefined> = {
    _page: filters.page,
    _size: filters.size,
    technology: filters.technology || undefined,
    dependency: filters.dependency || undefined,
    severity: filters.severities && filters.severities.length > 0 ? filters.severities : undefined,
    dateFrom: filters.dateFrom || undefined,
    dateTo: filters.dateTo || undefined,
    search: filters.search?.trim() || undefined,
  };

  return params;
}

export async function listInventoryRepositories(
  filters: InventoryQuery,
): Promise<InventoryRepositoryList> {
  const response = await apiClient.get('/api/v1/inventory/repositories', {
    params: buildQueryParams(filters),
  });

  return normalizeRepositoryList(response.data as InventoryRepositorySummary[] | Record<string, unknown>);
}

export async function getRepositoryTimeline(id: string): Promise<RepositoryTimeline> {
  const response = await apiClient.get(`/api/v1/inventory/repositories/${id}/timeline`);
  return normalizeTimeline(response.data as Record<string, unknown>);
}
