using LubeLogMCP.Models;
using System.Net;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace LubeLogMCP.Helper
{
    /// <summary>
    /// Outcome of a call to the LubeLogger API.
    /// </summary>
    public class ApiResult
    {
        public bool Success { get; init; }
        public int StatusCode { get; init; }
        public string Body { get; init; } = string.Empty;
        /// <summary>
        /// Set when the request never got a response (network error, bad instance URL, etc.).
        /// </summary>
        public string? TransportError { get; init; }

        /// <summary>
        /// Text handed back to the MCP client: the raw body on success, a readable error otherwise.
        /// </summary>
        public string ToToolResult()
        {
            if (Success)
            {
                return Body;
            }
            if (TransportError != null)
            {
                return $"Error: could not reach LubeLogger: {TransportError}";
            }
            string reason = ((HttpStatusCode)StatusCode).ToString();
            string detail = ExtractMessage(Body);
            if (StatusCode >= 200 && StatusCode < 300)
            {
                // HTTP success but {"success":false}: a validation failure.
                return $"Error: LubeLogger rejected the request: {(string.IsNullOrWhiteSpace(detail) ? "no reason given" : detail)}";
            }
            string hint = StatusCode switch
            {
                401 => " The API key is missing or invalid, or lacks the permission this operation needs (writes need Edit permission, deletes need Delete permission).",
                403 => " The API key does not have the required permission for this vehicle or operation.",
                _ => string.Empty
            };
            return string.IsNullOrWhiteSpace(detail)
                ? $"Error: LubeLogger returned {StatusCode} {reason}.{hint}"
                : $"Error: LubeLogger returned {StatusCode} {reason}: {detail}{(string.IsNullOrEmpty(hint) ? string.Empty : $" ({hint.Trim()})")}";
        }

        /// <summary>
        /// LubeLogger errors are usually an OperationResponse ({"success":false,"message":"..."}).
        /// Pull the message out when possible; otherwise return a trimmed body.
        /// </summary>
        private static string ExtractMessage(string body)
        {
            if (string.IsNullOrWhiteSpace(body))
            {
                return string.Empty;
            }
            try
            {
                if (JsonNode.Parse(body) is JsonObject obj && obj["message"] is JsonValue message)
                {
                    return message.ToString();
                }
            }
            catch (JsonException)
            {
                // not JSON (e.g. an HTML error page); fall through
            }
            body = body.Trim();
            return body.Length > 500 ? body[..500] + "..." : body;
        }
    }

    /// <summary>
    /// Shared HTTP plumbing for talking to LubeLogger: auth passthrough, JSON bodies, error handling.
    /// </summary>
    public class LubeLoggerApiClient
    {
        private readonly string _instance;
        private readonly string _username;
        private readonly string _password;
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IHttpContextAccessor _httpContextAccessor;

        public LubeLoggerApiClient(IConfiguration config, IHttpClientFactory httpClientFactory, IHttpContextAccessor httpContextAccessor)
        {
            _instance = (config["LUBELOG_INSTANCE"] ?? string.Empty).TrimEnd('/');
            _username = config["LUBELOG_USER"] ?? string.Empty;
            _password = config["LUBELOG_PASS"] ?? string.Empty;
            _httpClientFactory = httpClientFactory;
            _httpContextAccessor = httpContextAccessor;
        }

        public string Instance => _instance;

        /// <summary>
        /// Describes where the credentials for outgoing requests come from.
        /// </summary>
        public string DescribeAuthSource()
        {
            var headers = _httpContextAccessor.HttpContext?.Request.Headers;
            if (headers?.ContainsKey("Authorization") ?? false)
            {
                return "using client-supplied Authorization header";
            }
            if (headers?.ContainsKey("x-api-key") ?? false)
            {
                return "using client-supplied API key (x-api-key header)";
            }
            if (_httpContextAccessor.HttpContext?.Request.Query.ContainsKey("apiKey") ?? false)
            {
                return "using client-supplied API key (apiKey query parameter)";
            }
            if (!string.IsNullOrWhiteSpace(_username) && !string.IsNullOrWhiteSpace(_password))
            {
                return "using LUBELOG_USER/LUBELOG_PASS configured on the MCP server";
            }
            return "Auth Not Configured: no client credentials and no LUBELOG_USER/LUBELOG_PASS";
        }

        public void AddAuthHeaders(HttpRequestMessage request)
        {
            //check if we have headers
            if (_httpContextAccessor.HttpContext?.Request.Headers.TryGetValue("Authorization", out var authHeader) ?? false)
            {
                request.Headers.Add("Authorization", authHeader.FirstOrDefault());
            }
            else if (_httpContextAccessor.HttpContext?.Request.Headers.TryGetValue("x-api-key", out var apiKeyHeader) ?? false)
            {
                request.Headers.Add("x-api-key", apiKeyHeader.FirstOrDefault());
            }
            else if (_httpContextAccessor.HttpContext?.Request.Query.TryGetValue("apiKey", out var apiKey) ?? false)
            {
                request.Headers.Add("x-api-key", apiKey.FirstOrDefault());
            }
            else if (!string.IsNullOrWhiteSpace(_username) && !string.IsNullOrWhiteSpace(_password))
            {
                var authenticationString = $"{_username}:{_password}";
                var base64EncodedAuthenticationString = Convert.ToBase64String(Encoding.UTF8.GetBytes(authenticationString));
                request.Headers.Add("Authorization", "Basic " + base64EncodedAuthenticationString);
            }
        }

        /// <summary>
        /// Sends a request to LubeLogger. <paramref name="path"/> is relative to the instance, e.g. "/api/vehicles".
        /// Reads send the culture-invariant header so dates come back as yyyy-MM-dd and numbers as JSON numbers.
        /// </summary>
        public async Task<ApiResult> SendAsync(HttpMethod method, string path, HttpContent? content = null, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(_instance))
            {
                return new ApiResult { TransportError = "LUBELOG_INSTANCE is not configured on the MCP server." };
            }
            using var request = new HttpRequestMessage(method, _instance + path) { Content = content };
            request.Headers.Add("culture-invariant", "true");
            AddAuthHeaders(request);
            try
            {
                var httpClient = _httpClientFactory.CreateClient();
                using var response = await httpClient.SendAsync(request, cancellationToken);
                string body = await response.Content.ReadAsStringAsync(cancellationToken);
                return new ApiResult
                {
                    Success = response.IsSuccessStatusCode && !IsFailedOperation(body),
                    StatusCode = (int)response.StatusCode,
                    Body = body
                };
            }
            catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException or InvalidOperationException or UriFormatException)
            {
                return new ApiResult { TransportError = ex.Message };
            }
        }

        public Task<ApiResult> GetAsync(string path, CancellationToken cancellationToken = default)
            => SendAsync(HttpMethod.Get, path, null, cancellationToken);

        public Task<ApiResult> SendJsonAsync(HttpMethod method, string path, JsonNode body, CancellationToken cancellationToken = default)
            => SendAsync(method, path, new StringContent(body.ToJsonString(), Encoding.UTF8, "application/json"), cancellationToken);

        /// <summary>
        /// Some LubeLogger validation failures come back as HTTP 200 with {"success":false,...}
        /// (e.g. reminder metric validation). Treat those as failures too.
        /// </summary>
        private static bool IsFailedOperation(string body)
        {
            if (string.IsNullOrWhiteSpace(body) || body[0] != '{')
            {
                return false;
            }
            try
            {
                return JsonNode.Parse(body) is JsonObject obj
                    && obj["success"] is JsonValue success
                    && success.TryGetValue(out bool ok)
                    && !ok;
            }
            catch (JsonException)
            {
                return false;
            }
        }

        /// <summary>
        /// Builds a query string from the non-empty values, e.g. "?vehicleId=1&amp;tags=a%20b".
        /// </summary>
        public static string Query(params (string Name, string? Value)[] parameters)
        {
            var parts = parameters
                .Where(p => !string.IsNullOrWhiteSpace(p.Value))
                .Select(p => $"{Uri.EscapeDataString(p.Name)}={Uri.EscapeDataString(p.Value!)}")
                .ToList();
            return parts.Count == 0 ? string.Empty : "?" + string.Join('&', parts);
        }

        public static string? FormatDate(DateTime? date) => date?.ToString("yyyy-MM-dd");

        /// <summary>
        /// Lists records of a type for a vehicle, filtered the way every LubeLogger list endpoint supports.
        /// </summary>
        public async Task<string> ListRecordsAsync(string recordPath, int vehicleId, int? id, DateTime? startDate, DateTime? endDate, string? tags)
        {
            var result = await GetAsync($"/api/vehicle/{recordPath}" + Query(
                ("vehicleId", vehicleId.ToString()),
                ("id", id?.ToString()),
                ("startDate", FormatDate(startDate)),
                ("endDate", FormatDate(endDate)),
                ("tags", tags)));
            return result.ToToolResult();
        }

        /// <summary>
        /// Read-modify-write update. LubeLogger's /update endpoints replace the whole record (files, extra fields and
        /// tags included), so fetch the current record, apply only the caller's changes, and PUT the whole object back.
        /// </summary>
        public async Task<string> UpdateRecordAsync(string recordPath, string recordLabel, int vehicleId, int id, Action<JsonObject> applyChanges)
        {
            var current = await GetAsync($"/api/vehicle/{recordPath}" + Query(("vehicleId", vehicleId.ToString()), ("id", id.ToString())));
            if (!current.Success)
            {
                return current.ToToolResult();
            }
            JsonObject? record;
            try
            {
                record = (JsonNode.Parse(current.Body) as JsonArray)?.FirstOrDefault()?.DeepClone() as JsonObject;
            }
            catch (JsonException ex)
            {
                return $"Error: unexpected response from LubeLogger while reading the {recordLabel}: {ex.Message}";
            }
            if (record == null)
            {
                return $"Error: no {recordLabel} with id {id} found for vehicle {vehicleId}.";
            }
            applyChanges(record);
            var result = await SendJsonAsync(HttpMethod.Put, $"/api/vehicle/{recordPath}/update", record);
            return result.ToToolResult();
        }

        /// <summary>
        /// Deletes a record by id. LubeLogger requires a key with Delete permission.
        /// </summary>
        public async Task<string> DeleteRecordAsync(string recordPath, int id)
        {
            var result = await SendAsync(HttpMethod.Delete, $"/api/vehicle/{recordPath}/delete" + Query(("id", id.ToString())));
            return result.ToToolResult();
        }

        /// <summary>
        /// Converts tool-supplied extra fields to the JSON array LubeLogger expects.
        /// </summary>
        public static JsonArray ToJsonArray(IEnumerable<ExtraField>? extraFields)
        {
            var array = new JsonArray();
            foreach (var field in extraFields ?? Enumerable.Empty<ExtraField>())
            {
                array.Add(new JsonObject { ["name"] = field.Name, ["value"] = field.Value });
            }
            return array;
        }

        /// <summary>
        /// Merges extra fields into a record by name: supplied names overwrite, others are kept.
        /// </summary>
        public static void MergeExtraFields(JsonObject record, IEnumerable<ExtraField>? extraFields)
        {
            if (extraFields == null || !extraFields.Any())
            {
                return;
            }
            var existing = record["extraFields"] as JsonArray ?? new JsonArray();
            foreach (var field in extraFields)
            {
                var match = existing.OfType<JsonObject>()
                    .FirstOrDefault(x => string.Equals(x["name"]?.GetValue<string>(), field.Name, StringComparison.OrdinalIgnoreCase));
                if (match != null)
                {
                    match["value"] = field.Value;
                }
                else
                {
                    existing.Add(new JsonObject { ["name"] = field.Name, ["value"] = field.Value });
                }
            }
            record["extraFields"] = existing;
        }

        /// <summary>
        /// Sets a field only when the caller supplied a value.
        /// </summary>
        public static void SetIfProvided(JsonObject record, string name, string? value)
        {
            if (value != null) record[name] = value;
        }
        public static void SetIfProvided(JsonObject record, string name, DateTime? value)
        {
            if (value.HasValue) record[name] = FormatDate(value);
        }
        public static void SetIfProvided(JsonObject record, string name, decimal? value)
        {
            if (value.HasValue) record[name] = value.Value;
        }
        public static void SetIfProvided(JsonObject record, string name, int? value)
        {
            if (value.HasValue) record[name] = value.Value;
        }
        public static void SetIfProvided(JsonObject record, string name, bool? value)
        {
            if (value.HasValue) record[name] = value.Value;
        }
    }
}
