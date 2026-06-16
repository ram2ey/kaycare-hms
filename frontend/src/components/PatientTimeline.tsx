import { useState } from 'react'
import { Link } from 'react-router-dom'
import type { AppointmentResponse } from '../types/appointments'
import type { BillResponse } from '../types/billing'
import type { LabOrder, LabOrderDetail, LabOrderStatus } from '../types/labOrders'
import type { RadiologyOrderSummary, RadiologyOrderDetail } from '../types/radiology'
import { getLabOrder } from '../api/labOrders'
import { getRadiologyOrder } from '../api/radiology'
import { PacsViewerModal } from './PacsViewerModal'
import { ORDER_STATUS_COLORS as LAB_STATUS_COLORS, FLAG_COLORS } from '../types/labOrders'
import { ORDER_STATUS_COLORS as RAD_STATUS_COLORS } from '../types/radiology'
import { STATUS_COLORS as BSC } from '../types/billing'
import { STATUS_COLORS as APPOINTMENT_STATUS_COLORS } from '../types/appointments'

interface PatientTimelineProps {
  patientName: string
  patientMrn: string
  appointments: AppointmentResponse[]
  bills: BillResponse[]
  labOrders: LabOrder[]
  radiologyOrders: RadiologyOrderSummary[]
}

interface TimelineEvent {
  id: string
  type: 'appointment' | 'bill' | 'labOrder' | 'radiologyOrder'
  date: Date
  title: string
  status: string
  statusColor: string
  priority?: string
  doctorName?: string
  subtitle?: string
  rawItem: any
}

