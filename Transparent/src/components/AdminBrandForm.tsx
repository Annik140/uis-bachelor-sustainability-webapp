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

type CriterionInputKind = 'number' | 'select'

type CriterionOption = {
  label: string
  value: number
}

type CriterionDefinition = {
  category: string
  name: string
  inputKind: CriterionInputKind
  options?: CriterionOption[]
}

type Brand = {
  id?: number
  brandName: string
  evidenceSourceCount?: number
  evidenceSources?: EvidenceSource[]
  criteriaItems?: CriterionItem[]
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
  evidenceSourceCount: 0,
  evidenceSources: [],
  criteriaItems: DEFAULT_CRITERIA.map(c => ({
    category: c.category,
    name: c.name,
    numericValue: undefined,
    unit: '',
    goodThreshold: undefined,
    warningThreshold: undefined,
    lowerIsBetter: true,
    weight: 1,
    notes: ''
  })),
})

const DEFAULT_CRITERIA: CriterionDefinition[] = [
  // Material
  { category: 'Material', name: 'Fiber provenance', inputKind: 'select', options: [
    { label: 'No traceability', value: 0 },
    { label: 'Partial traceability', value: 5 },
    { label: 'Full traceability', value: 10 }
  ] },
  { category: 'Material', name: 'Material toxicity & chemical management', inputKind: 'select', options: [
    { label: 'No clear policy', value: 0 },
    { label: 'Basic policy', value: 5 },
    { label: 'Strong third-party verified policy', value: 10 }
  ] },
  { category: 'Material', name: 'Recycled / regenerative content', inputKind: 'number' },
  { category: 'Material', name: 'Certifications & standards', inputKind: 'select', options: [
    { label: 'None', value: 0 },
    { label: 'One credible certification', value: 5 },
    { label: 'Multiple credible certifications', value: 10 }
  ] },
  // Labor
  { category: 'Labor', name: 'Living wage coverage', inputKind: 'number' },
  { category: 'Labor', name: 'Worker safety & working hours', inputKind: 'select', options: [
    { label: 'No evidence', value: 0 },
    { label: 'Partial evidence', value: 5 },
    { label: 'Clear evidence', value: 10 }
  ] },
  { category: 'Labor', name: 'Freedom of association / grievance mechanisms', inputKind: 'select', options: [
    { label: 'No evidence', value: 0 },
    { label: 'Partial evidence', value: 5 },
    { label: 'Clear evidence', value: 10 }
  ] },
  { category: 'Labor', name: 'Supplier audit transparency', inputKind: 'select', options: [
    { label: 'No public audits', value: 0 },
    { label: 'Some audit visibility', value: 5 },
    { label: 'Clear public audit reporting', value: 10 }
  ] },
  // Carbon
  { category: 'Carbon', name: 'Measured footprint (Scope 1–3)', inputKind: 'select', options: [
    { label: 'Not measured', value: 0 },
    { label: 'Partial measurement', value: 5 },
    { label: 'Full measurement', value: 10 }
  ] },
  { category: 'Carbon', name: 'Reduction targets & progress', inputKind: 'select', options: [
    { label: 'No target', value: 0 },
    { label: 'Target set', value: 5 },
    { label: 'Target plus visible progress', value: 10 }
  ] },
  { category: 'Carbon', name: 'Energy sourcing (% renewable)', inputKind: 'number' },
  { category: 'Carbon', name: 'Transport & logistics efficiency', inputKind: 'select', options: [
    { label: 'No evidence', value: 0 },
    { label: 'Some evidence', value: 5 },
    { label: 'Clear evidence', value: 10 }
  ] },
  // Longevity
  { category: 'Longevity', name: 'Durability testing / expected lifetime', inputKind: 'select', options: [
    { label: 'No evidence', value: 0 },
    { label: 'Partial evidence', value: 5 },
    { label: 'Clear evidence', value: 10 }
  ] },
  { category: 'Longevity', name: 'Repairability & spare parts', inputKind: 'select', options: [
    { label: 'No evidence', value: 0 },
    { label: 'Partial evidence', value: 5 },
    { label: 'Clear evidence', value: 10 }
  ] },
  { category: 'Longevity', name: 'Design for timelessness / modularity', inputKind: 'select', options: [
    { label: 'No evidence', value: 0 },
    { label: 'Partial evidence', value: 5 },
    { label: 'Clear evidence', value: 10 }
  ] },
  { category: 'Longevity', name: 'Care instructions & user guidance', inputKind: 'select', options: [
    { label: 'No evidence', value: 0 },
    { label: 'Partial evidence', value: 5 },
    { label: 'Clear evidence', value: 10 }
  ] }
]

