function downloadClassTimetablePdf(fileName, base64Data) {
    const { jsPDF } = window.jspdf;
    const doc = new jsPDF('landscape', 'pt', 'a4');

    const data = JSON.parse(atob(base64Data));
    if (!data.length) return;

    const days = [...new Set(data.map(item => item.Day))];
    const periods = ["BeforeFirstBreak", "AfterFirstBreak", "AfterSecondBreak"];

    const dayColumnWidth = 120;
    const subjectColumnWidth = 160;
    const rowHeight = 60;
    const startY = 110;
    const margin = 20;
    let currentY = startY;

    const pageWidth = doc.internal.pageSize.getWidth();
    const pageHeight = doc.internal.pageSize.getHeight();

    // 🔵 Title Bar
    doc.setFillColor(33, 150, 243);
    doc.rect(0, 0, pageWidth, 80, 'F');
    doc.setFont('helvetica', 'bold');
    doc.setFontSize(26);
    doc.setTextColor(255, 255, 255);
    doc.text("CLASS TIMETABLE", pageWidth / 2, 50, { align: 'center' });

    // 🔸 Table Header: Periods only (no Subject row)
    let x = dayColumnWidth;
    doc.setFontSize(10);
    doc.setTextColor('#2c3e50');

    periods.forEach(period => {
        doc.setFillColor('#f5f6fa');
        doc.setDrawColor('#dcdde1');
        doc.roundedRect(x, startY - 40, subjectColumnWidth, 30, 5, 5, 'FD');
        doc.setFontSize(9);
        doc.setTextColor('#34495e');
        doc.text(period.replace(/([A-Z])/g, ' $1').trim(), x + 10, startY - 22);
        x += subjectColumnWidth;
    });

    // 🗓️ Timetable Body
    days.forEach(day => {
        x = 0;

        // Day Label
        doc.setFillColor('#2d3436');
        doc.setTextColor(255, 255, 255);
        doc.setFontSize(11);
        doc.roundedRect(x, currentY, dayColumnWidth, rowHeight, 5, 5, 'F');
        doc.text(day, x + 10, currentY + 35);

        x += dayColumnWidth;

        periods.forEach(period => {
            const entry = data.find(d => d.Day === day && d.Period === period);

            doc.setDrawColor('#dfe6e9');
            doc.setFillColor('#ffffff');
            doc.roundedRect(x, currentY, subjectColumnWidth, rowHeight, 5, 5, 'FD');
            doc.setFontSize(9);
            doc.setTextColor('#2d3436');

            if (entry) {
                const subject = entry.Subject || "";
                const start = new Date(entry.StartTime).toLocaleTimeString([], { hour: '2-digit', minute: '2-digit' });
                const end = new Date(entry.EndTime).toLocaleTimeString([], { hour: '2-digit', minute: '2-digit' });

                const text = `${subject}\n${start} - ${end}`;
                const lines = doc.splitTextToSize(text, subjectColumnWidth - 20);
                doc.text(lines, x + 10, currentY + 25);
            }

            x += subjectColumnWidth;
        });

        currentY += rowHeight;

        if (currentY + rowHeight > pageHeight - 50) {
            doc.addPage();
            currentY = startY;
        }
    });

    // 📌 Footer
    doc.setFontSize(8);
    doc.setTextColor('#7f8c8d');
    doc.text(`Generated on ${new Date().toLocaleString()}`, margin, pageHeight - 20);
    doc.text("Page 1 of 1", pageWidth - 70, pageHeight - 20);

    doc.save(fileName);
}