export function PatientTimeline({
  patientName,
  patientMrn,
  appointments,
  bills,
  labOrders,
  radiologyOrders,
}: PatientTimelineProps) {
  const [labOrderDetails, setLabOrderDetails] = useState<Record<string, LabOrderDetail>>({})
  const [radiologyOrderDetails, setRadiologyOrderDetails] = useState<Record<string, RadiologyOrderDetail>>({})
  const [expandedEvents, setExpandedEvents] = useState<Record<string, boolean>>({})
  const [loadingDetails, setLoadingDetails] = useState<Record<string, boolean>>({})
  const [activePacsItem, setActivePacsItem] = useState<{
    procedureName: string
    modality: string
    accessionNumber: string
  } | null>(null)

  // Map and sort all events chronologically (newest first)
  const timelineEvents: TimelineEvent[] = [
    ...appointments.map((a) => ({
      id: a.appointmentId,
      type: 'appointment' as const,
      date: new Date(a.scheduledAt),
      title: `Appointment with Dr. ${a.doctorName}`,
      status: a.status,
      statusColor: APPOINTMENT_STATUS_COLORS[a.status] ?? 'bg-gray-100 text-gray-600',
      subtitle: a.chiefComplaint || 'No reason specified',
      rawItem: a,
    })),
    ...bills.map((b) => ({
      id: b.billId,
      type: 'bill' as const,
      date: new Date(b.createdAt),
      title: `Invoice Issued (${b.billNumber})`,
      status: b.status,
      statusColor: BSC[b.status] ?? 'bg-gray-100 text-gray-600',
      subtitle: `Total: $${b.totalAmount.toFixed(2)} · Balance: $${b.balanceDue.toFixed(2)}`,
      rawItem: b,
    })),
    ...labOrders.map((o) => ({
      id: o.labOrderId,
      type: 'labOrder' as const,
      date: new Date(o.orderedAt),
      title: `Lab Order (${o.testNames.slice(0, 2).join(', ')}${o.testNames.length > 2 ? '...' : ''})`,
      status: o.status,
      statusColor: LAB_STATUS_COLORS[o.status as LabOrderStatus] ?? 'bg-gray-100 text-gray-600',
      priority: undefined,
      doctorName: o.orderingDoctorName,
      subtitle: o.testNames.join(', '),
      rawItem: o,
    })),
    ...radiologyOrders.map((r) => ({
      id: r.radiologyOrderId,
      type: 'radiologyOrder' as const,
      date: new Date(r.orderedAt),
      title: `Radiology Order (${r.procedureNames.join(', ') || 'No Procedure'})`,
      status: r.status,
      statusColor: RAD_STATUS_COLORS[r.status] ?? 'bg-gray-100 text-gray-600',
      priority: r.priority,
      doctorName: r.orderingDoctorName,
      subtitle: `${r.procedureNames.length} Imaging ${r.procedureNames.length === 1 ? 'Study' : 'Studies'}`,
      rawItem: r,
    })),
  ].sort((a, b) => b.date.getTime() - a.date.getTime())

  async function toggleEvent(eventId: string, type: 'labOrder' | 'radiologyOrder') {
    const isExpanded = !!expandedEvents[eventId]
    if (isExpanded) {
      setExpandedEvents((prev) => ({ ...prev, [eventId]: false }))
      return
    }

    if (type === 'labOrder' && !labOrderDetails[eventId]) {
      setLoadingDetails((prev) => ({ ...prev, [eventId]: true }))
      try {
        const detail = await getLabOrder(eventId)
        setLabOrderDetails((prev) => ({ ...prev, [eventId]: detail }))
      } catch (err) {
        console.error('Failed to load lab order detail', err)
      } finally {
        setLoadingDetails((prev) => ({ ...prev, [eventId]: false }))
      }
    } else if (type === 'radiologyOrder' && !radiologyOrderDetails[eventId]) {
      setLoadingDetails((prev) => ({ ...prev, [eventId]: true }))
      try {
        const detail = await getRadiologyOrder(eventId)
        setRadiologyOrderDetails((prev) => ({ ...prev, [eventId]: detail }))
      } catch (err) {
        console.error('Failed to load radiology order detail', err)
      } finally {
        setLoadingDetails((prev) => ({ ...prev, [eventId]: false }))
      }
    }

    setExpandedEvents((prev) => ({ ...prev, [eventId]: true }))
  }

  // Helper icons
  const renderIcon = (type: TimelineEvent['type']) => {
    switch (type) {
      case 'appointment':
        return (
          <svg className="w-5 h-5 text-sky-600" fill="none" viewBox="0 0 24 24" stroke="currentColor" strokeWidth={2}>
            <path strokeLinecap="round" strokeLinejoin="round" d="M8 7V3m8 4V3m-9 8h10M5 21h14a2 2 0 002-2V7a2 2 0 00-2-2H5a2 2 0 00-2 2v12a2 2 0 002 2z" />
          </svg>
        )
      case 'bill':
        return (
          <svg className="w-5 h-5 text-emerald-600" fill="none" viewBox="0 0 24 24" stroke="currentColor" strokeWidth={2}>
            <path strokeLinecap="round" strokeLinejoin="round" d="M12 8c-1.657 0-3 .895-3 2s1.343 2 3 2 3 .895 3 2-1.343 2-3 2m0-8c1.11 0 2.08.402 2.599 1M12 8V7m0 1v8m0 0v1m0-1c-1.11 0-2.08-.402-2.599-1M21 12a9 9 0 11-18 0 9 9 0 0118 0z" />
          </svg>
        )
      case 'labOrder':
        return (
          <svg className="w-5 h-5 text-purple-600" fill="none" viewBox="0 0 24 24" stroke="currentColor" strokeWidth={2}>
            <path strokeLinecap="round" strokeLinejoin="round" d="M19.428 15.428a2 2 0 00-1.022-.547l-2.387-.477a6 6 0 00-3.86.517l-.318.158a6 6 0 01-3.86.517L6.05 15.21a2 2 0 00-1.806.547M8 4h8l-1 1v5.172a2 2 0 00.586 1.414l5 5c1.26 1.26.367 3.414-1.415 3.414H4.828c-1.782 0-2.674-2.154-1.414-3.414l5-5A2 2 0 009 10.172V5L8 4z" />
          </svg>
        )
      case 'radiologyOrder':
        return (
          <svg className="w-5 h-5 text-indigo-600" fill="none" viewBox="0 0 24 24" stroke="currentColor" strokeWidth={2}>
            <path strokeLinecap="round" strokeLinejoin="round" d="M12 4v1m0 11v3m8-7h-1M4 12H3m15.364-6.364l-.707.707M6.343 17.657l-.707.707m0-12.728l.707.707m11.314 11.314l.707-.707M12 8a4 4 0 100 8 4 4 0 000-8z" />
          </svg>
        )
    }
  }

  const getEventName = (type: TimelineEvent['type']) => {
    switch (type) {
      case 'appointment':
        return 'Appointment'
      case 'bill':
        return 'Billing Invoice'
      case 'labOrder':
        return 'Laboratory Order'
      case 'radiologyOrder':
        return 'Radiology Order'
    }
  }

  return (
    <div className="space-y-6">
      {timelineEvents.length === 0 ? (
        <div className="bg-white border border-gray-200 rounded-2xl p-12 text-center text-gray-400 text-sm shadow-sm">
          No medical history events recorded for this patient.
        </div>
      ) : (
        <div className="relative pl-8 border-l-2 border-gray-150 ml-4 py-2 space-y-8">
          {timelineEvents.map((event) => {
            const isExpanded = !!expandedEvents[event.id]
            const isLoading = !!loadingDetails[event.id]
            const isExpandable = event.type === 'labOrder' || event.type === 'radiologyOrder'

            return (
              <div key={event.id} className="relative group animate-fadeIn">
                {/* Timeline Dot/Icon */}
                <div className="absolute -left-[53px] top-1 bg-white border-2 border-gray-150 rounded-full p-2.5 shadow-sm group-hover:scale-105 group-hover:border-sky-500 transition-all">
                  {renderIcon(event.type)}
                </div>

                {/* Event Card */}
                <div className="bg-white border border-gray-200 rounded-2xl p-5 shadow-sm hover:shadow-md transition-shadow">
                  {/* Card Header */}
                  <div className="flex items-start justify-between flex-wrap gap-2 mb-3">
                    <div>
                      <div className="flex items-center gap-2">
                        <span className="text-[10px] font-bold text-gray-400 uppercase tracking-wider">
                          {getEventName(event.type)}
                        </span>
                        {event.priority && (
                          <span className={`inline-flex items-center gap-1 px-1.5 py-0.5 rounded text-[9px] font-extrabold uppercase ${
                            event.priority === 'STAT' ? 'bg-red-100 text-red-700 border border-red-200' :
                            event.priority === 'Urgent' ? 'bg-orange-100 text-orange-700' :
                            'bg-gray-150 text-gray-600'
                          }`}>
                            {event.priority === 'STAT' && (
                              <span className="relative flex h-1.5 w-1.5">
                                <span className="absolute inline-flex h-full w-full rounded-full bg-red-400 opacity-75 animate-ping"></span>
                                <span className="relative inline-flex rounded-full h-1.5 w-1.5 bg-red-500"></span>
                              </span>
                            )}
                            {event.priority}
                          </span>
                        )}
                      </div>
                      <h3 className="font-bold text-gray-800 text-sm mt-1">{event.title}</h3>
                    </div>
                    <div className="text-right">
                      <span className={`px-2 py-0.5 rounded text-[10px] font-bold uppercase tracking-wider ${event.statusColor}`}>
                        {event.status}
                      </span>
                      <p className="text-[10px] text-gray-400 mt-1 font-semibold">
                        {event.date.toLocaleString('en-GB', { dateStyle: 'medium', timeStyle: 'short' })}
                      </p>
                    </div>
                  </div>

                  {/* Card Content Summary */}
                  <div className="text-xs text-gray-500 space-y-1">
                    <p>{event.subtitle}</p>
                    {event.doctorName && (
                      <p className="text-gray-400 font-medium">Ordering Provider: {event.doctorName}</p>
                    )}
                  </div>

                  {/* Expand / Collapse Control */}
                  {isExpandable && (
                    <div className="mt-4 pt-4 border-t border-gray-100">
                      <button
                        onClick={() => toggleEvent(event.id, event.type as 'labOrder' | 'radiologyOrder')}
                        className="text-xs font-semibold text-sky-650 hover:text-sky-850 flex items-center gap-1 focus:outline-none cursor-pointer"
                      >
                        {isLoading ? (
                          <span className="flex items-center gap-1.5">
                            <svg className="animate-spin h-3.5 w-3.5 text-sky-650" fill="none" viewBox="0 0 24 24">
                              <circle className="opacity-25" cx="12" cy="12" r="10" stroke="currentColor" strokeWidth="4" />
                              <path className="opacity-75" fill="currentColor" d="M4 12a8 8 0 018-8V0C5.373 0 0 5.373 0 12h4zm2 5.291A7.962 7.962 0 014 12H0c0 3.042 1.135 5.824 3 7.938l3-2.647z" />
                            </svg>
                            Loading details…
                          </span>
                        ) : isExpanded ? (
                          <span>▲ Collapse Diagnostic Details</span>
                        ) : (
                          <span>▼ Expand Diagnostic Details</span>
                        )}
                      </button>
                    </div>
                  )}

                  {/* Expandable Panel */}
                  {isExpanded && !isLoading && (
                    <div className="mt-4 pt-4 border-t border-gray-100 space-y-4 animate-fadeIn">
                      {event.type === 'labOrder' && labOrderDetails[event.id] && (
                        <div className="space-y-3">
                          <div className="bg-gray-50/70 border border-gray-150 rounded-xl px-4 py-3 text-xs grid grid-cols-1 sm:grid-cols-2 gap-2">
                            <div>
                              <span className="text-gray-400 font-semibold block">Accession Number</span>
                              <span className="font-mono font-bold text-gray-700">
                                {labOrderDetails[event.id].items.map(i => i.accessionNumber).filter(Boolean).join(', ') || '—'}
                              </span>
                            </div>
                            {labOrderDetails[event.id].notes && (
                              <div>
                                <span className="text-gray-400 font-semibold block">Order Notes</span>
                                <span className="text-gray-700 italic">
                                  {labOrderDetails[event.id].notes}
                                </span>
                              </div>
                            )}
                          </div>

                          <div className="border border-gray-150 rounded-xl overflow-hidden">
                            <table className="w-full text-xs text-left">
                              <thead>
                                <tr className="text-gray-500 border-b border-gray-100 bg-gray-50 font-semibold">
                                  <th className="px-4 py-2.5">Test Name</th>
                                  <th className="px-4 py-2.5">Status</th>
                                  <th className="px-4 py-2.5">Result</th>
                                  <th className="px-4 py-2.5">Unit</th>
                                  <th className="px-4 py-2.5">Ref Range</th>
                                  <th className="px-4 py-2.5 text-center">Flag</th>
                                  <th className="px-4 py-2.5">Signed By</th>
                                </tr>
                              </thead>
                              <tbody className="divide-y divide-gray-50">
                                {labOrderDetails[event.id].items.map((item) => {
                                  const resultVal = item.manualResult ?? (item.labResultId ? 'HL7 Resulted' : '—')
                                  const resultUnit = item.manualResultUnit ?? '—'
                                  const refRange = item.manualResultReferenceRange ?? '—'
                                  const flag = item.manualResultFlag ?? '—'

                                  return (
                                    <tr key={item.labOrderItemId} className="hover:bg-gray-50/30">
                                      <td className="px-4 py-2.5">
                                        <p className="font-bold text-gray-800">{item.testName}</p>
                                        {item.accessionNumber && (
                                          <p className="text-[9px] text-gray-400 font-mono mt-0.5">ACC: {item.accessionNumber}</p>
                                        )}
                                      </td>
                                      <td className="px-4 py-2.5">
                                        <span className="text-[10px] font-semibold text-gray-650 bg-gray-100 px-1.5 py-0.5 rounded">
                                          {item.status}
                                        </span>
                                      </td>
                                      <td className="px-4 py-2.5 font-bold text-gray-800">
                                        {item.labResultId ? (
                                          <Link to={`/lab-results/${item.labResultId}`} className="text-sky-600 hover:underline">
                                            View HL7 Result
                                          </Link>
                                        ) : (
                                          resultVal
                                        )}
                                      </td>
                                      <td className="px-4 py-2.5 text-gray-500">{resultUnit}</td>
                                      <td className="px-4 py-2.5 text-gray-450 font-mono text-[10px]">{refRange}</td>
                                      <td className="px-4 py-2.5 text-center">
                                        {flag && flag !== '—' ? (
                                          <span className={`px-1.5 py-0.5 rounded text-[9px] font-extrabold ${FLAG_COLORS[flag] ?? 'text-gray-600'}`}>
                                            {flag}
                                          </span>
                                        ) : (
                                          <span className="text-gray-300">—</span>
                                        )}
                                      </td>
                                      <td className="px-4 py-2.5 text-gray-500">
                                        {item.signedAt ? (
                                          <div>
                                            <p className="font-semibold text-gray-700">Signed</p>
                                            <p className="text-[9px] text-gray-400">
                                              {new Date(item.signedAt).toLocaleDateString('en-GB')}
                                            </p>
                                          </div>
                                        ) : (
                                          <span className="text-gray-300">—</span>
                                        )}
                                      </td>
                                    </tr>
                                  )
                                })}
                              </tbody>
                            </table>
                          </div>
                        </div>
                      )}

                      {event.type === 'radiologyOrder' && radiologyOrderDetails[event.id] && (
                        <div className="space-y-4">
                          <div className="bg-gray-50/70 border border-gray-150 rounded-xl px-4 py-3 text-xs grid grid-cols-1 sm:grid-cols-2 gap-2">
                            {radiologyOrderDetails[event.id].clinicalIndication && (
                              <div>
                                <span className="text-gray-400 font-semibold block">Clinical Indication</span>
                                <span className="text-gray-700 italic font-semibold">
                                  {radiologyOrderDetails[event.id].clinicalIndication}
                                </span>
                              </div>
                            )}
                            {radiologyOrderDetails[event.id].notes && (
                              <div>
                                <span className="text-gray-400 font-semibold block">Order Notes</span>
                                <span className="text-gray-700">{radiologyOrderDetails[event.id].notes}</span>
                              </div>
                            )}
                          </div>

                          <div className="space-y-4 divide-y divide-gray-100">
                            {radiologyOrderDetails[event.id].items.map((item) => (
                              <div key={item.radiologyOrderItemId} className="pt-4 first:pt-0 space-y-2">
                                <div className="flex justify-between items-start flex-wrap gap-2">
                                  <div>
                                    <h4 className="font-bold text-gray-800 text-xs">{item.procedureName}</h4>
                                    <p className="text-[10px] text-gray-450 font-medium mt-0.5">
                                      {item.modality} · {item.bodyPart} · Accession: <span className="font-mono">{item.accessionNumber || '—'}</span>
                                    </p>
                                  </div>
                                  <div className="flex gap-2 items-center">
                                    <span className="text-[10px] font-semibold text-gray-650 bg-gray-100 px-1.5 py-0.5 rounded">
                                      {item.status}
                                    </span>
                                    {item.status !== 'Ordered' && (
                                      <button
                                        onClick={() =>
                                          setActivePacsItem({
                                            procedureName: item.procedureName,
                                            modality: item.modality,
                                            accessionNumber: item.accessionNumber ?? 'N/A',
                                          })
                                        }
                                        className="text-[10px] font-bold text-sky-700 hover:text-sky-850 flex items-center gap-0.5 border border-sky-100 px-2 py-0.5 rounded bg-sky-50 transition-colors cursor-pointer"
                                      >
                                        🩻 View Study
                                      </button>
                                    )}
                                  </div>
                                </div>

                                {item.impression && (
                                  <div className="bg-sky-50/30 border border-sky-100/50 rounded-xl p-3.5 text-xs space-y-2">
                                    {item.findings && (
                                      <div>
                                        <span className="font-bold text-gray-500 block text-[10px] uppercase">Findings</span>
                                        <p className="text-gray-750 font-medium">{item.findings}</p>
                                      </div>
                                    )}
                                    <div>
                                      <span className="font-bold text-gray-500 block text-[10px] uppercase">Impression</span>
                                      <p className="text-gray-800 font-semibold">{item.impression}</p>
                                    </div>
                                    {item.recommendations && (
                                      <div>
                                        <span className="font-bold text-gray-500 block text-[10px] uppercase">Recommendations</span>
                                        <p className="text-gray-705 italic">{item.recommendations}</p>
                                      </div>
                                    )}
                                    {item.reportingDoctorName && (
                                      <div className="text-[10px] text-gray-400 font-semibold pt-1.5 border-t border-sky-100/30 mt-2">
                                        Reported by {item.reportingDoctorName}{' '}
                                        {item.signedAt ? `on ${new Date(item.signedAt).toLocaleDateString('en-GB')}` : ''}
                                      </div>
                                    )}
                                  </div>
                                )}
                              </div>
                            ))}
                          </div>
                        </div>
                      )}
                    </div>
                  )}
                </div>
              </div>
            )
          })}
        </div>
      )}

      {/* PACS Viewer Modal */}
      {activePacsItem && (
        <PacsViewerModal
          isOpen={!!activePacsItem}
          onClose={() => setActivePacsItem(null)}
          patientName={patientName}
          patientMrn={patientMrn}
          procedureName={activePacsItem.procedureName}
          modality={activePacsItem.modality}
          accessionNumber={activePacsItem.accessionNumber}
        />
      )}
    </div>
  )
}
