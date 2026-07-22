import { MdRefresh, MdSearch } from 'react-icons/md'
import type { Category } from '../../../types/models'

interface MenuToolbarProps {
  keyword: string
  onKeywordChange: (value: string) => void
  categories: Category[]
  categoryId?: number
  onCategoryChange: (categoryId?: number) => void
  categoriesFailed: boolean
  onRetry: () => void
}

export function MenuToolbar({
  keyword,
  onKeywordChange,
  categories,
  categoryId,
  onCategoryChange,
  categoriesFailed,
  onRetry,
}: MenuToolbarProps) {
  return (
    <>
      {categoriesFailed && (
        <div className="menu-v2-category-error" role="alert" aria-label="Lỗi danh mục">
          <span>Không thể tải danh mục.</span>
          <button type="button" onClick={onRetry}>
            <MdRefresh aria-hidden="true" /> Tải lại danh mục
          </button>
        </div>
      )}

      <div className="menu-v2-toolbar">
        <label className="menu-v2-search">
          <MdSearch aria-hidden="true" />
          <input
            type="search"
            value={keyword}
            onChange={(event) => onKeywordChange(event.target.value)}
            placeholder="Tìm món yêu thích..."
            aria-label="Tìm món"
          />
        </label>

        <div className="menu-v2-categories" aria-label="Danh mục thực đơn">
          <button
            className={categoryId === undefined ? 'is-active' : ''}
            type="button"
            onClick={() => onCategoryChange(undefined)}
          >
            TẤT CẢ
          </button>
          {categories.map((category) => (
            <button
              className={categoryId === category.id ? 'is-active' : ''}
              type="button"
              key={category.id}
              onClick={() => onCategoryChange(category.id)}
            >
              {category.name}
            </button>
          ))}
        </div>
      </div>
    </>
  )
}
