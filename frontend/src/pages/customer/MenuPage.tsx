import { useEffect, useMemo, useState } from 'react'
import {
  MdAdd,
  MdArrowForward,
  MdErrorOutline,
  MdLocalCafe,
  MdPets,
  MdRefresh,
  MdSearch,
} from 'react-icons/md'
import { catalogRepository } from '../../services/catalogRepository'
import { useCart } from '../../state/CartContext'
import type { Category, Product } from '../../types/models'
import { formatVnd } from '../../utils/format'

export function MenuPage() {
  const [keyword, setKeyword] = useState('')
  const [categories, setCategories] = useState<Category[]>([])
  const [categoryError, setCategoryError] = useState(false)
  const [category, setCategory] = useState<number | undefined>(undefined)
  const [items, setItems] = useState<Product[]>([])
  const [loading, setLoading] = useState(true)
  const [error, setError] = useState(false)
  const [retryKey, setRetryKey] = useState(0)
  const cart = useCart()

  useEffect(() => {
    let alive = true
    setCategoryError(false)
    catalogRepository
      .listCategories()
      .then((result) => {
        if (alive) setCategories(result)
      })
      .catch(() => {
        if (alive) {
          setCategories([])
          setCategoryError(true)
        }
      })
    return () => { alive = false }
  }, [retryKey])

  useEffect(() => {
    let alive = true
    setLoading(true)
    setError(false)
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
          setError(true)
          setLoading(false)
        }
      })
    return () => { alive = false }
  }, [keyword, category, retryKey])

  const availableCount = useMemo(
    () => items.filter((item) => item.available).length,
    [items],
  )

  return (
    <>
      <section className="menu-hero page-width">
        <div className="menu-hero__copy">
          <span className="hero-kicker"><MdPets />Thực đơn dành cho những phút thật chậm</span>
          <h1>Một tách cà phê ngon hơn khi có mèo bên cạnh.</h1>
          <p>Chọn đồ uống và bánh mới trong ngày, rồi dành một góc ấm áp bên các bé mèo.</p>
          <div className="hero-actions">
            <a className="button button--light" href="#today-menu">
              Xem thực đơn hôm nay <MdArrowForward />
            </a>
            <span><strong>12</strong> bé mèo đang chờ làm quen</span>
          </div>
        </div>
        <div className="menu-hero__image">
          <img
            src="https://images.unsplash.com/photo-1684246524496-180d5b07ee7e?auto=format&fit=crop&w=1400&q=86"
            alt="Cà phê và bánh ngọt trên chiếc bàn gỗ ấm áp"
          />
          <div className="floating-note">
            <span className="icon-badge"><MdLocalCafe /></span>
            <div><strong>Làm mới mỗi ngày</strong><small>Chuẩn bị riêng cho từng món</small></div>
          </div>
        </div>
      </section>

      <section className="menu-section page-width" id="today-menu">
        <div className="section-heading">
          <div>
            <span className="eyebrow">Hôm nay tại Loaf</span>
            <h2>Chọn món bạn yêu thích</h2>
            <p>{availableCount} món đang sẵn sàng phục vụ.</p>
          </div>
          <div className="menu-toolbar">
            <label className="search-field">
              <MdSearch />
              <input
                type="search"
                value={keyword}
                onChange={(event) => setKeyword(event.target.value)}
                placeholder="Tìm đồ uống hoặc bánh"
                aria-label="Tìm món"
              />
            </label>
          </div>
        </div>

        {categoryError && (
          <div className="category-error" role="alert" aria-label="Lỗi danh mục">
            <span>Không thể tải danh mục.</span>
            <button type="button" onClick={() => setRetryKey((value) => value + 1)}>
              <MdRefresh /> Tải lại danh mục
            </button>
          </div>
        )}

        <div className="category-row" aria-label="Danh mục thực đơn">
          <button
            className={!category ? 'chip chip--active' : 'chip'}
            type="button"
            onClick={() => setCategory(undefined)}
          >
            Tất cả
          </button>
          {categories.map((item) => (
            <button
              className={category === item.id ? 'chip chip--active' : 'chip'}
              type="button"
              key={item.id}
              onClick={() => setCategory(item.id)}
            >
              {item.name}
            </button>
          ))}
        </div>

        {loading ? (
          <div className="product-grid" aria-label="Đang tải thực đơn">
            {[1, 2, 3].map((item) => (
              <div className="product-card product-card--skeleton" key={item} />
            ))}
          </div>
        ) : error ? (
          <div className="menu-error-state" role="alert">
            <MdErrorOutline />
            <div>
              <h3>Không thể tải thực đơn</h3>
              <p>Kết nối đang gián đoạn. Bạn hãy thử lại sau một chút nhé.</p>
            </div>
            <button type="button" onClick={() => setRetryKey((value) => value + 1)}>
              <MdRefresh /> Thử lại
            </button>
          </div>
        ) : items.length === 0 ? (
          <div className="empty-state empty-state--page">
            <span className="icon-badge icon-badge--large"><MdSearch /></span>
            <h3>Chưa tìm thấy món phù hợp</h3>
            <p>Hãy thử tên món hoặc danh mục khác.</p>
          </div>
        ) : (
          <div className="product-grid">
            {items.map((product) => {
              const price = product.discountPrice ?? product.price
              return (
                <article className="product-card" key={product.id}>
                  <div className="product-card__image">
                    <img src={product.imageUrl} alt={product.name} />
                    {product.badge && <span className="product-badge">{product.badge}</span>}
                    {!product.available && <span className="sold-out-badge">Hết món</span>}
                  </div>
                  <div className="product-card__body">
                    <div>
                      <span className="product-category">{product.categoryName}</span>
                      <h3>{product.name}</h3>
                    </div>
                    <p>{product.description}</p>
                    <div className="product-card__footer">
                      <div className="product-price">
                        <strong>{formatVnd(price)}</strong>
                        {product.discountPrice && <del>{formatVnd(product.price)}</del>}
                      </div>
                      <button
                        className="add-button"
                        type="button"
                        disabled={!product.available}
                        onClick={() => cart.add(product)}
                        aria-label={`Thêm ${product.name}`}
                      >
                        <MdAdd /><span>Thêm</span>
                      </button>
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
