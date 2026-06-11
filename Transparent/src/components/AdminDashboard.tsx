import { useCallback, useEffect, useState } from 'react'
import './AdminDashboard.css'
import { clearCsrfToken, withCsrfHeaders } from '../utils/csrf'

type Brand = {
  id: number
  brandName: string
  sustainabilityScore?: number
  transparencyScore?: number
  prosSummary?: string
  consSummary?: string
}

type PagedResponse<T> = {
  items: T[]
  page: number
  pageSize: number
  totalCount: number
  totalPages: number
}

export default function AdminDashboard() {
  const BRANDS_SCROLL_THRESHOLD = 8

  const [brands, setBrands] = useState<Brand[]>([])
  const [loading, setLoading] = useState(true)
  const [error, setError] = useState<string | null>(null)
  const [searchQuery, setSearchQuery] = useState('')
  const [page, setPage] = useState(1)
  const [totalPages, setTotalPages] = useState(1)
  const [totalCount, setTotalCount] = useState(0)
  const pageSize = 12
  const normalizedQuery = searchQuery.trim().toLowerCase()

  const fetchBrands = useCallback(async () => {
    setLoading(true)
    setError(null)

    try {
      const query = new URLSearchParams({
        page: String(page),
        pageSize: String(pageSize),
        sort: 'lastUpdatedDesc',
      })

      if (normalizedQuery) {
        query.set('q', normalizedQuery)
      }

      const res = await fetch(`/admin/clothingbrands?${query.toString()}`, { cache: 'no-store', credentials: 'include' })
      if (res.status === 401) {
        window.location.href = '/admin/login'
        return
      }

      if (res.ok) {
        const contentType = res.headers.get('content-type') ?? ''
        if (contentType.includes('application/json')) {
          const payload = await res.json() as PagedResponse<Brand> | Brand[]
          if (Array.isArray(payload)) {
            setBrands(payload)
            setTotalCount(payload.length)
            setTotalPages(1)
            setPage(1)
            return
          }

          setBrands(payload.items ?? [])
          setTotalCount(payload.totalCount ?? 0)
          setTotalPages(Math.max(1, payload.totalPages ?? 1))
          setPage(previous => {
            const nextPage = Math.max(1, payload.page ?? 1)
            return previous === nextPage ? previous : nextPage
          })
          return
        }
      }

      setError(`Could not load brands (${res.status}).`)
    } catch {
      setError('Could not load brands. Backend may be unavailable.')
      setBrands([])
      setTotalCount(0)
      setTotalPages(1)
    } finally {
      setLoading(false)
    }
  }, [normalizedQuery, page, pageSize])

  useEffect(() => {
    fetchBrands()
  }, [fetchBrands])

  function handleAddBrand() {
    window.location.href = '/admin/brands/new'
  }

  function handleBackToMainPage() {
    window.location.href = '/'
  }

  function handleEditBrand(id: number) {
    window.location.href = `/admin/brands/${id}/edit`
  }

  async function handleDelete(id: number, brandName: string) {
    const confirmed = window.confirm(`Delete brand "${brandName}"? This action cannot be undone.`)
    if (!confirmed) {
      return
    }

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

  const visibleBrands = brands

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
            <p className="admin-brands-count">{totalCount} total</p>
          </div>

          <div className="admin-add-brand-row">
            <button type="button" onClick={handleAddBrand} className="admin-btn admin-btn-primary">
              Add new brand
            </button>
          </div>

          <form className="admin-brand-search" onSubmit={e => e.preventDefault()}>
            <input
              type="text"
              placeholder="Search by brand..."
              value={searchQuery}
              onChange={e => {
                setSearchQuery(e.target.value)
                setPage(1)
              }}
              aria-label="Search created brands"
            />
          </form>

          {loading && <p className="admin-feedback">Loading brands...</p>}
          {error && <p className="admin-feedback admin-feedback-error">{error}</p>}

          {!loading && !error && totalCount === 0 ? (
            <p className="admin-feedback">No brands created yet.</p>
          ) : !loading && !error && visibleBrands.length === 0 ? (
            <p className="admin-feedback">No brands match your search.</p>
          ) : !loading && !error ? (
            <div className={`admin-brand-list ${visibleBrands.length > BRANDS_SCROLL_THRESHOLD ? 'admin-brand-list-scrollable' : ''}`}>
              {visibleBrands.map(brand => (
                <article key={brand.id} className="admin-brand-card">
                  <div className="admin-brand-main">
                    <h3>{brand.brandName}</h3>
                  </div>

                  <div className="admin-brand-scores">
                    <p>Sustainability: <strong>{brand.sustainabilityScore?.toFixed(1) ?? 'n/a'} / 100</strong></p>
                    <p>Transparency: <strong>{brand.transparencyScore?.toFixed(1) ?? 'n/a'} / 5</strong></p>
                  </div>

                  <div className="admin-brand-actions">
                    <button type="button" onClick={() => handleEditBrand(brand.id)} className="admin-btn admin-btn-ghost">Edit</button>
                    <button type="button" onClick={() => handleDelete(brand.id, brand.brandName)} className="admin-btn admin-btn-danger">Delete</button>
                  </div>
                </article>
              ))}
            </div>
          ) : null}

          {!loading && !error && totalPages > 1 && (
            <div className="admin-pagination" aria-label="Admin brand pagination">
              <button type="button" className="admin-btn admin-btn-ghost" onClick={() => setPage(current => Math.max(1, current - 1))} disabled={page <= 1}>
                Previous
              </button>
              <span>Page {page} of {totalPages}</span>
              <button type="button" className="admin-btn admin-btn-ghost" onClick={() => setPage(current => Math.min(totalPages, current + 1))} disabled={page >= totalPages}>
                Next
              </button>
            </div>
          )}
        </section>
      </section>
    </main>
  )
}