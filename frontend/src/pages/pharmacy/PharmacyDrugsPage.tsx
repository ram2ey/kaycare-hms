import { useState, useEffect, useCallback } from 'react';
import { getDrugs, createDrug, updateDrug, deactivateDrug, getDrugMovements, recordMovement } from '../../api/pharmacy';
import type { DrugInventoryResponse, SaveDrugRequest, StockMovementResponse } from '../../types/pharmacy';
import {
  DRUG_UNITS, DRUG_DOSAGE_FORMS, DRUG_CATEGORIES,
  MANUAL_MOVEMENT_TYPES, MOVEMENT_TYPE_LABELS, MOVEMENT_TYPE_COLORS,
} from '../../types/pharmacy';

const inp = 'w-full px-3 py-2 border border-gray-300 rounded-lg text-sm focus:outline-none focus:ring-2 focus:ring-blue-500';
const fmt = (n: number) => `GHS ${n.toFixed(2)}`;

const emptyForm = (): SaveDrugRequest => ({
  name: '', genericName: '', dosageForm: 'Tablet', strength: '',
  unit: 'Tablet', category: '', reorderThreshold: 10,
  unitCost: 0, sellingPrice: 0, isControlledSubstance: false, isActive: true,
});

export default function PharmacyDrugsPage() {
  const [drugs, setDrugs]                   = useState<DrugInventoryResponse[]>([]);
  const [loading, setLoading]               = useState(true);
  const [error, setError]                   = useState('');
  const [showInactive, setShowInactive]     = useState(false);
  const [filterLowStock, setFilterLowStock] = useState(false);
  const [filterCategory, setFilterCategory] = useState('');

  // Create / Edit modal
  const [editTarget, setEditTarget] = useState<DrugInventoryResponse | null>(null);
  const [showForm, setShowForm]     = useState(false);
  const [form, setForm]             = useState<SaveDrugRequest>(emptyForm());
  const [saving, setSaving]         = useState(false);

  // Stock movement modal
  const [moveDrug, setMoveDrug]           = useState<DrugInventoryResponse | null>(null);
  const [moveType, setMoveType]           = useState('Receive');
  const [moveQty, setMoveQty]             = useState(1);
  const [moveNotes, setMoveNotes]         = useState('');
  const [moveSaving, setMoveSaving]       = useState(false);

  // Movement history panel
  const [historyDrug, setHistoryDrug]       = useState<DrugInventoryResponse | null>(null);
  const [history, setHistory]               = useState<StockMovementResponse[]>([]);
  const [historyLoading, setHistoryLoading] = useState(false);

  const load = useCallback(async () => {
    setLoading(true);
    setError('');
    try {
      setDrugs(await getDrugs({
        activeOnly:   showInactive ? undefined : true,
        lowStockOnly: filterLowStock || undefined,
        category:     filterCategory || undefined,
      }));
    } catch {
      setError('Failed to load inventory.');
    } finally {
      setLoading(false);
    }
  }, [showInactive, filterLowStock, filterCategory]);

  useEffect(() => { load(); }, [load]);

  function openCreate() {
    setEditTarget(null);
    setForm(emptyForm());
    setShowForm(true);
  }

  function openEdit(d: DrugInventoryResponse) {
    setEditTarget(d);
    setForm({
      name: d.name, genericName: d.genericName ?? '', dosageForm: d.dosageForm ?? 'Tablet',
      strength: d.strength ?? '', unit: d.unit, category: d.category ?? '',
      reorderThreshold: d.reorderThreshold, unitCost: d.unitCost,
      sellingPrice: d.sellingPrice, isControlledSubstance: d.isControlledSubstance,
      isActive: d.isActive,
    });
    setShowForm(true);
  }

  async function handleSubmit(e: React.FormEvent) {
    e.preventDefault();
    setSaving(true);
    try {
      const payload: SaveDrugRequest = {
        ...form,
        genericName: form.genericName || undefined,
        dosageForm:  form.dosageForm  || undefined,
        strength:    form.strength    || undefined,
        category:    form.category    || undefined,
      };
      if (editTarget) {
        await updateDrug(editTarget.drugInventoryId, payload);
      } else {
        await createDrug(payload);
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

  async function handleDeactivate(d: DrugInventoryResponse) {
    if (!confirm(`Deactivate "${d.name}"? It will no longer appear in active inventory.`)) return;
    try {
      await deactivateDrug(d.drugInventoryId);
      await load();
    } catch {
      alert('Failed to deactivate drug.');
    }
  }

  function openMovement(d: DrugInventoryResponse) {
    setMoveDrug(d);
    setMoveType('Receive');
    setMoveQty(1);
    setMoveNotes('');
  }

  async function handleMovementSubmit(e: React.FormEvent) {
    e.preventDefault();
    if (!moveDrug) return;
    setMoveSaving(true);
    try {
      await recordMovement(moveDrug.drugInventoryId, {
        movementType: moveType,
        quantity:     moveQty,
        notes:        moveNotes || undefined,
      });
      setMoveDrug(null);
      await load();
    } catch (err) {
      const msg = (err as { response?: { data?: { error?: string } } })?.response?.data?.error ?? 'Movement failed.';
      alert(msg);
    } finally {
      setMoveSaving(false);
    }
  }

  async function openHistory(d: DrugInventoryResponse) {
    setHistoryDrug(d);
    setHistoryLoading(true);
    try {
      setHistory(await getDrugMovements(d.drugInventoryId));
    } catch {
      setHistory([]);
    } finally {
      setHistoryLoading(false);
    }
  }

  const grouped = drugs.reduce<Record<string, DrugInventoryResponse[]>>((acc, d) => {
    const key = d.category ?? 'Uncategorised';
    (acc[key] ??= []).push(d);
    return acc;
  }, {});

  const alertCount = drugs.filter(d => d.isLowStock && d.isActive).length;

  return (
    <div className="p-6 max-w-7xl">
      {/* Header */}
      <div className="flex items-center justify-between mb-6">
        <div>
          <h2 className="text-2xl font-semibold text-gray-800">Drug Inventory</h2>
          <p className="text-sm text-gray-500 mt-1">
            {drugs.length} drug{drugs.length !== 1 ? 's' : ''}
            {alertCount > 0 && (
              <span className="ml-2 px-2 py-0.5 bg-red-100 text-red-600 text-xs rounded-full font-medium">
                {alertCount} low stock
              </span>
            )}
          </p>
        </div>
        <button onClick={openCreate}
          className="px-4 py-2 bg-blue-700 hover:bg-blue-800 text-white text-sm font-medium rounded-lg transition-colors">
          + Add Drug
        </button>
      </div>

      {/* Filters */}
      <div className="flex flex-wrap items-center gap-4 mb-5">
        <label className="flex items-center gap-2 text-sm text-gray-600 cursor-pointer">
          <input type="checkbox" checked={showInactive} onChange={e => setShowInactive(e.target.checked)} className="rounded" />
          Show inactive
        </label>
        <label className="flex items-center gap-2 text-sm text-gray-600 cursor-pointer">
          <input type="checkbox" checked={filterLowStock} onChange={e => setFilterLowStock(e.target.checked)} className="rounded" />
          Low stock only
        </label>
        <select value={filterCategory} onChange={e => setFilterCategory(e.target.value)}
          className="px-3 py-1.5 border border-gray-300 rounded-lg text-sm text-gray-600 focus:outline-none focus:ring-2 focus:ring-blue-500">
          <option value="">All categories</option>
          {DRUG_CATEGORIES.map(c => <option key={c}>{c}</option>)}
        </select>
      </div>

      {loading ? (
        <div className="text-gray-400 py-8">Loading…</div>
      ) : error ? (
        <div className="text-red-600 py-4">{error}</div>
      ) : drugs.length === 0 ? (
        <div className="bg-white rounded-xl border border-gray-200 px-6 py-12 text-center text-gray-400">
          No drugs found. Add your first drug to start tracking inventory.
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
                      <th className="text-left px-4 py-3 font-medium text-gray-500 text-xs uppercase">Drug</th>
                      <th className="text-left px-4 py-3 font-medium text-gray-500 text-xs uppercase">Form / Strength</th>
                      <th className="text-center px-4 py-3 font-medium text-gray-500 text-xs uppercase">Stock</th>
                      <th className="text-right px-4 py-3 font-medium text-gray-500 text-xs uppercase">Selling Price</th>
                      <th className="text-left px-4 py-3 font-medium text-gray-500 text-xs uppercase">Status</th>
                      <th className="px-4 py-3"></th>
                    </tr>
                  </thead>
                  <tbody className="divide-y divide-gray-50">
                    {group.map(d => (
                      <tr key={d.drugInventoryId} className={d.isActive ? '' : 'opacity-50'}>
                        <td className="px-4 py-3">
                          <div className="flex items-center gap-2">
                            <p className="font-medium text-gray-800">{d.name}</p>
                            {d.isControlledSubstance && (
                              <span className="text-xs px-1.5 py-0.5 bg-red-100 text-red-600 rounded font-medium">CS</span>
                            )}
                          </div>
                          {d.genericName && <p className="text-xs text-gray-400 mt-0.5">{d.genericName}</p>}
                        </td>
                        <td className="px-4 py-3 text-gray-500 text-xs">
                          {[d.dosageForm, d.strength].filter(Boolean).join(' · ') || '—'}
                        </td>
                        <td className="px-4 py-3 text-center">
                          <span className={`font-mono font-semibold text-sm ${d.isLowStock ? 'text-red-600' : 'text-gray-800'}`}>
                            {d.currentStock}
                          </span>
                          <span className="text-xs text-gray-400 ml-1">{d.unit}</span>
                          {d.isLowStock && (
                            <div className="text-xs text-red-500 mt-0.5">⚠ Low (threshold: {d.reorderThreshold})</div>
                          )}
                        </td>
                        <td className="px-4 py-3 text-right font-mono text-gray-700">{fmt(d.sellingPrice)}</td>
                        <td className="px-4 py-3">
                          <span className={`text-xs font-medium px-2 py-0.5 rounded-full ${d.isActive ? 'bg-green-100 text-green-700' : 'bg-gray-100 text-gray-500'}`}>
                            {d.isActive ? 'Active' : 'Inactive'}
                          </span>
                        </td>
                        <td className="px-4 py-3 text-right space-x-3 whitespace-nowrap">
                          <button onClick={() => openMovement(d)} className="text-xs text-green-600 hover:underline">Adjust Stock</button>
                          <button onClick={() => openHistory(d)} className="text-xs text-blue-600 hover:underline">History</button>
                          <button onClick={() => openEdit(d)} className="text-xs text-gray-500 hover:underline">Edit</button>
                          {d.isActive && (
                            <button onClick={() => handleDeactivate(d)} className="text-xs text-red-500 hover:underline">Deactivate</button>
                          )}
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
            <h3 className="text-lg font-semibold text-gray-800 mb-5">
              {editTarget ? 'Edit Drug' : 'Add Drug to Inventory'}
            </h3>
            <form onSubmit={handleSubmit} className="space-y-4">
              <div className="grid grid-cols-2 gap-4">
                <div className="col-span-2">
                  <label className="block text-xs text-gray-600 mb-1">Drug Name *</label>
                  <input required value={form.name}
                    onChange={e => setForm(f => ({ ...f, name: e.target.value }))}
                    placeholder="e.g. Amoxicillin" className={inp} />
                </div>
                <div>
                  <label className="block text-xs text-gray-600 mb-1">Generic Name</label>
                  <input value={form.genericName ?? ''}
                    onChange={e => setForm(f => ({ ...f, genericName: e.target.value }))}
                    placeholder="e.g. Amoxicillin trihydrate" className={inp} />
                </div>
                <div>
                  <label className="block text-xs text-gray-600 mb-1">Category</label>
                  <select value={form.category ?? ''}
                    onChange={e => setForm(f => ({ ...f, category: e.target.value }))}
                    className={inp}>
                    <option value="">— Select —</option>
                    {DRUG_CATEGORIES.map(c => <option key={c}>{c}</option>)}
                  </select>
                </div>
                <div>
                  <label className="block text-xs text-gray-600 mb-1">Dosage Form</label>
                  <select value={form.dosageForm ?? ''}
                    onChange={e => setForm(f => ({ ...f, dosageForm: e.target.value }))}
                    className={inp}>
                    <option value="">— Select —</option>
                    {DRUG_DOSAGE_FORMS.map(f => <option key={f}>{f}</option>)}
                  </select>
                </div>
                <div>
                  <label className="block text-xs text-gray-600 mb-1">Strength</label>
                  <input value={form.strength ?? ''}
                    onChange={e => setForm(f => ({ ...f, strength: e.target.value }))}
                    placeholder="e.g. 500mg" className={inp} />
                </div>
                <div>
                  <label className="block text-xs text-gray-600 mb-1">Stock Unit *</label>
                  <select value={form.unit}
                    onChange={e => setForm(f => ({ ...f, unit: e.target.value }))}
                    className={inp}>
                    {DRUG_UNITS.map(u => <option key={u}>{u}</option>)}
                  </select>
                </div>
                <div>
                  <label className="block text-xs text-gray-600 mb-1">Reorder Threshold *</label>
                  <input required type="number" min={0} value={form.reorderThreshold}
                    onChange={e => setForm(f => ({ ...f, reorderThreshold: Number(e.target.value) }))}
                    className={inp} />
                </div>
                <div>
                  <label className="block text-xs text-gray-600 mb-1">Unit Cost (GHS)</label>
                  <input type="number" step="0.01" min={0} value={form.unitCost || ''}
                    onChange={e => setForm(f => ({ ...f, unitCost: Number(e.target.value) }))}
                    placeholder="0.00" className={inp} />
                </div>
                <div>
                  <label className="block text-xs text-gray-600 mb-1">Selling Price (GHS)</label>
                  <input type="number" step="0.01" min={0} value={form.sellingPrice || ''}
                    onChange={e => setForm(f => ({ ...f, sellingPrice: Number(e.target.value) }))}
                    placeholder="0.00" className={inp} />
                </div>
                <div className="col-span-2 flex items-center gap-6">
                  <label className="flex items-center gap-2 text-sm text-gray-700 cursor-pointer">
                    <input type="checkbox" checked={form.isControlledSubstance}
                      onChange={e => setForm(f => ({ ...f, isControlledSubstance: e.target.checked }))}
                      className="rounded" />
                    Controlled Substance
                  </label>
                  <label className="flex items-center gap-2 text-sm text-gray-700 cursor-pointer">
                    <input type="checkbox" checked={form.isActive}
                      onChange={e => setForm(f => ({ ...f, isActive: e.target.checked }))}
                      className="rounded" />
                    Active
                  </label>
                </div>
              </div>
              <div className="flex gap-3 justify-end pt-2">
                <button type="button" onClick={() => setShowForm(false)}
                  className="px-4 py-2 text-sm text-gray-600 border border-gray-300 rounded-lg hover:bg-gray-50">Cancel</button>
                <button type="submit" disabled={saving}
                  className="px-4 py-2 text-sm font-medium bg-blue-700 hover:bg-blue-800 text-white rounded-lg disabled:opacity-50 transition-colors">
                  {saving ? 'Saving…' : editTarget ? 'Save Changes' : 'Add Drug'}
                </button>
              </div>
            </form>
          </div>
        </div>
      )}

      {/* Stock movement modal */}
      {moveDrug && (
        <div className="fixed inset-0 bg-black/40 flex items-center justify-center z-50">
          <div className="bg-white rounded-2xl shadow-xl p-6 w-full max-w-md mx-4">
            <h3 className="text-lg font-semibold text-gray-800 mb-1">Adjust Stock</h3>
            <p className="text-sm text-gray-500 mb-4">
              {moveDrug.name}
              {moveDrug.strength ? ` ${moveDrug.strength}` : ''}
              {moveDrug.dosageForm ? ` · ${moveDrug.dosageForm}` : ''}
              {' — '}
              <span className="font-medium text-gray-700">Current stock: {moveDrug.currentStock} {moveDrug.unit}</span>
            </p>
            <form onSubmit={handleMovementSubmit} className="space-y-4">
              <div>
                <label className="block text-xs text-gray-600 mb-1">Movement Type *</label>
                <select value={moveType} onChange={e => setMoveType(e.target.value)} className={inp}>
                  {MANUAL_MOVEMENT_TYPES.map(t => (
                    <option key={t} value={t}>{MOVEMENT_TYPE_LABELS[t]}</option>
                  ))}
                </select>
              </div>
              <div>
                <label className="block text-xs text-gray-600 mb-1">Quantity *</label>
                <input required type="number" min={1} value={moveQty}
                  onChange={e => setMoveQty(Number(e.target.value))} className={inp} />
              </div>
              <div>
                <label className="block text-xs text-gray-600 mb-1">Notes</label>
                <input value={moveNotes} onChange={e => setMoveNotes(e.target.value)}
                  placeholder="Optional notes…" className={inp} />
              </div>
              <div className="flex gap-3 justify-end pt-2">
                <button type="button" onClick={() => setMoveDrug(null)}
                  className="px-4 py-2 text-sm text-gray-600 border border-gray-300 rounded-lg hover:bg-gray-50">Cancel</button>
                <button type="submit" disabled={moveSaving}
                  className="px-4 py-2 text-sm font-medium bg-green-700 hover:bg-green-800 text-white rounded-lg disabled:opacity-50 transition-colors">
                  {moveSaving ? 'Recording…' : 'Record Movement'}
                </button>
              </div>
            </form>
          </div>
        </div>
      )}

      {/* Movement history panel */}
      {historyDrug && (
        <div className="fixed inset-0 bg-black/40 flex items-start justify-end z-50">
          <div className="bg-white h-full w-full max-w-lg shadow-2xl flex flex-col">
            <div className="px-6 py-4 border-b border-gray-200 flex items-center justify-between">
              <div>
                <h3 className="font-semibold text-gray-800">{historyDrug.name} — Movement History</h3>
                <p className="text-xs text-gray-400 mt-0.5">Current stock: {historyDrug.currentStock} {historyDrug.unit}</p>
              </div>
              <button onClick={() => setHistoryDrug(null)} className="text-gray-400 hover:text-gray-600 text-2xl leading-none">×</button>
            </div>
            <div className="flex-1 overflow-y-auto p-4">
              {historyLoading ? (
                <div className="text-gray-400 py-8 text-center">Loading…</div>
              ) : history.length === 0 ? (
                <div className="text-gray-400 py-8 text-center">No movements recorded yet.</div>
              ) : (
                <div className="space-y-3">
                  {history.map(m => (
                    <div key={m.stockMovementId} className="bg-gray-50 rounded-xl p-4">
                      <div className="flex items-center justify-between mb-1">
                        <span className={`text-xs font-medium px-2 py-0.5 rounded-full ${MOVEMENT_TYPE_COLORS[m.movementType] ?? 'bg-gray-100 text-gray-600'}`}>
                          {MOVEMENT_TYPE_LABELS[m.movementType] ?? m.movementType}
                        </span>
                        <span className="text-xs text-gray-400">{new Date(m.createdAt).toLocaleString()}</span>
                      </div>
                      <div className="flex items-center gap-3 mt-2 text-sm">
                        <span className="font-mono font-semibold text-gray-700">Qty: {m.quantity}</span>
                        <span className="text-gray-400">
                          {m.previousStock} → <span className="font-semibold text-gray-700">{m.newStock}</span>
                        </span>
                      </div>
                      {m.notes && <p className="text-xs text-gray-500 mt-1">{m.notes}</p>}
                      <p className="text-xs text-gray-400 mt-1">By {m.createdByName}</p>
                    </div>
                  ))}
                </div>
              )}
            </div>
          </div>
        </div>
      )}
    </div>
  );
}
