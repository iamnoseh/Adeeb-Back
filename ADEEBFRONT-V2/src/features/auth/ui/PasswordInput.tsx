import { Eye, EyeOff } from 'lucide-react'
import { useState, type ComponentProps } from 'react'
import { useTranslation } from 'react-i18next'
import { Input } from '@/shared/ui/Input'

export function PasswordInput({ className, ...props }: Omit<ComponentProps<typeof Input>, 'type'>) {
  const { t } = useTranslation()
  const [visible, setVisible] = useState(false)
  return (
    <div className="relative">
      <Input {...props} className={`pr-12 ${className ?? ''}`} type={visible ? 'text' : 'password'} />
      <button
        type="button"
        className="absolute right-2 top-1/2 grid h-9 w-9 -translate-y-1/2 place-items-center rounded-md text-[var(--muted)] hover:bg-[var(--surface-muted)] hover:text-[var(--text)]"
        onClick={() => setVisible((current) => !current)}
        aria-label={visible ? t('hidePassword') : t('showPassword')}
      >
        {visible ? <EyeOff className="h-4 w-4" aria-hidden /> : <Eye className="h-4 w-4" aria-hidden />}
      </button>
    </div>
  )
}
