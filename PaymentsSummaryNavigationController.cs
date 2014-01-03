using System;
using MonoTouch.Foundation;
using MonoTouch.UIKit;

namespace Puratap
{
	public class PaymentsSummaryNavigationController : UINavigationController
	{
		public readonly DetailedTabs Tabs; 
			
		public PaymentsSummaryNavigationController (DetailedTabs tabs) : base ()
		{
			Tabs = tabs;
			NavigationBarHidden = true;
		}
	}
}

