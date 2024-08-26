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

                // Retrieve the UserID from session storage
                var userIdString = await JSRuntime.InvokeAsync<string>("sessionStorage.getItem", "userID");
                if (int.TryParse(userIdString, out int userId))
                {
                    // Log the logout event
                    await authService.LogUserEventAsync(userId, "Logout");
                }

                // Clear the user data from session storage
                await JSRuntime.InvokeVoidAsync("sessionStorage.removeItem", "userName");
                await JSRuntime.InvokeVoidAsync("sessionStorage.removeItem", "userID");

                // Redirect to the login page after logout
                NavigationManager.NavigateTo("/login");
                Notify.ShowNotification("", "Logged out successfully", NotificationSeverity.Success);
            }
            catch (Exception ex)
            {
                Notify.ShowNotification("", $"Error during logout: {ex.Message}", NotificationSeverity.Error);
            }
            finally
            {
                isLoading = false;
            }
        }

    }
}
