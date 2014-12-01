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

		Assembly _chosenAssembly;
		public Assembly ChosenAssembly {
			get { return _chosenAssembly; }
			set { 
				_chosenAssembly = value;
				PopViewControllerAnimated(true);
				if (this.TopViewController != null) {
					(this.TopViewController as UsedPartsViewController).AssemblyChosen (ChosenAssembly, false);
				}
			}
		}

	}
}

