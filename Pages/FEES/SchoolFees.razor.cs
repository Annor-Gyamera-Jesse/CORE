using CORE.Pages.BANK.TRANSFER_OF_FUNDS;

using CORE.Pages.FEES.CASH_IN_HAND;
using CORE.Pages.FEES.Fees_Carry_Over;
using CORE.Pages.FEES.FEES_STATEMENT;
using CORE.Pages.FEES.PDF_PER_DATE;
using Microsoft.AspNetCore.Components;
using Radzen;

namespace CORE.Pages.FEES
{
    public class SchoolFeesCodeBehind : ComponentBase
    {
        [Inject]
        public Radzen.DialogService DialogService { get; set; }

        protected async Task OpenPaymentDialog()
        {
            var dialogOptions = new DialogOptions() { Draggable = false, ShowClose = true, CloseDialogOnEsc = true, Width = "600px", Resizable = false, Height = "355px" };
            var dialogTitle = $"Student Fees Statements";
            // Register dialog closed event with RadzenDialogService
            await DialogService.OpenAsync<Payment_Dialog>(dialogTitle, null, dialogOptions);
        }

        protected async Task OpenTransferOfFundsDialog()
        {
            var dialogOptions = new DialogOptions() { Draggable = false, ShowClose = true, CloseDialogOnEsc = true, Width = "600px", Resizable = false, Height = "582px" };
            var dialogTitle = $"BANK TRANSFER";
            // Register dialog closed event with RadzenDialogService
            await DialogService.OpenAsync<Transfer_Of_Funds>(dialogTitle, null, dialogOptions);
        }
        protected async Task OpenCashInHandsDialog()
        {
            var dialogOptions = new DialogOptions() { Draggable = false, ShowClose = true, CloseDialogOnEsc = true, Width = "360px", Resizable = false, Height = "209px" };
            var dialogTitle = $"CASH IN HANDS";
            // Register dialog closed event with RadzenDialogService
            await DialogService.OpenAsync<Cash_In_Hand>(dialogTitle, null, dialogOptions);
        }
        protected async Task OpenFeesPerDateDialog()
        {
            var dialogOptions = new DialogOptions() { Draggable = false, ShowClose = true, CloseDialogOnEsc = true, Width = "360px", Resizable = false, Height = "295px" };
            var dialogTitle = $"Fees Export To PDF Per Date";
            // Register dialog closed event with RadzenDialogService
            await DialogService.OpenAsync<StudentFeeReportByDate>(dialogTitle, null, dialogOptions);
        }

        protected async Task OpenCarryOverDialog()
        {
            var dialogOptions = new DialogOptions() { Draggable = false, ShowClose = true, CloseDialogOnEsc = true, Width = "450px", Resizable = false, Height = "265px" };
            var dialogTitle = $"Carry Over Unpaid Fees to a New Term";
            // Register dialog closed event with RadzenDialogService
            await DialogService.OpenAsync<Carry_Over>(dialogTitle, null, dialogOptions);
        }

        protected async Task OpenFeesSummaryDialog()
        {
            var dialogOptions = new DialogOptions() { Draggable = false, ShowClose = true, CloseDialogOnEsc = true, Width = "450px", Resizable = false, Height = "273px" };
            var dialogTitle = $"FEES STATEMENT";
            // Register dialog closed event with RadzenDialogService
            await DialogService.OpenAsync<Fees_Statement_Summary>(dialogTitle, null, dialogOptions);
        }

    }
}