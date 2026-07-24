import { useEffect, useMemo, useState, type FormEvent } from 'react'
import {
  MdCalendarToday,
  MdCheckCircle,
  MdGroup,
  MdRefresh,
  MdSchedule,
  MdTableRestaurant,
} from 'react-icons/md'
import { ApiError } from '../../api/httpClient'
import { useAuth } from '../../features/auth/useAuth'
import {
  cancelReservation,
  createReservation,
  getReservationAvailability,
  listMyReservations,
  type Reservation,
  type ReservationAvailability,
} from '../../features/reservations/reservationApi'
import { ReservationTableChoice } from '../../features/reservations/ReservationTableChoice'

const timeSlots = Array.from({ length: 25 }, (_, index) => {
  const minutes = 8 * 60 + 30 + index * 30
  return `${String(Math.floor(minutes / 60)).padStart(2, '0')}:${String(minutes % 60).padStart(2, '0')}`
})

function dateFromToday(offsetDays: number) {
  const date = new Date()
  date.setDate(date.getDate() + offsetDays)
  return `${date.getFullYear()}-${String(date.getMonth() + 1).padStart(2, '0')}-${String(date.getDate()).padStart(2, '0')}`
}

function displayDate(value: string) {
  const [year, month, day] = value.split('-')
  return year && month && day ? `${day}/${month}/${year}` : value
}

function displayTime(value: string) {
  return value.slice(0, 5)
}

function normalize(value: string) {
  return value
    .normalize('NFD')
    .replace(/[\u0300-\u036f]/g, '')
    .toLocaleLowerCase('vi-VN')
}

function statusTone(value: string) {
  const normalized = normalize(value)
  if (normalized.includes('huy') || normalized.includes('cancel') ||
      normalized.includes('het han') || normalized.includes('expired') ||
      normalized.includes('khong den')) {
    return 'danger'
  }
  if (normalized.includes('hoan thanh') || normalized.includes('da den') ||
      normalized.includes('complete')) {
    return 'success'
  }
  if (normalized.includes('xac nhan') || normalized.includes('confirm')) {
    return 'info'
  }
  return 'warning'
}

function canCancel(reservation: Reservation) {
  const normalized = normalize(reservation.status)
  const terminal = ['huy', 'cancel', 'hoan thanh', 'complete', 'het han', 'expired', 'khong den']
    .some((value) => normalized.includes(value))
  return !terminal &&
    new Date(reservation.startAt).getTime() - Date.now() >= 2 * 60 * 60 * 1000
}

function errorMessage(error: unknown, fallback: string) {
  return error instanceof ApiError ? error.detail : fallback
}

