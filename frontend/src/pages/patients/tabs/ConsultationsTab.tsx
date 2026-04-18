import { useState, useEffect } from 'react';
import { Link } from 'react-router-dom';
import { getPatientConsultations } from '../../../api/consultations';
import type { ConsultationSummaryResponse } from '../../../types/consultations';
import { useAuth } from '../../../contexts/AuthContext';
import { Roles } from '../../../types';

const STATUS_COLORS: Record<string, string> = {
  Draft:  'bg-gray-100 text-gray-600',
  Signed: 'bg-green-100 text-green-700',
};

export default function ConsultationsTab({ patientId }: { patientId: string }) {
  const { user } = useAuth();
  const [consultations, setConsultations] = useState<ConsultationSummaryResponse[]>([]);
  const [loading, setLoading] = useState(true);

  useEffect(() => {
    getPatientConsultations(patientId)
      .then(setConsultations)
      .finally(() => setLoading(false));
  }, [patientId]);

  const canCreate = [Roles.Doctor, Roles.Admin, Roles.SuperAdmin].includes(user?.role as never);

  if (loading) return <div className="text-gray-400 text-sm">Loading…</div>;

  return (
    <div className="max-w-4xl">
      <div className="flex items-center justify-between mb-4">
        <h3 className="text-sm font-semibold text-gray-700">Consultations ({consultations.length})</h3>
        {canCreate && (
          <Link to={`/consultations/new?patientId=${patientId}`}
            className="px-3 py-1.5 bg-blue-700 text-white rounded-lg hover:bg-blue-800 text-sm font-medium">
            + New Consultation
          </Link>
        )}
      </div>

      {consultations.length === 0 ? (
        <p className="text-sm text-gray-400">No consultations on record.</p>
      ) : (
        <div className="bg-white rounded-xl border border-gray-200 overflow-hidden">
          <table className="w-full text-sm">
            <thead className="bg-gray-50 border-b border-gray-200">
              <tr>
                <th className="text-left px-4 py-3 text-xs font-medium text-gray-500">Date</th>
                <th className="text-left px-4 py-3 text-xs font-medium text-gray-500">Doctor</th>
                <th className="text-left px-4 py-3 text-xs font-medium text-gray-500">Primary Diagnosis</th>
                <th className="text-left px-4 py-3 text-xs font-medium text-gray-500">Status</th>
                <th />
              </tr>
            </thead>
            <tbody className="divide-y divide-gray-100">
              {consultations.map(c => (
                <tr key={c.consultationId} className="hover:bg-gray-50">
                  <td className="px-4 py-3 text-gray-600">{new Date(c.createdAt).toLocaleDateString()}</td>
                  <td className="px-4 py-3 text-gray-800">{c.doctorName}</td>
                  <td className="px-4 py-3 text-gray-600">
                    {c.primaryDiagnosisCode
                      ? <span>{c.primaryDiagnosisCode} — {c.primaryDiagnosisDesc}</span>
                      : <span className="text-gray-400">—</span>}
                  </td>
                  <td className="px-4 py-3">
                    <span className={`text-xs font-medium px-2 py-0.5 rounded-full ${STATUS_COLORS[c.status] ?? 'bg-gray-100 text-gray-600'}`}>
                      {c.status}
                    </span>
                  </td>
                  <td className="px-4 py-3 text-right">
                    <Link to={`/consultations/${c.consultationId}`} className="text-blue-600 hover:underline text-xs">View</Link>
                  </td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>
      )}
    </div>
  );
}
