using Swashbuckle.Swagger;
using System.Collections.Generic;
using System.Linq;
using System.Web.Http.Description;

namespace Allocation
{
    public class SwaggerPathDescriptionFilter : IDocumentFilter
    {
        private string tokenUrlRoute = "Auth";
        // the above is the action which returns token against valid credentials
        private Dictionary<HeaderType, Parameter> headerDictionary;
        private enum HeaderType { TokenAuth };

        public void Apply(SwaggerDocument swaggerDoc, SchemaRegistry schemaRegistry, IApiExplorer apiExplorer)
        {
            CreateHeadersDict();

            var allOtherPaths = swaggerDoc.paths.Where(entry => !entry.Key.Contains(tokenUrlRoute)) //get the other paths which expose API resources and require token auth
                .Select(entry => entry.Value)
                .ToList();

            foreach (var path in allOtherPaths)
            {
                AddHeadersToPath(path, HeaderType.TokenAuth);
            }
        }

        /// <summary>
        /// Adds the desired header descriptions to the path's parameter list
        /// </summary>
        private void AddHeadersToPath(PathItem path, params HeaderType[] headerTypes)
        {
            if (path.parameters != null)
            {
                path.parameters.Clear();
            }
            else
            {
                path.parameters = new List<Parameter>();
            }

            foreach (var type in headerTypes)
            {
                path.parameters.Add(headerDictionary[type]);
            }

        }

        /// <summary>
        /// Creates a dictionary containin all header descriptions
        /// </summary>
        private void CreateHeadersDict()
        {
            headerDictionary = new Dictionary<HeaderType, Parameter>();


            headerDictionary.Add(HeaderType.TokenAuth, new Parameter() //token auth header
            {
                name = "Authorization",
                @in = "header",
                type = "string",
                description = "Token Auth.",
                required = true,
                @default = "Bearer "
            });
        }
    }
}