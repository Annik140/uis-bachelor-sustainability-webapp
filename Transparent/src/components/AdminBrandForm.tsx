import { useEffect, useMemo, useRef, useState } from 'react'
import './AdminBrandForm.css'
import { clearCsrfToken, withCsrfHeaders } from '../utils/csrf'

type Mode = 'create' | 'edit'

type EvidenceSource = {
  sourceTitle: string
  sourceUrl: string
  sourceType?: string
  publishedAtUtc?: string
  notes?: string
}

type Certification = {
  id?: number
  name: string
}

type CriterionItem = {
  category: string
  name: string
  numericValue?: number
  unit?: string
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
  logoPath?: string
  description?: string
  evidenceSourceCount?: number
  evidenceSources?: EvidenceSource[]
  criteriaItems?: CriterionItem[]
  certifications?: Certification[]
}

function serializeBrandForDirtyCheck(brand: Brand) {
  return JSON.stringify({
    brandName: brand.brandName ?? '',
    logoPath: brand.logoPath ?? '',
    description: brand.description ?? '',
    evidenceSources: (brand.evidenceSources ?? []).map(source => ({
      sourceTitle: source.sourceTitle ?? '',
      sourceUrl: source.sourceUrl ?? '',
      sourceType: source.sourceType ?? '',
      publishedAtUtc: source.publishedAtUtc ?? '',
      notes: source.notes ?? '',
    })),
    criteriaItems: (brand.criteriaItems ?? []).map(item => ({
      category: item.category ?? '',
      name: item.name ?? '',
      numericValue: item.numericValue ?? null,
      unit: item.unit ?? '',
      weight: item.weight ?? 1,
      notes: item.notes ?? '',
    })),
    certifications: (brand.certifications ?? []).map(certification => ({
      name: certification.name ?? '',
    })),
  })
}

const CERTIFICATION_OPTIONS = [
  'GOTS',
  'GRS',
  'OEKO-TEX',
  'bluesign',
  'FSC',
  'RWS',
  'RDS',
  'Fair Wear Foundation',
  'SA8000',
  'WRAP',
  'SBTi',
  'RE100',
  'B Corp',
]

const categoryLabels = [
  'Material sustainability',
  'Labor practices',
  'Carbon footprint',
  'Product longevity'
]

const categoryValues = ['Material', 'Labor', 'Carbon', 'Longevity'] as const

const createEmptyBrand = (): Brand => ({
  brandName: '',
  logoPath: '',
  description: '',
  evidenceSourceCount: 0,
  evidenceSources: [],
  certifications: [],
  criteriaItems: DEFAULT_CRITERIA.map(c => ({
    category: c.category,
    name: c.name,
    numericValue: undefined,
    unit: '',
    weight: 1,
    notes: ''
  })),
})

