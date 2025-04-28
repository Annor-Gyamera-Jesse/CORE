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

    const doc = new jsPDF('landscape', 'pt', 'a4');

    csvRows.forEach((rowData, index) => {
        if (index !== 0) {
            doc.addPage('a4', 'landscape');
        }

        doc.setFont('helvetica', 'bold');
        doc.setFontSize(18);
        doc.text('LESSON NOTES REPORT', doc.internal.pageSize.getWidth() / 2, 30, { align: 'center' });

        const tableData = csvHeaders.map((header, headerIndex) => {
            return [header, rowData[headerIndex] || '-'];
        });

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
                0: {
                    cellWidth: 120,  // Make header column smaller (adjustable!)
                    fontStyle: 'bold',
                    fillColor: [230, 230, 230]
                },
                1: {
                    cellWidth: 'auto' // Data column gets rest of the space
                }
            },
            margin: { top: 50, left: 20, right: 20 },
        });
    });

    doc.save(fileName);
};
