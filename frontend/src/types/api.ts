export type UserRole = 'Student' | 'Teacher' | 'Admin'
export type DiagnosticStatus = 'Created' | 'InProgress' | 'Completed' | 'Analyzing' | 'ReportReady' | 'Cancelled'
export type MisconceptionStatus = 'Unknown' | 'Suspected' | 'Detected' | 'CorrectionInProgress' | 'Corrected' | 'RecheckRequired'
export type LearningStepStatus = 'Pending' | 'InProgress' | 'Completed' | 'Skipped'
export type LearningPathStatus = 'Active' | 'Completed' | 'Rebuilt'
export type PracticeStatus = 'Created' | 'InProgress' | 'Completed'
export type ExerciseType = 'Explanation' | 'WorkedExample' | 'ConceptCheck' | 'GuidedPractice' | 'IndependentPractice' | 'Transfer'
export type ContentStatus = 'Draft' | 'Published' | 'Archived'

export interface AuthUser {
  id: string
  email: string
  displayName: string
  role: UserRole
}

export interface AuthResponse {
  accessToken: string
  refreshToken: string
  accessTokenExpiresAt: string
  refreshTokenExpiresAt: string
  user: AuthUser
}

export interface TopicProgress {
  topicId: string
  code: string
  name: string
  mastery: number
  uncertainty: number
  observationCount: number
}

export interface Recommendation {
  id: string
  type: string
  title: string
  rationale: string
  priority: number
  topicId?: string | null
  misconceptionId?: string | null
  isDismissed: boolean
  generatedAt: string
}

export interface Dashboard {
  overallMastery: number
  activeMisconceptions: number
  correctedMisconceptions: number
  completedDiagnostics: number
  completedPracticeSessions: number
  topics: TopicProgress[]
  recommendations: Recommendation[]
}

export interface DiagnosticSession {
  id: string
  status: DiagnosticStatus
  plannedQuestionCount: number
  answeredQuestionCount: number
  startedAt?: string | null
  completedAt?: string | null
  overallScore?: number | null
}

export interface AnswerOption {
  id: string
  text: string
  sortOrder: number
}

export interface DiagnosticQuestion {
  questionId: string
  versionId: string
  topicCode: string
  topicName: string
  prompt: string
  difficulty: string
  isTransfer: boolean
  options: AnswerOption[]
  selectionExplanation: string
}

export interface AnswerFeedback {
  isCorrect: boolean
  feedback: string
  correctExplanation: string
  suspectedMisconceptionCode?: string | null
  updatedConfidence?: number | null
  updatedStatus?: MisconceptionStatus | null
}

export interface UserMisconception {
  misconceptionId: string
  code: string
  title: string
  description: string
  confidence: number
  status: MisconceptionStatus
  evidenceCount: number
  lastDetectedAt?: string | null
}

export interface Evidence {
  id: string
  kind: string
  weight: number
  relevance: number
  explanation: string
  observedAt: string
}

export interface EvidenceContribution {
  evidenceId: string
  kind: string
  signedContribution: number
  share: number
  direction: 'supports' | 'contradicts'
  reason: string
}

export interface DiagnosticReasoning {
  summary: string
  confidenceBand: string
  supportingReasons: string[]
  contradictingReasons: string[]
  nextAction: string
  contributions: EvidenceContribution[]
}

export interface MisconceptionDetail {
  state: UserMisconception
  evidence: Evidence[]
  correctiveExplanation: string
  diagnosticRationale: string
  reasoning: DiagnosticReasoning
}

export interface DiagnosticReport {
  sessionId: string
  accuracy: number
  answered: number
  correct: number
  topics: TopicProgress[]
  misconceptions: UserMisconception[]
  summary: string
  generatedAt: string
}

export interface DiagnosticComparisonEndpoint {
  sessionId: string
  generatedAt: string
  accuracy: number
  detectedMisconceptions: number
}

