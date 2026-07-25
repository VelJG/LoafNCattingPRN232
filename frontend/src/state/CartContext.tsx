import {
  createContext,
  useCallback,
  useContext,
  useEffect,
  useMemo,
  useState,
  type PropsWithChildren,
} from 'react'
import { ApiError } from '../api/httpClient'
import {
  cartApi,
  type ApiCart,
  type CartGateway,
  type CheckoutInput,
  type CheckoutOptions,
  type CheckoutOrder,
} from '../features/cart/cartApi'
import { useAuth } from '../features/auth/useAuth'
import {
  createPaymentLink,
  type PaymentLink,
} from '../features/orders/orderApi'
import type { CartLine, Product } from '../types/models'

interface CartContextValue {
  items: CartLine[]
  count: number
  total: number
  isOpen: boolean
  isLoading: boolean
  isMutating: boolean
  error: string | null
  toast: string | null
  checkoutOptions: CheckoutOptions | null
  completedOrder: CheckoutOrder | null
  add: (product: Product) => Promise<void>
  decrease: (productId: number) => Promise<void>
  remove: (productId: number) => Promise<void>
  clear: () => Promise<void>
  checkout: (input: CheckoutInput) => Promise<void>
  dismissError: () => void
  dismissCompletedOrder: () => void
  dismissToast: () => void
  open: () => void
  close: () => void
}

const CartContext = createContext<CartContextValue | null>(null)

interface CartProviderProps extends PropsWithChildren {
  gateway?: CartGateway
  paymentLinkCreator?: (token: string, orderId: number) => Promise<PaymentLink>
  paymentRedirect?: (checkoutUrl: string) => void
}

function errorMessage(error: unknown) {
  return error instanceof ApiError
    ? error.detail
    : 'Could not update your cart. Please try again.'
}

function fallbackProduct(item: ApiCart['items'][number]): Product {
  return {
    id: item.productId,
    name: item.productName,
    description: '',
    categoryId: 0,
    categoryName: '',
    price: item.unitPrice,
    stock: item.availableStock,
    available: item.isAvailable,
    imageUrl: item.picture ?? '',
  }
}

function redirectToPayment(checkoutUrl: string) {
  window.location.assign(checkoutUrl)
}

export function CartProvider({
  children,
  gateway = cartApi,
  paymentLinkCreator = createPaymentLink,
  paymentRedirect = redirectToPayment,
}: CartProviderProps) {
  const auth = useAuth()
  const token = auth.session?.user.role === 'Customer'
    ? auth.session.token
    : null
  const [items, setItems] = useState<CartLine[]>([])
  const [isOpen, setIsOpen] = useState(false)
  const [isLoading, setIsLoading] = useState(false)
  const [isMutating, setIsMutating] = useState(false)
  const [error, setError] = useState<string | null>(null)
  const [checkoutOptions, setCheckoutOptions] = useState<CheckoutOptions | null>(null)
  const [completedOrder, setCompletedOrder] = useState<CheckoutOrder | null>(null)
  const [toast, setToast] = useState<string | null>(null)

  useEffect(() => {
    if (!toast) return
    const timer = window.setTimeout(() => setToast(null), 2600)
    return () => window.clearTimeout(timer)
  }, [toast])

  const applyCart = useCallback((cart: ApiCart, hint?: Product) => {
    setItems((current) => {
      const knownProducts = new Map(
        current.map((line) => [line.product.id, line.product]),
      )
      if (hint) knownProducts.set(hint.id, hint)

      return cart.items.map((item) => ({
        product: knownProducts.get(item.productId) ?? fallbackProduct(item),
        quantity: item.quantity,
      }))
    })
  }, [])

  useEffect(() => {
    if (!token) {
      setItems([])
      setCheckoutOptions(null)
      setCompletedOrder(null)
      setError(null)
      return
    }

    const controller = new AbortController()
    let active = true
    setIsLoading(true)
    setError(null)
    Promise.all([
      gateway.get(token, controller.signal),
      gateway.getCheckoutOptions(token, controller.signal),
    ])
      .then(([cart, options]) => {
        if (!active) return
        applyCart(cart)
        setCheckoutOptions(options)
      })
      .catch((caught) => {
        if (active && !controller.signal.aborted) {
          setError(errorMessage(caught))
        }
      })
      .finally(() => {
        if (active) setIsLoading(false)
      })

    return () => {
      active = false
      controller.abort()
    }
  }, [applyCart, gateway, token])

  const runMutation = useCallback(async (
    action: () => Promise<ApiCart>,
    hint?: Product,
  ) => {
    if (isMutating) return false
    setIsMutating(true)
    setError(null)
    setCompletedOrder(null)
    try {
      applyCart(await action(), hint)
      return true
    } catch (caught) {
      setError(errorMessage(caught))
      return false
    } finally {
      setIsMutating(false)
    }
  }, [applyCart, isMutating])

  const add = useCallback(async (product: Product) => {
    if (!token) return
    const ok = await runMutation(() => gateway.add(token, product.id, 1), product)
    if (ok) setToast(`Đã thêm "${product.name}" vào giỏ hàng`)
  }, [gateway, runMutation, token])

  const decrease = useCallback(async (productId: number) => {
    if (!token) return
    const current = items.find((line) => line.product.id === productId)
    if (!current) return
    const nextQuantity = Math.max(0, current.quantity - 1)
    await runMutation(() => gateway.update(token, productId, nextQuantity))
  }, [gateway, items, runMutation, token])

  const remove = useCallback(async (productId: number) => {
    if (!token) return
    await runMutation(() => gateway.remove(token, productId))
  }, [gateway, runMutation, token])

  const clear = useCallback(async () => {
    if (!token) return
    await runMutation(() => gateway.clear(token))
  }, [gateway, runMutation, token])

  const checkout = useCallback(async (input: CheckoutInput) => {
    if (!token || isMutating) return
    setIsMutating(true)
    setError(null)
    setCompletedOrder(null)
    try {
      const order = await gateway.checkout(token, input)
      setItems([])
      setCompletedOrder(order)
      const payment = await paymentLinkCreator(token, order.orderId)
      paymentRedirect(payment.checkoutUrl)
    } catch (caught) {
      setError(errorMessage(caught))
    } finally {
      setIsMutating(false)
    }
  }, [
    gateway,
    isMutating,
    paymentLinkCreator,
    paymentRedirect,
    token,
  ])

  const value = useMemo<CartContextValue>(
    () => ({
      items,
      count: items.reduce((sum, line) => sum + line.quantity, 0),
      total: items.reduce(
        (sum, line) => sum +
          (line.product.discountPrice ?? line.product.price) * line.quantity,
        0,
      ),
      isOpen,
      isLoading,
      isMutating,
      error,
      toast,
      checkoutOptions,
      completedOrder,
      add,
      decrease,
      remove,
      clear,
      checkout,
      dismissError: () => setError(null),
      dismissCompletedOrder: () => setCompletedOrder(null),
      dismissToast: () => setToast(null),
      open: () => setIsOpen(true),
      close: () => setIsOpen(false),
    }),
    [
      add,
      checkout,
      checkoutOptions,
      clear,
      completedOrder,
      decrease,
      error,
      toast,
      isLoading,
      isMutating,
      isOpen,
      items,
      remove,
    ],
  )

  return <CartContext.Provider value={value}>{children}</CartContext.Provider>
}

export function useCart() {
  const value = useContext(CartContext)
  if (!value) throw new Error('useCart must be used inside CartProvider')
  return value
}
