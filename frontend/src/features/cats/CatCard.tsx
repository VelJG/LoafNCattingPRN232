import { useState } from 'react'
import { MdPets } from 'react-icons/md'
import { Link } from 'react-router-dom'
import type { CatProfile } from '../../types/models'

function statusTone(status: string) {
  const normalized = status.toLocaleLowerCase('vi-VN')
  if (normalized.includes('làm việc') || normalized.includes('available')) return 'ok'
  if (normalized.includes('bệnh') || normalized.includes('sick')) return 'info'
  return 'away'
}

export function CatImage({ cat, detail = false }: { cat: CatProfile; detail?: boolean }) {
  const [failed, setFailed] = useState(!cat.imageUrl)
  if (failed) {
    return (
      <div className={detail ? 'cat-v2-image-placeholder cat-v2-image-placeholder--detail' : 'cat-v2-image-placeholder'} role="img" aria-label={`Ảnh minh họa ${cat.name}`}>
        <MdPets aria-hidden="true" />
        {detail && <span>{cat.name.toLocaleUpperCase('vi-VN')}</span>}
      </div>
    )
  }
  return <img src={cat.imageUrl} alt={cat.name} onError={() => setFailed(true)} />
}

export function CatCard({ cat }: { cat: CatProfile }) {
  return (
    <Link className="cat-v2-card" to={`/cats/${cat.id}`} aria-label={`Xem ${cat.name}`}>
      <div className="cat-v2-card__media"><CatImage cat={cat} /></div>
      <div className="cat-v2-card__body">
        <h2>{cat.name}</h2>
        <p>{cat.breed.toLocaleUpperCase('vi-VN')}</p>
        <span className={`cat-v2-status cat-v2-status--${statusTone(cat.status)}`}>{cat.status}</span>
      </div>
    </Link>
  )
}
