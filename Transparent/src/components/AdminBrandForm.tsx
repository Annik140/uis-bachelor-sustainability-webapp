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
  { category: 'Material', name: 'Fiber traceability', inputKind: 'select', options: [
    { label: 'No disclosure', value: 0 },
    { label: 'General supplier disclosure', value: 25 },
    { label: 'Tier 1 supplier traceability', value: 50 },
    { label: 'Tier 1–2 traceability', value: 75 },
    { label: 'Tier 1–4 / farm-level traceability', value: 100 }
  ] },
  { category: 'Material', name: 'Chemical management', inputKind: 'select', options: [
    { label: 'No disclosure', value: 0 },
    { label: 'Chemical policy only', value: 25 },
    { label: 'Restricted Substance List (RSL) or testing program', value: 50 },
    { label: 'Uses recognized standards (ZDHC, bluesign, OEKO-TEX)', value: 75 },
    { label: 'Verified chemical management with public results/progress', value: 100 }
  ] },
  { category: 'Material', name: 'Recycled content / Preferred material content', inputKind: 'number' },
  { category: 'Material', name: 'Certifications', inputKind: 'select', options: [
    { label: 'None', value: 0 },
    { label: 'One relevant third-party certification', value: 35 },
    { label: 'Multiple relevant certifications', value: 70 },
    { label: 'Multiple certifications with broad coverage', value: 100 }
  ] },
  // Labor
  { category: 'Labor', name: 'Living wage commitment & coverage', inputKind: 'select', options: [
    { label: 'No disclosure', value: 0 },
    { label: 'Commitment only', value: 25 },
    { label: 'Pilot programs', value: 50 },
    { label: 'Partial documented coverage', value: 75 },
    { label: 'Majority coverage', value: 100 }
  ] },
  { category: 'Labor', name: 'Worker safety & working hours', inputKind: 'select', options: [
    { label: 'No disclosure', value: 0 },
    { label: 'Basic policy', value: 25 },
    { label: 'Audits conducted', value: 50 },
    { label: 'Performance metrics reported', value: 75 },
    { label: 'Strong verified safety performance', value: 100 }
  ] },
  { category: 'Labor', name: 'Freedom of association / grievance mechanisms', inputKind: 'select', options: [
    { label: 'No disclosure', value: 0 },
    { label: 'Policy commitment only', value: 25 },
    { label: 'Grievance mechanism OR freedom of association policy disclosed', value: 50 },
    { label: 'Both grievance mechanism and freedom of association policy disclosed', value: 75 },
    { label: 'Evidence of usage, worker engagement, outcomes, or remediation reported', value: 100 }
  ] },
  { category: 'Labor', name: 'Supplier audit transparency', inputKind: 'select', options: [
    { label: 'No disclosure', value: 0 },
    { label: 'States audits are conducted', value: 25 },
    { label: 'Describes audit process/frequency', value: 50 },
    { label: 'Publishes audit statistics or findings', value: 75 },
    { label: 'Publishes supplier lists, findings, corrective actions, and follow-up results', value: 100 }
  ] },
  // Carbon
  { category: 'Carbon', name: 'Scope 1-3 measurement', inputKind: 'select', options: [
    { label: 'No emissions reporting', value: 0 },
    { label: 'Scope 1 only reported', value: 25 },
    { label: 'Scope 1-2 reported', value: 50 },
    { label: 'Scope 1-3 reported', value: 75 },
    { label: 'Scope 1-3 reported with methodology and historical comparison', value: 100 }
  ] },
  { category: 'Carbon', name: 'Reduction targets & progress', inputKind: 'select', options: [
    { label: 'No targets disclosed', value: 0 },
    { label: 'General climate commitment', value: 25 },
    { label: 'Quantified emissions reduction targets', value: 50 },
    { label: 'Science-based targets (e.g. SBTi approved)', value: 75 },
    { label: 'Science-based targets with demonstrated progress toward targets', value: 100 }
  ] },
  { category: 'Carbon', name: 'Renewable energy', inputKind: 'number' },
  { category: 'Carbon', name: 'Transport & logistics', inputKind: 'select', options: [
    { label: 'No disclosure', value: 0 },
    { label: 'General efficiency initiatives mentioned', value: 25 },
    { label: 'Specific actions disclosed (route optimization, lower-carbon transport, etc.)', value: 50 },
    { label: 'Comprehensive logistics strategy with measurable targets', value: 75 },
    { label: 'Comprehensive strategy with demonstrated results and progress', value: 100 }
  ] },
  // Longevity
  { category: 'Longevity', name: 'Durability Testing / Expected Lifetime', inputKind: 'select', options: [
    { label: 'No disclosure', value: 0 },
    { label: 'General durability claims', value: 25 },
    { label: 'Internal testing reported', value: 50 },
    { label: 'Standardized testing disclosed', value: 75 },
    { label: 'Standardized testing + results reported', value: 100 }
  ] },
  { category: 'Longevity', name: 'Repairability & Repair Services', inputKind: 'select', options: [
    { label: 'No repair support', value: 0 },
    { label: 'Basic repair information', value: 25 },
    { label: 'Repair guidance available', value: 50 },
    { label: 'Repair services offered', value: 75 },
    { label: 'Comprehensive repair ecosystem', value: 100 }
  ] },
  { category: 'Longevity', name: 'Circularity Programs', inputKind: 'select', options: [
    { label: 'No programs', value: 0 },
    { label: 'General commitment', value: 25 },
    { label: 'One active program', value: 50 },
    { label: 'Multiple active programs', value: 75 },
    { label: 'Multiple programs + measurable results', value: 100 }
  ] },
  { category: 'Longevity', name: 'Care Instructions & User Guidance', inputKind: 'select', options: [
    { label: 'No guidance', value: 0 },
    { label: 'Standard care instructions', value: 50 },
    { label: 'Extended longevity guidance', value: 100 }
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
                        type="text"
                        inputMode="decimal"
                        value={inputValue}
                        onChange={e => {
                          const raw = e.target.value
                          if (raw === '' || /^\d*(?:[.,]\d*)?$/.test(raw)) {
                            updateNumericValue(raw.replace(',', '.'))
                          }
                        }}
                        placeholder="Enter %"
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