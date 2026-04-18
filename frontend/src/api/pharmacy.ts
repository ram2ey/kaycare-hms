import apiClient from './client';
import type {
  DrugInventoryResponse,
  SaveDrugRequest,
  StockMovementResponse,
  RecordMovementRequest,
  ReorderAlertResponse,
  SupplierResponse,
  SaveSupplierRequest,
  PurchaseOrderSummaryResponse,
  PurchaseOrderDetailResponse,
  SavePurchaseOrderRequest,
  ReceiveGoodsRequest,
  CSRegisterDrugEntry,
} from '../types/pharmacy';

export const getDrugs = (params?: { activeOnly?: boolean; lowStockOnly?: boolean; category?: string }) =>
  apiClient.get<DrugInventoryResponse[]>('/pharmacy/drugs', { params }).then((r) => r.data);

export const getDrug = (id: string) =>
  apiClient.get<DrugInventoryResponse>(`/pharmacy/drugs/${id}`).then((r) => r.data);

export const createDrug = (data: SaveDrugRequest) =>
  apiClient.post<DrugInventoryResponse>('/pharmacy/drugs', data).then((r) => r.data);

export const updateDrug = (id: string, data: SaveDrugRequest) =>
  apiClient.put<DrugInventoryResponse>(`/pharmacy/drugs/${id}`, data).then((r) => r.data);

export const deactivateDrug = (id: string) =>
  apiClient.delete<DrugInventoryResponse>(`/pharmacy/drugs/${id}`).then((r) => r.data);

export const getDrugMovements = (drugId: string) =>
  apiClient.get<StockMovementResponse[]>(`/pharmacy/drugs/${drugId}/movements`).then((r) => r.data);

export const recordMovement = (drugId: string, data: RecordMovementRequest) =>
  apiClient.post<StockMovementResponse>(`/pharmacy/drugs/${drugId}/movements`, data).then((r) => r.data);

export const getReorderAlerts = () =>
  apiClient.get<ReorderAlertResponse[]>('/pharmacy/reorder-alerts').then((r) => r.data);

// ── Suppliers ─────────────────────────────────────────────────────────────────

export const getSuppliers = (params?: { activeOnly?: boolean }) =>
  apiClient.get<SupplierResponse[]>('/pharmacy/suppliers', { params }).then((r) => r.data);

export const getSupplier = (id: string) =>
  apiClient.get<SupplierResponse>(`/pharmacy/suppliers/${id}`).then((r) => r.data);

export const createSupplier = (data: SaveSupplierRequest) =>
  apiClient.post<SupplierResponse>('/pharmacy/suppliers', data).then((r) => r.data);

export const updateSupplier = (id: string, data: SaveSupplierRequest) =>
  apiClient.put<SupplierResponse>(`/pharmacy/suppliers/${id}`, data).then((r) => r.data);

export const deactivateSupplier = (id: string) =>
  apiClient.delete<SupplierResponse>(`/pharmacy/suppliers/${id}`).then((r) => r.data);

// ── Purchase Orders ───────────────────────────────────────────────────────────

export const getPurchaseOrders = (params?: { status?: string; supplierId?: string }) =>
  apiClient.get<PurchaseOrderSummaryResponse[]>('/pharmacy/purchase-orders', { params }).then((r) => r.data);

export const getPurchaseOrder = (id: string) =>
  apiClient.get<PurchaseOrderDetailResponse>(`/pharmacy/purchase-orders/${id}`).then((r) => r.data);

export const createPurchaseOrder = (data: SavePurchaseOrderRequest) =>
  apiClient.post<PurchaseOrderDetailResponse>('/pharmacy/purchase-orders', data).then((r) => r.data);

export const updatePurchaseOrder = (id: string, data: SavePurchaseOrderRequest) =>
  apiClient.put<PurchaseOrderDetailResponse>(`/pharmacy/purchase-orders/${id}`, data).then((r) => r.data);

export const placePurchaseOrder = (id: string) =>
  apiClient.post<PurchaseOrderDetailResponse>(`/pharmacy/purchase-orders/${id}/place`).then((r) => r.data);

export const receiveGoods = (id: string, data: ReceiveGoodsRequest) =>
  apiClient.post<PurchaseOrderDetailResponse>(`/pharmacy/purchase-orders/${id}/receive`, data).then((r) => r.data);

export const cancelPurchaseOrder = (id: string) =>
  apiClient.post<PurchaseOrderDetailResponse>(`/pharmacy/purchase-orders/${id}/cancel`).then((r) => r.data);

// ── CS Register ───────────────────────────────────────────────────────────────

export const getCSRegister = (params?: { from?: string; to?: string }) =>
  apiClient.get<CSRegisterDrugEntry[]>('/pharmacy/cs-register', { params }).then((r) => r.data);

export const downloadCSRegisterReport = async (params?: { from?: string; to?: string }) => {
  const res = await apiClient.get('/pharmacy/cs-register/report', { params, responseType: 'blob' });
  const url = URL.createObjectURL(res.data);
  const a   = document.createElement('a');
  a.href    = url;
  a.download = `cs-register-${new Date().toISOString().slice(0, 10)}.pdf`;
  a.click();
  URL.revokeObjectURL(url);
};
