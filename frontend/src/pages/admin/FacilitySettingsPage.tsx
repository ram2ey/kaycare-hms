import { useState, useEffect, useRef } from 'react';
import { getFacilitySettings, saveFacilitySettings, uploadLogo, deleteLogo } from '../../api/facilitySettings';
import type { FacilitySettingsResponse, SaveFacilitySettingsRequest } from '../../types/facilitySettings';

const EMPTY: SaveFacilitySettingsRequest = {
  facilityName: '', address: '', phone: '', email: '',
};

export default function FacilitySettingsPage() {
  const [settings, setSettings]   = useState<FacilitySettingsResponse | null>(null);
  const [form, setForm]           = useState<SaveFacilitySettingsRequest>(EMPTY);
  const [loading, setLoading]     = useState(true);
  const [saving, setSaving]       = useState(false);
  const [logoUploading, setLogoUploading] = useState(false);
  const [success, setSuccess]     = useState('');
  const [error, setError]         = useState('');
  const fileRef                   = useRef<HTMLInputElement>(null);

  async function load() {
    setLoading(true);
    try {
      const data = await getFacilitySettings();
      setSettings(data);
      if (data) {
        setForm({
          facilityName: data.facilityName,
          address:      data.address ?? '',
          phone:        data.phone   ?? '',
          email:        data.email   ?? '',
        });
      }
    } catch {
      setError('Failed to load settings.');
    } finally {
      setLoading(false);
    }
  }

  useEffect(() => { load(); }, []);

  async function handleSave(e: React.FormEvent) {
    e.preventDefault();
    setSaving(true);
    setSuccess('');
    setError('');
    try {
      const updated = await saveFacilitySettings({
        facilityName: form.facilityName,
        address:      form.address || undefined,
        phone:        form.phone   || undefined,
        email:        form.email   || undefined,
      });
      setSettings(updated);
      setSuccess('Settings saved.');
    } catch {
      setError('Failed to save settings.');
    } finally {
      setSaving(false);
    }
  }

  async function handleLogoUpload(e: React.ChangeEvent<HTMLInputElement>) {
    const file = e.target.files?.[0];
    if (!file) return;
    setLogoUploading(true);
    setSuccess('');
    setError('');
    try {
      const updated = await uploadLogo(file);
      setSettings(updated);
      setSuccess('Logo uploaded.');
    } catch (err) {
      const msg = (err as { response?: { data?: { error?: string } } })?.response?.data?.error ?? 'Logo upload failed.';
      setError(msg);
    } finally {
      setLogoUploading(false);
      if (fileRef.current) fileRef.current.value = '';
    }
  }

  async function handleDeleteLogo() {
    if (!confirm('Remove the facility logo?')) return;
    setLogoUploading(true);
    setSuccess('');
    setError('');
    try {
      const updated = await deleteLogo();
      setSettings(updated);
      setSuccess('Logo removed.');
    } catch {
      setError('Failed to remove logo.');
    } finally {
      setLogoUploading(false);
    }
  }

  if (loading) return <div className="p-6 text-gray-400">Loading…</div>;

  return (
    <div className="p-6 max-w-2xl">
      <div className="mb-6">
        <h2 className="text-2xl font-semibold text-gray-800">Facility Settings</h2>
        <p className="text-sm text-gray-500 mt-1">
          This information appears on all generated PDFs (invoices, prescriptions, lab reports).
        </p>
      </div>

      {success && (
        <div className="mb-4 px-4 py-3 bg-green-50 border border-green-200 text-green-700 rounded-lg text-sm">
          {success}
        </div>
      )}
      {error && (
        <div className="mb-4 px-4 py-3 bg-red-50 border border-red-200 text-red-700 rounded-lg text-sm">
          {error}
        </div>
      )}

      {/* Logo section */}
      <div className="bg-white rounded-xl border border-gray-200 p-5 mb-5">
        <h3 className="text-sm font-semibold text-gray-700 mb-3">Facility Logo</h3>
        <div className="flex items-center gap-5">
          {settings?.hasLogo && settings.logoUrl ? (
            <img
              src={settings.logoUrl}
              alt="Facility logo"
              className="h-16 w-auto object-contain border border-gray-200 rounded-lg p-1"
            />
          ) : (
            <div className="h-16 w-28 flex items-center justify-center bg-gray-50 border border-dashed border-gray-300 rounded-lg text-xs text-gray-400">
              No logo
            </div>
          )}
          <div className="flex flex-col gap-2">
            <button
              onClick={() => fileRef.current?.click()}
              disabled={logoUploading}
              className="px-4 py-2 text-sm font-medium bg-blue-700 hover:bg-blue-800 text-white rounded-lg disabled:opacity-50 transition-colors">
              {logoUploading ? 'Uploading…' : settings?.hasLogo ? 'Replace Logo' : 'Upload Logo'}
            </button>
            {settings?.hasLogo && (
              <button
                onClick={handleDeleteLogo}
                disabled={logoUploading}
                className="px-4 py-2 text-sm text-red-600 border border-red-200 rounded-lg hover:bg-red-50 disabled:opacity-50 transition-colors">
                Remove Logo
              </button>
            )}
            <p className="text-xs text-gray-400">PNG or JPEG, max 2 MB</p>
          </div>
        </div>
        <input
          ref={fileRef}
          type="file"
          accept=".png,.jpg,.jpeg"
          className="hidden"
          onChange={handleLogoUpload}
        />
      </div>

      {/* Details form */}
      <div className="bg-white rounded-xl border border-gray-200 p-5">
        <h3 className="text-sm font-semibold text-gray-700 mb-4">Facility Details</h3>
        <form onSubmit={handleSave} className="space-y-4">
          <div>
            <label className="block text-xs text-gray-600 mb-1">Facility Name *</label>
            <input
              required
              value={form.facilityName}
              onChange={e => setForm(f => ({ ...f, facilityName: e.target.value }))}
              placeholder="e.g. Accra General Hospital"
              className="w-full px-3 py-2 border border-gray-300 rounded-lg text-sm focus:outline-none focus:ring-2 focus:ring-blue-500"
            />
            <p className="text-xs text-gray-400 mt-1">Appears as the large heading on all PDFs.</p>
          </div>

          <div>
            <label className="block text-xs text-gray-600 mb-1">Address</label>
            <textarea
              rows={2}
              value={form.address ?? ''}
              onChange={e => setForm(f => ({ ...f, address: e.target.value }))}
              placeholder="e.g. 12 Hospital Road, Accra, Ghana"
              className="w-full px-3 py-2 border border-gray-300 rounded-lg text-sm focus:outline-none focus:ring-2 focus:ring-blue-500 resize-none"
            />
          </div>

          <div className="grid grid-cols-2 gap-4">
            <div>
              <label className="block text-xs text-gray-600 mb-1">Phone</label>
              <input
                value={form.phone ?? ''}
                onChange={e => setForm(f => ({ ...f, phone: e.target.value }))}
                placeholder="e.g. +233 30 123 4567"
                className="w-full px-3 py-2 border border-gray-300 rounded-lg text-sm focus:outline-none focus:ring-2 focus:ring-blue-500"
              />
            </div>
            <div>
              <label className="block text-xs text-gray-600 mb-1">Email</label>
              <input
                type="email"
                value={form.email ?? ''}
                onChange={e => setForm(f => ({ ...f, email: e.target.value }))}
                placeholder="e.g. info@hospital.gh"
                className="w-full px-3 py-2 border border-gray-300 rounded-lg text-sm focus:outline-none focus:ring-2 focus:ring-blue-500"
              />
            </div>
          </div>

          <div className="flex justify-end pt-2">
            <button
              type="submit"
              disabled={saving}
              className="px-5 py-2 text-sm font-medium bg-blue-700 hover:bg-blue-800 text-white rounded-lg disabled:opacity-50 transition-colors">
              {saving ? 'Saving…' : 'Save Settings'}
            </button>
          </div>
        </form>
      </div>

      {settings && (
        <p className="mt-3 text-xs text-gray-400 text-right">
          Last updated: {new Date(settings.updatedAt).toLocaleString('en-GB', { dateStyle: 'medium', timeStyle: 'short' })}
        </p>
      )}
    </div>
  );
}
