
using CORE.Pages.FEES.FEEDING_FEE.OTHER_FEES_FEETYPE;
using CORE.Pages.FEES.FEEDING_FEE.OTHER_PAYMENT.EDIT;
using Microsoft.AspNetCore.Components;
using Radzen;

namespace CORE.Pages.FEES.FEEDING_FEE.OTHER_PAYMENT
{
    public class View_Other_FeesCodeBehind : ComponentBase
    {

        [Inject]
        public Radzen.DialogService DialogService { get; set; }

        public async Task OpenAddDialog()
        {
            var dialogOptions = new DialogOptions() { Draggable = false, ShowClose = true, CloseDialogOnEsc = true, Width = "600px", Resizable = false, Height = "355px" };
            var dialogTitle = $"Adding Other Fees";
            // Register dialog closed event with RadzenDialogService
            await DialogService.OpenAsync<Adding_Other_Fee_Feetype>(dialogTitle, null, dialogOptions);
        }
         public async Task OpenEditDialog()
        {
            var dialogOptions = new DialogOptions() { Draggable = false, ShowClose = true, CloseDialogOnEsc = true, Width = "1000px", Resizable = false, Height = "355px" };
            var dialogTitle = $"Editing Other Fees";
            // Register dialog closed event with RadzenDialogService
            await DialogService.OpenAsync<Edit_Other_Fee_Feetypes>(dialogTitle, null, dialogOptions);
        }

    }
}
