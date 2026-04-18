import { useState, useEffect } from 'react';
import { getReferralReport } from '../../../api/reports';
import type { ReferralReportResponse } from '../../../types/reports';
import { StatCard, BarTable, LoadingState, ErrorState, EmptyState } from '../components';

export default function ReferralsTab({ range }: { range: { from: string; to: string } }) {
  const [data, setData]       = useState<ReferralReportResponse | null>(null);
  const [loading, setLoading] = useState(true);
  const [error, setError]     = useState('');

  useEffect(() => {
    setLoading(true); setError('');
    getReferralReport(range)
      .then(setData)
      .catch(() => setError('Failed to load referral report.'))
      .finally(() => setLoading(false));
  }, [range.from, range.to]);

  if (loading) return <LoadingState />;
  if (error || !data) return <ErrorState msg={error || 'No data.'} />;
  if (data.totalReferrals === 0) return <EmptyState />;

  const acceptRate = (data.acceptedReferrals + data.declinedReferrals) === 0 ? '—'
    : `${((data.acceptedReferrals / (data.acceptedReferrals + data.declinedReferrals)) * 100).toFixed(0)}% acceptance`;

  return (
    <div className="space-y-5">
      <div className="grid grid-cols-3 gap-4">
        <StatCard label="Total Referrals"  value={data.totalReferrals} />
        <StatCard label="Internal"         value={data.internalReferrals} />
        <StatCard label="External"         value={data.externalReferrals} />
        <StatCard label="Accepted"         value={data.acceptedReferrals} color="text-teal-700" sub={acceptRate} />
        <StatCard label="Declined"         value={data.declinedReferrals} color="text-red-600" />
        <StatCard label="Pending"          value={data.pendingReferrals}  color="text-blue-600" />
      </div>
      <div className="grid grid-cols-3 gap-5">
        <BarTable title="By Status"               rows={data.byStatus} />
        <BarTable title="By Urgency"              rows={data.byUrgency} />
        <BarTable title="Top Referring Doctors"   rows={data.topReferringDoctors} />
      </div>
    </div>
  );
}
