using System;
using System.IO;
using System.Threading;
using System.Drawing;
using System.Collections.Generic;
using MonoTouch.Foundation;
using MonoTouch.CoreFoundation;
using MonoTouch.CoreLocation;
using MonoTouch.CoreGraphics;
using MonoTouch.UIKit;
using Mono.Data.Sqlite;
using MonoTouch.SystemConfiguration;
using ZSDK_Binding;

namespace Puratap
{
	public enum NavigationButtonsMode { CustomerDetails, CustomerMemos, JobHistory, PrePlumbing, ServerClient, None }
	public enum FileTypes { SQLiteDatabase, PDFDocument, Photo, Summary, None }
	public enum PaymentTypes { Cash, Cheque, CCDetails, CreditCard, EFTPOS, Invoice, Split, None }
	
	public enum CustomerDetailsUpdatableField { None = 0, 
		FirstName = 1, LastName = 2, 
		Address = 3, Suburb = 4, 
		Phone = 5, MobilePhone = 6, 
		FallbackContact = 7, FallbackPhone = 8, 
		JobPriceTotal = 9, CompanyName = 10, 
		SpecialComments = 11, AttentionReason = 12, TubingUpgradeDone = 13  };
	public enum SignableDocuments { None, PrePlumbingCheck, ServiceReport, Receipt };
	
	public enum FollowUpsRequired {
		None, // No,	Yes,
		UnableToOfferUpgradeOption1, UnableToOfferUpgradeOption2,
		ClientUnhappyWithPricing, ClientUnhappyWithService,
		WantsAQuoteOnExtras, 
		UnableToCompleteBookedJob,
		OtherTechnicalIssues,
		Other
	}
	
	public class MyConstants
	{
		public static JobRunTable _jrt;

		public static string DB_RECEIVED_FROM_SERVER = "PuratapLastDBReceivedFromServer";
		public static string DBReceivedFromServer
		{ 
			get {
				string result = NSUserDefaults.StandardUserDefaults.StringForKey (DB_RECEIVED_FROM_SERVER);
				// if nothing is in the user settings database, there is little we can do, the app was installed on a new device most likely
				if (string.IsNullOrEmpty (result))
					result = String.Empty;
				else if (! File.Exists (result))	// if database file does not exist, this could be a new version of the app installed (thus changing the bundle's directory name), we should attempt to find the file
				{
					// save just the filename to the result string
					result = result.Substring (result.LastIndexOf ('/')+1);
					// if we can locate the file in the current bundle's "Documents" folder, we will return that
					if (File.Exists ( Path.Combine (Environment.CurrentDirectory.Substring (0,Environment.CurrentDirectory.LastIndexOf ('/')), "Documents/"+result)))
						result = Path.Combine (Environment.CurrentDirectory.Substring (0,Environment.CurrentDirectory.LastIndexOf ('/')), "Documents/"+result);
				}
				return result;
			} 
			set {
				string newValue = value.ToString ();
				NSUserDefaults.StandardUserDefaults.SetString (newValue, DB_RECEIVED_FROM_SERVER);
				NSUserDefaults.StandardUserDefaults.Synchronize ();
			}
		}

		public static string LastDataExchangeTimeKey = "PURATAP_LAST_DATA_EXCHANGE_TIME";
		public static string LastDataExchangeTime
		{
			get {
				string saved = NSUserDefaults.StandardUserDefaults.StringForKey (LastDataExchangeTimeKey);
				if (string.IsNullOrEmpty (saved)) {
					saved = "";
				}
				return saved;
			}
			set {
				string newValue = value;
				NSUserDefaults.StandardUserDefaults.SetString (newValue, LastDataExchangeTimeKey);
				NSUserDefaults.StandardUserDefaults.Synchronize ();
			}
		}
		
		// DEPRECATED :: public static bool AUTO_CHANGE_DATES = true; // false
		public static string DEBUG_TODAY = String.Format (" '{0}' ", DateTime.Now.Date.ToString ("yyyy-MM-dd")); // " '2012-06-28' ";
		public static long DUMMY_MEMO_NUMBER = 999999999999;
		public enum JobStarted { Yes, CustomerNotAtHome, CustomerRebooked, PuratapLate, AddressWrong, Other, None }
				
		public static string NEW_DEVICE_GUID_STRING = "aaaabbbb-cccc-dddd-eeee-ffff00001111";
		private static string DEVICE_ID_STRING = "PuratapDeviceID";
		public static string DeviceID {
			get {
				string result = NSUserDefaults.StandardUserDefaults.StringForKey (DEVICE_ID_STRING);				
				if (string.IsNullOrEmpty (result))
					result = NEW_DEVICE_GUID_STRING;
				return result;
			}
			set {
				string newValue = value.ToString ();
				NSUserDefaults.StandardUserDefaults.SetString (newValue, DEVICE_ID_STRING);
				NSUserDefaults.StandardUserDefaults.Synchronize ();
			}
		}
		
		private static string PURATAP_EMPLOYEE_ID_STRING = "PuratapDeviceOwnerID";
		public static int EmployeeID { 
			get { return Convert.ToInt32 ( NSUserDefaults.StandardUserDefaults.StringForKey (PURATAP_EMPLOYEE_ID_STRING) ); }
			set {
				string newValue = value.ToString ();
				NSUserDefaults.StandardUserDefaults.SetString (newValue, PURATAP_EMPLOYEE_ID_STRING);
				NSUserDefaults.StandardUserDefaults.Synchronize ();
			} 
		}
		
		private static string PURATAP_EMPLOYEE_NAME_STRING = "PuratapDeviceOwnerName";
		public static string EmployeeName { 
			get { return NSUserDefaults.StandardUserDefaults.StringForKey (PURATAP_EMPLOYEE_NAME_STRING); }
			set { 
				string newValue = value.ToString (); 
				NSUserDefaults.StandardUserDefaults.SetString (newValue, PURATAP_EMPLOYEE_NAME_STRING);
				NSUserDefaults.StandardUserDefaults.Synchronize ();
			} 
		}
		
		private static string PURATAP_EMPLOYEE_TYPE_STRING = "PuratapDeviceOwnerJobType";
		public enum EmployeeTypes { Franchisee, Plumber, None }
		public static EmployeeTypes EmployeeType { 
			get { return (EmployeeTypes) Convert.ToInt32 ( NSUserDefaults.StandardUserDefaults.StringForKey (PURATAP_EMPLOYEE_TYPE_STRING) ); }
			set {
				string newValue = (Convert.ToInt32(value)).ToString();
				NSUserDefaults.StandardUserDefaults.SetString (newValue, PURATAP_EMPLOYEE_TYPE_STRING);
				NSUserDefaults.StandardUserDefaults.Synchronize ();
			}
		}
		
