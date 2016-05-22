using System.Diagnostics;
using System.Web.Http;
using Metamory.WebApi.Controllers.WebApi;
using Newtonsoft.Json.Serialization;

namespace ContentVersioning.Web
{
    public static class WebApiConfig
    {
        public static void Register(HttpConfiguration config)
        {
			Debug.WriteLine(typeof(ContentController));


            // Web API configuration and services
			config.EnableCors();

            // Web API routes
			config.MapHttpAttributeRoutes();

			//config.Routes.MapHttpRoute(
			//	name: "DefaultApi",
			//	routeTemplate: "api/{controller}/{id}",
			//	defaults: new { id = RouteParameter.Optional }
			//);

			var jsonFormatter = config.Formatters.JsonFormatter;
			var settings = jsonFormatter.SerializerSettings;
			settings.ContractResolver = new CamelCasePropertyNamesContractResolver();

        }
    }
}
