window.downloadEnrollmentFormPdf = function (companyName, schoolName, logoBase64, currentDate, level) {
    const { jsPDF } = window.jspdf;
    const doc = new jsPDF('p', 'mm', 'a4');
    let yOffset = 15;

    const marginX = 15;   // smaller side margins
    const pageWidth = 210; 
    const contentWidth = pageWidth - marginX * 2;

    function gh(amount) {
        return `GHS ${amount}`;
    }

    // === Logo + School Header ===
    if (logoBase64) {
        doc.addImage(`data:image/png;base64,${logoBase64}`, 'PNG', 92, yOffset, 22, 12);
        yOffset += 15;
    }

    doc.setFont('helvetica', 'bold');
    doc.setFontSize(9);
    let wrappedCompany = doc.splitTextToSize(companyName?.toUpperCase() || "", contentWidth);
    doc.text(wrappedCompany, pageWidth / 2, yOffset, { align: 'center' });
    yOffset += wrappedCompany.length * 4;

    doc.setFontSize(8);
    let wrappedSchool = doc.splitTextToSize(schoolName || "", contentWidth);
    doc.text(wrappedSchool, pageWidth / 2, yOffset, { align: 'center' });
    yOffset += wrappedSchool.length * 4;

    doc.line(marginX, yOffset, pageWidth - marginX, yOffset);
    yOffset += 5;

    // === Title with Level ===
    doc.setFont('helvetica', 'bold');
    doc.setFontSize(9);
    doc.text(`ENROLLMENT FORM – ${level}`, pageWidth / 2, yOffset, { align: 'center' });
    yOffset += 5;
    doc.setFont('helvetica', 'normal');
    doc.setFontSize(7);
    doc.text(`Printed: ${currentDate}`, pageWidth / 2, yOffset, { align: 'center' });
    yOffset += 5;

    // === Student Details Section ===
    doc.setFont('helvetica', 'bold');
    doc.setFontSize(8);
    doc.text("STUDENT INFORMATION", marginX, yOffset);
    yOffset += 5;

    doc.setFont('helvetica', 'normal');
    doc.setFontSize(7);
    const studentFields = [
        'Name of prospective pupil', 'Date of Birth', 'Sex', 'Previous Class',
        'Parent/Guardian Name', 'Occupation', 'Postal Address', 'Residential Address',
        'Telephone Number', 'Disability (Y/N)', 'Religious Denomination'
    ];
    studentFields.forEach(field => {
        doc.rect(marginX, yOffset - 3, contentWidth, 5);
        doc.text(field, marginX + 2, yOffset);
        yOffset += 5;
    });

    // === Fees Payable ===
    doc.setFont('helvetica', 'bold');
    doc.setFontSize(8);
    doc.text(`FEES PAYABLE – ${level}`, pageWidth / 2, yOffset, { align: 'center' });
    yOffset += 5;

    const items = [
        'Admission Fees', 'P.T.A DUES', 'Tuition Fees', 'Friday Wear', 'Maintenance Fee',
        'First Aid Fees', 'Crest', 'Sports Fee', 'Textbooks', 'Total'
    ];
    const feesByLevel = {
        Preschool: [25, 10, 190, 100, 10, 10, 10, 10, 0, 365],
        Primary: [25, 10, 200, 100, 10, 10, 10, 10, 70, 445],
        JHS: [25, 10, 300, 100, 10, 10, 10, 10, 70, 475]
    };
    const amounts = feesByLevel[level] || [];

    const colWidths = [contentWidth * 0.65, contentWidth * 0.35];
    const rowH = 5;

    doc.setFontSize(7);
    doc.rect(marginX, yOffset, colWidths[0], rowH);
    doc.text('Item', marginX + 2, yOffset + 3);
    doc.rect(marginX + colWidths[0], yOffset, colWidths[1], rowH);
    doc.text('Amount (GHS)', marginX + colWidths[0] + colWidths[1] / 2, yOffset + 3, { align: 'center' });
    yOffset += rowH;

    items.forEach((label, idx) => {
        const val = amounts[idx];
        doc.rect(marginX, yOffset, colWidths[0], rowH);
        doc.text(label, marginX + 2, yOffset + 3);
        doc.rect(marginX + colWidths[0], yOffset, colWidths[1], rowH);
        if (typeof val !== "undefined") {
            const isTotal = label.toLowerCase() === 'total';
            if (isTotal) doc.setFont('helvetica', 'bold');
            doc.text(gh(val), marginX + colWidths[0] + colWidths[1] - 3, yOffset + 3, { align: 'right' });
            if (isTotal) doc.setFont('helvetica', 'normal');
        }
        yOffset += rowH;
    });

    // === Requirements ===
    yOffset += 3;
    doc.setFont('helvetica', 'bold');
    doc.setFontSize(8);
    doc.text("REQUIREMENTS", pageWidth / 2, yOffset, { align: 'center' });
    yOffset += 5;
    doc.setFont('helvetica', 'normal');
    doc.setFontSize(7);
    ["2 Toilet Rolls", "2 Toilet Soaps", "Water bottle", "1 Dettol"].forEach(req => {
        doc.circle(marginX + 2, yOffset - 2, 0.7, 'F');
        doc.text(req, marginX + 6, yOffset);
        yOffset += 4.5;
    });

    // === Declaration ===
    yOffset += 3;
    doc.setFont('helvetica', 'bold');
    doc.setFontSize(8);
    doc.text("DECLARATION", pageWidth / 2, yOffset, { align: 'center' });
    yOffset += 5;

    doc.setFont('helvetica', 'normal');
    doc.setFontSize(6.5);
    [
        "1. Fees are neither refundable nor transferable, should a prospective student decide not to take up admission. The student must abide by school rules and regulations.",
        "2. I agree that the student’s name can be removed from the roll with one term’s notice by the institution and that child can be withdrawn by the parent/guardian with one term’s notice in writing. In default a term's fee shall be claimed by School Authorities."
    ].forEach(line => {
        const wrapped = doc.splitTextToSize(line, contentWidth);
        doc.text(wrapped, marginX, yOffset);
        yOffset += wrapped.length * 3.5;
    });

    // === Signature Lines ===
    yOffset += 5;
    doc.text("Date this …………… days of …………………", marginX, yOffset);
    yOffset += 8;
    doc.text("Headmaster: _______________________", marginX, yOffset);
    doc.text("Parent/Guardian: _______________________", pageWidth / 2 + 10, yOffset);

    // Save
    const safeLevel = (level || 'Form').replace(/[^\w-]/g, '');
    doc.save(`Enrollment_Form_${safeLevel}_${new Date().toISOString().slice(0, 10)}.pdf`);
};
