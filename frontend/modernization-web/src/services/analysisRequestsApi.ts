import apiClient from './apiClient';
import type {
  AnalysisRequest,
  AnalysisRequestListResponse,
  AnalysisRequestResults,
  AnalysisType,
  ConsolidatedResult,
  SourceProvider,
} from '../types/analysis';

type CreateAnalysisRequestPayload = {
  repositoryUrl: string;
  provider: SourceProvider;
  accessToken?: string;
  selectedTypes: AnalysisType[];
};

export async function createAnalysisRequest(
  payload: CreateAnalysisRequestPayload,
): Promise<void> {
  await apiClient.post('/api/v1/analysis-requests', payload);
}

export async function listAnalysisRequests(
  page = 1,
  size = 50,
): Promise<AnalysisRequest[]> {
  const response = await apiClient.get<AnalysisRequestListResponse | AnalysisRequest[]>(
    '/api/v1/analysis-requests',
    {
      params: { _page: page, _size: size },
    },
  );

  if (Array.isArray(response.data)) {
    return response.data;
  }

  return response.data.data ?? [];
}

export async function getAnalysisRequest(id: string): Promise<AnalysisRequest> {
  const response = await apiClient.get<AnalysisRequest>(`/api/v1/analysis-requests/${id}`);
  return response.data;
}

export async function getAnalysisRequestResults(
  id: string,
): Promise<AnalysisRequestResults> {
  const response = await apiClient.get<AnalysisRequestResults>(
    `/api/v1/analysis-requests/${id}/results`,
  );
  return response.data;
}

export async function getConsolidatedResults(id: string): Promise<ConsolidatedResult> {
  const response = await apiClient.get<ConsolidatedResult>(
    `/api/v1/analysis-requests/${id}/consolidated`,
  );
  return response.data;
}
