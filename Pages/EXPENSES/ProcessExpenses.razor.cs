using CORE.Pages.EXPENSES.Expense_Dialog;
using Microsoft.AspNetCore.Components;
using Radzen;

namespace CORE.Pages.EXPENSES
{
    public class ProcessExpensesCodeBehind : ComponentBase
    {
        [Inject]
        public Radzen.DialogService DialogService { get; set; }

        public async Task OpenPaymentDialog()
        {
            var dialogOptions = new DialogOptions() { Draggable = false, ShowClose = true, CloseDialogOnEsc = true, Width = "900px", Resizable = false, Height = "501px" };
            var dialogTitle = $"Manage Categories";
            // Register dialog closed event with RadzenDialogService
            await DialogService.OpenAsync<ManageCategories>(dialogTitle, null, dialogOptions);
        }
    }
}
