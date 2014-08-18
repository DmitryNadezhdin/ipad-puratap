using System;
using System.Collections.Generic;
using MonoTouch.Dialog;
using MonoTouch.UIKit;

namespace Puratap
{
	public class JobTubingUpgrade : UsedPartsViewController
	{
		public JobTubingUpgrade(RootElement root, WorkflowNavigationController nav, UsedPartsNavigationController upnav, bool pushing) : base (root, pushing)
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
			TubingUpgradeTypeSection.Add(new StyledStringElement("Use standard parts", "", UITableViewCellStyle.Value1) );
			Root.Add (TubingUpgradeTypeSection);
			
			PartsSection PartsUsedSection = new PartsSection("Parts used", this);
			PartsUsedSection.Add (new StyledStringElement("Tap here to add a part"));

			Root.Add (PartsUsedSection);
			using (var image = UIImage.FromBundle ("/Images/19-gear") )	this.TabBarItem.Image = image;
			this.Title = "Tubing upgrade";			
		
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

				// if in plumber mode
				if (MyConstants.EmployeeType == MyConstants.EmployeeTypes.Plumber)
				{
					// show a dialog prompting user to choose a tubing upgrade type
					var chooseTUType = new UIAlertView("", "Please choose the tubing upgrade type", null, "Option 1", "Option 2");
					chooseTUType.Dismissed += delegate(object sender, UIButtonEventArgs e) {
						switch (e.ButtonIndex)
						{
						case 0:
							// if option 1
							SetPartsToStandardBuild(16);
							// set the job price to 29.5
							ThisJob.MoneyToCollect = 32.5;
							break;
						case 1:
							// if option 2
							SetPartsToStandardBuild (31);
							// set the job price to 75
							ThisJob.MoneyToCollect = 75;
							break;
						}
					};
					chooseTUType.Show ();
				}
				else
					SetPartsToStandardBuild (16);
			}
			base.Selected (indexPath);
		}
		
		public void SetPartsToStandardBuild(int build)
		{
			// this should fill the list of chosen parts with the parts from the standard build
			// int buildNumber = 16; // standard build number for tubing upgrades
			SetPartsToBuildNumber(build);
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
			// set used stock to default if empty
			if (ThisJob.UsedParts.Count == 0 
					&& ThisJob.UsedAssemblies.Count == 0 
					&& MyConstants.EmployeeType == MyConstants.EmployeeTypes.Franchisee)
				SetPartsToStandardBuild (16);

			base.ViewDidAppear (animated);
		}	
	}
}

