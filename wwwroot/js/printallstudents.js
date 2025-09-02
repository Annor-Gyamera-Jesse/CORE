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

    const widths = [100, 60];
    const startX = 20;

    // Header row
    doc.setFillColor(200, 200, 200);
    doc.rect(startX, yOffset - 5, widths[0] + widths[1], 8, 'F');
    let currentX = startX;
    headers.forEach((h, i) => {
        doc.text(h.trim(), currentX + widths[i] / 2, yOffset, { align: 'center' });
        currentX += widths[i];
    });

    // Data rows
    yOffset += 10;
    doc.setFont('helvetica', 'normal');
    dataRows.forEach(row => {
        let rowX = startX;
        row.forEach((cell, i) => {
            doc.text(cell.trim(), rowX + widths[i] / 2, yOffset, { align: 'center' });
            rowX += widths[i];
        });
        yOffset += 8;
        if (yOffset > 270) {
            doc.addPage();
            yOffset = 20;
        }
    });

    // === Fees Section ===
    yOffset += 15;
    doc.setFont('helvetica', 'bold');
    doc.setFontSize(12);
    doc.text("Fee Structure", 105, yOffset, { align: 'center' });
    yOffset += 8;

    const fees = classFees[report.classId.toUpperCase()] || [];
    if (fees.length > 0) {
        doc.setFontSize(10);

        // Header
        doc.setFillColor(200, 200, 200);
        doc.rect(startX, yOffset - 5, 120, 8, 'F');
        doc.rect(startX + 120, yOffset - 5, 40, 8, 'F');
        doc.text("ITEM", startX + 60, yOffset, { align: 'center' });
        doc.text("AMOUNT (GHS)", startX + 140, yOffset, { align: 'center' });
        yOffset += 10;

        let total = 0;
        fees.forEach(fee => {
            doc.text(fee.item, startX + 2, yOffset);
            doc.text(fee.amount.toString(), startX + 140, yOffset, { align: 'center' });
            total += fee.amount;
            yOffset += 8;
            if (yOffset > 270) {
                doc.addPage();
                yOffset = 20;
            }
        });

        // Total row
        doc.setFont('helvetica', 'bold');
        doc.text("TOTAL", startX + 2, yOffset);
        doc.text(total.toString(), startX + 140, yOffset, { align: 'center' });
    }

    doc.save(fileName);
}
