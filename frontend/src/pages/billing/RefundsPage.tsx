import { useState, useEffect } from 'react';
import { Link } from 'react-router-dom';
import { getRefunds } from '../../api/refunds';
import type { RefundResponse } from '../../types/refunds';
import { REFUND_METHOD_LABELS } from '../../types/refunds';

const STATUS_COLORS: Record<string, string> = {
  Pending:   'bg-yellow-100 text-yellow-700',
  Processed: 'bg-green-100 text-green-700',
  Cancelled: 'bg-gray-100 text-gray-500',
};

function fmt(n: number) { return `GHS ${n.toFixed(2)}`; }
function fmtDate(s: string | null) {
  if (!s) return '—';
  return new Date(s).toLocaleDateString('en-GB', { day: '2-digit', month: 'short', year: 'numeric' });
}

export default function RefundsPage() {
  const [refunds, setRefunds] = useState<RefundResponse[]>([]);
  const [loading, setLoading] = useState(true);
  const [status, setStatus]   = useState('');

  useEffect(() => {
    setLoading(true);
    getRefunds({ status: status || undefined })
      .then(setRefunds)
      .catch(() => {})
      .finally(() => setLoading(false));
  }, [status]);

  return (
    <div className="max-w-7xl mx-auto p-6">
      <h1 className="text-2xl font-bold text-gray-900 mb-6">Refunds</h1>

      <div className="bg-white border border-gray-200 rounded-lg p-4 mb-6 flex gap-4">
        <div>
          <label className="block text-xs text-gray-500 mb-1">Status</label>
          <select value={status} onChange={(e) => setStatus(e.target.value)}
            className="border border-gray-300 rounded px-3 py-1.5 text-sm focus:outline-none focus:ring-2 focus:ring-blue-500">
            <option value="">All</option>
            <option value="Pending">Pending</option>
            <option value="Processed">Processed</option>
            <option value="Cancelled">Cancelled</option>
          </select>
        </div>
      </div>

      <div className="bg-white border border-gray-200 rounded-lg overflow-hidden">
        {loading ? (
          <div className="p-8 text-center text-gray-500">Loading…</div>
        ) : refunds.length === 0 ? (
          <div className="p-8 text-center text-gray-500">No refunds found.</div>
        ) : (
          <table className="w-full text-sm">
            <thead className="bg-gray-50 border-b border-gray-200">
              <tr>
                <th className="px-4 py-3 text-left font-semibold text-gray-700">REF #</th>
                <th className="px-4 py-3 text-left font-semibold text-gray-700">Patient</th>
                <th className="px-4 py-3 text-left font-semibold text-gray-700">Invoice</th>
                <th className="px-4 py-3 text-left font-semibold text-gray-700">Method</th>
                <th className="px-4 py-3 text-left font-semibold text-gray-700">Status</th>
                <th className="px-4 py-3 text-right font-semibold text-gray-700">Amount</th>
                <th className="px-4 py-3 text-left font-semibold text-gray-700">Processed</th>
                <th className="px-4 py-3 text-left font-semibold text-gray-700">Created</th>
              </tr>
            </thead>
            <tbody className="divide-y divide-gray-100">
              {refunds.map((r) => (
                <tr key={r.refundId} className="hover:bg-gray-50">
                  <td className="px-4 py-3 font-mono font-medium text-blue-600">
                    <Link to={`/billing/refunds/${r.refundId}`} className="hover:underline">{r.refundNumber}</Link>
                  </td>
                  <td className="px-4 py-3">
                    <div className="font-medium">{r.patientName}</div>
                    <div className="text-xs text-gray-500">{r.patientMrn}</div>
                  </td>
                  <td className="px-4 py-3">
                    <Link to={`/billing/${r.billId}`} className="font-mono text-blue-600 hover:underline">{r.billNumber}</Link>
                  </td>
                  <td className="px-4 py-3 text-gray-700">{REFUND_METHOD_LABELS[r.refundMethod] ?? r.refundMethod}</td>
                  <td className="px-4 py-3">
                    <span className={`px-2 py-0.5 rounded-full text-xs font-medium ${STATUS_COLORS[r.status] ?? 'bg-gray-100'}`}>
                      {r.status}
                    </span>
                  </td>
                  <td className="px-4 py-3 text-right font-medium">{fmt(r.amount)}</td>
                  <td className="px-4 py-3 text-gray-600">{fmtDate(r.processedAt)}</td>
                  <td className="px-4 py-3 text-gray-600">{fmtDate(r.createdAt)}</td>
                </tr>
              ))}
            </tbody>
          </table>
        )}
      </div>
    </div>
  );
}
