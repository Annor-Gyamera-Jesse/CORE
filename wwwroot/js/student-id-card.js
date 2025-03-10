function generateStudentIDCard(student) {
    console.log("Received Student Data:", student);

    if (!student || !student.StudentID) {
        console.error("Error: Student ID is undefined! Received student:", student);
        return;
    }

    console.log("Student ID Found:", student.StudentID);
    console.log("Student Name:", student.StudentFirstName, student.StudentLastName);
    console.log("Class:", student.ClassID);
    console.log("Gender:", student.StudentGender);
    console.log("ImageBase64 Status:", student.ImageBase64 ? "FOUND IMAGE" : "EMPTY");
    console.log("School Name:", student.SchoolName);
    console.log("School Image Status:", student.SchoolImageBase64 ? "FOUND SCHOOL IMAGE" : "EMPTY");

    const { jsPDF } = window.jspdf;
    const doc = new jsPDF('landscape', 'mm', [85, 55]);

    try {
        doc.setFillColor(0, 120, 215);
        doc.rect(0, 0, 85, 55, 'F');

        if (student.SchoolImageBase64) {
            doc.addImage(student.SchoolImageBase64, 'JPEG', 5, 2, 10, 10);
        }

        doc.setTextColor(255, 255, 255);
        doc.setFont('helvetica', 'bold');
        doc.setFontSize(10);
        doc.text(student.SchoolName || 'SCHOOL NAME', 42, 8, { align: 'center' });

        if (student.ImageBase64) {
            doc.addImage(student.ImageBase64, 'JPEG', 5, 12, 18, 24);
        } else {
            doc.setFillColor(200, 200, 200);
            doc.rect(5, 12, 18, 24, 'F');
        }

        doc.setTextColor(0, 0, 0);
        doc.setFont('helvetica', 'normal');
        doc.setFontSize(8);

        let textY = 15;
        doc.text(`Name: ${student.StudentFirstName} ${student.StudentLastName}`, 25, textY);
        textY += 5;
        doc.text(`Gender: ${student.StudentGender ?? '_'}`, 25, textY);
        textY += 5;
        doc.text(`Class: ${student.ClassID ?? '_'}`, 25, textY);
        textY += 5;
        doc.text(`DOB: ${student.StudentDateOfBirth ?? '_'}`, 25, textY);
        textY += 5;
        doc.text(`Guardian: ${student.GuardianFullName ?? '_'}`, 25, textY);
        textY += 5;
        doc.text(`GuardianFirstContact: ${student.GuardianFirstContact ?? '_'}`, 25, textY);
        textY += 5;
        doc.text(`GuardianFirstContact: ${student.GuardianSecondContact ?? '_'}`, 25, textY);

        doc.save(`Student_ID_${student.StudentID}.pdf`);
    } catch (error) {
        console.error("Error generating ID card:", error);
    }
}
