using System;
using System.IO;
using System.IO.Compression;
using System.Net;
using System.Drawing;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;
using MonoTouch.UIKit;
using Mono.Data.Sqlite;
using MonoTouch.Foundation;

using BigTed; // BTProgressHud component

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

			// additional setup after loading the view
			
			ProgressHUD.Shared.RingThickness = 4.0f;
			ProgressHUD.Shared.Ring.Color = UIColor.FromRGB(50, 150, 255);
			ProgressHUD.Shared.Ring.BackgroundColor = UIColor.White;
			ProgressHUD.Shared.SetNeedsLayout ();

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

					// TODO :: uncomment lines below and comment out hiding of FTP data exchange button in the following versions
			// _reachFTPServer = new Reachability ("puratap.com.au");
			// _reachFTPServer.ReachabilityUpdated += HandleFTPReachabilityUpdated;
			btnFTPDataExchange.Hidden = true;

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
			// TODO :: push stock signing if necessary
			ProgressHUD.Shared.Show ("Starting FTP data exchange...", 0, ProgressHUD.MaskType.Gradient, 1000);
			PrepareForDataExchange ();		// generate summaries, stop updating location, write locations buffer to database
			bool DataInputCompletedForRun = CheckDataInputCompletedForRun();

			if (!DataInputCompletedForRun) {
				ProgressHUD.Shared.Dismiss ();
				var confirmStart = new UIAlertView("Warning", "Data has not been input for some of the jobs.\nStart data exchange anyway?", null, "No", "Yes");
				confirmStart.Dismissed += delegate(object _sender, UIButtonEventArgs ev) {
					if (ev.ButtonIndex != confirmStart.CancelButtonIndex) {
						StartFTPDataExchange ();
					} else {
						this._tabs._app.myLocationManager.StartUpdatingLocation ();
						this._tabs._app.myLocationManager.StartMonitoringSignificantLocationChanges ();
					}
				};
				confirmStart.Show ();
			} else {
				StartFTPDataExchange ();
			}
		}

		async void StartFTPDataExchange() {
			DateTime FTPUploadStarted = DateTime.Now;
			bool uploadSuccess = await UploadDataToFTPAsync ();
			if (uploadSuccess == true) {
				DateTime FTPDownloadStarted = DateTime.Now;
				ProgressHUD.Shared.Show ("FTP data exchange in progress (uploading)...", 0, ProgressHUD.MaskType.Gradient);
				bool downloadResult = await DownloadDataFromFTPAsync ();
				if (downloadResult == true) {
					TimeSpan foo = DateTime.Now - FTPUploadStarted;
					TimeSpan bar = DateTime.Now - FTPDownloadStarted;
					Log (String.Format ("Download successful: {0}", GetDatabaseFileName ()));

					ProgressHUD.Shared.ShowContinuousProgress ("Purging old files...", ProgressHUD.MaskType.Gradient);
					PurgeOldFiles ();
					PurgeUnsignedPDFFiles ();

					Log (String.Format ("FTP download took {0:F} seconds to complete.", bar.TotalSeconds));
					Log (String.Format ("FTP data exchange took {0:F} seconds to complete.", foo.TotalSeconds));

					BTProgressHUD.Dismiss ();
					BTProgressHUD.ShowSuccessWithStatus ("Done!", 1000);
					this._tabs._jobRunTable._ds.LoadJobRun (2);
				} else {
					BTProgressHUD.Dismiss ();
					BTProgressHUD.Show (" [:-( ", delegate() { } , "Upload has been successful.\nDownload has failed.", -1, ProgressHUD.MaskType.Gradient);
				}
			} else {
				BTProgressHUD.Dismiss ();
				BTProgressHUD.Show (" [:-( ", delegate() { } , "Upload has failed for one or more files.", -1, ProgressHUD.MaskType.Gradient);
			}		}

		async Task<bool> UploadDataToFTPAsync () {
			return await Task.Run ( () => {
				bool uploadSuccess = UploadDataToFTP ();
				return uploadSuccess;
			} );
		}

		async Task<bool> DownloadDataFromFTPAsync () {
			return await Task.Run ( () => {
				string fileName = GetDatabaseFileName ();
				string fullPath = String.Format ("{0}/{1}/{2}", FTPServerPathDataOut, 
					DateTime.Now.Date.ToString("yyyy-MM-dd"), fileName.Replace (" ", "%20")); 
				bool downloadResult = GetDataFileFromFTP (fileName, fullPath);
				return downloadResult;
			} );
		}

		void CreateDirectoriesOnFtp() {
				for (int i = 0; i < 4; i++) {
					string directory = FTPServerPathDataIn;
					switch (i) {
						case 0: { directory += "SQLite%20DBs/"; break; }
						case 1: { directory += "PDFs/"; break; }
						case 2: { directory += "Photos/"; break; }
						case 3: { directory += "Summaries/"; break; }
					}
					directory += DateTime.Now.Date.ToString ("yyyy-MM-dd");
					FtpWebRequest createDirectoryRequest = (FtpWebRequest)WebRequest.Create (directory);
					createDirectoryRequest.Credentials = new NetworkCredential (FTPUserName, FTPPassword);
					createDirectoryRequest.Method = WebRequestMethods.Ftp.MakeDirectory;
					createDirectoryRequest.UseBinary = true;

					try {
						using (var response = (FtpWebResponse)createDirectoryRequest.GetResponse ()) {
							Console.WriteLine (String.Format("Creating directory: {0}\n" +
									"Response status code: {1}\n" +
									"Status description: {2}", directory, response.StatusCode, response.StatusDescription));
						}
					} catch (System.Net.WebException e) {
						if (e.Message.Contains ("550 Can't create directory: File exists")) {
							// 550 directory exists
							Log (String.Format ("Directory already exists: {0}", directory));
						} else {
							Log (String.Format ("Exception: {0}\n{1}", e.Message, e.StackTrace));
						}
					}
				}
			
		}

		float uploadProgress = 0.0f;
		float totalUploadSize = 0.0f;
		float dowloadProgress = 0.0f;
		float downloadSize = 1000.0f;

		bool UploadDataToFTP () 
		{
			bool result = true;
			try {
				// reset upload size and upload progress
				uploadProgress = 0.0f;
				totalUploadSize = 0.0f;

				// create directories
				CreateDirectoriesOnFtp();

				// get a list of files to be sent
				string[] fileNames = Directory.GetFiles ( Environment.GetFolderPath(Environment.SpecialFolder.Personal) );
				int count = fileNames.Length;

				// exclude UPLOADED files, NEWTESTDB and documents that are NOT SIGNED
				FileInfo f;
				for (int i = fileNames.Length; i>0; i--) {
					f = new FileInfo(fileNames[i-1]);
					if ( (f.Name.StartsWith ("UPLOADED")) || (f.Name.StartsWith ("tmp")) 
							|| f.Name.StartsWith("NEWTESTDB") || f.Name.Contains("Manual") 
							|| f.Name.Contains ("_Not_Signed") || f.Name.Contains ("_NOT_Signed") ) {
						fileNames[i-1] = "";
						count --;
					} else {
						totalUploadSize += (f.Length / 1024);
					}
				}

				string fileName = "";
				for (int i = 0; i < fileNames.Length; i++) {
					if (!String.IsNullOrEmpty(fileNames[i])) {
						// gzip the file
						fileName = fileNames[i];
						FileInfo fi = new FileInfo(fileName);
						string compressedFileName;
						if (fi.Extension == ".sqlite") {
							compressedFileName = CompressFile(fileName);
							FileInfo cfi = new FileInfo(compressedFileName);
							totalUploadSize = totalUploadSize - (fi.Length / 1024) + (cfi.Length / 1024);
						} else {
							compressedFileName = fileName;
						}
						// upload the file
						DateTime fileTransferStarted = DateTime.Now;
						bool uploadResult = UploadFileToFTP (compressedFileName);

						result = result && uploadResult;
						if (uploadResult == true) {
							// rename uploaded file
							FileInfo cfi = new FileInfo(compressedFileName);
							string uploadedFileName = cfi.DirectoryName + "/UPLOADED-" + cfi.Name;
							if (File.Exists(uploadedFileName))
								File.Delete(uploadedFileName);
							File.Move (compressedFileName, uploadedFileName);
							
							// delete the original (uncompressed) sqlite file
							if (File.Exists(fileName))
								File.Delete (fileName);
						}

						// log the upload time
						TimeSpan ts = DateTime.Now - fileTransferStarted;
						Log (String.Format("Uploaded file: {0} in {1:F} seconds.", fileName, ts.TotalSeconds));
						
					}
				}
			} catch (Exception e) {
				Log (String.Format ("Exception during FTP data upload: \n{0} \n{1}", e.Message, e.StackTrace));
				result = false;
			}
			return result;
		}

		string CompressFile(string fileName) {
			FileInfo fi = new FileInfo(fileName);
			using (FileStream fs = fi.OpenRead ()) {
				using (FileStream compressedFS = File.Create (fi.FullName + ".gz")) {
					using (GZipStream compressionStream = new GZipStream (compressedFS,  CompressionMode.Compress)) {
						fs.CopyTo (compressionStream);
					}
				}
			}
			string result = fi.FullName + ".gz";
			fi = null;
			return result;
		}

		const string FTPServerPathDataIn = "ftp://puratap.com.au/IPAD%20DATA/DATA.IN/";
		const string FTPServerPathDataOut = "ftp://puratap.com.au/IPAD%20DATA/DATA.OUT/";
		const string FTPUserName = "dmitry@puratap.com.au";
		const string FTPPassword = "D4770AMVB0";

		bool UploadFileToFTP(string fileName) {
			// uploading to different directories on FTP Server depending on file type and date

			// determine the directory first
			FileInfo fi = new FileInfo (fileName);
			string directory = FTPServerPathDataIn;
			switch (fi.Extension) {
				case ".gz": { directory += "SQLite%20DBs/"; break; }
				case ".sqlite": { directory += "SQLite%20DBs/"; break; }
				case ".pdf": { directory += "PDFs/"; break; }
				case ".jpg": { directory += "Photos/"; break; }
				case ".txt": { directory += "Summaries/"; break; }
				default: return true;
			}
			directory = directory + DateTime.Now.Date.ToString ("yyyy-MM-dd") + "/";
			string encodedFileName = (directory + fi.Name).Replace(" ", "%20");

			FtpWebRequest request = (FtpWebRequest)WebRequest.Create (encodedFileName);
			request.Credentials = new NetworkCredential (FTPUserName, FTPPassword);
			request.Method = WebRequestMethods.Ftp.UploadFile;
			request.UseBinary = true;

			using (FileStream fs = File.OpenRead (fileName)) {
				using (Stream requestStream = request.GetRequestStream ()) {
					byte[] buffer = new byte[4096];
					int byteCount;
					int j = 0;
					while ((byteCount = fs.Read (buffer, 0, buffer.Length)) != 0) {
						j++;
						requestStream.Write (buffer, 0, buffer.Length);
						uploadProgress += (4 / totalUploadSize);
						ProgressHUD.Shared.Show ("FTP data exchange in progress (uploading)...", uploadProgress, ProgressHUD.MaskType.Gradient);
						Console.WriteLine (String.Format("Upload progress: {0:F}", uploadProgress));
					}
				}
				fs.Close ();
			}

			return true;
		}
			
		bool GetDataFileFromFTP (string fileName, string fullPath) 
		{
			dowloadProgress = 0.0f;
			downloadSize = 1000.0f;

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
								int j = 0;
								while ((byteCount = bnrResponseStreamReader.Read(buffer, 0, buffer.Length)) != 0) {								
									ms.Write(buffer, 0, byteCount);
									j ++;
									if (j % 10 == 0) {
										dowloadProgress += (40 / downloadSize);
										ProgressHUD.Shared.Show ("FTP data exchange in progress (downloading)...", dowloadProgress, ProgressHUD.MaskType.Gradient);
										Console.WriteLine (String.Format("Download progress: {0:F}", dowloadProgress));
									}
								}
								bnrResponseStreamWriter.Write(ms.ToArray());
								bnrResponseStreamWriter.Flush();
								bnrResponseStreamWriter.Close();
								ProgressHUD.Shared.Show ("FTP data exchange in progress (downloading)...", 1, ProgressHUD.MaskType.Gradient);
								downloadResult = true;
							}
						}

						bool decompressResult;
						FileInfo dfi = new FileInfo(savePath);
						decompressResult = DecompressDataFile(dfi);
							
						downloadResult = downloadResult && decompressResult;
						if (downloadResult == true) {
							string decompressedFilePath = savePath.Remove(savePath.Length - 3);
							File.Delete (savePath);
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

		public void PurgeOldFiles() {
			Log ("Purging old files.");
			string[] fileNames = Directory.GetFiles ( Environment.GetFolderPath(Environment.SpecialFolder.Personal), "*", SearchOption.AllDirectories );

			for (int i = 0; i<fileNames.Length; i++)
			{
				if ( !fileNames [i].Contains ("Manual") ) {
					FileInfo f = new FileInfo (fileNames [i]);
					if (f.LastAccessTime.Date < DateTime.Now.Date.Subtract (TimeSpan.FromDays (7))) {
						Log (String.Format ("Found an old file: {0}, last access time: {1}, deleted", f.Name, f.LastAccessTime.ToString ("yyyy-MM-dd HH:mm:ss")));
						File.Delete (f.FullName);
					}
				}
			}
		}

		public void PurgeUnsignedPDFFiles() {
			Log ("Purging unsigned PDF files.");
			string[] fileNames = Directory.GetFiles ( Environment.GetFolderPath(Environment.SpecialFolder.Personal), "*.pdf", SearchOption.AllDirectories );

			for (int i = 0; i < fileNames.Length; i++) {
				if (fileNames [i].ToUpper().Contains ("_NOT_SIGNED") ) {
					FileInfo f = new FileInfo (fileNames [i]);
					File.Delete (f.FullName);
					Log (String.Format ("Deleted unsigned file: {0}", f.Name));
				}
			}
		}

		void PrepareForDataExchange() {
			this._tabs._app.myLocationManager.StopUpdatingLocation ();
			this._tabs._app.myLocationManager.StopMonitoringSignificantLocationChanges ();		

			try {		// generate summaries for all runs in the database, not just the current run
				if (this._tabs._paySummaryView.GenerateAllSummaryFiles ()) {
					Log ("Summary files generated.");
				} else {
					Log ("Error : An exception was raised while generating summary files!");
				}
			}
			catch (Exception exc) {
				this._tabs._scView.Log (String.Format ("Daily Summary Failed To Generate: {0}, stack trace: {1}", exc.Message, exc.StackTrace));
			}

			try {		// reload job run table data
				this._tabs._jobRunTable.TableView.ReloadData ();
			} catch (Exception ex) {
				this._tabs._scView.Log (String.Format ("Failed to reload JobRunTable data: {0}, stack trace: {1}", ex.Message, ex.StackTrace));
			}

			try {		// write locations buffer to database
				this._tabs._app.myLocationDelegate.DumpLocationsBufferToDatabase ();
			} catch (Exception exc) {
				this._tabs._scView.Log (String.Format ("Failed to write iPad locations buffer to database: {0}, stack trace: {1}", exc.Message, exc.StackTrace));
			}
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

			PrepareForDataExchange ();
			bool DataInputCompletedForRun = CheckDataInputCompletedForRun();

			if (! DataInputCompletedForRun) {
				var alert = new UIAlertView("Warning", "Data has not been input for some of the jobs.\nStart data exchange anyway?", null, "No", "Yes");
				alert.Dismissed += delegate(object _sender, UIButtonEventArgs ev) {
					if (ev.ButtonIndex != alert.CancelButtonIndex) {
						PushSignStockOrStartDataExchange();
					} else {
						this._tabs._app.myLocationManager.StartUpdatingLocation ();
						this._tabs._app.myLocationManager.StartMonitoringSignificantLocationChanges ();
					}
				};
				alert.Show ();
			} else {
				PushSignStockOrStartDataExchange ();
			}
		}

		private bool CheckDataInputCompletedForRun() {
			bool DataInputCompletedForRun;
			if ( string.IsNullOrEmpty(MyConstants.DBReceivedFromServer) || (!File.Exists(Path.Combine (Environment.GetFolderPath(Environment.SpecialFolder.Personal), MyConstants.DBReceivedFromServer))) ) {
				DataInputCompletedForRun = true; // it is not, but we don't want the warning message to pop up
			}
			else DataInputCompletedForRun = this._tabs._jobRunTable.AllJobsDone;
			return DataInputCompletedForRun;
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
			if (e.ButtonIndex != 1) { // if user pressed the "Yes" button (index=0)
				Log ("Resetting Device ID in UserDefaults database.");
				try { 
					// MyConstants.DeviceID = ""; 
					MyConstants.DeviceID = MyConstants.NEW_DEVICE_GUID_STRING; 
				} catch (Exception ex) {
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

