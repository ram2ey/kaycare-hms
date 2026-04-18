import { useState, useCallback } from 'react';
import { getCSRegister, downloadCSRegisterReport } from '../../api/pharmacy';
import type { CSRegisterDrugEntry } from '../../types/pharmacy';
import { MOVEMENT_TYPE_LABELS } from '../../types/pharmacy';

const fmt = (d: string) => new Date(d).toLocaleString('en-GB', { day: '2-digit', month: 'short', year: 'numeric', hour: '2-digit', minute: '2-digit' });

export default function CSRegisterPage() {
  const [entries, setEntries]   = useState<CSRegisterDrugEntry[]>([]);
  const [loading, setLoading]   = useState(false);
  const [error, setError]       = useState('');
  const [from, setFrom]         = useState('');
  const [to, setTo]             = useState('');
  const [searched, setSearched] = useState(false);
  const [downloading, setDownloading] = useState(false);
  const [expanded, setExpanded] = useState<Set<string>>(new Set());

  const load = useCallback(async () => {
    setLoading(true);
    setError('');
    try {
      const params: { from?: string; to?: string } = {};
      if (from) params.from = from;
      if (to)   params.to   = to;
      const data = await getCSRegister(params);
      setEntries(data);
      setSearched(true);
      setExpanded(new Set(data.map(d => d.drugInventoryId)));
    } catch {
      setError('Failed to load register.');
    } finally {
      setLoading(false);
    }
  }, [from, to]);

  const handleDownload = async () => {
    setDownloading(true);
    try {
      const params: { from?: string; to?: string } = {};
      if (from) params.from = from;
      if (to)   params.to   = to;
      await downloadCSRegisterReport(params);
    } catch {
      setError('Failed to download PDF.');
    } finally {
      setDownloading(false);
    }
  };

  const toggle = (id: string) => setExpanded(prev => {
    const next = new Set(prev);
    next.has(id) ? next.delete(id) : next.add(id);
    return next;
  });

  const totalMovements = entries.reduce((s, e) => s + e.movements.length, 0);

  return (
    <div className="p-6 max-w-7xl mx-auto">
      <div className="flex items-center justify-between mb-6">
        <div>
          <h1 className="text-2xl font-bold text-gray-900">Controlled Substance Register</h1>
          <p className="text-sm text-gray-500 mt-1">Running balance log for all controlled substances</p>
        </div>
        {searched && (
          <button
            onClick={handleDownload}
            disabled={downloading}
            className="flex items-center gap-2 px-4 py-2 bg-red-600 text-white rounded-lg hover:bg-red-700 disabled:opacity-50 text-sm font-medium"
          >
            {downloading ? 'Generating…' : '⬇ Download PDF'}
          </button>
        )}
      </div>

      {/* Filters */}
      <div className="bg-white border border-gray-200 rounded-xl p-4 mb-6 flex flex-wrap items-end gap-4">
        <div>
          <label className="block text-xs font-medium text-gray-600 mb-1">From date</label>
          <input
            type="date"
            value={from}
            onChange={e => setFrom(e.target.value)}
            className="px-3 py-2 border border-gray-300 rounded-lg text-sm focus:outline-none focus:ring-2 focus:ring-red-500"
          />
        </div>
        <div>
          <label className="block text-xs font-medium text-gray-600 mb-1">To date</label>
          <input
            type="date"
            value={to}
            onChange={e => setTo(e.target.value)}
            className="px-3 py-2 border border-gray-300 rounded-lg text-sm focus:outline-none focus:ring-2 focus:ring-red-500"
          />
        </div>
        <button
          onClick={load}
          disabled={loading}
          className="px-5 py-2 bg-red-600 text-white rounded-lg hover:bg-red-700 disabled:opacity-50 text-sm font-medium"
        >
          {loading ? 'Loading…' : 'Load Register'}
        </button>
        {(from || to) && (
          <button
            onClick={() => { setFrom(''); setTo(''); }}
            className="px-3 py-2 text-sm text-gray-500 hover:text-gray-700"
          >
            Clear
          </button>
        )}
      </div>

      {error && (
        <div className="mb-4 bg-red-50 border border-red-200 text-red-700 rounded-lg px-4 py-3 text-sm">{error}</div>
      )}

      {!searched && !loading && (
        <div className="text-center py-16 text-gray-400">
          <p className="text-4xl mb-3">🔒</p>
          <p className="text-lg font-medium">Select a date range and click Load Register</p>
          <p className="text-sm mt-1">Leave dates empty to load all records</p>
        </div>
      )}

      {searched && entries.length === 0 && (
        <div className="text-center py-16 text-gray-400">
          <p className="text-lg font-medium">No controlled substances found</p>
          <p className="text-sm mt-1">No drugs are marked as controlled substances in inventory</p>
        </div>
      )}

      {entries.length > 0 && (
        <>
          <div className="flex items-center gap-4 mb-4 text-sm text-gray-500">
            <span>{entries.length} drug{entries.length !== 1 ? 's' : ''}</span>
            <span>·</span>
            <span>{totalMovements} movement{totalMovements !== 1 ? 's' : ''}</span>
            <button onClick={() => setExpanded(new Set(entries.map(e => e.drugInventoryId)))} className="text-red-600 hover:underline ml-2">Expand all</button>
            <button onClick={() => setExpanded(new Set())} className="text-gray-500 hover:underline">Collapse all</button>
          </div>

          <div className="space-y-4">
            {entries.map(drug => (
              <div key={drug.drugInventoryId} className="bg-white border border-gray-200 rounded-xl overflow-hidden">
                {/* Drug header */}
                <button
                  onClick={() => toggle(drug.drugInventoryId)}
                  className="w-full flex items-center justify-between px-5 py-4 bg-red-50 hover:bg-red-100 transition-colors"
                >
                  <div className="flex items-center gap-3">
                    <span className="text-red-700 font-bold text-base">{drug.drugName}</span>
                    {drug.genericName && <span className="text-gray-500 text-sm">({drug.genericName})</span>}
                    {drug.dosageForm && <span className="text-xs bg-gray-100 text-gray-600 px-2 py-0.5 rounded">{drug.dosageForm}</span>}
                    {drug.strength && <span className="text-xs bg-gray-100 text-gray-600 px-2 py-0.5 rounded">{drug.strength}</span>}
                  </div>
                  <div className="flex items-center gap-4">
                    <div className="text-right">
                      <div className="text-xs text-gray-500">Current Stock</div>
                      <div className={`font-bold text-lg ${drug.currentStock === 0 ? 'text-red-600' : 'text-red-700'}`}>{drug.currentStock}</div>
                    </div>
                    <span className="text-gray-400 text-lg">{expanded.has(drug.drugInventoryId) ? '▲' : '▼'}</span>
                  </div>
                </button>

                {/* Movements table */}
                {expanded.has(drug.drugInventoryId) && (
                  drug.movements.length === 0 ? (
                    <div className="px-5 py-6 text-center text-gray-400 text-sm">No movements in this period</div>
                  ) : (
                    <div className="overflow-x-auto">
                      <table className="w-full text-sm">
                        <thead>
                          <tr className="bg-gray-50 border-b border-gray-200">
                            <th className="px-4 py-2 text-left text-xs font-semibold text-gray-500">Date</th>
                            <th className="px-4 py-2 text-left text-xs font-semibold text-gray-500">Movement</th>
                            <th className="px-4 py-2 text-center text-xs font-semibold text-green-600">In</th>
                            <th className="px-4 py-2 text-center text-xs font-semibold text-red-500">Out</th>
                            <th className="px-4 py-2 text-center text-xs font-semibold text-gray-700">Balance</th>
                            <th className="px-4 py-2 text-left text-xs font-semibold text-gray-500">Reference</th>
                            <th className="px-4 py-2 text-left text-xs font-semibold text-gray-500">Notes</th>
                            <th className="px-4 py-2 text-left text-xs font-semibold text-gray-500">Recorded By</th>
                          </tr>
                        </thead>
                        <tbody>
                          {drug.movements.map(m => (
                            <tr key={m.stockMovementId} className="border-b border-gray-100 hover:bg-gray-50">
                              <td className="px-4 py-2 text-gray-600 whitespace-nowrap">{fmt(m.date)}</td>
                              <td className="px-4 py-2 text-gray-700">{MOVEMENT_TYPE_LABELS[m.movementType] ?? m.movementType}</td>
                              <td className="px-4 py-2 text-center font-bold text-green-600">
                                {m.quantityIn != null ? `+${m.quantityIn}` : ''}
                              </td>
                              <td className="px-4 py-2 text-center font-bold text-red-500">
                                {m.quantityOut != null ? `-${m.quantityOut}` : ''}
                              </td>
                              <td className="px-4 py-2 text-center font-bold text-gray-900">{m.balance}</td>
                              <td className="px-4 py-2 text-gray-500">{m.referenceType ?? '—'}</td>
                              <td className="px-4 py-2 text-gray-500 max-w-xs truncate">{m.notes ?? '—'}</td>
                              <td className="px-4 py-2 text-gray-600">{m.recordedBy}</td>
                            </tr>
                          ))}
                        </tbody>
                      </table>
                    </div>
                  )
                )}
              </div>
            ))}
          </div>
        </>
      )}
    </div>
  );
}
