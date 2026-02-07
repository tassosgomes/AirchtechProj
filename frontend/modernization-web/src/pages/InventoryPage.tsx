import { useCallback, useEffect, useMemo, useState } from 'react';
import { useNavigate } from 'react-router-dom';
import { Search } from 'lucide-react';
import { Badge, Button, Card, Input, Spinner } from '../components';
import { listInventoryRepositories } from '../services/inventoryApi';
import { normalizeSeverity } from '../types/analysis';
import type { InventoryPagination, InventoryRepositorySummary } from '../types/inventory';

const SEVERITY_ORDER = ['Critical', 'High', 'Medium', 'Low', 'Informative'];
const PAGE_SIZE = 6;

const DEFAULT_PAGINATION: InventoryPagination = {
  page: 1,
  size: PAGE_SIZE,
  total: 0,
  totalPages: 1,
};

function formatDate(value?: string | null) {
  if (!value) {
    return '-';
  }

  return new Date(value).toLocaleDateString('en-GB', {
    dateStyle: 'medium',
  });
}

function getPageNumbers(current: number, total: number) {
  const pages = new Set<number>();
  pages.add(1);
  pages.add(total);

  for (let page = current - 1; page <= current + 1; page += 1) {
    if (page > 1 && page < total) {
      pages.add(page);
    }
  }

  return Array.from(pages).sort((a, b) => a - b);
}

