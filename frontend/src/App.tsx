import { Navigate, Route, Routes } from 'react-router-dom'
import { ProtectedRoute } from './auth/ProtectedRoute'
import { ApiErrorNotice } from './components/ApiErrorNotice'
import { useAuth } from './auth/AuthContext'
import { AppShell } from './layout/AppShell'
import { AdminUsersPage } from './pages/AdminUsersPage'
import { AuditLogPage } from './pages/AuditLogPage'
import { ContentManagerPage } from './pages/ContentManagerPage'
import { CohortBenchmarksPage } from './pages/CohortBenchmarksPage'
import { DiagnosticHistoryPage } from './pages/DiagnosticHistoryPage'
import { DiagnosticPage } from './pages/DiagnosticPage'
import { DiagnosticReportPage } from './pages/DiagnosticReportPage'
import { ExportCenterPage } from './pages/ExportCenterPage'
import { LearningPathPage } from './pages/LearningPathPage'
import { LoginPage } from './pages/LoginPage'
import { MisconceptionDetailPage } from './pages/MisconceptionDetailPage'
import { MisconceptionsPage } from './pages/MisconceptionsPage'
import { NotificationCenterPage } from './pages/NotificationCenterPage'
import { OperationsPage } from './pages/OperationsPage'
import { PracticePage } from './pages/PracticePage'
import { ProfilePage } from './pages/ProfilePage'
import { PsychometricsPage } from './pages/PsychometricsPage'
import { QuestionAnalyticsPage } from './pages/QuestionAnalyticsPage'
import { QuestionEditorPage } from './pages/QuestionEditorPage'
import { RegisterPage } from './pages/RegisterPage'
import { ResearchAnalyticsPage } from './pages/ResearchAnalyticsPage'
import { StatisticsPage } from './pages/StatisticsPage'
import { TeacherAnalyticsPage } from './pages/TeacherAnalyticsPage'
import { TeacherStudentDetailPage } from './pages/TeacherStudentDetailPage'
import { TeacherStudentsPage } from './pages/TeacherStudentsPage'
import { StudyMaterialsPage } from './pages/StudyMaterialsPage'
import { TeacherMaterialsPage } from './pages/TeacherMaterialsPage'
import { TeacherDiagnosticBuilderPage } from './pages/TeacherDiagnosticBuilderPage'
import { TeacherGamificationPage } from './pages/TeacherGamificationPage'
import { SpacedRepetitionPage } from './pages/SpacedRepetitionPage'
import { ConfidencePage } from './pages/ConfidencePage'
import { StudentExamsPage } from './pages/StudentExamsPage'
import { StudentInterventionsPage } from './pages/StudentInterventionsPage'
import { AchievementsPage } from './pages/AchievementsPage'
import { MaterialStudyCyclesPage } from './pages/MaterialStudyCyclesPage'
import { TeacherGroupsPage } from './pages/TeacherGroupsPage'
import { TeacherInterventionsPage } from './pages/TeacherInterventionsPage'
import { TeacherConfidencePage } from './pages/TeacherConfidencePage'
import { TeacherExamsPage } from './pages/TeacherExamsPage'
import { TeacherExamReviewPage } from './pages/TeacherExamReviewPage'
import { TeacherMaterialCyclesPage } from './pages/TeacherMaterialCyclesPage'

function HomeRedirect() {
  const { user } = useAuth()
  if (user?.role === 'Teacher') return <Navigate to="/teacher/analytics" replace />
  if (user?.role === 'Admin') return <Navigate to="/admin/users" replace />
  return <Navigate to="/profile" replace />
}

