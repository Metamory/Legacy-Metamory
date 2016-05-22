using System.Web.Http;
using Autofac;
using Autofac.Integration.WebApi;

namespace ContentVersioning.Web
{
    public class WebApiApplication : System.Web.HttpApplication
    {
        protected void Application_Start()
        {
	        RegisterAutofac(GlobalConfiguration.Configuration);

	        GlobalConfiguration.Configure(WebApiConfig.Register);
        }

	    private static void RegisterAutofac(HttpConfiguration config)
	    {
		    var builder = new ContainerBuilder();

		    builder.RegisterModule<AssemblyModule>();

			// OPTIONAL: Register the Autofac filter provider.
			builder.RegisterWebApiFilterProvider(config);

		    // Set the dependency resolver to be Autofac.
		    var container = builder.Build();
		    config.DependencyResolver = new AutofacWebApiDependencyResolver(container);
	    }
    }
}
