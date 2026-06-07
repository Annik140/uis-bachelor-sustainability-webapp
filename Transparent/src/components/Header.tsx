import './Header.css'

type HeaderProps = {
  searchQuery: string
  onSearchQueryChange: (value: string) => void
  brandCount: number
  averageSustainability?: number
  dataCoverage?: number
}

export default function Header({
  searchQuery,
  onSearchQueryChange,
  brandCount,
  averageSustainability,
  dataCoverage,
}: HeaderProps) {
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

        <form className="search-form" onSubmit={handleSearch}>
          <input
            type="text"
            className="search-input"
            placeholder="Search brands..."
            value={searchQuery}
            onChange={(e) => onSearchQueryChange(e.target.value)}
          />
        </form>

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
      </div>
    </header>
  )
}
