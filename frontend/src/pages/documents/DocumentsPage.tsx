import { useState, useRef } from 'react';
import { getPatientDocuments, getDownloadUrl, uploadDocument, deleteDocument } from '../../api/documents';
import { searchPatients } from '../../api/patients';
import type { DocumentResponse } from '../../types/documents';
import type { PatientResponse } from '../../types/patients';
import { DOCUMENT_CATEGORIES, formatBytes } from '../../types/documents';

const ICON: Record<string, string> = {
  'application/pdf': '📄',
  'image/jpeg': '🖼️',
  'image/png': '🖼️',
  default: '📎',
};

export default function DocumentsPage() {
  const [selectedPatient, setSelectedPatient] = useState<PatientResponse | null>(null);
  const [patientQuery, setPatientQuery] = useState('');
  const [patientResults, setPatientResults] = useState<PatientResponse[]>([]);
  const [documents, setDocuments] = useState<DocumentResponse[]>([]);
  const [searching, setSearching] = useState(false);
  const [loading, setLoading] = useState(false);
  const [downloading, setDownloading] = useState('');
  const [deleting, setDeleting] = useState('');

  // Upload form
  const [showUpload, setShowUpload] = useState(false);
  const [uploadCategory, setUploadCategory] = useState('Other');
  const [uploadDescription, setUploadDescription] = useState('');
  const [uploadFile, setUploadFile] = useState<File | null>(null);
  const [uploading, setUploading] = useState(false);
  const [uploadError, setUploadError] = useState('');
  const fileRef = useRef<HTMLInputElement>(null);

  async function searchForPatient() {
    if (!patientQuery.trim()) return;
    setSearching(true);
    try {
      const res = await searchPatients({ query: patientQuery, pageSize: 5 });
      setPatientResults(res.items);
    } finally {
      setSearching(false);
    }
  }

  async function selectPatient(p: PatientResponse) {
    setSelectedPatient(p);
    setPatientResults([]);
    setPatientQuery('');
    setLoading(true);
    try {
      setDocuments(await getPatientDocuments(p.patientId));
    } finally {
      setLoading(false);
    }
  }

  async function handleDownload(doc: DocumentResponse) {
    setDownloading(doc.documentId);
    try {
      const { downloadUrl } = await getDownloadUrl(doc.documentId);
      window.open(downloadUrl, '_blank', 'noopener,noreferrer');
    } catch {
      alert('Failed to get download URL.');
    } finally {
      setDownloading('');
    }
  }

  async function handleDelete(doc: DocumentResponse) {
    if (!confirm(`Delete "${doc.fileName}"? This cannot be undone.`)) return;
    setDeleting(doc.documentId);
    try {
      await deleteDocument(doc.documentId);
      setDocuments((prev) => prev.filter((d) => d.documentId !== doc.documentId));
    } catch {
      alert('Failed to delete document.');
    } finally {
      setDeleting('');
    }
  }

  async function handleUpload(e: React.FormEvent) {
    e.preventDefault();
    if (!selectedPatient || !uploadFile) return;
    setUploading(true);
    setUploadError('');
    try {
      const doc = await uploadDocument(
        selectedPatient.patientId,
        uploadCategory,
        uploadFile,
        uploadDescription || undefined,
      );
      setDocuments((prev) => [doc, ...prev]);
      setShowUpload(false);
      setUploadFile(null);
      setUploadDescription('');
      setUploadCategory('Other');
      if (fileRef.current) fileRef.current.value = '';
    } catch (err: unknown) {
      const msg = (err as { response?: { data?: { message?: string } } })?.response?.data?.message;
      setUploadError(msg || 'Upload failed.');
    } finally {
      setUploading(false);
    }
  }

  return (
    <div className="p-6">
      <div className="flex items-center justify-between mb-6">
        <h2 className="text-2xl font-semibold text-gray-800">Documents</h2>
        {selectedPatient && (
          <button
            onClick={() => setShowUpload((s) => !s)}
            className="bg-blue-700 hover:bg-blue-800 text-white text-sm font-medium px-4 py-2 rounded-lg transition-colors"
          >
            {showUpload ? 'Cancel Upload' : '+ Upload Document'}
          </button>
        )}
      </div>

      {/* Patient search */}
      <div className="bg-white rounded-xl border border-gray-200 p-5 mb-5">
        {selectedPatient ? (
          <div className="flex items-center justify-between bg-blue-50 rounded-lg px-4 py-3">
            <div>
              <p className="font-medium text-gray-800">{selectedPatient.fullName}</p>
              <p className="text-xs text-blue-600 font-mono">{selectedPatient.medicalRecordNumber}</p>
            </div>
            <button onClick={() => { setSelectedPatient(null); setDocuments([]); setShowUpload(false); }}
              className="text-xs text-gray-500 hover:text-red-500">Change</button>
          </div>
        ) : (
          <div className="relative">
            <div className="flex gap-2">
              <input type="text" value={patientQuery}
                onChange={(e) => setPatientQuery(e.target.value)}
                onKeyDown={(e) => e.key === 'Enter' && (e.preventDefault(), searchForPatient())}
                placeholder="Search patient by name, MRN, or phone…"
                className="flex-1 px-3 py-2 border border-gray-300 rounded-lg text-sm focus:outline-none focus:ring-2 focus:ring-blue-500"
              />
              <button onClick={searchForPatient} disabled={searching}
                className="px-4 py-2 bg-gray-100 hover:bg-gray-200 text-sm rounded-lg text-gray-700 transition-colors">
                {searching ? '…' : 'Search'}
              </button>
            </div>
            {patientResults.length > 0 && (
              <div className="absolute top-full mt-1 w-full bg-white border border-gray-200 rounded-lg shadow-lg z-10">
                {patientResults.map((p) => (
                  <button key={p.patientId} onClick={() => selectPatient(p)}
                    className="w-full text-left px-4 py-2.5 hover:bg-gray-50 text-sm border-b border-gray-100 last:border-0">
                    <span className="font-medium">{p.fullName}</span>
                    <span className="text-gray-400 font-mono text-xs ml-2">{p.medicalRecordNumber}</span>
                  </button>
                ))}
              </div>
            )}
          </div>
        )}
      </div>

      {/* Upload form */}
      {showUpload && selectedPatient && (
        <form onSubmit={handleUpload} className="bg-white rounded-xl border border-blue-200 p-5 mb-5">
          <h3 className="text-sm font-semibold text-gray-700 uppercase tracking-wide mb-4">Upload Document</h3>
          <div className="grid grid-cols-3 gap-4 mb-4">
            <div>
              <label className="block text-xs text-gray-600 mb-1">Category *</label>
              <select value={uploadCategory} onChange={(e) => setUploadCategory(e.target.value)}
                className="w-full px-3 py-2 border border-gray-300 rounded-lg text-sm focus:outline-none focus:ring-2 focus:ring-blue-500">
                {DOCUMENT_CATEGORIES.map((c) => <option key={c}>{c}</option>)}
              </select>
            </div>
            <div className="col-span-2">
              <label className="block text-xs text-gray-600 mb-1">Description</label>
              <input value={uploadDescription} onChange={(e) => setUploadDescription(e.target.value)}
                placeholder="e.g. CBC results from 2026-03-25"
                className="w-full px-3 py-2 border border-gray-300 rounded-lg text-sm focus:outline-none focus:ring-2 focus:ring-blue-500"
              />
            </div>
          </div>
          <div className="mb-4">
            <label className="block text-xs text-gray-600 mb-1">File *</label>
            <input ref={fileRef} type="file" required
              onChange={(e) => setUploadFile(e.target.files?.[0] ?? null)}
              className="w-full text-sm text-gray-600 file:mr-3 file:py-2 file:px-4 file:rounded-lg file:border-0 file:text-sm file:font-medium file:bg-blue-50 file:text-blue-700 hover:file:bg-blue-100"
            />
            {uploadFile && (
              <p className="text-xs text-gray-400 mt-1">{uploadFile.name} · {formatBytes(uploadFile.size)}</p>
            )}
          </div>
          {uploadError && <p className="text-sm text-red-600 bg-red-50 px-3 py-2 rounded-lg mb-3">{uploadError}</p>}
          <div className="flex justify-end gap-3">
            <button type="button" onClick={() => setShowUpload(false)}
              className="px-4 py-2 border border-gray-300 rounded-lg text-sm text-gray-600 hover:bg-gray-50">
              Cancel
            </button>
            <button type="submit" disabled={uploading || !uploadFile}
              className="px-4 py-2 bg-blue-700 hover:bg-blue-800 disabled:bg-blue-400 text-white text-sm font-medium rounded-lg transition-colors">
              {uploading ? 'Uploading…' : 'Upload'}
            </button>
          </div>
        </form>
      )}

      {/* Documents list */}
      {selectedPatient && (
        <div className="bg-white rounded-xl border border-gray-200 overflow-hidden">
          <div className="px-5 py-3 border-b border-gray-100 bg-gray-50">
            <h3 className="text-sm font-semibold text-gray-700">
              Documents — {documents.length} file{documents.length !== 1 ? 's' : ''}
            </h3>
          </div>
          {loading ? (
            <p className="px-5 py-8 text-center text-gray-400 text-sm">Loading…</p>
          ) : documents.length === 0 ? (
            <p className="px-5 py-8 text-center text-gray-400 text-sm">No documents found.</p>
          ) : (
            <div className="divide-y divide-gray-100">
              {documents.map((doc) => (
                <div key={doc.documentId} className="flex items-center px-5 py-3.5 gap-4 hover:bg-gray-50">
                  <span className="text-2xl shrink-0">{ICON[doc.contentType] ?? ICON.default}</span>
                  <div className="flex-1 min-w-0">
                    <p className="text-sm font-medium text-gray-800 truncate">{doc.fileName}</p>
                    <div className="flex gap-3 mt-0.5">
                      <span className="text-xs text-gray-500">{doc.category}</span>
                      <span className="text-xs text-gray-400">{formatBytes(doc.fileSizeBytes)}</span>
                      {doc.description && <span className="text-xs text-gray-500 truncate">{doc.description}</span>}
                    </div>
                  </div>
                  <div className="text-right shrink-0">
                    <p className="text-xs text-gray-500">{doc.uploadedByName}</p>
                    <p className="text-xs text-gray-400">
                      {new Date(doc.createdAt).toLocaleDateString('en-GB', { day: 'numeric', month: 'short', year: 'numeric' })}
                    </p>
                  </div>
                  <div className="flex gap-2 shrink-0">
                    <button
                      onClick={() => handleDownload(doc)}
                      disabled={downloading === doc.documentId}
                      className="px-3 py-1.5 text-xs bg-blue-50 hover:bg-blue-100 text-blue-700 font-medium rounded-lg transition-colors disabled:opacity-50"
                    >
                      {downloading === doc.documentId ? '…' : 'Download'}
                    </button>
                    <button
                      onClick={() => handleDelete(doc)}
                      disabled={deleting === doc.documentId}
                      className="px-3 py-1.5 text-xs text-red-400 hover:text-red-600 hover:bg-red-50 rounded-lg transition-colors disabled:opacity-50"
                    >
                      {deleting === doc.documentId ? '…' : 'Delete'}
                    </button>
                  </div>
                </div>
              ))}
            </div>
          )}
        </div>
      )}

      {!selectedPatient && (
        <div className="text-center py-16 text-gray-400 text-sm">
          Search for a patient to view their documents.
        </div>
      )}
    </div>
  );
}
