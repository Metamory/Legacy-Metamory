using System;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using System.Web.Http;
using System.Web.Http.Cors;
using System.Web.Http.Results;
using Metamory.Api;
using Metamory.WebApi.Models.WebApi.Content;
using Metamory.WebApi.Policies;
using Metamory.WebApi.Utils;
using Newtonsoft.Json.Linq;

namespace Metamory.WebApi.Controllers.WebApi
{
	//[RoutePrefix("api/content")]
	[StopwatchFilter]
	[EnableCors("*", "*", "*")]
	public class ContentController : ApiController
	{
		private readonly ContentVersioningService _contentVersioningService;
		private readonly IAuthorizationPolicy _authPolicy;

		public ContentController(ContentVersioningService contentVersioningService, IAuthorizationPolicy authPolicy)
		{
			_contentVersioningService = contentVersioningService;
			_authPolicy = authPolicy;
		}

		[HttpGet, Route("content/{siteId}/{contentId}/versions")]
		public async Task<IHttpActionResult> GetVersions(string siteId, string contentId)
		{
			if (!_authPolicy.AllowManageContent(siteId, contentId, User))
			{
				return new StatusCodeResult(User.Identity.IsAuthenticated ? HttpStatusCode.Forbidden : HttpStatusCode.Unauthorized, this);
			}

			try
			{
				var versions = await _contentVersioningService.GetVersionsAsync(siteId, contentId);
				return Ok(versions);
			}
			catch (Exception)
			{
				return StatusCode(HttpStatusCode.InternalServerError);
			}
		}

		[HttpGet, Route("content/{siteId}/{contentId}")]
		public async Task<HttpResponseMessage> GetPublishedContent(string siteId, string contentId)
		{
			var ifNoneMatchHeader = Request.Headers.IfNoneMatch.SingleOrDefault();

			string publishedVersionId = await _contentVersioningService.GetCurrentlyPublishedVersionIdAsync(siteId, contentId, DateTimeOffset.Now);

			if (!_authPolicy.AllowGetCurrentPublishedContent(siteId, contentId, User))
			{
				return new HttpResponseMessage(User.Identity.IsAuthenticated ? HttpStatusCode.Forbidden : HttpStatusCode.Unauthorized );
			}

			if (ifNoneMatchHeader != null
				&& "\"" + publishedVersionId + "\"" == ifNoneMatchHeader.Tag)
			{
				var notModifiedMessage = new HttpResponseMessage(HttpStatusCode.NotModified);
				return notModifiedMessage;
			}
			if(publishedVersionId == null)
			{
				var notFoundMessage = new HttpResponseMessage(HttpStatusCode.NotFound);
				return notFoundMessage;
			}

			var stream = new MemoryStream();
			var contentType = await _contentVersioningService.DownloadContentToStreamAsync(siteId, contentId, publishedVersionId, stream);
			stream.Seek(0, SeekOrigin.Begin);

			var responseMessage = new HttpResponseMessage(HttpStatusCode.OK);
			responseMessage.Content = new StreamContent(stream);
			responseMessage.Content.Headers.ContentType = new MediaTypeHeaderValue(contentType);
				responseMessage.Headers.ETag = new EntityTagHeaderValue("\"" + publishedVersionId + "\"");
			return responseMessage;
		}

		[HttpGet, Route("content/{siteId}/{contentId}/{versionId}")]
		public async Task<HttpResponseMessage> GetContent(string siteId, string contentId, string versionId)
		{
			if (!_authPolicy.AllowManageContent(siteId, contentId, User))
			{
				return new HttpResponseMessage(User.Identity.IsAuthenticated ? HttpStatusCode.Forbidden : HttpStatusCode.Unauthorized );
			}

			var stream = new MemoryStream();
			var contentType = await _contentVersioningService.DownloadContentToStreamAsync(siteId, contentId, versionId, stream);
			stream.Seek(0, SeekOrigin.Begin);

			if (contentType == null)
			{
				var notFoundMessage = new HttpResponseMessage(HttpStatusCode.NotFound);
				return notFoundMessage;
			}

			var responseMessage = new HttpResponseMessage(HttpStatusCode.OK);
			responseMessage.Content = new StreamContent(stream);
			responseMessage.Content.Headers.ContentType = new MediaTypeHeaderValue(contentType);

			return responseMessage;
		}

