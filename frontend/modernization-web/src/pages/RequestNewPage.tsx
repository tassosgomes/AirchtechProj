import { useState } from 'react';
import { Badge, Button, Card, Input, TextArea } from '../components';

export function RequestNewPage() {
  const [url, setUrl] = useState('');
  const [provider, setProvider] = useState('GitHub');

  return (
    <Card title="Nova Solicitacao" meta="RF-37">
      <form className="content-grid">
        <Input
          label="Repositorio"
          placeholder="https://github.com/org/repositorio"
          value={url}
          onChange={(event) => setUrl(event.target.value)}
          helperText="Validacao em tempo real sera aplicada na API."
        />
        <label className="input-field">
          <span className="input-field__label">Provedor</span>
          <select
            className="input-field__select"
            value={provider}
            onChange={(event) => setProvider(event.target.value)}
          >
            <option>GitHub</option>
            <option>Azure DevOps</option>
          </select>
        </label>
        <TextArea label="Token (opcional)" placeholder="********" />
        <div>
          <p className="card__meta">Pilares de analise</p>
          <div className="content-grid">
            <Badge tone="success">Obsolescencia</Badge>
            <Badge tone="danger">Seguranca</Badge>
            <Badge tone="warning">Observabilidade</Badge>
            <Badge tone="info">Documentacao</Badge>
          </div>
        </div>
        <Button type="button">Iniciar Analise</Button>
      </form>
    </Card>
  );
}
