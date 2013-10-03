using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Collections.Generic;
using System.Data;
using MonoTouch.Dialog;
using MonoTouch.UIKit;
using MonoTouch.MessageUI;
using MonoTouch.Foundation;
using Mono.Data.Sqlite;
using ZSDK_Binding;

namespace Puratap
{
	public enum SummaryModes { Money, Stock }

	public class PaymentsSummary : DialogViewController
	{
		public MFMailComposeViewController mail { get; set; }
		public PaymentsSummaryNavigationController Nav { get; set; }
		private EventHandler DailySummaryMail { get; set; }
		private EventHandler WeeklySummaryMail { get; set; }
		private EventHandler PrintDailySummary { get; set; }

		private EventHandler SwitchToOtherSummary { get; set; }
		private EventHandler SwitchToMoney { get; set; }
		private EventHandler SwitchToStock { get; set; }
		
		// private List<JobPayment> Payments;
		private JobRunTable jrt;
		
		private double ClusterFees;
		
		private List<string> SummaryData;
		public SummaryModes SummaryMode;

		private Dictionary<string, int> JobsByType;

		public PaymentsSummary (RootElement root, PaymentsSummaryNavigationController pnav, JobRunTable  JRT) : base (root)
		{
			SummaryMode = SummaryModes.Money;
			jrt = JRT;
			Nav = pnav;
			Root.Caption = "Daily payments summary";
			Root.Add (new Section("Customer#      Type             To collect             Received") );
			Root.Add (new Section("Totals") );
			Root[0].Footer = "";
			// Payments = new List<JobPayment>();
			SummaryData = new List<string>();
			JobsByType = new Dictionary<string, int>();

			PrintDailySummary = delegate {
				if (this.View != null)
				{
					// exception handling here
					string pdfFileName = MyConstants.PreparePDFFileForPrintingAView (this.TableView);
					if (pdfFileName != "")
						BeginPrintingDailySummary (pdfFileName);
					else
					{
						var savingError = new UIAlertView("Failed to generate a summary file for printing.", "", null, "Sad times...");
						savingError.Show ();
					}
				}
			};
			
			DailySummaryMail = delegate {
				if (MFMailComposeViewController.CanSendMail)
				{
					string dailySummaryPath = GenerateDailySummaryFile ();

					NSData fileContents = NSData.FromFile ( dailySummaryPath );

					mail = new MFMailComposeViewController();
					if (fileContents != null)
						mail.AddAttachmentData( fileContents, "text/plain", String.Format ("{0} Daily.txt", DateTime.Now.Date.ToString ("yyyy-MM-dd")) );

					NSAction act = delegate {	};
					
					mail.SetSubject (String.Format ("My Daily summary {0}", DateTime.Now.Date.ToString ("yyyy-MM-dd") ));
					mail.SetToRecipients (new string[] { "myemail@somewhere.com" });
					
					mail.Finished += delegate(object sender, MFComposeResultEventArgs e) {
						if (e.Result == MFMailComposeResult.Sent)
						{
							var alert = new UIAlertView("", "Mail sent", null, "OK");
							alert.Show();				
						}
						
						this.DismissViewController (true, act);				
					};

					this.PresentViewController (mail, true, act);
					// this.PresentModalViewController (mail, true);
				}	
				else 
				{
					var alert = new UIAlertView("", "It seems like this iPad cannot send e-mails at the time. Please check the network settings and try again", null, "OK");
					alert.Show();
				}
			};
			
			WeeklySummaryMail = delegate {
				if (MFMailComposeViewController.CanSendMail)
				{
					UIAlertView notYet = new UIAlertView("Not implemented yet...", "", null, "OK");
					notYet.Show ();
					/*
					mail = new MFMailComposeViewController();
					
					string weekStartDate = DateTime.Now.AddDays (0-DateTime.Now.DayOfWeek).Date.ToString ("yyyy-MM-dd");
					mail.AddAttachmentData( new NSData(), "application/pdf", String.Format ("{0} Weekly.pdf", weekStartDate) );
					
					mail.SetSubject (String.Format ("{0}'s invoice for week starting on {1}", MyConstants.EmployeeName, weekStartDate));
					mail.SetToRecipients (new string[] { "accounting@puratap.com" });

					NSAction act = delegate {	};
					
					mail.Finished += delegate(object sender, MFComposeResultEventArgs e) {
						if (e.Result == MFMailComposeResult.Sent)
						{
							var alert = new UIAlertView("", "Mail sent", null, "OK");
							alert.Show();				
						}
						
						this.DismissViewController (true, act);				
					};
					
					this.PresentModalViewController (mail, true);
					*/
				}	
				else 
				{
					var alert = new UIAlertView("", "It seems like this iPad cannot send e-mails at the time. Please check the network settings and try again", null, "OK");
					alert.Show();
				}
			};

			SwitchToMoney = delegate {
				UIView.BeginAnimations (null);
				// UIView.SetAnimationDuration (0.3f);

				ToolbarItems = new UIBarButtonItem[] {
					new UIBarButtonItem(UIBarButtonSystemItem.FlexibleSpace),
					new UIBarButtonItem("Print this summary", UIBarButtonItemStyle.Bordered, PrintDailySummary),
					new UIBarButtonItem(UIBarButtonSystemItem.FlexibleSpace),
					new UIBarButtonItem("E-m@il daily summary", UIBarButtonItemStyle.Bordered, DailySummaryMail),
					new UIBarButtonItem(UIBarButtonSystemItem.FlexibleSpace),
					new UIBarButtonItem("Switch to stock", UIBarButtonItemStyle.Bordered, SwitchToStock),
					new UIBarButtonItem(UIBarButtonSystemItem.FlexibleSpace),
				};
				this.SummaryMode = SummaryModes.Money;
				this.ViewDidAppear (false);

				UIView.CommitAnimations ();
			};

			SwitchToStock = delegate {
				UIView.BeginAnimations (null);
				// UIView.SetAnimationDuration (0.5f);

				ToolbarItems = new UIBarButtonItem[] {
					new UIBarButtonItem(UIBarButtonSystemItem.FlexibleSpace),
					new UIBarButtonItem("Print this summary", UIBarButtonItemStyle.Bordered, PrintDailySummary),
					new UIBarButtonItem(UIBarButtonSystemItem.FlexibleSpace),
					new UIBarButtonItem("E-m@il daily summary", UIBarButtonItemStyle.Bordered, DailySummaryMail),
					new UIBarButtonItem(UIBarButtonSystemItem.FlexibleSpace),
					new UIBarButtonItem("Switch to money", UIBarButtonItemStyle.Bordered, SwitchToMoney),
					new UIBarButtonItem(UIBarButtonSystemItem.FlexibleSpace),
				};			
				this.SummaryMode = SummaryModes.Stock;
				this.ViewDidAppear (false);
				this.ReloadData ();

				UIView.CommitAnimations ();
			}; 
			
			ToolbarItems = new UIBarButtonItem[] {
				new UIBarButtonItem(UIBarButtonSystemItem.FlexibleSpace),
				new UIBarButtonItem("Print this summary", UIBarButtonItemStyle.Bordered, PrintDailySummary),
				new UIBarButtonItem(UIBarButtonSystemItem.FlexibleSpace),
				new UIBarButtonItem("E-m@il daily summary", UIBarButtonItemStyle.Bordered, DailySummaryMail),
				new UIBarButtonItem(UIBarButtonSystemItem.FlexibleSpace),
				new UIBarButtonItem("Switch to stock", UIBarButtonItemStyle.Bordered, SwitchToStock),
				new UIBarButtonItem(UIBarButtonSystemItem.FlexibleSpace),
			};
		}

