import { useEffect, type PropsWithChildren } from 'react'
import { MdClose } from 'react-icons/md'

interface AdminDialogProps extends PropsWithChildren {
  open: boolean
  title: string
  onClose: () => void
}

export function AdminDialog({ open, title, onClose, children }: AdminDialogProps) {
  useEffect(() => {
    if (!open) return
    const closeOnEscape = (event: KeyboardEvent) => {
      if (event.key === 'Escape') onClose()
    }
    document.addEventListener('keydown', closeOnEscape)
    return () => document.removeEventListener('keydown', closeOnEscape)
  }, [onClose, open])

  if (!open) return null
  return (
    <div className="admin-dialog-backdrop" role="presentation" onMouseDown={(event) => {
      if (event.target === event.currentTarget) onClose()
    }}>
      <section className="admin-dialog" role="dialog" aria-modal="true" aria-labelledby="admin-dialog-title">
        <header>
          <h2 id="admin-dialog-title">{title}</h2>
          <button type="button" onClick={onClose} aria-label="Đóng"><MdClose aria-hidden="true" /></button>
        </header>
        <div className="admin-dialog__body">{children}</div>
      </section>
    </div>
  )
}
