import { useState, type FormEvent } from 'react'
import { MdCalendarMonth, MdCheckCircle, MdGroups, MdPets, MdSearch, MdTableRestaurant } from 'react-icons/md'
import { catalogRepository } from '../../services/catalogRepository'
import type { CafeTable } from '../../types/models'

export function ReservationPage() {
  const [guests, setGuests] = useState(2)
  const [tables, setTables] = useState<CafeTable[]>([])
  const [selectedTable, setSelectedTable] = useState<number | null>(null)
  const [searched, setSearched] = useState(false)
  const [confirmed, setConfirmed] = useState(false)

  const findTables = async (event: FormEvent) => {
    event.preventDefault()
    setTables(await catalogRepository.listAvailableTables(guests))
    setSelectedTable(null)
    setSearched(true)
    setConfirmed(false)
  }

  return (
    <section className="content-page page-width reservation-page">
      <div className="reservation-intro"><span className="hero-kicker hero-kicker--orange"><MdPets />Plan a cozy visit</span><h1>Your table, your time, your favorite cats.</h1><p>Choose a date and party size. We’ll show tables that fit your visit.</p></div>
      <div className="reservation-layout">
        <form className="form-card" onSubmit={findTables}>
          <div className="form-card__heading"><span className="icon-badge"><MdCalendarMonth /></span><div><h2>Visit details</h2><p>Mock availability search</p></div></div>
          <div className="form-grid">
            <label><span>Date</span><input type="date" defaultValue="2026-07-12" required /></label>
            <label><span>Time</span><input type="time" defaultValue="18:00" required /></label>
            <label><span>Guests</span><div className="input-with-icon"><MdGroups /><input type="number" min="1" max="8" value={guests} onChange={(event) => setGuests(Number(event.target.value))} /></div></label>
            <label><span>Contact phone</span><input type="tel" defaultValue="090 123 4567" required /></label>
          </div>
          <label><span>Note for the cafe</span><textarea rows={3} placeholder="Birthday, preferred area, accessibility needs..." /></label>
          <button className="button button--primary button--full" type="submit"><MdSearch />Find available tables</button>
        </form>

        <div className="availability-panel">
          <div className="form-card__heading"><span className="icon-badge"><MdTableRestaurant /></span><div><h2>Available tables</h2><p>{searched ? `${tables.length} good matches` : 'Complete your visit details first'}</p></div></div>
          {!searched ? <div className="availability-placeholder"><MdTableRestaurant /><p>Available tables will appear here.</p></div> : tables.map((table) => <button className={selectedTable === table.id ? 'table-option table-option--selected' : 'table-option'} type="button" key={table.id} onClick={() => { setSelectedTable(table.id); setConfirmed(false) }}><span className="icon-badge"><MdTableRestaurant /></span><span><strong>{table.name}</strong><small>{table.area} · up to {table.capacity} guests</small></span>{selectedTable === table.id && <MdCheckCircle />}</button>)}
          {searched && tables.length === 0 && <div className="availability-placeholder"><MdTableRestaurant /><p>No suitable table was found for this party size.</p></div>}
          {selectedTable && <button className="button button--primary button--full" type="button" onClick={() => setConfirmed(true)}>Confirm reservation</button>}
          {confirmed && <div className="success-message"><MdCheckCircle /><div><strong>Table held successfully</strong><span>This is a mock success state for the frontend flow.</span></div></div>}
        </div>
      </div>
    </section>
  )
}