		public enum ServerResponsesForUserID { UserIDNotFound=0, UserIDFound=1, UserIDNewUser=2, None=3 }
		public static ServerResponsesForUserID GetServerResponse(byte received)
		{
			return (ServerResponsesForUserID) received;
		}
		
		public static	 RectangleF rectAroundBallValve = new RectangleF(75,210,150,165);
		public static	 RectangleF rectAroundTap = new RectangleF(475, 0, 120, 210);
		public static	 RectangleF rectAroundInletTubing = new RectangleF(70, 0, 140, 210);
		public static	 RectangleF rectAroundOutletTubing = new RectangleF(460, 130, 80, 160);
		public static	 RectangleF rectAroundUnit = new RectangleF(260, 85, 190, 280);
				
		public struct PrePlumbingPDFTemplateTags
		{
			public static int CustomerNumber = 1;
			public static int Date = 2;
			public static int CustomerName = 3;
			public static int Signature = 4;
			public static int LeakingBrassFittings = 5;
			public static int LeakingTap = 6;
			public static int PotentialLeak = 7;
			public static int OldTubing = 8;
			public static int NonPuratapComponents = 9;
			public static int ExistingDamage = 10;
			public static int Comments = 11;
			public static int CustomerAcceptedUpgrade = 12;
			public static int UpgradeOfferText = 13;
			public static int CustomerSignatureLabel = 14;
			public static int OfficeFollowUpRequired = 15;
			public static int NotPuratapProblem = 16;
			public static int PuratapEmployeeName = 17;
			public static int PuratapLogo = 18;
			public static int TubingUpgradeNotice = 19;
		}
		
		public struct ServiceCallPDFTemplateTags
		{
			public static int CustomerNumber = 1;
			public static int Date = 2;
			public static int CustomerName = 3;
			public static int Signature = 4;
			public static int ServiceRepName = 5;
			public static int FilterType = 6;
			public static int TapType = 7;
			public static int Comments = 8;
			public static int IssuesFoundView = 9;
		}

		public struct ReceiptPDFTemplateTags
		{
			public static int Logo = 1;
			public static int Signature = 32;
			public static int GSTLabel = 35;
			public static int DepositLabel = 36;
		}
		 
		public MyConstants ()
		{

		}

		public static List<JobType> GetJobTypesFromDB() 
		{
			List<JobType> result = new List<JobType>();
			if (File.Exists (ServerClientViewController.dbFilePath) )
			{
				// read the data from database here
				using (var connection = new SqliteConnection("Data Source="+ServerClientViewController.dbFilePath) )
				{
					try {
						connection.Open();
						using (var cmd = connection.CreateCommand() )
						{
							cmd.CommandText = "SELECT * FROM Job_types ORDER BY JOB_TYPE_ID";
							using (var reader = cmd.ExecuteReader())
							{
								while ( reader.Read() )
								{
									string code = (string) (reader["job_type_code"]);
									string description = (string) reader["job_type_description"];
									double retailPrice = Convert.ToDouble (reader["job_type_retail_price"]);
									double loyaltyPrice = Convert.ToDouble (reader["job_type_loyalty_price"]);
									double employeeFee = (MyConstants.EmployeeType == EmployeeTypes.Franchisee) ? Convert.ToDouble (reader["job_type_franchisee_fee"]) : Convert.ToDouble (reader["job_type_plumber_fee"]);
									bool canDo = (MyConstants.EmployeeType == EmployeeTypes.Franchisee) ? Convert.ToBoolean (reader["franchisee_can_do"]) : Convert.ToBoolean (reader["plumber_can_do"]);
									
									result.Add ( new JobType() {
										Code = code,
										Description = description,
										RetailPrice = retailPrice,
										LoyaltyPrice = loyaltyPrice,
										EmployeeFee = employeeFee,
										CanDo = canDo
									} );
								}

								if (!reader.IsClosed) reader.Close ();
							}
						}
					}
					catch (Exception e) {
						// Console.WriteLine (e.Message);
					}
				}
				
				return result;
			}
			else { return null; }
		}
		
		public static UIImage ImageFromPDF (CGPDFDocument pdf, int pageNumber)
		{
			if (pageNumber < 1)
				return null;
			else {
				CGPDFPage pdfPage = pdf.GetPage (pageNumber);
				RectangleF rect = pdfPage.GetBoxRect (CGPDFBox.Art);
				
				UIImage result = new UIImage();
				
				UIGraphics.BeginImageContext (rect.Size);
				CGContext context = UIGraphics.GetCurrentContext ();
				CGColorSpace rgb = CGColorSpace.CreateDeviceRGB ();
				CGColor color = new CGColor(rgb, new float[] {1,1,1,1});
				context.SetFillColor (color);
				context.FillRect (rect);
				
				context.TranslateCTM (0.0f, rect.Size.Height);
				context.ScaleCTM (1.0f, -1.0f);
				
				if (pdfPage != null)
				{
					context.SaveState ();
					CGAffineTransform transform = pdfPage.GetDrawingTransform (CGPDFBox.Crop, rect, 0, true);
					context.ConcatCTM (transform);
					context.DrawPDFPage (pdfPage);
					context.RestoreState ();
					result = UIGraphics.GetImageFromCurrentImageContext ();
				}
				
				UIGraphics.EndImageContext ();
				return result;
			}
		}

		public static string PreparePDFFileForPrintingAView(UITableView view)
		{
			try {
				string tmpFileName = Path.Combine (Environment.GetFolderPath (Environment.SpecialFolder.Personal), "tmp.pdf");
				NSError err;
				NSMutableData pdfData = new NSMutableData();

				view.InvokeOnMainThread (delegate {
					UITableView myView = view;
					myView.SizeToFit ();

					UIGraphics.BeginPDFContext (pdfData, new RectangleF(0,0,myView.ContentSize.Width, myView.ContentSize.Height), null); // myView.Bounds
					UIGraphics.BeginPDFPage ();
					
					// render the view completely by "scrolling" through and rendering the graphics layer each time
					PointF currentOffset = new PointF(0, 0);
					while (currentOffset.Y < myView.ContentSize.Height)
					{
						myView.Layer.RenderInContext (UIGraphics.GetCurrentContext ()); // OLD :: this.View.Layer.RenderInContext (UIGraphics.GetCurrentContext ());
						myView.SetContentOffset (new PointF(0f, currentOffset.Y + myView.Bounds.Height), false);
						currentOffset.Y += myView.Bounds.Height;
					}
					
					UIGraphics.EndPDFContent ();
					myView.SetContentOffset (new PointF(0,0), false);
				});

				// save the rendered context to disk
				pdfData.Save (tmpFileName, true, out err);

				if (err != null) return "";
				else return tmpFileName;
			}
			catch { return ""; }
		}
		
