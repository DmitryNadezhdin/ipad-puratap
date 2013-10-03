
using System;
using System.Drawing;

using MonoTouch.Foundation;
using MonoTouch.UIKit;

namespace Puratap
{
	public partial class NotDoneCommentViewController : UIViewController
	{
		EventHandler CancelClicked;
		EventHandler DoneClicked;

		PrePlumbingCheckView _parent;
		string NotDoneReason;

		public NotDoneCommentViewController (PrePlumbingCheckView parent, string notDoneReason) : base ("NotDoneCommentViewController", null)
		{
			_parent = parent;
			NotDoneReason = notDoneReason;
		}
		
		public override void DidReceiveMemoryWarning ()
		{
			// Releases the view if it doesn't have a superview.
			base.DidReceiveMemoryWarning ();
			
			// Release any cached data, images, etc that aren't in use.
		}
		
		public override void ViewDidLoad ()
		{
			base.ViewDidLoad ();
			
			// Perform any additional setup after loading the view, typically from a nib.

			CancelClicked = delegate(object sender, EventArgs e) {
				_parent.NavigationController.PopToViewController ( _parent.NavigationController.ViewControllers[0], true);
			};

			DoneClicked = delegate(object sender, EventArgs e) {
				_parent.NavigationController.PopToRootViewController (true);
				_parent.SaveInfo_JobNotDone(NotDoneReason, this.tvComment.Text);
			};

			btnCancel.Clicked += CancelClicked;
			btnDone.Clicked += DoneClicked;
		}

		public override void ViewDidAppear (bool animated)
		{
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

