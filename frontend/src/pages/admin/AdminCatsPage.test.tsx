import { render, screen } from '@testing-library/react'
import { MemoryRouter } from 'react-router-dom'
import { afterEach, describe, expect, it, vi } from 'vitest'
import { catalogRepository } from '../../services/catalogRepository'
import { AdminCatsPage } from './AdminCatsPage'

afterEach(() => vi.restoreAllMocks())

describe('AdminCatsPage', () => {
  it('renders live cat cards and truthful disabled mutations', async () => {
    vi.spyOn(catalogRepository, 'listCats').mockResolvedValue([
      { id: 1, name: 'Mochi', breed: 'Mèo Anh lông ngắn', status: 'Đang ở quán', description: '', imageUrl: '' },
    ])
    render(<MemoryRouter><AdminCatsPage /></MemoryRouter>)

    expect(await screen.findByText('Mochi')).toBeInTheDocument()
    expect(screen.getByText('1 BÉ MÈO')).toBeInTheDocument()
    expect(screen.getByRole('button', { name: /thêm mèo/i })).toBeDisabled()
    expect(screen.getByText('Backend chưa hỗ trợ thao tác này.')).toBeInTheDocument()
  })
})
