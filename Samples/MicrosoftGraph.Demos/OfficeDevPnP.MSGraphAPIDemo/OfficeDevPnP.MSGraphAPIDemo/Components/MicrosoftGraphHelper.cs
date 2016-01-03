﻿using Microsoft.IdentityModel.Clients.ActiveDirectory;
using Microsoft.Owin.Security;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Security.Claims;
using System.Web;
using Microsoft.Owin.Security.OpenIdConnect;

namespace OfficeDevPnP.MSGraphAPIDemo.Components
{
    public static class MicrosoftGraphHelper
    {
        /// <summary>
        /// This helper method returns and OAuth Access Token for the current user
        /// </summary>
        /// <returns>The OAuth Access Token value</returns>
        public static String GetAccessTokenForCurrentUser()
        {
            String accessToken = null;

            try
            {
                ClientCredential credential = new ClientCredential(
                    MSGraphAPIDemoSettings.ClientId,
                    MSGraphAPIDemoSettings.ClientSecret);
                string signedInUserID = System.Security.Claims.ClaimsPrincipal.Current.FindFirst(
                    ClaimTypes.NameIdentifier).Value;
                AuthenticationContext authContext = new AuthenticationContext(
                    MSGraphAPIDemoSettings.Authority,
                    new SessionADALCache(signedInUserID));

                AuthenticationResult result = authContext.AcquireTokenSilent(
                    MSGraphAPIDemoSettings.MicrosoftGraphResourceId,
                    credential,
                    UserIdentifier.AnyUser);

                accessToken = result.AccessToken;
            }
            catch (AdalException ex)
            {
                if (ex.ErrorCode == "failed_to_acquire_token_silently")
                {
                    // Refresh the access token from scratch
                    HttpContext.Current.GetOwinContext().Authentication.Challenge(
                        new AuthenticationProperties {
                            RedirectUri = HttpContext.Current.Request.Url.ToString(),
                        }, 
                        OpenIdConnectAuthenticationDefaults.AuthenticationType);
                }
                else
                {
                    // Rethrow the exception
                    throw ex;
                }
            }

            return (accessToken);
        }

        /// <summary>
        /// This helper method makes an HTTP GET request and returns the result as a String
        /// </summary>
        /// <param name="graphRequestUri">The URL of the request</param>
        /// <returns>The String value of the result</returns>
        public static String MakeGetRequestAsString(String graphRequestUri)
        {
            String result = null;
            var accessToken = GetAccessTokenForCurrentUser();

            if (!String.IsNullOrEmpty(accessToken))
            {
                HttpClient httpClient = new HttpClient();
                httpClient.DefaultRequestHeaders.Authorization =
                    new AuthenticationHeaderValue("Bearer", GetAccessTokenForCurrentUser());

                HttpResponseMessage response = httpClient.GetAsync(graphRequestUri).Result;

                if (response.IsSuccessStatusCode)
                {
                    result = response.Content.ReadAsStringAsync().Result;
                }
                else
                {
                    throw new ApplicationException(
                        String.Format("Exception while invoking endpoint {0}.", graphRequestUri));
                }
            }

            return (result);
        }

        /// <summary>
        /// This helper method makes an HTTP GET request and returns the result as a String
        /// </summary>
        /// <param name="graphRequestUri">The URL of the request</param>
        /// <returns>The Stream  of the result</returns>
        public static System.IO.Stream MakeGetRequestForStream(String graphRequestUri, String accept)
        {
            System.IO.Stream result = null;
            var accessToken = GetAccessTokenForCurrentUser();

            if (!String.IsNullOrEmpty(accessToken))
            {
                HttpClient httpClient = new HttpClient();
                httpClient.DefaultRequestHeaders.Authorization =
                    new AuthenticationHeaderValue("Bearer", GetAccessTokenForCurrentUser());
                httpClient.DefaultRequestHeaders.Accept.Clear();
                httpClient.DefaultRequestHeaders.Accept.Add(
                    new MediaTypeWithQualityHeaderValue(accept));

                HttpResponseMessage response = httpClient.GetAsync(graphRequestUri).Result;

                if (response.IsSuccessStatusCode)
                {
                    result = response.Content.ReadAsStreamAsync().Result;
                }
                else
                {
                    throw new ApplicationException(
                        String.Format("Exception while invoking endpoint {0}.", graphRequestUri));
                }
            }

            return (result);
        }
    }
}