export interface TopicComparison {
  topicId: string
  code: string
  name: string
  fromMastery?: number | null
  toMastery?: number | null
  delta?: number | null
}

export interface DiagnosticComparison {
  from: DiagnosticComparisonEndpoint
  to: DiagnosticComparisonEndpoint
  accuracyDelta: number
  detectedMisconceptionDelta: number
  topics: TopicComparison[]
}

export interface LearningPathStep {
  id: string
  position: number
  topicId: string
  topicCode: string
  topicName: string
  misconceptionId?: string | null
  misconceptionCode?: string | null
  misconceptionTitle?: string | null
  priority: number
  status: LearningStepStatus
  reason: string
}

export interface LearningPath {
  id: string
  revision: number
  status: LearningPathStatus
  builtAt: string
  buildReason: string
  steps: LearningPathStep[]
}

export interface PracticeSession {
  id: string
  topicId: string
  misconceptionId?: string | null
  status: PracticeStatus
  targetExercises: number
  completedExercises: number
  startedAt?: string | null
  completedAt?: string | null
}

export interface PracticeResult {
  session: PracticeSession
  correct: number
  total: number
  transferPassed: boolean
  misconceptionConfidence?: number | null
  misconceptionStatus?: MisconceptionStatus | null
  updatedLearningPath?: LearningPath | null
}

export interface TimelinePoint {
  at: string
  value: number
  label: string
}

export interface MisconceptionPrevalence {
  misconceptionId: string
  code: string
  title: string
  studentsAffected: number
  totalStudents: number
  prevalence: number
  meanConfidence: number
  evidenceCount: number
}

export interface CohortAnalytics {
  students: number
  diagnosticSessions: number
  answers: number
  meanAccuracy: number
  misconceptions: MisconceptionPrevalence[]
  meanTopicMastery: TopicProgress[]
}

export interface Topic {
  id: string
  code: string
  nameRu: string
  nameEn: string
  description: string
  sortOrder: number
  status: ContentStatus
}

export interface MisconceptionCatalog {
  id: string
  topicId: string
  code: string
  title: string
  description: string
  correctiveExplanation: string
  diagnosticRationale: string
  status: ContentStatus
}

export interface QuestionSummary {
  id: string
  topicId: string
  code: string
  kind: string
  status: ContentStatus
  currentVersionNumber: number
  publishedAt?: string | null
}

export interface AdminUser {
  id: string
  email: string
  displayName: string
  role: UserRole
  isActive: boolean
  lastLoginAt?: string | null
  createdAt: string
}

export interface StudentListItem {
  userId: string
  displayName: string
  email: string
  overallMastery: number
  activeMisconceptions: number
  completedDiagnostics: number
  answeredQuestions: number
  wrongAnswers: number
  diagnosticAccuracy: number
  activeMisconceptionTitles: string[]
  lastActivityAt?: string | null
}

export interface StudentMistake {
  id: string
  submittedAt: string
  source: string
  topicName: string
  prompt: string
  selectedAnswer: string
  correctAnswer: string
  misconceptionTitle?: string | null
}

export interface StudentOverview {
  userId: string
  displayName: string
  email: string
  overallMastery: number
  activeMisconceptions: number
  correctedMisconceptions: number
  completedDiagnostics: number
  completedPracticeSessions: number
  lastActivityAt?: string | null
  topics: TopicProgress[]
  misconceptions: UserMisconception[]
}

export interface QuestionAnalytics {
  questionId: string
  code: string
  responses: number
  correctRate: number
  difficulty: number
  discrimination: number
  qualityBand: string
  distractorEntropy: number
  medianResponseSeconds: number
}

export interface Reliability {
  cronbachAlpha: number
  learners: number
  items: number
  interpretation: string
}

export interface InterventionEffectiveness {
  misconceptionId: string
  code: string
  title: string
  learners: number
  meanConfidenceReduction: number
  meanMasteryGain: number
  transferPassRate: number
  meanExercises: number
  compositeEffectiveness: number
}

