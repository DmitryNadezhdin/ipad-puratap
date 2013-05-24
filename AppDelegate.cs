using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Linq;
using System.IO;
using MonoTouch.UIKit;
using MonoTouch.Foundation;
using MonoTouch.CoreLocation;
using Mono.Data.Sqlite;
using System.Threading;
using MonoTouch.TestFlight; 

namespace Application
{
	// The UIApplicationDelegate for the application. This class is responsible for launching the 
	// User Interface of the application, as well as listening (and optionally responding) to 
	// application events from iOS.
	[Register ("AppDelegate")]
	public partial class AppDelegate : UIApplicationDelegate
	{
		// removed the following additional mtouch build parameters, since a release build would not start with them enabled:
		// -nosymbolstrip -nostrip -cxx -gcc_flags "-lgcc_eh -L${ProjectDir} -ltestflight -ObjC"

		// class-level declarations
		UIWindow window;
		public DetailedTabs _tabs;
		JobRunTable _jobs;
		UISplitViewController _split;
		UINavigationController _navTabs;

		public LocManager myLocationManager = null;
		public LocDelegate myLocationDelegate = null;

		volatile private List<DeviceLocation> _locationsBuffer = new List<DeviceLocation>();
		public List<DeviceLocation> LocationsBuffer { get { return _locationsBuffer; } set { _locationsBuffer = value; } }


		// This method is invoked when the application has loaded and is ready to run. In this 
		// method you should instantiate the window, load the UI into it and then make the window
		// visible.
		//
		// You have approximately 17 seconds to return from this method, or iOS will terminate your application.
		public override bool FinishedLaunching (UIApplication app, NSDictionary options)
		{	
			// EnableCrashReporting ();
			MonoTouch.TestFlight.TestFlight.SetDeviceIdentifier (MyConstants.DeviceID);
			MonoTouch.TestFlight.TestFlight.TakeOffThreadSafe("f1e1ead5-5ee8-4a3c-a52b-a18e7919b06d");
			InitializeLocationObjects();

			// if database file does not exist, copy test database from app bundle to /Documents folder so that we can edit it
		
			// DEBUG :: if (! File.Exists(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Personal), "NEWTESTDB.sqlite")) )
			{
				File.Delete (Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Personal), "NEWTESTDB.sqlite"));
				string _from = Path.Combine(NSBundle.MainBundle.BundlePath, "NEWTESTDB.sqlite");
				string _to = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Personal), "NEWTESTDB.sqlite");
				File.Copy(	_from,	_to );
			}
			// create a new window instance based on the screen size
			window = new UIWindow (UIScreen.MainScreen.Bounds);

			// create a tableview that will contain the jobs (left side)
			_jobs = new JobRunTable (UITableViewStyle.Grouped);

			// create the tabcontroller that defines and handles the tabs (rightside)  
			_tabs = new DetailedTabs (_jobs, this);
			_tabs.Title = "Puratap";

			// link the two above controllers
			_jobs._tabs = _tabs;
			_tabs._jobRunTable = _jobs;

			_navTabs = new UINavigationController(_tabs);
			_navTabs.NavigationBar.BarStyle = UIBarStyle.Black;
			_navTabs.NavigationBar.Translucent = true;
			

			// create a split controller that will hold the two above controllers
			_split = new UISplitViewController ();
			
			// add _jobs and _tabs to split view controller
			_split.ViewControllers = new UIViewController[] {
				_jobs,
				_navTabs // _tabs
			};

			_split.Delegate = new SplitDelegate();
			
			// make the split controller the root (main) application controller
			window.RootViewController = _split;

			// ?? DEBUG
