# Admin Panel BFF — Response Types & TypeScript Recommendations

All BFF responses are wrapped in `AizenApiResponse<T>`:

```typescript
interface AizenApiResponse<T> {
  data: T | null;
  success: boolean;
  errorCode: string | null;
  message: string | null;
}
```

---

## Shared Types

```typescript
interface AdminBffWarning {
  module: string;
  message: string;
}

interface AdminBffCommandResult {
  success: boolean;
  message: string | null;
}

interface AdminBffBulkCommandResult {
  totalRequested: number;
  succeeded: number;
  failed: number;
  errors: string[];
}

interface AdminBffRejectRequest {
  reason: string;
}
```

---

## Auth Types

```typescript
interface LoginWithUsernameRequest {
  username: string;
  password: string;
}

interface LoginWithPhoneRequest {
  phoneNumber: string;
  password: string;
}

interface LoginWithOtpRequest {
  phoneNumber: string;
  otpCode: string;
}

interface SendOtpRequest {
  phoneNumber: string;
}

interface CheckOtpRequest {
  phoneNumber: string;
  otpCode: string;
}

interface RefreshLoginRequest {
  refreshToken: string;
}

interface ChangePasswordRequest {
  currentPassword: string;
  newPassword: string;
}

interface UserLoginResponse {
  accessToken: string;
  refreshToken: string;
  expiresIn: number;
  tokenType: string;
}

interface SendOtpResult {
  referenceId: string | null;
  expiresAt: string | null;    // ISO 8601
}

interface CheckOtpResult {
  isValid: boolean;
}

interface ChangePasswordResult {
  success: boolean;
}
```

---

## Dashboard Types

```typescript
interface AdminDashboardOverviewResponse {
  totalVessels: number;
  totalActiveServiceRequests: number;
  totalOpenDisputes: number;
  pendingOrganizerApprovals: number;
  pendingVenueApprovals: number;
  warnings: AdminBffWarning[];
}
```

---

## Identity Types

```typescript
// Paged list wrappers
interface PagedResult<T> {
  items: T[];
  totalCount: number;
}

interface AdminUserOverviewResponse {
  organizers: PagedOrganizerProfileResult | null;
  venues: PagedVenueProfileResult | null;
  participants: PagedParticipantProfileResult | null;
  warnings: AdminBffWarning[];
}

type PagedOrganizerProfileResult = PagedResult<OrganizerProfileListItemDto>;
type PagedVenueProfileResult = PagedResult<VenueProfileListItemDto>;
type PagedParticipantProfileResult = PagedResult<ParticipantProfileListItemDto>;

// Profile list items
interface OrganizerProfileListItemDto {
  profileId: string;           // UUID
  displayName: string;
  approvalStatus: string;
  createdAt: string;           // ISO 8601
}

interface VenueProfileListItemDto {
  profileId: string;
  displayName: string;
  approvalStatus: string;
  createdAt: string;
}

interface ParticipantProfileListItemDto {
  profileId: string;
  displayName: string;
  createdAt: string;
}

// Profile detail types
interface ProfileDetailResult {
  profile: UserProfileDetailDto | null;
}

interface ProfileWithRolesResult {
  profile: UserProfileWithRolesDto | null;
}

interface OrganizerProfileResult {
  profile: OrganizerProfileDetailDto | null;
}

interface OrganizerProfileWithUserResult {
  profile: OrganizerProfileWithUserDetailDto | null;
}

interface VenueProfileResult {
  profile: VenueProfileDetailDto | null;
}

interface ParticipantProfileResult {
  profile: ParticipantProfileDetailDto | null;
}

// Generic profile shapes (map from Identity.Abstraction DTOs)
interface UserProfileDetailDto {
  profileId: string;
  userId: number;
  firstName: string;
  lastName: string;
  email: string | null;
  phoneNumber: string | null;
  approvalStatus: string;
  roleContext: string;
  createdAt: string;
}

interface UserProfileWithRolesDto extends UserProfileDetailDto {
  roles: string[];
}

interface OrganizerProfileDetailDto {
  profileId: string;
  organizationName: string;
  approvalStatus: string;
  contactEmail: string | null;
  createdAt: string;
}

interface OrganizerProfileWithUserDetailDto extends OrganizerProfileDetailDto {
  user: UserProfileDetailDto | null;
}

interface VenueProfileDetailDto {
  profileId: string;
  venueName: string;
  approvalStatus: string;
  contactEmail: string | null;
  createdAt: string;
}

interface ParticipantProfileDetailDto {
  profileId: string;
  displayName: string;
  createdAt: string;
}
```

