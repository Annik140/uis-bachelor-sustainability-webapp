import { useEffect, useMemo, useState } from 'react'

type Mode = 'create' | 'edit'

type EvidenceSource = {
  sourceTitle: string
  sourceUrl: string
  sourceType?: string
  publishedAtUtc?: string
  notes?: string
}

type CriterionItem = {
  category: string
  name: string
  numericValue?: number
  unit?: string
  goodThreshold?: number
  warningThreshold?: number
  lowerIsBetter: boolean
  weight: number
  notes?: string
}

type Brand = {
  id?: number
  brandName: string
  category?: string
  primarySourceTitle?: string
  primarySourceUrl?: string
  primarySourcePublishedAtUtc?: string
  evidenceSummary?: string
  sustainabilityScore?: number
  transparencyScore?: number
  materialSustainabilityScore?: number
  laborPracticesScore?: number
  carbonFootprintScore?: number
  productLongevityScore?: number
  evidenceSourceCount?: number
  evidenceSources?: EvidenceSource[]
  criteriaItems?: CriterionItem[]
  prosSummary?: string
  consSummary?: string
}

const categoryLabels = [
  'Material sustainability',
  'Labor practices',
  'Carbon footprint',
  'Product longevity'
]

const categoryValues = ['Material', 'Labor', 'Carbon', 'Longevity'] as const

const createEmptyBrand = (): Brand => ({
  brandName: '',
  category: '',
  primarySourceTitle: '',
  primarySourceUrl: '',
  primarySourcePublishedAtUtc: undefined,
  evidenceSummary: '',
  sustainabilityScore: undefined,
  transparencyScore: undefined,
  materialSustainabilityScore: undefined,
  laborPracticesScore: undefined,
  carbonFootprintScore: undefined,
  productLongevityScore: undefined,
  evidenceSourceCount: 0,
  evidenceSources: [],
  criteriaItems: [],
  prosSummary: '',
  consSummary: ''
})

