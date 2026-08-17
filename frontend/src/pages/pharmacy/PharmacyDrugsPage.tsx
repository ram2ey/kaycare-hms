import { useState, useEffect, useCallback } from 'react';
import { getDrugs, getDrugMovements, recordMovement } from '../../api/pharmacy';
import type { DrugInventoryResponse, StockMovementResponse } from '../../types/pharmacy';
import {
  DRUG_CATEGORIES,
  MANUAL_MOVEMENT_TYPES,
  MOVEMENT_TYPE_LABELS,
  MOVEMENT_TYPE_COLORS,
} from '../../types/pharmacy';

const inp = 'w-full px-3 py-2 border border-gray-300 rounded-lg text-sm focus:outline-none focus:ring-2 focus:ring-blue-500';

export default function PharmacyDrugsPage() {
  const [drugs, setDrugs]                   = useState<DrugInventoryResponse[]>([]);
  const [loading, setLoading]               = useState(true);
  const [error, setError]                   = useState('');
  const [filterLowStock, setFilterLowStock] = useState(false);
  const [filterCategory, setFilterCategory] = useState('');
  const [searchQuery, setSearchQuery]       = useState('');

  // Stock movement modal
  const [moveDrug, setMoveDrug]           = useState<DrugInventoryResponse | null>(null);
  const [moveType, setMoveType]           = useState('Receive');
  const [moveQty, setMoveQty]             = useState(1);
  const [moveNotes, setMoveNotes]         = useState('');
  const [moveSaving, setMoveSaving]       = useState(false);
  const [moveError, setMoveError]         = useState('');

  // Movement history panel
  const [historyDrug, setHistoryDrug]       = useState<DrugInventoryResponse | null>(null);
  const [history, setHistory]               = useState<StockMovementResponse[]>([]);
  const [historyLoading, setHistoryLoading] = useState(false);

  const load = useCallback(async () => {
    setLoading(true);
    setError('');
    try {
      // Pharmacy operational view shows only active drugs
      const data = await getDrugs({
        activeOnly:   true,
        lowStockOnly: filterLowStock || undefined,
        category:     filterCategory || undefined,
      });
      setDrugs(data);
    } catch {
      setError('Failed to load inventory.');
    } finally {
      setLoading(false);
    }
  }, [filterLowStock, filterCategory]);

  useEffect(() => { load(); }, [load]);

  function openMovement(d: DrugInventoryResponse) {
    setMoveDrug(d);
    setMoveType('Receive');
    setMoveQty(1);
    setMoveNotes('');
    setMoveError('');
  }

  async function handleMovementSubmit(e: React.FormEvent) {
    e.preventDefault();
    if (!moveDrug) return;
    setMoveSaving(true);
    setMoveError('');
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
      setMoveError(msg);
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

  const filtered = drugs.filter(d => {
    const matchesSearch =
      d.name.toLowerCase().includes(searchQuery.toLowerCase()) ||
      (d.genericName && d.genericName.toLowerCase().includes(searchQuery.toLowerCase()));
    return matchesSearch;
  });

  const grouped = filtered.reduce<Record<string, DrugInventoryResponse[]>>((acc, d) => {
    const key = d.category ?? 'Uncategorised';
    (acc[key] ??= []).push(d);
    return acc;
  }, {});

  const alertCount = drugs.filter(d => d.isLowStock && d.isActive).length;

  return (
    <div className="p-6 max-w-7xl mx-auto">
      {/* Header */}
      <div className="flex flex-col md:flex-row md:items-center justify-between gap-4 mb-6">
        <div>
          <h2 className="text-2xl font-bold text-gray-800">Pharmacy Stock Inventory</h2>
          <p className="text-sm text-gray-500 mt-1">
            {drugs.length} active drug formulary items tracked.
            {alertCount > 0 && (
              <span className="ml-2 px-2.5 py-0.5 bg-red-100 text-red-700 text-xs rounded-full font-bold">
                {alertCount} low stock alerts
              </span>
            )}
          </p>
        </div>
      </div>

      {/* Filters */}
      <div className="bg-white p-4 rounded-xl border border-gray-200 shadow-sm flex flex-wrap gap-4 items-center mb-6">
        <div className="flex-1 min-w-[240px]">
          <input
            type="text"
            placeholder="Search active stock by drug or generic name..."
            value={searchQuery}
            onChange={e => setSearchQuery(e.target.value)}
            className="w-full px-3 py-2 border border-gray-300 rounded-lg text-sm focus:outline-none focus:ring-2 focus:ring-blue-500"
          />
        </div>
        <div className="w-48">
          <select value={filterCategory} onChange={e => setFilterCategory(e.target.value)}
            className="w-full px-3 py-2 border border-gray-300 rounded-lg text-sm text-gray-600 focus:outline-none focus:ring-2 focus:ring-blue-500 bg-white">
            <option value="">All categories</option>
            {DRUG_CATEGORIES.map(c => <option key={c}>{c}</option>)}
          </select>
        </div>
        <label className="flex items-center gap-2 text-sm text-gray-600 cursor-pointer select-none">
          <input type="checkbox" checked={filterLowStock} onChange={e => setFilterLowStock(e.target.checked)} className="rounded text-blue-600 focus:ring-blue-500" />
          Low stock only
        </label>
      </div>

      {loading ? (
        <div className="text-gray-400 py-12 text-center bg-white rounded-xl border border-gray-200">Loading stock status...</div>
      ) : error ? (
        <div className="text-red-650 py-12 text-center bg-white rounded-xl border border-gray-200">{error}</div>
      ) : filtered.length === 0 ? (
        <div className="bg-white rounded-xl border border-gray-200 px-6 py-12 text-center text-gray-500">
          No stock items match your filter criteria.
        </div>
      ) : (
        <div className="space-y-6">
          {Object.entries(grouped).sort(([a], [b]) => a.localeCompare(b)).map(([category, group]) => (
            <div key={category} className="space-y-2">
              <h3 className="text-xs font-bold text-gray-400 uppercase tracking-wider pl-1">{category}</h3>
              <div className="bg-white rounded-xl border border-gray-200 overflow-hidden shadow-sm">
                <table className="w-full text-sm">
                  <thead className="border-b border-gray-100 bg-gray-50 text-gray-500 text-xs font-semibold uppercase tracking-wider">
                    <tr>
                      <th className="text-left px-5 py-3">Drug</th>
                      <th className="text-left px-5 py-3">Dosage / Strength</th>
                      <th className="text-right px-5 py-3">Current Stock</th>
                      <th className="text-right px-5 py-3">Reorder Threshold</th>
                      <th className="text-center px-5 py-3">Status</th>
                      <th className="px-5 py-3"></th>
                    </tr>
                  </thead>
                  <tbody className="divide-y divide-gray-100 text-gray-700">
                    {group.map(d => {
                      // Status: OK (stock > threshold), Low (stock <= threshold && stock > 0), Critical (stock === 0)
                      let statusText = 'OK';
                      let statusStyle = 'bg-green-150 text-green-700';
                      if (d.currentStock === 0) {
                        statusText = 'Critical';
                        statusStyle = 'bg-red-100 text-red-700';
                      } else if (d.currentStock <= d.reorderThreshold) {
                        statusText = 'Low';
                        statusStyle = 'bg-amber-100 text-amber-700';
                      }

                      return (
                        <tr key={d.drugInventoryId} className="hover:bg-gray-50/50 transition-colors">
                          <td className="px-5 py-3.5">
                            <div className="flex items-center gap-2">
                              <span className="font-bold text-gray-950">{d.name}</span>
                              {d.isControlledSubstance && (
                                <span className="inline-flex items-center px-1.5 py-0.2 rounded text-[10px] font-semibold bg-red-50 text-red-650 border border-red-200 uppercase">CS</span>
                              )}
                            </div>
                            {d.genericName && <p className="text-xs text-gray-400 mt-0.5">{d.genericName}</p>}
                          </td>
                          <td className="px-5 py-3.5 text-gray-550 text-xs font-medium">
                            {[d.dosageForm, d.strength].filter(Boolean).join(' · ') || '—'}
                          </td>
                          <td className="px-5 py-3.5 text-right font-mono font-bold text-gray-900">
                            {d.currentStock} <span className="text-xs font-sans text-gray-400 font-normal">{d.unit}</span>
                          </td>
                          <td className="px-5 py-3.5 text-right font-mono text-gray-500 font-semibold">{d.reorderThreshold}</td>
                          <td className="px-5 py-3.5 text-center">
                            <span className={`inline-flex items-center px-2.5 py-0.5 rounded-full text-xs font-bold ${statusStyle}`}>
                              {statusText}
                            </span>
                          </td>
                          <td className="px-5 py-3.5 text-right space-x-3 whitespace-nowrap">
                            <button
                              onClick={() => openMovement(d)}
                              className="text-xs text-green-650 hover:text-green-800 font-semibold hover:underline"
                            >
                              Adjust Stock
                            </button>
                            <button
                              onClick={() => openHistory(d)}
                              className="text-xs text-blue-650 hover:text-blue-800 font-semibold hover:underline"
                            >
                              View History
                            </button>
                          </td>
                        </tr>
                      );
                    })}
                  </tbody>
                </table>
              </div>
            </div>
          ))}
        </div>
      )}

      {/* Stock movement modal */}
      {moveDrug && (
        <div className="fixed inset-0 bg-black/45 flex items-center justify-center z-50 p-4">
          <div className="bg-white rounded-2xl shadow-xl p-6 w-full max-w-md">
            <h3 className="text-lg font-bold text-gray-900 mb-1">Adjust Stock Levels</h3>
            <p className="text-sm text-gray-500 mb-4">
              {moveDrug.name}
              {moveDrug.strength ? ` ${moveDrug.strength}` : ''}
              {moveDrug.dosageForm ? ` · ${moveDrug.dosageForm}` : ''}
              {' — '}
              <span className="font-bold text-gray-700">Current: {moveDrug.currentStock} {moveDrug.unit}</span>
            </p>
            <form onSubmit={handleMovementSubmit} className="space-y-4">
              <div>
                <label className="block text-xs font-semibold text-gray-600 mb-1">Adjustment Type *</label>
                <select value={moveType} onChange={e => setMoveType(e.target.value)} className={inp + ' bg-white'}>
                  {MANUAL_MOVEMENT_TYPES.map(t => (
                    <option key={t} value={t}>{MOVEMENT_TYPE_LABELS[t]}</option>
                  ))}
                </select>
              </div>
              <div>
                <label className="block text-xs font-semibold text-gray-600 mb-1">Quantity *</label>
                <input required type="number" min={1} value={moveQty}
                  onChange={e => setMoveQty(Number(e.target.value))} className={inp} />
              </div>
              <div>
                <label className="block text-xs font-semibold text-gray-600 mb-1">Reason / Notes</label>
                <input value={moveNotes} onChange={e => setMoveNotes(e.target.value)}
                  placeholder="e.g. Monthly audit correction..." className={inp} />
              </div>
              {moveError && <p className="text-sm text-red-600 bg-red-50 rounded-lg px-3 py-2">{moveError}</p>}
              <div className="flex gap-3 justify-end pt-4 border-t border-gray-150">
                <button type="button" onClick={() => setMoveDrug(null)}
                  className="px-4 py-2 text-sm font-semibold text-gray-600 border border-gray-300 rounded-lg hover:bg-gray-50 transition-colors">Cancel</button>
                <button type="submit" disabled={moveSaving}
                  className="px-5 py-2 text-sm font-semibold bg-green-700 hover:bg-green-800 text-white rounded-lg disabled:opacity-50 transition-colors flex items-center gap-2">
                  {moveSaving && (
                    <svg className="animate-spin h-4 w-4 text-white" fill="none" viewBox="0 0 24 24">
                      <circle className="opacity-25" cx="12" cy="12" r="10" stroke="currentColor" strokeWidth="4" />
                      <path className="opacity-75" fill="currentColor" d="M4 12a8 8 0 018-8V0C5.373 0 0 5.373 0 12h4z" />
                    </svg>
                  )}
                  Save Adjustment
                </button>
              </div>
            </form>
          </div>
        </div>
      )}

      {/* Movement history panel */}
      {historyDrug && (
        <div className="fixed inset-0 bg-black/45 flex items-start justify-end z-50">
          <div className="bg-white h-full w-full max-w-lg shadow-2xl flex flex-col">
            <div className="px-6 py-4 border-b border-gray-200 flex items-center justify-between">
              <div>
                <h3 className="font-bold text-gray-900">{historyDrug.name} — Movement Log</h3>
                <p className="text-xs text-gray-400 mt-0.5">Current Stock: {historyDrug.currentStock} {historyDrug.unit}</p>
              </div>
              <button onClick={() => setHistoryDrug(null)} className="text-gray-400 hover:text-gray-600 text-2xl leading-none">×</button>
            </div>
            <div className="flex-1 overflow-y-auto p-4 space-y-3">
              {historyLoading ? (
                <div className="text-gray-400 py-8 text-center text-sm">Loading logs...</div>
              ) : history.length === 0 ? (
                <div className="text-gray-400 py-8 text-center text-sm">No movements recorded yet.</div>
              ) : (
                <div className="space-y-3">
                  {history.map(m => (
                    <div key={m.stockMovementId} className="bg-gray-50 rounded-xl p-4 border border-gray-100">
                      <div className="flex items-center justify-between mb-1">
                        <span className={`text-xs font-bold px-2.5 py-0.5 rounded-full ${MOVEMENT_TYPE_COLORS[m.movementType] ?? 'bg-gray-150 text-gray-650'}`}>
                          {MOVEMENT_TYPE_LABELS[m.movementType] ?? m.movementType}
                        </span>
                        <span className="text-xs text-gray-400">{new Date(m.createdAt).toLocaleString()}</span>
                      </div>
                      <div className="flex items-center gap-3 mt-2 text-sm">
                        <span className="font-mono font-bold text-gray-800">Qty: {m.quantity}</span>
                        <span className="text-gray-400 font-medium text-xs">
                          Balance: {m.previousStock} → <span className="font-bold text-gray-800">{m.newStock}</span>
                        </span>
                      </div>
                      {m.notes && <p className="text-xs text-gray-500 mt-1 font-medium italic">"{m.notes}"</p>}
                      <p className="text-[10px] text-gray-400 mt-1.5 uppercase font-semibold">Recorded by {m.createdByName}</p>
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

