using FluentValidation;

namespace PonPon.Modules.Identity.Application.Features.RegisterFirstAdmin;

public sealed class RegisterFirstAdminValidator : AbstractValidator<RegisterFirstAdminCommand>
{
    public RegisterFirstAdminValidator()
    {
        RuleFor(x => x.Email).NotEmpty().EmailAddress();
        RuleFor(x => x.Password).NotEmpty().MinimumLength(8);
        RuleFor(x => x.DisplayName).MaximumLength(128);
    }
}
