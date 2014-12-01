using System;
using System.IO;
using System.Net;
using System.Drawing;
using System.Threading;
using System.Collections.Generic;
using MonoTouch.UIKit;
using Mono.Data.Sqlite;
using MonoTouch.Foundation;

namespace Puratap
{
	public partial class ServerClientViewController : UIViewController
	{
		public DetailedTabs _tabs { get; set; }
		public static ManualResetEvent _dataExchangeEvent = new ManualResetEvent(false);	// this event notifies the main application thread that data exchange with the Windows server has been completed

		// new implementation of data exchange does away with Bonjour and uses settings (NSUserDefaults.StandardUserDefaults) to determine server's IP address and port
		private Reachability _reachLocalServer;
		public ReachabilityStatus CurrentReachabilityStatusLocalServer { get { return _reachLocalServer.CurrentStatus; } }

		private Reachability _reachFTPServer;
		public ReachabilityStatus CurrentReachabilityStatusFTPServer { get { return _reachFTPServer.CurrentStatus; } }

		public string PuratapServerIP { get; set; }
		public int PuratapServerPort { get; set; }

		ClientServerDataExchange csde;

		
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
		
		public void SetExchangeActivityHidden()
		{
			InvokeOnMainThread (delegate() {
				aivActivity.Hidden = true;
				aivActivity.StopAnimating ();
			});
		}
		
		public void SetDataExchangeButtonEnabled()
		{
			InvokeOnMainThread ( delegate() {
				UIView.SetAnimationDuration (0.3f);
				UIView.BeginAnimations (null);

				aivConnectingToService.StopAnimating ();
				btnResetDeviceID.Frame = new RectangleF( btnResetDeviceID.Frame.X, btnResetDeviceID.Frame.Y, 160, btnResetDeviceID.Frame.Height);
				btnStartDataExchange.Frame = new RectangleF( btnStartDataExchange.Frame.X, btnStartDataExchange.Frame.Y, 160, btnStartDataExchange.Frame.Height);
				btnStartDataExchange.Enabled = true;
				btnStartDataExchange.SetTitle("Send/receive data", UIControlState.Normal);
				btnStartDataExchange.SetTitleColor(btnChangeDate.TitleColor (UIControlState.Normal), UIControlState.Normal);

				UIView.CommitAnimations ();
			});
		}

		public void SetDataExchangeButtonDisabled()
		{
			InvokeOnMainThread ( delegate() {
				UIView.SetAnimationDuration (0.3f);
				UIView.BeginAnimations (null);

				aivConnectingToService.StartAnimating ();
				btnStartDataExchange.Frame = new RectangleF( btnStartDataExchange.Frame.X, btnStartDataExchange.Frame.Y, 270, btnStartDataExchange.Frame.Height);
				btnResetDeviceID.Frame = new RectangleF( btnResetDeviceID.Frame.X, btnResetDeviceID.Frame.Y, 270, btnResetDeviceID.Frame.Height);

				UIView.CommitAnimations ();
			}); 
		}
		
