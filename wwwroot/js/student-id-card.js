function formatDate(isoDate) {
    if (!isoDate) return "_";
    const date = new Date(isoDate);
    return date.toLocaleDateString('en-GB', {
        day: '2-digit',
        month: 'short',
        year: 'numeric'
    }).replace(',', '');
}

function generateStudentIDCard(student) {
    console.log("Received Student Data:", student);

    if (!student || !student.StudentID) {
        console.error("Error: Student ID is undefined! Received student:", student);
        return;
    }

    const { jsPDF } = window.jspdf;
    const doc = new jsPDF('landscape', 'mm', [85.6, 53.98]);

    try {
        // Background Color
        doc.setFillColor(0, 76, 153);
        doc.rect(0, 0, 85, 55, 'F');

        // School Logo
        if (student.SchoolImageBase64) {
            doc.addImage(student.SchoolImageBase64, 'JPEG', 5, 3, 10, 10);
        }

        // School Name
        doc.setTextColor(255, 255, 255);
        doc.setFont('helvetica', 'bold');
        doc.setFontSize(8);
        doc.text(doc.splitTextToSize(`Name: ${student.StudentFirstName} ${student.StudentLastName}`, 50), 27, textY);
        doc.text(student.SchoolName || 'SCHOOL NAME', 42, 8, { align: 'center' });

        // Student Photo Placeholder
        if (student.ImageBase64) {
            doc.addImage(student.ImageBase64, 'JPEG', 5, 15, 18, 24);
        } else {
            doc.setFillColor(200, 200, 200);
            doc.roundedRect(5, 15, 18, 24, 2, 2, 'F');
        }

        // Student Information Box
        doc.setFillColor(255, 255, 255);
        doc.roundedRect(25, 12, 55, 30, 3, 3, 'F');
        doc.setTextColor(0, 0, 0);
        doc.setFont('helvetica', 'bold');
        doc.setFontSize(8);

        let textY = 17;
        doc.text(`Name: ${student.StudentFirstName} ${student.StudentLastName}`, 27, textY);
        textY += 5;
        doc.text(`Gender: ${student.StudentGender ?? '_'}`, 27, textY);
        textY += 5;
        doc.text(`Class: ${student.ClassID ?? '_'}`, 27, textY);
        textY += 5;
        doc.text(`DOB: ${formatDate(student.StudentDateOfBirth)}`, 27, textY); // **Formatted DOB**
        //textY += 5;
        //doc.text(`Guardian: ${student.GuardianFullName ?? '_'}`, 27, textY);
        textY += 5;
        doc.text(`Contact 1: ${student.GuardianFirstContact ?? '_'}`, 27, textY);
        textY += 5;
        doc.text(`Contact 2: ${student.GuardianSecondContact ?? '_'}`, 27, textY);

        // Footer Design
        doc.setFillColor(255, 255, 255);
        doc.roundedRect(0, 48, 85, 7, 2, 2, 'F');
        doc.setTextColor(0, 76, 153);
        doc.setFont('helvetica', 'bold');
        doc.text('STUDENT ID CARD', 42, 52, { align: 'center' });

        // Save PDF
        doc.save(`Student_ID_${student.StudentID}.pdf`);
    } catch (error) {
        console.error("Error generating ID card:", error);
    }
}
