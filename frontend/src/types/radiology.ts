export interface ImagingProcedureItem {
  imagingProcedureId: string
  procedureCode: string
  procedureName: string
  modality: string
  bodyPart: string
  department: string
  tatHours: number
}

export interface RadiologyOrderItemResponse {
  radiologyOrderItemId: string
  imagingProcedureId: string
  procedureName: string
  modality: string
  bodyPart: string
  department: string
  tatHours: number
  accessionNumber: string | null
  status: string
  acquiredAt: string | null
  reportedAt: string | null
  signedAt: string | null
  findings: string | null
  impression: string | null
  recommendations: string | null
  reportingDoctorName: string | null
  pacsViewerUrl: string | null
  isTatExceeded: boolean
}

export interface RadiologyOrderSummary {
  radiologyOrderId: string
  patientId: string
  patientName: string
  patientMrn: string
  patientGender: string
  orderingDoctorUserId: string
  orderingDoctorName: string
  priority: string
  status: string
  clinicalIndication: string | null
  notes: string | null
  orderedAt: string
  incompleteCount: number
  reportedCount: number
  signedCount: number
  procedureNames: string[]
}

export interface RadiologyOrderDetail extends RadiologyOrderSummary {
  billId: string | null
  billNumber: string | null
  items: RadiologyOrderItemResponse[]
}

export interface CreateRadiologyOrderRequest {
  patientId: string
  billId?: string
  priority: string
  clinicalIndication?: string
  notes?: string
  procedureIds: string[]
}

export interface RadiologyReportRequest {
  findings: string
  impression: string
  recommendations?: string
  pacsStudyUid?: string
  pacsViewerUrl?: string
}

export const ORDER_STATUS_COLORS: Record<string, string> = {
  Pending:    'bg-gray-100 text-gray-600',
  Scheduled:  'bg-blue-100 text-blue-700',
  InProgress: 'bg-orange-100 text-orange-700',
  Completed:  'bg-green-100 text-green-700',
  Signed:     'bg-indigo-100 text-indigo-700',
  Cancelled:  'bg-red-100 text-red-700',
}

export const ITEM_STATUS_COLORS: Record<string, string> = {
  Ordered:  'bg-gray-100 text-gray-600',
  Acquired: 'bg-yellow-100 text-yellow-700',
  Reported: 'bg-blue-100 text-blue-700',
  Signed:   'bg-green-100 text-green-700',
}

export const PRIORITY_OPTIONS = ['Routine', 'Urgent', 'STAT'] as const

export interface RadiologyStatsResponse {
  scheduledCount: number
  acquiredCount: number
  reportedCount: number
}
