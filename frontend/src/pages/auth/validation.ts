const emailPattern = /^[^\s@]+@[^\s@]+\.[^\s@]+$/

export interface LoginValues {
  email: string
  password: string
}

export function validateLogin(values: LoginValues) {
  const errors: Partial<Record<keyof LoginValues, string>> = {}
  if (!values.email.trim()) errors.email = 'Vui lòng nhập email.'
  else if (!emailPattern.test(values.email.trim())) {
    errors.email = 'Email không hợp lệ.'
  }
  if (!values.password) errors.password = 'Vui lòng nhập mật khẩu.'
  return errors
}

export interface RegisterValues extends LoginValues {
  name: string
  phoneNumber: string
  address: string
}

export function validateRegister(values: RegisterValues) {
  const errors: Partial<Record<keyof RegisterValues, string>> = {}
  if (!values.name.trim()) errors.name = 'Vui lòng nhập họ và tên.'
  if (!values.email.trim()) errors.email = 'Vui lòng nhập email.'
  else if (!emailPattern.test(values.email.trim())) {
    errors.email = 'Email không hợp lệ.'
  }
  if (!values.phoneNumber.trim()) {
    errors.phoneNumber = 'Vui lòng nhập số điện thoại.'
  } else if (values.phoneNumber.trim().length > 20) {
    errors.phoneNumber = 'Số điện thoại không được quá 20 ký tự.'
  }
  if (values.password.length < 8) {
    errors.password = 'Mật khẩu cần ít nhất 8 ký tự.'
  }
  return errors
}
