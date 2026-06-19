using System.Net.Http.Json;
using Microsoft.Extensions.Options;
using PonPon.Modules.Identity.Application.Abstractions;
using PonPon.Modules.Identity.Domain.Customers;
using PonPon.Shared.Application.Exceptions;

namespace PonPon.Modules.Identity.Infrastructure.Line;

public sealed class LineAuthService : ILineAuthService
{
    private readonly HttpClient _httpClient;
    private readonly LineOptions _options;

    public LineAuthService(HttpClient httpClient, IOptions<LineOptions> options)
    {
        _httpClient = httpClient;
        _options = options.Value;
    }

    public async Task<LineProfile> VerifyIdTokenAsync(string idToken, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(_options.ChannelId))
        {
            throw new UnauthorizedException("LINE ChannelId is not configured.");
        }

        using var content = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["id_token"] = idToken,
            ["client_id"] = _options.ChannelId
        });

        var response = await _httpClient.PostAsync("https://api.line.me/oauth2/v2.1/verify", content, cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            var error = await response.Content.ReadFromJsonAsync<LineErrorResponse>(cancellationToken: cancellationToken);
            var message = error?.Error switch
            {
                "invalid_request" when error.ErrorDescription.Contains("expired") => "LINE session has expired, please login again.",
                "invalid_request" => "Invalid LINE idToken.",
                "invalid_client" => "LINE channel configuration is invalid.",
                _ => "LINE idToken verification failed."
            };
            throw new UnauthorizedException(message);
        }

        var body = await response.Content.ReadFromJsonAsync<LineTokenVerifyResponse>(cancellationToken: cancellationToken) ?? throw new UnauthorizedException("LINE verification returned an empty response.");
        if (string.IsNullOrWhiteSpace(body.LineUserId))
        {
            throw new UnauthorizedException("LINE verification response did not include a user id.");
        }

        return new LineProfile(body.LineUserId, body.DisplayName, body.PictureUrl, body.Email);
    }
}
