using System.Diagnostics;
using System.Web.Http.Controllers;
using System.Web.Http.Filters;

namespace Metamory.WebApi.Utils
{
	public class StopwatchFilterAttribute : ActionFilterAttribute
	{
		private Stopwatch _stopwatch;

		public override void OnActionExecuting(HttpActionContext actionContext)
		{
			_stopwatch = Stopwatch.StartNew();
		}

		public override void OnActionExecuted(HttpActionExecutedContext actionExecutedContext)
		{
			var elapsed = _stopwatch.Elapsed;
			actionExecutedContext.Response.Headers.Add("X-Time-Taken", elapsed.ToString("c"));
		}
	}
}