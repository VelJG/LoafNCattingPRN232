import { useCallback, useEffect, useState } from 'react'
import { MdPets } from 'react-icons/md'
import { Link, useParams } from 'react-router-dom'
import { CatImage } from '../../features/cats/CatCard'
import { catalogRepository } from '../../services/catalogRepository'
import type { CatProfile } from '../../types/models'

function metric(value?: number) {
  return `${value ?? '—'}/5`
}

export function CatDetailPage() {
  const { catId } = useParams()
  const [cat, setCat] = useState<CatProfile | null>(null)
  const [loading, setLoading] = useState(true)
  const [error, setError] = useState(false)

  const loadCat = useCallback(async () => {
    setLoading(true)
    setError(false)
    try {
      const items = await catalogRepository.listCats()
      setCat(items.find((item) => item.id === Number(catId)) ?? null)
    } catch {
      setError(true)
    } finally {
      setLoading(false)
    }
  }, [catId])

  useEffect(() => { void loadCat() }, [loadCat])

  if (loading) return <section className="customer-v2-page cat-detail-v2-page" aria-label="Đang tải thông tin mèo"><div className="cat-v2-skeleton" /></section>
  if (error) return <section className="customer-v2-page"><div className="customer-v2-feedback" role="alert"><MdPets /><div><h2>Không thể tải thông tin mèo</h2><p>Vui lòng thử lại.</p></div><button type="button" onClick={() => void loadCat()}>THỬ LẠI</button></div></section>
  if (!cat) return <section className="customer-v2-page"><div className="customer-v2-feedback customer-v2-feedback--empty"><MdPets /><div><h2>Không tìm thấy bé mèo</h2><Link to="/cats">Quay lại danh sách mèo</Link></div></div></section>

  const meta = [cat.breed, cat.gender, cat.age ? `${cat.age} tuổi` : null]
    .filter(Boolean).join(' · ').toLocaleUpperCase('vi-VN')

  return (
    <section className="customer-v2-page cat-detail-v2-page">
      <Link className="customer-v2-back-link" to="/cats">← QUAY LẠI DANH SÁCH MÈO</Link>
      <div className="cat-detail-v2-grid">
        <div className="cat-detail-v2-media"><CatImage cat={cat} detail /></div>
        <div className="cat-detail-v2-copy">
          <h1>{cat.name}</h1>
          <p className="cat-detail-v2-meta">{meta}</p>
          <span className="cat-v2-status">{cat.status}</span>
          <p className="cat-detail-v2-description">{cat.description}</p>
          <div className="cat-detail-v2-metrics">
            <div><strong>{metric(cat.friendliness)}</strong><span>THÂN THIỆN</span></div>
            <div><strong>{metric(cat.cuteness)}</strong><span>ĐÁNG YÊU</span></div>
            <div><strong>{metric(cat.playfulness)}</strong><span>TINH NGHỊCH</span></div>
          </div>
        </div>
      </div>
    </section>
  )
}
