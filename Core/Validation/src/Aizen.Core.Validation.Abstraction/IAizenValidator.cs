using FluentValidation;

namespace Aizen.Core.Validation.Abstraction;

public interface IAizenValidator : IValidator
{
}

public interface IAizenValidator<in T> : IAizenValidator, IValidator<T>
{
}