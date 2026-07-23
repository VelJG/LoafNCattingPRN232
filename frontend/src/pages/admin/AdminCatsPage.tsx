import { useCallback, useEffect, useState } from 'react'
import type { FormEvent } from 'react'
import { MdDelete, MdEdit, MdPets } from 'react-icons/md'
import { ApiError } from '../../api/httpClient'
import {
  createAdminCat,
  deleteAdminCat,
  getAdminCatOptions,
  listAdminCats,
  updateAdminCat,
} from '../../features/admin/adminApi'
import { AdminDialog } from '../../features/admin/components/AdminDialog'
import { AdminFeedback } from '../../features/admin/components/AdminFeedback'
import { AdminStatusChip } from '../../features/admin/components/AdminStatusChip'
import { AdminToast } from '../../features/admin/components/AdminToast'
import type { AdminCat, AdminCatInput, AdminCatOptions } from '../../features/admin/adminTypes'
import { useAuth } from '../../features/auth/useAuth'

interface CatFormState {
  name: string
  age: string
  gender: string
  breed: string
  picture: string
  description: string
  friendlinessRating: string
  cutenessRating: string
  playfulnessRating: string
  status: string
}

const numberOrNull = (value: string) => value.trim() ? Number(value) : null

const toInput = (form: CatFormState): AdminCatInput => ({
  name: form.name.trim(),
  age: numberOrNull(form.age),
  gender: form.gender || null,
  breed: form.breed.trim() || null,
  picture: form.picture.trim() || null,
  description: form.description.trim() || null,
  friendlinessRating: numberOrNull(form.friendlinessRating),
  cutenessRating: numberOrNull(form.cutenessRating),
  playfulnessRating: numberOrNull(form.playfulnessRating),
  status: form.status,
})

const toForm = (cat: AdminCat): CatFormState => ({
  name: cat.name,
  age: cat.age === null ? '' : String(cat.age),
  gender: cat.gender ?? '',
  breed: cat.breed ?? '',
  picture: cat.picture ?? '',
  description: cat.description ?? '',
  friendlinessRating: cat.friendlinessRating === null ? '' : String(cat.friendlinessRating),
  cutenessRating: cat.cutenessRating === null ? '' : String(cat.cutenessRating),
  playfulnessRating: cat.playfulnessRating === null ? '' : String(cat.playfulnessRating),
  status: cat.status,
})

