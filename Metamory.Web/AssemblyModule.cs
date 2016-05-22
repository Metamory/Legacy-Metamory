using Autofac;
using Module = Autofac.Module;

namespace ContentVersioning.Web
{
	public class AssemblyModule : Module
	{
		protected override void Load(ContainerBuilder builder)
		{
			// Register your Web API controllers.
			//builder.RegisterApiControllers(Assembly.GetExecutingAssembly());

			builder.RegisterModule<global::Metamory.Api.AssemblyModule>();
			builder.RegisterModule<Metamory.WebApi.AssemblyModule>();


			//builder.RegisterInstance(CloudStorageAccount.Parse(CloudConfigurationManager.GetSetting("StorageConnectionString")));
		}
	}
}