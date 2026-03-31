import { useState, useEffect } from 'react';
import { Link } from 'react-router-dom';
import { getCreditNotes } from '../../api/creditNotes';
import type { CreditNoteResponse } from '../../types/creditNotes';
import { CREDIT_NOTE_STATUS_LABELS } from '../../types/creditNotes';

const STATUS_COLORS: Record<string, string> = {
  Draft:    'bg-gray-100 text-gray-700',
  Approved: 'bg-blue-100 text-blue-700',
  Applied:  'bg-green-100 text-green-700',
  Voided:   'bg-gray-100 text-gray-400',
};

function fmt(n: number) { return `GHS ${n.toFixed(2)}`; }
function fmtDate(s: string | null) {
  if (!s) return '—';
  return new Date(s).toLocaleDateString('en-GB', { day: '2-digit', month: 'short', year: 'numeric' });
}

export default function CreditNotesPage() {
  const [notes, setNotes]     = useState<CreditNoteResponse[]>([]);
  const [loading, setLoading] = useState(true);
  const [status, setStatus]   = useState('');

  useEffect(() => {
    setLoading(true);
    getCreditNotes({ status: status || undefined })
      .then(setNotes)
      .catch(() => {})
      .finally(() => setLoading(false));
  }, [status]);

  return (
    <div className="max-w-7xl mx-auto p-6">
      <h1 className="text-2xl font-bold text-gray-900 mb-6">Credit Notes</h1>

      <div className="bg-white border border-gray-200 rounded-lg p-4 mb-6 flex gap-4">
        <div>
          <label className="block text-xs text-gray-500 mb-1">Status</label>
          <select value={status} onChange={(e) => setStatus(e.target.value)}
            className="border border-gray-300 rounded px-3 py-1.5 text-sm focus:outline-none focus:ring-2 focus:ring-blue-500">
            <option value="">All</option>
            {Object.entries(CREDIT_NOTE_STATUS_LABELS).map(([v, l]) => (
              <option key={v} value={v}>{l}</option>
            ))}
          </select>
        </div>
      </div>

      <div className="bg-white border border-gray-200 rounded-lg overflow-hidden">
        {loading ? (
          <div className="p-8 text-center text-gray-500">Loading…</div>
        ) : notes.length === 0 ? (
          <div className="p-8 text-center text-gray-500">No credit notes found.</div>
        ) : (
          <table className="w-full text-sm">
            <thead className="bg-gray-50 border-b border-gray-200">
              <tr>
                <th className="px-4 py-3 text-left font-semibold text-gray-700">CN #</th>
                <th className="px-4 py-3 text-left font-semibold text-gray-700">Patient</th>
                <th className="px-4 py-3 text-left font-semibold text-gray-700">Invoice</th>
                <th className="px-4 py-3 text-left font-semibold text-gray-700">Reason</th>
                <th className="px-4 py-3 text-left font-semibold text-gray-700">Status</th>
                <th className="px-4 py-3 text-right font-semibold text-gray-700">Amount</th>
                <th className="px-4 py-3 text-left font-semibold text-gray-700">Applied</th>
                <th className="px-4 py-3 text-left font-semibold text-gray-700">Created</th>
              </tr>
            </thead>
            <tbody className="divide-y divide-gray-100">
              {notes.map((cn) => (
                <tr key={cn.creditNoteId} className="hover:bg-gray-50">
                  <td className="px-4 py-3 font-mono font-medium text-blue-600">
                    <Link to={`/billing/credit-notes/${cn.creditNoteId}`} className="hover:underline">
                      {cn.creditNoteNumber}
                    </Link>
                  </td>
                  <td className="px-4 py-3">
                    <div className="font-medium">{cn.patientName}</div>
                    <div className="text-xs text-gray-500">{cn.patientMrn}</div>
                  </td>
                  <td className="px-4 py-3">
                    <Link to={`/billing/${cn.billId}`} className="font-mono text-blue-600 hover:underline">
                      {cn.billNumber}
                    </Link>
                  </td>
                  <td className="px-4 py-3 text-gray-700 max-w-xs truncate">{cn.reason}</td>
                  <td className="px-4 py-3">
                    <span className={`px-2 py-0.5 rounded-full text-xs font-medium ${STATUS_COLORS[cn.status] ?? 'bg-gray-100 text-gray-700'}`}>
                      {CREDIT_NOTE_STATUS_LABELS[cn.status] ?? cn.status}
                    </span>
                  </td>
                  <td className="px-4 py-3 text-right font-medium">{fmt(cn.amount)}</td>
                  <td className="px-4 py-3 text-gray-600">{fmtDate(cn.appliedAt)}</td>
                  <td className="px-4 py-3 text-gray-600">{fmtDate(cn.createdAt)}</td>
                </tr>
              ))}
            </tbody>
          </table>
        )}
      </div>
    </div>
  );
}
