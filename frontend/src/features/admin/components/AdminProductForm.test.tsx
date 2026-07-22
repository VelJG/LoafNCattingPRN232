import { render, screen } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { describe, expect, it, vi } from 'vitest'
import { AdminProductForm } from './AdminProductForm'

const categories = [{ id: 2, name: 'Đồ uống' }]

describe('AdminProductForm', () => {
  it('validates required and numeric product fields', async () => {
    const submit = vi.fn()
    render(<AdminProductForm categories={categories} submitting={false} onCancel={vi.fn()} onSubmit={submit} />)

    await userEvent.click(screen.getByRole('button', { name: /lưu sản phẩm/i }))
    expect(screen.getByRole('alert')).toHaveTextContent('Vui lòng nhập tên sản phẩm')

    await userEvent.type(screen.getByLabelText('Tên sản phẩm'), 'Latte mèo')
    await userEvent.clear(screen.getByLabelText('Giá bán'))
    await userEvent.type(screen.getByLabelText('Giá bán'), '65000')
    await userEvent.clear(screen.getByLabelText('Tồn kho'))
    await userEvent.type(screen.getByLabelText('Tồn kho'), '8')
    await userEvent.selectOptions(screen.getByLabelText('Danh mục'), '2')
    await userEvent.click(screen.getByRole('button', { name: /lưu sản phẩm/i }))

    expect(submit).toHaveBeenCalledWith(expect.objectContaining({
      name: 'Latte mèo', price: 65000, unitInStock: 8, categoryId: 2, isAvailable: true,
    }))
  })

  it('rejects a discount above the selling price', async () => {
    render(<AdminProductForm categories={categories} submitting={false} onCancel={vi.fn()} onSubmit={vi.fn()} />)
    await userEvent.type(screen.getByLabelText('Tên sản phẩm'), 'Latte mèo')
    await userEvent.type(screen.getByLabelText('Giá bán'), '50000')
    await userEvent.type(screen.getByLabelText('Giá khuyến mãi'), '60000')
    await userEvent.type(screen.getByLabelText('Tồn kho'), '1')
    await userEvent.selectOptions(screen.getByLabelText('Danh mục'), '2')
    await userEvent.click(screen.getByRole('button', { name: /lưu sản phẩm/i }))
    expect(screen.getByRole('alert')).toHaveTextContent('không được cao hơn giá bán')
  })
})
