﻿using System.Windows.Forms;
using Sdl.Community.Transcreate.Model.ProjectSettings;
using Sdl.Desktop.IntegrationApi;
using Sdl.Desktop.IntegrationApi.Interfaces;

namespace Sdl.Community.Transcreate.BatchTasks
{
	public partial class ImportSettingsControl : UserControl, IUISettingsControl, ISettingsAware<SDLTranscreateImportSettings>
	{
		public ImportSettingsControl()
		{
			InitializeComponent();
		}
		
		public SDLTranscreateImportSettings Settings { get; set; }

		/// <summary> 
		/// Clean up any resources being used.
		/// </summary>
		/// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
		protected override void Dispose(bool disposing)
		{
			if (disposing && (components != null))
			{
				components.Dispose();
			}
			
			base.Dispose(disposing);
		}
	}
}
