import { useState } from 'react';
import { Link, useNavigate } from 'react-router-dom';
import { Eye, EyeOff, Lock, Mail } from 'lucide-react';
import { Button } from '../components/Button';
import { Card } from '../components/Card';
import { Input } from '../components/Input';
import { registerUser } from '../services/authApi';

export function RegisterPage() {
  const [email, setEmail] = useState('');
  const [password, setPassword] = useState('');
  const [confirmPassword, setConfirmPassword] = useState('');
  const [showPassword, setShowPassword] = useState(false);
  const [showConfirmPassword, setShowConfirmPassword] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [isSubmitting, setIsSubmitting] = useState(false);
  const navigate = useNavigate();

  const handleSubmit = async (event: React.FormEvent<HTMLFormElement>) => {
    event.preventDefault();
    setError(null);

    if (password !== confirmPassword) {
      setError('As senhas nao conferem.');
      return;
    }

    setIsSubmitting(true);

    try {
      await registerUser({ email, password });
      navigate('/login');
    } catch (requestError) {
      setError('Nao foi possivel criar a conta. Tente novamente.');
    } finally {
      setIsSubmitting(false);
    }
  };

  return (
    <div className="login-shell">
      <Card className="login-card">
        <div>
          <p className="card__meta">Modernization Platform</p>
          <h2 className="login-card__title">Criar Credencial</h2>
        </div>
        <form onSubmit={handleSubmit} className="login-card">
          <Input
            label="Email"
            type="email"
            placeholder="arquiteto@empresa.com"
            value={email}
            onChange={(event) => setEmail(event.target.value)}
            required
            leadingIcon={<Mail size={16} />}
          />
          <Input
            label="Senha"
            type={showPassword ? 'text' : 'password'}
            placeholder="Minimo 8 caracteres"
            value={password}
            onChange={(event) => setPassword(event.target.value)}
            required
            leadingIcon={<Lock size={16} />}
            trailingIcon={
              <button
                type="button"
                className="icon-button"
                onClick={() => setShowPassword((state) => !state)}
                aria-label={showPassword ? 'Ocultar senha' : 'Mostrar senha'}
              >
                {showPassword ? <EyeOff size={16} /> : <Eye size={16} />}
              </button>
            }
          />
          <Input
            label="Confirmar senha"
            type={showConfirmPassword ? 'text' : 'password'}
            placeholder="Repita a senha"
            value={confirmPassword}
            onChange={(event) => setConfirmPassword(event.target.value)}
            required
            leadingIcon={<Lock size={16} />}
            trailingIcon={
              <button
                type="button"
                className="icon-button"
                onClick={() => setShowConfirmPassword((state) => !state)}
                aria-label={showConfirmPassword ? 'Ocultar senha' : 'Mostrar senha'}
              >
                {showConfirmPassword ? <EyeOff size={16} /> : <Eye size={16} />}
              </button>
            }
          />
          {error && <p className="form-error">{error}</p>}
          <Button type="submit" disabled={isSubmitting}>
            {isSubmitting && <span className="spinner button__spinner" aria-hidden="true" />}
            Criar conta
          </Button>
          <div className="login-links">
            <span>Ja possui acesso?</span>
            <Link to="/login">Voltar para login</Link>
          </div>
        </form>
        <p className="card__meta">Solicite liberacao se precisar de acesso especial.</p>
      </Card>
    </div>
  );
}