		[HttpPost, Route("content/{siteId}/{contentId}/{versionId}/status")]
		public async Task<IHttpActionResult> PostStatusChange(string siteId, string contentId, string versionId, StatusChangeModel statusModel)
		{
			if (!_authPolicy.AllowChangeContentStatus(siteId, contentId, User))
			{
				return new StatusCodeResult(User.Identity.IsAuthenticated ? HttpStatusCode.Forbidden : HttpStatusCode.Unauthorized, this);
			}

			var now = DateTimeOffset.Now;
			await _contentVersioningService.ChangeStatusForContentAsync(siteId, contentId, versionId, statusModel.Status, statusModel.Responsible, now, statusModel.StartDate);

			return await GetVersions(siteId, contentId);
		}

		//[HttpPut, Route("content/{site}/{contentId}")]
		[HttpPost, Route("content/{siteId}/{contentId}")]
		public async Task<IHttpActionResult> Post(string siteId, string contentId, HttpRequestMessage requestMessage)
		{
			if (!_authPolicy.AllowManageContent(siteId, contentId, User))
			{
				return new StatusCodeResult(User.Identity.IsAuthenticated ? HttpStatusCode.Forbidden : HttpStatusCode.Unauthorized, this);
			}

			PostContentModel model;
			if (requestMessage.Content.IsMimeMultipartContent())
			{
				model = await GetPostContentModelFromMultiPartAsync(siteId, contentId, requestMessage);
			}
			else if (requestMessage.Content.IsFormData())
			{
				model = await GetPostContentModelFromFormAsync(siteId, contentId, requestMessage);
			}
			else
			{
				model = await GetPostContentModelFromAjaxAsync(siteId, contentId, requestMessage);
			}


			if (model.ContentStream != null && model.ContentType != null)
			{
				var contentMetadata = await _contentVersioningService.StoreAsync(siteId, contentId, DateTimeOffset.Now,
					model.ContentStream, model.ContentType, model.PreviousVersionId, model.Author, model.Label);
				return Ok(contentMetadata);
			}

			return StatusCode(HttpStatusCode.BadRequest);
		}

		private async Task<PostContentModel> GetPostContentModelFromAjaxAsync(string siteId, string contentId, HttpRequestMessage requestMessage)
		{
			string jsonBodyString = await requestMessage.Content.ReadAsStringAsync();

			var jsonBody = JObject.Parse(jsonBodyString);
			Func<string, string> GetValue = key =>
			{
				var val = jsonBody[key];
				return val != null ? val.ToString() : null;
			};

			var model = new PostContentModel()
			{
				Author = GetValue("author"),
				Label = GetValue("label"),
				PreviousVersionId = GetValue("previousVersionId"),
				ContentType = GetValue("contentType"),
				ContentStream = new MemoryStream(Encoding.UTF8.GetBytes(GetValue("content")))
			};

			return model;
		}

		private async Task<PostContentModel> GetPostContentModelFromFormAsync(string siteId, string contentId, HttpRequestMessage requestMessage)
		{
			var formValues = await requestMessage.Content.ReadAsFormDataAsync();
			Func<string, string> GetValue = key => formValues.AllKeys.Contains(key) ? formValues[key] : null;

			var model = new PostContentModel()
			{
				Author = GetValue("author"),
				Label = GetValue("label"),
				PreviousVersionId = GetValue("previousVersionId"),
				ContentType = GetValue("contentType"),
				ContentStream = new MemoryStream(Encoding.UTF8.GetBytes(GetValue("content")))
			};

			return model;
		}

		private async Task<PostContentModel> GetPostContentModelFromMultiPartAsync(string siteId, string contentId, HttpRequestMessage requestMessage)
		{
			var provider = await requestMessage.Content.ReadAsMultipartAsync(new MultipartMemoryStreamProvider());

			var model = new PostContentModel();
			foreach (var content in provider.Contents)
			{
				if (content.Headers.ContentDisposition.Name == "\"author\"")
				{
					model.Author = await content.ReadAsStringAsync();
				}

				if (content.Headers.ContentDisposition.Name == "\"label\"")
				{
					model.Label = await content.ReadAsStringAsync();
				}

				if (content.Headers.ContentDisposition.Name == "\"previousVersionId\"")
				{
					model.PreviousVersionId = await content.ReadAsStringAsync();
				}

				if (content.Headers.ContentDisposition.Name == "\"content\"")
				{
					model.ContentStream = await content.ReadAsStreamAsync();
					model.ContentType = content.Headers.ContentType.MediaType;
				}
			}

			return model;
		}


		//[HttpDelete, Route("content/{siteId}/{contentId}")]
		//public IHttpActionResult Delete(string siteId, string contentId)
		//{
		//	_contentVersioningService.DeleteContent(siteId, contentId);
		//	return StatusCode(HttpStatusCode.NoContent);
		//}
	}
}
