﻿namespace Sdl.Community.SignoffVerifySettings.Model
{
	public class LanguageFileXmlNodeModel
	{
		// .sdlproj LanguageFile node properties
		public string LanguageFileGuid { get; set; }
		public string SettingsBundleGuid { get; set; }
		public string LanguageCode { get; set; }

		// QAVerification "Verify Files" report properties
		public string FileName { get; set; }
		// RunAt is used to display info for each individual "Verify Files {sourceLanguage name-targetLanguage name}.xml report
		// generated by executing the 'Verify Files' batch task on selected files
		public string RunAt { get; set; }
	}
}