const DEFAULT_CRITERIA: CriterionDefinition[] = [
  // Material
  { category: 'Material', name: 'Fiber traceability', inputKind: 'select', options: [
    { label: 'No fiber traceability', value: 0 },
    { label: 'Tier 1 supplier traceability', value: 25 },
    { label: 'Tier 1-2 supplier traceability', value: 50 },
    { label: 'Tier 1–3 traceability', value: 75 },
    { label: 'Tier 1–4 / farm-level traceability', value: 100 }
  ] },
  { category: 'Material', name: 'Chemical management', inputKind: 'select', options: [
    { label: 'No chemical management disclosure', value: 0 },
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
    { label: 'No living wage disclosure', value: 0 },
    { label: 'Commitment only', value: 25 },
    { label: 'Pilot programs', value: 50 },
    { label: 'Partial documented coverage', value: 75 },
    { label: 'Majority coverage', value: 100 }
  ] },
  { category: 'Labor', name: 'Worker safety & working hours', inputKind: 'select', options: [
    { label: 'No worker safety disclosure', value: 0 },
    { label: 'Basic policy', value: 25 },
    { label: 'Audits conducted', value: 50 },
    { label: 'Performance metrics reported', value: 75 },
    { label: 'Strong verified safety performance', value: 100 }
  ] },
  { category: 'Labor', name: 'Freedom of association / grievance mechanisms', inputKind: 'select', options: [
    { label: 'No grievance mechanism or FOA disclosure', value: 0 },
    { label: 'Policy commitment only', value: 25 },
    { label: 'Grievance mechanism OR freedom of association policy disclosed', value: 50 },
    { label: 'Both grievance mechanism and freedom of association policy disclosed', value: 75 },
    { label: 'Evidence of usage, worker engagement, outcomes, or remediation reported', value: 100 }
  ] },
  { category: 'Labor', name: 'Supplier audit transparency', inputKind: 'select', options: [
    { label: 'No supplier audit disclosure', value: 0 },
    { label: 'States audits are conducted', value: 25 },
    { label: 'Describes audit process/frequency', value: 50 },
    { label: 'Publishes audit statistics or findings', value: 75 },
    { label: 'Publishes supplier lists, findings, corrective actions, and follow-up results', value: 100 }
  ] },
  // Carbon
  { category: 'Carbon', name: 'Scope 1-3 measurement', inputKind: 'select', options: [
    { label: 'No Scope 1-3 measurement', value: 0 },
    { label: 'Scope 1 only reported', value: 25 },
    { label: 'Scope 1-2 reported', value: 50 },
    { label: 'Scope 1-3 reported', value: 75 },
    { label: 'Scope 1-3 reported with methodology and historical comparison', value: 100 }
  ] },
  { category: 'Carbon', name: 'Reduction targets & progress', inputKind: 'select', options: [
    { label: 'No reduction targets', value: 0 },
    { label: 'General climate commitment', value: 25 },
    { label: 'Quantified emissions reduction targets', value: 50 },
    { label: 'Science-based targets (e.g. SBTi approved)', value: 75 },
    { label: 'Science-based targets with demonstrated progress toward targets', value: 100 }
  ] },
  { category: 'Carbon', name: 'Renewable energy', inputKind: 'number' },
  { category: 'Carbon', name: 'Transport & logistics', inputKind: 'select', options: [
    { label: 'No transport disclosure', value: 0 },
    { label: 'General efficiency initiatives mentioned', value: 25 },
    { label: 'Specific actions disclosed (route optimization, lower-carbon transport, etc.)', value: 50 },
    { label: 'Comprehensive logistics strategy with measurable targets', value: 75 },
    { label: 'Comprehensive strategy with demonstrated results and progress', value: 100 }
  ] },
  // Longevity
  { category: 'Longevity', name: 'Durability Testing / Expected Lifetime', inputKind: 'select', options: [
    { label: 'No durability or testing disclosure', value: 0 },
    { label: 'General durability claims', value: 25 },
    { label: 'Internal testing reported', value: 50 },
    { label: 'Standardized testing disclosed', value: 75 },
    { label: 'Standardized testing + results reported', value: 100 }
  ] },
  { category: 'Longevity', name: 'Repairability & Repair Services', inputKind: 'select', options: [
    { label: 'No repair support', value: 25 },
    { label: 'Repair information available', value: 50 },
    { label: 'Repair services OR repair program offered', value: 75 },
    { label: 'Repair services with measurable usage/results', value: 100 }
  ] },
  { category: 'Longevity', name: 'Circularity Programs', inputKind: 'select', options: [
    { label: 'No programs', value: 0 },
    { label: 'General commitment', value: 25 },
    { label: 'One active program', value: 50 },
    { label: 'Multiple active programs', value: 75 },
    { label: 'Multiple programs + measurable results', value: 100 }
  ] }
]