		public static string dbFilePath 
		{ 
			get {
				string result = MyConstants.DBReceivedFromServer;
				if (string.IsNullOrEmpty (result) || (! File.Exists (Path.Combine (Environment.GetFolderPath(Environment.SpecialFolder.Personal), result)) ))
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

			_tabs.SetNavigationButtons (NavigationButtonsMode.ServerClient);
			tvLog.ScrollRangeToVisible (new NSRange(tvLog.Text.Length, 0) );	

			UpdateLANDataExchangeButtonText (_reachLocalServer.CurrentStatus);		
		}
		
		public override void ViewDidDisappear (bool animated)
		{
			base.ViewDidDisappear (animated);
		}
		
		public override void ViewDidLoad ()
		{
			base.ViewDidLoad ();
			
			//any additional setup after loading the view, typically from a nib.

			btnUpload.TouchUpInside += HandleBtnShowInfoTouchUpInside;
			btnResetDeviceID.TouchUpInside += HandleBtnResetDeviceIDTouchUpInside;
			btnChangeDate.TouchUpInside += HandleBtnChangeDateTouchUpInside;
			btnStartDataExchange.TouchUpInside += HandleBtnStartLANDataExchangeTouchUpInside;
			btnFTPDataExchange.TouchUpInside += HandleBtnStartFTPDataExchangeTouchUpInside;

			var settings = NSUserDefaults.StandardUserDefaults;
			settings.Init ();
			if (string.IsNullOrEmpty (settings.StringForKey ("PuratapServerIP")))
				settings.SetString (MyConstants.DEFAULT_IPAD_SERVER_IP, "PuratapServerIP");
			if (string.IsNullOrEmpty (settings.StringForKey ("PuratapServerPort")))
				settings.SetString (MyConstants.DEFAULT_IPAD_SERVER_PORT, "PuratapServerPort");
			settings.Synchronize ();

			_reachFTPServer = new Reachability ("puratap.com.au");
			_reachFTPServer.ReachabilityUpdated += HandleFTPReachabilityUpdated;

			_reachLocalServer = new Reachability ( settings.StringForKey ("PuratapServerIP") );
			_reachLocalServer.ReachabilityUpdated += HandleLocalReachabilityUpdated;
		}

		protected virtual void HandleLocalReachabilityUpdated(object sender, ReachabilityEventArgs e)
		{
			UpdateLANDataExchangeButtonText (e.Status);
		}

		protected virtual void HandleFTPReachabilityUpdated(object sender, ReachabilityEventArgs e)
		{
			Log ("Local ReachabilityUpdated event fired.");
			Log (String.Format("Updated LAN connectivity status: {0}", e.Status.ToString()));
			UpdateFTPDataExchangeButtonText (e.Status);
		}

		protected virtual void UpdateFTPDataExchangeButtonText( ReachabilityStatus status)
		{
			Log ("FTP Reachability event fired.\nFTP Reachability status: " + status);
			switch (status) {
			case ReachabilityStatus.NotReachable:
					btnFTPDataExchange.SetTitle ("FTP: Server unreachable", UIControlState.Normal);
					btnFTPDataExchange.SetTitleColor (UIColor.Gray, UIControlState.Normal);
					btnFTPDataExchange.Enabled = false;
					break;
				case ReachabilityStatus.ViaWiFi:
					btnFTPDataExchange.SetTitle ("FTP: Send/receive data", UIControlState.Normal);
					btnFTPDataExchange.SetTitleColor (btnChangeDate.TitleColor (UIControlState.Normal), UIControlState.Normal);
					btnFTPDataExchange.Enabled = true;
					break;
				case ReachabilityStatus.ViaWWAN: 
					btnFTPDataExchange.SetTitle ("FTP: Send/receive data", UIControlState.Normal);
					btnFTPDataExchange.SetTitleColor (btnChangeDate.TitleColor (UIControlState.Normal), UIControlState.Normal);
					btnFTPDataExchange.Enabled = true;
					break;
			}
		}

		protected virtual void UpdateLANDataExchangeButtonText( ReachabilityStatus status )
		{
			InvokeOnMainThread (delegate() {
				switch (status) {
					case ReachabilityStatus.ViaWiFi:
						btnStartDataExchange.SetTitle("Send/receive data", UIControlState.Normal);
						btnStartDataExchange.SetTitleColor( btnChangeDate.TitleColor (UIControlState.Normal), UIControlState.Normal);
						btnStartDataExchange.Enabled = true;
						aivConnectingToService.Hidden = true;
					break;

					default:
						btnStartDataExchange.SetTitle("Connecting to data service", UIControlState.Normal);
						btnStartDataExchange.Enabled = false;
						aivConnectingToService.Hidden = false;
						aivConnectingToService.StartAnimating();
					break;					
				}
			});
		}

		public void UpdateDataExchangeButtonWithCurrentStatus() {
			UpdateLANDataExchangeButtonText (CurrentReachabilityStatusLocalServer);
		}

		private void SetDataExchangeInProgress() {
			InvokeOnMainThread (delegate() {
				// data exchange button
				btnStartDataExchange.SetTitle ("In progress...", UIControlState.Normal);
				btnStartDataExchange.Enabled = false;
				// activity indicator
				this.View.BringSubviewToFront(aivActivity);
				aivActivity.Hidden = false;
				aivActivity.StartAnimating ();
			});
		}

		private string GetDatabaseFileName() {
			string employeeType = (MyConstants.EmployeeType == MyConstants.EmployeeTypes.Plumber) ? "PLU" : "FRA";
			string result = String.Format ("{0} {1} {2} {3}.sqlite.gz", employeeType, MyConstants.EmployeeID,
											MyConstants.EmployeeName, DateTime.Now.Date.ToString("yyyy-MM-dd"));
			return result;
		}

		void HandleBtnStartFTPDataExchangeTouchUpInside (object sender, EventArgs e)
		{
			bool uploadSuccess = UploadDataToFTP ();

			if (uploadSuccess == true) {
				string fileName = GetDatabaseFileName ();
				string fullPath = "ftp://puratap.com.au" + "/IPAD%20DATA/DATA.OUT/" + fileName.Replace (" ", "%20");
				DateTime FTPExchangeStarted = DateTime.Now;
				GetDataFileFromFTP (fileName, fullPath);

				TimeSpan ts = DateTime.Now - FTPExchangeStarted;
				Log (String.Format ("FTP data exchange took {0:F} seconds to complete.", ts.TotalSeconds));
			}
		}

		bool UploadDataToFTP () 
		{
			bool result = true;
			try {
				// push stock signing if necessary

				// get a list of files to be sent

				// gzip each file

				// upload each file

			} catch (Exception e) {
				Log (String.Format ("Exception during FTP data upload: \n{0} \n{1}", e.Message, e.StackTrace));
				result = false;
			}
			return result;
		}

		bool GetDataFileFromFTP (string fileName, string fullPath) 
		{

			const string FTPUserName = "dmitry@puratap.com.au";
			const string FTPPassword = "D4770AMVB0";
			FtpWebRequest request = (FtpWebRequest)WebRequest.Create (fullPath);
			request.Credentials = new NetworkCredential (FTPUserName, FTPPassword);
			request.Method = WebRequestMethods.Ftp.DownloadFile;

			FtpWebResponse response = null;
			bool downloadResult = false;
			try {
				Stream responseStream = null;
				BinaryReader bnrResponseStreamReader;

				response = (FtpWebResponse)request.GetResponse();
				responseStream = response.GetResponseStream();

				if (responseStream != null) {
					bnrResponseStreamReader = new BinaryReader(responseStream);
					try {
						string savePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Personal), fileName);
						using (FileStream fs = new FileStream(savePath, FileMode.Create, FileAccess.ReadWrite)) {
							BinaryWriter bnrResponseStreamWriter = new BinaryWriter(fs);
							using (var ms = new MemoryStream()) {
								byte[] buffer = new byte[4096];
								int byteCount;
								while ((byteCount = bnrResponseStreamReader.Read(buffer, 0, buffer.Length)) != 0) {								
									ms.Write(buffer, 0, byteCount);
								}
								bnrResponseStreamWriter.Write(ms.ToArray());
								bnrResponseStreamWriter.Flush();
								bnrResponseStreamWriter.Close();
							}
						}

						bool decompressResult;
						FileInfo dfi = new FileInfo(savePath);
						decompressResult = DecompressDataFile(dfi);
							
						downloadResult = true && decompressResult;
						if (downloadResult == true) {
							string decompressedFilePath = savePath.Remove(savePath.Length - 3);
							MyConstants.DBReceivedFromServer = decompressedFilePath;
							MyConstants.LastDataExchangeTime = DateTime.Now.ToString ("yyyy-MM-dd HH:mm:ss");
						}

					} catch (Exception ex) {
						Console.WriteLine(String.Format("{0}\r\n{1}", ex.Message, ex.StackTrace));
					} finally {
						if (bnrResponseStreamReader != null)
							bnrResponseStreamReader.Close();
					}
				}
			} catch (Exception e) {
				downloadResult = false;
				Log (e.Message);
			} finally {
				if (response != null)
					response.Close ();
			}
			return downloadResult;
		}

