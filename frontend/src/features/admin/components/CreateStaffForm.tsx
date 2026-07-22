import { useState, type FormEvent } from 'react'
import type { CreateStaffInput } from '../adminTypes'

interface CreateStaffFormProps {
  submitting: boolean
  apiError?: string
  onCancel: () => void
  onSubmit: (input: CreateStaffInput) => void | Promise<void>
}

export function CreateStaffForm({ submitting, apiError, onCancel, onSubmit }: CreateStaffFormProps) {
  const [name, setName] = useState('')
  const [email, setEmail] = useState('')
  const [phoneNumber, setPhoneNumber] = useState('')
  const [password, setPassword] = useState('')
  const [address, setAddress] = useState('')
  const [validationError, setValidationError] = useState('')

  const submit = (event: FormEvent) => {
    event.preventDefault()
    const cleanName = name.trim()
    const cleanEmail = email.trim()
    const cleanPhone = phoneNumber.trim()
    if (!cleanName) return setValidationError('Vui lòng nhập họ và tên.')
    if (!/^[^\s@]+@[^\s@]+\.[^\s@]+$/.test(cleanEmail)) return setValidationError('Email không hợp lệ.')
    if (!/^[0-9+\s-]{8,20}$/.test(cleanPhone)) return setValidationError('Số điện thoại không hợp lệ.')
    if (password.length < 8) return setValidationError('Mật khẩu phải có ít nhất 8 ký tự.')
    setValidationError('')
    void onSubmit({ name: cleanName, email: cleanEmail, phoneNumber: cleanPhone, password, address: address.trim() || null })
  }

  return (
    <form className="admin-form" onSubmit={submit} noValidate>
      {(validationError || apiError) && <div className="admin-form__error" role="alert">{validationError || apiError}</div>}
      <label><span>HỌ VÀ TÊN</span><input aria-label="Họ và tên" value={name} onChange={(event) => setName(event.target.value)} disabled={submitting} /></label>
      <label><span>EMAIL</span><input aria-label="Email" type="email" value={email} onChange={(event) => setEmail(event.target.value)} disabled={submitting} /></label>
      <div className="admin-form__grid">
        <label><span>SỐ ĐIỆN THOẠI</span><input aria-label="Số điện thoại" value={phoneNumber} onChange={(event) => setPhoneNumber(event.target.value)} disabled={submitting} /></label>
        <label><span>MẬT KHẨU</span><input aria-label="Mật khẩu" type="password" value={password} onChange={(event) => setPassword(event.target.value)} disabled={submitting} /></label>
      </div>
      <label><span>ĐỊA CHỈ</span><input aria-label="Địa chỉ" value={address} onChange={(event) => setAddress(event.target.value)} disabled={submitting} /></label>
      <div className="admin-form__actions"><button type="button" onClick={onCancel} disabled={submitting}>HỦY</button><button className="is-primary" type="submit" disabled={submitting}>{submitting ? 'ĐANG LƯU...' : 'LƯU NHÂN VIÊN →'}</button></div>
    </form>
  )
}
