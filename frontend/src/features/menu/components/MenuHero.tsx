import { MdArrowForward } from 'react-icons/md'
import { Link } from 'react-router-dom'

export function MenuHero() {
  return (
    <section className="menu-v2-hero" aria-labelledby="menu-title">
      <div>
        <p className="menu-v2-eyebrow">THỰC ĐƠN HÔM NAY</p>
        <h1 id="menu-title">
          Xin chào, bạn yêu <em>mèo</em>.
        </h1>
        <p className="menu-v2-hero__description">
          Cà phê ngon, bánh xinh và mười hai bé mèo đang chờ bạn.
        </p>
      </div>
      <Link className="menu-v2-primary-action" to="/reservations">
        ĐẶT BÀN NGAY <MdArrowForward aria-hidden="true" />
      </Link>
    </section>
  )
}
