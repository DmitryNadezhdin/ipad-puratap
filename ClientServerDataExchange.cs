using System;
using System.IO;
using System.Net;
using System.Text;
using System.Drawing;
using System.Threading;
using System.Net.Sockets;
using System.Collections.Generic;

using MonoTouch.UIKit;
using MonoTouch.Foundation;

namespace Puratap
{
	public class ClientServerDataExchange
	{	
		public List<NSNetService> _serviceList;
		private NetworkStream nst;
		private ServerClientViewController _controller;
		
		private ManualResetEvent EmployeeHasBeenChosen = new ManualResetEvent(false);
		private ManualResetEvent DatabaseFileHasBeenReceived = new ManualResetEvent(false);
		
		// private NSNetServiceBrowser _netBrowser;
		public ClientServerDataExchange (ServerClientViewController controller)
		{
			_controller = controller;
			// InitNetBrowser();
		}
		
		internal void InitNetBrowser() 
		{
			_serviceList = new List<NSNetService> ();
			// _netBrowser = new NSNetServiceBrowser ();
			/*
			_netBrowser.FoundService += delegate (object sender, NSNetServiceEventArgs e) 
			{
				_controller.Log(String.Format("Service added: {0}", e.Service.Name));
				_serviceList.Add(e.Service);
				e.Service.AddressResolved += ServiceAddressResolved;
			};
			
			_netBrowser.ServiceRemoved += delegate(object sender, NSNetServiceEventArgs e) 
			{
				_controller.Log(String.Format("Service removed: {0}", e.Service.Name));
				var nsService = _serviceList.FindLast(s => s.Name.Equals (e.Service.Name));
				_serviceList.Remove(nsService);
			};

			_netBrowser.SearchForServices("_ipadService._tcp", "");		// a service created by Windows Forms App (rewritten it to be a console application)
			*/
		}

		delegate void AppendTextLineCallback(string text);
		public static ManualResetEvent receiveDone = new ManualResetEvent(false);		// These events notify the main data exchange thread that file operations have been completed
		public static ManualResetEvent sendDone = new ManualResetEvent(false);
		public static ManualResetEvent fileExchangeDone = new ManualResetEvent(false);
		
		/*
		public static UIImage GetImagefromByteArray (byte[] imageBuffer)					// This function is implemented for testing purposes, to be removed
		{
		    NSData imageData = NSData.FromArray(imageBuffer);
		    return UIImage.LoadFromData(imageData);
		}
		*/
		
		public class StateObject {																					// This class is used in file transfers from iPads to Windows-based server
			
			public NetworkStream workStream = null;					// client's NetworkStream
			public const int bufferSize = 1024;							// size of the receiving buffer
			public byte[] buffer = new byte[bufferSize];				// receiving buffer
			public int bytesReceived = 0;										// number of bytes received so far
			public int fileSize = 0;												// size of file to be transferred
			public FileTypes fileType;

		}
		
		void ServiceAddressResolved (object sender, EventArgs e) {
			NSNetService ns = sender as NSNetService;
			if (ns != null) CallServer(ns);
		}
		
