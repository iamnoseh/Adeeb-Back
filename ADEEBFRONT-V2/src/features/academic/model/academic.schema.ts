import { z } from 'zod'

export function createSubjectFormSchema(t: (key: string) => string) {
  return z.object({
    nameTg: z.string().trim().min(1, t('validationSubjectNameTg')),
    nameRu: z.string().trim().min(1, t('validationSubjectNameRu')),
    nameEn: z.string().trim(),
    descriptionTg: z.string().trim(),
    descriptionRu: z.string().trim(),
    descriptionEn: z.string().trim(),
    status: z.coerce.number().int().min(0).max(2),
    displayOrder: z.coerce.number().int().min(0),
    icon: z.instanceof(FileList).optional(),
  })
}

export function createTopicFormSchema(t: (key: string) => string) {
  return z.object({
    subjectId: z.string().uuid(t('validationChooseSubject')),
    displayOrder: z.coerce.number().int().min(0),
    status: z.coerce.number().int().min(0).max(2),
    nameTg: z.string().trim().min(1, t('validationSubjectNameTg')),
    nameRu: z.string().trim().min(1, t('validationSubjectNameRu')),
    nameEn: z.string().trim(),
    descriptionTg: z.string().trim(),
    descriptionRu: z.string().trim(),
    descriptionEn: z.string().trim(),
  })
}

export const subjectFormSchema = z.object({
  nameTg: z.string().trim().min(1),
  nameRu: z.string().trim().min(1),
  nameEn: z.string().trim(),
  descriptionTg: z.string().trim(),
  descriptionRu: z.string().trim(),
  descriptionEn: z.string().trim(),
  status: z.coerce.number().int().min(0).max(2),
  displayOrder: z.coerce.number().int().min(0),
  icon: z.instanceof(FileList).optional(),
})

export const topicFormSchema = z.object({
  subjectId: z.string().uuid(),
  displayOrder: z.coerce.number().int().min(0),
  status: z.coerce.number().int().min(0).max(2),
  nameTg: z.string().trim().min(1),
  nameRu: z.string().trim().min(1),
  nameEn: z.string().trim(),
  descriptionTg: z.string().trim(),
  descriptionRu: z.string().trim(),
  descriptionEn: z.string().trim(),
})