		public static bool PrintViewAsPdf(UITableView view)
		{
			// implemented :: was easy enough
			bool ok = (GetCurrentWiFiNetworkID () == "247");
			//if (! ok)
			//	return false;
			//else 
			{
				// 1. render the view in pdf context, then save the view as pdf file
				string tmpFileName = "";
				view.InvokeOnMainThread(delegate {
					UITableView myView = view;
					myView.SizeToFit ();
				
					NSMutableData pdfData = new NSMutableData();
					UIGraphics.BeginPDFContext (pdfData, new RectangleF(0,0,myView.ContentSize.Width, myView.ContentSize.Height), null); // myView.Bounds
					UIGraphics.BeginPDFPage ();

						// render the view completely by "scrolling" through and rendering the graphics layer each time
					PointF currentOffset = new PointF(0, 0);
					while (currentOffset.Y < myView.ContentSize.Height)
					{
						myView.Layer.RenderInContext (UIGraphics.GetCurrentContext ()); // OLD :: this.View.Layer.RenderInContext (UIGraphics.GetCurrentContext ());
						myView.SetContentOffset (new PointF(0f, currentOffset.Y + myView.Bounds.Height), false);
						currentOffset.Y += myView.Bounds.Height;
					}

					UIGraphics.EndPDFContent ();
					myView.SetContentOffset (new PointF(0,0), false);
						// save the rendered context to disk
					NSError err;
					tmpFileName = Path.Combine (Environment.GetFolderPath (Environment.SpecialFolder.Personal), "tmp.pdf");
					pdfData.Save (tmpFileName, true, out err);
					// 2. call PrintPDFFile
					ok = PrintPDFFile (tmpFileName);
					// 3. delete the created PDF file
					File.Delete (tmpFileName);
					// 4. return the result returned by PrintPDFFile()
				});
			}
			return ok;			
		}
		
		public static bool PrintPDFFile (string pdfFileName)
		{
			// Preparations for printing out the PDF through the Zebra mobile printer
			CGPDFDocument pdfDoc = CGPDFDocument.FromFile (pdfFileName);
			// Converting the first page of the PDF into image because apparently the Zebra printer has APIs to only print images, not PDFs
			UIImage pdfImage = MyConstants.ImageFromPDF(pdfDoc, 1);

			if (pdfDoc != null) { pdfDoc.Dispose (); pdfDoc = null; }
			
			// DEBUG
			MonoTouch.ObjCRuntime.Class.ThrowOnInitFailure = false;

			// Connecting to printer
			TcpPrinterConnection myConn;
			myConn = new TcpPrinterConnection("10.11.1.3", 6101);

			// myConn.Open ();
			bool connectionOK = myConn.Open();

			NSError err;
			if (connectionOK)
			{
				// Established connection, trying to send the image data through
				try {	
					// Creating an instance of Zebra printer class to access its graphical utility methods
					ZebraPrinterCpcl zprn = ZebraPrinterFactory.GetInstance(myConn, PrinterLanguage.PRINTER_LANGUAGE_CPCL);
					// Creating an instance of graphics utility class for the printer
					GraphicsUtilCpcl gu = zprn.GetGraphicsUtil();
					
					// The below 3 lines are sending a "SET FORM FEED" command to the printer, because otherwise it would leave a really big white space after printing pictures (up to 15 cm)
					string SETFF ="! U1 JOURNAL\r\n! U1 SETFF 50 5\r\n";
					NSData PaperFeedData = NSData.FromArray (System.Text.UTF8Encoding.UTF8.GetBytes (SETFF));
					myConn.Write (PaperFeedData, out err);
					
					// Attempt to print the image
					gu.printImage(pdfImage.CGImage, 0, 0, -1, -1, false, out err);
					if (err != null)
					{
						// Error when printing

						// Console.WriteLine (err.Description);
						// TODO :: alert the user here
						return false;
					}
				}	
				finally {
					myConn.Close ();
					if (pdfImage != null) pdfImage.Dispose ();
				}
			}

			else {
				// Could not establish connection with the printer
				// Console.WriteLine ("Could not establish connection with the printer.");
				return false;
			} 
			if (pdfImage != null) pdfImage.Dispose ();
			return true;
		}

		public static string GetCurrentWiFiNetworkID()
		{
			try 
			{
				string[] interfaces; 
				CaptiveNetwork.TryGetSupportedInterfaces (out interfaces);
				NSDictionary dictionary;
				CaptiveNetwork.TryCopyCurrentNetworkInfo (interfaces[0], out dictionary);
				string tmp = dictionary[CaptiveNetwork.NetworkInfoKeySSID].ToString ();
				return tmp;
			}
			catch
			{
				// TODO :: log the exception
				return String.Empty;
			}
		}
		
		public static Dictionary<int, string> GetFollowUpReasonsFromDB()
		{
			Dictionary<int, string> result = new Dictionary<int, string>();
			
			if (File.Exists (ServerClientViewController.dbFilePath) )
			{
				// read the data from database here
				using (var connection = new SqliteConnection("Data Source="+ServerClientViewController.dbFilePath) )
				{
					var cmd = connection.CreateCommand();
					connection.Open();
					string sql = 	"SELECT * FROM FU_REASONS WHERE ID > 2";
					cmd.CommandText = sql;
					using (var reader = cmd.ExecuteReader())
					{
						while ( reader.Read() )
						{
							int id = Convert.ToInt32 (reader["id"]);
							string reason = (string) reader["reason"];
							string code = (string) reader["code"];
							if (! (code.Contains ("INVOICE") || code.Contains("PAYMENT")) ) result.Add (id, reason);
						}
					}
				}
				return result;
			}
			else return null;
		}
		
		public static string OutputStringForValue(PaymentTypes pay)
		{
			switch(pay) {
			case PaymentTypes.Cash: return "Cash";
			case PaymentTypes.Cheque: return "Cheque";
			case PaymentTypes.CreditCard: return "Credit card to be drawn";
			case PaymentTypes.EFTPOS: return "EFT POS";
			case PaymentTypes.CCDetails: return "Credit card to be drawn";
			case PaymentTypes.Invoice : return "Invoice";
			case PaymentTypes.Split : return "Split"; // should never happen since split payment is essentially two payments for one job cluster
			//case PaymentTypes.MobileCreditCard: return "Mobile Credit Card";
			// case PaymentTypes.RefusedToPay : return "Refused to pay";
			case PaymentTypes.None: return "None"; // should only happen if the job did not involve any payment
			default: return "String not found in enumeration: PaymentTypes";
			}
		}
		
