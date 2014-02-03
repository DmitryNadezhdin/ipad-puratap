using System;
using System.IO;
using System.Data;
using System.Linq;
using System.Drawing;
using System.Threading;
using System.Collections.Generic;
using System.Collections.ObjectModel;

using MonoTouch.UIKit;
using Mono.Data.Sqlite;
using MonoTouch.Foundation;
using MonoTouch.ObjCRuntime;

namespace Puratap
{
	// [Adopts ("UIViewControllerRestoration")] // this attribute is needed if we are to implement state restoration through storyboard interface design
	public class NewJobAlertViewDelegate : UIAlertViewDelegate 
	{
		 
	}

	public class JobRunTable : UITableViewController 
	{
		// IMPLEMENTED:: replace all Constants.DEBUG_TODAY strings with proper parameter (today or tomorrow or whatever)
		private bool _highlightedMode;
		private NSIndexPath _lastSelectedRowPath;
		
		public JobRunTableSource _ds { get; set; }
		public DetailedTabs _tabs { get; set; }
		public bool HighlightedMode {
			get { return _highlightedMode; }	
			set { _highlightedMode = value; }
		}
		
		public NSIndexPath LastSelectedRowPath {
			get { return _lastSelectedRowPath; }
			set { _lastSelectedRowPath = value; }
		}
		
		private bool _allJobsDone = false;
		public bool AllJobsDone {
			get { return _allJobsDone; }
			set { _allJobsDone = value; 
				if (_allJobsDone) {
					using (var a = new UIAlertView("Congratulations", "All jobs done, yay!", null, "OK") ) {
						a.Show ();
						_tabs._app.myLocationManager.StopUpdatingLocation ();
						_tabs._app.myLocationManager.StopMonitoringSignificantLocationChanges ();
					}
				}
			}
		}

		/* // this binding is needed if we are to implement state restoration through storyboard interface design
		[Export ("viewControllerWithRestorationIdentifierPath:")]
		static UIViewController FromIdentifierPath (string [] identifierComponents, NSCoder coder)
		{
			var sb = (UIStoryboard) coder.DecodeObject (UIStateRestoration.ViewControllerStoryboardKey);
			if (sb != null){
				var vc = (JobRunTable) sb.InstantiateViewController ("JobRunTable");
				vc.RestorationIdentifier = identifierComponents [identifierComponents.Length-1];
				// vc.RestorationClass = Class.GetHandle (typeof (JobRunTable));

				return vc;
			}
			else return new JobRunTable(UITableViewStyle.Grouped);
		}
		*/



		private List<Customer> _customers;
		public List<Customer> Customers { get { return _customers;  } set { _customers = value; } }
		
		private List<Job> _mainjoblist;
		public List<Job> MainJobList { get { return _mainjoblist; } set { _mainjoblist = value; } }
		
		// List of Additional Jobs, displayed in section 1, added to by a tapping the "Tap this to add new job"
		// Customer Number required to add a new job
		
		private Job _currentJob;
		public Job CurrentJob { get { return _currentJob; } set { _currentJob = value; } }
		
		private Customer _currentCustomer;
		public Customer CurrentCustomer { get { return _currentCustomer; } set { _currentCustomer = value; } }
		
		public List<Job> UserCreatedJobs { get; set; }
		public List<Customer> UserAddedCustomers { get; set; }
		
		public static List<JobType> JobTypes { get; set; }		

		public JobRunTable(UITableViewStyle style) : base(style) // needed to create a grouped style table
		{
			_ds = new JobRunTableSource(this);

			MyConstants._jrt = this;
		}

		[Obsolete]
		public override bool ShouldAutorotateToInterfaceOrientation (UIInterfaceOrientation toInterfaceOrientation)
		{
			return true; // (toInterfaceOrientation == UIInterfaceOrientation.LandscapeLeft || toInterfaceOrientation == UIInterfaceOrientation.LandscapeRight);
		}


		
		public Job FindParentJob(Job j)
		{
			bool found = false;
			if (j.HasNoParent ()) return null;
			else {
				foreach (Job main in MainJobList)
				{
					if (main.JobBookingNumber == j.ParentJobBookingNumber)
					{
						found = true;
						return main;
					}
				}
				if (!found)
				{
					foreach (Job job in UserCreatedJobs)
					{
						if (job.JobBookingNumber == j.ParentJobBookingNumber)
						{
							found = true;
							return job;
						}
					}
				}
			}
			_tabs._scView.Log (String.Format ("FindParentJob: Parent job not found for job ID: {0}", j.JobBookingNumber));
			return null;
		}
		
		public void CreateNewJob()
		{
			var newJobAlert = new UIAlertView("Please enter customer number", "", null, "Cancel", "OK");
			newJobAlert.AlertViewStyle = UIAlertViewStyle.PlainTextInput;
			newJobAlert.Dismissed += HandleNewJobAlertDismissed;
			newJobAlert.Show ();
		}

		void HandleNewJobAlertDismissed (object sender, UIButtonEventArgs e)
		{
			UIAlertView newJobAlert = sender as UIAlertView;
			if (e.ButtonIndex != newJobAlert.CancelButtonIndex)
			{
				// WAS :: string input = (newJobAlert.Subviews[6] as UITextField).Text; -- THIS LINE CAUSES A CRASH ON iOS 7
				string input = newJobAlert.GetTextField(0).Text;

				int result = -1;
				bool ok = int.TryParse (input, out result);
				if (! ok) {
					// user entered rubbish, nothing to do
					var alert = new UIAlertView("", "Cannot parse input as a number. Please try again", null, "OK");
					alert.Show ();
					return;
				}
				else // managed to parse the input number
				{						
					// create a new customer with the number entered (check the number being unique against a list of existing customers) and save him to WCLIENT
					// check if a customer with this number already exists in the database
					// if it exists, the user should have added to his job cluster, show a message explaining that
					Customer c = new Customer(result, "Mr.", "FirstName", "LastName", "Address", "Suburb", 
									      					String.Format("({0}){1}", "000", "000-0000"), 
									                        String.Format("({0}){1}", "000", "000-0000"), DateTime.Now, 
					                          				"", "", 0, "", true, 0, 0);
					c.JobHistory = new List<HistoryJob>();
					c.CustomerMemos = new List<Memo>();
					
					if ( CustomerExistsInDB (c.CustomerNumber) ) 
					{
						var CustomerExistsAlert = new UIAlertView("Warning", "Customer with this number is already on the run. Are you sure you want to add another record?", null, "Cancel", "Add");
						CustomerExistsAlert.Dismissed += delegate(object send, UIButtonEventArgs ea) {
							if (ea.ButtonIndex != CustomerExistsAlert.CancelButtonIndex)
							{
								// overwrite
								// OverwriteCustomerRecord (c);
								bool found = false;
								foreach(Customer cust in this.Customers)
								{
									if (cust.CustomerNumber==c.CustomerNumber)
									{
										found = true;
										c.Title = cust.Title;
										c.FirstName = cust.FirstName;
										c.LastName = cust.LastName;
										c.Address = cust.Address;
										c.Suburb = cust.Suburb;
										c.PhoneNumber = cust.PhoneNumber;
										c.MobileNumber = cust.MobileNumber;
										c.FallbackContact = cust.FallbackContact;
										c.FallbackPhoneNumber = cust.FallbackPhoneNumber;
										c.CustomerMemos = cust.CustomerMemos;
										c.JobHistory = cust.JobHistory;
										c.CompanyID = cust.CompanyID;
									}
								}
								if (! found)
								{
									foreach (Customer cust in this.UserAddedCustomers)
									{
										if (cust.CustomerNumber == c.CustomerNumber)
										{
											found = true;
											c.Title = cust.Title;
											c.FirstName = cust.FirstName;
											c.LastName = cust.LastName;
											c.Address = cust.Address;
											c.Suburb = cust.Suburb;
											c.PhoneNumber = cust.PhoneNumber;
											c.MobileNumber = cust.MobileNumber;
											c.FallbackContact = cust.FallbackContact;
											c.FallbackPhoneNumber = cust.FallbackPhoneNumber;
											c.CustomerMemos = cust.CustomerMemos;
											c.JobHistory = cust.JobHistory;
											c.CompanyID = cust.CompanyID;
										}
									}
								}

								if (UserAddedCustomers == null) 
									UserAddedCustomers = new List<Customer> {c};
								else 
								{
									UserAddedCustomers.Add (c);
								}

								Job j = new Job(true);
								j.CustomerNumber = c.CustomerNumber;
								j.JobDate = DateTime.Now;
								j.JobTime = DateTime.Now;
								j.JobBookedOn = DateTime.Now;
								j.ParentJobBookingNumber = -1;
								j.ShouldPayFee = true;
								j.Type = new JobType("Unknown");
								
								if (UserCreatedJobs == null || UserCreatedJobs.Count == 0)
									UserCreatedJobs = new List<Job> {j};
								else
								{
									UserCreatedJobs.Add (j);
									/*
									// DEPRECATED LOGIC :: look for that job in UserCreatedJobs, erase its results from DB and replace it
									for (int k = UserCreatedJobs.Count-1; k>=0; k--)
									{
										if (UserCreatedJobs[k].CustomerNumber == j.CustomerNumber)
										{
											_tabs._navWorkflow.EraseMainJobResultsFromDatabase (UserCreatedJobs[k]);
											UserCreatedJobs.RemoveAt (k);
											UserCreatedJobs.Insert (k, j);
										}
									}
									*/
								}
								InsertNewJobIntoDB (j);
								
								// show customers tab and do not allow the user to proceed to workflow unless he has entered the required customer details
								TableView.ReloadData ();
								
								TableView.SelectRow ( NSIndexPath.FromRowSection (UserCreatedJobs.Count-1,1), true, UITableViewScrollPosition.None );
								LastSelectedRowPath = NSIndexPath.FromRowSection (UserCreatedJobs.Count-1,1);
								_tabs.SelectedViewController = _tabs.ViewControllers[0];
								CurrentCustomer = UserAddedCustomers[UserAddedCustomers.Count-1];
								CurrentJob = UserCreatedJobs[UserCreatedJobs.Count-1];
								ShowCustomerDetails (CurrentCustomer, CurrentJob);		

								_tabs._app.myLocationManager.StartUpdatingLocation ();
								_tabs._app.myLocationManager.StartMonitoringSignificantLocationChanges ();
							}
							else
							{
								// cancel
							}
						};
						CustomerExistsAlert.Show ();
					}
					else 
					{
						InsertNewCustomerIntoDB (c);
	
						if (UserAddedCustomers == null) 
							UserAddedCustomers = new List<Customer> {c};
						else UserAddedCustomers.Add (c);
						
						// create a new job with a random job number (can be anything from 0 to 1000), bind it to that customer and save it to PL_RECOR
						Job j = new Job(true);
						j.CustomerNumber = c.CustomerNumber;
						try {		
							j.JobDate = Convert.ToDateTime (MyConstants.DEBUG_TODAY.Substring (2,10)); // DateTime.Now;
						} catch {
							j.JobDate = DateTime.Now.Date;
						}
						j.JobTime = DateTime.Now;
						j.JobBookedOn = DateTime.Now;
						j.ParentJobBookingNumber = -1;
						j.Type = new JobType("Unknown");
						j.ChildJobs = new List<Job>();
						
						if (UserCreatedJobs == null)
							UserCreatedJobs = new List<Job> {j};
						else UserCreatedJobs.Add (j);
						InsertNewJobIntoDB (j);
						
						// show customers tab and do not allow the user to proceed to workflow unless he has entered the required customer details
						TableView.ReloadData ();
						
						TableView.SelectRow ( NSIndexPath.FromRowSection (UserCreatedJobs.Count-1,1), true, UITableViewScrollPosition.None );
						LastSelectedRowPath = NSIndexPath.FromRowSection (UserCreatedJobs.Count-1,1);
						_tabs.SelectedViewController = _tabs.ViewControllers[0];
						CurrentCustomer = UserAddedCustomers[UserAddedCustomers.Count-1];
						CurrentJob = UserCreatedJobs[UserCreatedJobs.Count-1];
						ShowCustomerDetails (CurrentCustomer, CurrentJob);						
					}
				}
			}
			else 
			{
				// user cancelled
			}
		}
		
