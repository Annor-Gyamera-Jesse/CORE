using CORE.Pages.FEES;
using CORE.Pages.LESSONS_NOTES.PRINT;
using Microsoft.AspNetCore.Components;
using Radzen;

namespace CORE.Pages.LESSONS_NOTES
{
    public class View_Submitted_Lesson_Note_Page_CodeBehind : ComponentBase
    {
        [Inject]
        public Radzen.DialogService DialogService { get; set; }

        public async Task OpenLessonNoteDialog()
        {
            var dialogOptions = new DialogOptions() { Draggable = false, ShowClose = true, CloseDialogOnEsc = true, Width = "600px", Resizable = false, Height = "401px" };
            var dialogTitle = $"PRINT LESSON";
            // Register dialog closed event with RadzenDialogService
            await DialogService.OpenAsync<Lesson_Note_Export_Print>(dialogTitle, null, dialogOptions);
        }

    }
}
