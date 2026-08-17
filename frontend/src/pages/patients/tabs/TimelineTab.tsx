import { useState, useEffect } from 'react'
import { getPatientAppointments } from '../../../api/appointments'
import { getPatientBills } from '../../../api/billing'
import { getLabOrdersByPatient } from '../../../api/labOrders'
import { getRadiologyOrdersForPatient } from '../../../api/radiology'
import { PatientTimeline } from '../../../components/PatientTimeline'
import type { AppointmentResponse } from '../../../types/appointments'
import type { BillResponse } from '../../../types/billing'
import type { LabOrder } from '../../../types/labOrders'
import type { RadiologyOrderSummary } from '../../../types/radiology'

export default function TimelineTab({
  patientId,
  patientName,
  patientMrn,
}: {
  patientId: string
  patientName: string
  patientMrn: string
}) {
  const [appointments, setAppointments] = useState<AppointmentResponse[]>([])
  const [bills, setBills] = useState<BillResponse[]>([])
  const [labOrders, setLabOrders] = useState<LabOrder[]>([])
  const [radiologyOrders, setRadiologyOrders] = useState<RadiologyOrderSummary[]>([])
  const [loading, setLoading] = useState(true)
  const [error, setError] = useState('')

  useEffect(() => {
    setLoading(true)
    setError('')
    Promise.all([
      getPatientAppointments(patientId),
      getPatientBills(patientId),
      getLabOrdersByPatient(patientId),
      getRadiologyOrdersForPatient(patientId),
    ])
      .then(([appts, blls, labs, rads]) => {
        setAppointments(appts)
        setBills(blls)
        setLabOrders(labs)
        setRadiologyOrders(rads)
      })
      .catch((err) => {
        console.error('Failed to load patient history timeline data', err)
        setError('Failed to load the patient timeline. Please try again.')
      })
      .finally(() => {
        setLoading(false)
      })
  }, [patientId])

  if (loading) {
    return <div className="text-gray-400 text-sm">Loading timeline history…</div>
  }

  return (
    <div className="max-w-4xl">
      <div className="mb-4">
        <h3 className="text-sm font-semibold text-gray-700">Patient Clinical Timeline</h3>
        <p className="text-xs text-gray-400 mt-0.5">Chronological record of encounters, billing, labs, and imaging studies</p>
      </div>

      {error && (
        <div className="mb-4 text-sm text-red-600 bg-red-50 border border-red-200 rounded-lg px-4 py-3">{error}</div>
      )}

      <PatientTimeline
        patientName={patientName}
        patientMrn={patientMrn}
        appointments={appointments}
        bills={bills}
        labOrders={labOrders}
        radiologyOrders={radiologyOrders}
      />
    </div>
  )
}
