using System;
using MonoTouch.Foundation;
using MonoTouch.UIKit;
using MonoTouch.Dialog;
using System.Collections.Generic;

namespace Puratap
{
	public class JobDeliveryViewController : UsedPartsViewController
	{
		public JobDeliveryViewController (RootElement root, WorkflowNavigationController nav, UsedPartsNavigationController upnav, bool pushing) : base (root, pushing)
		{
			NavUsedParts = upnav;
			NavWorkflow=nav;
			DBParts = new List<Part>();
			DBAssemblies = new List<Assembly> ();
			DeactivateEditingMode ();

			Section WarrantySection = new Section("");
			StyledStringElement warrantyElement = new StyledStringElement("", "Guaranteed delivery :-)");
			WarrantySection.Add (warrantyElement);
			Root.Add(WarrantySection);

			Section StandardPartsSection = new Section("");
			StandardPartsSection.Add(new StyledStringElement("Use standard parts", "", UITableViewCellStyle.Value1) );
			Root.Add(StandardPartsSection);
//			Root.Add (new Section("") );

			PartsSection PartsUsedSection = new PartsSection("Parts used", this);
			PartsUsedSection.Add (new StyledStringElement("Tap here to add a part"));

			Root.Add (PartsUsedSection);

			using (var image = UIImage.FromBundle ("Images/19-gear") )	this.TabBarItem.Image = image;
			this.Title = "Uninstallation";			

			ToolbarItems = new UIBarButtonItem[] {
				new UIBarButtonItem(UIBarButtonSystemItem.Reply),
				new UIBarButtonItem(UIBarButtonSystemItem.FlexibleSpace),
				new UIBarButtonItem("Clear parts list", UIBarButtonItemStyle.Plain, delegate { ClearPartsList (); }),
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
				NavUsedParts.Tabs.LastSelectedTab = NavUsedParts.Tabs.SelectedIndex;
				NavUsedParts.Tabs.SelectedViewController = NavUsedParts.Tabs.ViewControllers[3]; // jump to payment
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
			int buildNumber = 25;
			SetPartsToBuildNumber(buildNumber);
		}

		public override void ViewWillAppear (bool animated)
		{
			if (NavWorkflow.Toolbar.Hidden) 
				NavWorkflow.SetToolbarHidden (false, animated);
			NavWorkflow.SetToolbarButtons (WorkflowToolbarButtonsMode.Installation);			
			NavWorkflow.TabBarItem.Image = this.TabBarItem.Image;
			NavWorkflow.Title = this.Title;

			if (this.ThisJob.UsedParts == null || this.ThisJob.UsedParts.Count == 0) 
				SetPartsToStandardBuild ();

			base.ViewWillAppear (animated);
		}
	}
}
