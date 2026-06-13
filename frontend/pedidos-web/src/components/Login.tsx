import { useState } from 'react'
import { useAuthStore } from '../store/authStore'

export function Login() {
  const { login, loading, error } = useAuthStore()
  const [email, setEmail] = useState('admin@pedidos.local')
  const [password, setPassword] = useState('admin123')

  const submit = async (e: React.FormEvent) => {
    e.preventDefault()
    try {
      await login(email, password)
    } catch {
      /* erro já tratado no store */
    }
  }

  return (
    <div className="login-screen">
      <form className="login-card" onSubmit={submit}>
        <div className="login-brand">
          <span className="brand-mark">Pedidos</span>
          <span className="brand-sub">Painel de gestão</span>
        </div>

        <label className="field">
          <span>E-mail</span>
          <input type="email" value={email} onChange={(e) => setEmail(e.target.value)} autoFocus />
        </label>

        <label className="field">
          <span>Senha</span>
          <input type="password" value={password} onChange={(e) => setPassword(e.target.value)} />
        </label>

        {error && <p className="error">{error}</p>}

        <button className="primary block" type="submit" disabled={loading}>
          {loading ? 'Entrando...' : 'Entrar'}
        </button>

        <p className="login-hint">
          Acesso de demonstração: <strong>admin@pedidos.local</strong> / <strong>admin123</strong>
        </p>
      </form>
    </div>
  )
}
