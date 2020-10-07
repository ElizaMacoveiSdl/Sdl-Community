﻿using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Sdl.LC.AddonBlueprint.Enums;
using Sdl.LC.AddonBlueprint.Helpers;
using Sdl.LC.AddonBlueprint.Interfaces;
using Sdl.LC.AddonBlueprint.Models;
using Sdl.LC.AddonBlueprint.Services;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Text.Json;

namespace Sdl.LC.AddonBlueprint.Controllers
{
	[Route("api")]
	[ApiController]
	public class StandardController : ControllerBase
	{
		private IConfiguration _configuration;
		private readonly ILogger _logger;
		private readonly IDescriptorService _descriptorService;
		private readonly IAccountService _accountService;
		private readonly IHealthReporter _healthReporter;
		private readonly ITranslationService _translationService;

		/// <summary>
		/// Initializes a new instance of the <see cref="AccountService"/> class.
		/// </summary>
		/// <param name="configuration">The configuration.</param>
		/// <param name="logger">The logger.</param>
		/// <param name="descriptorService">The descriptor service.</param>
		/// <param name="accountService">The account service.</param>
		/// <param name="httpContextAccessor">The http context accessor.</param>
		/// <param name="healthReporter">The health reporter.</param>
		public StandardController(IConfiguration configuration,
			ILogger<StandardController> logger,
			IDescriptorService descriptorService,
			IAccountService accountService,
			IHttpContextAccessor httpContextAccessor,
			IHealthReporter healthReporter,ITranslationService translationService)
		{
			_configuration = configuration;
			_logger = logger;
			_descriptorService = descriptorService;
			_accountService = accountService;
			_healthReporter = healthReporter;
			_translationService = translationService;
		}

		/// <summary>
		/// Gets the add-on descriptor.
		/// </summary>
		/// <returns>The descriptor</returns>
		[HttpGet("descriptor")]
		public IActionResult Descriptor()
		{
			_logger.LogInformation("Entered Descriptor endpoint.");
			var descriptor = _descriptorService.GetDescriptor();

			return Content(JsonSerializer.Serialize(descriptor, JsonSettings.Default()), "application/json", Encoding.UTF8);
		}

		/// <summary>
		/// Gets the add-on health.
		/// </summary>
		/// <returns>200 status code if it's healthy.</returns>
		[HttpGet("health")]
		public IActionResult Health()
		{
			// This is a health check endpoint. In most cases returnin Ok is enough, but you might want to make checks
			// to resources this service uses, like: DB, message queues, storage etc.
			// Any response besides 200 Ok, will be considered as failure. As a suggestion use "return StatusCode(500);"
			// when you need to signal that the service is having health issues.

			var isHealthy = _healthReporter.IsServiceHealthy();
			if (isHealthy)
			{
				return Ok();
			}

			return StatusCode(500);
		}

		/// <summary>
		/// This endpoint provides the documentation for the Add-On. It can return the HTML page with the documentation
		/// or redirect to a page. In this sample redirect is used with URL configured in appsettings.json
		/// </summary>
		[HttpGet("help")]
		public IActionResult Documentation()
		{
			return Redirect(_configuration.GetValue<string>("documentationUrl"));
		}

		/// <summary>
		/// Receive lifecycle events for the add-on.
		/// </summary>
		/// <returns></returns>
		[HttpPost("addon-lifecycle")]
		public async Task<IActionResult> AddonLifecycle()
		{
			string payload;
			using (var sr = new StreamReader(Request.Body))
			{
				payload = await sr.ReadToEndAsync();
			}

			var lifecycle = JsonSerializer.Deserialize<AddOnLifecycleEvent>(payload, JsonSettings.Default());
			switch (lifecycle.Id)
			{
				case AddOnLifecycleEventEnum.REGISTERED:
					// This is the event notifying that the Add-On has been registered in Language Cloud.
					// No further details are available for that event.
					_logger.LogInformation("Addon Registered Event Received.");
					break;
				case AddOnLifecycleEventEnum.ACTIVATED:
					// This is an Activation event, tenant id and it's public key must be saved to the database.
					_logger.LogInformation("Addon Activated Event Received.");
					var activatedEvent = JsonSerializer.Deserialize<AddOnLifecycleEvent<ActivatedEvent>>(payload, JsonSettings.Default());
					await _accountService.SaveAccountInfo(activatedEvent.Data, CancellationToken.None).ConfigureAwait(false);
					break;
				case AddOnLifecycleEventEnum.UNREGISTERED:
					// This is the event notifying that the Add-On has been unregistered/deleted from Language Cloud.
					// No further details are available for that event.
					_logger.LogInformation("Addon Unregistered Event Received.");
					// All the tenant information should be removed.
					await _accountService.RemoveAccounts(CancellationToken.None).ConfigureAwait(false);
					break;
			}

			return Ok();
		}

