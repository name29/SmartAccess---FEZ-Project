////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
// Copyright (c) Microsoft Corporation.  All rights reserved.
////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////


namespace SmartAccess
{
    /// <summary>
    /// Helper class to create <see cref="HttpRequest"/> objects that are configured for the various http request methods (GET, PUT etc)
    /// </summary>
    public class HttpHelper
    {
        /// <summary>
        /// Create an Http PUT request.
        /// </summary>
        /// <param name="url">The Url of the web server to which the request will be sent.</param>
        /// <param name="content">The <see cref="PUTContent"/> object to be sent to the Url.</param>
        /// <param name="contentType">The MIME-Type of the message.</param>
        /// <param name="ms_timeout">Timeout of HTTP request in millisecond</param>
        /// <returns>An <see cref="HttpRequest"/> object that can be used to make PUT request.</returns>
        public static HttpRequest CreateHttpPutRequest(string url, PUTContent content, string contentType, int ms_timeout)
        {
            return new HttpRequest(HttpRequest.RequestMethod.PUT, url, content, contentType, null, null,ms_timeout);
        }

        /// <summary>
        /// Create an Http POST request.
        /// </summary>
        /// <param name="url">The Url of the web server to which the request will be sent.</param>
        /// <param name="content">The <see cref="POSTContent"/> object to be sent to the Url.</param>
        /// <param name="contentType">The MIME-Type of the message.</param>
        /// <param name="ms_timeout">Timeout of HTTP request in millisecond</param>
        /// <returns>An <see cref="HttpRequest"/> object that can be used to make POST request.</returns>
        public static HttpRequest CreateHttpPostRequest(string url, POSTContent content, string contentType,int ms_timeout)
        {
            return new HttpRequest(HttpRequest.RequestMethod.POST, url, content, contentType, null, null,ms_timeout);
        }

        /// <summary>
        /// Create an Http GET request.
        /// </summary>
        /// <param name="url">The Url of the web server to which the request will be sent.</param>
        /// <param name="content">The <see cref="GETContent"/> object to be sent to the Url.</param>
        /// <param name="ms_timeout">Timeout of HTTP request in millisecond</param>
        /// <returns>An <see cref="HttpRequest"/> object that can be used to make GET request.</returns>
        public static HttpRequest CreateHttpGetRequest(string url, GETContent content,int ms_timeout)
        {
            return new HttpRequest(HttpRequest.RequestMethod.GET, url, content, null, null, null,ms_timeout);
        }

        /// <summary>
        /// Create an Http GET request.
        /// </summary>
        /// <param name="Url">The Url of the web server to which the request will be sent.</param>
        /// <param name="ms_timeout">Timeout of HTTP request in millisecond</param>
        /// <returns>An <see cref="HttpRequest"/> object that can be used to make GET request.</returns>
        public static HttpRequest CreateHttpGetRequest(string Url,int ms_timeout)
        {
            return new HttpRequest(HttpRequest.RequestMethod.GET, Url, null, null, null, null,ms_timeout);
        }

        /// <summary>
        /// Create an Http DELETE request.
        /// </summary>
        /// <param name="url">The Url of the web server to which the request will be sent.</param>
        /// <param name="content">The <see cref="DELETEContent"/> object to be sent to the Url.</param>
        /// <param name="ms_timeout">Timeout of HTTP request in millisecond</param>
        /// <returns>An <see cref="HttpRequest"/> object that can be used to make DELETE request.</returns>
        public static HttpRequest CreateHttpDeleteRequest(string url, DELETEContent content,int ms_timeout)
        {
            return new HttpRequest(HttpRequest.RequestMethod.DELETE, url, content, null, null, null,ms_timeout);
        }

        /// <summary>
        /// Create an Http DELETE request.
        /// </summary>
        /// <param name="url">The Url of the web server to which the request will be sent.</param>
        /// <param name="ms_timeout">Timeout of HTTP request in millisecond</param>
        /// <returns>An <see cref="HttpRequest"/> object that can be used to make DELETE request.</returns>
        public static HttpRequest CreateHttpDeleteRequest(string url, int ms_timeout)
        {
            return new HttpRequest(HttpRequest.RequestMethod.DELETE, url, null, null, null, null,ms_timeout);
        }
    }
}
