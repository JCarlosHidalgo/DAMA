import http from 'k6/http';
import { check, sleep } from 'k6';
import { config, users, thresholds, constantVusScenario } from './lib/config.js';
import { login, authHeaders } from './lib/auth.js';

export const options = {
  scenarios: constantVusScenario(),
  thresholds: thresholds(),
};

export function setup() {
  const student = login(users.student);
  return { studentToken: student.accessToken };
}

export default function (tokens) {
  if (tokens.studentToken) {
    const headers = authHeaders(tokens.studentToken);

    const scheduledRes = http.get(
      `${config.attendanceBaseUrl}/api/attendance/attendance/scheduled/me`,
      { ...headers, tags: { name: 'scheduled-attendance-me' } },
    );
    check(scheduledRes, { 'scheduled me 200': (r) => r.status === 200 });

    const remainRes = http.get(
      `${config.attendanceBaseUrl}/api/attendance/remain/me`,
      { ...headers, tags: { name: 'remain-me' } },
    );
    check(remainRes, { 'remain me 200': (r) => r.status === 200 });
  }

  sleep(1);
}

export function handleSummary(data) {
  return { '/reports/attendance/summary.json': JSON.stringify(data, null, 2) };
}
