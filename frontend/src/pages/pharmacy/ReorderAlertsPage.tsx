import { useState, useEffect } from 'react';
import { Link } from 'react-router-dom';
import { getReorderAlerts } from '../../api/pharmacy';
import type { ReorderAlertResponse } from '../../types/pharmacy';

export default function ReorderAlertsPage() {
  const [alerts, setAlerts]   = useState<ReorderAlertResponse[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError]     = useState('');

  useEffect(() => {
    setLoading(true);
    setError('');
    getReorderAlerts()
      .then(setAlerts)
      .catch(() => setError('Failed to load reorder alerts.'))
      .finally(() => setLoading(false));
  }, []);

  const grouped = alerts.reduce<Record<string, ReorderAlertResponse[]>>((acc, a) => {
    const key = a.category ?? 'Uncategorised';
    (acc[key] ??= []).push(a);
    return acc;
  }, {});

  return (
    <div className="p-6 max-w-4xl">
      <div className="flex items-center justify-between mb-6">
        <div>
          <h2 className="text-2xl font-semibold text-gray-800">Reorder Alerts</h2>
          <p className="text-sm text-gray-500 mt-1">Drugs at or below their reorder threshold.</p>
        </div>
        <Link to="/pharmacy/drugs"
          className="px-4 py-2 text-sm text-blue-700 border border-blue-200 rounded-lg hover:bg-blue-50 transition-colors">
          ← Back to Inventory
        </Link>
      </div>

      {loading ? (
        <div className="text-gray-400 py-8">Loading…</div>
      ) : error ? (
        <div className="text-red-600 py-4">{error}</div>
      ) : alerts.length === 0 ? (
        <div className="bg-white rounded-xl border border-green-200 px-6 py-12 text-center">
          <p className="text-green-700 font-medium">All drugs are above their reorder thresholds.</p>
          <p className="text-sm text-gray-400 mt-1">No action required right now.</p>
        </div>
      ) : (
        <>
          <div className="mb-4 bg-red-50 border border-red-200 rounded-xl px-4 py-3 flex items-center gap-2">
            <span className="text-red-600 font-semibold">{alerts.length}</span>
            <span className="text-red-700 text-sm">drug{alerts.length !== 1 ? 's' : ''} need{alerts.length === 1 ? 's' : ''} to be reordered.</span>
          </div>
          <div className="space-y-6">
            {Object.entries(grouped).sort(([a], [b]) => a.localeCompare(b)).map(([category, group]) => (
              <div key={category}>
                <h3 className="text-xs font-semibold text-gray-400 uppercase tracking-wider mb-2">{category}</h3>
                <div className="bg-white rounded-xl border border-gray-200 overflow-hidden">
                  <table className="w-full text-sm">
                    <thead className="border-b border-gray-100 bg-gray-50">
                      <tr>
                        <th className="text-left px-5 py-3 font-medium text-gray-500 text-xs uppercase">Drug</th>
                        <th className="text-center px-5 py-3 font-medium text-gray-500 text-xs uppercase">Current Stock</th>
                        <th className="text-center px-5 py-3 font-medium text-gray-500 text-xs uppercase">Threshold</th>
                        <th className="text-center px-5 py-3 font-medium text-gray-500 text-xs uppercase">Deficit</th>
                        <th className="px-5 py-3"></th>
                      </tr>
                    </thead>
                    <tbody className="divide-y divide-gray-50">
                      {group.map(a => (
                        <tr key={a.drugInventoryId}>
                          <td className="px-5 py-3">
                            <p className="font-medium text-gray-800">{a.name}</p>
                            {a.genericName && <p className="text-xs text-gray-400">{a.genericName}</p>}
                            {(a.dosageForm || a.strength) && (
                              <p className="text-xs text-gray-400">
                                {[a.dosageForm, a.strength].filter(Boolean).join(' · ')}
                              </p>
                            )}
                          </td>
                          <td className="px-5 py-3 text-center">
                            <span className={`font-mono font-semibold ${a.currentStock === 0 ? 'text-red-700' : 'text-orange-600'}`}>
                              {a.currentStock}
                            </span>
                          </td>
                          <td className="px-5 py-3 text-center font-mono text-gray-500">{a.reorderThreshold}</td>
                          <td className="px-5 py-3 text-center">
                            <span className="font-mono font-semibold text-red-600">-{a.deficit}</span>
                          </td>
                          <td className="px-5 py-3 text-right">
                            <Link to="/pharmacy/drugs" className="text-xs text-blue-600 hover:underline">
                              Adjust stock →
                            </Link>
                          </td>
                        </tr>
                      ))}
                    </tbody>
                  </table>
                </div>
              </div>
            ))}
          </div>
        </>
      )}
    </div>
  );
}
