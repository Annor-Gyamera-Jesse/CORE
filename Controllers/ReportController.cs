using System.Data;
using FastReport;
using FastReport.Export.Pdf;
using Microsoft.AspNetCore.Mvc;

namespace CORE.Controllers
{
    public partial class ReportController : Controller
    {

        [HttpGet("/printreport")]
        public IActionResult PrintReport()
        {
            // Fetch data from your server-side data source
            DataSet dataSet = FetchData();

            // Generate the report
            Report report = new Report();
            report.Load("ReprtPageTemplate.frx");
            report.RegisterData(dataSet, "Exams");

            // Export report to PDF (you can choose other formats as needed)
            using (MemoryStream stream = new MemoryStream())
            {
                report.Prepare();
                report.Export(new PDFExport(), stream);
                stream.Seek(0, SeekOrigin.Begin);

                return File(stream.ToArray(), "application/pdf", "exams_report.pdf");
            }
        }

        private DataSet FetchData()
        {
            // Simulated data fetching (replace with your actual data retrieval logic)
            DataSet dataSet = new DataSet();
            // Fetch data from your server-side data source and populate the DataSet
            return dataSet;
        }
    }
}
