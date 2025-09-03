function downloadStudentListPdf(fileName, report, companyName, schoolName, logoBase64, generatedDate) {
    const { jsPDF } = window.jspdf;
    const doc = new jsPDF();

    let yOffset = 20;

    // === Logo ===
    if (logoBase64) {
        doc.addImage(`data:image/png;base64,${logoBase64}`, 'PNG', 85, yOffset, 40, 25);
        yOffset += 35;
    }

    // === Header ===
    doc.setFont('helvetica', 'bold');
    doc.setFontSize(14);
    doc.text(companyName.toUpperCase(), 105, yOffset, { align: 'center' });
    yOffset += 8;
    doc.text(schoolName, 105, yOffset, { align: 'center' });

    yOffset += 15;
    doc.setFontSize(12);
    doc.text(`STUDENT LIST - CLASS: ${report.classId}`, 105, yOffset, { align: 'center' });

    yOffset += 10;
    doc.setFontSize(10);
    doc.text('Generated on: ' + generatedDate, 20, yOffset);

    yOffset += 10;
    doc.line(20, yOffset, 190, yOffset);
    yOffset += 10;

    // === Student List Table ===
    const decodedCsv = atob(report.studentListCsv);
    const lines = decodedCsv.split('\n').filter(l => l.trim() !== "");
    const headers = lines[0].split(',');
    const dataRows = lines.slice(1).map(l => l.split(','));

    const widths = [120, 50]; // student name wider
    const startX = 20;

    // Header row
    doc.setFillColor(200, 200, 200);
    doc.rect(startX, yOffset - 5, widths[0], 8, 'F');
    doc.rect(startX + widths[0], yOffset - 5, widths[1], 8, 'F');
    doc.text(headers[0].trim(), startX + widths[0] / 2, yOffset, { align: 'center' });
    doc.text(headers[1].trim(), startX + widths[0] + widths[1] / 2, yOffset, { align: 'center' });

    yOffset += 10;
    doc.setFont('helvetica', 'normal');

    // Data rows
    dataRows.forEach(row => {
        if (row.length < 2) return;
        doc.text(row[0].trim(), startX + 2, yOffset); // student name (left aligned)
        doc.text(row[1].trim(), startX + widths[0] + widths[1] / 2, yOffset, { align: 'center' }); // class
        yOffset += 8;

        if (yOffset > 270) {
            doc.addPage();
            yOffset = 20;
        }
    });

    doc.save(fileName);
}
