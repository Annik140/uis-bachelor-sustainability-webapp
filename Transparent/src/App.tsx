import { useEffect, useState } from 'react'
import Header from './components/Header'
import Footer from './components/Footer'
import AdminLogin from './components/AdminLogin'
import AdminDashboard from './components/AdminDashboard'
import './App.css'

type Brand = {
  id: number
  brandName: string
  category?: string
  sustainabilityScore?: number
  transparencyScore?: number
  materialSustainabilityScore?: number
  laborPracticesScore?: number
  carbonFootprintScore?: number
  productLongevityScore?: number
}

function App() {
  const path = window.location.pathname;
  const [brands, setBrands] = useState<Brand[]>([])

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