		public static string OutputCodeForPaymentType(PaymentTypes pay)
		{
			switch (pay)
			{
			case PaymentTypes.Cash:				return "CSH";
			case PaymentTypes.Cheque:			return "CHQ";
			case PaymentTypes.CreditCard:		return "CRC";
			case PaymentTypes.CCDetails:		return "CCD";
			case PaymentTypes.EFTPOS:			return "EFT";
			case PaymentTypes.Invoice:			return "IPO";
			case PaymentTypes.Split:				return "Split";
			case PaymentTypes.None:				return "None";
			default : return "String not found in enumeration: PaymentTypes";
			}
		}
		
		public static string OutputStringForValue(FollowUpsRequired srf)
		{
			switch (srf)
			{
			case FollowUpsRequired.ClientUnhappyWithPricing : return "Client unhappy with pricing";
			case FollowUpsRequired.ClientUnhappyWithService : return "Client unhappy with service";
			case FollowUpsRequired.Other : return "None of above";
			case FollowUpsRequired.OtherTechnicalIssues : return "Other tecchnical issues";
			case FollowUpsRequired.UnableToCompleteBookedJob : return "Unable to complete job";
			case FollowUpsRequired.UnableToOfferUpgradeOption1 : return "Unable to offer opt. 1 upgrade";
			case FollowUpsRequired.UnableToOfferUpgradeOption2 : return "Unable to offer opt. 2 upgrade";
			case FollowUpsRequired.WantsAQuoteOnExtras : return "Wants a quote on extras";
			case FollowUpsRequired.None : return "None"; // should never happen
			default : return "String not found in enumeration: ServiceFollowUpsRequired";
			}
		}
		
		public static List<string> BallValveProblemPointsOutputStrings()
		{
			List<string> result = new List<string>();
			foreach( int i in Enum.GetValues (typeof (BallValveProblemPointsEnum)))
			{
				string s = BallValveProblemPoints.OutputStringForValue( (BallValveProblemPointsEnum) i );
				result.Add (s);	
			}
			return result;
		}
		public static List<string> TeeAreaProblemPointsOutputStrings()
		{
			List<string> result = new List<string>();
			foreach( int i in Enum.GetValues (typeof (TeeAreaProblemPointsEnum)))
			{
				string s = TeeAreaProblemPoints.OutputStringForValue( (TeeAreaProblemPointsEnum) i );
				result.Add (s);	
			}
			return result;
		}
		public static List<string> InletTubingProblemPointsOutputStrings()
		{
			List<string> result = new List<string>();
			foreach( int i in Enum.GetValues (typeof (InletTubingProblemPointsEnum)))
			{
				string s = InletTubingProblemPoints.OutputStringForValue( (InletTubingProblemPointsEnum) i );
				result.Add (s);	
			}
			return result;
		}
		public static List<string> UnitProblemPointsOutputStrings()
		{
			List<string> result = new List<string>();
			foreach( int i in Enum.GetValues (typeof (UnitProblemPointsEnum)))
			{
				string s = UnitProblemPoints.OutputStringForValue( (UnitProblemPointsEnum) i );
				result.Add (s);	
			}
			return result;
		}
		public static List<string> OutletTubingProblemPointsOutputStrings()
		{
			List<string> result = new List<string>();
			foreach( int i in Enum.GetValues (typeof (OutletTubingProblemPointsEnum)))
			{
				string s = OutletTubingProblemPoints.OutputStringForValue( (OutletTubingProblemPointsEnum) i );
				result.Add (s);	
			}
			return result;
		}
		public static List<string> TapProblemPointsOutputStrings()
		{
			List<string> result = new List<string>();
			foreach( int i in Enum.GetValues (typeof (TapProblemPointsEnum)))
			{
				string s = TapProblemPoints.OutputStringForValue( (TapProblemPointsEnum) i );
				result.Add (s);	
			}
			return result;
		}
		public static List<string> AllProblemPointsOutputStrings()
		{
			List<string> result = new List<string>();
			foreach( int i in Enum.GetValues (typeof (ExactPointsEnum)))
			{
				string s = ExactProblemPoints.OutputStringForValue( (ExactPointsEnum) i );
				result.Add (s);	
			}
			return result;
		}

		/*
		 * Saving coordinates to database like this leads to data file corruption since it was used by several different concurrent threads. Rewritten the logic, implemented a buffer to hold the locations
		 * The buffer is managed by LocDelegate class in AppDelegate.cs
		 * 
		public static void SaveDeviceCoordinates(DeviceLocation loc)
		{
			MyConstants.SaveDeviceCoordinates (loc.Lng, loc.Lat, loc.Timestamp, loc.Address);
		}

		public static void SaveDeviceCoordinates(double lng, double lat, string time, string address)
		{
			if ( File.Exists(ServerClientViewController.dbFilePath) && Math.Abs (lng) > 0.1 && Math.Abs (lat) > 0.1 )
			{
				using (var connection = new SqliteConnection("Data Source="+ServerClientViewController.dbFilePath) )
				{
					try {
						connection.Open();
						using (var cmd = connection.CreateCommand() )
						{
							cmd.CommandText = (String.IsNullOrEmpty (address)) ? "INSERT INTO IPAD_COORDS (empl_oid, timestamp, lng, lat, customer) VALUES (:employee_id, :time, :lng, :lat, :customer)" : 
																												"INSERT INTO IPAD_COORDS (empl_oid, timestamp, lng, lat, address, customer) VALUES (:employee_id, :time, :lng, :lat, :address, :customer)";

							cmd.Parameters.Add ("employee_id", System.Data.DbType.Int32).Value = EmployeeID;
							cmd.Parameters.Add ("time", System.Data.DbType.String).Value = time; // DateTime.Now.ToString ("yyyy-MM-dd HH:mm:ss");
							cmd.Parameters.Add ("lng", System.Data.DbType.Double).Value = lng;
							cmd.Parameters.Add ("lat", System.Data.DbType.Double).Value = lat;
							cmd.Parameters.Add ("customer", System.Data.DbType.Int64).Value = (MyConstants._jrt.CurrentCustomer == null) ? 0 : MyConstants._jrt.CurrentCustomer.CustomerNumber;

							if (!String.IsNullOrEmpty (address))
								cmd.Parameters.Add ("address", System.Data.DbType.String).Value = address;

							cmd.ExecuteNonQuery ();
						}
					}
					catch 
					{

					} // catch
				} // using (var connection = new SqliteConnection("Data Source="+ServerClientViewController.dbFilePath) )
			}
		}
		*/
	}

