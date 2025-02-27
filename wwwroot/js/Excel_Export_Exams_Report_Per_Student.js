function downloadExcel_Export_Exams_Report_Per_StudentPdf(fileName, base64Csv) {
    const { jsPDF } = window.jspdf;
    const doc = new jsPDF();

    // Decode base64 CSV data
    const decodedCsv = atob(base64Csv);
    const lines = decodedCsv.split('\n').map(line => line.trim()).filter(line => line);

    // Validate CSV structure
    if (lines.length < 2) {
        alert("Error: CSV file is empty or incorrectly formatted.");
        return;
    }

    // Extract headers and rows
    const csvHeaders = lines[0].split(',').map(h => h.trim() || '_');
    const tableData = lines.slice(1).map(line => line.split(',').map(cell => cell.trim() || '_'));

    // Set document title
    doc.setFont('helvetica', 'bold');
    doc.setFontSize(20);
    doc.text('STUDENT EXAM REPORT', 105, 20, { align: 'center' });

    doc.setFontSize(12);
    doc.setFont('helvetica', 'normal');
    doc.text(`Generated on: ${new Date().toLocaleString()}`, 20, 30);

    // Extract first row as student details
    const studentDetails = tableData[0] || Array(21).fill('_');

    // Student Information
    let yPos = 40;
    const details = [
        `Student Name: ${studentDetails[0]}`,
        `Class: ${studentDetails[1]}`,
        `Academic Year: ${studentDetails[2]}`,
        `Vacation Date: ${studentDetails[3]}`,
        `Promoted To: ${studentDetails[4]}`,
        `Number on Roll: ${studentDetails[5]}`,
        `Term: ${studentDetails[6]}`,
        `Position: ${studentDetails[7]}`,
        `Next Term Begins: ${studentDetails[8]}`
    ];

    details.forEach(detail => {
        doc.text(detail, 20, yPos);
        yPos += 7;
    });

    yPos += 5;

    // Draw table headers
    const headers = ['School Course', 'Class Score', 'Exams Score', 'Total Score', 'Subjects Position', 'Grade'];
    const columnWidths = [50, 30, 30, 30, 30, 20];

    let currentX = 20;
    doc.setFont('helvetica', 'bold');
    headers.forEach((header, index) => {
        doc.text(header, currentX + columnWidths[index] / 2, yPos, { align: 'center' });
        currentX += columnWidths[index];
    });

    // Draw table rows
    doc.setFont('helvetica', 'normal');
    let rowY = yPos + 10;
    tableData.slice(1).forEach(row => {
        let rowX = 20;
        for (let i = 9; i < Math.min(row.length, headers.length + 9); i++) {
            doc.text(row[i], rowX + columnWidths[i - 9] / 2, rowY, { align: 'center' });
            rowX += columnWidths[i - 9];
        }
        rowY += 10;
    });

    // Footer Section
    rowY += 10;
    doc.setFont('helvetica', 'bold');
    const footerDetails = [
        'Teacher’s Remarks:', studentDetails[15] || '_',
        'Conduct:', studentDetails[16] || '_',
        'Headmaster’s Remark:', studentDetails[17] || '_',
        'School Information:', studentDetails[18] || '_',
        'Teacher’s Signature:', studentDetails[19] || '_',
        'Headmaster’s Signature:', studentDetails[20] || '_'
    ];

    for (let i = 0; i < footerDetails.length; i += 2) {
        doc.text(footerDetails[i], 20, rowY);
        doc.text(footerDetails[i + 1], 80, rowY);
        rowY += 7;
    }

    // Save the PDF
    doc.save(fileName);
}
