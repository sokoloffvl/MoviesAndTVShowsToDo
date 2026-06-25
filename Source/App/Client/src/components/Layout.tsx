import { Link, NavLink, Outlet } from 'react-router-dom';
import './Layout.css';

export function Layout() {
  return (
    <div className="layout">
      <header className="header">
        <Link to="/" className="brand">
          <span className="brand-icon">🎬</span>
          <span>Movies To-Do</span>
        </Link>
        <nav className="nav">
          <NavLink to="/" end>
            Watchlist
          </NavLink>
          <NavLink to="/add">Add</NavLink>
          <NavLink to="/history">History</NavLink>
        </nav>
      </header>
      <main className="main">
        <Outlet />
      </main>
    </div>
  );
}
