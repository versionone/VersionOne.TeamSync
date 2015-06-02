namespace VersionOne.TeamSync.V1Connector.Interfaces
{

    public interface ICanSetUserAgentHeader
    {
        /// <summary>
        /// Required method for setting a custom user agent header for all HTTP requests made to the VersionOne API.
        /// </summary>
        /// <param name="name">The name of the application.</param>
        /// <param name="version">The version number of the application.</param>
        /// <returns></returns>
        ICanSetAuthMethod WithUserAgentHeader(string name, string version);
    }

    public interface ICanSetAuthMethod
    {
        /// <summary>
        /// Optional method for setting the username and password for authentication.
        /// </summary>
        /// <param name="username">The username of a valid VersionOne member account.</param>
        /// <param name="password">The password of a valid VersionOne member account.</param>
        /// <returns>ICanSetProxyOrEndpointOrGetConnector</returns>
        ICanSetProxyOrEndpointOrGetConnector WithUsernameAndPassword(string username, string password);

        /// <summary>
        /// Optional method for setting the Windows Integrated Authentication credentials for authentication based on the currently logged in user.
        /// </summary>
        /// <returns>ICanSetProxyOrEndpointOrGetConnector</returns>
        ICanSetProxyOrEndpointOrGetConnector WithWindowsIntegrated();

        /// <summary>
        /// Optional method for setting the Windows Integrated Authentication credentials for authentication based on specified user credentials.
        /// </summary>
        /// <param name="fullyQualifiedDomainUsername">The fully qualified domain name in form "DOMAIN\username".</param>
        /// <param name="password">The password of a valid VersionOne member account.</param>
        /// <returns>ICanSetProxyOrEndpointOrGetConnector</returns>
        ICanSetProxyOrEndpointOrGetConnector WithWindowsIntegrated(string fullyQualifiedDomainUsername, string password);

        /// <summary>
        /// Optional method for setting the access token for authentication.
        /// </summary>
        /// <param name="accessToken">The access token.</param>
        /// <returns>ICanSetProxyOrEndpointOrGetConnector</returns>
        ICanSetProxyOrEndpointOrGetConnector WithAccessToken(string accessToken);

        /// <summary>
        /// Optional method for setting the OAuth2 access token for authentication.
        /// </summary>
        /// <param name="accessToken">The OAuth2 access token.</param>
        /// <returns>ICanSetProxyOrEndpointOrGetConnector</returns>
        ICanSetProxyOrEndpointOrGetConnector WithOAuth2Token(string accessToken);
    }

    public interface ICanGetConnector
    {
        /// <summary>
        /// Required terminating method that returns the V1Connector object.
        /// </summary>
        /// <returns>V1Connector</returns>
        V1Connector Build();
    }

    public interface ICanSetProxyOrEndpointOrGetConnector : ICanSetEndpoint, ICanGetConnector
    {
        /// <summary>
        /// Optional method for setting the proxy credentials.
        /// </summary>
        /// <param name="proxyProvider">The ProxyProvider containing the proxy URI, username, and password.</param>
        /// <returns>ICanSetEndpointOrGetConnector</returns>
        ICanSetEndpointOrGetConnector WithProxy(ProxyProvider proxyProvider);
    }

    public interface ICanSetEndpointOrGetConnector : ICanGetConnector
    {
        /// <summary>
        /// Optional method for specifying an API endpoint to connect to.
        /// </summary>
        /// <param name="endpoint">The API endpoint.</param>
        /// <returns>ICanGetConnector</returns>
        ICanGetConnector UseEndpoint(string endpoint);
    }

    public interface ICanSetProxyOrGetConnector : ICanGetConnector
    {
        /// <summary>
        /// Optional method for setting the proxy credentials.
        /// </summary>
        /// <param name="proxyProvider">The ProxyProvider containing the proxy URI, username, and password.</param>
        /// <returns>ICanGetConnector</returns>
        ICanGetConnector WithProxy(ProxyProvider proxyProvider);
    }

    public interface ICanSetEndpoint
    {
        /// <summary>
        /// Optional method for specifying an API endpoint to connect to.
        /// </summary>
        /// <param name="endpoint">The API endpoint.</param>
        /// <returns>ICanSetProxyOrGetConnector</returns>
        ICanSetProxyOrGetConnector UseEndpoint(string endpoint);
    }

}
