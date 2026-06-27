import { useState, useEffect, useCallback } from 'react';
import { useParams, useNavigate } from 'react-router-dom';
import {
  getPurchaseOrder,
  placePurchaseOrder,
  receiveGoods,
  cancelPurchaseOrder,
  updatePurchaseOrder,
  getSuppliers,
  getDrugs,
} from '../../api/pharmacy';
import type {
  PurchaseOrderDetailResponse,
  SupplierResponse,
  ReceiveGoodsRequest,
  SavePurchaseOrderRequest,
  SavePurchaseOrderItemRequest,
} from '../../types/pharmacy';
import type { DrugInventoryResponse } from '../../types/pharmacy';
import { PO_STATUS_LABELS, PO_STATUS_COLORS } from '../../types/pharmacy';
import { safeArray } from '../../utils/array';


const inp = 'w-full px-3 py-2 border border-gray-300 rounded-lg text-sm focus:outline-none focus:ring-2 focus:ring-blue-500';
const fmt = (n: number) => `$${n.toFixed(2)}`;

const emptyItem = (): SavePurchaseOrderItemRequest & { _key: number } => ({
  _key: Date.now() + Math.random(),
  drugInventoryId: '',
  quantity: 1,
  unitCost: 0,
});

export default function PurchaseOrderDetailPage() {
  const { id } = useParams<{ id: string }>();
  const navigate = useNavigate();

  const [po, setPo]           = useState<PurchaseOrderDetailResponse | null>(null);
  const [loading, setLoading] = useState(true);
  const [error, setError]     = useState('');
  const [acting, setActing]   = useState(false);

  // Receive modal
  const [showReceive, setShowReceive]   = useState(false);
  const [receiveQtys, setReceiveQtys]   = useState<Record<string, number>>({});

  // Edit modal (Draft only)
  const [showEdit, setShowEdit]         = useState(false);
  const [suppliers, setSuppliers]       = useState<SupplierResponse[]>([]);
  const [drugs, setDrugs]               = useState<DrugInventoryResponse[]>([]);
  const [editSupplierId, setEditSupplierId]     = useState('');
  const [editExpectedDate, setEditExpectedDate] = useState('');
  const [editNotes, setEditNotes]               = useState('');
  const [editItems, setEditItems]               = useState<(SavePurchaseOrderItemRequest & { _key: number })[]>([]);

  const load = useCallback(async () => {
    if (!id) return;
    setLoading(true);
    setError('');
    try {
      setPo(await getPurchaseOrder(id));
    } catch {
      setError('Failed to load purchase order.');
    } finally {
      setLoading(false);
    }
  }, [id]);

  useEffect(() => { load(); }, [load]);

  async function openEdit() {
    if (!po) return;
    const [suppliersData, drugsData] = await Promise.all([
      getSuppliers({ activeOnly: true }),
      getDrugs({ activeOnly: true }),
    ]);
    setSuppliers(suppliersData);
    setDrugs(drugsData);
    setEditSupplierId(po.supplierId ?? '');
    setEditExpectedDate(po.expectedDeliveryDate ? po.expectedDeliveryDate.split('T')[0] : '');
    setEditNotes(po.notes ?? '');
    setEditItems(safeArray(po.items).map(i => ({
      _key: Math.random(),
      drugInventoryId: i.drugInventoryId,
      quantity: i.quantity,
      unitCost: i.unitCost,
    })));
    setShowEdit(true);
  }

  async function handleEdit(e: React.FormEvent) {
    e.preventDefault();
    if (!po) return;
    setActing(true);
    try {
      const payload: SavePurchaseOrderRequest = {
        supplierId:           editSupplierId || undefined,
        expectedDeliveryDate: editExpectedDate || undefined,
        notes:                editNotes || undefined,
        items:                editItems.map(({ drugInventoryId, quantity, unitCost }) => ({
          drugInventoryId, quantity, unitCost,
        })),
      };
      await updatePurchaseOrder(po.purchaseOrderId, payload);
      setShowEdit(false);
      await load();
    } catch (err) {
      const msg = (err as { response?: { data?: { error?: string } } })?.response?.data?.error ?? 'Save failed.';
      alert(msg);
    } finally {
      setActing(false);
    }
  }

  async function handlePlace() {
    if (!po || !confirm('Mark this order as Placed/Sent to supplier?')) return;
    setActing(true);
    try {
      await placePurchaseOrder(po.purchaseOrderId);
      await load();
    } catch (err) {
      const msg = (err as { response?: { data?: { error?: string } } })?.response?.data?.error ?? 'Failed to place order.';
      alert(msg);
    } finally {
      setActing(false);
    }
  }

  async function handleCancel() {
    if (!po || !confirm(`Cancel purchase order ${po.orderNumber}? This cannot be undone.`)) return;
    setActing(true);
    try {
      await cancelPurchaseOrder(po.purchaseOrderId);
      await load();
    } catch (err) {
      const msg = (err as { response?: { data?: { error?: string } } })?.response?.data?.error ?? 'Failed to cancel.';
      alert(msg);
    } finally {
      setActing(false);
    }
  }

  function openReceive() {
    if (!po) return;
    const defaults: Record<string, number> = {};
    po.items.forEach(i => {
      defaults[i.purchaseOrderItemId] = i.quantityPending;
    });
    setReceiveQtys(defaults);
    setShowReceive(true);
  }

  async function handleReceive(e: React.FormEvent) {
    e.preventDefault();
    if (!po) return;
    setActing(true);
    try {
      const req: ReceiveGoodsRequest = {
        items: po.items
          .filter(i => (receiveQtys[i.purchaseOrderItemId] ?? 0) > 0)
          .map(i => ({
            purchaseOrderItemId: i.purchaseOrderItemId,
            quantityReceived:    receiveQtys[i.purchaseOrderItemId] ?? 0,
          })),
      };
      if (req.items.length === 0) {
        alert('Enter quantity received for at least one item.');
        return;
      }
      await receiveGoods(po.purchaseOrderId, req);
      setShowReceive(false);
      await load();
    } catch (err) {
      const msg = (err as { response?: { data?: { error?: string } } })?.response?.data?.error ?? 'Failed to receive goods.';
      alert(msg);
    } finally {
      setActing(false);
    }
  }

  function getDrugUnitCost(drugId: string) {
    return drugs.find(d => d.drugInventoryId === drugId)?.unitCost ?? 0;
  }

  if (loading) return <div className="p-8 text-gray-400">Loading…</div>;
  if (error)   return <div className="p-8 text-red-600">{error}</div>;
  if (!po)     return <div className="p-8 text-gray-400">Purchase order not found.</div>;

  const canEdit    = po.status === 'Draft';
  const canPlace   = po.status === 'Draft';
  const canReceive = po.status === 'Ordered' || po.status === 'PartiallyReceived';
  const canCancel  = po.status === 'Draft' || po.status === 'Ordered';

  return (
    <div className="p-6 max-w-4xl">
      {/* Back link */}
      <button onClick={() => navigate('/pharmacy/purchase-orders')}
        className="text-sm text-blue-600 hover:underline mb-4 block">← Purchase Orders</button>

      {/* Header */}
      <div className="flex items-start justify-between mb-6">
        <div>
          <div className="flex items-center gap-3">
            <h2 className="text-2xl font-semibold text-gray-800">{po.orderNumber}</h2>
            <span className={`text-sm font-medium px-2.5 py-1 rounded-full ${PO_STATUS_COLORS[po.status] ?? 'bg-gray-100 text-gray-600'}`}>
              {PO_STATUS_LABELS[po.status] ?? po.status}
            </span>
          </div>
          <p className="text-sm text-gray-500 mt-1">
            Created {new Date(po.createdAt).toLocaleDateString()}
          </p>
        </div>
        <div className="flex gap-2">
          {canEdit    && <button onClick={openEdit}    disabled={acting} className="px-3 py-1.5 text-sm text-gray-600 border border-gray-300 rounded-lg hover:bg-gray-50 disabled:opacity-50">Edit</button>}
          {canPlace   && <button onClick={handlePlace}  disabled={acting} className="px-3 py-1.5 text-sm text-blue-700 border border-blue-300 bg-blue-50 rounded-lg hover:bg-blue-100 disabled:opacity-50">Place Order</button>}
          {canReceive && <button onClick={openReceive}  disabled={acting} className="px-3 py-1.5 text-sm text-white bg-green-600 rounded-lg hover:bg-green-700 disabled:opacity-50">Receive Goods</button>}
          {canCancel  && <button onClick={handleCancel} disabled={acting} className="px-3 py-1.5 text-sm text-red-600 border border-red-200 rounded-lg hover:bg-red-50 disabled:opacity-50">Cancel</button>}
        </div>
      </div>

      {/* Summary cards */}
      <div className="grid grid-cols-2 gap-4 mb-6">
        <div className="bg-white rounded-xl border border-gray-200 p-4">
          <p className="text-xs text-gray-400 uppercase tracking-wide mb-2">Supplier</p>
          {po.supplierName ? (
            <>
              <p className="font-medium text-gray-800">{po.supplierName}</p>
              {po.supplierPhone && <p className="text-xs text-gray-500 mt-0.5">{po.supplierPhone}</p>}
              {po.supplierEmail && <p className="text-xs text-blue-600 mt-0.5">{po.supplierEmail}</p>}
            </>
          ) : (
            <p className="text-gray-400 italic text-sm">No supplier assigned</p>
          )}
        </div>
        <div className="bg-white rounded-xl border border-gray-200 p-4">
          <p className="text-xs text-gray-400 uppercase tracking-wide mb-2">Dates</p>
          <div className="space-y-1">
            <div className="flex justify-between text-sm">
              <span className="text-gray-500">Order date</span>
              <span className="font-medium text-gray-700">{new Date(po.orderDate).toLocaleDateString()}</span>
            </div>
            <div className="flex justify-between text-sm">
              <span className="text-gray-500">Expected delivery</span>
              <span className="font-medium text-gray-700">
                {po.expectedDeliveryDate ? new Date(po.expectedDeliveryDate).toLocaleDateString() : '—'}
              </span>
            </div>
          </div>
        </div>
      </div>

      {po.notes && (
        <div className="bg-yellow-50 border border-yellow-200 rounded-xl p-4 mb-6 text-sm text-gray-700">
          <span className="font-medium">Notes: </span>{po.notes}
        </div>
      )}

      {/* Line items */}
      <div className="bg-white rounded-xl border border-gray-200 overflow-hidden mb-6">
        <div className="px-4 py-3 border-b border-gray-100 bg-gray-50">
          <h3 className="text-sm font-medium text-gray-700">Line Items</h3>
        </div>
        <table className="w-full text-sm">
          <thead className="border-b border-gray-100">
            <tr>
              <th className="text-left px-4 py-2.5 font-medium text-gray-500 text-xs uppercase">Drug</th>
              <th className="text-center px-4 py-2.5 font-medium text-gray-500 text-xs uppercase">Ordered</th>
              <th className="text-center px-4 py-2.5 font-medium text-gray-500 text-xs uppercase">Received</th>
              <th className="text-center px-4 py-2.5 font-medium text-gray-500 text-xs uppercase">Pending</th>
              <th className="text-right px-4 py-2.5 font-medium text-gray-500 text-xs uppercase">Unit Cost</th>
              <th className="text-right px-4 py-2.5 font-medium text-gray-500 text-xs uppercase">Total</th>
            </tr>
          </thead>
          <tbody className="divide-y divide-gray-50">
            {safeArray(po.items).map(item => (
              <tr key={item.purchaseOrderItemId} className={item.isFullyReceived ? 'bg-green-50' : ''}>
                <td className="px-4 py-3">
                  <p className="font-medium text-gray-800">{item.drugName}</p>
                  {(item.dosageForm || item.strength) && (
                    <p className="text-xs text-gray-400 mt-0.5">
                      {[item.dosageForm, item.strength].filter(Boolean).join(' · ')}
                    </p>
                  )}
                </td>
                <td className="px-4 py-3 text-center">
                  <span className="font-mono text-gray-700">{item.quantity}</span>
                  <span className="text-xs text-gray-400 ml-1">{item.unit}</span>
                </td>
                <td className="px-4 py-3 text-center">
                  <span className={`font-mono font-semibold ${item.quantityReceived > 0 ? 'text-green-600' : 'text-gray-400'}`}>
                    {item.quantityReceived}
                  </span>
                </td>
                <td className="px-4 py-3 text-center">
                  <span className={`font-mono ${item.quantityPending > 0 ? 'text-orange-600' : 'text-gray-400'}`}>
                    {item.quantityPending}
                  </span>
                </td>
                <td className="px-4 py-3 text-right font-mono text-gray-600">{fmt(item.unitCost)}</td>
                <td className="px-4 py-3 text-right font-mono font-medium text-gray-700">{fmt(item.totalCost)}</td>
              </tr>
            ))}
          </tbody>
          <tfoot className="border-t-2 border-gray-200">
            <tr>
              <td colSpan={5} className="px-4 py-3 text-right text-sm font-semibold text-gray-600">Total Order Value</td>
              <td className="px-4 py-3 text-right font-mono font-bold text-gray-800">{fmt(po.totalAmount)}</td>
            </tr>
          </tfoot>
        </table>
      </div>

      {/* Edit modal */}
      {showEdit && (
        <div className="fixed inset-0 bg-black/40 flex items-start justify-center z-50 overflow-y-auto py-10">
          <div className="bg-white rounded-2xl shadow-xl p-6 w-full max-w-2xl mx-4">
            <h3 className="text-lg font-semibold text-gray-800 mb-5">Edit Purchase Order</h3>
            <form onSubmit={handleEdit} className="space-y-4">
              <div className="grid grid-cols-2 gap-4">
                <div>
                  <label className="block text-xs text-gray-600 mb-1">Supplier</label>
                  <select value={editSupplierId} onChange={e => setEditSupplierId(e.target.value)} className={inp}>
                    <option value="">— No supplier —</option>
                    {suppliers.map(s => <option key={s.supplierId} value={s.supplierId}>{s.name}</option>)}
                  </select>
                </div>
                <div>
                  <label className="block text-xs text-gray-600 mb-1">Expected Delivery Date</label>
                  <input type="date" value={editExpectedDate}
                    onChange={e => setEditExpectedDate(e.target.value)} className={inp} />
                </div>
              </div>
              <div>
                <label className="block text-xs text-gray-600 mb-1">Notes</label>
                <input value={editNotes} onChange={e => setEditNotes(e.target.value)}
                  placeholder="Optional notes…" className={inp} />
              </div>
              {/* Line items */}
              <div>
                <div className="flex items-center justify-between mb-2">
                  <label className="text-xs font-medium text-gray-600 uppercase tracking-wide">Line Items *</label>
                  <button type="button" onClick={() => setEditItems(prev => [...prev, emptyItem()])}
                    className="text-xs text-blue-600 hover:underline">+ Add item</button>
                </div>
                <div className="space-y-2">
                  {editItems.map((item) => (
                    <div key={item._key} className="grid grid-cols-12 gap-2 items-end">
                      <div className="col-span-6">
                        <select
                          value={item.drugInventoryId}
                          onChange={e => {
                            const drugId = e.target.value;
                            setEditItems(prev => prev.map(i => i._key === item._key
                              ? { ...i, drugInventoryId: drugId, unitCost: getDrugUnitCost(drugId) }
                              : i
                            ));
                          }}
                          className={inp} required>
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
                          onChange={e => setEditItems(prev => prev.map(i => i._key === item._key ? { ...i, quantity: Number(e.target.value) } : i))}
                          className={inp} required />
                      </div>
                      <div className="col-span-3">
                        <input type="number" step="0.01" min={0} placeholder="Unit cost"
                          value={item.unitCost || ''}
                          onChange={e => setEditItems(prev => prev.map(i => i._key === item._key ? { ...i, unitCost: Number(e.target.value) } : i))}
                          className={inp} />
                      </div>
                      <div className="col-span-1 flex justify-end pb-0.5">
                        {editItems.length > 1 && (
                          <button type="button"
                            onClick={() => setEditItems(prev => prev.filter(i => i._key !== item._key))}
                            className="text-red-400 hover:text-red-600 text-lg leading-none">×</button>
                        )}
                      </div>
                    </div>
                  ))}
                </div>
              </div>
              <div className="flex gap-3 justify-end pt-2">
                <button type="button" onClick={() => setShowEdit(false)}
                  className="px-4 py-2 text-sm text-gray-600 border border-gray-300 rounded-lg hover:bg-gray-50">Cancel</button>
                <button type="submit" disabled={acting}
                  className="px-4 py-2 text-sm font-medium bg-blue-700 hover:bg-blue-800 text-white rounded-lg disabled:opacity-50 transition-colors">
                  {acting ? 'Saving…' : 'Save Changes'}
                </button>
              </div>
            </form>
          </div>
        </div>
      )}

      {/* Receive goods modal */}
      {showReceive && (
        <div className="fixed inset-0 bg-black/40 flex items-center justify-center z-50">
          <div className="bg-white rounded-2xl shadow-xl p-6 w-full max-w-lg mx-4">
            <h3 className="text-lg font-semibold text-gray-800 mb-1">Receive Goods</h3>
            <p className="text-sm text-gray-500 mb-4">Enter the quantity received for each item in this delivery.</p>
            <form onSubmit={handleReceive} className="space-y-3">
              {po.items.filter(i => !i.isFullyReceived).map(item => (
                <div key={item.purchaseOrderItemId} className="flex items-center gap-4">
                  <div className="flex-1">
                    <p className="text-sm font-medium text-gray-700">{item.drugName}</p>
                    <p className="text-xs text-gray-400">Pending: {item.quantityPending} {item.unit}</p>
                  </div>
                  <div className="w-28">
                    <input
                      type="number"
                      min={0}
                      max={item.quantityPending}
                      value={receiveQtys[item.purchaseOrderItemId] ?? 0}
                      onChange={e => setReceiveQtys(prev => ({
                        ...prev,
                        [item.purchaseOrderItemId]: Number(e.target.value),
                      }))}
                      className={inp}
                    />
                  </div>
                </div>
              ))}
              {po.items.every(i => i.isFullyReceived) && (
                <p className="text-green-600 text-sm">All items have been fully received.</p>
              )}
              <div className="flex gap-3 justify-end pt-2">
                <button type="button" onClick={() => setShowReceive(false)}
                  className="px-4 py-2 text-sm text-gray-600 border border-gray-300 rounded-lg hover:bg-gray-50">Cancel</button>
                <button type="submit" disabled={acting || po.items.every(i => i.isFullyReceived)}
                  className="px-4 py-2 text-sm font-medium bg-green-600 hover:bg-green-700 text-white rounded-lg disabled:opacity-50 transition-colors">
                  {acting ? 'Receiving…' : 'Confirm Receipt'}
                </button>
              </div>
            </form>
          </div>
        </div>
      )}
    </div>
  );
}
