using CORE.Pages.TEACHERS_ASSESMENT;
using CORE.Pages.TIMETABLE;
using Microsoft.AspNetCore.Components;
using Radzen;

namespace CORE.Pages.TEACHERS_TASK
{
    public class TeachersTask_Page_Codebehind : ComponentBase
    {
        [Inject]
        protected Radzen.DialogService DialogService { get; set; }
        public async Task OpenManageTeachersTimeTable()
        {
            var dialogOptions = new DialogOptions() { Draggable = true, ShowClose = true, CloseDialogOnEsc = true, Width = "1000px", Resizable = false, Height = "410px" };
            var dialogTitle = $"Manage Teachers Time Table";
            // Register dialog closed event with RadzenDialogService
            await DialogService.OpenAsync<EditTeachersTimeTableDialog>(dialogTitle, null, dialogOptions);
        }

        public async Task OpenTeacherTimeTable()
        {
            var dialogOptions = new DialogOptions() { Draggable = true, ShowClose = true, CloseDialogOnEsc = true, Width = "1000px", Resizable = false, Height = "512px" };
            var dialogTitle = $"Teachers Time Table";
            // Register dialog closed event with RadzenDialogService
            await DialogService.OpenAsync<TimetablePage>(dialogTitle, null, dialogOptions);
        }
    }
}
