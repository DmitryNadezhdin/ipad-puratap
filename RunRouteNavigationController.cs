using System;
using MonoTouch.UIKit;

namespace Puratap
{
	public class RunRouteNavigationController : UINavigationController
	{
		public readonly DetailedTabs Tabs;

		public RunRouteNavigationController (DetailedTabs tabs) : base ()
		{
			Tabs = tabs;
		}

		public override void ViewWillAppear (bool animated)
		{
			this.NavigationBarHidden = false;
			Tabs.MyNavigationBar.Hidden = true;
			base.ViewWillAppear (animated);
		}
	}
}

