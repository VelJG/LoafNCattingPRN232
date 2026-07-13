import { createContext, useContext, useMemo, useState, type PropsWithChildren } from 'react'
import type { CartLine, Product } from '../types/models'

interface CartContextValue {
  items: CartLine[]
  count: number
  total: number
  isOpen: boolean
  add: (product: Product) => void
  decrease: (productId: number) => void
  remove: (productId: number) => void
  open: () => void
  close: () => void
}

const CartContext = createContext<CartContextValue | null>(null)

export function CartProvider({ children }: PropsWithChildren) {
  const [items, setItems] = useState<CartLine[]>([])
  const [isOpen, setIsOpen] = useState(false)

  const add = (product: Product) => {
    setItems((current) => {
      const existing = current.find((line) => line.product.id === product.id)
      if (existing) {
        return current.map((line) =>
          line.product.id === product.id
            ? { ...line, quantity: Math.min(line.quantity + 1, product.stock) }
            : line,
        )
      }
      return [...current, { product, quantity: 1 }]
    })
  }

  const decrease = (productId: number) => {
    setItems((current) =>
      current
        .map((line) =>
          line.product.id === productId ? { ...line, quantity: line.quantity - 1 } : line,
        )
        .filter((line) => line.quantity > 0),
    )
  }

  const remove = (productId: number) =>
    setItems((current) => current.filter((line) => line.product.id !== productId))

  const value = useMemo(
    () => ({
      items,
      count: items.reduce((sum, line) => sum + line.quantity, 0),
      total: items.reduce(
        (sum, line) => sum + (line.product.discountPrice ?? line.product.price) * line.quantity,
        0,
      ),
      isOpen,
      add,
      decrease,
      remove,
      open: () => setIsOpen(true),
      close: () => setIsOpen(false),
    }),
    [items, isOpen],
  )

  return <CartContext.Provider value={value}>{children}</CartContext.Provider>
}

export function useCart() {
  const value = useContext(CartContext)
  if (!value) throw new Error('useCart must be used inside CartProvider')
  return value
}
