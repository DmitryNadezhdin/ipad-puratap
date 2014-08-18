using System;
using System.Collections.Generic;
using MonoTouch.Dialog;
using MonoTouch.UIKit;

namespace Puratap
{
	public class JobHDTubingUpgrade : UsedPartsViewController
	{
		public JobHDTubingUpgrade(RootElement root, WorkflowNavigationController nav, UsedPartsNavigationController upnav, bool pushing) : base (root, pushing)
		{
			NavUsedParts = upnav;
			NavWorkflow=nav;
			DBParts = new List<Part>();
			DBAssemblies = new List<Assembly> ();
			DeactivateEditingMode ();

			/*
			Section WarrantySection = new Section("");
			BooleanElement warrantyElement = new BooleanElement("Warranty", false);
			warrantyElement.ValueChanged += delegate {
				this.NavWorkflow._tabs._jobRunTable.CurrentJob.Warranty = warrantyElement.Value;
			};
			WarrantySection.Add (warrantyElement);
			Root.Add(WarrantySection);*/

			Section WarrantySection = new Section(" ");
			BooleanElement warrantyElement = new BooleanElement("Warranty", false);
			warrantyElement.ValueChanged += delegate {
				// this.NavWorkflow._tabs._jobRunTable.CurrentJob.Warranty = warrantyElement.Value;				
				ThisJob.Warranty = warrantyElement.Value;
				if (this.NavWorkflow._tabs._jobRunTable.CurrentJob.Warranty)
					this.AddJobReport(true);
			};
			WarrantySection.Add (warrantyElement);
			Root.Add(WarrantySection);


			Section TubingUpgradeTypeSection = new Section("");
			TubingUpgradeTypeSection.Add(new StyledStringElement("Tap to choose HD upgrade type", "", UITableViewCellStyle.Value1) );
			Root.Add (TubingUpgradeTypeSection);
			
			PartsSection PartsUsedSection = new PartsSection("Parts used", this);
			PartsUsedSection.Add (new StyledStringElement("Tap here to add a part"));

			Root.Add (PartsUsedSection);	
			using (var image = UIImage.FromBundle ("/Images/19-gear") ) this.TabBarItem.Image = image;
			this.Title = "HD Tubing upgrade";			
		
			ToolbarItems = new UIBarButtonItem[] {
				new UIBarButtonItem(UIBarButtonSystemItem.Reply),
				new UIBarButtonItem(UIBarButtonSystemItem.FlexibleSpace),
				new UIBarButtonItem("Clear parts list", UIBarButtonItemStyle.Bordered, delegate { ClearPartsList (); }),
				new UIBarButtonItem(UIBarButtonSystemItem.FlexibleSpace),
				new UIBarButtonItem(UIBarButtonSystemItem.Action)			
			};
			ToolbarItems[0].Clicked += delegate {
				if (NavUsedParts.Tabs._jobRunTable.CurrentJob.HasParent ())	// for child jobs, we jump to payment screen
				 	NavUsedParts.Tabs.SelectedViewController = NavUsedParts.Tabs.ViewControllers[3];
				else // for main jobs we jump to pre-plumbing check
					NavUsedParts.Tabs.SelectedViewController = NavUsedParts.Tabs.ViewControllers[0];
			};
			
			ToolbarItems[4].Clicked += delegate {
				if (ThisJob.JobReportAttached)
				{				
					if ( JobReportDataValid () )	// check that pressure and comment fields have been filled
					{
						foreach(Section section in Root)					
							if (section is JobReportSection)
						{
							(section as JobReportSection).jrd.Pressure = Convert.ToInt32 ( (section.Elements[1] as EntryElement).Value );
							(section as JobReportSection).jrd.Comment = (section.Elements[4] as MultilineEntryElement).Value; 
							
							SaveJobReport ( (section as JobReportSection).jrd );
							
							// save the service report
							nav._tabs._jobService.PdfListOfIssues = (UIView) this.TableView;
							nav._tabs._jobService.GenerateServicePDFPreview ();
							nav._tabs._jobService.RedrawServiceCallPDF (false);
						}
						
						NavUsedParts.Tabs.LastSelectedTab = NavUsedParts.Tabs.SelectedIndex;
						NavUsedParts.Tabs.SelectedViewController = NavUsedParts.Tabs.ViewControllers[3];
					}
				}
				else
				{
					NavUsedParts.Tabs.LastSelectedTab = NavUsedParts.Tabs.SelectedIndex;
					NavUsedParts.Tabs.SelectedViewController = NavUsedParts.Tabs.ViewControllers[3];
				}

				/*
				// save the job report (if there is one)
				if (ThisJob.JobReportAttached)				
					foreach(Section section in Root)					
						if (section is JobReportSection)						
							SaveJobReport ( (section as JobReportSection).jrd );

				NavUsedParts.Tabs.LastSelectedTab = NavUsedParts.Tabs.SelectedIndex;
				NavUsedParts.Tabs.SelectedViewController = NavUsedParts.Tabs.ViewControllers[3]; // jump to payment
				*/
			};
		}
		
