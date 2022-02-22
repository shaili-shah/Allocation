using Allocation.ApiModel;
using Allocation.Models;
using AutoMapper;
using Microsoft.AspNetCore.Http;
using Microsoft.IdentityModel.Tokens;
using System;
using System.Collections.Generic;
using System.Data.Entity;
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

                    bool isUserExist = dbContext.Users.Any(x => x.Email == model.Email);
                    if (isUserExist) return Ok(new ResultBase<UserModel> { Msg = "User already registered with this email" , Success = false});
                    
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
                    return Ok(new ResultBase<RegisterResponseModel> { Data = registerResponseModel, Success = true, Msg = "User registered successfully" });
                }
                catch (Exception ex)
                {
                    dbContextTransaction.Rollback();
                    return InternalServerError(ex);
                }
            }
        }

        #region login

        [Route("Login")]
        [HttpPost]
        public IHttpActionResult Login(string email, string password)
        {
            IHttpActionResult response = Unauthorized();
            var user = AuthenticateUser(email, password);
            if (user != null)
            {
                var tokenString = GenerateJSONWebToken(user);
                LoginResponseModel model = new LoginResponseModel { Token = tokenString };
                return Ok(new ResultBase<LoginResponseModel> { Data = model, Success = true });
            }
            return response;
        }

        [Authorize]
        [Route("CurrentUser")]
        [HttpGet]
        public IHttpActionResult CurrentUser()
        {
            var claimsIdentity = this.User.Identity as ClaimsIdentity;
            CurrentUserModel model = new CurrentUserModel
            {
                Name = claimsIdentity.Claims?.FirstOrDefault(x => x.Type.Equals("name", StringComparison.OrdinalIgnoreCase))?.Value,
                Email = claimsIdentity.Claims?.FirstOrDefault(x => x.Type.Equals("http://schemas.xmlsoap.org/ws/2005/05/identity/claims/emailaddress", StringComparison.OrdinalIgnoreCase))?.Value,
                Role = claimsIdentity.Claims?.FirstOrDefault(x => x.Type.Equals("http://schemas.microsoft.com/ws/2008/06/identity/claims/role", StringComparison.OrdinalIgnoreCase))?.Value,
                UserId =  Convert.ToInt32(claimsIdentity.Claims?.FirstOrDefault(x => x.Type.Equals("UserId", StringComparison.OrdinalIgnoreCase))?.Value),
            };
            return Ok(new ResultBase<CurrentUserModel> { Data = model , Success = true });
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
            new Claim("UserId",userInfo.UserId.ToString()),
            new Claim(ClaimTypes.Role, role?.Name),
        };

            var token = new JwtSecurityToken("Test.com",
                "Test.com",
                claims,
                expires: DateTime.Now.AddMinutes(120),
                signingCredentials: credentials);

            return new JwtSecurityTokenHandler().WriteToken(token);
        }

        #endregion

        #region get

        [Authorize(Roles = "Admin")]
        [Route("GetAll")]
        public IHttpActionResult GetAll()
        {
            try
            {
                var config = new MapperConfiguration(cfg =>
                {
                    cfg.CreateMap<User, UserModel>();
                });
                var mapper = config.CreateMapper();
                var users = dbContext.Users.Where(x => x.RoleId != 1).ToList(); // get all except admin
                var lstUserModel = mapper.Map<List<User>, List<UserModel>>(users);
                return Ok(new ResultBase<List<UserModel>> { Data = lstUserModel, Success = true });
            }
            catch (Exception ex)
            {
                return InternalServerError(ex);
            }
        }

        [Authorize]
        [Route("GetUserById")]
        public IHttpActionResult GetUserById(int userId)
        {
            try
            {
                var config = new MapperConfiguration(cfg =>
                {
                    cfg.CreateMap<User, UserProfileModel>();
                    cfg.CreateMap<UserSkill, UserSkillResponseModel>();
                });
                var mapper = config.CreateMapper();
                User user = dbContext.Users.FirstOrDefault(x => x.UserId == userId);
                List<UserSkill> LstUserSkill = dbContext.UserSkills.Where(x => x.UserId == userId).ToList();

                UserProfileModel profileModel = mapper.Map<UserProfileModel>(user);
                profileModel.LstUserSkill = mapper.Map<List<UserSkillResponseModel>>(user.UserSkills);
                foreach (var item in profileModel.LstUserSkill)
                {
                    item.IsAllocated =  LstUserSkill.Any(x => x.UserSkillId == item.UserSkillId && x.AllocatedSeats.Count>0);
                }
                return Ok(new ResultBase<UserProfileModel> { Data = profileModel, Success = true });
            }
            catch (Exception ex)
            {
                return InternalServerError(ex);
            }
        }

        #endregion

        [Authorize]
        [Route("EditUser")]
        public IHttpActionResult EditUser(UserProfileEditModel profileModel)
        {
            using (var dbContextTransaction = dbContext.Database.BeginTransaction())
            {
                try
                {
                    var config = new MapperConfiguration(cfg =>
                    {
                        cfg.CreateMap<UserProfileEditModel, User>();
                    });
                    var mapper = config.CreateMapper();
                    var user = mapper.Map<User>(profileModel);
                    dbContext.Entry(user).State = EntityState.Modified;
                    dbContext.SaveChanges();

                    // remove existing user skill
                    List<UserSkill> Lstuserskill = dbContext.UserSkills.Where(x => x.UserId == profileModel.UserId).ToList();
                    dbContext.UserSkills.RemoveRange(Lstuserskill);
                    dbContext.SaveChanges();

                    // add new user skill
                    List<UserSkill> userSkills = new List<UserSkill>();
                    foreach (var item in profileModel.LstSkillId)
                    {
                        UserSkill userSkill = new UserSkill();
                        userSkill.SkillId = item;
                        userSkill.UserId = profileModel.UserId;
                        userSkills.Add(userSkill);
                    }
                    dbContext.UserSkills.AddRange(userSkills);
                    dbContext.SaveChanges();

                    dbContextTransaction.Commit();
                    return Ok(new ResultBase<UserProfileEditModel> { Msg = "Update successfully", Success = true });
                }
                catch (Exception ex)
                {
                    dbContextTransaction.Rollback();
                    return InternalServerError(ex);
                }
            }
        }

        #region allocate seat

        [Route("RequestSeat")]
        [HttpPost]
        public IHttpActionResult RequestSeat(int userSkillId, DateTime date)
        {
            try
            {
                bool IsAllSeatAllocated = CheckIsAllSeatAllocated(date);
                if (IsAllSeatAllocated) return Ok(new ResultBase<AllocatedSeatModel> { Msg = "Seat not available for this date", Success = false });

                bool IsSeatAlreadyAllocated = dbContext.AllocatedSeats.Any(x => EntityFunctions.TruncateTime(x.Date) == date.Date
                && x.UserSkillId == userSkillId);
                if (IsSeatAlreadyAllocated)
                {
                    return Ok(new ResultBase<AllocatedSeatModel> { Msg = "Seat already allocated for this date, please select another date", Success = false });
                }

                int randomNumber = GenerateRandomNumber(userSkillId, date);

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

                return Ok(new ResultBase<AllocatedSeatModel> { Data = allocatedSeatModel, Success = true });
            }
            catch (Exception ex)
            {
                return InternalServerError(ex);
            }
        }

        private bool CheckIsAllSeatAllocated(DateTime date)
        {
            var LstSeatNo = Enumerable.Range(Constant.StartRange, (Constant.Count-1)).ToList();
            var LstAllocatedSeatNo = dbContext.AllocatedSeats.Where(x => EntityFunctions.TruncateTime(x.Date) == date.Date).Select(x => x.SeatNo).ToList();

            var set = new HashSet<int>(LstSeatNo);
            bool IsAllSeatAllocated = set.SetEquals(LstAllocatedSeatNo);
            return IsAllSeatAllocated;
        }

        private int GenerateRandomNumber(int userSkillId, DateTime date)
        {
            Random random = new Random();
            int randomNumber = random.Next(Constant.StartRange, Constant.Count);
            bool IsSeatAllocated = dbContext.AllocatedSeats.Any(x => EntityFunctions.TruncateTime(x.Date) == date.Date &&
            x.SeatNo == randomNumber);

            var userSkill = dbContext.UserSkills.FirstOrDefault(x => x.UserSkillId == userSkillId);
            var allocatedSeatsOfSimilarSkill = dbContext.AllocatedSeats.Where(x => x.UserSkill.SkillId == userSkill.SkillId && EntityFunctions.TruncateTime(x.Date) == date.Date).ToList();

            // check if next seat available
            foreach (var allocatedSeat in allocatedSeatsOfSimilarSkill)
            {
                int randomNumberNearSimillarSkill = allocatedSeat.SeatNo != 100 ? allocatedSeat.SeatNo + 1 : allocatedSeat.SeatNo;
                bool IsSeatNumAllocated = dbContext.AllocatedSeats.Any(x => EntityFunctions.TruncateTime(x.Date) == date.Date
                && x.SeatNo == randomNumberNearSimillarSkill);
                if (!IsSeatNumAllocated)
                {
                    return randomNumberNearSimillarSkill;
                }
            }

            // check if previous seat available
            foreach (var allocatedSeat in allocatedSeatsOfSimilarSkill)
            {
                int randomNumberNearSimillarSkill = allocatedSeat.SeatNo != 1 ? allocatedSeat.SeatNo - 1 : allocatedSeat.SeatNo;
                bool IsSeatNumAllocated = dbContext.AllocatedSeats.Any(x => EntityFunctions.TruncateTime(x.Date) == date.Date
                && x.SeatNo == randomNumberNearSimillarSkill);
                if (!IsSeatNumAllocated)
                {
                    return randomNumberNearSimillarSkill;
                }
            }

            if (IsSeatAllocated)
            {
                return GenerateRandomNumber(userSkillId, date);
            }
            return randomNumber;
        }

        #endregion

        //GetBookingByUserId()
        //{

        //}


    }
}