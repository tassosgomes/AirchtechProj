import { Badge, Card } from '../components';

export function InventoryPage() {
  return (
    <Card title="Inventario" meta="RF-40">
      <table className="table">
        <thead>
          <tr>
            <th>Repositorio</th>
            <th>Stack</th>
            <th>Severidade</th>
            <th>Ultima Analise</th>
          </tr>
        </thead>
        <tbody>
          <tr>
            <td>modernization-api</td>
            <td>.NET 8</td>
            <td>
              <Badge tone="warning">High</Badge>
            </td>
            <td>ha 2h</td>
          </tr>
          <tr>
            <td>modernization-worker</td>
            <td>.NET 8</td>
            <td>
              <Badge tone="success">Low</Badge>
            </td>
            <td>ha 1d</td>
          </tr>
        </tbody>
      </table>
    </Card>
  );
}
