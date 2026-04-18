import { useState } from 'react';
import { useNavigate } from 'react-router-dom';
import { getPatientConsultations } from '../../api/consultations';
import { searchPatients } from '../../api/patients';
import type { ConsultationSummaryResponse } from '../../types/consultations';
import type { PatientResponse } from '../../types/patients';

export default function ConsultationsPage() {
  const navigate = useNavigate();
  const [patientQuery, setPatientQuery] = useState('');
  const [patientResults, setPatientResults] = useState<PatientResponse[]>([]);
  const [selectedPatient, setSelectedPatient] = useState<PatientResponse | null>(null);
  const [consultations, setConsultations] = useState<ConsultationSummaryResponse[]>([]);
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
      const data = await getPatientConsultations(p.patientId);
      setConsultations(data);
    } finally {
      setLoading(false);
    }
  }

  return (
    <div className="p-6">
      <div className="flex items-center justify-between mb-6">
        <h2 className="text-2xl font-semibold text-gray-800">Consultations</h2>
      </div>

      {/* Patient search */}
      <div className="bg-white rounded-xl border border-gray-200 p-5 mb-5">
        <h3 className="text-sm font-semibold text-gray-700 uppercase tracking-wide mb-3">Select Patient</h3>
        {selectedPatient ? (
          <div className="flex items-center justify-between bg-blue-50 rounded-lg px-4 py-3">
            <div>
              <p className="font-medium text-gray-800">{selectedPatient.fullName}</p>
              <p className="text-xs text-blue-600 font-mono">{selectedPatient.medicalRecordNumber}</p>
            </div>
            <div className="flex gap-3">
              <button
                onClick={() => navigate(`/consultations/new?patientId=${selectedPatient.patientId}`)}
                className="text-xs bg-blue-700 hover:bg-blue-800 text-white font-medium px-3 py-1.5 rounded-lg transition-colors"
              >
                + New Consultation
              </button>
              <button
                onClick={() => { setSelectedPatient(null); setConsultations([]); }}
                className="text-xs text-gray-500 hover:text-red-500"
              >
                Change
              </button>
            </div>
          </div>
        ) : (
          <div className="relative">
            <div className="flex gap-2">
              <input
                type="text"
                value={patientQuery}
                onChange={(e) => setPatientQuery(e.target.value)}
                onKeyDown={(e) => e.key === 'Enter' && (e.preventDefault(), searchForPatient())}
                placeholder="Search patient by name, MRN, or phone…"
                className="flex-1 px-3 py-2 border border-gray-300 rounded-lg text-sm focus:outline-none focus:ring-2 focus:ring-blue-500"
              />
              <button
                onClick={searchForPatient}
                disabled={searching}
                className="px-4 py-2 bg-gray-100 hover:bg-gray-200 text-sm rounded-lg text-gray-700 transition-colors"
              >
                {searching ? '…' : 'Search'}
              </button>
            </div>
            {patientResults.length > 0 && (
              <div className="absolute top-full mt-1 w-full bg-white border border-gray-200 rounded-lg shadow-lg z-10">
                {patientResults.map((p) => (
                  <button
                    key={p.patientId}
                    onClick={() => selectPatient(p)}
                    className="w-full text-left px-4 py-2.5 hover:bg-gray-50 text-sm border-b border-gray-100 last:border-0"
                  >
                    <span className="font-medium">{p.fullName}</span>
                    <span className="text-gray-400 font-mono text-xs ml-2">{p.medicalRecordNumber}</span>
                  </button>
                ))}
              </div>
            )}
          </div>
        )}
      </div>

      {/* Consultation history */}
      {selectedPatient && (
        <div className="bg-white rounded-xl border border-gray-200 overflow-hidden">
          <div className="px-5 py-3 border-b border-gray-100 bg-gray-50">
            <h3 className="text-sm font-semibold text-gray-700">
              Consultation History — {consultations.length} record{consultations.length !== 1 ? 's' : ''}
            </h3>
          </div>
          {loading ? (
            <p className="px-5 py-8 text-center text-gray-400 text-sm">Loading…</p>
          ) : consultations.length === 0 ? (
            <p className="px-5 py-8 text-center text-gray-400 text-sm">No consultations found.</p>
          ) : (
            <table className="w-full text-sm">
              <thead className="border-b border-gray-100">
                <tr>
                  <th className="text-left px-5 py-3 font-medium text-gray-600">Date</th>
                  <th className="text-left px-5 py-3 font-medium text-gray-600">Doctor</th>
                  <th className="text-left px-5 py-3 font-medium text-gray-600">Primary Diagnosis</th>
                  <th className="text-left px-5 py-3 font-medium text-gray-600">Status</th>
                </tr>
              </thead>
              <tbody className="divide-y divide-gray-50">
                {consultations.map((c) => (
                  <tr key={c.consultationId} className="hover:bg-gray-50 transition-colors">
                    <td className="px-5 py-3 text-gray-600">
                      {new Date(c.createdAt).toLocaleDateString('en-GB', {
                        day: 'numeric', month: 'short', year: 'numeric',
                      })}
                    </td>
                    <td className="px-5 py-3 text-gray-700">{c.doctorName}</td>
                    <td className="px-5 py-3">
                      {c.primaryDiagnosisCode ? (
                        <span>
                          <span className="font-mono text-xs text-blue-600 mr-1.5">{c.primaryDiagnosisCode}</span>
                          <span className="text-gray-700">{c.primaryDiagnosisDesc}</span>
                        </span>
                      ) : (
                        <span className="text-gray-400">—</span>
                      )}
                    </td>
                    <td className="px-5 py-3">
                      <span className={`text-xs font-medium px-2 py-0.5 rounded-full ${
                        c.status === 'Signed'
                          ? 'bg-green-100 text-green-700'
                          : 'bg-yellow-100 text-yellow-700'
                      }`}>
                        {c.status}
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
          Search for a patient to view their consultation history.
        </div>
      )}
    </div>
  );
}
