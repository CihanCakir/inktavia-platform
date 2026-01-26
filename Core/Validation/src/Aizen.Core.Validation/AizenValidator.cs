using FluentValidation;
using Aizen.Core.Validation.Abstraction;

namespace Aizen.Core.Validation;

public class AizenValidator<T> : AbstractValidator<T>, IAizenValidator<T>
{
}