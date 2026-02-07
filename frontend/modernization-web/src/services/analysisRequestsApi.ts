import apiClient from './apiClient';

export type AnalysisType = 'Obsolescence' | 'Security' | 'Observability' | 'Documentation';
export type SourceProvider = 'GitHub' | 'AzureDevOps';

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
