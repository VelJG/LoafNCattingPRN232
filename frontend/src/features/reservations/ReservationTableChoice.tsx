import { MdTableRestaurant } from 'react-icons/md'

interface ReservationTableChoiceProps {
  capacity: number
  selected: boolean
  onSelect(): void
}

export function ReservationTableChoice({ capacity, selected, onSelect }: ReservationTableChoiceProps) {
  return (
    <button
      className={selected ? 'reservation-v2-table is-selected' : 'reservation-v2-table'}
      type="button"
      aria-pressed={selected}
      aria-label={`Bàn ${capacity}, ${capacity} khách`}
      onClick={onSelect}
    >
      <MdTableRestaurant aria-hidden="true" />
      <strong>Bàn {capacity}</strong>
      <span>{capacity} KHÁCH</span>
    </button>
  )
}
