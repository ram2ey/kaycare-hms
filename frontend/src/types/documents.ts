export interface DocumentResponse {
  documentId: string;
  patientId: string;
  patientName: string;
  consultationId: string | null;
  fileName: string;
  contentType: string;
  fileSizeBytes: number;
  category: string;
  description: string | null;
  uploadedByName: string;
  createdAt: string;
}

export const DOCUMENT_CATEGORIES = [
  'LabResult', 'Prescription', 'Referral', 'Consent', 'Report', 'Other',
];

export function formatBytes(bytes: number): string {
  if (bytes < 1024) return `${bytes} B`;
  if (bytes < 1024 * 1024) return `${(bytes / 1024).toFixed(1)} KB`;
  return `${(bytes / 1024 / 1024).toFixed(1)} MB`;
}
