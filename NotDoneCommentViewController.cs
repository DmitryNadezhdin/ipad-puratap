
using System;
using System.Drawing;

using MonoTouch.Foundation;
using MonoTouch.UIKit;

namespace Puratap
{
	public partial class NotDoneCommentViewController : UIViewController
	{
		EventHandler tCancelClicked;
		EventHandler tDoneClicked;

		PrePlumbingCheckView _parent;
		UINavigationController _nav;
		string NotDoneReason;

		public NotDoneCommentViewController (PrePlumbingCheckView parent, UINavigationController nav, string notDoneReason) : base ("NotDoneCommentViewController", null)
		{
			_parent = parent;
			_nav = nav;
			NotDoneReason = notDoneReason;
		}
		
		public override void ViewDidLoad ()
		{
			base.ViewDidLoad ();
			
			// Perform any additional setup after loading the view, typically from a nib.

			tCancelClicked = delegate(object sender, EventArgs e) {
				_parent.SetCurrentJobStartedNone();
				_nav.PopToRootViewController (true);
			};
			tDoneClicked = delegate(object sender, EventArgs e) {
				_parent.SaveInfo_JobNotDone (NotDoneReason, this.tvComment.Text);
			};

			tbtnCancel.Clicked += tCancelClicked;
			tbtnDone.Clicked += tDoneClicked;
		}

		public override void ViewDidAppear (bool animated)
		{
			foreach (UITabBarItem tbi in this.TabBarController.TabBar.Items)
				tbi.Enabled = false;			

			tvComment.BecomeFirstResponder ();
			base.ViewDidAppear (animated);
		}

		[Obsolete]
		public override void ViewDidUnload ()
		{
			base.ViewDidUnload ();
			
			// Clear any references to subviews of the main view in order to
			// allow the Garbage Collector to collect them sooner.
			//
			// e.g. myOutlet.Dispose (); myOutlet = null;
			
			ReleaseDesignerOutlets ();
		}

		[Obsolete]
		public override bool ShouldAutorotateToInterfaceOrientation (UIInterfaceOrientation toInterfaceOrientation)
		{
			// Return true for supported orientations
			return true;
		}
	}
}

