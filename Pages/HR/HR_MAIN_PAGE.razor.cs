using CORE.Pages.FEES;
using CORE.Pages.HR.SALARY_PAYMENT_HISTORY;
using Microsoft.AspNetCore.Components;
using Radzen;

namespace CORE.Pages.HR
{
    public class HR_MAIN_PAGECodeBehind : ComponentBase
    {
        [Inject]
        public Radzen.DialogService DialogService { get; set; }

        public async Task OpenSalaryHistoryDialogDialog()
        {
            var dialogOptions = new DialogOptions() { Draggable = false, ShowClose = true, CloseDialogOnEsc = true, Width = "600px", Resizable = false, Height = "301px" };
            var dialogTitle = $"Salary_Payment_History";
            // Register dialog closed event with RadzenDialogService
            await DialogService.OpenAsync<Salary_Payment_History>(dialogTitle, null, dialogOptions);
        }

    }
}
