import { useState, useEffect } from 'react';
import { Link } from 'react-router-dom';
import { getClaims } from '../../api/claims';
import { getPayers } from '../../api/payers';
import type { InsuranceClaimResponse } from '../../types/claims';
import type { PayerResponse } from '../../types/payers';
import { CLAIM_STATUS_LABELS } from '../../types/claims';

const STATUS_COLORS: Record<string, string> = {
  Draft:             'bg-gray-100 text-gray-700',
  Submitted:         'bg-blue-100 text-blue-700',
  Approved:          'bg-green-100 text-green-700',
  PartiallyApproved: 'bg-yellow-100 text-yellow-700',
  Rejected:          'bg-red-100 text-red-700',
  Cancelled:         'bg-gray-100 text-gray-500',
};

function fmt(n: number) {
  return `GHS ${n.toFixed(2)}`;
}

function fmtDate(s: string | null) {
  if (!s) return '—';
  return new Date(s).toLocaleDateString('en-GB', { day: '2-digit', month: 'short', year: 'numeric' });
}

export default function InsuranceClaimsPage() {
  const [claims, setClaims]   = useState<InsuranceClaimResponse[]>([]);
  const [payers, setPayers]   = useState<PayerResponse[]>([]);
  const [loading, setLoading] = useState(true);
  const [status, setStatus]   = useState('');
  const [payerId, setPayerId] = useState('');

  useEffect(() => {
    getPayers(false).then(setPayers).catch(() => {});
  }, []);

  useEffect(() => {
    setLoading(true);
    getClaims({
      status:  status  || undefined,
      payerId: payerId || undefined,
    })
      .then(setClaims)
      .catch(() => {})
      .finally(() => setLoading(false));
  }, [status, payerId]);

  return (
    <div className="max-w-7xl mx-auto p-6">
      <div className="flex items-center justify-between mb-6">
        <h1 className="text-2xl font-bold text-gray-900">Insurance Claims</h1>
      </div>

      {/* Filters */}
      <div className="bg-white border border-gray-200 rounded-lg p-4 mb-6 flex flex-wrap gap-4">
        <div>
          <label className="block text-xs text-gray-500 mb-1">Status</label>
          <select
            value={status}
            onChange={(e) => setStatus(e.target.value)}
            className="border border-gray-300 rounded px-3 py-1.5 text-sm focus:outline-none focus:ring-2 focus:ring-blue-500"
          >
            <option value="">All Statuses</option>
            {Object.entries(CLAIM_STATUS_LABELS).map(([val, label]) => (
              <option key={val} value={val}>{label}</option>
            ))}
          </select>
        </div>
        <div>
          <label className="block text-xs text-gray-500 mb-1">Payer</label>
          <select
            value={payerId}
            onChange={(e) => setPayerId(e.target.value)}
            className="border border-gray-300 rounded px-3 py-1.5 text-sm focus:outline-none focus:ring-2 focus:ring-blue-500"
          >
            <option value="">All Payers</option>
            {payers.map((p) => (
              <option key={p.payerId} value={p.payerId}>{p.name}</option>
            ))}
          </select>
        </div>
      </div>

      {/* Table */}
      <div className="bg-white border border-gray-200 rounded-lg overflow-hidden">
        {loading ? (
          <div className="p-8 text-center text-gray-500">Loading…</div>
        ) : claims.length === 0 ? (
          <div className="p-8 text-center text-gray-500">No claims found.</div>
        ) : (
          <table className="w-full text-sm">
            <thead className="bg-gray-50 border-b border-gray-200">
              <tr>
                <th className="px-4 py-3 text-left font-semibold text-gray-700">Claim #</th>
                <th className="px-4 py-3 text-left font-semibold text-gray-700">Patient</th>
                <th className="px-4 py-3 text-left font-semibold text-gray-700">Payer</th>
                <th className="px-4 py-3 text-left font-semibold text-gray-700">Invoice</th>
                <th className="px-4 py-3 text-left font-semibold text-gray-700">Status</th>
                <th className="px-4 py-3 text-right font-semibold text-gray-700">Claimed</th>
                <th className="px-4 py-3 text-right font-semibold text-gray-700">Approved</th>
                <th className="px-4 py-3 text-left font-semibold text-gray-700">Submitted</th>
                <th className="px-4 py-3 text-left font-semibold text-gray-700">Created</th>
              </tr>
            </thead>
            <tbody className="divide-y divide-gray-100">
              {claims.map((c) => (
                <tr key={c.claimId} className="hover:bg-gray-50">
                  <td className="px-4 py-3 font-mono font-medium">
                    <Link to={`/billing/claims/${c.claimId}`} className="text-blue-600 hover:underline">
                      {c.claimNumber}
                    </Link>
                  </td>
                  <td className="px-4 py-3">
                    <div className="font-medium">{c.patientName}</div>
                    <div className="text-xs text-gray-500">{c.patientMrn}</div>
                    {c.nhisNumber && (
                      <div className="text-xs text-blue-600">NHIS: {c.nhisNumber}</div>
                    )}
                  </td>
                  <td className="px-4 py-3">
                    <div>{c.payerName}</div>
                    <div className="text-xs text-gray-500">{c.payerType}</div>
                  </td>
                  <td className="px-4 py-3">
                    <Link to={`/billing/${c.billId}`} className="text-blue-600 hover:underline font-mono">
                      {c.billNumber}
                    </Link>
                  </td>
                  <td className="px-4 py-3">
                    <span className={`px-2 py-0.5 rounded-full text-xs font-medium ${STATUS_COLORS[c.status] ?? 'bg-gray-100 text-gray-700'}`}>
                      {CLAIM_STATUS_LABELS[c.status] ?? c.status}
                    </span>
                  </td>
                  <td className="px-4 py-3 text-right font-medium">{fmt(c.claimAmount)}</td>
                  <td className="px-4 py-3 text-right">
                    {c.approvedAmount != null ? (
                      <span className="text-green-700 font-medium">{fmt(c.approvedAmount)}</span>
                    ) : '—'}
                  </td>
                  <td className="px-4 py-3 text-gray-600">{fmtDate(c.submittedAt)}</td>
                  <td className="px-4 py-3 text-gray-600">{fmtDate(c.createdAt)}</td>
                </tr>
              ))}
            </tbody>
          </table>
        )}
      </div>
    </div>
  );
}
