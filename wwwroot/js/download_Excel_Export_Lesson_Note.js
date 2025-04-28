window.download_Excel_Export_Lesson_Note = function (fileName, base64Csv) {
    const { jsPDF } = window.jspdf;
    const decodedCsv = atob(base64Csv);
    const lines = decodedCsv.split('\n').map(line => line.trim()).filter(line => line.length > 0);

    if (lines.length < 2) {
        alert("No valid data found!");
        return;
    }

    const csvHeaders = lines[0].split(',').map(h => h.trim());
    const csvRows = lines.slice(1).map(line => line.split(',').map(cell => (cell && cell.trim()) || '-'));

    // Now transpose the data: flip rows/columns
    let tableData = csvHeaders.map((header, headerIndex) => {
        const row = [header]; // first column: header name
        csvRows.forEach(rowData => {
            row.push(rowData[headerIndex] || '-'); // corresponding data
        });
        return row;
    });

    // Create jsPDF instance with landscape orientation
    const doc = new jsPDF('landscape', 'pt', 'a4');

    // Title
    doc.setFont('helvetica', 'bold');
    doc.setFontSize(18);
    doc.text('LESSON NOTES REPORT', doc.internal.pageSize.getWidth() / 2, 30, { align: 'center' });

    // AutoTable with new vertical layout
    doc.autoTable({
        startY: 50,
        head: [],
        body: tableData,
        theme: 'grid',
        styles: {
            font: 'helvetica',
            fontSize: 9,
            cellPadding: 4,
            overflow: 'linebreak',
            halign: 'center',
            valign: 'middle',
        },
        bodyStyles: {
            textColor: [0, 0, 0],
        },
        columnStyles: {
            0: { fontStyle: 'bold', fillColor: [230, 230, 230] }, // Header column (gray background)
        },
        margin: { top: 50, left: 20, right: 20 },
        tableWidth: 'auto',
    });

    // Save the PDF
    doc.save(fileName);
};