		public bool CustomerExistsInDB(long cusnum)
		{
			if (File.Exists (ServerClientViewController.dbFilePath))
			{
				using (var connection = new SqliteConnection("Data Source="+ServerClientViewController.dbFilePath) )
				{
					connection.Open();
					var cmd = connection.CreateCommand();
					cmd.CommandText = "SELECT * FROM Wclient WHERE Cusnum = ?";
					cmd.Parameters.Add ("@CustomerID", DbType.Int32).Value = cusnum;
					var reader = cmd.ExecuteReader();
					
					return reader.HasRows;
				}
			}
			else 
			{
				return false;
			}
		}
		
		public void InsertNewCustomerIntoDB(Customer c)
		{
			// INSERT the customer data INTO WCLIENT			
			if (File.Exists (ServerClientViewController.dbFilePath))
			{
				using (var connection = new SqliteConnection("Data Source="+ServerClientViewController.dbFilePath) )
				{
					connection.Open();
					var cmd = connection.CreateCommand();
					cmd.CommandText = "INSERT INTO Wclient (Cusnum, Wcclcde, Wctitle, Wconame, Wcsname, Wcsoname, Wcssname, Wcadd1, Wcadd2, Wcacde, Wcphone, Mobpre, Mobile, Wccoacde, Wccophone, Coi_ID) " + 
										" VALUES ( " + c.CustomerNumber.ToString () +", 'CREATEDONIPAD', \"\", \"\", \"\", \"\", \"\", \"\", \"\", \"\", \"\", \"\", \"\", \"\", \"\", 0 )";
					// cmd.Parameters.Add ("@CustomerID", DbType.Int32).Value = c.CustomerNumber;
					cmd.ExecuteNonQuery();		
				}
			}
			else 
			{
				_tabs._scView.Log (String.Format ("InsertNewCustomerIntoDB : ERROR : Database file not found: {0}", ServerClientViewController.dbFilePath));
			}
		}
		
		public void OverwriteCustomerRecord(Customer c)
		{
			if (File.Exists (ServerClientViewController.dbFilePath))
			{
				using (var connection = new SqliteConnection("Data Source="+ServerClientViewController.dbFilePath) )
				{
					connection.Open();
					var cmd = connection.CreateCommand();
					cmd.CommandText = "UPDATE Wclient SET Cusnum=?, Wconame=?, Wcsname=?, Wcsoname=?, Wcssname=?, Wcadd1=?, Wcadd2=?, Wcacde=?, Wcphone=?, Mobpre=?, Mobile=?, Wccoacde=?, Wccophone=? " + 
						" WHERE Cusnum = ?";
					cmd.Parameters.Add ("@CustomerID", DbType.Int32).Value = c.CustomerNumber;

					cmd.Parameters.Add ("@FirstName", DbType.String).Value = c.FirstName;
					cmd.Parameters.Add ("@LastName", DbType.String).Value = c.LastName;
					cmd.Parameters.Add ("@FallbackFirstName", DbType.String).Value = "";
					cmd.Parameters.Add ("@FallbackLastName", DbType.String).Value = "";
					cmd.Parameters.Add ("@Address", DbType.String).Value = c.Address;
					cmd.Parameters.Add ("@Suburb", DbType.String).Value = c.Suburb;
					cmd.Parameters.Add ("@PhoneCode", DbType.String).Value = "";
					cmd.Parameters.Add ("@PhoneNumber", DbType.String).Value = "";
					cmd.Parameters.Add ("@MobilePrefix", DbType.String).Value = "";
					cmd.Parameters.Add ("@MobileNumber", DbType.String).Value = "";
					cmd.Parameters.Add ("@FallbackPhonePrefix", DbType.String).Value = "";
					cmd.Parameters.Add ("@FallbackPhoneNumber", DbType.String).Value = "";				
					
					cmd.Parameters.Add ("@WhereCustomerID", DbType.Int32).Value = c.CustomerNumber;
					cmd.ExecuteNonQuery();		
				}
			}
			else 
			{
				_tabs._scView.Log (String.Format ("InsertNewCustomerIntoDB : ERROR : Database file not found: {0}", ServerClientViewController.dbFilePath));
			}			
		}
		
		public void InsertNewJobIntoDB(Job j)
		{
			// INSERT the job data INTO PL_RECOR			
			if (File.Exists (ServerClientViewController.dbFilePath))
			{
				using (var connection = new SqliteConnection("Data Source="+ServerClientViewController.dbFilePath) )
				{
					connection.Open();
					var cmd = connection.CreateCommand();
					cmd.CommandText = "INSERT INTO Pl_recor (Cusnum, Booknum, Plappdate, Type, Pay_pl, Time, Plnum, Repnum, Jdone, Installed, Parentnum, " +	// essential fields
						"SheetType, Sheett, Suburb, TimeEntered, UnitNum, Code, Run, Rebooked, OCode, Warranty, NoSearch, Attention) " + // other crappy fields that do not really interest us, but have to be filled anyway
						"VALUES (?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, " + // essential
						"?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?)"; // other
					cmd.Parameters.Add ("@CustomerID", DbType.Int32).Value = j.CustomerNumber;
					cmd.Parameters.Add ("@JobID", DbType.Int64).Value = j.JobBookingNumber;

					// job should be saved in the run that is currently loaded -- i.e. use MyConstants.DEBUG_TODAY as the date
					string currentRunDateString = MyConstants.DEBUG_TODAY.Substring (2,10);
					DateTime currentRunDate = DateTime.ParseExact ( currentRunDateString, "yyyy-MM-dd", 
				                                      System.Globalization.CultureInfo.InvariantCulture);
					cmd.Parameters.Add ("@JobDate", DbType.String).Value = currentRunDate.ToString ("yyyy-MM-dd");

					cmd.Parameters.Add ("@JobType", DbType.String).Value = j.Type.Code;
					cmd.Parameters.Add ("@MoneyToCollect", DbType.Double).Value = j.MoneyToCollect;
					cmd.Parameters.Add ("@JobTime", DbType.String).Value = j.JobTime.ToString ("yyyy-MM-dd HH:mm:ss"); // j.JobTime.ToString ("hh:mm");
					cmd.Parameters.Add ("@PlumberID", DbType.Int32).Value = (MyConstants.EmployeeType == MyConstants.EmployeeTypes.Plumber) ? MyConstants.EmployeeID : -1;									
					cmd.Parameters.Add ("@FranchiseeID", DbType.Int32).Value = (MyConstants.EmployeeType == MyConstants.EmployeeTypes.Franchisee) ? MyConstants.EmployeeID : -1;
					cmd.Parameters.Add ("@JobDone", DbType.Int32).Value = j.JobDone;
					cmd.Parameters.Add ("@JobResult", DbType.String).Value = "";		// no result is known yet
					cmd.Parameters.Add ("@ParentJobID", DbType.Int64).Value = -1; 	// j.ParentJobBookingNumber;

					cmd.Parameters.Add ("@SheetType", DbType.String).Value = "Custom";
					cmd.Parameters.Add ("@Sheett", DbType.Int16).Value = -1;	
					cmd.Parameters.Add ("@Suburb", DbType.String).Value = "";	
					cmd.Parameters.Add ("@TimeEntered", DbType.String).Value = j.JobTime.ToString ("yyyy-MM-dd HH:mm:ss");
					cmd.Parameters.Add ("@UnitNum", DbType.Int16).Value = 0;	
					cmd.Parameters.Add ("@Code", DbType.Int16).Value = 0;	
					cmd.Parameters.Add ("@Run", DbType.Int16).Value = 0;	
					cmd.Parameters.Add ("@Rebooked", DbType.Int16).Value = 0;	
					cmd.Parameters.Add ("@OCode", DbType.Int16).Value = 0;	
					cmd.Parameters.Add ("@Warranty", DbType.Int16).Value = 0;	
					cmd.Parameters.Add ("@NoSearch", DbType.Int16).Value = 0;	
					cmd.Parameters.Add ("@Attention", DbType.Int16).Value = 0;	

					/*
SheetType, Sheett, Suburb, Time, TimeEntered, UnitNum, Code, Run, Rebooked, OCode, Warranty, NoSearch, Attention
					 */

					cmd.ExecuteNonQuery();		
				}
			}
			else 
			{
				_tabs._scView.Log (String.Format ("InsertNewCustomerIntoDB : ERROR : Database file not found: {0}", ServerClientViewController.dbFilePath));
			}
		}
		
		public void ShowCustomerDetails(Customer c, Job j)
		{
			_tabs._customersView.CompanyName = c.CompanyName;
			_tabs._customersView.CustomerNumber = c.CustomerNumber;
			_tabs._customersView.FirstName = c.FirstName;
			_tabs._customersView.LastName = c.LastName;
			_tabs._customersView.MainAddress = c.Address;
			_tabs._customersView.Suburb = c.Suburb;
			_tabs._customersView.PhoneNumber = c.PhoneNumber;
			_tabs._customersView.MobileNumber = c.MobileNumber;
			_tabs._customersView.LastInstallDate = c.LastInstallDate;
			_tabs._customersView.FallbackContact = c.FallbackContact;
			_tabs._customersView.FallbackPhone = c.FallbackPhoneNumber;
			_tabs._customersView.LastJobType = c.LastJobType;
			_tabs._customersView.SetBtnTUDoneState(c.TubingUpgradeDone);

			_tabs._customersView.JobBookingNumber = j.JobBookingNumber;
			_tabs._customersView.UnitNumber = j.UnitNumber;
			_tabs._customersView.JobType = j.Type.Description;
			_tabs._customersView.JobDate = j.JobDate;
			_tabs._customersView.JobTime = j.JobTime;
			_tabs._customersView.DisplayJobTimeFrame (j);

			_tabs._customersView.JobBookingNumber = j.JobBookingNumber;
			_tabs._customersView.MoneyToCollect = j.MoneyToCollect;
			_tabs._customersView.JobSpecialInstructions = j.JobSpecialInstructions;
			_tabs._customersView.JobPlumbingComments = j.JobPlumbingComments;
			_tabs._customersView.ContactPerson = j.ContactPerson;
			_tabs._customersView.AttentionFlag = j.AttentionFlag;
			_tabs._customersView.AttentionReason = j.AttentionReason;

			if (_tabs._customersView.AttentionFlag)
			{
				// _tabs._customersView.AttentionReason = c.AttentionReason;
				_tabs._customersView.SetBtnAttentionState (true);
				_tabs._customersView.SetAttentionReasonVisible ();
			}
			else 
			{
				_tabs._customersView.SetBtnAttentionState (false);
				_tabs._customersView.SetAttentionReasonHidden ();
			}			
			
			_tabs._customersView.NumberOfMemos = (c.CustomerMemos != null) ? c.CustomerMemos.Count() : 0;
			
			// Update the field values on Job History tab which will get them displayed in the view
			_tabs._jobHistoryView.JobHistoryLabel = String.Format ("Job history for {0} {1} {2}", 
			                                                              c.Title,
			                                                              c.FirstName,
			                                                              c.LastName);
			_tabs._jobHistoryView.JobHistoryData.JobHistory = c.JobHistory;
			_tabs._jobHistoryView._ds.JobHistory = c.JobHistory;
			_tabs._jobHistoryView.ReloadJobHistoryTable();
			
			// Update the field values on Memos tab which will get them displayed in the view
			_tabs._memosView.CustomerMemos = /*(List<Memo>)*/c.CustomerMemos;
			_tabs._memosView.MemosLabel = String.Format ("Memos for {0} {1} {2}", 
			                                                              c.Title,
			                                                              c.FirstName,
			                                                              c.LastName);
			_tabs._memosView.ReloadMemosTable ();
			
			// Update the PhotosTakenToday and CustomerNumber values for _photosView
			_tabs._photosView.PhotosCounter = c.PhotosTakenToday;
			_tabs._photosView.CustomerNumber = c.CustomerNumber;			
		}


		
		public class JobRunTableSource : UITableViewDataSource 
		{
			JobRunTable _table;
			// SqliteConnection dbConnection; -- WITHOUT "using" pattern implemented on SqliteConnection we would eventually get "too many files open" exception since dbConnection.Close() apparently does not release the file handle
			const string ReusableJobRunCellID = "JobRunCell";
			
