import { requestJson } from '../../api/httpClient'
import type {
  LoginRequest,
  LoginResponse,
  RegisterRequest,
  TokenVerificationResponse,
  User,
} from './authModels'

export interface AuthGateway {
  login(request: LoginRequest): Promise<LoginResponse>
  register(request: RegisterRequest): Promise<User>
  verify(token: string): Promise<TokenVerificationResponse>
  logout(token: string): Promise<void>
}

export const authApi: AuthGateway = {
  login: (body) => requestJson('/auth/login', { method: 'POST', body }),
  register: (body) => requestJson('/auth/register', { method: 'POST', body }),
  verify: (token) => requestJson('/auth/verify', { token }),
  logout: (token) => requestJson('/auth/logout', { method: 'POST', token }),
}