		private void BeginPrintingDailySummary(string pdfFileName)
		{
			ThreadStart tsPrinting = new ThreadStart( delegate {
				LoadingView printing = new LoadingView();
				printing.Show ("Printing, please wait");
				
				var printSummaryDone = new ManualResetEvent(false);
				bool printSummaryOK = false;
				
				ThreadStart tsPrintSummary = new ThreadStart( delegate {
					printSummaryOK = MyConstants.PrintPDFFile (pdfFileName);
					printSummaryDone.Set ();
				});
				Thread tPrintSummary = new Thread(tsPrintSummary);
				tPrintSummary.Start ();
				
				printSummaryDone.WaitOne (5000);
				if (tPrintSummary.ThreadState == ThreadState.Running) tPrintSummary.Abort ();
				
				if (!printSummaryOK)
				{
					InvokeOnMainThread (delegate {
						printing.Hide ();
						var printError = new UIAlertView("Failed to print summary", "We are sorry!", null, "OK");
						printError.Show ();
					});
				}
				else {
					InvokeOnMainThread (delegate { printing.Hide (); });
				}
			});

			Thread tPrinting = new Thread(tsPrinting);
			tPrinting.Start ();
		}

		public string GenerateDailySummaryFile(string date)
		{
			try {
				if (SummaryData.Count > 9 || MyConstants.EmployeeType == MyConstants.EmployeeTypes.Plumber) 
					// to prevent the app from creating empty (or almost empty) summary files, which lead to issues with the data exchange
				{
					// write the generated SummaryData to a summary file here
					string filePath = String.Format ("{0} {1} {2} Daily Summary.txt", MyConstants.EmployeeID, MyConstants.EmployeeName, date.Substring (2,10) );
					filePath = Path.Combine (Environment.GetFolderPath (Environment.SpecialFolder.Personal), filePath);
					this.jrt._tabs._scView.Log (String.Format ("Writing summaries..."));

					File.WriteAllLines (filePath, SummaryData);				
									
					// TODO :: append generated WeeklySummary entry to weekly summary file here

					string result = filePath;
					return result;
				}
				else return "";
			} catch
			{
				return "";
			}
		}

