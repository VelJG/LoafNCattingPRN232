import type { InputHTMLAttributes, ReactNode } from 'react'

interface FieldProps extends InputHTMLAttributes<HTMLInputElement> {
  id: string
  label: string
  error?: string
  hint?: string
  icon?: ReactNode
  action?: ReactNode
}

export function Field({
  id,
  label,
  error,
  hint,
  icon,
  action,
  className = '',
  ...props
}: FieldProps) {
  const messageId = `${id}-message`
  const description = error ?? hint

  return (
    <div className={`v2-field ${className}`.trim()}>
      <label htmlFor={id}>{label}</label>
      <div className="v2-field__control">
        {icon && <span className="v2-field__icon" aria-hidden="true">{icon}</span>}
        <input
          id={id}
          aria-invalid={Boolean(error)}
          aria-describedby={description ? messageId : undefined}
          {...props}
        />
        {action && <span className="v2-field__action">{action}</span>}
      </div>
      {description && (
        <small id={messageId} className={error ? 'v2-field__error' : undefined}>
          {description}
        </small>
      )}
    </div>
  )
}