---

## File Types

```typescript
interface AdminFileReviewOverviewResponse {
  fileMetadata: FileMetadataResult | null;
  warnings: AdminBffWarning[];
}

interface FileMetadataResult {
  file: FileMetadataDto | null;
}

interface FileMetadataDto {
  fileId: number;
  fileName: string;
  contentType: string;
  extension: string;
  sizeBytes: number;
  visibility: string;           // "Public" | "Private"
  status: string;               // lifecycle status
  ownerId: number | null;
  checksum: string | null;
  uploadedAt: string;           // ISO 8601
  deletedAt: string | null;
}

interface FileAccessUrlResult {
  accessUrl: FileAccessUrlDto | null;
}

interface FileAccessUrlDto {
  fileId: number;
  url: string;
  expiresAt: string;            // ISO 8601
}

// Requests
interface BulkGenerateReadUrlsRequest {
  fileIds: number[];
  expiresInMinutes: number;     // default: 60
}

interface CreateReadUrlRequest {
  expiresInMinutes: number;     // default: 60
}

interface UpdateVisibilityRequest {
  visibility: string;           // "Public" | "Private"
}
```

---

## Vessel Types

```typescript
interface AdminVesselOverviewResponse {
  vessels: GetAllVesselsAdminResponse | null;
  warnings: AdminBffWarning[];
}

interface AdminVesselDocumentsResponse {
  vessel: GetVesselDetailResponse | null;
  documents: GetVesselDocumentsResponse | null;
  owners: GetVesselOwnersResponse | null;
  warnings: AdminBffWarning[];
}

interface AdminVesselFormOptionsResponse {
  vesselTypes: string[];
  statusOptions: string[];
  warnings: AdminBffWarning[];
}

interface GetAllVesselsAdminResponse {
  items: VesselListItemDto[];
  totalCount: number;
}

interface VesselListItemDto {
  vesselId: number;
  name: string;
  imoNumber: string | null;
  vesselType: string;
  status: string;
  isArchived: boolean;
  ownerId: number | null;
  createdAt: string;
}

interface GetVesselDetailResponse {
  vesselId: number;
  name: string;
  imoNumber: string | null;
  mmsi: string | null;
  vesselType: string;
  flag: string | null;
  status: string;
  isArchived: boolean;
  grossTonnage: number | null;
  yearBuilt: number | null;
  createdAt: string;
}

interface GetVesselDocumentsResponse {
  items: VesselDocumentDto[];
  totalCount: number;
}

interface VesselDocumentDto {
  documentId: number;
  vesselId: number;
  documentType: string;
  fileId: number | null;
  accessUrl: string | null;
  expiresAt: string | null;
  uploadedAt: string;
}

interface GetVesselOwnersResponse {
  items: VesselOwnerDto[];
  totalCount: number;
}

interface VesselOwnerDto {
  userId: number;
  profileId: string;
  displayName: string;
  ownershipType: string;
}

// Mutation request/response
interface UpdateVesselRequest {
  name: string;
  imoNumber: string | null;
  mmsi: string | null;
  vesselType: string;
  flag: string | null;
  grossTonnage: number | null;
  yearBuilt: number | null;
}

interface ArchiveVesselRequest {
  reason: string | null;
}

interface UpdateVesselStatusRequest {
  status: string;
}

interface UpdateVesselResponse { success: boolean; }
interface ArchiveVesselResponse { success: boolean; }
interface RestoreVesselResponse { success: boolean; }
interface UpdateVesselStatusResponse { success: boolean; }
interface RemoveVesselDocumentResponse { success: boolean; }
```

