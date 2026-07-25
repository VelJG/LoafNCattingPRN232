import { render, screen, waitFor } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { describe, expect, it, vi } from 'vitest'
import {
  AuthContext,
  type AuthContextValue,
} from '../features/auth/AuthProvider'
import type { CartGateway } from '../features/cart/cartApi'
import { CartProvider, useCart } from './CartContext'

const auth: AuthContextValue = {
  status: 'authenticated',
  session: {
    token: 'customer-token',
    expiresAtUtc: '2030-01-01T00:00:00Z',
    user: {
      userId: 7,
      name: 'Customer',
      email: 'customer@example.com',
      phoneNumber: '0900000001',
      address: null,
      role: 'Customer',
      isActive: true,
      isEmailVerified: false,
    },
  },
  login: vi.fn(),
  register: vi.fn(),
  logout: vi.fn(),
}

function CheckoutHarness() {
  const cart = useCart()

  return (
    <>
      <button
        type="button"
        onClick={() => void cart.checkout({
          orderType: 'Takeaway',
          tableId: null,
          reservationId: null,
          paymentMethodId: 2,
          note: null,
        })}
      >
        Checkout
      </button>
      <span data-testid="completed-order">
        {cart.completedOrder?.orderId ?? 'none'}
      </span>
      {cart.error && <span role="alert">{cart.error}</span>}
    </>
  )
}

function createGateway(): CartGateway {
  const emptyCart = {
    cartId: 1,
    userId: 7,
    items: [],
    total: 0,
  }

  return {
    get: vi.fn().mockResolvedValue(emptyCart),
    getCheckoutOptions: vi.fn().mockResolvedValue({
      orderTypes: ['Takeaway'],
      paymentMethods: [{
        paymentMethodId: 2,
        name: 'Bank transfer',
        description: null,
      }],
    }),
    add: vi.fn().mockResolvedValue(emptyCart),
    update: vi.fn().mockResolvedValue(emptyCart),
    remove: vi.fn().mockResolvedValue(emptyCart),
    clear: vi.fn().mockResolvedValue(emptyCart),
    checkout: vi.fn().mockResolvedValue({
      orderId: 42,
      totalPrice: 28_000,
      orderStatusId: 1,
      orderStatusName: 'Pending',
    }),
  }
}

describe('CartProvider checkout payment flow', () => {
  it('creates a PayOS link and redirects after the order is created', async () => {
    const gateway = createGateway()
    const paymentLinkCreator = vi.fn().mockResolvedValue({
      orderId: 42,
      orderCode: 123456,
      amount: 28_000,
      checkoutUrl: 'https://pay.payos.vn/web/checkout/123456',
      qrCode: 'qr-code',
      paymentLinkId: 'payment-link-id',
    })
    const paymentRedirect = vi.fn()

    render(
      <AuthContext.Provider value={auth}>
        <CartProvider
          gateway={gateway}
          paymentLinkCreator={paymentLinkCreator}
          paymentRedirect={paymentRedirect}
        >
          <CheckoutHarness />
        </CartProvider>
      </AuthContext.Provider>,
    )

    await userEvent.click(screen.getByRole('button', { name: 'Checkout' }))

    await waitFor(() => {
      expect(paymentLinkCreator).toHaveBeenCalledWith('customer-token', 42)
    })
    expect(paymentRedirect).toHaveBeenCalledWith(
      'https://pay.payos.vn/web/checkout/123456',
    )
    expect(screen.getByTestId('completed-order')).toHaveTextContent('42')
  })
})
