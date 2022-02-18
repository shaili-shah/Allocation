using Allocation;
using Swashbuckle.Application;
using System.Web.Http;
using WebActivatorEx;

[assembly: PreApplicationStartMethod(typeof(SwaggerConfig), "Register")]

namespace Allocation
{
    public class SwaggerConfig
    {
        public static void Register()
        {
            var thisAssembly = typeof(SwaggerConfig).Assembly;

            GlobalConfiguration.Configuration 
                .EnableSwagger(c =>
                    {
                        c.SingleApiVersion("v1", "Allocation");
                        
                        // add bearer authorization
                        c.DocumentFilter<SwaggerPathDescriptionFilter>();
                    })
                .EnableSwaggerUi(c =>
                    {

                    });
        }
    }
}