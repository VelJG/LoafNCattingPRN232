import { render, screen } from '@testing-library/react'
import { describe, expect, it } from 'vitest'
import { AdminStorePage } from './AdminStorePage'

describe('AdminStorePage', () => {
  it('renders the exact reference values as read-only data', () => {
    render(<AdminStorePage />)

    expect(screen.getByDisplayValue('Loaf’N Catting Cafe')).toHaveProperty('readOnly', true)
    expect(screen.getByDisplayValue('128 Nguyễn Huệ, Quận 1, TP.HCM')).toHaveProperty('readOnly', true)
    expect(screen.getByDisplayValue('028 3822 1188')).toHaveProperty('readOnly', true)
    expect(screen.getByDisplayValue('10.774300')).toHaveProperty('readOnly', true)
    expect(screen.getByDisplayValue('106.703600')).toHaveProperty('readOnly', true)
    expect(screen.getByRole('button', { name: /lưu thay đổi/i })).toBeDisabled()
    expect(screen.getByText('Backend chưa hỗ trợ thao tác này.')).toBeInTheDocument()
  })
})
