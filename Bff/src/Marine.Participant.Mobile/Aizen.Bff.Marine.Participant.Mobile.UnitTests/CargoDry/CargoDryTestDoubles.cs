using System.Reflection;
using Aizen.Bff.Marine.Participant.Mobile.Application.Common.RemoteClients;
using Aizen.Bff.Marine.Participant.Mobile.Application.Common.Services;
using Aizen.Bff.Marine.Participant.Mobile.Application.Contracts.CargoDry;
using Aizen.Core.Infrastructure.Api;
using Aizen.Modules.CargoDry.Abstraction.Dto;
using Aizen.Modules.Vessel.Abstraction.Dto.Vessel;
using Aizen.Modules.Vessel.Abstraction.Request.Document;
using Aizen.Modules.Vessel.Abstraction.Request.Engine;
using Aizen.Modules.Vessel.Abstraction.Request.Media;
using Aizen.Modules.Vessel.Abstraction.Request.Specification;
using Aizen.Modules.Vessel.Abstraction.Request.Vessel;
using Aizen.Modules.Vessel.Abstraction.Response.Document;
using Aizen.Modules.Vessel.Abstraction.Response.Engine;
using Aizen.Modules.Vessel.Abstraction.Response.Media;
using Aizen.Modules.Vessel.Abstraction.Response.Specification;
using Aizen.Modules.Vessel.Abstraction.Response.Vessel;
using MiniUow.Paging;

namespace Aizen.Bff.Marine.Participant.Mobile.UnitTests.CargoDry;

/// <summary>Resolver stub — returns a fixed profile id (or none) so the handler's identity gate is deterministic.</summary>
internal sealed class FakeParticipantProfileResolver : IParticipantProfileResolver
{
    private readonly long? _profileId;
    public FakeParticipantProfileResolver(long? profileId) => _profileId = profileId;

    public Task<ParticipantProfileResolution> ResolveAsync(CancellationToken cancellationToken = default)
        => Task.FromResult(new ParticipantProfileResolution(_profileId, null, "test"));
}

/// <summary>CargoDry remote stub. Captures the activate body (to prove no user id is forwarded), returns canned
/// responses, or throws a supplied exception (e.g. a Refit 401) to exercise the clean-error path.</summary>
internal sealed class FakeCargoDryRemoteCall : ICargoDryRemoteCall
{
    public CargoDryKitValidationDto? ValidationResponse { get; set; }
    public CargoDryKitDto? ActivateResponse { get; set; }
    public CargoDryMyKitsRemoteResponse? MyKitsResponse { get; set; }
    public Exception? ThrowOnActivate { get; set; }
    public Exception? ThrowOnValidate { get; set; }
    public Exception? ThrowOnGetMyKits { get; set; }

    public int ActivateCallCount { get; private set; }
    public ActivateKitRemoteRequest? LastActivateBody { get; private set; }

    public Task<CargoDryKitValidationDto> ValidateKit(ValidateKitRemoteRequest request)
    {
        if (ThrowOnValidate is not null) throw ThrowOnValidate;
        return Task.FromResult(ValidationResponse!);
    }

    public Task<CargoDryKitDto> ActivateKit(ActivateKitRemoteRequest request)
    {
        ActivateCallCount++;
        LastActivateBody = request;
        if (ThrowOnActivate is not null) throw ThrowOnActivate;
        return Task.FromResult(ActivateResponse!);
    }

    public Task<CargoDryMyKitsRemoteResponse> GetMyKits()
    {
        if (ThrowOnGetMyKits is not null) throw ThrowOnGetMyKits;
        return Task.FromResult(MyKitsResponse!);
    }
}

/// <summary>Vessel remote stub. Only GetUserVessels is functional (returns a configurable owned set); the rest of the
/// (large) IVesselRemoteCall surface is unused by the CargoDry handlers and throws if ever called.</summary>
internal sealed class FakeVesselRemoteCall : IVesselRemoteCall
{
    private readonly long[] _ownedVesselIds;
    public FakeVesselRemoteCall(params long[] ownedVesselIds) => _ownedVesselIds = ownedVesselIds;