		public int CallServer(NSNetService ns) 
		{					// this procedure handles the data transfer between the app server and our client application
			try 
			{
				if (ns != null) {

					string hostName = ns.HostName;
					int port = ns.Port;
					
					ThreadStart ts = new ThreadStart ( delegate {
						using (var pool = new NSAutoreleasePool() ) {

							const int maxSize = 1024;

							TcpClient tcpClient = null;
							try {
								tcpClient = new TcpClient(hostName, port);
								tcpClient.ReceiveTimeout = 10000;
								tcpClient.SendTimeout = 10000; // these timeouts only apply to sync read and write operations
								
								_controller.Log ("Data exchange thread started: " + Thread.CurrentThread.ManagedThreadId.ToString ());

								using (NetworkStream netStream = tcpClient.GetStream() )
								{
									/*		A couple potential issues here:
									 * 
									 * 		1. We should never assume that data piece, however small it may be, will come through the network synchronously
									 * 			Therefore, every send or receive operation should be performed in async mode (!not the case currently!).
									 * 		2. Device identification. We DO NOT rely on devices' serial numbers or ECIDs, we create a GUID on the device and 
									 * 			tie those GUIDs to employee number (REPNUM in WREP or PLNUM in PLUMB) in FoxPro database.
									 * 			When the device initiates the data exchange, it sends over its GUID. 
									 * 			The server then checks the database to decide which file(s) should be transferred to the device.
									*/	

									receiveDone.Reset();
									sendDone.Reset();

									nst = netStream;
									// DEBUG :: NEW DEVICE :: 
									//string msg = MyConstants.NEW_DEVICE_GUID_STRING; // "aaaabbbb-cccc-dddd-eeee-ffff00001111";
									string msg = MyConstants.DeviceID;
									
									byte[] sendBuffer = Encoding.ASCII.GetBytes(msg);
									netStream.Write(sendBuffer, 0, sendBuffer.Length);
									_controller.Log("We told the server: "+msg);
									
									// get the server response for the user ID sent and act accordingly
									byte[] receiveBuffer = new byte[1];
									netStream.Read (receiveBuffer, 0 , 1);
									var r = MyConstants.GetServerResponse(receiveBuffer[0]);
									
									switch(r)
									{
										case MyConstants.ServerResponsesForUserID.UserIDNewUser:
										{
										// this starts a process handling a new device
										// 1st we should determine the employee who is the device's owner
										// to do that, the server sends out a list of employees that do not have a mobile device linked to them
										// the client accepts the list and presents it to the user
										// user makes his choice, the app sends it back to the server along with a device GUID
										// the client then receives a database file
											
											tcpClient.ReceiveTimeout = 100000;
											tcpClient.SendTimeout = 100000;

											EmployeeHasBeenChosen.Reset ();

											// RECEIVE list of employess without a device linked to them
											List<Employee> AvailableEmployees = ReceiveEmployees ();
										
											// DISPLAY the list to the user and get him to choose one
											Employee chosen = new Employee();
											chosen = ChooseEmployee (AvailableEmployees);
											
											if (chosen != null) {
												MyConstants.DeviceID = ( Guid.NewGuid () ).ToString();
												chosen.DeviceGuid = new Guid( MyConstants.DeviceID );
											
												// SEND the chosen employee to server
												SendEmployee (chosen);

												// RECEIVE database file
												DatabaseFileHasBeenReceived.Reset ();
												if ( ReceiveDatabaseFile () ) {
													// DISPLAY RECEIVED DATA
													_controller.InvokeOnMainThread ( delegate() {
														DatabaseFileHasBeenReceived.WaitOne ();
														_controller._tabs._jobRunTable._ds.LoadJobRun(false); 
														_controller.Log ("Data exhange with the server completed.");
													});
												}
												// NEW USER, NOTHING TO SEND
												// SendFilesToServer (netStream);							
											}
											break;
										}
										
										case MyConstants.ServerResponsesForUserID.UserIDFound:
										{
											_controller.Log(String.Format ("We received code: {0}", MyConstants.ServerResponsesForUserID.UserIDFound.ToString() ));

											// IMPLEMENTED :: sending files does happen BEFORE receiving now (or not at all in case of new users)
										
											//	now we must transfer data from iPad to server: database itself (IMPLEMENTED), all SIGNED .pdf documents, all taken photos (IMPLEMENTED)
											//	after the data has been successfully transferred, we must consider purging it from the device (perhaps keeping last 7 days worth of data) (IMPLEMENTED)
											// 	then we receive the database file for current date (dates?) and disconnect gracefully, leaving the server open to accept other clients		
											bool sendSuccess = false;
											try {
												sendSuccess = SendFilesToServer (netStream);
											}
											catch (Exception e)
											{												
												_controller.Log (e.Message);
												sendSuccess = false;
											}
											
											if (sendSuccess)
											{
												if ( ReceiveDatabaseFile () ) {
													// DISPLAY contents of received database
													_controller.InvokeOnMainThread ( delegate() {
														DatabaseFileHasBeenReceived.WaitOne ();	// wait for database file to be received before proceeding further
														// TODO :: 	now we should send server an acknowledgement that the database file has been successfully saved 
														// (why? server doesn't care. Actually, it DOES CARE, and it could re-send the data if not received successfully and the connection is still alive)
														
														if (_controller._tabs._jobRunTable._ds.TestDBIntegrity () == true)
														{
															_controller._tabs._jobRunTable._ds.LoadJobRun(false); // reload the run
															_controller.Log ("Data exhange with the server completed successfully.");
															// PURGE old files (older than a week)
															PurgeOldFiles();
														}
														else {
															var dataIntegrityError = new UIAlertView("Database integrity error", 
														                                         "Please try downloading the data from the server again. If problem persists, call the ambulance!", null, "OK");
															dataIntegrityError.Show ();
															_controller.Log ("Database integrity error. The database file may have been corrupted during the data transfer.");
														}
													});
												}
												else {
													// Could not receive database file
													_controller.Log ("Database transfer failed.");
												}
											}
											else {
												_controller.Log ("Sending data failed.");
											}

											break; 
										}
										case MyConstants.ServerResponsesForUserID.UserIDNotFound:
										{ 
											_controller.Log (String.Format ("Cannot proceed: Device ID not found on the server: {0}.", MyConstants.DeviceID)); 
											
											_controller.InvokeOnMainThread (delegate {
												using (var a = new UIAlertView("Data exchange failed", String.Format ("Device ID not found on the server: ", MyConstants.DeviceID), null, "Too bad") ) { a.Show (); }
											});

											fileExchangeDone.Set (); 
											break; 
										}
									} // end switch(r)

									_controller.SetDataExchangeButtonEnabled ();
									_controller.SetExchangeActivityHidden ();			
									tcpClient.Close();
									_controller.Log ("Data exchange thread finished: "+ Thread.CurrentThread.ManagedThreadId.ToString () );
									return;		// this line will close the data exchanging thread
								}
							} 
							catch (Exception ex) 
							{
								if (tcpClient != null) 
									tcpClient.Close();

								_controller.Log(String.Format("Exception when calling server: {0} \r\n Message: {0}", ex, ex.Message));
								_controller.SetExchangeActivityHidden ();
								_controller.SetDataExchangeButtonDisabled ();

								_controller.InvokeOnMainThread (delegate {
									using (var dataExchangeUnsuccessful = new UIAlertView("Data exchange unsuccessful", "Please try again", null, "OK")) 
									{
										dataExchangeUnsuccessful.Show (); 
									}
								});
								return;
							}
						}
					} );
					Thread t = new Thread(ts);
					t.Start();
					return 1;
				}
				else { _controller.SetDataExchangeButtonDisabled (); return 0; }
			}
			catch (Exception e)
			{
				_controller.Log (e.Message);
				return 0;
			}
			finally 
			{
			}
		}
		
