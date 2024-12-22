function downloadPdf(fileName, base64Csv) {
    const { jsPDF } = window.jspdf;
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
    doc.setFontSize(22);
    doc.text('Teacher Attendance Report', 105, 20, { align: 'center' });

    // Add space after the title
    doc.setFontSize(12);
    doc.setFont('helvetica', 'normal');
    doc.text('Generated on: ' + new Date().toLocaleString(), 20, 30);

    // Add a line separator
    doc.setLineWidth(0.5);
    doc.line(20, 35, 190, 35); // Horizontal line below the title

    // Define table headers and column widths
    const headers = ['Teacher Name', 'Clock-In Time', 'Clock-Out Time', 'Attendance Status'];
    const columnWidths = [60, 40, 40, 50]; // Adjust column widths for better fit

    // Table positioning
    const tableX = 20;
    const tableY = 45;

    // Draw the table header with styling
    doc.setFont('helvetica', 'bold');
    let currentX = tableX;
    headers.forEach((header, index) => {
        doc.setFillColor(100, 100, 255); // Background color for the header
        doc.rect(currentX, tableY, columnWidths[index], 10, 'F'); // Draw a rectangle for the header
        doc.setTextColor(255, 255, 255); // Set text color to white
        doc.text(header, currentX + columnWidths[index] / 2, tableY + 6, { align: 'center' });
        currentX += columnWidths[index];
    });

    // Add a line below the header
    doc.setLineWidth(0.5);
    doc.line(tableX, tableY + 10, tableX + columnWidths.reduce((a, b) => a + b), tableY + 10);

    // Draw the table rows with alternating colors
    doc.setFont('helvetica', 'normal');
    let rowY = tableY + 12; // Start from the next line after the header
    let isOddRow = true; // Used for alternating row colors
    tableData.forEach(row => {
        let rowX = tableX;

        // Format Clock-In and Clock-Out times
        const clockIn = row[3] ? formatDate(row[3]) : 'N/A';
        const clockOut = row[4] ? formatDate(row[4]) : 'N/A';

        // Determine attendance status based on EnableSwitch
        const attendanceStatus = row[2] === '1' ? 'Present' : 'Absent';

        // Set alternating row colors for better readability
        if (isOddRow) {
            doc.setFillColor(240, 240, 240); // Light gray for odd rows
        } else {
            doc.setFillColor(255, 255, 255); // White for even rows
        }

        // Draw row background
        doc.rect(rowX, rowY, columnWidths[0], 10, 'F');
        doc.rect(rowX + columnWidths[0], rowY, columnWidths[1], 10, 'F');
        doc.rect(rowX + columnWidths[0] + columnWidths[1], rowY, columnWidths[2], 10, 'F');
        doc.rect(rowX + columnWidths[0] + columnWidths[1] + columnWidths[2], rowY, columnWidths[3], 10, 'F');

        // Set text color for rows
        doc.setTextColor(0, 0, 0); // Black text for rows

        // Add the data to the PDF table
        doc.text(row[1] + ' ' + row[0], rowX + columnWidths[0] / 2, rowY + 6, { align: 'center' });
        doc.text(clockIn, rowX + columnWidths[0] + columnWidths[1] / 2, rowY + 6, { align: 'center' });
        doc.text(clockOut, rowX + columnWidths[0] + columnWidths[1] + columnWidths[2] / 2, rowY + 6, { align: 'center' });
        doc.text(attendanceStatus, rowX + columnWidths[0] + columnWidths[1] + columnWidths[2] + columnWidths[3] / 2, rowY + 6, { align: 'center' });

        rowY += 12; // Space between rows
        isOddRow = !isOddRow; // Toggle row color
    });

    // Add a footer with page number
    doc.setFontSize(10);
    doc.setFont('helvetica', 'italic');
    doc.text('Page ' + doc.internal.getNumberOfPages(), 180, 285);

    // Add a footer line
    doc.setLineWidth(0.0);
    doc.line(20, rowY + 10, 190, rowY + 10); // Horizontal line at the bottom

    // Save the PDF
    doc.save(fileName);
}

// Helper function to format the date
const formatDate = (dateStr) => {
    const date = new Date(dateStr);
    return date.toLocaleString(); // Adjust this to match your desired format
};
