import { useEffect, useState } from 'react'

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
  const [brands, setBrands] = useState<Brand[]>([])
  const [loading, setLoading] = useState(true)
  const [error, setError] = useState<string | null>(null)

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
    const response = await fetch(`/admin/clothingbrands/${id}`, { method: 'DELETE', credentials: 'include' })
    if (response.status === 401) {
      window.location.href = '/admin/login'
      return
    }
    fetchBrands()
  }

  async function handleLogout() {
    const response = await fetch('/admin/logout', { method: 'POST', credentials: 'include' })
    if (response.status === 401 || response.ok) {
      window.location.href = '/admin/login'
    }
  }

  return (
    <div style={{ padding: 24 }}>
      <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', marginBottom: 24 }}>
        <div>
          <h2>Admin Dashboard</h2>
          <p>Manage your clothing brands, criteria, and score reasoning from here.</p>
        </div>
        <div style={{ display: 'flex', gap: 10 }}>
          <button type="button" onClick={handleLogout} style={{ padding: '12px 18px' }}>
            Logout
          </button>
          <button type="button" onClick={handleBackToMainPage} style={{ padding: '12px 18px' }}>
            Back to main page
          </button>
          <button type="button" onClick={handleAddBrand} style={{ padding: '12px 18px', fontWeight: 700 }}>
            Add new brand
          </button>
        </div>
      </div>

      <section>
        <h3>Created brands</h3>
        {loading && <p>Loading brands...</p>}
        {error && <p style={{ color: '#b00020' }}>{error}</p>}
        {!loading && !error && brands.length === 0 ? (
          <p>No brands created yet.</p>
        ) : !loading && !error ? (
          <div style={{ display: 'grid', gap: 16 }}>
            {brands.map(brand => (
              <article key={brand.id} style={{ border: '1px solid #ddd', borderRadius: 10, padding: 16, background: '#fff' }}>
                <h4>{brand.brandName}</h4>
                <p>{brand.category}</p>
                <p>Sustainability: {brand.sustainabilityScore?.toFixed(1) ?? 'n/a'} / 100</p>
                <p>Transparency: {brand.transparencyScore?.toFixed(1) ?? 'n/a'} / 5</p>
                <div style={{ display: 'flex', gap: 8, marginTop: 12 }}>
                  <button type="button" onClick={() => handleEditBrand(brand.id)}>Edit</button>
                  <button type="button" onClick={() => handleDelete(brand.id)}>Delete</button>
                </div>
              </article>
            ))}
          </div>
        ) : null
        }
      </section>
    </div>
  )
}