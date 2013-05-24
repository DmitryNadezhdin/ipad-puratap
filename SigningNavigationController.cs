using System;
using MonoTouch.UIKit;

namespace Application
{
	public class SigningNavigationController : UINavigationController
	{
		public DetailedTabs Tabs { get; set; }
		
		public SigningNavigationController (DetailedTabs tabs)
		{
			Tabs = tabs;
			
			this.View.AutosizesSubviews = true;
			this.View.AutoresizingMask = UIViewAutoresizing.FlexibleWidth;
		}
	}
}

