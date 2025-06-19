using CORE.Pages.BANK.TRANSFER_OF_FUNDS;
using CORE.Pages.FEES.CASH_IN_HAND;
using CORE.Pages.FEES.PDF_PER_DATE;
using Microsoft.AspNetCore.Components;
using Radzen;

namespace CORE.Pages.FEES
{
    public class SchoolFeesCodeBehind : ComponentBase
    {
        [Inject]
        public Radzen.DialogService DialogService { get; set; }

        public async Task OpenPaymentDialog()
        {
            var dialogOptions = new DialogOptions() { Draggable = false, ShowClose = true, CloseDialogOnEsc = true, Width = "600px", Resizable = false, Height = "355px" };
            var dialogTitle = $"Fees Export To PDF";
            // Register dialog closed event with RadzenDialogService
            await DialogService.OpenAsync<Payment_Dialog>(dialogTitle, null, dialogOptions);
        }

        public async Task OpenTransferOfFundsDialog()
        {
            var dialogOptions = new DialogOptions() { Draggable = false, ShowClose = true, CloseDialogOnEsc = true, Width = "600px", Resizable = false, Height = "582px" };
            var dialogTitle = $"BANK TRANSFER";
            // Register dialog closed event with RadzenDialogService
            await DialogService.OpenAsync<Transfer_Of_Funds>(dialogTitle, null, dialogOptions);
        } 
        public async Task OpenCashInHandsDialog()
        {
            var dialogOptions = new DialogOptions() { Draggable = false, ShowClose = true, CloseDialogOnEsc = true, Width = "360px", Resizable = false, Height = "209px" };
            var dialogTitle = $"CASH IN HANDS";
            // Register dialog closed event with RadzenDialogService
            await DialogService.OpenAsync<Cash_In_Hand>(dialogTitle, null, dialogOptions);
        }
        public async Task OpenFeesPerDateDialog()
        {
            var dialogOptions = new DialogOptions() { Draggable = false, ShowClose = true, CloseDialogOnEsc = true, Width = "360px", Resizable = false, Height = "295px" };
            var dialogTitle = $"Fees Export To PDF Per Date";
            // Register dialog closed event with RadzenDialogService
            await DialogService.OpenAsync<StudentFeeReportByDate>(dialogTitle, null, dialogOptions);
        }

    }
}
