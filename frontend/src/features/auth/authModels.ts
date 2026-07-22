export type UserRole = 'Customer' | 'Staff' | 'Admin'

export interface User {
  userId: number
  name: string
  email: string
  phoneNumber: string
  address: string | null
  role: UserRole
  isActive: boolean
  isEmailVerified: boolean
}

export interface LoginRequest {
  email: string
  password: string
}

export interface RegisterRequest {
  name: string
  email: string
  password: string
  phoneNumber: string
  address: string | null
}

export interface LoginResponse {
  accessToken: string
  tokenType: 'Bearer'
  expiresAtUtc: string
  user: User
}

export interface TokenVerificationResponse {
  user: User
  expiresAtUtc: string
}

export interface Session {
  token: string
  expiresAtUtc: string
  user: User
}