		public override void Selected (MonoTouch.Foundation.NSIndexPath indexPath)
		{
			if (indexPath.Section == 1)
			{
				ClearPartsList ();
				SetPartsToStandardBuild();				
			}
			base.Selected (indexPath);
		}
		
		public void SetPartsToStandardBuild()
		{
			var ac = new UIActionSheet("Choose a HD tubing upgrade type", null, null, null, "Slow flow upgrade", "Full HD Upgrade");
			ac.Dismissed += delegate(object sender, UIButtonEventArgs e) {
				if (e.ButtonIndex != ac.CancelButtonIndex)
				{
					switch (e.ButtonIndex)
					{
					case 0: { 
						int buildNumber = 18; SetPartsToBuildNumber(buildNumber); 
						ThisJob.EmployeeFee = 10; // FIXME :: hard-coded value for fee
						if (ThisJob.HasParent ())
						{
							Job parent = this.NavWorkflow._tabs._jobRunTable.FindParentJob (ThisJob);
							foreach (Job child in parent.ChildJobs)
							{
								if (child.JobBookingNumber == ThisJob.JobBookingNumber)
									child.EmployeeFee = 10; // FIXME :: hard-coded value for fee
							}
						}
						break; }

					case 1: { 
						int buildNumber = 19; SetPartsToBuildNumber(buildNumber); 
						ThisJob.EmployeeFee = ThisJob.Type.EmployeeFee; // FIXED :: hard-coded value for fee

						if (ThisJob.HasParent ()) {
							Job parent = this.NavWorkflow._tabs._jobRunTable.FindParentJob (ThisJob);
							foreach (Job main in this.NavWorkflow._tabs._jobRunTable.MainJobList) {
								if (main.JobBookingNumber == parent.JobBookingNumber) {
									foreach (Job child in main.ChildJobs) {
										if (child.JobBookingNumber == ThisJob.JobBookingNumber) {
											child.EmployeeFee = ThisJob.Type.EmployeeFee; // FIXED :: hard-coded value for fee 
										}
									}
								}
							}

							foreach (Job main in this.NavWorkflow._tabs._jobRunTable.UserCreatedJobs) {
								if (main.JobBookingNumber == parent.JobBookingNumber) {
									foreach (Job child in main.ChildJobs) {
										if (child.JobBookingNumber == ThisJob.JobBookingNumber) {
											child.EmployeeFee = ThisJob.Type.EmployeeFee; // FIXED :: hard-coded value for fee
										}
									}
								}
							}
						}
						break; }
					default: { break; }
					}
				}
			};
			ac.ShowInView (this.View);
		}
			

		public override void ViewWillAppear (bool animated)
		{
			NavigationItem.HidesBackButton = true;
			NavUsedParts.TabBarItem.Image = this.TabBarItem.Image;
			NavUsedParts.Title = this.Title;
			base.ViewWillAppear (animated);
		}
		
		public override void ViewDidAppear (bool animated)
		{			
			base.ViewDidAppear (animated);
		}
	}
}
