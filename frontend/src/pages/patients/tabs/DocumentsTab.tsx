import { useState, useEffect, useRef } from 'react';
import { getPatientDocuments, getDownloadUrl, uploadDocument, deleteDocument } from '../../../api/documents';
import type { DocumentResponse } from '../../../types/documents';
import { useAuth } from '../../../contexts/AuthContext';
import { Roles } from '../../../types';

const CATEGORIES = ['Clinical Report', 'Lab Result', 'Imaging', 'Consent Form', 'Referral Letter', 'Discharge Summary', 'Other'];

export default function DocumentsTab({ patientId }: { patientId: string }) {
  const { user } = useAuth();
  const [documents, setDocuments] = useState<DocumentResponse[]>([]);
  const [loading, setLoading]     = useState(true);
  const [uploading, setUploading] = useState(false);
  const [uploadError, setUploadError] = useState('');
  const [category, setCategory]   = useState('Other');
  const [description, setDescription] = useState('');
  const fileRef = useRef<HTMLInputElement>(null);

  useEffect(() => {
    getPatientDocuments(patientId)
      .then((data) => setDocuments(Array.isArray(data) ? data : []))
      .catch(() => setDocuments([]))
      .finally(() => setLoading(false));
  }, [patientId]);

  const canUpload = [Roles.Doctor, Roles.Nurse, Roles.Admin, Roles.SuperAdmin, Roles.Receptionist].includes(user?.role as never);
  const canDelete = [Roles.Admin, Roles.SuperAdmin].includes(user?.role as never);

  async function handleUpload(e: React.ChangeEvent<HTMLInputElement>) {
    const file = e.target.files?.[0];
    if (!file) return;
    setUploading(true);
    setUploadError('');
    try {
      const doc = await uploadDocument(patientId, category, file, description || undefined);
      setDocuments(prev => [doc, ...prev]);
      setDescription('');
      if (fileRef.current) fileRef.current.value = '';
    } catch (err: unknown) {
      const msg = (err as { response?: { data?: { message?: string } } })?.response?.data?.message;
      setUploadError(msg || 'Upload failed.');
    } finally { setUploading(false); }
  }

  async function handleDownload(doc: DocumentResponse) {
    const { downloadUrl } = await getDownloadUrl(doc.documentId);
    window.open(downloadUrl, '_blank');
  }

  async function handleDelete(id: string) {
    if (!confirm('Delete this document permanently?')) return;
    await deleteDocument(id);
    setDocuments(prev => prev.filter(d => d.documentId !== id));
  }

  if (loading) return <div className="text-gray-400 text-sm">Loading…</div>;

  return (
    <div className="max-w-4xl">
      {uploadError && <p className="mb-3 text-sm text-red-600 bg-red-50 px-3 py-2 rounded-lg">{uploadError}</p>}
      <div className="flex items-start justify-between mb-4">
        <h3 className="text-sm font-semibold text-gray-700">Documents ({documents.length})</h3>
        {canUpload && (
          <div className="flex items-center gap-2">
            <select value={category} onChange={e => setCategory(e.target.value)}
              className="px-3 py-1.5 border border-gray-300 rounded-lg text-sm">
              {CATEGORIES.map(c => <option key={c}>{c}</option>)}
            </select>
            <input value={description} onChange={e => setDescription(e.target.value)}
              placeholder="Description (optional)"
              className="px-3 py-1.5 border border-gray-300 rounded-lg text-sm w-48" />
            <label className={`px-3 py-1.5 rounded-lg text-sm font-medium cursor-pointer ${uploading ? 'bg-gray-300 text-gray-500' : 'bg-blue-700 text-white hover:bg-blue-800'}`}>
              {uploading ? 'Uploading…' : 'Upload File'}
              <input ref={fileRef} type="file" className="hidden" onChange={handleUpload} disabled={uploading} />
            </label>
          </div>
        )}
      </div>

      {documents.length === 0 ? (
        <p className="text-sm text-gray-400">No documents uploaded.</p>
      ) : (
        <div className="bg-white rounded-xl border border-gray-200 overflow-hidden">
          <table className="w-full text-sm">
            <thead className="bg-gray-50 border-b border-gray-200">
              <tr>
                <th className="text-left px-4 py-3 text-xs font-medium text-gray-500">Date</th>
                <th className="text-left px-4 py-3 text-xs font-medium text-gray-500">File Name</th>
                <th className="text-left px-4 py-3 text-xs font-medium text-gray-500">Category</th>
                <th className="text-left px-4 py-3 text-xs font-medium text-gray-500">Description</th>
                <th className="text-left px-4 py-3 text-xs font-medium text-gray-500">Uploaded By</th>
                <th />
              </tr>
            </thead>
            <tbody className="divide-y divide-gray-100">
              {documents.map(d => (
                <tr key={d.documentId} className="hover:bg-gray-50">
                  <td className="px-4 py-3 text-gray-500 text-xs">{new Date(d.createdAt).toLocaleDateString()}</td>
                  <td className="px-4 py-3 text-gray-800 font-medium">{d.fileName}</td>
                  <td className="px-4 py-3 text-gray-600">{d.category}</td>
                  <td className="px-4 py-3 text-gray-500">{d.description ?? '—'}</td>
                  <td className="px-4 py-3 text-gray-500 text-xs">{d.uploadedByName}</td>
                  <td className="px-4 py-3 text-right flex items-center gap-3 justify-end">
                    <button onClick={() => handleDownload(d)} className="text-blue-600 hover:underline text-xs">Download</button>
                    {canDelete && (
                      <button onClick={() => handleDelete(d.documentId)} className="text-red-500 hover:text-red-700 text-xs">Delete</button>
                    )}
                  </td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>
      )}
    </div>
  );
}
