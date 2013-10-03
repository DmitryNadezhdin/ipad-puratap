using System;
using MonoTouch.Foundation;
using MonoTouch.UIKit;

namespace Puratap
{
	public class CustomerNavigationController : UINavigationController
	{
		public DetailedTabs Tabs { get; set; }
		public CustomerNavigationController (DetailedTabs tabs)
		{
			Tabs = tabs;
		}

		public override void ViewDidAppear (bool animated)
		{
			base.ViewDidAppear (animated);
			if (this.ViewControllers.Length < 2) this.NavigationBar.Hidden = true;
			this.Tabs.MyNavigationBar.Hidden = false;
			this.Tabs.SetNavigationButtons (NavigationButtonsMode.CustomerDetails);
		}
	}
}

