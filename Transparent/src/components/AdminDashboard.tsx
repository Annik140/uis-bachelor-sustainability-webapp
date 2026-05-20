import { useState, useEffect } from 'react'

type Brand = {
  id?: number
  brandName: string
  category?: string
  sustainabilityScore?: number
}

export default function AdminDashboard(){
  const [brands, setBrands] = useState<Brand[]>([])
  const [form, setForm] = useState<Brand>({ brandName: '', category: '', sustainabilityScore: undefined })
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
          setForm({ brandName: '', category: '', sustainabilityScore: undefined })
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
          setForm({ brandName: '', category: '', sustainabilityScore: undefined })
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
    setForm({ brandName: b.brandName, category: b.category, sustainabilityScore: b.sustainabilityScore })
  }

  function cancelEdit(){
    setEditingId(null)
    setForm({ brandName: '', category: '', sustainabilityScore: undefined })
  }

  return (
    <div style={{padding: 20}}>
      <h2>Admin Dashboard</h2>
      <section>
        <h3>Create Brand</h3>
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
            <label>Sustainability score</label>
            <input type="number" step="0.1" value={form.sustainabilityScore ?? ''} onChange={e => setForm({...form, sustainabilityScore: e.target.value ? Number(e.target.value) : undefined})} />
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
              <strong>{b.brandName}</strong> — {b.category} — {b.sustainabilityScore}
              <button onClick={()=>startEdit(b)} style={{marginLeft:8}}>Edit</button>
              <button onClick={()=>handleDelete(b.id)} style={{marginLeft:8}}>Delete</button>
            </li>
          ))}
        </ul>
      </section>
    </div>
  )
}
