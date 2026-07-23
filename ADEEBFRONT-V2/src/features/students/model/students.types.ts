export type StudentProfileResponse = {
  displayName?: string | null
  avatarUrl?: string | null
  dateOfBirth?: string | null // YYYY-MM-DD
  region?: string | null
  city?: string | null
  schoolName?: string | null
  grade?: number | null
  gender?: string | null
  timeZoneId: string
  updatedAtUtc: string
}

export type StudentResponse = {
  studentId: string
  identityUserId: string
  status: string
  onboardingState: string
  profile: StudentProfileResponse
  createdAtUtc: string
  updatedAtUtc: string
}

export type UpdateStudentProfileRequest = {
  displayName?: string | null
  avatarUrl?: string | null
  dateOfBirth?: string | null
  region?: string | null
  city?: string | null
  schoolName?: string | null
  grade?: number | null
  gender?: string | null
}
