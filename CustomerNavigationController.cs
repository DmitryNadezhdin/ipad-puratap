using System;
using Foundation;
using UIKit;

namespace Puratap
{
	public class CustomerNavigationController : UINavigationController
	{
		public DetailedTabs Tabs { get; set; }
		public CustomerNavigationController (DetailedTabs tabs)
		{
			Tabs = tabs;
		}

		public override void ViewWillAppear (bool animated)
		{
			base.ViewWillAppear (animated);
			if (this.ViewControllers.Length < 2) this.NavigationBar.Hidden = true;
			this.Tabs.MyNavigationBar.Hidden = false;
			this.Tabs.SetNavigationButtons (NavigationButtonsMode.CustomerDetails);
		}
	}
}

