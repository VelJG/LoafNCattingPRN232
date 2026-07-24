import { useCallback, useEffect, useMemo, useState } from 'react'
import { ApiError } from '../../api/httpClient'
import { listStoreReservations, transitionReservation } from '../../features/admin/adminApi'
import { AdminFeedback } from '../../features/admin/components/AdminFeedback'
import { AdminStatusChip } from '../../features/admin/components/AdminStatusChip'
import { AdminToast } from '../../features/admin/components/AdminToast'
import type { ReservationTransition, StoreReservation } from '../../features/admin/adminTypes'
import { useAuth } from '../../features/auth/useAuth'

const filters = [
  { key: 'all', label: 'Tất cả', matches: () => true },
  { key: 'pending', label: 'Chờ xác nhận', matches: (status: string) => /(pending|chờ)/i.test(status) },
  { key: 'confirmed', label: 'Đã xác nhận', matches: (status: string) => /(confirmed|xác nhận)/i.test(status) },
  { key: 'checked', label: 'Đã đến', matches: (status: string) => /(checked|đã đến)/i.test(status) },
  { key: 'completed', label: 'Hoàn thành', matches: (status: string) => /(completed|hoàn)/i.test(status) },
] as const

interface TransitionOption {
  value: ReservationTransition
  label: string
}

function allowedTransitions(status: string): TransitionOption[] {
  if (/(pending|chờ)/i.test(status)) {
    return [
      { value: 'confirm', label: 'Đã xác nhận' },
      { value: 'cancel', label: 'Đã hủy' },
    ]
  }
  if (/(confirmed|xác nhận)/i.test(status)) {
    return [
      { value: 'check-in', label: 'Đã đến (check-in)' },
      { value: 'cancel', label: 'Đã hủy' },
    ]
  }
  if (/(checked|đã đến)/i.test(status)) {
    return [{ value: 'complete', label: 'Hoàn thành' }]
  }
  return []
}

export function AdminReservationsPage() {
  const token = useAuth().session?.token ?? ''
  const [items, setItems] = useState<StoreReservation[] | null>(null)
  const [filter, setFilter] = useState<(typeof filters)[number]['key']>('all')
  const [error, setError] = useState(false)
  const [updating, setUpdating] = useState<number | null>(null)
  const [editing, setEditing] = useState<{ reservationId: number; action: ReservationTransition | '' } | null>(null)
  const [toast, setToast] = useState<{ message: string; tone: 'success' | 'error' } | null>(null)
  const [reloadKey, setReloadKey] = useState(0)
  const reload = useCallback(() => setReloadKey((value) => value + 1), [])

  useEffect(() => {
    const controller = new AbortController()
    let alive = true
    setItems(null)
    setError(false)
    listStoreReservations(token, controller.signal)
      .then((value) => { if (alive) setItems(value) })
      .catch(() => { if (alive) { setItems([]); setError(true) } })
    return () => { alive = false; controller.abort() }
  }, [reloadKey, token])

  const visible = useMemo(() => {
    const active = filters.find((item) => item.key === filter) ?? filters[0]
    return [...(items ?? [])]
      .filter((item) => active.matches(item.status))
      .sort((a, b) => b.startAt.localeCompare(a.startAt))
  }, [filter, items])

  const transition = async (reservation: StoreReservation, action: ReservationTransition) => {
    setUpdating(reservation.reservationId)
    try {
      const updated = await transitionReservation(token, reservation.reservationId, action)
      setItems((current) => current?.map((item) => item.reservationId === updated.reservationId ? updated : item) ?? [])
      setEditing(null)
      setToast({ message: `Đã cập nhật đặt bàn #${reservation.reservationId}`, tone: 'success' })
    } catch (caught) {
      setToast({ message: caught instanceof ApiError ? caught.detail : 'Không thể cập nhật đặt bàn.', tone: 'error' })
    } finally {
      setUpdating(null)
    }
  }

  return (
    <section className="admin-page admin-list-page">
      <div className="admin-filter-row" aria-label="Lọc trạng thái đặt bàn">
        {filters.map((item) => <button className={filter === item.key ? 'is-active' : ''} type="button" key={item.key} onClick={() => setFilter(item.key)}>{item.label}</button>)}
      </div>
      {error ? <AdminFeedback state="error" title="Không thể tải đặt bàn" message="Vui lòng kiểm tra kết nối tới máy chủ." onRetry={reload} />
        : items === null ? <AdminFeedback state="loading" />
          : visible.length === 0 ? <AdminFeedback state="empty" title="Không có lịch phù hợp" message="Hãy chọn trạng thái khác để xem lịch đặt bàn." />
            : (
              <div className="admin-entity-grid">
                {visible.map((reservation) => {
                  const transitionOptions = allowedTransitions(reservation.status)
                  const isEditing = editing?.reservationId === reservation.reservationId
                  return (
                    <article className="admin-reservation-card" key={reservation.reservationId}>
                      <header><strong>{reservation.date.split('-').reverse().join('/')} · {reservation.time.slice(0, 5)}</strong><AdminStatusChip value={reservation.status} /></header>
                      <p>{reservation.guestName}</p>
                      <span>{reservation.numberOfGuests} KHÁCH · {reservation.table.tableName.toLocaleUpperCase('vi-VN')} · {(reservation.table.area || 'KHU VỰC CHUNG').toLocaleUpperCase('vi-VN')}</span>
                      {transitionOptions.length > 0 && (
                        isEditing ? (
                          <div className="admin-reservation-transition">
                            <label>
                              <span>TRẠNG THÁI MỚI</span>
                              <select
                                aria-label={`Trạng thái mới cho đặt bàn #${reservation.reservationId}`}
                                value={editing.action}
                                disabled={updating === reservation.reservationId}
                                onChange={(event) => setEditing({
                                  reservationId: reservation.reservationId,
                                  action: event.target.value as ReservationTransition | '',
                                })}
                              >
                                <option value="">Chọn trạng thái...</option>
                                {transitionOptions.map((option) => (
                                  <option key={option.value} value={option.value}>{option.label}</option>
                                ))}
                              </select>
                            </label>
                            <div className="admin-card-actions admin-card-actions--split">
                              <button
                                className="admin-cancel-action"
                                type="button"
                                disabled={updating === reservation.reservationId}
                                onClick={() => setEditing(null)}
                              >
                                ĐÓNG
                              </button>
                              <button
                                type="button"
                                disabled={!editing.action || updating === reservation.reservationId}
                                onClick={() => {
                                  if (editing.action) void transition(reservation, editing.action)
                                }}
                                aria-label={`Xác nhận trạng thái đặt bàn #${reservation.reservationId}`}
                              >
                                {updating === reservation.reservationId ? 'ĐANG CẬP NHẬT...' : 'XÁC NHẬN'}
                              </button>
                            </div>
                          </div>
                        ) : (
                          <div className="admin-card-actions">
                            <button
                              type="button"
                              onClick={() => setEditing({ reservationId: reservation.reservationId, action: '' })}
                              aria-label={`Cập nhật trạng thái đặt bàn #${reservation.reservationId}`}
                            >
                              CẬP NHẬT TRẠNG THÁI
                            </button>
                          </div>
                        )
                      )}
                    </article>
                  )
                })}
              </div>
            )}
      {toast && <AdminToast message={toast.message} tone={toast.tone} onDismiss={() => setToast(null)} />}
    </section>
  )
}
