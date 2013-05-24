using MonoTouch.UIKit;
using System.Drawing;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using MonoTouch.Foundation;

namespace Application
{
	public partial class JobHistoryViewController : UIViewController
	{
		DetailedTabs _tabs;
		
		private string _jobHistoryLabel;
		public string JobHistoryLabel 
		{
			get { return _jobHistoryLabel; }
			set { _jobHistoryLabel = value;
				oJobHistoryLabel.Text = _jobHistoryLabel; }
		}
		
		public JobHistoryDataModel JobHistoryData { get; set; }
		public JobHistoryTableSource _ds;
		
		public JobHistoryViewController (DetailedTabs tabs) : base ("JobHistoryViewController", null)
		{
			this.Title = NSBundle.MainBundle.LocalizedString ("Job History", "Job History");
			using(var image = UIImage.FromBundle ("Images/104-index-cards") ) this.TabBarItem.Image = image;

			this._tabs = tabs;
			
			JobHistoryData = new JobHistoryDataModel(this);
			_ds = new JobHistoryTableSource(this);
		}
		
		public class JobHistoryTableSource : UITableViewDataSource
		{
			private List<HistoryJob> _jobHistory;
			public List<HistoryJob> JobHistory { get { return _jobHistory; } set { _jobHistory = value; } }

			// JobHistoryViewController _jobHistoryView;
			const string ReusableMemosTableCellID = "jobHistoryTableCell";
			public JobHistoryTableSource (JobHistoryViewController jh)
			{
				// this._jobHistoryView = jh;
				_jobHistory = new List<HistoryJob>();
			}
			public override UITableViewCell GetCell (UITableView tableView, NSIndexPath indexPath)
			{
				var cell = tableView.DequeueReusableCell(ReusableMemosTableCellID) ?? new UITableViewCell(UITableViewCellStyle.Value1, ReusableMemosTableCellID);
				
				HistoryJob j = new HistoryJob();
				j = _jobHistory[indexPath.Row];
				cell.TextLabel.Text = String.Format ("{0}   {1}", 
				                                   j.JobDate.ToString("dd/MM/yyyy"),
				                                   j.JobType
				                                   
				                                   /*, j.JobPerformedBy.ToString() */ );
				cell.DetailTextLabel.Text = String.Format ("Collected on the day: ${0}", j.MoneyCollected );
				// cell.DetailTextLabel.Text = ""; // apparently, there's nothing else to display about a job that has been done in the past
				return cell;
			}
			public override int RowsInSection (UITableView tableView, int section)
			{
				return (JobHistory != null) ? JobHistory.Count : 0;
			}
		}
		
		public void ReloadJobHistoryTable() 
		{
			// this.oPicker.ReloadAllComponents ();
			this.oJobHistoryTable.ReloadData();
		}
		
		public class JobHistoryDataModel : UIPickerViewModel // unfortunately, we won't be using UIPickerView for now, leaving this here as it could prove useful later on
		{
			// JobHistoryViewController _jobHistoryView;
			private List<HistoryJob> _jobHistory;
			public List<HistoryJob> JobHistory { get { return _jobHistory; } set { _jobHistory = value; } }

			public JobHistoryDataModel(JobHistoryViewController jh)
			{
				// _jobHistoryView = jh;
				JobHistory = new List<HistoryJob>();
			}
			
			public override int GetComponentCount(UIPickerView picker)
			{
				return 1; // 2 or more would split the picker and it would act like that number of separate pickers
			}
			public override int GetRowsInComponent (UIPickerView picker, int component)
			{
				int rows = JobHistory.Count;
				return rows;
			}
			public override string GetTitle (UIPickerView picker, int row, int component)
			{
				string jobSummary = String.Format ("{0} {1} Collected: ${2} Performed by: {3}", 
				                                   JobHistory[row].JobDate.ToString("dd/MM/yyyy"),
				                                   JobHistory[row].JobType,
				                                   JobHistory[row].MoneyCollected.ToString() );
				return jobSummary;
			}
			public override float GetRowHeight (UIPickerView picker, int component)
			{
				return 40f;
			}
		}
		
		public override void ViewDidAppear (bool animated)
		{
			base.ViewDidAppear (animated);
			_tabs.SetNavigationButtons (NavigationButtonsMode.JobHistory);
		}
		
		public override void DidReceiveMemoryWarning ()
		{
			// Releases the view if it doesn't have a superview.
			base.DidReceiveMemoryWarning ();
			
			// Release any cached data, images, etc that aren't in use.
		}
		
		public override void ViewDidLoad ()
		{
			base.ViewDidLoad ();
			//any additional setup after loading the view, typically from a nib.

			// this.oPicker.Model = JobHistoryData;
			this.oJobHistoryTable.DataSource = _ds;
			this.oJobHistoryTable.AutoresizingMask = UIViewAutoresizing.FlexibleWidth;
		}

		[Obsolete]
		public override void ViewDidUnload ()
		{
			base.ViewDidUnload ();
			
			// Release any retained subviews of the main view.
			// e.g. this.myOutlet = null;
		}

		[Obsolete]
		public override bool ShouldAutorotateToInterfaceOrientation (UIInterfaceOrientation toInterfaceOrientation)
		{
			// Return true for supported orientations
			/*
			// An attempt to auto-size the table manually -- DOES NOT WORK this way
			if (toInterfaceOrientation == UIInterfaceOrientation.Portrait || toInterfaceOrientation == UIInterfaceOrientation.PortraitUpsideDown)
				this.oJobHistoryTable.Frame = new RectangleF(0, 0, this.oJobHistoryTable.Frame.Width, this.oJobHistoryTable.Frame.Height+320);
			if (toInterfaceOrientation == UIInterfaceOrientation.LandscapeLeft || toInterfaceOrientation == UIInterfaceOrientation.LandscapeRight)
				this.oJobHistoryTable.Frame = new RectangleF(0, 0, this.oJobHistoryTable.Frame.Width, this.oJobHistoryTable.Frame.Height-320);
			*/

			return (toInterfaceOrientation == UIInterfaceOrientation.LandscapeLeft || toInterfaceOrientation == UIInterfaceOrientation.LandscapeRight);
		}
		
		
	}
}