export function InventoryPage() {
  const [repositories, setRepositories] = useState<InventoryRepositorySummary[]>([]);
  const [pagination, setPagination] = useState<InventoryPagination>(DEFAULT_PAGINATION);
  const [isLoading, setIsLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [page, setPage] = useState(1);
  const [search, setSearch] = useState('');
  const [technology, setTechnology] = useState('all');
  const [dependency, setDependency] = useState('');
  const [severities, setSeverities] = useState<string[]>([]);
  const [dateFrom, setDateFrom] = useState('');
  const [dateTo, setDateTo] = useState('');
  const navigate = useNavigate();

  const appliedTechnologies = useMemo(() => {
    const options = new Set<string>();
    repositories.forEach((repository) => {
      repository.technologies.forEach((entry) => options.add(entry));
    });
    return Array.from(options).sort();
  }, [repositories]);

  useEffect(() => {
    setPage(1);
  }, [search, technology, dependency, severities.join('|'), dateFrom, dateTo]);

  const loadInventory = useCallback(async () => {
    setIsLoading(true);
    setError(null);

    try {
      const response = await listInventoryRepositories({
        page,
        size: PAGE_SIZE,
        search,
        technology: technology === 'all' ? undefined : technology,
        dependency: dependency.trim() || undefined,
        severities,
        dateFrom: dateFrom || undefined,
        dateTo: dateTo || undefined,
      });
      setRepositories(response.data);
      setPagination(response.pagination);
    } catch (requestError) {
      setError('Nao foi possivel carregar o inventario.');
    } finally {
      setIsLoading(false);
    }
  }, [page, search, technology, dependency, severities, dateFrom, dateTo]);

  useEffect(() => {
    loadInventory();
  }, [loadInventory]);

  const totalPages = Math.max(1, pagination.totalPages || 1);
  const pageNumbers = useMemo(() => getPageNumbers(page, totalPages), [page, totalPages]);

  const toggleSeverity = (severity: string) => {
    setSeverities((current) =>
      current.includes(severity)
        ? current.filter((item) => item !== severity)
        : [...current, severity],
    );
  };

  return (
    <>
      <div className="dashboard-header">
        <div>
          <h2 className="page-title">Inventario de Software</h2>
          <span className="page-subtitle">
            Visao consolidada de repositorios, stacks e achados
          </span>
        </div>
      </div>

      <Card title="Filtros e Busca" meta="RF-40">
        <div className="filters">
          <div className="filters__row">
            <Input
              label="Buscar"
              placeholder="github.com/org/repositorio"
              value={search}
              onChange={(event) => setSearch(event.target.value)}
              leadingIcon={<Search size={14} />}
            />
            <label className="input-field">
              <span className="input-field__label">Tecnologia</span>
              <select
                className="input-field__select"
                value={technology}
                onChange={(event) => setTechnology(event.target.value)}
              >
                <option value="all">Todas</option>
                {appliedTechnologies.map((option) => (
                  <option key={option} value={option}>
                    {option}
                  </option>
                ))}
              </select>
            </label>
            <label className="input-field">
              <span className="input-field__label">Data inicial</span>
              <input
                type="date"
                className="input-field__control"
                value={dateFrom}
                onChange={(event) => setDateFrom(event.target.value)}
              />
            </label>
            <Input
              label="Dependencia"
              placeholder="ex: Newtonsoft.Json"
              value={dependency}
              onChange={(event) => setDependency(event.target.value)}
            />
            <label className="input-field">
              <span className="input-field__label">Data final</span>
              <input
                type="date"
                className="input-field__control"
                value={dateTo}
                onChange={(event) => setDateTo(event.target.value)}
              />
            </label>
          </div>
          <div className="filters__chips">
            {SEVERITY_ORDER.map((severity) => {
              const normalized = normalizeSeverity(severity);
              const active = severities.includes(severity);
              return (
                <button
                  key={severity}
                  type="button"
                  className={[
                    'filter-chip',
                    `filter-chip--${normalized}`,
                    active ? 'filter-chip--active' : '',
                  ]
                    .filter(Boolean)
                    .join(' ')}
                  onClick={() => toggleSeverity(severity)}
                >
                  {severity}
                </button>
              );
            })}
          </div>
        </div>
      </Card>

      <Card title="Repositorios analisados" meta={`Pagina ${page} de ${totalPages}`}>
        {isLoading && (
          <div className="request-empty">
            <Spinner className="spinner--inline" />
            Carregando inventario...
          </div>
        )}
        {!isLoading && error && <p className="form-error">{error}</p>}
        {!isLoading && !error && repositories.length === 0 && (
          <div className="request-empty">Nenhum repositorio encontrado.</div>
        )}
        {!isLoading && !error && repositories.length > 0 && (
          <div className="inventory-grid">
            {repositories.map((repository) => (
              <div key={repository.id} className="inventory-card">
                <div className="inventory-card__header">
                  <div>
                    <p className="inventory-card__repo">{repository.url}</p>
                    <div className="inventory-card__meta">
                      {repository.provider && (
                        <Badge tone="info">{repository.provider}</Badge>
                      )}
                      <span>Ultima analise: {formatDate(repository.lastAnalysisAt)}</span>
                    </div>
                  </div>
                  <Button
                    type="button"
                    variant="secondary"
                    onClick={() => navigate(`/inventory/${repository.id}/timeline`)}
                  >
                    Ver timeline
                  </Button>
                </div>
                <div className="inventory-card__stack">
                  {repository.technologies.length === 0 && (
                    <span className="card__meta">Stack nao informada</span>
                  )}
                  {repository.technologies.map((tech) => (
                    <Badge key={tech} tone="neutral">
                      {tech}
                    </Badge>
                  ))}
                </div>
                <div className="inventory-card__severity">
                  {SEVERITY_ORDER.map((severity) => (
                    <div key={severity} className="severity-count">
                      <Badge className={`badge--severity-${normalizeSeverity(severity)}`}>
                        {severity}
                      </Badge>
                      <span>{repository.findingsBySeverity[severity] ?? 0}</span>
                    </div>
                  ))}
                </div>
              </div>
            ))}
          </div>
        )}

        <div className="pagination">
          <Button
            variant="ghost"
            type="button"
            onClick={() => setPage((current) => Math.max(1, current - 1))}
            disabled={page <= 1}
          >
            Anterior
          </Button>
          <div className="pagination__pages">
            {pageNumbers.map((pageNumber) => (
              <button
                key={pageNumber}
                type="button"
                className={['pagination__page', pageNumber === page ? 'is-active' : '']
                  .filter(Boolean)
                  .join(' ')}
                onClick={() => setPage(pageNumber)}
              >
                {pageNumber}
              </button>
            ))}
          </div>
          <Button
            variant="ghost"
            type="button"
            onClick={() => setPage((current) => Math.min(totalPages, current + 1))}
            disabled={page >= totalPages}
          >
            Proxima
          </Button>
        </div>
      </Card>
    </>
  );
}
