import { z } from 'zod'

export function createLoginSchema(t: (key: string) => string) {
  return z.object({
    identifier: z.string().trim().min(1, t('requiredIdentifier')),
    password: z.string().min(1, t('requiredPassword')),
  })
}

export const loginSchema = createLoginSchema((key) => key)

export type LoginFormValues = z.infer<typeof loginSchema>

export function createRegisterSchema(t: (key: string) => string) {
  return z.object({
    firstName: z.string().trim().min(1, t('requiredFirstName')).max(80, t('nameTooLong')),
    lastName: z.string().trim().min(1, t('requiredLastName')).max(80, t('nameTooLong')),
    email: z.string().trim().min(1, t('requiredEmail')).email(t('invalidEmail')),
    phoneNumber: z.string().trim().refine((value) => {
      if (!value) return true
      const digits = value.replace(/\D/g, '')
      return /^\+?[\d\s()-]{7,22}$/.test(value) && digits.length >= 7 && digits.length <= 15
    }, t('invalidPhone')),
    password: z.string()
      .min(8, t('passwordPolicy'))
      .regex(/[A-ZА-ЯЁ]/, t('passwordPolicy'))
      .regex(/[a-zа-яё]/, t('passwordPolicy'))
      .regex(/\d/, t('passwordPolicy')),
    confirmPassword: z.string().min(1, t('requiredConfirmPassword')),
  }).refine((values) => values.password === values.confirmPassword, {
    path: ['confirmPassword'],
    message: t('passwordsDoNotMatch'),
  })
}

export type RegisterFormValues = z.infer<ReturnType<typeof createRegisterSchema>>
