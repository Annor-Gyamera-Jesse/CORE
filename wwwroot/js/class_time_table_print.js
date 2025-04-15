function downloadClassTimetablePdf(fileName, base64Data) {
    const { jsPDF } = window.jspdf;
    const doc = new jsPDF('l', 'pt', 'a4');

    const data = JSON.parse(atob(base64Data));
    const days = [...new Set(data.map(item => item.Day))];
    const periods = ["BeforeFirstBreak", "AfterFirstBreak", "AfterSecondBreak"];

    const dayColumnWidth = 120;
    const subjectColumnWidth = 150;
    const rowHeight = 60;
    const startY = 120;
    let currentY = startY;

    // 🌈 Title with gradient bar
    const pageWidth = doc.internal.pageSize.getWidth();
    doc.setFillColor(58, 123, 213); // gradient-like blue bar
    doc.rect(0, 0, pageWidth, 80, 'F');

    doc.setFontSize(28);
    doc.setTextColor('#ffffff');
    doc.setFont('helvetica', 'bold');
    doc.text("📘 CLASS TIMETABLE", pageWidth / 2, 50, { align: 'center' });

    // 📌 Period headers with time
    const periodHeaders = {};
    periods.forEach(period => {
        const subjects = data.filter(x => x.Period === period);
        const uniqueSubs = [];
        subjects.forEach(s => {
            const exists = uniqueSubs.find(u => u.Subject === s.Subject && u.StartTime === s.StartTime && u.EndTime === s.EndTime);
            if (!exists) uniqueSubs.push(s);
        });
        periodHeaders[period] = uniqueSubs;
    });

    // 📎 Draw header row
    let x = dayColumnWidth;
    doc.setFontSize(10);
    doc.setFont('helvetica', 'bold');
    doc.setTextColor('#2c3e50');

    periods.forEach(period => {
        periodHeaders[period].forEach(sub => {
            const start = new Date(sub.StartTime).toLocaleTimeString([], { hour: '2-digit', minute: '2-digit' });
            const end = new Date(sub.EndTime).toLocaleTimeString([], { hour: '2-digit', minute: '2-digit' });
            const header = `${sub.Subject}\n${start} - ${end}`;

            // Header Box
            doc.setFillColor('#f1f3f6');
            doc.setDrawColor('#e0e0e0');
            doc.roundedRect(x, startY - 50, subjectColumnWidth, 50, 6, 6, 'FD');
            doc.setTextColor('#34495e');
            doc.text(header, x + 10, startY - 25);
            x += subjectColumnWidth;
        });
    });

    // 📅 Render timetable rows
    days.forEach((day, index) => {
        x = 0;

        // Day label column
        doc.setFontSize(11);
        doc.setTextColor('#ffffff');
        doc.setFillColor('#2c3e50');
        doc.roundedRect(x, currentY, dayColumnWidth, rowHeight, 5, 5, 'F');
        doc.text(day, x + 10, currentY + 35);

        x += dayColumnWidth;

        periods.forEach(period => {
            periodHeaders[period].forEach(subHeader => {
                const match = data.find(d =>
                    d.Day === day &&
                    d.Subject === subHeader.Subject &&
                    d.Period === period
                );
                const cellText = match ? match.Subject : "";

                // Pretty cell box
                doc.setDrawColor('#dfe6e9');
                doc.setFillColor('#ffffff');
                doc.roundedRect(x, currentY, subjectColumnWidth, rowHeight, 5, 5, 'FD');
                doc.setFontSize(10);
                doc.setTextColor('#2d3436');
                doc.setFont('helvetica', 'normal');
                doc.text(cellText, x + 10, currentY + 35);
                x += subjectColumnWidth;
            });
        });

        currentY += rowHeight;
    });

    // 🧾 Footer
    doc.setFontSize(9);
    doc.setTextColor('#b2bec3');
    doc.text(`Generated on ${new Date().toLocaleString()}`, 30, doc.internal.pageSize.getHeight() - 20);
    doc.text(`Page 1 of 1`, pageWidth - 70, doc.internal.pageSize.getHeight() - 20);

    // 💾 Save the PDF
    doc.save(fileName);
}
