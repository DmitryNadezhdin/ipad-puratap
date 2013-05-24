// WARNING
//
// This file has been generated automatically by MonoDevelop to store outlets and
// actions made in the Xcode designer. If it is removed, they will be lost.
// Manual changes to this file may not be handled correctly.
//
using MonoTouch.Foundation;

namespace Application
{
	[Register ("PrePlumbingCheckView")]
	partial class PrePlumbingCheckView
	{
		[Outlet]
		MonoTouch.UIKit.UITextField leakFittingsTextField { get; set; }

		[Outlet]
		MonoTouch.UIKit.UITextField leakTapTextField { get; set; }

		[Outlet]
		MonoTouch.UIKit.UITextField leakPotentialTextField { get; set; }

		[Outlet]
		MonoTouch.UIKit.UITextField oldTubingTextField { get; set; }

		[Outlet]
		MonoTouch.UIKit.UITextField nonPuratapTextField { get; set; }

		[Outlet]
		MonoTouch.UIKit.UITextField existingDamageTextField { get; set; }

		[Outlet]
		MonoTouch.UIKit.UITextField upgradeOfferedTextfield { get; set; }

		[Outlet]
		MonoTouch.UIKit.UITextField customerAcceptedTextField { get; set; }

		[Outlet]
		MonoTouch.UIKit.UITextField officeFollowupTextField { get; set; }

		[Outlet]
		MonoTouch.UIKit.UITextView commentsTextView { get; set; }

		[Outlet]
		MonoTouch.UIKit.UITextField UnwillingToSignTextField { get; set; }

		[Outlet]
		MonoTouch.UIKit.UITextField notPuratapProblemTextField { get; set; }

		[Outlet]
		MonoTouch.UIKit.UILabel lbComments { get; set; }

		[Outlet]
		MonoTouch.UIKit.UILabel lbNotAPuratapProblem { get; set; }

		[Outlet]
		MonoTouch.UIKit.UILabel lbExistingDamage { get; set; }

		[Outlet]
		MonoTouch.UIKit.UILabel lbJobType { get; set; }

		[Action ("acLeaveWorkflow:")]
		partial void acLeaveWorkflow (MonoTouch.Foundation.NSObject sender);

		[Action ("acProceed:")]
		partial void acProceed (MonoTouch.Foundation.NSObject sender);

		[Action ("btnNotDoneClicked:")]
		partial void btnNotDoneClicked (MonoTouch.Foundation.NSObject sender);

		[Action ("btnChangeJobTypeClicked:")]
		partial void btnChangeJobTypeClicked (MonoTouch.Foundation.NSObject sender);

		[Action ("leakFittingsTouchDown:")]
		partial void leakFittingsTouchDown (MonoTouch.Foundation.NSObject sender);

		[Action ("leakTapTouchDown:")]
		partial void leakTapTouchDown (MonoTouch.Foundation.NSObject sender);

		[Action ("leakPotentialTouchDown:")]
		partial void leakPotentialTouchDown (MonoTouch.Foundation.NSObject sender);

		[Action ("oldTubingTouchDown:")]
		partial void oldTubingTouchDown (MonoTouch.Foundation.NSObject sender);

		[Action ("nonPuratapTouchDown:")]
		partial void nonPuratapTouchDown (MonoTouch.Foundation.NSObject sender);

		[Action ("exisitingDamageTouchDown:")]
		partial void exisitingDamageTouchDown (MonoTouch.Foundation.NSObject sender);

		[Action ("NotPuratapProblemTouchDown:")]
		partial void NotPuratapProblemTouchDown (MonoTouch.Foundation.NSObject sender);

		[Action ("upgradeOfferedTouchDown:")]
		partial void upgradeOfferedTouchDown (MonoTouch.Foundation.NSObject sender);

		[Action ("customerAcceptedTouchDown:")]
		partial void customerAcceptedTouchDown (MonoTouch.Foundation.NSObject sender);

		[Action ("officeFollowupTouchDown:")]
		partial void officeFollowupTouchDown (MonoTouch.Foundation.NSObject sender);

		[Action ("unwillingToSignTouchDown:")]
		partial void unwillingToSignTouchDown (MonoTouch.Foundation.NSObject sender);
	}
}
