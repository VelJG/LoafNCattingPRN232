import { useCallback, useEffect, useMemo, useState } from 'react'
import { MdPets, MdSearch } from 'react-icons/md'
import { CatCard } from '../../features/cats/CatCard'
import { catalogRepository } from '../../services/catalogRepository'
import type { CatProfile } from '../../types/models'

export function CatsPage() {
  const [cats, setCats] = useState<CatProfile[]>([])
  const [query, setQuery] = useState('')
  const [loading, setLoading] = useState(true)
  const [error, setError] = useState(false)

  const loadCats = useCallback(async () => {
    setLoading(true)
    setError(false)
    try {
      setCats(await catalogRepository.listCats())
    } catch {
      setError(true)
    } finally {
      setLoading(false)
    }
  }, [])

  useEffect(() => { void loadCats() }, [loadCats])

  const visibleCats = useMemo(() => {
    const keyword = query.trim().toLocaleLowerCase('vi-VN')
    if (!keyword) return cats
    return cats.filter((cat) =>
      `${cat.name} ${cat.breed}`.toLocaleLowerCase('vi-VN').includes(keyword),
    )
  }, [cats, query])

  return (
    <section className="customer-v2-page cats-v2-page">
      <header className="cats-v2-heading">
        <span className="cats-v2-heart">(♥)</span>
        <h1>Nhân viên bốn chân</h1>
        <label className="cats-v2-search">
          <MdSearch aria-hidden="true" />
          <input
            type="search"
            aria-label="Tìm bé mèo"
            placeholder="Tìm bé mèo..."
            value={query}
            onChange={(event) => setQuery(event.target.value)}
          />
        </label>
      </header>

      {loading && (
        <div className="cats-v2-grid" aria-label="Đang tải danh sách mèo">
          {Array.from({ length: 4 }, (_, index) => <div className="cat-v2-skeleton" key={index} />)}
        </div>
      )}
      {!loading && error && (
        <div className="customer-v2-feedback" role="alert">
          <MdPets aria-hidden="true" />
          <div><h2>Không thể tải danh sách mèo</h2><p>Vui lòng kiểm tra kết nối rồi thử lại.</p></div>
          <button type="button" onClick={() => void loadCats()}>THỬ LẠI</button>
        </div>
      )}
      {!loading && !error && visibleCats.length === 0 && (
        <div className="customer-v2-feedback customer-v2-feedback--empty">
          <MdPets aria-hidden="true" />
          <div><h2>Chưa tìm thấy bé mèo nào</h2><p>Thử một tên hoặc giống mèo khác nhé.</p></div>
        </div>
      )}
      {!loading && !error && visibleCats.length > 0 && (
        <div className="cats-v2-grid">
          {visibleCats.map((cat) => <CatCard cat={cat} key={cat.id} />)}
        </div>
      )}
    </section>
  )
}
