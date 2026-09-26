using System.Net;
using System.Text.RegularExpressions;

namespace AgentFw.Tests.Infrastructure
{
    /// <summary>
    /// Submits Razor Pages forms the way a browser does: load the page for the
    /// antiforgery token (and cookie), then post the fields.
    /// </summary>
    public static partial class FormClient
    {
        [GeneratedRegex("name=\"__RequestVerificationToken\"[^>]*value=\"([^\"]+)\"")]
        private static partial Regex TokenRegex();

        public static async Task<HttpResponseMessage> PostFormAsync(
            this HttpClient client, string pageUrl, string postUrl, IDictionary<string, string?> fields)
        {
            var page = await client.GetAsync(pageUrl);
            page.EnsureSuccessStatusCode();
            var token = TokenRegex().Match(await page.Content.ReadAsStringAsync()).Groups[1].Value;
            Assert.False(string.IsNullOrEmpty(token), $"No antiforgery token found on {pageUrl}.");

            var body = fields
                .Where(f => f.Value is not null)
                .Select(f => new KeyValuePair<string, string>(f.Key, f.Value!))
                .Append(new("__RequestVerificationToken", token));

            return await client.PostAsync(postUrl, new FormUrlEncodedContent(body));
        }

        public static Task<HttpResponseMessage> PostFormAsync(
            this HttpClient client, string url, IDictionary<string, string?> fields) =>
            client.PostFormAsync(url, url, fields);

        /// <summary>Page text with HTML entities decoded (Razor encodes characters like "ı").</summary>
        public static async Task<string> ReadDecodedAsync(this HttpResponseMessage response) =>
            WebUtility.HtmlDecode(await response.Content.ReadAsStringAsync());

        public static void AssertRedirectsTo(this HttpResponseMessage response, string path)
        {
            Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
            Assert.Equal(path, response.Headers.Location?.OriginalString, StringComparer.OrdinalIgnoreCase);
        }

        public static async Task AssertShowsErrorAsync(this HttpResponseMessage response, string message)
        {
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.Contains(message, await response.ReadDecodedAsync());
        }
    }
}
