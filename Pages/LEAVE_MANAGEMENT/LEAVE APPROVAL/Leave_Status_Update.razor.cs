using CORE.Pages.FEES;
using CORE.Pages.LEAVE_MANAGEMENT.DIALOG;
using Microsoft.AspNetCore.Components;
using Radzen;

namespace CORE.Pages.LEAVE_MANAGEMENT.LEAVE_APPROVAL
{
    public class Leave_Status_Update_CodeBehind : ComponentBase
    {

        [Inject]
        public Radzen.DialogService DialogService { get; set; }

        public async Task OpenApprovedDialog()
        {
            var dialogOptions = new DialogOptions() { Draggable = false, ShowClose = true, CloseDialogOnEsc = true, Width = "600px", Resizable = false, Height = "360px" };
            var dialogTitle = $"Approved Leaves";
            // Register dialog closed event with RadzenDialogService
            await DialogService.OpenAsync<ApprovedLeaveRequests>(dialogTitle, null, dialogOptions);
        }
        //public async Task OpenIncomingDialog()
        //{
        //    var dialogOptions = new DialogOptions() { Draggable = false, ShowClose = true, CloseDialogOnEsc = true, Width = "600px", Resizable = false, Height = "360px" };
        //    var dialogTitle = $"Incoming Leaves";
        //    // Register dialog closed event with RadzenDialogService
        //    await DialogService.OpenAsync<Payment_Dialog>(dialogTitle, null, dialogOptions);
        //} 
        public async Task OpenRejectedDialog()
        {
            var dialogOptions = new DialogOptions() { Draggable = false, ShowClose = true, CloseDialogOnEsc = true, Width = "600px", Resizable = false, Height = "360px" };
            var dialogTitle = $"Rejected Leaves";
            // Register dialog closed event with RadzenDialogService
            await DialogService.OpenAsync<RejectedLeaveRequests>(dialogTitle, null, dialogOptions);
        }



    }
}
