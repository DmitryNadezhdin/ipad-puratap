// WARNING
//
// This file has been generated automatically by MonoDevelop to store outlets and
// actions made in the Xcode designer. If it is removed, they will be lost.
// Manual changes to this file may not be handled correctly.
//
using MonoTouch.Foundation;

namespace Puratap
{
	[Register ("NotDoneCommentViewController")]
	partial class NotDoneCommentViewController
	{
		[Outlet]
		MonoTouch.UIKit.UIBarButtonItem btnCancel { get; set; }

		[Outlet]
		MonoTouch.UIKit.UIBarButtonItem btnDone { get; set; }

		[Outlet]
		MonoTouch.UIKit.UITextView tvComment { get; set; }
		
		void ReleaseDesignerOutlets ()
		{
			if (btnCancel != null) {
				btnCancel.Dispose ();
				btnCancel = null;
			}

			if (btnDone != null) {
				btnDone.Dispose ();
				btnDone = null;
			}

			if (tvComment != null) {
				tvComment.Dispose ();
				tvComment = null;
			}
		}
	}
}
