function downloadPdf(fileName, base64Csv) {
    const { jsPDF } = window.jspdf;  // Accessing jsPDF from the window object
    const doc = new jsPDF();

    // Decode base64 CSV data and split it into lines
    const decodedCsv = atob(base64Csv);
    const lines = decodedCsv.split('\n');

    // Set document font and title
    doc.setFont('helvetica', 'bold');
    doc.setFontSize(20);
    doc.text('Student Fee Report', 105, 20, { align: 'center' });

    // Add space after the title
    doc.setFontSize(12);
    doc.setFont('helvetica', 'normal');
    doc.text('Generated on: ' + new Date().toLocaleString(), 20, 30);

    // Add a line separator
    doc.setLineWidth(0.5);
    doc.line(20, 35, 190, 35);  // Horizontal line at the top

    // Table headers
    const headers = ['Student Name', 'Fee Type', 'Class ID', 'Amount Paid', 'Amount Left'];
    const tableData = [];
    lines.slice(2).forEach(line => {  // Skip header line (already added above)
        if (line) {
            const row = line.split(',');
            tableData.push(row);
        }
    });

    // Define table position and column widths
    const tableX = 20;
    const tableY = 40;
    const columnWidths = [60, 40, 30, 30, 30];  // Adjust column widths for better fit
    const cellPadding = 5;

    // Draw the table header
    doc.setFont('helvetica', 'bold');
    for (let i = 0; i < headers.length; i++) {
        doc.text(headers[i], tableX + columnWidths[i] / 2, tableY, { align: 'center' });
    }

    // Draw the table rows
    doc.setFont('helvetica', 'normal');
    let rowY = tableY + 10;  // Start from the next line after the header
    tableData.forEach(row => {
        let rowX = tableX;
        row.forEach((cell, index) => {
            doc.text(cell, rowX + columnWidths[index] / 2, rowY, { align: 'center' });
            rowX += columnWidths[index];
        });
        rowY += 10;  // Space between rows
    });

    // Add a footer line
    doc.setLineWidth(0.5);
    doc.line(20, rowY + 10, 190, rowY + 10);  // Horizontal line at the bottom

    // Save the PDF
    doc.save(fileName);
}
