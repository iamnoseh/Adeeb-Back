import { useTranslation } from 'react-i18next'
import { AuthPageShell } from '@/features/auth/ui/AuthPageShell'
import { LoginForm } from '@/features/auth/ui/LoginForm'

export function LoginRoute() {
  const { t } = useTranslation()
  return (
    <AuthPageShell title={t('loginTitle')} subtitle={t('loginSubtitle')} panelTitle={t('signIn')}>
      <LoginForm />
    </AuthPageShell>
  )
}
