import { useState, useEffect } from 'react';
import { useNavigate, useParams, Link } from 'react-router-dom';
import { createTemplate, updateTemplate, getTemplate } from '../../api/prescriptionTemplates';
import type { CreatePrescriptionTemplateRequest } from '../../types/prescriptionTemplates';
import type { PrescriptionItemRequest } from '../../types/prescriptions';
import { DOSAGE_FORMS, FREQUENCIES } from '../../types/prescriptions';
import MedicationAutocomplete from '../../components/MedicationAutocomplete';
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

interface Props {
  mode: 'create' | 'edit';
}

export default function CreateEditTemplatePage({ mode }: Props) {
  const navigate = useNavigate();
  const { id }   = useParams<{ id: string }>();

  const [name, setName]             = useState('');
  const [description, setDesc]      = useState('');
  const [isShared, setIsShared]     = useState(false);
  const [items, setItems]           = useState<PrescriptionItemRequest[]>([emptyItem()]);
  const [selectedMeds, setSelectedMeds] = useState<(MedicationEntry | null)[]>([null]);
  const [saving, setSaving]         = useState(false);
  const [error, setError]           = useState('');
  const [loading, setLoading]       = useState(mode === 'edit');

  useEffect(() => {
    if (mode === 'edit' && id) {
      getTemplate(id)
        .then((t) => {
          setName(t.name);
          setDesc(t.description ?? '');
          setIsShared(t.isShared);
          setItems(t.items.map((i) => ({
            medicationName:        i.medicationName,
            genericName:           i.genericName ?? '',
            strength:              i.strength,
            dosageForm:            i.dosageForm,
            frequency:             i.frequency,
            durationDays:          i.durationDays,
            quantity:              i.quantity,
            refills:               i.refills,
            instructions:          i.instructions ?? '',
            isControlledSubstance: i.isControlledSubstance,
          })));
          setSelectedMeds(t.items.map(() => null));
        })
        .catch(() => setError('Failed to load template.'))
        .finally(() => setLoading(false));
    }
  }, [mode, id]);

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

  async function handleSubmit(e: React.FormEvent) {
    e.preventDefault();
    if (!name.trim()) { setError('Template name is required.'); return; }
    setSaving(true);
    setError('');
    const payload: CreatePrescriptionTemplateRequest = {
      name: name.trim(),
      description: description.trim() || undefined,
      isShared,
      items: items.map((item) => ({
        ...item,
        genericName:  item.genericName  || undefined,
        instructions: item.instructions || undefined,
      })),
    };
    try {
      if (mode === 'create') {
        await createTemplate(payload);
      } else {
        await updateTemplate(id!, payload);
      }
      navigate('/prescriptions/templates');
    } catch (err: unknown) {
      const msg = (err as { response?: { data?: { message?: string } } })?.response?.data?.message;
      setError(msg || 'Failed to save template.');
    } finally {
      setSaving(false);
    }
  }

  if (loading) return <div className="p-8 text-gray-400">Loading…</div>;

  return (
    <div className="p-6 max-w-4xl">
      <div className="text-sm text-gray-500 mb-4">
        <Link to="/prescriptions" className="hover:text-blue-600">Prescriptions</Link>
        <span className="mx-2">/</span>
        <Link to="/prescriptions/templates" className="hover:text-blue-600">Templates</Link>
        <span className="mx-2">/</span>
        <span className="text-gray-800">{mode === 'create' ? 'New Template' : 'Edit Template'}</span>
      </div>

      <h2 className="text-2xl font-semibold text-gray-800 mb-6">
        {mode === 'create' ? 'New Prescription Template' : 'Edit Template'}
      </h2>

      <form onSubmit={handleSubmit} className="space-y-5">
        <section className="bg-white rounded-xl border border-gray-200 p-5 space-y-4">
          <h3 className="text-sm font-semibold text-gray-700 uppercase tracking-wide">Template Details</h3>
          <div>
            <label className="block text-xs text-gray-600 mb-1">Template Name *</label>
            <input
              required
              value={name}
              onChange={(e) => setName(e.target.value)}
              placeholder="e.g. Malaria Combo, Hypertension Pack"
              className={inp}
            />
          </div>
          <div>
            <label className="block text-xs text-gray-600 mb-1">Description</label>
            <input
              value={description}
              onChange={(e) => setDesc(e.target.value)}
              placeholder="Optional description"
              className={inp}
            />
          </div>
          <div className="flex items-center gap-2">
            <input
              type="checkbox"
              id="isShared"
              checked={isShared}
              onChange={(e) => setIsShared(e.target.checked)}
              className="rounded"
            />
            <label htmlFor="isShared" className="text-sm text-gray-700">
              Share with all doctors in this facility
            </label>
          </div>
        </section>

        <section className="bg-white rounded-xl border border-gray-200 p-5">
          <div className="flex items-center justify-between mb-4">
            <h3 className="text-sm font-semibold text-gray-700 uppercase tracking-wide">
              Medications ({items.length})
            </h3>
            <button
              type="button"
              onClick={addItem}
              className="text-xs bg-blue-50 hover:bg-blue-100 text-blue-700 font-medium px-3 py-1.5 rounded-lg transition-colors"
            >
              + Add Item
            </button>
          </div>

          <div className="space-y-5">
            {items.map((item, i) => (
              <div key={i} className="border border-gray-200 rounded-xl p-4">
                <div className="flex items-center justify-between mb-3">
                  <span className="text-xs font-semibold text-gray-500 uppercase">Item #{i + 1}</span>
                  {items.length > 1 && (
                    <button type="button" onClick={() => removeItem(i)} className="text-xs text-red-400 hover:text-red-600">
                      Remove
                    </button>
                  )}
                </div>

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
                    <input value={item.genericName ?? ''} onChange={(e) => updateItem(i, 'genericName', e.target.value)} placeholder="e.g. Amoxicillin" className={inp} />
                  </div>

                  <div>
                    <label className="block text-xs text-gray-600 mb-1">Strength *</label>
                    <input required value={item.strength} onChange={(e) => updateItem(i, 'strength', e.target.value)} placeholder="e.g. 500mg" className={inp} />
                    {selectedMeds[i] && selectedMeds[i]!.strengths.length > 1 && (
                      <div className="flex flex-wrap gap-1 mt-1">
                        {selectedMeds[i]!.strengths.map((s) => (
                          <button key={s} type="button" onClick={() => updateItem(i, 'strength', s)}
                            className={`text-xs px-2 py-0.5 border transition-colors ${item.strength === s ? 'bg-blue-100 border-blue-400 text-blue-700' : 'border-gray-300 text-gray-500 hover:border-gray-400'}`}>
                            {s}
                          </button>
                        ))}
                      </div>
                    )}
                  </div>
                  <div>
                    <label className="block text-xs text-gray-600 mb-1">Dosage Form *</label>
                    <select value={item.dosageForm} onChange={(e) => updateItem(i, 'dosageForm', e.target.value)} className={inp}>
                      {DOSAGE_FORMS.map((f) => <option key={f}>{f}</option>)}
                    </select>
                  </div>
                  <div>
                    <label className="block text-xs text-gray-600 mb-1">Frequency *</label>
                    <select value={item.frequency} onChange={(e) => updateItem(i, 'frequency', e.target.value)} className={inp}>
                      {FREQUENCIES.map((f) => <option key={f}>{f}</option>)}
                    </select>
                  </div>

                  <div>
                    <label className="block text-xs text-gray-600 mb-1">Duration (days) *</label>
                    <input required type="number" min={1} max={365} value={item.durationDays} onChange={(e) => updateItem(i, 'durationDays', Number(e.target.value))} className={inp} />
                  </div>
                  <div>
                    <label className="block text-xs text-gray-600 mb-1">Quantity *</label>
                    <input required type="number" min={1} max={9999} value={item.quantity} onChange={(e) => updateItem(i, 'quantity', Number(e.target.value))} className={inp} />
                  </div>
                  <div>
                    <label className="block text-xs text-gray-600 mb-1">Refills</label>
                    <input type="number" min={0} max={12} value={item.refills} onChange={(e) => updateItem(i, 'refills', Number(e.target.value))} className={inp} />
                  </div>

                  <div className="col-span-3">
                    <label className="block text-xs text-gray-600 mb-1">Patient Instructions</label>
                    <input value={item.instructions ?? ''} onChange={(e) => updateItem(i, 'instructions', e.target.value)} placeholder="e.g. Take with food…" className={inp} />
                  </div>

                  <div className="col-span-3 flex items-center gap-2">
                    <input type="checkbox" id={`cs-${i}`} checked={item.isControlledSubstance} onChange={(e) => updateItem(i, 'isControlledSubstance', e.target.checked)} className="rounded" />
                    <label htmlFor={`cs-${i}`} className="text-sm text-gray-700">Controlled Substance</label>
                  </div>
                </div>
              </div>
            ))}
          </div>
        </section>

        {error && <p className="text-sm text-red-600 bg-red-50 px-4 py-3 rounded-lg">{error}</p>}

        <div className="flex gap-3 justify-end">
          <Link to="/prescriptions/templates" className="px-5 py-2 border border-gray-300 rounded-lg text-sm text-gray-600 hover:bg-gray-50 transition-colors">
            Cancel
          </Link>
          <button type="submit" disabled={saving} className="px-5 py-2 bg-blue-700 hover:bg-blue-800 disabled:bg-blue-400 text-white text-sm font-medium rounded-lg transition-colors">
            {saving ? 'Saving…' : mode === 'create' ? 'Create Template' : 'Save Changes'}
          </button>
        </div>
      </form>
    </div>
  );
}