		public string GenerateDailySummaryFile()
		{
			try 
			{
				if (SummaryData.Count > 9) // to prevent the app from creating empty (or almost empty) summary files, which lead to issues with the data exchange
				{
					// write the generated SummaryData to a summary file here
					string filePath = String.Format ("{0} {1} {2} Daily Summary.txt", MyConstants.EmployeeID, MyConstants.EmployeeName, MyConstants.DEBUG_TODAY.Substring (2,10) );
					filePath = Path.Combine (Environment.GetFolderPath (Environment.SpecialFolder.Personal), filePath);

					File.WriteAllLines (filePath, SummaryData);

					// TODO :: append generated WeeklySummary entry to weekly summary file here 

					string result = filePath;
					return result;
				}
				else return "";
			} catch 
			{
				return "";
			}
		}

		public bool GenerateAllSummaryFiles()
		{
			try {
				string dbPath = MyConstants.DBReceivedFromServer; // concern
				var dbDates = this.jrt._ds.GetRunDatesFromDB (dbPath);
				if (dbDates != null)
				{
					foreach(string dbDate in dbDates)
					{
						jrt.Customers = new List<Customer> ();
						jrt.UserAddedCustomers = new List<Customer> ();
						jrt.MainJobList = new List<Job> ();
						jrt.UserCreatedJobs = new List<Job> ();					

						this.jrt._ds.ReadRunData (dbDate, dbPath);
						this.SummaryMode = SummaryModes.Money;
						this.ViewDidAppear (false);

						// string summary = String.Join (String.Empty, SummaryData.ToArray ());

						GenerateDailySummaryFile (dbDate);
					}
				}
				return true;
			}
			catch (Exception e) {
				this.jrt._tabs._scView.Log ("Exception : "+e.Message+"\n"+e.StackTrace);
				return false;
			}
		}

		public string AppendToWeeklySummary()
		{
			return "";
			// TODO :: implement this
		}
		
