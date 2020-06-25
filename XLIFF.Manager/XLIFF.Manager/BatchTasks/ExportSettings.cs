﻿using System;
using System.Collections.Generic;
using Sdl.Community.XLIFF.Manager.Model;
using Sdl.Core.Settings;

namespace Sdl.Community.XLIFF.Manager.BatchTasks
{
	public class ExportSettings : SettingsGroup
	{
		private const string ExportOptionsSettingId = "ExportOptions";
		private const string TransactionFolderSettingId = "TransactionFolder";
		private const string SelectedFilterItemIdsSettingId = "SelectedFilterItemIds";
		private const string DateTimeStampSettingId = "DateTimeStamp";
		private const string ProjectFilesSettingId = "ProjectFiles";
		private const string LocalProjectFolderSettingId = "LocalProjectFolder";

		public string LocalProjectFolder
		{
			get => GetSetting<string>(LocalProjectFolderSettingId);
			set => GetSetting<string>(LocalProjectFolderSettingId).Value = value;
		}

		public List<ProjectFile> ProjectFiles
		{
			get => GetSetting<List<ProjectFile>>(ProjectFilesSettingId);
			set => GetSetting<List<ProjectFile>>(ProjectFilesSettingId).Value = value;
		}

		public ExportOptions ExportOptions
		{
			get => GetSetting<ExportOptions>(ExportOptionsSettingId);
			set => GetSetting<ExportOptions>(ExportOptionsSettingId).Value = value;
		}
		
		public DateTime DateTimeStamp
		{
			get => GetSetting<DateTime>(DateTimeStampSettingId);
			set => GetSetting<DateTime>(DateTimeStampSettingId).Value = value;
		}

		public string TransactionFolder
		{
			get => GetSetting<string>(TransactionFolderSettingId);
			set => GetSetting<string>(TransactionFolderSettingId).Value = value;
		}

		public List<string> SelectedFilterItemIds
		{
			get => GetSetting<List<string>>(SelectedFilterItemIdsSettingId);
			set => GetSetting<List<string>>(SelectedFilterItemIdsSettingId).Value = value;
		}
	}
}
