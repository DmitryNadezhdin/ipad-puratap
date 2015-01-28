using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Linq;
using System.IO;
using UIKit;
using Foundation;
using CoreLocation;
using Mono.Data.Sqlite;
using System.Threading;
// using MonoTouch.TestFlight; 
using MessageUI;


namespace Puratap
{
	// The UIApplicationDelegate for the application. This class is responsible for launching the 
	// User Interface of the application, as well as listening (and optionally responding) to 
	// application events from iOS.
	[Register ("AppDelegate")]
	public partial class AppDelegate : UIApplicationDelegate
	{
		// -nosymbolstrip -nostrip -cxx --registrar:oldstatic

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
			Xamarin.Insights.Initialize ("20b7565ad9a4eee1d56492b26841b0bf70fbb5ed");
			Xamarin.Insights.Identify (MyConstants.DeviceID, MyConstants.EmployeeID.ToString (), MyConstants.EmployeeName);
			// MonoTouch.TestFlight.TestFlight.TakeOffThreadSafe("f1e1ead5-5ee8-4a3c-a52b-a18e7919b06d");
			InitializeLocationObjects();

			// if database file does not exist, copy test database from app bundle to /Documents folder so that we can edit it
		
			// DEBUG :: if (! File.Exists(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Personal), "NEWTESTDB.sqlite")) )
			{
				File.Delete (Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Personal), "NEWTESTDB.sqlite"));
				string _from = Path.Combine(NSBundle.MainBundle.BundlePath, "NEWTESTDB.sqlite");
				string _to = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Personal), "NEWTESTDB.sqlite");
				File.Copy( _from, _to );

				if (File.Exists (Path.Combine (Environment.GetFolderPath (Environment.SpecialFolder.Personal), "Franchisee Training Manual.pdf"))) {
					File.Delete (Path.Combine (Environment.GetFolderPath (Environment.SpecialFolder.Personal), "Franchisee Training Manual.pdf"));
				}
				_from = Path.Combine (NSBundle.MainBundle.BundlePath, "Franchisee Training Manual.pdf");
				_to = Path.Combine (Environment.GetFolderPath (Environment.SpecialFolder.Personal), "Franchisee Training Manual.pdf");
				File.Copy (_from, _to);
			}
			// create a new window instance based on the screen size
			window = new UIWindow (UIScreen.MainScreen.Bounds);

			// create a tableview that will contain the jobs (left side)
			_jobs = new JobRunTable (UITableViewStyle.Grouped);

			// create the tabcontroller that defines and handles the tabs (rightside)  
			_tabs = new DetailedTabs (_jobs, this);
			_tabs.Title = "Puratap " + MyConstants.AppVersion;

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
				if (File.Exists(MyConstants.DBReceivedFromServer)) {
					if (_tabs._jobRunTable._ds.TestDBIntegrity () ) {
						// HACK temporary transitional code for updating from version 1.3c to 1.3d
						// 1.3d requires IPAD_ORDERING field in PL_RECOR table
						// some of the databases may not contain it if they were exported before the change was implemented
	
						// check if "IPAD_ORDERING" field exists in database
						string dbPath = MyConstants.DBReceivedFromServer;
						using (var dbCon = new SqliteConnection("Data Source="+dbPath)) {
							dbCon.Open ();
							using (var cmd = dbCon.CreateCommand ()) {
								string sql = " SELECT IPAD_ORDERING FROM PL_RECOR ";
								cmd.CommandText = sql;
								try {
									using (var reader = cmd.ExecuteReader ()) {								
										if (reader.HasRows) {
											// db is good, the IPAD_ORDERING field exists
										}
									} 
								} catch { 
									// IPAD_ORDERING field does not exists, we need to add it
									cmd.Parameters.Clear();
									cmd.CommandText = "ALTER TABLE PL_RECOR ADD COLUMN IPAD_ORDERING INTEGER NULL DEFAULT 9999";
									cmd.ExecuteNonQuery();
									cmd.CommandText = "UPDATE PL_RECOR SET IPAD_ORDERING = 9999";
									cmd.ExecuteNonQuery();
								}
							} // end using dbCommand
						} // end using dbConnection
						// END HACK this code should be removed on 08.11.2013

						// if database integrity check went ok, load customers and jobs from it
						_tabs._jobRunTable._ds.LoadJobRun (1);
					} else {
						var integrityCheckFailedAlert = new UIAlertView ("Database integrity check failed", "Try loading the data anyway? (App may crash if you choose 'Yes')", null, "No", "Yes");
						integrityCheckFailedAlert.Dismissed += delegate(object sender, UIButtonEventArgs e) {
							if (e.ButtonIndex != integrityCheckFailedAlert.CancelButtonIndex)
							{
								_tabs._jobRunTable._ds.LoadJobRun (1);
							}
						};
						this.InvokeOnMainThread (delegate {
							integrityCheckFailedAlert.Show ();
						});
					}
				} else 
				{
					// db file does not exist
					var dbNotfound = new UIAlertView("Database file not found", "Please complete the data exchange with the server.", null, "OK");
					this.InvokeOnMainThread (delegate { dbNotfound.Show(); });
				}
			} 
			finally 
			{  
				window.MakeKeyAndVisible ();

			}
			return true;
		}

		public override void WillTerminate (UIApplication application)
		{
			// Console.WriteLine ("AppDelegate.WillTerminate method fired.");
			myLocationDelegate.DumpLocationsBufferToDatabase ();
		}

		public override void DidEnterBackground (UIApplication application)
		{
			// Console.WriteLine ("AppDelegate.DidEnterBackground method fired.");
			nint taskID = 0;
			Action backgroundTimerExpired = delegate {
				application.EndBackgroundTask (taskID);
				taskID = UIApplication.BackgroundTaskInvalid;
			};
			
			taskID = application.BeginBackgroundTask (backgroundTimerExpired);

			myLocationDelegate.DumpLocationsBufferToDatabase ();

			application.EndBackgroundTask (taskID);
			taskID = UIApplication.BackgroundTaskInvalid;
		}

		public override bool OpenUrl (UIApplication application, NSUrl url, string sourceApplication, NSObject annotation)
		{
			// if in workflow, show a message that workflow has to be finished
			if (_tabs.Mode == DetailedTabsMode.Workflow) {
				var cannotUpdateDuringWorkflow = new UIAlertView ("Cannot add data during workflow.", "Please finish entering data for current job and try again.", null, "OK");
				cannotUpdateDuringWorkflow.Show ();
			} else {
				// brach: extra job or full run replacing current database
				if (url.Path.Contains ("Extra_Job")) {  // adding extra job
					string[] temp = url.Path.Split ('/');
					string fname = temp [temp.Length - 1];
					var addJobPrompt = new UIAlertView ("Are you sure?", "Adding job data from file: " + fname, null, "No", "Yes");
					addJobPrompt.Dismissed += delegate(object sender, UIButtonEventArgs e) {
						if (e.ButtonIndex != addJobPrompt.CancelButtonIndex) {
							// copy database entries from the extra db file to current database and delete the source
							bool copyResult = SqliteCopyDataToDB(url.Path, MyConstants.DBReceivedFromServer);

							if (copyResult == true) {
								// reload the run so that the added job is displayed
								_tabs._jobRunTable._ds.LoadJobRun(2);

								// if data copying is successful, send the e-mail confirmation
								if (MFMailComposeViewController.CanSendMail)
									SendEmailConfirmationJobAccepted( url.Path.Substring(url.Path.LastIndexOf('/')+1) );
								else 
								{
									var notEmailAble = new UIAlertView("iPad cannot send e-mails at this time.", "Please send the confirmation when able.", null, "OK");
									notEmailAble.Show();
								}			
							}
						}
					};
					addJobPrompt.Show ();

				} else { // full run data replacement
					string file = Path.GetFileNameWithoutExtension (url.Path);
					var loadRunPrompt = new UIAlertView ("Are you sure?", "Load runs from file: " + file, null, "No", "Yes");
					loadRunPrompt.Dismissed += delegate (object sender, UIButtonEventArgs e) {
						if (e.ButtonIndex != loadRunPrompt.CancelButtonIndex) {
							// generate run summary for the current run
							if (_tabs._paySummaryView != null)
								_tabs._paySummaryView.GenerateAllSummaryFiles();

							// move the file from Inbox to Documents folder
							//// string fileName = Path.GetFileName (url.Path);
							string fileName = Path.GetFileNameWithoutExtension (url.Path);
							if (fileName.EndsWith ("-1"))
								fileName = fileName.Remove (fileName.Length - 2, 2) + ".sqlite";
							else
								fileName = Path.GetFileName (url.Path);

							// if file with that <name> existed, rename it to EXISTED_<name>
							string openedFileName = Environment.GetFolderPath (Environment.SpecialFolder.Personal) + "/" + fileName; // Path.GetFileName (url.Path);
							if (File.Exists (openedFileName)) {
								// if file with EXISTED_<name> exists, delete it
								File.Delete (openedFileName.Replace (fileName, "EXISTED_" + fileName));
								File.Move (openedFileName, openedFileName.Replace (fileName, "EXISTED_" + fileName));
							}
							File.Move (url.Path, openedFileName);

							// set the appropriate keys in the app so that this database becomes the current working one
							MyConstants.DBReceivedFromServer = openedFileName;

							// call LoadJobRun to reload the run
							_tabs._jobRunTable._ds.LoadJobRun (2);
						} else {
							// delete the file that has been copied to Inbox folder of the app
							File.Delete (url.Path);
						}
					};
					loadRunPrompt.Show ();
				} // endif -- check if it is one extra job or a full run replacement
			} // endif -- check if in workflow
			return true;
		}

