function downloadClassTimetablePdf(fileName, base64Data) {
    const { jsPDF } = window.jspdf;
    const doc = new jsPDF('l', 'pt', 'a4');

    const data = JSON.parse(atob(base64Data));
    const days = [...new Set(data.map(item => item.Day))];
    const periods = ["BeforeFirstBreak", "AfterFirstBreak", "AfterSecondBreak"];

    const dayColumnWidth = 80;
    const subjectColumnWidth = 120;
    const rowHeight = 40;
    const startY = 80;
    let currentY = startY;

    doc.setFontSize(16);
    doc.text("📘 Class Timetable", doc.internal.pageSize.getWidth() / 2, 40, { align: 'center' });

    // Collect period -> subject headers with time
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

    // Draw top headers
    let x = dayColumnWidth;
    doc.setFontSize(10);
    periods.forEach(period => {
        periodHeaders[period].forEach(sub => {
            const text = `${sub.Subject}\n${new Date(sub.StartTime).toLocaleTimeString([], { hour: '2-digit', minute: '2-digit' })} - ${new Date(sub.EndTime).toLocaleTimeString([], { hour: '2-digit', minute: '2-digit' })}`;
            doc.text(text, x + 5, startY - 20);
            x += subjectColumnWidth;
        });
    });

    // Row per day
    days.forEach(day => {
        x = 0;
        doc.text(day, 10, currentY + 15); // Day column
        x += dayColumnWidth;

        periods.forEach(period => {
            periodHeaders[period].forEach(subHeader => {
                const match = data.find(d => d.Day === day && d.Subject === subHeader.Subject && d.Period === period);
                const cellText = match ? `${match.Subject}` : "";
                doc.rect(x, currentY, subjectColumnWidth, rowHeight);
                doc.text(cellText, x + 5, currentY + 20);
                x += subjectColumnWidth;
            });
        });

        currentY += rowHeight;
    });

    doc.save(fileName);
}