//			try {
//				NSUrl url = (NSUrl) options.ValueForKey (UIApplication.LaunchOptionsUrlKey);
//				if (url != null)
//				{
//					// Console.WriteLine (url.Path);
//				}
//			} 
//			catch {}


			// if database file exists, check its integrity 
			try {
				if (_tabs._jobRunTable._ds.TestDBIntegrity () )
					// if database integrity check went ok, load customers and jobs from it
					_tabs._jobRunTable._ds.LoadJobRun (true);
				else
				{
					var integrityCheckFailedAlert = new UIAlertView ("Database integrity check failed", "We are really sorry about that.\nTry loading the data anyway?", null, "No", "Yes");
					integrityCheckFailedAlert.Dismissed += delegate(object sender, UIButtonEventArgs e) {
						if (e.ButtonIndex != integrityCheckFailedAlert.CancelButtonIndex)
						{
							_tabs._jobRunTable._ds.LoadJobRun (true);
						}
					};
					this.InvokeOnMainThread (delegate {
						integrityCheckFailedAlert.Show ();
					});
				}
			} 
			finally 
			{  
				window.MakeKeyAndVisible ();
			}
			return true;
		}

//		public override bool ShouldSaveApplicationState (UIApplication application, NSCoder coder)
//		{
//			return true;
//		}
//
//		public override bool ShouldRestoreApplicationState (UIApplication application, NSCoder coder)
//		{
//			return true;
//		}

		public override void WillTerminate (UIApplication application)
		{
			// Console.WriteLine ("AppDelegate.WillTerminate method fired.");
			myLocationDelegate.DumpLocationsBufferToDatabase ();
		}

		public override void DidEnterBackground (UIApplication application)
		{
			// Console.WriteLine ("AppDelegate.DidEnterBackground method fired.");
			int taskID = 0;
			NSAction backgroundTimerExpired = delegate {
				application.EndBackgroundTask (taskID);
				taskID = UIApplication.BackgroundTaskInvalid;
			};
			
			taskID = application.BeginBackgroundTask (backgroundTimerExpired);

			myLocationDelegate.DumpLocationsBufferToDatabase ();

			application.EndBackgroundTask (taskID);
			taskID = UIApplication.BackgroundTaskInvalid;
		}

		public override bool HandleOpenURL (UIApplication application, MonoTouch.Foundation.NSUrl url)
		{
			var loadRunPrompt = new UIAlertView ("Are you sure?", "Run path: "+url.RelativePath, null, "No", "Yes");
			loadRunPrompt.Dismissed += delegate (object sender, UIButtonEventArgs e) {
				if (e.ButtonIndex != loadRunPrompt.CancelButtonIndex)
				{
					// move the file from Inbox to Documents folder
					//// string fileName = Path.GetFileName (url.Path);

					string openedFileName = Environment.GetFolderPath (Environment.SpecialFolder.Personal) +"/"+ Path.GetFileName (url.Path);
					File.Move (url.Path, openedFileName);
					// set the appropriate keys in the app so that this database becomes the current working one
					MyConstants.DBReceivedFromServer = openedFileName;
					// issue LoadJobRun call
					_tabs._jobRunTable._ds.LoadJobRun (true);
				}
				else
				{
					// delete the file that has been copied to Inbox folder of the app
					File.Delete (url.Path);
				}
			};
			loadRunPrompt.Show ();

			return true;
		}

		/*
		public override void WillEnterForeground (UIApplication application)
		{
			// this should allow to change the dates automatically
			if (MyConstants.AUTO_CHANGE_DATES == true)
			{
				string currentDate = String.Format (" '{0}' ", DateTime.Now.Date.ToString ("yyyy-MM-dd"));
				MyConstants.DEBUG_TODAY = currentDate;

				if (_tabs.Mode == DetailedTabsMode.Lookup)
				{
					try 
					{	
						_tabs._scView.Log ("AppDelegate.WillEnterForeground : Reloading database...");
						_tabs._jobRunTable._ds.GetCustomersFromDB ();
						_tabs._scView.Log ("AppDelegate.WillEnterForeground : Database reloaded.");
					} 
					finally {  	}
				}
			}
		} */

	
		public class SplitDelegate : UISplitViewControllerDelegate
	    {		// this defines split controller behavior in case of device orientation changes
	        public override void WillHideViewController (UISplitViewController svc, UIViewController aViewController, UIBarButtonItem barButtonItem, UIPopoverController pc)
	        {
	            DetailedTabs dvc = svc.ViewControllers[1] as DetailedTabs;
	            
	            if (dvc != null) {
					// defines the new frame for right side view controller which should now take up the entire screen space
					dvc.View.Frame = new System.Drawing.RectangleF(0,0, 768, 1004);
					
					// adds a button which will show the left side table with customers data when tapped
	                dvc.AddLeftNavBarButton (barButtonItem);
					
					// defines a navigation frame for the navigation bar
					dvc.MyNavigationBar.Frame = new System.Drawing.RectangleF(0, 0, dvc.View.Bounds.Width,44);
					
					// set up the popover view controller ( JobRunTable )
	                dvc.Popover = pc;
	            }
	        }
			
			
	
	        public override void WillShowViewController (UISplitViewController svc, UIViewController aViewController, UIBarButtonItem button)
	        {
	            DetailedTabs dvc = svc.ViewControllers[1] as DetailedTabs;
	            
	            if (dvc != null) {
					// if we can get the detailed tabs controller, undo all the changes made when orientation changed last time
					dvc.View.Frame = new System.Drawing.RectangleF(0,0, 703, 748);		// set the frame to old size
	                dvc.RemoveLeftNavBarButton ();															// remove navigation bar button
					// dvc.NavigationBar.Frame = new System.Drawing.RectangleF(0, 0, dvc.View.Bounds.Width, 44);
	                dvc.Popover = null;																			// remove popover controller link
	            }
			}
	    }

		// THE CODE BELOW IS TO PREVENT TESTFLIGHT FRAMEWORK FROM CRASHING THE APP
		[DllImport ("libc")]
		private static extern int sigaction (Signal sig, IntPtr act, IntPtr oact);
		
		enum Signal {
			SIGBUS = 10,
			SIGSEGV = 11
		}
		
		static void EnableCrashReporting ()
		{
			IntPtr sigbus = Marshal.AllocHGlobal (512);
			IntPtr sigsegv = Marshal.AllocHGlobal (512);
			
			// Store Mono SIGSEGV and SIGBUS handlers
			sigaction (Signal.SIGBUS, IntPtr.Zero, sigbus);
			sigaction (Signal.SIGSEGV, IntPtr.Zero, sigsegv);
			
			// Enable crash reporting libraries
			EnableCrashReportingUnsafe ();
			
			// Restore Mono SIGSEGV and SIGBUS handlers            
			sigaction (Signal.SIGBUS, sigbus, IntPtr.Zero);
			sigaction (Signal.SIGSEGV, sigsegv, IntPtr.Zero);
			
			Marshal.FreeHGlobal (sigbus);
			Marshal.FreeHGlobal (sigsegv);
		}
		
		static void EnableCrashReportingUnsafe ()
		{
			// Finally, we can engage this again
			// Had to remove the whole thing previously as the TestFlight.TakeOff call would crash the app during the launch attempt
			if ( Application.RunningOnDevice() )

				TestFlight.TakeOff ("d8d373f84f2089a4f245614aecb024fd_MTMwMDU1MjAxMi0wOS0wOSAxNzoxMDoxMy4xMjA3MjI"); // this is the Puratap team token
				// TestFlight.TakeOff ("f1e1ead5-5ee8-4a3c-a52b-a18e7919b06d"); this is the app token
		}

		void InitializeLocationObjects()
		{
			this.myLocationManager = new LocManager(this);
			this.myLocationDelegate = new LocDelegate(this.myLocationManager);
			this.myLocationManager.Delegate = myLocationDelegate;

			this.myLocationManager.StartUpdatingLocation ();
			this.myLocationManager.StartMonitoringSignificantLocationChanges ();
		}
	}

	public class LocManager : MonoTouch.CoreLocation.CLLocationManager
	{
		public AppDelegate thisApp;
		public LocManager(AppDelegate app) : base ()
		{
			thisApp = app;
			this.DesiredAccuracy = CLLocation.AccurracyBestForNavigation;
			// this.DistanceFilter = 10;
		}
	}

	public class LocDelegate : MonoTouch.CoreLocation.CLLocationManagerDelegate
	{
		// AppDelegate thisApp;
		// private int ThreadCounter = 0;
		private LocManager thisManager;
		private Object locationsBufferLock = new object();

		// device coordinates
		public double thisDeviceLat; 					// { get; set; }
		public double thisDeviceLng; 					// { get; set; }
		volatile public string thisDeviceAddress; 		// { get; set; }
		public CLGeocoder geo = new CLGeocoder();


		public DateTime LastCoordsRecordedTimeStamp { get; set; }

		public LocDelegate(LocManager man) : base()
		{
			// this.thisApp = app;
			thisManager = man;
			LastCoordsRecordedTimeStamp = DateTime.Now.AddMinutes(-2);
		}

		[Obsolete]
		public override void UpdatedLocation (CLLocationManager manager, CLLocation newLocation, CLLocation oldLocation)
		{
			// if one of the coordinates has changed, the address has to be reset
			if (thisDeviceLat != newLocation.Coordinate.Latitude || thisDeviceLng != newLocation.Coordinate.Longitude)
			{
				Interlocked.Exchange<string>(ref thisDeviceAddress, string.Empty);
			}

			thisDeviceLat = newLocation.Coordinate.Latitude;
			thisDeviceLng = newLocation.Coordinate.Longitude;

			if ( (DateTime.Now - LastCoordsRecordedTimeStamp).TotalMinutes >= 1 )
			{
				// string time = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
				// string time = (DateTime.SpecifyKind (newLocation.Timestamp, DateTimeKind.Unspecified)).ToString("yyyy-MM-dd HH:mm:ss");
				string anotherTime = (new DateTime(2001,1,1,0,0,0)).AddSeconds(newLocation.Timestamp.SecondsSinceReferenceDate).ToLocalTime().ToString ("yyyy-MM-dd HH:mm:ss");
				AddLocationToBuffer (thisDeviceLng, thisDeviceLat, anotherTime, thisDeviceAddress, -1, -1);
				LastCoordsRecordedTimeStamp = DateTime.Now;

			}
		}

		public void AddLocationToBuffer(double lng, double lat, string timeStamp, string address, long customer, long job)
		{
			lock (locationsBufferLock)
			{
				var locBuffer = this.thisManager.thisApp.LocationsBuffer;
				var loc = new DeviceLocation();
				loc.Address = address;
				loc.Timestamp = timeStamp;
				loc.Lat = lat;
				loc.Lng = lng;

				long jrtCustomer = (MyConstants._jrt.CurrentCustomer == null)? 0 : MyConstants._jrt.CurrentCustomer.CustomerNumber;
				loc.CustomerID = (customer != -1)? customer : jrtCustomer;
				loc.JobID = job;

				locBuffer.Add (loc);

				// throwing out excess values if more than 300 were saved between buffer flushes
				int k = 0;
				while (locBuffer.Count > 300)
				{
					if (locBuffer[k].Address == String.Empty) // making sure to only throw out ones where no address has been saved
					{
						locBuffer.RemoveAt(k);
					}
					else k++ ;
				}

				LastCoordsRecordedTimeStamp = DateTime.Now;
				try {
					// Console.WriteLine (String.Format ("Location added to buffer: {0}, {1}, {2}, \"{3}\"", loc.Lat, loc.Lng, loc.CustomerID, loc.Address));
				} catch 
				{
					// do nothing
				}
			}
		}

		public override void LocationsUpdated (CLLocationManager manager, CLLocation[] locations)
		{
			CLLocation lastLocation = locations[locations.Length - 1];

			// if one of the coordinates has changed, the address should be reset
			if (thisDeviceLat != lastLocation.Coordinate.Latitude || thisDeviceLng != lastLocation.Coordinate.Longitude)
			{
				Interlocked.Exchange<string>(ref  thisDeviceAddress, string.Empty);
			}

			thisDeviceLat = lastLocation.Coordinate.Latitude;
			thisDeviceLng = lastLocation.Coordinate.Longitude;

			if ( (DateTime.Now - LastCoordsRecordedTimeStamp).TotalMinutes >= 1 )
			{
				// string time = (DateTime.SpecifyKind (lastLocation.Timestamp, DateTimeKind.Unspecified)).ToString("yyyy-MM-dd HH:mm:ss"); // DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
				string anotherTime = (new DateTime(2001,1,1,0,0,0)).AddSeconds(lastLocation.Timestamp.SecondsSinceReferenceDate).ToLocalTime().ToString ("yyyy-MM-dd HH:mm:ss");
				AddLocationToBuffer (thisDeviceLng, thisDeviceLat, anotherTime, thisDeviceAddress, -1, -1);
				LastCoordsRecordedTimeStamp = DateTime.Now;
			}
		}

		public override void Failed (CLLocationManager manager, NSError error)
		{
			if (error.Code == (int)CLError.Denied)
			{
				thisManager.StopUpdatingLocation ();
				thisManager.StopMonitoringSignificantLocationChanges ();
				string time = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
				AddLocationToBuffer (-1, -1, time, "Location monitoring denied by user.", 0, 0);
			}
		}

		struct GeoCodingRequestInfo {
			public long customer;
			public long job;
			public DateTime timeStamp;
			public double lat;
			public double lng;
		}

		private List<GeoCodingRequestInfo> RequestQueue = new List<GeoCodingRequestInfo>();

		public void GeocodeLastKnownLocation()
		{
			var curjob = this.thisManager.thisApp._tabs._jobRunTable.CurrentJob;
			if (this.thisManager.thisApp._tabs._jobRunTable.FindParentJob (curjob) != null)
				curjob = this.thisManager.thisApp._tabs._jobRunTable.FindParentJob (curjob);

			var newRequest = new GeoCodingRequestInfo {
				customer = this.thisManager.thisApp._tabs._jobRunTable.CurrentCustomer.CustomerNumber,
				job = curjob.JobBookingNumber,
				timeStamp = DateTime.Now,
				lat = thisDeviceLat,
				lng = thisDeviceLng
			};

			RequestQueue.Add (newRequest);
			if (!geo.Geocoding)
				geo.ReverseGeocodeLocation (new CLLocation(newRequest.lat, newRequest.lng), HandleGeocodeCompletion);
		}

		private void HandleGeocodeCompletion(CLPlacemark[] placemarks, NSError error)
		{
			string newAddress = "";
			if (error == null)
			{

				if (placemarks.Length > 0 && placemarks != null)
				{
					CLPlacemark placemark = placemarks[0]; // placemarks[placemarks.Length - 1];
					newAddress = placemark.AddressDictionary.Values[0].ToString();
				}
				else
				{
					newAddress = "Geocoding returned empty dataset...";
				}
				Interlocked.Exchange<string>(ref thisDeviceAddress, newAddress);
			}
			else
			{
//				try {
//					Console.WriteLine (String.Format ("Geocoding returned an error: {0} {1}", error.Code, error.Description));
//				} catch {
//					// nothing to be done about this one
//					// this should be removed in release builds
//				}

				newAddress = String.Format ("Geocoding error: {0}", error.Code);
				Interlocked.Exchange<string>(ref thisDeviceAddress, newAddress);
			}

			var CurrentRequest = RequestQueue[0];
			string checkPointMessage = String.Format ("{0} {1}: Job data input finished: Run={2},  CN={3}, Job={4}, Lat={5}, Lng={6}, Address={7}", 
			                                          CurrentRequest.timeStamp.Date.ToString("yyyy-MM-dd"), 
			                                          CurrentRequest.timeStamp.ToString ("HH:mm:ss"), 
			                                          MyConstants.EmployeeID, 
			                                          CurrentRequest.customer, CurrentRequest.job, 
			                                          CurrentRequest.lat, CurrentRequest.lng, newAddress);
			
			MonoTouch.TestFlight.TestFlight.PassCheckpoint (checkPointMessage);
			AddLocationToBuffer(CurrentRequest.lng, CurrentRequest.lat, 
			                    CurrentRequest.timeStamp.ToString ("yyyy-MM-dd HH:mm:ss"), 
			                    "Job data input finished: " + newAddress, 
			                    CurrentRequest.customer, CurrentRequest.job);

			RequestQueue.RemoveAt (0);
			if (RequestQueue.Count > 0)
			{
				geo.ReverseGeocodeLocation (new CLLocation(RequestQueue[0].lat, RequestQueue[0].lng), HandleGeocodeCompletion);
			}
		}

		public void DumpLocationsBufferToDatabase()
		{
			if (File.Exists (ServerClientViewController.dbFilePath) )
			{
				// connect to database
				using (var connection = new SqliteConnection("Data Source="+ServerClientViewController.dbFilePath) )
				{
					using (var cmd = connection.CreateCommand())
					{
						connection.Open();
						using (var transaction = connection.BeginTransaction() )
						{
							try 
							{
								cmd.CommandText = "INSERT INTO IPAD_COORDS (empl_oid, timestamp, lng, lat, address, customer, job_oid) VALUES (:employee_id, :time, :lng, :lat, :address, :customer, :job_id)";
								cmd.Parameters.Clear ();
								cmd.Parameters.Add ("employee_id", System.Data.DbType.Int32);
								cmd.Parameters.Add ("time", System.Data.DbType.String);
								cmd.Parameters.Add ("lng", System.Data.DbType.Double);
								cmd.Parameters.Add ("lat", System.Data.DbType.Double);
								cmd.Parameters.Add ("address", System.Data.DbType.String);
								cmd.Parameters.Add ("customer", System.Data.DbType.Int64);
								cmd.Parameters.Add ("job_id", System.Data.DbType.Int64);

								lock(locationsBufferLock)
								{
									var locBuffer = this.thisManager.thisApp.LocationsBuffer;
									for (int i = 0; i <= locBuffer.Count-1; i++)
									{
										var loc = (DeviceLocation) locBuffer[i];

										cmd.Parameters["employee_id"].Value = MyConstants.EmployeeID;
										cmd.Parameters["time"].Value = loc.Timestamp;
										cmd.Parameters["lng"].Value = loc.Lng;
										cmd.Parameters["lat"].Value = loc.Lat;
										cmd.Parameters["address"].Value = loc.Address;
										cmd.Parameters["customer"].Value = loc.CustomerID;
										cmd.Parameters["job_id"].Value = loc.JobID;

										cmd.ExecuteNonQuery ();
									}
									transaction.Commit();
									locBuffer.Clear ();
									cmd.Parameters.Clear ();
									// this.thisManager.thisApp._tabs._scView.Log(string.Format ("Flushing locations buffer: SUCCESS"));
								}
							}
							catch (Exception e) {
								this.thisManager.thisApp._tabs._scView.Log(string.Format ("Flushing locations buffer: EXCEPTION: {0}\n{1}", e.Message, e.StackTrace));
								transaction.Rollback();
							}
						}	// using (var transaction = connection.BeginTransaction() )
					}	// using (var cmd = connection.CreateCommand())
				} 	// using (var connection = new SqliteConnection(...)
			} // if (File.Exists (ServerClientViewController.dbFilePath) )
		} 
	}
}