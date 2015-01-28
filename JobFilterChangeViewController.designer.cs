// WARNING
//
// This file has been generated automatically by MonoDevelop to store outlets and
// actions made in the Xcode designer. If it is removed, they will be lost.
// Manual changes to this file may not be handled correctly.
//
using Foundation;

namespace Puratap
{
	[Register ("JobFilterChangeViewController")]
	partial class JobFilterChangeViewController
	{
		[Outlet]
		UIKit.UITextField tfFilterChangeType { get; set; }

		[Outlet]
		UIKit.UIButton btnUsedAdditionalParts { get; set; }

		[Outlet]
		UIKit.UITableView tblAdditionalParts { get; set; }

		[Action ("acFilterChangeTypeTouchDown:")]
		partial void acFilterChangeTypeTouchDown (Foundation.NSObject sender);

		[Action ("acUsedAdditionalParts:")]
		partial void acUsedAdditionalParts (Foundation.NSObject sender);
	}
}
