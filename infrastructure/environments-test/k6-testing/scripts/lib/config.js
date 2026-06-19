const num = (value, fallback) => parseInt(value || String(fallback), 10);

export const config = {
  authBaseUrl: __ENV.AUTH_BASE_URL || 'http://AuthService',
  courseManagementBaseUrl: __ENV.COURSE_MANAGEMENT_BASE_URL || 'http://CourseManagementService',
  attendanceBaseUrl: __ENV.ATTENDANCE_BASE_URL || 'http://AttendanceService',
  paymentBaseUrl: __ENV.PAYMENT_BASE_URL || 'http://PaymentService',
  vus: num(__ENV.LOAD_VUS, 10),
  duration: __ENV.LOAD_DURATION || '30s',
  p95Ms: num(__ENV.LOAD_P95_MS, 500),
};

export const users = {
  client: {
    username: __ENV.LOAD_CLIENT_USERNAME || 'Client Example',
    password: __ENV.LOAD_USER_PASSWORD || 'Admin123',
  },
  teacher: {
    username: __ENV.LOAD_TEACHER_USERNAME || 'Teacher Example',
    password: __ENV.LOAD_USER_PASSWORD || 'Admin123',
  },
  student: {
    username: __ENV.LOAD_STUDENT_USERNAME || 'Student Example',
    password: __ENV.LOAD_USER_PASSWORD || 'Admin123',
  },
  admin: {
    username: __ENV.ADMIN_USERNAME || 'Juan Carlos Hidalgo Sosa Admin',
    password: __ENV.ADMIN_PASSWORD || '',
  },
};

export function thresholds() {
  return {
    http_req_failed: ['rate<0.01'],
    http_req_duration: [`p(95)<${config.p95Ms}`],
  };
}

export function constantVusScenario() {
  return {
    load: {
      executor: 'constant-vus',
      vus: config.vus,
      duration: config.duration,
    },
  };
}
