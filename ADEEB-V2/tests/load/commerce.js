import http from 'k6/http';
import { check } from 'k6';

export const options = {
  scenarios: {
    commerce_reads: {
      executor: 'ramping-vus',
      stages: [
        { duration: '15s', target: 10 },
        { duration: '30s', target: 10 },
        { duration: '15s', target: 0 },
      ],
    },
  },
  thresholds: {
    http_req_failed: ['rate<0.01'],
    'http_req_duration{endpoint:public-tariffs}': ['p(95)<500'],
    'http_req_duration{endpoint:student-receipts}': ['p(95)<500'],
    'http_req_duration{endpoint:admin-receipts}': ['p(95)<750'],
  },
};

const baseUrl = __ENV.BASE_URL || 'http://localhost:5000';
const studentToken = __ENV.STUDENT_TOKEN;
const adminToken = __ENV.ADMIN_TOKEN;

export function setup() {
  if (!studentToken || !adminToken) {
    throw new Error('STUDENT_TOKEN and ADMIN_TOKEN are required.');
  }
}

export default function () {
  const responses = http.batch([
    ['GET', `${baseUrl}/api/v2/commerce/tariffs`, null, { tags: { endpoint: 'public-tariffs' } }],
    ['GET', `${baseUrl}/api/v2/commerce/me/payment-receipts?limit=30`, null, {
      headers: { Authorization: `Bearer ${studentToken}` },
      tags: { endpoint: 'student-receipts' },
    }],
    ['GET', `${baseUrl}/api/v2/admin/commerce/payment-receipts?status=Pending&limit=30`, null, {
      headers: { Authorization: `Bearer ${adminToken}` },
      tags: { endpoint: 'admin-receipts' },
    }],
  ]);

  check(responses[0], { 'public tariffs 200': (response) => response.status === 200 });
  check(responses[1], { 'student receipts 200': (response) => response.status === 200 });
  check(responses[2], { 'admin receipts 200': (response) => response.status === 200 });
}
