using CORE.Pages.EXAMS.EXAMS_EXPORT.REPORT_PER_CLASS;
using CORE.Pages.EXAMS.EXAMS_EXPORT;
using Microsoft.AspNetCore.Components;
using Radzen;
using CORE.Pages.STAFF_ATTENDANCES.ATTENDANCE_PPRINT;

namespace CORE.Pages.STAFF_ATTENDANCES.TEACHERS
{
    public class Attendance_Teachers_CodeBehind : ComponentBase
    {
        [Inject]
        public DialogService DialogService { get; set; }

        public async Task PerTeacherExportDialog()
        {
            if (DialogService == null)
            {
                Console.WriteLine("DialogService is null! Make sure it's registered in Program.cs.");
                return;
            }

            var dialogOptions = new DialogOptions() { Draggable = false, ShowClose = true, CloseDialogOnEsc = true, Width = "600px", Resizable = false, Height = "100px" };
            var dialogTitle = $"EXPORT TO PDF-(PER TEACHER)";

            await DialogService.OpenAsync<All_Staffs_Attendance_Print_Per_Teacher>(dialogTitle, null, dialogOptions);
        }
        public async Task PerTermExportDialog()
        {
            if (DialogService == null)
            {
                Console.WriteLine("DialogService is null! Make sure it's registered in Program.cs.");
                return;
            }

            var dialogOptions = new DialogOptions() { Draggable = false, ShowClose = true, CloseDialogOnEsc = true, Width = "600px", Resizable = false, Height = "100px" };
            var dialogTitle = $"EXPORT TO PDF-(DATE RANGE)";

            await DialogService.OpenAsync<All_Staffs_Attendance_Print_Per_Date_Range>(dialogTitle, null, dialogOptions);
        }
    }
}
