function downloadPdf(fileName, base64Csv, companyName, schoolName, logoBase64) {
    const { jsPDF } = window.jspdf;
    const doc = new jsPDF();

    const decodedCsv = atob(base64Csv);
    const lines = decodedCsv.split('\n');
    const csvHeaders = lines[0].split(',');
    const tableData = lines.slice(1).filter(line => line.trim() !== '').map(line => line.split(','));

    const termId = tableData.length > 0 ? tableData[0][5] : '';

    let yOffset = 10;

    // === Company Logo ===
    if (logoBase64) {
        // Centered image: x=85 (PDF width 210 - image width 40) / 2
        doc.addImage(`data:image/png;base64,${logoBase64}`, 'PNG', 85, yOffset, 40, 25);
        yOffset += 30;
    }

    // === Company Name ===
    doc.setFont('helvetica', 'bold');
    doc.setFontSize(24);
    doc.text(companyName, 105, yOffset, { align: 'center' });

    // === Report Title ===
    yOffset += 10;
    doc.setFontSize(18);
    doc.text('Student Fee Report', 105, yOffset, { align: 'center' });

    // === Metadata ===
    yOffset += 10;
    doc.setFontSize(12);
    doc.setFont('helvetica', 'normal');
    doc.text('Generated on: ' + new Date().toLocaleString(), 20, yOffset);
    doc.text('Term: ' + termId, 20, yOffset + 7);
    doc.line(20, yOffset + 12, 190, yOffset + 12);

    // === Table Headers ===
    const headers = ['Student Name', 'Fee Type', 'Class', 'Amount Paid', 'Amount Left'];
    const columnWidths = [60, 40, 30, 30, 30];
    const tableX = 20;
    const tableY = yOffset + 17;

    doc.setFont('helvetica', 'bold');
    let currentX = tableX;
    headers.forEach((header, index) => {
        doc.text(header, currentX + columnWidths[index] / 2, tableY, { align: 'center' });
        currentX += columnWidths[index];
    });

    doc.line(tableX, tableY + 2, tableX + columnWidths.reduce((a, b) => a + b), tableY + 2);

    // === Table Rows ===
    doc.setFont('helvetica', 'normal');
    let rowY = tableY + 10;
    let totalPaid = 0;
    let totalLeft = 0;

    tableData.forEach(row => {
        let rowX = tableX;

        const paid = parseFloat(row[3]) || 0;
        const left = parseFloat(row[4]) || 0;
        totalPaid += paid;
        totalLeft += left;

        for (let i = 0; i < headers.length; i++) {
            const cell = row[i] !== undefined ? row[i] : '';
            doc.text(String(cell), rowX + columnWidths[i] / 2, rowY, { align: 'center' });
            rowX += columnWidths[i];
        }
        rowY += 10;
    });

    doc.line(20, rowY + 5, 190, rowY + 5); // Bottom border after table

    // === Totals ===
    rowY += 15;
    doc.setFont('helvetica', 'bold');
    doc.setFontSize(14);
    doc.text(`Total Amount Paid: GH₵ ${totalPaid.toFixed(2)}`, 20, rowY);
    doc.text(`Total Amount Left to be Paid: GH₵ ${totalLeft.toFixed(2)}`, 20, rowY + 10);

    // === School Name at Bottom ===
    doc.setFontSize(14);
    doc.setFont('helvetica', 'bold');
    doc.text(schoolName, 105, 290, { align: 'center' });

    // === Save PDF ===
    doc.save(fileName);
}
