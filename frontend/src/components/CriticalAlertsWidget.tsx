import { useEffect, useState } from 'react'
import { getCriticalAlerts, recordCriticalCallLog } from '../api/labOrders'
import type { LabOrderItemResponse } from '../types/labOrders'

interface CriticalAlertsWidgetProps {
  onAlertResolved?: () => void
}

export function CriticalAlertsWidget({ onAlertResolved }: CriticalAlertsWidgetProps) {
  const [alerts, setAlerts] = useState<LabOrderItemResponse[]>([])
  const [loading, setLoading] = useState(true)
  const [selectedItem, setSelectedItem] = useState<LabOrderItemResponse | null>(null)
  const [recipientName, setRecipientName] = useState('')
  const [notes, setNotes] = useState('')
  const [submitting, setSubmitting] = useState(false)
  const [error, setError] = useState<string | null>(null)

  const fetchAlerts = () => {
    setLoading(true)
    getCriticalAlerts()
      .then((data) => {
        // Only show alerts that haven't been resolved with a call log yet
        const unlogged = data.filter((item) => !item.criticalCallLogId)
        setAlerts(unlogged)
      })
      .catch(() => {})
      .finally(() => setLoading(false))
  }

  useEffect(() => {
    fetchAlerts()
    // Poll for critical alerts every 10 seconds
    const interval = setInterval(fetchAlerts, 10000)
    return () => clearInterval(interval)
  }, [])

  const handleOpenLogModal = (item: LabOrderItemResponse) => {
    setSelectedItem(item)
    setRecipientName('')
    setNotes('Read-back confirmed')
    setError(null)
  }

  const handleCloseModal = () => {
    setSelectedItem(null)
  }

  const handleSubmitLog = (e: React.FormEvent) => {
    e.preventDefault()
    if (!selectedItem) return
    if (!recipientName.trim()) {
      setError('Recipient clinician name is required.')
      return
    }

    setSubmitting(true)
    setError(null)
    recordCriticalCallLog(selectedItem.labOrderItemId, {
      recipientName: recipientName.trim(),
      notes: notes.trim(),
    })
      .then(() => {
        setSelectedItem(null)
        fetchAlerts()
        if (onAlertResolved) {
          onAlertResolved()
        }
      })
      .catch((err) => {
        setError(err.response?.data?.message || 'Failed to record call log.')
      })
      .finally(() => {
        setSubmitting(false)
      })
  }

  if (loading && alerts.length === 0) {
    return (
      <div className="bg-white/80 backdrop-blur-md border border-gray-100 rounded-2xl p-6 text-center text-gray-400 text-xs font-semibold shadow-sm">
        Checking for critical alerts...
      </div>
    )
  }

  if (alerts.length === 0) {
    return null // Return nothing if there are no critical alerts to show
  }

  return (
    <div className="relative overflow-hidden bg-gradient-to-br from-rose-50 to-red-100/50 border border-red-200 rounded-2xl p-5 shadow-md animate-pulse-subtle">
      {/* Visual background elements */}
      <div className="absolute right-0 top-0 w-32 h-32 bg-red-500/5 rounded-full blur-2xl pointer-events-none"></div>

      <div className="flex items-center justify-between mb-4">
        <div className="flex items-center gap-2">
          <span className="flex h-3.5 w-3.5 relative">
            <span className="animate-ping absolute inline-flex h-full w-full rounded-full bg-red-400 opacity-75"></span>
            <span className="relative inline-flex rounded-full h-3.5 w-3.5 bg-red-650"></span>
          </span>
          <h2 className="font-extrabold text-red-950 text-sm tracking-tight uppercase font-outfit">
            CRITICAL PANIC VALUES DETECTED ({alerts.length})
          </h2>
        </div>
        <button
          onClick={fetchAlerts}
          className="text-[10px] bg-red-100 hover:bg-red-200 text-red-800 font-bold px-2.5 py-1 rounded-lg transition-colors"
        >
          🔄 Refresh
        </button>
      </div>

      <div className="space-y-3 max-h-[300px] overflow-y-auto pr-1">
        {alerts.map((item) => {
          const resultValue = item.manualResult || item.hl7ResultValue || 'N/A'
          const resultUnit = item.manualResultUnit || item.hl7ResultUnit || ''
          const refRange = item.manualResultReferenceRange || item.hl7ResultReferenceRange || 'N/A'

          return (
            <div
              key={item.labOrderItemId}
              className="bg-white/90 backdrop-blur-sm border border-red-200 rounded-xl p-4 flex flex-col sm:flex-row sm:items-center sm:justify-between gap-4 shadow-sm hover:border-red-300 transition-colors"
            >
              <div className="space-y-1">
                <div className="flex items-center gap-2 flex-wrap">
                  <span className="font-bold text-gray-800 text-sm">{item.patientName || 'Unknown Patient'}</span>
                  <span className="text-gray-400 text-xs font-mono">({item.patientMrn || 'N/A'})</span>
                  {item.accessionNumber && (
                    <span className="bg-red-50 text-red-700 border border-red-100 px-2 py-0.5 rounded text-[10px] font-mono font-bold">
                      {item.accessionNumber}
                    </span>
                  )}
                </div>
                <div className="text-xs text-gray-500 font-medium">
                  Test: <span className="font-semibold text-gray-700">{item.testName}</span>
                </div>
                <div className="text-xs font-bold text-red-750 flex items-center gap-1">
                  <span>Panic Value:</span>
                  <span className="bg-red-100 px-2 py-0.5 rounded text-red-900 font-extrabold text-sm">
                    {resultValue} {resultUnit}
                  </span>
                  <span className="text-[10px] text-gray-400 font-normal ml-1">
                    (Normal Range: {refRange})
                  </span>
                </div>
              </div>

              <div>
                <button
                  onClick={() => handleOpenLogModal(item)}
                  className="w-full sm:w-auto bg-red-600 hover:bg-red-500 text-white font-bold text-xs px-4 py-2.5 rounded-xl shadow-sm hover:shadow transition-all flex items-center justify-center gap-1.5"
                >
                  📞 Log Clinician Call
                </button>
              </div>
            </div>
          )
        })}
      </div>

      {/* Log Call Modal */}
      {selectedItem && (
        <div className="fixed inset-0 bg-black/40 backdrop-blur-sm z-50 flex items-center justify-center p-4">
          <div className="bg-white rounded-2xl border border-gray-150 shadow-2xl max-w-md w-full overflow-hidden animate-scale-up font-outfit">
            <div className="bg-red-650 p-4 text-white">
              <h3 className="font-extrabold text-base flex items-center gap-2">
                📞 Record Critical Value Call Log
              </h3>
              <p className="text-red-100 text-xs mt-1">
                CLIA/CAP regulations require verbal verification of panic values.
              </p>
            </div>

            <form onSubmit={handleSubmitLog} className="p-5 space-y-4">
              {error && (
                <div className="bg-red-50 border border-red-200 text-red-800 text-xs rounded-xl p-3 font-semibold">
                  ⚠️ {error}
                </div>
              )}

              <div className="space-y-1">
                <label className="block text-xs font-bold text-gray-600 uppercase">Patient</label>
                <div className="bg-gray-50 border border-gray-100 rounded-xl p-2.5 text-xs text-gray-700 font-semibold">
                  {selectedItem.patientName} ({selectedItem.patientMrn})
                </div>
              </div>

              <div className="space-y-1">
                <label className="block text-xs font-bold text-gray-600 uppercase">Test & Result</label>
                <div className="bg-red-50/50 border border-red-100 rounded-xl p-2.5 text-xs text-red-950 font-bold">
                  {selectedItem.testName}: {selectedItem.manualResult || selectedItem.hl7ResultValue} {selectedItem.manualResultUnit || selectedItem.hl7ResultUnit}
                </div>
              </div>

              <div className="space-y-1">
                <label htmlFor="recipientName" className="block text-xs font-bold text-gray-700 uppercase">
                  Recipient Clinician Name <span className="text-red-500">*</span>
                </label>
                <input
                  id="recipientName"
                  type="text"
                  required
                  placeholder="e.g. Dr. Jane Smith"
                  value={recipientName}
                  onChange={(e) => setRecipientName(e.target.value)}
                  className="w-full border border-gray-250 rounded-xl px-3.5 py-2.5 text-xs focus:ring-2 focus:ring-sky-500 focus:border-sky-500 outline-none"
                />
              </div>

              <div className="space-y-1">
                <label htmlFor="notes" className="block text-xs font-bold text-gray-700 uppercase">
                  Read-back Notes / Confirmation
                </label>
                <textarea
                  id="notes"
                  rows={3}
                  value={notes}
                  onChange={(e) => setNotes(e.target.value)}
                  placeholder="e.g. Read-back confirmed. Patient being scheduled for immediate IV saline."
                  className="w-full border border-gray-250 rounded-xl px-3.5 py-2.5 text-xs focus:ring-2 focus:ring-sky-500 focus:border-sky-500 outline-none resize-none"
                />
              </div>

              <div className="flex gap-2 justify-end pt-2 border-t border-gray-100">
                <button
                  type="button"
                  onClick={handleCloseModal}
                  disabled={submitting}
                  className="px-4 py-2.5 border border-gray-200 text-gray-600 font-semibold text-xs rounded-xl hover:bg-gray-50 transition-colors"
                >
                  Cancel
                </button>
                <button
                  type="submit"
                  disabled={submitting}
                  className="px-4 py-2.5 bg-red-600 hover:bg-red-500 text-white font-bold text-xs rounded-xl shadow-sm transition-all"
                >
                  {submitting ? 'Recording...' : 'Confirm & Resolve Alert'}
                </button>
              </div>
            </form>
          </div>
        </div>
      )}
    </div>
  )
}