	/*
	 * Purifier Outlet jaco :
	 * 		Jaco Nut (loose & leaking)
	 * 		Jaco bolt (leaking thread seal)
	 * 		Tubing pulled out
	 * 		Broken
	 * 		Corroded
	 * 		
	 * Outlet tubing :
	 * 		Split
	 * 		UV damage (discolored)
	 * 		Tubing pulled out -- choose jaco or shank olive
	 * 		Corroded
	 * 		Surface rough or cut
	 * 
	 * 	
	 * 
	 * */

	
	public enum BallValveProblemPointsEnum { BallValveValve, BallValveAreaTJoin, BallValveHandle, BallValveBPVJoin, BPVReducingNippleJoin, ReducingNippleAmberJacoJoin, AmberJacoWhiteJacoJoin, None }
	public class 	BallValveProblemPoints {
		public static string OutputStringForValue(BallValveProblemPointsEnum val)
		{
			switch (val)
			{
			case BallValveProblemPointsEnum.AmberJacoWhiteJacoJoin : return "Amber jaco <-> White jaco join";
			case BallValveProblemPointsEnum.BallValveAreaTJoin : return "Tee join";
			case BallValveProblemPointsEnum.BallValveBPVJoin : return "BPV join";
			case BallValveProblemPointsEnum.BallValveHandle : return "Ball valve handle";
			case BallValveProblemPointsEnum.BallValveValve : return "The valve itself";
			case BallValveProblemPointsEnum.BPVReducingNippleJoin : return "BPV <-> Reducing nipple join";
			case BallValveProblemPointsEnum.ReducingNippleAmberJacoJoin : return "Reducing nipple <-> Amber jaco join";
			case BallValveProblemPointsEnum.None : return "None";
				default : { 
					string msg = "Error : BallValveProblemPoints : String not found in enumeration : " + val.ToString ();
					throw new NotImplementedException(msg);
				}
			}
		}
	}

	public enum TeeAreaProblemPointsEnum {  KincoFeed, KincoOutlet, TAreaBallValveTJoin, Tee, FlexibleLine, BrassJaco, BrassJacoOlive, QuarterInchCopper, None }
	public class TeeAreaProblemPoints {
		public static string OutputStringForValue(TeeAreaProblemPointsEnum val)
		{
			switch(val)
			{
		case TeeAreaProblemPointsEnum.BrassJaco : return "Brass jaco";
		case TeeAreaProblemPointsEnum.BrassJacoOlive : return "Brass jaco olive";
		case TeeAreaProblemPointsEnum.FlexibleLine : return "Flexible line";
		case TeeAreaProblemPointsEnum.KincoFeed : return "Kinco nut (feed)";
		case TeeAreaProblemPointsEnum.KincoOutlet : return "Kinco nut (outlet)";
		case TeeAreaProblemPointsEnum.QuarterInchCopper : return "Quarter inch copper";
		case TeeAreaProblemPointsEnum.TAreaBallValveTJoin : return "Ball valve join";
		case TeeAreaProblemPointsEnum.Tee : return "Tee itself";
		case TeeAreaProblemPointsEnum.None : return "None";
				default : { 
					string msg = "Error : TeeAreaProblemPoints : String not found in enumeration : " + val.ToString ();
					throw new NotImplementedException(msg);
				}
			}
		}
	}
	public enum InletTubingProblemPointsEnum { InletTubingPurifierJaco, InletTubingBallValveJaco, InletTubingLength, None }
	public class InletTubingProblemPoints {
		public static string OutputStringForValue(InletTubingProblemPointsEnum val)
		{
			switch(val)
			{
			case InletTubingProblemPointsEnum.InletTubingBallValveJaco : return "Ball valve jaco";
			case  InletTubingProblemPointsEnum.InletTubingLength : return "Inlet tubing";
			case InletTubingProblemPointsEnum.InletTubingPurifierJaco : return "Purifier jaco";
			case InletTubingProblemPointsEnum.None : return "None";
				default : { 
					string msg = "Error : InletTubingProblemPoints : String not found in enumeration : " + val.ToString ();
					throw new NotImplementedException(msg);
				}				
			}
		}
	}
	public enum UnitProblemPointsEnum { BodyOutletJaco, OutletSump, CarbonFilter, BodyHead, InletSump, SedimentFilter, BodyInletJaco, None }
	public class UnitProblemPoints {
		public static string OutputStringForValue(UnitProblemPointsEnum val)
		{
			switch(val)
			{
			case UnitProblemPointsEnum.BodyHead : return "Purifier head";
			case UnitProblemPointsEnum.BodyInletJaco : return "Inlet jaco";
			case UnitProblemPointsEnum.BodyOutletJaco : return "Outlet jaco";
			case UnitProblemPointsEnum.CarbonFilter : return "Carbon filter";
			case UnitProblemPointsEnum.InletSump : return "Inlet sump";
			case UnitProblemPointsEnum.None : return "None";
			case UnitProblemPointsEnum.OutletSump : return "Outlet sump";
			case UnitProblemPointsEnum.SedimentFilter : return "Sediment filter";
				default : { 
					string msg = "Error : UnitProblemPoints : String not found in enumeration : " + val.ToString ();
					throw new NotImplementedException(msg);
				}					
			}
		}
	}
	public enum OutletTubingProblemPointsEnum { OutletTubingLength, OutletTubingOutletJaco, TubeShankNut, None }
	public class OutletTubingProblemPoints {
		public static string OutputStringForValue(OutletTubingProblemPointsEnum val)
		{
			switch(val)
			{
			case OutletTubingProblemPointsEnum.OutletTubingLength : return "Outlet tubing";
			case OutletTubingProblemPointsEnum.OutletTubingOutletJaco : return "Purifier outlet jaco";
			case OutletTubingProblemPointsEnum.TubeShankNut : return "Shank nut";
			case OutletTubingProblemPointsEnum.None : return "None";
				default : { 
					string msg = "Error : OutletTubingProblemPoints : String not found in enumeration : " + val.ToString ();
					throw new NotImplementedException(msg);
				}				
			}
		}
	}
	public enum TapProblemPointsEnum { TapSpout, TapHandle, TapBase, TapShank, None }
	public class TapProblemPoints {
		public static string OutputStringForValue(TapProblemPointsEnum val)
		{
			switch(val)
			{
			case TapProblemPointsEnum.TapHandle : return "Tap handle";
			case TapProblemPointsEnum.TapShank : return "Shank";
			case TapProblemPointsEnum.TapSpout : return "Tap spout";
			case TapProblemPointsEnum.TapBase : return "Tap base";
			case TapProblemPointsEnum.None : return "None";
				default : { 
					string msg = "Error : TapProblemPoints : String not found in enumeration : " + val.ToString ();
					throw new NotImplementedException(msg);
				}
			}
		}
	}

