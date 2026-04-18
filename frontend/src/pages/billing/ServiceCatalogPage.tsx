import { useState, useEffect } from 'react';
import { Link } from 'react-router-dom';
import { getCatalog, createCatalogItem, updateCatalogItem, deleteCatalogItem } from '../../api/serviceCatalog';
import type { ServiceCatalogItem, SaveServiceCatalogItemRequest } from '../../types/serviceCatalog';
import { BILL_CATEGORIES } from '../../types/billing';

const inp = 'w-full px-3 py-2 border border-gray-300 rounded-lg text-sm focus:outline-none focus:ring-2 focus:ring-blue-500';

const emptyForm = (): SaveServiceCatalogItemRequest => ({
  name: '', description: '', category: 'Consultation', unitPrice: 0, isActive: true,
});

function fmt(n: number) { return `GHS ${n.toFixed(2)}`; }

export default function ServiceCatalogPage() {
  const [items, setItems]           = useState<ServiceCatalogItem[]>([]);
  const [loading, setLoading]       = useState(true);
  const [showInactive, setShowInactive] = useState(false);
  const [editing, setEditing]       = useState<ServiceCatalogItem | null>(null);
  const [showForm, setShowForm]     = useState(false);
  const [form, setForm]             = useState<SaveServiceCatalogItemRequest>(emptyForm());
  const [saving, setSaving]         = useState(false);
  const [error, setError]           = useState('');
  const [filterCategory, setFilterCategory] = useState('');

  async function load() {
    setLoading(true);
    try { setItems(await getCatalog(!showInactive)); }
    finally { setLoading(false); }
  }

  useEffect(() => { load(); }, [showInactive]); // eslint-disable-line react-hooks/exhaustive-deps

  function openCreate() {
    setEditing(null);
    setForm(emptyForm());
    setError('');
    setShowForm(true);
  }

  function openEdit(item: ServiceCatalogItem) {
    setEditing(item);
    setForm({ name: item.name, description: item.description ?? '', category: item.category, unitPrice: item.unitPrice, isActive: item.isActive });
    setError('');
    setShowForm(true);
  }

  async function handleSave(e: React.FormEvent) {
    e.preventDefault();
    setSaving(true);
    setError('');
    try {
      if (editing) {
        const updated = await updateCatalogItem(editing.serviceCatalogItemId, form);
        setItems((prev) => prev.map((i) => i.serviceCatalogItemId === updated.serviceCatalogItemId ? updated : i));
      } else {
        const created = await createCatalogItem(form);
        setItems((prev) => [...prev, created]);
      }
      setShowForm(false);
    } catch {
      setError('Failed to save. Please try again.');
    } finally {
      setSaving(false);
    }
  }

  async function handleDelete(item: ServiceCatalogItem) {
    if (!confirm(`Delete "${item.name}"?`)) return;
    try {
      await deleteCatalogItem(item.serviceCatalogItemId);
      setItems((prev) => prev.filter((i) => i.serviceCatalogItemId !== item.serviceCatalogItemId));
    } catch {
      alert('Failed to delete item.');
    }
  }

  const categories = Array.from(new Set(items.map((i) => i.category))).sort();
  const visible = filterCategory ? items.filter((i) => i.category === filterCategory) : items;

  // Group by category
  const grouped = categories
    .filter((c) => !filterCategory || c === filterCategory)
    .map((cat) => ({ cat, rows: visible.filter((i) => i.category === cat) }))
    .filter((g) => g.rows.length > 0);

  return (
    <div className="p-6 max-w-4xl">
      <div className="text-sm text-gray-500 mb-4">
        <Link to="/billing" className="hover:text-blue-600">Billing</Link>
        <span className="mx-2">/</span>
        <span className="text-gray-800">Price Catalog</span>
      </div>

      <div className="flex items-center justify-between mb-6">
        <div>
          <h2 className="text-2xl font-semibold text-gray-800">Price Catalog</h2>
          <p className="text-sm text-gray-500 mt-0.5">{items.length} item{items.length !== 1 ? 's' : ''}</p>
        </div>
        <div className="flex items-center gap-3">
          <label className="flex items-center gap-2 text-sm text-gray-600 cursor-pointer">
            <input type="checkbox" checked={showInactive} onChange={(e) => setShowInactive(e.target.checked)}
              className="rounded" />
            Show inactive
          </label>
          <button onClick={openCreate}
            className="px-4 py-2 bg-blue-700 hover:bg-blue-800 text-white text-sm font-medium rounded-lg transition-colors">
            + New Item
          </button>
        </div>
      </div>

      {/* Category filter */}
      {categories.length > 1 && (
        <div className="flex flex-wrap gap-2 mb-4">
          <button onClick={() => setFilterCategory('')}
            className={`px-3 py-1 text-xs rounded-full border transition-colors ${!filterCategory ? 'bg-blue-700 text-white border-blue-700' : 'border-gray-300 text-gray-600 hover:bg-gray-50'}`}>
            All
          </button>
          {categories.map((c) => (
            <button key={c} onClick={() => setFilterCategory(c === filterCategory ? '' : c)}
              className={`px-3 py-1 text-xs rounded-full border transition-colors ${filterCategory === c ? 'bg-blue-700 text-white border-blue-700' : 'border-gray-300 text-gray-600 hover:bg-gray-50'}`}>
              {c}
            </button>
          ))}
        </div>
      )}

      {loading ? (
        <p className="text-gray-400 text-sm py-8">Loading…</p>
      ) : items.length === 0 ? (
        <div className="bg-white rounded-xl border border-gray-200 p-10 text-center">
          <p className="text-gray-500 text-sm mb-3">No items in the price catalog yet.</p>
          <button onClick={openCreate} className="text-sm text-blue-600 hover:underline">Add the first item</button>
        </div>
      ) : (
        <div className="space-y-5">
          {grouped.map(({ cat, rows }) => (
            <section key={cat} className="bg-white rounded-xl border border-gray-200 overflow-hidden">
              <div className="px-5 py-3 bg-gray-50 border-b border-gray-100">
                <h3 className="text-sm font-semibold text-gray-700">{cat}</h3>
              </div>
              <table className="w-full text-sm">
                <thead className="border-b border-gray-100">
                  <tr>
                    <th className="text-left px-5 py-2.5 font-medium text-gray-500 text-xs uppercase">Name</th>
                    <th className="text-left px-5 py-2.5 font-medium text-gray-500 text-xs uppercase hidden sm:table-cell">Description</th>
                    <th className="text-right px-5 py-2.5 font-medium text-gray-500 text-xs uppercase">Unit Price</th>
                    <th className="text-center px-5 py-2.5 font-medium text-gray-500 text-xs uppercase">Status</th>
                    <th className="px-5 py-2.5"></th>
                  </tr>
                </thead>
                <tbody className="divide-y divide-gray-50">
                  {rows.map((item) => (
                    <tr key={item.serviceCatalogItemId} className={!item.isActive ? 'opacity-50' : ''}>
                      <td className="px-5 py-3 font-medium text-gray-800">{item.name}</td>
                      <td className="px-5 py-3 text-gray-500 hidden sm:table-cell">{item.description ?? '—'}</td>
                      <td className="px-5 py-3 text-right font-mono text-gray-800">{fmt(item.unitPrice)}</td>
                      <td className="px-5 py-3 text-center">
                        <span className={`text-xs px-2 py-0.5 rounded-full font-medium ${item.isActive ? 'bg-green-100 text-green-700' : 'bg-gray-100 text-gray-500'}`}>
                          {item.isActive ? 'Active' : 'Inactive'}
                        </span>
                      </td>
                      <td className="px-5 py-3 text-right">
                        <div className="flex justify-end gap-3">
                          <button onClick={() => openEdit(item)} className="text-xs text-blue-600 hover:underline">Edit</button>
                          <button onClick={() => handleDelete(item)} className="text-xs text-red-500 hover:underline">Delete</button>
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
      {showForm && (
        <div className="fixed inset-0 bg-black/40 flex items-center justify-center z-50">
          <div className="bg-white rounded-2xl shadow-xl p-6 w-full max-w-md">
            <h3 className="text-lg font-semibold text-gray-800 mb-4">
              {editing ? 'Edit Item' : 'New Catalog Item'}
            </h3>
            <form onSubmit={handleSave} className="space-y-3">
              <div>
                <label className="block text-xs text-gray-600 mb-1">Name *</label>
                <input required value={form.name} onChange={(e) => setForm((f) => ({ ...f, name: e.target.value }))}
                  placeholder="e.g. General Consultation" className={inp} />
              </div>
              <div>
                <label className="block text-xs text-gray-600 mb-1">Category *</label>
                <select required value={form.category} onChange={(e) => setForm((f) => ({ ...f, category: e.target.value }))} className={inp}>
                  {BILL_CATEGORIES.map((c) => <option key={c}>{c}</option>)}
                </select>
              </div>
              <div>
                <label className="block text-xs text-gray-600 mb-1">Unit Price (GHS) *</label>
                <input required type="number" step="0.01" min={0} value={form.unitPrice || ''}
                  onChange={(e) => setForm((f) => ({ ...f, unitPrice: Number(e.target.value) }))}
                  placeholder="0.00" className={inp} />
              </div>
              <div>
                <label className="block text-xs text-gray-600 mb-1">Description</label>
                <input value={form.description ?? ''} onChange={(e) => setForm((f) => ({ ...f, description: e.target.value }))}
                  placeholder="Optional description" className={inp} />
              </div>
              <div className="flex items-center gap-2">
                <input type="checkbox" id="isActive" checked={form.isActive}
                  onChange={(e) => setForm((f) => ({ ...f, isActive: e.target.checked }))}
                  className="rounded" />
                <label htmlFor="isActive" className="text-sm text-gray-700 cursor-pointer">Active</label>
              </div>
              {error && <p className="text-xs text-red-600">{error}</p>}
              <div className="flex gap-3 justify-end pt-2">
                <button type="button" onClick={() => setShowForm(false)}
                  className="px-4 py-2 text-sm text-gray-600 border border-gray-300 rounded-lg hover:bg-gray-50">
                  Cancel
                </button>
                <button type="submit" disabled={saving}
                  className="px-4 py-2 text-sm font-medium bg-blue-700 hover:bg-blue-800 text-white rounded-lg disabled:opacity-50 transition-colors">
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
