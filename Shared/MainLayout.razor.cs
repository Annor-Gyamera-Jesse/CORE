using System.Net.Http;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.AspNetCore.Components.Routing;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.Web.Virtualization;
using Microsoft.JSInterop;
using Radzen;
using Radzen.Blazor;

namespace CORE.Shared
{
    public partial class MainLayout
    {
        [Inject]
        protected IJSRuntime JSRuntime { get; set; }

        [Inject]
        protected NavigationManager NavigationManager { get; set; }

        [Inject]
        protected DialogService DialogService { get; set; }

        [Inject]
        protected TooltipService TooltipService { get; set; }

        [Inject]
        protected ContextMenuService ContextMenuService { get; set; }

        [Inject]
        protected NotificationService NotificationService { get; set; }

        private bool sidebarExpanded = true;

        void SidebarToggleClick()
        {
            sidebarExpanded = !sidebarExpanded;
        }

        private string userName;
        protected override async Task OnAfterRenderAsync(bool firstRender)
        {
            if (firstRender)
            {
                userName = await JSRuntime.InvokeAsync<string>("sessionStorage.getItem", "userName") ?? "Guest";

                _isFirstRender = false;
                await AuthStateService.EnsureAuthenticated();
                StateHasChanged();
            }
        }

        private async Task logout()
        {

            // Clear the user name from session storage
            await JSRuntime.InvokeVoidAsync("sessionStorage.removeItem", "userName");
            await JSRuntime.InvokeVoidAsync("sessionStorage.removeItem", "userID");
            // Redirect to the login page after logout
            NavigationManager.NavigateTo("/login");
        }
    }
}