			public JobRunTableSource(JobRunTable table) {
				_table = table;
				
				Customer c = new Customer(999999999, "Mr.", "FirstName", "LastName", "Address", "Suburb", 
								                          String.Format("({0}){1}", "PhoneAreaCode", "PhoneNumber"), 
								                          String.Format("({0}){1}", "MobileCode", "MobileNumber"), 
				                          				  DateTime.Now, "", "", 0, "", true, 0, 0); // dummy customer
				_table._customers = new List<Customer> {c};
				
				Job j = new Job(999999999, 999999999, 999999999, DateTime.Now, DateTime.Now, 0, DateTime.Now, 0, "", "", "FIL", 0, false, false, "", "");	// dummy job
				_table._mainjoblist = new List<Job> {j};
			}

			public string GetDateFromDBName(string dbPath)
			{
				string result = " '" + dbPath.Substring (dbPath.Length-17, 10) + "' ";
				try {
					Convert.ToDateTime (result.Substring (2,10));
				}
				catch { 
					return " '2012-07-24' "; 
				}
				return result;
			}

			public bool TestDBIntegrity()
			{
				try
				{
					string dbPath = MyConstants.DBReceivedFromServer;
					using (var dbCon = new SqliteConnection("Data Source="+dbPath))
					{
						dbCon.Open ();
						using (var cmd = dbCon.CreateCommand ())
						{
							try 
							{
								string sql = " PRAGMA integrity_check; ";
								cmd.CommandText = sql;
								using (var reader = cmd.ExecuteReader ())
								{
									if ( (string)reader["integrity_check"] != "ok")
									{
										// integrity check failed
										// Console.WriteLine(String.Format ("reader[\"integrity_check\"] = {0}"));
										reader.Close ();
										return false;
									}
									else	
									{
										reader.Close ();
										return true;
									}
								}
							}
							catch
							{
								return false;
							}
						}
					}
				}
				catch
				{
					return false;
				}
			}