export default function AdminBrandForm({ mode, brandId }: { mode: Mode; brandId?: number }) {
  const [form, setForm] = useState<Brand>(createEmptyBrand())
  const [error, setError] = useState<string | null>(null)
  const [loading, setLoading] = useState(mode === 'edit')

  const title = mode === 'create' ? 'Add new brand' : 'Edit brand'
  const subtitle = mode === 'create'
    ? 'Fill in the four sustainability categories and add subcriteria rows underneath each one.'
    : 'Update the brand and refine the scoring rubric without changing the dashboard layout.'

  const groupedCriteria = useMemo(() => {
    return categoryValues.map(category => ({
      category,
      items: (form.criteriaItems ?? []).filter(item => item.category === category)
    }))
  }, [form.criteriaItems])

  useEffect(() => {
    if (mode !== 'edit' || !brandId) {
      setLoading(false)
      return
    }

    fetch(`/brands/${brandId}`)
      .then(response => response.ok ? response.json() : null)
      .then(data => {
        if (data) {
          setForm({
            ...createEmptyBrand(),
            ...data,
            evidenceSources: data.evidenceSources ?? [],
            criteriaItems: data.criteriaItems ?? []
          })
        }
      })
      .finally(() => setLoading(false))
  }, [brandId, mode])

  function backToDashboard() {
    window.location.href = '/admin/dashboard'
  }

  function updateBrand(patch: Partial<Brand>) {
    setForm({ ...form, ...patch })
  }

  function addEvidenceSource() {
    updateBrand({
      evidenceSources: [
        ...(form.evidenceSources ?? []),
        { sourceTitle: '', sourceUrl: '', sourceType: '', notes: '' }
      ]
    })
  }

  function updateEvidenceSource(index: number, patch: Partial<EvidenceSource>) {
    const evidenceSources = [...(form.evidenceSources ?? [])]
    evidenceSources[index] = { ...evidenceSources[index], ...patch }
    updateBrand({ evidenceSources })
  }

  function removeEvidenceSource(index: number) {
    const evidenceSources = [...(form.evidenceSources ?? [])]
    evidenceSources.splice(index, 1)
    updateBrand({ evidenceSources })
  }

  function addCriterion(category: (typeof categoryValues)[number]) {
    updateBrand({
      criteriaItems: [
        ...(form.criteriaItems ?? []),
        {
          category,
          name: '',
          numericValue: undefined,
          unit: '',
          goodThreshold: undefined,
          warningThreshold: undefined,
          lowerIsBetter: true,
          weight: 1,
          notes: ''
        }
      ]
    })
  }

  function updateCriterion(index: number, patch: Partial<CriterionItem>) {
    const criteriaItems = [...(form.criteriaItems ?? [])]
    criteriaItems[index] = { ...criteriaItems[index], ...patch }
    updateBrand({ criteriaItems })
  }

  function removeCriterion(index: number) {
    const criteriaItems = [...(form.criteriaItems ?? [])]
    criteriaItems.splice(index, 1)
    updateBrand({ criteriaItems })
  }

  async function handleSubmit(e: React.FormEvent) {
    e.preventDefault()
    setError(null)

    try {
      const payload = form
      const response = mode === 'create'
        ? await fetch('/admin/clothingbrands', {
            method: 'POST',
            credentials: 'include',
            headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify(payload)
          })
        : await fetch(`/admin/clothingbrands/${brandId}`, {
            method: 'PUT',
            credentials: 'include',
            headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify(payload)
          })

      if (response.ok) {
        window.location.href = '/admin/dashboard'
        return
      }

      setError('Failed to save brand')
    } catch {
      setError('Network error')
    }
  }

  if (loading) {
    return <div style={{ padding: 24 }}>Loading brand...</div>
  }

  return (
    <div style={{
      padding: 24,
      minHeight: '100vh',
      background: mode === 'create' ? 'linear-gradient(180deg, #f6f4ee 0%, #ffffff 100%)' : 'linear-gradient(180deg, #eef3f7 0%, #ffffff 100%)'
    }}>
      <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', marginBottom: 20 }}>
        <div>
          <h2>{title}</h2>
          <p>{subtitle}</p>
        </div>
        <button type="button" onClick={backToDashboard}>Back to dashboard</button>
      </div>

      <form onSubmit={handleSubmit} style={{ display: 'grid', gap: 16, maxWidth: 980, background: '#fff', padding: 20, borderRadius: 14, boxShadow: '0 8px 30px rgba(0,0,0,0.06)' }}>
        <section>
          <h3>Brand details</h3>
          <div>
            <label>Brand name</label>
            <input required value={form.brandName} onChange={e => updateBrand({ brandName: e.target.value })} />
          </div>
          <div>
            <label>Category</label>
            <input value={form.category ?? ''} onChange={e => updateBrand({ category: e.target.value })} />
          </div>
          <div>
            <label>Primary source title</label>
            <input value={form.primarySourceTitle ?? ''} onChange={e => updateBrand({ primarySourceTitle: e.target.value })} />
          </div>
          <div>
            <label>Primary source URL</label>
            <input value={form.primarySourceUrl ?? ''} onChange={e => updateBrand({ primarySourceUrl: e.target.value })} />
          </div>
          <div>
            <label>Primary source published date</label>
            <input type="date" value={form.primarySourcePublishedAtUtc ? form.primarySourcePublishedAtUtc.slice(0, 10) : ''} onChange={e => updateBrand({ primarySourcePublishedAtUtc: e.target.value ? new Date(e.target.value).toISOString() : undefined })} />
          </div>
          <div>
            <label>Evidence summary</label>
            <textarea value={form.evidenceSummary ?? ''} onChange={e => updateBrand({ evidenceSummary: e.target.value })} />
          </div>
        </section>

        <section>
          <h3>Scoring rubric</h3>
          <p>Each category can contain multiple subcriteria. Use a numeric value and thresholds to automatically create pros and cons reasoning.</p>
          {groupedCriteria.map(group => (
            <div key={group.category} style={{ marginTop: 18, border: '1px solid #e5e5e5', borderRadius: 12, padding: 16 }}>
              <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center' }}>
                <h4>{categoryLabels[categoryValues.indexOf(group.category as (typeof categoryValues)[number])]}</h4>
                <button type="button" onClick={() => addCriterion(group.category as (typeof categoryValues)[number])}>Add subcriteria</button>
              </div>

              {group.items.length === 0 ? (
                <p style={{ color: '#777' }}>No subcriteria added yet.</p>
              ) : (
                group.items.map(item => {
                  const index = (form.criteriaItems ?? []).findIndex(candidate => candidate === item)
                  return (
                    <div key={`${item.category}-${index}-${item.name}`} style={{ marginTop: 12, padding: 12, background: '#fafafa', borderRadius: 10 }}>
                      <div>
                        <label>Subcriteria label</label>
                        <input value={item.name} onChange={e => updateCriterion(index, { name: e.target.value })} placeholder="Example: annual CO2 output" />
                      </div>
                      <div>
                        <label>Numeric value</label>
                        <input type="number" step="0.1" value={item.numericValue ?? ''} onChange={e => updateCriterion(index, { numericValue: e.target.value ? Number(e.target.value) : undefined })} />
                      </div>
                      <div>
                        <label>Unit</label>
                        <input value={item.unit ?? ''} onChange={e => updateCriterion(index, { unit: e.target.value })} placeholder="kg CO2e / year" />
                      </div>
                      <div>
                        <label>Good threshold</label>
                        <input type="number" step="0.1" value={item.goodThreshold ?? ''} onChange={e => updateCriterion(index, { goodThreshold: e.target.value ? Number(e.target.value) : undefined })} />
                      </div>
                      <div>
                        <label>Warning threshold</label>
                        <input type="number" step="0.1" value={item.warningThreshold ?? ''} onChange={e => updateCriterion(index, { warningThreshold: e.target.value ? Number(e.target.value) : undefined })} />
                      </div>
                      <div>
                        <label>Lower is better</label>
                        <input type="checkbox" checked={item.lowerIsBetter} onChange={e => updateCriterion(index, { lowerIsBetter: e.target.checked })} />
                      </div>
                      <div>
                        <label>Weight</label>
                        <input type="number" step="0.1" value={item.weight ?? 1} onChange={e => updateCriterion(index, { weight: e.target.value ? Number(e.target.value) : 1 })} />
                      </div>
                      <div>
                        <label>Notes</label>
                        <textarea value={item.notes ?? ''} onChange={e => updateCriterion(index, { notes: e.target.value })} />
                      </div>
                      <button type="button" onClick={() => removeCriterion(index)}>Remove subcriteria</button>
                    </div>
                  )
                })
              )}
            </div>
          ))}
        </section>

        <section>
          <h3>Evidence sources</h3>
          <button type="button" onClick={addEvidenceSource}>Add source</button>
          {(form.evidenceSources ?? []).map((source, index) => (
            <div key={index} style={{ marginTop: 12, padding: 12, border: '1px solid #ddd', borderRadius: 10 }}>
              <div>
                <label>Source title</label>
                <input value={source.sourceTitle} onChange={e => updateEvidenceSource(index, { sourceTitle: e.target.value })} />
              </div>
              <div>
                <label>Source URL</label>
                <input value={source.sourceUrl} onChange={e => updateEvidenceSource(index, { sourceUrl: e.target.value })} />
              </div>
              <div>
                <label>Source type</label>
                <input value={source.sourceType ?? ''} onChange={e => updateEvidenceSource(index, { sourceType: e.target.value })} />
              </div>
              <div>
                <label>Notes</label>
                <textarea value={source.notes ?? ''} onChange={e => updateEvidenceSource(index, { notes: e.target.value })} />
              </div>
              <button type="button" onClick={() => removeEvidenceSource(index)}>Remove source</button>
            </div>
          ))}
        </section>

        <section>
          <h3>Scoring result preview</h3>
          <div>
            <label>Pros summary</label>
            <textarea value={form.prosSummary ?? ''} readOnly />
          </div>
          <div>
            <label>Cons summary</label>
            <textarea value={form.consSummary ?? ''} readOnly />
          </div>
          <div>
            <label>Material score</label>
            <input readOnly value={form.materialSustainabilityScore ?? ''} />
          </div>
          <div>
            <label>Labor score</label>
            <input readOnly value={form.laborPracticesScore ?? ''} />
          </div>
          <div>
            <label>Carbon score</label>
            <input readOnly value={form.carbonFootprintScore ?? ''} />
          </div>
          <div>
            <label>Longevity score</label>
            <input readOnly value={form.productLongevityScore ?? ''} />
          </div>
          <div>
            <label>Transparency score</label>
            <input readOnly value={form.transparencyScore ?? ''} />
          </div>
        </section>

        {error && <p style={{ color: 'red' }}>{error}</p>}

        <div style={{ display: 'flex', gap: 12 }}>
          <button type="submit">{mode === 'create' ? 'Create brand' : 'Save changes'}</button>
          <button type="button" onClick={backToDashboard}>Cancel</button>
        </div>
      </form>
    </div>
  )
}