import { zodResolver } from '@hookform/resolvers/zod'
import { LogIn, UserRound } from 'lucide-react'
import { useState } from 'react'
import { useTranslation } from 'react-i18next'
import { useForm } from 'react-hook-form'
import { useNavigate } from 'react-router-dom'
import { useAuth } from '@/features/auth/model/auth-context'
import { authenticatedHome } from '@/features/auth/lib/auth-role'
import { createLoginSchema, type LoginFormValues } from '@/features/auth/model/auth.schema'
import { ApiError } from '@/shared/api/problem-details'
import { Button } from '@/shared/ui/Button'
import { FormField } from '@/shared/ui/FormField'
import { Input } from '@/shared/ui/Input'
import { PasswordInput } from '@/features/auth/ui/PasswordInput'
import { Link } from 'react-router-dom'

export function LoginForm() {
  const navigate = useNavigate()
  const { login } = useAuth()
  const { t } = useTranslation()
  const [formError, setFormError] = useState<string | null>(null)
  const {
    register,
    handleSubmit,
    formState: { errors, isSubmitting },
  } = useForm<LoginFormValues>({
    resolver: zodResolver(createLoginSchema(t)),
    defaultValues: {
      identifier: '',
      password: '',
    },
  })

  async function onSubmit(values: LoginFormValues) {
    setFormError(null)
    try {
      const response = await login({
        identifier: values.identifier,
        email: values.identifier.includes('@') ? values.identifier : null,
        password: values.password,
        device: null,
      })
      navigate(authenticatedHome(response.user.role), { replace: true })
    } catch (error: unknown) {
      if (error instanceof ApiError) {
        setFormError(error.problem?.title ?? t('loginFailed'))
        return
      }

      setFormError(t('loginFailed'))
    }
  }

  return (
    <form className="grid gap-5" onSubmit={(event) => void handleSubmit(onSubmit)(event)}>
      {formError ? (
        <div className="rounded-2xl border border-red-100 bg-red-50 px-4 py-3 text-sm font-semibold text-[var(--danger)]">
          {formError}
        </div>
      ) : null}

      <FormField label={t('identifier')} error={errors.identifier?.message}>
        <div className="relative"><UserRound className="pointer-events-none absolute left-4 top-1/2 z-10 h-5 w-5 -translate-y-1/2 text-[#929bb4]" /><Input className="min-h-13 rounded-lg border-[#dfe3ef] bg-white pl-12 focus:border-[#5146f0] focus:shadow-[0_0_0_4px_rgb(81_70_240/0.1)]" autoComplete="username" {...register('identifier')} /></div>
      </FormField>

      <FormField label={t('password')} error={errors.password?.message}>
        <PasswordInput className="min-h-13 rounded-lg border-[#dfe3ef] bg-white focus:border-[#5146f0] focus:shadow-[0_0_0_4px_rgb(81_70_240/0.1)]" autoComplete="current-password" {...register('password')} />
      </FormField>

      <Button type="submit" disabled={isSubmitting} className="mt-1 w-full rounded-lg !bg-[#5146f0] !bg-none text-white shadow-[0_12px_26px_rgb(81_70_240/0.22)] hover:!bg-[#4338dc] hover:brightness-100">
        <LogIn className="h-4 w-4" aria-hidden />
        {isSubmitting ? t('signingIn') : t('signIn')}
      </Button>
      <p className="text-center text-sm text-[#68718c]">{t('noAccount')} <Link className="font-black text-[#5146f0] no-underline hover:text-[#352bc7]" to="/register">{t('createAccount')}</Link></p>
    </form>
  )
}
