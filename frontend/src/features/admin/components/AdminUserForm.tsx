import { useState, type FormEvent } from 'react'
import type { AdminUser, AdminUserInput } from '../adminTypes'

interface AdminUserFormProps {
  roles: string[]
  initial?: AdminUser | null
  submitting: boolean
  apiError?: string
  onCancel: () => void
  onSubmit: (input: AdminUserInput) => void | Promise<void>
}

export function AdminUserForm({ roles, initial, submitting, apiError, onCancel, onSubmit }: AdminUserFormProps) {
  const [name, setName] = useState(initial?.name ?? '')
  const [email, setEmail] = useState(initial?.email ?? '')
  const [phoneNumber, setPhoneNumber] = useState(initial?.phoneNumber ?? '')
  const [address, setAddress] = useState(initial?.address ?? '')
  const [avatarUrl, setAvatarUrl] = useState(initial?.avatarUrl ?? '')
  const [role, setRole] = useState(initial?.role ?? roles[0] ?? '')
  const [password, setPassword] = useState('')
  const [isActive, setIsActive] = useState(initial?.isActive ?? true)
  const [isEmailVerified, setIsEmailVerified] = useState(initial?.isEmailVerified ?? false)
  const [validationError, setValidationError] = useState('')

  const submit = (event: FormEvent) => {
    event.preventDefault()
    const cleanName = name.trim()
    const cleanEmail = email.trim().toLowerCase()
    const cleanPhone = phoneNumber.trim()
    const cleanRole = role.trim()
    const cleanPassword = password.length === 0 ? null : password

    if (!cleanName) return setValidationError('Vui lòng nhập họ và tên.')
    if (!/^[^\s@]+@[^\s@]+\.[^\s@]+$/.test(cleanEmail)) return setValidationError('Email không hợp lệ.')
    if (!/^[0-9+\s-]{8,20}$/.test(cleanPhone)) return setValidationError('Số điện thoại không hợp lệ.')
    if (!cleanRole) return setValidationError('Vui lòng chọn vai trò.')
    if (!initial && !cleanPassword) return setValidationError('Vui lòng nhập mật khẩu.')
    if (cleanPassword && cleanPassword.length < 8) return setValidationError('Mật khẩu phải có ít nhất 8 ký tự.')

    setValidationError('')
    void onSubmit({
      name: cleanName,
      email: cleanEmail,
      phoneNumber: cleanPhone,
      address: address.trim() || null,
      avatarUrl: avatarUrl.trim() || null,
      role: cleanRole,
      isActive,
      isEmailVerified,
      password: cleanPassword,
    })
  }

  return (
    <form className="admin-form" onSubmit={submit} noValidate>
      {(validationError || apiError) && <div className="admin-form__error" role="alert">{validationError || apiError}</div>}
      <label><span>HỌ VÀ TÊN</span><input aria-label="Họ và tên" value={name} onChange={(event) => setName(event.target.value)} disabled={submitting} /></label>
      <label><span>EMAIL</span><input aria-label="Email" type="email" value={email} onChange={(event) => setEmail(event.target.value)} disabled={submitting} /></label>
      <div className="admin-form__grid">
        <label><span>SỐ ĐIỆN THOẠI</span><input aria-label="Số điện thoại" value={phoneNumber} onChange={(event) => setPhoneNumber(event.target.value)} disabled={submitting} /></label>
        <label><span>VAI TRÒ</span><select aria-label="Vai trò" value={role} onChange={(event) => setRole(event.target.value)} disabled={submitting}>{roles.map((item) => <option key={item} value={item}>{item}</option>)}</select></label>
      </div>
      <label><span>{initial ? 'MẬT KHẨU MỚI (NẾU ĐỔI)' : 'MẬT KHẨU'}</span><input aria-label="Mật khẩu" type="password" value={password} onChange={(event) => setPassword(event.target.value)} disabled={submitting} placeholder={initial ? 'Để trống nếu giữ mật khẩu cũ' : undefined} /></label>
      <label><span>ĐỊA CHỈ</span><input aria-label="Địa chỉ" value={address} onChange={(event) => setAddress(event.target.value)} disabled={submitting} /></label>
      <label><span>ẢNH ĐẠI DIỆN</span><input aria-label="Ảnh đại diện" value={avatarUrl} onChange={(event) => setAvatarUrl(event.target.value)} disabled={submitting} /></label>
      <div className="admin-form__grid admin-form__checks">
        <label className="admin-form__switch"><input type="checkbox" checked={isActive} onChange={(event) => setIsActive(event.target.checked)} disabled={submitting} /><span>Tài khoản đang hoạt động</span></label>
        <label className="admin-form__switch"><input type="checkbox" checked={isEmailVerified} onChange={(event) => setIsEmailVerified(event.target.checked)} disabled={submitting} /><span>Email đã xác thực</span></label>
      </div>
      <div className="admin-form__actions"><button type="button" onClick={onCancel} disabled={submitting}>HỦY</button><button className="is-primary" type="submit" disabled={submitting}>{submitting ? 'ĐANG LƯU...' : 'LƯU NGƯỜI DÙNG →'}</button></div>
    </form>
  )
}