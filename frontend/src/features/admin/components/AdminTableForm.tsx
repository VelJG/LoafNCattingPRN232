import { useState, type FormEvent } from 'react'
import type { AdminTable, AdminTableInput } from '../adminTypes'

interface AdminTableFormProps {
  statuses: string[]
  initial?: AdminTable | null
  submitting: boolean
  apiError?: string
  onCancel: () => void
  onSubmit: (input: AdminTableInput) => void | Promise<void>
}

export function AdminTableForm({ statuses, initial, submitting, apiError, onCancel, onSubmit }: AdminTableFormProps) {
  const [tableName, setTableName] = useState(initial?.tableName ?? '')
  const [capacity, setCapacity] = useState(initial ? String(initial.capacity) : '')
  const [area, setArea] = useState(initial?.area ?? '')
  const [description, setDescription] = useState(initial?.description ?? '')
  const [status, setStatus] = useState(initial?.status ?? statuses[0] ?? '')
  const [validationError, setValidationError] = useState('')

  const submit = (event: FormEvent) => {
    event.preventDefault()
    const cleanName = tableName.trim()
    const numericCapacity = Number(capacity)
    const cleanStatus = status.trim()

    if (!cleanName) return setValidationError('Vui lòng nhập tên bàn.')
    if (!Number.isInteger(numericCapacity) || numericCapacity <= 0) return setValidationError('Sức chứa phải là số nguyên lớn hơn 0.')
    if (!cleanStatus) return setValidationError('Vui lòng chọn trạng thái bàn.')

    setValidationError('')
    void onSubmit({
      tableName: cleanName,
      capacity: numericCapacity,
      area: area.trim() || null,
      description: description.trim() || null,
      status: cleanStatus,
    })
  }

  return (
    <form className="admin-form" onSubmit={submit} noValidate>
      {(validationError || apiError) && <div className="admin-form__error" role="alert">{validationError || apiError}</div>}
      <label><span>TÊN BÀN</span><input aria-label="Tên bàn" value={tableName} onChange={(event) => setTableName(event.target.value)} disabled={submitting} /></label>
      <div className="admin-form__grid">
        <label><span>SỨC CHỨA</span><input aria-label="Sức chứa" inputMode="numeric" value={capacity} onChange={(event) => setCapacity(event.target.value)} disabled={submitting} /></label>
        <label><span>TRẠNG THÁI</span><select aria-label="Trạng thái" value={status} onChange={(event) => setStatus(event.target.value)} disabled={submitting}><option value="">Chọn trạng thái</option>{statuses.map((item) => <option key={item} value={item}>{item}</option>)}</select></label>
      </div>
      <label><span>KHU VỰC</span><input aria-label="Khu vực" value={area} onChange={(event) => setArea(event.target.value)} disabled={submitting} /></label>
      <label><span>MÔ TẢ</span><textarea aria-label="Mô tả" value={description} onChange={(event) => setDescription(event.target.value)} disabled={submitting} /></label>
      <div className="admin-form__actions"><button type="button" onClick={onCancel} disabled={submitting}>HỦY</button><button className="is-primary" type="submit" disabled={submitting}>{submitting ? 'ĐANG LƯU...' : 'LƯU BÀN →'}</button></div>
    </form>
  )
}