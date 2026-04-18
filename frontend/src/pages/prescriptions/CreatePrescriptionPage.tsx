import { useState, useEffect } from 'react';
import { useNavigate, useSearchParams, Link } from 'react-router-dom';
import { createPrescription } from '../../api/prescriptions';
import { getPatientConsultations } from '../../api/consultations';
import { getAllergies } from '../../api/patients';
import type { CreatePrescriptionRequest, PrescriptionItemRequest } from '../../types/prescriptions';
import type { ConsultationSummaryResponse } from '../../types/consultations';
import type { AllergyResponse } from '../../types/patients';
import { DOSAGE_FORMS, FREQUENCIES } from '../../types/prescriptions';
import MedicationAutocomplete from '../../components/MedicationAutocomplete';
import TemplatePickerModal from '../../components/TemplatePickerModal';
import type { MedicationEntry } from '../../data/medications';

const emptyItem = (): PrescriptionItemRequest => ({
  medicationName: '',
  genericName: '',
  strength: '',
  dosageForm: 'Tablet',
  frequency: 'Once daily',
  durationDays: 7,
  quantity: 1,
  refills: 0,
  instructions: '',
  isControlledSubstance: false,
});

const inp = 'w-full px-3 py-2 border border-gray-300 rounded-lg text-sm focus:outline-none focus:ring-2 focus:ring-blue-500';

