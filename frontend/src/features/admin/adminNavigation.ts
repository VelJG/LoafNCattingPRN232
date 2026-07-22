import type { IconType } from 'react-icons'
import {
  MdDashboard,
  MdEvent,
  MdGroup,
  MdLocalCafe,
  MdOutlineReceiptLong,
  MdPets,
  MdStorefront,
  MdTableRestaurant,
} from 'react-icons/md'
import type { UserRole } from '../auth/authModels'

export interface AdminNavigationItem {
  key: string
  to: string
  label: string
  subtitle: string
  icon: IconType
  roles: readonly UserRole[]
  badge?: string
}

export const adminNavigation: readonly AdminNavigationItem[] = [
  { key: 'dashboard', to: '/admin', label: 'Tổng quan', subtitle: 'THỨ SÁU, 10/07/2026 · CA SÁNG', icon: MdDashboard, roles: ['Admin', 'Staff'] },
  { key: 'orders', to: '/admin/orders', label: 'Đơn hàng', subtitle: 'THEO DÕI VÀ CẬP NHẬT ĐƠN', icon: MdOutlineReceiptLong, roles: ['Admin', 'Staff'], badge: '6' },
  { key: 'reservations', to: '/admin/reservations', label: 'Đặt bàn', subtitle: 'LỊCH HẸN VÀ KHÁCH ĐẾN QUÁN', icon: MdEvent, roles: ['Admin', 'Staff'], badge: '3' },
  { key: 'catalog', to: '/admin/catalog', label: 'Thực đơn', subtitle: 'SẢN PHẨM, GIÁ VÀ TỒN KHO', icon: MdLocalCafe, roles: ['Admin', 'Staff'] },
  { key: 'cats', to: '/admin/cats', label: 'Các bé mèo', subtitle: 'HỒ SƠ VÀ TRẠNG THÁI CÁC BÉ', icon: MdPets, roles: ['Admin', 'Staff'] },
  { key: 'tables', to: '/admin/tables', label: 'Quản lý bàn', subtitle: 'SƠ ĐỒ VÀ TRẠNG THÁI BÀN', icon: MdTableRestaurant, roles: ['Admin', 'Staff'] },
  { key: 'users', to: '/admin/users', label: 'Người dùng', subtitle: 'TÀI KHOẢN VÀ PHÂN QUYỀN', icon: MdGroup, roles: ['Admin'] },
  { key: 'store', to: '/admin/store', label: 'Vị trí cửa hàng', subtitle: 'ĐỊA CHỈ, GIỜ MỞ CỬA VÀ TOẠ ĐỘ', icon: MdStorefront, roles: ['Admin'] },
]

export function navigationForRole(role: UserRole | undefined) {
  return adminNavigation.filter((item) => role && item.roles.includes(role))
}

export function navigationForPath(pathname: string) {
  return [...adminNavigation]
    .sort((a, b) => b.to.length - a.to.length)
    .find((item) => item.to === '/admin' ? pathname === '/admin' : pathname.startsWith(item.to))
    ?? adminNavigation[0]
}
