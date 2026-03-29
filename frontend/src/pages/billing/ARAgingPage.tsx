import { useState, useEffect } from 'react';
import { Link } from 'react-router-dom';
import { getArAging } from '../../api/billingReports';
import type { ArAgingReport, ArAgingRow } from '../../types/billingReports';

function fmt(n: number) { return `GHS ${n.toFixed(2)}`; }

const BUCKET_COLORS: Record<string, string> = {
  '0-30':  'bg-green-100 text-green-700',
  '31-60': 'bg-yellow-100 text-yellow-700',
  '61-90': 'bg-orange-100 text-orange-700',
  '90+':   'bg-red-100 text-red-700',
};


export default function ARAgingPage() {
  const [report, setReport] = useState<ArAgingReport | null>(null);
  const [loading, setLoading] = useState(true);
  const [filter, setFilter] = useState<string>('All');

  useEffect(() => {
    getArAging()
      .then(setReport)
      .catch(() => {})
      .finally(() => setLoading(false));
  }, []);

  const rows: ArAgingRow[] = report
    ? (filter === 'All' ? report.rows : report.rows.filter(r => r.agingBucket === filter))
    : [];

  return (
    <div className="p-6 max-w-6xl">
      <div className="text-sm text-gray-500 mb-4">
        <Link to="/billing" className="hover:text-blue-600">Billing</Link>
        <span className="mx-2">/</span>
        <span className="text-gray-800">AR Aging Report</span>
      </div>

      <div className="flex items-center justify-between mb-6">
        <div>
          <h2 className="text-2xl font-semibold text-gray-800">Accounts Receivable Aging</h2>
          <p className="text-sm text-gray-500 mt-0.5">Outstanding balances by days since issue</p>
        </div>
      </div>

      {loading ? (
        <div className="text-gray-400 text-sm">Loading…</div>
      ) : !report ? (
        <div className="text-red-500 text-sm">Failed to load report.</div>
      ) : (
        <>
          {/* Summary buckets */}
          <div className="grid grid-cols-5 gap-4 mb-6">
            {[
              { label: '0–30 days', value: report.totalBalance0To30, bucket: '0-30', color: 'border-green-200 bg-green-50' },
              { label: '31–60 days', value: report.totalBalance31To60, bucket: '31-60', color: 'border-yellow-200 bg-yellow-50' },
              { label: '61–90 days', value: report.totalBalance61To90, bucket: '61-90', color: 'border-orange-200 bg-orange-50' },
              { label: '90+ days', value: report.totalBalance90Plus, bucket: '90+', color: 'border-red-200 bg-red-50' },
              { label: 'Grand Total', value: report.grandTotalBalance, bucket: 'All', color: 'border-gray-200 bg-white' },
            ].map(({ label, value, bucket, color }) => (
              <button
                key={bucket}
                onClick={() => setFilter(bucket)}
                className={`rounded-xl border p-4 text-left transition-all ${color} ${filter === bucket ? 'ring-2 ring-blue-400' : 'hover:ring-1 hover:ring-gray-300'}`}
              >
                <p className="text-xs text-gray-500 uppercase font-semibold tracking-wide mb-1">{label}</p>
                <p className="text-xl font-bold text-gray-800">{fmt(value)}</p>
                {bucket !== 'All' && (
                  <p className="text-xs text-gray-400 mt-0.5">
                    {report.rows.filter(r => r.agingBucket === bucket).length} bill{report.rows.filter(r => r.agingBucket === bucket).length !== 1 ? 's' : ''}
                  </p>
                )}
              </button>
            ))}
          </div>

          {/* Table */}
          <div className="bg-white rounded-xl border border-gray-200 overflow-hidden">
            <div className="px-5 py-3 border-b border-gray-100 bg-gray-50 flex items-center justify-between">
              <h3 className="text-sm font-semibold text-gray-700">
                {filter === 'All' ? 'All Outstanding Bills' : `${filter} Days — ${rows.length} bill${rows.length !== 1 ? 's' : ''}`}
              </h3>
              {filter !== 'All' && (
                <button onClick={() => setFilter('All')} className="text-xs text-blue-600 hover:underline">Show all</button>
              )}
            </div>

            {rows.length === 0 ? (
              <p className="px-5 py-8 text-sm text-gray-400 text-center">No outstanding bills in this range.</p>
            ) : (
              <table className="w-full text-sm">
                <thead className="border-b border-gray-100">
                  <tr>
                    <th className="text-left px-5 py-2.5 font-medium text-gray-500 text-xs uppercase">Invoice</th>
                    <th className="text-left px-5 py-2.5 font-medium text-gray-500 text-xs uppercase">Patient</th>
                    <th className="text-left px-5 py-2.5 font-medium text-gray-500 text-xs uppercase">Payer</th>
                    <th className="text-left px-5 py-2.5 font-medium text-gray-500 text-xs uppercase">Issued</th>
                    <th className="text-right px-5 py-2.5 font-medium text-gray-500 text-xs uppercase">Days</th>
                    <th className="text-right px-5 py-2.5 font-medium text-gray-500 text-xs uppercase">Total</th>
                    <th className="text-right px-5 py-2.5 font-medium text-gray-500 text-xs uppercase">Paid</th>
                    <th className="text-right px-5 py-2.5 font-medium text-gray-500 text-xs uppercase">Balance</th>
                    <th className="text-left px-5 py-2.5 font-medium text-gray-500 text-xs uppercase">Bucket</th>
                  </tr>
                </thead>
                <tbody className="divide-y divide-gray-50">
                  {rows.map((row) => (
                    <tr key={row.billId} className="hover:bg-gray-50">
                      <td className="px-5 py-3">
                        <Link to={`/billing/${row.billId}`} className="font-mono text-xs text-blue-700 hover:underline">
                          {row.billNumber}
                        </Link>
                      </td>
                      <td className="px-5 py-3">
                        <p className="font-medium text-gray-800">{row.patientName}</p>
                        <p className="text-xs text-gray-400 font-mono">{row.medicalRecordNumber}</p>
                      </td>
                      <td className="px-5 py-3 text-gray-500 text-sm">{row.payerName ?? 'Self-Pay'}</td>
                      <td className="px-5 py-3 text-gray-500 text-xs">
                        {new Date(row.issuedAt).toLocaleDateString('en-GB', { day: 'numeric', month: 'short', year: 'numeric' })}
                      </td>
                      <td className={`px-5 py-3 text-right font-semibold ${row.daysOutstanding > 90 ? 'text-red-600' : row.daysOutstanding > 60 ? 'text-orange-600' : row.daysOutstanding > 30 ? 'text-yellow-700' : 'text-gray-700'}`}>
                        {row.daysOutstanding}
                      </td>
                      <td className="px-5 py-3 text-right text-gray-700">{fmt(row.totalAmount)}</td>
                      <td className="px-5 py-3 text-right text-green-700">{fmt(row.paidAmount)}</td>
                      <td className="px-5 py-3 text-right font-semibold text-red-600">{fmt(row.balanceDue)}</td>
                      <td className="px-5 py-3">
                        <span className={`text-xs font-medium px-2 py-0.5 rounded-full ${BUCKET_COLORS[row.agingBucket]}`}>
                          {row.agingBucket}
                        </span>
                      </td>
                    </tr>
                  ))}
                </tbody>
                <tfoot className="border-t border-gray-200 bg-gray-50">
                  <tr>
                    <td colSpan={7} className="px-5 py-3 text-right font-semibold text-gray-700">Total Balance Due</td>
                    <td className="px-5 py-3 text-right font-bold text-red-600">
                      {fmt(rows.reduce((s, r) => s + r.balanceDue, 0))}
                    </td>
                    <td />
                  </tr>
                </tfoot>
              </table>
            )}
          </div>
        </>
      )}
    </div>
  );
}
