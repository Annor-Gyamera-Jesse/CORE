using CORE.Pages.BANK.DEPOSIT;
using CORE.STUDENTS_ATTENDANCES.PRINT;
using Microsoft.AspNetCore.Components;
using Radzen;

namespace CORE.STUDENTS_ATTENDANCES.STUDENTS
{
    public class Students_AttendanceCodeBehind : ComponentBase
    {
        [Inject]
        public DialogService DialogService { get; set; }
        public async Task OpenAttendanceByClassDialog()
        {
            var dialogOptions = new DialogOptions() { Draggable = false, ShowClose = true, CloseDialogOnEsc = true, Width = "600px", Resizable = false, Height = "340px" };
            var dialogTitle = $"PRINT PER CLASS";
            // Register dialog closed event with RadzenDialogService
            await DialogService.OpenAsync<Per_Class_Attendance_Export>(dialogTitle, null, dialogOptions);
        }
     
        public async Task OpenAttendancePerStudentDialog()
        {
            var dialogOptions = new DialogOptions() { Draggable = false, ShowClose = true, CloseDialogOnEsc = true, Width = "600px", Resizable = false, Height = "340px" };
            var dialogTitle = $"PRINT PER STUDENT";
            // Register dialog closed event with RadzenDialogService
            await DialogService.OpenAsync<Per_Student_Attendance_Export>(dialogTitle, null, dialogOptions);
        }


    }
}
