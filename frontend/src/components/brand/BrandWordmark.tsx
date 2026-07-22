import { Link } from 'react-router-dom'

interface BrandWordmarkProps {
  to?: string
  inverse?: boolean
}

export function BrandWordmark({
  to = '/',
  inverse = false,
}: BrandWordmarkProps) {
  return (
    <Link
      className={inverse ? 'brand-wordmark brand-wordmark--inverse' : 'brand-wordmark'}
      to={to}
      aria-label="Loaf'N Catting"
    >
      Loaf<span>'</span>N Catting
    </Link>
  )
}
