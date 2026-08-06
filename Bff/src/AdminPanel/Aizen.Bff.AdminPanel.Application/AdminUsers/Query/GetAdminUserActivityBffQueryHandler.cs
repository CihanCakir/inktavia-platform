using Aizen.Bff.AdminPanel.Application.AdminUsers.Dto;
using Aizen.Bff.AdminPanel.Application.Common.RemoteClients;
using Aizen.Bff.AdminPanel.Application.Common.Warnings;
using Aizen.Core.CQRS.Handler;
using Microsoft.Extensions.Logging;

namespace Aizen.Bff.AdminPanel.Application.AdminUsers.Query;

[DocumentationInfo("Get admin user activity BFF query handler", "Aggregates paginated activity events from Identity, Vessel and ServiceRequest modules for a given user.")]
public sealed class GetAdminUserActivityBffQueryHandler
    : AizenQueryHandler<GetAdminUserActivityBffQuery, AdminUserActivityBffResponse>
{
    private readonly IIdentityRemoteCall _identity;
    private readonly IVesselRemoteCall _vessel;
    private readonly IServiceRequestRemoteCall _serviceRequest;
    private readonly ILogger<GetAdminUserActivityBffQueryHandler> _logger;

    public GetAdminUserActivityBffQueryHandler(
        IIdentityRemoteCall identity,
        IVesselRemoteCall vessel,
        IServiceRequestRemoteCall serviceRequest,
        ILogger<GetAdminUserActivityBffQueryHandler> logger)
    {
        _identity = identity;
        _vessel = vessel;
        _serviceRequest = serviceRequest;
        _logger = logger;
    }

    public override async Task<AdminUserActivityBffResponse?> Handle(
        GetAdminUserActivityBffQuery request, CancellationToken cancellationToken)
    {
        var response = new AdminUserActivityBffResponse { Page = request.Page, PageSize = request.PageSize };
        var events = new List<AdminUserActivityEventBffDto>();

        try
        {
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "[UserActivityBff] Failed to acquire service token.");
            response.Warnings.Add(AdminBffWarning.ModuleUnavailable("Keycloak"));
            return response;
        }

        // Resolve userId from profileId — Vessel and ServiceRequest use userId, not profileId.
        long userId;
        try
        {
            var profileResult = await _identity.GetAdminUserProfileDetail(request.ProfileId);
            if (profileResult?.Header?.IsSuccess != true || profileResult.Body == null)
            {
                _logger.LogWarning("[UserActivityBff] Profile {ProfileId} not found.", request.ProfileId);
                return response;
            }
            userId = profileResult.Body.UserId;

            // Profile creation is an identity event.
            if (ShouldInclude(request.Category, "identity") && profileResult.Body.CreateDate.HasValue)
            {
                var profileCreatedAt = profileResult.Body.CreateDate.Value;
                if (PassesDateFilter(profileCreatedAt, request.DateFrom, request.DateTo))
                {
                    events.Add(BuildEvent(
                        $"profile_created_{request.ProfileId}",
                        "profile_created",
                        "identity",
                        "person_add",
                        "Profil oluşturuldu",
                        null,
                        profileCreatedAt));
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "[UserActivityBff] Identity profile call failed: {Message}", ex.Message);
            response.Warnings.Add(AdminBffWarning.ModuleUnavailable("Identity"));
            return response;
        }

        // ── Identity: login history ───────────────────────────────────────────────
        if (ShouldInclude(request.Category, "identity"))
        {
            try
            {
                var loginResult = await _identity.GetUserLoginHistory(userId, pageSize: 200);
                if (loginResult?.Header?.IsSuccess == true && loginResult.Body != null)
                {
                    foreach (var login in loginResult.Body)
                    {
                        if (!login.LoginAt.HasValue) continue;
                        if (!PassesDateFilter(login.LoginAt.Value, request.DateFrom, request.DateTo)) continue;

                        events.Add(BuildEvent(
                            $"login_{login.Id}",
                            "login",
                            "identity",
                            "login",
                            "Sisteme giriş yapıldı",
                            login.RoleContext,
                            login.LoginAt.Value,
                            new AdminUserActivityMetaBffDto
                            {
                                NewValue = login.RoleContext
                            }));
                    }
                }
                else
                {
                    _logger.LogDebug("[UserActivityBff] Login history returned no data for userId={UserId}.", userId);
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "[UserActivityBff] Login history call failed: {Message}", ex.Message);
                response.Warnings.Add(AdminBffWarning.CallFailed("Identity.LoginHistory", ex.Message));
            }
        }

        // ── Vessel: registration events ───────────────────────────────────────────
        if (ShouldInclude(request.Category, "vessel"))
        {
            try
            {
                var vesselResult = await _vessel.GetAdminVesselList(
pageIndex: 0, pageSize: 200,
                    ownerUserId: userId);

                if (vesselResult?.Header?.IsSuccess == true && vesselResult.Body?.Vessels?.Items != null)
                {
                    foreach (var v in vesselResult.Body.Vessels.Items)
                    {
                        if (!v.CreateDate.HasValue) continue;
                        if (!PassesDateFilter(v.CreateDate.Value, request.DateFrom, request.DateTo)) continue;

                        events.Add(BuildEvent(
                            $"vessel_registered_{v.Id}",
                            "vessel_registered",
                            "vessel",
                            "directions_boat",
                            $"Gemi kaydedildi: {v.Name}",
                            null,
                            v.CreateDate.Value,
                            new AdminUserActivityMetaBffDto
                            {
                                EntityId = v.Id.ToString(),
                                EntityName = v.Name,
                                NewValue = v.Status.ToString()
                            }));
                    }
                }
                else
                {
                    _logger.LogDebug("[UserActivityBff] Vessel list returned no data for userId={UserId}.", userId);
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "[UserActivityBff] Vessel registration events failed: {Message}", ex.Message);
                response.Warnings.Add(AdminBffWarning.CallFailed("Vessel", ex.Message));
            }

            // ── Vessel: status change events ──────────────────────────────────────
            try
            {
                var statusResult = await _vessel.GetVesselStatusHistoryByOwner(
ownerUserId: userId,
                    pageSize: 200);

                if (statusResult?.Header?.IsSuccess == true && statusResult.Body?.Items != null)
                {
                    foreach (var h in statusResult.Body.Items)
                    {
                        if (!PassesDateFilter(h.ChangedAt, request.DateFrom, request.DateTo)) continue;

                        events.Add(BuildEvent(
                            $"vessel_status_{h.Id}",
                            "vessel_status_changed",
                            "vessel",
                            "sync_alt",
                            $"Gemi durumu değişti: {h.VesselName}",
                            h.Reason,
                            h.ChangedAt,
                            new AdminUserActivityMetaBffDto
                            {
                                EntityId = h.VesselId.ToString(),
                                EntityName = h.VesselName,
                                PreviousValue = h.FromStatus?.ToString(),
                                NewValue = h.ToStatus.ToString()
                            }));
                    }
                }
                else
                {
                    _logger.LogDebug("[UserActivityBff] Vessel status history returned no data for userId={UserId}.", userId);
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "[UserActivityBff] Vessel status history events failed: {Message}", ex.Message);
                response.Warnings.Add(AdminBffWarning.CallFailed("Vessel.StatusHistory", ex.Message));
            }
        }

        // ── Service request events ────────────────────────────────────────────────
        if (ShouldInclude(request.Category, "service"))
        {
            try
            {
                var srResult = await _serviceRequest.GetAdminServiceRequestList(
ownerUserId: userId,
                    pageIndex: 0,
                    pageSize: 200);

                if (srResult?.Header?.IsSuccess == true && srResult.Body?.Items != null)
                {
                    foreach (var sr in srResult.Body.Items)
                    {
                        if (!PassesDateFilter(sr.CreatedAt, request.DateFrom, request.DateTo)) continue;

                        events.Add(BuildEvent(
                            $"service_request_created_{sr.Id}",
                            "service_request_created",
                            "service",
                            "engineering",
                            $"Servis talebi oluşturuldu: {sr.Title}",
                            sr.ServiceCategoryCode,
                            sr.CreatedAt,
                            new AdminUserActivityMetaBffDto
                            {
                                EntityId = sr.Id.ToString(),
                                EntityName = sr.Title,
                                NewValue = sr.Status.ToString()
                            }));
                    }
                }
                else
                {
                    _logger.LogDebug("[UserActivityBff] ServiceRequest returned no data for userId={UserId}.", userId);
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "[UserActivityBff] ServiceRequest events failed: {Message}", ex.Message);
                response.Warnings.Add(AdminBffWarning.CallFailed("ServiceRequest", ex.Message));
            }
        }

        // ── Sort + paginate ───────────────────────────────────────────────────────
        var sorted = events.OrderByDescending(e => e.Timestamp).ToList();

        response.Total = sorted.Count;
        response.Items = sorted
            .Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize)
            .ToList();

        return response;
    }

    private static bool ShouldInclude(string? filter, string category)
        => string.IsNullOrEmpty(filter) || filter == category;

    private static bool PassesDateFilter(DateTime timestamp, DateOnly? from, DateOnly? to)
    {
        if (from.HasValue && DateOnly.FromDateTime(timestamp) < from.Value) return false;
        if (to.HasValue && DateOnly.FromDateTime(timestamp) > to.Value) return false;
        return true;
    }

    private static AdminUserActivityEventBffDto BuildEvent(
        string id,
        string type,
        string category,
        string icon,
        string label,
        string? description,
        DateTime timestamp,
        AdminUserActivityMetaBffDto? meta = null)
    {
        return new AdminUserActivityEventBffDto
        {
            Id = id,
            Type = type,
            Category = category,
            Icon = icon,
            Label = label,
            Description = description,
            Timestamp = DateTime.SpecifyKind(timestamp, DateTimeKind.Utc).ToString("o"),
            RelativeTime = AdminUserBffHelpers.ComputeRelativeTime(timestamp),
            Meta = meta,
        };
    }
}
