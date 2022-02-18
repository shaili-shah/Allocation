using Allocation.ApiModel;
using Allocation.Models;
using AutoMapper;
using System;
using System.Linq;
using System.Web.Http;

namespace Allocation.Controllers
{
    [Authorize(Roles = "Admin")]
    [RoutePrefix("Role")]
    public class RoleController : ApiController
    {
        private AllocationEntities dbContext = new AllocationEntities();

        [Route("AddRole")]
        [HttpPost]
        public IHttpActionResult Add(string roleName)
        {
            try
            {
                Role role = new Role
                {
                    Name = roleName,
                    IsActive = true
                };
                dbContext.Roles.Add(role);
                dbContext.SaveChanges();
                return Ok();
            }
            catch(Exception ex)
            {
                throw ex;
            }
        }

        [Route("EditRole")]
        [HttpPost]
        public IHttpActionResult Edit(RoleModel model)
        {
            try
            {
                var config = new MapperConfiguration(cfg => {
                    cfg.CreateMap<RoleModel, Role>();
                });
                var mapper = config.CreateMapper();
                var role = dbContext.Roles.FirstOrDefault(x => x.Id == model.Id);
                var role1 = mapper.Map<RoleModel, Role>(model);

                dbContext.Entry(role).CurrentValues.SetValues(role1);
                dbContext.SaveChanges();
                return Ok();
            }
            catch(Exception ex)
            {
                throw ex;
            }
        }

        [Route("RemoveRole")]
        [HttpPost]
        public IHttpActionResult RemoveRole(int id)
        {
            try
            {
                var role = dbContext.Roles.FirstOrDefault(x => x.Id == id);
                if (role.Users.Any())
                    return Ok(new { data = "can not delete role because role is in use" });

                role.IsActive = false;
                dbContext.SaveChanges();
                return Ok(new { data = "role deleted successfully" });
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }


    }
}