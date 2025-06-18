function downloadPdf(fileName, base64Csv, companyName, schoolName, logoBase64) {
    const { jsPDF } = window.jspdf;
    const doc = new jsPDF();

    const decodedCsv = atob(base64Csv);
    const lines = decodedCsv.split('\n');

    const csvHeaders = lines[0].split(',');
    const tableData = lines.slice(1).filter(line => line.trim() !== '').map(line => line.split(','));
    const termId = tableData.length > 0 ? tableData[0][5] : '';

    let yOffset = 10;

    // === Company Logo with Border ===
    if (logoBase64) {
        doc.setDrawColor(0);
        doc.setLineWidth(0.3);
        doc.rect(82, yOffset, 46, 28);
        doc.addImage(data: image / png; base64, ${ logoBase64 }, 'PNG', 85, yOffset + 1.5, 40, 25);
        yOffset += 35;
    }

    // === Colored Company Name Title Bar ===
    doc.setFillColor(41, 128, 185);
    doc.rect(20, yOffset, 170, 12, 'F');
    doc.setFontSize(16);
    doc.setTextColor(255);
    doc.setFont('helvetica', 'bold');
    doc.text(companyName.toUpperCase(), 105, yOffset + 8, { align: 'center' });

    yOffset += 18;

    // === Report Title ===
    doc.setFontSize(14);
    doc.setTextColor(0);
    doc.setFont('helvetica', 'bold');
    doc.text('Student Fee Report'.toUpperCase(), 105, yOffset, { align: 'center' });

    yOffset += 10;
    doc.setFont('helvetica', 'normal');
    doc.setFontSize(10);
    doc.text('Generated on: ' + new Date().toLocaleString(), 20, yOffset);
    doc.text('Term: ' + termId, 160, yOffset, { align: 'right' });

    yOffset += 5;
    doc.setDrawColor(200);
    doc.line(20, yOffset, 190, yOffset);

    // === Table Headers ===
    const headers = ['Student Name', 'Fee Type', 'Class', 'Amount Paid', 'Amount Left'];
    const columnWidths = [60, 40, 30, 30, 30];
    const tableX = 20;
    const tableY = yOffset + 5;

    doc.setFont('helvetica', 'bold');
    doc.setFillColor(230, 230, 230);
    doc.rect(tableX, tableY - 5, columnWidths.reduce((a, b) => a + b), 8, 'F');

    let currentX = tableX;
    headers.forEach((header, index) => {
        doc.setTextColor(0);
        doc.text(header, currentX + columnWidths[index] / 2, tableY, { align: 'center' });
        currentX += columnWidths[index];
    });

    doc.setDrawColor(100);
    doc.line(tableX, tableY + 2, tableX + columnWidths.reduce((a, b) => a + b), tableY + 2);

    // === Table Rows ===
    doc.setFont('helvetica', 'normal');
    let rowY = tableY + 10;
    let totalPaid = 0;
    let totalLeft = 0;

    tableData.forEach((row) => {
        let rowX = tableX;
        const paid = parseFloat(row[3]) || 0;
        const left = parseFloat(row[4]) || 0;
        totalPaid += paid;
        totalLeft += left;

        for (let i = 0; i < headers.length; i++) {
            const cell = row[i] !== undefined ? row[i] : '';
            doc.setTextColor(0);
            doc.text(String(cell), rowX + columnWidths[i] / 2, rowY, { align: 'center' });
            rowX += columnWidths[i];
        }
        rowY += 10;
    });

    doc.setDrawColor(150);
    doc.line(20, rowY + 5, 190, rowY + 5);

    // === Totals Box ===
    rowY += 15;
    doc.setDrawColor(0);
    doc.setFillColor(245, 245, 245);
    doc.rect(20, rowY - 8, 170, 20, 'F');

    doc.setFont('helvetica', 'bold');
    doc.setFontSize(12);
    doc.setTextColor(0);
    doc.text(`Total Amount Paid: GH₵ ${totalPaid.toFixed(2)}`, 25, rowY);
    doc.text(`Total Amount Left: GH₵ ${totalLeft.toFixed(2)}`, 25, rowY + 8);

    // Extract and Show Expected Fee Amounts *right here*
    let expectedFeeLines = [];
    if (lines[1] && lines[1].startsWith("Expected Fee Amounts:")) {
        let i = 2;
        while (i < lines.length && lines[i].trim() !== "") {
            expectedFeeLines.push(lines[i]);
            i++;
        }
        i++; // skip blank line
        lines.splice(0, i); // remove the expected block from main CSV
    }

    if (expectedFeeLines.length > 0) {
        rowY += 25;
        doc.setFont('helvetica', 'bold');
        doc.setFontSize(11);
        doc.text("Expected Fee Amounts", 20, rowY);
        rowY += 7;

        doc.setFont('helvetica', 'normal');
        doc.setFontSize(10);
        expectedFeeLines.forEach(line => {
            doc.text(line, 25, rowY);
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