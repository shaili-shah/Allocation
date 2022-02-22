namespace Allocation.ApiModel
{
    public class UserSkillResponseModel
    {
        public int UserSkillId { get; set; }

        public int UserId { get; set; }

        public string UserName { get; set; }

        public int SkillId { get; set; }

        public string SkillName { get; set; }

        public bool IsAllocated { get; set; }

    }
}