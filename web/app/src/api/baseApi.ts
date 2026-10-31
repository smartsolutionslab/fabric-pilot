import { createApi, fetchBaseQuery } from '@reduxjs/toolkit/query/react';
import { accessToken } from '@/auth/accessToken';

/** The API the generated endpoints (src/api/generated.ts) are injected into; /api reaches the host. */
export const baseApi = createApi({
  baseQuery: fetchBaseQuery({
    baseUrl: '/api',
    prepareHeaders: (headers) => {
      const token = accessToken();
      if (token) {
        headers.set('Authorization', `Bearer ${token}`);
      }
      return headers;
    },
  }),
  endpoints: () => ({}),
});
