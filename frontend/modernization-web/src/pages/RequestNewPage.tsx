import { useEffect, useMemo, useState, type ReactNode } from 'react';
import { useNavigate } from 'react-router-dom';
import { CheckCircle2, FileText, RefreshCw, ShieldCheck, XCircle } from 'lucide-react';
import { Button, Card, Input } from '../components';
import {
  createAnalysisRequest,
  type AnalysisType,
  type SourceProvider,
} from '../services/analysisRequestsApi';

const analysisOptions: Array<{
  value: AnalysisType;
  label: string;
  description: string;
  icon: ReactNode;
}> = [
  {
    value: 'Obsolescence',
    label: 'Obsolescencia',
    description: 'Stack e dependencias',
    icon: <RefreshCw size={18} />,
  },
  {
    value: 'Security',
    label: 'Seguranca',
    description: 'Superficie de risco',
    icon: <ShieldCheck size={18} />,
  },
  {
    value: 'Observability',
    label: 'Observabilidade',
    description: 'Metricas e logs',
    icon: <CheckCircle2 size={18} />,
  },
  {
    value: 'Documentation',
    label: 'Documentacao',
    description: 'Cobertura tecnica',
    icon: <FileText size={18} />,
  },
];

const providerOptions: Array<{ value: SourceProvider; label: string }> = [
  { value: 'GitHub', label: 'GitHub' },
  { value: 'AzureDevOps', label: 'Azure DevOps' },
];

function isValidRepositoryUrl(value: string) {
  try {
    const parsed = new URL(value);
    return parsed.protocol === 'http:' || parsed.protocol === 'https:';
  } catch {
    return false;
  }
}

export function RequestNewPage() {
  const [url, setUrl] = useState('');
  const [provider, setProvider] = useState<SourceProvider>('GitHub');
  const [accessToken, setAccessToken] = useState('');
  const [selectedTypes, setSelectedTypes] = useState<AnalysisType[]>(
    analysisOptions.map((option) => option.value),
  );
  const [urlStatus, setUrlStatus] = useState<'idle' | 'valid' | 'invalid'>('idle');
  const [hasSubmitted, setHasSubmitted] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [isSubmitting, setIsSubmitting] = useState(false);
  const navigate = useNavigate();

  useEffect(() => {
    if (!url.trim()) {
      setUrlStatus('idle');
      return;
    }

    const handler = window.setTimeout(() => {
      setUrlStatus(isValidRepositoryUrl(url) ? 'valid' : 'invalid');
    }, 300);

    return () => window.clearTimeout(handler);
  }, [url]);

  const urlHelperText = useMemo(() => {
    if (urlStatus === 'valid') {
      return 'URL valida detectada.';
    }

    if (urlStatus === 'invalid') {
      return 'URL invalida. Use http/https.';
    }

    return 'Validacao em tempo real sera aplicada.';
  }, [urlStatus]);

  const urlTrailingIcon = useMemo(() => {
    if (urlStatus === 'valid') {
      return <CheckCircle2 size={16} className="input-field__icon--valid" />;
    }

    if (urlStatus === 'invalid') {
      return <XCircle size={16} className="input-field__icon--invalid" />;
    }

    return undefined;
  }, [urlStatus]);

  const canSubmit = useMemo(() => {
    return urlStatus === 'valid' && selectedTypes.length > 0 && !isSubmitting;
  }, [urlStatus, selectedTypes.length, isSubmitting]);

  const toggleType = (value: AnalysisType) => {
    setSelectedTypes((current) =>
      current.includes(value)
        ? current.filter((type) => type !== value)
        : [...current, value],
    );
  };

  const handleSubmit = async (event: React.FormEvent<HTMLFormElement>) => {
    event.preventDefault();
    setHasSubmitted(true);
    setError(null);

    if (!canSubmit) {
      return;
    }

    setIsSubmitting(true);

    try {
      await createAnalysisRequest({
        repositoryUrl: url.trim(),
        provider,
        accessToken: accessToken.trim() || undefined,
        selectedTypes,
      });
      navigate('/dashboard');
    } catch (requestError) {
      setError('Nao foi possivel criar a solicitacao. Tente novamente.');
    } finally {
      setIsSubmitting(false);
    }
  };

  return (
    <Card title="Nova Solicitacao" meta="RF-37">
      <form className="content-grid" onSubmit={handleSubmit}>
        <Input
          label="Repositorio"
          placeholder="https://github.com/org/repositorio"
          value={url}
          onChange={(event) => setUrl(event.target.value)}
          helperText={urlHelperText}
          trailingIcon={urlTrailingIcon}
          required
        />
        <label className="input-field">
          <span className="input-field__label">Provedor</span>
          <select
            className="input-field__select"
            value={provider}
            onChange={(event) => setProvider(event.target.value as SourceProvider)}
          >
            {providerOptions.map((option) => (
              <option key={option.value} value={option.value}>
                {option.label}
              </option>
            ))}
          </select>
        </label>
        <Input
          label="Token (opcional)"
          type="password"
          placeholder="********"
          value={accessToken}
          onChange={(event) => setAccessToken(event.target.value)}
          helperText="Token nao sera persistido no backend."
        />
        <div>
          <p className="card__meta">Pilares de analise</p>
          <div className="analysis-grid">
            {analysisOptions.map((option) => {
              const isSelected = selectedTypes.includes(option.value);
              return (
                <label
                  key={option.value}
                  className={[
                    'analysis-option',
                    isSelected ? 'analysis-option--selected' : '',
                  ]
                    .filter(Boolean)
                    .join(' ')}
                >
                  <input
                    type="checkbox"
                    checked={isSelected}
                    onChange={() => toggleType(option.value)}
                  />
                  {option.icon}
                  <div className="analysis-option__label">
                    <strong>{option.label}</strong>
                    <span>{option.description}</span>
                  </div>
                </label>
              );
            })}
          </div>
          {hasSubmitted && selectedTypes.length === 0 && (
            <p className="form-error">Selecione ao menos um tipo de analise.</p>
          )}
        </div>
        {error && <p className="form-error">{error}</p>}
        <Button type="submit" disabled={!canSubmit}>
          {isSubmitting && <span className="spinner button__spinner" aria-hidden="true" />}
          Iniciar Analise
        </Button>
      </form>
    </Card>
  );
}
