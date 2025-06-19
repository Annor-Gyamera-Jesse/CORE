function downloadPdfForMultipleStudents(fileName, reports, companyName, schoolName, logoBase64) {
    const { jsPDF } = window.jspdf;
    const doc = new jsPDF();

    let isFirstPage = true;

    reports.forEach((report) => {
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

        // Extract table data (skip empty lines and expected fees block)
        const tableData = lines
            .filter(line => line.trim() !== "" && !line.trim().startsWith("Expected Fee Amounts:"))
            .map(line => line.split(','))
            .filter(row => row.length >= 5);

        if (tableData.length === 0) {
            return; // Skip blank reports
        }

        if (!isFirstPage) {
            doc.addPage();
        }
        isFirstPage = false;

        let yOffset = 10;

        // === Logo ===
        if (logoBase64) {
            doc.setDrawColor(0);
            doc.setLineWidth(0.3);
            doc.rect(82, yOffset, 46, 28);
            doc.addImage(`data:image/png;base64,${logoBase64}`, 'PNG', 85, yOffset + 1.5, 40, 25);
            yOffset += 35;
        }

        // === Header Bar ===
        doc.setFillColor(41, 128, 185);
        doc.rect(20, yOffset, 170, 12, 'F');
        doc.setFontSize(16);
        doc.setTextColor(255);
        doc.setFont('helvetica', 'bold');
        doc.text(companyName.toUpperCase(), 105, yOffset + 8, { align: 'center' });

        yOffset += 18;

        // === Title and Student Name ===
        doc.setFontSize(14);
        doc.setTextColor(0);
        doc.text('STUDENT FEE REPORT', 105, yOffset, { align: 'center' });

        yOffset += 8;
        doc.setFontSize(12);
        doc.text(`Student: ${report.studentName}`, 105, yOffset, { align: 'center' });

        yOffset += 10;
        doc.setFontSize(10);
        doc.setFont('helvetica', 'normal');
        doc.text('Generated on: ' + new Date().toLocaleString(), 20, yOffset);
        doc.text('Term: ' + report.termId, 190, yOffset, { align: 'right' });

        yOffset += 5;
        doc.setDrawColor(200);
        doc.line(20, yOffset, 190, yOffset);

        // === Table Headers ===
        const headers = ['Student Name', 'Fee Type', 'Class', 'Amount Paid', 'Amount Left'];
        const columnWidths = [60, 40, 30, 30, 30];
        let rowY = yOffset + 10;
        let totalPaid = 0;
        let totalLeft = 0;

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

        // === Totals Box ===
        rowY += 10;
        doc.setFont('helvetica', 'bold');
        doc.setFontSize(11);
        doc.text(`Total Paid: GH ${totalPaid.toFixed(2)}`, 25, rowY);
        // doc.text(`Total Left: GH ${totalLeft.toFixed(2)}`, 25, rowY + 7);

        // === Expected Fees Section ===
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

        // === Signature Line ===
        rowY += 20;
        doc.line(140, rowY, 190, rowY);
        doc.setFontSize(10);
        doc.text('Authorized Signature', 165, rowY + 5, { align: 'center' });

        // === Footer ===
        doc.setFont('helvetica', 'bold');
        doc.setFontSize(10);
        doc.setTextColor(100);
        doc.text(schoolName, 105, 290, { align: 'center' });
    });

    doc.save(fileName);
}
