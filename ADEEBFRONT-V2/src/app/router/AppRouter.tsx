import { lazy, Suspense } from "react";
import { useTranslation } from "react-i18next";
import {
  createBrowserRouter,
  createRoutesFromElements,
  Outlet,
  Route,
  RouterProvider,
} from "react-router-dom";
import { LoginRoute } from "@/routes/login/LoginRoute";
import { RegisterRoute } from "@/routes/register/RegisterRoute";
import { AdminLayout } from "@/routes/admin/AdminLayout";
import { AdminHomeRoute } from "@/routes/admin/AdminHomeRoute";
import { SubjectsRoute } from "@/routes/admin/subjects/SubjectsRoute";
import { SubjectFormRoute } from "@/routes/admin/subjects/SubjectFormRoute";
import { TopicsRoute } from "@/routes/admin/topics/TopicsRoute";
import { TopicFormRoute } from "@/routes/admin/topics/TopicFormRoute";
import { QuestionsRoute } from "@/routes/admin/questions/QuestionsRoute";
import { QuestionFormRoute } from "@/routes/admin/questions/QuestionFormRoute";
import { QuestionImportRoute } from "@/routes/admin/questions/QuestionImportRoute";
import { AuthRoute, AuthenticatedHomeRedirect } from "@/features/auth/ui/AuthRoute";
import { GuestRoute } from "@/features/auth/ui/GuestRoute";
import { StudentLayout } from "@/routes/student/StudentLayout";
import { StudentHomePage } from "@/routes/student/StudentHomePage";
import { StudentMmtPage } from "@/routes/student/StudentMmtPage";
import { StudentLearningPage } from "@/routes/student/StudentLearningPage";
import { StudentProfilePage } from "@/routes/student/StudentProfilePage";
import { StudentSectionPage } from "@/routes/student/StudentSectionPage";
import { StudentSettingsPage } from "@/routes/student/StudentSettingsPage";
import { Award, CalendarCheck2, HelpCircle, Rocket, Swords } from "lucide-react";

const MmtCatalogPage = lazy(() =>
  import("@/features/mmt/ui/MmtCatalogPage").then((module) => ({
    default: module.MmtCatalogPage,
  })),
);
const MmtDashboardPage = lazy(() =>
  import("@/features/mmt/ui/MmtDashboardPage").then((module) => ({
    default: module.MmtDashboardPage,
  })),
);
const MmtEvaluationsPage = lazy(() =>
  import("@/features/mmt/ui/MmtEvaluationsPage").then((module) => ({
    default: module.MmtEvaluationsPage,
  })),
);
const MmtEvaluationDetailPage = lazy(() =>
  import("@/features/mmt/ui/MmtEvaluationsPage").then((module) => ({
    default: module.MmtEvaluationDetailPage,
  })),
);
const MmtImportPage = lazy(() =>
  import("@/features/mmt/ui/MmtImportPage").then((module) => ({
    default: module.MmtImportPage,
  })),
);
const MmtProfilesPage = lazy(() =>
  import("@/features/mmt/ui/MmtProfilesPage").then((module) => ({
    default: module.MmtProfilesPage,
  })),
);
const MmtProfileDetailPage = lazy(() =>
  import("@/features/mmt/ui/MmtProfilesPage").then((module) => ({
    default: module.MmtProfileDetailPage,
  })),
);
const MmtProgramDetailPage = lazy(() =>
  import("@/features/mmt/ui/MmtProgramDetailPage").then((module) => ({
    default: module.MmtProgramDetailPage,
  })),
);
const MmtProgramFormPage = lazy(() =>
  import("@/features/mmt/ui/MmtProgramFormPage").then((module) => ({
    default: module.MmtProgramFormPage,
  })),
);
const MmtProgramsPage = lazy(() =>
  import("@/features/mmt/ui/MmtProgramsPage").then((module) => ({
    default: module.MmtProgramsPage,
  })),
);
const MmtReferenceDetailPage = lazy(() =>
  import("@/features/mmt/ui/MmtReferenceDetailPage").then((module) => ({
    default: module.MmtReferenceDetailPage,
  })),
);
const StudentTestsHubPage = lazy(() => import("@/routes/student/testing/StudentTestsHubPage").then((module) => ({ default: module.StudentTestsHubPage })));
const SubjectTestStartPage = lazy(() => import("@/routes/student/testing/StudentTestStartPages").then((module) => ({ default: module.SubjectTestStartPage })));
const MmtPracticeStartPage = lazy(() => import("@/routes/student/testing/StudentTestStartPages").then((module) => ({ default: module.MmtPracticeStartPage })));
const MonthlyExamStartPage = lazy(() => import("@/routes/student/testing/StudentTestStartPages").then((module) => ({ default: module.MonthlyExamStartPage })));
const RedListPracticeStartPage = lazy(() => import("@/routes/student/testing/StudentTestStartPages").then((module) => ({ default: module.RedListPracticeStartPage })));
const TestAttemptPage = lazy(() => import("@/routes/student/testing/TestAttemptPage").then((module) => ({ default: module.TestAttemptPage })));
const TestResultPage = lazy(() => import("@/routes/student/testing/StudentTestingResultsPages").then((module) => ({ default: module.TestResultPage })));
const TestHistoryPage = lazy(() => import("@/routes/student/testing/StudentTestingResultsPages").then((module) => ({ default: module.TestHistoryPage })));
const StudentRedListPage = lazy(() => import("@/routes/student/testing/StudentTestingResultsPages").then((module) => ({ default: module.StudentRedListPage })));
const StudentVocabularyPage = lazy(() => import("@/routes/student/vocabulary/StudentVocabularyPage").then((module) => ({ default: module.StudentVocabularyPage })));
const StudentVocabularySessionPage = lazy(() => import("@/routes/student/vocabulary/StudentVocabularySessionPage").then((module) => ({ default: module.StudentVocabularySessionPage })));
const AdminVocabularyPage = lazy(() => import("@/routes/admin/vocabulary/AdminVocabularyPage").then((module) => ({ default: module.AdminVocabularyPage })));
const AdminProgressionPage = lazy(() => import("@/routes/admin/progression/AdminProgressionPage").then((module) => ({ default: module.AdminProgressionPage })));
const StudentLeaguePage = lazy(() => import("@/routes/student/StudentLeaguePage").then((module) => ({ default: module.StudentLeaguePage })));

