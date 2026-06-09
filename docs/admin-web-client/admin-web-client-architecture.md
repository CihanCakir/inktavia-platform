# Admin Web Client — Architecture

## Principles

- The React admin client communicates **only** with the AdminPanel BFF (`api/v1/admin-panel`).
- The client **never** calls internal module APIs (Identity, Vessel, ServiceRequest, FileStorage, ReferenceData) directly.
- Payment and Profile domain flows are **out of scope**.
- All state management is local to the admin UI; no global backend session is managed beyond tokens.

---

## Technology Recommendations

| Concern | Recommendation |
|---------|----------------|
| Framework | React 18+ (Vite or Next.js App Router) |
| Language | TypeScript 5+ |
| HTTP client | Axios with interceptors or `fetch` wrapper |
| Auth | `keycloak-js` + `@react-keycloak/web` (or custom PKCE hook) |
| State management | React Query (TanStack Query v5) for server state; Zustand or React Context for auth state |
| Forms | React Hook Form + Zod |
| UI Components | shadcn/ui or MUI v5 |
| Routing | React Router v6 or Next.js App Router |
| Bundler | Vite (recommended for SPA) |

---

## Project Structure

```
src/
├── api/                        # BFF API client layer
│   ├── client.ts               # Axios instance with interceptors
│   ├── auth/
│   │   └── authApi.ts
│   ├── dashboard/
│   │   └── dashboardApi.ts
│   ├── identity/
│   │   └── identityApi.ts
│   ├── files/
│   │   └── filesApi.ts
│   ├── vessels/
│   │   └── vesselsApi.ts
│   ├── serviceRequests/
│   │   └── serviceRequestsApi.ts
│   └── referenceData/
│       └── referenceDataApi.ts
│
├── types/                      # TypeScript types (from bff-response-types.md)
│   ├── auth.types.ts
│   ├── dashboard.types.ts
│   ├── identity.types.ts
│   ├── files.types.ts
│   ├── vessels.types.ts
│   ├── serviceRequests.types.ts
│   └── referenceData.types.ts
│
├── hooks/                      # TanStack Query hooks
│   ├── useAuth.ts
│   ├── useDashboard.ts
│   ├── useIdentity.ts
│   ├── useFiles.ts
│   ├── useVessels.ts
│   ├── useServiceRequests.ts
│   └── useReferenceData.ts
│
├── store/                      # Auth token store (Zustand or Context)
│   └── authStore.ts
│
├── pages/                      # Route-level page components
│   ├── LoginPage.tsx
│   ├── DashboardPage.tsx
│   ├── IdentityPage.tsx
│   ├── FilesPage.tsx
│   ├── VesselsPage.tsx
│   └── ServiceRequestsPage.tsx
│
├── components/                 # Shared UI components
│   ├── layout/
│   ├── tables/
│   ├── modals/
│   └── forms/
│
└── router/
    └── AppRouter.tsx
```

---

## API Client Layer

### Axios Instance

```typescript
// src/api/client.ts
import axios from 'axios';
import { getKeycloakToken, getIdentityToken, refreshTokens } from '../store/authStore';

const bffClient = axios.create({
  baseURL: import.meta.env.VITE_BFF_BASE_URL,   // e.g. https://api.inktavia.com
  timeout: 30_000,
});

bffClient.interceptors.request.use((config) => {
  const keycloakToken = getKeycloakToken();
  const identityToken = getIdentityToken();
  if (keycloakToken) config.headers['Authorization'] = `Bearer ${keycloakToken}`;
  if (identityToken) config.headers['X-Aizen-User-Token'] = `Bearer ${identityToken}`;
  return config;
});

bffClient.interceptors.response.use(
  (response) => response,
  async (error) => {
    if (error.response?.status === 401 && !error.config._retry) {
      error.config._retry = true;
      await refreshTokens();
      return bffClient(error.config);
    }
    return Promise.reject(error);
  }
);

export default bffClient;
```

---

## Auth Store

```typescript
// src/store/authStore.ts
interface AuthState {
  keycloakToken: string | null;
  identityToken: string | null;
  identityRefreshToken: string | null;
  setKeycloakToken: (token: string) => void;
  setIdentityTokens: (access: string, refresh: string) => void;
  clearAll: () => void;
}
```

- `keycloakToken`: Keycloak access token (managed via `keycloak-js`)
- `identityToken`: Identity access token (from `UserLoginResponse.accessToken`)
- `identityRefreshToken`: Identity refresh token (from `UserLoginResponse.refreshToken`)

---

## TanStack Query Usage

```typescript
// src/hooks/useDashboard.ts
import { useQuery } from '@tanstack/react-query';
import { getDashboardOverview } from '../api/dashboard/dashboardApi';

export function useDashboardOverview() {
  return useQuery({
    queryKey: ['dashboard', 'overview'],
    queryFn: getDashboardOverview,
    staleTime: 30_000,
  });
}
```

All server-state is managed via `useQuery` and `useMutation`. No manual fetch/loading state in components.

---

## Route Guard

```typescript
// src/router/ProtectedRoute.tsx
import { Navigate } from 'react-router-dom';
import { useAuthStore } from '../store/authStore';

export function ProtectedRoute({ children }: { children: React.ReactNode }) {
  const { keycloakToken, identityToken } = useAuthStore();
  if (!keycloakToken || !identityToken) return <Navigate to="/login" replace />;
  return <>{children}</>;
}
```

---

## Environment Variables

```env
VITE_BFF_BASE_URL=https://api.inktavia.com
VITE_KEYCLOAK_URL=https://auth.inktavia.com
VITE_KEYCLOAK_REALM=inktavia
VITE_KEYCLOAK_CLIENT_ID=admin-panel
```

---

## Module Boundaries

| Module | Admin Client Access | Method |
|--------|---------------------|--------|
| Identity | Via BFF only | `api/v1/admin-panel/identity/*` |
| Vessel | Via BFF only | `api/v1/admin-panel/vessels/*` |
| ServiceRequest | Via BFF only | `api/v1/admin-panel/service-requests/*` |
| FileStorage | Via BFF only | `api/v1/admin-panel/files/*` |
| ReferenceData | Via BFF only | `api/v1/admin-panel/reference-data/*` |
| Payment | **Not in scope** | — |
| Profile | **Not in scope** | — |
