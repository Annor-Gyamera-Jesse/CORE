using CORE.MODEL;
using static CORE.MODEL.Teachers_Time_Table;
using System.Data.SqlClient;
using System.Data;
using Dapper;

namespace CORE.SERVICE
{
    public class TimetableService
    {
        private readonly string _connectionString;

        public TimetableService(string connectionString)
        {
            _connectionString = connectionString;
        }

        private IDbConnection Connection => new SqlConnection(_connectionString);

        public async Task<IEnumerable<Timeslot>> GetTimeslotsAsync()
        {
            using var connection = Connection;
            return await connection.QueryAsync<Timeslot>("SELECT * FROM SchoolManagement.Timeslots");
        }

        public async Task<IEnumerable<Day>> GetDaysAsync()
        {
            using var connection = Connection;
            return await connection.QueryAsync<Day>("SELECT * FROM SchoolManagement.Days");
        }

        public async Task<IEnumerable<SchoolCourse>> GetCoursesAsync()
        {
            using var connection = Connection;
            return await connection.QueryAsync<SchoolCourse>("SELECT * FROM SchoolManagement.SchoolCourse");
        }

        public async Task<IEnumerable<Class>> GetClassesAsync()
        {
            using var connection = Connection;
            return await connection.QueryAsync<Class>("SELECT * FROM SchoolManagement.Class");
        }

        public async Task<IEnumerable<Schedule>> GetScheduleAsync()
        {
            using var connection = Connection;
            string sql = @"SELECT s.ScheduleID, sc.SchoolCourse, c.ClassID, t.StartTime, t.EndTime, d.DayName
                       FROM SchoolManagement.Schedule s
                       JOIN SchoolManagement.SchoolCourse sc ON s.SCID = sc.SCID
                       JOIN SchoolManagement.Class c ON s.ClassID = c.ClassID
                       JOIN SchoolManagement.Timeslots t ON s.TimeslotID = t.TimeslotID
                       JOIN SchoolManagement.Days d ON s.DayID = d.DayID";
            return await connection.QueryAsync<Schedule>(sql);
        }

        public async Task<int> AddScheduleAsync(Schedule schedule)
        {
            using var connection = Connection;
            string sql = @"INSERT INTO SchoolManagement.Schedule (SCID, ClassID, TimeslotID, DayID)
                       VALUES (@SCID, @ClassID, @TimeslotID, @DayID)";
            return await connection.ExecuteAsync(sql, schedule);
        }

        public async Task<int> UpdateScheduleAsync(Schedule schedule)
        {
            using var connection = Connection;
            string sql = @"UPDATE SchoolManagement.Schedule
                       SET SCID = @SCID, ClassID = @ClassID, TimeslotID = @TimeslotID, DayID = @DayID
                       WHERE ScheduleID = @ScheduleID";
            return await connection.ExecuteAsync(sql, schedule);
        }

        public async Task<int> DeleteScheduleAsync(int scheduleID)
        {
            using var connection = Connection;
            string sql = @"DELETE FROM SchoolManagement.Schedule WHERE ScheduleID = @ScheduleID";
            return await connection.ExecuteAsync(sql, new { ScheduleID = scheduleID });
        }
    }

}
