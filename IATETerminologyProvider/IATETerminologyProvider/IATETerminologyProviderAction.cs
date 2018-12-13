﻿using System;
using System.Windows;
using IATETerminologyProvider.Helpers;
using Sdl.Desktop.IntegrationApi;
using Sdl.Desktop.IntegrationApi.Extensions;
using Sdl.TranslationStudioAutomation.IntegrationApi;
using Sdl.TranslationStudioAutomation.IntegrationApi.Presentation.DefaultLocations;

namespace IATETerminologyProvider
{
	[RibbonGroup("IATETermDefinition", Name = "IATE Term Definition")]
	[RibbonGroupLayout(LocationByType = typeof(TranslationStudioDefaultViews.TradosStudioViewsLocation))]
	public class IATETerminologyProviderAction: AbstractRibbonGroup
	{
		public static EditorController GetEditorController()
		{
			return SdlTradosStudio.Application.GetController<EditorController>();
		}

		[Action("IATESearchAllAction", Name = "Search IATE (all)", Icon = "Iate_logo")]
		[ActionLayout(typeof(IATETerminologyProviderAction), 20, DisplayType.Large)]
		[ActionLayout(typeof(TranslationStudioDefaultContextMenus.EditorDocumentContextMenuLocation), 10, DisplayType.Large)]
		public class IATESearchAllAction : AbstractAction
		{
			// Navigate to the IATE search term based on the document source and all existing target languages(from IATE)
			protected override void Execute()
			{
				var editorController = GetEditorController();
				var activeDocument = editorController != null ? editorController.ActiveDocument : null;
				if (activeDocument != null)
				{
					var currentSelection = activeDocument.Selection != null ? activeDocument.Selection.Current.ToString().TrimEnd()	: string.Empty;
					var sourceLanguage = activeDocument.ActiveFile.SourceFile.Language.CultureInfo.TwoLetterISOLanguageName;

					if (!string.IsNullOrEmpty(currentSelection))
					{
						var url = @"http://iate.europa.eu/search/byUrl?term=" + currentSelection + "&sl=" + sourceLanguage + "&tl=all";
						System.Diagnostics.Process.Start(url);
					}
					else
					{
						MessageBox.Show(Constants.NoTermSelected, string.Empty, MessageBoxButton.OK, MessageBoxImage.Information);
					}
				}
			}
		}

		[Action("IATESearchSourceTargetAction", Name = "Search IATE (source and target only)", Icon = "Iate_logo")]
		[ActionLayout(typeof(IATETerminologyProviderAction), 20, DisplayType.Large)]
		[ActionLayout(typeof(TranslationStudioDefaultContextMenus.EditorDocumentContextMenuLocation), 10, DisplayType.Large)]
		public class IATESearchSourceLanguageAction : AbstractAction
		{	
			// Navigate to the IATE search term based on only source and target languages set on the active document
			protected override void Execute()
			{
				var editorController = GetEditorController();
				var activeDocument = editorController != null ? editorController.ActiveDocument : null;
				if (activeDocument != null)
				{
					var targetLanguages = string.Empty;
					var currentSelection = activeDocument.Selection != null ? activeDocument.Selection.Current.ToString().TrimEnd() : string.Empty;
					if (activeDocument.ActiveFile != null && !string.IsNullOrEmpty(currentSelection))
					{
						var sourceFile = activeDocument.ActiveFile.SourceFile;
						if (sourceFile != null)
						{
							var sourceLanguage = activeDocument.ActiveFile.SourceFile.Language.CultureInfo.TwoLetterISOLanguageName;
							var targetFiles = activeDocument.ActiveFile.SourceFile.TargetFiles;

							foreach (var targetFile in targetFiles)
							{
								var targetLanguage = targetFile.Language.CultureInfo.TwoLetterISOLanguageName;
								targetLanguages += $"{targetLanguage},";
							}
							var url = @"http://iate.europa.eu/search/byUrl?term=" + currentSelection + "&sl=" + sourceLanguage + "&tl=" + targetLanguages.TrimEnd(',');
							System.Diagnostics.Process.Start(url);
						}
					}
					else
					{
						MessageBox.Show(Constants.NoTermSelected, string.Empty, MessageBoxButton.OK, MessageBoxImage.Information);
					}
				}
			}
		}
	}
}