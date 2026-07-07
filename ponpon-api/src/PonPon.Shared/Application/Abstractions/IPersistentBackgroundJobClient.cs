using System.Linq.Expressions;

namespace PonPon.Shared.Application.Abstractions;

public interface IPersistentBackgroundJobClient
{
    string Enqueue<TJob>(Expression<Func<TJob, Task>> methodCall);
}
