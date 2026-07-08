import { z } from 'zod'

export function createLoginSchema(t: (key: string) => string) {
  return z.object({
    identifier: z.string().trim().min(1, t('requiredIdentifier')),
    password: z.string().min(1, t('requiredPassword')),
  })
}

export const loginSchema = createLoginSchema((key) => key)

export type LoginFormValues = z.infer<typeof loginSchema>
