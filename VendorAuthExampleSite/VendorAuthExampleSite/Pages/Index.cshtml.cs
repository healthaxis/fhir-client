using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.WebUtilities;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;

namespace VendorAuthExampleSite.Pages.Shared
{
    /// <summary>
    /// This page gives an example for how to implement the Authorization Code Flow to get a token to access
    /// the HealthAxis FHIR API on behalf of an authorizing member.
    /// </summary>
    [BindProperties(SupportsGet = true)]
    public class IndexModel : PageModel
    {
        public string ErrorMessage { get; set; } = "";
        public string TokenResponseMessage { get; set; } = "";

        readonly Dictionary<string, string> Settings = new Dictionary<string, string>();

        public IndexModel()
        {
            // Set up your page-level configuration somewhere (in an actual site, you would use dependency
            // injection, but let's keep things simple).
            Settings["client_id"] = "INSERT VALUE FROM DEVELOPER PORTAL";
            Settings["client_secret"] = "INSERT VALUE FROM DEVELOPER PORTAL";
            Settings["scope"] = "INSERT WHAT YOU WANT TO ACCESS";
            Settings["auth_uri"] = "INSERT HEALTHAXIS AUTH URI";
            Settings["token_uri"] = "INSERT HEALTHAXIS TOKEN URI";

            // URL to this page's OnGetCallbackAsync(), must match value entered in developer portal
            Settings["redirect_uri"] = "https://localhost:44353/callback";
        }

        /// <summary>
        /// Redirects the user when the "Test the authorizaton process" button is clicked.
        /// </summary>
        /// <returns></returns>
        public IActionResult OnGetTest()
        {
            // Build the auth URL using these settings
            string url = QueryHelpers.AddQueryString(Settings["auth_uri"], new Dictionary<string, string>
            {
                ["client_id"] = Settings["client_id"],
                ["response_type"] = "code",
                ["scope"] = Settings["scope"],
                ["redirect_uri"] = Settings["redirect_uri"],
                ["state"] = "optional anti-forgery token"
            });

            return Redirect(url);
        }

        /// <summary>
        /// Called by HealthAxis when the user has finished authorizing your data request.
        /// </summary>
        /// <param name="code"></param>
        /// <param name="error"></param>
        /// <param name="state"></param>
        /// <returns></returns>
        public async Task<IActionResult> OnGetCallbackAsync(string? code = null, string? error = null, string? state = null)
        {
            if (string.IsNullOrWhiteSpace(code))
            {
                ErrorMessage = error ?? "You did not approve the authorization request. We cannot collect your data.";
                return Page();
            }

            var httpClient = new HttpClient();

            // Get the access token from the code. Auth codes are valid for one attempt, so if you receive a "authorization code
            // is incorrect" message if the redirect_uri or client_id is wrong, you will need to ask the user to approve again.
            var payload = new Dictionary<string, string>
            {
                ["code"] = code,
                ["grant_type"] = "authorization_code",
                ["client_id"] = Settings["client_id"],
                ["client_secret"] = Settings["client_secret"],
                ["redirect_uri"] = Settings["redirect_uri"],
                ["expires_in"] = "360000" // token lifetime in seconds
            };

            TokenResponseMessage = await GetAuthTokenAsync(httpClient, Settings["token_uri"], payload).ConfigureAwait(false);

            return Page();
        }

        async Task<string> GetAuthTokenAsync(HttpClient httpClient, string uri, Dictionary<string, string> form)
        {
            HttpRequestMessage requestMessage = new HttpRequestMessage(HttpMethod.Post, uri);
            requestMessage.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            requestMessage.Content = new FormUrlEncodedContent(form);

            var response = await httpClient.SendAsync(requestMessage).ConfigureAwait(false);
            response.EnsureSuccessStatusCode();

            var content = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
            var token = JsonConvert.DeserializeObject<JObject>(content) ?? throw new InvalidOperationException("Azure API returned empty content");

            return (string)token["access_token"];
        }
    }
}