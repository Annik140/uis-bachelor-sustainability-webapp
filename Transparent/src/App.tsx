import Header from './components/Header'
import Footer from './components/Footer'
import './App.css'

function App() {
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
