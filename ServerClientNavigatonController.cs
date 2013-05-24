using System;
using MonoTouch.UIKit;

namespace Application
{
	public class ServerClientNavigatonController : UINavigationController
	{
		public DetailedTabs Tabs { get; set; }

		public ServerClientNavigatonController (DetailedTabs tabs) : base() 
		{
			Tabs = tabs;
			NavigationBarHidden = true;
		}
	}
}

