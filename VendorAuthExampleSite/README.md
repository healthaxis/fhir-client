# Example Authorization Consent Site for Vendors

This example project demonstrates how to use the OAuth 2 authorization code flow to receive consent from a member for data access and then to use that consent to retrieve an access token.

This solution is almost completely a boilerplate ASP.Net Core 3.1 site. To see how to implement the authorization code, see the one modified page in the project, the Index in the Pages directory, specifically Index.cshtml.cs.

## Step 1: Redirect the user to the HealthAxis consent URL

Build a URL using the following query parameters (state is optional) and redirect the user to it. It is important that you redirect the user directly so the user can see the URL and the browser's SSL and certificate status. Do not hide the consent page in an iframe or similar.

```
string url = QueryHelpers.AddQueryString(Settings["auth_uri"], new Dictionary<string, string>
{
    ["client_id"] = Settings["client_id"],
    ["response_type"] = "code",
    ["scope"] = Settings["scope"],
    ["redirect_uri"] = Settings["redirect_uri"],
    ["site_uri"] = Settings["site_uri"],
    ["state"] = "optional anti-forgery token"
});
```

## Step 2: Get the access token

After the user authorizes your access, the consent page redirects the user back to your callback page, providing the "code" parameter if consent is given. If you provided "state," it is returned to you.

`
OnGetCallbackAsync(string? code = null, string? error = null, string? state = null)
`

Once you have the code, post an object matching this format to the HealthAxis token URL, using application/json content-type.

```
var payload = new
{
    code,
    grant_type = "authorization_code",
    client_id = Settings["client_id"],
    client_secret = Settings["client_secret"],
    redirect_uri = Settings["redirect_uri"],
    expires_in = 360000 // token lifetime in seconds
};

var response = await httpClient.PostAsJsonAsync(Settings["token_uri"], payload);
```

### Step 2.1: Decode the response

The object returned from the token endpoint is a simple JSON object. The access code is found in the access_code property.

```
var result = await response.Content.ReadAsAsync<JObject>();
TokenResponseMessage = (string)result["access_token"];
```
