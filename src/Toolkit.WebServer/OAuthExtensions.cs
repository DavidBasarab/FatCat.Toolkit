using FatCat.Toolkit.Console;
using FatCat.Toolkit.Extensions;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.IdentityModel.Tokens;

namespace FatCat.Toolkit.WebServer;

internal static class OAuthExtensions
{
	public static void AddPermissionsPolicies(this AuthorizationOptions options)
	{
		options.AddPolicy("Admin", policy => policy.RequireClaim("Admin"));
		options.AddPolicy("User", policy => policy.RequireClaim("User"));
	}

	public static JwtBearerEvents GetTokenBearerEvents()
	{
		return new JwtBearerEvents
		{
			OnTokenValidated = _ => Task.CompletedTask,
			OnMessageReceived = context =>
			{
				var accessToken = context.Request.Query["access_token"];

				// If the request is for our hub...
				var path = context.HttpContext.Request.Path;

				if (
					!string.IsNullOrEmpty(accessToken)
					&& path.StartsWithSegments(ToolkitWebApplication.Settings.SignalRPath)
				)
				{
					// Read the token out of the query string
					context.Token = accessToken;
				}

				return Task.CompletedTask;
			},
			OnForbidden = _ => Task.CompletedTask,
			OnAuthenticationFailed = context =>
			{
				if (context.Exception.GetType() == typeof(SecurityTokenExpiredException))
				{
					context.Response.Headers.TryAdd("Token-Expired", "true");
				}
				else
				{
					ConsoleLog.WriteException(context.Exception);
				}

				return Task.CompletedTask;
			},
			OnChallenge = context =>
			{
				var hasError = context.Error.IsNotNullOrEmpty() || context.ErrorDescription.IsNotNullOrEmpty();

				if (!hasError)
				{
					return Task.CompletedTask;
				}

				ConsoleLog.WriteRed("Error: {Error}", context.Error);
				ConsoleLog.WriteRed("ErrorDescription: {ErrorDescription}", context.ErrorDescription);

				return Task.CompletedTask;
			},
		};
	}
}
