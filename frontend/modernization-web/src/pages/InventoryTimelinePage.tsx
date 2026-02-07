import { Card, StatusBadge } from '../components';

export function InventoryTimelinePage() {
  return (
    <Card title="Timeline de Analises" meta="Repositorio: modernization-api">
      <div className="timeline">
        <div className="timeline__item">
          <p className="card__meta">06/02/2026 - Job #8821</p>
          <StatusBadge status="completed" />
        </div>
        <div className="timeline__item">
          <p className="card__meta">03/02/2026 - Job #8780</p>
          <StatusBadge status="completed" />
        </div>
      </div>
    </Card>
  );
}
