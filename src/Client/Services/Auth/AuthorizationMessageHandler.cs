using System.Net;
using System.Net.Http.Headers;
using Blazored.LocalStorage;
using Microsoft.AspNetCore.Components;

namespace SubTrack.Client.Services.Auth;

public sealed class AuthorizationMessageHandler(
    ILocalStorageService localStorage,
    NavigationManager navigation) : DelegatingHandler
{
    protected override async Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        var token = await localStorage.GetItemAsync<string>(JwtAuthenticationStateProvider.TokenKey);
        if (!string.IsNullOrEmpty(token))
        {
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        }

        var response = await base.SendAsync(request, cancellationToken);

        if (response.StatusCode == HttpStatusCode.Unauthorized && !string.IsNullOrEmpty(token))
        {
            // Token rejected — clear and redirect to login.
            await localStorage.RemoveItemAsync(JwtAuthenticationStateProvider.TokenKey);
            navigation.NavigateTo("/login", forceLoad: false);
        }

        return response;
    }
}
