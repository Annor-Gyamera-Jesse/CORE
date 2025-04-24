window.downloadExcel_Export_Per_Student_Attendance_Pdf = function (fileName, base64Csv) {
    const { jsPDF } = window.jspdf;

    // Decode & parse CSV
    const decoded = atob(base64Csv);
    const lines = decoded.split('\n').map(l => l.trim()).filter(l => l);
    if (lines.length < 2) {
        alert("No valid data!");
        return;
    }
    const rows = lines.slice(1).map(l => l.split(','));

    // Count totals
    let totalPresent = 0, totalAbsent = 0;
    rows.forEach(r => {
        if ((r[2] || '').toLowerCase() === 'present') totalPresent++;
        else totalAbsent++;
    });

    const doc = new jsPDF();
    const W = doc.internal.pageSize.getWidth();
    let y = 20;

    // Title
    doc.setFontSize(18).setFont('helvetica', 'bold');
    doc.text('STUDENT ATTENDANCE REPORT', W / 2, y, { align: 'center' });
    y += 10;

    // Student meta from first row (Adding student name)
    const [firstDate, firstTerm] = rows[0];
    const studentName = `${rows[0][3]} ${rows[0][4]}`; // Assuming first and last name are in column 3 and 4
    doc.setFontSize(11).setFont('helvetica', 'normal');
    doc.text(`Student: ${studentName}`, 20, y); // Displaying student's name
    doc.text(`Date Range: ${rows[0][0]} – ${rows[rows.length - 1][0]}`, 20, y + 7);
    doc.text(`Term: ${firstTerm}`, W - 60, y + 7);
    y += 20;

    // Table header
    const colW = [60, 60, 40];
    let x = 20;
    doc.setFont('helvetica', 'bold');
    ['Date', 'Term', 'Status'].forEach((h, i) => {
        doc.text(h, x + colW[i] / 2, y, { align: 'center' });
        x += colW[i];
    });
    y += 6;
    doc.setLineWidth(0.5).line(20, y, W - 20, y);
    y += 6;

    // Rows
    doc.setFont('helvetica', 'normal');
    rows.forEach(r => {
        x = 20;
        [r[0], r[1], r[2]].forEach((cell, i) => {
            doc.text(cell || '-', x + colW[i] / 2, y, { align: 'center' });
            x += colW[i];
        });
        y += 8;
        if (y > doc.internal.pageSize.getHeight() - 30) {
            doc.addPage(); y = 20;
        }
    });

    // Totals summary
    if (y > doc.internal.pageSize.getHeight() - 30) {
        doc.addPage(); y = 20;
    }
    y += 10;
    doc.setFont('helvetica', 'bold');
    doc.text(`Total Present: ${totalPresent}`, 20, y);
    y += 7;
    doc.text(`Total Absent: ${totalAbsent}`, 20, y);

    doc.save(fileName);
};
