import { useState, useEffect, useCallback } from 'react';
import { Link } from 'react-router-dom';
import { getPurchaseOrders, getSuppliers, createPurchaseOrder, getDrugs } from '../../api/pharmacy';
import type {
  PurchaseOrderSummaryResponse,
  SupplierResponse,
  SavePurchaseOrderRequest,
  SavePurchaseOrderItemRequest,
} from '../../types/pharmacy';
import type { DrugInventoryResponse } from '../../types/pharmacy';
import { PO_STATUS_LABELS, PO_STATUS_COLORS } from '../../types/pharmacy';

const inp = 'w-full px-3 py-2 border border-gray-300 rounded-lg text-sm focus:outline-none focus:ring-2 focus:ring-blue-500';
const fmt = (n: number) => `$${n.toFixed(2)}`;

const emptyItem = (): SavePurchaseOrderItemRequest & { _key: number } => ({
  _key: Date.now() + Math.random(),
  drugInventoryId: '',
  quantity: 1,
  unitCost: 0,
});

export default function PurchaseOrdersPage() {
  const [orders, setOrders]         = useState<PurchaseOrderSummaryResponse[]>([]);
  const [suppliers, setSuppliers]   = useState<SupplierResponse[]>([]);
  const [drugs, setDrugs]           = useState<DrugInventoryResponse[]>([]);
  const [loading, setLoading]       = useState(true);
  const [error, setError]           = useState('');
  const [filterStatus, setFilterStatus] = useState('');

  const [showForm, setShowForm]     = useState(false);
  const [supplierId, setSupplierId] = useState('');
  const [expectedDate, setExpectedDate] = useState('');
  const [notes, setNotes]           = useState('');
  const [items, setItems]           = useState<(SavePurchaseOrderItemRequest & { _key: number })[]>([emptyItem()]);
  const [saving, setSaving]         = useState(false);

  const load = useCallback(async () => {
    setLoading(true);
    setError('');
    try {
      const [ordersData, suppliersData, drugsData] = await Promise.all([
        getPurchaseOrders({ status: filterStatus || undefined }),
        getSuppliers({ activeOnly: true }),
        getDrugs({ activeOnly: true }),
      ]);
      setOrders(ordersData);
      setSuppliers(suppliersData);
      setDrugs(drugsData);
    } catch {
      setError('Failed to load purchase orders.');
    } finally {
      setLoading(false);
    }
  }, [filterStatus]);

  useEffect(() => { load(); }, [load]);

  function openCreate() {
    setSupplierId('');
    setExpectedDate('');
    setNotes('');
    setItems([emptyItem()]);
    setShowForm(true);
  }

  function addItem() {
    setItems(prev => [...prev, emptyItem()]);
  }

  function removeItem(key: number) {
    setItems(prev => prev.filter(i => i._key !== key));
  }

  function updateItem(key: number, field: keyof SavePurchaseOrderItemRequest, value: string | number) {
    setItems(prev => prev.map(i => i._key === key ? { ...i, [field]: value } : i));
  }

  function getDrugUnitCost(drugId: string) {
    return drugs.find(d => d.drugInventoryId === drugId)?.unitCost ?? 0;
  }

  const totalAmount = items.reduce((sum, i) => sum + (i.quantity || 0) * (i.unitCost || 0), 0);

  async function handleSubmit(e: React.FormEvent) {
    e.preventDefault();
    if (items.some(i => !i.drugInventoryId)) {
      alert('All line items must have a drug selected.');
      return;
    }
    setSaving(true);
    try {
      const payload: SavePurchaseOrderRequest = {
        supplierId:           supplierId || undefined,
        expectedDeliveryDate: expectedDate || undefined,
        notes:                notes || undefined,
        items:                items.map(({ drugInventoryId, quantity, unitCost }) => ({
          drugInventoryId, quantity, unitCost,
        })),
      };
      await createPurchaseOrder(payload);
      setShowForm(false);
      await load();
    } catch (err) {
      const msg = (err as { response?: { data?: { error?: string } } })?.response?.data?.error ?? 'Save failed.';
      alert(msg);
    } finally {
      setSaving(false);
    }
  }

  const PO_STATUSES = ['', 'Draft', 'Ordered', 'PartiallyReceived', 'Received', 'Cancelled'];

  return (
    <div className="p-6 max-w-6xl">
      {/* Header */}
      <div className="flex items-center justify-between mb-6">
        <div>
          <h2 className="text-2xl font-semibold text-gray-800">Purchase Orders</h2>
          <p className="text-sm text-gray-500 mt-1">{orders.length} order{orders.length !== 1 ? 's' : ''}</p>
        </div>
        <button onClick={openCreate}
          className="px-4 py-2 bg-blue-700 hover:bg-blue-800 text-white text-sm font-medium rounded-lg transition-colors">
          + New Purchase Order
        </button>
      </div>

      {/* Filters */}
      <div className="flex items-center gap-4 mb-5">
        <select value={filterStatus} onChange={e => setFilterStatus(e.target.value)}
          className="px-3 py-1.5 border border-gray-300 rounded-lg text-sm text-gray-600 focus:outline-none focus:ring-2 focus:ring-blue-500">
          {PO_STATUSES.map(s => (
            <option key={s} value={s}>{s ? PO_STATUS_LABELS[s] : 'All statuses'}</option>
          ))}
        </select>
      </div>

      {loading ? (
        <div className="text-gray-400 py-8">Loading…</div>
      ) : error ? (
        <div className="text-red-600 py-4">{error}</div>
      ) : orders.length === 0 ? (
        <div className="bg-white rounded-xl border border-gray-200 px-6 py-12 text-center text-gray-400">
          No purchase orders found.
        </div>
      ) : (
        <div className="bg-white rounded-xl border border-gray-200 overflow-hidden">
          <table className="w-full text-sm">
            <thead className="border-b border-gray-100 bg-gray-50">
              <tr>
                <th className="text-left px-4 py-3 font-medium text-gray-500 text-xs uppercase">Order #</th>
                <th className="text-left px-4 py-3 font-medium text-gray-500 text-xs uppercase">Supplier</th>
                <th className="text-left px-4 py-3 font-medium text-gray-500 text-xs uppercase">Status</th>
                <th className="text-left px-4 py-3 font-medium text-gray-500 text-xs uppercase">Order Date</th>
                <th className="text-left px-4 py-3 font-medium text-gray-500 text-xs uppercase">Expected</th>
                <th className="text-center px-4 py-3 font-medium text-gray-500 text-xs uppercase">Items</th>
                <th className="text-right px-4 py-3 font-medium text-gray-500 text-xs uppercase">Total</th>
                <th className="px-4 py-3"></th>
              </tr>
            </thead>
            <tbody className="divide-y divide-gray-50">
              {orders.map(o => (
                <tr key={o.purchaseOrderId} className="hover:bg-gray-50 transition-colors">
                  <td className="px-4 py-3">
                    <span className="font-mono font-medium text-gray-800">{o.orderNumber}</span>
                  </td>
                  <td className="px-4 py-3 text-gray-600">{o.supplierName ?? <span className="text-gray-400 italic">No supplier</span>}</td>
                  <td className="px-4 py-3">
                    <span className={`text-xs font-medium px-2 py-0.5 rounded-full ${PO_STATUS_COLORS[o.status] ?? 'bg-gray-100 text-gray-600'}`}>
                      {PO_STATUS_LABELS[o.status] ?? o.status}
                    </span>
                  </td>
                  <td className="px-4 py-3 text-gray-500">{new Date(o.orderDate).toLocaleDateString()}</td>
                  <td className="px-4 py-3 text-gray-500">
                    {o.expectedDeliveryDate ? new Date(o.expectedDeliveryDate).toLocaleDateString() : '—'}
                  </td>
                  <td className="px-4 py-3 text-center text-gray-600">{o.itemCount}</td>
                  <td className="px-4 py-3 text-right font-mono text-gray-700">{fmt(o.totalAmount)}</td>
                  <td className="px-4 py-3 text-right">
                    <Link to={`/pharmacy/purchase-orders/${o.purchaseOrderId}`}
                      className="text-xs text-blue-600 hover:underline">View</Link>
                  </td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>
      )}

      {/* Create PO modal */}
      {showForm && (
        <div className="fixed inset-0 bg-black/40 flex items-start justify-center z-50 overflow-y-auto py-10">
          <div className="bg-white rounded-2xl shadow-xl p-6 w-full max-w-2xl mx-4">
            <h3 className="text-lg font-semibold text-gray-800 mb-5">New Purchase Order</h3>
            <form onSubmit={handleSubmit} className="space-y-4">
              <div className="grid grid-cols-2 gap-4">
                <div>
                  <label className="block text-xs text-gray-600 mb-1">Supplier</label>
                  <select value={supplierId} onChange={e => setSupplierId(e.target.value)} className={inp}>
                    <option value="">— No supplier —</option>
                    {suppliers.map(s => <option key={s.supplierId} value={s.supplierId}>{s.name}</option>)}
                  </select>
                </div>
                <div>
                  <label className="block text-xs text-gray-600 mb-1">Expected Delivery Date</label>
                  <input type="date" value={expectedDate}
                    onChange={e => setExpectedDate(e.target.value)} className={inp} />
                </div>
              </div>
              <div>
                <label className="block text-xs text-gray-600 mb-1">Notes</label>
                <input value={notes} onChange={e => setNotes(e.target.value)}
                  placeholder="Optional notes…" className={inp} />
              </div>

              {/* Line items */}
              <div>
                <div className="flex items-center justify-between mb-2">
                  <label className="text-xs font-medium text-gray-600 uppercase tracking-wide">Line Items *</label>
                  <button type="button" onClick={addItem}
                    className="text-xs text-blue-600 hover:underline">+ Add item</button>
                </div>
                <div className="space-y-2">
                  {items.map((item) => (
                    <div key={item._key} className="grid grid-cols-12 gap-2 items-end">
                      <div className="col-span-6">
                        <select
                          value={item.drugInventoryId}
                          onChange={e => {
                            const drugId = e.target.value;
                            updateItem(item._key, 'drugInventoryId', drugId);
                            if (drugId) updateItem(item._key, 'unitCost', getDrugUnitCost(drugId));
                          }}
                          className={inp}
                          required
                        >
                          <option value="">— Select drug —</option>
                          {drugs.map(d => (
                            <option key={d.drugInventoryId} value={d.drugInventoryId}>
                              {d.name}{d.strength ? ` ${d.strength}` : ''}{d.dosageForm ? ` · ${d.dosageForm}` : ''}
                            </option>
                          ))}
                        </select>
                      </div>
                      <div className="col-span-2">
                        <input type="number" min={1} placeholder="Qty"
                          value={item.quantity || ''}
                          onChange={e => updateItem(item._key, 'quantity', Number(e.target.value))}
                          className={inp} required />
                      </div>
                      <div className="col-span-3">
                        <input type="number" step="0.01" min={0} placeholder="Unit cost"
                          value={item.unitCost || ''}
                          onChange={e => updateItem(item._key, 'unitCost', Number(e.target.value))}
                          className={inp} />
                      </div>
                      <div className="col-span-1 flex justify-end pb-0.5">
                        {items.length > 1 && (
                          <button type="button" onClick={() => removeItem(item._key)}
                            className="text-red-400 hover:text-red-600 text-lg leading-none">×</button>
                        )}
                      </div>
                    </div>
                  ))}
                </div>
                <div className="text-right text-sm font-medium text-gray-700 mt-2">
                  Total: <span className="font-mono">{fmt(totalAmount)}</span>
                </div>
              </div>

              <div className="flex gap-3 justify-end pt-2">
                <button type="button" onClick={() => setShowForm(false)}
                  className="px-4 py-2 text-sm text-gray-600 border border-gray-300 rounded-lg hover:bg-gray-50">Cancel</button>
                <button type="submit" disabled={saving}
                  className="px-4 py-2 text-sm font-medium bg-blue-700 hover:bg-blue-800 text-white rounded-lg disabled:opacity-50 transition-colors">
                  {saving ? 'Creating…' : 'Create Draft'}
                </button>
              </div>
            </form>
          </div>
        </div>
      )}
    </div>
  );
}
