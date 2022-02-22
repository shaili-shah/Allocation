using Allocation.ApiModel;
using Allocation.Models;
using AutoMapper;
using System;
using System.Collections.Generic;
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
                var config = new MapperConfiguration(cfg =>
                {
                    cfg.CreateMap<Role, RoleModel>();
                });
                var mapper = config.CreateMapper();
                Role role = new Role
                {
                    Name = roleName,
                    IsActive = true
                };
                dbContext.Roles.Add(role);
                dbContext.SaveChanges();
                RoleModel roleModel = mapper.Map<RoleModel>(role);
                return Ok(new ResultBase<RoleModel> { Data = roleModel , Success = true });
            }
            catch(Exception ex)
            {
                return InternalServerError(ex);
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
                    cfg.CreateMap<Role, RoleModel>();
                });
                var mapper = config.CreateMapper();
                var role = dbContext.Roles.FirstOrDefault(x => x.RoleId == model.RoleId);
                var role1 = mapper.Map<RoleModel, Role>(model);

                dbContext.Entry(role).CurrentValues.SetValues(role1);
                dbContext.SaveChanges();
                RoleModel roleModel = mapper.Map<RoleModel>(role1);
                return Ok(new ResultBase<RoleModel> {Data = roleModel , Success = true });
            }
            catch(Exception ex)
            {
                return InternalServerError(ex);
            }
        }

        [Route("RemoveRole")]
        [HttpPost]
        public IHttpActionResult RemoveRole(int roleId)
        {
            try
            {
                var role = dbContext.Roles.FirstOrDefault(x => x.RoleId == roleId);
                if (role.Users.Any())
                    return Ok(new ResultBase<RoleModel> { Msg = "can not delete role because role is in use" , Success = false});

                role.IsActive = false;
                dbContext.SaveChanges();
                return Ok(new ResultBase<RoleModel> { Msg = "role deleted successfully" , Success = true });
            }
            catch (Exception ex)
            {
                return InternalServerError(ex);
            }
        }

        [Route("AssignRole")]
        public IHttpActionResult AssignRole(int userId, int roleId)
        {
            try
            {
                User user = dbContext.Users.FirstOrDefault(x => x.UserId == userId);
                user.RoleId = roleId;
                dbContext.SaveChanges();
                return Ok(new ResultBase<User> { Msg = "role assign successfully" , Success = true });
            }
            catch(Exception ex)
            {
                return InternalServerError(ex);
            }
        }

        [Route("GetAll")]
        public IHttpActionResult GetAll()
        {
            try
            {
                var config = new MapperConfiguration(cfg =>  {
                    cfg.CreateMap<Role, RoleModel>();
                });
                var mapper = config.CreateMapper();
                List<Role> lstRole = dbContext.Roles.Where(x => x.IsActive && x.RoleId != 1).ToList();
                List<RoleModel> lstRoleModel = mapper.Map<List<RoleModel>>(lstRole);
                return Ok(new ResultBase<List<RoleModel>> { Data = lstRoleModel, Success = true});
            }
            catch(Exception ex)
            {
                return InternalServerError(ex);
            }
        }


    }
}