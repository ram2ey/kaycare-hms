import { useState, useEffect } from 'react';
import { useParams, Link } from 'react-router-dom';
import { getPrescription, dispensePrescription, cancelPrescription } from '../../api/prescriptions';
import type { PrescriptionDetailResponse } from '../../types/prescriptions';
import { STATUS_COLORS } from '../../types/prescriptions';
import { useAuth } from '../../contexts/AuthContext';
import { Roles } from '../../types';

export default function PrescriptionDetailPage() {
  const { id } = useParams<{ id: string }>();
  const { user } = useAuth();

  const [rx, setRx] = useState<PrescriptionDetailResponse | null>(null);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState('');
  const [dispensing, setDispensing] = useState(false);
  const [cancelling, setCancelling] = useState(false);
  const [dispenseNotes, setDispenseNotes] = useState('');
  const [showDispenseModal, setShowDispenseModal] = useState(false);

  useEffect(() => {
    if (!id) return;
    getPrescription(id)
      .then(setRx)
      .catch(() => setError('Failed to load prescription.'))
      .finally(() => setLoading(false));
  }, [id]);

  async function handleDispense() {
    if (!id) return;
    setDispensing(true);
    try {
      const updated = await dispensePrescription(id, dispenseNotes || undefined);
      setRx(updated);
      setShowDispenseModal(false);
    } catch (err: unknown) {
      const msg = (err as { response?: { data?: { message?: string } } })?.response?.data?.message;
      alert(msg || 'Dispense failed.');
    } finally {
      setDispensing(false);
    }
  }

  async function handleCancel() {
    if (!id || !confirm('Cancel this prescription?')) return;
    setCancelling(true);
    try {
      const updated = await cancelPrescription(id);
      setRx(updated);
    } catch (err: unknown) {
      const msg = (err as { response?: { data?: { message?: string } } })?.response?.data?.message;
      alert(msg || 'Cancel failed.');
    } finally {
      setCancelling(false);
    }
  }

  const canDispense = user && [Roles.Pharmacist, Roles.Admin, Roles.SuperAdmin].includes(user.role as never);
  const canCancel   = user && [Roles.Doctor, Roles.Admin, Roles.SuperAdmin].includes(user.role as never);

  if (loading) return <div className="p-8 text-gray-400">Loading…</div>;
  if (error || !rx) return <div className="p-8 text-red-600">{error || 'Not found.'}</div>;

  return (
    <div className="p-6 max-w-4xl">
      {/* Breadcrumb */}
      <div className="text-sm text-gray-500 mb-4">
        <Link to="/prescriptions" className="hover:text-blue-600">Prescriptions</Link>
        <span className="mx-2">/</span>
        <span className="text-gray-800">{rx.patientName}</span>
      </div>

      {/* Header */}
      <div className="flex items-start justify-between mb-6">
        <div>
          <h2 className="text-2xl font-semibold text-gray-800">{rx.patientName}</h2>
          <p className="text-sm text-gray-500 mt-0.5">
            Prescribed by {rx.prescribedByName} · {rx.prescriptionDate}
          </p>
        </div>
        <div className="flex items-center gap-3">
          <span className={`text-sm font-medium px-3 py-1.5 rounded-full ${STATUS_COLORS[rx.status] ?? 'bg-gray-100 text-gray-600'}`}>
            {rx.status}
          </span>
          {rx.status === 'Active' && canDispense && (
            <button
              onClick={() => setShowDispenseModal(true)}
              className="px-4 py-2 bg-green-600 hover:bg-green-700 text-white text-sm font-medium rounded-lg transition-colors"
            >
              Dispense
            </button>
          )}
          {rx.status === 'Active' && canCancel && (
            <button
              onClick={handleCancel}
              disabled={cancelling}
              className="px-4 py-2 border border-red-300 text-red-600 hover:bg-red-50 text-sm font-medium rounded-lg transition-colors disabled:opacity-50"
            >
              {cancelling ? 'Cancelling…' : 'Cancel'}
            </button>
          )}
        </div>
      </div>

      <div className="space-y-5">
        {/* Meta */}
        <div className="grid grid-cols-2 gap-5">
          <section className="bg-white rounded-xl border border-gray-200 p-5">
            <h3 className="text-sm font-semibold text-gray-700 uppercase tracking-wide mb-4">Patient</h3>
            <dl className="space-y-2.5">
              <Row label="Name" value={rx.patientName} />
              <Row label="MRN" value={rx.medicalRecordNumber} />
            </dl>
            <div className="mt-3">
              <Link to={`/patients/${rx.patientId}`} className="text-sm text-blue-600 hover:underline">
                View Patient Record →
              </Link>
            </div>
          </section>

          <section className="bg-white rounded-xl border border-gray-200 p-5">
            <h3 className="text-sm font-semibold text-gray-700 uppercase tracking-wide mb-4">Dispensing Info</h3>
            <dl className="space-y-2.5">
              <Row label="Dispensed By" value={rx.dispensedByName} />
              <Row label="Dispensed At" value={rx.dispensedAt ? new Date(rx.dispensedAt).toLocaleString('en-GB') : null} />
            </dl>
          </section>
        </div>

        {rx.notes && (
          <section className="bg-white rounded-xl border border-gray-200 p-5">
            <h3 className="text-sm font-semibold text-gray-700 uppercase tracking-wide mb-2">Notes</h3>
            <p className="text-sm text-gray-700 whitespace-pre-wrap">{rx.notes}</p>
          </section>
        )}

        {/* Medication items */}
        <section className="bg-white rounded-xl border border-gray-200 overflow-hidden">
          <div className="px-5 py-3 border-b border-gray-100 bg-gray-50 flex items-center justify-between">
            <h3 className="text-sm font-semibold text-gray-700">
              Medications — {rx.items.length} item{rx.items.length !== 1 ? 's' : ''}
            </h3>
            {rx.hasControlledSubstances && (
              <span className="text-xs bg-red-100 text-red-700 px-2 py-0.5 rounded font-medium">
                Contains Controlled Substances
              </span>
            )}
          </div>
          <div className="divide-y divide-gray-100">
            {rx.items.map((item, i) => (
              <div key={item.itemId} className="px-5 py-4">
                <div className="flex items-start justify-between">
                  <div>
                    <div className="flex items-center gap-2">
                      <span className="text-xs text-gray-400 font-mono">#{i + 1}</span>
                      <span className="font-semibold text-gray-800">{item.medicationName}</span>
                      {item.genericName && (
                        <span className="text-xs text-gray-400">({item.genericName})</span>
                      )}
                      {item.isControlledSubstance && (
                        <span className="text-xs bg-red-100 text-red-700 px-1.5 py-0.5 rounded font-medium">CS</span>
                      )}
                    </div>
                    <div className="flex gap-4 mt-1.5 text-sm text-gray-600">
                      <span>{item.strength} · {item.dosageForm}</span>
                      <span>{item.frequency}</span>
                      <span>{item.durationDays} day{item.durationDays !== 1 ? 's' : ''}</span>
                    </div>
                  </div>
                  <div className="text-right text-sm text-gray-600">
                    <div>Qty: <span className="font-medium">{item.quantity}</span></div>
                    {item.refills > 0 && <div>Refills: {item.refills}</div>}
                  </div>
                </div>
                {item.instructions && (
                  <p className="mt-2 text-sm text-gray-500 italic">{item.instructions}</p>
                )}
              </div>
            ))}
          </div>
        </section>

        <section className="bg-white rounded-xl border border-gray-200 p-5">
          <div className="flex justify-between text-xs text-gray-400">
            <span>Created: {new Date(rx.createdAt).toLocaleString('en-GB')}</span>
            <span>Updated: {new Date(rx.updatedAt).toLocaleString('en-GB')}</span>
          </div>
          <div className="mt-2">
            <Link to={`/consultations/${rx.consultationId}`} className="text-sm text-blue-600 hover:underline">
              View Consultation →
            </Link>
          </div>
        </section>
      </div>

      {/* Dispense modal */}
      {showDispenseModal && (
        <div className="fixed inset-0 bg-black/40 flex items-center justify-center z-50">
          <div className="bg-white rounded-2xl shadow-xl p-6 w-full max-w-md">
            <h3 className="text-lg font-semibold text-gray-800 mb-4">Dispense Prescription</h3>
            <p className="text-sm text-gray-600 mb-3">
              Dispensing <strong>{rx.items.length} item{rx.items.length !== 1 ? 's' : ''}</strong> to{' '}
              <strong>{rx.patientName}</strong>.
            </p>
            <label className="block text-sm text-gray-600 mb-2">Pharmacist Notes (optional)</label>
            <textarea
              value={dispenseNotes}
              onChange={(e) => setDispenseNotes(e.target.value)}
              rows={3}
              className="w-full px-3 py-2 border border-gray-300 rounded-lg text-sm focus:outline-none focus:ring-2 focus:ring-green-400 resize-none"
              placeholder="e.g. Substituted with generic, counselled patient…"
            />
            <div className="flex gap-3 mt-4 justify-end">
              <button
                onClick={() => setShowDispenseModal(false)}
                className="px-4 py-2 text-sm text-gray-600 border border-gray-300 rounded-lg hover:bg-gray-50"
              >
                Cancel
              </button>
              <button
                onClick={handleDispense}
                disabled={dispensing}
                className="px-4 py-2 text-sm font-medium bg-green-600 hover:bg-green-700 text-white rounded-lg disabled:opacity-50 transition-colors"
              >
                {dispensing ? 'Dispensing…' : 'Confirm Dispense'}
              </button>
            </div>
          </div>
        </div>
      )}
    </div>
  );
}

function Row({ label, value }: { label: string; value: string | null | undefined }) {
  return (
    <div className="flex text-sm">
      <dt className="w-36 text-gray-500 shrink-0">{label}</dt>
      <dd className="text-gray-800">{value || '—'}</dd>
    </div>
  );
}
