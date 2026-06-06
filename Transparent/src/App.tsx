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
                  <h3>{brand.brandName}</h3>
                  <p>{brand.category}</p>
                  <p>Sustainability: {brand.sustainabilityScore?.toFixed(1) ?? 'n/a'} / 10</p>
                  <p>Transparency: {brand.transparencyScore?.toFixed(1) ?? 'n/a'} / 5</p>
                  <small>
                    Material {brand.materialSustainabilityScore?.toFixed(1) ?? 'n/a'} | Labor {brand.laborPracticesScore?.toFixed(1) ?? 'n/a'} | Carbon {brand.carbonFootprintScore?.toFixed(1) ?? 'n/a'} | Longevity {brand.productLongevityScore?.toFixed(1) ?? 'n/a'}
                  </small>
                  <div className="brand-reasoning">
                    <div>
                      <h4>Pros</h4>
                      {brand.prosSummary ? (
                        <ul>
                          {brand.prosSummary.split('\n').filter(Boolean).map((item, index) => <li key={index}>{item}</li>)}
                        </ul>
                      ) : (
                        <p>No positive reasoning added yet.</p>
                      )}
                    </div>
                    <div>
                      <h4>Cons</h4>
                      {brand.consSummary ? (
                        <ul>
                          {brand.consSummary.split('\n').filter(Boolean).map((item, index) => <li key={index}>{item}</li>)}
                        </ul>
                      ) : (
                        <p>No negative reasoning added yet.</p>
                      )}
                    </div>
                  </div>
                  {brand.evidenceSources && brand.evidenceSources.length > 0 && (
                    <ul>
                      {brand.evidenceSources.map(source => (
                        <li key={source.id}>
                          <a href={source.sourceUrl} target="_blank" rel="noreferrer">{source.sourceTitle}</a>
                          {source.sourceType ? ` (${source.sourceType})` : ''}
                        </li>
                      ))}
                    </ul>
                  )}
                  {brand.criteriaItems && brand.criteriaItems.length > 0 && (
                    <ul>
                      {brand.criteriaItems.map(item => (
                        <li key={item.id}>
                          {item.category}: {item.name} — {item.numericValue?.toFixed(1) ?? 'n/a'} {item.unit ?? ''}
                        </li>
                      ))}
                    </ul>
                  )}
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
