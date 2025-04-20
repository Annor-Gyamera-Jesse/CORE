using CORE.Pages.BANK.DEPOSIT;
using Microsoft.AspNetCore.Components;
using Radzen;

namespace CORE.Pages.BANK
{
    public class Bank_TransferCodeBehind : ComponentBase
    {
        [Inject]
        public DialogService DialogService { get; set; }
        public async Task OpenManualDepositDialog()
        {
            var dialogOptions = new DialogOptions() { Draggable = false, ShowClose = true, CloseDialogOnEsc = true, Width = "600px", Resizable = false, Height = "449px" };
            var dialogTitle = $"MANUAL DEPOSIT 💰 Bank Deposit";
            // Register dialog closed event with RadzenDialogService
            await DialogService.OpenAsync<Bank_Deposit>(dialogTitle, null, dialogOptions);
        }
    }
}
