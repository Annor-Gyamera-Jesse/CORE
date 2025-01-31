using CORE.Pages.EXAMS.EXAMS_EXPORT;
using CORE.Pages.EXAMS.EXAMS_EXPORT.REPORT_PER_CLASS;
using Microsoft.AspNetCore.Components;
using Radzen;

namespace CORE.Pages.EXAMS
{
    public class ExamsCodeBehind : ComponentBase
    {
        [Inject]
        public DialogService DialogService { get; set; }

        public async Task OpenExamsExportDialog()
        {
            if (DialogService == null)
            {
                Console.WriteLine("DialogService is null! Make sure it's registered in Program.cs.");
                return;
            }

            var dialogOptions = new DialogOptions() { Draggable = false, ShowClose = true, CloseDialogOnEsc = true, Width = "600px", Resizable = false, Height = "301px" };
            var dialogTitle = $"EXPORT TO PDF";

            await DialogService.OpenAsync<Exams_Export>(dialogTitle, null, dialogOptions);
        }
        public async Task OpenExamsPerClassExportDialog()
        {
            if (DialogService == null)
            {
                Console.WriteLine("DialogService is null! Make sure it's registered in Program.cs.");
                return;
            }

            var dialogOptions = new DialogOptions() { Draggable = false, ShowClose = true, CloseDialogOnEsc = true, Width = "600px", Resizable = false, Height = "301px" };
            var dialogTitle = $"EXPORT TO PDF";

            await DialogService.OpenAsync<Print_Per_Class>(dialogTitle, null, dialogOptions);
        }
    }
}
