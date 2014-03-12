using MonoTouch.UIKit;
using System;

namespace Puratap
{
	public class UsedPartsNavigationController : UINavigationController
	{
		public readonly DetailedTabs Tabs;
		public UsedPartsNavigationController (DetailedTabs tabs)
		{
			Tabs = tabs;
			this.NavigationBar.BarStyle = UIBarStyle.Default; // .Black;

			// DEPRECATED
//			if (MyConstants.iOSVersion >= 7) {
//				this.NavigationBar.TintColor = UIColor.Blue;
//				this.NavigationBar.BarTintColor = UIColor.FromRGBA (0, 0, 0, 25); 
//				this.NavigationBar.SetTitleTextAttributes (new UITextAttributes () {
//					TextColor = UIColor.Black,
//					TextShadowColor = UIColor.Clear
//				});
//			}
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

