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
	[Register ("ServerClient")]
	partial class ServerClientViewController
	{
		[Outlet]
		UIKit.UIActivityIndicatorView aivActivity { get; set; }

		[Outlet]
		UIKit.UIActivityIndicatorView aivConnectingToService { get; set; }

		[Outlet]
		UIKit.UIButton btnChangeDate { get; set; }

		[Outlet]
		UIKit.UIButton btnFTPDataExchange { get; set; }

		[Outlet]
		UIKit.UIButton btnResetDeviceID { get; set; }

		[Outlet]
		UIKit.UIButton btnStartDataExchange { get; set; }

		[Outlet]
		UIKit.UIButton btnUpload { get; set; }

		[Outlet]
		UIKit.UITextView tvLog { get; set; }
		
		void ReleaseDesignerOutlets ()
		{
			if (aivActivity != null) {
				aivActivity.Dispose ();
				aivActivity = null;
			}

			if (aivConnectingToService != null) {
				aivConnectingToService.Dispose ();
				aivConnectingToService = null;
			}

			if (btnChangeDate != null) {
				btnChangeDate.Dispose ();
				btnChangeDate = null;
			}

			if (btnFTPDataExchange != null) {
				btnFTPDataExchange.Dispose ();
				btnFTPDataExchange = null;
			}

			if (btnResetDeviceID != null) {
				btnResetDeviceID.Dispose ();
				btnResetDeviceID = null;
			}

			if (btnStartDataExchange != null) {
				btnStartDataExchange.Dispose ();
				btnStartDataExchange = null;
			}

			if (btnUpload != null) {
				btnUpload.Dispose ();
				btnUpload = null;
			}

			if (tvLog != null) {
				tvLog.Dispose ();
				tvLog = null;
			}
		}
	}
}
