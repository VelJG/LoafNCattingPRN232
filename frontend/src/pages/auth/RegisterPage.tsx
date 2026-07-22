import { useState, type FormEvent } from 'react'
import {
  MdArrowForward,
  MdEmail,
  MdHome,
  MdLock,
  MdPerson,
  MdPhone,
  MdVisibility,
  MdVisibilityOff,
} from 'react-icons/md'
import { Link, useNavigate } from 'react-router-dom'
import { ApiError } from '../../api/httpClient'
import { Button } from '../../components/ui/Button'
import { Field } from '../../components/ui/Field'
import { useAuth } from '../../features/auth/useAuth'
import { validateRegister, type RegisterValues } from './validation'

const initialValues: RegisterValues = {
  name: '',
  email: '',
  phoneNumber: '',
  password: '',
  address: '',
}

export function RegisterPage() {
  const auth = useAuth()
  const navigate = useNavigate()
  const [values, setValues] = useState(initialValues)
  const [errors, setErrors] = useState<
    Partial<Record<keyof RegisterValues, string>>
  >({})
  const [showPassword, setShowPassword] = useState(false)
  const [apiMessage, setApiMessage] = useState('')
  const [submitting, setSubmitting] = useState(false)

  const update = (field: keyof RegisterValues, value: string) =>
    setValues((current) => ({ ...current, [field]: value }))

  async function handleSubmit(event: FormEvent<HTMLFormElement>) {
    event.preventDefault()
    const nextErrors = validateRegister(values)
    setErrors(nextErrors)
    if (Object.keys(nextErrors).length > 0) return

    setSubmitting(true)
    setApiMessage('')
    try {
      await auth.register({
        name: values.name.trim(),
        email: values.email.trim(),
        phoneNumber: values.phoneNumber.trim(),
        password: values.password,
        address: values.address.trim() || null,
      })
      navigate('/login', {
        replace: true,
        state: { registeredEmail: values.email.trim() },
      })
    } catch (error) {
      setApiMessage(
        error instanceof ApiError
          ? error.detail
          : 'Không thể tạo tài khoản. Vui lòng thử lại.',
      )
    } finally {
      setSubmitting(false)
    }
  }

  return (
    <section className="auth-card auth-card--register" aria-labelledby="register-title">
      <div className="auth-card__heading">
        <span>TRỞ THÀNH KHÁCH QUEN</span>
        <h1 id="register-title">Tạo tài khoản.</h1>
        <p>Một lần đăng ký cho đặt bàn, gọi món và lưu lịch sử ghé quán.</p>
      </div>

      {apiMessage && <div className="auth-error" role="alert">{apiMessage}</div>}

      <form className="auth-form auth-form--register" noValidate onSubmit={handleSubmit}>
        <Field
          id="register-name"
          label="Họ và tên"
          autoComplete="name"
          value={values.name}
          onChange={(event) => update('name', event.target.value)}
          error={errors.name}
          icon={<MdPerson />}
        />
        <Field
          id="register-email"
          label="Email"
          type="email"
          autoComplete="email"
          value={values.email}
          onChange={(event) => update('email', event.target.value)}
          error={errors.email}
          icon={<MdEmail />}
        />
        <Field
          id="register-phone"
          label="Số điện thoại"
          type="tel"
          autoComplete="tel"
          value={values.phoneNumber}
          onChange={(event) => update('phoneNumber', event.target.value)}
          error={errors.phoneNumber}
          icon={<MdPhone />}
        />
        <Field
          id="register-password"
          label="Mật khẩu"
          type={showPassword ? 'text' : 'password'}
          autoComplete="new-password"
          value={values.password}
          onChange={(event) => update('password', event.target.value)}
          error={errors.password}
          hint="Tối thiểu 8 ký tự."
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
        <Field
          id="register-address"
          label="Địa chỉ (không bắt buộc)"
          autoComplete="street-address"
          value={values.address}
          onChange={(event) => update('address', event.target.value)}
          icon={<MdHome />}
        />
        <Button className="auth-submit" type="submit" disabled={submitting}>
          {submitting ? 'Đang tạo tài khoản…' : 'Tạo tài khoản'}
          {!submitting && <MdArrowForward />}
        </Button>
      </form>

      <p className="auth-switch">
        Đã có tài khoản? <Link to="/login">Đăng nhập</Link>
      </p>
    </section>
  )
}
