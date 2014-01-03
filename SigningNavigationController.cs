using System;
using MonoTouch.UIKit;

namespace Puratap
{
	public class SigningNavigationController : UINavigationController
	{
		public readonly DetailedTabs Tabs;
		
		public SigningNavigationController (DetailedTabs tabs)
		{
			Tabs = tabs;
			
			this.NavigationBar.BarStyle = UIBarStyle.Black;

			if (MyConstants.iOSVersion >= 7) {
				this.NavigationBar.TintColor = UIColor.Blue;
				this.NavigationBar.BarTintColor = UIColor.FromRGBA (0, 0, 0, 25); 
				this.NavigationBar.SetTitleTextAttributes (new UITextAttributes () {
					TextColor = UIColor.Black,
					TextShadowColor = UIColor.Clear
				});
			}
		}
	}
}

