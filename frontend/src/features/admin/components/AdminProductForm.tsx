import { useState, type FormEvent } from 'react'
import type { Category } from '../../../types/models'
import type { AdminProduct, AdminProductInput } from '../adminTypes'

interface AdminProductFormProps {
  categories: Category[]
  initial?: AdminProduct | null
  submitting: boolean
  apiError?: string
  onCancel: () => void
  onSubmit: (input: AdminProductInput) => void | Promise<void>
}

export function AdminProductForm({ categories, initial, submitting, apiError, onCancel, onSubmit }: AdminProductFormProps) {
  const [name, setName] = useState(initial?.name ?? '')
  const [description, setDescription] = useState(initial?.description ?? '')
  const [price, setPrice] = useState(initial ? String(initial.price) : '')
  const [discountPrice, setDiscountPrice] = useState(initial?.discountPrice == null ? '' : String(initial.discountPrice))
  const [stock, setStock] = useState(initial ? String(initial.unitInStock) : '')
  const [picture, setPicture] = useState(initial?.picture ?? '')
  const [categoryId, setCategoryId] = useState(initial ? String(initial.categoryId) : '')
  const [isAvailable, setIsAvailable] = useState(initial?.isAvailable ?? true)
  const [validationError, setValidationError] = useState('')

  const submit = (event: FormEvent) => {
    event.preventDefault()
    const cleanName = name.trim()
    const numericPrice = Number(price)
    const numericDiscount = discountPrice.trim() ? Number(discountPrice) : null
    const numericStock = Number(stock)
    const numericCategory = Number(categoryId)

    if (!cleanName) return setValidationError('Vui lòng nhập tên sản phẩm.')
    if (!Number.isFinite(numericPrice) || numericPrice <= 0) return setValidationError('Giá bán phải lớn hơn 0.')
    if (numericDiscount !== null && (!Number.isFinite(numericDiscount) || numericDiscount < 0)) return setValidationError('Giá khuyến mãi không hợp lệ.')
    if (numericDiscount !== null && numericDiscount > numericPrice) return setValidationError('Giá khuyến mãi không được cao hơn giá bán.')
    if (!Number.isInteger(numericStock) || numericStock < 0) return setValidationError('Tồn kho phải là số nguyên không âm.')
    if (!Number.isInteger(numericCategory) || numericCategory <= 0) return setValidationError('Vui lòng chọn danh mục.')

    setValidationError('')
    void onSubmit({
      name: cleanName,
      description: description.trim() || null,
      price: numericPrice,
      discountPrice: numericDiscount,
      unitInStock: numericStock,
      picture: picture.trim() || null,
      categoryId: numericCategory,
      isAvailable,
    })
  }

  return (
    <form className="admin-form" onSubmit={submit} noValidate>
      {(validationError || apiError) && <div className="admin-form__error" role="alert">{validationError || apiError}</div>}
      <label><span>TÊN SẢN PHẨM</span><input aria-label="Tên sản phẩm" value={name} onChange={(event) => setName(event.target.value)} disabled={submitting} /></label>
      <label><span>MÔ TẢ</span><textarea aria-label="Mô tả" value={description} onChange={(event) => setDescription(event.target.value)} disabled={submitting} /></label>
      <div className="admin-form__grid">
        <label><span>DANH MỤC</span><select aria-label="Danh mục" value={categoryId} onChange={(event) => setCategoryId(event.target.value)} disabled={submitting}><option value="">Chọn danh mục</option>{categories.map((category) => <option key={category.id} value={category.id}>{category.name}</option>)}</select></label>
        <label><span>TỒN KHO</span><input aria-label="Tồn kho" inputMode="numeric" value={stock} onChange={(event) => setStock(event.target.value)} disabled={submitting} /></label>
        <label><span>GIÁ BÁN</span><input aria-label="Giá bán" inputMode="decimal" value={price} onChange={(event) => setPrice(event.target.value)} disabled={submitting} /></label>
        <label><span>GIÁ KHUYẾN MÃI</span><input aria-label="Giá khuyến mãi" inputMode="decimal" value={discountPrice} onChange={(event) => setDiscountPrice(event.target.value)} disabled={submitting} /></label>
      </div>
      <label><span>ĐƯỜNG DẪN HÌNH ẢNH</span><input aria-label="Đường dẫn hình ảnh" value={picture} onChange={(event) => setPicture(event.target.value)} disabled={submitting} /></label>
      <label className="admin-form__switch"><input type="checkbox" checked={isAvailable} onChange={(event) => setIsAvailable(event.target.checked)} disabled={submitting} /><span>Đang bán sản phẩm này</span></label>
      <div className="admin-form__actions"><button type="button" onClick={onCancel} disabled={submitting}>HỦY</button><button className="is-primary" type="submit" disabled={submitting}>{submitting ? 'ĐANG LƯU...' : 'LƯU SẢN PHẨM →'}</button></div>
    </form>
  )
}
