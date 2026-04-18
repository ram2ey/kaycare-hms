import { useState, useEffect, useCallback } from 'react';
import { queryAuditLogs } from '../../api/audit';
import { searchPatients } from '../../api/patients';
import type { AuditLogResponse, AuditLogQueryRequest } from '../../types/audit';
import type { PagedResult } from '../../types';
import type { PatientResponse } from '../../types/patients';
import { AUDIT_ACTIONS } from '../../types/audit';
import { Link } from 'react-router-dom';

export default function AuditLogsPage() {
  const [filters, setFilters] = useState<AuditLogQueryRequest>({ page: 1, pageSize: 50 });
  const [result, setResult] = useState<PagedResult<AuditLogResponse> | null>(null);
  const [loading, setLoading] = useState(false);

  // Patient picker for patientId filter
  const [patientQuery, setPatientQuery] = useState('');
  const [patientResults, setPatientResults] = useState<PatientResponse[]>([]);
  const [selectedPatient, setSelectedPatient] = useState<PatientResponse | null>(null);
  const [searching, setSearching] = useState(false);

  const load = useCallback(async (f: AuditLogQueryRequest) => {
    setLoading(true);
    try {
      setResult(await queryAuditLogs(f));
    } catch {
      setResult(null);
    } finally {
      setLoading(false);
    }
  }, []);

  useEffect(() => { load(filters); }, [load, filters]);

  function setFilter(field: keyof AuditLogQueryRequest, value: unknown) {
    setFilters((f) => ({ ...f, [field]: value || undefined, page: 1 }));
  }

  async function searchForPatient() {
    if (!patientQuery.trim()) return;
    setSearching(true);
    try {
      const res = await searchPatients({ query: patientQuery, pageSize: 5 });
      setPatientResults(res.items);
    } finally {
      setSearching(false);
    }
  }

  function selectPatient(p: PatientResponse) {
    setSelectedPatient(p);
    setPatientResults([]);
    setPatientQuery('');
    setFilter('patientId', p.patientId);
  }

  function clearPatient() {
    setSelectedPatient(null);
    setFilter('patientId', undefined);
  }

  const totalPages = result ? Math.ceil(result.totalCount / (filters.pageSize ?? 50)) : 1;

  return (
    <div className="p-6">
      <div className="flex items-center justify-between mb-6">
        <div>
          <h2 className="text-2xl font-semibold text-gray-800">Audit Logs</h2>
          {result && (
            <p className="text-sm text-gray-500 mt-0.5">{result.totalCount.toLocaleString()} total entries</p>
          )}
        </div>
      </div>

      {/* Filters */}
      <div className="bg-white rounded-xl border border-gray-200 p-5 mb-5">
        <h3 className="text-sm font-semibold text-gray-700 uppercase tracking-wide mb-4">Filters</h3>
        <div className="grid grid-cols-3 gap-4">
          {/* Patient filter */}
          <div>
            <label className="block text-xs text-gray-600 mb-1">Patient</label>
            {selectedPatient ? (
              <div className="flex items-center gap-2 px-3 py-2 bg-blue-50 border border-blue-200 rounded-lg">
                <span className="text-sm text-gray-800 flex-1 truncate">{selectedPatient.fullName}</span>
                <button onClick={clearPatient} className="text-gray-400 hover:text-red-500 text-xs">×</button>
              </div>
            ) : (
              <div className="relative">
                <div className="flex gap-1">
                  <input type="text" value={patientQuery}
                    onChange={(e) => setPatientQuery(e.target.value)}
                    onKeyDown={(e) => e.key === 'Enter' && (e.preventDefault(), searchForPatient())}
                    placeholder="Search patient…"
                    className="flex-1 px-3 py-2 border border-gray-300 rounded-lg text-sm focus:outline-none focus:ring-2 focus:ring-blue-500"
                  />
                  <button onClick={searchForPatient} disabled={searching}
                    className="px-2 py-2 bg-gray-100 hover:bg-gray-200 text-sm rounded-lg text-gray-700">
                    {searching ? '…' : '↵'}
                  </button>
                </div>
                {patientResults.length > 0 && (
                  <div className="absolute top-full mt-1 w-full bg-white border border-gray-200 rounded-lg shadow-lg z-10">
                    {patientResults.map((p) => (
                      <button key={p.patientId} onClick={() => selectPatient(p)}
                        className="w-full text-left px-3 py-2 hover:bg-gray-50 text-sm border-b border-gray-100 last:border-0">
                        <span className="font-medium">{p.fullName}</span>
                        <span className="text-gray-400 font-mono text-xs ml-2">{p.medicalRecordNumber}</span>
                      </button>
                    ))}
                  </div>
                )}
              </div>
            )}
          </div>

          {/* Action filter */}
          <div>
            <label className="block text-xs text-gray-600 mb-1">Action</label>
            <select value={filters.action ?? ''}
              onChange={(e) => setFilter('action', e.target.value)}
              className="w-full px-3 py-2 border border-gray-300 rounded-lg text-sm focus:outline-none focus:ring-2 focus:ring-blue-500">
              <option value="">All Actions</option>
              {AUDIT_ACTIONS.map((a) => <option key={a}>{a}</option>)}
            </select>
          </div>

          {/* Date range */}
          <div className="grid grid-cols-2 gap-2">
            <div>
              <label className="block text-xs text-gray-600 mb-1">From</label>
              <input type="datetime-local" value={filters.from ?? ''}
                onChange={(e) => setFilter('from', e.target.value)}
                className="w-full px-3 py-2 border border-gray-300 rounded-lg text-sm focus:outline-none focus:ring-2 focus:ring-blue-500"
              />
            </div>
            <div>
              <label className="block text-xs text-gray-600 mb-1">To</label>
              <input type="datetime-local" value={filters.to ?? ''}
                onChange={(e) => setFilter('to', e.target.value)}
                className="w-full px-3 py-2 border border-gray-300 rounded-lg text-sm focus:outline-none focus:ring-2 focus:ring-blue-500"
              />
            </div>
          </div>
        </div>
      </div>

      {/* Table */}
      <div className="bg-white rounded-xl border border-gray-200 overflow-hidden">
        <table className="w-full text-sm">
          <thead className="bg-gray-50 border-b border-gray-200">
            <tr>
              <th className="text-left px-5 py-3 font-medium text-gray-600">Timestamp</th>
              <th className="text-left px-5 py-3 font-medium text-gray-600">User</th>
              <th className="text-left px-5 py-3 font-medium text-gray-600">Action</th>
              <th className="text-left px-5 py-3 font-medium text-gray-600">Entity</th>
              <th className="text-left px-5 py-3 font-medium text-gray-600">Patient</th>
              <th className="text-left px-5 py-3 font-medium text-gray-600">Details</th>
              <th className="text-left px-5 py-3 font-medium text-gray-600">IP</th>
            </tr>
          </thead>
          <tbody className="divide-y divide-gray-100">
            {loading && (
              <tr><td colSpan={7} className="px-5 py-8 text-center text-gray-400">Loading…</td></tr>
            )}
            {!loading && result?.items.length === 0 && (
              <tr><td colSpan={7} className="px-5 py-8 text-center text-gray-400">No audit log entries found.</td></tr>
            )}
            {!loading && result?.items.map((log) => (
              <tr key={log.auditLogId} className="hover:bg-gray-50 transition-colors">
                <td className="px-5 py-2.5 text-gray-600 text-xs whitespace-nowrap">
                  {new Date(log.timestamp).toLocaleString('en-GB', {
                    day: '2-digit', month: 'short', year: 'numeric',
                    hour: '2-digit', minute: '2-digit', second: '2-digit',
                  })}
                </td>
                <td className="px-5 py-2.5 text-gray-700 text-xs">{log.userEmail}</td>
                <td className="px-5 py-2.5">
                  <span className="text-xs font-medium font-mono bg-gray-100 text-gray-700 px-2 py-0.5 rounded">
                    {log.action}
                  </span>
                </td>
                <td className="px-5 py-2.5 text-xs text-gray-500">
                  <span className="text-gray-600">{log.entityType}</span>
                  <span className="block font-mono text-gray-400">{log.entityId.slice(0, 8)}…</span>
                </td>
                <td className="px-5 py-2.5 text-xs">
                  {log.patientId ? (
                    <Link to={`/patients/${log.patientId}`} className="text-blue-600 hover:underline font-mono">
                      {log.patientId.slice(0, 8)}…
                    </Link>
                  ) : '—'}
                </td>
                <td className="px-5 py-2.5 text-xs text-gray-500 max-w-xs truncate">
                  {log.details ?? '—'}
                </td>
                <td className="px-5 py-2.5 text-xs text-gray-400 font-mono">
                  {log.ipAddress ?? '—'}
                </td>
              </tr>
            ))}
          </tbody>
        </table>
      </div>

      {/* Pagination */}
      {totalPages > 1 && (
        <div className="flex items-center justify-between mt-4 text-sm text-gray-600">
          <span>Page {filters.page} of {totalPages} · {result?.totalCount.toLocaleString()} entries</span>
          <div className="flex gap-2">
            <button disabled={(filters.page ?? 1) <= 1}
              onClick={() => setFilters((f) => ({ ...f, page: (f.page ?? 1) - 1 }))}
              className="px-3 py-1.5 border border-gray-300 rounded-lg disabled:opacity-40 hover:bg-gray-50">
              Previous
            </button>
            <button disabled={(filters.page ?? 1) >= totalPages}
              onClick={() => setFilters((f) => ({ ...f, page: (f.page ?? 1) + 1 }))}
              className="px-3 py-1.5 border border-gray-300 rounded-lg disabled:opacity-40 hover:bg-gray-50">
              Next
            </button>
          </div>
        </div>
      )}
    </div>
  );
}
