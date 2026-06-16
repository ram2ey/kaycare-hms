import apiClient from './client'
import type {
  ImagingProcedureItem,
  RadiologyOrderSummary,
  RadiologyOrderDetail,
  CreateRadiologyOrderRequest,
  RadiologyReportRequest,
  RadiologyOrderItemResponse,
  RadiologyStatsResponse,
} from '../types/radiology'

export const getProcedureCatalog = () =>
  apiClient.get<ImagingProcedureItem[]>('/radiology-orders/catalog').then((r) => r.data)

export const getRadiologyWorklist = (date?: string, status?: string) =>
  apiClient.get<RadiologyOrderSummary[]>('/radiology-orders/worklist', { params: { date, status } }).then((r) => r.data)

export const getRadiologyOrdersForPatient = (patientId: string) =>
  apiClient.get<RadiologyOrderSummary[]>(`/radiology-orders/patient/${patientId}`).then((r) => r.data)

export const getRadiologyOrder = (id: string) =>
  apiClient.get<RadiologyOrderDetail>(`/radiology-orders/${id}`).then((r) => r.data)

export const createRadiologyOrder = (data: CreateRadiologyOrderRequest) =>
  apiClient.post<RadiologyOrderDetail>('/radiology-orders', data).then((r) => r.data)

export const markAcquired = (itemId: string) =>
  apiClient.post<RadiologyOrderItemResponse>(`/radiology-orders/items/${itemId}/acquire`).then((r) => r.data)

export const enterReport = (itemId: string, data: RadiologyReportRequest) =>
  apiClient.post<RadiologyOrderItemResponse>(`/radiology-orders/items/${itemId}/report`, data).then((r) => r.data)

export const signItem = (itemId: string) =>
  apiClient.post<RadiologyOrderItemResponse>(`/radiology-orders/items/${itemId}/sign`).then((r) => r.data)

export const downloadRadiologyReport = (id: string) =>
  apiClient.get(`/radiology-orders/${id}/report`, { responseType: 'blob' }).then((r) => r.data as Blob)

export const uploadPacsStudy = (itemId: string, file: File) => {
  const formData = new FormData()
  formData.append('file', file)
  return apiClient.post<RadiologyOrderItemResponse>(`/radiology-orders/items/${itemId}/pacs-upload`, formData, {
    headers: {
      'Content-Type': 'multipart/form-data',
    },
  }).then((r) => r.data)
}

export const getRadiologyStats = () =>
  apiClient.get<RadiologyStatsResponse>('/radiology-orders/stats').then((r) => r.data)
