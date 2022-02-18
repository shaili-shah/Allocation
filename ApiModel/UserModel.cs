using System.ComponentModel.DataAnnotations;

namespace Allocation.ApiModel
{
    public class UserModel
    {
        [Required]
        public string Name { get; set; }

        [Required]
        public string Email { get; set; }

        [Required]
        public string Phone { get; set; }

        [Required]
        public int RoleId { get; set; }

        [Required]
        public string Password { get; set; } 

        public int SkillId { get; set; }
    }
}