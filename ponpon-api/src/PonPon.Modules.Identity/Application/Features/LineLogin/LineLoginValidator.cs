using FluentValidation;

namespace PonPon.Modules.Identity.Application.Features.LineLogin;

public sealed class LineLoginValidator : AbstractValidator<LineLoginCommand>
{
    public LineLoginValidator() => RuleFor(x => x.IdToken).NotEmpty();
}
