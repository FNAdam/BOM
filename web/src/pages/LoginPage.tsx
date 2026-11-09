import { type FormEvent, useState } from 'react';
import { api } from '../api/client';
import { setSession, type LoginResponse } from '../api/auth';
import logo from '../assets/logo.png';

export function LoginPage({ onLoggedIn }: { onLoggedIn: () => void }) {
  const [username, setUsername] = useState('');
  const [password, setPassword] = useState('');
  const [error, setError] = useState<string | null>(null);
  const [loading, setLoading] = useState(false);

  async function handleSubmit(e: FormEvent) {
    e.preventDefault();
    setError(null);
    setLoading(true);
    try {
      const session = await api.post<LoginResponse>('/api/auth/login', { username, password });
      setSession(session);
      onLoggedIn();
    } catch {
      setError('Username atau password salah.');
    } finally {
      setLoading(false);
    }
  }

  return (
    <div className="login-shell">
      <form className="login-card" onSubmit={handleSubmit}>
        <div className="login-logo">
          <span className="logo-badge">
            <img src={logo} alt="Ramuan BunSa" />
          </span>
          <p className="page-desc" style={{ marginTop: 10 }}>Masuk untuk membuka Sistem Resep &amp; Biaya</p>
        </div>

        <label>
          Username
          <input
            value={username}
            onChange={(e) => setUsername(e.target.value)}
            autoFocus
            required
          />
        </label>

        <label>
          Password
          <input
            type="password"
            value={password}
            onChange={(e) => setPassword(e.target.value)}
            required
          />
        </label>

        {error && <p className="login-error">{error}</p>}

        <button type="submit" className="btn btn-primary" disabled={loading} style={{ width: '100%' }}>
          {loading ? 'Masuk...' : 'Masuk'}
        </button>
      </form>
    </div>
  );
}
