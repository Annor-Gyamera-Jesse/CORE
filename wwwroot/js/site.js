function downloadEnrollmentFormPdf(companyName, schoolName, logoBase64, currentDate) {
    const { jsPDF } = window.jspdf;
    const doc = new jsPDF('p', 'mm', 'a4');

    let yOffset = 10;

    // === Logo and School Info ===
    if (logoBase64) {
        doc.addImage(`data:image/png;base64,${logoBase64}`, 'PNG', 80, yOffset, 50, 30);
        yOffset += 35;
    }

    doc.setFont('helvetica', 'bold');
    doc.setFontSize(14);
    doc.text(companyName.toUpperCase(), 105, yOffset, { align: 'center' });
    yOffset += 7;
    doc.setFontSize(12);
    doc.text(schoolName, 105, yOffset, { align: 'center' });
    yOffset += 10;

    // === Parent/Student Details Table ===
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
        'Is your ward having any disability (Y/N)',
        'Religious Denomination'
    ];

    studentFields.forEach(field => {
        doc.text(`${field}: ___________________________________________`, 20, yOffset);
        yOffset += 8;
    });

    yOffset += 5;

    // === Fees Table Header ===
    doc.setFont('helvetica', 'bold');
    doc.text("FEES PAYABLE", 105, yOffset, { align: 'center' });
    yOffset += 8;

    const headers = ['Item', 'Preschool', 'Primary', 'JHS'];
    const items = ['Admission Fees GH', 'Tuition Fees GH', 'Friday Wear', 'Maintenance Fee', 'First Aid Fees', 'Crest', 'Sports Fee', 'Textbooks', 'Total'];
    const colWidths = [60, 40, 40, 40];
    let startX = 20;

    // Header row
    headers.forEach((h, i) => {
        doc.text(h, startX + colWidths[i] / 2, yOffset, { align: 'center' });
        startX += colWidths[i];
    });

    yOffset += 7;
    doc.setFont('helvetica', 'normal');

    // Table rows
    items.forEach(item => {
        startX = 20;
        doc.text(item, startX + colWidths[0] / 2, yOffset, { align: 'center' });
        // Leave empty columns for Preschool/Primary/JHS
        for (let i = 1; i < headers.length; i++) {
            doc.text("", startX + colWidths[i] / 2, yOffset, { align: 'center' });
            startX += colWidths[i];
        }
        startX += colWidths[0];
        yOffset += 7;
    });

    yOffset += 5;

    // === Requirements ===
    doc.setFont('helvetica', 'bold');
    doc.text("REQUIREMENTS", 105, yOffset, { align: 'center', underline: true });
    yOffset += 8;
    doc.setFont('helvetica', 'normal');
    const requirements = ["2 Toilet Rolls", "2 Toilet Soaps", "Water bottles", "1 Dettol"];
    requirements.forEach(req => {
        doc.text("- " + req, 25, yOffset);
        yOffset += 7;
    });

    yOffset += 5;

    // === Declaration ===
    doc.setFont('helvetica', 'bold');
    doc.text("DECLARATION", 105, yOffset, { align: 'center', underline: true });
    yOffset += 8;
    doc.setFont('helvetica', 'normal');
    const declaration = [
        "1. Please note that the fees are neither refundable nor transferable, should a potentially student decide not to take up the offer or admission any longer and should abide by the rules and regulations.",
        "2. I agree to the condition that student name can be removed from the roll on term’s notices given by institution and that child can be withdrawn by the parents or guardians on term’s notice in writing. In default a term fee shall be claimed by Authorities."
    ];
    declaration.forEach(line => {
        doc.text(line, 20, yOffset, { maxWidth: 170 });
        yOffset += 10;
    });

    yOffset += 5;
    doc.text("Date this …………… days of …………………", 20, yOffset);
    yOffset += 15;

    // === Signature Lines ===
    doc.text("Headmaster: _______________________", 25, yOffset);
    doc.text("Parent/Guardian: _______________________", 110, yOffset);

    // === Save PDF ===
    doc.save(`Enrollment_Form_${new Date().toISOString().slice(0, 10)}.pdf`);
}
