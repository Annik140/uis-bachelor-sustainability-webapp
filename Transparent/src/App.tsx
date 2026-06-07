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
  updatedAtUtc?: string
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

function toHundredScale(scoreOutOfTen?: number) {
  if (scoreOutOfTen === undefined || scoreOutOfTen === null) {
    return undefined
  }

  return Math.min(Math.max(scoreOutOfTen * 10, 0), 100)
}

function getSustainabilityColor(scoreOutOfTen?: number) {
  const score = toHundredScale(scoreOutOfTen)
  if (score === undefined || score === null) return '#6b7280'
  if (score >= 70) return '#3d9f66'
  if (score >= 40) return '#b1843b'
  return '#c35b5b'
}

function buildCategoryScores(brand: Brand) {
  return [
    { key: 'Material', label: 'Material', value: brand.materialSustainabilityScore },
    { key: 'Labor', label: 'Labor', value: brand.laborPracticesScore },
    { key: 'Carbon', label: 'Carbon', value: brand.carbonFootprintScore },
    { key: 'Longevity', label: 'Longevity', value: brand.productLongevityScore }
  ].filter(item => item.value !== undefined && item.value !== null)
}

function buildCriteriaByCategory(brand: Brand) {
  const groups = new Map<string, CriterionItem[]>()
  for (const item of brand.criteriaItems ?? []) {
    const bucket = groups.get(item.category) ?? []
    bucket.push(item)
    groups.set(item.category, bucket)
  }

  return Array.from(groups.entries())
}

