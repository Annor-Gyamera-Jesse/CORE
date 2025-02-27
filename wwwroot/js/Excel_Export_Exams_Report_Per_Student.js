function downloadExcel_Export_Exams_Report_Per_StudentPdf(fileName, base64Csv) {
    const { jsPDF } = window.jspdf;

    // Decode base64 CSV data
    const decodedCsv = atob(base64Csv);
    const lines = decodedCsv.split('\n').map(line => line.trim()).filter(line => line.length > 0); // Remove empty lines

    if (lines.length < 2) {
        alert("No valid data found!");
        return;
    }

    const csvHeaders = lines[0]?.split(',').map(h => h.trim()) || []; // Extract column headers
    const tableData = lines.slice(1).map(line => line.split(',').map(value => (value && value.trim()) || '-'));

    // **Grouping students by name**
    const students = {};
    tableData.forEach(row => {
        const studentName = row[0] || 'Unknown Student';
        if (!students[studentName]) {
            students[studentName] = [];
        }
        students[studentName].push(row);
    });

    // **Generate PDF for each student**
    Object.keys(students).forEach(studentName => {
        const doc = new jsPDF();
        const studentRecords = students[studentName]; // Get all rows for this student
        const studentDetails = studentRecords[0] || Array(21).fill('-'); // Default values

        // **Document Title**
        doc.setFont('helvetica', 'bold');
        doc.setFontSize(20);
        doc.text('STUDENT EXAM REPORT', 105, 20, { align: 'center' });

        doc.setFontSize(12);
        doc.setFont('helvetica', 'normal');
        doc.text(`Generated on: ${new Date().toLocaleString()}`, 20, 30);

        // **Header Information**
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
            doc.text(detail.toString(), 20, yPos); // Ensure it's a string
            yPos += 7;
        });

        yPos += 5;

        // **Draw Table Headers**
        const headers = ['School Course', 'Class Score', 'Exams Score', 'Total Score', 'Subjects Position', 'Grade'];
        const columnWidths = [50, 30, 30, 30, 30, 20];

        let currentX = 20;
        doc.setFont('helvetica', 'bold');
        headers.forEach((header, index) => {
            doc.text(header, currentX + columnWidths[index] / 2, yPos, { align: 'center' });
            currentX += columnWidths[index];
        });

        // **Draw Rows**
        doc.setFont('helvetica', 'normal');
        let rowY = yPos + 10;
        studentRecords.forEach(row => {
            let rowX = 20;
            for (let i = 9; i < Math.min(row.length, headers.length + 9); i++) {
                const cellValue = row[i] ? row[i].toString() : '-'; // Ensure text is always a string
                doc.text(cellValue, rowX + columnWidths[i - 9] / 2, rowY, { align: 'center' });
                rowX += columnWidths[i - 9];
            }
            rowY += 10;
        });

        // **Footer Section**
        rowY += 10;
        doc.setFont('helvetica', 'bold');
        const footerDetails = [
            'Teacher’s Remarks:', studentDetails[15] || '-',
            'Conduct:', studentDetails[16] || '-',
            'Headmaster’s Remark:', studentDetails[17] || '-',
            'School Information:', studentDetails[18] || '-',
            'Teacher’s Signature:', studentDetails[19] || '-',
            'Headmaster’s Signature:', studentDetails[20] || '-'
        ];

        for (let i = 0; i < footerDetails.length; i += 2) {
            doc.text(footerDetails[i], 20, rowY);
            doc.text(footerDetails[i + 1].toString(), 80, rowY); // Ensure text is always a string
            rowY += 7;
        }

        // **Save the PDF**
        doc.save(`${studentName}_${fileName}`);
    });
}
