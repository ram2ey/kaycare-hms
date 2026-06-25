import { useState, useEffect } from 'react';
import { Link } from 'react-router-dom';
import { getPatientAdmissions } from '../../../api/inpatient';
import type { AdmissionSummaryResponse } from '../../../types/inpatient';

const STATUS_COLORS: Record<string, string> = {
  Active:     'bg-green-100 text-green-700',
  Discharged: 'bg-gray-100 text-gray-600',
  Transferred:'bg-blue-100 text-blue-700',
};

export default function AdmissionsTab({ patientId }: { patientId: string }) {
  const [admissions, setAdmissions] = useState<AdmissionSummaryResponse[]>([]);
  const [loading, setLoading] = useState(true);

  useEffect(() => {
    getPatientAdmissions(patientId)
      .then((data) => setAdmissions(Array.isArray(data) ? data : []))
      .catch(() => setAdmissions([]))
      .finally(() => setLoading(false));
  }, [patientId]);

  if (loading) return <div className="text-gray-400 text-sm">Loading…</div>;

  return (
    <div className="max-w-4xl">
      <h3 className="text-sm font-semibold text-gray-700 mb-4">Admission History ({admissions.length})</h3>

      {admissions.length === 0 ? (
        <p className="text-sm text-gray-400">No admission history.</p>
      ) : (
        <div className="bg-white rounded-xl border border-gray-200 overflow-hidden">
          <table className="w-full text-sm">
            <thead className="bg-gray-50 border-b border-gray-200">
              <tr>
                <th className="text-left px-4 py-3 text-xs font-medium text-gray-500">Admitted</th>
                <th className="text-left px-4 py-3 text-xs font-medium text-gray-500">Discharged</th>
                <th className="text-left px-4 py-3 text-xs font-medium text-gray-500">Ward / Bed</th>
                <th className="text-left px-4 py-3 text-xs font-medium text-gray-500">Admitting Doctor</th>
                <th className="text-left px-4 py-3 text-xs font-medium text-gray-500">Reason</th>
                <th className="text-left px-4 py-3 text-xs font-medium text-gray-500">Status</th>
                <th />
              </tr>
            </thead>
            <tbody className="divide-y divide-gray-100">
              {admissions.map(a => (
                <tr key={a.admissionId} className="hover:bg-gray-50">
                  <td className="px-4 py-3 text-gray-600">{new Date(a.admissionDate).toLocaleDateString()}</td>
                  <td className="px-4 py-3 text-gray-500">{a.expectedDischargeDate ? new Date(a.expectedDischargeDate).toLocaleDateString() : '—'}</td>
                  <td className="px-4 py-3 text-gray-800">{a.wardName} · {a.bedNumber}</td>
                  <td className="px-4 py-3 text-gray-600">{a.admittingDoctorName}</td>
                  <td className="px-4 py-3 text-gray-500 max-w-xs truncate">{a.admissionReason}</td>
                  <td className="px-4 py-3">
                    <span className={`text-xs font-medium px-2 py-0.5 rounded-full ${STATUS_COLORS[a.status] ?? 'bg-gray-100 text-gray-600'}`}>
                      {a.status}
                    </span>
                  </td>
                  <td className="px-4 py-3 text-right">
                    <Link to={`/inpatient/admissions/${a.admissionId}`} className="text-blue-600 hover:underline text-xs">View</Link>
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
