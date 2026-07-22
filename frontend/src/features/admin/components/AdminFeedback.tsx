import { MdErrorOutline, MdInbox, MdRefresh } from 'react-icons/md'

interface AdminFeedbackProps {
  state: 'loading' | 'empty' | 'error'
  title?: string
  message?: string
  onRetry?: () => void
}

export function AdminFeedback({ state, title, message, onRetry }: AdminFeedbackProps) {
  if (state === 'loading') {
    return (
      <div className="admin-feedback admin-feedback--loading" role="status" aria-label="Đang tải dữ liệu">
        <span /><span /><span />
      </div>
    )
  }

  const isError = state === 'error'
  const Icon = isError ? MdErrorOutline : MdInbox
  return (
    <div className={`admin-feedback admin-feedback--${state}`} role={isError ? 'alert' : 'status'}>
      <Icon aria-hidden="true" />
      <div>
        <h2>{title ?? (isError ? 'Không thể tải dữ liệu' : 'Chưa có dữ liệu')}</h2>
        {message && <p>{message}</p>}
      </div>
      {isError && onRetry && (
        <button type="button" onClick={onRetry}><MdRefresh aria-hidden="true" />Thử lại</button>
      )}
    </div>
  )
}
