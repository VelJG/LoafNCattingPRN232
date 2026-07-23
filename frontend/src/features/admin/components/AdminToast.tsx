import { useEffect } from 'react'
import { MdCheckCircle, MdError } from 'react-icons/md'

interface AdminToastProps {
  message: string
  tone?: 'success' | 'error'
  onDismiss: () => void
}

export function AdminToast({ message, tone = 'success', onDismiss }: AdminToastProps) {
  useEffect(() => {
    const timer = window.setTimeout(onDismiss, 2200)
    return () => window.clearTimeout(timer)
  }, [message, onDismiss])

  const Icon = tone === 'success' ? MdCheckCircle : MdError
  return (
    <div className={`admin-toast admin-toast--${tone}`} role="status">
      <Icon aria-hidden="true" />{message}
    </div>
  )
}
