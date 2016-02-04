﻿namespace Constellation.Sitecore.Presentation.Mvc.Pipelines.RenderRendering
{
	using global::Sitecore.Diagnostics;
	using global::Sitecore.Mvc.Common;
	using global::Sitecore.Mvc.Pipelines.Response.RenderRendering;
	using System.Text;
	using WebMarkupMin.Core.Minifiers;
	using WebMarkupMin.Core.Settings;

	/// <summary>
	/// Replacement for the stock Sitecore MVC response pipeline step that minifies the output of the Rendering.
	/// </summary>
	/// <remarks>
	/// To replace the stock pipeline handler, override the same-named pipeline handler in the Mvc.RenderRenderings pipeline.
	/// Uses https://webmarkupmin.codeplex.com/ to handle the minification. Settings for WebMarkupMin are stored in the
	/// Web.config file using the traditional custom configuration section technology, *not* Sitecore configuration.
	/// This handler will default to normal behavior if minification fails for some reason.
	/// Minification only occurs in PageMode.Normal, allowing for easier troubleshooting in Preview and Editor modes.
	/// </remarks>
	public class AddRecordedHtmlToCache : global::Sitecore.Mvc.Pipelines.Response.RenderRendering.AddRecordedHtmlToCache
	{
		protected override void UpdateCache(string cacheKey, RenderRenderingArgs args)
		{
			var recordingTextWriter = args.Writer as RecordingTextWriter;
			if (recordingTextWriter == null)
			{
				return;
			}

			if (!global::Sitecore.Context.PageMode.IsNormal)
			{
				this.AddHtmlToCache(cacheKey, recordingTextWriter.GetRecording(), args);
			}

			var recording = Minify(recordingTextWriter.GetRecording());
			this.AddHtmlToCache(cacheKey, recording, args);
		}

		/// <summary>
		/// Safely Attempts to minify the provided string.
		/// </summary>
		/// <param name="recording">The string to minify</param>
		/// <returns>Minified version of the string, or, if errors were encountered, returns the original string.</returns>
		private string Minify(string recording)
		{
			var settings = new HtmlMinificationSettings();
			var cssMinifier = new KristensenCssMinifier();
			var jsMinifier = new CrockfordJsMinifier();

			var minifier = new HtmlMinifier(settings, cssMinifier, jsMinifier);

			MarkupMinificationResult result = minifier.Minify(recording);

			if (result.Errors.Count != 0)
			{
				var builder = new StringBuilder("Attempt to minify rendering failed");

				foreach (var error in result.Errors)
				{
					builder.AppendLine(error.Category + " - " + error.Message);
				}

				Log.Warn(builder.ToString(), this);
				return recording;
			}

			return result.MinifiedContent;
		}
	}
}
