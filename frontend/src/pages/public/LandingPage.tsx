import { MdArrowOutward, MdLocationOn, MdSchedule } from 'react-icons/md'
import { Link } from 'react-router-dom'
import {
  landingCats,
  landingHeroImage,
  landingMenu,
} from '../../data/landingContent'

export function LandingPage() {
  return (
    <div className="landing-page">
      <section className="landing-hero" aria-labelledby="landing-title">
        <div className="landing-hero__meta">
          <span>CÀ PHÊ MÈO · QUẬN 1, TP.HCM</span>
          <span>EST. 2021 · 12 CƯ DÂN LÔNG XÙ</span>
        </div>
        <h1 id="landing-title">
          Chỗ ngồi ấm, cà phê <span>đậm</span>, và <em>mèo</em>.
        </h1>
        <div className="landing-hero__grid">
          <div className="landing-hero__copy">
            <p>
              Một quán cà phê nhỏ giữa lòng Sài Gòn, nơi hạt rang mộc gặp
              những người bạn bốn chân được nuông chiều. Không vội vã — chỉ
              có ly cà phê ngon và một chiếc đuôi phe phẩy bên cạnh.
            </p>
            <div className="landing-actions">
              <a className="landing-action landing-action--primary" href="#menu">
                Xem thực đơn <MdArrowOutward />
              </a>
              <Link className="landing-action" to="/login">
                Đăng nhập
              </Link>
              <Link className="landing-text-link" to="/register">
                Tạo tài khoản
              </Link>
            </div>
            <div className="landing-quick-facts">
              <span><MdSchedule /> 08:00 — 22:00</span>
              <span><MdLocationOn /> Đa Kao, Quận 1</span>
            </div>
          </div>
          <figure className="landing-frame">
            <img
              src={landingHeroImage}
              alt="Cà phê và bánh được phục vụ tại một góc bàn ấm cúng"
            />
            <figcaption>
              <span>GÓC CỬA SỔ · 14:30</span>
              <span>N°.01</span>
            </figcaption>
          </figure>
        </div>
      </section>

      <div className="landing-marquee" aria-label="Điểm nổi bật của quán">
        <div>
          <span>Espresso rang mộc</span><i>✳</i><span>Bánh nướng mỗi sáng</span><i>✳</i>
          <span>Mười hai bé mèo</span><i>✳</i><span>Wifi khỏe, ổ cắm mọi bàn</span><i>✳</i>
          <span>Đặt bàn dễ dàng</span><i>✳</i>
        </div>
      </div>

      <section id="menu" className="landing-section">
        <header className="landing-section__heading">
          <span>(01)</span>
          <h2>Thực đơn</h2>
          <Link to="/login">Vào ứng dụng để gọi món <MdArrowOutward /></Link>
        </header>
        <div className="landing-menu-grid">
          {landingMenu.map((item, index) => (
            <article key={item.name}>
              <span className="landing-menu-index">0{index + 1}</span>
              <div>
                <h3>{item.name}</h3>
                <p>{item.description}</p>
              </div>
              <strong>{item.price}</strong>
            </article>
          ))}
        </div>
      </section>

      <section id="cats" className="landing-section landing-cats">
        <header className="landing-section__heading">
          <span>(02)</span>
          <h2>Nhân viên bốn chân</h2>
          <small>12 BÉ · THAY CA MỖI NGÀY</small>
        </header>
        <div className="landing-cats-grid">
          {landingCats.map((cat, index) => (
            <article key={cat.name}>
              <div className="landing-cat-image">
                <img src={cat.image} alt={`${cat.name}, ${cat.breed}`} />
                <span>{String(index + 1).padStart(2, '0')}</span>
              </div>
              <div className="landing-cat-name">
                <h3>{cat.name}</h3>
                <span>{cat.breed}</span>
              </div>
              <p>{cat.note}</p>
            </article>
          ))}
        </div>
      </section>

      <section id="about" className="landing-section landing-about">
        <header className="landing-section__heading">
          <span>(03)</span>
          <h2>Về quán</h2>
        </header>
        <div className="landing-about__grid">
          <p>
            Chúng tôi tin một buổi chiều đẹp chỉ cần ba thứ: ly cà phê pha
            đúng, một góc ngồi không ai giục, và tiếng gừ gừ của một chú mèo
            vừa chọn ngồi cạnh bạn.
          </p>
          <div>
            <p>
              Loaf'N Catting mở cửa từ 2021, bắt đầu từ ba chú mèo được cứu
              và một chiếc máy espresso cũ. Hôm nay, mười hai cư dân lông xù
              gọi nơi này là nhà.
            </p>
            <dl>
              <div><dt>Hạt cà phê</dt><dd>Rang hàng tuần</dd></div>
              <div><dt>Không gian</dt><dd>36 chỗ ngồi</dd></div>
              <div><dt>Thời gian đẹp</dt><dd>14:00 — 17:00</dd></div>
            </dl>
          </div>
        </div>
      </section>

      <section id="reserve" className="landing-reserve">
        <div>
          <span>SẴN SÀNG GHÉ QUÁN?</span>
          <h2>Giữ một chỗ cho buổi chiều chậm rãi.</h2>
        </div>
        <Link to="/login">Đặt bàn <MdArrowOutward /></Link>
      </section>
    </div>
  )
}
