import { useEffect, useMemo, useState } from 'react'
import { MdAccessTime, MdExplore, MdMyLocation, MdPhone, MdPlace, MdStorefront } from 'react-icons/md'
import { getStoreLocation, type StoreLocation } from '../../features/location/locationApi'

const fallbackLocation: StoreLocation = {
  storeLocationId: 0,
  storeName: 'Loaf’N Catting Cafe',
  address: '128 Nguyễn Huệ, Quận 1, TP.HCM',
  phoneNumber: '028 3822 1188',
  openingHours: '08:00 - 22:00 mỗi ngày',
  latitude: 10.7743,
  longitude: 106.7036,
}

export function AdminStorePage() {
  const [location, setLocation] = useState<StoreLocation>(fallbackLocation)
  const [loading, setLoading] = useState(true)
  const [error, setError] = useState(false)

  useEffect(() => {
    let active = true
    setLoading(true)
    setError(false)

    getStoreLocation()
      .then((result) => {
        if (active) setLocation(result)
      })
      .catch(() => {
        if (active) setError(true)
      })
      .finally(() => {
        if (active) setLoading(false)
      })

    return () => {
      active = false
    }
  }, [])

  const fields = useMemo(() => [
    { label: 'TÊN CỬA HÀNG', value: location.storeName, icon: MdStorefront },
    { label: 'ĐỊA CHỈ', value: location.address, icon: MdPlace },
    { label: 'SỐ ĐIỆN THOẠI', value: location.phoneNumber || 'Đang cập nhật', icon: MdPhone },
    { label: 'GIỜ MỞ CỬA', value: location.openingHours || 'Đang cập nhật', icon: MdAccessTime },
    { label: 'VĨ ĐỘ (LATITUDE)', value: location.latitude.toFixed(6), icon: MdMyLocation },
    { label: 'KINH ĐỘ (LONGITUDE)', value: location.longitude.toFixed(6), icon: MdExplore },
  ], [location])

  return (
    <section className="admin-page admin-store-page">
      <div className="admin-store-form">
        {fields.map((field) => {
          const Icon = field.icon
          return <label key={field.label}><span>{field.label}</span><div><Icon aria-hidden="true" /><input aria-label={field.label} value={field.value} readOnly /></div></label>
        })}
        <button type="button" disabled aria-describedby="store-api-note">LƯU THAY ĐỔI →</button>
        <p id="store-api-note">
          {loading
            ? 'Đang tải dữ liệu cửa hàng từ hệ thống.'
            : error
              ? 'Không thể tải dữ liệu mới nhất từ backend. Đang hiển thị thông tin hiện có.'
              : 'Backend hiện hỗ trợ đọc dữ liệu cửa hàng. Chức năng cập nhật vẫn chưa được bật.'}
        </p>
      </div>
    </section>
  )
}
