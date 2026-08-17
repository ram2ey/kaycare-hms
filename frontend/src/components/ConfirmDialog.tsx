import { useEffect, useRef } from 'react';

interface ConfirmDialogProps {
  open: boolean;
  title: string;
  message: string;
  confirmLabel?: string;
  danger?: boolean;
  busy?: boolean;
  onConfirm: () => void;
  onCancel: () => void;
}

/**
 * Replaces native window.confirm() for irreversible/high-stakes actions (voiding a bill,
 * processing a refund, deleting a record). Unlike confirm(), it can show the specific record or
 * amount affected and isn't a reflexively-dismissible browser dialog.
 */
export function ConfirmDialog({
  open, title, message, confirmLabel = 'Confirm', danger, busy, onConfirm, onCancel,
}: ConfirmDialogProps) {
  const titleId = 'confirm-dialog-title';
  const cancelRef = useRef<HTMLButtonElement>(null);
  const confirmRef = useRef<HTMLButtonElement>(null);

  useEffect(() => {
    if (!open) return;
    cancelRef.current?.focus();
  }, [open]);

  useEffect(() => {
    if (!open) return;

    function handleKeyDown(e: KeyboardEvent) {
      if (busy) return;

      if (e.key === 'Escape') {
        e.preventDefault();
        onCancel();
        return;
      }

      // Minimal focus trap — the dialog only ever has these two focusable elements.
      if (e.key === 'Tab') {
        const first = cancelRef.current;
        const last = confirmRef.current;
        if (!first || !last) return;

        if (e.shiftKey && document.activeElement === first) {
          e.preventDefault();
          last.focus();
        } else if (!e.shiftKey && document.activeElement === last) {
          e.preventDefault();
          first.focus();
        }
      }
    }

    document.addEventListener('keydown', handleKeyDown);
    return () => document.removeEventListener('keydown', handleKeyDown);
  }, [open, busy, onCancel]);

  if (!open) return null;

  return (
    <div
      className="fixed inset-0 bg-black/40 flex items-center justify-center z-50 p-4"
      role="dialog"
      aria-modal="true"
      aria-labelledby={titleId}
    >
      <div className="bg-white rounded-2xl shadow-lg w-full max-w-md p-6 space-y-4">
        <h3 id={titleId} className="text-lg font-semibold text-gray-900">{title}</h3>
        <p className="text-sm text-gray-600">{message}</p>
        <div className="flex gap-3 justify-end pt-1">
          <button ref={cancelRef} onClick={onCancel} disabled={busy}
            className="text-sm text-gray-600 hover:text-gray-800 disabled:opacity-50">
            Cancel
          </button>
          <button ref={confirmRef} onClick={onConfirm} disabled={busy}
            className={`px-5 py-2 text-white text-sm font-medium rounded-lg transition-colors disabled:opacity-50 ${
              danger ? 'bg-red-600 hover:bg-red-700' : 'bg-blue-700 hover:bg-blue-800'
            }`}>
            {busy ? 'Working…' : confirmLabel}
          </button>
        </div>
      </div>
    </div>
  );
}
