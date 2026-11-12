import { createApi, fetchBaseQuery } from '@reduxjs/toolkit/query/react';
import { accessToken } from '@/auth/accessToken';

/** Sets what every request carries. */
function prepareHeaders(headers: Headers) {
  const token = accessToken();
  if (token) {
    headers.set('Authorization', `Bearer ${token}`);
  }
  return headers;
}

/** The API the generated endpoints (src/api/generated.ts) are injected into; /api reaches the host. */
export const baseApi = createApi({
  baseQuery: fetchBaseQuery({ baseUrl: '/api', prepareHeaders }),
  endpoints: () => ({}),
});
