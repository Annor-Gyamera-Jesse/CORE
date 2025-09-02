function downloadEnrollmentFormPdf(companyName, schoolName, logoBase64, currentDate, level) {
    const { jsPDF } = window.jspdf;
    const doc = new jsPDF('p', 'mm', 'a4');
    const pageHeight = doc.internal.pageSize.height;
    const margin = 20;
    let yOffset = 20;

    function checkPageBreak(extraHeight = 10) {
        if (yOffset + extraHeight > pageHeight - margin) {
            doc.addPage();
            yOffset = margin;
        }
    }

    function gh(amount) {
        // Render whole amounts as-is; you can switch to toFixed(2) if you prefer
        return `GHS ${amount}`;
    }

    // === Logo + School Header ===
    if (logoBase64) {
        doc.addImage(`data:image/png;base64,${logoBase64}`, 'PNG', 85, yOffset, 40, 25);
        yOffset += 30;
    }
    doc.setFont('helvetica', 'bold');
    doc.setFontSize(16);
    doc.text(companyName?.toUpperCase() || "", 105, yOffset, { align: 'center' });
    yOffset += 8;
    doc.setFontSize(13);
    doc.text(schoolName || "", 105, yOffset, { align: 'center' });
    yOffset += 5;
    doc.setDrawColor(0);
    doc.line(20, yOffset, 190, yOffset);
    yOffset += 10;

    // === Title with Level ===
    doc.setFont('helvetica', 'bold');
    doc.setFontSize(12);
    doc.text(`ENROLLMENT FORM – ${level}`, 105, yOffset, { align: 'center' });
    yOffset += 8;
    doc.setFont('helvetica', 'normal');
    doc.setFontSize(10);
    doc.text(`Printed: ${currentDate}`, 105, yOffset, { align: 'center' });
    yOffset += 10;

    // === Student Details Section ===
    doc.setFont('helvetica', 'bold');
    doc.setFontSize(12);
    doc.text("STUDENT INFORMATION", 20, yOffset);
    yOffset += 8;

    doc.setFont('helvetica', 'normal');
    doc.setFontSize(11);
    const studentFields = [
        'Name of prospective pupil',
        'Date of Birth',
        'Sex',
        'Previous Class',
        'Parent/Guardian Name',
        'Occupation',
        'Postal Address',
        'Residential Address',
        'Telephone Number',
        'Disability (Y/N)',
        'Religious Denomination'
    ];
    studentFields.forEach(field => {
        checkPageBreak(12);
        doc.rect(20, yOffset - 5, 170, 8);
        doc.text(field, 22, yOffset);
        yOffset += 12;
    });

    // === Fees Payable (single level only) ===
    checkPageBreak(50);
    doc.setFont('helvetica', 'bold');
    doc.setFontSize(12);
    doc.text(`FEES PAYABLE – ${level}`, 105, yOffset, { align: 'center' });
    yOffset += 6;

    const items = [
        'Admission Fees',
        'P.T.A DUES',
        'Tuition Fees',
        'Friday Wear',
        'Maintenance Fee',
        'First Aid Fees',
        'Crest',
        'Sports Fee',
        'Textbooks',
        'Total'
    ];

    // Keep your exact numbers (including Total) per level
    const feesByLevel = {
        Preschool: [25, 10, 190, 100, 10, 10, 10, 10, 0, 365],
        Primary: [25, 10, 200, 100, 10, 10, 10, 10, 70, 445],
        JHS: [25, 10, 300, 100, 10, 10, 10, 10, 70, 475]
    };

    const amounts = feesByLevel[level] || [];

    // Two-column table: Item | Amount
    const colWidths = [110, 60];
    let startX = 20;
    const rowH = 8;

    // Header
    doc.setFontSize(11);
    doc.rect(startX, yOffset, colWidths[0], rowH);
    doc.text('Item', startX + 2, yOffset + 6);
    doc.rect(startX + colWidths[0], yOffset, colWidths[1], rowH);
    doc.text('Amount (GHS)', startX + colWidths[0] + colWidths[1] / 2, yOffset + 6, { align: 'center' });
    yOffset += rowH;

    // Rows
    doc.setFont('helvetica', 'normal');
    items.forEach((label, idx) => {
        checkPageBreak(12);
        const val = amounts[idx];
        doc.rect(startX, yOffset, colWidths[0], rowH);
        doc.text(label, startX + 2, yOffset + 6);

        doc.rect(startX + colWidths[0], yOffset, colWidths[1], rowH);
        if (typeof val !== "undefined") {
            const isTotal = label.toLowerCase() === 'total';
            if (isTotal) {
                doc.setFont('helvetica', 'bold');
            }
            doc.text(gh(val), startX + colWidths[0] + colWidths[1] - 4, yOffset + 6, { align: 'right' });
            if (isTotal) {
                doc.setFont('helvetica', 'normal');
            }
        }
        yOffset += rowH;
    });

    // === Requirements ===
    checkPageBreak(40);
    yOffset += 5;
    doc.setFont('helvetica', 'bold');
    doc.text("REQUIREMENTS", 105, yOffset, { align: 'center' });
    yOffset += 8;

    doc.setFont('helvetica', 'normal');
    const requirements = ["2 Toilet Rolls", "2 Toilet Soaps", "Water bottle", "1 Dettol"];
    requirements.forEach(req => {
        checkPageBreak(12);
        doc.circle(23, yOffset - 2, 1, 'F');
        doc.text(req, 28, yOffset);
        yOffset += 8;
    });

    // === Declaration ===
    checkPageBreak(60);
    yOffset += 5;
    doc.setFont('helvetica', 'bold');
    doc.text("DECLARATION", 105, yOffset, { align: 'center' });
    yOffset += 8;

    doc.setFont('helvetica', 'normal');
    const declaration = [
        "1. Please note that the fees are neither refundable nor transferable, should a prospective student decide not to take up the offer or admission any longer and must abide by the school rules and regulations.",
        "2. I agree to the condition that the student’s name can be removed from the roll with one term’s notice given by the institution. A child may also be withdrawn by the parents/guardians with one term’s notice in writing. In default, a full term’s fee shall be claimed by the school authorities."
    ];
    declaration.forEach(line => {
        checkPageBreak(20);
        doc.text(line, 20, yOffset, { maxWidth: 170 });
        yOffset += 12;
    });

    // === Signature Lines ===
    checkPageBreak(40);
    yOffset += 10;
    doc.text("Date this …………… days of …………………", 20, yOffset);
    yOffset += 20;
    doc.text("Headmaster: _______________________", 25, yOffset);
    doc.text("Parent/Guardian: _______________________", 110, yOffset);

    // === Save PDF ===
    const safeLevel = (level || 'Form').replace(/[^\w-]/g, '');
    doc.save(`Enrollment_Form_${safeLevel}_${new Date().toISOString().slice(0, 10)}.pdf`);
}
