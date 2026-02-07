import { Activity, Database, LogOut, Radar, Terminal } from 'lucide-react';
import { NavLink, Outlet, useLocation, useNavigate } from 'react-router-dom';
import { Button } from '../components/Button';
import { useAuth } from '../hooks/useAuth';

const navItems = [
  { to: '/dashboard', label: 'Dashboard', icon: <Activity size={18} /> },
  { to: '/requests/new', label: 'Nova Solicitação', icon: <Radar size={18} /> },
  { to: '/inventory', label: 'Inventário', icon: <Database size={18} /> },
];

export function AppLayout() {
  const { logout } = useAuth();
  const location = useLocation();
  const navigate = useNavigate();

  const handleLogout = () => {
    logout();
    navigate('/login');
  };

  return (
    <div className="app-shell">
      <aside className="sidebar">
        <div className="sidebar__brand">
          <Terminal size={18} />
          Modernization <span>Hub</span>
        </div>
        <nav className="sidebar__nav">
          {navItems.map((item) => (
            <NavLink
              key={item.to}
              to={item.to}
              className={({ isActive }) => (isActive ? 'nav-link active' : 'nav-link')}
            >
              {item.icon}
              {item.label}
            </NavLink>
          ))}
        </nav>
        <div className="sidebar__footer">{location.pathname}</div>
      </aside>
      <main className="main">
        <header className="header">
          <div className="header__title">
            <h1>Cyber Operations Console</h1>
            <span>status: LIVE | env: local</span>
          </div>
          <div className="header__actions">
            <Button variant="ghost" onClick={handleLogout}>
              <LogOut size={16} /> Sair
            </Button>
          </div>
        </header>
        <section className="content">
          <Outlet />
        </section>
      </main>
    </div>
  );
}