		public void SendByte(byte val) {
			nst.WriteByte (val);
		}
		public void SendInteger(int val) {
			byte[] buffer = new byte[4];
			buffer = BitConverter.GetBytes (val);
			nst.Write (buffer, 0, 4);
		}
		
		public void SendString(string val) {
			SendInteger (val.Length);
			nst.Write ( Encoding.ASCII.GetBytes (val), 0, val.Length);

			/* Needs more testing
			sendDone.Reset ();
			nst.BeginRead ( Encoding.ASCII.GetBytes (val), 0, val.Length, SendCallback, new StateObject { workStream = nst } );
			sendDone.WaitOne (); */
		}
		
		public void SendEmployee(Employee e) {
			SendByte( (byte)e.EmployeeType );
			SendInteger (e.EmployeeID);
			SendString (e.DeviceGuid.ToString() );
		}
		
		public byte ReceiveByte() { 
			byte[] b = new byte[1];
			nst.Read (b, 0, 1);
			return b[0];
		}
		
		public int ReceiveInteger() { 
			byte[] buffer = new byte[4];
			nst.Read (buffer, 0, 4);
			return BitConverter.ToInt32 (buffer, 0); 
		}
		
		public string ReceiveString() { 
			int length = ReceiveInteger ();
			byte[] str = new byte[length];
			nst.Read (str, 0, length);
			return Encoding.ASCII.GetString (str); 
		}
		