		public override void ViewDidAppear (bool animated)
		{
			switch (SummaryMode) 
			{
				case SummaryModes.Money : 
				{ 
					string currentRunDate = MyConstants.DEBUG_TODAY.Substring (2,10);
					currentRunDate = DateTime.ParseExact ( currentRunDate, "yyyy-MM-dd", 
				                                      System.Globalization.CultureInfo.InvariantCulture).ToLongDateString ();

					SummaryData.Clear (); 
					JobsByType.Clear ();
					Root.Clear ();
					Root.Caption = "Daily payments summary";
					Root.Add (new Section(currentRunDate + "\n\nCustomer#      Type             To collect             Received                          Fees") );
					
					this.Root.Add (new Section("Totals for " + currentRunDate));

					Root[0].Footer = "";
					// Root[0].Clear (); Root[1].Clear ();
					double TotalAmount=0, TotalFees=0, totalCash=0, totalCheques=0, totalEFTPOS=0, totalInvoices=0;
					foreach(Job j in jrt.MainJobList)
					{
						if (j.JobDone == true)
						{
							if (j.Started == MyConstants.JobStarted.Yes || j.Started == MyConstants.JobStarted.None) // this tells us that a job was started and done, so we're safe adding it to jobs done by type list
							{
								if ( JobsByType.ContainsKey (j.Type.Description)) 
									JobsByType[j.Type.Description]++;
								else JobsByType.Add (j.Type.Description, 1);								
							}

							ClusterFees = GetClusterFees (j.JobBookingNumber);
							TotalFees += ClusterFees;
							foreach(Job childJob in j.ChildJobs)
							{
								if ( JobsByType.ContainsKey (childJob.Type.Description))
									JobsByType[childJob.Type.Description]++;
								else JobsByType.Add (childJob.Type.Description, 1);																					
							}

							// Payments.Add (j.Payments); // IMPLEMENTED:: since jobs are now loaded from database, the payment data is loaded from DB as well
							if (j.Payments.Count == 1)
							{
								Root[0].Add (new StyledStringElement(
									GenerateString(j.CustomerNumber, j.Payments[0].Type, j.TotalToCollect (), j.Payments[0].Amount, true), 
									String.Format ("${0:0.00}", ClusterFees) ) );
								SummaryData.Add ( GenerateString(j.CustomerNumber, j.Payments[0].Type, j.TotalToCollect (), j.Payments[0].Amount, false) );
								
								// add to totals
								TotalAmount += j.Payments[0].Amount;
								switch (j.Payments[0].Type)
								{
								case PaymentTypes.Cash : { totalCash += j.Payments[0].Amount; break; }
								case PaymentTypes.Cheque : { totalCheques += j.Payments[0].Amount; break; }
								case PaymentTypes.EFTPOS : { totalEFTPOS += j.Payments[0].Amount; break; }
								case PaymentTypes.Invoice : { totalInvoices += j.TotalToCollect(); break; }
								case PaymentTypes.CCDetails : { totalInvoices += j.TotalToCollect(); break; }
								case PaymentTypes.CreditCard : { totalInvoices += j.TotalToCollect(); break; }
								default : { break; }
								}
							}
							else if (j.Payments.Count > 1)
							{	// job had a split payment or no payments recorded
								double totalCollectedForCustomer = 0;
								foreach (JobPayment payment in j.Payments)
								{
									TotalAmount += payment.Amount;
									totalCollectedForCustomer += payment.Amount;
									switch (payment.Type)
									{
									case PaymentTypes.Cash : { totalCash += payment.Amount; break; }
									case PaymentTypes.Cheque : { totalCheques += payment.Amount; break; }
									case PaymentTypes.EFTPOS : { totalEFTPOS += payment.Amount; break; }
									default : { break; }
									}
								}
									Root[0].Add (new StyledStringElement(
									GenerateString(j.CustomerNumber, PaymentTypes.Split, j.TotalToCollect (), totalCollectedForCustomer, true), 
									String.Format ("${0:0.00}", ClusterFees) ) );
								SummaryData.Add ( GenerateString(j.CustomerNumber, PaymentTypes.Split, j.TotalToCollect (), totalCollectedForCustomer, false) );								
							}
							else {
								// no payments recorded
								Root[0].Add (new StyledStringElement(
									GenerateString(j.CustomerNumber, PaymentTypes.None, j.TotalToCollect (), 0, true), 
									String.Format ("${0:0.00}", ClusterFees) ) );
								SummaryData.Add ( GenerateString(j.CustomerNumber, PaymentTypes.None, j.TotalToCollect (), 0, false) );								
							}
						}
					}
					
					if (jrt.UserCreatedJobs != null)
					{
						foreach(Job j in jrt.UserCreatedJobs)
						{
							if (j.JobDone == true)
							{
								if (j.Started == MyConstants.JobStarted.Yes || j.Started == MyConstants.JobStarted.None) // this tells us that a job was started and done, so we're safe adding it to jobs done by type list
								{
									if ( JobsByType.ContainsKey (j.Type.Description))
										JobsByType[j.Type.Description]++;
									else JobsByType.Add (j.Type.Description, 1);									
								}

								ClusterFees = GetClusterFees (j.JobBookingNumber);
								TotalFees += ClusterFees;
								if (j.ChildJobs != null)
								{
									foreach(Job childJob in j.ChildJobs)
									{
										if ( JobsByType.ContainsKey (childJob.Type.Description))
											JobsByType[childJob.Type.Description]++;
										else JobsByType.Add (childJob.Type.Description, 1);
									}
								}

								// Payments.Add (j.Payments); // IMPLEMENTED:: since jobs are now loaded from database, the payment data is loaded from DB as well
								if (j.Payments.Count == 1)
								{
									Root[0].Add (new StyledStringElement(
										GenerateString(j.CustomerNumber, j.Payments[0].Type, j.TotalToCollect (), j.Payments[0].Amount, true), 
										String.Format ("${0:0.00}", ClusterFees) ) );
									SummaryData.Add ( GenerateString(j.CustomerNumber, j.Payments[0].Type, j.TotalToCollect (), j.Payments[0].Amount, false) );
									TotalAmount += j.Payments[0].Amount;
									switch (j.Payments[0].Type)
									{
									case PaymentTypes.Cash : { totalCash += j.Payments[0].Amount; break; }
									case PaymentTypes.Cheque : { totalCheques += j.Payments[0].Amount; break; }
									case PaymentTypes.EFTPOS : { totalEFTPOS += j.Payments[0].Amount; break; }
									case PaymentTypes.Invoice : { totalInvoices += j.TotalToCollect(); break; }
									case PaymentTypes.CCDetails : { totalInvoices += j.TotalToCollect(); break; }
									case PaymentTypes.CreditCard : { totalInvoices += j.TotalToCollect(); break; }
									default : { break; }
									}
								}
								else if (j.Payments.Count > 1)
								{	// job had a split payment or no payments recorded
									double totalCollectedForCustomer = 0;
									foreach (JobPayment payment in j.Payments)
									{
										TotalAmount += payment.Amount;
										totalCollectedForCustomer += payment.Amount;
										switch (payment.Type)
										{
										case PaymentTypes.Cash : { totalCash += payment.Amount; break; }
										case PaymentTypes.Cheque : { totalCheques += payment.Amount; break; }
										case PaymentTypes.EFTPOS : { totalEFTPOS += payment.Amount; break; }
										default : { break; }
										}
									}
									Root[0].Add (new StyledStringElement(
										GenerateString(j.CustomerNumber, PaymentTypes.Split, j.TotalToCollect (), totalCollectedForCustomer, true), 
										String.Format ("${0:0.00}", ClusterFees) ) );
									SummaryData.Add ( GenerateString(j.CustomerNumber, PaymentTypes.Split, j.TotalToCollect (), totalCollectedForCustomer, false) );								
								}
								else {
									// no payments recorded
									Root[0].Add (new StyledStringElement(
										GenerateString(j.CustomerNumber, PaymentTypes.None, j.TotalToCollect (), 0, true), 
										String.Format ("${0:0.00}", ClusterFees) ) );
									SummaryData.Add ( GenerateString(j.CustomerNumber, PaymentTypes.None, j.TotalToCollect (), 0, false) );								
								}
							}							
						}
					}
					// Root[0].Footer = String.Format ("Total: ${0:0.00}                   Total fees: ${1:0.00}", TotalAmount, TotalFees);
					Root[1].Add (new StyledStringElement("Cash", String.Format ("${0:0.00}", totalCash)) );
					Root[1].Add (new StyledStringElement("Cheques", String.Format ("${0:0.00}", totalCheques)) );
					Root[1].Add (new StyledStringElement("EFT POS", String.Format ("${0:0.00}", totalEFTPOS)) );
					Root[1].Add (new StyledStringElement("Invoices", String.Format ("${0:0.00}", totalInvoices)) );
					Root[1].Add (new StyledStringElement("Total money", String.Format ("${0:0.00}", TotalAmount)) );
					Root[1].Add (new StyledStringElement("Total fees", String.Format ("${0:0.00}", TotalFees)) );
					this.ReloadData ();
					
					SummaryData.Add ("------------------------------------------------------------\r\nTOTALS:\r\n");
					SummaryData.Add (String.Format ("Total cash: ${0:0.00}", totalCash));
					SummaryData.Add (String.Format ("Total cheques: ${0:0.00}", totalCheques));
					SummaryData.Add (String.Format ("Total EFT POS: ${0:0.00}", totalEFTPOS));
					SummaryData.Add (String.Format ("Total invoices: ${0:0.00}", totalInvoices));
					SummaryData.Add (String.Format ("Total money: ${0:0.00}", TotalAmount));
					SummaryData.Add (String.Format ("Total fees: ${0:0.00}", TotalFees));
					SummaryData.Add ("\n------------------------------------------------------------\r\nJobs done by type:\r\n");
					
					Section jobs = new Section("Jobs done by type");
					foreach(var pair in JobsByType)
					{
						jobs.Add (new StyledStringElement(pair.Key, pair.Value.ToString ()));
						SummaryData.Add ( String.Format ("{0}: {1}", pair.Key, pair.Value) );
					}
					if (jobs.Count > 0) Root.Add (jobs);
					
					// base.ViewDidAppear (animated);
					Nav.SetToolbarHidden (false, true);
					Nav.SetToolbarItems (this.ToolbarItems, true);				
					break; 
				}
				case SummaryModes.Stock : 
				{ 
					this.Root = new RootElement("Used stock");
					string currentRunDate = MyConstants.DEBUG_TODAY.Substring (2,10);
					currentRunDate = DateTime.ParseExact ( currentRunDate, "yyyy-MM-dd", 
				                                      System.Globalization.CultureInfo.InvariantCulture).ToLongDateString ();
					this.Root.Add (new Section("Parts used on " + currentRunDate ));

					if (File.Exists (ServerClientViewController.dbFilePath) )
					{
						// read the data from database here
						using (var connection = new SqliteConnection("Data Source="+ServerClientViewController.dbFilePath) )
						{
							using (var cmd = connection.CreateCommand())
							{
								connection.Open();
								cmd.CommandText = "SELECT PARTS.PartNo, PARTS.PrtDesc, " +
									" SUM(STOCKUSED.Num_Used) as USED_TODAY " +
									" FROM STOCKUSED, PARTS " +
									" WHERE STOCKUSED.PartNo=PARTS.PartNo " +
										" AND DATE(STOCKUSED.USE_DATE) = " + MyConstants.DEBUG_TODAY +
									" GROUP BY PARTS.PartNo, PARTS.PrtDesc";
								try 
								{
									using (var reader = cmd.ExecuteReader())
									{
										while ( reader.Read() )
										{
											double partnum = (double) (reader["partno"]);
											string description = (string) reader["prtdesc"];
											double used = Convert.ToDouble (reader["used_today"]);

											this.Root[0].Add ( new StyledStringElement(
												partnum.ToString() + " " + description,
												used.ToString(), 
												UITableViewCellStyle.Value1));
										}
									}
								}
								catch (Exception e) {
									// Console.WriteLine (e.Message);
								} 
							} // END using cmd
						}	// END using connection
					}					
					this.ReloadData ();
					Nav.SetToolbarHidden (false, true);
					Nav.SetToolbarItems (this.ToolbarItems, true);				
					break; 
				} // END case SummaryModes.Stock : 
			} // END switch (SummaryMode)

		} // END ViewDidAppear()