		/// <summary>
		/// Receive lifecycle events for the account.
		/// </summary>
		/// <returns></returns>
		[Authorize]
		[HttpPost("account-lifecycle")]
		public async Task<IActionResult> AccountLifecycle()
		{
			string payload;
			using (var sr = new StreamReader(Request.Body))
			{
				payload = await sr.ReadToEndAsync();
			}

			var tenantId = Request.HttpContext.User.Claims.Single(c => c.Type == "X-LC-Tenant").Value;
			var lifecycle = JsonSerializer.Deserialize<AccountLifecycleEvent>(payload, JsonSettings.Default());
			switch (lifecycle.Id)
			{
				case AccountLifecycleEventEnum.DEACTIVATED:
					// This is the event notifying that the Add-On has been uninstalled from a tenant account.
					// No further details are available for that event.
					_logger.LogInformation("Addon Deactivated Event Received.");
					await _accountService.RemoveAccountInfo(tenantId, CancellationToken.None).ConfigureAwait(false);
					break;
			}

			return Ok();
		}

		/// <summary>
		/// Gets the configuration settings.
		/// </summary>
		/// <returns>The updated configuration settings.</returns>
		[Authorize]
		[HttpGet("configuration")]
		public async Task<IActionResult> GetConfigurationSettings()
		{
			_logger.LogInformation("Retrieving the configuration settings.");

			var tenantId = Request.HttpContext.User.Claims.Single(c => c.Type == "X-LC-Tenant").Value;
			var configurationSettingsResult = await _accountService.GetConfigurationSettings(tenantId, CancellationToken.None).ConfigureAwait(false);

			var resultValue = Content(JsonSerializer.Serialize(configurationSettingsResult, JsonSettings.Default()), "application/json", Encoding.UTF8);
			resultValue.StatusCode = 200;

			return resultValue;
		}

		/// <summary>
		/// Sets or updates the configuration settings.
		/// </summary>
		/// <returns>The updated configuration settings.</returns>
		[Authorize]
		[HttpPost("configuration")]
		public async Task<IActionResult> SetConfigurationSettings()
		{
			_logger.LogInformation("Setting the configuration settings.");

			string payload;
			using (var sr = new StreamReader(Request.Body))
			{
				payload = await sr.ReadToEndAsync();
			}

			var tenantId = Request.HttpContext.User.Claims.Single(c => c.Type == "X-LC-Tenant").Value;
			var configurationValues = Newtonsoft.Json.JsonConvert.DeserializeObject<List<ConfigurationValueModel>>(payload);
			var configurationSettingsResult = await _accountService.SaveOrUpdateConfigurationSettings(tenantId, configurationValues, CancellationToken.None).ConfigureAwait(false);
			var resultValue = Content(JsonSerializer.Serialize(configurationSettingsResult, JsonSettings.Default()), "application/json", Encoding.UTF8);
			resultValue.StatusCode = 200;

			return resultValue;
		}

		//[Authorize]
		[HttpGet("translation-engines")]
		public async Task<IActionResult> GetTranslationEngines([FromQuery]TranslationEngineRequest request)
		{
			//var tenantId = Request.HttpContext.User.Claims.Single(c => c.Type == "X-LC-Tenant").Value;
			//var configurationSettingsResult = await _accountService.GetConfigurationSettings(tenantId, CancellationToken.None).ConfigureAwait(false);
			var translationService = new TranslationService();

			if(!string.IsNullOrEmpty(request.SourceLanguage) && request.TargetLanguages.Any())
			{
				var translationEngineResponse = await translationService.GetCorrespondingEngines(apiKey, request.SourceLanguage, request.TargetLanguages);
				return Ok(translationEngineResponse);
			}
			return Ok(new TranslationEngineResponse());
		}

		//[Authorize]
		[HttpPost("translate")]
		public async Task<IActionResult> Translate()
		{
			return Ok("postTranslate");
		}
	}
}