using MonoTouch.UIKit;
using System.Drawing;
using System.Threading;
using System;
using System.IO;
using System.Collections.Generic;
using MonoTouch.Foundation;
using Mono.Data.Sqlite;

namespace Puratap
{
	public partial class ServerClientViewController : UIViewController
	{
		public DetailedTabs _tabs { get; set; }
		public static ManualResetEvent _dataExchangeEvent = new ManualResetEvent(false);		// this event notifies the main application thread that data exchange with the Windows server has been completed
		
		ClientServerDataExchange csde;
		NSNetServiceBrowser _netBrowser;
		public List<NSNetService> _serviceList;
		bool netBrowserStarted = false;
		
		public ServerClientViewController (DetailedTabs tabs) : base ("ServerClient", null)
		{
			this.Title = NSBundle.MainBundle.LocalizedString ("Server/Client", "Server/Client");
			using(var image = UIImage.FromBundle ("Images/174-imac") ) this.TabBarItem.Image = image;
			this._tabs = tabs;
		}
		
		public void Log(string text) {
			this.InvokeOnMainThread (delegate {
				// Console.WriteLine(DateTime.Now.ToString ("HH:mm:ss")+": "+text);
				if (tvLog != null)
				{
					tvLog.Text = tvLog.Text + DateTime.Now.ToString ("HH:mm:ss")+": "+text+"\n";
					if (tvLog.Text.Length > 10000) tvLog.Text = tvLog.Text.Substring (tvLog.Text.Length - 10000);
					if (_tabs.SelectedViewController is ServerClientNavigatonController)
						tvLog.ScrollRangeToVisible (new NSRange(tvLog.Text.Length, 0) );				
				}
			});
		}
		
		internal void StartNetBrowser() 
		{
			if (! netBrowserStarted) 
			{
				_serviceList = new List<NSNetService> ();
				_netBrowser = new NSNetServiceBrowser ();

				_netBrowser.SearchStarted += delegate(object sender, EventArgs e) {
					Log(String.Format("netBrowser: Search started..."));
				};

				_netBrowser.SearchStopped += delegate(object sender, EventArgs e) {
					Log(String.Format("netBrowser: Search stopped..."));
				};

				_netBrowser.NotSearched += delegate(object sender, NSNetServiceErrorEventArgs e) {
					Log(String.Format("netBrowser: \"Did not search\" event fired. Attempting to start the search again..."));
					_netBrowser.SearchForServices("_ipadService._tcp", "");
				};

				_netBrowser.FoundService += delegate (object sender, NSNetServiceEventArgs e) 
				{
					if (_serviceList.Count == 0)		// only add a data exchange starter to the first service found
					{
						_serviceList.Add(e.Service);
						e.Service.AddressResolved += ServiceAddressResolved;
						Log ("ServiceFound event has been fired: Service added : " + e.Service.Name);
					}
					SetDataExchangeButtonEnabled ();
				};

				_netBrowser.ServiceRemoved += delegate(object sender, NSNetServiceEventArgs e) 
				{
					// var nsService = _serviceList.FindAll(s => s.Name.Equals (e.Service.Name));
					_serviceList.RemoveAll(s => s.Name.Equals (e.Service.Name));
					Log ("_netBrowser : ServiceRemoved event has been fired : Service removed : "+e.Service.Name);

					SetDataExchangeButtonDisabled ();
				};

				_netBrowser.Schedule (MonoTouch.Foundation.NSRunLoop.Current, "NSDefaultRunLoopMode");
				_netBrowser.SearchForServices("_ipadService._tcp", "");	// this service is created by the Application Server running on Windows machine (IMPLEMENTED :: rewritten the app server to be a console application)
				SetDataExchangeButtonDisabled ();
				netBrowserStarted = true;
				Log ("ServerClientViewController.ViewDidAppear : _netBrowser has been started.");
			}
			else {  }
		}
		
		void ServiceAddressResolved (object sender, EventArgs e) {
			Log ("ServiceAddressResolved event has been fired");
			NSNetService ns = sender as NSNetService;
			if (ns != null) 
			{
				Log (String.Format ( "Service has been resolved: {0}.", ns.Name ));
				ExchangeFilesAndUpdateJobTableView (ns);
			}
		}
		
