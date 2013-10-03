using System;
using MonoTouch.UIKit;
using System.Drawing;
using System.IO;
using System.Collections.Generic;
using MonoTouch.Foundation;
using MonoTouch.Dialog;
using MonoTouch.CoreGraphics;

namespace Puratap
{
	public class FilterChangeViewController : UsedPartsViewController
	{	
		//FilterChangeTypes FilterChangeType = new FilterChangeTypes();
		
		public FilterChangeViewController (RootElement root, WorkflowNavigationController nav, UsedPartsNavigationController upnav, bool pushing) : base (root, pushing)
		{
			NavUsedParts = upnav;
			NavWorkflow = nav;
			//Parts = new List<Part>();
			DBParts = new List<Part>();		
			DeactivateEditingMode ();

			/*
			Section WarrantySection = new Section("");
			BooleanElement warrantyElement = new BooleanElement("Warranty", false);
			warrantyElement.ValueChanged += delegate {
				this.NavWorkflow._tabs._jobRunTable.CurrentJob.Warranty = warrantyElement.Value;
			};
			WarrantySection.Add (warrantyElement);
			Root.Add(WarrantySection); */

			
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

			
			Section FilterChangeTypeSection = new Section("");
			FilterChangeTypeSection.Add(new StyledStringElement("Use standard parts", "", UITableViewCellStyle.Value1) );
			Root.Add(FilterChangeTypeSection);
			
			PartsSection AdditionalPartsSection = new PartsSection("Parts used for filter change", this);
			AdditionalPartsSection.Add (new StyledStringElement("Tap here to add a part"));

			Root.Add (AdditionalPartsSection);
			
			this.Title = NSBundle.MainBundle.LocalizedString ("Filter change", "Filter change");
			using (var image = UIImage.FromBundle ("Images/158-wrench-2") ) this.TabBarItem.Image = image;

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
		
		public override void Selected (NSIndexPath indexPath)
		{		
			// Use Standard parts row
			if (indexPath.Section == 1 && indexPath.Row == 0) {
				// use standard build
				ClearPartsList ();
				SetPartsToStandardBuild();
			}
			
			base.Selected (indexPath); // this handles adding parts to list
		}
		
		public void SetPartsToStandardBuild()
		{
			// this should fill the list of chosen parts with the parts from the standard build
			int buildNumber = 0;
			switch (ThisJob.Type.Code)
			{
			case "FRC": 
				buildNumber = 20; 	// standard build for rainwater filter changes
				break;	
			default : 
				buildNumber = 17;	// standard build number for filter changes 	
				break;		
			}
			SetPartsToBuildNumber(buildNumber);
		}
		
		public override void ViewWillAppear (bool animated)
		{
			NavigationItem.HidesBackButton = true;			
			NavUsedParts.Title = this.Title;
			NavUsedParts.TabBarItem.Image = this.TabBarItem.Image;

			if (ThisJob.Type.Code == "FRC")
			{
				Section section = Root[0];
				section.Caption = (ThisJob.Warranty)? "Job Report" : "REMEMBER TO FLUSH THE UNIT when doing a filter change with rainwater cartridges!";
				ReloadData ();
			}
			else {
				Section section = Root[0];
				section.Caption = (ThisJob.Warranty)? "Job Report" : " ";
				ReloadData ();
			}
			SetPartsToStandardBuild ();
			base.ViewWillAppear (animated);
		}
		
		public override void ViewDidAppear (bool animated)
		{			
			base.ViewDidAppear (animated);
		}
	}

	public class FilterChangeTypes
	{
		public FilterChangeTypesEnum Type { get; set; }
		public string OutputString()
		{
			return this.Type.ToString ();
		}
		public string OutputStringForValue(FilterChangeTypesEnum val)
		{
			return val.ToString ();
		}
	}
}

