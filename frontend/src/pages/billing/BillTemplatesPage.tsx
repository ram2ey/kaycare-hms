import { useState, useEffect } from 'react';
import { getBillTemplates, createBillTemplate, updateBillTemplate, deleteBillTemplate, toggleBillTemplate } from '../../api/billTemplates';
import type { BillTemplateResponse, SaveBillTemplateRequest, BillTemplateItemRequest } from '../../types/billTemplates';
import { BILL_CATEGORIES } from '../../types/billing';
import { safeArray } from '../../utils/array';


const inp = 'w-full px-3 py-2 border border-gray-300 rounded-lg text-sm focus:outline-none focus:ring-2 focus:ring-blue-500';
const emptyItem = (): BillTemplateItemRequest => ({ description: '', quantity: 1, unitPrice: 0 });
const emptyForm = (): SaveBillTemplateRequest => ({ name: '', description: '', category: '', isActive: true, items: [emptyItem()] });

function fmt(n: number) { return `GHS ${n.toFixed(2)}`; }

export default function BillTemplatesPage() {
  const [templates, setTemplates]         = useState<BillTemplateResponse[]>([]);
  const [loading, setLoading]             = useState(true);
  const [includeInactive, setIncludeInactive] = useState(false);
  const [error, setError]                 = useState('');

  // Create / Edit modal
  const [editTarget, setEditTarget]       = useState<BillTemplateResponse | null>(null);
  const [showForm, setShowForm]           = useState(false);
  const [form, setForm]                   = useState<SaveBillTemplateRequest>(emptyForm());
  const [saving, setSaving]               = useState(false);

  async function load() {
    setLoading(true);
    setError('');
    try {
      setTemplates(await getBillTemplates({ includeInactive }));
    } catch {
      setError('Failed to load templates.');
    } finally {
      setLoading(false);
    }
  }

  useEffect(() => { load(); }, [includeInactive]);

  function openCreate() {
    setEditTarget(null);
    setForm(emptyForm());
    setShowForm(true);
  }

  function openEdit(t: BillTemplateResponse) {
    setEditTarget(t);
    setForm({
      name:        t.name,
      description: t.description ?? '',
      category:    t.category    ?? '',
      isActive:    t.isActive,
      items:       safeArray(t.items).map(i => ({ description: i.description, category: i.category ?? '', quantity: i.quantity, unitPrice: i.unitPrice })),
    });
    setShowForm(true);
  }

  function updateItem(i: number, field: keyof BillTemplateItemRequest, value: unknown) {
    setForm(f => ({ ...f, items: safeArray(f.items).map((it, idx) => idx === i ? { ...it, [field]: value } : it) }));
  }

  async function handleSubmit(e: React.FormEvent) {
    e.preventDefault();
    if (form.items.length === 0) { alert('Add at least one line item.'); return; }
    setSaving(true);
    try {
      const payload: SaveBillTemplateRequest = {
        ...form,
        description: form.description || undefined,
        category:    form.category    || undefined,
        items:       safeArray(form.items).map(i => ({ ...i, category: i.category || undefined })),
      };
      if (editTarget) {
        await updateBillTemplate(editTarget.billTemplateId, payload);
      } else {
        await createBillTemplate(payload);
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

  async function handleDelete(t: BillTemplateResponse) {
    if (!confirm(`Delete template "${t.name}"? This cannot be undone.`)) return;
    try {
      await deleteBillTemplate(t.billTemplateId);
      await load();
    } catch {
      alert('Delete failed.');
    }
  }

  async function handleToggle(t: BillTemplateResponse) {
    try {
      await toggleBillTemplate(t.billTemplateId);
      await load();
    } catch {
      alert('Failed to update template status.');
    }
  }

  // Group by category for display
  const grouped = templates.reduce<Record<string, BillTemplateResponse[]>>((acc, t) => {
    const key = t.category ?? 'Uncategorised';
    (acc[key] ??= []).push(t);
    return acc;
  }, {});

  return (
    <div className="p-6 max-w-5xl">
      <div className="flex items-center justify-between mb-6">
        <div>
          <h2 className="text-2xl font-semibold text-gray-800">Bill Templates</h2>
          <p className="text-sm text-gray-500 mt-1">Pre-defined service bundles for quick bill creation.</p>
        </div>
        <button onClick={openCreate}
          className="px-4 py-2 bg-blue-700 hover:bg-blue-800 text-white text-sm font-medium rounded-lg transition-colors">
          + New Template
        </button>
      </div>

      <div className="flex items-center gap-3 mb-5">
        <label className="flex items-center gap-2 text-sm text-gray-600 cursor-pointer">
          <input type="checkbox" checked={includeInactive} onChange={e => setIncludeInactive(e.target.checked)} className="rounded" />
          Show inactive
        </label>
        <span className="text-xs text-gray-400 ml-auto">{templates.length} template{templates.length !== 1 ? 's' : ''}</span>
      </div>

      {loading ? (
        <div className="text-gray-400 py-8">Loading…</div>
      ) : error ? (
        <div className="text-red-600 py-4">{error}</div>
      ) : templates.length === 0 ? (
        <div className="bg-white rounded-xl border border-gray-200 px-6 py-12 text-center text-gray-400">
          No templates yet. Create one to speed up bill entry.
        </div>
      ) : (
        <div className="space-y-6">
          {Object.entries(grouped).sort(([a], [b]) => a.localeCompare(b)).map(([category, group]) => (
            <div key={category}>
              <h3 className="text-xs font-semibold text-gray-400 uppercase tracking-wider mb-2">{category}</h3>
              <div className="bg-white rounded-xl border border-gray-200 overflow-hidden">
                <table className="w-full text-sm">
                  <thead className="border-b border-gray-100 bg-gray-50">
                    <tr>
                      <th className="text-left px-5 py-3 font-medium text-gray-500 text-xs uppercase">Name</th>
                      <th className="text-left px-5 py-3 font-medium text-gray-500 text-xs uppercase">Items</th>
                      <th className="text-right px-5 py-3 font-medium text-gray-500 text-xs uppercase">Total Value</th>
                      <th className="text-left px-5 py-3 font-medium text-gray-500 text-xs uppercase">Status</th>
                      <th className="px-5 py-3"></th>
                    </tr>
                  </thead>
                  <tbody className="divide-y divide-gray-50">
                    {group.map(t => (
                      <tr key={t.billTemplateId} className={t.isActive ? '' : 'opacity-50'}>
                        <td className="px-5 py-3">
                          <p className="font-medium text-gray-800">{t.name}</p>
                          {t.description && <p className="text-xs text-gray-400 mt-0.5">{t.description}</p>}
                        </td>
                        <td className="px-5 py-3 text-gray-500">{t.items.length} item{t.items.length !== 1 ? 's' : ''}</td>
                        <td className="px-5 py-3 text-right font-mono text-gray-700">{fmt(t.totalValue)}</td>
                        <td className="px-5 py-3">
                          <span className={`text-xs font-medium px-2 py-0.5 rounded-full ${t.isActive ? 'bg-green-100 text-green-700' : 'bg-gray-100 text-gray-500'}`}>
                            {t.isActive ? 'Active' : 'Inactive'}
                          </span>
                        </td>
                        <td className="px-5 py-3 text-right space-x-3 whitespace-nowrap">
                          <button onClick={() => openEdit(t)} className="text-xs text-blue-600 hover:underline">Edit</button>
                          <button onClick={() => handleToggle(t)} className="text-xs text-gray-500 hover:underline">
                            {t.isActive ? 'Deactivate' : 'Activate'}
                          </button>
                          <button onClick={() => handleDelete(t)} className="text-xs text-red-500 hover:underline">Delete</button>
                        </td>
                      </tr>
                    ))}
                  </tbody>
                </table>
              </div>
            </div>
          ))}
        </div>
      )}

      {/* Create / Edit modal */}
      {showForm && (
        <div className="fixed inset-0 bg-black/40 flex items-start justify-center z-50 overflow-y-auto py-10">
          <div className="bg-white rounded-2xl shadow-xl p-6 w-full max-w-2xl mx-4">
            <h3 className="text-lg font-semibold text-gray-800 mb-4">
              {editTarget ? 'Edit Template' : 'New Template'}
            </h3>
            <form onSubmit={handleSubmit} className="space-y-4">
              <div className="grid grid-cols-2 gap-4">
                <div className="col-span-2">
                  <label className="block text-xs text-gray-600 mb-1">Template Name *</label>
                  <input required value={form.name}
                    onChange={e => setForm(f => ({ ...f, name: e.target.value }))}
                    placeholder="e.g. Antenatal Visit Package"
                    className={inp} />
                </div>
                <div>
                  <label className="block text-xs text-gray-600 mb-1">Category</label>
                  <input value={form.category ?? ''}
                    onChange={e => setForm(f => ({ ...f, category: e.target.value }))}
                    list="tmpl-category-list"
                    placeholder="e.g. Antenatal, Emergency…"
                    className={inp} />
                  <datalist id="tmpl-category-list">
                    {BILL_CATEGORIES.map(c => <option key={c} value={c} />)}
                  </datalist>
                </div>
                <div>
                  <label className="block text-xs text-gray-600 mb-1">Status</label>
                  <select value={form.isActive ? 'true' : 'false'}
                    onChange={e => setForm(f => ({ ...f, isActive: e.target.value === 'true' }))}
                    className={inp}>
                    <option value="true">Active</option>
                    <option value="false">Inactive</option>
                  </select>
                </div>
                <div className="col-span-2">
                  <label className="block text-xs text-gray-600 mb-1">Description</label>
                  <input value={form.description ?? ''}
                    onChange={e => setForm(f => ({ ...f, description: e.target.value }))}
                    placeholder="Optional description…"
                    className={inp} />
                </div>
              </div>

              {/* Line items */}
              <div>
                <div className="flex items-center justify-between mb-2">
                  <label className="text-xs font-semibold text-gray-600 uppercase tracking-wide">Line Items *</label>
                  <button type="button" onClick={() => setForm(f => ({ ...f, items: [...f.items, emptyItem()] }))}
                    className="text-xs text-blue-600 hover:underline">+ Add item</button>
                </div>
                <div className="space-y-2">
                  {safeArray(form.items).map((item, i) => (
                    <div key={i} className="grid grid-cols-12 gap-2 items-start">
                      <div className="col-span-4">
                        {i === 0 && <div className="text-xs text-gray-400 mb-1">Description *</div>}
                        <input required value={item.description}
                          onChange={e => updateItem(i, 'description', e.target.value)}
                          placeholder="Service or item name"
                          className={inp} />
                      </div>
                      <div className="col-span-3">
                        {i === 0 && <div className="text-xs text-gray-400 mb-1">Category</div>}
                        <select value={item.category ?? ''}
                          onChange={e => updateItem(i, 'category', e.target.value)}
                          className={inp}>
                          <option value="">—</option>
                          {BILL_CATEGORIES.map(c => <option key={c}>{c}</option>)}
                        </select>
                      </div>
                      <div className="col-span-2">
                        {i === 0 && <div className="text-xs text-gray-400 mb-1">Qty</div>}
                        <input required type="number" min={1} value={item.quantity}
                          onChange={e => updateItem(i, 'quantity', Number(e.target.value))}
                          className={inp} />
                      </div>
                      <div className="col-span-2">
                        {i === 0 && <div className="text-xs text-gray-400 mb-1">Unit Price</div>}
                        <input required type="number" step="0.01" min={0} value={item.unitPrice || ''}
                          onChange={e => updateItem(i, 'unitPrice', Number(e.target.value))}
                          placeholder="0.00" className={inp} />
                      </div>
                      <div className="col-span-1 flex items-end pb-0.5">
                        {i === 0 && <div className="h-5" />}
                        {form.items.length > 1 && (
                          <button type="button"
                            onClick={() => setForm(f => ({ ...f, items: f.items.filter((_, idx) => idx !== i) }))}
                            className="w-full text-red-400 hover:text-red-600 text-lg leading-none mt-1">×</button>
                        )}
                      </div>
                    </div>
                  ))}
                </div>
                <div className="mt-3 text-right text-sm font-medium text-gray-700">
                  Total: {fmt(form.items.reduce((s, i) => s + i.quantity * i.unitPrice, 0))}
                </div>
              </div>

              <div className="flex gap-3 justify-end pt-2">
                <button type="button" onClick={() => setShowForm(false)}
                  className="px-4 py-2 text-sm text-gray-600 border border-gray-300 rounded-lg hover:bg-gray-50">Cancel</button>
                <button type="submit" disabled={saving}
                  className="px-4 py-2 text-sm font-medium bg-blue-700 hover:bg-blue-800 text-white rounded-lg disabled:opacity-50 transition-colors">
                  {saving ? 'Saving…' : editTarget ? 'Save Changes' : 'Create Template'}
                </button>
              </div>
            </form>
          </div>
        </div>
      )}
    </div>
  );
}
