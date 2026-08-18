import client from './client';
import type {
  LabTestCatalog,
  LabOrder,
  LabOrderDetail,
  LabOrderItem,
  LabOrderItemResponse,
} from '../types/labOrders';

export const getTestCatalog = () =>
  client.get<LabTestCatalog[]>('/lab-orders/catalog').then(r => r.data);

export const getWaitingList = (date?: string, status?: string, department?: string) =>
  client.get<LabOrder[]>('/lab-orders/waiting-list', {
    params: { date, status, department },
  }).then(r => r.data);

export const getLabOrdersByPatient = (patientId: string) =>
  client.get<LabOrder[]>(`/lab-orders/patient/${patientId}`).then(r => r.data);

export const getLabOrderById = (id: string) =>
  client.get<LabOrderDetail>(`/lab-orders/${id}`).then(r => r.data);

export const placeLabOrder = (req: {
  patientId: string;
  consultationId?: string;
  billId?: string;
  organisation: string;
  notes?: string;
  testIds: string[];
}) => client.post<LabOrderDetail>('/lab-orders', req).then(r => r.data);

export const receiveSample = (itemId: string) =>
  client.patch<LabOrderItem>(`/lab-orders/items/${itemId}/receive`).then(r => r.data);

export const enterManualResult = (
  itemId: string,
  result: string,
  notes?: string,
  unit?: string,
  referenceRange?: string,
) =>
  client.post<LabOrderItem>(`/lab-orders/items/${itemId}/result`, {
    result, notes, unit, referenceRange,
  }).then(r => r.data);

export const downloadLabReport = (orderId: string) =>
  client.get(`/lab-orders/${orderId}/report`, { responseType: 'blob' }).then(r => r.data as Blob);

export const signItem = (itemId: string) =>
  client.patch<LabOrderItem>(`/lab-orders/items/${itemId}/sign`).then(r => r.data);

export const getLabOrder = getLabOrderById;

// Polled every 10s by CriticalAlertsWidget in the background - skipAuthRedirect keeps a
// transient failure on this one call from force-navigating the whole app to /login while the
// widget's own fetchError state handles showing (and letting the user retry) the failure.
export const getCriticalAlerts = () =>
  client.get<LabOrderItemResponse[]>('/lab-orders/critical-alerts', { skipAuthRedirect: true }).then(r => r.data);

export const recordCriticalCallLog = (itemId: string, req: { recipientName: string; notes?: string }) =>
  client.post<LabOrderItemResponse>(`/lab-orders/items/${itemId}/critical-log`, req).then(r => r.data);
