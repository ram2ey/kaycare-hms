import { useState, useEffect } from 'react';
import { getUsers, createUser, updateUser, deactivateUser, reactivateUser, resetPassword, getDepartments } from '../../api/users';
import type { UserResponse, CreateUserRequest, UpdateUserRequest } from '../../types/users';
import { ROLE_OPTIONS, ROLE_COLORS } from '../../types/users';
import { ConfirmDialog } from '../../components/ConfirmDialog';

const EMPTY_CREATE: CreateUserRequest = {
  email: '', firstName: '', lastName: '', roleId: 2, password: '',
};

export default function UsersPage() {
  const [users, setUsers] = useState<UserResponse[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState('');
  const [includeInactive, setIncludeInactive] = useState(false);
  const [roleFilter, setRoleFilter] = useState('');
  const [acting, setActing]             = useState('');
  const [deptOptions, setDeptOptions]   = useState<string[]>([]);
  const [actionError, setActionError]     = useState('');
  const [actionSuccess, setActionSuccess] = useState('');
  const [confirmAction, setConfirmAction] = useState<null | { message: string; run: () => void }>(null);

  // Create modal
  const [showCreate, setShowCreate] = useState(false);
  const [createForm, setCreateForm] = useState<CreateUserRequest>(EMPTY_CREATE);

  // Edit modal
  const [editTarget, setEditTarget] = useState<UserResponse | null>(null);
  const [editForm, setEditForm] = useState<UpdateUserRequest>({ firstName: '', lastName: '', roleId: 2 });

  // Reset password modal
  const [resetTarget, setResetTarget] = useState<UserResponse | null>(null);
  const [newPassword, setNewPassword] = useState('');

  async function load() {
    setLoading(true);
    try {
      setUsers(await getUsers({ includeInactive, role: roleFilter || undefined }));
    } catch {
      setError('Failed to load users.');
    } finally {
      setLoading(false);
    }
  }

  useEffect(() => { load(); }, [includeInactive, roleFilter]);

  useEffect(() => {
    getDepartments().then(list => setDeptOptions(list.map(d => d.name))).catch(() => setActionError('Failed to load departments list. Department autocomplete may be incomplete.'));
  }, []);

  function errMsg(err: unknown) {
    return (err as { response?: { data?: { error?: string } } })?.response?.data?.error ?? 'Action failed.';
  }

  async function handleCreate(e: React.FormEvent) {
    e.preventDefault();
    setActing('create');
    setActionError('');
    setActionSuccess('');
    try {
      await createUser(createForm);
      setShowCreate(false);
      setCreateForm(EMPTY_CREATE);
      await load();
    } catch (err) {
      setActionError(errMsg(err));
    } finally {
      setActing('');
    }
  }

  async function handleEdit(e: React.FormEvent) {
    e.preventDefault();
    if (!editTarget) return;
    setActing('edit');
    setActionError('');
    setActionSuccess('');
    try {
      await updateUser(editTarget.userId, editForm);
      setEditTarget(null);
      await load();
    } catch (err) {
      setActionError(errMsg(err));
    } finally {
      setActing('');
    }
  }

  async function handleDeactivate(u: UserResponse) {
    setActing(`deactivate-${u.userId}`);
    setActionError('');
    setActionSuccess('');
    try {
      await deactivateUser(u.userId);
      await load();
    } catch (err) {
      setActionError(errMsg(err));
    } finally {
      setActing('');
    }
  }

  async function handleReactivate(u: UserResponse) {
    setActing(`reactivate-${u.userId}`);
    setActionError('');
    setActionSuccess('');
    try {
      await reactivateUser(u.userId);
      await load();
    } catch (err) {
      setActionError(errMsg(err));
    } finally {
      setActing('');
    }
  }

  async function handleResetPassword(e: React.FormEvent) {
    e.preventDefault();
    if (!resetTarget) return;
    setActing('reset');
    setActionError('');
    setActionSuccess('');
    try {
      await resetPassword(resetTarget.userId, { newPassword });
      setActionSuccess(`Password reset for ${resetTarget.fullName}. They will be prompted to change it on next login.`);
      setResetTarget(null);
      setNewPassword('');
    } catch (err) {
      setActionError(errMsg(err));
    } finally {
      setActing('');
    }
  }

  const filtered = users.filter(u =>
    !roleFilter || u.role === roleFilter
  );

  return (
    <div className="p-6 max-w-6xl">
      <div className="flex items-center justify-between mb-6">
        <h2 className="text-2xl font-semibold text-gray-800">Staff Users</h2>
        <button onClick={() => { setCreateForm(EMPTY_CREATE); setShowCreate(true); }}
          className="px-4 py-2 bg-blue-700 hover:bg-blue-800 text-white text-sm font-medium rounded-lg transition-colors">
          + New User
        </button>
      </div>

      {actionError && (
        <div className="mb-4 text-sm text-red-600 bg-red-50 border border-red-200 rounded-lg px-4 py-3">{actionError}</div>
      )}
      {actionSuccess && (
        <div className="mb-4 text-sm text-green-700 bg-green-50 border border-green-200 rounded-lg px-4 py-3">{actionSuccess}</div>
      )}

      {/* Filters */}
      <div className="flex items-center gap-3 mb-5">
        <select value={roleFilter} onChange={e => setRoleFilter(e.target.value)}
          className="px-3 py-2 border border-gray-300 rounded-lg text-sm focus:outline-none focus:ring-2 focus:ring-blue-500">
          <option value="">All Roles</option>
          {ROLE_OPTIONS.map(r => <option key={r.id} value={r.name}>{r.name}</option>)}
        </select>
        <label className="flex items-center gap-2 text-sm text-gray-600 cursor-pointer">
          <input type="checkbox" checked={includeInactive} onChange={e => setIncludeInactive(e.target.checked)}
            className="rounded" />
          Show inactive
        </label>
        <span className="text-xs text-gray-400 ml-auto">{filtered.length} user{filtered.length !== 1 ? 's' : ''}</span>
      </div>

      {loading ? (
        <div className="text-gray-400 py-8">Loading…</div>
      ) : error ? (
        <div className="text-red-600 py-4">{error}</div>
      ) : (
        <div className="bg-white rounded-xl border border-gray-200 overflow-hidden">
          <table className="w-full text-sm">
            <thead className="border-b border-gray-100 bg-gray-50">
              <tr>
                <th className="text-left px-5 py-3 font-medium text-gray-500 text-xs uppercase">Name</th>
                <th className="text-left px-5 py-3 font-medium text-gray-500 text-xs uppercase">Email</th>
                <th className="text-left px-5 py-3 font-medium text-gray-500 text-xs uppercase">Role</th>
                <th className="text-left px-5 py-3 font-medium text-gray-500 text-xs uppercase">Department</th>
                <th className="text-left px-5 py-3 font-medium text-gray-500 text-xs uppercase">Last Login</th>
                <th className="text-left px-5 py-3 font-medium text-gray-500 text-xs uppercase">Status</th>
                <th className="px-5 py-3"></th>
              </tr>
            </thead>
            <tbody className="divide-y divide-gray-50">
              {filtered.length === 0 ? (
                <tr>
                  <td colSpan={7} className="px-5 py-8 text-center text-gray-400">No users found.</td>
                </tr>
              ) : filtered.map(u => (
                <tr key={u.userId} className={u.isActive ? '' : 'opacity-50'}>
                  <td className="px-5 py-3 font-medium text-gray-800">
                    {u.fullName}
                    {u.mustChangePassword && (
                      <span title="Must change password" className="ml-2 text-xs bg-yellow-100 text-yellow-700 px-1.5 py-0.5 rounded">
                        pwd reset
                      </span>
                    )}
                  </td>
                  <td className="px-5 py-3 text-gray-600">{u.email}</td>
                  <td className="px-5 py-3">
                    <span className={`text-xs font-medium px-2 py-0.5 rounded-full ${ROLE_COLORS[u.role] ?? 'bg-gray-100 text-gray-600'}`}>
                      {u.role}
                    </span>
                  </td>
                  <td className="px-5 py-3 text-gray-500">{u.department ?? '—'}</td>
                  <td className="px-5 py-3 text-gray-400 text-xs">
                    {u.lastLoginAt
                      ? new Date(u.lastLoginAt).toLocaleDateString('en-GB', { day: 'numeric', month: 'short', year: 'numeric' })
                      : 'Never'}
                  </td>
                  <td className="px-5 py-3">
                    <span className={`text-xs font-medium px-2 py-0.5 rounded-full ${u.isActive ? 'bg-green-100 text-green-700' : 'bg-gray-100 text-gray-500'}`}>
                      {u.isActive ? 'Active' : 'Inactive'}
                    </span>
                  </td>
                  <td className="px-5 py-3 text-right space-x-3 whitespace-nowrap">
                    <button onClick={() => { setEditTarget(u); setEditForm({ firstName: u.firstName, lastName: u.lastName, roleId: u.roleId, phoneNumber: u.phoneNumber ?? undefined, licenseNumber: u.licenseNumber ?? undefined, department: u.department ?? undefined }); }}
                      className="text-xs text-blue-600 hover:underline">Edit</button>
                    <button onClick={() => { setResetTarget(u); setNewPassword(''); }}
                      className="text-xs text-orange-600 hover:underline">Reset Password</button>
                    {u.isActive ? (
                      <button onClick={() => setConfirmAction({ message: `Deactivate ${u.fullName}? They will no longer be able to log in.`, run: () => handleDeactivate(u) })} disabled={acting === `deactivate-${u.userId}`}
                        className="text-xs text-red-500 hover:underline disabled:opacity-50">Deactivate</button>
                    ) : (
                      <button onClick={() => handleReactivate(u)} disabled={acting === `reactivate-${u.userId}`}
                        className="text-xs text-green-600 hover:underline disabled:opacity-50">Reactivate</button>
                    )}
                  </td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>
      )}

      {/* Create User modal */}
      {showCreate && (
        <div className="fixed inset-0 bg-black/40 flex items-center justify-center z-50">
          <div className="bg-white rounded-2xl shadow-xl p-6 w-full max-w-lg">
            <h3 className="text-lg font-semibold text-gray-800 mb-4">New Staff User</h3>
            <form onSubmit={handleCreate} className="space-y-3">
              <div className="grid grid-cols-2 gap-3">
                <div>
                  <label className="block text-xs text-gray-600 mb-1">First Name *</label>
                  <input required value={createForm.firstName}
                    onChange={e => setCreateForm(f => ({ ...f, firstName: e.target.value }))}
                    className="w-full px-3 py-2 border border-gray-300 rounded-lg text-sm focus:outline-none focus:ring-2 focus:ring-blue-500" />
                </div>
                <div>
                  <label className="block text-xs text-gray-600 mb-1">Last Name *</label>
                  <input required value={createForm.lastName}
                    onChange={e => setCreateForm(f => ({ ...f, lastName: e.target.value }))}
                    className="w-full px-3 py-2 border border-gray-300 rounded-lg text-sm focus:outline-none focus:ring-2 focus:ring-blue-500" />
                </div>
              </div>
              <div>
                <label className="block text-xs text-gray-600 mb-1">Email *</label>
                <input required type="email" value={createForm.email}
                  onChange={e => setCreateForm(f => ({ ...f, email: e.target.value }))}
                  className="w-full px-3 py-2 border border-gray-300 rounded-lg text-sm focus:outline-none focus:ring-2 focus:ring-blue-500" />
              </div>
              <div className="grid grid-cols-2 gap-3">
                <div>
                  <label className="block text-xs text-gray-600 mb-1">Role *</label>
                  <select required value={createForm.roleId}
                    onChange={e => setCreateForm(f => ({ ...f, roleId: Number(e.target.value) }))}
                    className="w-full px-3 py-2 border border-gray-300 rounded-lg text-sm focus:outline-none focus:ring-2 focus:ring-blue-500">
                    {ROLE_OPTIONS.map(r => <option key={r.id} value={r.id}>{r.name}</option>)}
                  </select>
                </div>
                <div>
                  <label className="block text-xs text-gray-600 mb-1">Department</label>
                  <input value={createForm.department ?? ''}
                    onChange={e => setCreateForm(f => ({ ...f, department: e.target.value || undefined }))}
                    list="dept-list"
                    placeholder="e.g. General, Paediatrics…"
                    className="w-full px-3 py-2 border border-gray-300 rounded-lg text-sm focus:outline-none focus:ring-2 focus:ring-blue-500" />
                </div>
              </div>
              <div className="grid grid-cols-2 gap-3">
                <div>
                  <label className="block text-xs text-gray-600 mb-1">Phone</label>
                  <input value={createForm.phoneNumber ?? ''}
                    onChange={e => setCreateForm(f => ({ ...f, phoneNumber: e.target.value || undefined }))}
                    className="w-full px-3 py-2 border border-gray-300 rounded-lg text-sm focus:outline-none focus:ring-2 focus:ring-blue-500" />
                </div>
                <div>
                  <label className="block text-xs text-gray-600 mb-1">License Number</label>
                  <input value={createForm.licenseNumber ?? ''}
                    onChange={e => setCreateForm(f => ({ ...f, licenseNumber: e.target.value || undefined }))}
                    className="w-full px-3 py-2 border border-gray-300 rounded-lg text-sm focus:outline-none focus:ring-2 focus:ring-blue-500" />
                </div>
              </div>
              <div>
                <label className="block text-xs text-gray-600 mb-1">Initial Password * <span className="text-gray-400">(user must change on first login)</span></label>
                <input required type="password" minLength={8} value={createForm.password}
                  onChange={e => setCreateForm(f => ({ ...f, password: e.target.value }))}
                  className="w-full px-3 py-2 border border-gray-300 rounded-lg text-sm focus:outline-none focus:ring-2 focus:ring-blue-500" />
              </div>
              <div className="flex gap-3 justify-end pt-2">
                <button type="button" onClick={() => setShowCreate(false)}
                  className="px-4 py-2 text-sm text-gray-600 border border-gray-300 rounded-lg hover:bg-gray-50">Cancel</button>
                <button type="submit" disabled={acting === 'create'}
                  className="px-4 py-2 text-sm font-medium bg-blue-700 hover:bg-blue-800 text-white rounded-lg disabled:opacity-50 transition-colors">
                  {acting === 'create' ? 'Creating…' : 'Create User'}
                </button>
              </div>
            </form>
          </div>
        </div>
      )}

      {/* Edit User modal */}
      {editTarget && (
        <div className="fixed inset-0 bg-black/40 flex items-center justify-center z-50">
          <div className="bg-white rounded-2xl shadow-xl p-6 w-full max-w-lg">
            <h3 className="text-lg font-semibold text-gray-800 mb-1">Edit User</h3>
            <p className="text-sm text-gray-500 mb-4">{editTarget.email}</p>
            <form onSubmit={handleEdit} className="space-y-3">
              <div className="grid grid-cols-2 gap-3">
                <div>
                  <label className="block text-xs text-gray-600 mb-1">First Name *</label>
                  <input required value={editForm.firstName}
                    onChange={e => setEditForm(f => ({ ...f, firstName: e.target.value }))}
                    className="w-full px-3 py-2 border border-gray-300 rounded-lg text-sm focus:outline-none focus:ring-2 focus:ring-blue-500" />
                </div>
                <div>
                  <label className="block text-xs text-gray-600 mb-1">Last Name *</label>
                  <input required value={editForm.lastName}
                    onChange={e => setEditForm(f => ({ ...f, lastName: e.target.value }))}
                    className="w-full px-3 py-2 border border-gray-300 rounded-lg text-sm focus:outline-none focus:ring-2 focus:ring-blue-500" />
                </div>
              </div>
              <div className="grid grid-cols-2 gap-3">
                <div>
                  <label className="block text-xs text-gray-600 mb-1">Role *</label>
                  <select required value={editForm.roleId}
                    onChange={e => setEditForm(f => ({ ...f, roleId: Number(e.target.value) }))}
                    className="w-full px-3 py-2 border border-gray-300 rounded-lg text-sm focus:outline-none focus:ring-2 focus:ring-blue-500">
                    {ROLE_OPTIONS.map(r => <option key={r.id} value={r.id}>{r.name}</option>)}
                  </select>
                </div>
                <div>
                  <label className="block text-xs text-gray-600 mb-1">Department</label>
                  <input value={editForm.department ?? ''}
                    onChange={e => setEditForm(f => ({ ...f, department: e.target.value || undefined }))}
                    list="dept-list"
                    className="w-full px-3 py-2 border border-gray-300 rounded-lg text-sm focus:outline-none focus:ring-2 focus:ring-blue-500" />
                </div>
              </div>
              <div className="grid grid-cols-2 gap-3">
                <div>
                  <label className="block text-xs text-gray-600 mb-1">Phone</label>
                  <input value={editForm.phoneNumber ?? ''}
                    onChange={e => setEditForm(f => ({ ...f, phoneNumber: e.target.value || undefined }))}
                    className="w-full px-3 py-2 border border-gray-300 rounded-lg text-sm focus:outline-none focus:ring-2 focus:ring-blue-500" />
                </div>
                <div>
                  <label className="block text-xs text-gray-600 mb-1">License Number</label>
                  <input value={editForm.licenseNumber ?? ''}
                    onChange={e => setEditForm(f => ({ ...f, licenseNumber: e.target.value || undefined }))}
                    className="w-full px-3 py-2 border border-gray-300 rounded-lg text-sm focus:outline-none focus:ring-2 focus:ring-blue-500" />
                </div>
              </div>
              <div className="flex gap-3 justify-end pt-2">
                <button type="button" onClick={() => setEditTarget(null)}
                  className="px-4 py-2 text-sm text-gray-600 border border-gray-300 rounded-lg hover:bg-gray-50">Cancel</button>
                <button type="submit" disabled={acting === 'edit'}
                  className="px-4 py-2 text-sm font-medium bg-blue-700 hover:bg-blue-800 text-white rounded-lg disabled:opacity-50 transition-colors">
                  {acting === 'edit' ? 'Saving…' : 'Save Changes'}
                </button>
              </div>
            </form>
          </div>
        </div>
      )}

      {/* Shared datalist for department autocomplete */}
      <datalist id="dept-list">
        {deptOptions.map(d => <option key={d} value={d} />)}
      </datalist>

      {/* Reset Password modal */}
      {resetTarget && (
        <div className="fixed inset-0 bg-black/40 flex items-center justify-center z-50">
          <div className="bg-white rounded-2xl shadow-xl p-6 w-full max-w-md">
            <h3 className="text-lg font-semibold text-gray-800 mb-1">Reset Password</h3>
            <p className="text-sm text-gray-500 mb-4">
              Set a temporary password for <strong>{resetTarget.fullName}</strong>. They will be required to change it on next login.
            </p>
            <form onSubmit={handleResetPassword} className="space-y-3">
              <div>
                <label className="block text-xs text-gray-600 mb-1">New Password *</label>
                <input required type="password" minLength={8} value={newPassword}
                  onChange={e => setNewPassword(e.target.value)}
                  className="w-full px-3 py-2 border border-gray-300 rounded-lg text-sm focus:outline-none focus:ring-2 focus:ring-orange-400" />
                <p className="text-xs text-gray-400 mt-1">Minimum 8 characters</p>
              </div>
              <div className="flex gap-3 justify-end pt-2">
                <button type="button" onClick={() => setResetTarget(null)}
                  className="px-4 py-2 text-sm text-gray-600 border border-gray-300 rounded-lg hover:bg-gray-50">Cancel</button>
                <button type="submit" disabled={acting === 'reset'}
                  className="px-4 py-2 text-sm font-medium bg-orange-500 hover:bg-orange-600 text-white rounded-lg disabled:opacity-50 transition-colors">
                  {acting === 'reset' ? 'Resetting…' : 'Reset Password'}
                </button>
              </div>
            </form>
          </div>
        </div>
      )}

      <ConfirmDialog
        open={!!confirmAction}
        title="Confirm"
        message={confirmAction?.message ?? ''}
        danger
        busy={!!acting}
        onConfirm={() => { confirmAction?.run(); setConfirmAction(null); }}
        onCancel={() => setConfirmAction(null)}
      />
    </div>
  );
}
