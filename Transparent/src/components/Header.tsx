import './Header.css'

type HeaderProps = {
  searchQuery: string
  onSearchQueryChange: (value: string) => void
  brandCount: number
  averageSustainability?: number
  dataCoverage?: number
  isLoading: boolean
  showAdminShortcut: boolean
  activeSort: 'lastUpdatedDesc' | 'sustainabilityDesc' | 'transparencyDesc' | 'alphabeticalAsc'
  onSortChange: (value: 'lastUpdatedDesc' | 'sustainabilityDesc' | 'transparencyDesc' | 'alphabeticalAsc') => void
  lastUpdatedLabel: string
}

export default function Header({
  searchQuery,
  onSearchQueryChange,
  brandCount,
  averageSustainability,
  dataCoverage,
  isLoading,
  showAdminShortcut,
  activeSort,
  onSortChange,
  lastUpdatedLabel,
}: HeaderProps) {
  const isSearching = searchQuery.trim().length > 0

  const handleSearch = (e: React.FormEvent) => {
    e.preventDefault()
  }

  return (
    <header className="header">
      <div className="header-content">
        {showAdminShortcut && (
          <button
            type="button"
            className="header-admin-shortcut"
            onClick={() => {
              window.location.href = '/admin/dashboard'
            }}
          >
            Back To Admin Dashboard
          </button>
        )}

        <h1 className="header-title">Transparent</h1>
        <p className="header-subtitle">
          Making fashion sustainability more transparent. Explore apparel brands, sustainability scores, and the publicly available information behind each evaluation.
        </p>
        <form className={`search-form ${isSearching ? 'search-form-active' : ''}`} onSubmit={handleSearch}>
          <input
            type="text"
            className="search-input"
            placeholder="Search brands..."
            value={searchQuery}
            onChange={(e) => onSearchQueryChange(e.target.value)}
          />
        </form>

        <p className="header-last-updated">Data last updated: {lastUpdatedLabel}</p>

        {!isSearching && (
          <div className="stats-container">
            <div className="stat">
              <div className="stat-number">{isLoading ? <span className="header-skeleton header-skeleton-number" /> : brandCount}</div>
              <div className="stat-label">BRANDS</div>
            </div>
            <div className="stat">
              <div className="stat-number">{isLoading ? <span className="header-skeleton header-skeleton-number" /> : (averageSustainability?.toFixed(0) ?? 'n/a')}</div>
              <div className="stat-label">AVG SCORE</div>
            </div>
            <div className="stat">
              <div className="stat-number">{isLoading ? <span className="header-skeleton header-skeleton-number" /> : `${dataCoverage?.toFixed(0) ?? 'n/a'}%`}</div>
              <div className="stat-label">AVG DATA COVERAGE</div>
            </div>
          </div>
        )}

        {!isSearching && (
          <div className="header-tools" aria-label="Dashboard controls">
            <div className="filter-row" role="group" aria-label="Sort brands">
              <button
                type="button"
                className={`filter-chip ${activeSort === 'lastUpdatedDesc' ? 'filter-chip-active' : ''}`}
                onClick={() => onSortChange('lastUpdatedDesc')}
              >
                Last updated
              </button>
              <button
                type="button"
                className={`filter-chip ${activeSort === 'sustainabilityDesc' ? 'filter-chip-active' : ''}`}
                onClick={() => onSortChange('sustainabilityDesc')}
              >
                High sustainability
              </button>
              <button
                type="button"
                className={`filter-chip ${activeSort === 'transparencyDesc' ? 'filter-chip-active' : ''}`}
                onClick={() => onSortChange('transparencyDesc')}
              >
                High transparency
              </button>
              <button
                type="button"
                className={`filter-chip ${activeSort === 'alphabeticalAsc' ? 'filter-chip-active' : ''}`}
                onClick={() => onSortChange('alphabeticalAsc')}
              >
                A-Z
              </button>
            </div>
          </div>
        )}
      </div>
    </header>
  )
}
