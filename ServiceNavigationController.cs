using System;
using MonoTouch.UIKit;

namespace Application
{
	public class ServiceNavigationController : UINavigationController
	{
		public DetailedTabs Tabs { get; set; }
		public ServiceNavigationController (DetailedTabs tabs)
		{
			Tabs = tabs;
		}
	}
}