	public enum ExactPointsEnum { 
		BallValveValve, BallValveAreaTJoin, BallValveHandle, BallValveBPVJoin, BPVReducingNippleJoin, ReducingNippleAmberJacoJoin, AmberJacoWhiteJacoJoin, // BallValveArea
		KincoFeed, KincoOutlet, TAreaBallValveTJoin, Tee, FlexibleLine, BrassJaco, BrassJacoOlive, QuarterInchCopper, 		// T area
		InletTubingPurifierJaco, InletTubingBallValveJaco, InletTubingLength, 																	// Inlet tubing area
		BodyOutletJaco, OutletSump, CarbonFilter, BodyHead, InletSump, SedimentFilter, BodyInletJaco, 							// Purifier area
		OutletTubingOutletJaco, OutletTubingLength, TubeShankNut,  																				// Outlet tubing area
		TapSpout, TapHandle, TapBase, TapShank,  																										// Tap area 
		
		None }
	public class ExactProblemPoints {
		public static string OutputStringForValue(ExactPointsEnum val)
		{
			foreach( int i in Enum.GetValues (typeof (TeeAreaProblemPointsEnum) ) )
			{
				if ( val.ToString () == ( (TeeAreaProblemPointsEnum) i ).ToString() )
					return TeeAreaProblemPoints.OutputStringForValue ( (TeeAreaProblemPointsEnum)i );
			}
			foreach( int i in Enum.GetValues (typeof (BallValveProblemPointsEnum) ) )
			{
				if ( val.ToString () == ( (BallValveProblemPointsEnum) i ).ToString() )
					return BallValveProblemPoints.OutputStringForValue ( (BallValveProblemPointsEnum)i );
			}
			
			foreach( int i in Enum.GetValues (typeof (InletTubingProblemPointsEnum) ) )
			{
				if ( val.ToString () == ( (InletTubingProblemPointsEnum) i ).ToString() )
					return InletTubingProblemPoints.OutputStringForValue ( (InletTubingProblemPointsEnum)i );
			}
			
			foreach( int i in Enum.GetValues (typeof (UnitProblemPointsEnum) ) )
			{
				if ( val.ToString () == ( (UnitProblemPointsEnum) i ).ToString() )
					return UnitProblemPoints.OutputStringForValue ( (UnitProblemPointsEnum)i );
			}
			
			foreach( int i in Enum.GetValues (typeof (OutletTubingProblemPointsEnum) ) )
			{
				if ( val.ToString () == ( (OutletTubingProblemPointsEnum) i ).ToString() )
					return OutletTubingProblemPoints.OutputStringForValue ( (OutletTubingProblemPointsEnum)i );
			}

			foreach( int i in Enum.GetValues (typeof (TapProblemPointsEnum) ) )
			{
				if ( val.ToString () == ( (TapProblemPointsEnum) i ).ToString() )
					return TapProblemPoints.OutputStringForValue ( (TapProblemPointsEnum)i );
			}
			
			string msg = "Error : ExactProblemPoints : String not found in enumeration : " + val.ToString ();
			throw new NotImplementedException(msg);
		}
	}

	public enum GeneralProblemTypesEnum { Loose, Broken, Corroded, NoneOfAbove, None } /* UnitHungOutside, BadWaterTaste, CarbonInWater,*/  // has been rewritten as a class implemeting a method OutputString() to get string representation of the choice made
	public enum TapSpoutProblemTypesEnum { Loose, Broken, Corroded, LeakingFromORings, NoneOfAbove, None }
	public enum TapHandleProblemTypesEnum { Loose, Broken, Corroded, LeakingPastORings, NoneOfAbove, None }
	public enum TapBaseProblemTypesEnum { Loose, LeakingPastSeatOrCeramicDisk, NoneOfAbove, None }
	public enum ShankProblemTypesEnum { Loose, Broken, Corroded, LeakingPastSeal, NoneOfAbove, None }

	public enum PossibleActionsEnum { Replaced, Refitted, NoActionTaken, None } /* ReplacedUnderWarranty, RefittedUnderWarranty, */ 	// has been rewritten as a class implemeting a method OutputString() to get string representation of the choice made
	public enum GeneralAreasEnum { BallValve, Tee, InletTubing, Purifier, OutletTubing, Tap, None }
	public enum JobServiceCallFilterTapType { GI2500, GI2600, Standard, Imperial, Mark2, None }
	public enum JobServiceCallChoices { Yes, No, Clean, Normal, Dirty, None }
	public enum FilterChangeTypesEnum { Standard, NonStandard, None }

	public partial class GetChoicesForObject : UIActionSheet
	{
		//This has been rewritten to accept objects on creation, e. g. a PossibleActions object
		// It then creates an ActionSheet with buttons corresponding to each of the possible choices automatically
		// and also sets up its own WillDismiss event handler which will change value of passed parameter according to that parameter's type (problem point, problem type, action taken)
		
		public GetChoicesForObject(string title, UIActionSheetDelegate del, string cancelTitle, string destroy, params string[] other) : base(title, del, cancelTitle, destroy, other) { } //  inherited constructor from base class
		
		~GetChoicesForObject()		// this is the finalizer implemetation to test if this thing gets disposed of properly (no memory leaks)
		{
			// Console.WriteLine ("GetChoicesForObject: finalized!");
		}
		
		// implementation of the class constructor for FollowUpsRequuired
		public GetChoicesForObject(string title, FollowUpsRequired fur) : base (title) {
			FollowUpsRequired tmp;
			foreach(int i in Enum.GetValues (typeof(FollowUpsRequired) ) )
			{
				tmp = (FollowUpsRequired) i;
				if (tmp != FollowUpsRequired.None)
					this.AddButton ( MyConstants.OutputStringForValue (tmp) );
			}
			this.WillDismiss += delegate(object sender, UIButtonEventArgs e) {
				if (e.ButtonIndex!=-1)
					fur = (FollowUpsRequired) e.ButtonIndex;
			};
		}
		
		// implementation of the class constructor for InstallationType
		/*public GetChoicesForObject(string title, InstallationTypes inst) : base(title)	
		{
			InstallationTypesEnum tmp;
			foreach(int i in Enum.GetValues (typeof(InstallationTypesEnum) ) )
			{
				tmp = (InstallationTypesEnum) i;
				if (tmp != InstallationTypesEnum.None) 
				{
					this.AddButton ( inst.OutputStringForValue (tmp) );
				}
			}
			
			this.WillDismiss += delegate(object sender, UIButtonEventArgs e) {
				if (e.ButtonIndex != -1)
				{
					inst.Type = (InstallationTypesEnum) e.ButtonIndex;
				}
			};
		}*/
		
