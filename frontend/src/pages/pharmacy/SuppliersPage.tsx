import { useState, useEffect, useCallback } from 'react';
import { getSuppliers, createSupplier, updateSupplier, deactivateSupplier } from '../../api/pharmacy';
import type { SupplierResponse, SaveSupplierRequest } from '../../types/pharmacy';

const inp = 'w-full px-3 py-2 border border-gray-300 rounded-lg text-sm focus:outline-none focus:ring-2 focus:ring-blue-500';

const emptyForm = (): SaveSupplierRequest => ({
  name: '', contactName: '', phone: '', email: '', address: '', notes: '', isActive: true,
});

export default function SuppliersPage() {
  const [suppliers, setSuppliers]       = useState<SupplierResponse[]>([]);
  const [loading, setLoading]           = useState(true);
  const [error, setError]               = useState('');
  const [showInactive, setShowInactive] = useState(false);

  const [editTarget, setEditTarget] = useState<SupplierResponse | null>(null);
  const [showForm, setShowForm]     = useState(false);
  const [form, setForm]             = useState<SaveSupplierRequest>(emptyForm());
  const [saving, setSaving]         = useState(false);

  const load = useCallback(async () => {
    setLoading(true);
    setError('');
    try {
      setSuppliers(await getSuppliers({ activeOnly: showInactive ? undefined : true }));
    } catch {
      setError('Failed to load suppliers.');
    } finally {
      setLoading(false);
    }
  }, [showInactive]);

  useEffect(() => { load(); }, [load]);

  function openCreate() {
    setEditTarget(null);
    setForm(emptyForm());
    setShowForm(true);
  }

  function openEdit(s: SupplierResponse) {
    setEditTarget(s);
    setForm({
      name: s.name, contactName: s.contactName ?? '', phone: s.phone ?? '',
      email: s.email ?? '', address: s.address ?? '', notes: s.notes ?? '',
      isActive: s.isActive,
    });
    setShowForm(true);
  }

  async function handleSubmit(e: React.FormEvent) {
    e.preventDefault();
    setSaving(true);
    try {
      const payload: SaveSupplierRequest = {
        ...form,
        contactName: form.contactName || undefined,
        phone:       form.phone       || undefined,
        email:       form.email       || undefined,
        address:     form.address     || undefined,
        notes:       form.notes       || undefined,
      };
      if (editTarget) {
        await updateSupplier(editTarget.supplierId, payload);
      } else {
        await createSupplier(payload);
      }
      setShowForm(false);
      await load();
    } catch (err) {
      const msg = (err as { response?: { data?: { error?: string } } })?.response?.data?.error ?? 'Save failed.';
      alert(msg);
    } finally {
      setSaving(false);
    }
  }

  async function handleDeactivate(s: SupplierResponse) {
    if (!confirm(`Deactivate "${s.name}"?`)) return;
    try {
      await deactivateSupplier(s.supplierId);
      await load();
    } catch {
      alert('Failed to deactivate supplier.');
    }
  }

  return (
    <div className="p-6 max-w-5xl">
      {/* Header */}
      <div className="flex items-center justify-between mb-6">
        <div>
          <h2 className="text-2xl font-semibold text-gray-800">Suppliers</h2>
          <p className="text-sm text-gray-500 mt-1">{suppliers.length} supplier{suppliers.length !== 1 ? 's' : ''}</p>
        </div>
        <button onClick={openCreate}
          className="px-4 py-2 bg-blue-700 hover:bg-blue-800 text-white text-sm font-medium rounded-lg transition-colors">
          + Add Supplier
        </button>
      </div>

      {/* Filters */}
      <div className="flex items-center gap-4 mb-5">
        <label className="flex items-center gap-2 text-sm text-gray-600 cursor-pointer">
          <input type="checkbox" checked={showInactive} onChange={e => setShowInactive(e.target.checked)} className="rounded" />
          Show inactive
        </label>
      </div>

      {loading ? (
        <div className="text-gray-400 py-8">Loading…</div>
      ) : error ? (
        <div className="text-red-600 py-4">{error}</div>
      ) : suppliers.length === 0 ? (
        <div className="bg-white rounded-xl border border-gray-200 px-6 py-12 text-center text-gray-400">
          No suppliers found. Add your first supplier.
        </div>
      ) : (
        <div className="bg-white rounded-xl border border-gray-200 overflow-hidden">
          <table className="w-full text-sm">
            <thead className="border-b border-gray-100 bg-gray-50">
              <tr>
                <th className="text-left px-4 py-3 font-medium text-gray-500 text-xs uppercase">Supplier</th>
                <th className="text-left px-4 py-3 font-medium text-gray-500 text-xs uppercase">Contact</th>
                <th className="text-left px-4 py-3 font-medium text-gray-500 text-xs uppercase">Phone / Email</th>
                <th className="text-left px-4 py-3 font-medium text-gray-500 text-xs uppercase">Status</th>
                <th className="px-4 py-3"></th>
              </tr>
            </thead>
            <tbody className="divide-y divide-gray-50">
              {suppliers.map(s => (
                <tr key={s.supplierId} className={s.isActive ? '' : 'opacity-50'}>
                  <td className="px-4 py-3">
                    <p className="font-medium text-gray-800">{s.name}</p>
                    {s.address && <p className="text-xs text-gray-400 mt-0.5 truncate max-w-xs">{s.address}</p>}
                  </td>
                  <td className="px-4 py-3 text-gray-600">{s.contactName ?? '—'}</td>
                  <td className="px-4 py-3 text-gray-500 text-xs">
                    {s.phone && <div>{s.phone}</div>}
                    {s.email && <div className="text-blue-600">{s.email}</div>}
                    {!s.phone && !s.email && '—'}
                  </td>
                  <td className="px-4 py-3">
                    <span className={`text-xs font-medium px-2 py-0.5 rounded-full ${s.isActive ? 'bg-green-100 text-green-700' : 'bg-gray-100 text-gray-500'}`}>
                      {s.isActive ? 'Active' : 'Inactive'}
                    </span>
                  </td>
                  <td className="px-4 py-3 text-right space-x-3 whitespace-nowrap">
                    <button onClick={() => openEdit(s)} className="text-xs text-gray-500 hover:underline">Edit</button>
                    {s.isActive && (
                      <button onClick={() => handleDeactivate(s)} className="text-xs text-red-500 hover:underline">Deactivate</button>
                    )}
                  </td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>
      )}

      {/* Create / Edit modal */}
      {showForm && (
        <div className="fixed inset-0 bg-black/40 flex items-start justify-center z-50 overflow-y-auto py-10">
          <div className="bg-white rounded-2xl shadow-xl p-6 w-full max-w-lg mx-4">
            <h3 className="text-lg font-semibold text-gray-800 mb-5">
              {editTarget ? 'Edit Supplier' : 'Add Supplier'}
            </h3>
            <form onSubmit={handleSubmit} className="space-y-4">
              <div>
                <label className="block text-xs text-gray-600 mb-1">Supplier Name *</label>
                <input required value={form.name}
                  onChange={e => setForm(f => ({ ...f, name: e.target.value }))}
                  placeholder="e.g. MedSupply Ghana Ltd" className={inp} />
              </div>
              <div className="grid grid-cols-2 gap-4">
                <div>
                  <label className="block text-xs text-gray-600 mb-1">Contact Person</label>
                  <input value={form.contactName ?? ''}
                    onChange={e => setForm(f => ({ ...f, contactName: e.target.value }))}
                    placeholder="Full name" className={inp} />
                </div>
                <div>
                  <label className="block text-xs text-gray-600 mb-1">Phone</label>
                  <input value={form.phone ?? ''}
                    onChange={e => setForm(f => ({ ...f, phone: e.target.value }))}
                    placeholder="+233 XX XXX XXXX" className={inp} />
                </div>
                <div>
                  <label className="block text-xs text-gray-600 mb-1">Email</label>
                  <input type="email" value={form.email ?? ''}
                    onChange={e => setForm(f => ({ ...f, email: e.target.value }))}
                    placeholder="supplier@example.com" className={inp} />
                </div>
                <div>
                  <label className="block text-xs text-gray-600 mb-1">Active</label>
                  <label className="flex items-center gap-2 text-sm text-gray-700 cursor-pointer mt-2">
                    <input type="checkbox" checked={form.isActive}
                      onChange={e => setForm(f => ({ ...f, isActive: e.target.checked }))}
                      className="rounded" />
                    Active
                  </label>
                </div>
              </div>
              <div>
                <label className="block text-xs text-gray-600 mb-1">Address</label>
                <input value={form.address ?? ''}
                  onChange={e => setForm(f => ({ ...f, address: e.target.value }))}
                  placeholder="Street, city, region" className={inp} />
              </div>
              <div>
                <label className="block text-xs text-gray-600 mb-1">Notes</label>
                <textarea rows={2} value={form.notes ?? ''}
                  onChange={e => setForm(f => ({ ...f, notes: e.target.value }))}
                  placeholder="Optional notes…"
                  className={inp + ' resize-none'} />
              </div>
              <div className="flex gap-3 justify-end pt-2">
                <button type="button" onClick={() => setShowForm(false)}
                  className="px-4 py-2 text-sm text-gray-600 border border-gray-300 rounded-lg hover:bg-gray-50">Cancel</button>
                <button type="submit" disabled={saving}
                  className="px-4 py-2 text-sm font-medium bg-blue-700 hover:bg-blue-800 text-white rounded-lg disabled:opacity-50 transition-colors">
                  {saving ? 'Saving…' : editTarget ? 'Save Changes' : 'Add Supplier'}
                </button>
              </div>
            </form>
          </div>
        </div>
      )}
    </div>
  );
}
