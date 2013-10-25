using System;
using MonoTouch.UIKit;

namespace Puratap
{
	public class PrePlumbingCheckNavigationController : UINavigationController
	{
		DetailedTabs _tabs;

		public PrePlumbingCheckNavigationController (DetailedTabs tabs) : base()
		{
			_tabs = tabs;
		}
	}
}

