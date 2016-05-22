using System.Reflection;
using Autofac;
using Autofac.Integration.WebApi;
using Metamory.WebApi.Policies;
using Metamory.WebApi.Policies.Authorization;
using Microsoft.Azure;
using Microsoft.WindowsAzure.Storage;
using Module = Autofac.Module;

namespace Metamory.WebApi
{
	public class AssemblyModule : Module
	{
		protected override void Load(ContainerBuilder builder)
		{
			// Register your Web API controllers.
			builder.RegisterApiControllers(Assembly.GetExecutingAssembly());

			//builder.RegisterModule<ContentVersioning.Api.AssemblyModule>();

			// TODO: What policy to register for this interface should be decided by the configuration. (For instance: ~/App_Data/authorization.config)
			builder.RegisterType<NoAuthorizationPolicy>().As<IAuthorizationPolicy>();

			builder.RegisterInstance(CloudStorageAccount.Parse(CloudConfigurationManager.GetSetting("StorageConnectionString")));
		}
	}
}