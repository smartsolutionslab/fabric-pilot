import { type BaseQueryFn, createApi, type FetchArgs, fetchBaseQuery } from '@reduxjs/toolkit/query/react';
import { accessToken } from '@/auth/accessToken';

/** Sets what every request carries. */
function prepareHeaders(headers: Headers) {
  const token = accessToken();
  if (token) {
    headers.set('Authorization', `Bearer ${token}`);
  }
  return headers;
}

const isFile = (value: unknown) => value instanceof Blob;

/**
 * A body that carries a file (a multipart/form-data endpoint: the generated client types it as an object) is sent as
 * form data, one field per value and one part per file; any other body is left to be sent as JSON.
 */
export function withFiles(body: unknown): unknown {
  if (typeof body !== 'object' || body === null || body instanceof FormData) {
    return body;
  }
  const values = Object.entries(body);
  if (!values.some(([, value]) => isFile(value) || (Array.isArray(value) && value.some(isFile)))) {
    return body;
  }

  const form = new FormData();
  for (const [name, value] of values) {
    for (const part of Array.isArray(value) ? value : [value]) {
      if (part !== undefined && part !== null) {
        form.append(name, isFile(part) ? (part as Blob) : String(part));
      }
    }
  }
  return form;
}

/** JSON (problem details included) as JSON, an empty answer as null, a file as a Blob of its bytes. */
export async function readResponse(response: Response): Promise<unknown> {
  const type = response.headers.get('Content-Type') ?? '';
  if (type.includes('json') || type === '') {
    const text = await response.text();
    return text.length > 0 ? JSON.parse(text) : null;
  }
  if (type.startsWith('text/')) {
    return response.text();
  }
  return response.blob();
}

const fetchFromHost = fetchBaseQuery({ baseUrl: '/api', prepareHeaders, responseHandler: readResponse });

const baseQuery: BaseQueryFn<string | FetchArgs> = (args, api, extraOptions) =>
  fetchFromHost(typeof args === 'string' ? args : { ...args, body: withFiles(args.body) }, api, extraOptions);

/** The API the generated endpoints (src/api/generated.ts) are injected into; /api reaches the host. */
export const baseApi = createApi({
  baseQuery,
  endpoints: () => ({}),
});
