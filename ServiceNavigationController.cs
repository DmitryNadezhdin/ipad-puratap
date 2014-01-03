using System;
using MonoTouch.UIKit;

namespace Puratap
{
	public class ServiceNavigationController : UINavigationController
	{
		public readonly DetailedTabs Tabs;
		public ServiceNavigationController (DetailedTabs tabs)
		{
			Tabs = tabs;
		}
	}
}

