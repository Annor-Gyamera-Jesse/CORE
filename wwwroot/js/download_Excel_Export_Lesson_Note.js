window.download_Excel_Export_Lesson_Note = function (
    fileName,
    base64Csv
) {
    const { jsPDF } = window.jspdf;

    // Decode base64 CSV data
    const decodedCsv = atob(base64Csv);
    const lines = decodedCsv.split('\n').map(line => line.trim()).filter(line => line.length > 0); // Remove empty lines

    if (lines.length < 2) {
        alert("No valid data found!");
        return;
    }

    const csvHeaders = lines[0]?.split(',').map(h => h.trim()) || []; // Extract column headers
    const tableData = lines.slice(1).map(line => line.split(',').map(value => (value && value.trim()) || '-')); // Ensure no null or undefined values

    const doc = new jsPDF();
    const W = doc.internal.pageSize.getWidth();
    let y = 20;

    // Title
    doc.setFontSize(18).setFont('helvetica', 'bold');
    doc.text('LESSON NOTES REPORT', W / 2, y, { align: 'center' });
    y += 10;

    // Draw Table Headers
    const columnWidths = [30, 30, 30, 30, 30, 30, 30, 30, 30, 30, 30, 30, 30, 30, 30, 30, 30, 30, 30, 30];
    let currentX = 20;
    doc.setFont('helvetica', 'bold');
    csvHeaders.forEach((header, index) => {
        doc.text(header, currentX + columnWidths[index] / 2, y, { align: 'center' });
        currentX += columnWidths[index];
    });
    y += 6;
    doc.setLineWidth(0.5).line(20, y, W - 20, y);
    y += 6;

    // Draw Rows
    doc.setFont('helvetica', 'normal');
    tableData.forEach(row => {
        currentX = 20;
        row.forEach((cell, index) => {
            const cellValue = (cell && cell.trim()) || '-'; // Ensure text is always a string
            doc.text(cellValue.toString(), currentX + columnWidths[index] / 2, y, { align: 'center' });
            currentX += columnWidths[index];
        });
        y += 8;
        if (y > doc.internal.pageSize.getHeight() - 30) {
            doc.addPage(); y = 20; // Add a new page if needed
        }
    });

    // Save the PDF
    doc.save(fileName);
};