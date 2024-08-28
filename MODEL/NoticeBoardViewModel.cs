namespace CORE.MODEL
{
    public class NoticeBoardViewModel
    {
        public class NoticeBoard
        {
            public int NoticeID { get; set; }
            public string Title { get; set; }
            public string Content { get; set; }
            public string Author { get; set; }
            public DateTime DatePosted { get; set; }
            public DateTime? ExpiryDate { get; set; }
            public bool IsActive { get; set; }
        }

        public class UserNoticeReadStatus
        {
            public int UserNoticeReadStatusID { get; set; }
            public int UserID { get; set; }
            public int NoticeID { get; set; }
            public bool IsRead { get; set; }
            public DateTime ReadDate { get; set; }
        }
    }
}
