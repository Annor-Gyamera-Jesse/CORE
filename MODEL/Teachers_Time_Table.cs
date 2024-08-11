namespace CORE.MODEL
{
    public enum CustomDayOfWeek
    {
        Monday,
        Tuesday,
        Wednesday,
        Thursday,
        Friday,
        Saturday,
        Sunday
    }

    public class Teachers_Time_Table
    {
        public class Timeslot
        {
            public int TimeslotID { get; set; }
            public TimeSpan StartTime { get; set; }
            public TimeSpan EndTime { get; set; }
        }

        public class Day
        {
            public int DayID { get; set; }
            public string DayName { get; set; }
        }

        public class Schedule
        {
            public int ScheduleID { get; set; }
            public string SchoolCourse { get; set; }
            public string SCID { get; set; }
            public string SchoolCourseID { get; set; } // Add this property
            public string ClassID { get; set; }
            public int TimeslotID { get; set; }
            public TimeSpan? StartTime { get; set; }
            public TimeSpan? EndTime { get; set; }
            public int DayID { get; set; }
            public DateTime SubjectStartTime { get; set; } = DateTime.Now; // Default value
            public DateTime SubjectEndTime { get; set; } = DateTime.Now; // Default value
            public CustomDayOfWeek DayName { get; set; } // Changed to enum
        }
    }  }
