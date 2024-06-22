using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

namespace CORE.SERVICE
{
    public class AuthStateService
    {
        private readonly IJSRuntime jsRuntime;
        private readonly NavigationManager navigationManager;

        public AuthStateService(IJSRuntime jsRuntime, NavigationManager navigationManager)
        {
            this.jsRuntime = jsRuntime;
            this.navigationManager = navigationManager;
        }

        public async Task<bool> IsUserAuthenticated()
        {
            var userName = await jsRuntime.InvokeAsync<string>("sessionStorage.getItem", "userName");
            return !string.IsNullOrEmpty(userName);
        }

        public async Task EnsureAuthenticated()
        {
            if (!await IsUserAuthenticated())
            {
                navigationManager.NavigateTo("/login", true); // Enforce redirect
            }
        }
    }
}
