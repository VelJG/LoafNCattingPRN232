import { MenuFeedback } from '../../features/menu/components/MenuFeedback'
import { MenuHero } from '../../features/menu/components/MenuHero'
import { MenuProductCard } from '../../features/menu/components/MenuProductCard'
import { MenuToolbar } from '../../features/menu/components/MenuToolbar'
import { useMenuCatalog } from '../../features/menu/useMenuCatalog'
import { useCart } from '../../state/CartContext'

export function MenuPage() {
  const menu = useMenuCatalog()
  const cart = useCart()

  return (
    <section className="menu-v2-page">
      <MenuHero />
      <MenuToolbar
        keyword={menu.keyword}
        onKeywordChange={menu.setKeyword}
        categories={menu.categories}
        categoryId={menu.categoryId}
        onCategoryChange={menu.setCategoryId}
        categoriesFailed={menu.categoriesFailed}
        onRetry={menu.retry}
      />

      <MenuFeedback
        loading={menu.loading}
        failed={menu.productsFailed}
        empty={menu.products.length === 0}
        onRetry={menu.retry}
      />

      {!menu.loading && !menu.productsFailed && menu.products.length > 0 && (
        <div className="menu-v2-grid">
          {menu.products.map((product) => (
            <MenuProductCard product={product} onAdd={cart.add} key={product.id} />
          ))}
        </div>
      )}
    </section>
  )
}
