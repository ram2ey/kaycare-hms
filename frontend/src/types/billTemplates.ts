export interface BillTemplateItemResponse {
  billTemplateItemId: string;
  description: string;
  category: string | null;
  quantity: number;
  unitPrice: number;
  totalPrice: number;
}

export interface BillTemplateResponse {
  billTemplateId: string;
  name: string;
  description: string | null;
  category: string | null;
  isActive: boolean;
  totalValue: number;
  items: BillTemplateItemResponse[];
  createdAt: string;
  updatedAt: string;
}

export interface BillTemplateItemRequest {
  description: string;
  category?: string;
  quantity: number;
  unitPrice: number;
}

export interface SaveBillTemplateRequest {
  name: string;
  description?: string;
  category?: string;
  isActive: boolean;
  items: BillTemplateItemRequest[];
}
