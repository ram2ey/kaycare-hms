import { useState, useEffect } from 'react';
import { useParams } from 'react-router-dom';
import { getNursingNotesForPatient, addNursingNote, deleteNursingNote } from '../../api/nursing';
import type { NursingNoteResponse, AddNursingNoteRequest } from '../../types/nursing';
import { NOTE_TYPES } from '../../types/nursing';
import { useAuth } from '../../contexts/AuthContext';
import { ConfirmDialog } from '../../components/ConfirmDialog';

const NOTE_TYPE_COLORS: Record<string, string> = {
  General: 'bg-gray-100 text-gray-700',
  Assessment: 'bg-blue-100 text-blue-700',
  Handover: 'bg-purple-100 text-purple-700',
  Procedure: 'bg-orange-100 text-orange-700',
};

export default function NursingNotesPage() {
  const { patientId } = useParams<{ patientId: string }>();
  const { user } = useAuth();
  const [notes, setNotes] = useState<NursingNoteResponse[]>([]);
  const [loading, setLoading] = useState(true);
  const [showModal, setShowModal] = useState(false);
  const [saving, setSaving] = useState(false);
  const [form, setForm] = useState<AddNursingNoteRequest>({ noteType: 'General', note: '' });
  const [confirmAction, setConfirmAction] = useState<null | { message: string; run: () => void }>(null);

  useEffect(() => { load(); }, [patientId]);

  const load = async () => {
    if (!patientId) return;
    setLoading(true);
    try { setNotes(await getNursingNotesForPatient(patientId)); }
    finally { setLoading(false); }
  };

  const handleSave = async () => {
    if (!patientId || !form.note.trim()) return;
    setSaving(true);
    try {
      await addNursingNote(patientId, form);
      setShowModal(false);
      setForm({ noteType: 'General', note: '' });
      await load();
    } finally { setSaving(false); }
  };

  const handleDelete = async (noteId: string) => {
    await deleteNursingNote(noteId);
    await load();
  };

  return (
    <div className="p-6 max-w-3xl mx-auto">
      <div className="flex items-center justify-between mb-6">
        <h1 className="text-2xl font-bold text-gray-900">Nursing Notes</h1>
        <button onClick={() => setShowModal(true)} className="btn-primary">+ Add Note</button>
      </div>

      {loading ? (
        <div className="text-center py-12 text-gray-400">Loading…</div>
      ) : notes.length === 0 ? (
        <div className="text-center py-12 text-gray-400">No nursing notes yet.</div>
      ) : (
        <div className="space-y-4">
          {notes.map(n => (
            <div key={n.nursingNoteId} className="bg-white border border-gray-200 rounded-xl p-5">
              <div className="flex items-start justify-between gap-3 mb-3">
                <div className="flex items-center gap-2 flex-wrap">
                  <span className={`text-xs font-semibold px-2 py-0.5 rounded-full ${NOTE_TYPE_COLORS[n.noteType] ?? 'bg-gray-100 text-gray-700'}`}>
                    {n.noteType}
                  </span>
                  <span className="text-sm font-medium text-gray-700">{n.authorName}</span>
                  <span className="text-xs text-gray-400">
                    {new Date(n.createdAt).toLocaleDateString()}{' '}
                    {new Date(n.createdAt).toLocaleTimeString([], { hour: '2-digit', minute: '2-digit' })}
                  </span>
                </div>
                {n.authorName === user?.fullName && (
                  <button
                    onClick={() => setConfirmAction({ message: 'Delete this nursing note?', run: () => handleDelete(n.nursingNoteId) })}
                    className="text-xs text-red-500 hover:text-red-700 flex-shrink-0"
                  >
                    Delete
                  </button>
                )}
              </div>
              <p className="text-sm text-gray-700 whitespace-pre-wrap leading-relaxed">{n.note}</p>
            </div>
          ))}
        </div>
      )}

      {showModal && (
        <div className="fixed inset-0 bg-black/40 flex items-center justify-center z-50 p-4">
          <div className="bg-white rounded-2xl shadow-xl w-full max-w-lg">
            <div className="px-6 py-4 border-b border-gray-100 flex items-center justify-between">
              <h2 className="font-semibold text-gray-900">Add Nursing Note</h2>
              <button onClick={() => setShowModal(false)} aria-label="Close" className="text-gray-400 hover:text-gray-600 text-xl">×</button>
            </div>
            <div className="p-6 space-y-4">
              <div>
                <span id="note-type-label" className="block text-xs font-medium text-gray-600 mb-2">Note Type</span>
                <div role="group" aria-labelledby="note-type-label" className="flex flex-wrap gap-2">
                  {NOTE_TYPES.map(t => (
                    <button
                      key={t}
                      onClick={() => setForm(f => ({ ...f, noteType: t }))}
                      className={`px-3 py-1.5 rounded-lg text-sm font-medium border transition ${
                        form.noteType === t
                          ? 'bg-indigo-600 text-white border-indigo-600'
                          : 'bg-white text-gray-600 border-gray-300 hover:border-indigo-400'
                      }`}
                    >
                      {t}
                    </button>
                  ))}
                </div>
              </div>
              <div>
                <label htmlFor="nursing-note-content" className="block text-xs font-medium text-gray-600 mb-1">Note</label>
                <textarea
                  id="nursing-note-content"
                  rows={6}
                  className="w-full border border-gray-300 rounded-lg px-3 py-2 text-sm resize-none"
                  placeholder="Enter nursing note…"
                  value={form.note}
                  onChange={e => setForm(f => ({ ...f, note: e.target.value }))}
                />
              </div>
            </div>
            <div className="px-6 py-4 border-t border-gray-100 flex justify-end gap-3">
              <button onClick={() => setShowModal(false)} className="btn-outline">Cancel</button>
              <button onClick={handleSave} disabled={saving || !form.note.trim()} className="btn-primary">
                {saving ? 'Saving…' : 'Save Note'}
              </button>
            </div>
          </div>
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