export default function CreatePrescriptionPage() {
  const navigate = useNavigate();
  const [searchParams] = useSearchParams();
  const patientId = searchParams.get('patientId') ?? '';
  const consultationIdParam = searchParams.get('consultationId') ?? '';

  const [consultations, setConsultations] = useState<ConsultationSummaryResponse[]>([]);
  const [drugAllergies, setDrugAllergies] = useState<AllergyResponse[]>([]);
  const [selectedMeds, setSelectedMeds] = useState<(MedicationEntry | null)[]>([null]);
  const [consultationId, setConsultationId] = useState(consultationIdParam);
  const [notes, setNotes] = useState('');
  const [items, setItems] = useState<PrescriptionItemRequest[]>([emptyItem()]);
  const [saving, setSaving]                   = useState(false);
  const [error, setError]                     = useState('');
  const [showTemplatePicker, setShowTemplatePicker] = useState(false);

  useEffect(() => {
    if (!patientId) return;
    getPatientConsultations(patientId)
      .then((data) => {
        setConsultations(data);
        if (!consultationId && data.length === 1) setConsultationId(data[0].consultationId);
      })
      .catch(() => {});
    getAllergies(patientId)
      .then((data) => setDrugAllergies(data.filter((a) => a.allergyType === 'Drug')))
      .catch(() => {});
  }, [patientId, consultationId]);

  function getAllergyMatch(medicationName: string, genericName: string): AllergyResponse | undefined {
    if (!medicationName && !genericName) return undefined;
    const needle = (s: string) => s.toLowerCase().trim();
    return drugAllergies.find((a) => {
      const allergen = needle(a.allergenName);
      return (
        (medicationName && needle(medicationName).includes(allergen)) ||
        (medicationName && allergen.includes(needle(medicationName))) ||
        (genericName && needle(genericName).includes(allergen)) ||
        (genericName && allergen.includes(needle(genericName)))
      );
    });
  }

  function updateItem(i: number, field: keyof PrescriptionItemRequest, value: unknown) {
    setItems((prev) => prev.map((item, idx) => idx === i ? { ...item, [field]: value } : item));
  }

  function handleMedicationSelect(i: number, med: MedicationEntry) {
    setItems((prev) => prev.map((item, idx) => idx !== i ? item : {
      ...item,
      medicationName:        med.name,
      genericName:           med.genericName,
      strength:              med.strengths[0],
      dosageForm:            med.dosageForm,
      isControlledSubstance: med.isControlledSubstance ?? false,
    }));
    setSelectedMeds((prev) => prev.map((m, idx) => idx === i ? med : m));
  }

  function addItem() {
    setItems((prev) => [...prev, emptyItem()]);
    setSelectedMeds((prev) => [...prev, null]);
  }

  function removeItem(i: number) {
    if (items.length === 1) return;
    setItems((prev) => prev.filter((_, idx) => idx !== i));
    setSelectedMeds((prev) => prev.filter((_, idx) => idx !== i));
  }

  function handleLoadTemplate(templateItems: PrescriptionItemRequest[], _name: string) {
    setItems(templateItems);
    setSelectedMeds(templateItems.map(() => null));
    setShowTemplatePicker(false);
  }

  async function handleSubmit(e: React.FormEvent) {
    e.preventDefault();
    if (!consultationId) { setError('Please select a consultation.'); return; }
    setSaving(true);
    setError('');
    const payload: CreatePrescriptionRequest = {
      consultationId,
      notes: notes || undefined,
      items: items.map((item) => ({
        ...item,
        genericName: item.genericName || undefined,
        instructions: item.instructions || undefined,
      })),
    };
    try {
      const rx = await createPrescription(payload);
      navigate(`/prescriptions/${rx.prescriptionId}`);
    } catch (err: unknown) {
      const msg = (err as { response?: { data?: { message?: string } } })?.response?.data?.message;
      setError(msg || 'Failed to create prescription.');
    } finally {
      setSaving(false);
    }
  }

  return (
    <>
    <div className="p-6 max-w-4xl">
      <div className="text-sm text-gray-500 mb-4">
        <Link to="/prescriptions" className="hover:text-blue-600">Prescriptions</Link>
        <span className="mx-2">/</span>
        <span className="text-gray-800">New Prescription</span>
      </div>

      <h2 className="text-2xl font-semibold text-gray-800 mb-6">Write Prescription</h2>

      {drugAllergies.length > 0 && (
        <div className="mb-5 bg-red-50 border border-red-300 px-4 py-3 text-sm text-red-800">
          <p className="font-semibold mb-1">⚠ Known Drug Allergies</p>
          <ul className="list-disc list-inside space-y-0.5">
            {drugAllergies.map((a) => (
              <li key={a.allergyId}>
                <span className="font-medium">{a.allergenName}</span>
                {a.severity && <span className="text-red-600"> · {a.severity}</span>}
                {a.reaction && <span className="text-red-500"> — {a.reaction}</span>}
              </li>
            ))}
          </ul>
        </div>
      )}

      <form onSubmit={handleSubmit} className="space-y-5">
        {/* Consultation picker */}
        <section className="bg-white rounded-xl border border-gray-200 p-5">
          <h3 className="text-sm font-semibold text-gray-700 uppercase tracking-wide mb-4">Consultation</h3>
          {consultations.length === 0 ? (
            <p className="text-sm text-gray-400">No consultations found for this patient.</p>
          ) : (
            <div className="space-y-2">
              {consultations.map((c) => (
                <label
                  key={c.consultationId}
                  className={`flex items-start gap-3 p-3 border rounded-lg cursor-pointer transition-colors ${
                    consultationId === c.consultationId
                      ? 'border-blue-500 bg-blue-50'
                      : 'border-gray-200 hover:border-gray-300'
                  }`}
                >
                  <input
                    type="radio"
                    name="consultation"
                    value={c.consultationId}
                    checked={consultationId === c.consultationId}
                    onChange={() => setConsultationId(c.consultationId)}
                    className="mt-0.5"
                  />
                  <div>
                    <p className="text-sm font-medium text-gray-800">
                      {new Date(c.createdAt).toLocaleDateString('en-GB', {
                        day: 'numeric', month: 'short', year: 'numeric',
                      })}
                      {' · '}Dr. {c.doctorName}
                    </p>
                    {c.primaryDiagnosisCode && (
                      <p className="text-xs text-gray-500 mt-0.5">
                        <span className="font-mono">{c.primaryDiagnosisCode}</span> — {c.primaryDiagnosisDesc}
                      </p>
                    )}
                  </div>
                </label>
              ))}
            </div>
          )}
        </section>

        {/* Medication items */}
        <section className="bg-white rounded-xl border border-gray-200 p-5">
          <div className="flex items-center justify-between mb-4">
            <h3 className="text-sm font-semibold text-gray-700 uppercase tracking-wide">
              Medications ({items.length})
            </h3>
            <div className="flex gap-2">
              <button
                type="button"
                onClick={() => setShowTemplatePicker(true)}
                className="text-xs bg-gray-100 hover:bg-gray-200 text-gray-700 font-medium px-3 py-1.5 rounded-lg transition-colors"
              >
                Load Template
              </button>
              <button
                type="button"
                onClick={addItem}
                className="text-xs bg-blue-50 hover:bg-blue-100 text-blue-700 font-medium px-3 py-1.5 rounded-lg transition-colors"
              >
                + Add Item
              </button>
            </div>
          </div>

          <div className="space-y-5">
            {items.map((item, i) => (
              <div key={i} className="border border-gray-200 rounded-xl p-4 relative">
                <div className="flex items-center justify-between mb-3">
                  <span className="text-xs font-semibold text-gray-500 uppercase">Item #{i + 1}</span>
                  {items.length > 1 && (
                    <button
                      type="button"
                      onClick={() => removeItem(i)}
                      className="text-xs text-red-400 hover:text-red-600"
                    >
                      Remove
                    </button>
                  )}
                </div>

                {(() => {
                  const match = getAllergyMatch(item.medicationName, item.genericName ?? '');
                  return match ? (
                    <div className="mb-3 bg-red-50 border border-red-300 px-3 py-2 text-xs text-red-800 font-medium">
                      ⚠ Allergy alert: Patient is allergic to <strong>{match.allergenName}</strong>
                      {match.severity && <> ({match.severity})</>}
                      {match.reaction && <> — {match.reaction}</>}
                    </div>
                  ) : null;
                })()}

                <div className="grid grid-cols-3 gap-3">
                  <div className="col-span-2">
                    <label className="block text-xs text-gray-600 mb-1">Medication Name *</label>
                    <MedicationAutocomplete
                      value={item.medicationName}
                      onChange={(v) => updateItem(i, 'medicationName', v)}
                      onSelect={(med) => handleMedicationSelect(i, med)}
                      placeholder="Search medication…"
                      className={inp}
                    />
                  </div>
                  <div>
                    <label className="block text-xs text-gray-600 mb-1">Generic Name</label>
                    <input value={item.genericName ?? ''}
                      onChange={(e) => updateItem(i, 'genericName', e.target.value)}
                      placeholder="e.g. Amoxicillin" className={inp} />
                  </div>

                  <div>
                    <label className="block text-xs text-gray-600 mb-1">Strength *</label>
                    <input required value={item.strength}
                      onChange={(e) => updateItem(i, 'strength', e.target.value)}
                      placeholder="e.g. 500mg" className={inp} />
                    {selectedMeds[i] && selectedMeds[i]!.strengths.length > 1 && (
                      <div className="flex flex-wrap gap-1 mt-1">
                        {selectedMeds[i]!.strengths.map((s) => (
                          <button
                            key={s}
                            type="button"
                            onClick={() => updateItem(i, 'strength', s)}
                            className={`text-xs px-2 py-0.5 border transition-colors ${
                              item.strength === s
                                ? 'bg-blue-100 border-blue-400 text-blue-700'
                                : 'border-gray-300 text-gray-500 hover:border-gray-400'
                            }`}
                          >
                            {s}
                          </button>
                        ))}
                      </div>
                    )}
                  </div>
                  <div>
                    <label className="block text-xs text-gray-600 mb-1">Dosage Form *</label>
                    <select value={item.dosageForm}
                      onChange={(e) => updateItem(i, 'dosageForm', e.target.value)} className={inp}>
                      {DOSAGE_FORMS.map((f) => <option key={f}>{f}</option>)}
                    </select>
                  </div>
                  <div>
                    <label className="block text-xs text-gray-600 mb-1">Frequency *</label>
                    <select value={item.frequency}
                      onChange={(e) => updateItem(i, 'frequency', e.target.value)} className={inp}>
                      {FREQUENCIES.map((f) => <option key={f}>{f}</option>)}
                    </select>
                  </div>

                  <div>
                    <label className="block text-xs text-gray-600 mb-1">Duration (days) *</label>
                    <input required type="number" min={1} max={365} value={item.durationDays}
                      onChange={(e) => updateItem(i, 'durationDays', Number(e.target.value))} className={inp} />
                  </div>
                  <div>
                    <label className="block text-xs text-gray-600 mb-1">Quantity *</label>
                    <input required type="number" min={1} max={9999} value={item.quantity}
                      onChange={(e) => updateItem(i, 'quantity', Number(e.target.value))} className={inp} />
                  </div>
                  <div>
                    <label className="block text-xs text-gray-600 mb-1">Refills</label>
                    <input type="number" min={0} max={12} value={item.refills}
                      onChange={(e) => updateItem(i, 'refills', Number(e.target.value))} className={inp} />
                  </div>

                  <div className="col-span-3">
                    <label className="block text-xs text-gray-600 mb-1">Patient Instructions</label>
                    <input value={item.instructions ?? ''}
                      onChange={(e) => updateItem(i, 'instructions', e.target.value)}
                      placeholder="e.g. Take with food, avoid alcohol…" className={inp} />
                  </div>

                  <div className="col-span-3 flex items-center gap-2">
                    <input
                      type="checkbox"
                      id={`cs-${i}`}
                      checked={item.isControlledSubstance}
                      onChange={(e) => updateItem(i, 'isControlledSubstance', e.target.checked)}
                      className="rounded"
                    />
                    <label htmlFor={`cs-${i}`} className="text-sm text-gray-700">
                      Controlled Substance
                    </label>
                  </div>
                </div>
              </div>
            ))}
          </div>
        </section>

        {/* Notes */}
        <section className="bg-white rounded-xl border border-gray-200 p-5">
          <h3 className="text-sm font-semibold text-gray-700 uppercase tracking-wide mb-3">Prescription Notes</h3>
          <textarea
            rows={3}
            value={notes}
            onChange={(e) => setNotes(e.target.value)}
            placeholder="Optional notes for the pharmacist…"
            className={`${inp} resize-none`}
          />
        </section>

        {error && <p className="text-sm text-red-600 bg-red-50 px-4 py-3 rounded-lg">{error}</p>}

        <div className="flex gap-3 justify-end">
          <Link to="/prescriptions" className="px-5 py-2 border border-gray-300 rounded-lg text-sm text-gray-600 hover:bg-gray-50 transition-colors">
            Cancel
          </Link>
          <button
            type="submit"
            disabled={saving}
            className="px-5 py-2 bg-blue-700 hover:bg-blue-800 disabled:bg-blue-400 text-white text-sm font-medium rounded-lg transition-colors"
          >
            {saving ? 'Saving…' : 'Write Prescription'}
          </button>
        </div>
      </form>
    </div>

    {showTemplatePicker && (
      <TemplatePickerModal
        onSelect={handleLoadTemplate}
        onClose={() => setShowTemplatePicker(false)}
      />
    )}
    </>
  );
}