		public bool DecompressDataFile(FileInfo dfi)
		{
			bool result;
			try {
				// Get the stream of the source file.
				using (FileStream inFile = dfi.OpenRead())
				{
					// Get original file extension, by removing ".gz" from Data.sqlite.gz
					string curFile = dfi.FullName;
					string origName = curFile.Remove(curFile.Length - dfi.Extension.Length);

					//Create the decompressed file.
					using (FileStream outFile = File.Create(origName))
					{
						using (System.IO.Compression.GZipStream Decompress = new System.IO.Compression.GZipStream(inFile,
							System.IO.Compression.CompressionMode.Decompress))
						{
							byte[] tmp = new byte[4];
							inFile.Read(tmp, 0, 4);
							inFile.Seek(0, SeekOrigin.Begin);
							// Copy the decompression stream into the output file. 
							Decompress.CopyTo(outFile);
							result = true;
							Console.WriteLine("Decompressed: {0}", dfi.Name);

						}
					}
				}
			} catch (Exception e) {
				Console.WriteLine (String.Format("Exception: {0}\n{1}", e.Message, e.StackTrace));
				result = false;
			}
		
			return result;
		}

		void HandleBtnStartLANDataExchangeTouchUpInside (object sender, EventArgs e)
		{
			var settings = NSUserDefaults.StandardUserDefaults;

			Log (String.Format("Puratap iPad Server IP = {0}", settings.StringForKey ("PuratapServerIP")));
			Log (String.Format("Puratap iPad Server port = {0}", settings.StringForKey ("PuratapServerPort")));
			Log (String.Format ("Connectivity status: {0}", _reachLocalServer.CurrentStatus.ToString ()));

			PuratapServerIP = settings.StringForKey ("PuratapServerIP");
			int serverPort = 0;

			if (!int.TryParse (settings.StringForKey ("PuratapServerPort"), out serverPort)) {
				Log (String.Format ("Invalid port in application settings: {0}. Trying default port: {1}.", settings.StringForKey ("PuratapServerPort"), MyConstants.DEFAULT_IPAD_SERVER_PORT));
				PuratapServerPort = int.Parse (MyConstants.DEFAULT_IPAD_SERVER_PORT);
			} else
				PuratapServerPort = serverPort;

			this._tabs._app.myLocationManager.StopUpdatingLocation ();
			this._tabs._app.myLocationManager.StopMonitoringSignificantLocationChanges ();		

			// summaries
			try {
				// this generates summaries for all runs in the database, not just the current run
				if (this._tabs._paySummaryView.GenerateAllSummaryFiles ()) {
					Log ("Summary files generated.");
				}
				else {
					Log ("Error : An exception was raised while generating summary files!");
				}
			}
			catch (Exception exc) {
				this._tabs._scView.Log (String.Format ("Daily Summary Failed To Generate: {0}, stack trace: {1}", exc.Message, exc.StackTrace));
			}
			// reload job run table data
			try {
				this._tabs._jobRunTable.TableView.ReloadData ();
			} catch (Exception ex) {
				this._tabs._scView.Log (String.Format ("Failed to reload JobRunTable data: {0}, stack trace: {1}", ex.Message, ex.StackTrace));
			}
			// write locations buffer to database
			try {
				this._tabs._app.myLocationDelegate.DumpLocationsBufferToDatabase ();
			} catch (Exception exc) {
				this._tabs._scView.Log (String.Format ("Failed to write iPad locations buffer to database: {0}, stack trace: {1}", exc.Message, exc.StackTrace));
			}

			// sign if necessary
			bool DataInputCompletedForRun;
			if ( string.IsNullOrEmpty(MyConstants.DBReceivedFromServer) || (!File.Exists(Path.Combine (Environment.GetFolderPath(Environment.SpecialFolder.Personal), MyConstants.DBReceivedFromServer))) ) {
				DataInputCompletedForRun = true; // it is not, but we don't want the warning message to pop up
			}
			else DataInputCompletedForRun = this._tabs._jobRunTable.AllJobsDone;

			if (! DataInputCompletedForRun)
			{
				var alert = new UIAlertView("Warning", "Data has not been input for some of the jobs.\nStart data exchange anyway?", null, "No", "Yes");
				alert.Dismissed += delegate(object _sender, UIButtonEventArgs ev) {
					if (ev.ButtonIndex != alert.CancelButtonIndex)
					{
						PushSignStockOrStartDataExchange();
					}
				};
				alert.Show ();
			}
			else 
			{
				PushSignStockOrStartDataExchange ();
			}
		}

