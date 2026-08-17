import { useState } from 'react'
import { Link } from 'react-router-dom'
import { getLabResultByAccession, simulateHl7Message } from '../../api/labResults'
import type { LabResultDetailResponse } from '../../types/labResults'
import { FLAG_COLORS } from '../../types/labOrders'

interface PresetObservation {
  code: string
  name: string
  val: string
  unit: string
  range: string
  flag: string
}

export function Hl7InboxPage() {
  // Search state
  const [accession, setAccession] = useState('')
  const [result, setResult] = useState<LabResultDetailResponse | null>(null)
  const [error, setError] = useState('')
  const [loading, setLoading] = useState(false)

  // Simulator state
  const [simAccession, setSimAccession] = useState('ACC-2026-00001')
  const [simMrn, setSimMrn] = useState('PAT-0001')
  const [simName, setSimName] = useState('John Doe')
  const [simDoctor, setSimDoctor] = useState('DOC-0001')
  const [rawHl7, setRawHl7] = useState('')
  const [transmitting, setTransmitting] = useState(false)
  const [simMessage, setSimMessage] = useState('')

  // Presets data
  const presets: Record<string, PresetObservation[]> = {
    cbc: [
      { code: 'HGB', name: 'Hemoglobin', val: '14.2', unit: 'g/dL', range: '12.0-16.0', flag: 'N' },
      { code: 'WBC', name: 'White Blood Cells', val: '6.5', unit: 'x10^3/uL', range: '4.0-11.0', flag: 'N' },
      { code: 'PLT', name: 'Platelets', val: '145', unit: 'x10^3/uL', range: '150-450', flag: 'L' },
    ],
    lipid: [
      { code: 'CHOL', name: 'Total Cholesterol', val: '5.8', unit: 'mmol/L', range: '3.0-5.2', flag: 'H' },
      { code: 'TRIG', name: 'Triglycerides', val: '1.8', unit: 'mmol/L', range: '0.5-1.7', flag: 'H' },
      { code: 'HDL', name: 'HDL Cholesterol', val: '1.2', unit: 'mmol/L', range: '0.9-2.0', flag: 'N' },
      { code: 'LDL', name: 'LDL Cholesterol', val: '3.8', unit: 'mmol/L', range: '1.5-3.0', flag: 'H' },
    ],
    renal: [
      { code: 'CREA', name: 'Creatinine', val: '85', unit: 'umol/L', range: '53-115', flag: 'N' },
      { code: 'UREA', name: 'Urea', val: '9.2', unit: 'mmol/L', range: '2.5-7.8', flag: 'H' },
      { code: 'SOD', name: 'Sodium', val: '138', unit: 'mmol/L', range: '135-145', flag: 'N' },
      { code: 'POT', name: 'Potassium', val: '4.2', unit: 'mmol/L', range: '3.5-5.1', flag: 'N' },
    ],
  }

  // Generate HL7 message string
  function generateMessage(type: 'cbc' | 'lipid' | 'renal') {
    const obs = presets[type]
    const ts = new Date().toISOString().replace(/[-T:.Z]/g, '').substring(0, 14)
    const segments = [
      `MSH|^~\\&|LIMS|FACILITY|ANALYZER|FACILITY|${ts}||ORU^R01|MSG${ts.substring(8)}|P|2.3`,
      `PID|||${simMrn.trim() || 'PAT-0001'}||${simName.trim().toUpperCase().replace(' ', '^')}||19850615|M`,
      `OBR|||${simAccession.trim() || 'ACC-2026-00001'}|^DIAGNOSTIC_PANEL|||${ts}|||||||||${simDoctor.trim() || 'DOC-0001'}`,
    ]
    obs.forEach((o, i) => {
      segments.push(
        `OBX|${i + 1}|NM|${o.code}^${o.name}^LN||${o.val}|${o.unit}|^${o.range}|${o.flag === 'N' ? '' : o.flag}|||F`
      )
    })
    setRawHl7(segments.join('\r'))
  }

  // Query HL7 database for results
  async function performLookup(accNo: string) {
    if (!accNo.trim()) return
    setError('')
    setLoading(true)
    try {
      const res = await getLabResultByAccession(accNo.trim())
      setResult(res)
    } catch {
      setError(`No HL7 result found for accession: ${accNo}`)
      setResult(null)
    } finally {
      setLoading(false)
    }
  }

  async function handleSearch(e: React.FormEvent) {
    e.preventDefault()
    performLookup(accession)
  }

  // Transmit simulated HL7 message
  async function handleSimulateSubmit(e: React.FormEvent) {
    e.preventDefault()
    if (!rawHl7.trim()) return
    setTransmitting(true)
    setSimMessage('')
    try {
      await simulateHl7Message(rawHl7)
      setSimMessage('HL7 message parsed and saved successfully!')
      
      // Auto lookup the accession
      const match = rawHl7.match(/OBR\|\|\|([^|]+)/)
      if (match && match[1]) {
        const targetAcc = match[1].split('^')[0].trim()
        setAccession(targetAcc)
        performLookup(targetAcc)
      }
    } catch (err: any) {
      const errMsg = err.response?.data?.error || 'Failed to transmit message.'
      setSimMessage(`Error: ${errMsg}`)
    } finally {
      setTransmitting(false)
    }
  }

  return (
    <div className="p-6 max-w-7xl mx-auto space-y-6">
      <div>
        <h1 className="text-2xl font-bold text-gray-900">HL7 Results Inbox</h1>
        <p className="text-gray-500 text-sm mt-0.5">
          Results received from lab instruments via MLLP (Port 2575) or HTTP Simulator
        </p>
      </div>

      <div className="grid grid-cols-1 lg:grid-cols-12 gap-6 items-start">
        {/* Left Column: Lookup and Inbox display (7 cols) */}
        <div className="lg:col-span-7 space-y-6">
          <div className="bg-white border border-gray-200 rounded-2xl p-5 shadow-sm">
            <h2 className="font-bold text-gray-800 text-sm mb-3">Lookup Accession Number</h2>
            <form onSubmit={handleSearch} className="flex gap-2">
              <input
                type="text"
                placeholder="Accession number (e.g. ACC-2026-00001)"
                value={accession}
                onChange={(e) => setAccession(e.target.value)}
                className="flex-1 border border-gray-300 rounded-lg px-3 py-2 text-sm focus:outline-none focus:ring-2 focus:ring-sky-500 font-mono"
              />
              <button
                type="submit"
                disabled={loading}
                className="bg-sky-700 hover:bg-sky-800 text-white px-4 py-2 rounded-lg text-sm font-semibold transition-colors disabled:opacity-60"
              >
                {loading ? '…' : 'Lookup'}
              </button>
            </form>
          </div>

          {error && (
            <div className="bg-red-50 border border-red-200 rounded-xl px-4 py-3 text-sm text-red-700 shadow-sm">
              {error}
            </div>
          )}
          {result ? (
            <div className="bg-white border border-gray-200 rounded-2xl p-6 shadow-sm space-y-4">
              <div className="flex items-start justify-between flex-wrap gap-2">
                <div>
                  <p className="text-[10px] font-bold text-zinc-400 font-mono uppercase tracking-wider">{result.accessionNumber}</p>
                  <h2 className="font-bold text-gray-800 text-base mt-1">{result.patientName}</h2>
                  <p className="text-xs text-gray-500 font-mono mt-0.5">MRN: {result.patientMrn}</p>
                </div>
                <div className="text-right">
                  <span className="bg-sky-100 text-sky-850 px-2 py-0.5 rounded text-[10px] font-bold uppercase tracking-wider">
                    {result.status}
                  </span>
                  <p className="text-[10px] text-gray-400 mt-1 font-semibold">
                    {new Date(result.receivedAt).toLocaleString('en-GB')}
                  </p>
                </div>
              </div>

              {result.labOrderId && (
                <Link
                  to={`/lab-orders/${result.labOrderId}`}
                  className="text-sky-650 hover:underline text-xs font-semibold flex items-center gap-1"
                >
                  View Lab Order Details →
                </Link>
              )}

              <div className="border border-gray-150 rounded-xl overflow-hidden">
                <table className="w-full text-xs text-left">
                  <thead>
                    <tr className="text-gray-500 border-b border-gray-100 bg-gray-50 font-semibold">
                      <th className="px-4 py-2.5">Test</th>
                      <th className="px-4 py-2.5">Result</th>
                      <th className="px-4 py-2.5">Unit</th>
                      <th className="px-4 py-2.5">Ref Range</th>
                      <th className="px-4 py-2.5 text-center">Flag</th>
                    </tr>
                  </thead>
                  <tbody className="divide-y divide-gray-50">
                    {result.observations.map((obs) => (
                      <tr key={obs.labObservationId} className="hover:bg-gray-50/50">
                        <td className="px-4 py-2.5">
                          <p className="font-bold text-gray-800">{obs.testName}</p>
                          <p className="text-[9px] text-gray-400 font-mono mt-0.5">{obs.testCode}</p>
                        </td>
                        <td className="px-4 py-2.5 font-bold text-gray-800">{obs.value ?? '—'}</td>
                        <td className="px-4 py-2.5 text-gray-500 font-medium">{obs.units ?? '—'}</td>
                        <td className="px-4 py-2.5 text-gray-500 font-mono text-[10px]">{obs.referenceRange ?? '—'}</td>
                        <td className="px-4 py-2.5 text-center">
                          {obs.abnormalFlag ? (
                            <span className={`px-1.5 py-0.5 rounded text-[9px] font-extrabold ${FLAG_COLORS[obs.abnormalFlag] ?? 'text-gray-600'}`}>
                              {obs.abnormalFlag}
                            </span>
                          ) : (
                            <span className="text-gray-300">—</span>
                          )}
                        </td>
                      </tr>
                    ))}
                  </tbody>
                </table>
              </div>

              {result.rawHl7 && (
                <details className="mt-4 group border-t border-gray-100 pt-4">
                  <summary className="text-[10px] font-bold text-zinc-400 cursor-pointer hover:text-zinc-600 list-none flex items-center gap-1">
                    <span>▶</span> Raw HL7 Message Payload
                  </summary>
                  <pre className="mt-2 bg-zinc-950 border border-zinc-900 rounded-xl p-3.5 text-[10px] text-emerald-400 overflow-auto whitespace-pre-wrap font-mono shadow-inner max-h-48 leading-relaxed">
                    {result.rawHl7}
                  </pre>
                </details>
              )}
            </div>
          ) : (
            <div className="bg-white border border-gray-200 rounded-2xl p-12 text-center text-gray-400 text-sm shadow-sm">
              Use lookup or transmit a simulated result to view observations.
            </div>
          )}
        </div>

        {/* Right Column: HL7 Simulator panel (5 cols) */}
        <div className="lg:col-span-5">
          <div className="bg-white border border-gray-200 rounded-2xl p-5 shadow-sm space-y-4">
            <div>
              <h2 className="font-bold text-gray-800 text-sm">HL7 Message Simulator</h2>
              <p className="text-gray-400 text-xs mt-0.5">Generate and transmit mock instrument results</p>
            </div>

            {/* Input helpers */}
            <div className="grid grid-cols-2 gap-3 text-xs">
              <div>
                <label htmlFor="hl7-sim-accession" className="block text-gray-600 font-semibold mb-1">Accession Number</label>
                <input
                  id="hl7-sim-accession"
                  type="text"
                  value={simAccession}
                  onChange={(e) => setSimAccession(e.target.value)}
                  placeholder="ACC-2026-00001"
                  className="w-full border border-gray-300 rounded-lg px-2.5 py-1.5 focus:outline-none focus:ring-1 focus:ring-sky-500 font-mono"
                />
              </div>
              <div>
                <label htmlFor="hl7-sim-mrn" className="block text-gray-600 font-semibold mb-1">Patient MRN</label>
                <input
                  id="hl7-sim-mrn"
                  type="text"
                  value={simMrn}
                  onChange={(e) => setSimMrn(e.target.value)}
                  placeholder="PAT-0001"
                  className="w-full border border-gray-300 rounded-lg px-2.5 py-1.5 focus:outline-none focus:ring-1 focus:ring-sky-500 font-mono"
                />
              </div>
              <div>
                <label htmlFor="hl7-sim-name" className="block text-gray-600 font-semibold mb-1">Patient Name</label>
                <input
                  id="hl7-sim-name"
                  type="text"
                  value={simName}
                  onChange={(e) => setSimName(e.target.value)}
                  placeholder="John Doe"
                  className="w-full border border-gray-300 rounded-lg px-2.5 py-1.5 focus:outline-none focus:ring-1 focus:ring-sky-500"
                />
              </div>
              <div>
                <label htmlFor="hl7-sim-doctor" className="block text-gray-600 font-semibold mb-1">Doctor ID</label>
                <input
                  id="hl7-sim-doctor"
                  type="text"
                  value={simDoctor}
                  onChange={(e) => setSimDoctor(e.target.value)}
                  placeholder="DOC-0001"
                  className="w-full border border-gray-300 rounded-lg px-2.5 py-1.5 focus:outline-none focus:ring-1 focus:ring-sky-500 font-mono"
                />
              </div>
            </div>

            {/* Presets loader */}
            <div className="space-y-1.5">
              <span className="text-[10px] font-bold text-gray-400 uppercase tracking-wider block">Load Panel Presets</span>
              <div className="flex gap-2 flex-wrap">
                <button
                  onClick={() => generateMessage('cbc')}
                  className="bg-sky-50 hover:bg-sky-100 text-sky-700 border border-sky-100 px-3 py-1.5 rounded-lg text-xs font-semibold transition-colors flex-1"
                >
                  🧪 CBC
                </button>
                <button
                  onClick={() => generateMessage('lipid')}
                  className="bg-indigo-50 hover:bg-indigo-100 text-indigo-700 border border-indigo-100 px-3 py-1.5 rounded-lg text-xs font-semibold transition-colors flex-1"
                >
                  🧬 Lipid Panel
                </button>
                <button
                  onClick={() => generateMessage('renal')}
                  className="bg-violet-50 hover:bg-violet-100 text-violet-700 border border-violet-100 px-3 py-1.5 rounded-lg text-xs font-semibold transition-colors flex-1"
                >
                  ⚡ Renal Panel
                </button>
              </div>
            </div>

            {/* Raw Message Textarea */}
            <form onSubmit={handleSimulateSubmit} className="space-y-3 pt-2">
              <div>
                <label htmlFor="hl7-sim-raw" className="block text-gray-600 font-semibold text-xs mb-1">Raw HL7 ORU^R01 Payload</label>
                <textarea
                  id="hl7-sim-raw"
                  rows={8}
                  value={rawHl7}
                  onChange={(e) => setRawHl7(e.target.value)}
                  placeholder="Select a preset above to load an HL7 message, or write your own custom payload here..."
                  className="w-full border border-gray-350 rounded-xl p-3 text-[10px] font-mono focus:outline-none focus:ring-2 focus:ring-sky-500 whitespace-pre scrollbar bg-zinc-950 text-emerald-400 shadow-inner"
                />
              </div>

              {simMessage && (
                <div className={`px-4 py-2.5 rounded-xl text-xs font-semibold border ${
                  simMessage.startsWith('Error') 
                    ? 'bg-red-50 border-red-200 text-red-700' 
                    : 'bg-emerald-50 border-emerald-200 text-emerald-700'
                }`}>
                  {simMessage}
                </div>
              )}

              <button
                type="submit"
                disabled={transmitting || !rawHl7}
                className="w-full bg-emerald-600 hover:bg-emerald-700 text-white font-semibold py-2.5 rounded-xl text-xs transition-transform hover:scale-[1.01] shadow disabled:opacity-60"
              >
                {transmitting ? 'Transmitting payload…' : '⚡ Transmit Simulated HL7'}
              </button>
            </form>
          </div>
        </div>
      </div>
    </div>
  )
}
