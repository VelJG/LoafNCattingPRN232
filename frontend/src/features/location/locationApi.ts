import { requestJson } from '../../api/httpClient'

export interface StoreLocation {
  storeLocationId: number
  storeName: string
  address: string
  phoneNumber?: string | null
  openingHours?: string | null
  latitude: number
  longitude: number
}

export function getStoreLocation() {
  return requestJson<StoreLocation>('/store-location')
}
