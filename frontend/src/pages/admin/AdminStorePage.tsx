import { MdAccessTime, MdExplore, MdMyLocation, MdPhone, MdPlace, MdStorefront } from 'react-icons/md'

const fields = [
  { label: 'TÊN CỬA HÀNG', value: 'Loaf’N Catting Cafe', icon: MdStorefront },
  { label: 'ĐỊA CHỈ', value: '128 Nguyễn Huệ, Quận 1, TP.HCM', icon: MdPlace },
  { label: 'SỐ ĐIỆN THOẠI', value: '028 3822 1188', icon: MdPhone },
  { label: 'GIỜ MỞ CỬA', value: '08:00 - 22:00 mỗi ngày', icon: MdAccessTime },
  { label: 'VĨ ĐỘ (LATITUDE)', value: '10.774300', icon: MdMyLocation },
  { label: 'KINH ĐỘ (LONGITUDE)', value: '106.703600', icon: MdExplore },
]

export function AdminStorePage() {
  return (
    <section className="admin-page admin-store-page">
      <div className="admin-store-form">
        {fields.map((field) => {
          const Icon = field.icon
          return <label key={field.label}><span>{field.label}</span><div><Icon aria-hidden="true" /><input aria-label={field.label} value={field.value} readOnly /></div></label>
        })}
        <button type="button" disabled aria-describedby="store-api-note">LƯU THAY ĐỔI →</button>
        <p id="store-api-note">Backend chưa hỗ trợ thao tác này.</p>
      </div>
    </section>
  )
}
