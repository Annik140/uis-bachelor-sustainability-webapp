import { useEffect, useState } from 'react'
import Header from './components/Header'
import Footer from './components/Footer'
import AdminLogin from './components/AdminLogin'
import AdminDashboard from './components/AdminDashboard'
import AdminBrandForm from './components/AdminBrandForm'
import './App.css'

type Brand = {
  id: number
  brandName: string
  category?: string
  prosSummary?: string
  consSummary?: string
  sustainabilityScore?: number
  transparencyScore?: number
  materialSustainabilityScore?: number
  laborPracticesScore?: number
  carbonFootprintScore?: number
  productLongevityScore?: number
  evidenceSources?: EvidenceSource[]
  criteriaItems?: CriterionItem[]
}

type EvidenceSource = {
  id: number
  sourceTitle: string
  sourceUrl: string
  sourceType?: string
  publishedAtUtc?: string
  notes?: string
}

type CriterionItem = {
  id: number
  category: string
  name: string
  numericValue?: number
  unit?: string
  weight: number
  notes?: string
}

function getSustainabilityColor(score?: number) {
  if (score === undefined || score === null) return '#6b7280'
  if (score >= 7) return '#3d9f66'
  if (score >= 4) return '#b1843b'
  return '#c35b5b'
}

function App() {
  const path = window.location.pathname;
  const [brands, setBrands] = useState<Brand[]>([])
  const editMatch = path.match(/^\/admin\/brands\/(\d+)\/edit$/)

  useEffect(() => {
    if (!path.startsWith('/admin')) {
      fetch('/brands')
        .then(response => response.ok ? response.json() : [])
        .then(setBrands)
        .catch(() => setBrands([]))
    }
  }, [path])

  if (path.startsWith('/admin')) {
    if (path === '/admin' || path === '/admin/login') return <AdminLogin />
    if (path === '/admin/dashboard') return <AdminDashboard />
    if (path === '/admin/brands/new') return <AdminBrandForm mode="create" />
    if (editMatch) return <AdminBrandForm mode="edit" brandId={Number(editMatch[1])} />
    return <AdminDashboard />
  }

  return (
    <>
      <Header />
      
      <main className="app-main">
        <section className="brands-container">
          {brands.length === 0 ? (
            <div className="brands-placeholder">
              <p>No brands added yet.</p>
            </div>
          ) : (
            <div className="brands-grid">
              {brands.map(brand => (
                <article key={brand.id} className="brand-card">
                  <div className="brand-card-top">
                    <h3 className="brand-title">{brand.brandName}</h3>
                  </div>

                  <div className="brand-card-body">
                    <p className="brand-description-preview">
                      {brand.category ? `${brand.category} profile.` : 'Description coming soon.'}
                    </p>

                    <div className="scores-row" aria-label="Brand scores">
                      <div className="score-chip">
                        <p className="score-chip-label">Sustainability</p>
                        <p className="score-chip-value" style={{ color: getSustainabilityColor(brand.sustainabilityScore) }}>
                          {brand.sustainabilityScore?.toFixed(1) ?? 'n/a'} / 10
                        </p>
                      </div>

                      <div className="score-chip">
                        <p className="score-chip-label">Transparency</p>
                        <p className="score-chip-value score-chip-value-neutral">
                          {brand.transparencyScore?.toFixed(1) ?? 'n/a'} / 5
                        </p>
                      </div>
                    </div>
                  </div>
                </article>
              ))}
            </div>
          )}
        </section>
      </main>

      <Footer />
    </>
  )
}

export default App
