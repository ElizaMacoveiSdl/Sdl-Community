﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Input;
using Sdl.Community.Reports.Viewer.Commands;
using Sdl.Community.Reports.Viewer.CustomEventArgs;
using Sdl.Community.Reports.Viewer.Model;
using Sdl.TranslationStudioAutomation.IntegrationApi;

namespace Sdl.Community.Reports.Viewer.ViewModel
{
	public class ReportsNavigationViewModel : INotifyPropertyChanged, IDisposable
	{
		private readonly ProjectsController _projectsController;
		private List<Report> _reports;
		private string _filterString;
		private List<Report> _filteredReports;
		private Report _selectedReport;
		private bool _isReportSelected;
		private List<ReportGroup> _reportGroups;
		private GroupType _groupType;
		private List<GroupType> _groupTypes;
		private ICommand _expandAllCommand;
		private ICommand _collapseAllCommand;
		private ICommand _clearFilterCommand;
		private ICommand _selectedItemChangedCommand;

		public ReportsNavigationViewModel(List<Report> reports, ProjectsController projectsController)
		{
			_reports = reports;
			_projectsController = projectsController;

			GroupType = GroupTypes[0];

			FilteredReports = _reports;
			FilterString = string.Empty;
		}

		public EventHandler<ReportSelectionChangedEventArgs> ReportSelectionChanged;

		public ICommand ExpandAllCommand => _expandAllCommand ?? (_expandAllCommand = new CommandHandler(ExpandAll));

		public ICommand CollapseAllCommand => _collapseAllCommand ?? (_collapseAllCommand = new CommandHandler(CollapseAll));

		public ICommand ClearFilterCommand => _clearFilterCommand ?? (_clearFilterCommand = new CommandHandler(ClearFilter));

		public ICommand SelectedItemChangedCommand => _selectedItemChangedCommand ?? (_selectedItemChangedCommand = new CommandHandler(SelectedItemChanged));


		public ReportViewModel ReportViewModel { get; internal set; }

		public List<Report> Reports
		{
			get => _reports;
			set
			{
				_reports = value;

				OnPropertyChanged(nameof(Reports));

				FilterString = string.Empty;
				FilteredReports = _reports;
			}
		}		

		public string FilterString
		{
			get => _filterString;
			set
			{
				if (_filterString == value)
				{
					return;
				}

				_filterString = value;
				OnPropertyChanged(nameof(FilterString));

				FilteredReports = string.IsNullOrEmpty(_filterString)
					? _reports
					: _reports.Where(a => a.Name.ToLower().Contains(_filterString.ToLower())).ToList();
			}
		}

		public List<Report> FilteredReports
		{
			get => _filteredReports;
			set
			{
				_filteredReports = value;
				OnPropertyChanged(nameof(FilteredReports));

				if (_filteredReports?.Count > 0 && !_filteredReports.Contains(SelectedReport))
				{
					SelectedReport = _filteredReports[0];
				}
				else if (_filteredReports?.Count == 0)
				{
					SelectedReport = null;
				}

				var reportGroups = BuildReportGroup();
				ReportGroups = ExpandAll(reportGroups);				

				OnPropertyChanged(nameof(StatusLabel));
			}
		}

		public List<ReportGroup> ReportGroups
		{
			get => _reportGroups;
			set
			{
				_reportGroups = value;
				OnPropertyChanged(nameof(ReportGroups));
			}
		}

		public Report SelectedReport
		{
			get => _selectedReport;
			set
			{
				_selectedReport = value;
				OnPropertyChanged(nameof(SelectedReport));

				if (_selectedReport != null)
				{
					ReportViewModel?.UpdateReport(_selectedReport.Path ?? string.Empty);
				}

				IsReportSelected = _selectedReport != null;
			}
		}

		public GroupType GroupType
		{
			get => _groupType;
			set
			{
				if (_groupType == value)
				{
					return;
				}

				_groupType = value;
				OnPropertyChanged(nameof(GroupType));

				ReportGroups = BuildReportGroup();
			}
		}

		public List<GroupType> GroupTypes
		{
			get
			{
				return _groupTypes ?? (_groupTypes = new List<GroupType>
				{
					new GroupType
					{
						Name = "Group Name",
						Type = "Group"
					},
					new GroupType
					{
						Name = "Language",
						Type = "Language"
					},
				});
			}
			set
			{
				_groupTypes = value;
				OnPropertyChanged(nameof(GroupType));
			}
		}

		public bool IsReportSelected
		{
			get => _isReportSelected;
			set
			{
				if (_isReportSelected == value)
				{
					return;
				}

				_isReportSelected = value;
				OnPropertyChanged(nameof(IsReportSelected));
			}
		}

