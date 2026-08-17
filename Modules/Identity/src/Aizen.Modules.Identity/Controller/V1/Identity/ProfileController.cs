using Aizen.Core.CQRS.Abstraction;
using Aizen.Core.Infrastructure.Api;
using Aizen.Modules.Identity.Abstraction.Request;
using Aizen.Modules.InktaviaStore.Application.Identity;
using Aizen.Modules.InktaviaStore.Application.Identity.Command.Organizer.UpdateOrganizerProfile;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Aizen.Modules.InktaviaStore.Controller.V1.Identity
{
    [ApiController]
    [Route("api/v1/identity")]
    [Tags("Identity - Profile")]
    [Authorize]
    public sealed class ProfileController : AizenWebApiController
    {
        private readonly IAizenCQRSProcessor _sender;

        public ProfileController(
            IHttpContextAccessor httpContextAccessor,
            IAizenCQRSProcessor cqrsProcessor) : base(httpContextAccessor)
        {
            _sender = cqrsProcessor;
        }

        // PUT /api/v1/identity/participant/profile
        [HttpPut("participant/profile")]
        [ProducesResponseType(typeof(ProfileUpdateResult), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
        public async Task<AizenApiResponse<ProfileUpdateResult>> UpdateParticipantProfile(
            [FromBody] UpdateParticipantProfileRequest req,
            CancellationToken ct)
        {
            var result = await _sender.ProcessAsync(
                new UpdateParticipantProfileCommand(
                    firstName: req.FirstName,
                    lastName: req.LastName,
                    gender: req.Gender,
                    birthDate: req.BirthDate,
                    bio: req.Bio,
                    profilePhotoUrl: req.ProfilePhotoUrl,
                    nationalityId: req.NationalityId,
                    allowPush: req.AllowPush,
                    allowSms: req.AllowSms,
                    allowEmail: req.AllowEmail
                ), ct);

            return SetResponse(result);
        }

        // PUT /api/v1/identity/organizers/profile
        [HttpPut("organizers/profile")]
        [ProducesResponseType(typeof(ProfileUpdateResult), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
        public async Task<AizenApiResponse<ProfileUpdateResult>> UpdateOrganizerProfile(
            [FromBody] UpdateOrganizerProfileRequest req,
            CancellationToken ct)
        {
            var result = await _sender.ProcessAsync(
                new UpdateOrganizerProfileCommand(
                    ownerFirstName: req.OwnerFirstName,
                    ownerLastName: req.OwnerLastName,
                    bio: req.Bio,
                    profilePhotoUrl: req.ProfilePhotoUrl,
                    nationalityId: req.NationalityId,
                    taxpayerType: req.TaxpayerType,
                    allowPush: req.AllowPush,
                    allowSms: req.AllowSms,
                    allowEmail: req.AllowEmail
                ), ct);

            return SetResponse(result);
        }

        // PUT /api/v1/identity/venues/profile
        [HttpPut("venues/profile")]
        [ProducesResponseType(typeof(ProfileUpdateResult), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
        public async Task<AizenApiResponse<ProfileUpdateResult>> UpdateVenueProfile(
            [FromBody] UpdateVenueProfileRequest req,
            CancellationToken ct)
        {
            var result = await _sender.ProcessAsync(
                new UpdateVenueProfileCommand(
                    ownerFirstName: req.OwnerFirstName,
                    ownerLastName: req.OwnerLastName,
                    bio: req.Bio,
                    profilePhotoUrl: req.ProfilePhotoUrl,
                    nationalityId: req.NationalityId,
                    taxpayerType: req.TaxpayerType,
                    allowPush: req.AllowPush,
                    allowSms: req.AllowSms,
                    allowEmail: req.AllowEmail
                ), ct);

            return SetResponse(result);
        }
    }
}
