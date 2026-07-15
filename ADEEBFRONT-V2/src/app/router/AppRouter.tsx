import { lazy, Suspense } from "react";
import { useTranslation } from "react-i18next";
import {
  Navigate,
  Outlet,
  Route,
  BrowserRouter as Router,
  Routes,
} from "react-router-dom";
import { LoginRoute } from "@/routes/login/LoginRoute";
import { AdminLayout } from "@/routes/admin/AdminLayout";
import { AdminHomeRoute } from "@/routes/admin/AdminHomeRoute";
import { SubjectsRoute } from "@/routes/admin/subjects/SubjectsRoute";
import { SubjectFormRoute } from "@/routes/admin/subjects/SubjectFormRoute";
import { TopicsRoute } from "@/routes/admin/topics/TopicsRoute";
import { TopicFormRoute } from "@/routes/admin/topics/TopicFormRoute";
import { QuestionsRoute } from "@/routes/admin/questions/QuestionsRoute";
import { QuestionFormRoute } from "@/routes/admin/questions/QuestionFormRoute";
import { QuestionImportRoute } from "@/routes/admin/questions/QuestionImportRoute";
import { AuthRoute } from "@/features/auth/ui/AuthRoute";
import { GuestRoute } from "@/features/auth/ui/GuestRoute";

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

function MmtRouteFallback() {
  const { t } = useTranslation();
  return <div className="text-sm text-[var(--muted)]">{t("mmt.loading")}</div>;
}

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
            <Route
              path="specialties"
              element={<MmtCatalogPage kind="specialties" />}
            />
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
        <Route path="*" element={<Navigate to="/admin" replace />} />
      </Routes>
    </Router>
  );
}
