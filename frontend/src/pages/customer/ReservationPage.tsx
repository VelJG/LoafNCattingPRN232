import { useState, type FormEvent } from 'react'
import { MdCalendarToday, MdGroup, MdSchedule } from 'react-icons/md'
import { useAuth } from '../../features/auth/useAuth'
import {
  createReservation,
  getReservationAvailability,
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
  return `${hour}:${minute}`
})

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
    </section>
  )
}
