import { Link } from 'react-router-dom'

interface BrandLogoProps {
  compact?: boolean
}

export function BrandLogo({ compact = false }: BrandLogoProps) {
  return (
    <Link className={compact ? 'brand-logo brand-logo--compact' : 'brand-logo'} to="/menu">
      <img src="/loafncatting-logo.png" alt="Loaf and Catting" />
    </Link>
  )
}
