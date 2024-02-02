using System.ComponentModel.DataAnnotations;

namespace CORE.MODEL
{
    public class User
    {
        public int UserID { get; set; }
        [Required(ErrorMessage = "Username is required.")]
        public string UserName { get; set; }
        [Required(ErrorMessage = "Password is required.")]
        public string Password { get; set; }
    }
}
