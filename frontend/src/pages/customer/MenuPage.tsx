import { useEffect, useMemo, useState } from 'react'
import { MdAdd, MdArrowForward, MdLocalCafe, MdPets, MdSearch } from 'react-icons/md'
import { catalogRepository } from '../../services/catalogRepository'
import { useCart } from '../../state/CartContext'
import type { Category, Product } from '../../types/models'
import { formatVnd } from '../../utils/format'

export function MenuPage() {
  const [keyword, setKeyword] = useState('')
  const [categories, setCategories] = useState<Category[]>([])
  const [category, setCategory] = useState<number | undefined>(undefined)
  const [items, setItems] = useState<Product[]>([])
  const [loading, setLoading] = useState(true)
  const cart = useCart()

  useEffect(() => {
    catalogRepository.listCategories().then(setCategories).catch(() => setCategories([]))
  }, [])

  useEffect(() => {
    let alive = true
    setLoading(true)
    catalogRepository
      .listProducts({ keyword, categoryId: category })
      .then((result) => {
        if (alive) {
          setItems(result)
          setLoading(false)
        }
      })
      .catch(() => {
        if (alive) {
          setItems([])
          setLoading(false)
        }
      })
    return () => { alive = false }
  }, [keyword, category])

  const availableCount = useMemo(() => items.filter((item) => item.available).length, [items])

  return (
    <>
      <section className="menu-hero page-width">
        <div className="menu-hero__copy">
          <span className="hero-kicker"><MdPets />Made for slow, cozy moments</span>
          <h1>Coffee tastes better with a cat nearby.</h1>
          <p>Explore today’s drinks and fresh bakes, then save a table in our cat lounge.</p>
          <div className="hero-actions">
            <a className="button button--light" href="#today-menu">Browse today’s menu <MdArrowForward /></a>
            <span><strong>12</strong> friendly cats on today’s roster</span>
          </div>
        </div>
        <div className="menu-hero__image">
          <img src="https://images.unsplash.com/photo-1684246524496-180d5b07ee7e?auto=format&fit=crop&w=1400&q=86" alt="Coffee and pastries served on a warm cafe table" />
          <div className="floating-note"><span className="icon-badge"><MdLocalCafe /></span><div><strong>Freshly made</strong><small>Every order, every day</small></div></div>
        </div>
      </section>

      <section className="menu-section page-width" id="today-menu">
        <div className="section-heading">
          <div><span className="eyebrow">Today at Loaf</span><h2>Find your cafe favorite</h2><p>{availableCount} items available right now.</p></div>
          <div className="menu-toolbar">
            <label className="search-field"><MdSearch /><input value={keyword} onChange={(event) => setKeyword(event.target.value)} placeholder="Search drinks and treats" /></label>
          </div>
        </div>

        <div className="category-row" aria-label="Menu categories">
          <button className={!category ? 'chip chip--active' : 'chip'} type="button" onClick={() => setCategory(undefined)}>All menu</button>
          {categories.map((item) => <button className={category === item.id ? 'chip chip--active' : 'chip'} type="button" key={item.id} onClick={() => setCategory(item.id)}>{item.name}</button>)}
        </div>

        {loading ? (
          <div className="product-grid" aria-label="Loading menu">
            {[1, 2, 3].map((item) => <div className="product-card product-card--skeleton" key={item} />)}
          </div>
        ) : items.length === 0 ? (
          <div className="empty-state empty-state--page"><span className="icon-badge icon-badge--large"><MdSearch /></span><h3>No menu items found</h3><p>Try a different name or category.</p></div>
        ) : (
          <div className="product-grid">
            {items.map((product) => {
              const price = product.discountPrice ?? product.price
              return (
                <article className="product-card" key={product.id}>
                  <div className="product-card__image">
                    <img src={product.imageUrl} alt={product.name} />
                    {product.badge && <span className="product-badge">{product.badge}</span>}
                    {!product.available && <span className="sold-out-badge">Sold out</span>}
                  </div>
                  <div className="product-card__body">
                    <div><span className="product-category">{product.categoryName}</span><h3>{product.name}</h3></div>
                    <p>{product.description}</p>
                    <div className="product-card__footer">
                      <div className="product-price"><strong>{formatVnd(price)}</strong>{product.discountPrice && <del>{formatVnd(product.price)}</del>}</div>
                      <button className="add-button" type="button" disabled={!product.available} onClick={() => cart.add(product)}><MdAdd /><span>Add</span></button>
                    </div>
                  </div>
                </article>
              )
            })}
          </div>
        )}
      </section>
    </>
  )
}
