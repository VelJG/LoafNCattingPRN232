import type {
  CafeTable,
  CatProfile,
  Category,
  DashboardMetric,
  Product,
  RecentOrder,
} from '../types/models'

export const categories: Category[] = [
  { id: 1, name: 'Coffee' },
  { id: 2, name: 'Tea & Matcha' },
  { id: 3, name: 'Cakes' },
  { id: 4, name: 'Cafe combos' },
]

export const products: Product[] = [
  {
    id: 1,
    name: 'Caramel Catpuccino',
    description: 'Double espresso, silky milk and house caramel.',
    categoryId: 1,
    categoryName: 'Coffee',
    price: 59000,
    stock: 18,
    available: true,
    badge: 'Best seller',
    imageUrl:
      'https://images.unsplash.com/photo-1509042239860-f550ce710b93?auto=format&fit=crop&w=900&q=82',
  },
  {
    id: 2,
    name: 'Orange Cold Brew',
    description: 'Slow-steeped coffee brightened with fresh orange.',
    categoryId: 1,
    categoryName: 'Coffee',
    price: 65000,
    discountPrice: 59000,
    stock: 12,
    available: true,
    badge: 'New',
    imageUrl:
      'https://images.unsplash.com/photo-1517701550927-30cf4ba1dba5?auto=format&fit=crop&w=900&q=82',
  },
  {
    id: 3,
    name: 'Cloud Matcha Latte',
    description: 'Ceremonial matcha with lightly sweetened fresh milk.',
    categoryId: 2,
    categoryName: 'Tea & Matcha',
    price: 62000,
    stock: 9,
    available: true,
    imageUrl:
      'https://images.unsplash.com/photo-1515823662972-da6a2e4d3002?auto=format&fit=crop&w=900&q=82',
  },
  {
    id: 4,
    name: 'Butter Paw Croissant',
    description: 'Flaky butter croissant baked fresh every morning.',
    categoryId: 3,
    categoryName: 'Cakes',
    price: 42000,
    stock: 7,
    available: true,
    imageUrl:
      'https://images.unsplash.com/photo-1555507036-ab1f4038808a?auto=format&fit=crop&w=900&q=82',
  },
  {
    id: 5,
    name: 'Strawberry Loaf Cake',
    description: 'Vanilla sponge, fresh cream and seasonal berries.',
    categoryId: 3,
    categoryName: 'Cakes',
    price: 72000,
    stock: 5,
    available: true,
    badge: 'Limited',
    imageUrl:
      'https://images.unsplash.com/photo-1578985545062-69928b1d9587?auto=format&fit=crop&w=900&q=82',
  },
  {
    id: 6,
    name: 'Cozy Cat Combo',
    description: 'One signature drink and one pastry of your choice.',
    categoryId: 4,
    categoryName: 'Cafe combos',
    price: 99000,
    stock: 0,
    available: false,
    imageUrl:
      'https://images.unsplash.com/photo-1684246524496-180d5b07ee7e?auto=format&fit=crop&w=900&q=82',
  },
]

export const cats: CatProfile[] = [
  {
    id: 1,
    name: 'Mochi',
    breed: 'British Shorthair',
    age: 2,
    gender: 'Female',
    status: 'At the cafe',
    description: 'Quiet, gentle and always ready for a window-side nap.',
    friendliness: 5,
    playfulness: 3,
    imageUrl:
      'https://images.unsplash.com/photo-1495360010541-f48722b34f7d?auto=format&fit=crop&w=900&q=82',
  },
  {
    id: 2,
    name: 'Biscuit',
    breed: 'Orange Tabby',
    age: 3,
    gender: 'Male',
    status: 'At the cafe',
    description: 'Social, curious and very interested in paper bags.',
    friendliness: 5,
    playfulness: 5,
    imageUrl:
      'https://images.unsplash.com/photo-1573865526739-10659fec78a5?auto=format&fit=crop&w=900&q=82',
  },
  {
    id: 3,
    name: 'Sesame',
    breed: 'Tuxedo',
    age: 4,
    gender: 'Female',
    status: 'Resting',
    description: 'A calm observer who prefers soft blankets and slow blinks.',
    friendliness: 4,
    playfulness: 2,
    imageUrl:
      'https://images.unsplash.com/photo-1518791841217-8f162f1e1131?auto=format&fit=crop&w=900&q=82',
  },
  {
    id: 4,
    name: 'Tofu',
    breed: 'Ragdoll',
    age: 1,
    gender: 'Male',
    status: 'In care',
    description: 'Young, fluffy and taking a short break from cafe duty.',
    friendliness: 4,
    playfulness: 4,
    imageUrl:
      'https://images.unsplash.com/photo-1533738363-b7f9aef128ce?auto=format&fit=crop&w=900&q=82',
  },
]

export const cafeTables: CafeTable[] = [
  { id: 1, name: 'Window 01', area: 'Cat lounge', capacity: 2, available: true },
  { id: 2, name: 'Garden 04', area: 'Quiet corner', capacity: 4, available: true },
  { id: 3, name: 'Loft 02', area: 'Upper floor', capacity: 6, available: true },
  { id: 4, name: 'Window 03', area: 'Cat lounge', capacity: 2, available: false },
]

export const dashboardMetrics: DashboardMetric[] = [
  { id: 'orders', label: 'Pending orders', value: '12', note: '4 need attention', tone: 'orange' },
  { id: 'reservations', label: "Today's bookings", value: '18', note: '72 guests expected', tone: 'green' },
  { id: 'stock', label: 'Low stock items', value: '5', note: 'Check before evening', tone: 'rose' },
  { id: 'cats', label: 'Cats on shift', value: '9 / 12', note: '3 are resting', tone: 'blue' },
]

export const recentOrders: RecentOrder[] = [
  { id: '#LC-1048', customer: 'Minh Anh', items: 3, total: 173000, status: 'Processing', time: '10:42' },
  { id: '#LC-1047', customer: 'Quang Huy', items: 2, total: 118000, status: 'Pending', time: '10:35' },
  { id: '#LC-1046', customer: 'Ngoc Ha', items: 4, total: 226000, status: 'Completed', time: '10:18' },
  { id: '#LC-1045', customer: 'Bao Tran', items: 1, total: 59000, status: 'Completed', time: '09:56' },
]