export function ReservationPage() {
  const auth = useAuth()
  const session = auth.session
  const token = session?.token
  const [date, setDate] = useState(() => dateFromToday(1))
  const [time, setTime] = useState('18:00')
  const [guests, setGuests] = useState(4)
  const [availability, setAvailability] = useState<ReservationAvailability | null>(null)
  const [checkingAvailability, setCheckingAvailability] = useState(false)
  const [availabilityError, setAvailabilityError] = useState('')
  const [reservations, setReservations] = useState<Reservation[] | null>(null)
  const [listError, setListError] = useState('')
  const [submitting, setSubmitting] = useState(false)
  const [cancellingId, setCancellingId] = useState<number | null>(null)
  const [confirmingId, setConfirmingId] = useState<number | null>(null)
  const [error, setError] = useState('')
  const [success, setSuccess] = useState('')

  const loadReservations = async (signal?: AbortSignal) => {
    if (!token) return
    setListError('')
    try {
      setReservations(await listMyReservations(token, signal))
    } catch (caught) {
      if (!signal?.aborted) {
        setReservations([])
        setListError(errorMessage(
          caught,
          'Không thể tải lịch đặt bàn. Vui lòng thử lại.',
        ))
      }
    }
  }

  useEffect(() => {
    const controller = new AbortController()
    void loadReservations(controller.signal)
    return () => controller.abort()
  }, [token])

  useEffect(() => {
    const controller = new AbortController()
    const timer = window.setTimeout(async () => {
      setCheckingAvailability(true)
      setAvailabilityError('')
      try {
        setAvailability(await getReservationAvailability({
          date,
          time,
          numberOfGuests: guests,
        }))
      } catch (caught) {
        if (!controller.signal.aborted) {
          setAvailability(null)
          setAvailabilityError(errorMessage(
            caught,
            'Không thể kiểm tra khung giờ này.',
          ))
        }
      } finally {
        if (!controller.signal.aborted) setCheckingAvailability(false)
      }
    }, 180)

    return () => {
      window.clearTimeout(timer)
      controller.abort()
    }
  }, [date, guests, time])

  const sortedReservations = useMemo(
    () => [...(reservations ?? [])].sort((left, right) =>
      new Date(right.startAt).getTime() - new Date(left.startAt).getTime(),
    ),
    [reservations],
  )

  const submit = async (event: FormEvent) => {
    event.preventDefault()
    if (!session) return
    setSubmitting(true)
    setError('')
    setSuccess('')
    try {
      const input = { date, time, numberOfGuests: guests }
      const latestAvailability = await getReservationAvailability(input)
      setAvailability(latestAvailability)
      if (!latestAvailability.isAvailable || !latestAvailability.suggestedTable) {
        setError(
          latestAvailability.reason ||
          'Quán chưa còn bàn phù hợp trong khung giờ này.',
        )
        return
      }
      const reservation = await createReservation({
        ...input,
        guestName: session.user.name,
        guestPhoneNumber: session.user.phoneNumber,
        note: null,
      }, session.token)
      setReservations((current) => [reservation, ...(current ?? [])])
      setSuccess(`Đã giữ ${reservation.table.tableName} cho bạn.`)
    } catch (caught) {
      setError(errorMessage(
        caught,
        'Không thể đặt bàn. Vui lòng thử lại.',
      ))
    } finally {
      setSubmitting(false)
    }
  }

  const cancel = async (reservationId: number) => {
    if (!token || cancellingId !== null) return
    setCancellingId(reservationId)
    setError('')
    setSuccess('')
    try {
      const updated = await cancelReservation(token, reservationId)
      setReservations((current) =>
        current?.map((reservation) =>
          reservation.reservationId === updated.reservationId
            ? updated
            : reservation,
        ) ?? [],
      )
      setSuccess(`Đã hủy lịch đặt bàn #${reservationId}.`)
      setConfirmingId(null)
    } catch (caught) {
      setError(errorMessage(
        caught,
        'Không thể hủy lịch đặt bàn. Vui lòng thử lại.',
      ))
    } finally {
      setCancellingId(null)
    }
  }

  return (
    <section className="customer-v2-page reservation-v2-page">
      <div className="reservation-v2-hero">
        <p>ĐẶT CHỖ</p>
        <h1>Giữ một chỗ ngồi <em>bên những người bạn nhỏ.</em></h1>
        <span>Mỗi lượt ghé thăm kéo dài 90 phút. Bạn có thể đặt trước tối đa 7 ngày.</span>
      </div>

      <div className="reservation-customer-layout">
        <form className="reservation-v2-form" onSubmit={submit}>
          <div className="reservation-v2-fields">
            <label className="reservation-v2-pill">
              <MdCalendarToday aria-hidden="true" />
              <span className="reservation-v2-value">{displayDate(date)}</span>
              <input
                className="reservation-v2-native-input"
                aria-label="Ngày đặt bàn"
                type="date"
                min={dateFromToday(0)}
                max={dateFromToday(7)}
                value={date}
                onChange={(event) => setDate(event.target.value)}
                required
              />
            </label>
            <div className="reservation-v2-pill reservation-v2-pill--summary">
              <MdSchedule aria-hidden="true" />
              <span>{time} · 90 phút</span>
            </div>
            <div className="reservation-v2-pill reservation-v2-pill--summary">
              <MdGroup aria-hidden="true" />
              <span>{guests} khách</span>
            </div>
          </div>

          <fieldset className="reservation-slot-fieldset">
            <legend>Chọn khung giờ</legend>
            <div className="reservation-time-slots">
              {timeSlots.map((slot) => (
                <button
                  className={time === slot ? 'is-selected' : ''}
                  type="button"
                  onClick={() => setTime(slot)}
                  aria-pressed={time === slot}
                  key={slot}
                >
                  {slot}
                </button>
              ))}
            </div>
          </fieldset>

          <fieldset className="reservation-slot-fieldset">
            <legend>Số khách</legend>
            <div className="reservation-v2-tables" aria-label="Chọn số khách">
              {[2, 4, 6, 8].map((capacity) => (
                <ReservationTableChoice
                  capacity={capacity}
                  selected={guests === capacity}
                  onSelect={() => setGuests(capacity)}
                  key={capacity}
                />
              ))}
            </div>
          </fieldset>

          <div className="reservation-availability" aria-live="polite">
            {checkingAvailability ? (
              <span><MdRefresh className="is-spinning" aria-hidden="true" />Đang tìm bàn phù hợp…</span>
            ) : availabilityError ? (
              <span className="is-error">{availabilityError}</span>
            ) : availability?.isAvailable && availability.suggestedTable ? (
              <>
                <MdCheckCircle aria-hidden="true" />
                <span>
                  <strong>{availability.suggestedTable.tableName}</strong>
                  {availability.suggestedTable.area || 'Khu vực chung'} · sức chứa {availability.suggestedTable.capacity} khách
                </span>
              </>
            ) : (
              <span className="is-error">
                {availability?.reason || 'Chưa tìm thấy bàn phù hợp.'}
              </span>
            )}
          </div>

          {error && (
            <div className="reservation-v2-message reservation-v2-message--error" role="alert">
              {error}
            </div>
          )}
          {success && (
            <div className="reservation-v2-message reservation-v2-message--success" role="status">
              {success}
            </div>
          )}
          <button
            className="customer-v2-primary-button"
            type="submit"
            disabled={submitting || checkingAvailability || !availability?.isAvailable}
          >
            {submitting ? 'ĐANG GIỮ CHỖ…' : 'XÁC NHẬN ĐẶT BÀN →'}
          </button>
        </form>

        <section className="my-reservations-panel" aria-labelledby="my-reservations-title">
          <header>
            <div>
              <p>LỊCH CỦA BẠN</p>
              <h2 id="my-reservations-title">Bàn đã đặt</h2>
            </div>
            <button
              type="button"
              onClick={() => void loadReservations()}
              aria-label="Tải lại lịch đặt bàn"
              disabled={reservations === null}
            >
              <MdRefresh aria-hidden="true" />
            </button>
          </header>

          {reservations === null ? (
            <div className="my-reservations-feedback" role="status">
              Đang tải lịch đặt bàn…
            </div>
          ) : listError ? (
            <div className="my-reservations-feedback" role="alert">
              <p>{listError}</p>
              <button type="button" onClick={() => void loadReservations()}>Thử lại</button>
            </div>
          ) : sortedReservations.length === 0 ? (
            <div className="my-reservations-feedback">
              <MdTableRestaurant aria-hidden="true" />
              <p>Bạn chưa có lịch đặt bàn nào.</p>
            </div>
          ) : (
            <div className="my-reservations-list">
              {sortedReservations.map((reservation) => {
                const cancellationAllowed = canCancel(reservation)
                const confirming = confirmingId === reservation.reservationId
                return (
                  <article key={reservation.reservationId}>
                    <header>
                      <div>
                        <small>ĐẶT BÀN #{reservation.reservationId}</small>
                        <h3>{reservation.table.tableName}</h3>
                      </div>
                      <span className={`status-chip status-chip--${statusTone(reservation.status)}`}>
                        {reservation.status}
                      </span>
                    </header>
                    <div className="my-reservation-time">
                      <MdCalendarToday aria-hidden="true" />
                      <span>
                        <strong>{displayDate(reservation.date)}</strong>
                        {displayTime(reservation.time)}–{new Intl.DateTimeFormat('vi-VN', {
                          hour: '2-digit',
                          minute: '2-digit',
                        }).format(new Date(reservation.endAt))}
                      </span>
                    </div>
                    <p>
                      {reservation.numberOfGuests} khách · {reservation.table.area || 'Khu vực chung'}
                    </p>
                    {cancellationAllowed && !confirming && (
                      <button
                        className="reservation-cancel-trigger"
                        type="button"
                        onClick={() => setConfirmingId(reservation.reservationId)}
                      >
                        Hủy bàn
                      </button>
                    )}
                    {confirming && (
                      <div className="reservation-cancel-confirm" role="group" aria-label={`Xác nhận hủy đặt bàn #${reservation.reservationId}`}>
                        <p>Hủy lịch này? Thao tác không thể hoàn tác.</p>
                        <div>
                          <button type="button" onClick={() => setConfirmingId(null)}>
                            Giữ lịch
                          </button>
                          <button
                            type="button"
                            disabled={cancellingId !== null}
                            onClick={() => void cancel(reservation.reservationId)}
                          >
                            {cancellingId === reservation.reservationId ? 'Đang hủy…' : 'Xác nhận hủy'}
                          </button>
                        </div>
                      </div>
                    )}
                    {!cancellationAllowed && !isNaN(new Date(reservation.startAt).getTime()) &&
                      !['huy', 'cancel', 'hoan thanh', 'complete', 'het han', 'expired', 'khong den']
                        .some((value) => normalize(reservation.status).includes(value)) && (
                        <small className="reservation-cancel-policy">
                          Chỉ có thể hủy trước giờ bắt đầu ít nhất 2 tiếng.
                        </small>
                      )}
                  </article>
                )
              })}
            </div>
          )}
        </section>
      </div>
    </section>
  )
}