		public double GetClusterFees(long clusterID)
		{
			double result = 0;
			foreach (Job j in jrt.MainJobList) {
				if (j.JobBookingNumber == clusterID) {
					if (j.HasChildJobs ()) {
						foreach (Job child in j.ChildJobs) {
							if (child.ShouldPayFee)
								result += child.EmployeeFee;
						}
					}
					if (j.ShouldPayFee)
						result += j.EmployeeFee;
				}
			}

			if (result < 0.01)
			{
				foreach (Job j in jrt.UserCreatedJobs) {
					if (j.JobBookingNumber == clusterID) {
						if (j.HasChildJobs ()) {
							foreach (Job child in j.ChildJobs) {
								if (child.ShouldPayFee)
									result += child.EmployeeFee;
							}
						}

						if (j.ShouldPayFee)
							result += j.EmployeeFee;
					}
				}
			}

			return result;
		}
		
		public string GenerateString(long CustomerNumber, PaymentTypes PaymentType, double toCollect, double Amount, bool visual)
		{
			if (visual)
			{
				// TODO :: Rewrite this routine to implement proper alignment of text
				// i hate this padding shit... feels so much like a temporary thing
				string cusnum = "  "+CustomerNumber.ToString ();
				string paytype = MyConstants.OutputCodeForPaymentType(PaymentType);
				string tocollect = String.Format ("${0}", toCollect);
				string amount = String.Format ("${0}", Amount);
				cusnum = cusnum.PadRight(20);
				paytype = paytype.PadRight (20);
				tocollect = tocollect.PadLeft (4);
				amount = amount.PadLeft (20);
				
				return String.Format ("{0} {1} {2} {3}", cusnum, paytype, tocollect, amount);
			}
			else 
			{
				return String.Format("{0}\t{1}\t${2}\t${3}\t${4}\r\n", CustomerNumber, MyConstants.OutputCodeForPaymentType(PaymentType), toCollect, Amount, ClusterFees);
			}
		}

