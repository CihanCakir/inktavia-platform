using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Aizen.Core.Validation;
using FluentValidation;

namespace Aizen.Modules.InktaviaStore.Application.Identity.Command.RegisterOrganizer
{

    public sealed class RegisterOrganizerCommandValidator : AizenValidator<RegisterOrganizerCommand>
    {
        public RegisterOrganizerCommandValidator()
        {
            RuleFor(x => x.Email).NotEmpty().EmailAddress().MaximumLength(256);
            RuleFor(x => x.Password).NotEmpty().MinimumLength(6).MaximumLength(100);
            RuleFor(x => x.OwnerFirstName).NotEmpty().MaximumLength(80);
            RuleFor(x => x.OwnerLastName).NotEmpty().MaximumLength(80);
            RuleFor(x => x.KvkkAccepted).Equal(true);
            RuleFor(x => x.DeviceId).MaximumLength(200).When(x => !string.IsNullOrWhiteSpace(x.DeviceId));
            RuleFor(x => x.NotificationToken).MaximumLength(1024).When(x => !string.IsNullOrWhiteSpace(x.NotificationToken));
        }
    }
}