using CORE.Pages.EXPENSES.Expense_Dialog;
using CORE.Pages.EXPENSES.Expenses_History;
using CORE.Pages.EXPENSES.Payment_Method_Dialog;
using Microsoft.AspNetCore.Components;
using Radzen;

namespace CORE.Pages.EXPENSES
{
    public class ProcessExpensesCodeBehind : ComponentBase
    {
        [Inject]
        public Radzen.DialogService DialogService { get; set; }

        public async Task OnEditCategories()
        {
            var dialogOptions = new DialogOptions() { Draggable = false, ShowClose = true, CloseDialogOnEsc = true, Width = "900px", Resizable = false, Height = "501px" };
            var dialogTitle = $"Manage Categories";
            // Register dialog closed event with RadzenDialogService
            await DialogService.OpenAsync<ManageCategories>(dialogTitle, null, dialogOptions);
        } 
        public async Task OnManagePaymentMethods()
        {
            var dialogOptions = new DialogOptions() { Draggable = false, ShowClose = true, CloseDialogOnEsc = true, Width = "900px", Resizable = false, Height = "501px" };
            var dialogTitle = $"Manage Payment Methods";
            // Register dialog closed event with RadzenDialogService
            await DialogService.OpenAsync<ManagePaymentMethods>(dialogTitle, null, dialogOptions);
        }
        public async Task OnHistory()
        {
            var dialogOptions = new DialogOptions() { Draggable = false, ShowClose = true, CloseDialogOnEsc = true, Width = "900px", Resizable = false, Height = "501px" };
            var dialogTitle = $"HISTORY";
            // Register dialog closed event with RadzenDialogService
            await DialogService.OpenAsync<ExpensesHistory>(dialogTitle, null, dialogOptions);
        }
    }
}
