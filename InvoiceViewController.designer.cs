// WARNING
//
// This file has been generated automatically by MonoDevelop to store outlets and
// actions made in the Xcode designer. If it is removed, they will be lost.
// Manual changes to this file may not be handled correctly.
//
using MonoTouch.Foundation;

namespace Application
{
	[Register ("PaymentViewController")]
	partial class PaymentViewController
	{
		[Outlet]
		MonoTouch.UIKit.UITextField tfToBeCollected { get; set; }

		[Outlet]
		MonoTouch.UIKit.UITextField tfPaymentType { get; set; }

		[Outlet]
		MonoTouch.UIKit.UITextField tfChequeNumber { get; set; }

		[Outlet]
		MonoTouch.UIKit.UITextField tfCreditCardNumber { get; set; }

		[Outlet]
		MonoTouch.UIKit.UITextField tfTotalMoneyReceived { get; set; }

		[Outlet]
		MonoTouch.UIKit.UILabel lbTip { get; set; }

		[Outlet]
		MonoTouch.UIKit.UITextField tfInvoicePO { get; set; }

		[Outlet]
		MonoTouch.UIKit.UITextField tfCreditCardName { get; set; }

		[Outlet]
		MonoTouch.UIKit.UITextField tfCreditCardExpiry { get; set; }

		[Outlet]
		MonoTouch.UIKit.UILabel lbCCNumberTip { get; set; }

		[Outlet]
		MonoTouch.UIKit.UILabel lbExpiryDateInvalid { get; set; }

		[Outlet]
		MonoTouch.UIKit.UILabel lbCardOwnerNameInvalid { get; set; }

		[Outlet]
		MonoTouch.UIKit.UILabel lbChequeNumberInvalid { get; set; }

		[Outlet]
		MonoTouch.UIKit.UILabel lbJobsInCluster { get; set; }

		[Outlet]
		MonoTouch.UIKit.UIToolbar myToolbar { get; set; }

		[Outlet]
		MonoTouch.UIKit.UIBarButtonItem btnBack { get; set; }

		[Outlet]
		MonoTouch.UIKit.UIBarButtonItem btnProceed { get; set; }

		[Outlet]
		MonoTouch.UIKit.UIBarButtonItem btnClearChildJobs { get; set; }

		[Outlet]
		MonoTouch.UIKit.UIBarButtonItem btnAddChildJob { get; set; }

		[Outlet]
		MonoTouch.UIKit.UISegmentedControl scPaymentType { get; set; }

		[Outlet]
		MonoTouch.UIKit.UIBarButtonItem btnSplitPayment { get; set; }

		[Outlet]
		MonoTouch.UIKit.UISegmentedControl scSplitPaymentMethod1 { get; set; }

		[Outlet]
		MonoTouch.UIKit.UISegmentedControl scSplitPaymentMethod2 { get; set; }

		[Outlet]
		MonoTouch.UIKit.UITextField tfSplitPaymentAmount1 { get; set; }

		[Outlet]
		MonoTouch.UIKit.UITextField tfSplitPaymentAmount2 { get; set; }
		
		void ReleaseDesignerOutlets ()
		{
			if (tfToBeCollected != null) {
				tfToBeCollected.Dispose ();
				tfToBeCollected = null;
			}

			if (tfPaymentType != null) {
				tfPaymentType.Dispose ();
				tfPaymentType = null;
			}

			if (tfChequeNumber != null) {
				tfChequeNumber.Dispose ();
				tfChequeNumber = null;
			}

			if (tfCreditCardNumber != null) {
				tfCreditCardNumber.Dispose ();
				tfCreditCardNumber = null;
			}

			if (tfTotalMoneyReceived != null) {
				tfTotalMoneyReceived.Dispose ();
				tfTotalMoneyReceived = null;
			}

			if (lbTip != null) {
				lbTip.Dispose ();
				lbTip = null;
			}

			if (tfInvoicePO != null) {
				tfInvoicePO.Dispose ();
				tfInvoicePO = null;
			}

			if (tfCreditCardName != null) {
				tfCreditCardName.Dispose ();
				tfCreditCardName = null;
			}

			if (tfCreditCardExpiry != null) {
				tfCreditCardExpiry.Dispose ();
				tfCreditCardExpiry = null;
			}

			if (lbCCNumberTip != null) {
				lbCCNumberTip.Dispose ();
				lbCCNumberTip = null;
			}

			if (lbExpiryDateInvalid != null) {
				lbExpiryDateInvalid.Dispose ();
				lbExpiryDateInvalid = null;
			}

			if (lbCardOwnerNameInvalid != null) {
				lbCardOwnerNameInvalid.Dispose ();
				lbCardOwnerNameInvalid = null;
			}

			if (lbChequeNumberInvalid != null) {
				lbChequeNumberInvalid.Dispose ();
				lbChequeNumberInvalid = null;
			}

			if (lbJobsInCluster != null) {
				lbJobsInCluster.Dispose ();
				lbJobsInCluster = null;
			}

			if (myToolbar != null) {
				myToolbar.Dispose ();
				myToolbar = null;
			}

			if (btnBack != null) {
				btnBack.Dispose ();
				btnBack = null;
			}

			if (btnProceed != null) {
				btnProceed.Dispose ();
				btnProceed = null;
			}

			if (btnClearChildJobs != null) {
				btnClearChildJobs.Dispose ();
				btnClearChildJobs = null;
			}

			if (btnAddChildJob != null) {
				btnAddChildJob.Dispose ();
				btnAddChildJob = null;
			}

			if (scPaymentType != null) {
				scPaymentType.Dispose ();
				scPaymentType = null;
			}

			if (btnSplitPayment != null) {
				btnSplitPayment.Dispose ();
				btnSplitPayment = null;
			}

			if (scSplitPaymentMethod1 != null) {
				scSplitPaymentMethod1.Dispose ();
				scSplitPaymentMethod1 = null;
			}

			if (scSplitPaymentMethod2 != null) {
				scSplitPaymentMethod2.Dispose ();
				scSplitPaymentMethod2 = null;
			}

			if (tfSplitPaymentAmount1 != null) {
				tfSplitPaymentAmount1.Dispose ();
				tfSplitPaymentAmount1 = null;
			}

			if (tfSplitPaymentAmount2 != null) {
				tfSplitPaymentAmount2.Dispose ();
				tfSplitPaymentAmount2 = null;
			}
		}
	}
}
