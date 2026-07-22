import { useEffect, useState } from 'react'
import type { FormEvent } from 'react'
import { MdDelete, MdEdit, MdRefresh, MdSave } from 'react-icons/md'
import { catalogRepository, type AdminProductInput } from '../../services/catalogRepository'
import type { Product } from '../../types/models'
import { formatVnd } from '../../utils/format'

interface ProductFormState {
  name: string
  description: string
  price: string
  discountPrice: string
  unitInStock: string
  picture: string
  categoryId: string
  isAvailable: boolean
}

const emptyForm: ProductFormState = {
  name: '',
  description: '',
  price: '',
  discountPrice: '',
  unitInStock: '0',
  picture: '',
  categoryId: '1',
  isAvailable: true,
}

const toInput = (form: ProductFormState): AdminProductInput => ({
  name: form.name.trim(),
  description: form.description.trim(),
  price: Number(form.price),
  discountPrice: form.discountPrice ? Number(form.discountPrice) : undefined,
  unitInStock: Number(form.unitInStock),
  picture: form.picture.trim(),
  categoryId: Number(form.categoryId),
  isAvailable: form.isAvailable,
})

const toForm = (product: Product): ProductFormState => ({
  name: product.name,
  description: product.description,
  price: String(product.price),
  discountPrice: product.discountPrice === undefined ? '' : String(product.discountPrice),
  unitInStock: String(product.stock),
  picture: product.imageUrl,
  categoryId: String(product.apiCategoryId ?? 1),
  isAvailable: product.available,
})

export function AdminCatalogPage() {
  const [products, setProducts] = useState<Product[]>([])
  const [form, setForm] = useState<ProductFormState>(emptyForm)
  const [editingId, setEditingId] = useState<number | null>(null)
  const [loading, setLoading] = useState(true)
  const [saving, setSaving] = useState(false)
  const [message, setMessage] = useState('')
  const [error, setError] = useState('')

  const load = () => {
    setLoading(true)
    setError('')
    catalogRepository.listProducts()
      .then(setProducts)
      .catch((reason: unknown) => setError(reason instanceof Error ? reason.message : 'Cannot load products'))
      .finally(() => setLoading(false))
  }

  useEffect(load, [])

  const resetForm = () => {
    setForm(emptyForm)
    setEditingId(null)
  }

  const submit = (event: FormEvent<HTMLFormElement>) => {
    event.preventDefault()
    setSaving(true)
    setError('')
    setMessage('')
    const request = editingId === null
      ? catalogRepository.createProduct(toInput(form))
      : catalogRepository.updateProduct(editingId, toInput(form))

    request
      .then(() => {
        setMessage(editingId === null ? 'Product created.' : 'Product updated.')
        resetForm()
        load()
      })
      .catch((reason: unknown) => setError(reason instanceof Error ? reason.message : 'Cannot save product'))
      .finally(() => setSaving(false))
  }

  const remove = (product: Product) => {
    if (!window.confirm(`Delete ${product.name}?`)) return
    setError('')
    catalogRepository.deleteProduct(product.id)
      .then(() => {
        setMessage('Product deleted.')
        load()
      })
      .catch((reason: unknown) => setError(reason instanceof Error ? reason.message : 'Cannot delete product'))
  }

  return (
    <section className="admin-page">
      <div className="admin-page__heading">
        <div><span className="eyebrow">Catalog API</span><h1>Product management</h1><p>Create, update, delete, and publish menu items from the backend API.</p></div>
        <button className="button button--secondary" type="button" onClick={load}><MdRefresh />Refresh</button>
      </div>

      {(error || message) && <div className={error ? 'notice notice--error' : 'notice notice--success'}>{error || message}</div>}

      <div className="admin-grid">
        <form className="form-card catalog-form" onSubmit={submit}>
          <div className="admin-table-card__heading"><div><h2>{editingId === null ? 'New product' : 'Edit product'}</h2><p>Category ID must exist in the database.</p></div></div>
          <div className="form-grid">
            <label><span>Name</span><input required value={form.name} onChange={(event) => setForm({ ...form, name: event.target.value })} /></label>
            <label><span>Category ID</span><input required min="1" type="number" value={form.categoryId} onChange={(event) => setForm({ ...form, categoryId: event.target.value })} /></label>
            <label><span>Price</span><input required min="0" type="number" value={form.price} onChange={(event) => setForm({ ...form, price: event.target.value })} /></label>
            <label><span>Discount price</span><input min="0" type="number" value={form.discountPrice} onChange={(event) => setForm({ ...form, discountPrice: event.target.value })} /></label>
            <label><span>Stock</span><input required min="0" type="number" value={form.unitInStock} onChange={(event) => setForm({ ...form, unitInStock: event.target.value })} /></label>
            <label><span>Image URL</span><input value={form.picture} onChange={(event) => setForm({ ...form, picture: event.target.value })} /></label>
            <label className="form-grid__full"><span>Description</span><textarea rows={4} value={form.description} onChange={(event) => setForm({ ...form, description: event.target.value })} /></label>
            <label className="check-row form-grid__full"><input type="checkbox" checked={form.isAvailable} onChange={(event) => setForm({ ...form, isAvailable: event.target.checked })} /><span>Available for sale</span></label>
          </div>
          <div className="form-actions">
            <button className="button button--primary" type="submit" disabled={saving}><MdSave />{saving ? 'Saving...' : 'Save product'}</button>
            <button className="button button--secondary" type="button" onClick={resetForm}>Clear</button>
          </div>
        </form>

        <div className="admin-table-card catalog-table">
          <div className="admin-table-card__heading"><div><h2>Menu products</h2><p>{products.length} products loaded from API.</p></div></div>
          <div className="table-scroll">
            <table>
              <thead><tr><th>Name</th><th>Category</th><th>Price</th><th>Stock</th><th>Status</th><th>Actions</th></tr></thead>
              <tbody>
                {loading ? <tr><td colSpan={6}>Loading products...</td></tr> : products.map((product) => (
                  <tr key={product.id}>
                    <td><strong>{product.name}</strong></td>
                    <td>{product.categoryName}</td>
                    <td>{formatVnd(product.discountPrice ?? product.price)}</td>
                    <td>{product.stock}</td>
                    <td><span className={product.available ? 'order-status order-status--completed' : 'order-status order-status--pending'}>{product.available ? 'Available' : 'Hidden'}</span></td>
                    <td><div className="table-actions"><button type="button" onClick={() => { setEditingId(product.id); setForm(toForm(product)) }}><MdEdit />Edit</button><button type="button" onClick={() => remove(product)}><MdDelete />Delete</button></div></td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
        </div>
      </div>
    </section>
  )
}

