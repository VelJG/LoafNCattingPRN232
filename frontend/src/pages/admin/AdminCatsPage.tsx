import { useCallback, useEffect, useState } from 'react'
import { MdDelete, MdEdit, MdPets } from 'react-icons/md'
import { AdminFeedback } from '../../features/admin/components/AdminFeedback'
import { AdminStatusChip } from '../../features/admin/components/AdminStatusChip'
import { catalogRepository } from '../../services/catalogRepository'
import type { CatProfile } from '../../types/models'

const unavailable = 'Backend chưa hỗ trợ thao tác này.'

export function AdminCatsPage() {
  const [cats, setCats] = useState<CatProfile[] | null>(null)
  const [error, setError] = useState(false)
  const [reloadKey, setReloadKey] = useState(0)
  const reload = useCallback(() => setReloadKey((value) => value + 1), [])

  useEffect(() => {
    let alive = true
    setCats(null)
    setError(false)
    catalogRepository.listCats()
      .then((items) => { if (alive) setCats(items) })
      .catch(() => { if (alive) { setCats([]); setError(true) } })
    return () => { alive = false }
  }, [reloadKey])

  return (
    <section className="admin-page admin-card-page">
      <div className="admin-page-toolbar">
        <span>{cats?.length ?? 0} BÉ MÈO</span>
        <button type="button" disabled title={unavailable}>+ THÊM MÈO</button>
      </div>
      <p className="admin-readonly-notice">{unavailable}</p>
      {error ? <AdminFeedback state="error" title="Không thể tải danh sách mèo" message="Vui lòng kiểm tra kết nối tới máy chủ." onRetry={reload} />
        : cats === null ? <AdminFeedback state="loading" />
          : cats.length === 0 ? <AdminFeedback state="empty" title="Chưa có hồ sơ mèo" />
            : (
              <div className="admin-cats-grid">
                {cats.map((cat) => (
                  <article className="admin-cat-card" key={cat.id}>
                    <div className="admin-cat-card__identity">
                      <span>{cat.imageUrl ? <img src={cat.imageUrl} alt="" /> : <MdPets aria-hidden="true" />}</span>
                      <div><h2>{cat.name}</h2><p>{cat.breed.toLocaleUpperCase('vi-VN')}</p></div>
                    </div>
                    <div className="admin-cat-card__footer">
                      <AdminStatusChip value={cat.status} />
                      <button type="button" disabled aria-label={`Sửa ${cat.name}`} title={unavailable}><MdEdit /></button>
                      <button type="button" disabled aria-label={`Xóa ${cat.name}`} title={unavailable}><MdDelete /></button>
                    </div>
                  </article>
                ))}
              </div>
            )}
    </section>
  )
}
