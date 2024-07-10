//using CORE.Pages.LESSONS_NOTES;
//using Microsoft.AspNetCore.Components;
//using Radzen;

//namespace CORE.Pages.TEACHERS_ASSESMENT
//{
//    public class Teachers_Assesment_Page_CodeBehind : ComponentBase
//    {
//        [Inject]
//        protected Radzen.DialogService DialogService { get; set; }

//        public async Task OpenAssesmentsDialog()
//        {
//            var dialogOptions = new DialogOptions() { Draggable = true, ShowClose = false, CloseDialogOnEsc = true, Width = "1000px", Resizable = false, Height = "512px" };
//            var dialogTitle = $"Assesment Sheet";
//            // Register dialog closed event with RadzenDialogService
//            await DialogService.OpenAsync<Teachers_Assesment_Page>(dialogTitle, null, dialogOptions);
//        }
//    }
//}
