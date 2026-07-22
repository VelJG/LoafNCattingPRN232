import { fireEvent, render, screen } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { describe, expect, it, vi } from 'vitest'
import { AdminDialog } from './AdminDialog'
import { AdminFeedback } from './AdminFeedback'
import { AdminStatusChip } from './AdminStatusChip'
import { AdminToast } from './AdminToast'

describe('admin shared components', () => {
  it('offers retry from an API error state', async () => {
    const retry = vi.fn()
    render(<AdminFeedback state="error" title="Không thể tải" message="Thử lại sau" onRetry={retry} />)

    expect(screen.getByRole('alert')).toHaveTextContent('Không thể tải')
    await userEvent.click(screen.getByRole('button', { name: /thử lại/i }))
    expect(retry).toHaveBeenCalledOnce()
  })

  it('renders an accessible dialog and closes it with Escape', () => {
    const close = vi.fn()
    render(<AdminDialog open title="Thêm sản phẩm" onClose={close}><button>Lưu</button></AdminDialog>)

    expect(screen.getByRole('dialog', { name: 'Thêm sản phẩm' })).toBeInTheDocument()
    fireEvent.keyDown(document, { key: 'Escape' })
    expect(close).toHaveBeenCalledOnce()
  })

  it.each([
    ['Đang chờ', 'warning'],
    ['Đã xác nhận', 'info'],
    ['Hoàn thành', 'success'],
    ['Đã hủy', 'danger'],
  ])('maps %s to the %s visual tone', (value, tone) => {
    render(<AdminStatusChip value={value} />)
    expect(screen.getByText(value)).toHaveClass(`admin-status--${tone}`)
  })

  it('renders toast feedback as a live status', () => {
    render(<AdminToast message="Đã lưu thay đổi" tone="success" onDismiss={vi.fn()} />)
    expect(screen.getByRole('status')).toHaveTextContent('Đã lưu thay đổi')
  })
})
