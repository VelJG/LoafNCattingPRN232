import { useState } from 'react'
import { MdMail, MdPhone } from 'react-icons/md'
import { useNavigate } from 'react-router-dom'
import { useAuth } from '../../features/auth/useAuth'

function initials(name = '') {
  const parts = name.trim().split(/\s+/).filter(Boolean)
  if (parts.length === 0) return 'LN'
  if (parts.length === 1) return parts[0].slice(0, 2).toLocaleUpperCase('vi-VN')
  return `${parts[0][0]}${parts.at(-1)?.[0] ?? ''}`.toLocaleUpperCase('vi-VN')
}

export function ProfilePage() {
  const auth = useAuth()
  const navigate = useNavigate()
  const [loggingOut, setLoggingOut] = useState(false)
  const user = auth.session?.user

  const handleLogout = async () => {
    setLoggingOut(true)
    await auth.logout()
    navigate('/', { replace: true })
  }

  return (
    <section className="customer-v2-page profile-v2-page">
      <div className="profile-v2-hero">
        <div className="profile-v2-avatar" aria-hidden="true">{initials(user?.name)}</div>
        <div>
          <h1>{user?.name ?? 'Khách hàng'}</h1>
          <p>KHÁCH HÀNG THÂN THIẾT</p>
        </div>
      </div>
      <div className="profile-v2-details">
        <div className="profile-v2-row">
          <MdMail />
          <div><span>EMAIL</span><strong>{user?.email ?? '—'}</strong></div>
        </div>
        <div className="profile-v2-divider" />
        <div className="profile-v2-row">
          <MdPhone />
          <div><span>SỐ ĐIỆN THOẠI</span><strong>{user?.phoneNumber || '—'}</strong></div>
        </div>
      </div>
      <button className="customer-v2-outline-button" type="button" onClick={handleLogout} disabled={loggingOut}>
        {loggingOut ? 'ĐANG ĐĂNG XUẤT…' : 'ĐĂNG XUẤT'}
      </button>
    </section>
  )
}
