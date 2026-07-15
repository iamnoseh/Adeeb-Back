import { zodResolver } from '@hookform/resolvers/zod'
import { Mail, Phone, UserPlus, UserRound } from 'lucide-react'
import { useState } from 'react'
import { useTranslation } from 'react-i18next'
import { useForm } from 'react-hook-form'
import { Link, useNavigate } from 'react-router-dom'
import { authenticatedHome } from '@/features/auth/lib/auth-role'
import { useAuth } from '@/features/auth/model/auth-context'
import { createRegisterSchema, type RegisterFormValues } from '@/features/auth/model/auth.schema'
import { ApiError } from '@/shared/api/problem-details'
import { Button } from '@/shared/ui/Button'
import { FormField } from '@/shared/ui/FormField'
import { Input } from '@/shared/ui/Input'
import { PasswordInput } from '@/features/auth/ui/PasswordInput'

const serverFields = new Set<keyof RegisterFormValues>(['firstName', 'lastName', 'email', 'phoneNumber', 'password'])

export function RegisterForm() {
  const { register: registerUser } = useAuth()
  const { i18n, t } = useTranslation()
  const navigate = useNavigate()
  const [formError, setFormError] = useState<string | null>(null)
  const {
    register,
    handleSubmit,
    setError,
    formState: { errors, isSubmitting },
  } = useForm<RegisterFormValues>({
    resolver: zodResolver(createRegisterSchema(t)),
    defaultValues: { firstName: '', lastName: '', email: '', phoneNumber: '', password: '', confirmPassword: '' },
  })

  async function onSubmit(values: RegisterFormValues) {
    setFormError(null)
    try {
      const response = await registerUser({
        firstName: values.firstName,
        lastName: values.lastName,
        email: values.email,
        phoneNumber: values.phoneNumber || null,
        password: values.password,
        language: i18n.language === 'ru-RU' ? 'ru-RU' : 'tg-TJ',
        device: null,
      })
      navigate(authenticatedHome(response.user.role), { replace: true })
    } catch (error: unknown) {
      if (error instanceof ApiError) {
        let fieldErrorApplied = false
        for (const [field, fieldErrors] of Object.entries(error.problem?.errors ?? {})) {
          if (!serverFields.has(field as keyof RegisterFormValues) || fieldErrors.length === 0) continue
          setError(field as keyof RegisterFormValues, { type: 'server', message: fieldErrors[0]?.message ?? t('registrationFailed') })
          fieldErrorApplied = true
        }
        if (!fieldErrorApplied) setFormError(error.problem?.title ?? t('registrationFailed'))
        return
      }
      setFormError(t('registrationFailed'))
    }
  }

  return (
    <form className="grid gap-4" onSubmit={(event) => void handleSubmit(onSubmit)(event)}>
      {formError ? <div className="rounded-lg border border-red-100 bg-red-50 px-4 py-3 text-sm font-semibold text-[var(--danger)]">{formError}</div> : null}

      <div className="grid gap-4 sm:grid-cols-2">
        <FormField label={t('firstName')} error={errors.firstName?.message}>
          <AuthInput icon={<UserRound />}><Input className={authInputClass} autoComplete="given-name" {...register('firstName')} /></AuthInput>
        </FormField>
        <FormField label={t('lastName')} error={errors.lastName?.message}>
          <AuthInput icon={<UserRound />}><Input className={authInputClass} autoComplete="family-name" {...register('lastName')} /></AuthInput>
        </FormField>
      </div>

      <div className="grid gap-4 sm:grid-cols-2">
        <FormField label={t('email')} error={errors.email?.message}>
          <AuthInput icon={<Mail />}><Input className={authInputClass} type="email" inputMode="email" autoComplete="email" {...register('email')} /></AuthInput>
        </FormField>
        <FormField label={t('phoneOptional')} error={errors.phoneNumber?.message}>
          <AuthInput icon={<Phone />}><Input className={authInputClass} type="tel" inputMode="tel" autoComplete="tel" placeholder="+992" {...register('phoneNumber')} /></AuthInput>
        </FormField>
      </div>

      <div className="grid gap-4 sm:grid-cols-2">
        <FormField label={t('password')} error={errors.password?.message}>
          <PasswordInput className={authInputClass} autoComplete="new-password" {...register('password')} />
        </FormField>
        <FormField label={t('confirmPassword')} error={errors.confirmPassword?.message}>
          <PasswordInput className={authInputClass} autoComplete="new-password" {...register('confirmPassword')} />
        </FormField>
      </div>

      <Button type="submit" disabled={isSubmitting} className="mt-1 w-full rounded-lg !bg-[#5146f0] !bg-none text-white shadow-[0_12px_26px_rgb(81_70_240/0.22)] hover:!bg-[#4338dc] hover:brightness-100">
        <UserPlus className="h-4 w-4" aria-hidden />
        {isSubmitting ? t('registering') : t('register')}
      </Button>

      <p className="text-center text-sm text-[#68718c]">{t('alreadyHaveAccount')} <Link className="font-black text-[#5146f0] no-underline hover:text-[#352bc7]" to="/login">{t('signIn')}</Link></p>
    </form>
  )
}

const authInputClass = 'min-h-13 rounded-lg border-[#dfe3ef] bg-white pl-12 focus:border-[#5146f0] focus:shadow-[0_0_0_4px_rgb(81_70_240/0.1)]'

function AuthInput({ icon, children }: { icon: React.ReactNode; children: React.ReactNode }) {
  return <div className="relative"><span className="pointer-events-none absolute left-4 top-1/2 z-10 -translate-y-1/2 text-[#929bb4] [&>svg]:h-5 [&>svg]:w-5">{icon}</span>{children}</div>
}