function MmtRouteFallback() {
  const { t } = useTranslation();
  return <div className="text-sm text-[var(--muted)]">{t("mmt.loading")}</div>;
}

function StudentTestingFallback() {
  const { t } = useTranslation();
  return <div className="text-sm text-[var(--student-muted)]">{t("student.testing.loading")}</div>;
}

function StudentTestingRoute({ children }: { children: React.ReactNode }) {
  return <Suspense fallback={<StudentTestingFallback />}>{children}</Suspense>;
}

const router = createBrowserRouter(
  createRoutesFromElements(
    <>
        <Route path="/" element={<AuthenticatedHomeRedirect />} />
        <Route
          path="/login"
          element={
            <GuestRoute>
              <LoginRoute />
            </GuestRoute>
          }
        />
        <Route
          path="/register"
          element={
            <GuestRoute>
              <RegisterRoute />
            </GuestRoute>
          }
        />
        <Route
          path="/admin"
          element={
            <AuthRoute audience="admin">
              <AdminLayout />
            </AuthRoute>
          }
        >
          <Route index element={<AdminHomeRoute />} />
          <Route path="subjects" element={<SubjectsRoute />} />
          <Route path="subjects/new" element={<SubjectFormRoute />} />
          <Route
            path="subjects/:subjectId/edit"
            element={<SubjectFormRoute />}
          />
          <Route path="topics" element={<TopicsRoute />} />
          <Route path="topics/new" element={<TopicFormRoute />} />
          <Route path="topics/:topicId/edit" element={<TopicFormRoute />} />
          <Route path="questions" element={<QuestionsRoute />} />
          <Route path="questions/import" element={<QuestionImportRoute />} />
          <Route path="questions/new" element={<QuestionFormRoute />} />
          <Route
            path="questions/:questionId/edit"
            element={<QuestionFormRoute />}
          />
          <Route path="vocabulary" element={<Suspense fallback={<MmtRouteFallback />}><AdminVocabularyPage /></Suspense>} />
          <Route path="progression" element={<Suspense fallback={<MmtRouteFallback />}><AdminProgressionPage /></Suspense>} />
          <Route
            path="mmt"
            element={
              <Suspense fallback={<MmtRouteFallback />}>
                <Outlet />
              </Suspense>
            }
          >
            <Route index element={<MmtDashboardPage />} />
            <Route
              path="clusters"
              element={<MmtCatalogPage kind="clusters" />}
            />
            <Route
              path="universities"
              element={<MmtCatalogPage kind="universities" />}
            />
            <Route path="universities/:universityId" element={<MmtReferenceDetailPage kind="universities" />} />
            <Route
              path="specialties"
              element={<MmtCatalogPage kind="specialties" />}
            />
            <Route path="specialties/:specialtyId" element={<MmtReferenceDetailPage kind="specialties" />} />
            <Route path="programs" element={<MmtProgramsPage />} />
            <Route path="programs/new" element={<MmtProgramFormPage />} />
            <Route
              path="programs/:programId"
              element={<MmtProgramDetailPage />}
            />
            <Route
              path="programs/:programId/edit"
              element={<MmtProgramFormPage />}
            />
            <Route path="import" element={<MmtImportPage />} />
            <Route path="profiles" element={<MmtProfilesPage />} />
            <Route
              path="profiles/:profileId"
              element={<MmtProfileDetailPage />}
            />
            <Route path="evaluations" element={<MmtEvaluationsPage />} />
            <Route
              path="evaluations/:evaluationId"
              element={<MmtEvaluationDetailPage />}
            />
          </Route>
        </Route>
        <Route
          path="/student"
          element={
            <AuthRoute audience="student">
              <StudentLayout />
            </AuthRoute>
          }
        >
          <Route index element={<StudentHomePage />} />
          <Route path="mmt" element={<StudentMmtPage />} />
          <Route path="mmt/setup" element={<StudentMmtPage />} />
          <Route path="learning" element={<StudentLearningPage />} />
          <Route path="vocabulary" element={<StudentTestingRoute><StudentVocabularyPage /></StudentTestingRoute>} />
          <Route path="vocabulary/sessions/:sessionId" element={<StudentTestingRoute><StudentVocabularySessionPage /></StudentTestingRoute>} />
          <Route path="tests" element={<StudentTestingRoute><StudentTestsHubPage /></StudentTestingRoute>} />
          <Route path="tests/subject" element={<StudentTestingRoute><SubjectTestStartPage /></StudentTestingRoute>} />
          <Route path="tests/mmt-practice" element={<StudentTestingRoute><MmtPracticeStartPage /></StudentTestingRoute>} />
          <Route path="tests/monthly-exam" element={<StudentTestingRoute><MonthlyExamStartPage /></StudentTestingRoute>} />
          <Route path="tests/red-list" element={<StudentTestingRoute><RedListPracticeStartPage /></StudentTestingRoute>} />
          <Route path="tests/attempts/:attemptId" element={<StudentTestingRoute><TestAttemptPage /></StudentTestingRoute>} />
          <Route path="tests/attempts/:attemptId/result" element={<StudentTestingRoute><TestResultPage /></StudentTestingRoute>} />
          <Route path="tests/history" element={<StudentTestingRoute><TestHistoryPage /></StudentTestingRoute>} />
          <Route path="red-list" element={<StudentTestingRoute><StudentRedListPage /></StudentTestingRoute>} />
          <Route path="duels" element={<StudentSectionPage titleKey="student.duel" icon={Swords} />} />
          <Route path="daily-tasks" element={<StudentSectionPage titleKey="student.dailyTasks" icon={CalendarCheck2} />} />
          <Route path="missions" element={<StudentSectionPage titleKey="student.missions" icon={Rocket} />} />
          <Route path="league" element={<StudentTestingRoute><StudentLeaguePage /></StudentTestingRoute>} />
          <Route path="achievements" element={<StudentSectionPage titleKey="student.achievements" icon={Award} />} />
          <Route path="profile" element={<StudentProfilePage />} />
          <Route path="settings" element={<StudentSettingsPage />} />
          <Route path="support" element={<StudentSectionPage titleKey="student.support" icon={HelpCircle} />} />
        </Route>
        <Route path="*" element={<AuthenticatedHomeRedirect />} />
    </>,
  ),
);

export function AppRouter() {
  return <RouterProvider router={router} />;
}