function CatForm({
  initial,
  options,
  submitting,
  apiError,
  onCancel,
  onSubmit,
}: {
  initial: AdminCat | null
  options: AdminCatOptions
  submitting: boolean
  apiError: string
  onCancel: () => void
  onSubmit: (input: AdminCatInput) => void
}) {
  const [form, setForm] = useState<CatFormState>(() => initial ? toForm(initial) : {
    name: '',
    age: '',
    gender: '',
    breed: '',
    picture: '',
    description: '',
    friendlinessRating: '',
    cutenessRating: '',
    playfulnessRating: '',
    status: options.statuses[0] ?? '',
  })
  const [validationError, setValidationError] = useState('')

  const submit = (event: FormEvent<HTMLFormElement>) => {
    event.preventDefault()
    const input = toInput(form)
    const ratings = [input.friendlinessRating, input.cutenessRating, input.playfulnessRating]

    if (!input.name) return setValidationError('Vui lòng nhập tên mèo.')
    if (!input.status) return setValidationError('Vui lòng chọn trạng thái.')
    if (input.age !== null && (!Number.isInteger(input.age) || input.age < 0)) {
      return setValidationError('Tuổi phải là số nguyên lớn hơn hoặc bằng 0.')
    }
    if (ratings.some((rating) => rating !== null && (!Number.isInteger(rating) || rating < 1 || rating > 5))) {
      return setValidationError('Các chỉ số tính cách phải là số nguyên từ 1 đến tối đa 5.')
    }

    setValidationError('')
    onSubmit(input)
  }

  const update = (field: keyof CatFormState, value: string) => {
    setForm((current) => ({ ...current, [field]: value }))
  }

  return (
    <form className="admin-form" onSubmit={submit} noValidate>
      {(validationError || apiError) && <div className="admin-form__error" role="alert">{validationError || apiError}</div>}

      <label>
        <span>TÊN MÈO</span>
        <input aria-label="Tên mèo" value={form.name} onChange={(event) => update('name', event.target.value)} disabled={submitting} />
      </label>

      <label>
        <span>MÔ TẢ</span>
        <textarea aria-label="Mô tả" value={form.description} onChange={(event) => update('description', event.target.value)} disabled={submitting} />
      </label>

      <div className="admin-form__grid">
        <label>
          <span>TRẠNG THÁI</span>
          <select aria-label="Trạng thái" value={form.status} onChange={(event) => update('status', event.target.value)} disabled={submitting}>
            <option value="">Chọn trạng thái</option>
            {options.statuses.map((status) => <option key={status} value={status}>{status}</option>)}
          </select>
        </label>
        <label>
          <span>GIỚI TÍNH</span>
          <select aria-label="Giới tính" value={form.gender} onChange={(event) => update('gender', event.target.value)} disabled={submitting}>
            <option value="">Chưa xác định</option>
            {options.genders.map((gender) => <option key={gender} value={gender}>{gender}</option>)}
          </select>
        </label>
        <label>
          <span>TUỔI</span>
          <input aria-label="Tuổi" min="0" step="1" type="number" value={form.age} onChange={(event) => update('age', event.target.value)} disabled={submitting} />
        </label>
        <label>
          <span>GIỐNG MÈO</span>
          <input aria-label="Giống mèo" value={form.breed} onChange={(event) => update('breed', event.target.value)} disabled={submitting} />
        </label>
      </div>

      <label>
        <span>ĐƯỜNG DẪN HÌNH ẢNH</span>
        <input aria-label="Đường dẫn hình ảnh" value={form.picture} onChange={(event) => update('picture', event.target.value)} disabled={submitting} />
      </label>

      <div className="admin-form__grid">
        <label>
          <span>THÂN THIỆN (1–5)</span>
          <input aria-label="Mức độ thân thiện, tối đa 5" min="1" max="5" step="1" type="number" placeholder="Tối đa 5" value={form.friendlinessRating} onChange={(event) => update('friendlinessRating', event.target.value)} disabled={submitting} />
        </label>
        <label>
          <span>ĐÁNG YÊU (1–5)</span>
          <input aria-label="Mức độ đáng yêu, tối đa 5" min="1" max="5" step="1" type="number" placeholder="Tối đa 5" value={form.cutenessRating} onChange={(event) => update('cutenessRating', event.target.value)} disabled={submitting} />
        </label>
        <label>
          <span>TINH NGHỊCH (1–5)</span>
          <input aria-label="Mức độ tinh nghịch, tối đa 5" min="1" max="5" step="1" type="number" placeholder="Tối đa 5" value={form.playfulnessRating} onChange={(event) => update('playfulnessRating', event.target.value)} disabled={submitting} />
        </label>
      </div>

      <div className="admin-form__actions">
        <button type="button" onClick={onCancel} disabled={submitting}>HỦY</button>
        <button className="is-primary" type="submit" disabled={submitting}>{submitting ? 'ĐANG LƯU...' : 'LƯU MÈO →'}</button>
      </div>
    </form>
  )
}

