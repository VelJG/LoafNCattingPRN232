import { useState, type ChangeEvent, type FormEvent } from 'react'
import type { Category } from '../../../types/models'
import type { AdminProduct, AdminProductInput } from '../adminTypes'

const maxImageBytes = 1024 * 1024
const allowedImageTypes = new Set(['image/jpeg', 'image/png'])

interface AdminProductFormProps {
  categories: Category[]
  initial?: AdminProduct | null
  submitting: boolean
  apiError?: string
  onCancel: () => void
  onSubmit: (input: AdminProductInput) => void | Promise<void>
  onUploadPicture?: (file: File) => Promise<string>
}

export function AdminProductForm({
  categories,
  initial,
  submitting,
  apiError,
  onCancel,
  onSubmit,
  onUploadPicture,
}: AdminProductFormProps) {
  const [name, setName] = useState(initial?.name ?? '')
  const [description, setDescription] = useState(initial?.description ?? '')
  const [price, setPrice] = useState(initial ? String(initial.price) : '')
  const [discountPrice, setDiscountPrice] = useState(initial?.discountPrice == null ? '' : String(initial.discountPrice))
  const [stock, setStock] = useState(initial ? String(initial.unitInStock) : '')
  const [picture, setPicture] = useState(initial?.pictureKey ?? initial?.picture ?? '')
  const [categoryId, setCategoryId] = useState(initial ? String(initial.categoryId) : '')
  const [isAvailable, setIsAvailable] = useState(initial?.isAvailable ?? true)
  const [validationError, setValidationError] = useState('')
  const [uploadError, setUploadError] = useState('')
  const [uploading, setUploading] = useState(false)
  const disabled = submitting || uploading

  const uploadPicture = async (event: ChangeEvent<HTMLInputElement>) => {
    const file = event.target.files?.[0]
    event.target.value = ''
    if (!file) return
    if (!onUploadPicture) return setUploadError('Chức năng upload ảnh chưa sẵn sàng.')
    if (!allowedImageTypes.has(file.type)) return setUploadError('Chỉ cho phép ảnh JPG hoặc PNG.')
    if (file.size > maxImageBytes) return setUploadError('Ảnh không được vượt quá 1 MB.')

    setUploading(true)
    setUploadError('')
    try {
      const key = await onUploadPicture(file)
      setPicture(key)
    } catch (caught) {
      setUploadError(caught instanceof Error ? caught.message : 'Không thể upload ảnh.')
    } finally {
      setUploading(false)
    }
  }

  const submit = (event: FormEvent) => {
    event.preventDefault()
    if (uploading) return
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
      {(validationError || uploadError || apiError) && <div className="admin-form__error" role="alert">{validationError || uploadError || apiError}</div>}
      <label><span>TÊN SẢN PHẨM</span><input aria-label="Tên sản phẩm" value={name} onChange={(event) => setName(event.target.value)} disabled={disabled} /></label>
      <label><span>MÔ TẢ</span><textarea aria-label="Mô tả" value={description} onChange={(event) => setDescription(event.target.value)} disabled={disabled} /></label>
      <div className="admin-form__grid">
        <label><span>DANH MỤC</span><select aria-label="Danh mục" value={categoryId} onChange={(event) => setCategoryId(event.target.value)} disabled={disabled}><option value="">Chọn danh mục</option>{categories.map((category) => <option key={category.id} value={category.id}>{category.name}</option>)}</select></label>
        <label><span>TỒN KHO</span><input aria-label="Tồn kho" inputMode="numeric" value={stock} onChange={(event) => setStock(event.target.value)} disabled={disabled} /></label>
        <label><span>GIÁ BÁN</span><input aria-label="Giá bán" inputMode="decimal" value={price} onChange={(event) => setPrice(event.target.value)} disabled={disabled} /></label>
        <label><span>GIÁ KHUYẾN MÃI</span><input aria-label="Giá khuyến mãi" inputMode="decimal" value={discountPrice} onChange={(event) => setDiscountPrice(event.target.value)} disabled={disabled} /></label>
      </div>
      <label><span>ĐƯỜNG DẪN HÌNH ẢNH</span><input aria-label="Đường dẫn hình ảnh" value={picture} onChange={(event) => setPicture(event.target.value)} disabled={disabled} /></label>
      <label>
        <span>CHỌN ẢNH TỪ MÁY</span>
        <input aria-label="Chọn ảnh sản phẩm từ máy" type="file" accept="image/png,image/jpeg" onChange={uploadPicture} disabled={disabled || !onUploadPicture} />
      </label>
      <label className="admin-form__switch"><input type="checkbox" checked={isAvailable} onChange={(event) => setIsAvailable(event.target.checked)} disabled={disabled} /><span>Đang bán sản phẩm này</span></label>
      <div className="admin-form__actions"><button type="button" onClick={onCancel} disabled={disabled}>HỦY</button><button className="is-primary" type="submit" disabled={disabled}>{uploading ? 'ĐANG UPLOAD...' : submitting ? 'ĐANG LƯU...' : 'LƯU SẢN PHẨM →'}</button></div>
    </form>
  )
}