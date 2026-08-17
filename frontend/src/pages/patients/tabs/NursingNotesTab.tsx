import { useState, useEffect } from 'react';
import { getNursingNotesForPatient, addNursingNote, deleteNursingNote } from '../../../api/nursing';
import type { NursingNoteResponse, AddNursingNoteRequest } from '../../../types/nursing';
import { useAuth } from '../../../contexts/AuthContext';
import { Roles } from '../../../types';
import { ConfirmDialog } from '../../../components/ConfirmDialog';

const NOTE_TYPES = ['General', 'Observation', 'Medication', 'Procedure', 'Handover', 'Incident'];

export default function NursingNotesTab({ patientId }: { patientId: string }) {
  const { user } = useAuth();
  const [notes, setNotes]       = useState<NursingNoteResponse[]>([]);
  const [loading, setLoading]   = useState(true);
  const [showForm, setShowForm] = useState(false);
  const [form, setForm]         = useState<AddNursingNoteRequest>({ noteType: 'General', note: '' });
  const [saving, setSaving]     = useState(false);
  const [saveError, setSaveError] = useState('');
  const [confirmAction, setConfirmAction] = useState<null | { message: string; run: () => void }>(null);

  useEffect(() => {
    getNursingNotesForPatient(patientId)
      .then((data) => setNotes(Array.isArray(data) ? data : []))
      .catch(() => setNotes([]))
      .finally(() => setLoading(false));
  }, [patientId]);

  const canAdd    = [Roles.Nurse, Roles.Doctor, Roles.Admin, Roles.SuperAdmin].includes(user?.role as never);
  const canDelete = [Roles.Nurse, Roles.Doctor, Roles.Admin, Roles.SuperAdmin].includes(user?.role as never);

  async function handleSubmit(e: React.FormEvent) {
    e.preventDefault();
    if (!form.note.trim()) return;
    setSaving(true);
    setSaveError('');
    try {
      const n = await addNursingNote(patientId, form);
      setNotes(prev => [n, ...prev]);
      setForm({ noteType: 'General', note: '' });
      setShowForm(false);
    } catch (err: unknown) {
      const msg = (err as { response?: { data?: { message?: string } } })?.response?.data?.message;
      setSaveError(msg || 'Failed to add note.');
    } finally { setSaving(false); }
  }

  async function handleDelete(id: string) {
    await deleteNursingNote(id);
    setNotes(prev => prev.filter(n => n.nursingNoteId !== id));
  }

  if (loading) return <div className="text-gray-400 text-sm">Loading…</div>;

  return (
    <div className="max-w-3xl">
      <div className="flex items-center justify-between mb-4">
        <h3 className="text-sm font-semibold text-gray-700">Nursing Notes ({notes.length})</h3>
        {canAdd && (
          <button onClick={() => setShowForm(s => !s)}
            className="px-3 py-1.5 bg-blue-700 text-white rounded-lg hover:bg-blue-800 text-sm font-medium">
            {showForm ? 'Cancel' : '+ Add Note'}
          </button>
        )}
      </div>

      {showForm && (
        <form onSubmit={handleSubmit} className="bg-white rounded-xl border border-gray-200 p-5 mb-4">
          <div className="grid grid-cols-4 gap-4">
            <div>
              <label htmlFor="nursing-note-type" className="block text-xs text-gray-600 mb-1">Type</label>
              <select id="nursing-note-type" value={form.noteType} onChange={e => setForm(f => ({ ...f, noteType: e.target.value }))}
                className="w-full px-3 py-2 border border-gray-300 rounded-lg text-sm">
                {NOTE_TYPES.map(t => <option key={t}>{t}</option>)}
              </select>
            </div>
            <div className="col-span-3">
              <label htmlFor="nursing-note-text" className="block text-xs text-gray-600 mb-1">Note *</label>
              <textarea id="nursing-note-text" required rows={3} value={form.note}
                onChange={e => setForm(f => ({ ...f, note: e.target.value }))}
                className="w-full px-3 py-2 border border-gray-300 rounded-lg text-sm resize-none"
                placeholder="Enter nursing note…" />
            </div>
          </div>
          {saveError && <p className="mt-2 text-sm text-red-600">{saveError}</p>}
          <div className="flex justify-end mt-3">
            <button type="submit" disabled={saving}
              className="bg-blue-700 hover:bg-blue-800 text-white text-sm font-medium px-4 py-2 rounded-lg disabled:opacity-50">
              {saving ? 'Saving…' : 'Save Note'}
            </button>
          </div>
        </form>
      )}

      {notes.length === 0 ? (
        <p className="text-sm text-gray-400">No nursing notes recorded.</p>
      ) : (
        <div className="space-y-3">
          {notes.map(n => (
            <div key={n.nursingNoteId} className="bg-white rounded-xl border border-gray-200 p-4">
              <div className="flex items-start justify-between">
                <div className="flex items-center gap-2">
                  <span className="text-xs font-medium bg-teal-100 text-teal-700 px-2 py-0.5 rounded-full">{n.noteType}</span>
                  <span className="text-xs text-gray-500">{n.authorName} · {new Date(n.createdAt).toLocaleString()}</span>
                </div>
                {canDelete && (
                  <button onClick={() => setConfirmAction({ message: 'Delete this note?', run: () => handleDelete(n.nursingNoteId) })} className="text-xs text-red-500 hover:text-red-700">Delete</button>
                )}
              </div>
              <p className="text-sm text-gray-800 mt-2 whitespace-pre-wrap">{n.note}</p>
            </div>
          ))}
        </div>
      )}

      <ConfirmDialog
        open={!!confirmAction}
        title="Confirm"
        message={confirmAction?.message ?? ''}
        danger
        onConfirm={() => { confirmAction?.run(); setConfirmAction(null); }}
        onCancel={() => setConfirmAction(null)}
      />
    </div>
  );
}