		public Employee ReceiveEmployee() 
		{ 
			Employee e = new Employee();
			e.EmployeeType = (MyConstants.EmployeeTypes) ReceiveByte ();
			e.EmployeeID = ReceiveInteger ();
			e.FirstName = ReceiveString ();
			e.LastName = ReceiveString ();
			e.FullName = String.Format ("{0} {1}", e.FirstName, e.LastName);
			return e;
		}
		
		public List<Employee> ReceiveEmployees() 
		{
			List<Employee> result = new List<Employee> ();
			int count = ReceiveInteger ();
			for (int i = 0; i<count; i++)
			{
				Employee e = ReceiveEmployee();
				result.Add (e);
			}
			return result; 
		}
		
		
		public Employee ChooseEmployee(List<Employee> employees)
		{
			Employee chosenEmp = new Employee();
			string[] fullNames = new string[employees.Count]; int i = 0;
			foreach(Employee e in employees) { fullNames[i] = e.FullName; i++; }
			this._controller.InvokeOnMainThread ( delegate() {
				UIActionSheet ac = new UIActionSheet("The server recognized this device as new. \nPlease choose your name from the list", null, "Cancel", null, fullNames);
				ac.WillDismiss += delegate(object sender, UIButtonEventArgs e) {
					if (e.ButtonIndex == ac.CancelButtonIndex) {
						chosenEmp = null;
					}
					else {
						chosenEmp = employees[e.ButtonIndex]; // fullNames[e.ButtonIndex];
						// chosenEmp.EmployeeType = employees[e.ButtonIndex].EmployeeType;
					}
					EmployeeHasBeenChosen.Set ();
				};
					
				ac.ShowInView (_controller.View);
			});
				
			EmployeeHasBeenChosen.WaitOne ();
			
			// Cannot proceed here unless the user has made a choice
			return chosenEmp;
		}
		
		public MyConstants.EmployeeTypes ReceivePuratapEmployeeType(NetworkStream netStream)
		{	
			byte[] response = new byte[1];
			netStream.Read (response, 0, 1);
			int result = response[0]; // BitConverter.ToInt32 (response, 0);
			return (MyConstants.EmployeeTypes) result;
		}
		
		public int ReceivePuratapEmployeeID(NetworkStream netStream)
		{
			byte[] response = new byte[4];
			netStream.Read (response, 0, 4);
			int result = BitConverter.ToInt32 (response, 0);
			return result;
		}
		
		public string ReceivePuratapEmployeeName(NetworkStream netStream)
		{
			byte[] strl = new byte[4];
			netStream.Read (strl, 0, 4);
			int length = BitConverter.ToInt32 (strl, 0);
			
			byte[] strContents = new byte[length];
			length = netStream.Read (strContents, 0, strContents.Length);
			string result = Encoding.ASCII.GetString (strContents);
			
			return result;
		}
						
