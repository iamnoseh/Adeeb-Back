import { Camera, Check, LoaderCircle, UserRound } from 'lucide-react'
import { useEffect, useMemo, useRef, useState } from 'react'
import { useForm } from 'react-hook-form'
import { zodResolver } from '@hookform/resolvers/zod'
import { z } from 'zod'
import { useTranslation } from 'react-i18next'
import { authApi } from '@/features/auth/api/auth.api'
import { useAuth } from '@/features/auth/model/auth-context'
import type { UserResponse } from '@/features/auth/model/auth.types'
import { studentsApi } from '@/features/students/api/students.api'
import type { StudentProfileResponse, StudentResponse } from '@/features/students/model/students.types'
import { Button } from '@/shared/ui/Button'
import { FormField } from '@/shared/ui/FormField'
import { Input } from '@/shared/ui/Input'
import { SelectField } from '@/shared/ui/SelectField'
import { toAssetUrl } from '@/shared/lib/asset-url'

const schema = z.object({
  firstName: z.string().trim().min(1).max(80),
  lastName: z.string().trim().min(1).max(80),
  email: z.string().trim().email().max(100),
  dateOfBirth: z.string().optional().nullable(),
  gender: z.string().optional().nullable(),
})

const avatarMaxBytes = 10 * 1024 * 1024
const allowedAvatarTypes = new Set(['image/png', 'image/jpeg', 'image/webp'])

type FormData = z.infer<typeof schema>

type EditStudentProfileProps = {
  profile: StudentProfileResponse | null
  onSuccess: (identity: UserResponse, student: StudentResponse) => void
  onCancel: () => void
}

