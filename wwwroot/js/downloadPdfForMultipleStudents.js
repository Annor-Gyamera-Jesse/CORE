function downloadPdfForMultipleStudents(fileName, reports, companyName, schoolName, logoBase64) {
    const { jsPDF } = window.jspdf;
    const doc = new jsPDF();

    reports.forEach((report, index) => {
        if (index > 0) doc.addPage(); // New page per student

        const decodedCsv = atob(report.feeDataCsv);
        const lines = decodedCsv.split('\n');

        // Extract expected fee lines
        let expectedFeeLines = [];
        const expectedStartIndex = lines.findIndex(line => line.trim() === "Expected Fee Amounts:");
        if (expectedStartIndex !== -1) {
            let i = expectedStartIndex + 1;
            while (i < lines.length && lines[i].trim() !== "") {
                expectedFeeLines.push(lines[i].trim());
                i++;
            }
            lines.splice(expectedStartIndex, i - expectedStartIndex);
        }

        const csvHeaders = lines[0].split(',');
        const tableData = lines.slice(1).filter(l => l.trim() !== "").map(l => l.split(','));

        let yOffset = 10;

        // Logo
        if (logoBase64) {
            doc.setDrawColor(0);
            doc.setLineWidth(0.3);
            doc.rect(82, yOffset, 46, 28);
            doc.addImage(`data:image/png;base64,${logoBase64}`, 'PNG', 85, yOffset + 1.5, 40, 25);
            yOffset += 35;
        }

        // Title
        doc.setFillColor(41, 128, 185);
        doc.rect(20, yOffset, 170, 12, 'F');
        doc.setFontSize(16);
        doc.setTextColor(255);
        doc.setFont('helvetica', 'bold');
        doc.text(companyName.toUpperCase(), 105, yOffset + 8, { align: 'center' });

        yOffset += 18;

        doc.setFontSize(14);
        doc.setTextColor(0);
        doc.text('Student Fee Report'.toUpperCase(), 105, yOffset, { align: 'center' });

        yOffset += 10;
        doc.setFontSize(10);
        doc.setFont('helvetica', 'normal');
        doc.text('Generated on: ' + new Date().toLocaleString(), 20, yOffset);
        doc.text('Term: ' + report.termId, 160, yOffset, { align: 'right' });

        yOffset += 5;
        doc.line(20, yOffset, 190, yOffset);

        // Table
        const headers = ['Student Name', 'Fee Type', 'Class', 'Amount Paid', 'Amount Left'];
        const columnWidths = [60, 40, 30, 30, 30];
        let rowY = yOffset + 15;
        let totalPaid = 0;
        let totalLeft = 0;

        // Header
        let currentX = 20;
        doc.setFont('helvetica', 'bold');
        doc.setFillColor(230, 230, 230);
        doc.rect(currentX, rowY - 5, columnWidths.reduce((a, b) => a + b), 8, 'F');
        headers.forEach((header, i) => {
            doc.text(header, currentX + columnWidths[i] / 2, rowY, { align: 'center' });
            currentX += columnWidths[i];
        });

        rowY += 10;
        doc.setFont('helvetica', 'normal');

        tableData.forEach(row => {
            let rowX = 20;
            const paid = parseFloat(row[3]) || 0;
            const left = parseFloat(row[4]) || 0;
            totalPaid += paid;
            totalLeft += left;

            for (let i = 0; i < headers.length; i++) {
                const cell = row[i] || '';
                doc.text(String(cell), rowX + columnWidths[i] / 2, rowY, { align: 'center' });
                rowX += columnWidths[i];
            }

            rowY += 10;
        });

        // Totals
        rowY += 10;
        doc.setFont('helvetica', 'bold');
        doc.setFontSize(11);
        doc.text(`Total Paid: GH ${totalPaid.toFixed(2)}`, 25, rowY);
        // doc.text(`Total Left: GH ${totalLeft.toFixed(2)}`, 25, rowY + 7);

        // Expected Fees block
        if (expectedFeeLines.length > 0) {
            rowY += 20;
            doc.setFont('courier', 'bold');
            doc.setFontSize(13);
            doc.text("EXPECTED FEES AMOUNT", 105, rowY, { align: 'center' });

            rowY += 10;
            doc.setFontSize(12);
            expectedFeeLines.forEach(line => {
                doc.text(line.trim(), 105, rowY, { align: 'center' });
                rowY += 7;
            });
        }

        // Signature line
        rowY += 20;
        doc.line(140, rowY, 190, rowY);
        doc.setFontSize(10);
        doc.text('Authorized Signature', 165, rowY + 5, { align: 'center' });

        // Footer
        doc.setFont('helvetica', 'bold');
        doc.setFontSize(10);
        doc.setTextColor(100);
        doc.text(schoolName, 105, 290, { align: 'center' });
    });

    doc.save(fileName);
}
