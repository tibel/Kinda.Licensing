using System;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace Kinda.Licensing;

/// <summary>Reads the HTTP <c>Date</c> header from a caller-selected HTTPS endpoint.</summary>
public static class HttpDateTimeProvider
{
    /// <summary>Gets the endpoint's current UTC time from its HTTP <c>Date</c> response header.</summary>
    /// <param name="httpClient">The caller-owned HTTP client.</param>
    /// <param name="endpoint">The trusted absolute HTTPS endpoint to query.</param>
    /// <param name="cancellationToken">A token that can cancel the request.</param>
    /// <returns>The UTC time reported by the endpoint.</returns>
    /// <exception cref="ArgumentNullException">An argument is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentException"><paramref name="endpoint"/> is not an absolute HTTPS URI.</exception>
    /// <exception cref="HttpRequestException">The request fails or the endpoint returns a non-success status.</exception>
    /// <exception cref="InvalidOperationException">The response is redirected to another authority or has no <c>Date</c> header.</exception>
    /// <exception cref="OperationCanceledException">The request is canceled or times out.</exception>
    public static async Task<DateTimeOffset> GetUtcNowAsync(
        HttpClient httpClient,
        Uri endpoint,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(httpClient);
        ArgumentNullException.ThrowIfNull(endpoint);
        if (!endpoint.IsAbsoluteUri || endpoint.Scheme != Uri.UriSchemeHttps)
            throw new ArgumentException("The time endpoint must be an absolute HTTPS URI.", nameof(endpoint));

        using var request = new HttpRequestMessage(HttpMethod.Get, endpoint);
        using var response = await httpClient.SendAsync(
            request,
            HttpCompletionOption.ResponseHeadersRead,
            cancellationToken).ConfigureAwait(false);
        response.EnsureSuccessStatusCode();

        var responseUri = response.RequestMessage?.RequestUri
            ?? throw new InvalidOperationException("The HTTP response has no request URI.");
        if (responseUri.Scheme != Uri.UriSchemeHttps ||
            !string.Equals(responseUri.IdnHost, endpoint.IdnHost, StringComparison.OrdinalIgnoreCase) ||
            responseUri.Port != endpoint.Port)
            throw new InvalidOperationException("The time endpoint redirected to another authority.");

        var date = response.Headers.Date
            ?? throw new InvalidOperationException("The HTTP response has no Date header.");
        return date.ToUniversalTime();
    }
}
