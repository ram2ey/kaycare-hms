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

// Mirrors KayCare.Core.Constants.DocumentConstants — client-side check only, the server enforces
// this regardless. Kept in sync manually since the two projects don't share types.
export const MAX_DOCUMENT_SIZE_BYTES = 25 * 1024 * 1024; // 25 MB
export const ALLOWED_DOCUMENT_TYPES = [
  'application/pdf',
  'image/jpeg',
  'image/png',
  'image/tiff',
  'application/msword',
  'application/vnd.openxmlformats-officedocument.wordprocessingml.document',
];

export function validateDocumentFile(file: File): string | null {
  if (file.size > MAX_DOCUMENT_SIZE_BYTES) {
    return `File exceeds the ${MAX_DOCUMENT_SIZE_BYTES / (1024 * 1024)}MB limit.`;
  }
  if (!ALLOWED_DOCUMENT_TYPES.includes(file.type)) {
    return `File type "${file.type || 'unknown'}" is not allowed.`;
  }
  return null;
}

export function formatBytes(bytes: number): string {
  if (bytes < 1024) return `${bytes} B`;
  if (bytes < 1024 * 1024) return `${(bytes / 1024).toFixed(1)} KB`;
  return `${(bytes / 1024 / 1024).toFixed(1)} MB`;
}