		public string StatusLabel
		{
			get
			{
				//var message = string.Format(PluginResources.StatusLabel_Selected_0, _selectedProjects?.Count);
				//return message;
				return string.Empty;
			}
		}

		private List<ReportGroup> BuildReportGroup()
		{
			var reportGroups = new List<ReportGroup>();
			if (FilteredReports == null)
			{
				return reportGroups;
			}

			foreach (var report in FilteredReports)
			{
				if (_groupType.Type == "Group")
				{
					var reportGroup = reportGroups.FirstOrDefault(a => a.Name == report.Group);
					if (reportGroup != null)
					{
						var groupItem = reportGroup.GroupItems.FirstOrDefault(a => a.Name == report.Language);
						if (groupItem != null)
						{
							groupItem.Reports.Add(report);
							UpdateIsExpanded(report, groupItem, reportGroup);
						}
						else
						{
							groupItem = new GroupItem
							{
								Name = report.Language,
								Reports = new List<Report> { report }
							};

							reportGroup.GroupItems.Add(groupItem);

							UpdateIsExpanded(report, groupItem, reportGroup);
						}
					}
					else
					{
						// new
						var groupItem = new GroupItem
						{
							Name = report.Language,
							Reports = new List<Report> { report }
						};

						reportGroup = new ReportGroup();
						reportGroup.Name = report.Group;
						reportGroup.GroupItems.Add(groupItem);
						reportGroups.Add(reportGroup);

						UpdateIsExpanded(report, groupItem, reportGroup);
					}
				}
				else
				{
					var reportGroup = reportGroups.FirstOrDefault(a => a.Name == report.Language);
					if (reportGroup != null)
					{
						var groupItem = reportGroup.GroupItems.FirstOrDefault(a => a.Name == report.Group);
						if (groupItem != null)
						{
							groupItem.Reports.Add(report);

							UpdateIsExpanded(report, groupItem, reportGroup);
						}
						else
						{
							groupItem = new GroupItem
							{
								Name = report.Group,
								Reports = new List<Report> { report }
							};

							reportGroup.GroupItems.Add(groupItem);

							UpdateIsExpanded(report, groupItem, reportGroup);
						}
					}
					else
					{
						// new
						var groupItem = new GroupItem
						{
							Name = report.Group,
							Reports = new List<Report> { report }
						};

						reportGroup = new ReportGroup();
						reportGroup.Name = report.Language;
						reportGroup.GroupItems.Add(groupItem);
						reportGroups.Add(reportGroup);

						UpdateIsExpanded(report, groupItem, reportGroup);
					}
				}
			}

			return reportGroups;
		}

		private static void UpdateIsExpanded(Report report, GroupItem groupItem, ReportGroup reportGroup)
		{
			if (report.IsSelected)
			{
				groupItem.IsExpanded = true;
				reportGroup.IsExpanded = true;
			}
		}

		private void ExpandAll(object parameter)
		{
			var reportGroups = ExpandAll(BuildReportGroup());

			ReportGroups = reportGroups;
		}

		private List<ReportGroup> ExpandAll(List<ReportGroup> reportGroups)
		{
			foreach (var reportGroup in reportGroups)
			{
				reportGroup.IsExpanded = true;
				foreach (var groupItem in reportGroup.GroupItems)
				{
					groupItem.IsExpanded = true;
				}
			}

			return reportGroups;
		}

		private void CollapseAll(object parameter)
		{
			var reportGroups = CollapseAll(BuildReportGroup());
			ReportGroups = reportGroups;
		}

		private List<ReportGroup> CollapseAll(List<ReportGroup> reportGroups)
		{
			foreach (var reportGroup in reportGroups)
			{
				reportGroup.IsExpanded = false;
				reportGroup.IsSelected = false;
				foreach (var groupItem in reportGroup.GroupItems)
				{
					groupItem.IsExpanded = false;
					groupItem.IsSelected = false;
				}
			}

			reportGroups[0].IsSelected = true;

			return reportGroups;
		}

		private void ClearFilter(object parameter)
		{
			FilterString = string.Empty;
		}

		private void SelectedItemChanged(object parameter)
		{
			if (parameter is RoutedPropertyChangedEventArgs<object> property)
			{
				if (property.NewValue is Report report)
				{
					SelectedReport = report;
				}
				else if (property.NewValue is GroupItem groupItem)
				{
					ReportViewModel.UpdateData(groupItem.Reports);
				}
				else if (property.NewValue is ReportGroup reportGroup)
				{
					ReportViewModel.UpdateData(reportGroup.GroupItems?.SelectMany(a => a.Reports).ToList());
				}
			}
		}

		public void Dispose()
		{
		}

		public event PropertyChangedEventHandler PropertyChanged;

		protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
		{
			PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
		}
	}
}