			public void ReadRunData(string dbDate, string databasePath)
			{
				using (var dbConnection = new SqliteConnection("Data Source="+databasePath))
				{
					dbConnection.Open ();
					using (var cmd = dbConnection.CreateCommand () )
					{
						// Get client details from WCLIENT: excluded WSALES table from the query since it was troublesome (wrong number of records selected)
						string sql = 	"SELECT " +  // "MAX(wsales.wdateins), " +
											" wclient.cusnum, wctitle, wconame, wcsname, wcomname, " +
											" wcadd1 || \" \" || wcadd11 as street_address, wcadd2, wcacde||exdigit, wcphone, mobpre, mobile, " +
											" wcstitle, wcsoname, wcssname, wccoacde, wccophone, tu_done, coi_id, coords_lat, coords_lng " +
											" FROM wclient, pl_recor " + // , wsales " +
											" WHERE pl_recor.plappdate= " + dbDate +
												" AND wclient.cusnum=pl_recor.cusnum " +
												" AND pl_recor.cusnum != 72077 " + // getting rid of dummy records ( Mr. Puratap )
												" AND wclient.wcclcde != 'CREATEDONIPAD' " +
												" AND pl_recor.parentnum < 1 AND pl_recor.parentnum != -1 " +
										" ORDER BY PL_RECOR.iPad_Ordering, PL_RECOR.TIME_START asc, PL_RECOR.TIME asc, booknum desc";		

											// " AND wsales.cusnum=pl_recor.cusnum " +
						cmd.CommandText = sql;
						using (var reader = cmd.ExecuteReader())
						{
							while (reader.Read () )
							{
								// TODO :: put thorough error handling here (data type checks, default values, etc.)
								long CustomerNumber = (long)reader["cusnum"];
								long companyID = (long)reader ["coi_id"];
								string companyName = (string)reader["wcomname"];
								string Title = (string)reader["wctitle"];
								string FirstName = (string)reader["wconame"];
								string LastName = (string)reader["wcsname"];
								string Address = (string)reader["street_address"];
								string Suburb = (string)reader["wcadd2"];
								
								string PhoneAreaCode = (string)reader["wcacde||exdigit"];
								if (PhoneAreaCode.Length>3)
									PhoneAreaCode = PhoneAreaCode.Substring(0,3);
								string PhoneNumber = (string)reader["wcphone"];
								string MobilePrefix = (string)reader["mobpre"];
								string MobileNumber = (string)reader["mobile"];
								
								string fbContact = String.Format ("{0} {1} {2}", (string)reader["wcstitle"], (string)reader["wcsoname"], (string)reader["wcssname"]);
								string fbPhone = String.Format ("{0} {1}", (string)reader["wccoacde"], (string)reader["wccophone"]);

								double cLat = (double)reader ["coords_lat"];
								double cLng = (double)reader ["coords_lng"];

								DateTime lastInstallDate;
								// DEBUG :: DateTime.TryParse((string)reader["max(wsales.wdateins)"], out lastInstallDate);
								lastInstallDate = DateTime.Now.Date;

								bool tubingDone = (reader["tu_done"] == DBNull.Value) ? false : Convert.ToBoolean (reader["tu_done"]);
								if (MyConstants.EmployeeType == MyConstants.EmployeeTypes.Plumber) tubingDone = true;

								Customer c = new Customer(CustomerNumber, Title, FirstName, LastName, Address, Suburb, 
								                          					String.Format("{0} {1}", PhoneAreaCode, PhoneNumber), 
								                          					String.Format("{0} {1}", MobilePrefix, MobileNumber),
								                          					lastInstallDate, fbContact, fbPhone, 
								                          					companyID, companyName, tubingDone, cLat, cLng);				
								_table._customers.Add(c);
							}
							if (! reader.IsClosed) reader.Close ();
						}


						
						Job j;
						// Gets today's job details for all customers from PL_RECOR:: IMPLEMENTED reading the child jobs as well
						// This will ignore manually added jobs
					
						sql = " SELECT wsales.specialinstruct, wsales.wplcomment, wsales.contact_name, " +
								" wsales.wsoldprice as sales_price, wcmemo.wmore as pl_memo, " +
								" pl_recor.* " +
								" FROM pl_recor LEFT OUTER JOIN wsales ON pl_recor.cusnum = wsales.cusnum AND pl_recor.unitnum = wsales.unitnum " +
											  " LEFT OUTER JOIN wcmemo ON pl_recor.booknum = wcmemo.booknum AND wcmemo.wmtype = 'PLU' " +
								" WHERE pl_recor.plAppDate = " + dbDate +		// IMPLEMENTED :: was a hardcoded date, has been replaced by something a bit more flexible
									// " AND wsales.unitnum != 0 " +				// getting rid of older WSALES records that cannot be used to determine the sale price
									" AND pl_recor.cusnum != 72077 " +			// getting rid of dummy records ( Mr. Puratap )
								" AND pl_recor.parentnum != -1 " +				// getting rid of manually created jobs 
									" AND (NOT EXISTS (SELECT booknum FROM pl_recor plr WHERE plr.booknum=pl_recor.parentnum AND plr.parentnum=-1)) " + // getting rid of child jobs of manually created jobs
								" ORDER BY PL_RECOR.iPad_Ordering, PL_RECOR.TIME_START asc, PL_RECOR.TIME asc, booknum desc";	

						cmd.Parameters.Clear ();
						cmd.CommandText = sql;
						using (var reader = cmd.ExecuteReader() )
						{
							while ( reader.Read () )
							{
								// TODO:: put thorough error handling here (data type checks, default values, etc.)
								// Implemented:: draw price from WSALES for install jobs (to consider deposits later on)
								long jnum = Convert.ToInt64( (double)reader["booknum"] );
								long cusnum = (long)reader["cusnum"];
								long unitnum = (reader["unitnum"] == DBNull.Value) ? 0 : Convert.ToInt64 (reader["unitnum"]);
								bool warranty = (reader["warranty"] == DBNull.Value) ? false : Convert.ToBoolean (reader["warranty"]);

								DateTime jTime, jtStart, jtEnd;
								DateTime jDate;

								try {
									jTime = (reader["time"] == DBNull.Value) ? new DateTime(1990, 1, 1) : (DateTime)reader["time"];
									jDate = (reader["plappdate"] == DBNull.Value) ? new DateTime(1990, 1, 1) : (DateTime)reader["plappdate"];
									jtStart = (reader["time_start"] == DBNull.Value) ? new DateTime(1990, 1, 1) : (DateTime)reader["time_start"];
									jtEnd = (reader["time_end"] == DBNull.Value) ? new DateTime(1990, 1, 1) : (DateTime)reader["time_end"];
								} 
								catch
								{
									// Console.WriteLine (e.Message);
									jTime = new DateTime(1990, 1, 1);
									jDate = new DateTime(1990, 1, 1);
									jtStart = new DateTime(1990, 1, 1);
									jtEnd = new DateTime(1990, 1, 1);
								}

								string jType = (string)reader["type"];

								// if this is an installation type job, this should be SALES_PRICE, deposits will be considered on the payments screen
								double money = 0;
								if (jType == "TWI" || jType == "RAI" || jType == "ROOF")
								{
									try {
										money = (double)reader ["sales_price"];
									}
									catch {
										money = 0;
									}
								}
								else
									money = (double)reader["pay_pl"];

								DateTime jbOn = (DateTime)reader["timeentered"];
								long jbBy = (long)reader["repnum"];
								bool attention = Convert.ToBoolean (reader["attention"]);									
								string sp = (reader["specialinstruct"] == DBNull.Value) ? "" : (string)reader["specialinstruct"];

								string plc = (reader["pl_memo"] != DBNull.Value) ? (string)reader["pl_memo"] : 
											 (reader["wplcomment"] != DBNull.Value) ? (string)reader["wplcomment"] : "" ;

								string cntct = (reader["contact_name"] == DBNull.Value) ? "" : (string)reader["contact_name"];
								string attreason = (reader["attention_reason"] == DBNull.Value) ? "" : (string)reader["attention_reason"];

								// plug the value from the parentnum field into the job constructor
								long parentnum = Convert.ToInt64(reader["parentnum"]);
																	
								j = new Job(cusnum, jnum, unitnum, jTime, jDate, money, jbOn, jbBy, sp, plc, jType, parentnum, warranty, attention, cntct, attreason);
								j.JobTimeStart = jtStart;
								j.JobTimeEnd = jtEnd;
								j.OrderInRun = Convert.ToInt32 (reader ["ipad_ordering"]);
							
								// getting job status values from database
								try
								{
									int jDone = Convert.ToInt32 (reader["jdone"]);
									j.JobDone = (jDone==1) ? true : false;

									if (j.JobDone)
									{
										string jStatus = (reader["installed"]==DBNull.Value)? "" : (string)reader["installed"];
										if (! (jStatus.Contains ("Installed") || jStatus.Contains("Upgraded") || jStatus.Contains ("New Tap") || 
										       jStatus.Contains ("Changed") || jStatus.Contains ("Service Done") || 	jStatus.Contains ("Uninstall")))
											j.Started = MyConstants.JobStarted.Other;
										else j.Started = MyConstants.JobStarted.Yes;
									}
								} 
								catch 
								{ 
									j.JobDone = false; 
								}

								if (parentnum == 0) {
									_table._mainjoblist.Add(j);									
								}
								else 
								{		//	parentnum != 0 => this is a child job, look for its parent and add it to its ChildJobs list
									foreach(Job mainJob in _table.MainJobList)
									{
										if (mainJob.JobBookingNumber == parentnum)
										{
											mainJob.ChildJobs.Add (j);
											break;
										}
									}
								}
							}
							if (! reader.IsClosed) reader.Close ();
						}
					
						// Get user created customers
						sql = 	"SELECT wclient.cusnum, wclient.wcclcde, wcomname, wctitle, wconame, wcsname, " + // used to be :: SELECT DISTINCT
												" wcadd1, wcadd2, wcacde||exdigit, wcphone, mobpre, mobile,  " +
												" wcstitle, wcsoname, wcssname, wccoacde, wccophone, coi_id  " +
												" FROM wclient, pl_recor " +
												" WHERE pl_recor.plappdate= " + dbDate +
												"	AND wclient.cusnum=pl_recor.cusnum  " +
												"	AND pl_recor.cusnum != 72077 " +
												"	AND pl_recor.parentnum = -1 " + // wclient.wcclcde = 'CREATEDONIPAD' " +
												" ORDER BY pl_recor.time";

						cmd.CommandText = sql;
						cmd.Parameters.Clear ();
						using (var reader = cmd.ExecuteReader())
						{
							while (reader.Read () )
							{
									long CustomerNumber = (long)reader["cusnum"];
									long CompanyID = (long)reader["coi_id"];
									string CompanyName = (reader["wcomname"] != DBNull.Value) ? (string)reader["wcomname"] : "";
									string Title = (reader["wctitle"] != DBNull.Value)? (string)reader["wctitle"] : "";
									string FirstName = (reader["wconame"] != DBNull.Value) ? (string)reader["wconame"] : "";
									string LastName = (reader["wcsname"] != DBNull.Value) ? (string)reader["wcsname"] : ""; // (string)reader["wcsname"];
									string Address = (reader["wcadd1"] != DBNull.Value) ? (string)reader["wcadd1"] : ""; // (string)reader["wcadd1"];
									string Suburb = (reader["wcadd2"] != DBNull.Value) ? (string)reader["wcadd2"] : ""; // (string)reader["wcadd2"];
									
									string PhoneAreaCode = (reader["wcacde||exdigit"] != DBNull.Value)? (string)reader["wcacde||exdigit"] : "";
									if (PhoneAreaCode.Length>3)
										PhoneAreaCode = PhoneAreaCode.Substring(0,3);
									string PhoneNumber = (reader["wcphone"] != DBNull.Value) ? (string)reader["wcphone"] : "000-0000";
									string MobilePrefix = (reader["mobpre"] != DBNull.Value) ? (string)reader["mobpre"] : "(000)";
									string MobileNumber = (reader["mobile"] != DBNull.Value) ?  (string)reader["mobile"] : "000-0000";
									
									string fbContactTitle = (reader["wcstitle"] != DBNull.Value) ? (string)reader["wcstitle"] : "";
									string fbContactFirstName = (reader["wcsoname"] != DBNull.Value) ? (string)reader["wcsoname"] : "";
									string fbContactLastName = (reader["wcssname"] != DBNull.Value) ? (string)reader["wcssname"] : "";
									string fbContact = String.Format ("{0} {1} {2}", fbContactTitle, fbContactFirstName, fbContactLastName);
								
									string fbPhoneCode = (reader["wccoacde"] != DBNull.Value) ? (string)reader["wccoacde"] : "";
									string fbPhoneNumber = (reader["wccophone"] != DBNull.Value) ? (string)reader["wccophone"] : "";																			
									string fbPhone = String.Format ("({0}){1}", fbPhoneCode, fbPhoneNumber);
									
									DateTime lastInstallDate = new DateTime();
									// DateTime.TryParse((string)reader["max(wsales.wdateins)"], out lastInstallDate);
									bool tubingDone = true;
								
									Customer c = new Customer(CustomerNumber, Title, FirstName, LastName, Address, Suburb, 
									                          					String.Format("({0}){1}", PhoneAreaCode, PhoneNumber), 
									                          					String.Format("({0}){1}", MobilePrefix, MobileNumber),
								                          						lastInstallDate, fbContact, fbPhone, CompanyID, CompanyName, tubingDone, 0, 0);
									
									if (_table.UserAddedCustomers == null) _table.UserAddedCustomers = new List<Customer> {c};
									else _table.UserAddedCustomers.Add (c);
							}
							if (! reader.IsClosed) reader.Close();
						}
						
						// Get user added jobs (manually created, were not booked)
						sql = " SELECT pl_recor.*, " +
										" '' as contact_name, '' as attention_reason " +
									" FROM pl_recor " +
									" WHERE pl_recor.plAppDate = " + dbDate +		// DEBUG :: was almost a hard-coded date, has been replaced by something a bit more flexible
										" AND pl_recor.cusnum != 72077 " +		// getting rid of dummy records ( Mr. Puratap )
										" AND (pl_recor.parentnum = -1 OR (pl_recor.parentnum > 0 AND pl_recor.parentnum < 1001)) " + 		// reading only user created jobs and their child jobs
									" ORDER BY pl_recor.time asc, parentnum asc, booknum desc";
						cmd.CommandText = sql;
						cmd.Parameters.Clear ();
						using (var reader = cmd.ExecuteReader())
						{
							while (reader.Read () )
							{
								long jnum = Convert.ToInt64( (double)reader["booknum"] );
								long cusnum = (long)reader["cusnum"];
								long unitnum = (reader["unitnum"] == DBNull.Value) ? 0 : Convert.ToInt64 (reader["unitnum"]);
								bool warranty = (reader["warranty"] == DBNull.Value) ? false : Convert.ToBoolean (reader["warranty"]);
								DateTime jTime = (DateTime)reader["time"];
								DateTime jDate = (DateTime)reader["plappdate"];
								string jType = (string)reader["type"];
								double money = (double)reader["pay_pl"];
								DateTime jbOn = (reader["timeentered"] != DBNull.Value) ? (DateTime)reader["timeentered"] : new DateTime();
								long jbBy = (long)reader["repnum"];
								long parentnum = Convert.ToInt64 ( reader["parentnum"] );	
								bool attention = Convert.ToBoolean( reader["attention"] );
								string cntct = (reader["contact_name"] == DBNull.Value) ? "" : (string)reader["contact_name"];
								string attreason = (reader["attention_reason"] == DBNull.Value) ? "" : (string)reader["attention_reason"];

								// plug the value from the parentnum field into the job constructor
								// two empty strings are SpecialInstructions and PlumbingComments
								j = new Job(cusnum, jnum, unitnum, jTime, jDate, money, jbOn, jbBy, "", "", jType, parentnum, warranty, attention, cntct, attreason);
							
								// getting job status values from database
								int jDone = Convert.ToInt32 (reader["jdone"]);
								j.JobDone = (jDone==1) ? true : false;	
							
								if (parentnum == -1) 
								{
									_table.UserCreatedJobs.Add (j);
								}
								else 
								{
									Job assumedMain = _table.FindParentJob(j);
									if (assumedMain != null) // may happen if FindParentJob fails to find one, which in turn may happen with unfinished child jobs (iPad reboot, app restart, etc)
									{
										if (assumedMain.ParentJobBookingNumber == -1) 
											assumedMain.ChildJobs.Add (j);
									}
								}
							}
							if (! reader.IsClosed) reader.Close();
						}

						// READING FEES DATA
						sql = "SELECT Fees.*, Pl_recor.Booknum, Pl_recor.Parentnum " +
							"FROM Fees, Pl_recor " +
							"WHERE Pl_recor.Booknum = Fees.Job_ID " +
							"AND FEES.JOB_ID IN (SELECT BOOKNUM FROM Pl_recor WHERE Plappdate = " + dbDate + ")";
						cmd.CommandText = sql;
						cmd.Parameters.Clear ();
						// cmd.Parameters.Add ("@Today", DbType.String).Value = MyConstants.DEBUG_TODAY;

						using(var FeeReader = cmd.ExecuteReader ())
						{
							List<Job> mainJobs = _table._mainjoblist;
							List<Job> userJobs = _table.UserCreatedJobs;
							while (FeeReader.Read () )
							{
								if ((double)FeeReader["parentnum"] < 1 && (double)FeeReader["parentnum"] != -1)
								{	// this is one of the main jobs, loop through main jobs, find it, set the values
									foreach(Job main in mainJobs)
									{
										if (main.JobBookingNumber == (double)FeeReader["booknum"])
										{ 
											main.EmployeeFee = (double)FeeReader["fee_amount"];
											main.ShouldPayFee = true;
											break; 
										}
									}
								}
								else // this is a child job OR a user-created job
								{ 
									if ((double)FeeReader["parentnum"] > 1)		// this is a child job, look for its main, then look for that child job in the main job list AND in user-created job list, set the values
									{ 
										foreach(Job main in mainJobs)
										{
											if (main.JobBookingNumber == (double)FeeReader["parentnum"] )
											{
												foreach (Job child in main.ChildJobs)
												{
													if (child.JobBookingNumber == (double)FeeReader["booknum"])
													{
														child.EmployeeFee = (double)FeeReader["fee_amount"];
														child.ShouldPayFee = true;
														break;
													}
												}
												break;
											}
										}

										foreach(Job main in userJobs)
										{
											if (main.JobBookingNumber == (double)FeeReader["parentnum"] )
											{
												foreach (Job child in main.ChildJobs)
												{
													if (child.JobBookingNumber == (double)FeeReader["booknum"])
													{
														child.EmployeeFee = (double)FeeReader["fee_amount"];
														child.ShouldPayFee = true;
														break;
													}
												}
												break;
											}
										}
									}
									else // this is a user-created MAIN job
									{
										foreach(Job main in userJobs)
										{
											if (main.JobBookingNumber == (double)FeeReader["booknum"])
											{ 
												main.EmployeeFee = (double)FeeReader["fee_amount"];
												main.ShouldPayFee = true;
												break; 
											}
										}
									}
								}
							} // END while (jobreader.Read () )
							if (! FeeReader.IsClosed) FeeReader.Close ();
						} // END using FeeReader
					} // END using dbCommand
				}	// END using dbConnection

				this.ReadCustomerDeposits (dbDate, databasePath);

				// check if every job in the table has been done
				bool allDone = true;
				foreach(Job j in _table._tabs._jobRunTable.MainJobList) {
					if (j.JobDone == false) { allDone = false; break; }
				}
				if (allDone)
				{
					foreach (Job j in _table._tabs._jobRunTable.UserCreatedJobs)
					{
						if (j.JobDone == false) { allDone = false; break; }
					}
				}
				_table._tabs._jobRunTable.AllJobsDone = allDone;

				JobRunTable.JobTypes = MyConstants.GetJobTypesFromDB (); // just in case the employee mode changes, they'd need to reload job types so that the fees are calculated correctly
			}

			/* * * * THIS WAS READING INVOICE FEES * * * * NO LONGER APPLIES * * * */
//			this.ReadCustomerCharges (dbDate, databasePath);

//			public void ReadCustomerCharges( string dbDate, string databasePath)
//			{
//				using (var dbConnection = new SqliteConnection("Data Source="+databasePath))
//				{
//					dbConnection.Open ();
//					using (var cmd = dbConnection.CreateCommand () )
//					{
//						// Get the InvoiceFeesWaived flag for each customer
//						string sql = "Select CusNum, Coi_No_Fees FROM WCLIENT, COI WHERE COI.COI_ID = WCLIENT.COI_ID";
//						cmd.CommandText = sql;
//						using (var reader = cmd.ExecuteReader ()) {
//							while (reader.Read ()) {
//								foreach (Customer c in _table.Customers) {
//									if (c.CustomerNumber == (long)reader["CusNum"]) {
//										c.InvoiceChargesWaived = true; // Convert.ToBoolean ((byte)reader["Coi_No_Fees"]);
//									}
//								}
//							}
//						}
//
//						// Get client charges from CHARGES table
//						sql = " SELECT Cust_OID, " +
//							" SUM(Amount) as Charges " +
//								" FROM CHARGES " +
//								" GROUP BY Cust_OID";
//						cmd.CommandText = sql;
//						using (var reader = cmd.ExecuteReader())
//						{
//							while (reader.Read () )
//							{
//								foreach(Customer c in _table.Customers){
//									if (c.CustomerNumber == (long)reader ["Cust_OID"] && !c.InvoiceChargesWaived) {
//										c.ChargeAmount = (double)reader ["Charges"];
//									}
//								}
//							}
//						}
//					} // end using dbCommand
//				} // end using dbConnection
//			}
			/* * * * THIS WAS READING INVOICE FEES * * * * NO LONGER APPLIES * * * */