    public Task<AizenApiResponse<GetUserVesselsResponse>> GetUserVessels(int pageIndex = 0, int pageSize = 100)
    {
        var items = _ownedVesselIds.Select(id => new VesselListItemDto { Id = id, Name = $"Vessel {id}" }).ToList();
        var page = PaginateFactory.Create(items);
        var body = new GetUserVesselsResponse(page);
        return Task.FromResult(new AizenApiResponse<GetUserVesselsResponse>(AizenResponseHeader.Success(), body));
    }

    public Task<AizenApiResponse<GetVesselDetailResponse>> GetVesselDetail(long vesselId) => throw new NotImplementedException();
    public Task<AizenApiResponse<GetVesselByCodeResponse>> GetVesselByCode(string vesselCode) => throw new NotImplementedException();
    public Task<AizenApiResponse<CreateVesselResponse>> CreateVessel(CreateVesselRequest request) => throw new NotImplementedException();
    public Task<AizenApiResponse<UpsertVesselSpecificationResponse>> UpsertSpecification(long vesselId, UpsertVesselSpecificationRequest request) => throw new NotImplementedException();
    public Task<AizenApiResponse<AddVesselEngineResponse>> AddEngine(long vesselId, AddVesselEngineRequest request) => throw new NotImplementedException();
    public Task<AizenApiResponse<UpdateVesselResponse>> UpdateVessel(long vesselId, UpdateVesselRequest request) => throw new NotImplementedException();
    public Task<AizenApiResponse<UpdateVesselEngineResponse>> UpdateEngine(long vesselId, long engineId, UpdateVesselEngineRequest request) => throw new NotImplementedException();
    public Task<AizenApiResponse<ArchiveVesselResponse>> ArchiveVessel(long vesselId, ArchiveVesselRequest request) => throw new NotImplementedException();
    public Task<AizenApiResponse<RestoreVesselResponse>> RestoreVessel(long vesselId) => throw new NotImplementedException();
    public Task<AizenApiResponse<UpdateVesselStatusResponse>> UpdateStatus(long vesselId, UpdateVesselStatusRequest request) => throw new NotImplementedException();
    public Task<AizenApiResponse<GetVesselDocumentsResponse>> GetVesselDocuments(long vesselId, int pageIndex = 0, int pageSize = 20, bool includeAccessUrls = false, int accessUrlExpiresInMinutes = 15) => throw new NotImplementedException();
    public Task<AizenApiResponse<AddVesselDocumentResponse>> AddVesselDocument(long vesselId, AddVesselDocumentRequest request) => throw new NotImplementedException();
    public Task<AizenApiResponse<RemoveVesselDocumentResponse>> RemoveVesselDocument(long vesselId, long documentId) => throw new NotImplementedException();
    public Task<AizenApiResponse<GetVesselMediaResponse>> GetVesselMedia(long vesselId, int pageIndex = 0, int pageSize = 20, bool includeAccessUrls = false, int accessUrlExpiresInMinutes = 15) => throw new NotImplementedException();
    public Task<AizenApiResponse<AddVesselMediaResponse>> AddVesselMedia(long vesselId, AddVesselMediaRequest request) => throw new NotImplementedException();
    public Task<AizenApiResponse<RemoveVesselMediaResponse>> RemoveVesselMedia(long vesselId, long mediaId) => throw new NotImplementedException();
    public Task<AizenApiResponse<SetCoverVesselMediaResponse>> SetCoverVesselMedia(long vesselId, long mediaId) => throw new NotImplementedException();
}

/// <summary>MiniUow's <c>Paginate&lt;T&gt;</c> has only an internal parameterless constructor, so build one via
/// reflection (the same technique the Refit <c>PaginateJsonConverter</c> uses) and set Items.</summary>
internal static class PaginateFactory
{
    public static Paginate<T> Create<T>(IReadOnlyList<T> items)
    {
        var ctor = typeof(Paginate<T>).GetConstructor(
            BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public,
            binder: null, types: Type.EmptyTypes, modifiers: null)
            ?? throw new InvalidOperationException("No parameterless ctor on Paginate<T>.");
        var page = (Paginate<T>)ctor.Invoke(null);
        page.Items = items;
        page.Count = items.Count;
        page.Size = items.Count;
        page.Pages = 1;
        return page;
    }
}
