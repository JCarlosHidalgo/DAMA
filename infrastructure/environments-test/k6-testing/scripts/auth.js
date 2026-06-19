import http from 'k6/http';
import { check, sleep } from 'k6';
import { config, users, thresholds, constantVusScenario } from './lib/config.js';
import { login, authHeaders } from './lib/auth.js';

export const options = {
  scenarios: constantVusScenario(),
  thresholds: thresholds(),
};

export function setup() {
  const admin = login(users.admin);
  return { adminToken: admin.accessToken };
}

export default function (tokens) {
  if (!tokens.adminToken) {
    sleep(1);
    return;
  }

  const tenantsRes = http.get(
    `${config.authBaseUrl}/api/auth/tenants`,
    { ...authHeaders(tokens.adminToken), tags: { name: 'tenants' } },
  );
  check(tenantsRes, { 'tenants 200': (r) => r.status === 200 });

  sleep(1);
}

export function handleSummary(data) {
  return { '/reports/auth/summary.json': JSON.stringify(data, null, 2) };
}
