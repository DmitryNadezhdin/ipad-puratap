using System;
using MonoTouch.Dialog;
using System.Collections.Generic;
using MonoTouch.UIKit;

namespace Puratap
{
	public class JobUnitUpgrade : UsedPartsViewController
	{
		public JobUnitUpgrade(RootElement root, WorkflowNavigationController nav, UsedPartsNavigationController upnav, bool pushing) : base (root, pushing)
		{
			NavUsedParts = upnav;
			NavWorkflow = nav;
			//Parts = new List<Part>();
			DBParts = new List<Part>();		
			DeactivateEditingMode ();
					
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
			
			Section UnitUpgradeTypeSection = new Section("");
			UnitUpgradeTypeSection.Add(new StyledStringElement("Use standard parts", "", UITableViewCellStyle.Value1) );
			Root.Add(UnitUpgradeTypeSection);
			
			PartsSection AdditionalPartsSection = new PartsSection("Parts used", this);
			AdditionalPartsSection.Add (new StyledStringElement("Tap here to add a part"));

			Root.Add (AdditionalPartsSection);
			using (var image = UIImage.FromBundle ("/Images/19-gear") )  this.TabBarItem.Image = image;
			this.Title = "Unit upgrade";			

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
				SetPartsToStandardBuild ();
			}
			
			base.Selected (indexPath);
		}
		
		public void SetPartsToStandardBuild()
		{
			// this should fill the list of chosen parts with the parts from the standard build
			int buildNumber = 11; // standard build number for unit upgrades
			SetPartsToBuildNumber(buildNumber);
		}
				
		public override void ViewWillAppear (bool animated)
		{
			NavUsedParts.TabBarItem.Image = this.TabBarItem.Image;
			NavUsedParts.TabBarItem.Title = this.Title;
			NavigationItem.HidesBackButton = true;
			base.ViewWillAppear (animated);
		}
		
		public override void ViewDidAppear (bool animated)
		{
			base.ViewDidAppear (animated);
			/*
			if (NavWorkflow.Toolbar.Hidden) 
				NavWorkflow.SetToolbarHidden (false, animated);
			NavWorkflow.SetToolbarButtons (WorkflowToolbarButtonsMode.Installation);		
			*/
		}
	}
}

