function toneFor(value: string) {
  const normalized = value.toLocaleLowerCase('vi-VN')
  if (/(hủy|huỷ|cancel|expired|hết hạn|hết hàng|không đến|ngừng bán)/.test(normalized)) return 'danger'
  if (/(hoàn thành|completed|paid|đã thanh toán|available|còn trống|còn bán|hoạt động)/.test(normalized)) return 'success'
  if (/(xác nhận|confirmed|processing|pha chế|đã đến|checked)/.test(normalized)) return 'info'
  if (/(chờ|pending|sắp hết)/.test(normalized)) return 'warning'
  return 'neutral'
}

export function AdminStatusChip({ value }: { value: string }) {
  return <span className={`admin-status admin-status--${toneFor(value)}`}>{value}</span>
}
