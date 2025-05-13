function downloadPdf(fileName, base64Csv) {
    const { jsPDF } = window.jspdf;
    const doc = new jsPDF();

    const decodedCsv = atob(base64Csv);
    const lines = decodedCsv.split('\n').filter(line => line.trim() !== '');

    // Skip title and blank line
    const feeLines = lines.slice(2);

    const headers = ['Student Name', 'Fee Type', 'Class', 'Amount Paid', 'Amount Left'];
    const columnWidths = [60, 40, 30, 30, 30];
    const tableX = 20;
    const headerY = 50;

    // Group data by FeeTypeName
    const groupedData = {};

    feeLines.forEach(line => {
        const columns = line.split(',');
        const feeType = columns[1]?.trim();
        if (!groupedData[feeType]) groupedData[feeType] = [];
        groupedData[feeType].push(columns);
    });

    let isFirstPage = true;

    Object.entries(groupedData).forEach(([feeType, rows], groupIndex) => {
        if (!isFirstPage) {
            doc.addPage();
        }
        isFirstPage = false;

        // Header
        doc.setFont('helvetica', 'bold');
        doc.setFontSize(18);
        doc.text(`Fee Report - ${feeType}`, 105, 20, { align: 'center' });

        doc.setFontSize(12);
        doc.setFont('helvetica', 'normal');
        doc.text('Generated on: ' + new Date().toLocaleString(), 20, 30);

        // Draw headers
        doc.setFont('helvetica', 'bold');
        let currentX = tableX;
        headers.forEach((header, index) => {
            doc.text(header, currentX + columnWidths[index] / 2, headerY, { align: 'center' });
            currentX += columnWidths[index];
        });

        doc.line(tableX, headerY + 2, tableX + columnWidths.reduce((a, b) => a + b), headerY + 2);

        // Draw rows
        doc.setFont('helvetica', 'normal');
        let rowY = headerY + 10;

        rows.forEach(row => {
            let rowX = tableX;
            for (let index = 0; index < headers.length; index++) {
                const cell = row[index]?.trim() ?? '';
                doc.text(String(cell), rowX + columnWidths[index] / 2, rowY, { align: 'center' });
                rowX += columnWidths[index];
            }
            rowY += 10;
        });
    });

    doc.save(fileName);
}