export default function AdminBrandForm({ mode, brandId }: { mode: Mode; brandId?: number }) {
  const [form, setForm] = useState<Brand>(createEmptyBrand())
  const [error, setError] = useState<string | null>(null)
  const [loading, setLoading] = useState(mode === 'edit')
  const [isUploadingLogo, setIsUploadingLogo] = useState(false)
  const [selectedLogoFile, setSelectedLogoFile] = useState<File | null>(null)
  const [selectedLogoPreviewUrl, setSelectedLogoPreviewUrl] = useState<string | null>(null)
  const [isLogoPreviewBroken, setIsLogoPreviewBroken] = useState(false)
  const [isSourceModalOpen, setIsSourceModalOpen] = useState(false)
  const [editingSourceIndex, setEditingSourceIndex] = useState<number | null>(null)
  const [sourceTitleInput, setSourceTitleInput] = useState('')
  const [sourceUrlInput, setSourceUrlInput] = useState('')
  const logoFileInputRef = useRef<HTMLInputElement | null>(null)
  const initialFormSnapshotRef = useRef<string>(serializeBrandForDirtyCheck(createEmptyBrand()))

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

  const hasUnsavedChanges = useMemo(() => {
    return serializeBrandForDirtyCheck(form) !== initialFormSnapshotRef.current || selectedLogoFile !== null
  }, [form, selectedLogoFile])

  useEffect(() => {
    if (!selectedLogoFile) {
      setSelectedLogoPreviewUrl(null)
      return
    }

    const previewUrl = URL.createObjectURL(selectedLogoFile)
    setSelectedLogoPreviewUrl(previewUrl)

    return () => URL.revokeObjectURL(previewUrl)
  }, [selectedLogoFile])

  useEffect(() => {
    if (mode !== 'edit' || !brandId) {
      return
    }

    const loadBrand = async () => {
      try {
        setError(null)
        const response = await fetch(`/admin/clothingbrands/${brandId}`, { credentials: 'include' })
        
        if (response.status === 401) {
          window.location.href = '/admin/login'
          return
        }

        if (!response.ok) {
          if (response.status === 404) {
            setError('Brand not found.')
          } else {
            setError(`Failed to load brand (HTTP ${response.status}). Please try again.`)
          }
          setLoading(false)
          return
        }

        const data = await response.json().catch(() => {
          throw new Error('Invalid response format from server.')
        })

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
              weight: 1,
              notes: ''
            }
          })

          setForm({
            ...createEmptyBrand(),
            ...data,
            evidenceSources: data.evidenceSources ?? [],
            certifications: data.certifications ?? [],
            criteriaItems: merged
          })

          initialFormSnapshotRef.current = serializeBrandForDirtyCheck({
            ...createEmptyBrand(),
            ...data,
            evidenceSources: data.evidenceSources ?? [],
            certifications: data.certifications ?? [],
            criteriaItems: merged
          })
        }
      } catch (err) {
        const errorMessage = err instanceof Error ? err.message : 'An unexpected error occurred while loading the brand.'
        setError(errorMessage)
        console.error('Brand load error:', err)
      } finally {
        setLoading(false)
      }
    }

    loadBrand()
  }, [brandId, mode])

  function backToDashboard() {
    if (hasUnsavedChanges) {
      const shouldLeave = window.confirm('You have unsaved changes. Are you sure you want to leave this page?')
      if (!shouldLeave) {
        return
      }
    }

    window.location.href = '/admin/dashboard'
  }

  function updateBrand(patch: Partial<Brand>) {
    setForm({ ...form, ...patch })
  }

  useEffect(() => {
    setIsLogoPreviewBroken(false)
  }, [form.logoPath])

  useEffect(() => {
    setIsLogoPreviewBroken(false)
  }, [selectedLogoPreviewUrl])

  function isWebLink(value: string) {
    if (!value) return false
    try {
      const url = new URL(value)
      return url.protocol === 'http:' || url.protocol === 'https:'
    } catch {
      return false
    }
  }

  function openAddSourceModal() {
    setEditingSourceIndex(null)
    setSourceTitleInput('')
    setSourceUrlInput('')
    setIsSourceModalOpen(true)
  }

  function openEditSourceModal(index: number) {
    const source = (form.evidenceSources ?? [])[index]
    if (!source) return
    setEditingSourceIndex(index)
    setSourceTitleInput(source.sourceTitle ?? '')
    setSourceUrlInput(source.sourceUrl ?? '')
    setIsSourceModalOpen(true)
  }

  function saveSourceFromModal() {
    const title = sourceTitleInput.trim()
    const sourceUrl = sourceUrlInput.trim()
    if (!title || !sourceUrl || !isWebLink(sourceUrl)) return

    const sourceRecord: EvidenceSource = {
      sourceTitle: title,
      sourceUrl,
      sourceType: '',
      notes: ''
    }

    const evidenceSources = [...(form.evidenceSources ?? [])]
    if (editingSourceIndex === null) {
      evidenceSources.push(sourceRecord)
    } else {
      evidenceSources[editingSourceIndex] = {
        ...evidenceSources[editingSourceIndex],
        ...sourceRecord
      }
    }

    updateBrand({ evidenceSources })
    setIsSourceModalOpen(false)
    setEditingSourceIndex(null)
    setSourceTitleInput('')
    setSourceUrlInput('')
  }

  function closeSourceModal() {
    setIsSourceModalOpen(false)
    setEditingSourceIndex(null)
    setSourceTitleInput('')
    setSourceUrlInput('')
  }

  function removeEvidenceSource(index: number) {
    const evidenceSources = [...(form.evidenceSources ?? [])]
    evidenceSources.splice(index, 1)
    updateBrand({ evidenceSources })
  }

  function toggleCertification(name: string, checked: boolean) {
    const current = [...(form.certifications ?? [])]

    if (checked) {
      if (!current.some(item => item.name.toLowerCase() === name.toLowerCase())) {
        current.push({ name })
      }
    } else {
      const next = current.filter(item => item.name.toLowerCase() !== name.toLowerCase())
      updateBrand({ certifications: next })
      return
    }

    updateBrand({ certifications: current })
  }

  function updateCriterion(index: number, patch: Partial<CriterionItem>) {
    const criteriaItems = [...(form.criteriaItems ?? [])]
    criteriaItems[index] = { ...criteriaItems[index], ...patch }
    updateBrand({ criteriaItems })
  }

  async function uploadLogo(file: File) {
    setError(null)
    setIsUploadingLogo(true)

    try {
      const formData = new FormData()
      formData.append('file', file)
      const headers = await withCsrfHeaders()
      const response = await fetch('/admin/upload-logo', {
        method: 'POST',
        credentials: 'include',
        headers,
        body: formData
      })

      if (response.status === 401) {
        clearCsrfToken()
        window.location.href = '/admin/login'
        return
      }

      const data = await response.json().catch(() => null) as { logoPath?: string; message?: string } | null
      if (!response.ok || !data?.logoPath) {
        setError(data?.message ?? 'Failed to upload logo.')
        return null
      }

      return data.logoPath
    } catch {
      setError('Failed to upload logo. Please try again.')
      return null
    } finally {
      setIsUploadingLogo(false)
    }
  }

  function openLogoFilePicker() {
    logoFileInputRef.current?.click()
  }

  async function handleSubmit(e: React.FormEvent) {
    e.preventDefault()
    setError(null)

    try {
      const previousLogoPath = form.logoPath?.trim() ?? ''
      let finalLogoPath = previousLogoPath

      if (selectedLogoFile) {
        const uploadedLogoPath = await uploadLogo(selectedLogoFile)
        if (!uploadedLogoPath) {
          return
        }
        finalLogoPath = uploadedLogoPath
      }

      const payload = {
        ...form,
        logoPath: finalLogoPath
      }

      const headers = await withCsrfHeaders({ 'Content-Type': 'application/json' })
      const response = mode === 'create'
        ? await fetch('/admin/clothingbrands', {
            method: 'POST',
            credentials: 'include',
            headers,
            body: JSON.stringify(payload)
          })
        : await fetch(`/admin/clothingbrands/${brandId}`, {
            method: 'PUT',
            credentials: 'include',
            headers,
            body: JSON.stringify(payload)
          })

        if (response.status === 401) {
          clearCsrfToken()
          window.location.href = '/admin/login'
          return
        }

      if (response.ok) {
        window.location.href = '/admin/dashboard'
        return
      }

      const contentType = response.headers.get('content-type') ?? ''
      let details = ''

      try {
        if (contentType.includes('application/json')) {
          const data = await response.json()
          details = data?.detail ?? data?.title ?? data?.message ?? JSON.stringify(data)
        } else {
          details = (await response.text()).trim()
        }
      } catch {
        details = ''
      }

      const action = mode === 'create' ? 'Could not create brand' : 'Could not save changes'
      setError(details ? `${action}: ${details}` : action)
    } catch {
      const action = mode === 'create' ? 'Could not create brand' : 'Could not save changes'
      setError(`${action} — network error. Check that the backend is running and try again.`)
    }
  }

  if (loading) {
    return <div className="admin-brand-form-loading">Loading brand...</div>
  }

  if (error && mode === 'edit') {
    return (
      <div className="admin-brand-form-page admin-brand-form-page-edit">
        <div className="admin-brand-form-header">
          <div className="admin-brand-form-header-copy">
            <h2>{title}</h2>
            <p>{subtitle}</p>
          </div>
          <button type="button" onClick={backToDashboard} className="admin-brand-form-btn admin-brand-form-btn-ghost">Back to admin dashboard</button>
        </div>
        <div className="admin-brand-form-error-banner">
          <strong>Error loading brand:</strong> {error}
        </div>
      </div>
    )
  }

  return (
    <div className={`admin-brand-form-page ${mode === 'create' ? 'admin-brand-form-page-create' : 'admin-brand-form-page-edit'}`}>
      <div className="admin-brand-form-header">
        <div className="admin-brand-form-header-copy">
          <h2>{title}</h2>
          <p>{subtitle}</p>
        </div>
        <button type="button" onClick={backToDashboard} className="admin-brand-form-btn admin-brand-form-btn-ghost">Back to admin dashboard</button>
      </div>

      {error && (
        <div className="admin-brand-form-error-banner">
          {error}
          <button type="button" onClick={() => setError(null)} className="admin-brand-form-error-close">✕</button>
        </div>
      )}

      <form onSubmit={handleSubmit} className="admin-brand-form-shell">
        <section className="admin-brand-form-section">
          <h3>Brand details</h3>
          <div className="admin-brand-form-brand-details-grid">
            <div className="admin-brand-form-brand-copy-column">
              <div className="admin-brand-form-field">
                <label>Brand name</label>
                <input className="admin-brand-form-control" required value={form.brandName} onChange={e => updateBrand({ brandName: e.target.value })} />
              </div>

              <div className="admin-brand-form-field admin-brand-form-field-spaced admin-brand-form-description-field">
                <label>Description</label>
                <textarea
                  className="admin-brand-form-control admin-brand-form-description-control"
                  value={form.description ?? ''}
                  onChange={e => updateBrand({ description: e.target.value })}
                  rows={3}
                  placeholder="Optional description shown on the public brand card and brand page."
                />
              </div>
            </div>

            <div className="admin-brand-form-logo-column">
              <div className="admin-brand-form-logo-heading">
                <label>Brand logo (optional)</label>
                {form.logoPath?.trim() && (
                  <button
                    type="button"
                    className="admin-brand-form-btn admin-brand-form-btn-ghost"
                    onClick={() => {
                      setSelectedLogoFile(null)
                      updateBrand({ logoPath: '' })
                    }}
                  >
                    Remove logo
                  </button>
                )}
              </div>
              <input
                ref={logoFileInputRef}
                className="admin-brand-form-logo-file-input"
                type="file"
                accept=".png,.jpg,.jpeg,.webp,image/png,image/jpeg,image/webp"
                disabled={isUploadingLogo}
                onChange={e => {
                  const file = e.target.files?.[0]
                  e.currentTarget.value = ''
                  if (file) {
                    setSelectedLogoFile(file)
                  }
                }}
              />

              <div
                className={`admin-brand-form-logo-dropzone ${form.logoPath?.trim() ? 'admin-brand-form-logo-dropzone-has-preview' : ''}`}
                tabIndex={0}
                role="button"
                aria-label="Click to upload a brand photo"
                onClick={openLogoFilePicker}
                onKeyDown={event => {
                  if (event.key === 'Enter' || event.key === ' ') {
                    event.preventDefault()
                    openLogoFilePicker()
                  }
                }}
              >
                {!form.logoPath?.trim() && (
                  <span className="admin-brand-form-logo-upload-text">Click to upload photo</span>
                )}
                {isUploadingLogo && <p className="admin-brand-form-muted">Uploading logo...</p>}
                {(selectedLogoPreviewUrl || form.logoPath?.trim()) && !isLogoPreviewBroken && (
                  <div className="admin-brand-form-logo-preview">
                    <img
                      src={selectedLogoPreviewUrl ?? form.logoPath}
                      alt=""
                      onError={() => setIsLogoPreviewBroken(true)}
                    />
                  </div>
                )}
                {(selectedLogoPreviewUrl || form.logoPath?.trim()) && isLogoPreviewBroken && (
                  <p className="admin-brand-form-muted">Could not load logo preview. You can still save and verify on the card.</p>
                )}
              </div>
            </div>
          </div>
        </section>

        <section className="admin-brand-form-section">
          <h3>Scoring rubric</h3>
          <p className="admin-brand-form-muted">Fixed subcriteria are provided below for each category. Some boxes are numeric, others are simple dropdowns. Sources are attached to the brand (not per criterion).</p>
          {groupedCriteria.map(group => (
            <div key={group.category} className="admin-brand-form-category-block">
              <h4>{categoryLabels[categoryValues.indexOf(group.category as (typeof categoryValues)[number])]}</h4>

              <div className="admin-brand-form-criteria-grid">
                {group.items.map(def => {
                  const index = (form.criteriaItems ?? []).findIndex(candidate => candidate.category === def.category && candidate.name === def.name)
                  const item = (form.criteriaItems ?? [])[index]

                  const inputValue = item?.numericValue ?? ''
                  const updateNumericValue = (value: string) => {
                    updateCriterion(index, { numericValue: value ? Number(value) : undefined })
                  }

                  return (
                  <div key={`${def.category}-${def.name}`} className="admin-brand-form-criterion-card">
                    <div className="admin-brand-form-criterion-label-wrap">
                      <label className="admin-brand-form-criterion-label">{def.name}</label>
                    </div>

                    {def.inputKind === 'number' ? (
                      <input
                        className="admin-brand-form-control"
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
                        className="admin-brand-form-control"
                        value={inputValue}
                        onChange={e => updateNumericValue(e.target.value)}
                      >
                        <option value="">Information not found</option>
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

        <section className="admin-brand-form-section">
          <h3>Certifications</h3>
          <p className="admin-brand-form-muted">Select all certifications that apply. These are displayed on the brand page and do not affect score calculations.</p>
          <div className="admin-brand-form-cert-grid">
            {CERTIFICATION_OPTIONS.map(certification => {
              const isChecked = (form.certifications ?? []).some(item => item.name.toLowerCase() === certification.toLowerCase())
              return (
                <label
                  key={certification}
                  className="admin-brand-form-cert-item"
                >
                  <input
                    type="checkbox"
                    checked={isChecked}
                    onChange={e => toggleCertification(certification, e.target.checked)}
                  />
                  <span>{certification}</span>
                </label>
              )
            })}
          </div>
        </section>

        <section className="admin-brand-form-section">
          <h3>Evidence sources</h3>
          <button type="button" onClick={openAddSourceModal} className="admin-brand-form-btn admin-brand-form-btn-ghost">Add source</button>

          {(form.evidenceSources ?? []).length === 0 ? (
            <p className="admin-brand-form-muted">No sources added yet.</p>
          ) : (
            <ul className="admin-brand-form-source-list">
              {(form.evidenceSources ?? []).map((source, index) => (
                <li key={`${source.sourceTitle}-${index}`}>
                  {isWebLink(source.sourceUrl) ? (
                    <a href={source.sourceUrl} target="_blank" rel="noreferrer">{source.sourceTitle}</a>
                  ) : (
                    <span>{source.sourceTitle}</span>
                  )}
                  <span className="admin-brand-form-source-actions">
                    <button type="button" onClick={() => openEditSourceModal(index)} className="admin-brand-form-btn admin-brand-form-btn-ghost">Edit</button>
                    <button type="button" onClick={() => removeEvidenceSource(index)} className="admin-brand-form-btn admin-brand-form-btn-danger">Delete</button>
                  </span>
                </li>
              ))}
            </ul>
          )}
        </section>

        {error && <p className="admin-brand-form-error">{error}</p>}

        <div className="admin-brand-form-actions">
          <button type="submit" className="admin-brand-form-btn admin-brand-form-btn-primary">{mode === 'create' ? 'Create brand' : 'Save changes'}</button>
          <button type="button" onClick={backToDashboard} className="admin-brand-form-btn admin-brand-form-btn-ghost">Cancel</button>
        </div>
      </form>

      {isSourceModalOpen && (
        <div className="admin-brand-form-modal-overlay">
          <div className="admin-brand-form-modal">
            <h3>{editingSourceIndex === null ? 'Add source' : 'Edit source'}</h3>
            <label>Source title</label>
            <input
              className="admin-brand-form-control"
              autoFocus
              value={sourceTitleInput}
              onChange={e => setSourceTitleInput(e.target.value)}
              placeholder="Name shown in the evidence list"
            />
            <label>Source URL</label>
            <input
              className="admin-brand-form-control"
              value={sourceUrlInput}
              onChange={e => setSourceUrlInput(e.target.value)}
              placeholder="https://example.com/report"
            />
            <p className="admin-brand-form-modal-note">
              Source URL must be an absolute http/https link.
            </p>
            <div className="admin-brand-form-modal-actions">
              <button type="button" onClick={closeSourceModal} className="admin-brand-form-btn admin-brand-form-btn-ghost">Cancel</button>
              <button type="button" onClick={saveSourceFromModal} disabled={!sourceTitleInput.trim() || !isWebLink(sourceUrlInput.trim())} className="admin-brand-form-btn admin-brand-form-btn-primary">
                {editingSourceIndex === null ? 'Add' : 'Save'}
              </button>
            </div>
          </div>
        </div>
      )}
    </div>
  )
}