export default function AdminBrandForm({ mode, brandId }: { mode: Mode; brandId?: number }) {
  const [form, setForm] = useState<Brand>(createEmptyBrand())
  const [error, setError] = useState<string | null>(null)
  const [loading, setLoading] = useState(mode === 'edit')

  const title = mode === 'create' ? 'Add new brand' : 'Edit brand'
  const subtitle = mode === 'create'
    ? 'Fill in the fixed subcriteria for each category.'
    : 'Update the fixed subcriteria without changing the dashboard layout.'

  const groupedCriteria = useMemo(() => {
    return categoryValues.map(category => ({
      category,
      items: DEFAULT_CRITERIA.filter(item => item.category === category)
    }))
  }, [])

  useEffect(() => {
    if (mode !== 'edit' || !brandId) {
      setLoading(false)
      return
    }

    fetch(`/brands/${brandId}`)
      .then(response => response.ok ? response.json() : null)
      .then(data => {
        if (data) {
          // Merge existing criteria with defaults so fixed subcriteria are always present
          const existing: CriterionItem[] = data.criteriaItems ?? []
          const merged = DEFAULT_CRITERIA.map(def => {
            const found = existing.find((c: CriterionItem) => c.category === def.category && c.name === def.name)
            return found ? { ...found } : {
              category: def.category,
              name: def.name,
              numericValue: undefined,
              unit: '',
              goodThreshold: undefined,
              warningThreshold: undefined,
              lowerIsBetter: true,
              weight: 1,
              notes: ''
            }
          })

          setForm({
            ...createEmptyBrand(),
            ...data,
            evidenceSources: data.evidenceSources ?? [],
            criteriaItems: merged
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

  function updateCriterion(index: number, patch: Partial<CriterionItem>) {
    const criteriaItems = [...(form.criteriaItems ?? [])]
    criteriaItems[index] = { ...criteriaItems[index], ...patch }
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
        </section>

        <section>
          <h3>Scoring rubric</h3>
          <p>Fixed subcriteria are provided below for each category. Some boxes are numeric, others are simple dropdowns. Sources are attached to the brand (not per criterion).</p>
          {groupedCriteria.map(group => (
            <div key={group.category} style={{ marginTop: 18, border: '1px solid #e5e5e5', borderRadius: 12, padding: 16 }}>
              <h4>{categoryLabels[categoryValues.indexOf(group.category as (typeof categoryValues)[number])]}</h4>

              <div style={{ display: 'grid', gridTemplateColumns: 'repeat(auto-fit, minmax(190px, 1fr))', gap: 12 }}>
                {group.items.map(def => {
                  const index = (form.criteriaItems ?? []).findIndex(candidate => candidate.category === def.category && candidate.name === def.name)
                  const item = (form.criteriaItems ?? [])[index]

                  const inputValue = item?.numericValue ?? ''
                  const updateNumericValue = (value: string) => {
                    updateCriterion(index, { numericValue: value ? Number(value) : undefined })
                  }

                return (
                  <div key={`${def.category}-${def.name}`} style={{ padding: 12, background: '#fafafa', borderRadius: 10, minHeight: 120 }}>
                    <div style={{ marginBottom: 8 }}>
                      <label style={{ display: 'block', fontSize: 13, color: '#555' }}>{def.name}</label>
                    </div>

                    {def.inputKind === 'number' ? (
                      <input
                        type="number"
                        min="0"
                        max="100"
                        step="0.1"
                        value={inputValue}
                        onChange={e => updateNumericValue(e.target.value)}
                        placeholder="Enter % or value"
                      />
                    ) : (
                      <select
                        value={inputValue}
                        onChange={e => updateNumericValue(e.target.value)}
                      >
                        <option value="">Select one</option>
                        {def.options?.map(option => (
                          <option key={option.value} value={option.value}>{option.label}</option>
                        ))}
                      </select>
                    )}
                  </div>
                )
                })}
              </div>
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

        {error && <p style={{ color: 'red' }}>{error}</p>}

        <div style={{ display: 'flex', gap: 12 }}>
          <button type="submit">{mode === 'create' ? 'Create brand' : 'Save changes'}</button>
          <button type="button" onClick={backToDashboard}>Cancel</button>
        </div>
      </form>
    </div>
  )
}