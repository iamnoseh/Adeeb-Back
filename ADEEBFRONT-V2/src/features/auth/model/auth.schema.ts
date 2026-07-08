import { z } from 'zod'

export const loginSchema = z.object({
  identifier: z.string().trim().min(1, 'Email ё рақами телефонро ворид кунед'),
  password: z.string().min(1, 'Паролро ворид кунед'),
})

export type LoginFormValues = z.infer<typeof loginSchema>
