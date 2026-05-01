using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;

using App.Desktop.Boundaries;

namespace App.Desktop.Services.GroupTree;

public sealed class HttpDesktopGroupTreeClient : IDesktopGroupTreeClient
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);
    private const string SafeFallbackDisplayName = "Группа без безопасного имени";

    private readonly HttpClient _httpClient;
    private readonly IDesktopControlPlaneAccessTokenProvider _accessTokenProvider;
    private readonly Uri _nodesEndpoint;

    public HttpDesktopGroupTreeClient(
        HttpClient httpClient,
        IDesktopControlPlaneAccessTokenProvider accessTokenProvider)
        : this(httpClient, accessTokenProvider, new Uri("/api/group-tree/nodes", UriKind.Relative))
    {
    }

    public HttpDesktopGroupTreeClient(
        HttpClient httpClient,
        IDesktopControlPlaneAccessTokenProvider accessTokenProvider,
        Uri nodesEndpoint)
    {
        ArgumentNullException.ThrowIfNull(httpClient);
        ArgumentNullException.ThrowIfNull(accessTokenProvider);
        ArgumentNullException.ThrowIfNull(nodesEndpoint);

        _httpClient = httpClient;
        _accessTokenProvider = accessTokenProvider;
        _nodesEndpoint = nodesEndpoint;
    }

    public async ValueTask<DesktopGroupTreeLoadResult> GetGroupTreeAsync(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        try
        {
            DesktopControlPlaneAccessTokenSnapshot accessToken =
                await _accessTokenProvider.GetCurrentAccessTokenAsync(cancellationToken);

            if (!accessToken.HasAccessToken)
            {
                return DesktopGroupTreeLoadResult.Unavailable(DesktopGroupTreeText.LiveUnauthorizedMessage);
            }

            using var request = new HttpRequestMessage(HttpMethod.Get, _nodesEndpoint);
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken.AccessToken);

            using HttpResponseMessage response = await _httpClient.SendAsync(
                request,
                HttpCompletionOption.ResponseHeadersRead,
                cancellationToken);

            if (response.StatusCode is HttpStatusCode.Unauthorized or HttpStatusCode.Forbidden)
            {
                return DesktopGroupTreeLoadResult.Unavailable(DesktopGroupTreeText.LiveUnauthorizedMessage);
            }

            if (!response.IsSuccessStatusCode)
            {
                return DesktopGroupTreeLoadResult.Failed(DesktopGroupTreeText.LiveFailedMessage);
            }

            List<GroupNodeFlatPayload>? payload =
                await response.Content.ReadFromJsonAsync<List<GroupNodeFlatPayload>>(JsonOptions, cancellationToken);

            IReadOnlyList<DesktopGroupTreeNode>? nodes = TryMapNodes(payload);
            if (nodes is null)
            {
                return DesktopGroupTreeLoadResult.Failed(DesktopGroupTreeText.LiveMalformedMessage);
            }

            return DesktopGroupTreeLoadResult.Loaded(nodes, DesktopGroupTreeText.LiveLoadedMessage);
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            return DesktopGroupTreeLoadResult.Unavailable(DesktopGroupTreeText.LiveUnavailableMessage);
        }
        catch (HttpRequestException)
        {
            return DesktopGroupTreeLoadResult.Unavailable(DesktopGroupTreeText.LiveUnavailableMessage);
        }
        catch (InvalidOperationException)
        {
            return DesktopGroupTreeLoadResult.Unavailable(DesktopGroupTreeText.LiveUnavailableMessage);
        }
        catch (JsonException)
        {
            return DesktopGroupTreeLoadResult.Failed(DesktopGroupTreeText.LiveMalformedMessage);
        }
        catch (NotSupportedException)
        {
            return DesktopGroupTreeLoadResult.Failed(DesktopGroupTreeText.LiveMalformedMessage);
        }
    }

    private static List<DesktopGroupTreeNode>? TryMapNodes(
        IReadOnlyCollection<GroupNodeFlatPayload>? payload)
    {
        if (payload is null)
        {
            return null;
        }

        var parentNodeIds = payload
            .Where(item => item.ParentGroupNodeId.HasValue)
            .Select(item => item.ParentGroupNodeId!.Value)
            .ToHashSet();

        var nodes = new List<DesktopGroupTreeNode>(payload.Count);

        foreach (GroupNodeFlatPayload item in payload
            .OrderBy(item => item.Depth ?? int.MaxValue)
            .ThenBy(item => item.Name, StringComparer.Ordinal))
        {
            if (item.GroupNodeId == Guid.Empty
                || item.Depth is null or < 0 or > 64
                || item.IsActive is null)
            {
                return null;
            }

            string displayName = TryCreateSafeDisplayName(item.Name, out string safeDisplayName)
                ? safeDisplayName
                : SafeFallbackDisplayName;

            bool canSelect = item.IsActive.Value && !parentNodeIds.Contains(item.GroupNodeId);

            nodes.Add(new DesktopGroupTreeNode(
                item.GroupNodeId.ToString("D"),
                displayName,
                item.Depth.Value,
                canSelect));
        }

        return nodes;
    }

    private static bool TryCreateSafeDisplayName(string? value, out string safeValue)
    {
        safeValue = string.Empty;

        if (string.IsNullOrWhiteSpace(value))
        {
            return false;
        }

        string trimmed = value.Trim();
        if (trimmed.Length > 80)
        {
            return false;
        }

        foreach (char character in trimmed)
        {
            if (char.IsControl(character))
            {
                return false;
            }
        }

        string lower = trimmed.ToLowerInvariant();
        string[] blockedFragments =
        [
            "authorization",
            "bearer",
            "password",
            "token",
            "sessionid",
            "session_id",
            "access_token",
            "refresh_token",
            "accesstoken",
            "refreshtoken"
        ];

        foreach (string blockedFragment in blockedFragments)
        {
            if (lower.Contains(blockedFragment, StringComparison.Ordinal))
            {
                return false;
            }
        }

        safeValue = trimmed;
        return true;
    }

    private sealed class GroupNodeFlatPayload
    {
        public Guid GroupNodeId { get; init; }

        public Guid? ParentGroupNodeId { get; init; }

        public string? Code { get; init; }

        public string? Name { get; init; }

        public int? Depth { get; init; }

        public bool? IsActive { get; init; }
    }
}