using CORE.Pages.TEACHERS_TASK.PRINT_SCHOOL_TIME_TABLE;
using Microsoft.AspNetCore.Components;
using Radzen;

namespace CORE.Pages.TEACHERS_TASK.TIMETABLE
{
    public class Time_Table_Codebehind : ComponentBase
    {
        [Inject]
        protected Radzen.DialogService DialogService { get; set; }
        public async Task OpenManageTeachersTimeTable()
        {
            var dialogOptions = new DialogOptions() { Draggable = true, ShowClose = true, CloseDialogOnEsc = true, Width = "900px", Resizable = false, Height = "500px" };
            var dialogTitle = $"Teachers Time Table";
            // Register dialog closed event with RadzenDialogService
            await DialogService.OpenAsync<EditTeachersTimeTableDialog>(dialogTitle, null, dialogOptions);
        }

        public async Task OpenOnNoticeBoard()
        {
            var dialogOptions = new DialogOptions() { Draggable = true, ShowClose = true, CloseDialogOnEsc = true, Width = "600px", Resizable = false, Height = "301px" };
            var dialogTitle = $"PRINT CLASS TIME TABLE";
            // Register dialog closed event with RadzenDialogService
            await DialogService.OpenAsync<ClassTimetable>(dialogTitle, null, dialogOptions);
        }
    }
}
