using System;
using MonoTouch.UIKit;
using MonoTouch.Dialog;
using System.Collections.Generic;

namespace Puratap
{
	public class JobUninstallViewController : UsedPartsViewController
	{
		public JobUninstallViewController (RootElement root, WorkflowNavigationController nav, UsedPartsNavigationController upnav, bool pushing) : base (root, pushing)
		{
			NavUsedParts = upnav;
			NavWorkflow=nav;
			DBParts = new List<Part>();
			DBAssemblies = new List<Assembly> ();
			DeactivateEditingMode ();
			
			Section WarrantySection = new Section("");
			BooleanElement warrantyElement = new BooleanElement("Warranty", false);
			warrantyElement.ValueChanged += delegate {
				this.NavWorkflow._tabs._jobRunTable.CurrentJob.Warranty = warrantyElement.Value;
			};
			WarrantySection.Add (warrantyElement);
			Root.Add(WarrantySection);
			
			Root.Add (new Section("") );
			
			PartsSection PartsUsedSection = new PartsSection("Parts used", this);
			PartsUsedSection.Add (new StyledStringElement("Tap here to add a part"));

			Root.Add (PartsUsedSection);

			using (var image = UIImage.FromBundle ("Images/19-gear") )	this.TabBarItem.Image = image;
			this.Title = "Uninstallation";			

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
				NavUsedParts.Tabs.LastSelectedTab = NavUsedParts.Tabs.SelectedIndex;
				NavUsedParts.Tabs.SelectedViewController = NavUsedParts.Tabs.ViewControllers[3]; // jump to payment
			};
		}
		
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

