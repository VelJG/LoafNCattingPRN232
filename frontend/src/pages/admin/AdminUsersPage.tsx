import { useCallback, useEffect, useMemo, useState } from 'react'
import { MdDelete, MdEdit, MdSearch } from 'react-icons/md'
import { ApiError } from '../../api/httpClient'
import {
  createAdminUser,
  deleteAdminUser,
  getAdminUserOptions,
  listAdminUsers,
  updateAdminUser,
} from '../../features/admin/adminApi'
import { AdminDialog } from '../../features/admin/components/AdminDialog'
import { AdminFeedback } from '../../features/admin/components/AdminFeedback'
import { AdminStatusChip } from '../../features/admin/components/AdminStatusChip'
import { AdminToast } from '../../features/admin/components/AdminToast'
import { AdminUserForm } from '../../features/admin/components/AdminUserForm'
import type { AdminUser, AdminUserInput } from '../../features/admin/adminTypes'
import { useAuth } from '../../features/auth/useAuth'

export function AdminUsersPage() {
  const token = useAuth().session?.token ?? ''
  const [users, setUsers] = useState<AdminUser[] | null>(null)
  const [roles, setRoles] = useState<string[]>([])
  const [query, setQuery] = useState('')
  const [error, setError] = useState(false)
  const [reloadKey, setReloadKey] = useState(0)
  const [editing, setEditing] = useState<AdminUser | null | undefined>(undefined)
  const [deleting, setDeleting] = useState<AdminUser | null>(null)
  const [submitting, setSubmitting] = useState(false)
  const [formError, setFormError] = useState('')
  const [toast, setToast] = useState<{ message: string; tone: 'success' | 'error' } | null>(null)
  const reload = useCallback(() => setReloadKey((value) => value + 1), [])

  useEffect(() => {
    const controller = new AbortController()
    let alive = true
    setUsers(null)
    setError(false)
    Promise.all([
      listAdminUsers(token, controller.signal),
      getAdminUserOptions(token, controller.signal),
    ])
      .then(([userItems, options]) => {
        if (alive) {
          setUsers(userItems)
          setRoles(options.roles)
        }
      })
      .catch(() => {
        if (alive) {
          setUsers([])
          setRoles([])
          setError(true)
        }
      })
    return () => { alive = false; controller.abort() }
  }, [reloadKey, token])

  const visibleUsers = useMemo(() => {
    const current = users ?? []
    const keyword = query.trim().toLocaleLowerCase('vi-VN')
    if (!keyword) return current
    return current.filter((user) => [
      user.name,
      user.email,
      user.phoneNumber,
      user.role,
      user.address ?? '',
    ].some((value) => value.toLocaleLowerCase('vi-VN').includes(keyword)))
  }, [query, users])

  const saveUser = async (input: AdminUserInput) => {
    setSubmitting(true)
    setFormError('')
    try {
      if (editing) {
        const updated = await updateAdminUser(token, editing.userId, input)
        setUsers((current) => current?.map((user) => user.userId === updated.userId ? updated : user) ?? [])
        setToast({ message: `Đã cập nhật ${updated.name}`, tone: 'success' })
      } else {
        const created = await createAdminUser(token, input)
        setUsers((current) => [created, ...(current ?? [])])
        setToast({ message: `Đã thêm ${created.name}`, tone: 'success' })
      }
      setEditing(undefined)
    } catch (caught) {
      setFormError(caught instanceof ApiError ? caught.detail : 'Không thể lưu người dùng.')
    } finally {
      setSubmitting(false)
    }
  }

  const confirmDelete = async () => {
    if (!deleting) return
    setSubmitting(true)
    try {
      await deleteAdminUser(token, deleting.userId)
      setUsers((current) => current?.filter((user) => user.userId !== deleting.userId) ?? [])
      setToast({ message: `Đã xóa ${deleting.name}`, tone: 'success' })
      setDeleting(null)
    } catch (caught) {
      setToast({ message: caught instanceof ApiError ? caught.detail : 'Không thể xóa người dùng.', tone: 'error' })
    } finally {
      setSubmitting(false)
    }
  }

  return (
    <section className="admin-page admin-users-page">
      <div className="admin-users-toolbar">
        <label><MdSearch aria-hidden="true" /><span className="sr-only">Tìm người dùng</span><input value={query} onChange={(event) => setQuery(event.target.value)} placeholder="Tìm theo tên/email/SĐT/vai trò" /></label>
        <button type="button" onClick={() => { setEditing(null); setFormError('') }}>+ THÊM NGƯỜI DÙNG</button>
      </div>
      {error ? <AdminFeedback state="error" title="Không thể tải người dùng" message="Vui lòng kiểm tra kết nối tới máy chủ." onRetry={reload} />
        : users === null ? <AdminFeedback state="loading" />
          : visibleUsers.length === 0 ? <AdminFeedback state="empty" title="Không có người dùng phù hợp" message="Hãy đổi từ khóa tìm kiếm hoặc thêm tài khoản mới." />
            : (
              <div className="admin-data-table admin-users-table">
                <div className="admin-data-table__head"><span>TÊN</span><span>LIÊN HỆ</span><span>VAI TRÒ</span><span>TRẠNG THÁI</span><span>THAO TÁC</span></div>
                {visibleUsers.map((user) => (
                  <article key={user.userId}>
                    <strong>{user.name}</strong>
                    <span>{user.email} · {user.phoneNumber}</span>
                    <b>{user.role.toLocaleUpperCase('vi-VN')}</b>
                    <AdminStatusChip value={user.isActive ? 'Hoạt động' : 'Đã khóa'} />
                    <div className="admin-row-actions"><button type="button" aria-label={`Sửa ${user.name}`} onClick={() => { setEditing(user); setFormError('') }}><MdEdit /></button><button type="button" aria-label={`Xóa ${user.name}`} onClick={() => setDeleting(user)}><MdDelete /></button></div>
                  </article>
                ))}
              </div>
            )}

      <AdminDialog open={editing !== undefined} title={editing ? 'Sửa người dùng' : 'Thêm người dùng'} onClose={() => !submitting && setEditing(undefined)}>
        <AdminUserForm roles={roles} initial={editing || null} submitting={submitting} apiError={formError} onCancel={() => setEditing(undefined)} onSubmit={saveUser} />
      </AdminDialog>
      <AdminDialog open={Boolean(deleting)} title="Xóa người dùng" onClose={() => !submitting && setDeleting(null)}>
        <div className="admin-confirm"><p>Bạn có chắc muốn xóa <strong>{deleting?.name}</strong>? Nếu tài khoản đã có đơn hàng, đặt bàn hoặc tin nhắn, backend sẽ từ chối để bảo toàn dữ liệu.</p><div><button type="button" onClick={() => setDeleting(null)} disabled={submitting}>HỦY</button><button className="is-danger" type="button" onClick={confirmDelete} disabled={submitting}>{submitting ? 'ĐANG XÓA...' : 'XÓA NGƯỜI DÙNG'}</button></div></div>
      </AdminDialog>
      {toast && <AdminToast message={toast.message} tone={toast.tone} onDismiss={() => setToast(null)} />}
    </section>
  )
}