using System;
using MonoTouch.Foundation;
using MonoTouch.UIKit;

namespace Application
{
	public class PaymentsSummaryNavigationController : UINavigationController
	{
		public DetailedTabs Tabs { get; set; } 
			
		public PaymentsSummaryNavigationController (DetailedTabs tabs) : base ()
		{
			Tabs = tabs;
			NavigationBarHidden = true;
		}
	}
}