		public void SetExchangeActivityHidden()
		{
			InvokeOnMainThread (delegate() {
				aivActivity.Hidden = true;
				aivActivity.StopAnimating ();
			});
		}
		
		void ExchangeFilesAndUpdateJobTableView(NSNetService ns)
		{
			csde = new ClientServerDataExchange(this);

			try { 
				int result = csde.CallServer (ns); // this starts another thread for data exchange
				if (result == 0)
				{
					SetExchangeActivityHidden ();
					_serviceList.Clear ();
					StopNetBrowser ();
					StartNetBrowser ();
				}
			}
			catch (Exception e) 
			{ 
				Log (String.Format ("ExchangeFilesAndUpdateJobTableView : Exception : ", e.Message));
				SetExchangeActivityHidden ();
				_serviceList.Clear ();
				StopNetBrowser ();
				StartNetBrowser ();
			}

			// ClientServerDataExchange.fileExchangeDone.WaitOne (); // wait until file exchange with the server has been completed
			// _tabs._jobRunTable._ds.GetCustomersFromDB();
			// _tabs._jobRunTable.TableView.ReloadData();			
			
		}
		
		public void SetDataExchangeButtonEnabled()
		{
			InvokeOnMainThread ( delegate() {
				UIView.SetAnimationDuration (0.3f);
				UIView.BeginAnimations (null);
				this.btnDownload.Enabled = true;
				btnDownload.SetTitle ("Submit data", UIControlState.Normal);
				btnDownload.SetTitleColor(btnChangeDate.TitleColor (UIControlState.Normal), UIControlState.Normal);
				btnDownload.Frame = new RectangleF( btnDownload.Frame.X, btnDownload.Frame.Y, 160, btnDownload.Frame.Height);
				aivConnectingToService.StopAnimating ();

				btnSubmitFeedback.Frame = new RectangleF( btnSubmitFeedback.Frame.X, btnSubmitFeedback.Frame.Y, 160, btnSubmitFeedback.Frame.Height);
				btnResetDeviceID.Frame = new RectangleF( btnResetDeviceID.Frame.X, btnResetDeviceID.Frame.Y, 160, btnResetDeviceID.Frame.Height);

				UIView.CommitAnimations ();
			});
		}

		public void SetDataExchangeButtonDisabled()
		{
			InvokeOnMainThread ( delegate() {
				UIView.SetAnimationDuration (0.3f);
				UIView.BeginAnimations (null);
				this.btnDownload.Enabled = false;
				btnDownload.SetTitle ("Connecting to data service...", UIControlState.Normal);
				btnDownload.SetTitleColor (UIColor.Gray, UIControlState.Normal);
				btnDownload.Frame = new RectangleF( btnDownload.Frame.X, btnDownload.Frame.Y, 270, btnDownload.Frame.Height);
				aivConnectingToService.StartAnimating ();

				btnSubmitFeedback.Frame = new RectangleF( btnSubmitFeedback.Frame.X, btnSubmitFeedback.Frame.Y, 270, btnSubmitFeedback.Frame.Height);
				btnResetDeviceID.Frame = new RectangleF( btnResetDeviceID.Frame.X, btnResetDeviceID.Frame.Y, 270, btnResetDeviceID.Frame.Height);

				UIView.CommitAnimations ();
			}); 
		}
		
		public void StopNetBrowser()
		{
			_serviceList.Clear ();
			_netBrowser.Stop ();
			netBrowserStarted = false;
		}
		
		public static string dbFilePath 
		{ 
			get {
				string result = MyConstants.DBReceivedFromServer;
				if (string.IsNullOrEmpty (result) || (! File.Exists (result) ))
				{
					result = Path.Combine (Environment.GetFolderPath(Environment.SpecialFolder.Personal), "NEWTESTDB.sqlite");
					if (! File.Exists (result))		// if database file does not exist, this could be a new version of the app installed (thus changing the bundle's directory name), we should attempt to find the file
					{
						// save just the filename to the result string
						result = result.Substring (result.LastIndexOf ('/')+1);
						// if we can locate the file in the current bundle's "Documents" folder, we will return that
						if (File.Exists ( Path.Combine (Environment.CurrentDirectory.Substring (0,Environment.CurrentDirectory.LastIndexOf ('/')), "Documents/"+result)))
							result = Path.Combine (Environment.CurrentDirectory.Substring (0,Environment.CurrentDirectory.LastIndexOf ('/')), "Documents/"+result);
						else result = "";
					}
				}
				// result = Path.Combine (Environment.GetFolderPath(Environment.SpecialFolder.Personal), "NEWTESTDB.sqlite"); // debug
				return result;
			} 
			// no setter, this is a read-only property
		}
		public static string GetDBDirectoryPath()
		{
			return Environment.GetFolderPath (Environment.SpecialFolder.Personal);
			// return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Personal), "NEWTESTDB.sqlite" /* WAS :: "TodaysJobDB.sqlite" */);
		}
		
