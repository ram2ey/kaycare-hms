import { useState, useEffect } from 'react';
import { Link } from 'react-router-dom';
import { getReferrals, getIncomingReferrals } from '../../api/referrals';
import type { ReferralSummaryResponse } from '../../types/referrals';
import { REFERRAL_STATUS_COLORS, REFERRAL_URGENCY_COLORS } from '../../types/referrals';
import { useAuth } from '../../contexts/AuthContext';
import { Roles } from '../../types';

const STATUS_OPTIONS = ['', 'Draft', 'Sent', 'Accepted', 'Completed', 'Declined', 'Cancelled'];

export default function ReferralsPage() {
  const { user } = useAuth();
  const [referrals, setReferrals]   = useState<ReferralSummaryResponse[]>([]);
  const [loading, setLoading]       = useState(true);
  const [status, setStatus]         = useState('');
  const [view, setView]             = useState<'all' | 'incoming'>('all');

  const canCreate = user && [Roles.Doctor, Roles.Admin, Roles.SuperAdmin].includes(user.role as never);
  const isDoctor  = user?.role === Roles.Doctor;

  useEffect(() => {
    setLoading(true);
    const fetch = view === 'incoming' ? getIncomingReferrals() : getReferrals(status || undefined);
    fetch
      .then(res => setReferrals(Array.isArray(res) ? res : ((res as any)?.items || (res as any)?.data || [])))
      .catch(() => setReferrals([]))
      .finally(() => setLoading(false));
  }, [status, view]);

  return (
    <div className="p-6 max-w-6xl mx-auto">
      <div className="flex items-center justify-between mb-6">
        <div>
          <h1 className="text-2xl font-bold text-gray-900">Referrals</h1>
          <p className="text-gray-500 text-sm mt-0.5">Manage patient referrals</p>
        </div>
        {canCreate && (
          <Link
            to="/referrals/new"
            className="px-4 py-2 bg-blue-700 text-white rounded-lg hover:bg-blue-800 text-sm font-medium"
          >
            + New Referral
          </Link>
        )}
      </div>

      <div className="flex items-center gap-4 mb-5">
        {isDoctor && (
          <div className="flex border border-gray-200 rounded-lg overflow-hidden text-sm">
            <button
              onClick={() => setView('all')}
              className={`px-4 py-2 font-medium transition-colors ${view === 'all' ? 'bg-blue-700 text-white' : 'bg-white text-gray-600 hover:bg-gray-50'}`}
            >
              All Referrals
            </button>
            <button
              onClick={() => setView('incoming')}
              className={`px-4 py-2 font-medium transition-colors ${view === 'incoming' ? 'bg-blue-700 text-white' : 'bg-white text-gray-600 hover:bg-gray-50'}`}
            >
              Incoming (My Queue)
            </button>
          </div>
        )}
        {view === 'all' && (
          <select
            value={status}
            onChange={e => setStatus(e.target.value)}
            className="px-3 py-2 border border-gray-300 rounded-lg text-sm focus:outline-none focus:ring-2 focus:ring-blue-500"
          >
            {STATUS_OPTIONS.map(s => (
              <option key={s} value={s}>{s || 'All Statuses'}</option>
            ))}
          </select>
        )}
      </div>

      {loading ? (
        <div className="text-center py-12 text-gray-400">Loading…</div>
      ) : referrals.length === 0 ? (
        <div className="text-center py-12 text-gray-400">No referrals found.</div>
      ) : (
        <div className="bg-white border border-gray-200 rounded-xl overflow-hidden">
          <table className="w-full text-sm">
            <thead className="bg-gray-50 border-b border-gray-200">
              <tr>
                {['Ref #', 'Patient', 'Referring Doctor', 'Referred To', 'Type', 'Urgency', 'Status', 'Date', ''].map(h => (
                  <th key={h} className="px-4 py-3 text-left text-xs font-semibold text-gray-500 uppercase tracking-wide">{h}</th>
                ))}
              </tr>
            </thead>
            <tbody className="divide-y divide-gray-100">
              {referrals.map(r => (
                <tr key={r.referralId} className="hover:bg-gray-50 transition-colors">
                  <td className="px-4 py-3 font-mono text-xs text-blue-700">{r.referralNumber}</td>
                  <td className="px-4 py-3">
                    <div className="font-medium text-gray-800">{r.patientName}</div>
                    <div className="text-xs text-gray-400">{r.patientMrn}</div>
                  </td>
                  <td className="px-4 py-3 text-gray-600">{r.referringDoctorName}</td>
                  <td className="px-4 py-3 text-gray-600">
                    {r.referralType === 'External'
                      ? (r.externalFacility ?? '—')
                      : (r.referredToDoctorName ?? r.referredToDepartment ?? '—')}
                  </td>
                  <td className="px-4 py-3">
                    <span className={`text-xs font-medium px-2 py-0.5 rounded-full ${r.referralType === 'External' ? 'bg-purple-100 text-purple-700' : 'bg-teal-100 text-teal-700'}`}>
                      {r.referralType}
                    </span>
                  </td>
                  <td className="px-4 py-3">
                    <span className={`text-xs font-medium px-2 py-0.5 rounded-full ${REFERRAL_URGENCY_COLORS[r.urgency] ?? 'bg-gray-100 text-gray-600'}`}>
                      {r.urgency}
                    </span>
                  </td>
                  <td className="px-4 py-3">
                    <span className={`text-xs font-medium px-2 py-0.5 rounded-full ${REFERRAL_STATUS_COLORS[r.status] ?? 'bg-gray-100 text-gray-600'}`}>
                      {r.status}
                    </span>
                  </td>
                  <td className="px-4 py-3 text-gray-400 text-xs whitespace-nowrap">
                    {new Date(r.createdAt).toLocaleDateString()}
                  </td>
                  <td className="px-4 py-3">
                    <Link to={`/referrals/${r.referralId}`} className="text-blue-600 hover:text-blue-800 text-xs font-medium">
                      View →
                    </Link>
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
