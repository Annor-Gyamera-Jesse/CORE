using CORE.Pages.LESSONS_NOTES;
using CORE.Pages.TEACHERS_ASSESMENT;
using Microsoft.AspNetCore.Components;
using Radzen;

namespace CORE.Pages.FEES
{
    public class SchoolFeesCodeBehind : ComponentBase
    {
        [Inject]
        protected Radzen.DialogService DialogService { get; set; }

        public async Task OpenPaymentDialog()
        {
            var dialogOptions = new DialogOptions() { Draggable = true, ShowClose = false, CloseDialogOnEsc = true, Width = "1000px", Resizable = false, Height = "512px" };
            var dialogTitle = $"Payment";
            // Register dialog closed event with RadzenDialogService
            await DialogService.OpenAsync<Payment_Dialog>(dialogTitle, null, dialogOptions);
        }      

    }
}
