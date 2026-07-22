import { BrandWordmark } from '../brand/BrandWordmark'

export function LoadingScreen() {
  return (
    <main className="session-loading" role="status" aria-live="polite">
      <BrandWordmark />
      <span className="session-loading__mark" aria-hidden="true" />
      <span>Đang xác thực phiên</span>
    </main>
  )
}
