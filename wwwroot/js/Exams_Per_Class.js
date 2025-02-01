function downloadExamsPdfPerClass(fileName, base64Csv) {
    const { jsPDF } = window.jspdf;
    const doc = new jsPDF();

    const decodedCsv = atob(base64Csv);
    const lines = decodedCsv.split('\n');

    const headers = lines[0].split(',');
    const tableData = lines.slice(1).filter(line => line.trim() !== '').map(line => line.split(','));

    const columnWidths = [40, 25, 25, 25, 30, 30]; // Column widths for the table
    const tableX = 20; // X position for the table

    let tableY = 45; // Starting Y position

    // Function to generate individual student reports
    function generateStudentReport(studentData) {
        // Student details header
        doc.setFont('helvetica', 'bold');
        doc.setFontSize(16);
        doc.text('Student Report', 105, tableY, { align: 'center' });

        doc.setFontSize(12);
        doc.setFont('helvetica', 'normal');

        // Assuming studentData contains Name, Class, etc.
        doc.text('Name: ' + studentData.name, 20, tableY + 15);
        doc.text('Class: ' + studentData.class, 20, tableY + 25);
        doc.text('Exam: ' + studentData.exam, 20, tableY + 35);

        // Add a horizontal line after the header
        doc.setLineWidth(0.5);
        doc.line(tableX, tableY + 40, tableX + columnWidths.reduce((a, b) => a + b), tableY + 40);

        // Draw the table headers
        let currentX = tableX;
        headers.forEach((header, index) => {
            doc.text(header, currentX, tableY + 50);
            currentX += columnWidths[index];
        });

        // Draw the header separator line
        doc.line(tableX, tableY + 52, tableX + columnWidths.reduce((a, b) => a + b), tableY + 52);

        // Start rendering student exam results rows
        tableY = tableY + 60;
        doc.setFont('helvetica', 'normal');
        studentData.results.forEach((row, rowIndex) => {
            let rowX = tableX;
            row.forEach((cell, index) => {
                doc.text(cell, rowX, tableY);
                rowX += columnWidths[index];
            });
            tableY += 10;

            // Check if the page overflows, if so, add a new page
            if (tableY > 250) {
                doc.addPage();
                tableY = 20; // Reset table Y position on new page
                generateStudentReport(studentData); // Re-generate student report for the new page
            }
        });

        // Footer message
        doc.setFont('helvetica', 'italic');
        doc.text('End of Report for ' + studentData.name, 105, tableY + 20, { align: 'center' });
    }

    // Loop through each student and generate their report
    let currentStudentIndex = 0;
    while (currentStudentIndex < tableData.length) {
        const studentData = {
            name: tableData[currentStudentIndex][0], // Assuming the first column is the student's name
            class: tableData[currentStudentIndex][1], // Assuming the second column is the class
            exam: tableData[currentStudentIndex][2], // Assuming the third column is the exam name
            results: [] // This will store the student's exam results
        };

        // Gather the results for the current student (from the same row)
        studentData.results.push(tableData[currentStudentIndex].slice(3)); // Assuming the rest is exam data

        // Add a new page for each student
        if (currentStudentIndex > 0) {
            doc.addPage(); // Add new page for the next student
        }

        // Generate the report for the current student
        generateStudentReport(studentData);

        currentStudentIndex++; // Move to the next student
    }

    // Save the final PDF
    doc.save(fileName);
}
