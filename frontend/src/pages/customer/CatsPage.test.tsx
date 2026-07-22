import { fireEvent, render, screen } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { MemoryRouter, Route, Routes } from 'react-router-dom'
import { afterEach, describe, expect, it, vi } from 'vitest'
import { catalogRepository } from '../../services/catalogRepository'
import type { CatProfile } from '../../types/models'
import { CatDetailPage } from './CatDetailPage'
import { CatsPage } from './CatsPage'

const cats: CatProfile[] = [
  {
    id: 1,
    name: 'Mochi',
    breed: 'Anh lông ngắn',
    age: 2,
    gender: 'Đực',
    status: 'Đang làm việc',
    description: 'Mochi mê ngủ trên quầy bar.',
    friendliness: 5,
    cuteness: 5,
    playfulness: 4,
    imageUrl: '/mochi.jpg',
  },
  {
    id: 2,
    name: 'Cà Rốt',
    breed: 'Munchkin',
    age: 1,
    gender: 'Cái',
    status: 'Xin nghỉ',
    description: 'Cà Rốt chân ngắn tinh nghịch.',
    friendliness: 4,
    cuteness: 5,
    playfulness: 5,
    imageUrl: '/carot.jpg',
  },
]

function renderCats(path = '/cats') {
  return render(
    <MemoryRouter initialEntries={[path]}>
      <Routes>
        <Route path="/cats" element={<CatsPage />} />
        <Route path="/cats/:catId" element={<CatDetailPage />} />
      </Routes>
    </MemoryRouter>,
  )
}

afterEach(() => vi.restoreAllMocks())

describe('customer cats screens', () => {
  it('renders the V2 cat grid and filters it with the pill search', async () => {
    vi.spyOn(catalogRepository, 'listCats').mockResolvedValue(cats)
    renderCats()

    expect(await screen.findByRole('heading', { name: 'Nhân viên bốn chân' })).toBeInTheDocument()
    expect(screen.getByRole('link', { name: /xem mochi/i })).toHaveAttribute('href', '/cats/1')
    expect(screen.getByText('Cà Rốt')).toBeInTheDocument()

    await userEvent.type(screen.getByRole('searchbox', { name: /tìm bé mèo/i }), 'mochi')
    expect(screen.getByText('Mochi')).toBeInTheDocument()
    expect(screen.queryByText('Cà Rốt')).not.toBeInTheDocument()
  })

  it('shows the reference cat detail metrics and image fallback', async () => {
    vi.spyOn(catalogRepository, 'listCats').mockResolvedValue(cats)
    renderCats('/cats/1')

    expect(await screen.findByRole('heading', { name: 'Mochi' })).toBeInTheDocument()
    expect(screen.getByText('ANH LÔNG NGẮN · ĐỰC · 2 TUỔI')).toBeInTheDocument()
    expect(screen.getByText('THÂN THIỆN')).toBeInTheDocument()
    expect(screen.getByText('4/5')).toBeInTheDocument()
    fireEvent.error(screen.getByRole('img', { name: 'Mochi' }))
    expect(screen.getByRole('img', { name: 'Ảnh minh họa Mochi' })).toBeInTheDocument()
  })

  it('offers retry when the cats API fails', async () => {
    const listCats = vi.spyOn(catalogRepository, 'listCats')
      .mockRejectedValueOnce(new Error('offline'))
      .mockResolvedValueOnce(cats)
    renderCats()

    expect(await screen.findByRole('alert')).toHaveTextContent('Không thể tải danh sách mèo')
    await userEvent.click(screen.getByRole('button', { name: /thử lại/i }))
    expect(await screen.findByText('Mochi')).toBeInTheDocument()
    expect(listCats).toHaveBeenCalledTimes(2)
  })
})
