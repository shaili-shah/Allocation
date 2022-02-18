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
                return Ok(new { data = lstSkillModel });
            }
            catch(Exception ex)
            {
                throw ex;
            }
        }

        [Route("GetById")]
        [HttpGet]
        public IHttpActionResult Get(int id)
        {
            try
            {
                var config = new MapperConfiguration(cfg => {
                    cfg.CreateMap<Skill, SkillModel>();
                });
                var mapper = config.CreateMapper();
                var skill = dbContext.Skills.FirstOrDefault(x => x.Id == id);
                var skillModel = mapper.Map<Skill, SkillModel>(skill);
                return Ok(new { data = skillModel });
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        #endregion

        #region post

        [Authorize(Roles = "Admin")]
        [Route("AddSkill")]
        [HttpPost]
        public IHttpActionResult Add(string skillName)
        {
            try
            {
                Skill skill = new Skill
                {
                    Name = skillName,
                    IsActive = true
                };
                dbContext.Skills.Add(skill);
                dbContext.SaveChanges();
                return Ok();
            }
           catch(Exception ex)
            {
                throw ex;
            }
        }

        [Authorize(Roles = "Admin")]
        [Route("EditSkill")]
        [HttpPost]
        public IHttpActionResult Edit(SkillModel model)
        {
            try
            {
                var config = new MapperConfiguration(cfg => {
                    cfg.CreateMap<SkillModel, Skill>();
                });
                var mapper = config.CreateMapper();
                var skill = dbContext.Skills.Include(x => x.UserSkills).FirstOrDefault(x => x.Id == model.Id);
                var skill1 = mapper.Map<SkillModel, Skill>(model);

                dbContext.Entry(skill).CurrentValues.SetValues(skill1);
                dbContext.SaveChanges();
                return Ok();
            }
            catch(Exception ex)
            {
                throw ex;
            }
        }


        [Route("AssignSkill")]
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
        [Route("RemoveSkill")]
        [HttpPost]
        public IHttpActionResult RemoveSkill(int id)
        {
            try
            {
                var skill = dbContext.Skills.FirstOrDefault(x => x.Id == id);
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