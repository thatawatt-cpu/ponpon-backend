using PonPon.Shared.Domain;

namespace PonPon.Modules.Identity.Domain.Users;

public sealed class Role : Entity
{
    private Role()
    {
        Name = string.Empty;
    }

    public Role(string name) => Name = name;

    public string Name { get; private set; }
}
