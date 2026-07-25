import { useCallback, useEffect, useState } from 'react'
import {
  MdAdd,
  MdArrowBack,
  MdInventory2,
  MdLocalCafe,
  MdRefresh,
} from 'react-icons/md'
import { Link, useParams } from 'react-router-dom'
import { catalogRepository } from '../../services/catalogRepository'
import { useCart } from '../../state/CartContext'
import type { Product } from '../../types/models'
import { formatVnd } from '../../utils/format'

function ProductImage({ product }: { product: Product }) {
  const [failed, setFailed] = useState(!product.imageUrl)

  useEffect(() => {
    setFailed(!product.imageUrl)
  }, [product.imageUrl])

  if (failed) {
    return (
      <div
        className="product-detail-v2-placeholder"
        role="img"
        aria-label={`Ảnh minh họa ${product.name}`}
      >
        <MdLocalCafe aria-hidden="true" />
        <span>{product.name.toLocaleUpperCase('vi-VN')}</span>
      </div>
    )
  }

  return (
    <img
      src={product.imageUrl}
      alt={product.name}
      onError={() => setFailed(true)}
    />
  )
}

export function ProductDetailPage() {
  const { productId } = useParams()
  const cart = useCart()
  const [product, setProduct] = useState<Product | null>(null)
  const [loading, setLoading] = useState(true)
  const [failed, setFailed] = useState(false)

  const loadProduct = useCallback(async () => {
    const id = Number(productId)
    if (!Number.isInteger(id) || id <= 0) {
      setProduct(null)
      setFailed(false)
      setLoading(false)
      return
    }

    setLoading(true)
    setFailed(false)
    try {
      setProduct(await catalogRepository.getProduct(id))
    } catch {
      setFailed(true)
    } finally {
      setLoading(false)
    }
  }, [productId])

  useEffect(() => {
    void loadProduct()
  }, [loadProduct])

  if (loading) {
    return (
      <section
        className="customer-v2-page product-detail-v2-page"
        aria-label="Đang tải chi tiết sản phẩm"
      >
        <div className="product-detail-v2-skeleton" />
      </section>
    )
  }

  if (failed) {
    return (
      <section className="customer-v2-page product-detail-v2-page">
        <div className="customer-v2-feedback" role="alert">
          <MdLocalCafe aria-hidden="true" />
          <div>
            <h2>Không thể tải sản phẩm</h2>
            <p>Vui lòng kiểm tra kết nối rồi thử lại.</p>
          </div>
          <button type="button" onClick={() => void loadProduct()}>
            <MdRefresh aria-hidden="true" /> THỬ LẠI
          </button>
        </div>
      </section>
    )
  }

  if (!product) {
    return (
      <section className="customer-v2-page product-detail-v2-page">
        <div className="customer-v2-feedback customer-v2-feedback--empty">
          <MdLocalCafe aria-hidden="true" />
          <div>
            <h2>Không tìm thấy sản phẩm</h2>
            <Link to="/menu">Quay lại thực đơn</Link>
          </div>
        </div>
      </section>
    )
  }

  const unavailable = !product.available || product.stock <= 0
  const currentPrice = product.discountPrice ?? product.price

  return (
    <section className="customer-v2-page product-detail-v2-page">
      <Link className="customer-v2-back-link" to="/menu">
        <MdArrowBack aria-hidden="true" /> QUAY LẠI THỰC ĐƠN
      </Link>

      <div className="product-detail-v2-grid">
        <div className="product-detail-v2-media">
          <ProductImage product={product} />
          <span
            className={
              unavailable
                ? 'product-detail-v2-stock product-detail-v2-stock--sold'
                : 'product-detail-v2-stock'
            }
          >
            {unavailable ? 'HẾT MÓN' : `CÒN ${product.stock}`}
          </span>
        </div>

        <div className="product-detail-v2-copy">
          <p className="product-detail-v2-category">
            {product.categoryName}
          </p>
          <h1>{product.name}</h1>
          <p className="product-detail-v2-description">
            {product.description || 'Một món ngon được chuẩn bị tại Loaf’N Catting.'}
          </p>

          <div className="product-detail-v2-price">
            <strong>{formatVnd(currentPrice)}</strong>
            {product.discountPrice && <del>{formatVnd(product.price)}</del>}
          </div>

          <div className="product-detail-v2-meta">
            <MdInventory2 aria-hidden="true" />
            <div>
              <span>TÌNH TRẠNG</span>
              <strong>
                {unavailable
                  ? 'Tạm hết món'
                  : `${product.stock} sản phẩm sẵn sàng`}
              </strong>
            </div>
          </div>

          <button
            className="product-detail-v2-add"
            type="button"
            disabled={unavailable || cart.isMutating}
            onClick={() => void cart.add(product)}
          >
            <MdAdd aria-hidden="true" />
            {cart.isMutating ? 'ĐANG THÊM...' : 'THÊM VÀO GIỎ'}
          </button>
        </div>
      </div>
    </section>
  )
}
