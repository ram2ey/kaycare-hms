import client from './client';
import type {
  LabTestCatalog,
  LabOrder,
  LabOrderDetail,
  LabOrderItem,
} from '../types/labOrders';

export const getTestCatalog = () =>
  client.get<LabTestCatalog[]>('/api/lab-orders/catalog').then(r => r.data);

export const getWaitingList = (date?: string, status?: string, department?: string) =>
  client.get<LabOrder[]>('/api/lab-orders/waiting-list', {
    params: { date, status, department },
  }).then(r => r.data);

export const getLabOrdersByPatient = (patientId: string) =>
  client.get<LabOrder[]>(`/api/lab-orders/patient/${patientId}`).then(r => r.data);

export const getLabOrderById = (id: string) =>
  client.get<LabOrderDetail>(`/api/lab-orders/${id}`).then(r => r.data);

export const placeLabOrder = (req: {
  patientId: string;
  consultationId?: string;
  billId?: string;
  organisation: string;
  notes?: string;
  testIds: string[];
}) => client.post<LabOrderDetail>('/api/lab-orders', req).then(r => r.data);

export const receiveSample = (itemId: string) =>
  client.patch<LabOrderItem>(`/api/lab-orders/items/${itemId}/receive`).then(r => r.data);

export const enterManualResult = (
  itemId: string,
  result: string,
  notes?: string,
  unit?: string,
  referenceRange?: string,
) =>
  client.post<LabOrderItem>(`/api/lab-orders/items/${itemId}/result`, {
    result, notes, unit, referenceRange,
  }).then(r => r.data);

export const downloadLabReport = (orderId: string) =>
  client.get(`/api/lab-orders/${orderId}/report`, { responseType: 'blob' }).then(r => r.data as Blob);

export const signItem = (itemId: string) =>
  client.patch<LabOrderItem>(`/api/lab-orders/items/${itemId}/sign`).then(r => r.data);