export interface CohortSegment {
  userId: string
  displayName: string
  segment: string
  priority: number
  rationale: string
}

export interface LearnerForecastTopic {
  topicId: string
  topicCode: string
  topicName: string
  currentMastery: number
  forecast7Days: number
  forecast30Days: number
  dailyTrend: number
  confidence: number
  direction: string
}

export interface LearnerForecast {
  userId: string
  topics: LearnerForecastTopic[]
  meanCurrentMastery: number
  meanForecast30Days: number
  overallDirection: string
}

export interface PsychometricItem {
  questionId: string
  code: string
  topicCode: string
  responses: number
  correctRate: number
  irtDifficulty: number
  discrimination: number
  informationAtAverageAbility: number
  medianResponseSeconds: number
  qualityBand: string
}

export interface CohortBenchmark {
  userId: string
  displayName: string
  mastery: number
  percentile: number
  zScore: number
  band: string
  activeMisconceptions: number
}

export interface StudentRisk {
  userId: string
  displayName: string
  risk: number
  level: string
  drivers: string[]
  mastery: number
  activeMisconceptions: number
  daysInactive: number
}

export interface MisconceptionCluster {
  cluster: number
  label: string
  learners: number
  centroid: number[]
  userIds: string[]
}

export interface ContentDrift {
  questionId: string
  code: string
  baselineResponses: number
  recentResponses: number
  accuracyShift: number
  responseTimeShift: number
  entropyShift: number
  driftScore: number
  requiresReview: boolean
  reason: string
}

export interface AdvancedSystemOverview {
  learners: number
  questions: number
  answers: number
  activeMisconceptions: number
  atRiskLearners: number
  questionsRequiringReview: number
  meanMastery: number
  meanDiagnosticAccuracy: number
  generatedAt: string
}

export interface AuditLog {
  id: string
  actorUserId?: string | null
  action: string
  entityType: string
  entityId?: string | null
  oldValueJson: string
  newValueJson: string
  requestId: string
  createdAt: string
}

export interface NotificationItem {
  id: string
  type: string
  title: string
  body: string
  isRead: boolean
  createdAt: string
  readAt?: string | null
}

export interface CooccurrenceEdge {
  misconceptionAId: string
  misconceptionATitle: string
  misconceptionBId: string
  misconceptionBTitle: string
  together: number
  studentsA: number
  studentsB: number
  totalStudents: number
  jaccard: number
  lift: number
}

export interface CalibrationBucket {
  from: number
  to: number
  predictions: number
  confirmed: number
  observedRate: number
}

export interface CalibrationReport {
  buckets: CalibrationBucket[]
  brierScore: number
  meanAbsoluteCalibrationError: number
}

export interface PathStability {
  stability: number
  revisions: number
  meanRetention: number
  interpretation: string
}

export interface QuestionVersionAdmin {
  id: string
  versionNumber: number
  prompt: string
  correctExplanation: string
  difficulty: string
  isTransfer: boolean
  options: AnswerOptionAdmin[]
}

export interface AnswerOptionAdmin {
  id: string
  text: string
  isCorrect: boolean
  misconceptionId?: string | null
  feedback: string
  sortOrder: number
}

export interface QuestionDetail {
  question: QuestionSummary
  currentVersion: QuestionVersionAdmin
  testedMisconceptionIds: string[]
}

export interface ApiEndpointMetric {
  method: string
  route: string
  requests: number
  failures: number
  meanMilliseconds: number
  maxMilliseconds: number
  lastSeenAt?: string | null
}

export interface ApiRuntimeSnapshot {
  totalRequests: number
  totalFailures: number
  meanMilliseconds: number
  endpoints: ApiEndpointMetric[]
  capturedAt: string
}

export interface ProcessSnapshot {
  processId: number
  machineName: string
  processorCount: number
  workingSetBytes: number
  privateMemoryBytes: number
  threads: number
  startedAt: string
  uptimeSeconds: number
  framework: string
  os: string
}
