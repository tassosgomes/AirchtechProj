import { useEffect, useMemo, useState } from 'react';
import { useParams } from 'react-router-dom';
import { CheckCircle2, Clock, RefreshCw, Search, Zap } from 'lucide-react';
import { Badge, Button, Card, Spinner, StatusBadge } from '../components';
import {
  getAnalysisRequest,
  getAnalysisRequestResults,
  getConsolidatedResults,
} from '../services/analysisRequestsApi';
import {
  normalizeJobStatus,
  normalizeRequestStatus,
  normalizeSeverity,
  type AnalysisRequest,
  type AnalysisRequestResults,
  type ConsolidatedResult,
  type JobStatus,
  type Severity,
} from '../types/analysis';

const PIPELINE_STEPS = [
  { key: 'queued', label: 'Fila', icon: <Clock size={14} /> },
  { key: 'discovery_running', label: 'Discovery', icon: <Search size={14} /> },
  { key: 'analysis_running', label: 'Analises', icon: <Zap size={14} /> },
  { key: 'consolidating', label: 'Consolidacao', icon: <RefreshCw size={14} /> },
  { key: 'completed', label: 'Completo', icon: <CheckCircle2 size={14} /> },
];

const STATUS_ORDER = [
  'queued',
  'discovery_running',
  'analysis_running',
  'consolidating',
  'completed',
] as const;

const SEVERITY_ORDER: Severity[] = ['Critical', 'High', 'Medium', 'Low', 'Informative'];

const JOB_TONE: Record<JobStatus, 'neutral' | 'info' | 'success' | 'danger'> = {
  pending: 'neutral',
  running: 'info',
  completed: 'success',
  failed: 'danger',
};

function formatDateTime(value?: string | null) {
  if (!value) {
    return '-';
  }

  return new Date(value).toLocaleString('en-GB', {
    dateStyle: 'medium',
    timeStyle: 'short',
  });
}

function formatDuration(value?: number | null) {
  if (!value) {
    return '-';
  }

  const totalSeconds = Math.floor(value / 1000);
  const minutes = Math.floor(totalSeconds / 60);
  const seconds = totalSeconds % 60;

  if (minutes > 0) {
    return `${minutes}m ${seconds}s`;
  }

  return `${seconds}s`;
}

function getActiveIndex(status: string) {
  if (status === 'failed') {
    return STATUS_ORDER.length - 1;
  }

  return STATUS_ORDER.indexOf(status as (typeof STATUS_ORDER)[number]);
}

function getSeverityClass(severity: string) {
  const normalized = normalizeSeverity(severity);
  return `badge--severity-${normalized}`;
}

function resolveSeverityLabel(severity: string): string {
  return (
    SEVERITY_ORDER.find(
      (known) => normalizeSeverity(known) === normalizeSeverity(severity),
    ) ?? severity
  );
}

