import { useState, type FormEvent } from 'react'
import { MdArrowForward, MdEmail, MdLock, MdVisibility, MdVisibilityOff } from 'react-icons/md'
import { Link, useLocation, useNavigate } from 'react-router-dom'
import { ApiError } from '../../api/httpClient'
import { Button } from '../../components/ui/Button'
import { Field } from '../../components/ui/Field'
import { safeRedirectForRole } from '../../features/auth/authRouting'
import { useAuth } from '../../features/auth/useAuth'
import { validateLogin, type LoginValues } from './validation'

interface LoginLocationState {
  from?: string
  registeredEmail?: string
}

export function LoginPage() {
  const auth = useAuth()
  const navigate = useNavigate()
  const location = useLocation()
  const routeState = location.state as LoginLocationState | null
  const [email, setEmail] = useState(routeState?.registeredEmail ?? '')
  const [password, setPassword] = useState('')
  const [showPassword, setShowPassword] = useState(false)
  const [errors, setErrors] = useState<Partial<Record<keyof LoginValues, string>>>({})
  const [apiMessage, setApiMessage] = useState('')
  const [submitting, setSubmitting] = useState(false)

  async function handleSubmit(event: FormEvent<HTMLFormElement>) {
    event.preventDefault()
    const nextErrors = validateLogin({ email, password })
    setErrors(nextErrors)
    if (Object.keys(nextErrors).length > 0) return

    setSubmitting(true)
    setApiMessage('')
    try {
      const session = await auth.login({ email: email.trim(), password })
      navigate(safeRedirectForRole(session.user.role, routeState?.from), {
        replace: true,
      })
    } catch (error) {
      setApiMessage(
        error instanceof ApiError
          ? error.detail
          : 'Không thể đăng nhập. Vui lòng thử lại.',
      )
    } finally {
      setSubmitting(false)
    }
  }

  return (
    <section className="auth-card" aria-labelledby="login-title">
      <div className="auth-card__heading">
        <span>CHÀO MỪNG TRỞ LẠI</span>
        <h1 id="login-title">Đăng nhập vào góc quen.</h1>
        <p>Gọi món, đặt bàn hoặc bắt đầu ca làm việc của bạn.</p>
      </div>

      {routeState?.registeredEmail && (
        <div className="auth-success" role="status">
          Tài khoản đã được tạo. Bạn có thể đăng nhập ngay.
        </div>
      )}
      {apiMessage && <div className="auth-error" role="alert">{apiMessage}</div>}

      <form className="auth-form" noValidate onSubmit={handleSubmit}>
        <Field
          id="login-email"
          label="Email"
          type="email"
          autoComplete="email"
          placeholder="ban@example.com"
          value={email}
          onChange={(event) => setEmail(event.target.value)}
          error={errors.email}
          icon={<MdEmail />}
        />
        <Field
          id="login-password"
          label="Mật khẩu"
          type={showPassword ? 'text' : 'password'}
          autoComplete="current-password"
          placeholder="Nhập mật khẩu"
          value={password}
          onChange={(event) => setPassword(event.target.value)}
          error={errors.password}
          icon={<MdLock />}
          action={
            <button
              type="button"
              aria-label={showPassword ? 'Ẩn mật khẩu' : 'Hiện mật khẩu'}
              onClick={() => setShowPassword((value) => !value)}
            >
              {showPassword ? <MdVisibilityOff /> : <MdVisibility />}
            </button>
          }
        />
        <Button className="auth-submit" type="submit" disabled={submitting}>
          {submitting ? 'Đang đăng nhập…' : 'Đăng nhập'}
          {!submitting && <MdArrowForward />}
        </Button>
      </form>

      <p className="auth-switch">
        Chưa có tài khoản? <Link to="/register">Tạo tài khoản khách hàng</Link>
      </p>
    </section>
  )
}
