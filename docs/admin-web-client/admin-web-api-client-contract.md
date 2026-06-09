# Admin Web Client — API Client Contract

All API functions return `AizenApiResponse<T>`. Callers must check `response.data.success` before consuming `response.data.data`.

Base URL: `VITE_BFF_BASE_URL` (env var)  
All paths below are relative to `/api/v1/admin-panel`.

---

## Auth API

```typescript
// src/api/auth/authApi.ts

export const authApi = {
  loginWithUsername: (body: LoginWithUsernameRequest) =>
    bffClient.post<AizenApiResponse<UserLoginResponse>>('/auth/login/username', body),

  loginWithPhone: (body: LoginWithPhoneRequest) =>
    bffClient.post<AizenApiResponse<UserLoginResponse>>('/auth/login/phone', body),

  loginWithOtp: (body: LoginWithOtpRequest) =>
    bffClient.post<AizenApiResponse<UserLoginResponse>>('/auth/login/otp', body),

  sendOtp: (body: SendOtpRequest) =>
    bffClient.post<AizenApiResponse<SendOtpResult>>('/auth/otp/send', body),

  checkOtp: (body: CheckOtpRequest) =>
    bffClient.post<AizenApiResponse<CheckOtpResult>>('/auth/otp/check', body),

  refresh: (body: RefreshLoginRequest) =>
    bffClient.post<AizenApiResponse<UserLoginResponse>>('/auth/refresh', body),

  changePassword: (body: ChangePasswordRequest) =>
    bffClient.post<AizenApiResponse<ChangePasswordResult>>('/auth/password/change', body),
};
```

---

## Dashboard API

```typescript
// src/api/dashboard/dashboardApi.ts

export const dashboardApi = {
  getOverview: () =>
    bffClient.get<AizenApiResponse<AdminDashboardOverviewResponse>>('/dashboard/overview'),
};
```

---

## Identity API

```typescript
// src/api/identity/identityApi.ts

export const identityApi = {
  // General profiles
  getProfiles: (params: {
    roleContext?: string;
    approvalStatus?: string;
    pageIndex?: number;
    pageSize?: number;
  }) => bffClient.get<AizenApiResponse<AdminUserOverviewResponse>>('/identity/profiles', { params }),

  getProfileDetail: (profileId: string, roleContext = 'General') =>
    bffClient.get<AizenApiResponse<ProfileDetailResult>>(
      `/identity/profiles/${profileId}`,
      { params: { roleContext } }
    ),

  getProfileWithRoles: (profileId: string) =>
    bffClient.get<AizenApiResponse<ProfileWithRolesResult>>(
      `/identity/profiles/${profileId}/with-roles`
    ),

  // Organizers
  getOrganizerProfiles: (params: { pageIndex?: number; pageSize?: number }) =>
    bffClient.get<AizenApiResponse<PagedOrganizerProfileResult>>(
      '/identity/organizers/profiles', { params }
    ),

  getOrganizerProfile: (profileId: string) =>
    bffClient.get<AizenApiResponse<OrganizerProfileResult>>(
      `/identity/organizers/profiles/${profileId}`
    ),

  getOrganizerProfileWithUser: (profileId: string) =>
    bffClient.get<AizenApiResponse<OrganizerProfileWithUserResult>>(
      `/identity/organizers/profiles/${profileId}/with-user`
    ),

  approveOrganizerProfile: (userId: number, profileId: string) =>
    bffClient.post<AizenApiResponse<AdminBffCommandResult>>(
      `/identity/organizers/${userId}/profiles/${profileId}/approve`
    ),

  rejectOrganizerProfile: (userId: number, profileId: string, body: AdminBffRejectRequest) =>
    bffClient.post<AizenApiResponse<AdminBffCommandResult>>(
      `/identity/organizers/${userId}/profiles/${profileId}/reject`, body
    ),

  // Venues
  getVenueProfiles: (params: { pageIndex?: number; pageSize?: number }) =>
    bffClient.get<AizenApiResponse<PagedVenueProfileResult>>(
      '/identity/venues/profiles', { params }
    ),

  getVenueProfile: (profileId: string) =>
    bffClient.get<AizenApiResponse<VenueProfileResult>>(
      `/identity/venues/profiles/${profileId}`
    ),

  approveVenueProfile: (userId: number, profileId: string) =>
    bffClient.post<AizenApiResponse<AdminBffCommandResult>>(
      `/identity/venues/${userId}/profiles/${profileId}/approve`
    ),

  rejectVenueProfile: (userId: number, profileId: string, body: AdminBffRejectRequest) =>
    bffClient.post<AizenApiResponse<AdminBffCommandResult>>(
      `/identity/venues/${userId}/profiles/${profileId}/reject`, body
    ),

  // Participants
  getParticipantProfiles: (params: { pageIndex?: number; pageSize?: number }) =>
    bffClient.get<AizenApiResponse<PagedParticipantProfileResult>>(
      '/identity/participant/profiles', { params }
    ),

  getParticipantProfile: (profileId: string) =>
    bffClient.get<AizenApiResponse<ParticipantProfileResult>>(
      `/identity/participant/profiles/${profileId}`
    ),
};
```

