import { render, screen } from '@testing-library/react'
import { MemoryRouter } from 'react-router-dom'
import { afterEach, describe, expect, it, vi } from 'vitest'
import { catalogRepository } from '../../services/catalogRepository'
import { AdminTablesPage } from './AdminTablesPage'

afterEach(() => vi.restoreAllMocks())

describe('AdminTablesPage', () => {
  it('renders availability-backed table cards as read-only management data', async () => {
    vi.spyOn(catalogRepository, 'listAvailableTables').mockResolvedValue([
      { id: 4, name: 'Bàn 4', area: 'Tầng 1', capacity: 4, available: true },
    ])
    render(<MemoryRouter><AdminTablesPage /></MemoryRouter>)

    expect(await screen.findByText('Bàn 4')).toBeInTheDocument()
    expect(screen.getByText('1 BÀN')).toBeInTheDocument()
    expect(screen.getByRole('button', { name: /thêm bàn/i })).toBeDisabled()
    expect(screen.getByText('Backend chưa hỗ trợ thao tác này.')).toBeInTheDocument()
  })
})
