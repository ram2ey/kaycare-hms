import { useState, useEffect } from 'react';
import { Link } from 'react-router-dom';
import { getReferralsForPatient } from '../../../api/referrals';
import type { ReferralSummaryResponse } from '../../../types/referrals';
import { useAuth } from '../../../contexts/AuthContext';
import { Roles } from '../../../types';

const STATUS_COLORS: Record<string, string> = {
  Draft:     'bg-gray-100 text-gray-600',
  Sent:      'bg-blue-100 text-blue-700',
  Accepted:  'bg-teal-100 text-teal-700',
  Completed: 'bg-green-100 text-green-700',
  Declined:  'bg-red-100 text-red-600',
  Cancelled: 'bg-gray-100 text-gray-400',
};

const URGENCY_COLORS: Record<string, string> = {
  Routine:   'bg-gray-100 text-gray-600',
  Urgent:    'bg-orange-100 text-orange-700',
  Emergency: 'bg-red-100 text-red-700',
};

export default function ReferralsTab({ patientId }: { patientId: string }) {
  const { user } = useAuth();
  const [referrals, setReferrals] = useState<ReferralSummaryResponse[]>([]);
  const [loading, setLoading] = useState(true);

  useEffect(() => {
    getReferralsForPatient(patientId)
      .then((data) => setReferrals(Array.isArray(data) ? data : []))
      .catch(() => setReferrals([]))
      .finally(() => setLoading(false));
  }, [patientId]);

  const canCreate = [Roles.Doctor, Roles.Admin, Roles.SuperAdmin].includes(user?.role as never);

  if (loading) return <div className="text-gray-400 text-sm">Loading…</div>;

  return (
    <div className="max-w-4xl">
      <div className="flex items-center justify-between mb-4">
        <h3 className="text-sm font-semibold text-gray-700">Referrals ({referrals.length})</h3>
        {canCreate && (
          <Link to={`/referrals/new?patientId=${patientId}`}
            className="px-3 py-1.5 bg-blue-700 text-white rounded-lg hover:bg-blue-800 text-sm font-medium">
            + New Referral
          </Link>
        )}
      </div>

      {referrals.length === 0 ? (
        <p className="text-sm text-gray-400">No referrals on record.</p>
      ) : (
        <div className="bg-white rounded-xl border border-gray-200 overflow-hidden">
          <table className="w-full text-sm">
            <thead className="bg-gray-50 border-b border-gray-200">
              <tr>
                <th className="text-left px-4 py-3 text-xs font-medium text-gray-500">Date</th>
                <th className="text-left px-4 py-3 text-xs font-medium text-gray-500">Ref #</th>
                <th className="text-left px-4 py-3 text-xs font-medium text-gray-500">Type</th>
                <th className="text-left px-4 py-3 text-xs font-medium text-gray-500">Referred To</th>
                <th className="text-left px-4 py-3 text-xs font-medium text-gray-500">Urgency</th>
                <th className="text-left px-4 py-3 text-xs font-medium text-gray-500">Status</th>
                <th />
              </tr>
            </thead>
            <tbody className="divide-y divide-gray-100">
              {referrals.map(r => (
                <tr key={r.referralId} className="hover:bg-gray-50">
                  <td className="px-4 py-3 text-gray-600">{new Date(r.createdAt).toLocaleDateString()}</td>
                  <td className="px-4 py-3 font-mono text-xs text-gray-700">{r.referralNumber}</td>
                  <td className="px-4 py-3 text-gray-600">{r.referralType}</td>
                  <td className="px-4 py-3 text-gray-800">
                    {r.referredToDoctorName ?? r.referredToDepartment ?? r.externalFacility ?? '—'}
                  </td>
                  <td className="px-4 py-3">
                    <span className={`text-xs font-medium px-2 py-0.5 rounded-full ${URGENCY_COLORS[r.urgency] ?? 'bg-gray-100 text-gray-600'}`}>
                      {r.urgency}
                    </span>
                  </td>
                  <td className="px-4 py-3">
                    <span className={`text-xs font-medium px-2 py-0.5 rounded-full ${STATUS_COLORS[r.status] ?? 'bg-gray-100 text-gray-600'}`}>
                      {r.status}
                    </span>
                  </td>
                  <td className="px-4 py-3 text-right">
                    <Link to={`/referrals/${r.referralId}`} className="text-blue-600 hover:underline text-xs">View</Link>
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