		private void PushSignStockOrStartDataExchange() {
			if (! SignDailyStockUsed.IsEmpty ()) 
				PushSignStockViewController ();
			else
				StartNewDataExchange ();
		}

		private void PushSignStockViewController() {
			this._tabs.MyNavigationBar.Hidden = true;
			this.NavigationController.NavigationBarHidden = false;
			this.NavigationController.PushViewController (new SignDailyStockUsed(this._tabs), true);
		}

		public void StartNewDataExchange() {
			SetDataExchangeInProgress ();
			MonoTouch.TestFlight.TestFlight.PassCheckpoint (String.Format ("DataExchangeInitiated : {0} {1}", MyConstants.EmployeeID, MyConstants.EmployeeName));
			csde = new ClientServerDataExchange (this);
			csde.CallServer (PuratapServerIP, PuratapServerPort);
		}

		void HandleBtnChangeDateTouchUpInside (object sender, EventArgs e) {
			_tabs._jobRunTable._ds.LoadJobRun (2);
		}

		void HandleBtnResetDeviceIDTouchUpInside (object sender, EventArgs e) {
			UIAlertView resetAlert = new UIAlertView("Warning", "Resetting Device ID is permanent and irreversible. Are you sure you want to do this?", null, "Yes", "No");
			// the "default" (highlighted) button is "No", trying to tell the user again that resetting is not to be done lightly
			
			resetAlert.Dismissed += HandleResetAlertDismissed;
			resetAlert.Show ();
		}

		void HandleResetAlertDismissed (object sender, UIButtonEventArgs e) {
			if (e.ButtonIndex != 1) // if user pressed the "Cancel" button that has index=0
			{
				Log ("Resetting Device ID in UserDefaults database.");
				try { 
					// MyConstants.DeviceID = ""; 
					MyConstants.DeviceID = MyConstants.NEW_DEVICE_GUID_STRING; 
				}
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

			if (string.IsNullOrEmpty (MyConstants.DBReceivedFromServer) || 
			    (! File.Exists (Path.Combine (Environment.GetFolderPath(Environment.SpecialFolder.Personal), MyConstants.DBReceivedFromServer))))
			{
				Log (String.Format ("Last DB received from server was '{0}', but it was not found.", MyConstants.DBReceivedFromServer));
				Log (String.Format ("Current working database: '{0}'", Path.GetFileName(dbFilePath)));
			}
			else
			{
				Log (String.Format ("Current working database: '{0}'",  MyConstants.DBReceivedFromServer));
			}
		}
	}
}

