import { useQuery } from '@tanstack/react-query'
import { Link, useParams } from 'react-router-dom'
import { api } from '../api/client'
import { LoadingView } from '../components/LoadingView'
import { PageHeader } from '../components/PageHeader'
import type { TeacherExamReview } from '../types/api'
import { dateTime, percent } from '../utils/format'

const confidenceLabels: Record<number, string> = {
  1: 'Совсем не уверен',
  2: 'Скорее не уверен',
  3: 'Скорее уверен',
  4: 'Полностью уверен',
}

export function TeacherExamReviewPage() {
  const { examId } = useParams()
  const review = useQuery({
    queryKey: ['teacher-exam-review', examId],
    queryFn: async () => (await api.get<TeacherExamReview>(`/edtech/teacher/exams/${examId}/review`)).data,
    enabled: Boolean(examId),
  })

  if (review.isLoading) return <LoadingView />
  const data = review.data
  if (!data) return null

  return (
    <div>
      <PageHeader
        eyebrow="Экзамен"
        title={data.title}
        description={`${data.audience}. Ответы студентов открываются преподавателю только после того, как экзамен завершили все назначенные студенты.`}
      />

      <div className="exam-review-toolbar">
        <Link className="ghost-button" to="/teacher/exams">← К экзаменам</Link>
        <div className={`exam-review-completion ${data.allCompleted ? 'exam-review-completion--ready' : ''}`}>
          <strong>{data.completedStudents} / {data.assignedStudents}</strong>
          <span>{data.allCompleted ? 'Все завершили - ответы открыты' : 'Завершили экзамен'}</span>
        </div>
      </div>

      {!data.allCompleted ? (
        <section className="panel">
          <h2>Статус студентов</h2>
          <p className="muted">До завершения экзамена всеми назначенными студентами ответы скрыты, чтобы не раскрывать задания раньше времени.</p>
          <div className="table-frame">
            <table className="academic-table">
              <thead><tr><th>Студент</th><th>Статус</th><th>Начал</th><th>Завершил</th></tr></thead>
              <tbody>
                {data.students.map((student) => (
                  <tr key={student.studentId}>
                    <td><strong>{student.studentName}</strong><div className="muted small-text">{student.email}</div></td>
                    <td>{student.status}</td>
                    <td>{dateTime(student.startedAt)}</td>
                    <td>{dateTime(student.completedAt)}</td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
        </section>
      ) : (
        <div className="exam-student-review-list">
          {data.students.map((student) => (
            <section className="panel exam-student-review" key={student.studentId}>
              <div className="exam-student-review__header">
                <div>
                  <span className="eyebrow">Студент</span>
                  <h2>{student.studentName}</h2>
                  <p className="muted">{student.email}</p>
                </div>
                <div className="exam-student-score">
                  <span>Результат</span>
                  <strong>{percent(student.score)}</strong>
                  <small>Завершено {dateTime(student.completedAt)}</small>
                </div>
              </div>

              <div className="exam-answer-review-list">
                {student.answers.map((answer) => (
                  <article className={`exam-answer-review ${answer.isCorrect === true ? 'exam-answer-review--correct' : answer.isCorrect === false ? 'exam-answer-review--wrong' : ''}`} key={`${student.studentId}-${answer.position}`}>
                    <div className="exam-answer-review__top">
                      <span>Задание {answer.position}</span>
                      <strong>{answer.isCorrect === true ? 'Верно' : answer.isCorrect === false ? 'Неверно' : 'Нет ответа'}</strong>
                    </div>
                    <h3>{answer.prompt}</h3>
                    <dl className="exam-answer-details">
                      <div><dt>Ответ студента</dt><dd>{answer.selectedAnswer ?? 'Нет ответа'}</dd></div>
                      <div><dt>Правильный ответ</dt><dd>{answer.correctAnswer}</dd></div>
                      <div><dt>Уверенность</dt><dd>{answer.confidenceLevel ? confidenceLabels[answer.confidenceLevel] ?? '-' : 'Не указана'}</dd></div>
                      <div><dt>Время ответа</dt><dd>{answer.responseTimeMs != null ? `${Math.max(0, Math.round(answer.responseTimeMs / 1000))} сек.` : '-'}</dd></div>
                    </dl>
                    {answer.reasoning ? <div className="exam-answer-note"><strong>Ход рассуждения</strong><p>{answer.reasoning}</p></div> : null}
                    {answer.studentNote ? <div className="exam-answer-note"><strong>Комментарий преподавателю</strong><p>{answer.studentNote}</p></div> : null}
                  </article>
                ))}
              </div>
            </section>
          ))}
        </div>
      )}
    </div>
  )
}
