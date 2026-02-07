import { Badge, Card, Spinner, StatusBadge } from '../components';

export function DashboardPage() {
  return (
    <>
      <div className="content-grid">
        <Card title="Pipeline Ativo" meta="Fila principal">
          <StatusBadge status="analysis_running" />
          <p className="card__meta">Repo: modernization-platform-api</p>
          <Spinner />
        </Card>
        <Card title="Solicitacoes Hoje" meta="Ultimas 24h">
          <h2>12</h2>
          <Badge tone="info">+3% vs ontem</Badge>
        </Card>
        <Card title="Falhas Recentes" meta="Ultimas 7d">
          <h2>2</h2>
          <Badge tone="danger">Critical</Badge>
        </Card>
      </div>
      <Card title="Timeline de Execucao" meta="Atualizado agora">
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