		public bool ReceiveDatabaseFile() 
		{
			// RECEIVE PURATAP_EMPLOYEE_ID, employee type and employee name and save these to iPad's UserDefaults database
			MyConstants.EmployeeType = ReceivePuratapEmployeeType (nst); 	// this is sent as a single byte
			MyConstants.EmployeeID = ReceivePuratapEmployeeID (nst);			// this is sent as 32-bit integer (4 bytes)
			MyConstants.EmployeeName = ReceivePuratapEmployeeName (nst);	// this is sent as string length (integer) and string contents (string)
			
			string fileName;
			try 
			{
				// RECEIVE FILE NAME
				// string fileName = ServerClientViewController.dbFilePath; // wtf? server should send file name length and a file name
				fileName = ReceiveString ();
				_controller.Log (String.Format ("Received database file name: {0}", fileName));
				fileName = Environment.GetFolderPath (Environment.SpecialFolder.Personal) +"/"+ fileName;

				// RECEIVE DATABASE FILE contents
				byte[] fileContents = ReceiveFileContents (nst);
				
				if (fileContents.Length > 0 && fileContents != null) {
					// 	SAVE DATABASE FILE
					SaveDownloadedFile(fileContents, fileName);
					DatabaseFileHasBeenReceived.Set ();
				}
				else { DatabaseFileHasBeenReceived.Set (); return false; }

				MyConstants.DBReceivedFromServer = fileName;
				MyConstants.LastDataExchangeTime = DateTime.Now.ToString ("yyyy-MM-dd HH:mm:ss");
				return true;
			} 
			catch (Exception e) 
			{ 
				_controller.Log (String.Format ("ReceiveDatabaseFile : Exception : {0}", e.Message));
				DatabaseFileHasBeenReceived.Set ();
				return false; 
			}			
		}
		
		public byte[] ReceiveFileContents (NetworkStream netStream)
		{
			try {
				byte[] fileLength = new byte[4];
				netStream.Read(fileLength, 0, 4); 		// sync network stream read :: concern
				int filesize = BitConverter.ToInt32(fileLength,0);
				string response = Convert.ToString(filesize);
				_controller.Log("Received data: File Length: "+response);
				
				// fileLength could be 0 here, this notifies the client that the file has not been found, we should act accordingly
				if (filesize == 0) { 
					_controller.Log ("File couldn't be found on the server."); 
					fileExchangeDone.Set(); 
					return new byte[0];
				}
			
				// start async file receive operation
				StateObject state = new StateObject();
				state.workStream = netStream;
				state.fileSize = filesize;
				state.buffer = new byte [filesize];
				
				netStream.BeginRead(state.buffer, 0, state.fileSize, ReceiveCallback, state);
				if (! receiveDone.WaitOne(new TimeSpan(0,0, 10)))
					throw new SocketException( (int) SocketError.TimedOut);

				return state.buffer;
			}
			catch (Exception e) {
				_controller.Log (String.Format ("Received file contents: {0}: {1}", e.GetType().ToString(), e.Message));
				return null;
			}
		}
		
		public bool SendFilesToServer(NetworkStream netStream)
		{
			// send all files except those that start with UPLOADED prefix and NEWTESTDB.sqlite
			string[] fileNames = Directory.GetFiles ( Environment.GetFolderPath(Environment.SpecialFolder.Personal) );
			int count = fileNames.Length;
			
			// exclude UPLOADED files, NEWTESTDB and documents that are NOT SIGNED
			for (int i = fileNames.Length; i>0; i--)
			{
				FileInfo f = new FileInfo(fileNames[i-1]);
				if ( (f.Name.StartsWith ("UPLOADED")) || (f.Name.StartsWith ("tmp")) || f.Name.StartsWith("NEWTESTDB") || f.Name.Contains ("_Not_Signed") || f.Name.Contains ("_NOT_Signed") ) 
				{
					fileNames[i-1] = "";
					count --;
				}
			}

			// send an integer flag signaling the server if the data is live
			int isLiveData = 1; // TODO :: (this._controller._tabs._jobRunTable.AllJobsDone)? 1: 0;
			// TODO :: SendInteger (isLiveData);
			// TODO :: _controller.Log ( String.Format ("Sent live data flag: {0}", (isLiveData==1) ) );

			// send files count
			SendInteger (count);
			_controller.Log ( String.Format ("Files to send: {0}", count) );
			
			for (int i = 0; i<fileNames.Length; i++)
			{
				if (fileNames[i] != "") {
					string fileName = fileNames[i];  // ServerClientViewController.dbPath;
					
					string extension = Path.GetExtension (fileName);
					FileTypes fileType;
					switch(extension)
					{
					case ".jpg": { fileType = FileTypes.Photo; break; }
					case ".sqlite": { fileType = FileTypes.SQLiteDatabase; break; }
					case ".db": { fileType = FileTypes.SQLiteDatabase; break; }
					case ".pdf": { fileType = FileTypes.PDFDocument; break; }
					case ".txt": { fileType = FileTypes.Summary; break; }
					default: { fileType = FileTypes.None; break; }
					}

					try {
						if (SendFile(fileName, fileType, netStream))
						{
							// if the file has been sent successfully, rename the file to "UPLOADED-"+oldFileName
							RenameUploadedFile(fileName);
						}
						else
						{
							return false;
							// result = false;
							// break;
						}
					}
					catch 
					{
						return false;
					}
				}
			}
			return true;
		}
		
