import { zodResolver } from '@hookform/resolvers/zod'
import { LogIn } from 'lucide-react'
import { useState } from 'react'
import { useForm } from 'react-hook-form'
import { useNavigate } from 'react-router-dom'
import { useAuth } from '@/features/auth/model/auth-context'
import { loginSchema, type LoginFormValues } from '@/features/auth/model/auth.schema'
import { ApiError } from '@/shared/api/problem-details'
import { Button } from '@/shared/ui/Button'
import { FormField } from '@/shared/ui/FormField'
import { Input } from '@/shared/ui/Input'

export function LoginForm() {
  const navigate = useNavigate()
  const { login } = useAuth()
  const [formError, setFormError] = useState<string | null>(null)
  const {
    register,
    handleSubmit,
    formState: { errors, isSubmitting },
  } = useForm<LoginFormValues>({
    resolver: zodResolver(loginSchema),
    defaultValues: {
      identifier: '',
      password: '',
    },
  })

  async function onSubmit(values: LoginFormValues) {
    setFormError(null)
    try {
      await login({
        identifier: values.identifier,
        email: values.identifier.includes('@') ? values.identifier : null,
        password: values.password,
        device: null,
      })
      navigate('/admin', { replace: true })
    } catch (error: unknown) {
      if (error instanceof ApiError) {
        setFormError(error.problem?.title ?? error.message)
        return
      }

      setFormError('Воридшавӣ анҷом нашуд. Серверро санҷед.')
    }
  }

  return (
    <form className="grid gap-5" onSubmit={(event) => void handleSubmit(onSubmit)(event)}>
      {formError ? (
        <div className="rounded-md border border-red-200 bg-red-50 px-3 py-2 text-sm font-semibold text-[var(--danger)]">
          {formError}
        </div>
      ) : null}

      <FormField label="Email ё рақами телефон" error={errors.identifier?.message}>
        <Input autoComplete="username" placeholder="superadmin@adeeb.tj ё +992..." {...register('identifier')} />
      </FormField>

      <FormField label="Парол" error={errors.password?.message}>
        <Input autoComplete="current-password" type="password" placeholder="Парол" {...register('password')} />
      </FormField>

      <Button type="submit" disabled={isSubmitting} className="w-full">
        <LogIn className="h-4 w-4" aria-hidden />
        {isSubmitting ? 'Ворид шуда истодааст...' : 'Ворид шудан'}
      </Button>
    </form>
  )
}