export function EditStudentProfile({ profile, onSuccess, onCancel }: EditStudentProfileProps) {
  const { t } = useTranslation()
  const { user } = useAuth()
  const fileInputRef = useRef<HTMLInputElement>(null)
  const currentAvatarUrl = toAssetUrl(profile?.avatarUrl)
  const [avatarFile, setAvatarFile] = useState<File | null>(null)
  const [avatarPreview, setAvatarPreview] = useState<string | null>(currentAvatarUrl)
  const [isSubmitting, setIsSubmitting] = useState(false)
  const [error, setError] = useState<string | null>(null)
  const isDobImmutable = Boolean(profile?.dateOfBirth)

  const genderOptions = useMemo(
    () => [
      { value: '', label: t('student.selectGender') },
      { value: 'Male', label: t('student.genderMale') },
      { value: 'Female', label: t('student.genderFemale') },
    ],
    [t],
  )

  const { register, handleSubmit, watch, setValue, formState: { errors } } = useForm<FormData>({
    resolver: zodResolver(schema),
    defaultValues: {
      firstName: user?.firstName ?? '',
      lastName: user?.lastName ?? '',
      email: user?.email ?? '',
      dateOfBirth: profile?.dateOfBirth ?? '',
      gender: profile?.gender ?? '',
    },
  })

  function chooseAvatar(file: File | null) {
    if (!file) {
      setAvatarFile(null)
      return
    }

    if (!allowedAvatarTypes.has(file.type) || file.size > avatarMaxBytes) {
      setAvatarFile(null)
      setError(t('student.avatarInvalid'))
      return
    }

    setError(null)
    setAvatarFile(file)
  }

  useEffect(() => {
    if (!avatarFile) {
      setAvatarPreview(currentAvatarUrl)
      return
    }

    const objectUrl = URL.createObjectURL(avatarFile)
    setAvatarPreview(objectUrl)
    return () => URL.revokeObjectURL(objectUrl)
  }, [avatarFile, currentAvatarUrl])

  async function onSubmit(data: FormData) {
    setIsSubmitting(true)
    setError(null)
    try {
      const identity = await authApi.updateProfile({
        firstName: data.firstName.trim(),
        lastName: data.lastName.trim(),
        email: data.email.trim(),
      })

      let student = await studentsApi.updateProfile({
        displayName: `${data.firstName.trim()} ${data.lastName.trim()}`.trim(),
        avatarUrl: profile?.avatarUrl ?? null,
        dateOfBirth: data.dateOfBirth || null,
        region: profile?.region ?? null,
        city: profile?.city ?? null,
        schoolName: profile?.schoolName ?? null,
        grade: profile?.grade ?? null,
        gender: data.gender || null,
      })

      if (avatarFile) {
        student = await studentsApi.uploadAvatar(avatarFile)
      }

      onSuccess(identity, student)
    } catch (err: any) {
      setError(err?.response?.data?.title || err?.message || t('student.profileSaveFailed'))
    } finally {
      setIsSubmitting(false)
    }
  }

  return (
    <form onSubmit={handleSubmit(onSubmit)} className="grid gap-5 rounded-lg border border-[var(--student-border)] bg-[var(--student-surface)] p-5 shadow-[0_16px_40px_rgb(20_31_70/0.06)] sm:p-7">
      <div className="flex flex-col gap-5 border-b border-[var(--student-border)] pb-5 md:flex-row md:items-center md:justify-between">
        <div className="flex items-center gap-4">
          <button
            type="button"
            onClick={() => fileInputRef.current?.click()}
            className="group relative grid h-24 w-24 shrink-0 place-items-center overflow-hidden rounded-[1.4rem] border border-[#ddd9ff] bg-[#f7f6ff] text-[#5146f0] shadow-sm"
          >
            {avatarPreview ? (
              <img src={avatarPreview} alt="" className="h-full w-full object-cover" />
            ) : (
              <UserRound className="h-9 w-9" />
            )}
            <span className="absolute inset-x-0 bottom-0 flex min-h-8 items-center justify-center bg-[#111b3d]/70 text-white opacity-0 transition group-hover:opacity-100">
              <Camera className="h-4 w-4" />
            </span>
          </button>
          <input
            ref={fileInputRef}
            type="file"
            accept="image/png,image/jpeg,image/webp"
            className="sr-only"
            onChange={(event) => chooseAvatar(event.target.files?.[0] ?? null)}
          />
          <div>
            <h2 className="text-xl font-black tracking-normal text-[var(--student-text)]">{t('student.editProfile')}</h2>
            <p className="mt-1 text-sm font-semibold text-[var(--student-muted)]">{t('student.photoHint')}</p>
            <button type="button" className="mt-3 inline-flex min-h-10 items-center gap-2 rounded-lg border border-[#ddd9ff] bg-white px-4 text-sm font-black text-[#5146f0] shadow-sm" onClick={() => fileInputRef.current?.click()}>
              <Camera className="h-4 w-4" />
              {avatarFile ? t('student.changePhoto') : t('student.uploadPhoto')}
            </button>
          </div>
        </div>
        {avatarFile ? <span className="inline-flex items-center gap-2 rounded-full bg-emerald-50 px-3 py-1 text-xs font-black text-emerald-700"><Check className="h-3.5 w-3.5" />{avatarFile.name}</span> : null}
      </div>

      {error ? <div className="rounded-lg border border-red-100 bg-red-50 p-4 text-sm font-black text-red-700">{error}</div> : null}

      <div className="grid gap-x-6 gap-y-5 md:grid-cols-2">
        <FormField label={t('student.firstName')} error={errors.firstName?.message}>
          <Input {...register('firstName')} />
        </FormField>
        <FormField label={t('student.lastName')} error={errors.lastName?.message}>
          <Input {...register('lastName')} />
        </FormField>
        <FormField label={t('student.email')} error={errors.email?.message}>
          <Input type="email" {...register('email')} />
        </FormField>
        <FormField label={t('student.gender')} error={errors.gender?.message}>
          <SelectField
            value={watch('gender') ?? ''}
            onValueChange={(value) => setValue('gender', value, { shouldDirty: true })}
            placeholder={t('student.selectGender')}
            options={genderOptions}
            className="[&_button]:min-h-11 [&_button]:rounded-2xl [&_button]:border [&_button]:border-transparent [&_button]:bg-[var(--surface-muted)] [&_button]:px-4 [&_button]:py-2.5 [&_button]:text-sm [&_button]:shadow-[inset_0_0_0_1px_rgb(17_24_23/0.04)]"
          />
        </FormField>
        <FormField label={t('student.dateOfBirth')} error={errors.dateOfBirth?.message}>
          <Input
            type="date"
            {...register('dateOfBirth')}
            disabled={isDobImmutable}
            title={isDobImmutable ? t('student.dobImmutable') : undefined}
          />
        </FormField>
        {isDobImmutable ? <p className="self-end rounded-lg bg-[#f7f6ff] p-3 text-xs font-bold leading-5 text-[#68718c] md:mb-0">{t('student.dobImmutable')}</p> : null}
      </div>

      <div className="flex flex-col-reverse gap-3 border-t border-[var(--student-border)] pt-5 sm:flex-row sm:justify-end">
        <Button type="button" variant="ghost" onClick={onCancel} disabled={isSubmitting}>{t('cancel')}</Button>
        <Button type="submit" disabled={isSubmitting} className="min-w-40">
          {isSubmitting ? <LoaderCircle className="h-4 w-4 animate-spin" /> : <Check className="h-4 w-4" />}
          {isSubmitting ? t('saving') : t('save')}
        </Button>
      </div>
    </form>
  )
}
