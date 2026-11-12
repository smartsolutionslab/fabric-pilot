import { type BaseQueryFn, createApi, type FetchArgs, fetchBaseQuery } from '@reduxjs/toolkit/query/react';
import { accessToken } from '@/auth/accessToken';
import openApi from '../../openapi.json';

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
 * form data, one field per value and one part per file; any other body is left to be sent as JSON, unless the
 * endpoint always takes a form.
 */
export function withFiles(body: unknown, always = false): unknown {
  if (typeof body !== 'object' || body === null || body instanceof FormData) {
    return body;
  }
  const values = Object.entries(body);
  if (!always && !values.some(([, value]) => isFile(value) || (Array.isArray(value) && value.some(isFile)))) {
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

interface Operation {
  requestBody?: { content?: Record<string, unknown> };
}

/** Every operation the API takes as multipart form data, read from the document the client is generated from. */
const formOperations = Object.entries(openApi.paths as Record<string, Record<string, Operation>>).flatMap(
  ([path, operations]) =>
    Object.entries(operations)
      .filter(([, operation]) => operation.requestBody?.content?.['multipart/form-data'] !== undefined)
      .map(([method]) => ({
        method: method.toUpperCase(),
        path: new RegExp(`^${path.replace(/\{[^/}]+\}/g, '[^/]+')}$`),
      })),
);

/**
 * The body as the endpoint takes it: form data for an operation the API takes as multipart form data, files or not,
 * and for any body carrying files; JSON otherwise.
 */
export function requestBody({ url, method = 'GET', body }: Pick<FetchArgs, 'url' | 'method' | 'body'>): unknown {
  const path = url.split('?')[0];
  const takesForm = formOperations.some(
    (operation) => operation.method === method.toUpperCase() && operation.path.test(path ?? ''),
  );
  return withFiles(body, takesForm);
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
  fetchFromHost(typeof args === 'string' ? args : { ...args, body: requestBody(args) }, api, extraOptions);

/** The API the generated endpoints (src/api/generated.ts) are injected into; /api reaches the host. */
export const baseApi = createApi({
  baseQuery,
  endpoints: () => ({}),
});
