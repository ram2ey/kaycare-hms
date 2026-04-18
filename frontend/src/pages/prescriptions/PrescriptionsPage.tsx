import { useState, useEffect } from 'react';
import { Link } from 'react-router-dom';
import { getPending, getPatientPrescriptions } from '../../api/prescriptions';
import { searchPatients } from '../../api/patients';
import type { PrescriptionResponse } from '../../types/prescriptions';
import type { PatientResponse } from '../../types/patients';
import { STATUS_COLORS } from '../../types/prescriptions';
import { useAuth } from '../../contexts/AuthContext';
import { Roles } from '../../types';

const PHARMACIST_ROLES = [Roles.Pharmacist, Roles.Admin, Roles.SuperAdmin];

export default function PrescriptionsPage() {
  const { user } = useAuth();
  const isPharmacist = user && PHARMACIST_ROLES.includes(user.role as never);

  const [tab, setTab] = useState<'queue' | 'patient'>(isPharmacist ? 'queue' : 'patient');
  const [pending, setPending] = useState<PrescriptionResponse[]>([]);
  const [patientRx, setPatientRx] = useState<PrescriptionResponse[]>([]);
  const [patientQuery, setPatientQuery] = useState('');
  const [patientResults, setPatientResults] = useState<PatientResponse[]>([]);
  const [selectedPatient, setSelectedPatient] = useState<PatientResponse | null>(null);
  const [searching, setSearching] = useState(false);
  const [loading, setLoading] = useState(false);

  useEffect(() => {
    if (tab === 'queue' && isPharmacist) {
      setLoading(true);
      getPending().then(setPending).catch(() => {}).finally(() => setLoading(false));
    }
  }, [tab, isPharmacist]);

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
      const data = await getPatientPrescriptions(p.patientId);
      setPatientRx(data);
    } finally {
      setLoading(false);
    }
  }

  const displayList = tab === 'queue' ? pending : patientRx;

  return (
    <div className="p-6">
      <div className="flex items-center justify-between mb-6">
        <h2 className="text-2xl font-semibold text-gray-800">Prescriptions</h2>
      </div>

      {/* Tabs */}
      <div className="flex rounded-lg border border-gray-200 overflow-hidden text-sm mb-5 w-fit">
        {isPharmacist && (
          <button
            onClick={() => setTab('queue')}
            className={`px-5 py-2 ${tab === 'queue' ? 'bg-blue-700 text-white' : 'bg-white text-gray-600 hover:bg-gray-50'}`}
          >
            Dispensing Queue
          </button>
        )}
        <button
          onClick={() => setTab('patient')}
          className={`px-5 py-2 ${tab === 'patient' ? 'bg-blue-700 text-white' : 'bg-white text-gray-600 hover:bg-gray-50'}`}
        >
          By Patient
        </button>
      </div>

      {/* Patient search (patient tab) */}
      {tab === 'patient' && (
        <div className="bg-white rounded-xl border border-gray-200 p-5 mb-5">
          {selectedPatient ? (
            <div className="flex items-center justify-between bg-blue-50 rounded-lg px-4 py-3">
              <div>
                <p className="font-medium text-gray-800">{selectedPatient.fullName}</p>
                <p className="text-xs text-blue-600 font-mono">{selectedPatient.medicalRecordNumber}</p>
              </div>
              <button
                onClick={() => { setSelectedPatient(null); setPatientRx([]); }}
                className="text-xs text-gray-500 hover:text-red-500"
              >
                Change
              </button>
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
      )}

      {/* Results */}
      {(tab === 'queue' || selectedPatient) && (
        <div className="bg-white rounded-xl border border-gray-200 overflow-hidden">
          <table className="w-full text-sm">
            <thead className="bg-gray-50 border-b border-gray-200">
              <tr>
                <th className="text-left px-5 py-3 font-medium text-gray-600">Date</th>
                <th className="text-left px-5 py-3 font-medium text-gray-600">Patient</th>
                <th className="text-left px-5 py-3 font-medium text-gray-600">Prescribed By</th>
                <th className="text-left px-5 py-3 font-medium text-gray-600">Items</th>
                <th className="text-left px-5 py-3 font-medium text-gray-600">Status</th>
              </tr>
            </thead>
            <tbody className="divide-y divide-gray-100">
              {loading && (
                <tr><td colSpan={5} className="px-5 py-8 text-center text-gray-400">Loading…</td></tr>
              )}
              {!loading && displayList.length === 0 && (
                <tr><td colSpan={5} className="px-5 py-8 text-center text-gray-400">
                  {tab === 'queue' ? 'No pending prescriptions.' : 'No prescriptions found.'}
                </td></tr>
              )}
              {!loading && displayList.map((rx) => (
                <tr key={rx.prescriptionId} className="hover:bg-gray-50 transition-colors">
                  <td className="px-5 py-3 text-gray-600">{rx.prescriptionDate}</td>
                  <td className="px-5 py-3">
                    <Link to={`/patients/${rx.patientId}`} className="font-medium text-gray-800 hover:text-blue-700">
                      {rx.patientName}
                    </Link>
                    <div className="text-xs text-gray-400 font-mono">{rx.medicalRecordNumber}</div>
                  </td>
                  <td className="px-5 py-3 text-gray-600">{rx.prescribedByName}</td>
                  <td className="px-5 py-3 text-gray-600">
                    {rx.itemCount} item{rx.itemCount !== 1 ? 's' : ''}
                    {rx.hasControlledSubstances && (
                      <span className="ml-2 text-xs bg-red-100 text-red-700 px-1.5 py-0.5 rounded font-medium">CS</span>
                    )}
                  </td>
                  <td className="px-5 py-3">
                    <Link
                      to={`/prescriptions/${rx.prescriptionId}`}
                      className={`inline-block text-xs font-medium px-2 py-0.5 rounded-full hover:opacity-80 ${STATUS_COLORS[rx.status] ?? 'bg-gray-100 text-gray-600'}`}
                    >
                      {rx.status}
                    </Link>
                  </td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>
      )}

      {tab === 'patient' && !selectedPatient && (
        <div className="text-center py-16 text-gray-400 text-sm">
          Search for a patient to view their prescription history.
        </div>
      )}
    </div>
  );
}