---

## Files API

```typescript
// src/api/files/filesApi.ts

export const filesApi = {
  getFileReviewOverview: (fileId: number) =>
    bffClient.get<AizenApiResponse<AdminFileReviewOverviewResponse>>(`/files/${fileId}`),

  createReadUrl: (fileId: number, body: CreateReadUrlRequest) =>
    bffClient.post<AizenApiResponse<FileAccessUrlResult>>(`/files/${fileId}/read-url`, body),

  bulkCreateReadUrls: (body: BulkGenerateReadUrlsRequest) =>
    bffClient.post<AizenApiResponse<FileAccessUrlResult[]>>('/files/bulk-read-urls', body),

  deleteFile: (fileId: number) =>
    bffClient.delete<AizenApiResponse<AdminBffCommandResult>>(`/files/${fileId}`),

  updateVisibility: (fileId: number, body: UpdateVisibilityRequest) =>
    bffClient.patch<AizenApiResponse<void>>(`/files/${fileId}/visibility`, body),
};
```

---

## Vessels API

```typescript
// src/api/vessels/vesselsApi.ts

export const vesselsApi = {
  getVessels: (params: {
    pageIndex?: number;
    pageSize?: number;
    searchTerm?: string;
    isArchived?: boolean;
  }) => bffClient.get<AizenApiResponse<AdminVesselOverviewResponse>>('/vessels', { params }),

  getFormOptions: () =>
    bffClient.get<AizenApiResponse<AdminVesselFormOptionsResponse>>('/vessels/form-options'),

  getVesselById: (vesselId: number) =>
    bffClient.get<AizenApiResponse<GetVesselDetailResponse>>(`/vessels/${vesselId}`),

  getVesselDetail: (vesselId: number) =>
    bffClient.get<AizenApiResponse<AdminVesselDocumentsResponse>>(`/vessels/${vesselId}/detail`),

  updateVessel: (vesselId: number, body: UpdateVesselRequest) =>
    bffClient.put<AizenApiResponse<UpdateVesselResponse>>(`/vessels/${vesselId}`, body),

  archiveVessel: (vesselId: number, body: ArchiveVesselRequest) =>
    bffClient.patch<AizenApiResponse<ArchiveVesselResponse>>(`/vessels/${vesselId}/archive`, body),

  restoreVessel: (vesselId: number) =>
    bffClient.patch<AizenApiResponse<RestoreVesselResponse>>(`/vessels/${vesselId}/restore`),

  updateVesselStatus: (vesselId: number, body: UpdateVesselStatusRequest) =>
    bffClient.patch<AizenApiResponse<UpdateVesselStatusResponse>>(`/vessels/${vesselId}/status`, body),

  removeVesselDocument: (vesselId: number, documentId: number) =>
    bffClient.delete<AizenApiResponse<RemoveVesselDocumentResponse>>(
      `/vessels/${vesselId}/documents/${documentId}`
    ),
};
```

---

## Service Requests API

