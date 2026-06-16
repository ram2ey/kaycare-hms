import { useEffect, useState } from 'react'
import { useNavigate, useSearchParams } from 'react-router-dom'
import { getProcedureCatalog, createRadiologyOrder } from '../../api/radiology'
import { searchPatients, getPatient } from '../../api/patients'
import { useDebounce } from '../../hooks/useDebounce'
import type { ImagingProcedureItem } from '../../types/radiology'
import { PRIORITY_OPTIONS } from '../../types/radiology'

export function NewRadiologyOrderPage() {
  const navigate = useNavigate()
  const [searchParams] = useSearchParams()
  const patientIdParam = searchParams.get('patientId') ?? ''

  const [catalog, setCatalog] = useState<ImagingProcedureItem[]>([])
  const [selectedProcedures, setSelectedProcedures] = useState<ImagingProcedureItem[]>([])
  const [catalogSearch, setCatalogSearch] = useState('')
  const [patientId, setPatientId] = useState(patientIdParam)
  const [patientName, setPatientName] = useState('')
  const [patientQuery, setPatientQuery] = useState('')
  const [suggestions, setSuggestions] = useState<{ patientId: string; name: string; mrn: string }[]>([])
  const [priority, setPriority] = useState('Routine')
  const [clinicalIndication, setClinicalIndication] = useState('')
  const [notes, setNotes] = useState('')
  const [error, setError] = useState('')
  const [loading, setLoading] = useState(false)

  const debouncedQuery = useDebounce(patientQuery, 250)

  useEffect(() => {
    getProcedureCatalog().then(setCatalog)
    if (patientIdParam) {
      getPatient(patientIdParam).then((p) => {
        setPatientName(`${p.fullName} (${p.medicalRecordNumber})`)
      })
    }
  }, [patientIdParam])

  useEffect(() => {
    if (debouncedQuery.length < 2) {
      setSuggestions([])
      return
    }
    if (patientId) return // Selected

    searchPatients({ query: debouncedQuery, page: 1, pageSize: 6 }).then((res) => {
      setSuggestions(
        res.items.map((p) => ({
          patientId: p.patientId,
          name: p.fullName,
          mrn: p.medicalRecordNumber,
        }))
      )
    })
  }, [debouncedQuery, patientId])

  function toggleProcedure(p: ImagingProcedureItem) {
    setSelectedProcedures((prev) =>
      prev.find((x) => x.imagingProcedureId === p.imagingProcedureId)
        ? prev.filter((x) => x.imagingProcedureId !== p.imagingProcedureId)
        : [...prev, p],
    )
  }

  async function handleSubmit(e: React.FormEvent) {
    e.preventDefault()
    if (!patientId) { setError('Please select a patient.'); return }
    if (selectedProcedures.length === 0) { setError('Please select at least one procedure.'); return }
    setError('')
    setLoading(true)
    try {
      const order = await createRadiologyOrder({
        patientId,
        priority,
        clinicalIndication: clinicalIndication || undefined,
        notes: notes || undefined,
        procedureIds: selectedProcedures.map((p) => p.imagingProcedureId),
      })
      navigate(`/radiology/${order.radiologyOrderId}`)
    } catch (err: unknown) {
      const msg = (err as { response?: { data?: { message?: string } } })?.response?.data?.message
      setError(msg ?? 'Failed to create radiology order.')
    } finally {
      setLoading(false)
    }
  }

  const filteredCatalog = catalogSearch
    ? catalog.filter((p) =>
        p.procedureName.toLowerCase().includes(catalogSearch.toLowerCase()) ||
        p.modality.toLowerCase().includes(catalogSearch.toLowerCase()) ||
        p.bodyPart.toLowerCase().includes(catalogSearch.toLowerCase()),
      )
    : catalog

  const byModality = filteredCatalog.reduce<Record<string, ImagingProcedureItem[]>>((acc, p) => {
    ;(acc[p.modality] ??= []).push(p)
    return acc
  }, {})

  const MODALITY_LABELS: Record<string, string> = {
    XR: 'X-Ray', US: 'Ultrasound', CT: 'CT Scan', MRI: 'MRI', MG: 'Mammography',
  }

  return (
    <div className="p-6 max-w-3xl">
      <div className="mb-6">
        <button onClick={() => navigate(-1)} className="text-sm text-gray-500 hover:text-gray-700 mb-2">← Back</button>
        <h1 className="text-2xl font-bold text-gray-900">New Radiology Order</h1>
      </div>

      <form onSubmit={handleSubmit} className="space-y-6">
        {/* Patient */}
        <div className="bg-white border border-gray-200 rounded-xl p-5">
          <h2 className="font-semibold text-gray-700 mb-3">Patient</h2>
          {patientId && patientName ? (
            <div className="flex items-center justify-between">
              <p className="text-sm font-medium text-gray-800">{patientName}</p>
              <button
                type="button"
                onClick={() => { setPatientId(''); setPatientName(''); setPatientQuery('') }}
                className="text-xs text-red-500 hover:underline"
              >
                Change
              </button>
            </div>
          ) : (
            <div className="relative">
              <input
                type="text"
                placeholder="Search patient…"
                value={patientQuery}
                onChange={(e) => setPatientQuery(e.target.value)}
                className="w-full border border-gray-300 rounded-lg px-3 py-2 text-sm focus:outline-none focus:ring-2 focus:ring-sky-500"
              />
              {suggestions.length > 0 && (
                <div className="absolute top-full mt-1 w-full bg-white border border-gray-200 rounded-lg shadow-lg z-10">
                  {suggestions.map((s) => (
                    <button
                      key={s.patientId}
                      type="button"
                      onClick={() => {
                        setPatientId(s.patientId)
                        setPatientName(`${s.name} (${s.mrn})`)
                        setPatientQuery(`${s.name} (${s.mrn})`)
                        setSuggestions([])
                      }}
                      className="w-full text-left px-3 py-2 text-sm hover:bg-gray-50 border-b border-gray-50 last:border-0"
                    >
                      <span className="font-medium">{s.name}</span>{' '}
                      <span className="text-gray-400 font-mono text-xs">{s.mrn}</span>
                    </button>
                  ))}
                </div>
              )}
            </div>
          )}
        </div>

        {/* Order details */}
        <div className="bg-white border border-gray-200 rounded-xl p-5">
          <h2 className="font-semibold text-gray-700 mb-3">Order Details</h2>
          <div className="grid grid-cols-2 gap-4">
            <div>
              <label className="block text-sm font-medium text-gray-700 mb-1">Priority</label>
              <select
                value={priority}
                onChange={(e) => setPriority(e.target.value)}
                className="w-full border border-gray-300 rounded-lg px-3 py-2 text-sm focus:outline-none focus:ring-2 focus:ring-sky-500"
              >
                {PRIORITY_OPTIONS.map((p) => <option key={p}>{p}</option>)}
              </select>
            </div>
            <div>
              <label className="block text-sm font-medium text-gray-700 mb-1">Clinical Indication</label>
              <input
                value={clinicalIndication}
                onChange={(e) => setClinicalIndication(e.target.value)}
                placeholder="Optional"
                className="w-full border border-gray-300 rounded-lg px-3 py-2 text-sm focus:outline-none focus:ring-2 focus:ring-sky-500"
              />
            </div>
          </div>
          <div className="mt-3">
            <label className="block text-sm font-medium text-gray-700 mb-1">Notes</label>
            <input
              value={notes}
              onChange={(e) => setNotes(e.target.value)}
              placeholder="Optional"
              className="w-full border border-gray-300 rounded-lg px-3 py-2 text-sm focus:outline-none focus:ring-2 focus:ring-sky-500"
            />
          </div>
        </div>

        {/* Procedure selection */}
        <div className="bg-white border border-gray-200 rounded-xl p-5">
          <div className="flex items-center justify-between mb-3">
            <h2 className="font-semibold text-gray-700">Select Procedures</h2>
            {selectedProcedures.length > 0 && (
              <span className="text-xs bg-sky-100 text-sky-700 px-2 py-0.5 rounded-full font-medium">
                {selectedProcedures.length} selected
              </span>
            )}
          </div>
          <input
            type="text"
            placeholder="Search procedures…"
            value={catalogSearch}
            onChange={(e) => setCatalogSearch(e.target.value)}
            className="w-full border border-gray-300 rounded-lg px-3 py-2 text-sm mb-4 focus:outline-none focus:ring-2 focus:ring-sky-500"
          />

          {selectedProcedures.length > 0 && (
            <div className="mb-4 flex flex-wrap gap-2">
              {selectedProcedures.map((p) => (
                <span
                  key={p.imagingProcedureId}
                  className="flex items-center gap-1 bg-sky-100 text-sky-700 px-2 py-1 rounded text-xs font-medium"
                >
                  {p.procedureName}
                  <button type="button" onClick={() => toggleProcedure(p)} className="hover:text-red-600">×</button>
                </span>
              ))}
            </div>
          )}

          <div className="space-y-4 max-h-80 overflow-y-auto">
            {Object.entries(byModality).map(([modality, procedures]) => (
              <div key={modality}>
                <p className="text-xs font-semibold text-gray-500 uppercase tracking-wider mb-2">
                  {MODALITY_LABELS[modality] ?? modality}
                </p>
                <div className="grid grid-cols-2 gap-2">
                  {procedures.map((p) => {
                    const isSelected = selectedProcedures.some((x) => x.imagingProcedureId === p.imagingProcedureId)
                    return (
                      <button
                        key={p.imagingProcedureId}
                        type="button"
                        onClick={() => toggleProcedure(p)}
                        className={`text-left p-3 rounded-lg border text-sm transition-colors ${
                          isSelected
                            ? 'border-sky-500 bg-sky-50 text-sky-700'
                            : 'border-gray-200 hover:border-gray-300 text-gray-700'
                        }`}
                      >
                        <p className="font-medium text-xs">{p.procedureName}</p>
                        <p className="text-xs text-gray-400 mt-0.5">{p.bodyPart} · {p.tatHours}h TAT</p>
                      </button>
                    )
                  })}
                </div>
              </div>
            ))}
          </div>
        </div>

        {error && (
          <p className="text-red-600 text-sm bg-red-50 border border-red-200 rounded-lg px-3 py-2">{error}</p>
        )}

        <div className="flex gap-3">
          <button
            type="button"
            onClick={() => navigate(-1)}
            className="px-4 py-2 border border-gray-300 rounded-lg text-sm hover:bg-gray-50"
          >
            Cancel
          </button>
          <button
            type="submit"
            disabled={loading}
            className="bg-sky-700 hover:bg-sky-800 text-white px-6 py-2 rounded-lg text-sm font-medium disabled:opacity-60"
          >
            {loading ? 'Creating…' : 'Place Order'}
          </button>
        </div>
      </form>
    </div>
  )
}
