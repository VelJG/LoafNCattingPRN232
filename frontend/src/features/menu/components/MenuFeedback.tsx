import { MdErrorOutline, MdRefresh, MdSearch } from 'react-icons/md'

interface MenuFeedbackProps {
  loading: boolean
  failed: boolean
  empty: boolean
  onRetry: () => void
}

export function MenuFeedback({ loading, failed, empty, onRetry }: MenuFeedbackProps) {
  if (loading) {
    return (
      <div className="menu-v2-grid" aria-label="Đang tải thực đơn">
        {[1, 2, 3, 4, 5, 6].map((item) => (
          <div className="menu-v2-product-skeleton" key={item} />
        ))}
      </div>
    )
  }

  if (failed) {
    return (
      <div className="menu-v2-feedback" role="alert">
        <MdErrorOutline aria-hidden="true" />
        <div>
          <h2>Không thể tải thực đơn</h2>
          <p>Kết nối đang gián đoạn. Bạn hãy thử lại sau một chút nhé.</p>
        </div>
        <button type="button" onClick={onRetry}>
          <MdRefresh aria-hidden="true" /> THỬ LẠI
        </button>
      </div>
    )
  }

  if (empty) {
    return (
      <div className="menu-v2-feedback menu-v2-feedback--empty">
        <MdSearch aria-hidden="true" />
        <div>
          <h2>Chưa tìm thấy món phù hợp</h2>
          <p>Hãy thử tên món hoặc danh mục khác.</p>
        </div>
      </div>
    )
  }

  return null
}
