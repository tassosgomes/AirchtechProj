import { useCallback, useEffect, useMemo, useState } from 'react';
import { useNavigate } from 'react-router-dom';
import { CheckCircle2, Clock, RefreshCw, Search, Zap } from 'lucide-react';
import { Badge, Button, Card, Spinner, StatusBadge } from '../components';
import { listAnalysisRequests } from '../services/analysisRequestsApi';
import {
  normalizeRequestStatus,
  type AnalysisRequest,
  type RequestStatus,
} from '../types/analysis';

const PIPELINE_STEPS = [
  { key: 'queued', label: 'Fila', icon: <Clock size={14} /> },
  { key: 'discovery_running', label: 'Discovery', icon: <Search size={14} /> },
  { key: 'analysis_running', label: 'Analises', icon: <Zap size={14} /> },
  { key: 'consolidating', label: 'Consolidacao', icon: <RefreshCw size={14} /> },
  { key: 'completed', label: 'Completo', icon: <CheckCircle2 size={14} /> },
];

const STATUS_ORDER: RequestStatus[] = [
  'queued',
  'discovery_running',
  'analysis_running',
  'consolidating',
  'completed',
];

function formatDateTime(value: string) {
  if (!value) {
    return '-';
  }

  return new Date(value).toLocaleString('en-GB', {
    dateStyle: 'medium',
    timeStyle: 'short',
  });
}

function getActiveIndex(status: RequestStatus) {
  if (status === 'failed') {
    return STATUS_ORDER.length - 1;
  }

  return STATUS_ORDER.indexOf(status);
}

export function DashboardPage() {
  const [requests, setRequests] = useState<AnalysisRequest[]>([]);
  const [isLoading, setIsLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const navigate = useNavigate();

  const loadRequests = useCallback(async () => {
    try {
      const data = await listAnalysisRequests(1, 50);
      setRequests(data);
      setError(null);
    } catch (requestError) {
      setError('Nao foi possivel carregar as solicitacoes.');
    } finally {
      setIsLoading(false);
    }
  }, []);

  useEffect(() => {
    loadRequests();
    const interval = window.setInterval(loadRequests, 5000);
    return () => window.clearInterval(interval);
  }, [loadRequests]);

  const summary = useMemo(() => {
    const total = requests.length;
    const queued = requests.filter(
      (request) => normalizeRequestStatus(request.status) === 'queued',
    ).length;
    const active = requests.filter((request) =>
      ['discovery_running', 'analysis_running', 'consolidating'].includes(
        normalizeRequestStatus(request.status),
      ),
    ).length;
    const completed = requests.filter(
      (request) => normalizeRequestStatus(request.status) === 'completed',
    ).length;
    const failed = requests.filter(
      (request) => normalizeRequestStatus(request.status) === 'failed',
    ).length;

    return { total, queued, active, completed, failed };
  }, [requests]);

  return (
    <>
      <div className="dashboard-header">
        <div>
          <h2 className="page-title">Minhas Solicitacoes</h2>
          <span className="page-subtitle">Pipeline em tempo real com polling a cada 5s</span>
        </div>
        <Button variant="primary" onClick={() => navigate('/requests/new')}>
          + Nova
        </Button>
      </div>

      <div className="content-grid">
        <Card title="Solicitacoes" meta="Visao geral">
          <div className="summary-metrics">
            <div>
              <p className="card__meta">Total</p>
              <h2>{summary.total}</h2>
            </div>
            <div>
              <p className="card__meta">Em fila</p>
              <Badge tone="neutral">{summary.queued}</Badge>
            </div>
            <div>
              <p className="card__meta">Em execucao</p>
              <Badge tone="info">{summary.active}</Badge>
            </div>
            <div>
              <p className="card__meta">Concluidas</p>
              <Badge tone="success">{summary.completed}</Badge>
            </div>
            <div>
              <p className="card__meta">Falhas</p>
              <Badge tone="danger">{summary.failed}</Badge>
            </div>
          </div>
        </Card>
      </div>

      <Card title="Solicitacoes recentes" meta="Clique para ver detalhes">
        {isLoading && (
          <div className="request-empty">
            <Spinner className="spinner--inline" />
            Carregando solicitacoes...
          </div>
        )}
        {!isLoading && error && <p className="form-error">{error}</p>}
        {!isLoading && !error && requests.length === 0 && (
          <div className="request-empty">
            Nenhuma solicitacao encontrada.
            <Button variant="secondary" onClick={() => navigate('/requests/new')}>
              Criar nova
            </Button>
          </div>
        )}
        {!isLoading && !error && requests.length > 0 && (
          <div className="request-list">
            {requests.map((request) => {
              const normalizedStatus = normalizeRequestStatus(request.status);
              const activeIndex = getActiveIndex(normalizedStatus);
              const isRunning = [
                'discovery_running',
                'analysis_running',
                'consolidating',
              ].includes(normalizedStatus);

              return (
                <button
                  key={request.id}
                  type="button"
                  className="request-card"
                  onClick={() => navigate(`/requests/${request.id}`)}
                >
                  <div className="request-card__header">
                    <div>
                      <p className="request-card__repo">{request.repositoryUrl}</p>
                      <span className="card__meta">{request.provider}</span>
                    </div>
                    <div className="request-card__status">
                      <StatusBadge status={request.status} />
                      {isRunning && <Spinner className="spinner--inline" />}
                    </div>
                  </div>
                  <div className="pipeline">
                    {PIPELINE_STEPS.map((step, index) => {
                      const stepState = index < activeIndex ? 'done' : index === activeIndex ? 'active' : 'pending';
                      const classes = [
                        'pipeline-step',
                        `pipeline-step--${stepState}`,
                        normalizedStatus === 'failed' && index === activeIndex ? 'pipeline-step--failed' : '',
                      ]
                        .filter(Boolean)
                        .join(' ');

                      return (
                        <div key={step.key} className={classes}>
                          <span className="pipeline-step__icon">{step.icon}</span>
                          <span className="pipeline-step__label">{step.label}</span>
                        </div>
                      );
                    })}
                  </div>
                  <div className="request-card__meta">
                    <span>
                      Posicao na fila:{' '}
                      <strong>
                        {normalizedStatus === 'queued' && request.queuePosition != null
                          ? request.queuePosition
                          : '-'}
                      </strong>
                    </span>
                    <span>Criado em {formatDateTime(request.createdAt)}</span>
                  </div>
                </button>
              );
            })}
          </div>
        )}
      </Card>
    </>
  );
}