			public void ReadCustomerDeposits(string dbDate, string databasePath)
			{
				using (var dbConnection = new SqliteConnection("Data Source="+databasePath))
				{
					dbConnection.Open ();
					using (var cmd = dbConnection.CreateCommand () )
					{
						// Get client deposits from JOURNAL: sum by customer, deduct SUM(debit) from SUM(credit), exclude really old deposits,
						// (more than 12 months old), exclude deposits where a job has been done after they were taken (check if Last_Job_Date is null), 
						// exclude deposits that have been used on this run already (j.jDesc != 'Deposit used on iPad')

						// Also, a scenario could arise where deposit was credited more than 12 months ago and then debited recently.
						// The journal credit record would not be included in the result set, but the debit one would. 
						// This can lead to SUM(Credit)-SUM(Debit) being < 0, thus added "HAVING Deposit > 0" clause

						string sql = "SELECT j.CusNum as Customer, " +
						             	" SUM(j.Credit) - SUM(j.Debit) as Deposit, " +
						             	" MAX(PlAppDate) as Last_Job_Date " +
						             " FROM JOURNAL j LEFT OUTER JOIN PL_RECOR pl ON j.CusNum = pl.CusNum " +
						             											" AND pl.PlAppDate < ? " +
						             											" AND Installed IN ('', 'Installed', 'Changed', 'Upgraded', 'New Tap', 'Service Do', 'Service Done', 'Result') " +
						             " WHERE j.AccNum = 2.1300 " +
						             	" AND j.jDesc != 'Deposit used on iPad' " +
						             	" AND j.jDate > DATE('now', '-12 months')" +
						             " GROUP BY j.CusNum " +
						             " HAVING Deposit > 0 ";

						cmd.CommandText = sql;
						cmd.Parameters.Add ("@Run_Date", DbType.String).Value = MyConstants.DEBUG_TODAY;
						using (var reader = cmd.ExecuteReader())
						{
							while (reader.Read () )
							{
								// Console.WriteLine (String.Format ("Customer = {0}; Deposit = {1}", reader ["CusNum"], reader ["Deposit"]));
								if (reader ["Last_Job_Date"] == DBNull.Value) {
									foreach(Customer c in _table.Customers){
										if (c.CustomerNumber == (long)reader ["Customer"]) {
											c.DepositAmount = (double)reader ["Deposit"];
										}
									}
								} // endif -- check if Last_Job_Date is null
							} // end while reader.Read()
						} // end using reader

						// this reads deposits used only on the current run (Journal.jDate = dbDate)
						sql = " SELECT CusNum, " +
							" SUM(Debit) as DepositUsed " +
								" FROM JOURNAL " +
								" WHERE Journal.AccNum = 2.1300 " +
								" AND Journal.jDate = " + dbDate +
								" AND Journal.jDesc = 'Deposit used on iPad' " +
								" GROUP BY CusNum";
						cmd.CommandText = sql;
						using (var reader = cmd.ExecuteReader ()) {
							while (reader.Read ()) {
								foreach (Customer c in _table.Customers) {
									if (c.CustomerNumber == (long)reader ["CusNum"]) {
										c.DepositUsed = (double)reader ["DepositUsed"];
										c.DepositAmount = c.DepositAmount - c.DepositUsed;
									}
								}
							}
						}
					} // end using dbCommand
				} // end using dbConnection
			}

			public List<string> GetRunDatesFromDB(string dbPath)
			{
				var result = new List<string>();

				using (var dbConnection = new SqliteConnection("Data Source="+dbPath))
				{
					try {
						//dbConnection = new SqliteConnection("Data Source="+dbPath);
						dbConnection.Open();
						using (var cmd = dbConnection.CreateCommand())
						{	
							// get the possible dates to load from the database

							string sql = (MyConstants.EmployeeType == MyConstants.EmployeeTypes.Franchisee) ? 
							             "SELECT plappdate as date, count(booknum) as jobs from pl_recor where plappdate >= date('now', '-7 day') group by plappdate having jobs > 15 order by plappdate" : 
							             "SELECT plappdate as date, count(booknum) as jobs from pl_recor where plappdate >= date('now', '-7 day') and plnum = " + MyConstants.EmployeeID.ToString() + " group by plappdate having jobs > 0 order by plappdate";
							cmd.CommandText = sql;
							using (var dateReader = cmd.ExecuteReader())
							{
								if (!dateReader.HasRows)	// no dates -- return empty List<string>
									return result;
								else // there are possible dates
								{
									while (dateReader.Read ())
									{
										string date = String.Format (" '{0}' ", ((DateTime)dateReader["date"]).Date.ToString ("yyyy-MM-dd"));
										result.Add (date);
									}
									if (!dateReader.IsClosed) dateReader.Close ();
								}
							}
						}
						return result;
					}
					catch {
						return null;
					}
				}
			}
			
			public void LoadJobRun(int isStarting)
			{
				// checks if database file exists, if it does, gets customer data into _table._customers
				string dbPath = MyConstants.DBReceivedFromServer;
				if (string.IsNullOrEmpty (dbPath) || (! File.Exists (dbPath)))
				{
					// alert the user
					UIAlertView LastDBNotFound = new UIAlertView("Warning", "Could not find the last database downloaded from server.\r\nLoading TEST database", null, "OK");
					LastDBNotFound.Show ();
					dbPath = ServerClientViewController.dbFilePath;
				}
				if (File.Exists(dbPath))
				{
					string dbDate = GetDateFromDBName(dbPath);
					// DEPRECATED :: if (MyConstants.AUTO_CHANGE_DATES == false) dbDate = MyConstants.DEBUG_TODAY;

					_table._customers = new List<Customer> ();
					_table.UserAddedCustomers = new List<Customer> ();
					_table._mainjoblist = new List<Job> ();
					_table.UserCreatedJobs = new List<Job> ();

					/*
					if (MyConstants.EmployeeType == MyConstants.EmployeeTypes.Plumber)
					{
						// THIS IS NO LONGER CORRECT SINCE SATURDAYS ARE HANDLED THE SAME WAY AS COUNTRY RUNS
						MyConstants.DEBUG_TODAY = dbDate;
						ReadRunData (dbDate, dbPath);
					}
					else */
					{
						// create SQLite connection to file and extract data
						using (var dbConnection = new SqliteConnection("Data Source="+dbPath))
						{
							using (var cmd = dbConnection.CreateCommand())
							{	
								try 
								{
									dbConnection.Open();
									// get the possible dates to load from the database
									string sql = (MyConstants.EmployeeType == MyConstants.EmployeeTypes.Plumber)? 
										"SELECT plappdate as date, count(booknum) as jobs from pl_recor where plappdate>=date('now', '-7 day') and plnum = " + MyConstants.EmployeeID.ToString() + 
										" GROUP BY plappdate having jobs>0 order by plappdate" : 
											"SELECT plappdate as date, count(booknum) as jobs from pl_recor where plappdate>=date('now', '-7 day') and repnum =  " + MyConstants.EmployeeID.ToString() + 
											" GROUP BY plappdate having jobs>15 order by plappdate";
									cmd.CommandText = sql;
									using (var dateReader = cmd.ExecuteReader())
									{
										if (!dateReader.HasRows)	// no dates -- meaning no jobs in that database -- load TEST database
										{
											// alert the user
											dateReader.Close ();
											dbPath = ServerClientViewController.dbFilePath; // set the database file path to TEST database
											
											dbDate = " '2012-07-24' ";
											this.ReadRunData (dbDate, dbPath); // load TEST database with a set date
										}
										else // there are possible dates
										{
											UIActionSheet chooseRunDate = new UIActionSheet("Please choose a run date.");
											while (dateReader.Read ())
											{
												// debug 
												//if (chooseRunDate.ButtonCount < 1)
												string tmp = String.Format ("{0} ({1} jobs)", ((DateTime)dateReader["date"]).Date.ToString ("yyyy-MM-dd"), dateReader["jobs"]);
												chooseRunDate.Add ( tmp );
											}
											if (!dateReader.IsClosed) dateReader.Close ();
											
											if (chooseRunDate.ButtonCount == 1) // this db has one run on it, we load it
											{
												dbDate = " '" + chooseRunDate.ButtonTitle (0).Substring (0,10) + "' "; // set the dbDate to run date
												MyConstants.DEBUG_TODAY = dbDate;
												ReadRunData (dbDate, dbPath);
											}
											else 
											{
												if (isStarting == 0)
												{
													// if passed 0 (null), reload run for currently selected date  
													ReadRunData(MyConstants.DEBUG_TODAY, dbPath);
												}
												else if (isStarting == 1)
												{
													// if passed 1 (true), do not display a dialog to pick a date as it will lead to a crash
													// instead try to find today's date and load it
													bool foundToday = false;
													for (int btnIndex = 0; btnIndex < chooseRunDate.ButtonCount; btnIndex++)
													{
														// looking for today's date among possible run dates
														if (chooseRunDate.ButtonTitle (btnIndex) == DateTime.Now.Date.ToString ("yyyy-MM-dd"))
														{
															foundToday = true;
															dbDate = " '" + chooseRunDate.ButtonTitle (0).Substring (0,10) + "' "; // set the dbDate to today's run date
															break;
														}
													}

													if (! foundToday) 
														dbDate = " '" + chooseRunDate.ButtonTitle (0).Substring (0,10) + "' "; // set the dbDate to the lowest possible run date value

													MyConstants.DEBUG_TODAY = dbDate;
													ReadRunData (dbDate, dbPath);
												}
												else // isStarting == 2 (false)
												{
													// if passed false, display an action sheet with available dates
													chooseRunDate.Dismissed += HandleChooseRunDateDismissed;
													chooseRunDate.ShowInView (this._table._tabs.View);
												}
												
												// chooseRunDate.ShowInView (this._table._tabs.SelectedViewController.View);
											}
										}
									}								
								} // end try
								catch (Exception e) {
									_table._tabs._scView.Log (String.Format ("Exception when loading database: {0}", e.Message));
									_table.TableView.ReloadData ();
								} // end catch
								
							}	 // END using (var cmd = dbConnection.CreateCommand())
						} // END using (var dbConnection = ...)
					} // END else -- if (employee type is not a plumber)
					_table.TableView.ReloadData ();
					_table._tabs._scView.Log (String.Format ("GetCustomersFromDB: Loaded database: {0}, run date: {1}", Path.GetFileName (dbPath), dbDate));
					this._table._tabs._app.myLocationManager.StartUpdatingLocation ();
					this._table._tabs._app.myLocationManager.StartMonitoringSignificantLocationChanges ();
				} // END if (File.Exists(dbPath))
				else 
				{
					// Database file does not exist
					UIAlertView dbDoesNotExist = new UIAlertView ("Database file does not exist", "File name: "+Path.GetFileName(dbPath), null, "OK");
					dbDoesNotExist.Show ();
				}

			}

			void HandleChooseRunDateDismissed (object sender, UIButtonEventArgs e)
			{
				if (e.ButtonIndex != -1)
				{
					string chosenDate = " '" + (((UIActionSheet)sender).ButtonTitle(e.ButtonIndex)).Substring (0,10) + "' " ;
					MyConstants.DEBUG_TODAY = chosenDate;

					string dbPath = MyConstants.DBReceivedFromServer;
					if (string.IsNullOrEmpty (dbPath) || (! File.Exists (dbPath)))
						dbPath = ServerClientViewController.dbFilePath;

					try {
						ReadRunData (chosenDate, dbPath);
					} catch {
						var readRunDataFailed = new UIAlertView ("Error", "Reading run data has failed. Please try to donwload data from server again", null, "OK");
						readRunDataFailed.Show ();
					}

					_table.TableView.ReloadData ();
				}
				else 
				{
					// 
				}
			}
									
