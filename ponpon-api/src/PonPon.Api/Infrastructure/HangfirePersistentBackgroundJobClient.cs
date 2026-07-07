using System.Linq.Expressions;
using Hangfire;
using PonPon.Shared.Application.Abstractions;

namespace PonPon.Api.Infrastructure;

public sealed class HangfirePersistentBackgroundJobClient : IPersistentBackgroundJobClient
{
    private readonly IBackgroundJobClient _backgroundJobs;

    public HangfirePersistentBackgroundJobClient(IBackgroundJobClient backgroundJobs)
    {
        _backgroundJobs = backgroundJobs;
    }

    public string Enqueue<TJob>(Expression<Func<TJob, Task>> methodCall)
        => _backgroundJobs.Enqueue(methodCall);
}
