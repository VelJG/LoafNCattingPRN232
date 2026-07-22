import { useEffect, useState } from 'react'
import { MdFavorite, MdPets, MdSchedule } from 'react-icons/md'
import { catalogRepository } from '../../services/catalogRepository'
import type { CatProfile } from '../../types/models'

export function CatsPage() {
  const [items, setItems] = useState<CatProfile[]>([])
  const [status, setStatus] = useState<string>('All')

  useEffect(() => { catalogRepository.listCats().then(setItems).catch(() => setItems([])) }, [])
  const statuses = ['All', ...new Set(items.map((cat) => cat.status))]
  const visible = status === 'All' ? items : items.filter((cat) => cat.status === status)

  return (
    <section className="content-page page-width">
      <div className="playful-hero">
        <div><span className="hero-kicker hero-kicker--orange"><MdPets />Meet the residents</span><h1>Every cat has a story.</h1><p>See who is at the cafe today and learn how each resident likes to make friends.</p></div>
        <div className="playful-hero__stamp"><MdFavorite /><strong>{items.length}</strong><span>cats call Loaf home</span></div>
      </div>
      <div className="section-heading section-heading--compact"><div><span className="eyebrow">Cat gallery</span><h2>Who would you like to meet?</h2></div><div className="category-row">{statuses.map((item) => <button className={status === item ? 'chip chip--active' : 'chip'} type="button" key={item} onClick={() => setStatus(item)}>{item}</button>)}</div></div>
      <div className="cat-grid">
        {visible.map((cat) => (
          <article className="cat-card" key={cat.id}>
            <div className="cat-card__image"><img src={cat.imageUrl} alt={cat.name} /><span className={`status-pill status-pill--${cat.status.toLowerCase().replaceAll(' ', '-')}`}>{cat.status}</span></div>
            <div className="cat-card__body"><div><h3>{cat.name}</h3><span>{cat.breed}{cat.age ? ` · ${cat.age} years` : ''}</span></div><p>{cat.description}</p><div className="cat-traits"><span><MdFavorite />Friendly {cat.friendliness ?? '—'}/5</span><span><MdSchedule />Playful {cat.playfulness ?? '—'}/5</span></div></div>
          </article>
        ))}
      </div>
    </section>
  )
}
