import { httpClient } from '@/shared/api/http-client'
import type { StudentResponse, UpdateStudentProfileRequest } from '@/features/students/model/students.types'

export const studentsApi = {
  async me() {
    const response = await httpClient.get<StudentResponse>('/api/v2/students/me')
    return response.data
  },
  async updateProfile(request: UpdateStudentProfileRequest) {
    const response = await httpClient.patch<StudentResponse>('/api/v2/students/me/profile', request)
    return response.data
  },
  async uploadAvatar(file: File) {
    const formData = new FormData()
    formData.append('Avatar', file)
    const response = await httpClient.post<StudentResponse>('/api/v2/students/me/profile/avatar', formData)
    return response.data
  },
}
