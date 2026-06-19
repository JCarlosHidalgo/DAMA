import http from 'k6/http';
import { check, sleep } from 'k6';
import { config, users, thresholds, constantVusScenario } from './lib/config.js';
import { login, authHeaders } from './lib/auth.js';

export const options = {
  scenarios: constantVusScenario(),
  thresholds: thresholds(),
};

export function setup() {
  const client = login(users.client);
  const teacher = login(users.teacher);
  return { clientToken: client.accessToken, teacherToken: teacher.accessToken };
}

export default function (tokens) {
  if (tokens.clientToken) {
    const coursesRes = http.get(
      `${config.courseManagementBaseUrl}/api/course-management/course`,
      { ...authHeaders(tokens.clientToken), tags: { name: 'list-courses' } },
    );
    check(coursesRes, { 'courses 200': (r) => r.status === 200 });
  }

  if (tokens.teacherToken) {
    const teacherCoursesRes = http.get(
      `${config.courseManagementBaseUrl}/api/course-management/course/teacher/me`,
      { ...authHeaders(tokens.teacherToken), tags: { name: 'teacher-courses' } },
    );
    check(teacherCoursesRes, { 'teacher courses 200': (r) => r.status === 200 });
  }

  sleep(1);
}

export function handleSummary(data) {
  return { '/reports/course-management/summary.json': JSON.stringify(data, null, 2) };
}