export function RequestDetailsPage() {
  const { id } = useParams();
  const [request, setRequest] = useState<AnalysisRequest | null>(null);
  const [results, setResults] = useState<AnalysisRequestResults | null>(null);
  const [consolidated, setConsolidated] = useState<ConsolidatedResult | null>(null);
  const [isLoading, setIsLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [severityFilter, setSeverityFilter] = useState('all');
  const [categoryFilter, setCategoryFilter] = useState('all');
  const [fileFilter, setFileFilter] = useState('');

  useEffect(() => {
    if (!id) {
      return;
    }

    const load = async () => {
      setIsLoading(true);
      setError(null);

      try {
        const [requestData, resultsData] = await Promise.all([
          getAnalysisRequest(id),
          getAnalysisRequestResults(id),
        ]);
        setRequest(requestData);
        setResults(resultsData);
      } catch (requestError) {
        setError('Nao foi possivel carregar os detalhes da solicitacao.');
      }

      try {
        const consolidatedData = await getConsolidatedResults(id);
        setConsolidated(consolidatedData);
      } catch {
        setConsolidated(null);
      } finally {
        setIsLoading(false);
      }
    };

    load();
  }, [id]);

  const normalizedStatus = request ? normalizeRequestStatus(request.status) : 'queued';
  const activeIndex = getActiveIndex(normalizedStatus);

  const availableCategories = useMemo(() => {
    const categories = new Set<string>();
    consolidated?.findings.forEach((finding) => {
      if (finding.category) {
        categories.add(finding.category);
      }
    });
    return Array.from(categories).sort();
  }, [consolidated]);

  const severityCounts = useMemo(() => {
    const counts: Record<string, number> = {};
    const fromSummary = consolidated?.summary?.bySeverity ?? {};

    SEVERITY_ORDER.forEach((severity) => {
      const direct = fromSummary[severity];
      const fallback = fromSummary[severity.toLowerCase()];
      counts[severity] = direct ?? fallback ?? 0;
    });

    if (consolidated?.findings?.length && Object.values(counts).every((count) => count === 0)) {
      consolidated.findings.forEach((finding) => {
        const matched =
          SEVERITY_ORDER.find(
            (severity) => normalizeSeverity(severity) === normalizeSeverity(finding.severity),
          ) ?? String(finding.severity);
        counts[matched] = (counts[matched] ?? 0) + 1;
      });
    }

    return counts;
  }, [consolidated]);

  const maxSeverityCount = Math.max(1, ...Object.values(severityCounts));

  const filteredFindings = useMemo(() => {
    const findings = consolidated?.findings ?? [];

    return findings.filter((finding) => {
      if (severityFilter !== 'all') {
        if (normalizeSeverity(finding.severity) !== severityFilter) {
          return false;
        }
      }

      if (categoryFilter !== 'all') {
        if (finding.category.toLowerCase() !== categoryFilter.toLowerCase()) {
          return false;
        }
      }

      if (fileFilter.trim()) {
        return finding.filePath.toLowerCase().includes(fileFilter.trim().toLowerCase());
      }

      return true;
    });
  }, [consolidated, severityFilter, categoryFilter, fileFilter]);

  const groupedFindings = useMemo(() => {
    const groups: Record<string, typeof filteredFindings> = {};
    filteredFindings.forEach((finding) => {
      const key = resolveSeverityLabel(String(finding.severity));
      groups[key] = groups[key] ? [...groups[key], finding] : [finding];
    });
    return groups;
  }, [filteredFindings]);

  if (!id) {
    return <Card title="Solicitacao" meta="ID invalido">ID nao informado.</Card>;
  }

  return (
    <>
      <Card title="Solicitacao" meta={request ? request.provider : 'Carregando'}>
        {isLoading && <Spinner className="spinner--inline" />}
        {!isLoading && error && <p className="form-error">{error}</p>}
        {!isLoading && request && (
          <div className="request-detail">
            <div>
              <p className="request-card__repo">{request.repositoryUrl}</p>
              <div className="request-detail__meta">
                <span>Criado em {formatDateTime(request.createdAt)}</span>
                <span>Concluido em {formatDateTime(request.completedAt)}</span>
              </div>
            </div>
            <div className="request-detail__status">
              <StatusBadge status={request.status} />
              {normalizedStatus === 'queued' && request.queuePosition != null && (
                <Badge tone="neutral">Posicao {request.queuePosition}</Badge>
              )}
            </div>
          </div>
        )}
      </Card>

      <Card title="Pipeline detalhado" meta="Fila - Discovery - Analises - Consolidacao">
        {request && (
          <div className="pipeline pipeline--detailed">
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
        )}
        {results && (
          <div className="job-grid">
            {results.jobs.map((job) => {
              const jobStatus = normalizeJobStatus(job.status);
              return (
                <div key={job.analysisType} className="job-card">
                  <div>
                    <p className="card__meta">{job.analysisType}</p>
                    <Badge tone={JOB_TONE[jobStatus]}>{jobStatus}</Badge>
                  </div>
                  <div>
                    <p className="card__meta">Duracao</p>
                    <span className="job-card__duration">{formatDuration(job.durationMs)}</span>
                  </div>
                </div>
              );
            })}
          </div>
        )}
      </Card>

      <Card title="Resultados consolidados" meta="RF-39">
        {!consolidated && !isLoading && (
          <div className="request-empty">
            Resultados ainda nao disponiveis.
            <Button variant="ghost" onClick={() => window.location.reload()}>
              Atualizar
            </Button>
          </div>
        )}

        {consolidated && (
          <div className="results-grid">
            <div className="summary-panel">
              <div className="summary-header">
                <div>
                  <p className="card__meta">Total de findings</p>
                  <h2>{consolidated.summary.totalFindings}</h2>
                </div>
                <div className="summary-meta">
                  <span>Concluido em {formatDateTime(consolidated.completedAt)}</span>
                </div>
              </div>
              <div className="summary-grid">
                {SEVERITY_ORDER.map((severity) => {
                  const count = severityCounts[severity] ?? 0;
                  const width = Math.round((count / maxSeverityCount) * 100);
                  return (
                    <div key={severity} className="summary-item">
                      <div className="summary-item__header">
                        <Badge className={getSeverityClass(severity)}>{severity}</Badge>
                        <span className="summary-item__count">{count}</span>
                      </div>
                      <div className="summary-bar">
                        <span
                          className={`summary-bar__fill summary-bar__fill--${normalizeSeverity(severity)}`}
                          style={{ width: `${width}%` }}
                        />
                      </div>
                    </div>
                  );
                })}
              </div>
            </div>

            <div className="filters">
              <div className="filters__row">
                <label className="input-field">
                  <span className="input-field__label">Severidade</span>
                  <select
                    className="input-field__select"
                    value={severityFilter}
                    onChange={(event) => setSeverityFilter(event.target.value)}
                  >
                    <option value="all">Todas</option>
                    {SEVERITY_ORDER.map((severity) => (
                      <option key={severity} value={normalizeSeverity(severity)}>
                        {severity}
                      </option>
                    ))}
                  </select>
                </label>
                <label className="input-field">
                  <span className="input-field__label">Categoria</span>
                  <select
                    className="input-field__select"
                    value={categoryFilter}
                    onChange={(event) => setCategoryFilter(event.target.value)}
                  >
                    <option value="all">Todas</option>
                    {availableCategories.map((category) => (
                      <option key={category} value={category}>
                        {category}
                      </option>
                    ))}
                  </select>
                </label>
                <label className="input-field">
                  <span className="input-field__label">Arquivo</span>
                  <input
                    className="input-field__control"
                    placeholder="src/services/api.ts"
                    value={fileFilter}
                    onChange={(event) => setFileFilter(event.target.value)}
                  />
                </label>
              </div>
            </div>

            <div className="findings">
              {filteredFindings.length === 0 && (
                <div className="request-empty">Nenhum finding encontrado.</div>
              )}
              {SEVERITY_ORDER.map((severity) => {
                const group = groupedFindings[severity];
                if (!group || group.length === 0) {
                  return null;
                }
                return (
                  <div key={severity} className="finding-group">
                    <div className="finding-group__header">
                      <Badge className={getSeverityClass(severity)}>{severity}</Badge>
                      <span className="finding-group__count">{group.length} itens</span>
                    </div>
                    <div className="finding-grid">
                      {group.map((finding) => (
                        <div key={finding.id} className="finding-card">
                          <div className="finding-card__header">
                            <Badge className={getSeverityClass(finding.severity)}>
                              {finding.severity}
                            </Badge>
                            <span className="finding-card__category">{finding.category}</span>
                          </div>
                          <h4 className="finding-card__title">{finding.title}</h4>
                          <p className="finding-card__description">{finding.description}</p>
                          <span className="finding-card__file">{finding.filePath}</span>
                        </div>
                      ))}
                    </div>
                  </div>
                );
              })}
            </div>
          </div>
        )}
      </Card>
    </>
  );
}