---

## Service Request Types

```typescript
interface AdminServiceRequestListResponse {
  serviceRequests: GetAdminServiceRequestListResponse | null;
  warnings: AdminBffWarning[];
}

interface AdminServiceRequestOperationDetailResponse {
  serviceRequest: GetServiceRequestDetailResponse | null;
  warnings: AdminBffWarning[];
}

interface AdminServiceRequestTimelineResponse {
  serviceRequest: GetServiceRequestDetailResponse | null;
  warnings: AdminBffWarning[];
}

interface AdminServiceRequestFilterOptionsResponse {
  statusOptions: string[];
  disputeStatusOptions: string[];
  warnings: AdminBffWarning[];
}

interface GetAdminServiceRequestListResponse {
  items: ServiceRequestListItemDto[];
  totalCount: number;
}

interface ServiceRequestListItemDto {
  serviceRequestId: number;
  vesselId: number;
  status: string;
  createdAt: string;
}

interface GetServiceRequestDetailResponse {
  serviceRequestId: number;
  vesselId: number;
  status: string;
  timeline: ServiceRequestTimelineEventDto[];
  disputes: ServiceRequestDisputeDto[];
  createdAt: string;
  updatedAt: string;
}

interface ServiceRequestTimelineEventDto {
  eventType: string;
  occurredAt: string;
  note: string | null;
}

interface ServiceRequestDisputeDto {
  disputeId: number;
  status: string;
  reason: string | null;
  resolution: string | null;
  createdAt: string;
}

interface GetAdminDisputeListResponse {
  items: AdminDisputeListItemDto[];
  totalCount: number;
}

interface AdminDisputeListItemDto {
  disputeId: number;
  serviceRequestId: number;
  status: string;
  reason: string | null;
  createdAt: string;
}

// Mutation types
interface CancelServiceRequestRequest { reason: string | null; }
interface ApproveServiceRequestCompletionRequest { note: string | null; }
interface RejectServiceRequestCompletionRequest { reason: string; }
interface ChangeServiceRequestDisputeStatusRequest { status: string; }
interface ResolveServiceRequestDisputeRequest { resolution: string; }

interface CancelServiceRequestResponse { success: boolean; }
interface ApproveServiceRequestCompletionResponse { success: boolean; }
interface RejectServiceRequestCompletionResponse { success: boolean; }
interface ResolveServiceRequestDisputeResponse { success: boolean; }
```

---

## Reference Data Types

```typescript
interface LookupGroupListResult { items: LookupGroupDto[]; }
interface LookupGroupTreeResult { items: LookupGroupTreeDto[]; }
interface LookupItemListResult { items: LookupItemDto[]; }
interface CurrencyListResult { items: CurrencyDto[]; }
interface CountryListResult { items: CountryDto[]; }
interface CityListResult { items: CityDto[]; }
interface MeasurementUnitListResult { items: MeasurementUnitDto[]; }
interface SystemParameterListResult { items: SystemParameterDto[]; }

interface LookupGroupDto {
  groupId: number;
  code: string;
  displayName: string;
}

interface LookupGroupTreeDto extends LookupGroupDto {
  items: LookupItemDto[];
}

interface LookupItemDto {
  itemId: number;
  groupCode: string;
  code: string;
  displayName: string;
  sortOrder: number;
}

interface CurrencyDto {
  currencyId: number;
  code: string;        // e.g. "USD"
  symbol: string;
  displayName: string;
}

interface CountryDto {
  countryId: number;
  code: string;        // ISO 3166-1 alpha-2
  displayName: string;
}

interface CityDto {
  cityId: number;
  countryId: number;
  displayName: string;
}

interface MeasurementUnitDto {
  unitId: number;
  type: string;
  code: string;
  displayName: string;
}

interface SystemParameterDto {
  parameterKey: string;
  value: string;
  description: string | null;
}
```
