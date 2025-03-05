namespace CORE.MODEL
{
    public class Staff
    {
        public int StaffID { get; set; }
        public string StaffFirstName { get; set; }
        public string StaffLastName { get; set; }
        public DateTime StaffDateOfBirth { get; set; }
        public string StaffGender { get; set; }
        public string StaffAddress { get; set; }
        public string StaffPhoneNumber { get; set; }
        public string StaffEmail { get; set; }
        public string Position { get; set; }
        public string Department { get; set; }
        public string EmploymentStatus { get; set; } // Active, Suspended, Terminated
        public string BankName { get; set; }
        public string AccountName { get; set; }
        public string AccountNumber { get; set; }
        public decimal Salary { get; set; }
        public decimal OverTime { get; set; }
        public decimal TaxMoney { get; set; }
        public byte[] ImageData { get; set; }
        public bool IsEnabled { get; set; } = true;
        public DateTime? ClockIN { get; set; }
        public DateTime? ClockOUT { get; set; }
        public string EmergencyContactName { get; set; }
        public string EmergencyContactRelationship { get; set; }
        public string EmergencyContactPhone { get; set; }
        public DateTime DateHired { get; set; } = DateTime.Now;
        public int LeaveBalance { get; set; } = 20; // Default leave days per year
        public float PerformanceRating { get; set; } = 0.0f; // HR performance rating
        public string StaffFullName => $"{StaffFirstName} {StaffLastName}";
        // SSNIT Fields
        public string SSNIT { get; set; }  // Dropdown (Y/N)
        public decimal BasicSalary { get; set; }
        public decimal PAYE { get; set; }
        public decimal SSNITTIER2 { get; set; }

        // Account Details Fields
        public string VotersID { get; set; }  // Dropdown (Yes/No)
        public string HealthInsurance { get; set; }  // Dropdown (Yes/No)
        public string GhanaCard { get; set; }  // Dropdown (Yes/No)
        public string Bank { get; set; }  // Selected Bank Name
        public string Remarks { get; set; }
        public string SSNITNumber { get; set; }
        public string CategoryName { get; set; }

    }

    public class PaymentRecord
    {
        public int StaffID { get; set; } // Foreign Key (Staff Table)
        public decimal TaxMoney { get; set; } // Tax Deductions
        public decimal OverTime { get; set; } // Extra Work Payment
        public decimal Salary { get; set; } // Base Salary
        public string BankName { get; set; } // Staff's Bank Name
        public string AccountNumber { get; set; } // Staff's Account Number
        public string AccountName { get; set; } // Account Holder's Name
        public DateTime PayedOn { get; set; } // Payment Date
        public int SalaryFor { get; set; } // Month of Salary (1 = Jan, 12 = Dec)
        public int PaymentYear { get; set; } // Year of Payment
        public bool IsEnabled { get; set; } // Payment Active Status
    }

    public class PaymentCategory
    {
        public int CategoryID { get; set; } // Unique ID for the category
        public string CategoryName { get; set; } // Name of the category (e.g., "Cat1", "Cat2")
        public decimal Amount { get; set; } // Salary amount associated with the category
    }
}
