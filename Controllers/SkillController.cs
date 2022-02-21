using Allocation.ApiModel;
using Allocation.Models;
using AutoMapper;
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Web.Http;

namespace Allocation.Controllers
{
    [RoutePrefix("Skill")]
    public class SkillController : ApiController
    {
        private AllocationEntities dbContext = new AllocationEntities();

        #region get
        
        [Route("GetAll")]
        [HttpGet]
        public IHttpActionResult GetAll()
        {
            try
            {
                var config = new MapperConfiguration(cfg => {
                    cfg.CreateMap<Skill, SkillModel>();
                });
                var mapper = config.CreateMapper();
                var lstSkill = dbContext.Skills.Where(x => x.IsActive).ToList();
                var lstSkillModel = mapper.Map<List<Skill>, List<SkillModel>>(lstSkill);
                //return Ok(new ResultBase<List<SkillModel>>{ Data = lstSkillModel , Success = true });
                return Ok(new { data = lstSkillModel });
            }
            catch(Exception ex)
            {
                throw ex;
            }
        }

        [Route("GetById")]
        [HttpGet]
        public IHttpActionResult Get(int skillId)
        {
            try
            {
                var config = new MapperConfiguration(cfg => {
                    cfg.CreateMap<Skill, SkillModel>();
                });
                var mapper = config.CreateMapper();
                var skill = dbContext.Skills.FirstOrDefault(x => x.SkillId == skillId);
                var skillModel = mapper.Map<Skill, SkillModel>(skill);
                return Ok(new { data = skillModel });
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        [Route("GetSkillsByUserId")]
        [HttpGet]
        public IHttpActionResult GetSkillsByUserId(int userId)
        {
            try
            {
                var config = new MapperConfiguration(cfg =>
                {
                    cfg.CreateMap<UserSkill, UserSkillResponseModel>();
                });
                var mapper = config.CreateMapper();
                var lstUserSkill = dbContext.UserSkills.Where(x => x.UserId == userId).ToList();

                List<UserSkillResponseModel> lstmodel = new List<UserSkillResponseModel>();
                lstmodel = mapper.Map<List<UserSkill>, List<UserSkillResponseModel>>(lstUserSkill);
               
                return Ok(new { data = lstmodel });
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        [Route("GetUsersBySkillId")]
        [HttpGet]
        public IHttpActionResult GetUsersBySkillId(int skillId)
        {
            try
            {
                var config = new MapperConfiguration(cfg =>
                {
                    cfg.CreateMap<UserSkill, UserSkillResponseModel>();
                });
                var mapper = config.CreateMapper();
                var lstUser = dbContext.UserSkills.Where(x => x.SkillId == skillId).ToList();

                List<UserSkillResponseModel> lstmodel = new List<UserSkillResponseModel>();
                lstmodel = mapper.Map<List<UserSkill>, List<UserSkillResponseModel>>(lstUser);

                return Ok(new { data = lstmodel });
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        #endregion

        #region post

        [Authorize(Roles = "Admin")]
        [Route("Add Skill")]
        [HttpPost]
        public IHttpActionResult Add(string skillName)
        {
            try
            {
                var config = new MapperConfiguration(cfg => {
                    cfg.CreateMap<Skill, SkillModel>();
                });
                var mapper = config.CreateMapper();
                Skill skill = new Skill
                {
                    Name = skillName,
                    IsActive = true
                };
                dbContext.Skills.Add(skill);
                dbContext.SaveChanges();
                SkillModel skillModel =  mapper.Map<SkillModel>(skill);
                return Ok(new { data = skillModel });
            }
           catch(Exception ex)
            {
                throw ex;
            }
        }

        [Authorize(Roles = "Admin")]
        [Route("Edit Skill")]
        [HttpPost]
        public IHttpActionResult Edit(SkillModel model)
        {
            try
            {
                var config = new MapperConfiguration(cfg => {
                    cfg.CreateMap<SkillModel, Skill>();
                    cfg.CreateMap<Skill, SkillModel>();
                });
                var mapper = config.CreateMapper();
                var skill = dbContext.Skills.Include(x => x.UserSkills).FirstOrDefault(x => x.SkillId == model.SkillId);
                var skill1 = mapper.Map<SkillModel, Skill>(model);

                dbContext.Entry(skill).CurrentValues.SetValues(skill1);
                dbContext.SaveChanges();
                SkillModel skillModel = mapper.Map<SkillModel>(skill1);
                return Ok(new { data = skillModel });
            }
            catch(Exception ex)
            {
                throw ex;
            }
        }


        [Route("Assign Skill")]
        [HttpPost]
        public IHttpActionResult AssignSkill(int userId, int skillId)
        {
            try
            {
                UserSkill userSkill = new UserSkill
                {
                    UserId = userId,
                    SkillId = skillId
                };
                dbContext.UserSkills.Add(userSkill);
                dbContext.SaveChanges();
                return Ok();
            }
            catch (Exception ex)
            {
                throw ex;
            }

        }

        #endregion

        [Authorize(Roles = "Admin")]
        [Route("Remove Skill")]
        [HttpPost]
        public IHttpActionResult RemoveSkill(int skillId)
        {
            try
            {
                var skill = dbContext.Skills.FirstOrDefault(x => x.SkillId == skillId);
                if (skill.UserSkills.Any())
                    return Ok(new { data = "can not delete skill because skill is in use" });
                skill.IsActive = false;

                dbContext.SaveChanges();
                return Ok(new { data = "skil deleted successfully" });
            }
            catch(Exception ex)
            {
                throw ex;
            }
        }
      
    }
}