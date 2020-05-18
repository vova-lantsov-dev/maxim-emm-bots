using System.ComponentModel.DataAnnotations;

namespace Host.Models
{
    public sealed class LogInModel
    {
        [Required]
        [StringLength(64, MinimumLength = 4)]
        public string Username { get; set; }

        [Required]
        [StringLength(64, MinimumLength = 12)]
        public string Password { get; set; }
    }
}