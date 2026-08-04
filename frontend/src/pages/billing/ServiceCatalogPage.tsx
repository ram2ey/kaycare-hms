import { useState, useEffect } from 'react';
import { Link } from 'react-router-dom';
import { useAuth } from '../../contexts/AuthContext';
import { getCatalog, createCatalogItem, updateCatalogItem, deleteCatalogItem } from '../../api/serviceCatalog';
import type { ServiceCatalogItem, SaveServiceCatalogItemRequest } from '../../types/serviceCatalog';
import { BILL_CATEGORIES } from '../../types/billing';
import { Roles } from '../../types';

const inp = 'w-full px-3 py-2 border border-gray-300 rounded-lg text-sm focus:outline-none focus:ring-2 focus:ring-blue-500';

const emptyForm = (): SaveServiceCatalogItemRequest => ({
  name: '', description: '', category: 'Consultation', unitPrice: 0, isActive: true,
});

function fmt(n: number) { return `GHS ${n.toFixed(2)}`; }

export default function ServiceCatalogPage() {
  const { user } = useAuth();
  const isPharmacyManager = user?.role === Roles.PharmacyManager;
  const canEditAll = [Roles.BillingManager, Roles.Admin, Roles.SuperAdmin].includes(user?.role as never);

  function canEditItem(cat: string) {
    if (canEditAll) return true;
    if (isPharmacyManager && cat === 'Pharmacy') return true;
    return false;
  }

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
    <div className="p-6 max-w-5xl mx-auto">
      <div className="text-sm text-gray-500 mb-4">
        <Link to="/billing" className="hover:text-blue-600">Billing</Link>
        <span className="mx-2">/</span>
        <span className="text-gray-800">Price Catalog</span>
      </div>

      <div className="flex flex-col md:flex-row md:items-center justify-between gap-4 mb-6">
        <div>
          <h2 className="text-2xl font-bold text-gray-800">Price Catalog</h2>
          <p className="text-sm text-gray-500 mt-1">{items.length} service/billing item{items.length !== 1 ? 's' : ''} configured</p>
        </div>
        <div className="flex items-center gap-4 self-start md:self-auto">
          <label className="flex items-center gap-2 text-sm text-gray-600 cursor-pointer select-none">
            <input type="checkbox" checked={showInactive} onChange={(e) => setShowInactive(e.target.checked)}
              className="rounded text-blue-600 focus:ring-blue-500" />
            Show inactive items
          </label>
          <button onClick={openCreate}
            className="px-4 py-2 bg-blue-700 hover:bg-blue-800 text-white text-sm font-semibold rounded-lg shadow-sm transition-colors">
            + New Item
          </button>
        </div>
      </div>

      {/* Category filter tabs */}
      {categories.length > 0 && (
        <div className="border-b border-gray-200 mb-6 flex gap-6 overflow-x-auto scrollbar-none select-none">
          <button
            onClick={() => setFilterCategory('')}
            className={`pb-3 text-sm font-semibold border-b-2 transition-colors whitespace-nowrap ${
              !filterCategory
                ? 'border-blue-700 text-blue-700'
                : 'border-transparent text-gray-500 hover:text-gray-700 hover:border-gray-300'
            }`}
          >
            All Categories
          </button>
          {categories.map((c) => (
            <button
              key={c}
              onClick={() => setFilterCategory(c)}
              className={`pb-3 text-sm font-semibold border-b-2 transition-colors whitespace-nowrap ${
                filterCategory === c
                  ? 'border-blue-700 text-blue-700'
                  : 'border-transparent text-gray-500 hover:text-gray-700 hover:border-gray-300'
              }`}
            >
              {c}
            </button>
          ))}
        </div>
      )}

      {loading ? (
        <p className="text-gray-400 text-sm py-12 text-center bg-white rounded-xl border border-gray-200">Loading catalog items...</p>
      ) : items.length === 0 ? (
        <div className="bg-white rounded-xl border border-gray-200 p-10 text-center">
          <p className="text-gray-500 text-sm mb-3">No items in the price catalog yet.</p>
          <button onClick={openCreate} className="text-sm text-blue-600 hover:underline font-semibold">Add the first item</button>
        </div>
      ) : (
        <div className="space-y-6">
          {grouped.map(({ cat, rows }) => (
            <section key={cat} className="bg-white rounded-xl border border-gray-200 overflow-hidden shadow-sm">
              <div className="px-5 py-3 bg-gray-50 border-b border-gray-100">
                <h3 className="text-sm font-bold text-gray-700 uppercase tracking-wider">{cat}</h3>
              </div>
              <table className="w-full text-sm">
                <thead className="border-b border-gray-100 text-gray-500 text-xs font-semibold uppercase tracking-wider bg-gray-50/30">
                  <tr>
                    <th className="text-left px-5 py-2.5">Name</th>
                    <th className="text-left px-5 py-2.5 hidden sm:table-cell">Description</th>
                    <th className="text-right px-5 py-2.5">Standard Price</th>
                    <th className="text-center px-5 py-2.5">Status</th>
                    <th className="px-5 py-2.5"></th>
                  </tr>
                </thead>
                <tbody className="divide-y divide-gray-100 text-gray-700">
                  {rows.map((item) => (
                    <tr key={item.serviceCatalogItemId} className={`hover:bg-gray-50/50 transition-colors ${!item.isActive ? 'opacity-50' : ''}`}>
                      <td className="px-5 py-3.5 font-bold text-gray-950">{item.name}</td>
                      <td className="px-5 py-3.5 text-gray-500 hidden sm:table-cell font-medium">{item.description ?? '—'}</td>
                      <td className="px-5 py-3.5 text-right font-mono font-bold text-gray-900">{fmt(item.unitPrice)}</td>
                      <td className="px-5 py-3.5 text-center">
                        <span className={`text-xs px-2.5 py-0.5 rounded-full font-bold ${item.isActive ? 'bg-green-100 text-green-700' : 'bg-gray-150 text-gray-550'}`}>
                          {item.isActive ? 'Active' : 'Inactive'}
                        </span>
                      </td>
                      <td className="px-5 py-3.5 text-right">
                        <div className="flex justify-end items-center gap-4">
                          <Link
                            to={`/admin/payer-tariffs?serviceId=${item.serviceCatalogItemId}`}
                            className="text-xs text-indigo-650 hover:text-indigo-800 font-semibold hover:underline"
                          >
                            Tariffs →
                          </Link>
                          {canEditItem(item.category) && (
                            <>
                              <button onClick={() => openEdit(item)} className="text-xs text-blue-650 hover:text-blue-800 font-semibold hover:underline">Edit</button>
                              <button onClick={() => handleDelete(item)} className="text-xs text-red-600 hover:text-red-800 font-semibold hover:underline">Delete</button>
                            </>
                          )}
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
        <div className="fixed inset-0 bg-black/45 flex items-center justify-center z-50 p-4">
          <div className="bg-white rounded-2xl shadow-xl p-6 w-full max-w-md">
            <h3 className="text-lg font-bold text-gray-900 mb-4">
              {editing ? 'Edit Catalog Item' : 'New Catalog Item'}
            </h3>
            <form onSubmit={handleSave} className="space-y-4">
              <div>
                <label className="block text-xs font-semibold text-gray-600 mb-1">Name *</label>
                <input required value={form.name} onChange={(e) => setForm((f) => ({ ...f, name: e.target.value }))}
                  placeholder="e.g. General Consultation" className={inp} />
              </div>
              <div>
                <label className="block text-xs font-semibold text-gray-600 mb-1">Category *</label>
                <select required value={form.category} onChange={(e) => setForm((f) => ({ ...f, category: e.target.value }))} className={inp + ' bg-white'}>
                  {BILL_CATEGORIES.map((c) => <option key={c}>{c}</option>)}
                </select>
              </div>
              <div>
                <label className="block text-xs font-semibold text-gray-600 mb-1">Unit Price (GHS) *</label>
                <input required type="number" step="0.01" min={0} value={form.unitPrice || ''}
                  onChange={(e) => setForm((f) => ({ ...f, unitPrice: Number(e.target.value) }))}
                  placeholder="0.00" className={inp} />
              </div>
              <div>
                <label className="block text-xs font-semibold text-gray-600 mb-1">Description / Notes</label>
                <input value={form.description ?? ''} onChange={(e) => setForm((f) => ({ ...f, description: e.target.value }))}
                  placeholder="Optional details" className={inp} />
              </div>
              <div className="flex items-center gap-2">
                <input type="checkbox" id="isActive" checked={form.isActive}
                  onChange={(e) => setForm((f) => ({ ...f, isActive: e.target.checked }))}
                  className="rounded text-blue-600 focus:ring-blue-500 h-4 w-4 border-gray-300" />
                <label htmlFor="isActive" className="text-sm text-gray-700 cursor-pointer select-none">Active and available for billing</label>
              </div>
              {error && <p className="text-xs text-red-600">{error}</p>}
              <div className="flex gap-3 justify-end pt-4 border-t border-gray-150">
                <button type="button" onClick={() => setShowForm(false)}
                  className="px-4 py-2 text-sm font-semibold text-gray-600 border border-gray-300 rounded-lg hover:bg-gray-50 transition-colors">
                  Cancel
                </button>
                <button type="submit" disabled={saving}
                  className="px-5 py-2 text-sm font-semibold bg-blue-700 hover:bg-blue-800 text-white rounded-lg disabled:opacity-50 transition-colors flex items-center gap-2">
                  {saving && (
                    <svg className="animate-spin h-4 w-4 text-white" fill="none" viewBox="0 0 24 24">
                      <circle className="opacity-25" cx="12" cy="12" r="10" stroke="currentColor" strokeWidth="4" />
                      <path className="opacity-75" fill="currentColor" d="M4 12a8 8 0 018-8V0C5.373 0 0 5.373 0 12h4z" />
                    </svg>
                  )}
                  Save Item
                </button>
              </div>
            </form>
          </div>
        </div>
      )}
    </div>
  );
}

