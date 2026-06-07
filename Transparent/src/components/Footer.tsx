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
              Transparent provides verified data on fashion brands' environmental and ethical practices. We believe informed consumers drive sustainable change.
            </p>
          </div>

          <div className="footer-section">
            <h3 className="footer-section-title">METHODOLOGY</h3>
            <p className="footer-text">
              Scores are calculated from publicly available data, third-party certifications, and verified reports on materials, water use, labor practices, and circularity.
            </p>
          </div>
        </div>
      </div>
    </footer>
  )
}