function App() {
  const path = window.location.pathname
  const [brands, setBrands] = useState<Brand[]>([])
  const [searchQuery, setSearchQuery] = useState('')
  const [selectedBrand, setSelectedBrand] = useState<Brand | null>(null)
  const editMatch = path.match(/^\/admin\/brands\/(\d+)\/edit$/)

  const normalizedQuery = searchQuery.trim().toLowerCase()
  const filteredBrands = normalizedQuery
    ? brands.filter(brand =>
        brand.brandName.toLowerCase().includes(normalizedQuery) ||
        (brand.category ?? '').toLowerCase().includes(normalizedQuery)
      )
    : brands

  const sustainabilityValues = filteredBrands
    .map(brand => toHundredScale(brand.sustainabilityScore))
    .filter((value): value is number => value !== undefined)

  const totalCriteriaCount = filteredBrands.reduce(
    (sum, brand) => sum + (brand.criteriaItems?.length ?? 0),
    0
  )
  const filledCriteriaCount = filteredBrands.reduce(
    (sum, brand) => sum + (brand.criteriaItems?.filter(item => item.numericValue !== undefined && item.numericValue !== null).length ?? 0),
    0
  )

  const averageSustainability = sustainabilityValues.length > 0
    ? sustainabilityValues.reduce((sum, value) => sum + value, 0) / sustainabilityValues.length
    : undefined
  const dataCoverage = totalCriteriaCount > 0
    ? (filledCriteriaCount / totalCriteriaCount) * 100
    : undefined

  useEffect(() => {
    if (!path.startsWith('/admin')) {
      fetch('/brands')
        .then(response => response.ok ? response.json() : [])
        .then(setBrands)
        .catch(() => setBrands([]))
    }
  }, [path])

  useEffect(() => {
    if (!selectedBrand) {
      return
    }

    function handleEscape(event: KeyboardEvent) {
      if (event.key === 'Escape') {
        setSelectedBrand(null)
      }
    }

    const originalOverflow = document.body.style.overflow
    document.body.style.overflow = 'hidden'
    window.addEventListener('keydown', handleEscape)

    return () => {
      document.body.style.overflow = originalOverflow
      window.removeEventListener('keydown', handleEscape)
    }
  }, [selectedBrand])

  if (path.startsWith('/admin')) {
    if (path === '/admin' || path === '/admin/login') return <AdminLogin />
    if (path === '/admin/dashboard') return <AdminDashboard />
    if (path === '/admin/brands/new') return <AdminBrandForm mode="create" />
    if (editMatch) return <AdminBrandForm mode="edit" brandId={Number(editMatch[1])} />
    return <AdminDashboard />
  }

  return (
    <>
      <Header
        searchQuery={searchQuery}
        onSearchQueryChange={setSearchQuery}
        brandCount={filteredBrands.length}
        averageSustainability={averageSustainability}
        dataCoverage={dataCoverage}
      />
      
      <main className="app-main">
        <section className="brands-container">
          {filteredBrands.length === 0 ? (
            <div className="brands-placeholder">
              <p>{brands.length === 0 ? 'No brands added yet.' : 'No brands match your search.'}</p>
            </div>
          ) : (
            <div className="brands-grid">
              {filteredBrands.map(brand => (
                <article
                  key={brand.id}
                  className="brand-card"
                  role="button"
                  tabIndex={0}
                  onClick={() => setSelectedBrand(brand)}
                  onKeyDown={event => {
                    if (event.key === 'Enter' || event.key === ' ') {
                      event.preventDefault()
                      setSelectedBrand(brand)
                    }
                  }}
                >
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
                          {toHundredScale(brand.sustainabilityScore)?.toFixed(0) ?? 'n/a'} / 100
                        </p>
                      </div>

                      <div className="score-chip">
                        <p className="score-chip-label">Transparency</p>
                        <p className="score-chip-value score-chip-value-neutral">
                          {brand.transparencyScore?.toFixed(1) ?? 'n/a'} / 5
                        </p>
                      </div>
                    </div>

                    <p className="view-details">View details {'->'}</p>
                  </div>
                </article>
              ))}
            </div>
          )}
        </section>

        {selectedBrand && (
          <div className="brand-modal-overlay" onClick={() => setSelectedBrand(null)}>
            <article className="brand-modal" onClick={event => event.stopPropagation()} role="dialog" aria-modal="true" aria-label={`${selectedBrand.brandName} details`}>
              <header className="brand-modal-header">
                <div>
                  <h2>{selectedBrand.brandName}</h2>
                  <p>{selectedBrand.category ?? 'Brand profile'}</p>
                </div>
                <button type="button" className="brand-modal-close" onClick={() => setSelectedBrand(null)} aria-label="Close brand details">
                  x
                </button>
              </header>

              <div className="brand-modal-content">
                <section className="brand-modal-hero">
                  <div className="hero-score-block">
                    <p className="hero-score-label">Sustainability</p>
                    <p className="hero-score-value" style={{ color: getSustainabilityColor(selectedBrand.sustainabilityScore) }}>
                      {toHundredScale(selectedBrand.sustainabilityScore)?.toFixed(0) ?? 'n/a'}
                    </p>
                    <p className="hero-score-unit">/ 100</p>
                  </div>

                  <div className="hero-score-block secondary">
                    <p className="hero-score-label">Transparency</p>
                    <p className="hero-score-value">
                      {selectedBrand.transparencyScore?.toFixed(1) ?? 'n/a'}
                    </p>
                    <p className="hero-score-unit">/ 5</p>
                  </div>
                </section>

                {buildCategoryScores(selectedBrand).length > 0 && (
                  <section className="modal-section">
                    <h3>Category breakdown</h3>
                    <div className="category-breakdown">
                      {buildCategoryScores(selectedBrand).map(score => (
                        <div key={score.key} className="category-row">
                          <div className="category-row-head">
                            <span>{score.label}</span>
                            <span>{score.value?.toFixed(0)}%</span>
                          </div>
                          <div className="category-track">
                            <div className="category-fill" style={{ width: `${Math.min(Math.max(score.value ?? 0, 0), 100)}%` }} />
                          </div>
                        </div>
                      ))}
                    </div>
                  </section>
                )}

                {(selectedBrand.prosSummary || selectedBrand.consSummary) && (
                  <section className="modal-section two-col">
                    <div>
                      <h3>Strengths</h3>
                      <ul>
                        {(selectedBrand.prosSummary ?? 'No strengths recorded yet.')
                          .split('\n')
                          .filter(Boolean)
                          .map((item, index) => (
                            <li key={`pro-${index}`}>{item}</li>
                          ))}
                      </ul>
                    </div>

                    <div>
                      <h3>Concerns</h3>
                      <ul>
                        {(selectedBrand.consSummary ?? 'No concerns recorded yet.')
                          .split('\n')
                          .filter(Boolean)
                          .map((item, index) => (
                            <li key={`con-${index}`}>{item}</li>
                          ))}
                      </ul>
                    </div>
                  </section>
                )}

                {buildCriteriaByCategory(selectedBrand).length > 0 && (
                  <section className="modal-section">
                    <h3>Criteria details</h3>
                    <div className="criteria-groups">
                      {buildCriteriaByCategory(selectedBrand).map(([category, items]) => (
                        <div key={category} className="criteria-group">
                          <h4>{category}</h4>
                          <ul>
                            {items.map(item => (
                              <li key={item.id}>
                                <span>{item.name}</span>
                                <span>{item.numericValue?.toFixed(1) ?? 'n/a'}{item.unit ? ` ${item.unit}` : ''}</span>
                              </li>
                            ))}
                          </ul>
                        </div>
                      ))}
                    </div>
                  </section>
                )}

                {selectedBrand.evidenceSources && selectedBrand.evidenceSources.length > 0 && (
                  <section className="modal-section">
                    <h3>Evidence sources</h3>
                    <ul className="evidence-list">
                      {selectedBrand.evidenceSources.map(source => (
                        <li key={source.id}>
                          <a href={source.sourceUrl} target="_blank" rel="noreferrer">{source.sourceTitle}</a>
                        </li>
                      ))}
                    </ul>
                  </section>
                )}

                {selectedBrand.updatedAtUtc && (
                  <p className="last-updated">Last updated: {new Date(selectedBrand.updatedAtUtc).toLocaleDateString()}</p>
                )}
              </div>
            </article>
          </div>
        )}
      </main>

      <Footer />
    </>
  )
}

export default App
