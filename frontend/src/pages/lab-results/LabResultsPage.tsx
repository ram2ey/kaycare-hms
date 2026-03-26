import { useState } from 'react';
import { Link } from 'react-router-dom';
import { getPatientLabResults } from '../../api/labResults';
import { searchPatients } from '../../api/patients';
import type { LabResultResponse } from '../../types/labResults';
import type { PatientResponse } from '../../types/patients';

const STATUS_COLORS: Record<string, string> = {
  Received: 'bg-blue-100 text-blue-700',
  Verified: 'bg-green-100 text-green-700',
};

export default function LabResultsPage() {
  const [selectedPatient, setSelectedPatient] = useState<PatientResponse | null>(null);
  const [patientQuery, setPatientQuery] = useState('');
  const [patientResults, setPatientResults] = useState<PatientResponse[]>([]);
  const [results, setResults] = useState<LabResultResponse[]>([]);
  const [searching, setSearching] = useState(false);
  const [loading, setLoading] = useState(false);

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

  async function selectPatient(p: PatientResponse) {
    setSelectedPatient(p);
    setPatientResults([]);
    setPatientQuery('');
    setLoading(true);
    try {
      setResults(await getPatientLabResults(p.patientId));
    } finally {
      setLoading(false);
    }
  }

  return (
    <div className="p-6">
      <div className="flex items-center justify-between mb-6">
        <h2 className="text-2xl font-semibold text-gray-800">Lab Results</h2>
      </div>

      {/* Patient search */}
      <div className="bg-white rounded-xl border border-gray-200 p-5 mb-5">
        {selectedPatient ? (
          <div className="flex items-center justify-between bg-blue-50 rounded-lg px-4 py-3">
            <div>
              <p className="font-medium text-gray-800">{selectedPatient.fullName}</p>
              <p className="text-xs text-blue-600 font-mono">{selectedPatient.medicalRecordNumber}</p>
            </div>
            <button onClick={() => { setSelectedPatient(null); setResults([]); }}
              className="text-xs text-gray-500 hover:text-red-500">Change</button>
          </div>
        ) : (
          <div className="relative">
            <div className="flex gap-2">
              <input type="text" value={patientQuery}
                onChange={(e) => setPatientQuery(e.target.value)}
                onKeyDown={(e) => e.key === 'Enter' && (e.preventDefault(), searchForPatient())}
                placeholder="Search patient by name, MRN, or phone…"
                className="flex-1 px-3 py-2 border border-gray-300 rounded-lg text-sm focus:outline-none focus:ring-2 focus:ring-blue-500"
              />
              <button onClick={searchForPatient} disabled={searching}
                className="px-4 py-2 bg-gray-100 hover:bg-gray-200 text-sm rounded-lg text-gray-700 transition-colors">
                {searching ? '…' : 'Search'}
              </button>
            </div>
            {patientResults.length > 0 && (
              <div className="absolute top-full mt-1 w-full bg-white border border-gray-200 rounded-lg shadow-lg z-10">
                {patientResults.map((p) => (
                  <button key={p.patientId} onClick={() => selectPatient(p)}
                    className="w-full text-left px-4 py-2.5 hover:bg-gray-50 text-sm border-b border-gray-100 last:border-0">
                    <span className="font-medium">{p.fullName}</span>
                    <span className="text-gray-400 font-mono text-xs ml-2">{p.medicalRecordNumber}</span>
                  </button>
                ))}
              </div>
            )}
          </div>
        )}
      </div>

      {/* Results list */}
      {selectedPatient && (
        <div className="bg-white rounded-xl border border-gray-200 overflow-hidden">
          <div className="px-5 py-3 border-b border-gray-100 bg-gray-50">
            <h3 className="text-sm font-semibold text-gray-700">
              Lab Results — {results.length} result{results.length !== 1 ? 's' : ''}
            </h3>
          </div>
          {loading ? (
            <p className="px-5 py-8 text-center text-gray-400 text-sm">Loading…</p>
          ) : results.length === 0 ? (
            <p className="px-5 py-8 text-center text-gray-400 text-sm">No lab results found.</p>
          ) : (
            <table className="w-full text-sm">
              <thead className="border-b border-gray-100">
                <tr>
                  <th className="text-left px-5 py-3 font-medium text-gray-600">Accession #</th>
                  <th className="text-left px-5 py-3 font-medium text-gray-600">Test</th>
                  <th className="text-left px-5 py-3 font-medium text-gray-600">Ordered By</th>
                  <th className="text-left px-5 py-3 font-medium text-gray-600">Received</th>
                  <th className="text-left px-5 py-3 font-medium text-gray-600">Observations</th>
                  <th className="text-left px-5 py-3 font-medium text-gray-600">Status</th>
                </tr>
              </thead>
              <tbody className="divide-y divide-gray-50">
                {results.map((r) => (
                  <tr key={r.labResultId} className="hover:bg-gray-50 transition-colors">
                    <td className="px-5 py-3">
                      <Link to={`/lab-results/${r.labResultId}`} className="font-mono text-xs text-blue-700 hover:underline">
                        {r.accessionNumber}
                      </Link>
                    </td>
                    <td className="px-5 py-3">
                      <p className="font-medium text-gray-800">{r.orderName ?? '—'}</p>
                      {r.orderCode && <p className="text-xs text-gray-400 font-mono">{r.orderCode}</p>}
                    </td>
                    <td className="px-5 py-3 text-gray-600">{r.orderingDoctorName ?? '—'}</td>
                    <td className="px-5 py-3 text-gray-600">
                      {new Date(r.receivedAt).toLocaleDateString('en-GB', { day: 'numeric', month: 'short', year: 'numeric' })}
                    </td>
                    <td className="px-5 py-3 text-gray-600">{r.observationCount}</td>
                    <td className="px-5 py-3">
                      <span className={`text-xs font-medium px-2 py-0.5 rounded-full ${STATUS_COLORS[r.status] ?? 'bg-gray-100 text-gray-600'}`}>
                        {r.status}
                      </span>
                    </td>
                  </tr>
                ))}
              </tbody>
            </table>
          )}
        </div>
      )}

      {!selectedPatient && (
        <div className="text-center py-16 text-gray-400 text-sm">
          Search for a patient to view their lab results.
        </div>
      )}
    </div>
  );
}
