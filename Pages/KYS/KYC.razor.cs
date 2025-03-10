using Microsoft.AspNetCore.Components;
using Radzen;
using CORE.Pages.KYS.STUDENT_ID_CARD;

namespace CORE.Pages.KYS
{
    public class KYCCodeBehind : ComponentBase
    {
        [Inject]
        public DialogService DialogService { get; set; }

        public async Task OpenStudentIDCardExportDialog()
        {
            if (DialogService == null)
            {
                Console.WriteLine("DialogService is null! Make sure it's registered in Program.cs.");
                return;
            }

            var dialogOptions = new DialogOptions() { Draggable = false, ShowClose = true, CloseDialogOnEsc = true, Width = "600px", Resizable = false, Height = "301px" };
            var dialogTitle = $"PRINT STUDENT ID CARD";

            await DialogService.OpenAsync<StudentIDCard>(dialogTitle, null, dialogOptions);
        }
        //public async Task OpenExamsPerClassExportDialog()
        //{
        //    if (DialogService == null)
        //    {
        //        Console.WriteLine("DialogService is null! Make sure it's registered in Program.cs.");
        //        return;
        //    }

        //    var dialogOptions = new DialogOptions() { Draggable = false, ShowClose = true, CloseDialogOnEsc = true, Width = "600px", Resizable = false, Height = "301px" };
        //    var dialogTitle = $"EXPORT TO PDF";

        //    await DialogService.OpenAsync<Print_Per_Class>(dialogTitle, null, dialogOptions);
        //}
    }
}
