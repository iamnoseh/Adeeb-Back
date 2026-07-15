export function isAdminRole(role: string | null | undefined) {
  return role === 'Admin' || role === 'SuperAdmin'
}

export function isStudentRole(role: string | null | undefined) {
  return role === 'User' || role === 'Student'
}

export function authenticatedHome(role: string | null | undefined) {
  return isAdminRole(role) ? '/admin' : '/student'
}
