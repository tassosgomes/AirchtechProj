import { useState } from 'react';
import { useNavigate } from 'react-router-dom';
import { Button } from '../components/Button';
import { Card } from '../components/Card';
import { Input } from '../components/Input';
import { useAuth } from '../hooks/useAuth';

export function LoginPage() {
  const [email, setEmail] = useState('');
  const [password, setPassword] = useState('');
  const { login } = useAuth();
  const navigate = useNavigate();

  const handleSubmit = (event: React.FormEvent<HTMLFormElement>) => {
    event.preventDefault();
    login('demo-token');
    navigate('/dashboard');
  };

  return (
    <div className="login-shell">
      <Card className="login-card">
        <div>
          <p className="card__meta">Modernization Platform</p>
          <h2 className="login-card__title">Acesso Controlado</h2>
        </div>
        <form onSubmit={handleSubmit} className="login-card">
          <Input
            label="Email"
            type="email"
            placeholder="arquiteto@empresa.com"
            value={email}
            onChange={(event) => setEmail(event.target.value)}
            required
          />
          <Input
            label="Senha"
            type="password"
            placeholder="••••••••"
            value={password}
            onChange={(event) => setPassword(event.target.value)}
            required
          />
          <Button type="submit">Entrar</Button>
        </form>
        <p className="card__meta">Use credenciais internas para acessar o console.</p>
      </Card>
    </div>
  );
}
