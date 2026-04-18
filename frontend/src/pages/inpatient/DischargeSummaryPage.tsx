import { useState, useEffect, useCallback } from 'react';
import { useParams, Link } from 'react-router-dom';
import { getDischargeSummary, updateDischargeSummary, downloadDischargeSummaryReport } from '../../api/inpatient';
import type { DischargeSummaryResponse, UpdateDischargeSummaryRequest } from '../../types/inpatient';
import { DISCHARGE_CONDITIONS, DISCHARGE_CONDITIONS_COLORS, DISCHARGE_TYPE_LABELS } from '../../types/inpatient';
import { useAuth } from '../../contexts/AuthContext';
import { Roles } from '../../types';

const inp = 'w-full px-3 py-2 border border-gray-300 rounded-lg text-sm focus:outline-none focus:ring-2 focus:ring-blue-500';
const fmtD = (d: string) => new Date(d).toLocaleDateString('en-GB', { day: '2-digit', month: 'short', year: 'numeric' });

const EMPTY_FORM: UpdateDischargeSummaryRequest = {
  finalDiagnosis: '',
  treatmentSummary: '',
  proceduresPerformed: '',
  dischargeMedications: '',
  dischargeCondition: '',
  followUpInstructions: '',
  attendingPhysicianNotes: '',
};

