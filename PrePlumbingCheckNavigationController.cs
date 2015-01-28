using System;
using UIKit;

namespace Puratap
{
	public class PrePlumbingCheckNavigationController : UINavigationController
	{
		public readonly DetailedTabs Tabs;

		public PrePlumbingCheckNavigationController (DetailedTabs tabs) : base()
		{
			Tabs = tabs;
		}
	}
}

