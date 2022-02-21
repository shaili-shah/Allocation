using Allocation.ApiModel;
using Allocation.Models;
using AutoMapper;
using Microsoft.IdentityModel.Tokens;
using System;
using System.Collections.Generic;
using System.Data.Entity.Core.Objects;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Security.Claims;
using System.Text;
using System.Web.Http;

namespace Allocation.Controllers
{
    [RoutePrefix("User")]
    public class UserController : ApiController
    {
        private AllocationEntities dbContext = new AllocationEntities();

        [Route("Register")]
        public IHttpActionResult Register(UserModel model)
        {

            using (var dbContextTransaction = dbContext.Database.BeginTransaction())
            {
                try
                {
                    bool isAdminExist = dbContext.Users.Any(x => x.Email == Constant.adminEmail);
                    if (!isAdminExist)
                    {
                        User adminUser = new User();
                        adminUser.Email = Constant.adminEmail;
                        adminUser.Password = Constant.adminPassword;
                        adminUser.Name = "admin";
                        adminUser.RoleId = 1;
                        adminUser.Phone = Constant.adminPhone;
                        dbContext.Users.Add(adminUser);
                        dbContext.SaveChanges();

                        UserSkill Skill = new UserSkill
                        {
                            UserId = adminUser.UserId,
                            SkillId = 1 // default skill
                        };
                        dbContext.UserSkills.Add(Skill);
                        dbContext.SaveChanges();
                    }

                    var config = new MapperConfiguration(cfg =>
                    {
                        cfg.CreateMap<UserModel, User>();
                        cfg.CreateMap<User, RegisterResponseModel>();
                    });
                    var mapper = config.CreateMapper();

                    var user = mapper.Map<UserModel, User>(model);
                    dbContext.Users.Add(user);
                    dbContext.SaveChanges();

                    UserSkill userSkill = new UserSkill
                    {
                        UserId = user.UserId,
                        SkillId = model.SkillId == 0 ? 1 : model.SkillId // default skill
                    };
                    dbContext.UserSkills.Add(userSkill);
                    dbContext.SaveChanges();

                    dbContextTransaction.Commit();
                    RegisterResponseModel registerResponseModel = new RegisterResponseModel();
                    registerResponseModel = mapper.Map<User, RegisterResponseModel>(user);
                    registerResponseModel.UserSkillId = userSkill.UserSkillId;
                    registerResponseModel.SkillId = userSkill.SkillId;
                    return Ok(new { data = registerResponseModel });
                }
                catch(Exception ex)
                {
                    dbContextTransaction.Rollback();
                    throw ex;
                }
            }
        }


        [Route("Login")]
        [HttpPost]
        public IHttpActionResult Login(string email, string password)
        {
            IHttpActionResult response = Unauthorized();
            var user = AuthenticateUser(email, password);

            if (user != null)
            {
                var tokenString = GenerateJSONWebToken(user);
                return Ok(new { data = tokenString });
            }
            return response;
        }

        private UserModel AuthenticateUser(string email, string password)
        {
            UserModel user = null;

            //Validate the User Credentials    
            if (!string.IsNullOrWhiteSpace(email) && !string.IsNullOrWhiteSpace(password))
            {
                var validateUser = dbContext.Users.FirstOrDefault(x => x.Email == email && x.Password == password);
                if (validateUser != null)
                {
                    var config = new MapperConfiguration(cfg =>
                    {
                        cfg.CreateMap<User, UserModel>();
                    });
                    var mapper = config.CreateMapper();
                    user = mapper.Map<User, UserModel>(validateUser);
                }
            }
            return user;
        }

        private string GenerateJSONWebToken(UserModel userInfo)
        {
            var securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes("ThisismySecretKey"));
            var credentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);
            var role = dbContext.Roles.FirstOrDefault(x => x.RoleId == userInfo.RoleId);
            var claims = new[] {
            new Claim(JwtRegisteredClaimNames.Name, userInfo.Name),
            new Claim(JwtRegisteredClaimNames.Email, userInfo.Email),
            new Claim("Phone", userInfo.Phone.ToString()),
            new Claim(ClaimTypes.Role, role?.Name),
        };

            var token = new JwtSecurityToken("Test.com",
                "Test.com",
                claims,
                expires: DateTime.Now.AddMinutes(120),
                signingCredentials: credentials);

