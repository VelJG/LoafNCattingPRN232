import type { PropsWithChildren } from 'react'

type StatusTone = 'warning' | 'success' | 'danger' | 'info' | 'neutral'

export function StatusChip({
  tone = 'neutral',
  children,
}: PropsWithChildren<{ tone?: StatusTone }>) {
  return <span className={`status-chip status-chip--${tone}`}>{children}</span>
}