			public override UITableViewCell GetCell (UITableView tableView, NSIndexPath indexPath)
			{
				var cell = tableView.DequeueReusableCell(ReusableJobRunCellID) ?? new UITableViewCell(UITableViewCellStyle.Subtitle, ReusableJobRunCellID);

				// cell.SelectionStyle = UITableViewCellSelectionStyle.Blue; -- DOES NOT WORK in iOS 7 -- it is supposed to do what code below does (sort of)
				if (MyConstants.iOSVersion >= 7) {
					using (var back = new UIView() { BackgroundColor = UIColor.FromRGBA(0,50,220,200) }) {
						back.Layer.MasksToBounds = true;
						cell.SelectedBackgroundView = back;
					}
				}
				cell.TextLabel.Font = UIFont.BoldSystemFontOfSize (20);
				cell.DetailTextLabel.Font = UIFont.SystemFontOfSize (15);
				cell.TextLabel.HighlightedTextColor = UIColor.White;
				cell.DetailTextLabel.HighlightedTextColor = UIColor.White;

				switch (indexPath.Section) {
				
				case 3:
					{
						string caption = (string.IsNullOrEmpty (MyConstants.LastDataExchangeTime)) ? "Unknown" : MyConstants.LastDataExchangeTime;
						cell.TextLabel.Text = caption;
						cell.DetailTextLabel.Text = "Last data exchange time";
						cell.Accessory = UITableViewCellAccessory.None;
						cell.BackgroundColor = UIColor.White;
						break;
					}
				case 2:
					{
						cell.TextLabel.Text = "Tap to add new job";
						cell.DetailTextLabel.Text = "Use with caution :-)";
						cell.Accessory = UITableViewCellAccessory.None;
						cell.BackgroundColor = UIColor.White;
						break;
					}
				case 1:
					{
						if (_table.UserCreatedJobs != null && _table.UserCreatedJobs.Count != 0 
							&& (_table.UserAddedCustomers != null && _table.UserAddedCustomers.Count != 0)) {
							// this should do almost the same stuff that is done for section 0, just looking up customers and jobs in other lists
							Customer c = _table.UserAddedCustomers [indexPath.Row];
							cell.TextLabel.Text = (! c.isCompany) ? String.Format ("{0} {1}",	// FIXED :: was only checking UserCreatedJobs for being not null, but used to draw data from other objects as well (UserAddedCustomers) 
						                                                      /*c.Title,*/c.FirstName, c.LastName) : c.CompanyName;

							cell.DetailTextLabel.Text = _table.UserCreatedJobs [indexPath.Row].JobTime.ToString ("h:mm tt") + "   " + c.Suburb + "\n" + 
								c.Address; // + "   (CN# " + c.CustomerNumber.ToString() + ")";
							cell.DetailTextLabel.Lines = 2;

							_table.UserAddedCustomers [indexPath.Row].HighLighted = false;
							if (_table.UserCreatedJobs [indexPath.Row].JobDone)
								cell.Accessory = UITableViewCellAccessory.Checkmark;
							else
								cell.Accessory = UITableViewCellAccessory.None;

							if (_table.UserCreatedJobs [indexPath.Row].AttentionFlag)
								cell.BackgroundColor = UIColor.Yellow;
							else {
								if (_table.UserAddedCustomers [indexPath.Row].TubingUpgradeDone)
									cell.BackgroundColor = UIColor.White;
								else
									cell.BackgroundColor = UIColor.Cyan;
							}

						}
						break;
					}
				case 0:
					{
						Customer c = _table._customers [indexPath.Row];
						cell.TextLabel.Text = (c.isCompany) ? c.CompanyName : String.Format ("{0} {1}", /*c.Title,*/c.FirstName, c.LastName);

						if (_table.MainJobList.Count > 0) {
							try {
								cell.DetailTextLabel.Text = _table.MainJobList [indexPath.Row].JobTime.ToString ("h:mm tt") + "   " + c.Suburb + "\n" +
									c.Address; // + "   (CN# " + c.CustomerNumber.ToString() + ")";
							} catch {
								cell.DetailTextLabel.Text = "Exception!";
							}
						} else
							cell.DetailTextLabel.Text = "Nothing to see here, move along";
						cell.DetailTextLabel.Lines = 2;

						try {

							if (_table._mainjoblist [indexPath.Row].JobDone)
								cell.Accessory = UITableViewCellAccessory.Checkmark;
							else
								cell.Accessory = UITableViewCellAccessory.None;

							if (_table.MainJobList [indexPath.Row].AttentionFlag)
								cell.BackgroundColor = UIColor.Yellow;
							else {
								if (_table.Customers [indexPath.Row].TubingUpgradeDone)
									cell.BackgroundColor = UIColor.White;
								else
									cell.BackgroundColor = UIColor.Cyan;
							}
						} catch {
							cell.Accessory = UITableViewCellAccessory.None;
							cell.BackgroundColor = UIColor.White;
						}
						break;
					}
				}

				return cell;
			}
			
			public override int NumberOfSections (UITableView tableView)
			{
				return 4;
			}
			
			public override int RowsInSection (UITableView tableview, int section)
			{
				switch (section)
				{
				case 0:	return _table._customers.Count;
				case 1: return (_table.UserCreatedJobs == null)? 0 : _table.UserCreatedJobs.Count;
				default: return 1;
				}
			}
			
			public override bool CanEditRow (UITableView tableView, NSIndexPath indexPath)
			{
				return true;
			}
			
			public override bool CanMoveRow (UITableView tableView, NSIndexPath indexPath)
			{
				if (indexPath.Section > 0) return false;
				else return true;
			}
			
			public override void MoveRow (UITableView tableView, NSIndexPath sourceIndexPath, NSIndexPath destinationIndexPath)
			{
				// move customer record in _customers
				Customer c = _table._customers[sourceIndexPath.Row];
				_table._customers.RemoveAt(sourceIndexPath.Row);
				_table._customers.Insert (destinationIndexPath.Row, c);
				// move job record in _joblist
				Job j = _table._mainjoblist[sourceIndexPath.Row];
				_table._mainjoblist.RemoveAt(sourceIndexPath.Row);
				_table._mainjoblist.Insert(destinationIndexPath.Row, j);
			}
			
			public override void CommitEditingStyle (UITableView tableView, UITableViewCellEditingStyle editingStyle, NSIndexPath indexPath)
			{
				if (editingStyle == UITableViewCellEditingStyle.Delete)
				{
					// delete the client record from WCLIENT : only delete ones with empty history & memos
					if (_table.UserAddedCustomers[indexPath.Row].JobHistory.Count == 0 && _table.UserAddedCustomers[indexPath.Row].CustomerMemos.Count == 0)
						_table._tabs._navWorkflow.EraseCustomerRecordFromDatabase (_table.UserAddedCustomers[indexPath.Row]);

					// remove the customer data from customer list
					_table.UserAddedCustomers.RemoveAt(indexPath.Row);
					// delete everything from PAYMENTS, FEES, FOLLOWUPS, STOCKUSED and PL_RECOR (including child jobs)
					_table._tabs._navWorkflow.EraseMainJobResultsFromDatabase ( _table.UserCreatedJobs[indexPath.Row] );
					// erase the main job record from PL_RECOR
					_table._tabs._navWorkflow.EraseJobRecordFromDatabase (_table.UserCreatedJobs[indexPath.Row]);
					// remove the main job record from job list
					_table.UserCreatedJobs.RemoveAt(indexPath.Row);
					// refresh the table view
					_table.TableView.ReloadData ();
				}
				if (editingStyle == UITableViewCellEditingStyle.Insert) {
					// INSERT USER CREATED JOB
					_table.CreateNewJob();
				}
			}
		} // end class JobRunTableSource : UITableViewDataSource
		
		class JobRunTableDelegate : UITableViewDelegate
		{
			JobRunTable _table;
			
			public JobRunTableDelegate(JobRunTable table)
			{
				_table = table;
			}
			public override UITableViewCellEditingStyle EditingStyleForRow (UITableView tableView, NSIndexPath indexPath)
			{
				switch (indexPath.Section) {
				case 0: return UITableViewCellEditingStyle.None;
				case 1: {
						return UITableViewCellEditingStyle.Delete;
						//return (_table.UserCreatedJobs == null || _table.UserCreatedJobs.Count == 0) ? UITableViewCellEditingStyle.Insert : UITableViewCellEditingStyle.Delete;
					}
				case 2: return UITableViewCellEditingStyle.Insert;
				default : return UITableViewCellEditingStyle.None;
				}
			}
			
			public override void WillDisplay (UITableView tableView, UITableViewCell cell, NSIndexPath indexPath)
			{
				if (tableView.IndexPathForSelectedRow != indexPath) // cell is not the selected cell
				{
					if (_table.HighlightedMode && _table._customers[indexPath.Row].HighLighted) {
						cell.BackgroundColor = UIColor.Green;
					}
					else
					{
						if (cell.BackgroundColor == UIColor.Green)
						{
							cell.BackgroundColor = (_table.MainJobList[indexPath.Row].AttentionFlag)? UIColor.Orange : UIColor.White;
						}
					}
				}
			}

			public void HighlightCustomerRecordsInSummary()
			{
				if (_table._tabs.SelectedViewController is PaymentsSummaryNavigationController)
				{
					if ((_table._tabs.SelectedViewController as PaymentsSummaryNavigationController).TopViewController is PaymentsSummary)
					{
						// _table._tabs._scView.Log ("1");

						_table._tabs._paySummaryView.ClearHighlightedRows ();
						switch (_table._tabs._paySummaryView.SummaryMode)
						{
						case SummaryModes.Money:
							_table._tabs._paySummaryView.HighlightCustomerRows ();
							break;
						case SummaryModes.Stock:
							_table._tabs._paySummaryView.HighlightStockRows ();
							break;
						default : break;
						}
					}
				}
			}

			public override void RowSelected  (UITableView tableView, NSIndexPath indexPath)
			{
				int row = indexPath.Row;
				if (indexPath.Section == 0) {		// Selected a row corresponding to one of the booked jobs 
					_table.CurrentCustomer = _table.Customers[row];
					_table.CurrentJob = _table.MainJobList[row];
					_table.LastSelectedRowPath = indexPath;

					if (_table.HighlightedMode) { 
						_table.HighlightedMode = false;
						_table.TableView.SelectRow (indexPath, true, UITableViewScrollPosition.None);
						_table.TableView.ReloadData ();
					}

					try {						
						// Update the field values in _table._tabs._customersView which will get them updated in the view
						_table._tabs._customersView.SetJobTimeDisabled ();
						_table.ShowCustomerDetails (_table.Customers[row], _table.MainJobList[row]);
						
						if (_table._tabs.Popover != null) 
						{
							_table._tabs.Popover.Dismiss(true);
						}
	
						// DEBUG :: string msg = String.Format("JobRunTableDelegate: Row selected {0}:{1}: Job Booking Number: {2}", indexPath.Section, indexPath.Row, _table.MainJobList[row].JobBookingNumber);
						// DEBUG :: _table._tabs._scView.Log (msg);

						HighlightCustomerRecordsInSummary ();
					} 
					catch (Exception e) { 
						_table._tabs._scView.Log (e.Message);
					}
				}
				if (indexPath.Section == 1)		// Selected a row corresponding to one of the jobs added by the user
				{
					_table.CurrentCustomer = _table.UserAddedCustomers[row];
					_table.CurrentJob = _table.UserCreatedJobs[row];					
					_table.LastSelectedRowPath = indexPath;
					if (_table.HighlightedMode) { 
						_table.HighlightedMode = false;
						_table.TableView.SelectRow (indexPath, true, UITableViewScrollPosition.None);
						_table.TableView.ReloadData ();
					}

					_table.CurrentCustomer = _table.UserAddedCustomers[row];
					_table.ShowCustomerDetails (_table.UserAddedCustomers[row], _table.UserCreatedJobs[row]);

					_table._tabs._customersView.SetJobTimeEnabled ();

					if (_table._tabs.Popover != null) 
					{
						_table._tabs.Popover.Dismiss(true);
					}

					// DEBUG :: string msg = String.Format("JobRunTableDelegate: Row selected {0}:{1}: Job Booking Number: {2}", indexPath.Section, indexPath.Row, _table.UserCreatedJobs[row].JobBookingNumber);
					// DEBUG :: _table._tabs._scView.Log (msg);

					HighlightCustomerRecordsInSummary ();
				}
				if (indexPath.Section == 2) 
				{	
					_table.TableView.DeselectRow (indexPath, true); 
					_table.CreateNewJob ();
				}
				if (indexPath.Section == 3) {
					_table.TableView.DeselectRow (indexPath, true);
				}
            }
			
