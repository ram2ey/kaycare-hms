export interface ServiceCatalogItem {
  serviceCatalogItemId: string;
  name: string;
  description?: string;
  category: string;
  unitPrice: number;
  isActive: boolean;
  createdAt: string;
  updatedAt: string;
}

export interface SaveServiceCatalogItemRequest {
  name: string;
  description?: string;
  category: string;
  unitPrice: number;
  isActive: boolean;
}