		public override void DidReceiveMemoryWarning ()
		{
			// Releases the view if it doesn't have a superview.
			base.DidReceiveMemoryWarning ();
			
			// Release any cached data, images, etc that aren't in use.
		}
		
		public override void ViewDidAppear (bool animated)
		{
			base.ViewDidAppear (animated);
			this.NavigationController.SetToolbarHidden (true,true);
			StartNetBrowser();

			_tabs.SetNavigationButtons (NavigationButtonsMode.ServerClient);
			tvLog.ScrollRangeToVisible (new NSRange(tvLog.Text.Length, 0) );			
		}
		
		public override void ViewDidDisappear (bool animated)
		{
			base.ViewDidDisappear (animated);
			if (this.NavigationController.ViewControllers.Length < 2) {
				StopNetBrowser ();
				SetDataExchangeButtonDisabled ();
				Log ("ServerClientViewController.ViewDidDisappear : _netBrowser has been stopped.");
			}
		}
		
		public override void ViewDidLoad ()
		{
			base.ViewDidLoad ();
			
			//any additional setup after loading the view, typically from a nib.
			
			// csde = new ClientServerDataExchange(this);  // DEBUG :: starts browsing for Bonjour services
			btnDownload.TouchUpInside += HandleBtnDownloadTouchUpInside;
			btnUpload.TouchUpInside += HandleBtnShowInfoTouchUpInside;
			btnResetDeviceID.TouchUpInside += HandleBtnResetDeviceIDTouchUpInside;
			btnChangeDate.TouchUpInside += HandleBtnChangeDateTouchUpInside;
			btnSubmitFeedback.TouchUpInside += HandleBtnSubmitFeedbackTouchUpInside;
		}

		void HandleBtnSubmitFeedbackTouchUpInside(object sender, EventArgs e)
		{
			MonoTouch.TestFlight.TestFlight.OpenFeedbackView ();
		}

		void HandleBtnChangeDateTouchUpInside (object sender, EventArgs e)
		{
			_tabs._jobRunTable._ds.LoadJobRun (false);
		}

		void HandleBtnResetDeviceIDTouchUpInside (object sender, EventArgs e)
		{
			UIAlertView resetAlert = new UIAlertView("Warning", "Resetting Device ID is permanent and irreversible. Are you sure you want to do this?", null, "Yes", "No");
			// note that the "Cancel" button says "Yes" and the "default" (highlighted) button is "No", trying to tell the user again that resetting is not to be done lightly
			
			resetAlert.Dismissed += HandleResetAlertDismissed;
			resetAlert.Show ();
		}

		void HandleResetAlertDismissed (object sender, UIButtonEventArgs e)
		{
			if (e.ButtonIndex != 1) // if user pressed the "Cancel" button that has index=0
			{
				Log ("Resetting Device ID in UserDefaults database.");
				try { MyConstants.DeviceID = ""; }
				catch (Exception ex) {
					Log (String.Format ("Device ID Reset: Exception: {0}", ex.Message));
				}
				Log ("Device ID has been reset.");
			}
		}

		void HandleBtnShowInfoTouchUpInside (object sender, EventArgs e)
		{
			Log (String.Format ("App version: {0}", NSBundle.MainBundle.ObjectForInfoDictionary("CFBundleVersion").ToString()));
			Log (String.Format ("Device ID: {0}", MyConstants.DeviceID));
			Log (String.Format ("Employee ID: {0}", MyConstants.EmployeeID));
			Log (String.Format ("Employee Type: {0}", MyConstants.EmployeeType));
			Log (String.Format ("Employee Name: {0}", MyConstants.EmployeeName));
			Log (String.Format ("Current run date: {0}", MyConstants.DEBUG_TODAY));

			if (string.IsNullOrEmpty (MyConstants.DBReceivedFromServer) || (! File.Exists (MyConstants.DBReceivedFromServer)))
			{
				Log (String.Format ("Last DB received from server was {0}, but it was not found.", MyConstants.DBReceivedFromServer));
				Log (String.Format ("Current working database: {0}",  dbFilePath));
			}
			else
			{
				Log (String.Format ("Current working database: {0}",  MyConstants.DBReceivedFromServer));
			}
		}

