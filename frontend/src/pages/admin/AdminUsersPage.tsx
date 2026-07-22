import { useMemo, useState } from 'react'
import { MdLockOutline, MdSearch } from 'react-icons/md'
import { ApiError } from '../../api/httpClient'
import { createStaff } from '../../features/admin/adminApi'
import { AdminDialog } from '../../features/admin/components/AdminDialog'
import { AdminStatusChip } from '../../features/admin/components/AdminStatusChip'
import { AdminToast } from '../../features/admin/components/AdminToast'
import { CreateStaffForm } from '../../features/admin/components/CreateStaffForm'
import type { CreatedStaff, CreateStaffInput } from '../../features/admin/adminTypes'
import { useAuth } from '../../features/auth/useAuth'

const unavailable = 'Backend chưa hỗ trợ thao tác này.'

export function AdminUsersPage() {
  const token = useAuth().session?.token ?? ''
  const [users, setUsers] = useState<CreatedStaff[]>([])
  const [query, setQuery] = useState('')
  const [formOpen, setFormOpen] = useState(false)
  const [submitting, setSubmitting] = useState(false)
  const [formError, setFormError] = useState('')
  const [toast, setToast] = useState<{ message: string; tone: 'success' | 'error' } | null>(null)

  const visibleUsers = useMemo(() => {
    const keyword = query.trim().toLocaleLowerCase('vi-VN')
    if (!keyword) return users
    return users.filter((user) => [user.name, user.email, user.phoneNumber].some((value) => value.toLocaleLowerCase('vi-VN').includes(keyword)))
  }, [query, users])

  const submit = async (input: CreateStaffInput) => {
    setSubmitting(true)
    setFormError('')
    try {
      const created = await createStaff(token, input)
      setUsers((current) => [created, ...current])
      setFormOpen(false)
      setToast({ message: `Đã tạo nhân viên ${created.name}`, tone: 'success' })
    } catch (caught) {
      setFormError(caught instanceof ApiError ? caught.detail : 'Không thể tạo nhân viên.')
    } finally {
      setSubmitting(false)
    }
  }

  return (
    <section className="admin-page admin-users-page">
      <div className="admin-users-toolbar">
        <label><MdSearch aria-hidden="true" /><span className="sr-only">Tìm người dùng</span><input value={query} onChange={(event) => setQuery(event.target.value)} placeholder="Tìm theo tên/email/SĐT" /></label>
        <button type="button" onClick={() => { setFormOpen(true); setFormError('') }}>+ TẠO NHÂN VIÊN</button>
      </div>
      <p className="admin-readonly-notice">Danh sách đầy đủ, đổi vai trò và khóa tài khoản: {unavailable}</p>
      <div className="admin-data-table admin-users-table">
        <div className="admin-data-table__head"><span>TÊN</span><span>LIÊN HỆ</span><span>VAI TRÒ</span><span>TRẠNG THÁI</span><span>THAO TÁC</span></div>
        {visibleUsers.length === 0 ? (
          <div className="admin-users-empty">Backend hiện chưa có API lấy danh sách người dùng. Nhân viên vừa tạo trong phiên này sẽ xuất hiện tại đây.</div>
        ) : visibleUsers.map((user) => (
          <article key={user.userId}>
            <strong>{user.name}</strong>
            <span>{user.email} · {user.phoneNumber}</span>
            <b>{user.role.toLocaleUpperCase('vi-VN')}</b>
            <AdminStatusChip value={user.isActive ? 'Hoạt động' : 'Đã khóa'} />
            <div><button type="button" disabled title={unavailable}>ĐỔI VAI TRÒ</button><button type="button" disabled aria-label={`Khóa ${user.name}`} title={unavailable}><MdLockOutline /></button></div>
          </article>
        ))}
      </div>
      <AdminDialog open={formOpen} title="Tạo nhân viên" onClose={() => !submitting && setFormOpen(false)}>
        <CreateStaffForm submitting={submitting} apiError={formError} onCancel={() => setFormOpen(false)} onSubmit={submit} />
      </AdminDialog>
      {toast && <AdminToast message={toast.message} tone={toast.tone} onDismiss={() => setToast(null)} />}
    </section>
  )
}
