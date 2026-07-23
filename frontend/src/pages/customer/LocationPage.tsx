import { MdLocationOn, MdPhone, MdPlace, MdSchedule } from 'react-icons/md'

const directionsUrl = 'https://www.google.com/maps/search/?api=1&query=128+Nguyễn+Huệ,+Phường+Bến+Nghé,+Quận+1,+TP.+Hồ+Chí+Minh'

export function LocationPage() {
  return (
    <section className="customer-v2-page location-v2-page">
      <div className="location-v2-map" role="img" aria-label="Bản đồ vị trí Loaf'N Catting Cafe">
        <div className="location-v2-road location-v2-road--diagonal" />
        <div className="location-v2-road location-v2-road--vertical" />
        <MdLocationOn aria-hidden="true" />
      </div>
      <div className="location-v2-info">
        <div className="location-v2-title">
          <h1>Loaf’N Catting Cafe</h1>
          <p>ĐẾN VÌ CÀ PHÊ, Ở LẠI VÌ NHỮNG BÉ MÈO.</p>
        </div>
        <div className="location-v2-details">
          <div><MdPlace aria-hidden="true" /><span>128 Nguyễn Huệ, Phường Bến Nghé, Quận 1, TP. Hồ Chí Minh</span></div>
          <div><MdPhone aria-hidden="true" /><span>028 3822 1188</span></div>
          <div><MdSchedule aria-hidden="true" /><span>08:00 — 22:00 · HÀNG NGÀY</span></div>
        </div>
        <a className="customer-v2-primary-button" href={directionsUrl} target="_blank" rel="noreferrer">MỞ CHỈ ĐƯỜNG →</a>
      </div>
    </section>
  )
}
