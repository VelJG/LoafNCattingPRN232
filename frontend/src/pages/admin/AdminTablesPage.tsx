import { useCallback, useEffect, useMemo, useState } from 'react'
import { MdDelete, MdEdit, MdSearch, MdTableRestaurant } from 'react-icons/md'
import { ApiError } from '../../api/httpClient'
import {
  createAdminTable,
  deleteAdminTable,
  getAdminTableOptions,
  listAdminTables,
  updateAdminTable,
} from '../../features/admin/adminApi'
import { AdminDialog } from '../../features/admin/components/AdminDialog'
import { AdminFeedback } from '../../features/admin/components/AdminFeedback'
import { AdminStatusChip } from '../../features/admin/components/AdminStatusChip'
import { AdminTableForm } from '../../features/admin/components/AdminTableForm'
import { AdminToast } from '../../features/admin/components/AdminToast'
import type { AdminTable, AdminTableInput } from '../../features/admin/adminTypes'
import { useAuth } from '../../features/auth/useAuth'

export function AdminTablesPage() {
  const token = useAuth().session?.token ?? ''
  const [tables, setTables] = useState<AdminTable[] | null>(null)
  const [statuses, setStatuses] = useState<string[]>([])
  const [query, setQuery] = useState('')
  const [error, setError] = useState(false)
  const [reloadKey, setReloadKey] = useState(0)
  const [editing, setEditing] = useState<AdminTable | null | undefined>(undefined)
  const [deleting, setDeleting] = useState<AdminTable | null>(null)
  const [submitting, setSubmitting] = useState(false)
  const [formError, setFormError] = useState('')
  const [toast, setToast] = useState<{ message: string; tone: 'success' | 'error' } | null>(null)
  const reload = useCallback(() => setReloadKey((value) => value + 1), [])

  useEffect(() => {
    const controller = new AbortController()
    let alive = true
    setTables(null)
    setError(false)
    Promise.all([
      listAdminTables(token, controller.signal),
      getAdminTableOptions(token, controller.signal),
    ])
      .then(([tableItems, options]) => {
        if (alive) {
          setTables(tableItems)
          setStatuses(options.statuses)
        }
      })
      .catch(() => {
        if (alive) {
          setTables([])
          setStatuses([])
          setError(true)
        }
      })
    return () => { alive = false; controller.abort() }
  }, [reloadKey, token])

  const visibleTables = useMemo(() => {
    const current = tables ?? []
    const keyword = query.trim().toLocaleLowerCase('vi-VN')
    if (!keyword) return current
    return current.filter((table) => [
      table.tableName,
      table.area ?? '',
      table.status,
      table.description ?? '',
    ].some((value) => value.toLocaleLowerCase('vi-VN').includes(keyword)))
  }, [query, tables])

  const saveTable = async (input: AdminTableInput) => {
    setSubmitting(true)
    setFormError('')
    try {
      if (editing) {
        const updated = await updateAdminTable(token, editing.tableId, input)
        setTables((current) => current?.map((table) => table.tableId === updated.tableId ? updated : table) ?? [])
        setToast({ message: `Đã cập nhật ${updated.tableName}`, tone: 'success' })
      } else {
        const created = await createAdminTable(token, input)
        setTables((current) => [created, ...(current ?? [])])
        setToast({ message: `Đã thêm ${created.tableName}`, tone: 'success' })
      }
      setEditing(undefined)
    } catch (caught) {
      setFormError(caught instanceof ApiError ? caught.detail : 'Không thể lưu bàn.')
    } finally {
      setSubmitting(false)
    }
  }

  const confirmDelete = async () => {
    if (!deleting) return
    setSubmitting(true)
    try {
      await deleteAdminTable(token, deleting.tableId)
      setTables((current) => current?.filter((table) => table.tableId !== deleting.tableId) ?? [])
      setToast({ message: `Đã xóa ${deleting.tableName}`, tone: 'success' })
      setDeleting(null)
    } catch (caught) {
      setToast({ message: caught instanceof ApiError ? caught.detail : 'Không thể xóa bàn.', tone: 'error' })
    } finally {
      setSubmitting(false)
    }
  }

  return (
    <section className="admin-page admin-card-page">
      <div className="admin-users-toolbar admin-tables-toolbar">
        <label><MdSearch aria-hidden="true" /><span className="sr-only">Tìm bàn</span><input value={query} onChange={(event) => setQuery(event.target.value)} placeholder="Tìm theo tên/khu vực/trạng thái" /></label>
        <span className="admin-toolbar-count">{visibleTables.length} BÀN</span>
        <button type="button" onClick={() => { setEditing(null); setFormError('') }}>+ THÊM BÀN</button>
      </div>
      {error ? <AdminFeedback state="error" title="Không thể tải danh sách bàn" message="Vui lòng kiểm tra kết nối tới máy chủ." onRetry={reload} />
        : tables === null ? <AdminFeedback state="loading" />
          : visibleTables.length === 0 ? <AdminFeedback state="empty" title="Không có bàn phù hợp" message="Hãy đổi từ khóa tìm kiếm hoặc thêm bàn mới." />
            : (
              <div className="admin-tables-grid">
                {visibleTables.map((table) => (
                  <article className="admin-table-card-v2" key={table.tableId}>
                    <header>
                      <MdTableRestaurant aria-hidden="true" />
                      <AdminStatusChip value={table.status} />
                      <div className="admin-table-actions"><button type="button" aria-label={`Sửa ${table.tableName}`} onClick={() => { setEditing(table); setFormError('') }}><MdEdit /></button><button type="button" aria-label={`Xóa ${table.tableName}`} onClick={() => setDeleting(table)}><MdDelete /></button></div>
                    </header>
                    <h2>{table.tableName}</h2>
                    <p>{table.capacity} CHỖ · {(table.area || 'KHU VỰC CHUNG').toLocaleUpperCase('vi-VN')}</p>
                    {table.description && <small>{table.description}</small>}
                  </article>
                ))}
              </div>
            )}

      <AdminDialog open={editing !== undefined} title={editing ? 'Sửa bàn' : 'Thêm bàn'} onClose={() => !submitting && setEditing(undefined)}>
        <AdminTableForm statuses={statuses} initial={editing || null} submitting={submitting} apiError={formError} onCancel={() => setEditing(undefined)} onSubmit={saveTable} />
      </AdminDialog>
      <AdminDialog open={Boolean(deleting)} title="Xóa bàn" onClose={() => !submitting && setDeleting(null)}>
        <div className="admin-confirm"><p>Bạn có chắc muốn xóa <strong>{deleting?.tableName}</strong>? Nếu bàn đã có đặt chỗ hoặc đơn hàng, backend sẽ từ chối để bảo toàn dữ liệu.</p><div><button type="button" onClick={() => setDeleting(null)} disabled={submitting}>HỦY</button><button className="is-danger" type="button" onClick={confirmDelete} disabled={submitting}>{submitting ? 'ĐANG XÓA...' : 'XÓA BÀN'}</button></div></div>
      </AdminDialog>
      {toast && <AdminToast message={toast.message} tone={toast.tone} onDismiss={() => setToast(null)} />}
    </section>
  )
}