		// when the user selects a customer in the run table, the rows in the summary that have that customer number are highlighted 
		public void HighlightCustomerRows()
		{
			// payments section rows
			foreach(StyledStringElement element in Root[0])
			{
				if (element.Caption.TrimStart (' ').StartsWith (jrt.CurrentCustomer.CustomerNumber.ToString()))
				{
					element.BackgroundColor = UIColor.Blue;
					element.TextColor = UIColor.White;
					element.DetailColor = UIColor.White;

					// jobs by type section
					foreach(StyledStringElement jobTypeElement in Root[2])
					{
						if (jobTypeElement.Caption.StartsWith (jrt.CurrentJob.Type.Description))
						{
							jobTypeElement.BackgroundColor = UIColor.Blue;
							jobTypeElement.TextColor = UIColor.White;
							jobTypeElement.DetailColor = UIColor.White;
						}

						foreach(Job childJob in jrt.CurrentJob.ChildJobs)
						{
							if (jobTypeElement.Caption.StartsWith (childJob.Type.Description))
							{
								jobTypeElement.BackgroundColor = UIColor.Blue;
								jobTypeElement.TextColor = UIColor.White;
								jobTypeElement.DetailColor = UIColor.White;
							}
						}
					}

				}
			}

			this.ReloadData ();
		}

