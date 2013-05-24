using MonoTouch.UIKit;
using System;

namespace Application
{
	public class UsedPartsNavigationController : UINavigationController
	{
		public DetailedTabs Tabs { get; set; }
		public UsedPartsNavigationController (DetailedTabs tabs)
		{
			Tabs = tabs;
		}
		
		Part _chosenPart;
		public Part ChosenPart {
			get { return _chosenPart; }
			set {
				_chosenPart = value;
				PopViewControllerAnimated (true);
				if (this.TopViewController != null)
				{
					(this.TopViewController as UsedPartsViewController).PartChosen(ChosenPart, false);
				}
			}
		}

	}
}

