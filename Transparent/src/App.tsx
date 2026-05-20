import Header from './components/Header'
import Footer from './components/Footer'
import AdminLogin from './components/AdminLogin'
import AdminDashboard from './components/AdminDashboard'
import './App.css'

function App() {
  const path = window.location.pathname;

  if (path.startsWith('/admin')) {
    if (path === '/admin' || path === '/admin/login') return <AdminLogin />
    return <AdminDashboard />
  }

  return (
    <>
      <Header />
      
      <main className="app-main">
        <section className="brands-container">
          {/* Brand cards will go here */}
          <div className="brands-placeholder">
            <p>Brand cards section</p>
          </div>
        </section>
      </main>

      <Footer />
    </>
  )
}

export default App
