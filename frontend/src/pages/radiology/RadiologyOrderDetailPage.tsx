import { useEffect, useState } from 'react'
import { useParams, useNavigate } from 'react-router-dom'
import {
  getRadiologyOrder,
  markAcquired,
  enterReport,
  signItem,
  downloadRadiologyReport,
  uploadPacsStudy,
} from '../../api/radiology'
import type { RadiologyOrderDetail, RadiologyOrderItemResponse } from '../../types/radiology'
import { ORDER_STATUS_COLORS, ITEM_STATUS_COLORS } from '../../types/radiology'
import { useAuth } from '../../contexts/AuthContext'
import { PacsViewerModal } from '../../components/PacsViewerModal'
import { ConfirmDialog } from '../../components/ConfirmDialog'
import { safeArray } from '../../utils/array';


export function RadiologyOrderDetailPage() {
  const { id } = useParams<{ id: string }>()
  const navigate = useNavigate()
  const { user } = useAuth()
  const [order, setOrder] = useState<RadiologyOrderDetail | null>(null)
  const [loading, setLoading] = useState(true)
  const [reportItem, setReportItem] = useState<RadiologyOrderItemResponse | null>(null)
  const [reportForm, setReportForm] = useState({ findings: '', impression: '', recommendations: '', pacsStudyUid: '', pacsViewerUrl: '' })
  const [submitting, setSubmitting] = useState(false)
  const [activePacsItem, setActivePacsItem] = useState<RadiologyOrderItemResponse | null>(null)
  const [confirmAction, setConfirmAction] = useState<null | { message: string; run: () => void }>(null)
  const [actionError, setActionError] = useState('')

  function load() {
    if (!id) return
    setLoading(true)
    getRadiologyOrder(id).then(setOrder).finally(() => setLoading(false))
  }

  useEffect(() => { load() }, [id])

  async function handleMarkAcquired(itemId: string) {
    setActionError('')
    await markAcquired(itemId)
    load()
  }

  async function handleUploadScan(itemId: string, file: File) {
    setSubmitting(true)
    setActionError('')
    try {
      await uploadPacsStudy(itemId, file)
      load()
    } catch (err: any) {
      setActionError(err.response?.data?.message || 'Failed to upload scan file.')
    } finally {
      setSubmitting(false)
    }
  }

  async function handleEnterReport(e: React.FormEvent) {
    e.preventDefault()
    if (!reportItem) return
    setSubmitting(true)
    try {
      await enterReport(reportItem.radiologyOrderItemId, {
        findings: reportForm.findings,
        impression: reportForm.impression,
        recommendations: reportForm.recommendations || undefined,
        pacsStudyUid: reportForm.pacsStudyUid || undefined,
        pacsViewerUrl: reportForm.pacsViewerUrl || undefined,
      })
      setReportItem(null)
      setReportForm({ findings: '', impression: '', recommendations: '', pacsStudyUid: '', pacsViewerUrl: '' })
      load()
    } finally {
      setSubmitting(false)
    }
  }

  async function handleSign(itemId: string) {
    setActionError('')
    await signItem(itemId)
    load()
  }

  async function handleDownloadReport() {
    if (!id) return
    const blob = await downloadRadiologyReport(id)
    const url = URL.createObjectURL(blob)
    const a = document.createElement('a')
    a.href = url
    a.download = `radiology-report-${id}.pdf`
    a.click()
    URL.revokeObjectURL(url)
  }

  const canAcquire = ['LabTechnician', 'Admin', 'SuperAdmin'].includes(user?.role ?? '')
  const canReport  = ['Doctor', 'Admin', 'SuperAdmin'].includes(user?.role ?? '')
  const canSign    = ['Doctor', 'Admin', 'SuperAdmin'].includes(user?.role ?? '')

  if (loading) return <div className="p-6 text-gray-400 text-sm">Loading…</div>
  if (!order)  return <div className="p-6 text-red-600 text-sm">Order not found.</div>

  const isCompleted = order.status === 'Completed' || order.status === 'Signed'

  return (
    <div className="p-6 max-w-4xl">
      <button onClick={() => navigate(-1)} className="text-sm text-gray-500 hover:text-gray-700 mb-4">← Back</button>

      {actionError && (
        <div className="mb-4 text-sm text-red-600 bg-red-50 border border-red-200 rounded-lg px-4 py-3">{actionError}</div>
      )}

      {/* Header */}
      <div className="flex items-start justify-between mb-6">
        <div>
          <div className="flex items-center gap-3 mb-1">
            <h1 className="text-2xl font-bold text-gray-900">Radiology Order</h1>
            <span className={`px-2.5 py-1 rounded text-xs font-medium ${ORDER_STATUS_COLORS[order.status] ?? 'bg-gray-100 text-gray-600'}`}>
              {order.status}
            </span>
            <span className={`inline-flex items-center gap-1.5 px-2 py-0.5 rounded-full text-xs font-medium ${
              order.priority === 'STAT' ? 'bg-red-100 text-red-700 border border-red-200' :
              order.priority === 'Urgent' ? 'bg-orange-100 text-orange-700' :
              'bg-gray-150 text-gray-600'
            }`}>{order.priority === 'STAT' && (
              <span className="relative flex h-1.5 w-1.5">
                <span className="absolute inline-flex h-full w-full rounded-full bg-red-400 opacity-75 animate-ping"></span>
                <span className="relative inline-flex rounded-full h-1.5 w-1.5 bg-red-500"></span>
              </span>
            )}{order.priority}</span>
          </div>
          <p className="text-gray-600 text-sm">
            {order.patientName} · <span className="font-mono text-xs">{order.patientMrn}</span>
          </p>
          <p className="text-gray-400 text-xs mt-0.5">
            Referred by {order.orderingDoctorName} · {new Date(order.orderedAt).toLocaleString('en-GB')}
          </p>
        </div>
        {isCompleted && (
          <button
            onClick={handleDownloadReport}
            className="bg-sky-700 hover:bg-sky-800 text-white px-3 py-1.5 rounded-lg text-sm font-medium"
          >
            Download Report
          </button>
        )}
      </div>

      {order.clinicalIndication && (
        <div className="bg-yellow-50 border border-yellow-200 rounded-xl px-4 py-3 mb-6 text-sm text-yellow-800">
          <span className="font-medium">Clinical Indication: </span>{order.clinicalIndication}
        </div>
      )}

      {/* Items table */}
      <div className="bg-white border border-gray-200 rounded-xl overflow-hidden mb-6">
        <div className="px-5 py-4 border-b border-gray-100">
          <h2 className="font-semibold text-gray-800">Imaging Studies</h2>
        </div>
        <table className="w-full text-sm">
          <thead>
            <tr className="text-left text-xs text-gray-500 border-b border-gray-100 bg-gray-50">
              <th className="px-5 py-3 font-medium">Procedure</th>
              <th className="px-5 py-3 font-medium">Accession</th>
              <th className="px-5 py-3 font-medium">Status</th>
              <th className="px-5 py-3 font-medium">Report</th>
              <th className="px-5 py-3 font-medium">Actions</th>
            </tr>
          </thead>
          <tbody>
            {safeArray(order.items).map((item) => (
              <tr key={item.radiologyOrderItemId} className="border-b border-gray-50">
                <td className="px-5 py-3">
                  <p className="font-medium text-gray-800">{item.procedureName}</p>
                  <p className="text-xs text-gray-400">{item.modality} · {item.bodyPart}</p>
                  {item.isTatExceeded && (
                    <span className="text-xs text-red-500 font-medium">TAT exceeded</span>
                  )}
                </td>
                <td className="px-5 py-3 text-gray-400 font-mono text-xs">{item.accessionNumber ?? '—'}</td>
                <td className="px-5 py-3">
                  <span className={`px-2 py-0.5 rounded text-xs font-medium ${ITEM_STATUS_COLORS[item.status] ?? 'bg-gray-100 text-gray-600'}`}>
                    {item.status}
                  </span>
                  {item.acquiredAt && (
                    <p className="text-xs text-gray-400 mt-0.5">
                      Acquired {new Date(item.acquiredAt).toLocaleString('en-GB', { dateStyle: 'short', timeStyle: 'short' })}
                    </p>
                  )}
                </td>
                <td className="px-5 py-3 max-w-xs">
                  {item.impression ? (
                    <div>
                      <p className="text-xs text-gray-600 line-clamp-2">{item.impression}</p>
                      {item.reportingDoctorName && (
                        <p className="text-xs text-gray-400 mt-0.5">by {item.reportingDoctorName}</p>
                      )}
                      {item.status !== 'Ordered' && (
                        <button
                          onClick={(e) => {
                            e.stopPropagation()
                            setActivePacsItem(item)
                          }}
                          className="text-xs text-sky-600 hover:underline mt-1.5 block font-semibold text-left"
                        >
                          🩻 View in PACS Workstation
                        </button>
                      )}
                    </div>
                  ) : (
                    <span className="text-gray-300 text-xs">—</span>
                  )}
                </td>
                <td className="px-5 py-3">
                  <div className="flex flex-col gap-1">
                    {canAcquire && item.status === 'Ordered' && (
                      <div className="flex flex-col gap-1.5 border border-dashed border-gray-300 rounded-xl p-2 bg-gray-50/50 hover:bg-gray-100/50 transition-colors">
                        <button
                          onClick={() => setConfirmAction({ message: 'Mark images as acquired?', run: () => handleMarkAcquired(item.radiologyOrderItemId) })}
                          className="text-yellow-600 hover:underline text-[10px] font-bold text-left self-start"
                        >
                          ⚡ Mark Acquired (No Scan)
                        </button>
                        <div className="relative flex flex-col items-center justify-center p-1.5 bg-white border border-gray-200 rounded-lg shadow-sm hover:border-sky-300 transition-colors">
                          <label className="cursor-pointer text-[10px] text-sky-655 font-bold flex items-center gap-1">
                            <span>🩻 Upload scan file</span>
                            <input
                              type="file"
                              accept="image/png, image/jpeg"
                              className="hidden"
                              onChange={(e) => {
                                const file = e.target.files?.[0]
                                if (file) {
                                  handleUploadScan(item.radiologyOrderItemId, file)
                                }
                              }}
                            />
                          </label>
                        </div>
                      </div>
                    )}
                    {canReport && (item.status === 'Acquired' || item.status === 'Ordered') && (
                      <button
                        onClick={() => {
                          setReportItem(item)
                          setReportForm({
                            findings: item.findings ?? '',
                            impression: item.impression ?? '',
                            recommendations: item.recommendations ?? '',
                            pacsStudyUid: '',
                            pacsViewerUrl: item.pacsViewerUrl ?? '',
                          })
                        }}
                        className="text-sky-600 hover:underline text-xs font-medium"
                      >
                        {item.impression ? 'Edit Report' : 'Enter Report'}
                      </button>
                    )}
                    {canSign && item.status === 'Reported' && (
                      <button
                        onClick={() => setConfirmAction({ message: 'Sign off this report?', run: () => handleSign(item.radiologyOrderItemId) })}
                        className="text-green-600 hover:underline text-xs font-medium"
                      >
                        Sign
                      </button>
                    )}
                    {item.signedAt && (
                      <span className="text-gray-400 text-xs">
                        Signed {new Date(item.signedAt).toLocaleDateString('en-GB')}
                      </span>
                    )}
                  </div>
                </td>
              </tr>
            ))}
          </tbody>
        </table>
      </div>

      {/* Enter report modal */}
      {reportItem && (
        <div className="fixed inset-0 bg-black/30 flex items-center justify-center z-50 p-4">
          <div className="bg-white rounded-2xl shadow-xl p-6 w-full max-w-lg">
            <h3 className="font-semibold text-gray-800 mb-4">Radiology Report — {reportItem.procedureName}</h3>
            <form onSubmit={handleEnterReport} className="space-y-4">
              <div>
                <label className="block text-sm font-medium text-gray-700 mb-1">Findings <span className="text-red-500">*</span></label>
                <textarea
                  required
                  rows={4}
                  value={reportForm.findings}
                  onChange={(e) => setReportForm({ ...reportForm, findings: e.target.value })}
                  placeholder="Describe imaging findings…"
                  className="w-full border border-gray-300 rounded-lg px-3 py-2 text-sm focus:outline-none focus:ring-2 focus:ring-sky-500"
                />
              </div>
              <div>
                <label className="block text-sm font-medium text-gray-700 mb-1">Impression <span className="text-red-500">*</span></label>
                <textarea
                  required
                  rows={3}
                  value={reportForm.impression}
                  onChange={(e) => setReportForm({ ...reportForm, impression: e.target.value })}
                  placeholder="Clinical impression / diagnosis…"
                  className="w-full border border-gray-300 rounded-lg px-3 py-2 text-sm focus:outline-none focus:ring-2 focus:ring-sky-500"
                />
              </div>
              <div>
                <label className="block text-sm font-medium text-gray-700 mb-1">Recommendations</label>
                <textarea
                  rows={2}
                  value={reportForm.recommendations}
                  onChange={(e) => setReportForm({ ...reportForm, recommendations: e.target.value })}
                  placeholder="Optional follow-up recommendations…"
                  className="w-full border border-gray-300 rounded-lg px-3 py-2 text-sm focus:outline-none focus:ring-2 focus:ring-sky-500"
                />
              </div>
              <div>
                <label className="block text-sm font-medium text-gray-700 mb-1">PACS Viewer URL</label>
                <input
                  value={reportForm.pacsViewerUrl}
                  onChange={(e) => setReportForm({ ...reportForm, pacsViewerUrl: e.target.value })}
                  placeholder="https://pacs.hospital.com/viewer/…"
                  className="w-full border border-gray-300 rounded-lg px-3 py-2 text-sm focus:outline-none focus:ring-2 focus:ring-sky-500"
                />
              </div>
              <div>
                <label className="block text-sm font-medium text-gray-700 mb-1">PACS Study UID</label>
                <input
                  value={reportForm.pacsStudyUid}
                  onChange={(e) => setReportForm({ ...reportForm, pacsStudyUid: e.target.value })}
                  placeholder="1.2.840.10008…"
                  className="w-full border border-gray-300 rounded-lg px-3 py-2 text-sm focus:outline-none focus:ring-2 focus:ring-sky-500"
                />
              </div>
              <div className="flex gap-3 pt-2">
                <button
                  type="button"
                  onClick={() => setReportItem(null)}
                  className="flex-1 border border-gray-300 rounded-lg py-2 text-sm hover:bg-gray-50"
                >
                  Cancel
                </button>
                <button
                  type="submit"
                  disabled={submitting}
                  className="flex-1 bg-sky-700 text-white rounded-lg py-2 text-sm font-medium hover:bg-sky-800 disabled:opacity-60"
                >
                  {submitting ? 'Saving…' : 'Save Report'}
                </button>
              </div>
            </form>
          </div>
        </div>
      )}

      {activePacsItem && (
        <PacsViewerModal
          isOpen={!!activePacsItem}
          onClose={() => setActivePacsItem(null)}
          patientName={order.patientName}
          patientMrn={order.patientMrn}
          procedureName={activePacsItem.procedureName}
          modality={activePacsItem.modality}
          accessionNumber={activePacsItem.accessionNumber ?? 'N/A'}
          imageUrl={activePacsItem.pacsViewerUrl ?? undefined}
        />
      )}

      <ConfirmDialog
        open={!!confirmAction}
        title="Confirm"
        message={confirmAction?.message ?? ''}
        onConfirm={() => { confirmAction?.run(); setConfirmAction(null); }}
        onCancel={() => setConfirmAction(null)}
      />
    </div>
  )
}
