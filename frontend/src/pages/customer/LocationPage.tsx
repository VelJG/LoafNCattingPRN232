import { useEffect, useMemo, useState } from 'react'
import { MdLocationOn, MdPhone, MdPlace, MdSchedule } from 'react-icons/md'
import { getStoreLocation, type StoreLocation } from '../../features/location/locationApi'

const fallbackLocation: StoreLocation = {
  storeLocationId: 0,
  storeName: 'Loaf’N Catting Cafe',
  address: '128 Nguyễn Huệ, Phường Bến Nghé, Quận 1, TP. Hồ Chí Minh',
  phoneNumber: '028 3822 1188',
  openingHours: '08:00 — 22:00 · HÀNG NGÀY',
  latitude: 10.7743,
  longitude: 106.7036,
}

export function LocationPage() {
  const [location, setLocation] = useState<StoreLocation>(fallbackLocation)
  const [loading, setLoading] = useState(true)
  const [error, setError] = useState(false)

  useEffect(() => {
    let active = true
    setLoading(true)
    setError(false)

    getStoreLocation()
      .then((result) => {
        if (!active) return
        setLocation(result)
      })
      .catch(() => {
        if (!active) return
        setError(true)
      })
      .finally(() => {
        if (active) setLoading(false)
      })

    return () => {
      active = false
    }
  }, [])

  const directionsUrl = useMemo(() => {
    if (Number.isFinite(location.latitude) && Number.isFinite(location.longitude)) {
      return `https://www.google.com/maps/search/?api=1&query=${location.latitude},${location.longitude}`
    }

    return `https://www.google.com/maps/search/?api=1&query=${encodeURIComponent(location.address)}`
  }, [location.address, location.latitude, location.longitude])

  return (
    <section className="customer-v2-page location-v2-page">
      <div className="location-v2-map" role="img" aria-label="Bản đồ vị trí Loaf'N Catting Cafe">
        <div className="location-v2-road location-v2-road--diagonal" />
        <div className="location-v2-road location-v2-road--vertical" />
        <MdLocationOn aria-hidden="true" />
      </div>
      <div className="location-v2-info">
        <div className="location-v2-title">
          <h1>{location.storeName}</h1>
          <p>ĐẾN VÌ CÀ PHÊ, Ở LẠI VÌ NHỮNG BÉ MÈO.</p>
        </div>
        <div className="location-v2-details">
          <div><MdPlace aria-hidden="true" /><span>{location.address}</span></div>
          <div><MdPhone aria-hidden="true" /><span>{location.phoneNumber || 'Đang cập nhật'}</span></div>
          <div><MdSchedule aria-hidden="true" /><span>{location.openingHours || 'Đang cập nhật'}</span></div>
        </div>
        {error && !loading ? <p className="customer-v2-feedback" role="status">Không thể tải dữ liệu mới nhất. Đang hiển thị thông tin hiện có.</p> : null}
        <a className="customer-v2-primary-button" href={directionsUrl} target="_blank" rel="noreferrer">MỞ CHỈ ĐƯỜNG →</a>
      </div>
    </section>
  )
}
