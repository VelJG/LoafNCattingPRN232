import { render, screen } from '@testing-library/react'
import { MemoryRouter } from 'react-router-dom'
import { describe, expect, it } from 'vitest'
import { BrandWordmark } from '../brand/BrandWordmark'
import { Button } from './Button'
import { Field } from './Field'
import { LoadingScreen } from './LoadingScreen'
import { StatusChip } from './StatusChip'

describe('shared v2 UI', () => {
  it('associates field errors with the control', () => {
    render(
      <Field
        id="email"
        label="Email"
        type="email"
        error="Email không hợp lệ"
      />,
    )
    expect(screen.getByLabelText('Email')).toHaveAccessibleDescription(
      'Email không hợp lệ',
    )
  })

  it('renders explicit button variants and semantic status labels', () => {
    render(
      <>
        <Button variant="secondary">Quay lại</Button>
        <StatusChip tone="success">Đang hoạt động</StatusChip>
      </>,
    )
    expect(screen.getByRole('button', { name: 'Quay lại' })).toHaveClass(
      'v2-button--secondary',
    )
    expect(screen.getByText('Đang hoạt động')).toHaveClass(
      'status-chip--success',
    )
  })

  it('renders a reusable wordmark and announces session loading', () => {
    render(
      <MemoryRouter>
        <BrandWordmark />
        <LoadingScreen />
      </MemoryRouter>,
    )
    expect(screen.getAllByRole('link', { name: "Loaf'N Catting" })).toHaveLength(2)
    expect(screen.getByRole('status')).toHaveTextContent('Đang xác thực phiên')
  })
})
