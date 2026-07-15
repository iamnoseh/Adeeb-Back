import { useTranslation } from 'react-i18next'
import { AuthPageShell } from '@/features/auth/ui/AuthPageShell'
import { RegisterForm } from '@/features/auth/ui/RegisterForm'

export function RegisterRoute() {
  const { t } = useTranslation()
  return (
    <AuthPageShell title={t('registerTitle')} subtitle={t('registerSubtitle')} panelTitle={t('createAccount')} mode="register">
      <RegisterForm />
    </AuthPageShell>
  )
}
