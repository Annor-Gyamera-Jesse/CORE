using CORE.SERVICE.NOTIFICATION;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using Radzen;

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
        protected NotificationMessageService Notify { get; set; }

        private bool sidebarExpanded = true;
        private bool isLoading = false;
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
            try
            {
                isLoading = true;
                // Clear the user name from session storage
                await JSRuntime.InvokeVoidAsync("sessionStorage.removeItem", "userName");
                await JSRuntime.InvokeVoidAsync("sessionStorage.removeItem", "userID");
                // Redirect to the login page after logout
                NavigationManager.NavigateTo("/login");
                Notify.ShowNotification("", "LogOut", NotificationSeverity.Success);
            }
            catch (Exception ex)
            {
                Notify.ShowNotification("", $"{ex.Message}", NotificationSeverity.Info);
            }
            finally
            {
                isLoading = false;
            }
        }
    }
}
