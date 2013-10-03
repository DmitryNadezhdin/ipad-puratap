// WARNING
//
// This file has been generated automatically by MonoDevelop to store outlets and
// actions made in the Xcode designer. If it is removed, they will be lost.
// Manual changes to this file may not be handled correctly.
//
using MonoTouch.Foundation;

namespace Puratap
{
	[Register ("JobFilterChangeViewController")]
	partial class JobFilterChangeViewController
	{
		[Outlet]
		MonoTouch.UIKit.UITextField tfFilterChangeType { get; set; }

		[Outlet]
		MonoTouch.UIKit.UIButton btnUsedAdditionalParts { get; set; }

		[Outlet]
		MonoTouch.UIKit.UITableView tblAdditionalParts { get; set; }

		[Action ("acFilterChangeTypeTouchDown:")]
		partial void acFilterChangeTypeTouchDown (MonoTouch.Foundation.NSObject sender);

		[Action ("acUsedAdditionalParts:")]
		partial void acUsedAdditionalParts (MonoTouch.Foundation.NSObject sender);
	}
}
