// WARNING
//
// This file has been generated automatically by Xamarin Studio to store outlets and
// actions made in the UI designer. If it is removed, they will be lost.
// Manual changes to this file may not be handled correctly.
//
using Foundation;
using System.CodeDom.Compiler;

namespace Puratap
{
	[Register ("NotDoneCommentViewController")]
	partial class NotDoneCommentViewController
	{
		[Outlet]
		UIKit.UIBarButtonItem tbtnCancel { get; set; }

		[Outlet]
		UIKit.UIBarButtonItem tbtnDone { get; set; }

		[Outlet]
		UIKit.UITextView tvComment { get; set; }
		
		void ReleaseDesignerOutlets ()
		{
			if (tvComment != null) {
				tvComment.Dispose ();
				tvComment = null;
			}

			if (tbtnDone != null) {
				tbtnDone.Dispose ();
				tbtnDone = null;
			}

			if (tbtnCancel != null) {
				tbtnCancel.Dispose ();
				tbtnCancel = null;
			}
		}
	}
}