		public void RenameUploadedFile(string fName)
		{
			if ( File.Exists (fName) )
			{
				FileInfo f = new FileInfo(fName);
				string newFileName = Path.Combine (f.Directory.ToString(), "UPLOADED-"+f.Name);
				if (File.Exists (newFileName) ) 
					File.Delete (newFileName);
				File.Move (f.FullName, newFileName);		// DEBUG :: comment out this line when debugging data exchange
			}
		}
		
		public void PurgeOldFiles() {
			_controller.Log ("Purging old files.");
			string[] fileNames = Directory.GetFiles ( Environment.GetFolderPath(Environment.SpecialFolder.Personal), "*", SearchOption.AllDirectories );
			
			for (int i = 0; i<fileNames.Length; i++)
			{
				FileInfo f = new FileInfo(fileNames[i]);
				if ( f.LastAccessTime.Date < DateTime.Now.Date.Subtract (TimeSpan.FromDays (7)) )
				{

					_controller.Log (String.Format ("Found an old file: {0}, last access time: {1}, deleted", f.Name, f.LastAccessTime.ToString ("yyyy-MM-dd HH:mm:ss") ));
					File.Delete (f.FullName);
				}
			}
		}
		
		public bool SendFile(string fileName, FileTypes fType, NetworkStream netStream)
		{
			FileInfo fileInfo = new FileInfo(fileName);
			//using (FileStream fileStream = File.OpenRead (fileName) ) {
			using ( FileStream fileStream = new FileStream(fileName, FileMode.OpenOrCreate, FileAccess.Read) ) {
				using( BinaryReader reader = new BinaryReader( File.OpenRead (fileName) ) ) {
					try 
					{
						StateObject state = new StateObject();
						state.workStream = netStream;
						state.fileType = fType;
						state.buffer = new byte [fileInfo.Length];
						state.buffer = reader.ReadBytes (state.buffer.Length); // WAS :: File.ReadAllBytes(fileName); reader.ReadBytes (state.buffer.Length);
						state.fileSize = state.buffer.Length;	
		
						// send file type
						netStream.Write ( BitConverter.GetBytes ((Int32)state.fileType), 0, 4);
						_controller.Log(String.Format ( "Sending file type: {0}", state.fileType.ToString () ));
			
						// send file name length
						byte[] fileNameLength = new byte[4];
						fileNameLength = BitConverter.GetBytes (fileInfo.Name.Length);
						netStream.Write (fileNameLength, 0, 4);
						_controller.Log(String.Format ( "Sending file name length: {0}", BitConverter.ToInt32(fileNameLength, 0) ));
						
						// send file name
						byte[] sendBuffer = Encoding.ASCII.GetBytes (fileInfo.Name);
						netStream.Write (sendBuffer, 0, sendBuffer.Length);
						_controller.Log(String.Format ( "Sending file name: {0}", fileInfo.Name));
			
						// send file size
						byte[] fileLength = new byte[4];
						fileLength = BitConverter.GetBytes (state.fileSize);
						netStream.Write (fileLength, 0, 4);
						_controller.Log(String.Format ( "Sending file size: {0}", BitConverter.ToInt32(fileLength,0) ));
						
						// async send file contents
						sendDone.Reset ();
						netStream.BeginWrite(state.buffer, 0, state.fileSize, SendCallback, state);
						bool socketTimedOut = !sendDone.WaitOne(new TimeSpan(0,0,0,10));
						if (socketTimedOut)
							throw new SocketException((int)SocketError.TimedOut);
						_controller.Log (String.Format ("Sent file contents to server: {0} bytes", state.fileSize));
					} 
					catch (Exception e) { 
						_controller.Log ( String.Format ("Exception when transferring file to server: \nFile name: {0} \nMessage: {1}\nStack trace: {2}", fileName, e.Message, e.StackTrace) );
						_controller.SetExchangeActivityHidden ();
						_controller.StopNetBrowser();
						_controller.StartNetBrowser ();

						this._controller.InvokeOnMainThread (delegate { 
							var alert = new UIAlertView ("Data exchange incomplete.", "One or more files have not been trasferred successfully. Please try again", null, "OK");
							alert.Show (); }
						);

						return false; 
					}
				}
			}
			return true;
		}
		
