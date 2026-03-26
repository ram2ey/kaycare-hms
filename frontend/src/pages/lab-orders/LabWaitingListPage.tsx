import { useEffect, useState } from 'react';
import { Link } from 'react-router-dom';
import {
  getWaitingList,
  getLabOrderById,
  receiveSample,
  enterManualResult,
  signItem,
} from '../../api/labOrders';
import type { LabOrder, LabOrderItem, LabOrderStatus } from '../../types/labOrders';
import { ORDER_STATUS_COLORS, ITEM_STATUS_COLORS, DEPARTMENTS } from '../../types/labOrders';

// ── Barcode print helper ──────────────────────────────────────────────────────
function printBarcode(item: LabOrderItem, patientName: string, patientMrn: string) {
  const win = window.open('', '_blank', 'width=400,height=300');
  if (!win) return;
  win.document.write(`
    <html><head><title>Label</title>
    <style>
      body { font-family: monospace; margin: 10px; }
      .label { border: 1px dashed #333; padding: 8px; width: 280px; }
      h3 { margin: 0 0 4px; font-size: 13px; }
      p { margin: 2px 0; font-size: 11px; }
      .acc { font-size: 18px; font-weight: bold; letter-spacing: 3px; margin: 6px 0; }
    </style></head>
    <body onload="window.print();window.close()">
      <div class="label">
        <h3>${patientName}</h3>
        <p>MRN: ${patientMrn}</p>
        <p>Test: ${item.testName}</p>
        <p>Dept: ${item.department}</p>
        <div class="acc">|||  ${item.accessionNumber}  |||</div>
        <p>${item.accessionNumber}</p>
        <p>${new Date().toLocaleDateString()}</p>
      </div>
    </body></html>
  `);
}

// ── Manual result modal ───────────────────────────────────────────────────────
function ManualResultModal({
  item,
  onSave,
  onClose,
}: {
  item: LabOrderItem;
  onSave: (result: string, notes: string) => Promise<void>;
  onClose: () => void;
}) {
  const [result, setResult] = useState('');
  const [notes, setNotes] = useState('');
  const [saving, setSaving] = useState(false);

  return (
    <div className="fixed inset-0 bg-black/40 flex items-center justify-center z-50">
      <div className="bg-white rounded-lg shadow-xl w-full max-w-md p-6">
        <h3 className="text-lg font-semibold mb-1">Enter Result</h3>
        <p className="text-sm text-gray-500 mb-4">{item.testName}</p>
        <textarea
          className="w-full border rounded px-3 py-2 text-sm mb-3 h-20 resize-none"
          placeholder="Result (e.g. Negative, Positive, Grade I, etc.)"
          value={result}
          onChange={e => setResult(e.target.value)}
        />
        <textarea
          className="w-full border rounded px-3 py-2 text-sm mb-4 h-16 resize-none"
          placeholder="Notes (optional)"
          value={notes}
          onChange={e => setNotes(e.target.value)}
        />
        <div className="flex gap-2 justify-end">
          <button onClick={onClose} className="px-4 py-2 text-sm border rounded hover:bg-gray-50">
            Cancel
          </button>
          <button
            disabled={!result.trim() || saving}
            onClick={async () => {
              setSaving(true);
              await onSave(result.trim(), notes.trim());
            }}
            className="px-4 py-2 text-sm bg-blue-600 text-white rounded hover:bg-blue-700 disabled:opacity-50"
          >
            {saving ? 'Saving…' : 'Save Result'}
          </button>
        </div>
      </div>
    </div>
  );
}

// ── Status sidebar counts ─────────────────────────────────────────────────────
const STATUS_TABS: { label: string; value: LabOrderStatus | '' }[] = [
  { label: 'All Tests',           value: '' },
  { label: 'Pending',             value: 'Pending' },
  { label: 'Active',              value: 'Active' },
  { label: 'Partially Completed', value: 'PartiallyCompleted' },
  { label: 'Completed',           value: 'Completed' },
  { label: 'Signed',              value: 'Signed' },
];