		// implementation of the class constructor for PaymentType
		public GetChoicesForObject(string title, PaymentTypes pay) : base(title) {
			PaymentTypes tmp;
			foreach(int i in Enum.GetValues (typeof(PaymentTypes)))
			{
				tmp = (PaymentTypes) i;
				if (tmp!=PaymentTypes.None && 
				    tmp!=PaymentTypes.Invoice) // && tmp!=PaymentTypes.RefusedToPay)
				     { this.AddButton ( MyConstants.OutputStringForValue(tmp) ); }
			}
		}
		
	
		// implementation of the class constructor for FilterChangeType
		public GetChoicesForObject(string title, FilterChangeTypes fct) : base(title) {
			FilterChangeTypesEnum tmp;
			foreach (int i in Enum.GetValues (typeof(FilterChangeTypesEnum) ) )
			{
				tmp = (FilterChangeTypesEnum) i;
				if ( tmp != FilterChangeTypesEnum.None )
				{
					this.AddButton ( fct.OutputStringForValue(tmp) );
				}
			}
			
			this.WillDismiss += delegate(object sender, UIButtonEventArgs e) {
				if (e.ButtonIndex != -1)
				{
					fct.Type = (FilterChangeTypesEnum) e.ButtonIndex;
				}
			};
		}
				
		// implementation for PossibleActions 
		public GetChoicesForObject(string title, PossibleActions pac, JobServiceCallViewController.ProblemsDialogViewController dvc) : base(title) {
			PossibleActionsEnum tmp;
			foreach(int i in Enum.GetValues( typeof( PossibleActionsEnum ) ) )
			{
				tmp = (PossibleActionsEnum) i;
				if (tmp != PossibleActionsEnum.None) 
					this.AddButton ( pac.OutputStringForValue (tmp) );
			}
			
			this.WillDismiss += delegate(object sender, UIButtonEventArgs e) {
				if (e.ButtonIndex != -1) 
					pac.Action = (PossibleActionsEnum) e.ButtonIndex;
				dvc.ReloadDataAndCheck ();
			};
		}
	
		// implementation for ProblemTypes 
		public GetChoicesForObject(string title, ProblemTypes prt) : base (title) {
			GeneralProblemTypesEnum tmp;
			foreach (int i in Enum.GetValues (typeof(GeneralProblemTypesEnum)))
			{
				tmp = (GeneralProblemTypesEnum) i;
				if (tmp != GeneralProblemTypesEnum.None)
					this.AddButton(prt.OutputStringForValue (tmp));
			}
			
			this.WillDismiss += delegate(object sender, UIButtonEventArgs e) {
				if (e.ButtonIndex != -1)
					prt.Type = (GeneralProblemTypesEnum) e.ButtonIndex;
					// ? some sort of feedback depending on the choice made (updating interface, etc.) ?
			};
		}

		// implementation for ProblemPoints			
		public GetChoicesForObject(string title, ProblemPoints prp) : base(title) {
			switch (prp.Point.GeneralArea.Area)
			{
				case GeneralAreasEnum.BallValve : {
					foreach ( int i in Enum.GetValues( typeof(ExactPointsEnum) ) )
					{
						ExactPointsEnum tmp = (ExactPointsEnum) i;
						if (tmp != ExactPointsEnum.None) {
							string stmp = ExactPoints.OutputStringForValue (tmp);
							var list = MyConstants.BallValveProblemPointsOutputStrings();
							if (list.Contains( stmp ))	this.AddButton ( stmp );
						}
					}
					break; 
				}
				case GeneralAreasEnum.Tee : { 
					foreach ( int i in Enum.GetValues( typeof(ExactPointsEnum) ) )
					{
						ExactPointsEnum tmp = (ExactPointsEnum) i;
						if (tmp != ExactPointsEnum.None) {
							string stmp = ExactPoints.OutputStringForValue (tmp);
							var list = MyConstants.TeeAreaProblemPointsOutputStrings();
							if (list.Contains( stmp ))	this.AddButton ( stmp );
						}
					}
					break; 
				}
				case GeneralAreasEnum.InletTubing : { 
					foreach ( int i in Enum.GetValues( typeof(ExactPointsEnum) ) )
					{
						ExactPointsEnum tmp = (ExactPointsEnum) i;
						if (tmp != ExactPointsEnum.None) {
							string stmp = ExactPoints.OutputStringForValue (tmp);
							var list = MyConstants.InletTubingProblemPointsOutputStrings();
							if (list.Contains( stmp ))	this.AddButton ( stmp );
						}
					}
					break; 
				}
				case GeneralAreasEnum.Purifier : { 
					foreach ( int i in Enum.GetValues( typeof(ExactPointsEnum) ) )
					{
						ExactPointsEnum tmp = (ExactPointsEnum) i;
						if (tmp != ExactPointsEnum.None) {
							string stmp = ExactPoints.OutputStringForValue (tmp);
							var list = MyConstants.UnitProblemPointsOutputStrings();
							if (list.Contains( stmp ))	this.AddButton ( stmp );
						}
					}
					break; 
				}
				case GeneralAreasEnum.OutletTubing : {
					var list = MyConstants.OutletTubingProblemPointsOutputStrings();
					foreach ( int i in Enum.GetValues( typeof(ExactPointsEnum) ) )
					{
						ExactPointsEnum tmp = (ExactPointsEnum) i;
						if (tmp != ExactPointsEnum.None) {
							string stmp = ExactPoints.OutputStringForValue (tmp);
							if (list.Contains( stmp ))	this.AddButton ( stmp );
						}
					}
					break; 
				}
				case GeneralAreasEnum.Tap : { 
					foreach ( int i in Enum.GetValues( typeof(ExactPointsEnum) ) )
					{
						ExactPointsEnum tmp = (ExactPointsEnum) i;
						if (tmp != ExactPointsEnum.None) {
							string stmp = ExactPoints.OutputStringForValue (tmp);
							var list = MyConstants.TapProblemPointsOutputStrings();
							if (list.Contains( stmp ))	this.AddButton ( stmp );
						}
					}
					break; 
				}
			} // end switch
			
			this.WillDismiss += delegate(object sender, UIButtonEventArgs e) {
				if (e.ButtonIndex != -1) {
					ExactPointsEnum tmp = ExactPointsEnum.None;
					
					foreach ( int i in Enum.GetValues (typeof(ExactPointsEnum)))
					{
						if (ExactPoints.OutputStringForValue( (ExactPointsEnum) i ) == this.ButtonTitle (e.ButtonIndex) ) {
							tmp = (ExactPointsEnum) i;
						}
					}
					
					foreach ( int j in Enum.GetValues (typeof(ExactPointsEnum)) )
					{
						if ( (ExactPointsEnum) j == tmp )
							prp.Point.ExactPoint.Point = (ExactPointsEnum) j;
					}
				}
			};
		}
	}
			
