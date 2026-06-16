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

  useEffect(() => {
    setLoading(true)
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
