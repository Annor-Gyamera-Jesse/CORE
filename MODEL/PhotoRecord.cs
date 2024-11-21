namespace CORE.MODEL
{
    public class PhotoRecord
    {
        public int PhotoID { get; set; }
        public string Class { get; set; }
        public string StudentName { get; set; }
        public int StudentID { get; set; }
        public string PhotoTitle { get; set; }
        public string PhotoDescription { get; set; }
        public byte[] PhotoData { get; set; }
    }
}
