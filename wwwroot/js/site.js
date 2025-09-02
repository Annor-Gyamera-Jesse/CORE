function downloadEnrollmentFormPdf(companyName, schoolName, logoBase64, currentDate) {
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

    // === Logo + School Header ===
    if (logoBase64) {
        doc.addImage(`data:image/png;base64,${logoBase64}`, 'PNG', 85, yOffset, 40, 25);
        yOffset += 30;
    }
    doc.setFont('helvetica', 'bold');
    doc.setFontSize(16);
    doc.text(companyName.toUpperCase(), 105, yOffset, { align: 'center' });
    yOffset += 8;
    doc.setFontSize(13);
    doc.text(schoolName, 105, yOffset, { align: 'center' });
    yOffset += 5;
    doc.setDrawColor(0);
    doc.line(20, yOffset, 190, yOffset); // underline
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
        doc.rect(20, yOffset - 5, 170, 8); // input box
        doc.text(field, 22, yOffset);
        yOffset += 12;
    });

    // === Fees Table ===
    checkPageBreak(50);
    doc.setFont('helvetica', 'bold');
    doc.setFontSize(12);
    doc.text("FEES PAYABLE", 105, yOffset, { align: 'center' });
    yOffset += 5;

    const headers = ['Item', 'Preschool', 'Primary', 'JHS'];
    const items = ['Admission Fees GH', 'Tuition Fees GH', 'Friday Wear', 'Maintenance Fee', 'First Aid Fees', 'Crest', 'Sports Fee', 'Textbooks', 'Total'];
    const colWidths = [70, 35, 35, 35];
    let startX = 20;

    // draw table header
    doc.setFontSize(11);
    let rowHeight = 8;
    startX = 20;
    headers.forEach((h, i) => {
        doc.rect(startX, yOffset, colWidths[i], rowHeight);
        doc.text(h, startX + colWidths[i] / 2, yOffset + 6, { align: 'center' });
        startX += colWidths[i];
    });
    yOffset += rowHeight;

    // table rows
    doc.setFont('helvetica', 'normal');
    items.forEach(item => {
        checkPageBreak(15);
        startX = 20;
        headers.forEach((h, i) => {
            doc.rect(startX, yOffset, colWidths[i], rowHeight);
            if (i === 0) {
                doc.text(item, startX + 2, yOffset + 6); // left align for item name
            }
            startX += colWidths[i];
        });
        yOffset += rowHeight;
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
        doc.circle(23, yOffset - 2, 1, 'F'); // bullet
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
    doc.save(`Enrollment_Form_${new Date().toISOString().slice(0, 10)}.pdf`);
}
