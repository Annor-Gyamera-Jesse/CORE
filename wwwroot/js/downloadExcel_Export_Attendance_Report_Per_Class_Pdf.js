// wwwroot/js/report-export.js

window.downloadExcel_Export_Attendance_Report_Per_Class_Pdf = function (fileName, base64Csv) {
    const { jsPDF } = window.jspdf;

    // Decode & parse CSV
    const decoded = atob(base64Csv);
    const lines = decoded
        .split('\n')
        .map(l => l.trim())
        .filter(l => l.length > 0);

    if (lines.length < 2) {
        alert("No valid data found!");
        return;
    }

    const headers = lines[0].split(',').map(h => h.trim());
    const rows = lines.slice(1).map(l => l.split(',').map(c => c.trim()));

    const doc = new jsPDF();
    const pageWidth = doc.internal.pageSize.getWidth();
    let y = 20;

    // Title
    doc.setFontSize(18).setFont('helvetica', 'bold');
    doc.text('ATTENDANCE REPORT', pageWidth / 2, y, { align: 'center' });
    y += 10;

    // Meta (Class, Term, Date)
    doc.setFontSize(11).setFont('helvetica', 'normal');
    const meta = rows[0];
    const classId = meta[2];
    const term = meta[4];
    const date = meta[3];
    doc.text(`Class: ${classId}`, 20, y);
    doc.text(`Term: ${term}`, pageWidth / 2, y);
    doc.text(`Date: ${date}`, pageWidth - 60, y);
    y += 10;

    // Table header
    const colWidths = [40, 40, 40]; // adjust or compute dynamically
    let x = 20;
    doc.setFont('helvetica', 'bold');
    ['First Name', 'Last Name', 'Status'].forEach((h, i) => {
        doc.text(h, x + colWidths[i] / 2, y, { align: 'center' });
        x += colWidths[i];
    });
    y += 8;
    doc.setLineWidth(0.5).line(20, y, pageWidth - 20, y);
    y += 6;

    // Table rows
    doc.setFont('helvetica', 'normal');
    rows.forEach(r => {
        x = 20;
        const [first, last, , dateVal, , status] = r;
        [first, last, status].forEach((cell, i) => {
            doc.text(cell || '-', x + colWidths[i] / 2, y, { align: 'center' });
            x += colWidths[i];
        });
        y += 8;
        // handle page break
        if (y > doc.internal.pageSize.getHeight() - 20) {
            doc.addPage();
            y = 20;
        }
    });

    doc.save(fileName);
};
