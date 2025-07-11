function downloadDetailedStudentFeePdf(fileName, base64Csv, companyName, schoolName, logoBase64, dateGenerated) {
    const { jsPDF } = window.jspdf;
    const doc = new jsPDF("p", "mm", "a4");
    let yOffset = 10;

    const csvContent = atob(base64Csv);
    const lines = csvContent.split('\n').filter(line => line.trim() !== "");

    if (logoBase64) {
        doc.addImage(`data:image/png;base64,${logoBase64}`, 'PNG', 80, yOffset, 50, 20);
        yOffset += 25;
    }

    doc.setFontSize(14);
    doc.setFont("helvetica", "bold");
    doc.text(companyName.toUpperCase(), 105, yOffset, { align: 'center' });
    yOffset += 6;

    doc.setFontSize(12);
    doc.text("Detailed Student Fee Report", 105, yOffset, { align: 'center' });
    yOffset += 8;

    doc.setFontSize(10);
    doc.setFont("helvetica", "normal");
    doc.text(`Generated on: ${dateGenerated}`, 20, yOffset);
    yOffset += 5;

    const columnWidths = [35, 20, 10, 25, 15, 15, 15, 20, 25, 15, 15, 15, 50];
    const headers = lines[0].split(',');

    const tableRows = lines.slice(1).map(line => {
        const fields = line.split(',').map(f => f.trim().replace(/^"|"$/g, ''));
        return fields;
    });

    // Pagination handling
    let rowY = yOffset + 10;
    doc.setFont("helvetica", "bold");
    doc.setFontSize(8);

    let x = 10;
    headers.forEach((header, i) => {
        doc.text(header.trim(), x, rowY, { maxWidth: columnWidths[i] });
        x += columnWidths[i];
    });

    doc.setFont("helvetica", "normal");

    rowY += 6;
    tableRows.forEach(row => {
        let colX = 10;
        row.forEach((cell, i) => {
            const wrapped = doc.splitTextToSize(cell, columnWidths[i]);
            doc.text(wrapped, colX, rowY);
            colX += columnWidths[i];
        });
        rowY += 10;

        if (rowY > 280) {
            doc.addPage();
            rowY = 20;
        }
    });

    doc.save(fileName);
}
