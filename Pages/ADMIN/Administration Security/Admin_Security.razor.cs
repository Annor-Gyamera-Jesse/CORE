using Microsoft.AspNetCore.Components;
using Radzen;

namespace CORE.Pages.ADMIN.Administration_Security
{
    public class Admin_Security_CodeBehind : ComponentBase
    {
        [Inject]
        public DialogService DialogService { get; set; }
        public async Task OpenUserLogsDialog()
        {

            ///compose options for Razden dialo service
            //var dialogParams = new Dictionary<string, object>() { { "TransactionBatch", m_MBC_UIView.TransactionBatch }     };
            var dialogOptions = new DialogOptions() { Draggable = true, ShowClose = false, Width = "1000px", Resizable = false, Height = "512px"/*, Left = "calc(30% - 350px)", Top = "calc(30% - 265px)"*/ };
            var dialogtitle = $"User Logs";
            //register dialog closed event with RadzenDialogService
            await DialogService.OpenAsync<View_UserLogs>(dialogtitle, null, dialogOptions);
        }
    }
}
