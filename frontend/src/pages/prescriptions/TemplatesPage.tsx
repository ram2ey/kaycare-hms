import { useState, useEffect } from 'react';
import { Link, useNavigate } from 'react-router-dom';
import { getMyTemplates, getSharedTemplates, deleteTemplate } from '../../api/prescriptionTemplates';
import type { PrescriptionTemplateResponse } from '../../types/prescriptionTemplates';

export default function TemplatesPage() {
  const navigate = useNavigate();
  const [mine, setMine]       = useState<PrescriptionTemplateResponse[]>([]);
  const [shared, setShared]   = useState<PrescriptionTemplateResponse[]>([]);
  const [loading, setLoading] = useState(true);

  function load() {
    setLoading(true);
    Promise.all([getMyTemplates(), getSharedTemplates()])
      .then(([m, s]) => { setMine(m); setShared(s); })
      .catch(() => {})
      .finally(() => setLoading(false));
  }

  useEffect(() => { load(); }, []);

  async function handleDelete(id: string, name: string) {
    if (!confirm(`Delete template "${name}"?`)) return;
    try {
      await deleteTemplate(id);
      load();
    } catch {
      alert('Failed to delete template.');
    }
  }

  const sharedOthers = shared.filter((s) => !mine.some((m) => m.templateId === s.templateId));

  if (loading) return <div className="p-8 text-gray-400">Loading…</div>;

  return (
    <div className="p-6 max-w-4xl">
      <div className="text-sm text-gray-500 mb-4">
        <Link to="/prescriptions" className="hover:text-blue-600">Prescriptions</Link>
        <span className="mx-2">/</span>
        <span className="text-gray-800">Templates</span>
      </div>

      <div className="flex items-center justify-between mb-6">
        <h2 className="text-2xl font-semibold text-gray-800">Prescription Templates</h2>
        <button
          onClick={() => navigate('/prescriptions/templates/new')}
          className="px-4 py-2 bg-blue-700 hover:bg-blue-800 text-white text-sm font-medium rounded-lg transition-colors"
        >
          + New Template
        </button>
      </div>

      <div className="space-y-6">
        <TemplateSection
          title="My Templates"
          templates={mine}
          editable
          onDelete={handleDelete}
        />
        <TemplateSection
          title="Shared by Others"
          templates={sharedOthers}
          editable={false}
          onDelete={handleDelete}
        />
        {mine.length === 0 && sharedOthers.length === 0 && (
          <div className="bg-white border border-gray-200 rounded-xl p-10 text-center">
            <p className="text-gray-400 text-sm">No templates yet. Create your first template to save medication sets for quick reuse.</p>
          </div>
        )}
      </div>
    </div>
  );
}

function TemplateSection({
  title, templates, editable, onDelete,
}: {
  title: string;
  templates: PrescriptionTemplateResponse[];
  editable: boolean;
  onDelete: (id: string, name: string) => void;
}) {
  if (templates.length === 0) return null;
  return (
    <section className="bg-white rounded-xl border border-gray-200 overflow-hidden">
      <div className="px-5 py-3 bg-gray-50 border-b border-gray-100">
        <h3 className="text-sm font-semibold text-gray-700">{title}</h3>
      </div>
      <div className="divide-y divide-gray-100">
        {templates.map((t) => (
          <div key={t.templateId} className="px-5 py-4 flex items-center justify-between">
            <div>
              <div className="flex items-center gap-2">
                <span className="text-sm font-medium text-gray-800">{t.name}</span>
                {t.isShared && (
                  <span className="text-xs bg-blue-100 text-blue-700 px-1.5 py-0.5 rounded font-medium">Shared</span>
                )}
              </div>
              {t.description && <p className="text-xs text-gray-400 mt-0.5">{t.description}</p>}
              <p className="text-xs text-gray-400 mt-0.5">
                {t.itemCount} medication{t.itemCount !== 1 ? 's' : ''} · By {t.createdByName}
              </p>
            </div>
            {editable && (
              <div className="flex items-center gap-2 ml-4 shrink-0">
                <Link
                  to={`/prescriptions/templates/${t.templateId}/edit`}
                  className="text-xs px-3 py-1.5 border border-gray-300 rounded-lg text-gray-600 hover:bg-gray-50 transition-colors"
                >
                  Edit
                </Link>
                <button
                  onClick={() => onDelete(t.templateId, t.name)}
                  className="text-xs px-3 py-1.5 border border-red-200 rounded-lg text-red-500 hover:bg-red-50 transition-colors"
                >
                  Delete
                </button>
              </div>
            )}
          </div>
        ))}
      </div>
    </section>
  );
}
