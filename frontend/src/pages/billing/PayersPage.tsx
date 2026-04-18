import { useState, useEffect } from 'react';
import { Link } from 'react-router-dom';
import { getPayers, createPayer, updatePayer, deletePayer } from '../../api/payers';
import type { PayerResponse, SavePayerRequest } from '../../types/payers';
import { PAYER_TYPES, PAYER_TYPE_LABELS } from '../../types/payers';

const inp = 'w-full px-3 py-2 border border-gray-300 rounded-lg text-sm focus:outline-none focus:ring-2 focus:ring-blue-500';

const emptyForm = (): SavePayerRequest => ({
  name: '', type: 'NHIS', contactPhone: '', contactEmail: '', notes: '', isActive: true,
});

const TYPE_COLORS: Record<string, string> = {
  NHIS:             'bg-green-100 text-green-700',
  PrivateInsurance: 'bg-blue-100 text-blue-700',
  Corporate:        'bg-purple-100 text-purple-700',
  Government:       'bg-orange-100 text-orange-700',
};

export default function PayersPage() {
  const [payers, setPayers]         = useState<PayerResponse[]>([]);
  const [loading, setLoading]       = useState(true);
  const [showAll, setShowAll]       = useState(false);
  const [showModal, setShowModal]   = useState(false);
  const [editing, setEditing]       = useState<PayerResponse | null>(null);
  const [form, setForm]             = useState<SavePayerRequest>(emptyForm());
  const [saving, setSaving]         = useState(false);
  const [formError, setFormError]   = useState('');

  function load(activeOnly: boolean) {
    setLoading(true);
    getPayers(activeOnly)
      .then(setPayers)
      .catch(() => {})
      .finally(() => setLoading(false));
  }

  useEffect(() => { load(!showAll); }, [showAll]);

  function openCreate() {
    setEditing(null);
    setForm(emptyForm());
    setFormError('');
    setShowModal(true);
  }

  function openEdit(p: PayerResponse) {
    setEditing(p);
    setForm({
      name:         p.name,
      type:         p.type,
      contactPhone: p.contactPhone ?? '',
      contactEmail: p.contactEmail ?? '',
      notes:        p.notes ?? '',
      isActive:     p.isActive,
    });
    setFormError('');
    setShowModal(true);
  }

  async function handleSave(e: React.FormEvent) {
    e.preventDefault();
    setSaving(true);
    setFormError('');
    try {
      const payload: SavePayerRequest = {
        ...form,
        contactPhone: form.contactPhone || undefined,
        contactEmail: form.contactEmail || undefined,
        notes:        form.notes        || undefined,
      };
      if (editing) {
        await updatePayer(editing.payerId, payload);
      } else {
        await createPayer(payload);
      }
      setShowModal(false);
      load(!showAll);
    } catch (err: unknown) {
      const msg = (err as { response?: { data?: { message?: string } } })?.response?.data?.message;
      setFormError(msg || 'Failed to save payer.');
    } finally {
      setSaving(false);
    }
  }

  async function handleDelete(id: string, name: string) {
    if (!confirm(`Delete payer "${name}"? This cannot be undone.`)) return;
    try {
      await deletePayer(id);
      load(!showAll);
    } catch {
      alert('Failed to delete payer.');
    }
  }

  // Group by type
  const grouped = payers.reduce<Record<string, PayerResponse[]>>((acc, p) => {
    (acc[p.type] ??= []).push(p);
    return acc;
  }, {});

  return (
    <div className="p-6 max-w-4xl">
      <div className="text-sm text-gray-500 mb-4">
        <Link to="/billing" className="hover:text-blue-600">Billing</Link>
        <span className="mx-2">/</span>
        <span className="text-gray-800">Payers</span>
      </div>

      <div className="flex items-center justify-between mb-6">
        <div>
          <h2 className="text-2xl font-semibold text-gray-800">Payers</h2>
          <p className="text-sm text-gray-500 mt-0.5">Insurance companies, NHIS, corporate & government payers</p>
        </div>
        <div className="flex items-center gap-3">
          <label className="flex items-center gap-2 text-sm text-gray-600 cursor-pointer">
            <input type="checkbox" checked={showAll} onChange={(e) => setShowAll(e.target.checked)}
              className="rounded" />
            Show inactive
          </label>
          <button onClick={openCreate}
            className="px-4 py-2 bg-blue-700 hover:bg-blue-800 text-white text-sm font-medium rounded-lg transition-colors">
            + New Payer
          </button>
        </div>
      </div>

      {loading ? (
        <div className="text-gray-400 text-sm">Loading…</div>
      ) : payers.length === 0 ? (
        <div className="text-gray-400 text-sm bg-white rounded-xl border border-gray-200 p-8 text-center">
          No payers found. Add your first payer to get started.
        </div>
      ) : (
        <div className="space-y-6">
          {Object.entries(grouped).map(([type, items]) => (
            <section key={type} className="bg-white rounded-xl border border-gray-200 overflow-hidden">
              <div className="px-5 py-3 border-b border-gray-100 bg-gray-50 flex items-center gap-3">
                <span className={`text-xs font-semibold px-2.5 py-1 rounded-full ${TYPE_COLORS[type] ?? 'bg-gray-100 text-gray-600'}`}>
                  {PAYER_TYPE_LABELS[type] ?? type}
                </span>
                <span className="text-xs text-gray-400">{items.length} {items.length === 1 ? 'payer' : 'payers'}</span>
              </div>
              <table className="w-full text-sm">
                <thead className="border-b border-gray-100">
                  <tr>
                    <th className="text-left px-5 py-2.5 font-medium text-gray-500 text-xs uppercase">Name</th>
                    <th className="text-left px-5 py-2.5 font-medium text-gray-500 text-xs uppercase">Contact</th>
                    <th className="text-left px-5 py-2.5 font-medium text-gray-500 text-xs uppercase">Status</th>
                    <th className="px-5 py-2.5"></th>
                  </tr>
                </thead>
                <tbody className="divide-y divide-gray-50">
                  {items.map((p) => (
                    <tr key={p.payerId} className="hover:bg-gray-50">
                      <td className="px-5 py-3 font-medium text-gray-800">{p.name}</td>
                      <td className="px-5 py-3 text-gray-500 text-xs">
                        {p.contactPhone && <div>{p.contactPhone}</div>}
                        {p.contactEmail && <div>{p.contactEmail}</div>}
                        {!p.contactPhone && !p.contactEmail && '—'}
                      </td>
                      <td className="px-5 py-3">
                        <span className={`text-xs font-medium px-2 py-0.5 rounded-full ${p.isActive ? 'bg-green-100 text-green-700' : 'bg-gray-100 text-gray-400'}`}>
                          {p.isActive ? 'Active' : 'Inactive'}
                        </span>
                      </td>
                      <td className="px-5 py-3 text-right">
                        <div className="flex items-center justify-end gap-3">
                          <button onClick={() => openEdit(p)}
                            className="text-xs text-blue-600 hover:underline">Edit</button>
                          <button onClick={() => handleDelete(p.payerId, p.name)}
                            className="text-xs text-red-400 hover:text-red-600 hover:underline">Delete</button>
                        </div>
                      </td>
                    </tr>
                  ))}
                </tbody>
              </table>
            </section>
          ))}
        </div>
      )}

      {/* Create / Edit modal */}
      {showModal && (
        <div className="fixed inset-0 bg-black/40 flex items-center justify-center z-50">
          <div className="bg-white rounded-2xl shadow-xl p-6 w-full max-w-md">
            <h3 className="text-lg font-semibold text-gray-800 mb-4">
              {editing ? 'Edit Payer' : 'New Payer'}
            </h3>
            <form onSubmit={handleSave} className="space-y-3">
              <div>
                <label className="block text-xs text-gray-600 mb-1">Name *</label>
                <input required value={form.name} onChange={(e) => setForm((f) => ({ ...f, name: e.target.value }))}
                  placeholder="e.g. NHIA Ghana" className={inp} />
              </div>
              <div>
                <label className="block text-xs text-gray-600 mb-1">Type *</label>
                <select value={form.type} onChange={(e) => setForm((f) => ({ ...f, type: e.target.value }))} className={inp}>
                  {PAYER_TYPES.map((t) => (
                    <option key={t} value={t}>{PAYER_TYPE_LABELS[t]}</option>
                  ))}
                </select>
              </div>
              <div>
                <label className="block text-xs text-gray-600 mb-1">Contact Phone</label>
                <input value={form.contactPhone ?? ''} onChange={(e) => setForm((f) => ({ ...f, contactPhone: e.target.value }))}
                  placeholder="+233 …" className={inp} />
              </div>
              <div>
                <label className="block text-xs text-gray-600 mb-1">Contact Email</label>
                <input type="email" value={form.contactEmail ?? ''} onChange={(e) => setForm((f) => ({ ...f, contactEmail: e.target.value }))}
                  placeholder="claims@payer.com" className={inp} />
              </div>
              <div>
                <label className="block text-xs text-gray-600 mb-1">Notes</label>
                <textarea rows={2} value={form.notes ?? ''} onChange={(e) => setForm((f) => ({ ...f, notes: e.target.value }))}
                  className={`${inp} resize-none`} />
              </div>
              <label className="flex items-center gap-2 text-sm text-gray-700 cursor-pointer">
                <input type="checkbox" checked={form.isActive}
                  onChange={(e) => setForm((f) => ({ ...f, isActive: e.target.checked }))} className="rounded" />
                Active
              </label>

              {formError && <p className="text-xs text-red-600 bg-red-50 px-3 py-2 rounded-lg">{formError}</p>}

              <div className="flex gap-3 justify-end pt-2">
                <button type="button" onClick={() => setShowModal(false)}
                  className="px-4 py-2 text-sm text-gray-600 border border-gray-300 rounded-lg hover:bg-gray-50">
                  Cancel
                </button>
                <button type="submit" disabled={saving}
                  className="px-4 py-2 text-sm font-medium bg-blue-700 hover:bg-blue-800 disabled:bg-blue-400 text-white rounded-lg transition-colors">
                  {saving ? 'Saving…' : 'Save'}
                </button>
              </div>
            </form>
          </div>
        </div>
      )}
    </div>
  );
}
