import { useCallback, useEffect, useState, type FormEvent } from 'react'
import { MdCalendarToday, MdEventAvailable, MdGroup, MdRefresh, MdSchedule } from 'react-icons/md'
import { useAuth } from '../../features/auth/useAuth'
import {
  cancelReservation,
  createReservation,
  getReservationAvailability,
  listMyReservations,
  type Reservation,
} from '../../features/reservations/reservationApi'

function defaultDate() {
  const date = new Date()
  date.setDate(date.getDate() + 1)
  return date.toISOString().slice(0, 10)
}

const bookingSlots = Array.from({ length: 24 }, (_, index) => {
  const totalMinutes = 8 * 60 + 30 + index * 30
  const hour = Math.floor(totalMinutes / 60).toString().padStart(2, '0')
  const minute = (totalMinutes % 60).toString().padStart(2, '0')
  return hour + ':' + minute
})

function normalize(value: string) {
  return value
    .normalize('NFD')
    .replace(/[\u0300-\u036f]/g, '')
    .toLocaleLowerCase('vi-VN')
}

function statusTone(status: string) {
  const value = normalize(status)
  if (value.includes('cancel') || value.includes('huy') || value.includes('no show')) return 'danger'
  if (value.includes('complete') || value.includes('hoan thanh') || value.includes('check')) return 'success'
  if (value.includes('confirm') || value.includes('xac nhan')) return 'info'
  return 'warning'
}

function canCancelReservation(reservation: Reservation) {
  const value = normalize(reservation.status)
  return value.includes('pending') || value.includes('cho') ||
    value.includes('confirm') || value.includes('xac nhan')
}

function formatReservationDate(reservation: Reservation) {
  const date = new Date(reservation.date + 'T00:00:00')
  const dateText = Number.isNaN(date.getTime())
    ? reservation.date
    : new Intl.DateTimeFormat('vi-VN', { dateStyle: 'medium' }).format(date)
  return dateText + ' lúc ' + reservation.time.slice(0, 5)
}

