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
  return { clientToken: client.accessToken };
}

export default function (tokens) {
  if (tokens.clientToken) {
    const headers = authHeaders(tokens.clientToken);

    const templatesRes = http.get(
      `${config.paymentBaseUrl}/api/payment/debt-template`,
      { ...headers, tags: { name: 'debt-templates' } },
    );
    check(templatesRes, { 'debt templates 200': (r) => r.status === 200 });

    const summaryRes = http.get(
      `${config.paymentBaseUrl}/api/payment/summary`,
      { ...headers, tags: { name: 'payment-summary' } },
    );
    check(summaryRes, { 'summary 200': (r) => r.status === 200 });
  }

  sleep(1);
}

export function handleSummary(data) {
  return { '/reports/payment/summary.json': JSON.stringify(data, null, 2) };
}
