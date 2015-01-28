using System;
using UIKit;

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

