import { useCallback, useEffect, useState } from 'react'
import { MdDelete, MdEdit, MdLocalCafe } from 'react-icons/md'
import { ApiError } from '../../api/httpClient'
import {
  createAdminProduct,
  deleteAdminProduct,
  listAdminProducts,
  updateAdminProduct,
} from '../../features/admin/adminApi'
import { AdminDialog } from '../../features/admin/components/AdminDialog'
import { AdminFeedback } from '../../features/admin/components/AdminFeedback'
import { AdminProductForm } from '../../features/admin/components/AdminProductForm'
import { AdminStatusChip } from '../../features/admin/components/AdminStatusChip'
import { AdminToast } from '../../features/admin/components/AdminToast'
import type { AdminProduct, AdminProductInput } from '../../features/admin/adminTypes'
import { useAuth } from '../../features/auth/useAuth'
import { catalogRepository } from '../../services/catalogRepository'
import type { Category } from '../../types/models'

function money(value: number) {
  return `${Math.round(value).toLocaleString('vi-VN')} VND`
}

export function AdminCatalogPage() {
  const token = useAuth().session?.token ?? ''
  const [products, setProducts] = useState<AdminProduct[] | null>(null)
  const [categories, setCategories] = useState<Category[]>([])
  const [error, setError] = useState(false)
  const [reloadKey, setReloadKey] = useState(0)
  const [editing, setEditing] = useState<AdminProduct | null | undefined>(undefined)
  const [deleting, setDeleting] = useState<AdminProduct | null>(null)
  const [submitting, setSubmitting] = useState(false)
  const [formError, setFormError] = useState('')
  const [toast, setToast] = useState<{ message: string; tone: 'success' | 'error' } | null>(null)
  const reload = useCallback(() => setReloadKey((value) => value + 1), [])

  useEffect(() => {
    const controller = new AbortController()
    let alive = true
    setProducts(null)
    setError(false)
    Promise.all([listAdminProducts(token, controller.signal), catalogRepository.listCategories()])
      .then(([productItems, categoryItems]) => { if (alive) { setProducts(productItems); setCategories(categoryItems) } })
      .catch(() => { if (alive) { setProducts([]); setError(true) } })
    return () => { alive = false; controller.abort() }
  }, [reloadKey, token])

  const saveProduct = async (input: AdminProductInput) => {
    setSubmitting(true)
    setFormError('')
    try {
      if (editing) {
        const updated = await updateAdminProduct(token, editing.productId, input)
        setProducts((current) => current?.map((product) => product.productId === updated.productId ? updated : product) ?? [])
        setToast({ message: `Đã cập nhật ${updated.name}`, tone: 'success' })
      } else {
        const created = await createAdminProduct(token, input)
        setProducts((current) => [created, ...(current ?? [])])
        setToast({ message: `Đã thêm ${created.name}`, tone: 'success' })
      }
      setEditing(undefined)
    } catch (caught) {
      setFormError(caught instanceof ApiError ? caught.detail : 'Không thể lưu sản phẩm.')
    } finally {
      setSubmitting(false)
    }
  }

  const confirmDelete = async () => {
    if (!deleting) return
    setSubmitting(true)
    try {
      await deleteAdminProduct(token, deleting.productId)
      setProducts((current) => current?.filter((product) => product.productId !== deleting.productId) ?? [])
      setToast({ message: `Đã xóa ${deleting.name}`, tone: 'success' })
      setDeleting(null)
    } catch (caught) {
      setToast({ message: caught instanceof ApiError ? caught.detail : 'Không thể xóa sản phẩm.', tone: 'error' })
    } finally {
      setSubmitting(false)
    }
  }

  return (
    <section className="admin-page admin-catalog-page">
      <div className="admin-page-toolbar"><span>{products?.length ?? 0} SẢN PHẨM</span><button type="button" onClick={() => { setEditing(null); setFormError('') }}>+ THÊM SẢN PHẨM</button></div>
      {error ? <AdminFeedback state="error" title="Không thể tải thực đơn" message="Vui lòng kiểm tra kết nối tới máy chủ." onRetry={reload} />
        : products === null ? <AdminFeedback state="loading" />
          : products.length === 0 ? <AdminFeedback state="empty" title="Chưa có sản phẩm" message="Thêm sản phẩm đầu tiên vào thực đơn." />
            : (
              <div className="admin-data-table admin-product-table">
                <div className="admin-data-table__head"><span>SẢN PHẨM</span><span>DANH MỤC</span><span>GIÁ</span><span>TỒN</span><span>THAO TÁC</span></div>
                {products.map((product) => (
                  <article key={product.productId}>
                    <div className="admin-product-name">
                      <span className="admin-product-thumb">{product.picture ? <img src={product.picture} alt="" /> : <MdLocalCafe aria-hidden="true" />}</span>
                      <span><strong>{product.name}</strong><AdminStatusChip value={product.isAvailable ? 'Còn bán' : 'Ngừng bán'} /></span>
                    </div>
                    <span>{product.categoryName}</span>
                    <strong>{money(product.discountPrice ?? product.price)}</strong>
                    <b>{product.unitInStock}</b>
                    <div className="admin-row-actions"><button type="button" aria-label={`Sửa ${product.name}`} onClick={() => { setEditing(product); setFormError('') }}><MdEdit /></button><button type="button" aria-label={`Xóa ${product.name}`} onClick={() => setDeleting(product)}><MdDelete /></button></div>
                  </article>
                ))}
              </div>
            )}

      <AdminDialog open={editing !== undefined} title={editing ? 'Sửa sản phẩm' : 'Thêm sản phẩm'} onClose={() => !submitting && setEditing(undefined)}>
        <AdminProductForm categories={categories} initial={editing || null} submitting={submitting} apiError={formError} onCancel={() => setEditing(undefined)} onSubmit={saveProduct} />
      </AdminDialog>
      <AdminDialog open={Boolean(deleting)} title="Xóa sản phẩm" onClose={() => !submitting && setDeleting(null)}>
        <div className="admin-confirm"><p>Bạn có chắc muốn xóa <strong>{deleting?.name}</strong>? Thao tác này không thể hoàn tác.</p><div><button type="button" onClick={() => setDeleting(null)} disabled={submitting}>HỦY</button><button className="is-danger" type="button" onClick={confirmDelete} disabled={submitting}>{submitting ? 'ĐANG XÓA...' : 'XÓA SẢN PHẨM'}</button></div></div>
      </AdminDialog>
      {toast && <AdminToast message={toast.message} tone={toast.tone} onDismiss={() => setToast(null)} />}
    </section>
  )
}
