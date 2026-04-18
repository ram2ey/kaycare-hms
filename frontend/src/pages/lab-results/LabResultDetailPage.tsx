import { useState, useEffect } from 'react';
import { useParams, Link } from 'react-router-dom';
import { getLabResultById } from '../../api/labResults';
import type { LabResultDetailResponse } from '../../types/labResults';

const ABNORMAL_COLORS: Record<string, string> = {
  H:  'text-red-600 font-semibold',
  HH: 'text-red-700 font-bold',
  L:  'text-blue-600 font-semibold',
  LL: 'text-blue-700 font-bold',
  A:  'text-orange-600 font-semibold',
};

export default function LabResultDetailPage() {
  const { id } = useParams<{ id: string }>();
  const [result, setResult] = useState<LabResultDetailResponse | null>(null);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState('');

  useEffect(() => {
    if (!id) return;
    getLabResultById(id)
      .then(setResult)
      .catch(() => setError('Failed to load lab result.'))
      .finally(() => setLoading(false));
  }, [id]);

  if (loading) return <div className="p-8 text-gray-400">Loading…</div>;
  if (error || !result) return <div className="p-8 text-red-600">{error || 'Not found.'}</div>;

  const hasAbnormal = result.observations.some((o) => o.abnormalFlag);

  return (
    <div className="p-6 max-w-4xl">
      {/* Breadcrumb */}
      <div className="text-sm text-gray-500 mb-4">
        <Link to="/lab-results" className="hover:text-blue-600">Lab Results</Link>
        <span className="mx-2">/</span>
        <span className="font-mono text-gray-800">{result.accessionNumber}</span>
      </div>

      {/* Header */}
      <div className="flex items-start justify-between mb-6">
        <div>
          <h2 className="text-2xl font-semibold text-gray-800">{result.orderName ?? result.accessionNumber}</h2>
          <p className="text-sm text-gray-500 mt-0.5">
            {result.patientName} · <span className="font-mono">{result.patientMrn}</span>
            {result.orderingDoctorName && ` · Dr. ${result.orderingDoctorName}`}
          </p>
        </div>
        <div className="flex items-center gap-2">
          {hasAbnormal && (
            <span className="bg-red-100 text-red-700 text-xs font-semibold px-3 py-1.5 rounded-full">
              Abnormal Results
            </span>
          )}
          <span className={`text-sm font-medium px-3 py-1.5 rounded-full ${
            result.status === 'Verified' ? 'bg-green-100 text-green-700' : 'bg-blue-100 text-blue-700'
          }`}>
            {result.status}
          </span>
        </div>
      </div>

      <div className="space-y-5">
        {/* Meta */}
        <div className="grid grid-cols-2 gap-5">
          <section className="bg-white rounded-xl border border-gray-200 p-5">
            <h3 className="text-sm font-semibold text-gray-700 uppercase tracking-wide mb-4">Order Info</h3>
            <dl className="space-y-2.5">
              <Row label="Accession #" value={result.accessionNumber} mono />
              <Row label="Order Code" value={result.orderCode} mono />
              <Row label="Order Name" value={result.orderName} />
              <Row label="Ordered" value={result.orderedAt ? new Date(result.orderedAt).toLocaleString('en-GB') : null} />
              <Row label="Received" value={new Date(result.receivedAt).toLocaleString('en-GB')} />
            </dl>
          </section>
          <section className="bg-white rounded-xl border border-gray-200 p-5">
            <h3 className="text-sm font-semibold text-gray-700 uppercase tracking-wide mb-4">Patient</h3>
            <dl className="space-y-2.5">
              <Row label="Name" value={result.patientName} />
              <Row label="MRN" value={result.patientMrn} mono />
              <Row label="Ordering Doctor" value={result.orderingDoctorName} />
            </dl>
            <div className="mt-3">
              <Link to={`/patients/${result.patientId}`} className="text-sm text-blue-600 hover:underline">
                View Patient Record →
              </Link>
            </div>
          </section>
        </div>

        {/* Observations */}
        <section className="bg-white rounded-xl border border-gray-200 overflow-hidden">
          <div className="px-5 py-3 border-b border-gray-100 bg-gray-50">
            <h3 className="text-sm font-semibold text-gray-700">
              Observations ({result.observations.length})
            </h3>
          </div>
          {result.observations.length === 0 ? (
            <p className="px-5 py-6 text-sm text-gray-400">No observations.</p>
          ) : (
            <table className="w-full text-sm">
              <thead className="border-b border-gray-100">
                <tr>
                  <th className="text-left px-5 py-2.5 font-medium text-gray-500 text-xs uppercase">#</th>
                  <th className="text-left px-5 py-2.5 font-medium text-gray-500 text-xs uppercase">Test</th>
                  <th className="text-left px-5 py-2.5 font-medium text-gray-500 text-xs uppercase">Code</th>
                  <th className="text-right px-5 py-2.5 font-medium text-gray-500 text-xs uppercase">Value</th>
                  <th className="text-left px-5 py-2.5 font-medium text-gray-500 text-xs uppercase">Units</th>
                  <th className="text-left px-5 py-2.5 font-medium text-gray-500 text-xs uppercase">Reference</th>
                  <th className="text-left px-5 py-2.5 font-medium text-gray-500 text-xs uppercase">Flag</th>
                </tr>
              </thead>
              <tbody className="divide-y divide-gray-50">
                {result.observations.map((obs) => (
                  <tr key={obs.labObservationId} className={obs.abnormalFlag ? 'bg-red-50/40' : ''}>
                    <td className="px-5 py-3 text-gray-400 text-xs">{obs.sequenceNumber}</td>
                    <td className="px-5 py-3 font-medium text-gray-800">{obs.testName}</td>
                    <td className="px-5 py-3 font-mono text-xs text-gray-500">{obs.testCode}</td>
                    <td className={`px-5 py-3 text-right ${obs.abnormalFlag ? ABNORMAL_COLORS[obs.abnormalFlag] ?? 'text-orange-600 font-semibold' : 'text-gray-800'}`}>
                      {obs.value ?? '—'}
                    </td>
                    <td className="px-5 py-3 text-gray-500">{obs.units ?? '—'}</td>
                    <td className="px-5 py-3 text-gray-500 font-mono text-xs">{obs.referenceRange ?? '—'}</td>
                    <td className="px-5 py-3">
                      {obs.abnormalFlag && (
                        <span className={`text-xs font-bold ${ABNORMAL_COLORS[obs.abnormalFlag] ?? 'text-orange-600'}`}>
                          {obs.abnormalFlag}
                        </span>
                      )}
                    </td>
                  </tr>
                ))}
              </tbody>
            </table>
          )}
        </section>
      </div>
    </div>
  );
}

function Row({ label, value, mono }: { label: string; value: string | null | undefined; mono?: boolean }) {
  return (
    <div className="flex text-sm">
      <dt className="w-36 text-gray-500 shrink-0">{label}</dt>
      <dd className={`text-gray-800 ${mono ? 'font-mono text-xs' : ''}`}>{value || '—'}</dd>
    </div>
  );
}