			/* public override float GetHeightForRow (UITableView tableView, NSIndexPath indexPath)
			{	// This can be used should we decide to switch to custom cells, etc.
				return 44f;
			}*/

			public override NSIndexPath CustomizeMoveTarget (UITableView tableView, NSIndexPath sourceIndexPath, NSIndexPath proposedIndexPath)
			{
				if (sourceIndexPath.Section != proposedIndexPath.Section)
				{
					// if user tries to drag a row from one section to another, show alert that it is not allowed
					var cannotChangeSections = new UIAlertView ("Sorry", "Cells must stay within their table section", null, "OK");
					cannotChangeSections.Show ();
					return sourceIndexPath;
				}
				else return proposedIndexPath;
			}

		} // end class JobRunTableDelegate : UITableViewDelegate
		
		public override void ViewDidLoad ()
        {
            base.ViewDidLoad ();
            Title = "Job Run";

			for (int i = 0; i < _tabs.ViewControllers.Count(); i++) {		// DEBUG:: here we loop through all tabs to get the objects instances up
				_tabs.SelectedViewController = _tabs.ViewControllers[i];	// highlighting customer/stock rows in payments/stock summary will not work if this is commented out
			}
			_tabs.SelectedViewController = _tabs.ViewControllers[0];		// Back to the customers view		

            TableView.Delegate = new JobRunTableDelegate (this);		// set up a table delegate that handles the choosing of rows in the table on the left
            TableView.DataSource = _ds;									// set up a data source for the table;
			TableView.RowHeight = 64;
			JobTypes = MyConstants.GetJobTypesFromDB ();
        }
	} // end class JobRunTable : UITableViewController
	
	public class Customer 
	{
		public bool HighLighted;
		
		private List<HistoryJob> _jobHistory;
		private List<Memo> _customerMemos;
		
		public List<HistoryJob> JobHistory
		{
			get { return _jobHistory; }
			set { _jobHistory = value; }
		}
		public List<Memo> CustomerMemos
		{
			get { return _customerMemos; }
			set { _customerMemos = value; }
		}
		
		public long CustomerNumber;
		public double Lat { get; set; }
		public double Lng { get; set; }

		public bool isCompany { get; set; }
		public long CompanyID { get; set; }
		public string CompanyName { get; set; }

		public bool InvoiceChargesWaived { get; set; }
		public string ChargeType { get; set; }
		public double ChargeAmount { get; set; }

		public double DepositAmount { get; set; }
		public double DepositUsed { get; set; }

		public string Title { get; set; }
		public string FirstName { get; set; }
		public string LastName { get; set; }
		public string Address { get; set; }
		public string Suburb { get; set; }
		public string PhoneNumber { get; set; }
		public string MobileNumber { get; set; }

		public bool TubingUpgradeDone { get; set; }
		public string AttentionReason { get; set; }

		public DateTime LastInstallDate 
		{ 
			get 
			{
				if (this.JobHistory != null)
				{
					DateTime installDate = DateTime.Now;
					foreach(HistoryJob hj in JobHistory)
					{
						if (hj.JobDate < installDate)
							installDate = hj.JobDate;
					}
					return installDate;
				}
				else return DateTime.Now;
			}  
		}

		public string LastJobType { get; set; }
		public string FallbackContact { get; set; }
		public string FallbackPhoneNumber { get; set; }
		
		public int PhotosTakenToday; // defaults to 0
		public List<string> FilesToPrint { get; set; }

		
		public string GetStreet()
		{
			string s = this.Address.Substring(this.Address.IndexOf (" ")+1);
			if (s.IndexOf (" ") > 0)
				s = s.Substring (0, s.IndexOf(" ") );
			return s;
		}
		
		public Customer(long customernumber, 
		                string title, string firstname, string lastname, 
		                string address, string suburb, // postcode ? probably unnecessary
		                string phonenumber, string mobilenumber, 
		                DateTime lastinstalldate, string fbcontact, 
		                string fbphone, long companyID, string companyName, bool tubingDone, 
		                double cLat, double cLng)
		{
			this.HighLighted = false;
			this.CustomerNumber = customernumber;
			this.Title = title;
			this.FirstName = firstname;
			this.LastName = lastname;
			this.Address = address;
			this.Suburb = suburb;
			this.PhoneNumber = phonenumber;
			this.MobileNumber = mobilenumber;
			// this.LastInstallDate = lastinstalldate;
			this.FallbackContact = fbcontact;
			this.FallbackPhoneNumber = fbphone;
			this.CompanyID = companyID;
			this.CompanyName = companyName;
			this.isCompany = (companyName != "");
			this.PhotosTakenToday = 0;
			this.TubingUpgradeDone = tubingDone;
			this.FilesToPrint = new List<string>();
			this.DepositAmount = 0;
			this.DepositUsed = 0;

			this.Lat = cLat;
			this.Lng = cLng;

			if (this.FirstName != "FirstName")	// if this condition is true, it is either a dummy customer or a newly created one by user
			{
				// reading job history
				string dbPath = ServerClientViewController.dbFilePath;
				if (File.Exists( dbPath ))
				{
					// we can pull data from database into JobHistory and CustomerMemos here
					// we have a customer number handy, the database exists and is VERY LIKELY to be already open and connected to
					// if we do that, it would mean that for each customer, we will be querying the database separately
					// memos can be implemented similarly, here in the customer constructor
					
					_jobHistory = new List<HistoryJob> ();
					_customerMemos = new List<Memo> ();
					// create SQLite connection to file and extract data
					
					// reading JOB HISTORY
					using (var connection = new SqliteConnection("Data Source="+dbPath) )
					{
						using (var cmd = connection.CreateCommand())
						{
							connection.Open();
							string sql = 	"SELECT Booknum, Type, Pay_pl, Plappdate FROM Pl_recor " +
												"WHERE cusnum="+this.CustomerNumber.ToString()+
													" AND plappdate != " + MyConstants.DEBUG_TODAY; // Gets all jobs' data except today's job for the customer
							cmd.CommandText = sql;
							using (var reader = cmd.ExecuteReader())
							{
								while (reader.Read () )
								{
									// TODO:: put thorough error handling here (data type checks, default values, etc.)
									long jnum = Convert.ToInt64( (double)reader["booknum"] );
									// long cusnum = (long)reader["cusnum"];
									
									DateTime jDate;
									try 
									{ 
										jDate = (DateTime)reader["plappdate"];
									} 
									catch (System.FormatException) 
									{
										// if we are here, it means that the date in FoxPro database is something like 0000-00-00  00:00:00
										jDate = new DateTime(1990,1,1);
									}
									string jType = (string)reader["type"];

									double money = (double)reader["pay_pl"];
									
									HistoryJob j = new HistoryJob();
									
									j.BookingNumber = jnum;
									j.JobDate = jDate;
									j.MoneyCollected = money;
									j.JobType = new JobType(jType).Description;
																	
									this._jobHistory.Add(j);
								}
								if (this._jobHistory.Count != 0)
									{ this.LastJobType = this._jobHistory[this._jobHistory.Count-1].JobType; }
								if (! reader.IsClosed) reader.Close ();
							}
							// reading MEMOS
							sql = 	"SELECT * FROM Wcmemo " +
												"WHERE cusnum="+this.CustomerNumber.ToString()+" "+
										"ORDER BY wmdate, wctime"; // Gets all memos for the customer
							cmd.CommandText = sql;
							using (var reader = cmd.ExecuteReader())
							{
								while (reader.Read () )
								{
									// TODO:: put thorough error handling here (data type checks, default values, etc.)
									string contents = "";
									if ( reader["wmore"] != null) 
										contents = reader["wmore"].ToString() ;
									Memo m = new Memo(contents);
									m.MemoCustomerNumber = this.CustomerNumber;
									m.MemoNumber = Convert.ToInt64( (double)reader["wmemnum"] );
									m.MemoType = (string)reader["wmtype"];
									m.MemoDescription = (string)reader["wmm"];
									
									DateTime t;
									DateTime.TryParseExact((string)reader["wctime"], "HH:mm:ss", null, System.Globalization.DateTimeStyles.AssumeLocal, out t);
									DateTime d;
									try 
									{
										DateTime.TryParse ( ((DateTime)reader["wmdate"]).ToString(), out d);
									}
									catch (System.FormatException e) {
										d = new DateTime(1990,1,1);
										// Console.WriteLine (String.Format ("{0}: Exception when reading WCMEMO: MemoNumber = {1}, error: {2}", DateTime.Now.ToString("HH:mm:ss"), m.MemoNumber, e.Message));
									}
									
									m.MemoDateEntered = d;
									m.MemoTimeEntered = new DateTime(d.Year, d.Month, d.Day,t.Hour,t.Minute,t.Second);
									
									if ((double)reader["wmemnum"] == MyConstants.DUMMY_MEMO_NUMBER && m.MemoDateEntered.Date == DateTime.Now.Date)
									{
										m.Editable = true;
									}
									else m.Editable = false;
									
									this._customerMemos.Add (m);
								}
								if (! reader.IsClosed) reader.Close ();
							} // end using reader
						} // end using command
					} // end using connection
				} // end File.Exists( dbPath )

				// get the files the could be needed to be reprinted
				string[] files = Directory.GetFiles( Environment.GetFolderPath(Environment.SpecialFolder.Personal), 
				                                    string.Format ("{0}*", CustomerNumber), SearchOption.AllDirectories );
				foreach(string fileName in files)
				{
					if ((fileName.Contains ("Receipt") || fileName.Contains("PrePlumbing")) && !fileName.ToUpper ().Contains ("NOT"))
						FilesToPrint.Add (fileName);
				}

			} // end Customer's FirstName != "FirstName" i. e. this is not a dummy record
		} // end constructor Customer()

		public bool CheckIfInvoiceChargesWaived()
		{
			// this is a stub that serves no purpose now
			// this logic layer was removed along with the invoice charges
			return true;
		}
	} // end class Customer
	
	public class HistoryJob
	{
		public long BookingNumber { get; set; }
		public double MoneyCollected { get; set; }
		public DateTime JobDate { get; set; }
		public string JobType { get; set; }
	}
	
	public class Job 
	{
		public List<Part> UsedParts;  // ? This should probably be saved to and read from database because of potential application restarts
			// On the other hand, the usual workflow goes like this: you start the job, then you either finish it or reset workflow
			// if you reset, there's nothing to save to the database
			// if you finish it, UsedParts are saved when the workflow finishing events fire up
			// when the job has been finished, you CANNOT go in and just have a look (which is when reading the parts from database might come handy)
			// you may, however, reset the workflow which promptly rolls back all the changes made in the database for that job and clears this UsedParts list as well
			// so it seems like there's no reason to implement reading this from database, it simply will not come up
		
		public List<Job> ChildJobs;
		
		public long ParentJobBookingNumber { get; set; }
		
		public long JobBookingNumber { get; set; }	
		public long CustomerNumber { get; set; }
		public long UnitNumber { get; set; }

		public int OrderInRun { get; set; }				// read from PL_RECOR.iPad_Ordering field initially
														// this is updated when job cells are rearranged in JobRunTable

		public DateTime JobTime { get; set; } 			// this is getting replaced by JobTimeStart and JobTimeEnd
		public DateTime JobTimeStart { get; set; }
		public DateTime JobTimeEnd { get; set; }

		public DateTime JobDate { get; set; }
		public double MoneyToCollect { get; set; }
		public double EmployeeFee { get; set; }			// IMPLEMENTED :: this is calculated depending on a myriad of factors
														// the base value is sitting in the JOB_TYPES table
		public bool AttentionFlag { get; set; }
		public string AttentionReason { get; set; }
		
		public bool ShouldPayFee { get; set; }			// just for convenience, we could probably live without this just by setting the EmployeeFee to 0.00
		public bool Warranty { get; set; }
		public bool JobReportAttached { get; set; }
		
		public DateTime JobBookedOn { get; set; }
		public long JobPerformedBy { get; set; }
		public string JobSpecialInstructions { get; set; }
		public string JobPlumbingComments { get; set; }
		public string ContactPerson { get; set; }
		
		// public JobDescription JobTypeDescription { get; set; }
		// public string JobType { get; set; }
		
		public JobType Type { get; set; }
		
