import {
  createContext,
  useEffect,
  useMemo,
  useState,
  type PropsWithChildren,
} from 'react'
import { ApiError } from '../../api/httpClient'
import { authApi, type AuthGateway } from './authApi'
import type {
  LoginRequest,
  RegisterRequest,
  Session,
  User,
} from './authModels'
import { normalizeRole } from './authRouting'

const storageKey = 'loafncatting.session'

export type AuthStatus = 'initializing' | 'unauthenticated' | 'authenticated'

export interface AuthContextValue {
  status: AuthStatus
  session: Session | null
  login(request: LoginRequest): Promise<Session>
  register(request: RegisterRequest): Promise<User>
  logout(): Promise<void>
}

export const AuthContext = createContext<AuthContextValue | null>(null)

interface AuthProviderProps extends PropsWithChildren {
  gateway?: AuthGateway
}

function storeSession(session: Session) {
  localStorage.setItem(storageKey, JSON.stringify(session))
}

function clearStoredSession() {
  localStorage.removeItem(storageKey)
}

export function AuthProvider({
  children,
  gateway = authApi,
}: AuthProviderProps) {
  const [status, setStatus] = useState<AuthStatus>('initializing')
  const [session, setSession] = useState<Session | null>(null)

  useEffect(() => {
    const rawSession = localStorage.getItem(storageKey)
    if (!rawSession) {
      setStatus('unauthenticated')
      return
    }

    let stored: Session
    try {
      stored = JSON.parse(rawSession) as Session
    } catch {
      clearStoredSession()
      setStatus('unauthenticated')
      return
    }

    let alive = true
    gateway
      .verify(stored.token)
      .then((verified) => {
        if (!alive) return
        const restored: Session = {
          token: stored.token,
          expiresAtUtc: verified.expiresAtUtc,
          user: {
            ...verified.user,
            role: normalizeRole(verified.user.role),
          },
        }
        storeSession(restored)
        setSession(restored)
        setStatus('authenticated')
      })
      .catch((error) => {
        if (!alive) return
        if (error instanceof ApiError && error.status !== 401) {
          try {
            const restored: Session = {
              ...stored,
              user: {
                ...stored.user,
                role: normalizeRole(stored.user.role),
              },
            }
            setSession(restored)
            setStatus('authenticated')
            return
          } catch {
            // A malformed local session must never become authenticated.
          }
        }
        clearStoredSession()
        setSession(null)
        setStatus('unauthenticated')
      })

    return () => {
      alive = false
    }
  }, [gateway])

  const value = useMemo<AuthContextValue>(
    () => ({
      status,
      session,
      async login(request) {
        const response = await gateway.login(request)
        const nextSession: Session = {
          token: response.accessToken,
          expiresAtUtc: response.expiresAtUtc,
          user: {
            ...response.user,
            role: normalizeRole(response.user.role),
          },
        }
        storeSession(nextSession)
        setSession(nextSession)
        setStatus('authenticated')
        return nextSession
      },
      register: (request) => gateway.register(request),
      async logout() {
        const token = session?.token
        clearStoredSession()
        setSession(null)
        setStatus('unauthenticated')
        if (token) {
          await gateway.logout(token).catch(() => undefined)
        }
      },
    }),
    [gateway, session, status],
  )

  return <AuthContext.Provider value={value}>{children}</AuthContext.Provider>
}
