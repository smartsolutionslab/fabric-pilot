import { HttpAgent } from '@ag-ui/client';
import { accessToken } from '@/auth/accessToken';

/**
 * A conversation with one of the host's agents over AG-UI: each run posts the whole conversation to
 * /api/agents/{name}, like every other call through /api, and reads the answer as it streams back.
 */
export function connectAgent(name: string) {
  return new HttpAgent({
    url: `/api/agents/${name}`,
    // The token is read for every run, as the API client reads it for every call: it may have been renewed meanwhile.
    fetch: (url, init) => {
      const headers = new Headers(init.headers);
      const token = accessToken();
      if (token) {
        headers.set('Authorization', `Bearer ${token}`);
      }
      return fetch(url, { ...init, headers });
    },
  });
}
