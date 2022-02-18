using Microsoft.IdentityModel.Tokens;
using Microsoft.Owin;
using Microsoft.Owin.Security.Jwt;
using Owin;
using System.Text;
using System.Web.Configuration;

[assembly: OwinStartup(typeof(Allocation.Startup))]

namespace Allocation
{
    public class Startup
    {
        public void Configuration(IAppBuilder app)
        {
            app.UseJwtBearerAuthentication(
                new JwtBearerAuthenticationOptions
                {
                    //AuthenticationMode = AuthenticationMode.Windows.ToString(),
                    TokenValidationParameters = new TokenValidationParameters()
                    {
                        ValidateIssuer = true,
                        ValidateAudience = true,
                        ValidateIssuerSigningKey = true,
                        ValidIssuer = "Test.com", //some string, normally web url,  
                        ValidAudience = "Test.com",
                        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes("ThisismySecretKey"))
                    }
                });
        }
    }
}