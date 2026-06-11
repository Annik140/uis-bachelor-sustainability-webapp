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
  logoPath?: string
  description?: string
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
  certifications?: BrandCertification[]
}

type EvidenceSource = {
  id: number
  sourceTitle: string
  sourceUrl: string
  sourceType?: string
  publishedAtUtc?: string
  notes?: string
}

type BrandCertification = {
  id: number
  name: string
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

type DashboardSort = 'lastUpdatedDesc' | 'sustainabilityDesc' | 'transparencyDesc' | 'alphabeticalAsc'

type PagedResponse<T> = {
  items: T[]
  page: number
  pageSize: number
  totalCount: number
  totalPages: number
}

function normalizeSustainabilityScore(scoreOutOfHundred?: number) {
  if (scoreOutOfHundred === undefined || scoreOutOfHundred === null) {
    return undefined
  }

  return Math.min(Math.max(scoreOutOfHundred, 0), 100)
}

function getTransparencySegmentFill(scoreOutOfFive: number | undefined, segmentIndex: number) {
  if (scoreOutOfFive === undefined || scoreOutOfFive === null) {
    return 0
  }

  const clamped = Math.min(Math.max(scoreOutOfFive, 0), 5)
  const segmentValue = clamped - segmentIndex
  return Math.min(Math.max(segmentValue, 0), 1) * 100
}

function getSustainabilityColor(scoreOutOfHundred?: number) {
  const score = normalizeSustainabilityScore(scoreOutOfHundred)
  if (score === undefined || score === null) return '#6b7280'
  if (score >= 70) return '#3d9f66'
  if (score >= 40) return '#b1843b'
  return '#c35b5b'
}

function formatSustainabilityScore(scoreOutOfHundred?: number) {
  const score = normalizeSustainabilityScore(scoreOutOfHundred)
  if (score === undefined || score === null) {
    return 'No info / 100'
  }

  return `${score.toFixed(0)} / 100`
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

function shouldShowPercentUnit(item: CriterionItem) {
  if (item.numericValue === undefined || item.numericValue === null) {
    return false
  }

  return item.name === 'Recycled content / Preferred material content' || item.name === 'Renewable energy'
}

type SummaryItemType = 'pro' | 'con'
type SummaryItemTier = 'strong' | 'moderate' | 'weak' | 'concern' | 'weak-concern' | 'neutral'

function getSummaryTier(item: string, type: SummaryItemType): SummaryItemTier {
  const normalized = item.trim().toLowerCase()

  if (normalized === 'no strengths recorded yet.' || normalized === 'no concerns recorded yet.') {
    return 'neutral'
  }

  if (type === 'pro') {
    if (normalized.startsWith('high ') || normalized.startsWith('broad ') || normalized.startsWith('extended ')) {
      return 'strong'
    }

    if (normalized.startsWith('good ') || normalized.startsWith('multiple ') || normalized.startsWith('standard ')) {
      return 'moderate'
    }

    if (normalized.startsWith('some ') || normalized.startsWith('one ')) {
      return 'weak'
    }

    return 'moderate'
  }

  if (normalized.startsWith('limited ')) {
    return 'weak-concern'
  }

  return 'concern'
}

function getSummaryItemClass(type: SummaryItemType, tier: SummaryItemTier) {
  return `summary-list-item summary-list-item-${type} summary-list-item-${tier}`
}

function App() {
  const path = window.location.pathname
  const [brands, setBrands] = useState<Brand[]>([])
  const [isLoading, setIsLoading] = useState(true)
  const [searchQuery, setSearchQuery] = useState('')
  const [activeSort, setActiveSort] = useState<DashboardSort>('lastUpdatedDesc')
  const [page, setPage] = useState(1)
  const [totalPages, setTotalPages] = useState(1)
  const [totalCount, setTotalCount] = useState(0)
  const [isAdminSessionActive, setIsAdminSessionActive] = useState(false)
  const [selectedBrand, setSelectedBrand] = useState<Brand | null>(null)
  const pageSize = 12
  const editMatch = path.match(/^\/admin\/brands\/(\d+)\/edit$/)

  const normalizedQuery = searchQuery.trim().toLowerCase()
  const appliedSort: DashboardSort = normalizedQuery ? 'lastUpdatedDesc' : activeSort

  function handleSearchQueryChange(value: string) {
    setIsLoading(true)
    setSearchQuery(value)
    setPage(1)
    if (value.trim().length > 0 && activeSort !== 'lastUpdatedDesc') {
      setActiveSort('lastUpdatedDesc')
    }
  }

  function handleSortChange(value: DashboardSort) {
    setIsLoading(true)
    setActiveSort(value)
    setPage(1)
  }

  function handlePageChange(nextPage: number) {
    setIsLoading(true)
    setPage(nextPage)
  }

  const visibleBrands = brands

  const latestUpdateMs = brands.reduce((latest, brand) => {
    if (!brand.updatedAtUtc) return latest
    const current = new Date(brand.updatedAtUtc).getTime()
    if (Number.isNaN(current)) return latest
    return Math.max(latest, current)
  }, 0)

  const lastUpdatedLabel = new Intl.DateTimeFormat('en-US', {
    month: 'long',
    year: 'numeric'
  }).format(latestUpdateMs > 0 ? new Date(latestUpdateMs) : new Date())

  const sustainabilityValues = brands
    .map(brand => normalizeSustainabilityScore(brand.sustainabilityScore))
    .filter((value): value is number => value !== undefined)

  const totalCriteriaCount = brands.reduce(
    (sum, brand) => sum + (brand.criteriaItems?.length ?? 0),
    0
  )
  const filledCriteriaCount = brands.reduce(
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
    if (path.startsWith('/admin')) {
      return
    }

    let disposed = false

    const checkAdminSession = async () => {
      try {
        const response = await fetch('/admin/session', {
          method: 'GET',
          credentials: 'include',
          cache: 'no-store'
        })

        if (!disposed) {
          setIsAdminSessionActive(response.ok)
        }
      } catch {
        if (!disposed) {
          setIsAdminSessionActive(false)
        }
      }
    }

    void checkAdminSession()
    const intervalId = window.setInterval(() => {
      void checkAdminSession()
    }, 60000)
    const onFocus = () => {
      void checkAdminSession()
    }
    window.addEventListener('focus', onFocus)

    return () => {
      disposed = true
      window.clearInterval(intervalId)
      window.removeEventListener('focus', onFocus)
    }
  }, [path])

  useEffect(() => {
    if (!path.startsWith('/admin')) {
      const query = new URLSearchParams({
        page: String(page),
        pageSize: String(pageSize),
        sort: appliedSort,
      })

      if (normalizedQuery) {
        query.set('q', normalizedQuery)
      }

      fetch(`/brands?${query.toString()}`)
        .then(response => response.ok ? response.json() : null)
        .then((data: PagedResponse<Brand> | Brand[] | null) => {
          if (!data) {
            setBrands([])
            setTotalCount(0)
            setTotalPages(1)
            return
          }

          if (Array.isArray(data)) {
            setBrands(data)
            setTotalCount(data.length)
            setTotalPages(1)
            setPage(1)
            return
          }

          setBrands(data.items ?? [])
          setTotalCount(data.totalCount ?? 0)
          setTotalPages(Math.max(1, data.totalPages ?? 1))
          setPage(previous => {
            const nextPage = Math.max(1, data.page ?? 1)
            return previous === nextPage ? previous : nextPage
          })
        })
        .catch(() => {
          setBrands([])
          setTotalCount(0)
          setTotalPages(1)
        })
        .finally(() => setIsLoading(false))
    }
  }, [appliedSort, normalizedQuery, page, path, pageSize])

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
        onSearchQueryChange={handleSearchQueryChange}
        brandCount={totalCount}
        averageSustainability={averageSustainability}
        dataCoverage={dataCoverage}
        isLoading={isLoading}
        showAdminShortcut={isAdminSessionActive}
        activeSort={activeSort}
        onSortChange={handleSortChange}
        lastUpdatedLabel={lastUpdatedLabel}
      />
      
      <main className="app-main">
        <section className="brands-container">
          {isLoading && visibleBrands.length === 0 ? (
            <div className="brands-grid-shell">
              <div className="brands-grid brands-grid-loading" aria-label="Loading brands">
                {Array.from({ length: 8 }, (_, index) => (
                  <article key={`loading-${index}`} className="brand-card brand-card-loading" aria-hidden="true">
                    <div className="brand-card-top">
                      <div className="skeleton skeleton-title" />
                    </div>
                    <div className="brand-card-body">
                      <div className="skeleton skeleton-line" />
                      <div className="skeleton skeleton-line short" />
                      <div className="skeleton skeleton-line" />
                    </div>
                  </article>
                ))}
              </div>
            </div>
          ) : visibleBrands.length === 0 ? (
            <div className="brands-placeholder">
              <p>{totalCount === 0 ? 'No brands added yet.' : 'No brands match your search.'}</p>
            </div>
          ) : (
            <div className="brands-grid-shell">
              <div className="brands-grid">
                {visibleBrands.map(brand => (
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
                      {brand.logoPath?.trim() ? (
                        <img className="brand-logo" src={brand.logoPath} alt={`${brand.brandName} logo`} loading="lazy" />
                      ) : (
                        <h3 className="brand-title">{brand.brandName}</h3>
                      )}
                    </div>

                    <div className="brand-card-body">
                      <p className="brand-description-preview">
                        {brand.description?.trim() || (brand.category ? `${brand.category} profile.` : 'Description coming soon.')}
                      </p>

                      <div className="scores-row" aria-label="Brand scores">
                        <div className="score-chip">
                          <p className="score-chip-label">Sustainability</p>
                          <p className="score-chip-value" style={{ color: getSustainabilityColor(brand.sustainabilityScore) }}>
                            {formatSustainabilityScore(brand.sustainabilityScore)}
                          </p>
                        </div>

                        <div className="score-chip">
                          <p className="score-chip-label">Transparency</p>
                          <p className="score-chip-value score-chip-value-neutral">
                            {brand.transparencyScore?.toFixed(1) ?? 'No info'} / 5
                          </p>
                          <div className="transparency-track" aria-hidden="true">
                            {Array.from({ length: 5 }, (_, index) => (
                              <div key={index} className="transparency-segment">
                                <div
                                  className="transparency-segment-fill"
                                  style={{ width: `${getTransparencySegmentFill(brand.transparencyScore, index)}%` }}
                                />
                              </div>
                            ))}
                          </div>
                        </div>
                      </div>

                      <p className="view-details">View details {'->'}</p>
                    </div>
                  </article>
                ))}
              </div>
            </div>
          )}

          {isLoading && visibleBrands.length > 0 && (
            <div className="brands-loading-overlay" aria-live="polite">Loading updated brands...</div>
          )}

          {totalPages > 1 && (
            <div className="dashboard-pagination" aria-label="Brand pagination">
              <button type="button" onClick={() => handlePageChange(Math.max(1, page - 1))} disabled={page <= 1}>
                Previous
              </button>
              <span>Page {page} of {totalPages}</span>
              <button type="button" onClick={() => handlePageChange(Math.min(totalPages, page + 1))} disabled={page >= totalPages}>
                Next
              </button>
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
                  {selectedBrand.description?.trim() && <p>{selectedBrand.description}</p>}
                </div>
                <button type="button" className="brand-modal-close" onClick={() => setSelectedBrand(null)} aria-label="Close brand details">
                  x
                </button>
              </header>

              <div className="brand-modal-content">
                <section className="brand-modal-hero">
                  <div className="hero-score-block">
                    <p className="hero-score-label">Sustainability</p>
                    <p
                      className={`hero-score-value ${normalizeSustainabilityScore(selectedBrand.sustainabilityScore) === undefined ? 'hero-score-value-no-info' : ''}`}
                      style={{ color: getSustainabilityColor(selectedBrand.sustainabilityScore) }}
                    >
                      {normalizeSustainabilityScore(selectedBrand.sustainabilityScore)?.toFixed(0) ?? 'No info'}
                    </p>
                    <p className="hero-score-unit">/ 100</p>
                  </div>

                  <div className="hero-score-block secondary">
                    <p className="hero-score-label">Transparency</p>
                    <p className="hero-score-value">
                      {selectedBrand.transparencyScore?.toFixed(1) ?? 'No info'}
                    </p>
                    <p className="hero-score-unit">/ 5</p>
                    <div className="transparency-track transparency-track-hero" aria-hidden="true">
                      {Array.from({ length: 5 }, (_, index) => (
                        <div key={index} className="transparency-segment">
                          <div
                            className="transparency-segment-fill"
                            style={{ width: `${getTransparencySegmentFill(selectedBrand.transparencyScore, index)}%` }}
                          />
                        </div>
                      ))}
                    </div>
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
                      <ul className="summary-list summary-list-pros">
                        {(selectedBrand.prosSummary ?? 'No strengths recorded yet.')
                          .split('\n')
                          .filter(Boolean)
                          .map((item, index) => {
                            const tier = getSummaryTier(item, 'pro')

                            return (
                            <li key={`pro-${index}`} className={getSummaryItemClass('pro', tier)}>
                              {tier !== 'neutral' && <span className="summary-list-symbol" aria-hidden="true">+</span>}
                              <span>{item}</span>
                            </li>
                            )
                          })}
                      </ul>
                    </div>

                    <div>
                      <h3>Concerns</h3>
                      <ul className="summary-list summary-list-cons">
                        {(selectedBrand.consSummary ?? 'No concerns recorded yet.')
                          .split('\n')
                          .filter(Boolean)
                          .map((item, index) => {
                            const tier = getSummaryTier(item, 'con')

                            return (
                            <li key={`con-${index}`} className={getSummaryItemClass('con', tier)}>
                              {tier !== 'neutral' && <span className="summary-list-symbol" aria-hidden="true">-</span>}
                              <span>{item}</span>
                            </li>
                            )
                          })}
                      </ul>
                    </div>
                  </section>
                )}

                {selectedBrand.certifications && selectedBrand.certifications.length > 0 && (
                  <section className="modal-section">
                    <h3>Certifications</h3>
                    <div className="certification-tags">
                      {selectedBrand.certifications.map(certification => (
                        <span key={certification.id} className="certification-tag">{certification.name}</span>
                      ))}
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
                                <span>{item.numericValue?.toFixed(1) ?? 'No info'}{shouldShowPercentUnit(item) ? ' %' : ''}</span>
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