		public bool SaveDownloadedFile(byte[] bytes, string fileName) 
		{
			try {
				if (File.Exists (fileName) )
					File.Delete (fileName);

				File.WriteAllBytes (fileName, bytes);	
				_controller.Log(String.Format("The file has been saved to disk: {0}", fileName));
			}
			catch (Exception e) {
				_controller.Log(String.Format("Exception when saving file: {0}", e.Message));
				return false;
			}
			return true;
		}
		
		public void ReceiveCallback(IAsyncResult ar) 
		{ 	
			StateObject state = (StateObject) ar.AsyncState;	// get the state of the opertation
			try 
			{
				NetworkStream stream = state.workStream;		// get the client's net stream from the state

				int bytesRead = stream.EndRead(ar);					// get the amount of bytes read from a socket
				state.bytesReceived += bytesRead;						// save it to the state object
				
				if (state.bytesReceived == state.fileSize) {
					receiveDone.Set();											// when state.fileSize bytes have been received, notify the receiving thread to continue execution
				}
				else {																// if not, continue reading from socket
					stream.BeginRead(state.buffer, state.bytesReceived, state.fileSize-state.bytesReceived, ReceiveCallback, state);
				} 
			}
			catch (ObjectDisposedException e) {
				state.workStream.Close ();
				_controller.Log(String.Format ("Apparently, the NetworkStream object was disposed from another thread...\r\n {0}", e.Message));
				_controller.SetExchangeActivityHidden ();
			}
			catch (IOException e)
			{
				_controller.Log (String.Format ("IOException: {0}, stack trace: {1}", e.Message, e.StackTrace));
				_controller.SetExchangeActivityHidden ();
			}
		}
		
		public void SendCallback(IAsyncResult ar) {
			// try {
			StateObject state = (StateObject)ar.AsyncState;		// extracting state of the async operation
			try {
				state.workStream.EndWrite(ar);								// this call ends the writing operation (if this is not called, the socket remains in writing state and is not able to send or receive data)													
				_controller.Log (String.Format("SendCallback : {0} bytes sent to server.", state.fileSize));
			}

			catch (Exception e) {
				// state.workStream.Close ();
				_controller.SetExchangeActivityHidden ();
				_controller.SetDataExchangeButtonDisabled ();
				_controller.StopNetBrowser ();
				_controller.StartNetBrowser ();
				_controller.Log (String.Format ("Exception: {0}, stack trace: {1}", e.Message, e.StackTrace));
			}
			finally {
				sendDone.Set ();	// raise an event to notify the other threads that file has been sent
			}
			
		}
	}
}


