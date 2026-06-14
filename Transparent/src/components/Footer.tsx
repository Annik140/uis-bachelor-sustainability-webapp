import './Footer.css'

export default function Footer() {
  return (
    <footer className="footer">
      <div className="footer-content">
        <h2 className="footer-title">About the Database</h2>

        <div className="footer-sections">
          <div className="footer-section">
            <h3 className="footer-section-title">OUR MISSION</h3>
            <p className="footer-text">
              Transparent Fashion aims to make clothing sustainability information easier to access and understand through a structured database, helping users explore sustainability information across apparel brands.
            </p>
          </div>

          <div className="footer-section">
            <h3 className="footer-section-title">METHODOLOGY</h3>
            <p className="footer-text">
              Scores are calculated using publicly available sustainability disclosures and reports. Brands are evaluated across materials, labor practices, carbon footprint, and product longevity.
            </p>
          </div>
        </div>
      </div>
    </footer>
  )
}