// ── Main page ─────────────────────────────────────────────────────────────────
export default function LabWaitingListPage() {
  const today = new Date().toISOString().slice(0, 10);
  const [date, setDate] = useState(today);
  const [status, setStatus] = useState<LabOrderStatus | ''>('');
  const [department, setDepartment] = useState('');
  const [orders, setOrders] = useState<LabOrder[]>([]);
  const [loading, setLoading] = useState(false);
  const [manualItem, setManualItem] = useState<{ item: LabOrderItem; order: LabOrder } | null>(null);

  const load = async () => {
    setLoading(true);
    try {
      const data = await getWaitingList(date, status || undefined, department || undefined);
      setOrders(data);
    } finally {
      setLoading(false);
    }
  };

  useEffect(() => { load(); }, [date, status, department]);

  const totalIncomplete = orders.reduce((s, o) => s + o.incompleteCount, 0);
  const totalCompleted  = orders.reduce((s, o) => s + o.completedCount, 0);
  const totalSigned     = orders.reduce((s, o) => s + o.signedCount, 0);

  const handleReceive = async (itemId: string) => {
    await receiveSample(itemId);
    load();
  };

  const handleManualSave = async (item: LabOrderItem, result: string, notes: string) => {
    await enterManualResult(item.labOrderItemId, result, notes);
    setManualItem(null);
    load();
  };

  const handleSign = async (itemId: string) => {
    await signItem(itemId);
    load();
  };

  return (
    <div className="flex h-full">
      {/* ── Left sidebar ──────────────────────────────────────────────────── */}
      <aside className="w-52 bg-white border-r flex flex-col py-4 shrink-0">
        <div className="px-4 pb-3 border-b">
          <p className="text-xs font-semibold text-gray-500 uppercase tracking-wide">Status</p>
        </div>
        {STATUS_TABS.map(tab => (
          <button
            key={tab.value}
            onClick={() => setStatus(tab.value)}
            className={`text-left px-4 py-2.5 text-sm transition-colors ${
              status === tab.value
                ? 'bg-blue-50 text-blue-700 font-medium border-l-2 border-blue-600'
                : 'text-gray-600 hover:bg-gray-50'
            }`}
          >
            {tab.label}
          </button>
        ))}
      </aside>

      {/* ── Main content ──────────────────────────────────────────────────── */}
      <div className="flex-1 flex flex-col overflow-hidden">
        {/* Header */}
        <div className="bg-white border-b px-6 py-4">
          <div className="flex items-center justify-between mb-3">
            <h1 className="text-xl font-semibold text-gray-800">Waiting List</h1>
            <Link
              to="/lab-orders/new"
              className="px-4 py-2 bg-blue-600 text-white text-sm rounded hover:bg-blue-700"
            >
              + Place Order
            </Link>
          </div>

          {/* Filters */}
          <div className="flex gap-3 items-center">
            <input
              type="date"
              value={date}
              onChange={e => setDate(e.target.value)}
              className="border rounded px-3 py-1.5 text-sm"
            />
            <select
              value={department}
              onChange={e => setDepartment(e.target.value)}
              className="border rounded px-3 py-1.5 text-sm"
            >
              <option value="">All Departments</option>
              {DEPARTMENTS.map(d => <option key={d} value={d}>{d}</option>)}
            </select>
            <button onClick={load} className="px-3 py-1.5 border rounded text-sm hover:bg-gray-50">
              Refresh
            </button>
            <div className="ml-auto flex gap-4 text-sm text-gray-600">
              <span>Rows: <strong>{orders.length}</strong></span>
              <span className="text-orange-600">Incomplete: <strong>{totalIncomplete}</strong></span>
              <span className="text-green-600">Completed: <strong>{totalCompleted}</strong></span>
              <span className="text-purple-600">Signed: <strong>{totalSigned}</strong></span>
            </div>
          </div>
        </div>

        {/* Table */}
        <div className="flex-1 overflow-y-auto p-4">
          {loading ? (
            <div className="text-center py-16 text-gray-400">Loading…</div>
          ) : orders.length === 0 ? (
            <div className="text-center py-16 text-gray-400">No orders found for this date.</div>
          ) : (
            <div className="bg-white rounded-lg border overflow-hidden">
              <table className="w-full text-sm">
                <thead>
                  <tr className="bg-gray-50 border-b text-left text-xs font-semibold text-gray-500 uppercase tracking-wide">
                    <th className="px-4 py-3">Patient Details</th>
                    <th className="px-4 py-3">Bill</th>
                    <th className="px-4 py-3">Referral</th>
                    <th className="px-4 py-3">Organisation</th>
                    <th className="px-4 py-3 text-orange-600">Incomplete</th>
                    <th className="px-4 py-3 text-green-600">Completed</th>
                    <th className="px-4 py-3 text-purple-600">Signed</th>
                    <th className="px-4 py-3"></th>
                  </tr>
                </thead>
                <tbody>
                  {orders.map(order => (
                    <OrderRow
                      key={order.labOrderId}
                      order={order}
                      onReceive={handleReceive}
                      onManualEntry={(item) => setManualItem({ item, order })}
                      onSign={handleSign}
                      onPrint={(item) => printBarcode(item, order.patientName, order.patientMrn)}
                    />
                  ))}
                </tbody>
              </table>
            </div>
          )}
        </div>
      </div>

      {/* Manual result modal */}
      {manualItem && (
        <ManualResultModal
          item={manualItem.item}
          onSave={(result, notes) => handleManualSave(manualItem.item, result, notes)}
          onClose={() => setManualItem(null)}
        />
      )}
    </div>
  );
}

