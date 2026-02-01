using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Aizen.Core.Validation;
using FluentValidation;

namespace Aizen.Modules.InktaviaStore.Application.Identity
{
    public class ApproveOrganizerProfileCommandValidator : AizenValidator<ApproveOrganizerProfileCommand>
    {
        public ApproveOrganizerProfileCommandValidator()
        {
            RuleFor(x => x.UserId).GreaterThan(0);
            RuleFor(x => x.ProfileId).GreaterThan(0);
        }
    }
}
