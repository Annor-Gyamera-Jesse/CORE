function downloadPdf(fileName, base64Csv, companyName, schoolName) {
    const { jsPDF } = window.jspdf;
    const doc = new jsPDF();

    const decodedCsv = atob(base64Csv);
    const lines = decodedCsv.split('\n');
    const csvHeaders = lines[0].split(',');
    const tableData = lines.slice(1).filter(line => line.trim() !== '').map(line => line.split(','));
    const termId = tableData.length > 0 ? tableData[0][5] : '';

    // === Display Company Name Bold and Large ===
    doc.setFont('helvetica', 'bold');
    doc.setFontSize(24);
    doc.text(companyName, 105, 15, { align: 'center' });

    // === Title and Metadata ===
    doc.setFontSize(18);
    doc.text('Student Fee Report', 105, 25, { align: 'center' });

    doc.setFontSize(12);
    doc.setFont('helvetica', 'normal');
    doc.text('Generated on: ' + new Date().toLocaleString(), 20, 35);
    doc.text('Term: ' + termId, 20, 42);
    doc.line(20, 47, 190, 47); // Line separator

    // === Table Headers ===
    const headers = ['Student Name', 'Fee Type', 'Class', 'Amount Paid', 'Amount Left'];
    const columnWidths = [60, 40, 30, 30, 30];
    const tableX = 20;
    const tableY = 52;

    doc.setFont('helvetica', 'bold');
    let currentX = tableX;
    headers.forEach((header, index) => {
        doc.text(header, currentX + columnWidths[index] / 2, tableY, { align: 'center' });
        currentX += columnWidths[index];
    });

    doc.line(tableX, tableY + 2, tableX + columnWidths.reduce((a, b) => a + b), tableY + 2);

    // === Table Data ===
    doc.setFont('helvetica', 'normal');
    let rowY = tableY + 10;
    tableData.forEach(row => {
        let rowX = tableX;
        for (let i = 0; i < headers.length; i++) {
            const cell = row[i] !== undefined ? row[i] : '';
            doc.text(String(cell), rowX + columnWidths[i] / 2, rowY, { align: 'center' });
            rowX += columnWidths[i];
        }
        rowY += 10;
    });

    doc.line(20, rowY + 10, 190, rowY + 10); // Bottom line

    // === School Name at Bottom ===
    doc.setFontSize(14);
    doc.setFont('helvetica', 'bold');
    doc.text(schoolName, 105, 290, { align: 'center' }); // Near bottom of A4

    // === Save PDF ===
    doc.save(fileName);
}
