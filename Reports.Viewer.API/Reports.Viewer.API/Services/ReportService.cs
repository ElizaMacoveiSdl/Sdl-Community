﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using System.Xml;
using System.Xml.Xsl;
using Sdl.Core.PluginFramework;
using Sdl.ProjectAutomation.Core;
using Sdl.ProjectAutomation.FileBased;
using Sdl.Reports.Viewer.API.Model;

namespace Sdl.Reports.Viewer.API.Services
{
	public class ReportService
	{
		private static readonly object LockObject = new object();
		private static readonly Random Rnd = new Random(DateTime.Now.Millisecond);
		private readonly List<ReportDefinition> _thirdPartyReportDefinitions;
		private readonly ProjectSettingsService _projectSettingsService;		
		private Report _report;

		public ReportService(ProjectSettingsService projectSettingsService)
		{
			_projectSettingsService = projectSettingsService;
			_thirdPartyReportDefinitions = GetThirdPartyReportDefinitions();
		}

		public IEnumerable<Report> GetStudioReports(IProject project, bool overwrite)
		{
			var reportDefinitions = new List<ReportDefinition>();

			var reports = new List<Report>();
			if (project is FileBasedProject fileBasedProject)
			{
				var studioReports = _projectSettingsService.GetProjectTaskReports(fileBasedProject.FilePath);
				foreach (var studioReport in studioReports)
				{
					if (studioReport == null)
					{
						continue;
					}

					var reportDefinition =
						reportDefinitions.FirstOrDefault(a => string.Compare(a.Id, studioReport.TemplateId, StringComparison.CurrentCultureIgnoreCase) == 0) ??
						_thirdPartyReportDefinitions.FirstOrDefault(a => string.Compare(a.Id, studioReport.TemplateId, StringComparison.CurrentCultureIgnoreCase) == 0) ??
						GetReportDefinition(studioReport.TemplateId);

					if (reportDefinition == null)
					{
						continue;
					}

					if (!reportDefinitions.Exists(a => a.Id == reportDefinition.Id))
					{
						reportDefinitions.Add(reportDefinition);
					}

					var projectLocalFolder = fileBasedProject.GetProjectInfo().LocalProjectFolder.Trim('\\');
					var xmlPath = Path.Combine(projectLocalFolder, studioReport.Path);
					if (!File.Exists(xmlPath) || !studioReport.IsStudioReport)
					{
						continue;
					}

					var reportPath = CreateHtmlReport(reportDefinition, xmlPath, overwrite);
					if (reportPath != null)
					{
						studioReport.Path = reportPath;
					}

					reports.Add(studioReport);
				}
			}

			return reports;
		}

