import { useState, useEffect, useCallback, useId } from 'react';
import { useParams, Link } from 'react-router-dom';
import { getConsultation, updateConsultation, signConsultation } from '../../api/consultations';
import type { ConsultationDetailResponse, UpdateConsultationRequest, DiagnosisDto } from '../../types/consultations';
import { useAuth } from '../../contexts/AuthContext';
import { Roles } from '../../types';
import { getSoapCopilot, getPatientSummary } from '../../api/ai';
import { ConfirmDialog } from '../../components/ConfirmDialog';

const inputCls = 'w-full px-3 py-2 border border-gray-300 rounded-lg text-sm focus:outline-none focus:ring-2 focus:ring-blue-500 disabled:bg-gray-50 disabled:text-gray-500';
const textareaCls = `${inputCls} resize-none`;

export default function ConsultationDetailPage() {
  const { id } = useParams<{ id: string }>();
  const { user } = useAuth();

  const [consultation, setConsultation] = useState<ConsultationDetailResponse | null>(null);
  const [loading, setLoading] = useState(true);
  const [saving, setSaving] = useState(false);
  const [signing, setSigning] = useState(false);
  const [dirty, setDirty] = useState(false);
  const [error, setError] = useState('');
  const [confirmAction, setConfirmAction] = useState<null | { message: string; run: () => void }>(null);

  // Edit state
  const [soap, setSoap] = useState({ s: '', o: '', a: '', p: '' });
  const [vitals, setVitals] = useState({
    systolic: '', diastolic: '', hr: '', temp: '', weight: '', height: '', spo2: '',
  });
  const [primaryCode, setPrimaryCode] = useState('');
  const [primaryDesc, setPrimaryDesc] = useState('');
  const [secondaryDx, setSecondaryDx] = useState<DiagnosisDto[]>([]);

  // AI & Speech recognition states
  const [recordingField, setRecordingField] = useState<'s' | 'o' | 'a' | 'p' | null>(null);
  const [copilotOpen, setCopilotOpen] = useState(false);
  const [unstructuredText, setUnstructuredText] = useState('');
  const [copilotLoading, setCopilotLoading] = useState(false);
  const [copilotError, setCopilotError] = useState('');

  const [summaryOpen, setSummaryOpen] = useState(false);
  const [summaryLoading, setSummaryLoading] = useState(false);
  const [patientSummary, setPatientSummary] = useState('');

  const populate = useCallback((c: ConsultationDetailResponse) => {
    setSoap({
      s: c.subjectiveNotes ?? '',
      o: c.objectiveNotes ?? '',
      a: c.assessmentNotes ?? '',
      p: c.planNotes ?? '',
    });
    setVitals({
      systolic: c.bloodPressureSystolic?.toString() ?? '',
      diastolic: c.bloodPressureDiastolic?.toString() ?? '',
      hr: c.heartRateBPM?.toString() ?? '',
      temp: c.temperatureCelsius?.toString() ?? '',
      weight: c.weightKg?.toString() ?? '',
      height: c.heightCm?.toString() ?? '',
      spo2: c.oxygenSaturationPct?.toString() ?? '',
    });
    setPrimaryCode(c.primaryDiagnosisCode ?? '');
    setPrimaryDesc(c.primaryDiagnosisDesc ?? '');
    setSecondaryDx(c.secondaryDiagnoses ?? []);
    setDirty(false);
  }, []);

  useEffect(() => {
    if (!id) return;
    getConsultation(id)
      .then((c) => { setConsultation(c); populate(c); })
      .catch(() => setError('Failed to load consultation.'))
      .finally(() => setLoading(false));
  }, [id, populate]);

  function markDirty() { setDirty(true); }

  async function handleSave() {
    if (!id) return;
    setSaving(true);
    setError('');
    const payload: UpdateConsultationRequest = {
      subjectiveNotes: soap.s || undefined,
      objectiveNotes: soap.o || undefined,
      assessmentNotes: soap.a || undefined,
      planNotes: soap.p || undefined,
      bloodPressureSystolic: vitals.systolic ? Number(vitals.systolic) : null,
      bloodPressureDiastolic: vitals.diastolic ? Number(vitals.diastolic) : null,
      heartRateBPM: vitals.hr ? Number(vitals.hr) : null,
      temperatureCelsius: vitals.temp ? Number(vitals.temp) : null,
      weightKg: vitals.weight ? Number(vitals.weight) : null,
      heightCm: vitals.height ? Number(vitals.height) : null,
      oxygenSaturationPct: vitals.spo2 ? Number(vitals.spo2) : null,
      primaryDiagnosisCode: primaryCode || undefined,
      primaryDiagnosisDesc: primaryDesc || undefined,
      secondaryDiagnoses: secondaryDx.length > 0 ? secondaryDx : undefined,
    };
    try {
      const updated = await updateConsultation(id, payload);
      setConsultation(updated);
      populate(updated);
    } catch (err: unknown) {
      const msg = (err as { response?: { data?: { message?: string } } })?.response?.data?.message;
      setError(msg || 'Save failed.');
    } finally {
      setSaving(false);
    }
  }

  async function handleSign() {
    if (!id) return;
    setSigning(true);
    setError('');
    try {
      const updated = await signConsultation(id);
      setConsultation(updated);
      populate(updated);
    } catch (err: unknown) {
      const msg = (err as { response?: { data?: { message?: string } } })?.response?.data?.message;
      setError(msg || 'Sign-off failed.');
    } finally {
      setSigning(false);
    }
  }

  function addSecondaryDx() {
    setSecondaryDx((prev) => [...prev, { code: '', description: '' }]);
    markDirty();
  }

  function updateSecondaryDx(i: number, field: keyof DiagnosisDto, value: string) {
    setSecondaryDx((prev) => prev.map((d, idx) => idx === i ? { ...d, [field]: value } : d));
    markDirty();
  }

  function removeSecondaryDx(i: number) {
    setSecondaryDx((prev) => prev.filter((_, idx) => idx !== i));
    markDirty();
  }

  const startDictation = (field: 's' | 'o' | 'a' | 'p') => {
    const SpeechObj = (window as any).SpeechRecognition || (window as any).webkitSpeechRecognition;
    if (!SpeechObj) {
      setError('Speech recognition is not supported in this browser. Please use Chrome or Edge.');
      return;
    }
    if (recordingField) {
      stopDictation();
      return;
    }
    const rec = new SpeechObj();
    rec.continuous = true;
    rec.interimResults = true;
    rec.lang = 'en-US';
    rec.onresult = (event: any) => {
      let transcript = '';
      for (let i = event.resultIndex; i < event.results.length; ++i) {
        if (event.results[i].isFinal) {
          transcript += event.results[i][0].transcript;
        }
      }
      if (transcript) {
        setSoap((s) => ({ ...s, [field]: (s[field] + ' ' + transcript).trim() }));
        setDirty(true);
      }
    };
    rec.onend = () => {
      setRecordingField(null);
    };
    rec.onerror = () => {
      setRecordingField(null);
    };
    rec.start();
    (window as any).activeSpeechRec = rec;
    setRecordingField(field);
  };

  const stopDictation = () => {
    if ((window as any).activeSpeechRec) {
      (window as any).activeSpeechRec.stop();
      (window as any).activeSpeechRec = null;
    }
    setRecordingField(null);
  };

  async function handleSoapCopilot() {
    if (!unstructuredText.trim()) return;
    setCopilotLoading(true);
    setCopilotError('');
    try {
      const data = await getSoapCopilot(unstructuredText);
      setSoap({
        s: data.subjective,
        o: data.objective,
        a: data.assessment,
        p: data.plan,
      });
      setPrimaryCode(data.primaryCode);
      setPrimaryDesc(data.primaryDesc);
      setSecondaryDx(data.secondary);
      setDirty(true);
      setCopilotOpen(false);
      setUnstructuredText('');
    } catch (err) {
      setCopilotError('AI processing failed. Please try again.');
    } finally {
      setCopilotLoading(false);
    }
  }

  async function handleGenerateSummary() {
    setSummaryOpen(true);
    setSummaryLoading(true);
    try {
      const data = await getPatientSummary(soap.s, soap.o, soap.a, soap.p);
      setPatientSummary(data.summary);
    } catch (err) {
      setPatientSummary('Failed to generate patient instructions. Please ensure notes are completed.');
    } finally {
      setSummaryLoading(false);
    }
  }

  const isSigned = consultation?.status === 'Signed';
  const canSign = user && [Roles.Doctor, Roles.SuperAdmin, Roles.Admin].includes(user.role as never);
  const canEdit = user && [Roles.Doctor, Roles.Nurse, Roles.SuperAdmin, Roles.Admin].includes(user.role as never);

  if (loading) return <div className="p-8 text-gray-400">Loading…</div>;
  if (error && !consultation) return <div className="p-8 text-red-600">{error}</div>;
  if (!consultation) return <div className="p-8 text-red-600">Consultation not found.</div>;

  return (
    <div className="p-6 max-w-4xl">
      {/* Breadcrumb */}
      <div className="text-sm text-gray-500 mb-4">
        <Link to="/consultations" className="hover:text-blue-600">Consultations</Link>
        <span className="mx-2">/</span>
        <span className="text-gray-800">{consultation.patientName}</span>
      </div>

      {/* Header */}
      <div className="flex items-start justify-between mb-6">
        <div>
          <h2 className="text-2xl font-semibold text-gray-800">{consultation.patientName}</h2>
          <p className="text-sm text-gray-500 mt-0.5">
            {new Date(consultation.createdAt).toLocaleDateString('en-GB', {
              weekday: 'long', day: 'numeric', month: 'long', year: 'numeric',
            })} · Dr. {consultation.doctorName}
          </p>
        </div>
        <div className="flex items-center gap-3">
          <button
            onClick={handleGenerateSummary}
            className="px-3 py-1.5 border border-amber-500 text-amber-700 rounded-lg hover:bg-amber-50 text-sm font-medium flex items-center gap-1.5 cursor-pointer"
          >
            <span>📋 Take-Home Instructions</span>
          </button>
          <Link to={`/referrals/new?patientId=${consultation.patientId}&consultationId=${consultation.consultationId}`}
            className="px-3 py-1.5 border border-indigo-500 text-indigo-700 rounded-lg hover:bg-indigo-50 text-sm font-medium">
            + Referral
          </Link>
          <span className={`text-sm font-medium px-3 py-1.5 rounded-full ${
            isSigned ? 'bg-green-100 text-green-700' : 'bg-yellow-100 text-yellow-700'
          }`}>
            {isSigned ? `Signed ${new Date(consultation.signedAt!).toLocaleDateString('en-GB')}` : 'Draft'}
          </span>
          {!isSigned && canEdit && dirty && (
            <button
              onClick={handleSave}
              disabled={saving}
              className="px-4 py-2 bg-blue-700 hover:bg-blue-800 text-white text-sm font-medium rounded-lg disabled:opacity-50 transition-colors"
            >
              {saving ? 'Saving…' : 'Save'}
            </button>
          )}
          {!isSigned && canSign && (
            <button
              onClick={() => setConfirmAction({ message: 'Sign off this consultation? This cannot be undone.', run: handleSign })}
              disabled={signing || saving}
              className="px-4 py-2 bg-green-600 hover:bg-green-700 text-white text-sm font-medium rounded-lg disabled:opacity-50 transition-colors"
            >
              {signing ? 'Signing…' : 'Sign & Complete'}
            </button>
          )}
        </div>
      </div>

      {error && <p className="text-sm text-red-600 bg-red-50 px-4 py-3 rounded-lg mb-4">{error}</p>}

      <div className="space-y-5">
        {/* SOAP Notes */}
        <section className="bg-white rounded-xl border border-gray-200 p-5">
          <div className="flex items-center justify-between mb-4">
            <h3 className="text-sm font-semibold text-gray-700 uppercase tracking-wide">SOAP Notes</h3>
            {!isSigned && canEdit && (
              <button
                type="button"
                onClick={() => setCopilotOpen(true)}
                className="text-xs bg-indigo-50 hover:bg-indigo-100 text-indigo-700 font-semibold px-3 py-1.5 rounded-lg border border-indigo-200 transition-colors flex items-center gap-1.5 cursor-pointer"
              >
                <span>✨ AI SOAP Copilot</span>
              </button>
            )}
          </div>
          <div className="space-y-4">
            {(['s', 'o', 'a', 'p'] as const).map((key) => {
              const labels = { s: 'Subjective', o: 'Objective', a: 'Assessment', p: 'Plan' };
              return (
                <div key={key}>
                  <div className="flex items-center justify-between mb-1">
                    <label htmlFor={`soap-${key}`} className="block text-xs font-semibold text-gray-500 uppercase">{labels[key]}</label>
                    {!isSigned && canEdit && (
                      <button
                        type="button"
                        onClick={() => recordingField === key ? stopDictation() : startDictation(key)}
                        className={`text-[10px] flex items-center gap-1 px-2 py-0.5 rounded-full border transition-all cursor-pointer ${
                          recordingField === key
                            ? 'bg-red-500 text-white border-red-500 animate-pulse'
                            : 'bg-gray-50 hover:bg-gray-100 text-gray-600 border-gray-200'
                        }`}
                      >
                        {recordingField === key ? '🛑 Stop' : '🎙️ Dictate'}
                      </button>
                    )}
                  </div>
                  <textarea
                    id={`soap-${key}`}
                    rows={3}
                    disabled={isSigned || !canEdit}
                    value={soap[key]}
                    onChange={(e) => { setSoap((s) => ({ ...s, [key]: e.target.value })); markDirty(); }}
                    className={textareaCls}
                    placeholder={`${labels[key]} notes…`}
                  />
                </div>
              );
            })}
          </div>
        </section>

        {/* Vitals */}
        <section className="bg-white rounded-xl border border-gray-200 p-5">
          <h3 className="text-sm font-semibold text-gray-700 uppercase tracking-wide mb-4">Vitals</h3>
          <div className="grid grid-cols-4 gap-4">
            <VitalField label="BP Systolic (mmHg)" value={vitals.systolic} disabled={isSigned || !canEdit}
              onChange={(v) => { setVitals((s) => ({ ...s, systolic: v })); markDirty(); }} />
            <VitalField label="BP Diastolic (mmHg)" value={vitals.diastolic} disabled={isSigned || !canEdit}
              onChange={(v) => { setVitals((s) => ({ ...s, diastolic: v })); markDirty(); }} />
            <VitalField label="Heart Rate (bpm)" value={vitals.hr} disabled={isSigned || !canEdit}
              onChange={(v) => { setVitals((s) => ({ ...s, hr: v })); markDirty(); }} />
            <VitalField label="Temp (°C)" value={vitals.temp} disabled={isSigned || !canEdit}
              onChange={(v) => { setVitals((s) => ({ ...s, temp: v })); markDirty(); }} step="0.1" />
            <VitalField label="Weight (kg)" value={vitals.weight} disabled={isSigned || !canEdit}
              onChange={(v) => { setVitals((s) => ({ ...s, weight: v })); markDirty(); }} step="0.1" />
            <VitalField label="Height (cm)" value={vitals.height} disabled={isSigned || !canEdit}
              onChange={(v) => { setVitals((s) => ({ ...s, height: v })); markDirty(); }} step="0.1" />
            <VitalField label="SpO₂ (%)" value={vitals.spo2} disabled={isSigned || !canEdit}
              onChange={(v) => { setVitals((s) => ({ ...s, spo2: v })); markDirty(); }} step="0.1" />
            {/* BMI derived */}
            {vitals.weight && vitals.height && (
              <div>
                <p className="text-xs font-semibold text-gray-500 uppercase mb-1">BMI</p>
                <p className="text-sm text-gray-700 py-2 font-medium">
                  {(Number(vitals.weight) / Math.pow(Number(vitals.height) / 100, 2)).toFixed(1)}
                </p>
              </div>
            )}
          </div>
        </section>

        {/* Diagnoses */}
        <section className="bg-white rounded-xl border border-gray-200 p-5">
          <h3 className="text-sm font-semibold text-gray-700 uppercase tracking-wide mb-4">ICD-10 Diagnoses</h3>

          <div className="mb-4">
            <p className="text-xs font-semibold text-gray-500 uppercase mb-2">Primary Diagnosis</p>
            <div className="flex gap-3">
              <input
                disabled={isSigned || !canEdit}
                value={primaryCode}
                onChange={(e) => { setPrimaryCode(e.target.value); markDirty(); }}
                placeholder="ICD-10 Code (e.g. J06.9)"
                className={`${inputCls} w-40 font-mono`}
              />
              <input
                disabled={isSigned || !canEdit}
                value={primaryDesc}
                onChange={(e) => { setPrimaryDesc(e.target.value); markDirty(); }}
                placeholder="Description"
                className={`${inputCls} flex-1`}
              />
            </div>
          </div>

          <div>
            <div className="flex items-center justify-between mb-2">
              <p className="text-xs font-semibold text-gray-500 uppercase">Secondary Diagnoses</p>
              {!isSigned && canEdit && (
                <button
                  type="button"
                  onClick={addSecondaryDx}
                  className="text-xs text-blue-600 hover:underline"
                >
                  + Add
                </button>
              )}
            </div>
            {secondaryDx.length === 0 && <p className="text-sm text-gray-400">None.</p>}
            {secondaryDx.map((dx, i) => (
              <div key={i} className="flex gap-3 mb-2">
                <input
                  disabled={isSigned || !canEdit}
                  value={dx.code}
                  onChange={(e) => updateSecondaryDx(i, 'code', e.target.value)}
                  placeholder="ICD-10 Code"
                  className={`${inputCls} w-40 font-mono`}
                />
                <input
                  disabled={isSigned || !canEdit}
                  value={dx.description}
                  onChange={(e) => updateSecondaryDx(i, 'description', e.target.value)}
                  placeholder="Description"
                  className={`${inputCls} flex-1`}
                />
                {!isSigned && canEdit && (
                  <button
                    type="button"
                    onClick={() => removeSecondaryDx(i)}
                    aria-label="Remove secondary diagnosis"
                    className="text-red-400 hover:text-red-600 text-sm px-2"
                  >
                    ×
                  </button>
                )}
              </div>
            ))}
          </div>
        </section>

        {/* Metadata */}
        <section className="bg-white rounded-xl border border-gray-200 p-5">
          <div className="flex justify-between text-xs text-gray-400">
            <span>Created: {new Date(consultation.createdAt).toLocaleString('en-GB')}</span>
            <span>Updated: {new Date(consultation.updatedAt).toLocaleString('en-GB')}</span>
          </div>
          <div className="mt-3 flex gap-4">
            <Link to={`/patients/${consultation.patientId}`} className="text-sm text-blue-600 hover:underline">
              Patient Record →
            </Link>
            <Link to={`/appointments/${consultation.appointmentId}`} className="text-sm text-blue-600 hover:underline">
              Appointment →
            </Link>
          </div>
        </section>
      </div>

      {/* Sidebar Copilot Drawer */}
      {copilotOpen && (
        <div className="fixed inset-0 bg-black/40 backdrop-blur-sm z-50 flex justify-end">
          <div className="bg-white w-full max-w-md h-full shadow-2xl flex flex-col p-6 animate-slide-in">
            <div className="flex items-center justify-between border-b border-gray-100 pb-4 mb-4">
              <h3 className="font-bold text-gray-800 text-lg flex items-center gap-2">
                <span>✨</span> AI Clinical SOAP Copilot
              </h3>
              <button onClick={() => setCopilotOpen(false)} aria-label="Close" className="text-gray-400 hover:text-gray-600 text-2xl font-bold cursor-pointer">×</button>
            </div>
            <div className="flex-1 flex flex-col space-y-4 overflow-y-auto">
              <p className="text-xs text-gray-500">
                Enter your rough consultation notes, patient comments, or clinical observations below. The AI will structure them into Subjective, Objective, Assessment, and Plan fields and suggest matching diagnosis codes.
              </p>
              <textarea
                rows={10}
                value={unstructuredText}
                onChange={(e) => setUnstructuredText(e.target.value)}
                placeholder="e.g., patient has a dry cough and throat pain for 3 days, temp is 38.1C, tonsils swollen and red. suspect strep pharyngitis. rx amoxicillin, gargle salt water, follow up in a week..."
                className="w-full flex-1 p-3 border border-gray-300 rounded-xl text-sm focus:ring-2 focus:ring-indigo-500 outline-none resize-none"
              />
              {copilotError && <p className="text-xs text-red-600 bg-red-50 p-2.5 rounded-lg font-semibold">{copilotError}</p>}
            </div>
            <div className="border-t border-gray-100 pt-4 flex gap-3">
              <button
                type="button"
                onClick={() => setCopilotOpen(false)}
                className="flex-1 py-2.5 border border-gray-300 rounded-xl text-xs font-bold text-gray-600 hover:bg-gray-50 transition-all cursor-pointer"
              >
                Cancel
              </button>
              <button
                type="button"
                disabled={copilotLoading || !unstructuredText.trim()}
                onClick={handleSoapCopilot}
                className="flex-1 py-2.5 bg-indigo-600 hover:bg-indigo-700 disabled:opacity-50 text-white rounded-xl text-xs font-bold transition-all cursor-pointer"
              >
                {copilotLoading ? 'Structuring Notes...' : 'Apply AI Formatting'}
              </button>
            </div>
          </div>
        </div>
      )}

      {/* Patient Summary Modal */}
      {summaryOpen && (
        <div className="fixed inset-0 bg-black/45 backdrop-blur-sm z-50 flex items-center justify-center p-4">
          <div className="bg-white rounded-2xl max-w-2xl w-full max-h-[85vh] shadow-2xl overflow-hidden flex flex-col animate-scale-up">
            <div className="bg-gradient-to-r from-amber-500 to-orange-500 p-5 text-white flex justify-between items-center">
              <div>
                <h3 className="font-extrabold text-base flex items-center gap-2">
                  <span>📋</span> Patient Take-Home Instructions
                </h3>
                <p className="text-amber-50 text-xs mt-0.5">Simplified, patient-friendly guidance based on clinical notes.</p>
              </div>
              <button onClick={() => setSummaryOpen(false)} aria-label="Close" className="text-white hover:text-amber-105 text-2xl font-bold cursor-pointer">×</button>
            </div>
            <div className="flex-1 p-6 overflow-y-auto prose max-w-none text-sm text-gray-700">
              {summaryLoading ? (
                <div className="flex flex-col items-center justify-center py-12 space-y-3">
                  <div className="w-8 h-8 border-4 border-amber-500 border-t-transparent rounded-full animate-spin"></div>
                  <p className="text-gray-400 font-medium text-xs">Translating medical notes into plain English...</p>
                </div>
              ) : (
                <div className="whitespace-pre-line leading-relaxed">{patientSummary}</div>
              )}
            </div>
            <div className="bg-gray-50 border-t border-gray-100 p-4 flex justify-end gap-3">
              <button
                onClick={() => window.print()}
                disabled={summaryLoading}
                className="px-4 py-2 border border-gray-300 text-gray-600 hover:bg-gray-100 rounded-xl text-xs font-semibold cursor-pointer"
              >
                🖨️ Print Instructions
              </button>
              <button
                onClick={() => setSummaryOpen(false)}
                className="px-4 py-2 bg-amber-600 hover:bg-amber-700 text-white rounded-xl text-xs font-semibold cursor-pointer"
              >
                Close
              </button>
            </div>
          </div>
        </div>
      )}

      <ConfirmDialog
        open={!!confirmAction}
        title="Confirm"
        message={confirmAction?.message ?? ''}
        danger
        busy={signing}
        onConfirm={() => { confirmAction?.run(); setConfirmAction(null); }}
        onCancel={() => setConfirmAction(null)}
      />
    </div>
  );
}

function VitalField({
  label, value, onChange, disabled, step = '1',
}: {
  label: string; value: string; onChange: (v: string) => void; disabled: boolean; step?: string;
}) {
  const id = useId();
  return (
    <div>
      <label htmlFor={id} className="block text-xs font-semibold text-gray-500 uppercase mb-1">{label}</label>
      <input
        id={id}
        type="number"
        step={step}
        value={value}
        onChange={(e) => onChange(e.target.value)}
        disabled={disabled}
        className="w-full px-3 py-2 border border-gray-300 rounded-lg text-sm focus:outline-none focus:ring-2 focus:ring-blue-500 disabled:bg-gray-50 disabled:text-gray-500"
      />
    </div>
  );
}
