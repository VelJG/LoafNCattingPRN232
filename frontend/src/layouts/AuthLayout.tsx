import { MdArrowBack, MdLocalCafe, MdPets } from 'react-icons/md'
import { Link, Outlet } from 'react-router-dom'
import { BrandWordmark } from '../components/brand/BrandWordmark'

export function AuthLayout() {
  return (
    <main className="auth-page">
      <div className="auth-shell">
        <aside className="auth-story">
          <div>
            <BrandWordmark inverse />
            <Link className="auth-back" to="/"><MdArrowBack /> Trang giới thiệu</Link>
          </div>
          <div className="auth-story__copy">
            <span>LOAF'N CATTING · SÀI GÒN</span>
            <h2>Mỗi lần ghé quán nên bắt đầu thật nhẹ.</h2>
            <p>Đăng nhập để dành một chiếc bàn, chọn món trước và biết hôm nay bé mèo nào đang trực.</p>
          </div>
          <div className="auth-story__facts">
            <div><MdLocalCafe /><span><strong>Rang mộc</strong><small>Hạt mới mỗi tuần</small></span></div>
            <div><MdPets /><span><strong>12 cư dân</strong><small>Thay ca mỗi ngày</small></span></div>
          </div>
        </aside>
        <div className="auth-panel"><Outlet /></div>
      </div>
    </main>
  )
}
