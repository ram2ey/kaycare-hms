export interface DrugInventoryResponse {
  drugInventoryId: string;
  name: string;
  genericName: string | null;
  dosageForm: string | null;
  strength: string | null;
  unit: string;
  category: string | null;
  currentStock: number;
  reorderThreshold: number;
  isLowStock: boolean;
  unitCost: number;
  sellingPrice: number;
  isControlledSubstance: boolean;
  isActive: boolean;
  createdAt: string;
  updatedAt: string;
}

export interface SaveDrugRequest {
  name: string;
  genericName?: string;
  dosageForm?: string;
  strength?: string;
  unit: string;
  category?: string;
  reorderThreshold: number;
  unitCost: number;
  sellingPrice: number;
  isControlledSubstance: boolean;
  isActive: boolean;
}

export interface StockMovementResponse {
  stockMovementId: string;
  drugInventoryId: string;
  drugName: string;
  movementType: string;
  quantity: number;
  previousStock: number;
  newStock: number;
  referenceId: string | null;
  referenceType: string | null;
  notes: string | null;
  createdByName: string;
  createdAt: string;
}

export interface RecordMovementRequest {
  movementType: string;
  quantity: number;
  notes?: string;
}

export interface ReorderAlertResponse {
  drugInventoryId: string;
  name: string;
  genericName: string | null;
  dosageForm: string | null;
  strength: string | null;
  category: string | null;
  currentStock: number;
  reorderThreshold: number;
  deficit: number;
}

export const DRUG_UNITS = [
  'Tablet', 'Capsule', 'ml', 'mg', 'g', 'Vial', 'Ampoule',
  'Suppository', 'Patch', 'Sachet', 'Drop', 'Inhaler', 'Other',
];

export const DRUG_DOSAGE_FORMS = [
  'Tablet', 'Capsule', 'Syrup', 'Suspension', 'Injection', 'Infusion',
  'Cream', 'Ointment', 'Gel', 'Drops', 'Inhaler', 'Suppository',
  'Patch', 'Powder', 'Granules', 'Other',
];

export const DRUG_CATEGORIES = [
  'Analgesics', 'Antibiotics', 'Antimalarials', 'Antihypertensives',
  'Antidiabetics', 'ARVs', 'Antifungals', 'Antiparasitics',
  'Antihistamines', 'Vitamins & Supplements', 'GI', 'Respiratory',
  'Cardiovascular', 'Neurological', 'Psychiatric', 'Hormones',
  'Controlled Substances', 'IV Fluids', 'Vaccines', 'Other',
];

export const MOVEMENT_TYPE_LABELS: Record<string, string> = {
  Receive:      'Receive Stock',
  Dispense:     'Dispensed',
  AdjustAdd:    'Adjustment (Add)',
  AdjustDeduct: 'Adjustment (Deduct)',
  Return:       'Patient Return',
  Expire:       'Expired',
  WriteOff:     'Write-Off',
};

export const MOVEMENT_TYPE_COLORS: Record<string, string> = {
  Receive:      'bg-green-100 text-green-700',
  Dispense:     'bg-blue-100 text-blue-700',
  AdjustAdd:    'bg-teal-100 text-teal-700',
  AdjustDeduct: 'bg-orange-100 text-orange-700',
  Return:       'bg-purple-100 text-purple-700',
  Expire:       'bg-red-100 text-red-600',
  WriteOff:     'bg-gray-100 text-gray-600',
};

// Movement types available for manual recording (excludes Dispense which is auto)
export const MANUAL_MOVEMENT_TYPES = [
  'Receive', 'AdjustAdd', 'AdjustDeduct', 'Return', 'Expire', 'WriteOff',
];

// ── Suppliers ─────────────────────────────────────────────────────────────────

export interface SupplierResponse {
  supplierId: string;
  name: string;
  contactName: string | null;
  phone: string | null;
  email: string | null;
  address: string | null;
  notes: string | null;
  isActive: boolean;
  createdAt: string;
  updatedAt: string;
}

export interface SaveSupplierRequest {
  name: string;
  contactName?: string;
  phone?: string;
  email?: string;
  address?: string;
  notes?: string;
  isActive: boolean;
}

// ── Purchase Orders ───────────────────────────────────────────────────────────

export interface PurchaseOrderSummaryResponse {
  purchaseOrderId: string;
  orderNumber: string;
  supplierId: string | null;
  supplierName: string | null;
  status: string;
  orderDate: string;
  expectedDeliveryDate: string | null;
  itemCount: number;
  totalAmount: number;
  createdAt: string;
}

export interface PurchaseOrderDetailResponse {
  purchaseOrderId: string;
  orderNumber: string;
  supplierId: string | null;
  supplierName: string | null;
  supplierPhone: string | null;
  supplierEmail: string | null;
  status: string;
  orderDate: string;
  expectedDeliveryDate: string | null;
  notes: string | null;
  totalAmount: number;
  createdAt: string;
  updatedAt: string;
  items: PurchaseOrderItemResponse[];
}

export interface PurchaseOrderItemResponse {
  purchaseOrderItemId: string;
  drugInventoryId: string;
  drugName: string;
  dosageForm: string | null;
  strength: string | null;
  unit: string;
  quantity: number;
  quantityReceived: number;
  quantityPending: number;
  unitCost: number;
  totalCost: number;
  isFullyReceived: boolean;
}

export interface SavePurchaseOrderRequest {
  supplierId?: string;
  expectedDeliveryDate?: string;
  notes?: string;
  items: SavePurchaseOrderItemRequest[];
}

export interface SavePurchaseOrderItemRequest {
  drugInventoryId: string;
  quantity: number;
  unitCost: number;
}

export interface ReceiveGoodsRequest {
  items: ReceiveGoodsItemRequest[];
}

export interface ReceiveGoodsItemRequest {
  purchaseOrderItemId: string;
  quantityReceived: number;
}

export const PO_STATUS_LABELS: Record<string, string> = {
  Draft:             'Draft',
  Ordered:           'Ordered',
  PartiallyReceived: 'Partially Received',
  Received:          'Received',
  Cancelled:         'Cancelled',
};

export const PO_STATUS_COLORS: Record<string, string> = {
  Draft:             'bg-gray-100 text-gray-600',
  Ordered:           'bg-blue-100 text-blue-700',
  PartiallyReceived: 'bg-yellow-100 text-yellow-700',
  Received:          'bg-green-100 text-green-700',
  Cancelled:         'bg-red-100 text-red-600',
};

// ── CS Register ───────────────────────────────────────────────────────────────

export interface CSRegisterMovement {
  stockMovementId: string;
  date: string;
  movementType: string;
  quantityIn: number | null;
  quantityOut: number | null;
  balance: number;
  referenceType: string | null;
  notes: string | null;
  recordedBy: string;
}

export interface CSRegisterDrugEntry {
  drugInventoryId: string;
  drugName: string;
  genericName: string | null;
  dosageForm: string | null;
  strength: string | null;
  currentStock: number;
  movements: CSRegisterMovement[];
}
