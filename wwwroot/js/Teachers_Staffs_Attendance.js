function downloadTeachersAttendancePdf(fileName, base64Data) {
    const { jsPDF } = window.jspdf;
    const doc = new jsPDF('portrait', 'pt', 'a4');
    const margin = 40;
    const lineHeight = 20;
    const pageWidth = doc.internal.pageSize.getWidth();
    const pageHeight = doc.internal.pageSize.getHeight();

    // Decode and parse data
    const jsonString = atob(base64Data);
    const data = JSON.parse(jsonString);
    if (!data.length) return alert("No attendance data to print.");

    // Title
    doc.setFontSize(18);
    doc.setFont('helvetica', 'bold');
    doc.text("Teacher Attendance Report", pageWidth / 2, margin, { align: 'center' });

    // Subtitle with date or range (assumes all same date here)
    let dateText = "";
    if (data.length > 0 && data[0].ClockIN) {
        const firstDate = new Date(data[0].ClockIN);
        dateText = `Date: ${firstDate.toLocaleDateString()}`;
    }
    doc.setFontSize(12);
    doc.setFont('helvetica', 'normal');
    doc.text(dateText, pageWidth / 2, margin + lineHeight, { align: 'center' });

    // Table headers
    const startY = margin + lineHeight * 3;
    const colX = [margin, margin + 120, margin + 260, margin + 400]; // Column X positions
    doc.setFontSize(11);
    doc.setFont('helvetica', 'bold');
    doc.text("NAME", colX[0], startY);
    /*doc.text("Last Name", colX[1], startY);*/
    doc.text("Clock IN", colX[2], startY);
    doc.text("Clock OUT", colX[3], startY);

    doc.setDrawColor(0);
    doc.line(margin, startY + 4, pageWidth - margin, startY + 4); // underline headers

    // Table body
    doc.setFont('helvetica', 'normal');
    let y = startY + lineHeight;

    for (let i = 0; i < data.length; i++) {
        const row = data[i];
        if (y > pageHeight - margin) {
            doc.addPage();
            y = margin;
        }
        const clockIn = new Date(row.ClockIN).toLocaleString();
        const clockOut = row.ClockOUT ? new Date(row.ClockOUT).toLocaleString() : "N/A";

        doc.text(row.TeacherFirstName, colX[0], y);
        doc.text(row.TeacherLastName, colX[1], y);
        doc.text(clockIn, colX[2], y);
        doc.text(clockOut, colX[3], y);

        y += lineHeight;
    }

    // Footer with page number and timestamp
    const pageCount = doc.internal.getNumberOfPages();
    for (let i = 1; i <= pageCount; i++) {
        doc.setPage(i);
        doc.setFontSize(8);
        doc.setTextColor('#888');
        doc.text(`Generated on ${new Date().toLocaleString()}`, margin, pageHeight - 10);
        doc.text(`Page ${i} of ${pageCount}`, pageWidth - margin - 50, pageHeight - 10);
    }

    doc.save(fileName);
}
