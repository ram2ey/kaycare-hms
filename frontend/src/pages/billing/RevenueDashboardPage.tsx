import { useState, useEffect } from 'react';
import { Link } from 'react-router-dom';
import { getRevenueDashboard } from '../../api/billingReports';
import type { RevenueDashboardResponse } from '../../types/billingReports';
import { STATUS_COLORS } from '../../types/billing';

function fmt(n: number) { return `GHS ${n.toFixed(2)}`; }
function pct(part: number, total: number) {
  if (total === 0) return '0%';
  return `${((part / total) * 100).toFixed(1)}%`;
}

export default function RevenueDashboardPage() {
  const [data, setData] = useState<RevenueDashboardResponse | null>(null);
  const [loading, setLoading] = useState(true);

  useEffect(() => {
    getRevenueDashboard()
      .then(setData)
      .catch(() => {})
      .finally(() => setLoading(false));
  }, []);

  if (loading) return <div className="p-8 text-gray-400 text-sm">Loading…</div>;
  if (!data)   return <div className="p-8 text-red-500 text-sm">Failed to load dashboard.</div>;

  const collectionRate = pct(data.totalCollected, data.totalInvoiced);
  const maxBar = Math.max(...data.monthlyRevenue.map(m => Math.max(m.invoiced, m.collected)), 1);

  return (
    <div className="p-6 max-w-6xl">
      <div className="text-sm text-gray-500 mb-4">
        <Link to="/billing" className="hover:text-blue-600">Billing</Link>
        <span className="mx-2">/</span>
        <span className="text-gray-800">Revenue Dashboard</span>
      </div>

      <h2 className="text-2xl font-semibold text-gray-800 mb-6">Revenue Dashboard</h2>

      {/* Headline metrics */}
      <div className="grid grid-cols-3 gap-4 mb-6">
        <MetricCard label="Total Invoiced"    value={fmt(data.totalInvoiced)}    sub={`${data.totalBills} bills`}           color="text-gray-800" />
        <MetricCard label="Total Collected"   value={fmt(data.totalCollected)}   sub={`Collection rate: ${collectionRate}`} color="text-green-700" />
        <MetricCard label="Outstanding"       value={fmt(data.totalOutstanding)} sub={`${data.outstandingBills} bills`}     color="text-red-600" />
        <MetricCard label="Overdue (>30 days)" value={`${data.overdueBills} bills`} sub="Issued/PartiallyPaid"             color={data.overdueBills > 0 ? 'text-orange-600' : 'text-gray-400'} />
        <MetricCard label="Total Discounts"   value={fmt(data.totalDiscounts)}   sub="Waivers applied"                     color="text-yellow-700" />
        <MetricCard label="Written Off"       value={fmt(data.totalWrittenOff)}  sub="Bad debt"                            color="text-purple-600" />
      </div>

      {/* Monthly revenue chart */}
      <section className="bg-white rounded-xl border border-gray-200 p-5 mb-5">
        <h3 className="text-sm font-semibold text-gray-700 uppercase tracking-wide mb-4">Monthly Revenue — Last 6 Months</h3>
        <div className="flex items-end gap-4 h-48">
          {data.monthlyRevenue.map((m) => (
            <div key={m.month} className="flex-1 flex flex-col items-center gap-1">
              <div className="w-full flex gap-1 items-end" style={{ height: '160px' }}>
                {/* Invoiced bar */}
                <div className="flex-1 relative group">
                  <div
                    className="w-full bg-blue-200 rounded-t-sm transition-all"
                    style={{ height: `${(m.invoiced / maxBar) * 160}px` }}
                  />
                  <div className="absolute bottom-full left-1/2 -translate-x-1/2 mb-1 hidden group-hover:block bg-gray-800 text-white text-xs rounded px-2 py-1 whitespace-nowrap z-10">
                    Invoiced: {fmt(m.invoiced)}
                  </div>
                </div>
                {/* Collected bar */}
                <div className="flex-1 relative group">
                  <div
                    className="w-full bg-green-400 rounded-t-sm transition-all"
                    style={{ height: `${(m.collected / maxBar) * 160}px` }}
                  />
                  <div className="absolute bottom-full left-1/2 -translate-x-1/2 mb-1 hidden group-hover:block bg-gray-800 text-white text-xs rounded px-2 py-1 whitespace-nowrap z-10">
                    Collected: {fmt(m.collected)}
                  </div>
                </div>
              </div>
              <p className="text-xs text-gray-500 text-center">{m.month}</p>
            </div>
          ))}
        </div>
        <div className="flex gap-4 mt-3 text-xs text-gray-500">
          <span className="flex items-center gap-1.5"><span className="w-3 h-3 rounded-sm bg-blue-200 inline-block" /> Invoiced</span>
          <span className="flex items-center gap-1.5"><span className="w-3 h-3 rounded-sm bg-green-400 inline-block" /> Collected</span>
        </div>
      </section>

      <div className="grid grid-cols-2 gap-5">
        {/* By Payer */}
        <section className="bg-white rounded-xl border border-gray-200 overflow-hidden">
          <div className="px-5 py-3 border-b border-gray-100 bg-gray-50">
            <h3 className="text-sm font-semibold text-gray-700">Revenue by Payer</h3>
          </div>
          {data.byPayer.length === 0 ? (
            <p className="px-5 py-6 text-sm text-gray-400">No data.</p>
          ) : (
            <table className="w-full text-sm">
              <thead className="border-b border-gray-100">
                <tr>
                  <th className="text-left px-5 py-2.5 font-medium text-gray-500 text-xs uppercase">Payer</th>
                  <th className="text-right px-5 py-2.5 font-medium text-gray-500 text-xs uppercase">Bills</th>
                  <th className="text-right px-5 py-2.5 font-medium text-gray-500 text-xs uppercase">Invoiced</th>
                  <th className="text-right px-5 py-2.5 font-medium text-gray-500 text-xs uppercase">Collected</th>
                  <th className="text-right px-5 py-2.5 font-medium text-gray-500 text-xs uppercase">Outstanding</th>
                </tr>
              </thead>
              <tbody className="divide-y divide-gray-50">
                {data.byPayer.map((p) => (
                  <tr key={p.payerName} className="hover:bg-gray-50">
                    <td className="px-5 py-3 font-medium text-gray-800">{p.payerName}</td>
                    <td className="px-5 py-3 text-right text-gray-500">{p.billCount}</td>
                    <td className="px-5 py-3 text-right text-gray-700">{fmt(p.invoiced)}</td>
                    <td className="px-5 py-3 text-right text-green-700">{fmt(p.collected)}</td>
                    <td className="px-5 py-3 text-right font-medium text-red-500">{fmt(p.outstanding)}</td>
                  </tr>
                ))}
              </tbody>
            </table>
          )}
        </section>

        {/* By Status */}
        <section className="bg-white rounded-xl border border-gray-200 overflow-hidden">
          <div className="px-5 py-3 border-b border-gray-100 bg-gray-50">
            <h3 className="text-sm font-semibold text-gray-700">Bills by Status</h3>
          </div>
          {data.byStatus.length === 0 ? (
            <p className="px-5 py-6 text-sm text-gray-400">No data.</p>
          ) : (
            <table className="w-full text-sm">
              <thead className="border-b border-gray-100">
                <tr>
                  <th className="text-left px-5 py-2.5 font-medium text-gray-500 text-xs uppercase">Status</th>
                  <th className="text-right px-5 py-2.5 font-medium text-gray-500 text-xs uppercase">Count</th>
                  <th className="text-right px-5 py-2.5 font-medium text-gray-500 text-xs uppercase">Total Value</th>
                  <th className="text-right px-5 py-2.5 font-medium text-gray-500 text-xs uppercase">% of Bills</th>
                </tr>
              </thead>
              <tbody className="divide-y divide-gray-50">
                {data.byStatus.map((s) => (
                  <tr key={s.status} className="hover:bg-gray-50">
                    <td className="px-5 py-3">
                      <span className={`text-xs font-medium px-2 py-0.5 rounded-full ${STATUS_COLORS[s.status] ?? 'bg-gray-100 text-gray-600'}`}>
                        {s.status}
                      </span>
                    </td>
                    <td className="px-5 py-3 text-right text-gray-700 font-medium">{s.count}</td>
                    <td className="px-5 py-3 text-right text-gray-600">{fmt(s.total)}</td>
                    <td className="px-5 py-3 text-right text-gray-500">{pct(s.count, data.totalBills)}</td>
                  </tr>
                ))}
              </tbody>
            </table>
          )}
        </section>
      </div>
    </div>
  );
}

function MetricCard({ label, value, sub, color }: { label: string; value: string; sub: string; color: string }) {
  return (
    <div className="bg-white rounded-xl border border-gray-200 p-4">
      <p className="text-xs text-gray-500 uppercase font-semibold tracking-wide mb-1">{label}</p>
      <p className={`text-2xl font-bold ${color}`}>{value}</p>
      <p className="text-xs text-gray-400 mt-1">{sub}</p>
    </div>
  );
}
