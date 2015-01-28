using System;
using UIKit;

namespace Puratap
{
	public class SigningNavigationController : UINavigationController
	{
		public readonly DetailedTabs Tabs;
		
		public SigningNavigationController (DetailedTabs tabs)
		{
			Tabs = tabs;
			
			this.NavigationBar.BarStyle = UIBarStyle.Default; // .Black;
		}
	}
}

