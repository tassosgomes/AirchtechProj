import { Card, StatusBadge } from '../components';

export function RequestDetailsPage() {
  return (
    <>
      <Card title="Solicitacao #1284" meta="GitHub | modernization-platform">
        <StatusBadge status="analysis_running" />
        <p className="card__meta">Fila: 2 | ETA: 35m</p>
      </Card>
      <Card title="Etapas" meta="Pipeline">
        <div className="content-grid">
          <div>
            <p className="card__meta">Discovery</p>
            <StatusBadge status="completed" />
          </div>
          <div>
            <p className="card__meta">Security</p>
            <StatusBadge status="analysis_running" />
          </div>
          <div>
            <p className="card__meta">Consolidation</p>
            <StatusBadge status="queued" />
          </div>
        </div>
      </Card>
    </>
  );
}