export function ReservationPage() {
  const auth = useAuth()
  const session = auth.session
  const token = session?.token
  const [date, setDate] = useState(defaultDate)
  const [time, setTime] = useState('18:00')
  const [guests, setGuests] = useState(4)
  const [reservations, setReservations] = useState<Reservation[] | null>(null)
  const [submitting, setSubmitting] = useState(false)
  const [loadingHistory, setLoadingHistory] = useState(false)
  const [cancellingReservationId, setCancellingReservationId] = useState<number | null>(null)
  const [confirmCancelId, setConfirmCancelId] = useState<number | null>(null)
  const [error, setError] = useState('')
  const [historyError, setHistoryError] = useState('')
  const [success, setSuccess] = useState('')

  const loadReservations = useCallback(async (signal?: AbortSignal) => {
    if (!token) return
    setLoadingHistory(true)
    setHistoryError('')
    try {
      setReservations(await listMyReservations(token, signal))
    } catch (caught) {
      if (!signal?.aborted) {
        setHistoryError(caught instanceof Error ? caught.message : 'Không thể tải lịch sử đặt bàn.')
        setReservations([])
      }
    } finally {
      if (!signal?.aborted) setLoadingHistory(false)
    }
  }, [token])

  useEffect(() => {
    const controller = new AbortController()
    void loadReservations(controller.signal)
    return () => controller.abort()
  }, [loadReservations])

  const submit = async (event: FormEvent) => {
    event.preventDefault()
    if (!session) return
    setSubmitting(true)
    setError('')
    setSuccess('')
    try {
      const input = { date, time, numberOfGuests: guests }
      const availability = await getReservationAvailability(input)
      if (!availability.isAvailable) {
        setError(availability.reason || 'Quán chưa còn bàn phù hợp trong khung giờ này.')
        return
      }
      await createReservation({
        ...input,
        guestName: session.user.name,
        guestPhoneNumber: session.user.phoneNumber,
        note: null,
      }, session.token)
      setSuccess('Yêu cầu đặt chỗ đã được gửi. Vui lòng chờ quán xác nhận.')
      void loadReservations()
    } catch (caught) {
      setError(caught instanceof Error ? caught.message : 'Không thể đặt bàn. Vui lòng thử lại.')
    } finally {
      setSubmitting(false)
    }
  }

  const confirmCancelReservation = async (reservationId: number) => {
    if (!token || cancellingReservationId !== null) return
    setCancellingReservationId(reservationId)
    setHistoryError('')
    try {
      const updated = await cancelReservation(token, reservationId)
      setReservations((current) => current?.map((reservation) =>
        reservation.reservationId === updated.reservationId ? updated : reservation,
      ) ?? [updated])
      setConfirmCancelId(null)
    } catch (caught) {
      setHistoryError(caught instanceof Error ? caught.message : 'Không thể hủy đặt bàn.')
    } finally {
      setCancellingReservationId(null)
    }
  }

  return (
    <section className="customer-v2-page reservation-v2-page">
      <div className="reservation-customer-layout">
        <div>
          <div className="reservation-v2-hero">
            <p>ĐẶT CHỖ</p>
            <h1>Đặt lịch ghé quán <em>cùng những chú mèo.</em></h1>
            <span>Chọn ngày, giờ và số khách; quán sẽ sắp xếp bàn phù hợp.</span>
          </div>
          <form className="reservation-v2-form" onSubmit={submit}>
            <div className="reservation-v2-fields">
              <label className="reservation-v2-pill">
                <MdCalendarToday aria-hidden="true" />
                <input className="reservation-v2-native-input" aria-label="Ngày đặt bàn" type="date" value={date} onChange={(event) => setDate(event.target.value)} required />
              </label>
              <label className="reservation-v2-pill">
                <MdSchedule aria-hidden="true" />
                <select className="reservation-v2-native-input" aria-label="Giờ đặt bàn" value={time} onChange={(event) => setTime(event.target.value)} required>
                  {bookingSlots.map((slot) => <option value={slot} key={slot}>{slot}</option>)}
                </select>
              </label>
              <label className="reservation-v2-pill">
                <MdGroup aria-hidden="true" />
                <select className="reservation-v2-native-input" aria-label="Số khách" value={guests} onChange={(event) => setGuests(Number(event.target.value))} required>
                  {Array.from({ length: 8 }, (_, index) => index + 1).map((count) => (
                    <option value={count} key={count}>{count} khách</option>
                  ))}
                </select>
              </label>
            </div>
            {error && <div className="reservation-v2-message reservation-v2-message--error" role="alert">{error}</div>}
            {success && <div className="reservation-v2-message reservation-v2-message--success" role="status">{success}</div>}
            <button className="customer-v2-primary-button" type="submit" disabled={submitting}>
              {submitting ? 'ĐANG GIỮ CHỖ…' : 'XÁC NHẬN ĐẶT BÀN →'}
            </button>
          </form>
        </div>

        <aside className="my-reservations-panel" aria-label="Lịch sử đặt bàn">
          <header>
            <div>
              <p>LỊCH SỬ</p>
              <h2>Lịch đặt bàn</h2>
            </div>
            <button type="button" aria-label="Tải lại lịch đặt bàn" onClick={() => void loadReservations()} disabled={loadingHistory}>
              <MdRefresh className={loadingHistory ? 'is-spinning' : undefined} aria-hidden="true" />
            </button>
          </header>

          {reservations === null ? (
            <div className="my-reservations-feedback" role="status">
              <MdEventAvailable aria-hidden="true" />
              <p>Đang tải lịch đặt bàn…</p>
            </div>
          ) : historyError && reservations.length === 0 ? (
            <div className="my-reservations-feedback" role="alert">
              <MdEventAvailable aria-hidden="true" />
              <p>{historyError}</p>
              <button type="button" onClick={() => void loadReservations()}>Thử lại</button>
            </div>
          ) : reservations.length === 0 ? (
            <div className="my-reservations-feedback">
              <MdEventAvailable aria-hidden="true" />
              <p>Bạn chưa có lịch đặt bàn nào.</p>
            </div>
          ) : (
            <div className="my-reservations-list">
              {historyError && <div className="reservation-v2-message reservation-v2-message--error" role="alert">{historyError}</div>}
              {reservations.map((reservation) => {
                const canCancel = canCancelReservation(reservation)
                const isConfirmingCancel = confirmCancelId === reservation.reservationId
                return (
                  <article key={reservation.reservationId}>
                    <header>
                      <div>
                        <small>RESERVATION #{reservation.reservationId}</small>
                        <h3>{reservation.guestName}</h3>
                      </div>
                      <span className={'status-chip status-chip--' + statusTone(reservation.status)}>{reservation.status}</span>
                    </header>
                    <div className="my-reservation-time">
                      <MdSchedule aria-hidden="true" />
                      <span>
                        <strong>{formatReservationDate(reservation)}</strong>
                        <small>{reservation.numberOfGuests} khách · {reservation.durationMinutes} phút</small>
                      </span>
                    </div>
                    {reservation.note && <p>{reservation.note}</p>}
                    {canCancel && !isConfirmingCancel && (
                      <button className="reservation-cancel-trigger" type="button" onClick={() => setConfirmCancelId(reservation.reservationId)} disabled={cancellingReservationId !== null}>
                        Hủy bàn
                      </button>
                    )}
                    {canCancel && isConfirmingCancel && (
                      <div className="reservation-cancel-confirm">
                        <p>Bạn chắc chắn muốn hủy lịch đặt bàn này?</p>
                        <div>
                          <button type="button" onClick={() => setConfirmCancelId(null)} disabled={cancellingReservationId !== null}>Giữ lại</button>
                          <button type="button" onClick={() => void confirmCancelReservation(reservation.reservationId)} disabled={cancellingReservationId !== null}>
                            {cancellingReservationId === reservation.reservationId ? 'Đang hủy…' : 'Xác nhận hủy'}
                          </button>
                        </div>
                      </div>
                    )}
                  </article>
                )
              })}
            </div>
          )}
        </aside>
      </div>
    </section>
  )
}