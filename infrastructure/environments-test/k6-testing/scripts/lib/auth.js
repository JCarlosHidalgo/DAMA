import http from 'k6/http';
import { check } from 'k6';
import { config } from './config.js';

export function login(user) {
  const res = http.post(
    `${config.authBaseUrl}/api/auth/login`,
    JSON.stringify({ username: user.username, password: user.password }),
    { headers: { 'Content-Type': 'application/json' }, tags: { name: 'login' } },
  );
  check(res, { 'login 200': (r) => r.status === 200 });

  let body = null;
  try {
    body = res.json();
  } catch (error) {
    body = null;
  }

  return {
    accessToken: body && (body.accessToken || body.AccessToken),
    refreshToken: body && (body.refreshToken || body.RefreshToken),
    response: res,
  };
}

export function authHeaders(accessToken) {
  return { headers: { Authorization: `Bearer ${accessToken}` } };
}
