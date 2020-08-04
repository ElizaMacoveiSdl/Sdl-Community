﻿using System;
using System.Collections.Generic;
using System.Windows.Forms;
using Sdl.Community.Reports.Viewer.Controls;
using Sdl.Community.Reports.Viewer.CustomEventArgs;
using Sdl.Community.Reports.Viewer.Model;
using Sdl.Community.Reports.Viewer.TestData;
using Sdl.Community.Reports.Viewer.View;
using Sdl.Community.Reports.Viewer.ViewModel;
using Sdl.Desktop.IntegrationApi;
using Sdl.Desktop.IntegrationApi.Extensions;
using Sdl.TranslationStudioAutomation.IntegrationApi;
using Sdl.TranslationStudioAutomation.IntegrationApi.Presentation.DefaultLocations;

namespace Sdl.Community.Reports.Viewer
{
	[View(
		Id = "SDLReportsViewer_View",
		Name = "SDLReportsViewer_Name",
		Description = "SDLReportsViewer_Description",
		Icon = "ReportsView",
		AllowViewParts = true,
		LocationByType = typeof(TranslationStudioDefaultViews.TradosStudioViewsLocation))]
	public class ReportsViewerController : AbstractViewController
	{
		private List<Report> _reports;
		private ReportViewModel _reportViewModel;
		private ReportsNavigationViewModel _reportsNavigationViewModel;
		private ReportViewControl _reportViewControl;
		private ReportsNavigationViewControl _reportsNavigationViewControl;
		private ProjectsController _projectsController;

		protected override void Initialize(IViewContext context)
		{
			_projectsController = SdlTradosStudio.Application.GetController<ProjectsController>();
			_projectsController.CurrentProjectChanged += ProjectsController_CurrentProjectChanged;

			var testDataUtil = new TestDataUtil();
			_reports = testDataUtil.GetTestReports();
		}

		protected override Control GetExplorerBarControl()
		{
			if (_reportsNavigationViewControl == null)
			{
				_reportsNavigationViewModel = new ReportsNavigationViewModel(new List<Report>(), _projectsController);
				_reportsNavigationViewModel.ReportSelectionChanged += OnReportSelectionChanged;

				_reportsNavigationViewControl = new ReportsNavigationViewControl();

			}

			return _reportsNavigationViewControl;
		}

		protected override Control GetContentControl()
		{
			if (_reportViewControl == null)
			{
				var reportView = new ReportView();
				_reportViewModel = new ReportViewModel(reportView);
				reportView.DataContext = _reportViewModel;

				_reportViewControl = new ReportViewControl();
				_reportsNavigationViewModel.ReportViewModel = _reportViewModel;

				if (_reports.Count > 0)
				{
					_reports[0].IsSelected = true;
				}

				_reportsNavigationViewModel.Reports = _reports;


				var reportsNavigationView = new ReportsNavigationView(_reportsNavigationViewModel);


				_reportsNavigationViewControl.UpdateViewModel(reportsNavigationView);
				_reportViewControl.UpdateViewModel(reportView);
			}

			return _reportViewControl;
		}

		public EventHandler<ReportSelectionChangedEventArgs> ReportSelectionChanged;

		private void OnReportSelectionChanged(object sender, ReportSelectionChangedEventArgs e)
		{
			ReportSelectionChanged?.Invoke(this, e);
		}

		private void ProjectsController_CurrentProjectChanged(object sender, EventArgs e)
		{
			// TODO load project reports into the viewer

			//var updated = AddNewProjectToContainer(_projectsController?.CurrentProject);
			//if (!updated)
			//{
			//	updated = UnloadRemovedProjectsFromContainer();
			//}

			//if (updated && _projectsNavigationViewModel != null)
			//{
			//	_projectsNavigationViewModel.Projects = new List<Project>();
			//	_projectsNavigationViewModel.Projects = _xliffProjects;
			//}
		}

	}
}
