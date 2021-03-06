﻿using Sdl.Desktop.IntegrationApi.Extensions;
using Sdl.TranslationStudioAutomation.IntegrationApi;

namespace Sdl.Community.ProjectTerms.Plugin.ViewPart
{
    [ViewPart(Id = "ProjectsTermsViewPart", Name = "Project Terms Cloud", Description = "Show contents of the project terms in a word cloud.", Icon = "applogo")]
    [ViewPartLayout(Dock = DockType.Bottom, LocationByType = typeof(FilesController))]
    public class ProjectTermsViewPartFiles : ProjectTermsViewPart
    {
    }
}
