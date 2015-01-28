using System;
using CoreGraphics;
using UIKit;
using MonoTouch.Dialog;
using Foundation;
using System.Collections.Generic;

namespace Puratap
{
	public class JobReinstallViewController : JobInstallationViewController
	{
		public JobReinstallViewController (RootElement root, WorkflowNavigationController nav, UsedPartsNavigationController upnav, bool pushing) : base(root, pushing)
		{
			NavUsedParts = upnav;
			NavWorkflow = nav;
			DBParts = new List<Part>();
			DBAssemblies = new List<Assembly> ();
			dvcSO = new SaleOptionsDVC(null, false, true, this.NavUsedParts.Tabs._jobRunTable, this);
			DeactivateEditingMode ();
			
			Section OptionsSection = new Section("");
			EntryElement pressureElement = new EntryElement("Pressure", "Value", "", false);
			pressureElement.KeyboardType = UIKeyboardType.NumbersAndPunctuation;
			OptionsSection.Add (pressureElement);
			OptionsSection.EntryAlignment = new CGSize(565, 20);
			
			Root.Add(OptionsSection); 
			
			Section InstallationTypeSection = new Section("Installation information");
			
			var img = UIImage.FromBundle ("Images/181-hammer");
			InstallationTypeSection.Add(new BadgeElement(img, "Enter info", delegate {
				GoChooseProduct ();
			}));
			img.Dispose ();
			
			Root.Add(InstallationTypeSection);
			
			PartsSection UsedPartsSection = new PartsSection("Parts used", this);
			UsedPartsSection.Add (new StyledStringElement("Tap here to add a part"));
			Root.Add (UsedPartsSection);
			
			this.Title = "Re-installation";
			using (var image = UIImage.FromBundle ("Images/181-hammer") ) this.TabBarItem.Image = image;
			
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
				// pressure has to be entered and correct, check if parts list is empty
				if ( PressureValueOK () )
				{
					SavePressureValueToDatabase ();
					
					if ( PartsListNotEmpty () )
					{
						// all good, jump to payment
						NavUsedParts.Tabs.LastSelectedTab = NavUsedParts.Tabs.SelectedIndex;
						NavUsedParts.Tabs.SelectedViewController = NavUsedParts.Tabs.ViewControllers[3];
					}
					else
					{
						// alert the user that parts list is empty 
						var partsEmpty = new UIAlertView("", "Parts list is empty, are you sure?", null, "No", "Yes");
						partsEmpty.Dismissed += delegate(object sender, UIButtonEventArgs e) {
							if (e.ButtonIndex != partsEmpty.CancelButtonIndex)
							{
								// jump to payment
								NavUsedParts.Tabs.LastSelectedTab = NavUsedParts.Tabs.SelectedIndex;
								NavUsedParts.Tabs.SelectedViewController = NavUsedParts.Tabs.ViewControllers[3];
							}
						};
						partsEmpty.Show ();
					}
				}
				else
				{
					// alert the user that pressure value must be entered
					var pressureNotEntered = new UIAlertView("", "Cannot parse pressure value. Please enter a number.", null, "OK");
					pressureNotEntered.Show ();
					this.Root[0].Elements[0].Selected (this, this.TableView, NSIndexPath.FromRowSection (0,0));
				}
			};
		}

//		public override void InstallDataGathered()
//		{
//			ClearPartsList ();
//			base.InstallDataGathered ();
//		}
		
		public override void ViewDidAppear (bool animated)
		{
			base.ViewDidAppear (animated);
			if (NavWorkflow.Toolbar.Hidden) 
				NavWorkflow.SetToolbarHidden (false, animated);
			NavWorkflow.SetToolbarButtons (WorkflowToolbarButtonsMode.Installation);			
			
			NavWorkflow.TabBarItem.Image = this.TabBarItem.Image;
			NavWorkflow.Title = this.Title;
		}
	}
}

