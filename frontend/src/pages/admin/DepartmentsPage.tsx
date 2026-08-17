import { useState, useEffect } from 'react';
import { getDepartments, renameDepartment } from '../../api/users';
import type { DepartmentSummary } from '../../types/users';

export default function DepartmentsPage() {
  const [departments, setDepartments] = useState<DepartmentSummary[]>([]);
  const [loading, setLoading]         = useState(true);
  const [error, setError]             = useState('');

  // Rename modal
  const [renameTarget, setRenameTarget] = useState<DepartmentSummary | null>(null);
  const [newName, setNewName]           = useState('');
  const [saving, setSaving]             = useState(false);
  const [actionError, setActionError]   = useState('');

  async function load() {
    setLoading(true);
    setError('');
    try {
      setDepartments(await getDepartments());
    } catch {
      setError('Failed to load departments.');
    } finally {
      setLoading(false);
    }
  }

  useEffect(() => { load(); }, []);

  function openRename(d: DepartmentSummary) {
    setRenameTarget(d);
    setNewName(d.name);
    setActionError('');
  }

  async function handleRename(e: React.FormEvent) {
    e.preventDefault();
    if (!renameTarget) return;
    const trimmed = newName.trim();
    if (!trimmed) return;
    setSaving(true);
    setActionError('');
    try {
      await renameDepartment(renameTarget.name, trimmed);
      setRenameTarget(null);
      await load();
    } catch (err) {
      const msg = (err as { response?: { data?: { error?: string } } })?.response?.data?.error ?? 'Rename failed.';
      setActionError(msg);
    } finally {
      setSaving(false);
    }
  }

  return (
    <div className="p-6 max-w-3xl">
      <div className="flex items-center justify-between mb-6">
        <div>
          <h2 className="text-2xl font-semibold text-gray-800">Departments</h2>
          <p className="text-sm text-gray-500 mt-1">
            Departments are derived from staff user assignments. Assign a department when creating or editing a user.
          </p>
        </div>
      </div>

      {actionError && (
        <div className="mb-4 text-sm text-red-600 bg-red-50 border border-red-200 rounded-lg px-4 py-3">{actionError}</div>
      )}

      {loading ? (
        <div className="text-gray-400 py-8">Loading…</div>
      ) : error ? (
        <div className="text-red-600 py-4">{error}</div>
      ) : departments.length === 0 ? (
        <div className="bg-white rounded-xl border border-gray-200 px-6 py-10 text-center text-gray-400">
          No departments yet. Assign departments to users to populate this list.
        </div>
      ) : (
        <div className="bg-white rounded-xl border border-gray-200 overflow-hidden">
          <table className="w-full text-sm">
            <thead className="border-b border-gray-100 bg-gray-50">
              <tr>
                <th className="text-left px-5 py-3 font-medium text-gray-500 text-xs uppercase">Department</th>
                <th className="text-left px-5 py-3 font-medium text-gray-500 text-xs uppercase">Staff</th>
                <th className="px-5 py-3"></th>
              </tr>
            </thead>
            <tbody className="divide-y divide-gray-50">
              {departments.map(d => (
                <tr key={d.name}>
                  <td className="px-5 py-3 font-medium text-gray-800">{d.name}</td>
                  <td className="px-5 py-3 text-gray-500">
                    <span className="inline-flex items-center justify-center min-w-[1.75rem] h-7 px-2 text-xs font-medium bg-blue-50 text-blue-700 rounded-full">
                      {d.userCount}
                    </span>
                  </td>
                  <td className="px-5 py-3 text-right">
                    <button onClick={() => openRename(d)}
                      className="text-xs text-blue-600 hover:underline">
                      Rename
                    </button>
                  </td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>
      )}

      {/* Rename modal */}
      {renameTarget && (
        <div className="fixed inset-0 bg-black/40 flex items-center justify-center z-50">
          <div className="bg-white rounded-2xl shadow-xl p-6 w-full max-w-sm">
            <h3 className="text-lg font-semibold text-gray-800 mb-1">Rename Department</h3>
            <p className="text-sm text-gray-500 mb-4">
              This will update all <strong>{renameTarget.userCount}</strong> staff member
              {renameTarget.userCount !== 1 ? 's' : ''} assigned to <strong>{renameTarget.name}</strong>.
            </p>
            <form onSubmit={handleRename} className="space-y-3">
              <div>
                <label className="block text-xs text-gray-600 mb-1">New Name *</label>
                <input
                  required
                  value={newName}
                  onChange={e => setNewName(e.target.value)}
                  className="w-full px-3 py-2 border border-gray-300 rounded-lg text-sm focus:outline-none focus:ring-2 focus:ring-blue-500"
                  autoFocus
                />
              </div>
              <div className="flex gap-3 justify-end pt-1">
                <button type="button" onClick={() => setRenameTarget(null)}
                  className="px-4 py-2 text-sm text-gray-600 border border-gray-300 rounded-lg hover:bg-gray-50">
                  Cancel
                </button>
                <button type="submit" disabled={saving || !newName.trim()}
                  className="px-4 py-2 text-sm font-medium bg-blue-700 hover:bg-blue-800 text-white rounded-lg disabled:opacity-50 transition-colors">
                  {saving ? 'Saving…' : 'Rename'}
                </button>
              </div>
            </form>
          </div>
        </div>
      )}
    </div>
  );
}
