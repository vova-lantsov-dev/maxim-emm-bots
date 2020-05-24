using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Host.Authentication
{
    [Table("users", Schema = "public")]
    public sealed class ApplicationUser
    {
        [Key, DatabaseGenerated(DatabaseGeneratedOption.Identity), Required, Column("user_id", TypeName = "integer")]
        public int Id { get; set; }
        
        [Column("username", TypeName = "text"), Required]
        public string Username { get; set; }
        
        [Column("password_hash", TypeName = "text"), Required]
        public string PasswordHash { get; set; }
        
        [Column("password_salt", TypeName = "text"), Required]
        public string Salt { get; set; }
        
        [Column("user_role", TypeName = "text"), Required]
        public string Role { get; set; }
    }
}