```typescript
// src/api/serviceRequests/serviceRequestsApi.ts

export const serviceRequestsApi = {
  getServiceRequests: (params: {
    status?: string;
    vesselId?: number;
    pageIndex?: number;
    pageSize?: number;
  }) => bffClient.get<AizenApiResponse<AdminServiceRequestListResponse>>('/service-requests', { params }),

  getFilterOptions: () =>
    bffClient.get<AizenApiResponse<AdminServiceRequestFilterOptionsResponse>>(
      '/service-requests/filter-options'
    ),

  getDisputes: (params: { status?: string; pageIndex?: number; pageSize?: number }) =>
    bffClient.get<AizenApiResponse<GetAdminDisputeListResponse>>(
      '/service-requests/disputes', { params }
    ),

  getServiceRequestDetail: (serviceRequestId: number) =>
    bffClient.get<AizenApiResponse<AdminServiceRequestOperationDetailResponse>>(
      `/service-requests/${serviceRequestId}`
    ),

  getTimeline: (serviceRequestId: number) =>
    bffClient.get<AizenApiResponse<AdminServiceRequestTimelineResponse>>(
      `/service-requests/${serviceRequestId}/timeline`
    ),

  cancelServiceRequest: (serviceRequestId: number, body: CancelServiceRequestRequest) =>
    bffClient.patch<AizenApiResponse<CancelServiceRequestResponse>>(
      `/service-requests/${serviceRequestId}/cancel`, body
    ),

  approveCompletion: (serviceRequestId: number, body: ApproveServiceRequestCompletionRequest) =>
    bffClient.patch<AizenApiResponse<ApproveServiceRequestCompletionResponse>>(
      `/service-requests/${serviceRequestId}/completion/approve`, body
    ),

  rejectCompletion: (serviceRequestId: number, body: RejectServiceRequestCompletionRequest) =>
    bffClient.patch<AizenApiResponse<RejectServiceRequestCompletionResponse>>(
      `/service-requests/${serviceRequestId}/completion/reject`, body
    ),

  changeDisputeStatus: (
    serviceRequestId: number,
    disputeId: number,
    body: ChangeServiceRequestDisputeStatusRequest
  ) => bffClient.patch<AizenApiResponse<AdminBffCommandResult>>(
    `/service-requests/${serviceRequestId}/disputes/${disputeId}/status`, body
  ),

  resolveDispute: (
    serviceRequestId: number,
    disputeId: number,
    body: ResolveServiceRequestDisputeRequest
  ) => bffClient.patch<AizenApiResponse<ResolveServiceRequestDisputeResponse>>(
    `/service-requests/${serviceRequestId}/disputes/${disputeId}/resolve`, body
  ),
};
```

---

## Reference Data API

```typescript
// src/api/referenceData/referenceDataApi.ts

export const referenceDataApi = {
  getLookupGroups: () =>
    bffClient.get<AizenApiResponse<LookupGroupListResult>>('/reference-data/lookup-groups'),

  getLookupTree: () =>
    bffClient.get<AizenApiResponse<LookupGroupTreeResult>>('/reference-data/lookup-tree'),

  getLookupItemsByGroupCode: (groupCode: string) =>
    bffClient.get<AizenApiResponse<LookupItemListResult>>(
      `/reference-data/lookup/${groupCode}/items`
    ),

  getCurrencies: () =>
    bffClient.get<AizenApiResponse<CurrencyListResult>>('/reference-data/currencies'),

  getCountries: () =>
    bffClient.get<AizenApiResponse<CountryListResult>>('/reference-data/locations/countries'),

  getCities: (countryId?: number) =>
    bffClient.get<AizenApiResponse<CityListResult>>(
      '/reference-data/locations/cities',
      { params: countryId ? { countryId } : {} }
    ),

  getMeasurementUnits: (type?: string) =>
    bffClient.get<AizenApiResponse<MeasurementUnitListResult>>(
      '/reference-data/measurement-units',
      { params: type ? { type } : {} }
    ),

  getSystemParameters: () =>
    bffClient.get<AizenApiResponse<SystemParameterListResult>>(
      '/reference-data/system-parameters'
    ),
};
```

---

## Response Unwrap Helper

```typescript
// src/api/unwrap.ts
import type { AizenApiResponse } from '../types/shared.types';

export function unwrap<T>(response: { data: AizenApiResponse<T> }): T {
  if (!response.data.success || response.data.data === null) {
    throw new Error(response.data.message ?? 'BFF request failed');
  }
  return response.data.data;
}
```

Usage in query functions:

```typescript
export async function getDashboardOverview(): Promise<AdminDashboardOverviewResponse> {
  const response = await dashboardApi.getOverview();
  return unwrap(response);
}
```
