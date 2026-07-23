import { useEffect, useState } from 'react'
import { MdAdd, MdLocalCafe } from 'react-icons/md'
import type { Product } from '../../../types/models'
import { formatVnd } from '../../../utils/format'

interface MenuProductCardProps {
  product: Product
  onAdd: (product: Product) => void
}

function stockLabel(product: Product) {
  if (!product.available || product.stock <= 0) return 'HẾT MÓN'
  return `CÒN ${product.stock}`
}

export function MenuProductCard({ product, onAdd }: MenuProductCardProps) {
  const currentPrice = product.discountPrice ?? product.price
  const unavailable = !product.available || product.stock <= 0
  const [imageFailed, setImageFailed] = useState(false)

  useEffect(() => {
    setImageFailed(false)
  }, [product.imageUrl])

  return (
    <article className="menu-v2-product-card">
      <div className="menu-v2-product-card__media">
        {imageFailed ? (
          <div
            className="menu-v2-product-placeholder"
            role="img"
            aria-label={`Ảnh minh họa ${product.name}`}
          >
            <MdLocalCafe aria-hidden="true" />
          </div>
        ) : (
          <img
            src={product.imageUrl}
            alt={product.name}
            loading="lazy"
            onError={() => setImageFailed(true)}
          />
        )}
        <span
          className={unavailable ? 'menu-v2-stock menu-v2-stock--sold' : 'menu-v2-stock'}
        >
          {stockLabel(product)}
        </span>
        {product.badge && <span className="menu-v2-product-badge">{product.badge}</span>}
      </div>

      <div className="menu-v2-product-card__body">
        <p className="menu-v2-product-category">{product.categoryName}</p>
        <h2>{product.name}</h2>
        <p className="menu-v2-product-description">{product.description}</p>

        <div className="menu-v2-product-card__footer">
          <div className="menu-v2-price">
            <strong>{formatVnd(currentPrice)}</strong>
            {product.discountPrice && <del>{formatVnd(product.price)}</del>}
          </div>
          <button
            type="button"
            disabled={unavailable}
            onClick={() => onAdd(product)}
            aria-label={`Thêm ${product.name}`}
          >
            <MdAdd aria-hidden="true" /> THÊM
          </button>
        </div>
      </div>
    </article>
  )
}