		public void UpdateStudioReportInformation(string filePath, List<Report> reports)
		{						
			try
			{
				string content;
				using (var reader = new StreamReader(filePath, Encoding.UTF8))
				{
					content = reader.ReadToEnd();
					reader.Close();
				}

				foreach (var report in reports)
				{
					_report = report;

					var regexReport = new Regex(@"<Report\s+Guid\=""" + report.Id + @"""\s+(?<attributes>.*?|)/>",
						RegexOptions.Singleline | RegexOptions.IgnoreCase);
					if (regexReport.Matches(content).Count > 0)
					{
						content = regexReport.Replace(content, ReplaceReportInfo);
					}
				}

				using (var writer = new StreamWriter(filePath, false, Encoding.UTF8))
				{
					writer.Write(content);
					writer.Flush();
					writer.Close();
				}
			}
			catch
			{
				// ignore catch all
			}
		}

		public void DeleteStudioReportInformation(string filePath, List<Report> reports)
		{
			try
			{
				string content;
				using (var reader = new StreamReader(filePath, Encoding.UTF8))
				{
					content = reader.ReadToEnd();
					reader.Close();
				}

				foreach (var report in reports)
				{
					_report = report;

					var regexReport = new Regex(@"<Report\s+Guid\=""" + report.Id + @"""\s+(?<attributes>.*?|)/>",
						RegexOptions.Singleline | RegexOptions.IgnoreCase);
					if (regexReport.Matches(content).Count > 0)
					{
						content = regexReport.Replace(content, string.Empty);
					}
				}

				using (var writer = new StreamWriter(filePath, false, Encoding.UTF8))
				{
					writer.Write(content);
					writer.Flush();
					writer.Close();
				}
			}
			catch
			{
				// ignore catch all
			}
		}

		private string ReplaceReportInfo(Match match)
		{
			var regexName = new Regex(@"(Name\="")([^""]*)("")", RegexOptions.Singleline);
			var regexDescription = new Regex(@"(Description\="")([^""]*)("")", RegexOptions.Singleline);
			//var regexPhysicalPath = new Regex(@"(PhysicalPath\="")([^""]*)("")", RegexOptions.Singleline);

			var content = match.Value;

			content = regexName.Replace(content,
				"$1" + _report.Name + "$3");

			content = regexDescription.Replace(content,
				"$1" + _report.Description + "$3");

			//content = regexPhysicalPath.Replace(content,
			//	"$1" + _report.Path + "$3");

			return content;
		}
		
		private string CreateHtmlReport(ReportDefinition reportDefinition, string xmlPath, bool replace)
		{
			var reportFilePath = xmlPath + ".html";
			if (File.Exists(reportFilePath))
			{
				if (replace)
				{
					File.Delete(reportFilePath);
				}
				else
				{
					return reportFilePath;
				}
			}

			string xmlText;
			using (var r = new StreamReader(xmlPath, Encoding.UTF8))
			{
				xmlText = r.ReadToEnd();
				r.Close();
			}

			var bytes = Transform(xmlText, reportDefinition, false);
			using (Stream s = File.Create(reportFilePath))
			{
				s.Write(bytes, 0, bytes.Length);
				s.Flush();
				s.Close();
			}

			return reportFilePath;
		}

		private ReportDefinition GetReportDefinition(string templateId)
		{
			try
			{
				var reportDefinition = new ReportDefinition
				{
					Id = templateId,
					Assembly = Assembly.Load(templateId)
				};

				if (reportDefinition.Assembly == null)
				{
					return null;
				}

				reportDefinition.Data = GetXslData(reportDefinition.Assembly);
				return reportDefinition;
			}
			catch
			{
				// catch all; ignore
			}

			return null;
		}

		private List<ReportDefinition> GetThirdPartyReportDefinitions()
		{
			var reportDefinitions = new List<ReportDefinition>();

			var automationExtensionPoint =
				PluginManager.DefaultPluginRegistry.GetExtensionPoint<ProjectAutomation.AutomaticTasks.AutomaticTaskAttribute>();

			foreach (var extension in automationExtensionPoint.Extensions)
			{
				var id = extension?.ExtensionAttribute?.Id;

				if (id == null)
				{
					continue;
				}

				if (!reportDefinitions.Exists(a => a.Id == id))
				{
					var reportDefinition = new ReportDefinition
					{
						Id = id,
						Assembly = extension.ExtensionType.Assembly,
						Data = GetXslData(extension.ExtensionType.Assembly)
					};
					reportDefinitions.Add(reportDefinition);

					var extensionTypeId = extension.ExtensionType.FullName;
					if (extensionTypeId != id)
					{
						reportDefinitions.Add(new ReportDefinition
						{
							Id = extensionTypeId,
							Assembly = reportDefinition.Assembly,
							Data = reportDefinition.Data
						});
					}
				}
			}

			return reportDefinitions;
		}

		private static byte[] Transform(string xml, ReportDefinition reportDefinition, bool useExternalResources)
		{
			var xsl = Encoding.UTF8.GetString(reportDefinition.Data);

			var resourceAssembly = reportDefinition.Assembly;
			if (resourceAssembly == null)
			{
				return null;
			}

			var transform = new XslCompiledTransform(true);
			var xslDoc = new XmlDocument();

			if (string.IsNullOrEmpty(xsl))
			{
				throw new ApplicationException("Error loading stylesheet: stylesheet is empty.");
			}

			try
			{
				xslDoc.LoadXml(xsl);
			}
			catch (Exception ex)
			{
				throw new ApplicationException("Error loading stylesheet.", ex);
			}

			var nsm = new XmlNamespaceManager(xslDoc.NameTable);
			nsm.AddNamespace("xsl", "http://www.w3.org/1999/XSL/Transform");

			transform.Load(xslDoc, new XsltSettings(true, true), new XmlUrlResolver());

			var sw = new StreamWriter(new MemoryStream(), Encoding.UTF8);

			var args = new XsltArgumentList();

			args.AddParam("UseExternalResources", "", useExternalResources);

			// add default extension object
			args.AddExtensionObject("urn:XmlReporting", new DefaultXslExtension(resourceAssembly));


			// add extra extension objects
			//Hashtable extensions = XmlReportRendererConfiguration.Instance.XslExtensionObjects;
			//foreach (string key in extensions.Keys)
			//{
			//	args.AddExtensionObject(key, extensions[key]);
			//}

			transform.Transform(new XmlTextReader(new StringReader(xml)), args, sw);

			sw.BaseStream.Seek(0, SeekOrigin.Begin);
			var output = new byte[sw.BaseStream.Length];
			var bytesRead = sw.BaseStream.Read(output, 0, output.Length);
			return output;
		}
		
		public static string GetTempFile(string extension)
		{
			var prefix = "temp";

			string file;

			lock (LockObject)
			{
				while (File.Exists(file = Path.Combine(Path.GetTempPath(), prefix + Rnd.Next() + "." + extension)))
				{
				}

				var fs = File.Create(file);
				fs.Close();
			}

			return file;
		}

		private byte[] GetXslData(Assembly automaticTaskAssembly)
		{
			if (automaticTaskAssembly == null)
			{
				return null;
			}

			var resourceNames = automaticTaskAssembly.GetManifestResourceNames();
			var xslResourceName = resourceNames.FirstOrDefault(resourceName => resourceName.ToLower().EndsWith(".xsl"));

			if (xslResourceName != null)
			{
				using (var stream = automaticTaskAssembly.GetManifestResourceStream(xslResourceName))
				{
					if (stream == null)
					{
						return null;
					}

					var len = (int)stream.Length;
					var data = new byte[len];
					stream.Read(data, 0, len);
					return data;
				}
			}

			return null;
		}
	}
}