		public class ProblemTypes {
			public GeneralProblemTypesEnum Type { get; set; }
			public string OutputString ()
			{
				switch(this.Type)
				{
				//	case ProblemTypesEnum.BadWaterTaste : return "Bad water taste";
				// case ProblemTypesEnum.CarbonInWater : return "Carbon in water";
				// case ProblemTypesEnum.UnitHungOutside : return "Unit hung outside house";
				// case GeneralProblemTypesEnum.ChemicalAttack : return "Chemical attack";
				// case GeneralProblemTypesEnum.Split : return "Split";
				case GeneralProblemTypesEnum.NoneOfAbove : return "Other (see comments)";
				case GeneralProblemTypesEnum.Broken : return "Broken";
				case GeneralProblemTypesEnum.Corroded : return "Corroded";
				case GeneralProblemTypesEnum.Loose : return "Loose";
				case GeneralProblemTypesEnum.None : return "None";
				default : return this.Type.ToString ();
				}
			}
			
			public string OutputStringForValue(GeneralProblemTypesEnum val)
			{
				switch(val)
				{
				// case ProblemTypesEnum.BadWaterTaste : return "Bad water taste";
				// case ProblemTypesEnum.CarbonInWater : return "Carbon in water";
				// case ProblemTypesEnum.UnitHungOutside : return "Unit hung outside house";
				// case GeneralProblemTypesEnum.ChemicalAttack : return "Chemical attack";
				// case GeneralProblemTypesEnum.Split : return "Split";
				case GeneralProblemTypesEnum.NoneOfAbove : return "None of above";
				case GeneralProblemTypesEnum.Broken : return "Broken";
				case GeneralProblemTypesEnum.Corroded : return "Corroded";
				case GeneralProblemTypesEnum.Loose : return "Loose";
				case GeneralProblemTypesEnum.None : return "None";
				default : return this.Type.ToString ();
				}
			}
		}
		
		public class PossibleActions {
			public PossibleActionsEnum Action { get; set; }
			public string OutputString ()
			{
				switch (this.Action)
				{
				case PossibleActionsEnum.NoActionTaken : return "No action taken";
				// case PossibleActionsEnum.RefittedUnderWarranty : return "Refitted under warranty";
				// case PossibleActionsEnum.ReplacedUnderWarranty : return "Replaced under warranty";
				case PossibleActionsEnum.None : return "Not chosen";
				case PossibleActionsEnum.Refitted : return "Refitted";
				case PossibleActionsEnum.Replaced : return "Replaced";
				default : return this.Action.ToString ();
				}
			}
			public string OutputStringForValue(PossibleActionsEnum val)
			{
				switch (val)
				{
				case PossibleActionsEnum.NoActionTaken : return "No action taken";
				case PossibleActionsEnum.None : return "Not chosen";
				// case PossibleActionsEnum.RefittedUnderWarranty : return "Refitted under warranty";
				// case PossibleActionsEnum.ReplacedUnderWarranty : return "Replaced under warranty";
				default : return val.ToString ();
				}				
			}
		}

		public class GeneralAreas {
			public GeneralAreasEnum Area = GeneralAreasEnum.None;
			public string OutputString()
			{
				switch(this.Area)
				{
				case GeneralAreasEnum.BallValve : return "Ball valve";
				case GeneralAreasEnum.InletTubing : return "Inlet tubing";
				case GeneralAreasEnum.OutletTubing : return "Outlet tubing";
				default : return this.Area.ToString ();
				}
			}
			public string OutputStringForValue(GeneralAreasEnum val)
			{
				switch(val)
				{
				case GeneralAreasEnum.BallValve : return "Ball valve";
				case GeneralAreasEnum.InletTubing : return "Inlet tubing";
				case GeneralAreasEnum.OutletTubing : return "Outlet tubing";
				default : return val.ToString ();
				}				
			}
		}
		
		public class ExactPoints {
			public ExactPointsEnum Point { get; set; }
			public string OutputString()
			{
				return ExactPoints.OutputStringForValue (this.Point);
			}
			public static string OutputStringForValue(ExactPointsEnum val)
			{
				return ExactProblemPoints.OutputStringForValue (val);
			}
		}
	
		public class ProblemPointsHierarchy {
			public GeneralAreas GeneralArea;
			public ExactPoints ExactPoint;
			
			public ProblemPointsHierarchy()
			{ 
				this.GeneralArea = new GeneralAreas(); 
				this.ExactPoint = new ExactPoints();
			}
		}

		public class ProblemPoints {
			public ProblemPointsHierarchy Point { get; set; }
			public string OutputString ()
			{
				return this.Point.GeneralArea.OutputString() + ": " + this.Point.ExactPoint.OutputString();
			}

			public static string OutputString(ProblemPointsHierarchy prh)
			{
				return prh.GeneralArea.OutputString () + ": " + prh.ExactPoint.OutputString ();
			}
		}
	public class Employee 
	{
		public string FirstName;
		public string LastName;
		public string FullName;
		public Guid DeviceGuid;
		public MyConstants.EmployeeTypes EmployeeType;
		public int EmployeeID;
	}

	public class DeviceLocation
	{
		public double Lat { get; set; }
		public double Lng {get; set; }
		public string Address { get; set; }
		public string Timestamp { get; set; }
		public long CustomerID { get; set; }
		public long JobID { get; set; }
	}
	
	public static class IDictionaryExtensions
    {
        public static TKey FindKeyByValue<TKey, TValue>(this IDictionary<TKey, TValue> dictionary, TValue value)
        {
            if (dictionary == null)
                throw new ArgumentNullException("dictionary");

            foreach (KeyValuePair<TKey, TValue> pair in dictionary)
                if (value.Equals(pair.Value)) return pair.Key;

            throw new Exception("the value is not found in the dictionary");
        }
    }

	public class LoadingView : UIAlertView
	{
		private UIActivityIndicatorView _activityView;
		
		public void Show(string title)
		{		
			InvokeOnMainThread(delegate() {
				_activityView = new UIActivityIndicatorView(UIActivityIndicatorViewStyle.WhiteLarge);
				_activityView.Frame = new System.Drawing.RectangleF(122,50,40,40);
				AddSubview(_activityView);
				
				Title = title;
				_activityView.StartAnimating();
				UIApplication.SharedApplication.NetworkActivityIndicatorVisible = true;			
				this.Show();
			});
		}
		
		public void Hide()
		{
			InvokeOnMainThread(delegate() {
				UIApplication.SharedApplication.NetworkActivityIndicatorVisible = false;
				DismissWithClickedButtonIndex(0, true);
			});
		}
	}
}

