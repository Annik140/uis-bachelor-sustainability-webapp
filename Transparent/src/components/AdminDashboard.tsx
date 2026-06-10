import { useEffect, useState } from 'react'
import './AdminDashboard.css'
import { clearCsrfToken, withCsrfHeaders } from '../utils/csrf'

type Brand = {
  id: number
  brandName: string
  category?: string
  sustainabilityScore?: number
  transparencyScore?: number
  prosSummary?: string
  consSummary?: string
}

export default function AdminDashboard() {
  const BRANDS_SCROLL_THRESHOLD = 8

  const [brands, setBrands] = useState<Brand[]>([])
  const [loading, setLoading] = useState(true)
  const [error, setError] = useState<string | null>(null)
  const [searchQuery, setSearchQuery] = useState('')

  useEffect(() => {
    fetchBrands()
  }, [])

  async function fetchBrands() {
    setLoading(true)
    setError(null)

    try {
      const res = await fetch('/admin/clothingbrands', { cache: 'no-store', credentials: 'include' })
      if (res.status === 401) {
        window.location.href = '/admin/login'
        return
      }

      if (res.ok) {
        const contentType = res.headers.get('content-type') ?? ''
        if (contentType.includes('application/json')) {
          setBrands(await res.json())
          return
        }
      }

      // Fallback: if protected read endpoint fails unexpectedly, try public read endpoint.
      const fallback = await fetch('/brands', { cache: 'no-store' })
      if (fallback.ok) {
        setBrands(await fallback.json())
        return
      }

      setError(`Could not load brands (${res.status}).`)
    } catch {
      setError('Could not load brands. Backend may be unavailable.')
    } finally {
      setLoading(false)
    }
  }

  function handleAddBrand() {
    window.location.href = '/admin/brands/new'
  }

  function handleBackToMainPage() {
    window.location.href = '/'
  }

  function handleEditBrand(id: number) {
    window.location.href = `/admin/brands/${id}/edit`
  }

  async function handleDelete(id: number) {
    const headers = await withCsrfHeaders()
    const response = await fetch(`/admin/clothingbrands/${id}`, { method: 'DELETE', credentials: 'include', headers })
    if (response.status === 401) {
      clearCsrfToken()
      window.location.href = '/admin/login'
      return
    }

    if (response.status === 400) {
      setError('Could not delete brand: session security token expired. Please refresh and try again.')
      return
    }

    fetchBrands()
  }

  async function handleLogout() {
    const headers = await withCsrfHeaders()
    const response = await fetch('/admin/logout', { method: 'POST', credentials: 'include', headers })
    if (response.status === 401 || response.ok) {
      clearCsrfToken()
      window.location.href = '/admin/login'
      return
    }

    if (response.status === 400) {
      setError('Could not logout: session security token expired. Please refresh and try again.')
    }
  }

  const normalizedQuery = searchQuery.trim().toLowerCase()
  const visibleBrands = normalizedQuery
    ? brands.filter(brand =>
        brand.brandName.toLowerCase().includes(normalizedQuery) ||
        (brand.category ?? '').toLowerCase().includes(normalizedQuery)
      )
    : brands

  return (
    <main className="admin-dashboard-page">
      <section className="admin-dashboard-shell">
        <header className="admin-dashboard-header">
          <div>
            <p className="admin-dashboard-kicker">Transparent</p>
            <h1 className="admin-dashboard-title">Admin Dashboard</h1>
            <p className="admin-dashboard-subtitle">Manage brand entries, keep scoring data current, and publish consistent updates.</p>
          </div>

          <div className="admin-dashboard-controls">
            <div className="admin-dashboard-actions">
              <button type="button" onClick={handleBackToMainPage} className="admin-btn admin-btn-ghost">
                Back to main page
              </button>
              <button type="button" onClick={handleLogout} className="admin-btn admin-btn-logout">
                Logout
              </button>
            </div>
          </div>
        </header>

        <section className="admin-brands-section">
          <div className="admin-brands-top">
            <h2 className="admin-brands-heading">Created brands</h2>
            <p className="admin-brands-count">{brands.length} total</p>
          </div>

          <div className="admin-add-brand-row">
            <button type="button" onClick={handleAddBrand} className="admin-btn admin-btn-primary">
              Add new brand
            </button>
          </div>

          <form className="admin-brand-search" onSubmit={e => e.preventDefault()}>
            <input
              type="text"
              placeholder="Search by brand or category..."
              value={searchQuery}
              onChange={e => setSearchQuery(e.target.value)}
              aria-label="Search created brands"
            />
          </form>

          {loading && <p className="admin-feedback">Loading brands...</p>}
          {error && <p className="admin-feedback admin-feedback-error">{error}</p>}

          {!loading && !error && brands.length === 0 ? (
            <p className="admin-feedback">No brands created yet.</p>
          ) : !loading && !error && visibleBrands.length === 0 ? (
            <p className="admin-feedback">No brands match your search.</p>
          ) : !loading && !error ? (
            <div className={`admin-brand-list ${visibleBrands.length > BRANDS_SCROLL_THRESHOLD ? 'admin-brand-list-scrollable' : ''}`}>
              {visibleBrands.map(brand => (
                <article key={brand.id} className="admin-brand-card">
                  <div className="admin-brand-main">
                    <h3>{brand.brandName}</h3>
                    <p className="admin-brand-category">{brand.category ?? 'Uncategorized'}</p>
                  </div>

                  <div className="admin-brand-scores">
                    <p>Sustainability: <strong>{brand.sustainabilityScore?.toFixed(1) ?? 'n/a'} / 100</strong></p>
                    <p>Transparency: <strong>{brand.transparencyScore?.toFixed(1) ?? 'n/a'} / 5</strong></p>
                  </div>

                  <div className="admin-brand-actions">
                    <button type="button" onClick={() => handleEditBrand(brand.id)} className="admin-btn admin-btn-ghost">Edit</button>
                    <button type="button" onClick={() => handleDelete(brand.id)} className="admin-btn admin-btn-danger">Delete</button>
                  </div>
                </article>
              ))}
            </div>
          ) : null}
        </section>
      </section>
    </main>
  )
}