import { useState, useEffect } from 'react'

type Brand = {
  id?: number
  brandName: string
  category?: string
  sustainabilityScore?: number
  transparencyScore?: number
  materialSustainabilityScore?: number
  laborPracticesScore?: number
  carbonFootprintScore?: number
  productLongevityScore?: number
  evidenceSourceCount?: number
}

export default function AdminDashboard(){
  const [brands, setBrands] = useState<Brand[]>([])
  const [form, setForm] = useState<Brand>({
    brandName: '',
    category: '',
    materialSustainabilityScore: undefined,
    laborPracticesScore: undefined,
    carbonFootprintScore: undefined,
    productLongevityScore: undefined,
    evidenceSourceCount: 0
  })
  const [error, setError] = useState<string | null>(null)
  const [editingId, setEditingId] = useState<number | null>(null)

  useEffect(()=>{ fetchBrands() }, [])

  async function fetchBrands(){
    const res = await fetch('/brands')
    if(res.ok) setBrands(await res.json())
  }

  async function handleCreate(e: React.FormEvent){
    e.preventDefault()
    setError(null)
    try{
      if (editingId) {
        const res = await fetch(`/admin/clothingbrands/${editingId}`, {
          method: 'PUT',
          credentials: 'include',
          headers: { 'Content-Type': 'application/json' },
          body: JSON.stringify(form)
        })
        if (res.ok) {
          setForm({
            brandName: '',
            category: '',
            materialSustainabilityScore: undefined,
            laborPracticesScore: undefined,
            carbonFootprintScore: undefined,
            productLongevityScore: undefined,
            evidenceSourceCount: 0
          })
          setEditingId(null)
          fetchBrands()
          return
        }
      } else {
        const res = await fetch('/admin/clothingbrands', {
          method: 'POST',
          credentials: 'include',
          headers: { 'Content-Type': 'application/json' },
          body: JSON.stringify(form)
        })
        if(res.ok){
          setForm({
            brandName: '',
            category: '',
            materialSustainabilityScore: undefined,
            laborPracticesScore: undefined,
            carbonFootprintScore: undefined,
            productLongevityScore: undefined,
            evidenceSourceCount: 0
          })
          fetchBrands()
          return
        }
      }
      setError('Failed to save')
    } catch {
      setError('Network error')
    }
  }

  async function handleDelete(id?: number){
    if(!id) return
    await fetch(`/admin/clothingbrands/${id}`, { method: 'DELETE', credentials: 'include' })
    fetchBrands()
  }

  function startEdit(b: Brand){
    setEditingId(b.id ?? null)
    setForm({
      brandName: b.brandName,
      category: b.category,
      materialSustainabilityScore: b.materialSustainabilityScore,
      laborPracticesScore: b.laborPracticesScore,
      carbonFootprintScore: b.carbonFootprintScore,
      productLongevityScore: b.productLongevityScore,
      evidenceSourceCount: b.evidenceSourceCount ?? 0
    })
  }

  function cancelEdit(){
    setEditingId(null)
    setForm({
      brandName: '',
      category: '',
      materialSustainabilityScore: undefined,
      laborPracticesScore: undefined,
      carbonFootprintScore: undefined,
      productLongevityScore: undefined,
      evidenceSourceCount: 0
    })
  }

  return (
    <div style={{padding: 20}}>
      <h2>Admin Dashboard</h2>
      <section>
        <h3>{editingId ? 'Edit Brand' : 'Create Brand'}</h3>
        <form onSubmit={handleCreate}>
          <div>
            <label>Brand name</label>
            <input required value={form.brandName} onChange={e => setForm({...form, brandName: e.target.value})} />
          </div>
          <div>
            <label>Category</label>
            <input value={form.category} onChange={e => setForm({...form, category: e.target.value})} />
          </div>
          <div>
            <label>Material sustainability score</label>
            <input type="number" min="0" max="10" step="0.1" value={form.materialSustainabilityScore ?? ''} onChange={e => setForm({...form, materialSustainabilityScore: e.target.value ? Number(e.target.value) : undefined})} />
          </div>
          <div>
            <label>Labor practices score</label>
            <input type="number" min="0" max="10" step="0.1" value={form.laborPracticesScore ?? ''} onChange={e => setForm({...form, laborPracticesScore: e.target.value ? Number(e.target.value) : undefined})} />
          </div>
          <div>
            <label>Carbon footprint score</label>
            <input type="number" min="0" max="10" step="0.1" value={form.carbonFootprintScore ?? ''} onChange={e => setForm({...form, carbonFootprintScore: e.target.value ? Number(e.target.value) : undefined})} />
          </div>
          <div>
            <label>Product longevity score</label>
            <input type="number" min="0" max="10" step="0.1" value={form.productLongevityScore ?? ''} onChange={e => setForm({...form, productLongevityScore: e.target.value ? Number(e.target.value) : undefined})} />
          </div>
          <div>
            <label>Evidence sources used</label>
            <input type="number" min="0" step="1" value={form.evidenceSourceCount ?? 0} onChange={e => setForm({...form, evidenceSourceCount: e.target.value ? Number(e.target.value) : 0})} />
          </div>
          <button type="submit">{editingId ? 'Save' : 'Create'}</button>
          {editingId && <button type="button" onClick={cancelEdit} style={{marginLeft:8}}>Cancel</button>}
        </form>
        {error && <p style={{color:'red'}}>{error}</p>}
      </section>

      <section>
        <h3>Existing Brands</h3>
        <ul>
          {brands.map(b => (
            <li key={b.id} style={{marginBottom:8}}>
              <strong>{b.brandName}</strong> — {b.category} — sustainability {b.sustainabilityScore?.toFixed(1)} / 10 — transparency {b.transparencyScore?.toFixed(1)} / 5
              <button onClick={()=>startEdit(b)} style={{marginLeft:8}}>Edit</button>
              <button onClick={()=>handleDelete(b.id)} style={{marginLeft:8}}>Delete</button>
            </li>
          ))}
        </ul>
      </section>
    </div>
  )
}
