import { useState, useEffect } from 'react';
import { Link } from 'react-router-dom';
import { getPatientPrescriptions } from '../../../api/prescriptions';
import type { PrescriptionResponse } from '../../../types/prescriptions';
import { useAuth } from '../../../contexts/AuthContext';
import { Roles } from '../../../types';

const STATUS_COLORS: Record<string, string> = {
  Active:              'bg-green-100 text-green-700',
  Dispensed:           'bg-blue-100 text-blue-700',
  PartiallyDispensed:  'bg-yellow-100 text-yellow-700',
  Expired:             'bg-orange-100 text-orange-700',
  Cancelled:           'bg-gray-100 text-gray-500',
};

export default function PrescriptionsTab({ patientId }: { patientId: string }) {
  const { user } = useAuth();
  const [prescriptions, setPrescriptions] = useState<PrescriptionResponse[]>([]);
  const [loading, setLoading] = useState(true);

  useEffect(() => {
    getPatientPrescriptions(patientId).then(setPrescriptions).finally(() => setLoading(false));
  }, [patientId]);

  const canCreate = [Roles.Doctor, Roles.Admin, Roles.SuperAdmin].includes(user?.role as never);

  if (loading) return <div className="text-gray-400 text-sm">Loading…</div>;

  return (
    <div className="max-w-4xl">
      <div className="flex items-center justify-between mb-4">
        <h3 className="text-sm font-semibold text-gray-700">Prescriptions ({prescriptions.length})</h3>
        {canCreate && (
          <Link to={`/prescriptions/new?patientId=${patientId}`}
            className="px-3 py-1.5 bg-blue-700 text-white rounded-lg hover:bg-blue-800 text-sm font-medium">
            + New Prescription
          </Link>
        )}
      </div>

      {prescriptions.length === 0 ? (
        <p className="text-sm text-gray-400">No prescriptions on record.</p>
      ) : (
        <div className="bg-white rounded-xl border border-gray-200 overflow-hidden">
          <table className="w-full text-sm">
            <thead className="bg-gray-50 border-b border-gray-200">
              <tr>
                <th className="text-left px-4 py-3 text-xs font-medium text-gray-500">Date</th>
                <th className="text-left px-4 py-3 text-xs font-medium text-gray-500">Medications</th>
                <th className="text-left px-4 py-3 text-xs font-medium text-gray-500">Prescribed By</th>
                <th className="text-left px-4 py-3 text-xs font-medium text-gray-500">Expires</th>
                <th className="text-left px-4 py-3 text-xs font-medium text-gray-500">Status</th>
                <th />
              </tr>
            </thead>
            <tbody className="divide-y divide-gray-100">
              {prescriptions.map(p => (
                <tr key={p.prescriptionId} className="hover:bg-gray-50">
                  <td className="px-4 py-3 text-gray-600">{new Date(p.prescriptionDate).toLocaleDateString()}</td>
                  <td className="px-4 py-3 text-gray-800">{p.itemCount} medication{p.itemCount !== 1 ? 's' : ''}{p.hasControlledSubstances ? ' · CS' : ''}</td>
                  <td className="px-4 py-3 text-gray-600">{p.prescribedByName}</td>
                  <td className="px-4 py-3 text-gray-500 text-xs">{p.expiresAt ? new Date(p.expiresAt).toLocaleDateString() : '—'}</td>
                  <td className="px-4 py-3">
                    <span className={`text-xs font-medium px-2 py-0.5 rounded-full ${STATUS_COLORS[p.status] ?? 'bg-gray-100 text-gray-600'}`}>
                      {p.status}
                    </span>
                  </td>
                  <td className="px-4 py-3 text-right">
                    <Link to={`/prescriptions/${p.prescriptionId}`} className="text-blue-600 hover:underline text-xs">View</Link>
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
