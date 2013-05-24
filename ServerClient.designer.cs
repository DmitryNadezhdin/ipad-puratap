// WARNING
//
// This file has been generated automatically by MonoDevelop to store outlets and
// actions made in the Xcode designer. If it is removed, they will be lost.
// Manual changes to this file may not be handled correctly.
//
using MonoTouch.Foundation;

namespace Application
{
	[Register ("ServerClient")]
	partial class ServerClientViewController
	{
		[Outlet]
		MonoTouch.UIKit.UIButton btnDownload { get; set; }

		[Outlet]
		MonoTouch.UIKit.UIButton btnUpload { get; set; }

		[Outlet]
		MonoTouch.UIKit.UITextView tvLog { get; set; }

		[Outlet]
		MonoTouch.UIKit.UIButton btnResetDeviceID { get; set; }

		[Outlet]
		MonoTouch.UIKit.UIActivityIndicatorView aivActivity { get; set; }

		[Outlet]
		MonoTouch.UIKit.UIButton btnChangeDate { get; set; }

		[Outlet]
		MonoTouch.UIKit.UIActivityIndicatorView aivConnectingToService { get; set; }

		[Outlet]
		MonoTouch.UIKit.UIButton btnSubmitFeedback { get; set; }
		
		void ReleaseDesignerOutlets ()
		{
			if (btnDownload != null) {
				btnDownload.Dispose ();
				btnDownload = null;
			}

			if (btnUpload != null) {
				btnUpload.Dispose ();
				btnUpload = null;
			}

			if (tvLog != null) {
				tvLog.Dispose ();
				tvLog = null;
			}

			if (btnResetDeviceID != null) {
				btnResetDeviceID.Dispose ();
				btnResetDeviceID = null;
			}

			if (aivActivity != null) {
				aivActivity.Dispose ();
				aivActivity = null;
			}

			if (btnChangeDate != null) {
				btnChangeDate.Dispose ();
				btnChangeDate = null;
			}

			if (aivConnectingToService != null) {
				aivConnectingToService.Dispose ();
				aivConnectingToService = null;
			}

			if (btnSubmitFeedback != null) {
				btnSubmitFeedback.Dispose ();
				btnSubmitFeedback = null;
			}
		}
	}
}
