window.download_Excel_Export_Lesson_Note = function (fileName, base64Csv) {
    const { jsPDF } = window.jspdf;

    // Decode base64 CSV data
    const decodedCsv = atob(base64Csv);
    const lines = decodedCsv.split('\n').map(line => line.trim()).filter(line => line.length > 0); // Remove empty lines

    if (lines.length < 2) {
        alert("No valid data found!");
        return;
    }

    const csvHeaders = lines[0].split(',').map(h => h.trim()); // Extract headers
    const tableData = lines.slice(1).map(line => line.split(',').map(value => (value && value.trim()) || '-'));

    const doc = new jsPDF();
    const pageWidth = doc.internal.pageSize.getWidth();
    const margin = 20;
    let y = 20;

    // Title
    doc.setFont('helvetica', 'bold');
    doc.setFontSize(18);
    doc.text('LESSON NOTES REPORT', pageWidth / 2, y, { align: 'center' });
    y += 12;

    // Calculate dynamic column width
    const availableWidth = pageWidth - (2 * margin);
    const colWidth = availableWidth / csvHeaders.length;

    // Draw Table Headers
    doc.setFontSize(10);
    let currentX = margin;
    csvHeaders.forEach(header => {
        doc.text(header, currentX + colWidth / 2, y, { align: 'center' });
        currentX += colWidth;
    });
    y += 6;
    doc.setLineWidth(0.5).line(margin, y, pageWidth - margin, y);
    y += 6;

    // Draw Table Rows
    doc.setFont('helvetica', 'normal');
    tableData.forEach(row => {
        currentX = margin;
        row.forEach((cell, index) => {
            const cellValue = (cell && cell.trim()) || '-';
            doc.text(cellValue.toString(), currentX + colWidth / 2, y, { align: 'center' });
            currentX += colWidth;
        });
        y += 8;

        // Auto-add new page if needed
        if (y > doc.internal.pageSize.getHeight() - 20) {
            doc.addPage();
            y = 20;
        }
    });

    // Save the PDF
    doc.save(fileName);
};
