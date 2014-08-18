using System;
using MonoTouch.Dialog;
using System.Collections.Generic;
using MonoTouch.UIKit;

namespace Puratap
{
	public class JobNewTap : UsedPartsViewController
	{
		public JobNewTap  (RootElement root, WorkflowNavigationController nav, UsedPartsNavigationController upnav, bool pushing) : base (root, pushing)
		{
			NavUsedParts = upnav;
			NavWorkflow = nav;
			DBParts = new List<Part>();		
			DBAssemblies = new List<Assembly> ();
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

			Section TapTypeSection = new Section("New tap type");
			TapTypeSection.Add(new StyledStringElement("Tap to choose tap model", "", UITableViewCellStyle.Value1) );
			Root.Add(TapTypeSection);
			
			PartsSection AdditionalPartsSection = new PartsSection("Parts used", this);
			AdditionalPartsSection.Add (new StyledStringElement("Tap here to add a part"));

			Root.Add (AdditionalPartsSection);
			using (var image = UIImage.FromBundle ("/Images/19-gear") ) this.TabBarItem.Image = image;
			this.Title = "New tap";			

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
			if (indexPath.Section == 1 && indexPath.Row == 0)
			{
				List<SaleOption> saleOptions = new List<SaleOption>();
				saleOptions = SaleOptionsDVC.ReadSaleOptions ();

				var ac = new UIActionSheet("Please choose a tap");
				foreach(SaleOption so in saleOptions)
				{
					if (so.Type == "Taps")
						ac.AddButton (so.Name);
				}

				ac.Style = UIActionSheetStyle.BlackTranslucent;
				ac.Dismissed += delegate(object sender, UIButtonEventArgs e) 
				{
					this.TableView.DeselectRow (indexPath, true);
					
					if (e.ButtonIndex != ac.CancelButtonIndex)
					{
						ClearPartsList();
						PartsSection.ReadPartsFromDatabase(ServerClientViewController.dbFilePath);

						string chosenButton = ac.ButtonTitle (e.ButtonIndex);
						foreach(SaleOption so in saleOptions)
						{
							if (so.Name == chosenButton)
							{
								// putting the appropriate part into the list of stock used
								foreach(Part prt in DBParts)
								{
									if (prt.PartNo == so.PartLink)
									{
										PartChosen (prt, false);
										break;
									}
								}

								// adjusting the fee if the chosen tap model attracts something on top of the standard fee
								if (so.ExtraFee > 0)
								{
									ThisJob.EmployeeFee = so.ExtraFee;
								}
								else
								{
									ThisJob.EmployeeFee = ThisJob.Type.EmployeeFee;
								}

								break;
							}
						}
					}
				};
				
				ac.ShowInView (this.View);
			}
			else base.Selected (indexPath);
		}
		
		public override void ViewWillAppear (bool animated)
		{
			NavigationItem.HidesBackButton = true;
			NavUsedParts.Title = this.Title;
			NavUsedParts.TabBarItem.Image = this.TabBarItem.Image;

			/*
			Section WarrantySection = new Section(" ");
			BooleanElement warrantyElement = new BooleanElement("Warranty", false);
			warrantyElement.ValueChanged += delegate {
				// this.NavWorkflow._tabs._jobRunTable.CurrentJob.Warranty = warrantyElement.Value;
				
				ThisJob.Warranty = warrantyElement.Value;
				if (this.NavWorkflow._tabs._jobRunTable.CurrentJob.Warranty)
					this.AddJobReport();
			};
			WarrantySection.Add (warrantyElement);
			
			Root.RemoveAt (0);
			ReloadData ();
			Root.Insert (0, WarrantySection);
			ReloadData ();
			*/

			base.ViewWillAppear (animated);
		}
		
		public override void ViewDidAppear (bool animated)
		{
			base.ViewDidAppear (animated);
			/*
			if (NavWorkflow.Toolbar.Hidden) 
				NavWorkflow.SetToolbarHidden (false, animated);
			NavWorkflow.SetToolbarButtons (WorkflowToolbarButtonsMode.Installation);	
			NavWorkflow.TabBarItem.Image = this.TabBarItem.Image;
			NavWorkflow.Title = this.Title;
			*/
		}
	}
}