//		[Obsolete]
//		public override bool HandleOpenURL (UIApplication application, MonoTouch.Foundation.NSUrl url)
//		{
//			// if in workflow, show a message that workflow has to be finished
//			if (_tabs.Mode == DetailedTabsMode.Workflow) {
//				var cannotUpdateDuringWorkflow = new UIAlertView ("Cannot add data during workflow.", "Please finish entering data for current job and try again.", null, "OK");
//				cannotUpdateDuringWorkflow.Show ();
//			} else {
//				// brach: extra job or full run replacing current database
//				if (url.Path.Contains ("Extra_Job")) {  // adding extra job
//					string[] temp = url.Path.Split ('/');
//					string fname = temp [temp.Length - 1];
//					var addJobPrompt = new UIAlertView ("Are you sure?", "Adding job data from file: " + fname, null, "No", "Yes");
//					addJobPrompt.Dismissed += delegate(object sender, UIButtonEventArgs e) {
//						if (e.ButtonIndex != addJobPrompt.CancelButtonIndex) {
//							// copy database entries from the extra db file to current database and delete the source
//							bool copyResult = SqliteCopyDataToDB(url.Path, MyConstants.DBReceivedFromServer);
//
//							if (copyResult == true) {
//								// reload the run so that the added job is displayed
//								_tabs._jobRunTable._ds.LoadJobRun(2);
//
//								// if data copying is successful, send the e-mail confirmation
//								if (MFMailComposeViewController.CanSendMail)
//									SendEmailConfirmationJobAccepted( url.Path.Substring(url.Path.LastIndexOf('/')+1) );
//								else 
//								{
//									var notEmailAble = new UIAlertView("iPad cannot send e-mails at this time.", "Please send the confirmation when able.", null, "OK");
//									notEmailAble.Show();
//								}			
//							}
//						}
//					};
//					addJobPrompt.Show ();
//
//				} else { // full run data replacement
//					string file = Path.GetFileNameWithoutExtension (url.Path);
//					var loadRunPrompt = new UIAlertView ("Are you sure?", "Load runs from file: " + file, null, "No", "Yes");
//					loadRunPrompt.Dismissed += delegate (object sender, UIButtonEventArgs e) {
//						if (e.ButtonIndex != loadRunPrompt.CancelButtonIndex) {
//							// generate run summary for the current run
//							if (_tabs._paySummaryView != null)
//								_tabs._paySummaryView.GenerateAllSummaryFiles();
//							// move the file from Inbox to Documents folder
//							//// string fileName = Path.GetFileName (url.Path);
//							string fileName = Path.GetFileNameWithoutExtension (url.Path);
//							if (fileName.EndsWith ("-1"))
//								fileName = fileName.Remove (fileName.Length - 2, 2) + ".sqlite";
//							else
//								fileName = Path.GetFileName (url.Path);
//
//							// if file with that <name> existed, rename it to EXISTED_<name>
//							string openedFileName = Environment.GetFolderPath (Environment.SpecialFolder.Personal) + "/" + fileName; // Path.GetFileName (url.Path);
//							if (File.Exists (openedFileName)) {
//								// if file with EXISTED_<name> exists, delete it
//								File.Delete (openedFileName.Replace (fileName, "EXISTED_" + fileName));
//								File.Move (openedFileName, openedFileName.Replace (fileName, "EXISTED_" + fileName));
//							}
//							File.Move (url.Path, openedFileName);
//
//							// set the appropriate keys in the app so that this database becomes the current working one
//							MyConstants.DBReceivedFromServer = openedFileName;
//
//							// call LoadJobRun to reload the run
//							_tabs._jobRunTable._ds.LoadJobRun (2);
//						} else {
//							// delete the file that has been copied to Inbox folder of the app
//							File.Delete (url.Path);
//						}
//					};
//					loadRunPrompt.Show ();
//				} // endif -- check if it is one extra job or a full run replacement
//			} // endif -- check if in workflow
//			return true;
//		}

		private bool SqliteCopyDataToDB(string fromDB, string toDB) {
			bool result = false;

			using (var toDbConnection = new SqliteConnection("Data Source="+toDB)) {
				toDbConnection.Open ();

				using (var toCmd = toDbConnection.CreateCommand()) {
					using (var transIndexes = toDbConnection.BeginTransaction()) {
						try {
							// set up constraints so that nothing can be added twice
							toCmd.CommandText = "CREATE UNIQUE INDEX UNIQUE_CUSNUM ON WCLIENT(CusNum)";
							toCmd.ExecuteNonQuery ();
							toCmd.CommandText = "CREATE UNIQUE INDEX UNIQUE_BOOKNUM ON PL_RECOR(BookNum)";
							toCmd.ExecuteNonQuery ();
							toCmd.CommandText = "CREATE UNIQUE INDEX UNIQUE_UNITNUM ON WSALES(UnitNum)";
							toCmd.ExecuteNonQuery ();
							toCmd.CommandText = "CREATE UNIQUE INDEX UNIQUE_MEMOS ON WCMEMO(CusNum,WMemNum)";
							toCmd.ExecuteNonQuery ();
							toCmd.CommandText = "CREATE UNIQUE INDEX UNIQUE_JOURNAL ON JOURNAL(JDate,AccNum,JNum,CusNum,Debit,Credit)";

							transIndexes.Commit ();
						} catch {
							// if indexes already exist, an exception will be thrown, no big deal here
							transIndexes.Rollback ();
						}
					}

					try {
						// attach the database with extra data to the current DB connection
						toCmd.CommandText = "ATTACH DATABASE '" + fromDB + "' AS ExtraDb;";
						toCmd.ExecuteNonQuery ();
					} catch (Exception e) {
						using (var errorAlert = new UIAlertView("Error adding data", e.Message, null, "OK")) {
							errorAlert.Show ();
						}
						// failed to attach database -- cannot continue -- source file will linger!
						return false;
					}

					try {
						// copy records over from extra db into the main
						toCmd.CommandText = "INSERT OR REPLACE INTO Main.WCLIENT SELECT * FROM ExtraDb.WCLIENT";
						toCmd.ExecuteNonQuery ();
						toCmd.CommandText = "INSERT INTO Main.PL_RECOR SELECT * FROM ExtraDb.PL_RECOR";
						toCmd.ExecuteNonQuery ();
						toCmd.CommandText = "INSERT OR REPLACE INTO Main.WSALES SELECT * FROM ExtraDb.WSALES";
						toCmd.ExecuteNonQuery ();
						toCmd.CommandText = "INSERT OR REPLACE INTO Main.WCMEMO SELECT * FROM ExtraDb.WCMEMO";
						toCmd.ExecuteNonQuery ();
						toCmd.CommandText = "INSERT OR REPLACE INTO Main.Journal SELECT * FROM ExtraDb.JOURNAL";

						result = true;

					} catch (Exception e) {
						using (var errorAlert = new UIAlertView("Error adding data", e.Message, null, "OK")) {
							errorAlert.Show ();
						}
						result = false;

					} finally {
						// detach the database
						toCmd.CommandText = "DETACH DATABASE ExtraDb;";
						toCmd.ExecuteNonQuery();
					}

					// if successfully added, delete the extra job db file
					if (result == true)
						File.Delete (fromDB);

					return result;
				}
			}
		}

		private void SendEmailConfirmationJobAccepted(string dbFileName)
		{
			string[] tst = dbFileName.Split (' ');

			long customerNumber = 0;
			long jobID = 0;
			string emailSender = "";

			try {
				customerNumber = Convert.ToInt64(tst[1]);
				jobID = Convert.ToInt64 (tst [2]);
				emailSender = tst[4] + "@puratap.com";
			} catch {
			}
			Customer c = _jobs.Customers.Find (customer => customer.CustomerNumber == customerNumber);
			if (c != null) {
				var mail = new MFMailComposeViewController();

				Action act = delegate {	};

				string msg = String.Format ("Dear office,\r\n\r\n I hereby inform you that job {0} for customer {1} has been accepted.", jobID, c.CustomerNumber);

				mail.SetSubject (String.Format ("RE: Job accepted -- CN# {0} {1} {2}, job ID {3}", c.CustomerNumber, c.FirstName, c.LastName, jobID ));
				mail.SetToRecipients (new string[] { emailSender });
				mail.SetCcRecipients (new string[] { "admin@puratap.com" });
				mail.SetMessageBody (msg, false);

				mail.Finished += delegate(object sender, MFComposeResultEventArgs e) {
					if (e.Result == MFMailComposeResult.Sent)
					{
						var sendSuccess = new UIAlertView("", "Mail sent", null, "OK");
						sendSuccess.Show();				
					} else if (e.Result == MFMailComposeResult.Failed) {
							var sendFailed = new UIAlertView("Error", "Failed to send message: "+e.Error.Description, null, "OK");
							sendFailed.Show();				
					}

					mail.DismissViewController (true, act);				
				};

				_tabs.PresentViewController (mail, true, act);
			}	
		}
	
		public class SplitDelegate : UISplitViewControllerDelegate
	    {		// this defines split controller behavior in case of device orientation changes
	        public override void WillHideViewController (UISplitViewController svc, UIViewController aViewController, UIBarButtonItem barButtonItem, UIPopoverController pc)
	        {
	            DetailedTabs dvc = svc.ViewControllers[1] as DetailedTabs;
	            
	            if (dvc != null) {
					// defines the new frame for right side view controller which should now take up the entire screen space
					dvc.View.Frame = new CoreGraphics.CGRect(0,0, 768, 1004);
					
					// adds a button which will show the left side table with customers data when tapped
	                dvc.AddLeftNavBarButton (barButtonItem);
					
					// defines a navigation frame for the navigation bar
					dvc.MyNavigationBar.Frame = new CoreGraphics.CGRect(0, 0, dvc.View.Bounds.Width,44);
					
					// set up the popover view controller ( JobRunTable )
	                dvc.Popover = pc;
	            }
	        }
	
	        public override void WillShowViewController (UISplitViewController svc, UIViewController aViewController, UIBarButtonItem button)
	        {
	            DetailedTabs dvc = svc.ViewControllers[1] as DetailedTabs;
	            
	            if (dvc != null) {
					// if we can get the detailed tabs controller, undo all the changes made when orientation changed last time
					dvc.View.Frame = new CoreGraphics.CGRect(0,0, 703, 748);		// set the frame to old size
	                dvc.RemoveLeftNavBarButton ();															// remove navigation bar button
					// dvc.NavigationBar.Frame = new System.Drawing.RectangleF(0, 0, dvc.View.Bounds.Width, 44);
	                dvc.Popover = null;																			// remove popover controller link
	            }
			}
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

	public class LocManager : CoreLocation.CLLocationManager
	{
		public AppDelegate thisApp;
		public LocManager(AppDelegate app) : base ()
		{
			thisApp = app;
			this.DesiredAccuracy = CLLocation.AccurracyBestForNavigation;
			// this.DistanceFilter = 10;
		}
	}

	public class LocDelegate : CoreLocation.CLLocationManagerDelegate
	{
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
			thisManager = man;
			LastCoordsRecordedTimeStamp = DateTime.Now.AddMinutes(-2);
		}

//		[Obsolete]
//		public override void UpdatedLocation (CLLocationManager manager, CLLocation newLocation, CLLocation oldLocation)
//		{
//			// if one of the coordinates has changed, the address has to be reset
//			if (thisDeviceLat != newLocation.Coordinate.Latitude || thisDeviceLng != newLocation.Coordinate.Longitude)
//			{
//				Interlocked.Exchange<string>(ref thisDeviceAddress, string.Empty);
//			}
//
//			thisDeviceLat = newLocation.Coordinate.Latitude;
//			thisDeviceLng = newLocation.Coordinate.Longitude;
//
//			if ( (DateTime.Now - LastCoordsRecordedTimeStamp).TotalMinutes >= 1 )
//			{
//				// string time = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
//				// string time = (DateTime.SpecifyKind (newLocation.Timestamp, DateTimeKind.Unspecified)).ToString("yyyy-MM-dd HH:mm:ss");
//				string anotherTime = (new DateTime(2001,1,1,0,0,0)).AddSeconds(newLocation.Timestamp.SecondsSinceReferenceDate).ToLocalTime().ToString ("yyyy-MM-dd HH:mm:ss");
//				AddLocationToBuffer (thisDeviceLng, thisDeviceLat, anotherTime, thisDeviceAddress, -1, -1);
//				LastCoordsRecordedTimeStamp = DateTime.Now;
//
//			}
//		}

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

			// saving no more often than once per minute
			if ( (DateTime.Now - LastCoordsRecordedTimeStamp).TotalMinutes >= 1 )
			{
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
			
			// MonoTouch.TestFlight.TestFlight.PassCheckpoint (checkPointMessage);
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