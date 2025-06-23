function downloadPdf(fileName, base64Csv, companyName, schoolName, logoBase64) {
    const { jsPDF } = window.jspdf;
    const doc = new jsPDF();

    const decodedCsv = atob(base64Csv);
    const lines = decodedCsv.split('\n');

    // Extract expected fee lines from the bottom
    const expectedStartIndex = lines.findIndex(line => line.trim() === "Expected Fee Amounts:");
    const expectedFeeLines = expectedStartIndex !== -1 ? lines.slice(expectedStartIndex + 1) : [];
    const dataLines = expectedStartIndex !== -1 ? lines.slice(0, expectedStartIndex) : lines;

    const tableData = dataLines
        .filter(line => line.trim() !== '')
        .map(line => line.split(',').map(cell => cell.trim()));

    const termId = tableData.length > 0 ? tableData[0][5] : '';
    let yOffset = 10;

    // === Logo with border ===
    if (logoBase64) {
        doc.setDrawColor(0);
        doc.setLineWidth(0.3);
        doc.rect(82, yOffset, 46, 28);
        doc.addImage(`data:image/png;base64,${logoBase64}`, 'PNG', 85, yOffset + 1.5, 40, 25);
        yOffset += 35;
    }

    // === Company Header with Contact Info ===
    doc.setFillColor(41, 128, 185);
    doc.rect(20, yOffset, 170, 20, 'F'); // taller for contact line
    doc.setFontSize(14);
    doc.setTextColor(255);
    doc.setFont('helvetica', 'bold');
    doc.text(companyName.toUpperCase(), 105, yOffset + 7, { align: 'center' });

    doc.setFontSize(10);
    doc.text("Contact: +233 24 045 0421 / +233 20 642 9971", 105, yOffset + 14.5, { align: 'center' });

    yOffset += 26;

    // === Report Metadata ===
    doc.setTextColor(0);
    doc.setFontSize(12);
    doc.setFont('helvetica', 'bold');
    doc.text("STUDENT FEE REPORT", 105, yOffset, { align: 'center' });

    yOffset += 10;
    doc.setFont('helvetica', 'normal');
    doc.setFontSize(10);
    doc.text("Generated on: " + new Date().toLocaleString(), 20, yOffset);
    doc.text("Term: " + termId, 190, yOffset, { align: 'right' });

    yOffset += 5;
    doc.setDrawColor(200);
    doc.line(20, yOffset, 190, yOffset);

    // === Table Headers ===
    const headers = ['Student Name', 'Fee Type', 'Class', 'Amount Paid', 'Amount Left'];
    const columnWidths = [60, 40, 30, 30, 30];
    let rowY = yOffset + 10;

    doc.setFont('helvetica', 'bold');
    doc.setFillColor(230, 230, 230);
    doc.rect(20, rowY - 5, 170, 8, 'F');

    let colX = 20;
    headers.forEach((header, i) => {
        doc.setTextColor(0);
        doc.text(header, colX + columnWidths[i] / 2, rowY, { align: 'center' });
        colX += columnWidths[i];
    });

    // === Table Rows ===
    doc.setFont('helvetica', 'normal');
    rowY += 10;
    let totalPaid = 0;
    let totalLeft = 0;

    tableData.forEach(row => {
        let rowX = 20;
        const paid = parseFloat(row[3]) || 0;
        const left = parseFloat(row[4]) || 0;
        totalPaid += paid;
        totalLeft += left;

        headers.forEach((_, i) => {
            const cell = row[i] || '';
            doc.text(String(cell), rowX + columnWidths[i] / 2, rowY, { align: 'center' });
            rowX += columnWidths[i];
        });

        rowY += 10;
    });

    // === Totals Box ===
    rowY += 10;
    doc.setFont('helvetica', 'bold');
    doc.setFontSize(11);
    doc.setTextColor(34, 153, 84);
    doc.text(`Total Paid: GH ${totalPaid.toFixed(2)}`, 25, rowY);

    doc.setTextColor(192, 57, 43);
    doc.text(`Outstanding Balance: GH ${totalLeft.toFixed(2)}`, 120, rowY);

    // === Expected Fee Section ===
    if (expectedFeeLines.length > 0) {
        rowY += 20;
        doc.setFont('helvetica', 'bold');
        doc.setFontSize(12);
        doc.setTextColor(0);
        doc.text("EXPECTED FEE AMOUNTS", 105, rowY, { align: 'center' });

        rowY += 10;
        doc.setFont('helvetica', 'normal');
        doc.setFontSize(10);

        expectedFeeLines.forEach(line => {
            doc.text(line.trim(), 105, rowY, { align: 'center' });
            rowY += 6;
        });
    }

    // === Signature Line ===
    rowY += 20;
    doc.setDrawColor(100);
    doc.line(140, rowY, 190, rowY);
    doc.setFontSize(10);
    doc.text('Authorized Signature', 165, rowY + 5, { align: 'center' });

    // === Footer ===
    doc.setFont('helvetica', 'bold');
    doc.setFontSize(10);
    doc.setTextColor(100);
    doc.text(schoolName, 105, 290, { align: 'center' });

    doc.save(fileName);
}
