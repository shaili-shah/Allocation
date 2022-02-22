using System.Collections.Generic;

namespace Allocation.ApiModel
{
    public class UserProfileEditModel
    {
        public int UserId { get; set; }

        public string Name { get; set; }

        public string Email { get; set; }

        public string Phone { get; set; }

        public int RoleId { get; set; }

        public string Password { get; set; }

        public List<int> LstSkillId { get; set; }
    }
}