export default function DischargeSummaryPage() {
  const { id } = useParams<{ id: string }>();
  const { user } = useAuth();
  const canEdit = [Roles.Doctor, Roles.Admin, Roles.SuperAdmin].includes(user?.role as any);

  const [summary, setSummary]   = useState<DischargeSummaryResponse | null>(null);
  const [loading, setLoading]   = useState(true);
  const [error, setError]       = useState('');
  const [editing, setEditing]   = useState(false);
  const [form, setForm]         = useState<UpdateDischargeSummaryRequest>(EMPTY_FORM);
  const [saving, setSaving]     = useState(false);
  const [saveError, setSaveError] = useState('');
  const [downloading, setDownloading] = useState(false);

  const load = useCallback(async () => {
    if (!id) return;
    setLoading(true); setError('');
    try { setSummary(await getDischargeSummary(id)); }
    catch { setError('Failed to load discharge summary.'); }
    finally { setLoading(false); }
  }, [id]);

  useEffect(() => { load(); }, [load]);

  const openEdit = () => {
    if (!summary) return;
    setForm({
      finalDiagnosis:          summary.finalDiagnosis         ?? '',
      treatmentSummary:        summary.treatmentSummary       ?? '',
      proceduresPerformed:     summary.proceduresPerformed    ?? '',
      dischargeMedications:    summary.dischargeMedications   ?? '',
      dischargeCondition:      summary.dischargeCondition     ?? '',
      followUpInstructions:    summary.followUpInstructions   ?? '',
      attendingPhysicianNotes: summary.attendingPhysicianNotes ?? '',
    });
    setSaveError(''); setEditing(true);
  };

  const handleSave = async () => {
    setSaving(true); setSaveError('');
    try {
      setSummary(await updateDischargeSummary(id!, form));
      setEditing(false);
    } catch (e: any) {
      setSaveError(e?.response?.data?.message ?? 'Failed to save.');
    } finally { setSaving(false); }
  };

  const handleDownload = async () => {
    if (!summary) return;
    setDownloading(true);
    try { await downloadDischargeSummaryReport(id!, summary.admissionNumber); }
    catch { /* silent */ }
    finally { setDownloading(false); }
  };

  if (loading) return <div className="p-6 text-center text-gray-400">Loading…</div>;
  if (error)   return <div className="p-6 text-center text-red-500">{error}</div>;
  if (!summary) return <div className="p-6 text-center text-gray-400">Not found.</div>;

  const conditionColor = summary.dischargeCondition
    ? (DISCHARGE_CONDITIONS_COLORS[summary.dischargeCondition] ?? 'bg-gray-100 text-gray-600')
    : '';

  return (
    <div className="p-6 max-w-4xl mx-auto">
      <div className="mb-2">
        <Link to={`/inpatient/admissions/${id}`} className="text-sm text-gray-500 hover:text-gray-700">
          ← {summary.admissionNumber}
        </Link>
      </div>

      <div className="flex items-start justify-between mb-6">
        <div>
          <h1 className="text-2xl font-bold text-gray-900">Discharge Summary</h1>
          <p className="text-gray-500 mt-1">{summary.patientName} · {summary.patientMrn}</p>
        </div>
        <div className="flex gap-2 flex-wrap justify-end">
          <button
            onClick={handleDownload}
            disabled={downloading}
            className="px-4 py-2 border border-blue-500 text-blue-600 rounded-lg hover:bg-blue-50 text-sm font-medium disabled:opacity-50">
            {downloading ? 'Generating…' : 'Download PDF'}
          </button>
          {canEdit && (
            <button onClick={openEdit}
              className="px-4 py-2 bg-blue-600 text-white rounded-lg hover:bg-blue-700 text-sm font-medium">
              Edit Summary
            </button>
          )}
        </div>
      </div>

      {/* Patient + Admission overview */}
      <div className="bg-white border border-gray-200 rounded-xl p-5 mb-4">
        <h2 className="font-semibold text-gray-700 mb-3 text-sm uppercase tracking-wide">Patient & Admission</h2>
        <div className="grid grid-cols-2 md:grid-cols-3 gap-x-6 gap-y-2 text-sm">
          {[
            ['Patient',         summary.patientName],
            ['MRN',             summary.patientMrn],
            ['Date of Birth',   summary.patientDateOfBirth ?? '—'],
            ['Gender',          summary.patientGender ?? '—'],
            ['Phone',           summary.patientPhone ?? '—'],
            ['Ward / Bed',      `${summary.wardName} · ${summary.bedNumber}`],
            ['Admitting Doctor', summary.admittingDoctorName],
            ['Admitted',        fmtD(summary.admissionDate)],
            ['Discharged',      summary.actualDischargeDate ? fmtD(summary.actualDischargeDate) : '—'],
            ['Length of Stay',  summary.lengthOfStayDays != null ? `${summary.lengthOfStayDays} day${summary.lengthOfStayDays !== 1 ? 's' : ''}` : '—'],
            ['Discharge Type',  summary.dischargeType ? (DISCHARGE_TYPE_LABELS[summary.dischargeType] ?? summary.dischargeType) : '—'],
          ].map(([label, value]) => (
            <div key={label}>
              <dt className="text-xs text-gray-400">{label}</dt>
              <dd className="font-medium text-gray-800">{value}</dd>
            </div>
          ))}
        </div>
      </div>

      {/* Diagnoses side by side */}
      <div className="grid grid-cols-1 md:grid-cols-2 gap-4 mb-4">
        <Section title="Diagnosis on Admission" text={summary.diagnosisOnAdmission} />
        <Section title="Final Diagnosis"        text={summary.finalDiagnosis} />
      </div>

      {/* Clinical sections */}
      <div className="space-y-4 mb-4">
        <Section title="Treatment Summary"        text={summary.treatmentSummary} />
        <Section title="Procedures Performed"     text={summary.proceduresPerformed} />
        <Section title="Discharge Medications"    text={summary.dischargeMedications} />
        <Section title="Follow-up Instructions"   text={summary.followUpInstructions} />
        <Section title="Attending Physician Notes" text={summary.attendingPhysicianNotes} />
        {summary.dischargeNotes && <Section title="Discharge Notes" text={summary.dischargeNotes} />}
      </div>

      {/* Condition at discharge */}
      {summary.dischargeCondition && (
        <div className="flex items-center gap-3 mb-4">
          <span className="text-sm text-gray-500">Condition at Discharge:</span>
          <span className={`text-sm px-3 py-0.5 rounded-full font-medium ${conditionColor}`}>
            {summary.dischargeCondition}
          </span>
        </div>
      )}

      {/* Edit modal */}
      {editing && (
        <div className="fixed inset-0 bg-black/50 flex items-start justify-center z-50 p-4 overflow-y-auto">
          <div className="bg-white rounded-2xl shadow-xl w-full max-w-2xl my-8">
            <div className="px-6 py-4 border-b border-gray-100 flex items-center justify-between">
              <h2 className="font-semibold text-gray-900">Edit Discharge Summary</h2>
              <button onClick={() => setEditing(false)} className="text-gray-400 hover:text-gray-600 text-xl">×</button>
            </div>
            <div className="px-6 py-4 space-y-4 max-h-[70vh] overflow-y-auto">
              {saveError && (
                <div className="bg-red-50 border border-red-200 text-red-600 rounded-lg px-3 py-2 text-sm">{saveError}</div>
              )}

              <FormField label="Final Diagnosis">
                <textarea className={inp} rows={2} value={form.finalDiagnosis ?? ''}
                  onChange={e => setForm(f => ({ ...f, finalDiagnosis: e.target.value }))} />
              </FormField>

              <FormField label="Treatment Summary">
                <textarea className={inp} rows={4} value={form.treatmentSummary ?? ''}
                  onChange={e => setForm(f => ({ ...f, treatmentSummary: e.target.value }))} />
              </FormField>

              <FormField label="Procedures Performed">
                <textarea className={inp} rows={3} value={form.proceduresPerformed ?? ''}
                  onChange={e => setForm(f => ({ ...f, proceduresPerformed: e.target.value }))} />
              </FormField>

              <FormField label="Discharge Medications">
                <textarea className={inp} rows={3} value={form.dischargeMedications ?? ''}
                  onChange={e => setForm(f => ({ ...f, dischargeMedications: e.target.value }))} />
              </FormField>

              <FormField label="Condition at Discharge">
                <div className="flex flex-wrap gap-2">
                  <button
                    onClick={() => setForm(f => ({ ...f, dischargeCondition: '' }))}
                    className={`px-3 py-1.5 rounded-lg text-sm border-2 transition-colors ${
                      !form.dischargeCondition
                        ? 'border-gray-400 bg-gray-100 text-gray-700 font-medium'
                        : 'border-gray-200 text-gray-500 hover:border-gray-300'
                    }`}>
                    Not set
                  </button>
                  {DISCHARGE_CONDITIONS.map(c => (
                    <button key={c}
                      onClick={() => setForm(f => ({ ...f, dischargeCondition: c }))}
                      className={`px-3 py-1.5 rounded-lg text-sm border-2 transition-colors ${
                        form.dischargeCondition === c
                          ? 'border-blue-500 bg-blue-50 text-blue-700 font-medium'
                          : 'border-gray-200 text-gray-600 hover:border-gray-300'
                      }`}>
                      {c}
                    </button>
                  ))}
                </div>
              </FormField>

              <FormField label="Follow-up Instructions">
                <textarea className={inp} rows={3} value={form.followUpInstructions ?? ''}
                  onChange={e => setForm(f => ({ ...f, followUpInstructions: e.target.value }))} />
              </FormField>

              <FormField label="Attending Physician Notes">
                <textarea className={inp} rows={3} value={form.attendingPhysicianNotes ?? ''}
                  onChange={e => setForm(f => ({ ...f, attendingPhysicianNotes: e.target.value }))} />
              </FormField>
            </div>
            <div className="px-6 py-4 border-t border-gray-100 flex justify-end gap-3">
              <button onClick={() => setEditing(false)} className="px-4 py-2 text-sm text-gray-600">Cancel</button>
              <button onClick={handleSave} disabled={saving}
                className="px-5 py-2 bg-blue-600 text-white rounded-lg hover:bg-blue-700 disabled:opacity-50 text-sm font-medium">
                {saving ? 'Saving…' : 'Save Summary'}
              </button>
            </div>
          </div>
        </div>
      )}
    </div>
  );
}

function Section({ title, text }: { title: string; text: string | null | undefined }) {
  return (
    <div className="bg-white border border-gray-200 rounded-xl overflow-hidden">
      <div className="bg-blue-50 px-5 py-2.5">
        <h3 className="text-xs font-semibold text-blue-800 uppercase tracking-wide">{title}</h3>
      </div>
      <div className="px-5 py-3 text-sm text-gray-700 min-h-[48px] whitespace-pre-wrap">
        {text || <span className="text-gray-400 italic">Not recorded</span>}
      </div>
    </div>
  );
}

function FormField({ label, children }: { label: string; children: React.ReactNode }) {
  return (
    <div>
      <label className="block text-xs font-medium text-gray-600 mb-1">{label}</label>
      {children}
    </div>
  );
}
