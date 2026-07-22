import { useState, type FormEvent } from 'react'
import { MdCalendarToday, MdGroup, MdSchedule } from 'react-icons/md'
import { useAuth } from '../../features/auth/useAuth'
import {
  createReservation,
  getReservationAvailability,
} from '../../features/reservations/reservationApi'
import { ReservationTableChoice } from '../../features/reservations/ReservationTableChoice'

function defaultDate() {
  const date = new Date()
  date.setDate(date.getDate() + 1)
  return date.toISOString().slice(0, 10)
}

function displayDate(value: string) {
  const [year, month, day] = value.split('-')
  return year && month && day ? `${day}/${month}/${year}` : value
}

export function ReservationPage() {
  const auth = useAuth()
  const [date, setDate] = useState(defaultDate)
  const [time, setTime] = useState('18:00')
  const [guests, setGuests] = useState(4)
  const [submitting, setSubmitting] = useState(false)
  const [error, setError] = useState('')
  const [success, setSuccess] = useState('')

  const submit = async (event: FormEvent) => {
    event.preventDefault()
    const session = auth.session
    if (!session) return
    setSubmitting(true)
    setError('')
    setSuccess('')
    try {
      const input = { date, time, numberOfGuests: guests }
      const availability = await getReservationAvailability(input)
      if (!availability.isAvailable || !availability.suggestedTable) {
        setError(availability.reason || 'Quán chưa còn bàn phù hợp trong khung giờ này.')
        return
      }
      const reservation = await createReservation({
        ...input,
        guestName: session.user.name,
        guestPhoneNumber: session.user.phoneNumber,
        note: null,
      }, session.token)
      setSuccess(`Đã giữ ${reservation.table.tableName} cho bạn.`)
    } catch (caught) {
      setError(caught instanceof Error ? caught.message : 'Không thể đặt bàn. Vui lòng thử lại.')
    } finally {
      setSubmitting(false)
    }
  }

  return (
    <section className="customer-v2-page reservation-v2-page">
      <div className="reservation-v2-hero">
        <p>ĐẶT CHỖ</p>
        <h1>Giữ một chỗ ngồi <em>bên cửa sổ.</em></h1>
        <span>Chọn chỗ ngồi ưng ý trước khi quán đông khách.</span>
      </div>
      <form className="reservation-v2-form" onSubmit={submit}>
        <div className="reservation-v2-fields">
          <label className="reservation-v2-pill">
            <MdCalendarToday aria-hidden="true" />
            <span className="reservation-v2-value">{displayDate(date)}</span>
            <input className="reservation-v2-native-input" aria-label="Ngày đặt bàn" type="date" value={date} onChange={(event) => setDate(event.target.value)} required />
          </label>
          <label className="reservation-v2-pill">
            <MdSchedule aria-hidden="true" />
            <span className="reservation-v2-value">{time}</span>
            <input className="reservation-v2-native-input" aria-label="Giờ đặt bàn" type="time" value={time} onChange={(event) => setTime(event.target.value)} required />
          </label>
          <div className="reservation-v2-pill reservation-v2-pill--summary">
            <MdGroup aria-hidden="true" /><span>{guests} khách</span>
          </div>
        </div>
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
        {error && <div className="reservation-v2-message reservation-v2-message--error" role="alert">{error}</div>}
        {success && <div className="reservation-v2-message reservation-v2-message--success" role="status">{success}</div>}
        <button className="customer-v2-primary-button" type="submit" disabled={submitting}>
          {submitting ? 'ĐANG GIỮ CHỖ…' : 'XÁC NHẬN ĐẶT BÀN →'}
        </button>
      </form>
    </section>
  )
}