		public bool JobDone { get; set; }		// this is a tricky one, it actually answers the question: Has the workflow for this job been finished or not?

		public List<JobPayment> Payments  { get; set; }
		public FollowUpsRequired FollowUpRequired { get; set; }
		
		private MyConstants.JobStarted _started = MyConstants.JobStarted.None;
		public MyConstants.JobStarted Started { 
			// Again, a tricky one, this actually answers the following question: Did the guy actually start the job (in real world)? 
			// Everything except "Yes" (and "None") means "No" and a reason must be specified
			get { return _started; } 
			set { 
				_started = value;
				// If the guy tells us that he could not do a job, then the workflow for this job in the app is finished
//				if (value != MyConstants.JobStarted.Yes && value != MyConstants.JobStarted.None)
//					this.JobDone = true;
				/* else */ 
				if (value == MyConstants.JobStarted.None)
					this.JobDone = false;
				else // if (value == MyConstants.JobStarted.Yes)
					this.JobDone = true; 
			} 
		}
		
		public Job (long cusnum, long jnum, long unitnum, 
		            		DateTime jtime, DateTime jdate, double money, 
		            		DateTime jbOn, long jbBy, string specialinstr, string plc, 
		            		string jdescCode, long parentJobID, bool warranty, bool attention, string cntct, string attreason)
		{	// the most important constructor, used when working with today's jobs
			this.CustomerNumber = cusnum;
			this.JobBookingNumber = jnum;
			this.UnitNumber = unitnum;
			this.JobTime = jtime;
			this.JobDate = jdate;
			this.Warranty = warranty;
			this.MoneyToCollect = money;
			this.JobBookedOn = jbOn;
			this.JobPerformedBy = jbBy;
			this.JobSpecialInstructions = specialinstr;
			this.JobPlumbingComments = plc;
			this.Type = new JobType(jdescCode);
			this.FollowUpRequired = FollowUpsRequired.None;
			this.AttentionFlag = attention;
			this.AttentionReason = attreason;
			this.ContactPerson = cntct;

			this.Payments = new List<JobPayment>();
			this.ChildJobs = new List<Job> ();
			if (parentJobID == 0)  // no parent job, this is the parent that could have child jobs
			{
				// this.ParentJobBookingNumber = -1;
				this.ShouldPayFee = true;
			}
			else 		// has a parent, which means this job cannot have children
			{
				this.ParentJobBookingNumber = parentJobID;
				/* 
				 * THIS LOGIC HAS BEEN RECONSIDERED 15.11.2012
					if (this.Type.Code == "SER") { ShouldPayFee = false; }
					else { 
				 */
				ShouldPayFee = true; 
			}
			
			this.Payments = ReadPaymentsFromDatabase(cusnum, jnum); // this constructor reads payment data from database
			this.UsedParts = ReadPartsFromDatabase(jnum); // this was supposed to read used parts data from database (it doesn't for now)
			this.JobDone = false; // IMPLEMENTED :: this is saved to & read from database (when?), as it is important enough

			this.JobReportAttached = false;
		}
		
		public Job(bool unique)	{ // basic constructor for the class, generates a unique job ID
			if (unique) {
				// generate a unique booking number here
				Random r = new Random(DateTime.Now.Millisecond);
				while (true) {
					int num = r.Next () % 1000;
				
					// if the number generated is unique, break the loop
					if ( JobIDisUnique (num) ) 
					{
						this.JobBookingNumber = num;
						break;
					}
				}
			}
			JobDone = false;
			Warranty = false;
			JobReportAttached = false;
			this.AttentionFlag = false;
			Payments = new List<JobPayment>();
			ChildJobs = new List<Job>();
			UsedParts = new List<Part>();
		}

		public List<JobPayment> ReadPaymentsFromDatabase(long customer_id, long job_id)
		{
			List<JobPayment> result = new List<JobPayment>();
			// read stuff from database here
			string dbPath = ServerClientViewController.dbFilePath;
			if ( File.Exists (dbPath) )
			{
				try {
					using (var connection = new SqliteConnection("Data Source="+dbPath) )
					{
						connection.Open();
						using  (var cmd = connection.CreateCommand())
						{
							string sql = 	"SELECT * FROM Payments WHERE Bookingnum = ?";
							// " AND Cusnum = " + CustomerNumber.ToString(); DOES NOT LIKE CUSNUM THERE?
							cmd.CommandText = sql;
							cmd.Parameters.Add ("@JobID", DbType.Int64).Value = JobBookingNumber;
							using (var reader = cmd.ExecuteReader())
							{
								if (reader.HasRows)
								{
									while (reader.Read ())
									{
										string payType = (string)reader["payment_type"]; // this is saved as MyConstants.OutputStringForValue(PaymentType), i. e. with spaces
										bool found = false;
										PaymentTypes tmpType = PaymentTypes.None;
										foreach(int i in Enum.GetValues (typeof(PaymentTypes) ) )
										{
											tmpType = (PaymentTypes) i;
											if ( MyConstants.OutputStringForValue (tmpType) == payType )
											{
												found = true;
												break;
											}
										}
										if (!found) tmpType = PaymentTypes.None;

										double a = 0;
										Double.TryParse (reader["amount"].ToString(), out a);
										
										string ChequeNumber = (reader["chequenum"] == DBNull.Value) ? "" : (string)reader["chequenum"];
										string CreditCardNumber = (reader["creditcardnum"] == DBNull.Value) ? "" : (string)reader["creditcardnum"];
										string CreditCardExpiry = (reader["crcard_expiry"] == DBNull.Value) ? "" : (string)reader["crcard_expiry"];
										string CreditCardName = (reader["crcard_name"] == DBNull.Value) ? "" : (string)reader["crcard_name"];

										JobPayment jp = new JobPayment();
										jp.Amount = a;
										jp.Type = tmpType;
										jp.PaymentCustomerNumber = customer_id;
										jp.ChequeNumber = ChequeNumber;
										jp.CreditCardNumber = CreditCardNumber;
										jp.CreditCardExpiry = CreditCardExpiry;
										jp.CreditCardName = CreditCardName;

										result.Add (jp);
									}
								}
								else { // payment not found in PAYMENTS table, setting defaults
									result.Add (new JobPayment());
								}
							}
						}
					}
				}
				catch {
				 	//InvokeOnMainThread( delegate {
						UIAlertView failed = new UIAlertView("Failed to read payments data", "", null, "OK");
						failed.Show ();
					//});
					return result;
				}
			}
			else // database not found. setting defaults
			{
				result.Add (new JobPayment());
			}
			return result;
		}
		
		public List<Part> ReadPartsFromDatabase(long jobID)
		{
			var result = new List<Part>();
			/*
			// READING PARTS DATA
			string dbPath = MyConstants.DBReceivedFromServer;

			if (File.Exists(dbPath))
			{
				using (var connection = new SqliteConnection("Data Source="+dbPath) )
				{
					connection.Open ();
					using (var cmd = connection.CreateCommand ())
					{
						sql = "SELECT StockUsed.* " +
						" FROM StockUsed " +
							" WHERE USE_DATE = DATE(" + dbDate + ") " +
							" AND BOOKNUM = "+jobID.ToString ();
						cmd.CommandText = sql;
						cmd.Parameters.Clear ();
						
						using (var partsReader = cmd.ExecuteReader ())
						{
							while (partsReader.Read ())
							{
								double partNumber = (double)partsReader["partno"];
								double quantity = (double)partsReader["num_used"];

								foreach(Part dbPart in )
							}
							if (!partsReader.IsClosed) partsReader.Close ();
						}
					}
				}
			}
			*/
			return result;
		}
		
		public bool JobIDisUnique(long jobID)
		{
			if (jobID == 0) return false;	// job id of 0 causes some funny things to happen on the FoxPro side
			// check if a job with this number already exists				
			string dbPath = ServerClientViewController.dbFilePath;
		
			using (var connection = new SqliteConnection("Data Source="+dbPath) )
			{
				connection.Open();
				var cmd = connection.CreateCommand();
				cmd.CommandText = "SELECT * FROM Pl_recor WHERE Booknum = ?";
				cmd.Parameters.Add ("@JobID", DbType.Int64).Value = jobID;
				var reader = cmd.ExecuteReader();
				return !reader.HasRows;
			}
		}
		
		public bool HasNoParent()
		{ return (this.ParentJobBookingNumber <= 0); }
		public bool HasParent()
		{ return (this.ParentJobBookingNumber > 0); }
		public bool HasChildJobs()
		{ return (this.ChildJobs != null && this.ChildJobs.Count>0); }
		public bool HasNoChildJobs()
		{ return !(this.ChildJobs != null && this.ChildJobs.Count>0); }
		
		public void SetJobType(string code)
		{
			this.Type = new JobType(code);
			// update PL_RECOR here
			if (File.Exists (ServerClientViewController.dbFilePath))
			{
				using (var connection = new SqliteConnection("Data Source="+ServerClientViewController.dbFilePath) )
				{
					connection.Open();
					var cmd = connection.CreateCommand();
					cmd.CommandText = "UPDATE Pl_recor SET Type = ? WHERE Booknum = ?";
					cmd.Parameters.Add ("@Type", DbType.String).Value = this.Type.Code;				
					cmd.Parameters.Add ("@JobID", DbType.Int64).Value = this.JobBookingNumber;
					cmd.ExecuteNonQuery ();
				}
			}
		}
		
		public double TotalToCollect()
		{
			if (this.ChildJobs == null || ChildJobs.Count == 0)
				return MoneyToCollect;
			else 
			{
				double total = MoneyToCollect;
				foreach(Job child in ChildJobs)
				{
					total += child.MoneyToCollect;
				}
				return total;
			}
		}
	}
	
	public class JobPayment
	{
		public long PaymentCustomerNumber;
		public double Amount;
		public PaymentTypes Type;
		public string ChequeNumber;
		public string CreditCardNumber;
		public string CreditCardName;
		public string CreditCardExpiry;
		
		public JobPayment(long CustomerNumber, long JobBookingNumber)
		{
			this.PaymentCustomerNumber = CustomerNumber;
			this.Type = PaymentTypes.None;
			this.Amount = 0;
		}
		
		public JobPayment() {
			Type = PaymentTypes.None;
			Amount = 0;
		}
	}
	
	public class JobType {
		public string Code { get; set; }
		public string Description { get; set; }
		public double RetailPrice { get; set; }
		public double LoyaltyPrice { get; set; }
		public double EmployeeFee { get; set; }
		public bool CanDo { get; set; }
		
		public JobType() {} // turns out that constructor without parameters is useful sometimes
		
		public JobType(string code)
		{
			bool found = false;
			if (JobRunTable.JobTypes == null) 
				JobRunTable.JobTypes = MyConstants.GetJobTypesFromDB();

			if (JobRunTable.JobTypes != null) {
				foreach (JobType jt in JobRunTable.JobTypes) {
					if (jt.Code.ToUpper() == code.ToUpper() || 
							( (code == "SIN" || code == "RAI" || code == "ROOF") && jt.Code == "TWI") || 
							(code == "MIL" && jt.Code == "FIL")) {	// FIXME :: the above is ugly and unacceptable, should be rewritten
						found = true;
						this.Code = jt.Code;
						this.Description = jt.Description;
						this.RetailPrice = jt.RetailPrice;
						this.LoyaltyPrice = jt.LoyaltyPrice;
						this.EmployeeFee = jt.EmployeeFee;
						this.CanDo = jt.CanDo;
						break;
					}
				}
			}

			if (! found)
			{
				Code = "UNK";
				Description = "Unknown";
				CanDo = false;
				// throw new Exception("Job type not found!");
			}
		}
	}

	public class Memo
	{
		public long MemoCustomerNumber { get; set; }
		public long MemoNumber { get; set; }
		public string MemoType { get; set; }
		public string MemoDescription { get; set; }
		public DateTime MemoDateEntered { get; set; }
		public DateTime MemoTimeEntered { get; set; }
		public string MemoContents { get; set; }
		public long MemoRepNum { get; set; }
		public bool Editable { get; set; }
		
		public Memo (string contents)
		{
			this.MemoContents = contents;
		}
	}	
}

