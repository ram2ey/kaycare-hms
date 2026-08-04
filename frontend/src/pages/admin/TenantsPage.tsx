import { useEffect, useState } from 'react';
import {
  getTenants, createTenant, updateTenant, activateTenant, deactivateTenant, deleteTenant,
} from '../../api/tenants';
import { useAuth } from '../../contexts/AuthContext';
import type { TenantResponse, CreateTenantRequest, UpdateTenantRequest } from '../../types/tenants';
import { SUBSCRIPTION_PLANS, AI_TIERS } from '../../types/tenants';

const DEFAULT_CREATE: CreateTenantRequest = {
  tenantCode: '', tenantName: '', subscriptionPlan: 'Standard',
  maxUsers: 50, storageQuotaGB: 100,
  isAiEnabled: true, aiMonthlyQuota: 500, allowedAiTiers: 'Standard', customOpenRouterKey: '',
  adminEmail: '', adminFirstName: '', adminLastName: '',
};

export default function TenantsPage() {
  const { user } = useAuth();
  const [tenants, setTenants] = useState<TenantResponse[]>([]);
  const [loading, setLoading] = useState(true);
  const [modal, setModal] = useState<null | 'create' | TenantResponse>(null);
  const [createForm, setCreateForm] = useState<CreateTenantRequest>(DEFAULT_CREATE);
  const [editForm, setEditForm] = useState<UpdateTenantRequest>({
    tenantName: '', subscriptionPlan: 'Standard', maxUsers: 50, storageQuotaGB: 100,
    isAiEnabled: true, aiMonthlyQuota: 500, allowedAiTiers: 'Standard', customOpenRouterKey: '',
  });
  const [saving, setSaving] = useState(false);
  const [error, setError] = useState('');

  async function load() {
    setLoading(true);
    try { setTenants(await getTenants()); }
    finally { setLoading(false); }
  }

  useEffect(() => { load(); }, []);

  function openCreate() {
    setCreateForm(DEFAULT_CREATE); setError(''); setModal('create');
  }

  function openEdit(t: TenantResponse) {
    setEditForm({
      tenantName: t.tenantName,
      subscriptionPlan: t.subscriptionPlan,
      maxUsers: t.maxUsers,
      storageQuotaGB: t.storageQuotaGB,
      isAiEnabled: t.isAiEnabled ?? true,
      aiMonthlyQuota: t.aiMonthlyQuota ?? 500,
      allowedAiTiers: t.allowedAiTiers ?? 'Standard',
      customOpenRouterKey: t.customOpenRouterKey ?? '',
    });
    setError(''); setModal(t);
  }

  async function handleCreate() {
    if (!createForm.tenantCode.trim() || !createForm.tenantName.trim() || !createForm.adminEmail.trim()) {
      setError('Tenant code, name, and admin email are required.'); return;
    }
    setSaving(true);
    try {
      await createTenant(createForm);
      setModal(null); load();
    } catch (e: unknown) {
      const msg = (e as { response?: { data?: { message?: string } } })?.response?.data?.message;
      setError(msg ?? 'Failed to create tenant.');
    } finally { setSaving(false); }
  }

  async function handleEdit() {
    if (!editForm.tenantName.trim()) { setError('Tenant name is required.'); return; }
    setSaving(true);
    try {
      await updateTenant((modal as TenantResponse).tenantId, editForm);
      setModal(null); load();
    } catch { setError('Failed to update tenant.'); }
    finally { setSaving(false); }
  }

  async function handleToggle(t: TenantResponse) {
    const action = t.isActive ? 'deactivate' : 'activate';
    if (!confirm(`${t.isActive ? 'Deactivate' : 'Activate'} ${t.tenantName}?`)) return;
    try {
      t.isActive ? await deactivateTenant(t.tenantId) : await activateTenant(t.tenantId);
      load();
    } catch { alert(`Failed to ${action} tenant.`); }
  }

  async function handleDelete(t: TenantResponse) {
    if (!confirm(`Permanently delete "${t.tenantName}" and ALL its data? This cannot be undone.`)) return;
    if (!confirm(`Second confirmation: delete all patients, records, and users for "${t.tenantName}"?`)) return;
    try {
      await deleteTenant(t.tenantId);
      load();
    } catch { alert('Failed to delete tenant.'); }
  }

  const isOwnTenant = (t: TenantResponse) => t.tenantCode === user?.tenantCode;

  return (
    <div className="p-6 space-y-6">
      <div className="flex items-center justify-between">
        <div>
          <h1 className="text-2xl font-semibold text-gray-800">Tenants & AI Plan Management</h1>
          <p className="text-sm text-gray-500 mt-0.5">Manage facilities, user limits, AI tiers, and monthly quotas.</p>
        </div>
        <button onClick={openCreate}
          className="px-4 py-2 bg-blue-700 hover:bg-blue-800 text-white text-sm font-medium rounded-lg transition-colors">
          + New Tenant
        </button>
      </div>

      <div className="bg-white rounded-xl border border-gray-200 overflow-hidden">
        {loading ? <div className="p-8 text-center text-gray-400">Loading…</div> :
          tenants.length === 0 ? <div className="p-8 text-center text-gray-400 text-sm">No tenants yet.</div> : (
            <table className="w-full text-sm">
              <thead>
                <tr className="bg-gray-50 border-b border-gray-200 text-left">
                  {['Tenant', 'Code', 'Plan', 'AI Status & Tier', 'Monthly AI Quota', 'Users', 'Status', ''].map(h => (
                    <th key={h} className="px-5 py-3 font-medium text-gray-600 text-xs uppercase tracking-wide">{h}</th>
                  ))}
                </tr>
              </thead>
              <tbody className="divide-y divide-gray-100">
                {tenants.map(t => (
                  <tr key={t.tenantId} className="hover:bg-gray-50">
                    <td className="px-5 py-3 font-medium text-gray-900">{t.tenantName}</td>
                    <td className="px-5 py-3 font-mono text-xs text-gray-500">{t.tenantCode}</td>
                    <td className="px-5 py-3 text-gray-600">{t.subscriptionPlan}</td>
                    <td className="px-5 py-3">
                      {!t.isAiEnabled ? (
                        <span className="px-2.5 py-1 rounded-full text-xs font-semibold bg-gray-100 text-gray-600 border border-gray-200">
                          🔒 AI Disabled
                        </span>
                      ) : t.customOpenRouterKey ? (
                        <span className="px-2.5 py-1 rounded-full text-xs font-semibold bg-purple-100 text-purple-700 border border-purple-200">
                          🟣 Enterprise BYOK
                        </span>
                      ) : t.allowedAiTiers === 'Pro' ? (
                        <span className="px-2.5 py-1 rounded-full text-xs font-semibold bg-blue-100 text-blue-700 border border-blue-200">
                          🔵 Pro Vision AI
                        </span>
                      ) : (
                        <span className="px-2.5 py-1 rounded-full text-xs font-semibold bg-emerald-100 text-emerald-700 border border-emerald-200">
                          ⚪ Standard Free AI
                        </span>
                      )}
                    </td>
                    <td className="px-5 py-3">
                      {!t.isAiEnabled ? (
                        <span className="text-xs text-gray-400">—</span>
                      ) : (
                        <div className="flex flex-col gap-1">
                          <span className="text-xs font-medium text-gray-700">
                            {t.aiRequestsThisMonth ?? 0} / {t.aiMonthlyQuota ?? 500} requests
                          </span>
                          <div className="w-28 bg-gray-100 h-1.5 rounded-full overflow-hidden">
                            <div
                              className={`h-full rounded-full ${
                                ((t.aiRequestsThisMonth ?? 0) / (t.aiMonthlyQuota || 500)) > 0.9 ? 'bg-red-500' : 'bg-blue-600'
                              }`}
                              style={{ width: `${Math.min(100, (((t.aiRequestsThisMonth ?? 0) / (t.aiMonthlyQuota || 500)) * 100))}%` }}
                            />
                          </div>
                        </div>
                      )}
                    </td>
                    <td className="px-5 py-3 text-gray-600">{t.userCount} / {t.maxUsers}</td>
                    <td className="px-5 py-3">
                      <span className={`px-2 py-0.5 rounded-full text-xs font-medium ${t.isActive ? 'bg-green-100 text-green-700' : 'bg-red-100 text-red-600'}`}>
                        {t.isActive ? 'Active' : 'Inactive'}
                      </span>
                    </td>
                    <td className="px-5 py-3 flex gap-3 items-center">
                      <button onClick={() => openEdit(t)}
                        className="text-blue-600 hover:text-blue-800 text-xs font-medium">Edit AI & Plan</button>
                      {!isOwnTenant(t) && (
                        <>
                          <button onClick={() => handleToggle(t)}
                            className={`text-xs font-medium ${t.isActive ? 'text-amber-600 hover:text-amber-800' : 'text-green-600 hover:text-green-800'}`}>
                            {t.isActive ? 'Deactivate' : 'Activate'}
                          </button>
                          <button onClick={() => handleDelete(t)}
                            className="text-red-500 hover:text-red-700 text-xs font-medium">
                            Delete
                          </button>
                        </>
                      )}
                    </td>
                  </tr>
                ))}
              </tbody>
            </table>
          )}
      </div>

      {/* Create modal */}
      {modal === 'create' && (
        <div className="fixed inset-0 bg-black/40 flex items-center justify-center z-50 p-4 overflow-y-auto">
          <div className="bg-white rounded-2xl shadow-lg w-full max-w-lg p-6 space-y-5 my-4">
            <h2 className="text-lg font-semibold text-gray-900">New Tenant & AI Setup</h2>

            <div className="space-y-3">
              <p className="text-xs font-semibold text-gray-500 uppercase tracking-wide">Account & Facility Details</p>
              <div className="grid grid-cols-2 gap-3">
                <div className="col-span-2">
                  <label className="block text-xs font-medium text-gray-600 mb-1">Tenant Name *</label>
                  <input type="text" value={createForm.tenantName}
                    onChange={e => setCreateForm(f => ({ ...f, tenantName: e.target.value }))}
                    placeholder="e.g. City General Hospital"
                    className="w-full border border-gray-300 rounded-lg px-3 py-2 text-sm focus:outline-none focus:ring-2 focus:ring-blue-500" />
                </div>
                <div>
                  <label className="block text-xs font-medium text-gray-600 mb-1">Tenant Code *</label>
                  <input type="text" value={createForm.tenantCode}
                    onChange={e => setCreateForm(f => ({ ...f, tenantCode: e.target.value.toLowerCase().replace(/\s/g, '') }))}
                    placeholder="e.g. citygeneral"
                    className="w-full border border-gray-300 rounded-lg px-3 py-2 text-sm focus:outline-none focus:ring-2 focus:ring-blue-500" />
                </div>
                <div>
                  <label className="block text-xs font-medium text-gray-600 mb-1">SaaS Plan</label>
                  <select value={createForm.subscriptionPlan}
                    onChange={e => setCreateForm(f => ({ ...f, subscriptionPlan: e.target.value }))}
                    className="w-full border border-gray-300 rounded-lg px-3 py-2 text-sm focus:outline-none focus:ring-2 focus:ring-blue-500">
                    {SUBSCRIPTION_PLANS.map(p => <option key={p}>{p}</option>)}
                  </select>
                </div>
              </div>
            </div>

            {/* AI Settings Section */}
            <div className="space-y-3 border-t border-gray-100 pt-4">
              <div className="flex items-center justify-between">
                <p className="text-xs font-semibold text-purple-700 uppercase tracking-wide">🤖 AI Assistant Matrix & Quotas</p>
                <label className="flex items-center gap-2 cursor-pointer">
                  <input
                    type="checkbox"
                    checked={createForm.isAiEnabled}
                    onChange={e => setCreateForm(f => ({ ...f, isAiEnabled: e.target.checked }))}
                    className="rounded text-purple-600 focus:ring-purple-500"
                  />
                  <span className="text-xs font-medium text-gray-700">Enable AI Features</span>
                </label>
              </div>

              {createForm.isAiEnabled && (
                <div className="grid grid-cols-2 gap-3 bg-purple-50/50 p-3 rounded-xl border border-purple-100">
                  <div>
                    <label className="block text-xs font-medium text-gray-700 mb-1">AI Tier</label>
                    <select value={createForm.allowedAiTiers}
                      onChange={e => setCreateForm(f => ({ ...f, allowedAiTiers: e.target.value }))}
                      className="w-full border border-gray-300 rounded-lg px-3 py-2 text-sm focus:outline-none focus:ring-2 focus:ring-purple-500">
                      <option value="Standard">Standard (Free Models)</option>
                      <option value="Pro">Pro (Vision & Fast Commercial)</option>
                      <option value="Enterprise">Enterprise (BYOK Key)</option>
                    </select>
                  </div>
                  <div>
                    <label className="block text-xs font-medium text-gray-700 mb-1">Monthly AI Quota</label>
                    <input type="number" min={1} value={createForm.aiMonthlyQuota}
                      onChange={e => setCreateForm(f => ({ ...f, aiMonthlyQuota: parseInt(e.target.value) || 500 }))}
                      className="w-full border border-gray-300 rounded-lg px-3 py-2 text-sm focus:outline-none focus:ring-2 focus:ring-purple-500" />
                  </div>
                  {createForm.allowedAiTiers === 'Enterprise' && (
                    <div className="col-span-2">
                      <label className="block text-xs font-medium text-purple-900 mb-1">Custom OpenRouter API Key (BYOK)</label>
                      <input type="password" value={createForm.customOpenRouterKey}
                        onChange={e => setCreateForm(f => ({ ...f, customOpenRouterKey: e.target.value }))}
                        placeholder="sk-or-v1-..."
                        className="w-full border border-gray-300 rounded-lg px-3 py-2 text-sm font-mono focus:outline-none focus:ring-2 focus:ring-purple-500" />
                    </div>
                  )}
                </div>
              )}
            </div>

            <div className="space-y-1 border-t border-gray-100 pt-4">
              <p className="text-xs font-semibold text-gray-500 uppercase tracking-wide">First Admin User</p>
              <div className="grid grid-cols-2 gap-3 mt-2">
                <div className="col-span-2">
                  <label className="block text-xs font-medium text-gray-600 mb-1">Admin Email *</label>
                  <input type="email" value={createForm.adminEmail}
                    onChange={e => setCreateForm(f => ({ ...f, adminEmail: e.target.value }))}
                    placeholder="admin@hospital.com"
                    className="w-full border border-gray-300 rounded-lg px-3 py-2 text-sm focus:outline-none focus:ring-2 focus:ring-blue-500" />
                </div>
                <div>
                  <label className="block text-xs font-medium text-gray-600 mb-1">First Name</label>
                  <input type="text" value={createForm.adminFirstName}
                    onChange={e => setCreateForm(f => ({ ...f, adminFirstName: e.target.value }))}
                    className="w-full border border-gray-300 rounded-lg px-3 py-2 text-sm focus:outline-none focus:ring-2 focus:ring-blue-500" />
                </div>
                <div>
                  <label className="block text-xs font-medium text-gray-600 mb-1">Last Name</label>
                  <input type="text" value={createForm.adminLastName}
                    onChange={e => setCreateForm(f => ({ ...f, adminLastName: e.target.value }))}
                    className="w-full border border-gray-300 rounded-lg px-3 py-2 text-sm focus:outline-none focus:ring-2 focus:ring-blue-500" />
                </div>
              </div>
            </div>

            {error && <p className="text-sm text-red-600 bg-red-50 rounded-lg px-3 py-2">{error}</p>}
            <div className="flex gap-3 justify-end pt-1">
              <button onClick={() => setModal(null)} className="text-sm text-gray-600 hover:text-gray-800">Cancel</button>
              <button onClick={handleCreate} disabled={saving}
                className="px-5 py-2 bg-blue-700 hover:bg-blue-800 text-white text-sm font-medium rounded-lg transition-colors disabled:opacity-50">
                {saving ? 'Creating…' : 'Create Tenant'}
              </button>
            </div>
          </div>
        </div>
      )}

      {/* Edit modal */}
      {modal !== null && modal !== 'create' && (
        <div className="fixed inset-0 bg-black/40 flex items-center justify-center z-50 p-4">
          <div className="bg-white rounded-2xl shadow-lg w-full max-w-md p-6 space-y-4">
            <h2 className="text-lg font-semibold text-gray-900">Edit Tenant & AI — {(modal as TenantResponse).tenantName}</h2>
            <div className="grid grid-cols-2 gap-3">
              <div className="col-span-2">
                <label className="block text-xs font-medium text-gray-600 mb-1">Tenant Name *</label>
                <input type="text" value={editForm.tenantName}
                  onChange={e => setEditForm(f => ({ ...f, tenantName: e.target.value }))}
                  className="w-full border border-gray-300 rounded-lg px-3 py-2 text-sm focus:outline-none focus:ring-2 focus:ring-blue-500" />
              </div>
              <div>
                <label className="block text-xs font-medium text-gray-600 mb-1">Plan</label>
                <select value={editForm.subscriptionPlan}
                  onChange={e => setEditForm(f => ({ ...f, subscriptionPlan: e.target.value }))}
                  className="w-full border border-gray-300 rounded-lg px-3 py-2 text-sm focus:outline-none focus:ring-2 focus:ring-blue-500">
                  {SUBSCRIPTION_PLANS.map(p => <option key={p}>{p}</option>)}
                </select>
              </div>
              <div>
                <label className="block text-xs font-medium text-gray-600 mb-1">Max Users</label>
                <input type="number" min={1} value={editForm.maxUsers}
                  onChange={e => setEditForm(f => ({ ...f, maxUsers: parseInt(e.target.value) || 50 }))}
                  className="w-full border border-gray-300 rounded-lg px-3 py-2 text-sm focus:outline-none focus:ring-2 focus:ring-blue-500" />
              </div>
            </div>

            {/* AI Control Block */}
            <div className="space-y-3 border-t border-gray-100 pt-3">
              <div className="flex items-center justify-between">
                <p className="text-xs font-semibold text-purple-700 uppercase tracking-wide">🤖 AI Matrix Configuration</p>
                <label className="flex items-center gap-2 cursor-pointer">
                  <input
                    type="checkbox"
                    checked={editForm.isAiEnabled}
                    onChange={e => setEditForm(f => ({ ...f, isAiEnabled: e.target.checked }))}
                    className="rounded text-purple-600 focus:ring-purple-500"
                  />
                  <span className="text-xs font-medium text-gray-700">AI Features Enabled</span>
                </label>
              </div>

              {editForm.isAiEnabled && (
                <div className="grid grid-cols-2 gap-3 bg-purple-50/50 p-3 rounded-xl border border-purple-100">
                  <div>
                    <label className="block text-xs font-medium text-gray-700 mb-1">AI Tier</label>
                    <select value={editForm.allowedAiTiers}
                      onChange={e => setEditForm(f => ({ ...f, allowedAiTiers: e.target.value }))}
                      className="w-full border border-gray-300 rounded-lg px-3 py-2 text-sm focus:outline-none focus:ring-2 focus:ring-purple-500">
                      <option value="Standard">Standard (Free Models)</option>
                      <option value="Pro">Pro (Vision & Commercial)</option>
                      <option value="Enterprise">Enterprise (BYOK Key)</option>
                    </select>
                  </div>
                  <div>
                    <label className="block text-xs font-medium text-gray-700 mb-1">Monthly AI Quota</label>
                    <input type="number" min={1} value={editForm.aiMonthlyQuota}
                      onChange={e => setEditForm(f => ({ ...f, aiMonthlyQuota: parseInt(e.target.value) || 500 }))}
                      className="w-full border border-gray-300 rounded-lg px-3 py-2 text-sm focus:outline-none focus:ring-2 focus:ring-purple-500" />
                  </div>
                  {editForm.allowedAiTiers === 'Enterprise' && (
                    <div className="col-span-2">
                      <label className="block text-xs font-medium text-purple-900 mb-1">Custom OpenRouter Key (BYOK)</label>
                      <input type="password" value={editForm.customOpenRouterKey}
                        onChange={e => setEditForm(f => ({ ...f, customOpenRouterKey: e.target.value }))}
                        placeholder="sk-or-v1-..."
                        className="w-full border border-gray-300 rounded-lg px-3 py-2 text-sm font-mono focus:outline-none focus:ring-2 focus:ring-purple-500" />
                    </div>
                  )}
                </div>
              )}
            </div>

            {error && <p className="text-sm text-red-600 bg-red-50 rounded-lg px-3 py-2">{error}</p>}
            <div className="flex gap-3 justify-end pt-1">
              <button onClick={() => setModal(null)} className="text-sm text-gray-600 hover:text-gray-800">Cancel</button>
              <button onClick={handleEdit} disabled={saving}
                className="px-5 py-2 bg-blue-700 hover:bg-blue-800 text-white text-sm font-medium rounded-lg transition-colors disabled:opacity-50">
                {saving ? 'Saving…' : 'Save Changes'}
              </button>
            </div>
          </div>
        </div>
      )}
    </div>
  );
}
