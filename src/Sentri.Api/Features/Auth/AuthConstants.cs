namespace Sentri.Api.Features.Auth;

public static class AuthConstants
{
    public const string AuthScheme = "Sentri";
    public const string BusinessPolicy = "BusinessPolicy";
    public const string PanelPolicy = "PanelPolicy";
    public const string ApiKeyScheme = "SentriApiKey";
    public const string PanelCookieScheme = "SentriPanelCookie";
    public const string PanelCookieName = ".Sentri.Panel";
    public const string ApiKeyHeaderName = "X-API-KEY";
    public const string ApiKeyPrefix = "sk";
}