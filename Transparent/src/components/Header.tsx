import './Header.css'

type HeaderProps = {
  searchQuery: string
  onSearchQueryChange: (value: string) => void
  brandCount: number
  averageSustainability?: number
  dataCoverage?: number
  activeFilter: 'all' | 'highSustainability' | 'highTransparency' | 'mostDocumented'
  onFilterChange: (value: 'all' | 'highSustainability' | 'highTransparency' | 'mostDocumented') => void
  lastUpdatedLabel: string
}

export default function Header({
  searchQuery,
  onSearchQueryChange,
  brandCount,
  averageSustainability,
  dataCoverage,
  activeFilter,
  onFilterChange,
  lastUpdatedLabel,
}: HeaderProps) {
  const isSearching = searchQuery.trim().length > 0

  const handleSearch = (e: React.FormEvent) => {
    e.preventDefault()
  }

  return (
    <header className="header">
      <div className="header-content">
        <h1 className="header-title">Transparent</h1>
        <p className="header-subtitle">
          A curated database documenting the sustainability practices of fashion brands. Entries represent the clothing brand (manufacturer) and scores reflect their production practices. Search, discover, and make informed choices.
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

        <p className="header-last-updated">Last updated: {lastUpdatedLabel}</p>

        {!isSearching && (
          <div className="stats-container">
            <div className="stat">
              <div className="stat-number">{brandCount}</div>
              <div className="stat-label">BRANDS</div>
            </div>
            <div className="stat">
              <div className="stat-number">{averageSustainability?.toFixed(0) ?? 'n/a'}</div>
              <div className="stat-label">AVG SCORE</div>
            </div>
            <div className="stat">
              <div className="stat-number">{dataCoverage?.toFixed(0) ?? 'n/a'}%</div>
              <div className="stat-label">DATA COVERAGE</div>
            </div>
          </div>
        )}

        {!isSearching && (
          <div className="header-tools" aria-label="Dashboard controls">
            <div className="filter-row" role="group" aria-label="Filter brands">
              <button
                type="button"
                className={`filter-chip ${activeFilter === 'all' ? 'filter-chip-active' : ''}`}
                onClick={() => onFilterChange('all')}
              >
                All brands
              </button>
              <button
                type="button"
                className={`filter-chip ${activeFilter === 'highSustainability' ? 'filter-chip-active' : ''}`}
                onClick={() => onFilterChange('highSustainability')}
              >
                High sustainability
              </button>
              <button
                type="button"
                className={`filter-chip ${activeFilter === 'highTransparency' ? 'filter-chip-active' : ''}`}
                onClick={() => onFilterChange('highTransparency')}
              >
                High transparency
              </button>
              <button
                type="button"
                className={`filter-chip ${activeFilter === 'mostDocumented' ? 'filter-chip-active' : ''}`}
                onClick={() => onFilterChange('mostDocumented')}
              >
                Most documented
              </button>
            </div>
          </div>
        )}
      </div>
    </header>
  )
}
