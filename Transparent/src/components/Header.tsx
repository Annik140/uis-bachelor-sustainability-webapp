import { useState } from 'react'
import './Header.css'

export default function Header() {
  const [searchQuery, setSearchQuery] = useState('')

  const handleSearch = (e: React.FormEvent) => {
    e.preventDefault()
    // TODO: Implement search functionality
    console.log('Search:', searchQuery)
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
            onChange={(e) => setSearchQuery(e.target.value)}
          />
        </form>

        <div className="stats-container">
          <div className="stat">
            <div className="stat-number">8</div>
            <div className="stat-label">BRANDS</div>
          </div>
          <div className="stat">
            <div className="stat-number">86</div>
            <div className="stat-label">AVG SCORE</div>
          </div>
          <div className="stat">
            <div className="stat-number">28</div>
            <div className="stat-label">CERTIFICATIONS</div>
          </div>
        </div>
      </div>
    </header>
  )
}
