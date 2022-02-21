namespace Allocation.ApiModel
{
    public class RegisterResponseModel
    {
        public int UserId { get; set; }

        public int UserSkillId { get; set; }

        public string Name { get; set; }

        public string Email { get; set; }

        public string Phone { get; set; }

        public int RoleId { get; set; }

        public string Password { get; set; }

        public int SkillId { get; set; }
    }
}