export function AdminCatsPage() {
  const token = useAuth().session?.token ?? ''
  const [cats, setCats] = useState<AdminCat[] | null>(null)
  const [options, setOptions] = useState<AdminCatOptions | null>(null)
  const [error, setError] = useState(false)
  const [reloadKey, setReloadKey] = useState(0)
  const [editing, setEditing] = useState<AdminCat | null | undefined>(undefined)
  const [deleting, setDeleting] = useState<AdminCat | null>(null)
  const [submitting, setSubmitting] = useState(false)
  const [formError, setFormError] = useState('')
  const [toast, setToast] = useState<{ message: string; tone: 'success' | 'error' } | null>(null)
  const reload = useCallback(() => setReloadKey((value) => value + 1), [])

  useEffect(() => {
    const controller = new AbortController()
    let alive = true
    setCats(null)
    setOptions(null)
    setError(false)
    Promise.all([
      listAdminCats(token, controller.signal),
      getAdminCatOptions(token, controller.signal),
    ])
      .then(([items, formOptions]) => {
        if (alive) {
          setCats(items)
          setOptions(formOptions)
        }
      })
      .catch(() => {
        if (alive) {
          setCats([])
          setError(true)
        }
      })
    return () => { alive = false; controller.abort() }
  }, [reloadKey, token])

  const saveCat = async (input: AdminCatInput) => {
    setSubmitting(true)
    setFormError('')
    try {
      if (editing) {
        const updated = await updateAdminCat(token, editing.catId, input)
        setCats((current) => current?.map((cat) => cat.catId === updated.catId ? updated : cat) ?? [])
        setToast({ message: `Đã cập nhật ${updated.name}`, tone: 'success' })
      } else {
        const created = await createAdminCat(token, input)
        setCats((current) => [created, ...(current ?? [])])
        setToast({ message: `Đã thêm ${created.name}`, tone: 'success' })
      }
      setEditing(undefined)
    } catch (caught) {
      setFormError(caught instanceof ApiError ? caught.detail : 'Không thể lưu thông tin mèo.')
    } finally {
      setSubmitting(false)
    }
  }

  const confirmDelete = async () => {
    if (!deleting) return
    setSubmitting(true)
    try {
      await deleteAdminCat(token, deleting.catId)
      setCats((current) => current?.filter((cat) => cat.catId !== deleting.catId) ?? [])
      setToast({ message: `Đã xóa ${deleting.name}`, tone: 'success' })
      setDeleting(null)
    } catch (caught) {
      setToast({ message: caught instanceof ApiError ? caught.detail : 'Không thể xóa mèo.', tone: 'error' })
    } finally {
      setSubmitting(false)
    }
  }

  return (
    <section className="admin-page admin-card-page">
      <div className="admin-page-toolbar">
        <span>{cats?.length ?? 0} CATS</span>
        <button type="button" disabled={!options?.statuses.length} onClick={() => { setEditing(null); setFormError('') }}>+ ADD CAT</button>
      </div>
      {error ? <AdminFeedback state="error" title="Cannot load cats" message="Check the backend connection." onRetry={reload} />
        : cats === null ? <AdminFeedback state="loading" />
          : cats.length === 0 ? <AdminFeedback state="empty" title="No cat profiles yet" />
            : (
              <div className="admin-cats-grid">
                {cats.map((cat) => (
                  <article className="admin-cat-card" key={cat.catId}>
                    <div className="admin-cat-card__identity">
                      <span>{cat.picture ? <img src={cat.picture} alt="" /> : <MdPets aria-hidden="true" />}</span>
                      <div><h2>{cat.name}</h2><p>{(cat.breed ?? 'Unknown breed').toLocaleUpperCase('vi-VN')}</p></div>
                    </div>
                    <div className="admin-cat-card__footer">
                      <AdminStatusChip value={cat.status} />
                      <button type="button" aria-label={`Edit ${cat.name}`} onClick={() => { setEditing(cat); setFormError('') }}><MdEdit /></button>
                      <button type="button" aria-label={`Delete ${cat.name}`} onClick={() => setDeleting(cat)}><MdDelete /></button>
                    </div>
                  </article>
                ))}
              </div>
            )}

      <AdminDialog open={editing !== undefined} title={editing ? 'Edit cat' : 'Add cat'} onClose={() => !submitting && setEditing(undefined)}>
        {options && <CatForm initial={editing || null} options={options} submitting={submitting} apiError={formError} onCancel={() => setEditing(undefined)} onSubmit={saveCat} />}
      </AdminDialog>
      <AdminDialog open={Boolean(deleting)} title="Delete cat" onClose={() => !submitting && setDeleting(null)}>
        <div className="admin-confirm"><p>Delete <strong>{deleting?.name}</strong>? This cannot be undone.</p><div><button type="button" onClick={() => setDeleting(null)} disabled={submitting}>Cancel</button><button className="is-danger" type="button" onClick={confirmDelete} disabled={submitting}>{submitting ? 'Deleting...' : 'Delete cat'}</button></div></div>
      </AdminDialog>
      {toast && <AdminToast message={toast.message} tone={toast.tone} onDismiss={() => setToast(null)} />}
    </section>
  )
}
