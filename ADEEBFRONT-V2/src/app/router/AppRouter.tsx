import { Navigate, Route, BrowserRouter as Router, Routes } from 'react-router-dom'
import { LoginRoute } from '@/routes/login/LoginRoute'
import { AdminLayout } from '@/routes/admin/AdminLayout'
import { AdminHomeRoute } from '@/routes/admin/AdminHomeRoute'
import { SubjectsRoute } from '@/routes/admin/subjects/SubjectsRoute'
import { SubjectFormRoute } from '@/routes/admin/subjects/SubjectFormRoute'
import { TopicsRoute } from '@/routes/admin/topics/TopicsRoute'
import { TopicFormRoute } from '@/routes/admin/topics/TopicFormRoute'
import { QuestionsRoute } from '@/routes/admin/questions/QuestionsRoute'
import { QuestionFormRoute } from '@/routes/admin/questions/QuestionFormRoute'
import { QuestionImportRoute } from '@/routes/admin/questions/QuestionImportRoute'
import { AuthRoute } from '@/features/auth/ui/AuthRoute'
import { GuestRoute } from '@/features/auth/ui/GuestRoute'

export function AppRouter() {
  return (
    <Router>
      <Routes>
        <Route path="/" element={<Navigate to="/admin" replace />} />
        <Route
          path="/login"
          element={
            <GuestRoute>
              <LoginRoute />
            </GuestRoute>
          }
        />
        <Route
          path="/admin"
          element={
            <AuthRoute>
              <AdminLayout />
            </AuthRoute>
          }
        >
          <Route index element={<AdminHomeRoute />} />
          <Route path="subjects" element={<SubjectsRoute />} />
          <Route path="subjects/new" element={<SubjectFormRoute />} />
          <Route path="subjects/:subjectId/edit" element={<SubjectFormRoute />} />
          <Route path="topics" element={<TopicsRoute />} />
          <Route path="topics/new" element={<TopicFormRoute />} />
          <Route path="topics/:topicId/edit" element={<TopicFormRoute />} />
          <Route path="questions" element={<QuestionsRoute />} />
          <Route path="questions/import" element={<QuestionImportRoute />} />
          <Route path="questions/new" element={<QuestionFormRoute />} />
          <Route path="questions/:questionId/edit" element={<QuestionFormRoute />} />
        </Route>
        <Route path="*" element={<Navigate to="/admin" replace />} />
      </Routes>
    </Router>
  )
}
