function downloadPdfs(fileName, reports, companyName, schoolName, logoBase64, paymentDate) {
    const { jsPDF } = window.jspdf;
    const doc = new jsPDF();

    let isFirstPage = true;

    reports.forEach((report) => {
        const decodedCsv = atob(report.feeDataCsv);
        const lines = decodedCsv.split('\n');

        // === Extract Expected Fee Amounts ===
        const expectedFeeLines = [];
        const expectedIndex = lines.findIndex(line => line.trim() === "Expected Fee Amounts:");
        if (expectedIndex !== -1) {
            let i = expectedIndex + 1;
            while (i < lines.length && lines[i].trim() !== "") {
                expectedFeeLines.push(lines[i].trim());
                i++;
            }
            lines.splice(expectedIndex, i - expectedIndex);
        }

        // === Parse Table Data ===
        const tableData = lines
            .filter(line => line.trim() !== "")
            .map(line => line.split(','))
            .filter(row => row.length >= 5);

        if (tableData.length === 0) return;

        if (!isFirstPage) doc.addPage();
        isFirstPage = false;

        let yOffset = 10;

        // === Logo Section ===
        if (logoBase64) {
            doc.setDrawColor(0);
            doc.setLineWidth(0.3);
            doc.rect(82, yOffset, 46, 28);
            doc.addImage(`data:image/png;base64,${logoBase64}`, 'PNG', 85, yOffset + 1.5, 40, 25);
            yOffset += 35;
        }

        // === Header Bar with Contact Info under Company Name ===
        doc.setFillColor(41, 128, 185);
        doc.rect(20, yOffset, 170, 20, 'F'); // increased height for two lines
        doc.setFont('helvetica', 'bold');
        doc.setFontSize(14);
        doc.setTextColor(255);
        doc.text(companyName.toUpperCase(), 105, yOffset + 7, { align: 'center' });

        doc.setFontSize(10);
        doc.text("Contact: +233 24 045 0421 / +233 20 642 9971", 105, yOffset + 14.5, { align: 'center' });

        yOffset += 26;

        // === Report Title & Info ===
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
        doc.text('Payment Date: ' + paymentDate, 105, yOffset, { align: 'center' });

        yOffset += 5;
        doc.line(20, yOffset, 190, yOffset);

        // === Table Header ===
        const headers = ['Student Name', 'Fee Type', 'Class', 'Amount Paid', 'Amount Left'];
        const widths = [60, 40, 30, 30, 30];
        let rowY = yOffset + 15;
        let totalPaid = 0, totalLeft = 0;

        doc.setFont('helvetica', 'bold');
        doc.setFillColor(230, 230, 230);
        doc.rect(20, rowY - 5, widths.reduce((a, b) => a + b), 8, 'F');

        let currentX = 20;
        headers.forEach((header, i) => {
            doc.text(header, currentX + widths[i] / 2, rowY, { align: 'center' });
            currentX += widths[i];
        });

        // === Table Rows ===
        rowY += 10;
        doc.setFont('helvetica', 'normal');

        let totalPaid = 0;
        let feeTypeOutstandingMap = new Map();

        tableData.forEach(row => {
            let rowX = 20;
            const feeType = row[1]?.trim() ?? "";
            const paid = parseFloat(row[3]) || 0;
            const left = parseFloat(row[4]) || 0;
            totalPaid += paid;

            // Only keep the **latest or minimum** AmountLeft for each fee type
            if (!feeTypeOutstandingMap.has(feeType)) {
                feeTypeOutstandingMap.set(feeType, left);
            } else {
                // Choose the minimum outstanding (i.e. zero if paid up)
                const current = feeTypeOutstandingMap.get(feeType);
                feeTypeOutstandingMap.set(feeType, Math.min(current, left));
            }

            for (let i = 0; i < headers.length; i++) {
                const text = row[i] || '';
                doc.text(String(text), rowX + widths[i] / 2, rowY, { align: 'center' });
                rowX += widths[i];
            }
            rowY += 10;
        });

        let totalLeft = 0;
        for (const val of feeTypeOutstandingMap.values()) {
            totalLeft += val;
        }

        // === Totals Section ===
        rowY += 10;
        doc.setFont('helvetica', 'bold');
        doc.setFontSize(11);
        doc.setTextColor(34, 153, 84);
        doc.text(`TOTAL PAID: GH ${totalPaid.toFixed(2)}`, 25, rowY);

        doc.setTextColor(192, 57, 43);
        doc.text(`OUTSTANDING BALANCE: GH ${totalLeft.toFixed(2)}`, 110, rowY);

        // === Expected Fees Section ===
        if (expectedFeeLines.length > 0) {
            rowY += 20;
            doc.setFont('courier', 'bold');
            doc.setFontSize(13);
            doc.setTextColor(0);
            doc.text("EXPECTED FEES AMOUNT", 105, rowY, { align: 'center' });

            rowY += 10;
            doc.setFontSize(12);
            expectedFeeLines.forEach(line => {
                doc.text(line, 105, rowY, { align: 'center' });
                rowY += 7;
            });
        }

        // === Page Space Check ===
        if (rowY > 270) {
            doc.addPage();
            rowY = 20;
        }

        // === Signature ===
        rowY += 20;
        doc.line(140, rowY, 190, rowY);
        doc.setFontSize(10);
        doc.setTextColor(0);
        doc.text('Authorized Signature', 165, rowY + 5, { align: 'center' });

        // === Footer ===
        doc.setFont('helvetica', 'bold');
        doc.setFontSize(10);
        doc.setTextColor(100);
        doc.text(schoolName, 105, 280, { align: 'center' });

    });

    doc.save(fileName);
}
