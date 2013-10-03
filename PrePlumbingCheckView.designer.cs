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
	[Register ("PrePlumbingCheckView")]
	partial class PrePlumbingCheckView
	{
		[Outlet]
		MonoTouch.UIKit.UITextView commentsTextView { get; set; }

		[Outlet]
		MonoTouch.UIKit.UITextField customerAcceptedTextField { get; set; }

		[Outlet]
		MonoTouch.UIKit.UITextField existingDamageTextField { get; set; }

		[Outlet]
		MonoTouch.UIKit.UILabel lbComments { get; set; }

		[Outlet]
		MonoTouch.UIKit.UILabel lbExistingDamage { get; set; }

		[Outlet]
		MonoTouch.UIKit.UILabel lbJobType { get; set; }

		[Outlet]
		MonoTouch.UIKit.UILabel lbNotAPuratapProblem { get; set; }

		[Outlet]
		MonoTouch.UIKit.UITextField leakFittingsTextField { get; set; }

		[Outlet]
		MonoTouch.UIKit.UITextField leakPotentialTextField { get; set; }

		[Outlet]
		MonoTouch.UIKit.UITextField leakTapTextField { get; set; }

		[Outlet]
		MonoTouch.UIKit.UITextField nonPuratapTextField { get; set; }

		[Outlet]
		MonoTouch.UIKit.UITextField notPuratapProblemTextField { get; set; }

		[Outlet]
		MonoTouch.UIKit.UITextField officeFollowupTextField { get; set; }

		[Outlet]
		MonoTouch.UIKit.UITextField oldTubingTextField { get; set; }

		[Outlet]
		MonoTouch.UIKit.UIToolbar ppcToolbar { get; set; }

		[Outlet]
		MonoTouch.UIKit.UITextField UnwillingToSignTextField { get; set; }

		[Outlet]
		MonoTouch.UIKit.UITextField upgradeOfferedTextfield { get; set; }

		[Action ("acLeaveWorkflow:")]
		partial void acLeaveWorkflow (MonoTouch.Foundation.NSObject sender);

		[Action ("acProceed:")]
		partial void acProceed (MonoTouch.Foundation.NSObject sender);

		[Action ("btnChangeJobTypeClicked:")]
		partial void btnChangeJobTypeClicked (MonoTouch.Foundation.NSObject sender);

		[Action ("btnNotDoneClicked:")]
		partial void btnNotDoneClicked (MonoTouch.Foundation.NSObject sender);

		[Action ("customerAcceptedTouchDown:")]
		partial void customerAcceptedTouchDown (MonoTouch.Foundation.NSObject sender);

		[Action ("exisitingDamageTouchDown:")]
		partial void exisitingDamageTouchDown (MonoTouch.Foundation.NSObject sender);

		[Action ("leakFittingsTouchDown:")]
		partial void leakFittingsTouchDown (MonoTouch.Foundation.NSObject sender);

		[Action ("leakPotentialTouchDown:")]
		partial void leakPotentialTouchDown (MonoTouch.Foundation.NSObject sender);

		[Action ("leakTapTouchDown:")]
		partial void leakTapTouchDown (MonoTouch.Foundation.NSObject sender);

		[Action ("nonPuratapTouchDown:")]
		partial void nonPuratapTouchDown (MonoTouch.Foundation.NSObject sender);

		[Action ("NotPuratapProblemTouchDown:")]
		partial void NotPuratapProblemTouchDown (MonoTouch.Foundation.NSObject sender);

		[Action ("officeFollowupTouchDown:")]
		partial void officeFollowupTouchDown (MonoTouch.Foundation.NSObject sender);

		[Action ("oldTubingTouchDown:")]
		partial void oldTubingTouchDown (MonoTouch.Foundation.NSObject sender);

		[Action ("unwillingToSignTouchDown:")]
		partial void unwillingToSignTouchDown (MonoTouch.Foundation.NSObject sender);

		[Action ("upgradeOfferedTouchDown:")]
		partial void upgradeOfferedTouchDown (MonoTouch.Foundation.NSObject sender);
		
		void ReleaseDesignerOutlets ()
		{
			if (leakFittingsTextField != null) {
				leakFittingsTextField.Dispose ();
				leakFittingsTextField = null;
			}

			if (leakTapTextField != null) {
				leakTapTextField.Dispose ();
				leakTapTextField = null;
			}

			if (leakPotentialTextField != null) {
				leakPotentialTextField.Dispose ();
				leakPotentialTextField = null;
			}

			if (oldTubingTextField != null) {
				oldTubingTextField.Dispose ();
				oldTubingTextField = null;
			}

			if (nonPuratapTextField != null) {
				nonPuratapTextField.Dispose ();
				nonPuratapTextField = null;
			}

			if (existingDamageTextField != null) {
				existingDamageTextField.Dispose ();
				existingDamageTextField = null;
			}

			if (upgradeOfferedTextfield != null) {
				upgradeOfferedTextfield.Dispose ();
				upgradeOfferedTextfield = null;
			}

			if (customerAcceptedTextField != null) {
				customerAcceptedTextField.Dispose ();
				customerAcceptedTextField = null;
			}

			if (officeFollowupTextField != null) {
				officeFollowupTextField.Dispose ();
				officeFollowupTextField = null;
			}

			if (commentsTextView != null) {
				commentsTextView.Dispose ();
				commentsTextView = null;
			}

			if (UnwillingToSignTextField != null) {
				UnwillingToSignTextField.Dispose ();
				UnwillingToSignTextField = null;
			}

			if (notPuratapProblemTextField != null) {
				notPuratapProblemTextField.Dispose ();
				notPuratapProblemTextField = null;
			}

			if (lbComments != null) {
				lbComments.Dispose ();
				lbComments = null;
			}

			if (lbNotAPuratapProblem != null) {
				lbNotAPuratapProblem.Dispose ();
				lbNotAPuratapProblem = null;
			}

			if (lbExistingDamage != null) {
				lbExistingDamage.Dispose ();
				lbExistingDamage = null;
			}

			if (lbJobType != null) {
				lbJobType.Dispose ();
				lbJobType = null;
			}

			if (ppcToolbar != null) {
				ppcToolbar.Dispose ();
				ppcToolbar = null;
			}
		}
	}
}
