function downloadPdf(fileName, base64Csv) {
    const { jsPDF } = window.jspdf; // Access jsPDF from the window object
    const doc = new jsPDF();

    // Decode base64 CSV data and split it into lines
    const decodedCsv = atob(base64Csv);
    const lines = decodedCsv.split('\n');

    // Extract headers and rows
    const csvHeaders = lines[0].split(','); // Extract the first line as headers
    const tableData = lines.slice(1) // Skip the headers for data rows
        .filter(line => line.trim() !== '') // Exclude any empty lines
        .map(line => line.split(',')); // Split rows into cells

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
    doc.line(20, 35, 190, 35); // Horizontal line below the title

    // Define table headers and column widths
    const headers = ['Student Name', 'Fee Type', 'Class ID', 'Amount Paid', 'Amount Left'];
    const columnWidths = [60, 40, 30, 30, 30]; // Adjust column widths for better fit

    // Table positioning
    const tableX = 20;
    const tableY = 40;

    // Draw the table header
    doc.setFont('helvetica', 'bold');
    let currentX = tableX;
    headers.forEach((header, index) => {
        doc.text(header, currentX + columnWidths[index] / 2, tableY, { align: 'center' });
        currentX += columnWidths[index];
    });

    // Add a line below the header
    doc.line(tableX, tableY + 2, tableX + columnWidths.reduce((a, b) => a + b), tableY + 2);

    // Draw the table rows
    doc.setFont('helvetica', 'normal');
    let rowY = tableY + 10; // Start from the next line after the header
    tableData.forEach(row => {
        let rowX = tableX;
        row.forEach((cell, index) => {
            doc.text(cell, rowX + columnWidths[index] / 2, rowY, { align: 'center' });
            rowX += columnWidths[index];
        });
        rowY += 10; // Space between rows
    });

    // Add a footer line
    doc.setLineWidth(0.5);
    doc.line(20, rowY + 10, 190, rowY + 10); // Horizontal line at the bottom

    // Save the PDF
    doc.save(fileName);
}
