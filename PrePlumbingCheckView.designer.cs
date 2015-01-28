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
	[Register ("PrePlumbingCheckView")]
	partial class PrePlumbingCheckView
	{
		[Outlet]
		UIKit.UITextView commentsTextView { get; set; }

		[Outlet]
		UIKit.UITextField customerAcceptedTextField { get; set; }

		[Outlet]
		UIKit.UITextField existingDamageTextField { get; set; }

		[Outlet]
		UIKit.UILabel lbComments { get; set; }

		[Outlet]
		UIKit.UILabel lbExistingDamage { get; set; }

		[Outlet]
		UIKit.UILabel lbJobType { get; set; }

		[Outlet]
		UIKit.UILabel lbNotAPuratapProblem { get; set; }

		[Outlet]
		UIKit.UITextField leakFittingsTextField { get; set; }

		[Outlet]
		UIKit.UITextField leakPotentialTextField { get; set; }

		[Outlet]
		UIKit.UITextField leakTapTextField { get; set; }

		[Outlet]
		UIKit.UITextField nonPuratapTextField { get; set; }

		[Outlet]
		UIKit.UITextField notPuratapProblemTextField { get; set; }

		[Outlet]
		UIKit.UITextField officeFollowupTextField { get; set; }

		[Outlet]
		UIKit.UITextField oldTubingTextField { get; set; }

		[Outlet]
		UIKit.UIToolbar ppcToolbar { get; set; }

		[Outlet]
		UIKit.UITextField UnwillingToSignTextField { get; set; }

		[Outlet]
		UIKit.UITextField upgradeOfferedTextfield { get; set; }

		[Action ("acLeaveWorkflow:")]
		partial void acLeaveWorkflow (Foundation.NSObject sender);

		[Action ("acProceed:")]
		partial void acProceed (Foundation.NSObject sender);

		[Action ("btnChangeJobTypeClicked:")]
		partial void btnChangeJobTypeClicked (Foundation.NSObject sender);

		[Action ("btnNotDoneClicked:")]
		partial void btnNotDoneClicked (Foundation.NSObject sender);

		[Action ("customerAcceptedTouchDown:")]
		partial void customerAcceptedTouchDown (Foundation.NSObject sender);

		[Action ("exisitingDamageTouchDown:")]
		partial void exisitingDamageTouchDown (Foundation.NSObject sender);

		[Action ("leakFittingsTouchDown:")]
		partial void leakFittingsTouchDown (Foundation.NSObject sender);

		[Action ("leakPotentialTouchDown:")]
		partial void leakPotentialTouchDown (Foundation.NSObject sender);

		[Action ("leakTapTouchDown:")]
		partial void leakTapTouchDown (Foundation.NSObject sender);

		[Action ("nonPuratapTouchDown:")]
		partial void nonPuratapTouchDown (Foundation.NSObject sender);

		[Action ("NotPuratapProblemTouchDown:")]
		partial void NotPuratapProblemTouchDown (Foundation.NSObject sender);

		[Action ("officeFollowupTouchDown:")]
		partial void officeFollowupTouchDown (Foundation.NSObject sender);

		[Action ("oldTubingTouchDown:")]
		partial void oldTubingTouchDown (Foundation.NSObject sender);

		[Action ("unwillingToSignTouchDown:")]
		partial void unwillingToSignTouchDown (Foundation.NSObject sender);

		[Action ("upgradeOfferedTouchDown:")]
		partial void upgradeOfferedTouchDown (Foundation.NSObject sender);
		
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
