import { useCallback, useEffect, useState } from 'react'
import { MdTableRestaurant } from 'react-icons/md'
import { AdminFeedback } from '../../features/admin/components/AdminFeedback'
import { AdminStatusChip } from '../../features/admin/components/AdminStatusChip'
import { catalogRepository } from '../../services/catalogRepository'
import type { CafeTable } from '../../types/models'

const unavailable = 'Backend chưa hỗ trợ thao tác này.'

export function AdminTablesPage() {
  const [tables, setTables] = useState<CafeTable[] | null>(null)
  const [error, setError] = useState(false)
  const [reloadKey, setReloadKey] = useState(0)
  const reload = useCallback(() => setReloadKey((value) => value + 1), [])

  useEffect(() => {
    let alive = true
    setTables(null)
    setError(false)
    catalogRepository.listAvailableTables(1)
      .then((items) => { if (alive) setTables(items) })
      .catch(() => { if (alive) { setTables([]); setError(true) } })
    return () => { alive = false }
  }, [reloadKey])

  return (
    <section className="admin-page admin-card-page">
      <div className="admin-page-toolbar">
        <span>{tables?.length ?? 0} BÀN</span>
        <button type="button" disabled title={unavailable}>+ THÊM BÀN</button>
      </div>
      <p className="admin-readonly-notice">{unavailable}</p>
      {error ? <AdminFeedback state="error" title="Không thể tải danh sách bàn" message="Vui lòng kiểm tra dữ liệu đặt bàn." onRetry={reload} />
        : tables === null ? <AdminFeedback state="loading" />
          : tables.length === 0 ? <AdminFeedback state="empty" title="Chưa có bàn khả dụng" />
            : (
              <div className="admin-tables-grid">
                {tables.map((table) => (
                  <article className="admin-table-card-v2" key={table.id}>
                    <header><MdTableRestaurant aria-hidden="true" /><AdminStatusChip value={table.available ? 'Còn trống' : 'Đang dùng'} /></header>
                    <h2>{table.name}</h2>
                    <p>{table.capacity} CHỖ · {table.area.toLocaleUpperCase('vi-VN')}</p>
                  </article>
                ))}
              </div>
            )}
    </section>
  )
}
