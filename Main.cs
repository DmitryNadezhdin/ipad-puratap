using System;
using System.Collections.Generic;
using System.Linq;
using MonoTouch.Foundation;
using MonoTouch.UIKit;
using MonoTouch.ObjCRuntime;

namespace Puratap
{

	public class Application
	{
		public static bool RunningOnDevice()
		{
			return (Runtime.Arch == Arch.DEVICE);
		}

		// This is the main entry point of the application.
		static void Main (string[] args)
		{
			// if you want to use a different Application Delegate class from "AppDelegate"
			// you can specify it here.

			UIApplication.Main (args, null, "AppDelegate");
		}
	}
}
