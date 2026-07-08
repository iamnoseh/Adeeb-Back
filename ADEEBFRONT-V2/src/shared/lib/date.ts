export function formatDushanbeDate(value?: string | null) {
  if (!value) return '—'

  return new Intl.DateTimeFormat('ru-RU', {
    dateStyle: 'medium',
    timeStyle: 'short',
    timeZone: 'Asia/Dushanbe',
  }).format(new Date(value))
}