export function App() {
  return (
    <>
      <ApiErrorNotice />
      <Routes>
      <Route path="/login" element={<LoginPage />} />
      <Route path="/register" element={<RegisterPage />} />

      <Route element={<ProtectedRoute><AppShell /></ProtectedRoute>}>
        <Route index element={<HomeRedirect />} />
        <Route path="/profile" element={<ProfilePage />} />
        <Route path="/notifications" element={<NotificationCenterPage />} />

        <Route path="/diagnostic" element={<ProtectedRoute roles={['Student']}><DiagnosticPage /></ProtectedRoute>} />
        <Route path="/diagnostics/history" element={<ProtectedRoute roles={['Student']}><DiagnosticHistoryPage /></ProtectedRoute>} />
        <Route path="/diagnostics/:sessionId/continue" element={<ProtectedRoute roles={['Student']}><DiagnosticPage /></ProtectedRoute>} />
        <Route path="/diagnostics/:sessionId/report" element={<ProtectedRoute roles={['Student']}><DiagnosticReportPage /></ProtectedRoute>} />
        <Route path="/misconceptions" element={<ProtectedRoute roles={['Student']}><MisconceptionsPage /></ProtectedRoute>} />
        <Route path="/misconceptions/:misconceptionId" element={<ProtectedRoute roles={['Student']}><MisconceptionDetailPage /></ProtectedRoute>} />
        <Route path="/learning-path" element={<ProtectedRoute roles={['Student']}><LearningPathPage /></ProtectedRoute>} />
        <Route path="/practice" element={<ProtectedRoute roles={['Student']}><PracticePage /></ProtectedRoute>} />
        <Route path="/practice/:sessionId/continue" element={<ProtectedRoute roles={['Student']}><PracticePage /></ProtectedRoute>} />
        <Route path="/statistics" element={<ProtectedRoute roles={['Student']}><StatisticsPage /></ProtectedRoute>} />
        <Route path="/materials" element={<ProtectedRoute roles={['Student']}><StudyMaterialsPage /></ProtectedRoute>} />
        <Route path="/repetition" element={<ProtectedRoute roles={['Student']}><SpacedRepetitionPage /></ProtectedRoute>} />
        <Route path="/confidence" element={<ProtectedRoute roles={['Student']}><ConfidencePage /></ProtectedRoute>} />
        <Route path="/exams" element={<ProtectedRoute roles={['Student']}><StudentExamsPage /></ProtectedRoute>} />
        <Route path="/interventions" element={<ProtectedRoute roles={['Student']}><StudentInterventionsPage /></ProtectedRoute>} />
        <Route path="/achievements" element={<ProtectedRoute roles={['Student']}><AchievementsPage /></ProtectedRoute>} />
        <Route path="/material-cycles" element={<ProtectedRoute roles={['Student']}><MaterialStudyCyclesPage /></ProtectedRoute>} />

        <Route path="/teacher/analytics" element={<ProtectedRoute roles={['Teacher', 'Admin']}><TeacherAnalyticsPage /></ProtectedRoute>} />
        <Route path="/teacher/students" element={<ProtectedRoute roles={['Teacher', 'Admin']}><TeacherStudentsPage /></ProtectedRoute>} />
        <Route path="/teacher/students/:studentId" element={<ProtectedRoute roles={['Teacher', 'Admin']}><TeacherStudentDetailPage /></ProtectedRoute>} />
        <Route path="/teacher/benchmarks" element={<ProtectedRoute roles={['Teacher', 'Admin']}><CohortBenchmarksPage /></ProtectedRoute>} />
        <Route path="/teacher/interventions" element={<ProtectedRoute roles={['Teacher', 'Admin']}><TeacherInterventionsPage /></ProtectedRoute>} />
        <Route path="/teacher/questions/analytics" element={<ProtectedRoute roles={['Teacher', 'Admin']}><QuestionAnalyticsPage /></ProtectedRoute>} />
        <Route path="/teacher/psychometrics" element={<ProtectedRoute roles={['Teacher', 'Admin']}><PsychometricsPage /></ProtectedRoute>} />
        <Route path="/teacher/research" element={<ProtectedRoute roles={['Teacher', 'Admin']}><ResearchAnalyticsPage /></ProtectedRoute>} />
        <Route path="/teacher/content" element={<ProtectedRoute roles={['Teacher', 'Admin']}><ContentManagerPage /></ProtectedRoute>} />
        <Route path="/teacher/content/:questionId" element={<ProtectedRoute roles={['Teacher', 'Admin']}><QuestionEditorPage /></ProtectedRoute>} />
        <Route path="/teacher/exports" element={<ProtectedRoute roles={['Teacher', 'Admin']}><ExportCenterPage /></ProtectedRoute>} />
        <Route path="/teacher/materials" element={<ProtectedRoute roles={['Teacher', 'Admin']}><TeacherMaterialsPage /></ProtectedRoute>} />
        <Route path="/teacher/diagnostics" element={<ProtectedRoute roles={['Teacher', 'Admin']}><TeacherDiagnosticBuilderPage /></ProtectedRoute>} />
        <Route path="/teacher/gamification" element={<ProtectedRoute roles={['Teacher', 'Admin']}><TeacherGamificationPage /></ProtectedRoute>} />
        <Route path="/teacher/groups" element={<ProtectedRoute roles={['Teacher', 'Admin']}><TeacherGroupsPage /></ProtectedRoute>} />
        <Route path="/teacher/confidence" element={<ProtectedRoute roles={['Teacher', 'Admin']}><TeacherConfidencePage /></ProtectedRoute>} />
        <Route path="/teacher/exams" element={<ProtectedRoute roles={['Teacher', 'Admin']}><TeacherExamsPage /></ProtectedRoute>} />
        <Route path="/teacher/exams/:examId" element={<ProtectedRoute roles={['Teacher', 'Admin']}><TeacherExamReviewPage /></ProtectedRoute>} />
        <Route path="/teacher/material-cycles" element={<ProtectedRoute roles={['Teacher', 'Admin']}><TeacherMaterialCyclesPage /></ProtectedRoute>} />

        <Route path="/admin/users" element={<ProtectedRoute roles={['Admin']}><AdminUsersPage /></ProtectedRoute>} />
        <Route path="/admin/audit" element={<ProtectedRoute roles={['Admin']}><AuditLogPage /></ProtectedRoute>} />
        <Route path="/admin/operations" element={<ProtectedRoute roles={['Admin']}><OperationsPage /></ProtectedRoute>} />
      </Route>

        <Route path="*" element={<Navigate to="/" replace />} />
      </Routes>
    </>
  )
}
