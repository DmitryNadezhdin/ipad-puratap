// WARNING
//
// This file has been generated automatically by Xamarin Studio to store outlets and
// actions made in the UI designer. If it is removed, they will be lost.
// Manual changes to this file may not be handled correctly.
//
using MonoTouch.Foundation;
using System.CodeDom.Compiler;

namespace Puratap
{
	[Register ("RunRouteViewController")]
	partial class RunRouteViewController
	{
		[Outlet]
		MonoTouch.UIKit.UIButton btnFindRoute { get; set; }

		[Outlet]
		MonoTouch.UIKit.UILabel lbRouteSearchIterations { get; set; }

		[Outlet]
		MonoTouch.UIKit.UIProgressView pvRouteSearchProgress { get; set; }
		
		void ReleaseDesignerOutlets ()
		{
			if (lbRouteSearchIterations != null) {
				lbRouteSearchIterations.Dispose ();
				lbRouteSearchIterations = null;
			}

			if (pvRouteSearchProgress != null) {
				pvRouteSearchProgress.Dispose ();
				pvRouteSearchProgress = null;
			}

			if (btnFindRoute != null) {
				btnFindRoute.Dispose ();
				btnFindRoute = null;
			}
		}
	}
}
