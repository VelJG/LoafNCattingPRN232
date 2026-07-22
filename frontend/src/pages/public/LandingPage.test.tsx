import { render, screen } from '@testing-library/react'
import { MemoryRouter } from 'react-router-dom'
import { expect, it } from 'vitest'
import { LandingPage } from './LandingPage'

it('renders the public landing conversion path', () => {
  render(
    <MemoryRouter>
      <LandingPage />
    </MemoryRouter>,
  )

  expect(screen.getByRole('heading', { level: 1 })).toHaveTextContent(
    'Chỗ ngồi ấm',
  )
  expect(screen.getByRole('link', { name: 'Đăng nhập' })).toHaveAttribute(
    'href',
    '/login',
  )
  expect(screen.getByRole('heading', { name: 'Thực đơn' })).toBeInTheDocument()
  expect(
    screen.getByRole('heading', { name: 'Nhân viên bốn chân' }),
  ).toBeInTheDocument()
  expect(screen.getByRole('link', { name: 'Tạo tài khoản' })).toHaveAttribute(
    'href',
    '/register',
  )
})