// ── Order row (expandable) ────────────────────────────────────────────────────
function OrderRow({
  order,
  onReceive,
  onManualEntry,
  onSign,
  onPrint,
}: {
  order: LabOrder;
  onReceive: (itemId: string) => void;
  onManualEntry: (item: LabOrderItem) => void;
  onSign: (itemId: string) => void;
  onPrint: (item: LabOrderItem) => void;
}) {
  const [expanded, setExpanded] = useState(false);
  // LabOrder from the list endpoint doesn't include items — load detail on expand
  const [items, setItems] = useState<LabOrderItem[]>([]);
  const [loadingItems, setLoadingItems] = useState(false);

  const toggleExpand = async () => {
    if (!expanded && items.length === 0) {
      setLoadingItems(true);
      const detail = await getLabOrderById(order.labOrderId);
      setItems(detail.items);
      setLoadingItems(false);
    }
    setExpanded(e => !e);
  };

  const dob = order.patientDob ? new Date(order.patientDob) : null;
  const age = dob ? Math.floor((Date.now() - dob.getTime()) / (365.25 * 24 * 3600 * 1000)) : '';

  return (
    <>
      <tr
        className="border-b hover:bg-gray-50 cursor-pointer"
        onClick={toggleExpand}
      >
        <td className="px-4 py-3">
          <div className="font-medium text-gray-800">{order.patientName}</div>
          <div className="text-xs text-gray-500">
            {age} yrs / {order.patientGender.charAt(0)} · MRN: {order.patientMrn}
          </div>
          <div className="text-xs text-gray-400 mt-0.5">{order.testNames.join(', ')}</div>
        </td>
        <td className="px-4 py-3 text-gray-600">{order.billNumber ?? '—'}</td>
        <td className="px-4 py-3 text-gray-600">{order.orderingDoctorName}</td>
        <td className="px-4 py-3 text-gray-600">{order.organisation}</td>
        <td className="px-4 py-3 text-orange-600 font-semibold">{order.incompleteCount}</td>
        <td className="px-4 py-3 text-green-600 font-semibold">{order.completedCount}</td>
        <td className="px-4 py-3 text-purple-600 font-semibold">{order.signedCount}</td>
        <td className="px-4 py-3">
          <span className={`px-2 py-0.5 rounded text-xs font-medium ${ORDER_STATUS_COLORS[order.status]}`}>
            {order.status}
          </span>
        </td>
      </tr>

      {expanded && (
        <tr className="bg-blue-50/30 border-b">
          <td colSpan={8} className="px-6 py-3">
            {loadingItems ? (
              <div className="text-sm text-gray-400 py-2">Loading items…</div>
            ) : (
              <table className="w-full text-xs">
                <thead>
                  <tr className="text-gray-500">
                    <th className="text-left py-1 pr-4 font-medium">Test</th>
                    <th className="text-left py-1 pr-4 font-medium">Dept</th>
                    <th className="text-left py-1 pr-4 font-medium">Accession</th>
                    <th className="text-left py-1 pr-4 font-medium">Status</th>
                    <th className="text-left py-1 pr-4 font-medium">Result</th>
                    <th className="py-1"></th>
                  </tr>
                </thead>
                <tbody>
                  {items.map(item => (
                    <tr key={item.labOrderItemId} className="border-t border-blue-100">
                      <td className="py-1.5 pr-4">
                        <span className="font-medium">{item.testName}</span>
                        {item.isTatExceeded && (
                          <span className="ml-2 text-red-600 font-semibold">⚠ TAT</span>
                        )}
                      </td>
                      <td className="py-1.5 pr-4 text-gray-500">{item.department}</td>
                      <td className="py-1.5 pr-4 font-mono">{item.accessionNumber ?? '—'}</td>
                      <td className="py-1.5 pr-4">
                        <span className={`px-2 py-0.5 rounded text-xs ${ITEM_STATUS_COLORS[item.status]}`}>
                          {item.status}
                        </span>
                      </td>
                      <td className="py-1.5 pr-4 text-gray-600">{item.manualResult ?? '—'}</td>
                      <td className="py-1.5">
                        <div className="flex gap-2">
                          {item.status === 'Ordered' && (
                            <button
                              onClick={e => { e.stopPropagation(); onReceive(item.labOrderItemId); }}
                              className="px-2 py-1 bg-blue-600 text-white rounded text-xs hover:bg-blue-700"
                            >
                              Received
                            </button>
                          )}
                          {item.status === 'SampleReceived' && item.accessionNumber && (
                            <button
                              onClick={e => { e.stopPropagation(); onPrint(item); }}
                              className="px-2 py-1 bg-gray-600 text-white rounded text-xs hover:bg-gray-700"
                            >
                              Print
                            </button>
                          )}
                          {item.status === 'SampleReceived' && item.isManualEntry && (
                            <button
                              onClick={e => { e.stopPropagation(); onManualEntry(item); }}
                              className="px-2 py-1 bg-orange-500 text-white rounded text-xs hover:bg-orange-600"
                            >
                              Enter Result
                            </button>
                          )}
                          {item.status === 'Resulted' && (
                            <button
                              onClick={e => { e.stopPropagation(); onSign(item.labOrderItemId); }}
                              className="px-2 py-1 bg-purple-600 text-white rounded text-xs hover:bg-purple-700"
                            >
                              Sign
                            </button>
                          )}
                        </div>
                      </td>
                    </tr>
                  ))}
                </tbody>
              </table>
            )}
          </td>
        </tr>
      )}
    </>
  );
}
