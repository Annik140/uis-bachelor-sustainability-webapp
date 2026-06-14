import { useState, type SyntheticEvent } from 'react'
import './AdminLogin.css'

export default function AdminLogin() {
  const [username, setUsername] = useState('')
  const [password, setPassword] = useState('')
  const [error, setError] = useState<string | null>(null)

  async function handleSubmit(e: SyntheticEvent<HTMLFormElement, SubmitEvent>) {
    e.preventDefault()
    setError(null)
    try {
      const res = await fetch('/admin/login', {
        method: 'POST',
        credentials: 'include',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({ username, password })
      })
      if (res.ok) {
        window.location.href = '/admin/dashboard'
      } else {
        setError('Invalid credentials')
      }
    } catch {
      setError('Network error')
    }
  }

  return (
    <main className="admin-login-page">
      <section className="admin-login-card" aria-label="Admin login form">
        <p className="admin-login-kicker">Transparent Fashion</p>
        <h1 className="admin-login-title">Admin Login</h1>
        <p className="admin-login-subtitle">Sign in to manage brand profiles, criteria, and source updates.</p>

        <form className="admin-login-form" onSubmit={handleSubmit}>
          <div className="admin-field">
            <label htmlFor="admin-username">Username</label>
            <input
              id="admin-username"
              value={username}
              onChange={e => setUsername(e.target.value)}
              autoComplete="username"
            />
          </div>

          <div className="admin-field">
            <label htmlFor="admin-password">Password</label>
            <input
              id="admin-password"
              type="password"
              value={password}
              onChange={e => setPassword(e.target.value)}
              autoComplete="current-password"
            />
          </div>

          <button type="submit" className="admin-login-button">Sign in</button>
        </form>

        {error && <p className="admin-login-error">{error}</p>}

        <a href="/" className="admin-login-backlink">Back to Transparent Fashion</a>
      </section>
    </main>
  )
}
