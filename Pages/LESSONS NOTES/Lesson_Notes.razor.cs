using CORE.Pages.TEACHERS_ASSESMENT;
using Microsoft.AspNetCore.Components;
using Radzen;

namespace CORE.Pages.LESSONS_NOTES
{
    public class Lesson_Notes_Codebehind : ComponentBase
    {
        [Inject]
        protected Radzen.DialogService DialogService { get; set; }

        public async Task OpenSubmittedLessonNotesDialog()
        {
            var dialogOptions = new DialogOptions() { Draggable = true,ShowClose = false,CloseDialogOnEsc = true,Width = "1000px",Resizable = false,Height = "512px"};
            var dialogTitle = $"Submitted Lesson Notes";
            // Register dialog closed event with RadzenDialogService
            await DialogService.OpenAsync<View_Lesson_Note_Tab>(dialogTitle, null, dialogOptions);
        }

        public async Task OpenAssesmentsDialog()
        {
            var dialogOptions = new DialogOptions() { Draggable = true, ShowClose = true, CloseDialogOnEsc = true, Width = "1000px", Resizable = false, Height = "512px" };
            var dialogTitle = $"Assesment Sheet";
            // Register dialog closed event with RadzenDialogService
            await DialogService.OpenAsync<Teachers_Assesment_Page>(dialogTitle, null, dialogOptions);
        }
    }
}