            return new JwtSecurityTokenHandler().WriteToken(token);
        }

        [Authorize(Roles = "Admin")]
        [Route("GetAll")]
        public IHttpActionResult GetAll()
        {
            var config = new MapperConfiguration(cfg =>
            {
                cfg.CreateMap<User, UserModel>();
            });
            var mapper = config.CreateMapper();
            var users = dbContext.Users.Where(x => x.RoleId != 1).ToList(); // get all except admin
            var lstUserModel = mapper.Map<List<User>, List<UserModel>>(users);
            return Ok(new { data = lstUserModel });
        }

        #region allocate seat

        [Route("Request Seat")]
        [HttpPost]
        public IHttpActionResult RequestSeat(int userSkillId, DateTime date)
        {
            try
            {
                bool IsAllSeatAllocated = CheckIsAllSeatAllocated(date);
                if (IsAllSeatAllocated) return Ok(new { data = "Seat not available for this date" });

                bool IsSeatAlreadyAllocated = dbContext.AllocatedSeats.Any(x => EntityFunctions.TruncateTime(x.Date) == date.Date
                && x.UserSkillId == userSkillId);
                if (IsSeatAlreadyAllocated)
                {
                    return Ok(new { data = "Seat already allocated for this date, please select another date." });
                }

                int randomNumber = GenerateRandomNumber(userSkillId,date);

                AllocatedSeat allocatedSeat = new AllocatedSeat
                {
                    SeatNo = randomNumber,
                    Date = date,
                    UserSkillId = userSkillId
                };
                dbContext.AllocatedSeats.Add(allocatedSeat);
                dbContext.SaveChanges();

                var config = new MapperConfiguration(cfg =>
                {
                    cfg.CreateMap<AllocatedSeat, AllocatedSeatModel>();
                });
                var mapper = config.CreateMapper();
                var allocatedSeatModel = mapper.Map<AllocatedSeat, AllocatedSeatModel>(allocatedSeat);
               
                return Ok(new { data = allocatedSeatModel });
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        private bool CheckIsAllSeatAllocated(DateTime date)
        {
            var LstSeatNo = Enumerable.Range(Constant.StartRange, Constant.EndRange).ToList();
            var LstAllocatedSeatNo = dbContext.AllocatedSeats.Where(x => EntityFunctions.TruncateTime(x.Date) == date.Date).Select(x => x.SeatNo).ToList();

            var set = new HashSet<int>(LstSeatNo);
            bool IsAllSeatAllocated = set.SetEquals(LstAllocatedSeatNo);
            return IsAllSeatAllocated;
        }

        private int GenerateRandomNumber(int userSkillId, DateTime date)
        {
            Random random = new Random();
            int randomNumber = random.Next(Constant.StartRange, Constant.EndRange);
            bool IsSeatAllocated = dbContext.AllocatedSeats.Any(x => EntityFunctions.TruncateTime(x.Date) == date.Date &&
            x.SeatNo == randomNumber);

           var userSkill = dbContext.UserSkills.FirstOrDefault(x => x.UserSkillId == userSkillId);
           var allocatedSeatsOfSimilarSkill = dbContext.AllocatedSeats.Where(x => x.UserSkill.SkillId == userSkill.SkillId).ToList();
            
            // check if next seat available
            foreach(var allocatedSeat in allocatedSeatsOfSimilarSkill)
            {
                randomNumber = allocatedSeat.SeatNo != 100 ? allocatedSeat.SeatNo +1 : allocatedSeat.SeatNo;
                bool IsSeatNumAllocated = dbContext.AllocatedSeats.Any(x => EntityFunctions.TruncateTime(x.Date) == date.Date
                && x.SeatNo == randomNumber);
                if (!IsSeatNumAllocated)
                {
                    return randomNumber;
                }
            }

            // check if previous seat available
            foreach (var allocatedSeat in allocatedSeatsOfSimilarSkill)
            {
                randomNumber = allocatedSeat.SeatNo != 1 ? allocatedSeat.SeatNo - 1 : allocatedSeat.SeatNo;
                bool IsSeatNumAllocated = dbContext.AllocatedSeats.Any(x => EntityFunctions.TruncateTime(x.Date) == date.Date
                && x.SeatNo == randomNumber);
                if (!IsSeatNumAllocated)
                {
                    return randomNumber;
                }
            }

            if (IsSeatAllocated)
            {
                return GenerateRandomNumber(userSkillId, date);
            }
            return randomNumber;
        }

        #endregion
      
    }
}