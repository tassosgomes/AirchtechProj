import { useEffect, useMemo, useState } from 'react';
import { useNavigate, useParams } from 'react-router-dom';
import { ArrowLeft } from 'lucide-react';
import { Badge, Button, Card, Spinner } from '../components';
import { getRepositoryTimeline } from '../services/inventoryApi';
import { normalizeSeverity } from '../types/analysis';
import type { RepositoryTimeline, RepositoryTimelineEntry } from '../types/inventory';

const SEVERITY_ORDER = ['Critical', 'High', 'Medium', 'Low', 'Informative'];
const CHART_WIDTH = 720;
const CHART_HEIGHT = 220;
const CHART_PADDING = 32;

function formatDate(value?: string) {
  if (!value) {
    return '-';
  }

  return new Date(value).toLocaleDateString('en-GB', {
    dateStyle: 'medium',
  });
}

function buildPoints(entries: RepositoryTimelineEntry[]) {
  if (entries.length === 0) {
    return [];
  }

  const maxValue = Math.max(
    1,
    ...entries.flatMap((entry) =>
      SEVERITY_ORDER.map((severity) => entry.findingsBySeverity[severity] ?? 0),
    ),
  );

  const span = CHART_WIDTH - CHART_PADDING * 2;
  const step = entries.length > 1 ? span / (entries.length - 1) : 0;

  const mapX = (index: number) =>
    CHART_PADDING + (entries.length > 1 ? index * step : span / 2);

  const mapY = (value: number) => {
    const height = CHART_HEIGHT - CHART_PADDING * 2;
    return CHART_HEIGHT - CHART_PADDING - (value / maxValue) * height;
  };

  return SEVERITY_ORDER.map((severity) => {
    const points = entries
      .map((entry, index) => {
        const value = entry.findingsBySeverity[severity] ?? 0;
        return `${mapX(index)},${mapY(value)}`;
      })
      .join(' ');

    return { severity, points };
  });
}

export function InventoryTimelinePage() {
  const { id } = useParams();
  const [timeline, setTimeline] = useState<RepositoryTimeline | null>(null);
  const [isLoading, setIsLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const navigate = useNavigate();

  useEffect(() => {
    if (!id) {
      return;
    }

    const loadTimeline = async () => {
      setIsLoading(true);
      setError(null);
      try {
        const response = await getRepositoryTimeline(id);
        setTimeline(response);
      } catch (requestError) {
        setError('Nao foi possivel carregar o historico do repositorio.');
      } finally {
        setIsLoading(false);
      }
    };

    loadTimeline();
  }, [id]);

  const entries = useMemo(() => {
    const raw = timeline?.entries ?? [];
    return raw
      .filter((entry) => entry.analyzedAt)
      .sort((a, b) => new Date(a.analyzedAt).getTime() - new Date(b.analyzedAt).getTime());
  }, [timeline]);

  const series = useMemo(() => buildPoints(entries), [entries]);

  if (!id) {
    return <Card title="Timeline" meta="ID invalido">Repositorio nao informado.</Card>;
  }

  return (
    <>
      <div className="dashboard-header">
        <div>
          <h2 className="page-title">Timeline do Repositorio</h2>
          <span className="page-subtitle">
            {timeline?.repositoryUrl ?? 'Evolucao de achados por severidade'}
          </span>
        </div>
        <Button variant="ghost" type="button" onClick={() => navigate('/inventory')}>
          <ArrowLeft size={16} /> Voltar ao inventario
        </Button>
      </div>

      <Card title="Findings por severidade" meta="RF-41">
        {isLoading && (
          <div className="request-empty">
            <Spinner className="spinner--inline" />
            Carregando timeline...
          </div>
        )}
        {!isLoading && error && <p className="form-error">{error}</p>}
        {!isLoading && !error && entries.length === 0 && (
          <div className="request-empty">Nenhuma analise registrada.</div>
        )}
        {!isLoading && !error && entries.length > 0 && (
          <div className="timeline-chart">
            <svg viewBox={`0 0 ${CHART_WIDTH} ${CHART_HEIGHT}`} aria-hidden="true">
              <rect
                x={CHART_PADDING}
                y={CHART_PADDING}
                width={CHART_WIDTH - CHART_PADDING * 2}
                height={CHART_HEIGHT - CHART_PADDING * 2}
                className="timeline-chart__frame"
              />
              {series.map((line) => (
                <polyline
                  key={line.severity}
                  points={line.points}
                  className={`timeline-chart__line timeline-chart__line--${normalizeSeverity(
                    line.severity,
                  )}`}
                />
              ))}
            </svg>
            <div className="timeline-axis">
              {entries.map((entry) => (
                <span key={entry.id} className="timeline-axis__label">
                  {formatDate(entry.analyzedAt)}
                </span>
              ))}
            </div>
            <div className="timeline-legend">
              {SEVERITY_ORDER.map((severity) => (
                <Badge
                  key={severity}
                  className={`badge--severity-${normalizeSeverity(severity)}`}
                >
                  {severity}
                </Badge>
              ))}
            </div>
          </div>
        )}
      </Card>

      <Card title="Historico de analises" meta="Clique para detalhes">
        {!isLoading && !error && entries.length === 0 && (
          <div className="request-empty">Nenhum historico disponivel.</div>
        )}
        {!isLoading && !error && entries.length > 0 && (
          <div className="timeline-list">
            {entries
              .slice()
              .reverse()
              .map((entry) => (
                <div key={entry.id} className="timeline-entry">
                  <div>
                    <p className="timeline-entry__date">{formatDate(entry.analyzedAt)}</p>
                    <div className="timeline-entry__counts">
                      {SEVERITY_ORDER.map((severity) => (
                        <div key={severity} className="severity-count">
                          <Badge
                            className={`badge--severity-${normalizeSeverity(severity)}`}
                          >
                            {severity}
                          </Badge>
                          <span>{entry.findingsBySeverity[severity] ?? 0}</span>
                        </div>
                      ))}
                    </div>
                  </div>
                  <div className="timeline-entry__actions">
                    {entry.requestId ? (
                      <Button
                        type="button"
                        variant="secondary"
                        onClick={() => navigate(`/requests/${entry.requestId}`)}
                      >
                        Ver detalhes
                      </Button>
                    ) : (
                      <span className="card__meta">Sem detalhamento</span>
                    )}
                  </div>
                </div>
              ))}
          </div>
        )}
      </Card>
    </>
  );
}