		void HandleBtnDownloadTouchUpInside (object sender, EventArgs e)
		{
			// InitDataExchange ();
			this._tabs._app.myLocationManager.StopUpdatingLocation ();
			this._tabs._app.myLocationManager.StopMonitoringSignificantLocationChanges ();

			Log ("Generating daily payments summary files...");

			bool summariesGenerated = false;
			try {
				// this should generate summaries for all runs in the database, not just the current run
				// this._tabs._paySummaryView.SummaryMode = SummaryModes.Money;
				// this._tabs._paySummaryView.ViewDidAppear (false); 								// this fills the SummaryData with the current state of things
				if (this._tabs._paySummaryView.GenerateAllSummaryFiles ()) // .GenerateDailySummaryFiles ();
				{
					Log ("Summary files generated.");
					summariesGenerated = true;
				}
				else {
					Log ("Error : An exception was raised while generating summary files!");
					summariesGenerated = false;
				}

			}
			catch (Exception exc) {
				this._tabs._scView.Log (String.Format ("Daily Summary Failed To Generate: {0}, stack trace: {1}", exc.Message, exc.StackTrace));
			}

			try
			{
				this._tabs._jobRunTable.TableView.ReloadData ();
			} catch (Exception ex) 
			{
				this._tabs._scView.Log (String.Format ("Failed to reload JobRunTable data: {0}, stack trace: {1}", ex.Message, ex.StackTrace));
			}

			try
			{
				this._tabs._app.myLocationDelegate.DumpLocationsBufferToDatabase ();
			}
			catch (Exception exc) {
				this._tabs._scView.Log (String.Format ("Failed to write iPad locations buffer to database: {0}, stack trace: {1}", exc.Message, exc.StackTrace));
			}

			if (summariesGenerated)
			{
				this._tabs.MyNavigationBar.Hidden = true;
				this.NavigationController.NavigationBarHidden = false;

				bool DataInputCompletedForRun = this._tabs._jobRunTable.AllJobsDone;

				if (! DataInputCompletedForRun)
				{
					var alert = new UIAlertView("Warning", "Data has not been input for some of the jobs.\nStart data exchange anyway?", null, "No", "Yes");
					alert.Dismissed += delegate(object _sender, UIButtonEventArgs ev) {
						if (ev.ButtonIndex != alert.CancelButtonIndex)
						{
							if ( ! SignDailyStockUsed.IsEmpty ())
								this.NavigationController.PushViewController (new SignDailyStockUsed(this._tabs), true);
							else
								InitDataExchange ();
						}
					};

					alert.Show ();
				}
				else 
				{
					if ( ! SignDailyStockUsed.IsEmpty ())
						this.NavigationController.PushViewController (new SignDailyStockUsed(this._tabs), true);
					else
						InitDataExchange ();
				}
			}
			else
			{
				// alert the user that something bad happened, but still allow to start data transfer
				var alert = new UIAlertView("Warning", "An exception occurred when generating daily summary files.\nStart data exchange anyway?", null, "No", "Yes");
				alert.Dismissed += delegate(object _sender, UIButtonEventArgs ev) {
					if (ev.ButtonIndex != alert.CancelButtonIndex)
					{
						InitDataExchange ();
					}
				};
				alert.Show ();
			}
		}

		/*public void HandleServiceResolutionFailed(object sender, NSNetServiceErrorEventArgs e)
		{

		}*/

