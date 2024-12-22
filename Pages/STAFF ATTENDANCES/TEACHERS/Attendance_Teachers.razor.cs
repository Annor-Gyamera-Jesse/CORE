using CORE.Pages.STAFF_ATTENDANCES.STAFF_ATTENDANCE_REPORT;
using Microsoft.AspNetCore.Components;
using Radzen;

namespace CORE.Pages.STAFF_ATTENDANCES.TEACHERS
{
    public class Attendance_Teachers_codebehind : ComponentBase
    {
        [Inject]
        public Radzen.DialogService DialogService { get; set; }

        public async Task OpenTeachersAttendanceReportDialog()
        {
            var dialogOptions = new DialogOptions() { Draggable = false, ShowClose = true, CloseDialogOnEsc = true, Width = "600px", Resizable = false, Height = "301px" };
            var dialogTitle = $"Payment";
            // Register dialog closed event with RadzenDialogService
            await DialogService.OpenAsync<Teachers_Attendance_Report>(dialogTitle, null, dialogOptions);
        }

    }
}