		public void HighlightStockRows()
		{
			foreach (StyledStringElement element in Root[0])
			{
				// main job parts
				foreach(Part part in jrt.CurrentJob.UsedParts)
				{
					if (element.Caption.StartsWith (part.PartNo.ToString ()))
					{
						element.TextColor = UIColor.White;
						element.DetailColor = UIColor.White;
						element.BackgroundColor = UIColor.Blue;
					}
				}

				// child jobs' parts
				foreach(Job childJob in jrt.CurrentJob.ChildJobs)
				{
					foreach(Part part in childJob.UsedParts)
					{
						if (element.Caption.StartsWith (part.PartNo.ToString ()))
						{
							element.TextColor = UIColor.White;
							element.DetailColor = UIColor.White;
							element.BackgroundColor = UIColor.Blue;
						}
					}
				}
			}
		}

		public void ClearHighlightedRows()
		{
			foreach(Section section in Root)
				foreach(StyledStringElement element in section)
				{
					element.BackgroundColor = UIColor.White;
					element.TextColor = UIColor.Black;
					element.DetailColor = UIColor.Gray;
				}
			this.ReloadData ();
		}
		
		public override void ViewDidDisappear (bool animated)
		{
			base.ViewDidDisappear (animated);
			this.NavigationController.ToolbarHidden = true;
		}
		
		public override void ViewDidLoad ()
		{
			base.ViewDidLoad ();
			this.Title = "Summary";

			using (var image = UIImage.FromBundle ("/Images/162-receipt") )	this.TabBarItem.Image = image;
		}
	}
}

