// WARNING
//
// This file has been generated automatically by MonoDevelop to store outlets and
// actions made in the Xcode designer. If it is removed, they will be lost.
// Manual changes to this file may not be handled correctly.
//
using Foundation;

namespace Puratap
{
	[Register ("JobServiceCallViewController")]
	partial class JobServiceCallViewController
	{
		[Outlet]
		UIKit.UITextField tfFollowUp { get; set; }

		[Outlet]
		UIKit.UILabel lbCustomerNumber { get; set; }

		[Outlet]
		UIKit.UILabel lbCustomerName { get; set; }

		[Outlet]
		UIKit.UILabel lbDate { get; set; }

		[Outlet]
		UIKit.UILabel lbServiceRepName { get; set; }

		[Outlet]
		UIKit.UITextField tfFilterType { get; set; }

		[Outlet]
		UIKit.UITextField tfTapType { get; set; }

		[Outlet]
		UIKit.UITextField tfPressureTest { get; set; }

		[Outlet]
		UIKit.UITextField tfUnitCondition { get; set; }

		[Outlet]
		UIKit.UITextField tfChemicalsInCupboard { get; set; }

		[Outlet]
		UIKit.UIImageView ivUnitImage { get; set; }

		[Action ("acFollowUpTouchDown")]
		partial void acFollowUpTouchDown ();

		[Action ("acFilterTypeTouchDown:")]
		partial void acFilterTypeTouchDown (Foundation.NSObject sender);

		[Action ("acTapTypeTouchDown:")]
		partial void acTapTypeTouchDown (Foundation.NSObject sender);

		[Action ("acUnitConditionTouchDown:")]
		partial void acUnitConditionTouchDown (Foundation.NSObject sender);

		[Action ("acChemicalsInCupboardTouchDown:")]
		partial void acChemicalsInCupboardTouchDown (Foundation.NSObject sender);

		[Action ("acPressureTestEditingDidEnd:")]
		partial void acPressureTestEditingDidEnd (Foundation.NSObject sender);
		
		void ReleaseDesignerOutlets ()
		{
			if (tfFollowUp != null) {
				tfFollowUp.Dispose ();
				tfFollowUp = null;
			}

			if (lbCustomerNumber != null) {
				lbCustomerNumber.Dispose ();
				lbCustomerNumber = null;
			}

			if (lbCustomerName != null) {
				lbCustomerName.Dispose ();
				lbCustomerName = null;
			}

			if (lbDate != null) {
				lbDate.Dispose ();
				lbDate = null;
			}

			if (lbServiceRepName != null) {
				lbServiceRepName.Dispose ();
				lbServiceRepName = null;
			}

			if (tfFilterType != null) {
				tfFilterType.Dispose ();
				tfFilterType = null;
			}

			if (tfTapType != null) {
				tfTapType.Dispose ();
				tfTapType = null;
			}

			if (tfPressureTest != null) {
				tfPressureTest.Dispose ();
				tfPressureTest = null;
			}

			if (tfUnitCondition != null) {
				tfUnitCondition.Dispose ();
				tfUnitCondition = null;
			}

			if (tfChemicalsInCupboard != null) {
				tfChemicalsInCupboard.Dispose ();
				tfChemicalsInCupboard = null;
			}

			if (ivUnitImage != null) {
				ivUnitImage.Dispose ();
				ivUnitImage = null;
			}
		}
	}
}