		public void InitDataExchange()
		{
			// TestFlightSdk.TestFlight.PassCheckpoint (String.Format ("DataExchangeInitiated : {0} {1}", MyConstants.EmployeeID, MyConstants.EmployeeName));
			InvokeOnMainThread (delegate { 
				this.View.BringSubviewToFront(aivActivity);
				aivActivity.Hidden = false;
				aivActivity.StartAnimating ();
			});

			// check if all jobs for the previous day have been marked
			bool isDone = IsAllDone ();
			isDone = true; // FIXME :: DEBUG :: remove this line, shoud be --- If(isDone) { }

			if (isDone) 
			{
				// _tabs._jobRunTable.Customers.Clear ();
				if (_serviceList.Count == 0) 
				{
					var alert = new UIAlertView("iPad Bonjour service not found on the network", "Make sure your iPad is connected to Puratap's WiFi network (SSID: Wireless-Ptap) and signal strength is acceptable. If it JUST DOESN'T WORK, call for help!", null, "OK");
					InvokeOnMainThread (delegate { 
						alert.Show ();
						SetExchangeActivityHidden ();
					});
				}
				else 
				{
					foreach (NSNetService ns in _serviceList)
					{
						if (ns.HostName == null)
						{
							btnDownload.Enabled = false;
							ns.ResolveFailure += delegate(object sender, NSNetServiceErrorEventArgs e) {
								Log (String.Format ("Service resolution failed. Error messages below."));
								for (int i = 0; i<e.Errors.Count; i++)
								{
									Log (String.Format ("{0}: {1}", e.Errors.Keys[i].ToString (), e.Errors.Values[i].ToString ()));
								}

								InvokeOnMainThread (delegate {
									SetExchangeActivityHidden ();
									var resolveFailureAlert = new UIAlertView("Error", "Data exchange with the server failed because the data service could not be resolved.\n" +
									                                          "Please check wireless network connectivity and try again (switch to another tab and back)", null, "OK");
									resolveFailureAlert.Show ();

									//_netBrowser.SearchForServices("_ipadService._tcp", "");
									// _serviceList.Clear ();
									// SetDataExchangeButtonDisabled();

									this.btnDownload.Enabled = false;
									btnDownload.SetTitle ("Connecting to data service...", UIControlState.Normal);
									btnDownload.SetTitleColor (UIColor.Gray, UIControlState.Normal);
									btnDownload.Frame = new RectangleF( btnDownload.Frame.X, btnDownload.Frame.Y, 270, btnDownload.Frame.Height);
									aivConnectingToService.StartAnimating ();
								});
							};
							ns.Resolve(60);		// data exhange with server will be handled inside ns.Resolve() call
							Log ("DataService.Resolve has been called.");
						}
						else 
						{
							Log(ns.Name+" : Hostname = "+ns.HostName+" : Service resolution skipped.");
							btnDownload.Enabled = false;
							// data exhange with server here
							ExchangeFilesAndUpdateJobTableView (ns);
						}
					}
				}
				// we cannot attempt to resolve the service and then immidiately update the view
				// it will take some time to get the data, therefore the main thread must wait for a notification that the data exhange has been completed
				// the data exchange happens on a separate thread, then the main thread is notified by an instance of ManualResetEvent object
				
				// The two lines below were used when debugging
				// What these do should actually happen after data exchange, not here
				// _tabs._jobRunTable._ds.GetCustomersFromDB();
				// _tabs._jobRunTable.TableView.ReloadData();
			}
			else 
			{
				var notAllJobsMarked = new UIAlertView("Cannot upload incomplete data to server", "Please input data for every job in the jobs list.", null, "OK");
				notAllJobsMarked.Show ();
				SetExchangeActivityHidden ();
			}
		}
		
		public bool IsAllDone()
		{
			bool result = true;
			
			if (_tabs._jobRunTable.MainJobList != null)
			foreach (Job j in _tabs._jobRunTable.MainJobList)
			{ if (j.JobDone == false) result = false; }
			
			if (_tabs._jobRunTable.UserCreatedJobs != null)
			{
				foreach (Job j in _tabs._jobRunTable.UserCreatedJobs)
				{ if (j.JobDone == false) result = false; }
			}
			return result; // DEBUG :: return true;
		}

		[Obsolete]
		public override void ViewDidUnload ()
		{
			// Release any retained subviews of the main view.
			ReleaseDesignerOutlets ();
			// base.ViewDidUnload ();			
		}

		[Obsolete]
		public override bool ShouldAutorotateToInterfaceOrientation (UIInterfaceOrientation toInterfaceOrientation)
		{
			// Return true for supported orientations
			return (toInterfaceOrientation == UIInterfaceOrientation.LandscapeLeft || toInterfaceOrientation == UIInterfaceOrientation.LandscapeRight);
		}
	}
}

