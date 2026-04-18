import { useState, useEffect } from 'react';
import { useParams, Link } from 'react-router-dom';
import {
  getPrescription,
  cancelPrescription,
  downloadPrescriptionReport,
  partialDispensePrescription,
} from '../../api/prescriptions';
import type { PrescriptionDetailResponse, PartialDispenseItemRequest } from '../../types/prescriptions';
import { STATUS_COLORS } from '../../types/prescriptions';
import { useAuth } from '../../contexts/AuthContext';
import { Roles } from '../../types';

export default function PrescriptionDetailPage() {
  const { id } = useParams<{ id: string }>();
  const { user } = useAuth();

  const [rx, setRx]                       = useState<PrescriptionDetailResponse | null>(null);
  const [loading, setLoading]             = useState(true);
  const [error, setError]                 = useState('');
  const [cancelling, setCancelling]       = useState(false);
  const [downloading, setDownloading]     = useState(false);
  const [showDispenseModal, setShowDispenseModal] = useState(false);
  const [showHistory, setShowHistory]     = useState(false);

  useEffect(() => {
    if (!id) return;
    getPrescription(id)
      .then(setRx)
      .catch(() => setError('Failed to load prescription.'))
      .finally(() => setLoading(false));
  }, [id]);

  async function handleDownload() {
    if (!id) return;
    setDownloading(true);
    try {
      const blob = await downloadPrescriptionReport(id);
      const url = URL.createObjectURL(blob);
      const a = document.createElement('a');
      a.href = url;
      a.download = `Prescription-${id.slice(0, 8).toUpperCase()}.pdf`;
      a.click();
      URL.revokeObjectURL(url);
    } catch {
      alert('Failed to download prescription PDF.');
    } finally {
      setDownloading(false);
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

  const today = new Date();
  today.setHours(0, 0, 0, 0);
  const expiryDate   = rx?.expiresAt ? new Date(rx.expiresAt) : null;
  const daysToExpiry = expiryDate ? Math.ceil((expiryDate.getTime() - today.getTime()) / 86400000) : null;
  const expiringSoon = daysToExpiry !== null && daysToExpiry >= 0 && daysToExpiry <= 7 && rx?.status === 'Active';

  const isDispensable = rx?.status === 'Active' || rx?.status === 'PartiallyDispensed';

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
            {rx.status === 'PartiallyDispensed' ? 'Partially Dispensed' : rx.status}
          </span>
          <button
            onClick={handleDownload}
            disabled={downloading}
            className="px-4 py-2 border border-gray-300 text-gray-700 hover:bg-gray-50 text-sm font-medium rounded-lg transition-colors disabled:opacity-50"
          >
            {downloading ? 'Downloading…' : 'Download PDF'}
          </button>
          {isDispensable && canDispense && (
            <button
              onClick={() => setShowDispenseModal(true)}
              className="px-4 py-2 bg-green-600 hover:bg-green-700 text-white text-sm font-medium rounded-lg transition-colors"
            >
              Dispense
            </button>
          )}
          {isDispensable && canCancel && (
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

      {expiringSoon && (
        <div className="mb-5 bg-orange-50 border border-orange-300 px-4 py-3 text-sm text-orange-800 font-medium">
          ⚠ This prescription expires in <strong>{daysToExpiry} day{daysToExpiry !== 1 ? 's' : ''}</strong>
          {expiryDate && <> ({expiryDate.toLocaleDateString('en-GB', { day: 'numeric', month: 'short', year: 'numeric' })})</>}.
          Dispense before it expires.
        </div>
      )}

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
              <Row label="Expires On" value={expiryDate ? expiryDate.toLocaleDateString('en-GB', { day: 'numeric', month: 'short', year: 'numeric' }) : '—'} />
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
            {rx.items.map((item, i) => {
              const remaining = item.quantity - item.quantityDispensed;
              return (
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
                    <div className="text-right text-sm text-gray-600 space-y-1">
                      <div>Qty: <span className="font-medium">{item.quantity}</span></div>
                      {item.refills > 0 && <div>Refills: {item.refills}</div>}
                      {item.quantityDispensed > 0 && (
                        <div className={`text-xs font-medium ${item.isFullyDispensed ? 'text-green-600' : 'text-yellow-600'}`}>
                          {item.isFullyDispensed
                            ? `Fully dispensed (${item.quantity})`
                            : `${item.quantityDispensed} dispensed · ${remaining} remaining`}
                        </div>
                      )}
                    </div>
                  </div>
                  {item.instructions && (
                    <p className="mt-2 text-sm text-gray-500 italic">{item.instructions}</p>
                  )}
                </div>
              );
            })}
          </div>
        </section>

        {/* Dispense History */}
        {rx.dispenseHistory.length > 0 && (
          <section className="bg-white rounded-xl border border-gray-200 overflow-hidden">
            <button
              onClick={() => setShowHistory((v) => !v)}
              className="w-full px-5 py-3 border-b border-gray-100 bg-gray-50 flex items-center justify-between hover:bg-gray-100 transition-colors"
            >
              <h3 className="text-sm font-semibold text-gray-700">
                Dispense History ({rx.dispenseHistory.length} event{rx.dispenseHistory.length !== 1 ? 's' : ''})
              </h3>
              <span className="text-xs text-gray-400">{showHistory ? '▲ Hide' : '▼ Show'}</span>
            </button>
            {showHistory && (
              <div className="divide-y divide-gray-100">
                {rx.dispenseHistory.map((event) => (
                  <div key={event.dispenseEventId} className="px-5 py-4">
                    <div className="flex items-center justify-between mb-2">
                      <span className="text-sm font-medium text-gray-700">
                        {new Date(event.dispensedAt).toLocaleString('en-GB')}
                      </span>
                      <span className="text-xs text-gray-500">By {event.dispensedByName}</span>
                    </div>
                    <div className="space-y-1">
                      {event.items.map((ei) => (
                        <div key={ei.prescriptionItemId} className="flex items-center justify-between text-sm text-gray-600">
                          <span>{ei.medicationName}</span>
                          <span className="text-xs font-medium text-gray-500">Qty: {ei.quantityDispensed}</span>
                        </div>
                      ))}
                    </div>
                    {event.notes && (
                      <p className="mt-2 text-xs text-gray-400 italic">{event.notes}</p>
                    )}
                  </div>
                ))}
              </div>
            )}
          </section>
        )}

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

      {/* Partial Dispense Modal */}
      {showDispenseModal && (
        <PartialDispenseModal
          rx={rx}
          onDone={(updated) => { setRx(updated); setShowDispenseModal(false); }}
          onClose={() => setShowDispenseModal(false)}
        />
      )}
    </div>
  );
}

function PartialDispenseModal({
  rx, onDone, onClose,
}: {
  rx: PrescriptionDetailResponse;
  onDone: (updated: PrescriptionDetailResponse) => void;
  onClose: () => void;
}) {
  const [qtys, setQtys]     = useState<Record<string, number>>(() =>
    Object.fromEntries(rx.items.map((i) => [i.itemId, i.quantity - i.quantityDispensed]))
  );
  const [notes, setNotes]   = useState('');
  const [saving, setSaving] = useState(false);

  const hasAny = Object.values(qtys).some((q) => q > 0);

  async function handleConfirm() {
    setSaving(true);
    try {
      const items: PartialDispenseItemRequest[] = rx.items
        .map((i) => ({ prescriptionItemId: i.itemId, quantityToDispense: qtys[i.itemId] ?? 0 }));
      const updated = await partialDispensePrescription(rx.prescriptionId, { notes: notes || undefined, items });
      onDone(updated);
    } catch (err: unknown) {
      const msg = (err as { response?: { data?: { message?: string } } })?.response?.data?.message;
      alert(msg || 'Dispense failed.');
    } finally {
      setSaving(false);
    }
  }

  return (
    <div className="fixed inset-0 bg-black/40 flex items-center justify-center z-50">
      <div className="bg-white rounded-2xl shadow-xl p-6 w-full max-w-lg max-h-[85vh] flex flex-col">
        <h3 className="text-lg font-semibold text-gray-800 mb-1">Dispense Prescription</h3>
        <p className="text-sm text-gray-500 mb-4">
          Set the quantity to dispense for each item. Set to 0 to skip an item this time.
        </p>

        <div className="overflow-y-auto flex-1 space-y-3 mb-4">
          {rx.items.map((item) => {
            const remaining = item.quantity - item.quantityDispensed;
            return (
              <div key={item.itemId} className="border border-gray-200 rounded-lg p-3">
                <div className="flex items-start justify-between gap-3">
                  <div className="flex-1 min-w-0">
                    <p className="text-sm font-medium text-gray-800 truncate">{item.medicationName}</p>
                    <p className="text-xs text-gray-400">{item.strength} · {item.dosageForm}</p>
                    <p className="text-xs text-gray-400 mt-0.5">
                      Prescribed: {item.quantity} · Previously dispensed: {item.quantityDispensed} · Remaining: {remaining}
                    </p>
                  </div>
                  <div className="shrink-0">
                    <label className="block text-xs text-gray-500 mb-1 text-right">Qty now</label>
                    <input
                      type="number"
                      min={0}
                      max={remaining}
                      value={qtys[item.itemId] ?? 0}
                      onChange={(e) => setQtys((prev) => ({ ...prev, [item.itemId]: Number(e.target.value) }))}
                      disabled={item.isFullyDispensed}
                      className="w-20 px-2 py-1.5 border border-gray-300 rounded-lg text-sm text-right focus:outline-none focus:ring-2 focus:ring-green-400 disabled:bg-gray-100 disabled:text-gray-400"
                    />
                    {item.isFullyDispensed && (
                      <p className="text-xs text-green-600 text-right mt-0.5">Done</p>
                    )}
                  </div>
                </div>
              </div>
            );
          })}
        </div>

        <div className="mb-4">
          <label className="block text-sm text-gray-600 mb-1">Pharmacist Notes (optional)</label>
          <textarea
            value={notes}
            onChange={(e) => setNotes(e.target.value)}
            rows={2}
            className="w-full px-3 py-2 border border-gray-300 rounded-lg text-sm focus:outline-none focus:ring-2 focus:ring-green-400 resize-none"
            placeholder="e.g. Partial stock — remaining 7 tablets to be collected next week"
          />
        </div>

        <div className="flex gap-3 justify-end">
          <button
            onClick={onClose}
            className="px-4 py-2 text-sm text-gray-600 border border-gray-300 rounded-lg hover:bg-gray-50"
          >
            Cancel
          </button>
          <button
            onClick={handleConfirm}
            disabled={saving || !hasAny}
            className="px-4 py-2 text-sm font-medium bg-green-600 hover:bg-green-700 text-white rounded-lg disabled:opacity-50 transition-colors"
          >
            {saving ? 'Dispensing…' : 'Confirm Dispense'}
          </button>
        </div>
      </div>
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
