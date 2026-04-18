import { useState, useEffect } from 'react';
import { getMyTemplates, getSharedTemplates, getTemplate } from '../api/prescriptionTemplates';
import type { PrescriptionTemplateResponse } from '../types/prescriptionTemplates';
import type { PrescriptionItemRequest } from '../types/prescriptions';

interface Props {
  onSelect: (items: PrescriptionItemRequest[], templateName: string) => void;
  onClose: () => void;
}

export default function TemplatePickerModal({ onSelect, onClose }: Props) {
  const [mine, setMine]         = useState<PrescriptionTemplateResponse[]>([]);
  const [shared, setShared]     = useState<PrescriptionTemplateResponse[]>([]);
  const [loading, setLoading]   = useState(true);
  const [applying, setApplying] = useState<string | null>(null);

  useEffect(() => {
    Promise.all([getMyTemplates(), getSharedTemplates()])
      .then(([m, s]) => { setMine(m); setShared(s); })
      .catch(() => {})
      .finally(() => setLoading(false));
  }, []);

  async function handleSelect(templateId: string, name: string) {
    setApplying(templateId);
    try {
      const detail = await getTemplate(templateId);
      const items: PrescriptionItemRequest[] = detail.items.map((i) => ({
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
      }));
      onSelect(items, name);
    } catch {
      alert('Failed to load template.');
    } finally {
      setApplying(null);
    }
  }

  const sharedOthers = shared.filter((s) => !mine.some((m) => m.templateId === s.templateId));

  return (
    <div className="fixed inset-0 bg-black/40 flex items-center justify-center z-50">
      <div className="bg-white rounded-2xl shadow-xl p-6 w-full max-w-lg max-h-[80vh] flex flex-col">
        <div className="flex items-center justify-between mb-4">
          <h3 className="text-lg font-semibold text-gray-800">Load Template</h3>
          <button onClick={onClose} className="text-gray-400 hover:text-gray-600 text-xl leading-none">×</button>
        </div>

        {loading ? (
          <p className="text-sm text-gray-400 py-8 text-center">Loading templates…</p>
        ) : (
          <div className="overflow-y-auto flex-1 space-y-5">
            <TemplateGroup
              title="My Templates"
              templates={mine}
              applying={applying}
              onSelect={handleSelect}
            />
            {sharedOthers.length > 0 && (
              <TemplateGroup
                title="Shared Templates"
                templates={sharedOthers}
                applying={applying}
                onSelect={handleSelect}
              />
            )}
            {mine.length === 0 && sharedOthers.length === 0 && (
              <p className="text-sm text-gray-400 text-center py-6">
                No templates yet. Create one from the Templates page.
              </p>
            )}
          </div>
        )}
      </div>
    </div>
  );
}

function TemplateGroup({
  title, templates, applying, onSelect,
}: {
  title: string;
  templates: PrescriptionTemplateResponse[];
  applying: string | null;
  onSelect: (id: string, name: string) => void;
}) {
  if (templates.length === 0) return null;
  return (
    <div>
      <p className="text-xs font-semibold text-gray-500 uppercase tracking-wide mb-2">{title}</p>
      <div className="space-y-2">
        {templates.map((t) => (
          <div
            key={t.templateId}
            className="flex items-center justify-between p-3 border border-gray-200 rounded-lg hover:border-blue-300 hover:bg-blue-50 transition-colors"
          >
            <div>
              <p className="text-sm font-medium text-gray-800">{t.name}</p>
              {t.description && <p className="text-xs text-gray-400 mt-0.5">{t.description}</p>}
              <p className="text-xs text-gray-400 mt-0.5">{t.itemCount} medication{t.itemCount !== 1 ? 's' : ''} · {t.createdByName}</p>
            </div>
            <button
              onClick={() => onSelect(t.templateId, t.name)}
              disabled={applying === t.templateId}
              className="ml-3 shrink-0 px-3 py-1.5 text-xs font-medium bg-blue-600 hover:bg-blue-700 text-white rounded-lg disabled:opacity-50 transition-colors"
            >
              {applying === t.templateId ? 'Loading…' : 'Use'}
            </button>
          </div>
        ))}
      </div>
    </div>
  );
}
