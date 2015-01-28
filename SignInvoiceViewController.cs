using System;
using CoreGraphics;
using UIKit;
using MessageUI;
using Foundation;
using System.IO;
using System.Threading;
using System.Text.RegularExpressions;
using Mono.Data.Sqlite;

namespace Puratap
{
	public class SignInvoiceViewController : NewSignatureViewController
	{
		public EventHandler GoBack { get; set; }
		public EventHandler GoForward { get; set; }
		public EventHandler SkipSigning { get; set; }
		public EventHandler StartSigning { get; set; }
		public EventHandler FinishSigning { get; set; }
		public EventHandler DoSendReceiptEmail { get; set; }
		
		public bool SkippedSigning { get; set; }
		
		UIBarButtonItem back;
		UIBarButtonItem forward;
		UIBarButtonItem skipSigning;		
		UIBarButtonItem signing;		
		UIBarButtonItem clearSignature;
		UIBarButtonItem email;

		UIAlertView skipAlert;
		UIAlertView getEmailAddressView;
		private static string emailRecepients { get; set; }
		private bool sendingEmailCancelled { get; set; }

		private volatile bool printOK;

		public ManualResetEvent PrePlumbingPrintingDone = new ManualResetEvent(false);
		public ManualResetEvent ReceiptPrintingDone = new ManualResetEvent(false);

		public SignInvoiceViewController (DetailedTabs tabs) : base (tabs)
		{
			this.Title = "Sign receipt";
			using (var image = UIImage.FromBundle ("Images/187-pencil") ) this.TabBarItem.Image = image;
			this.NavigationItem.HidesBackButton = true;
			
			GoBack = delegate {
				if (SigningMode)
				{
					FinishSigning(null,null);
					hasBeenSigned = false;
				}
				
				// if service should be signed, push service
				bool ShouldSignService = false;
				Job main = (Tabs._jobRunTable.CurrentJob.HasParent ()) ?  Tabs._jobRunTable.FindParentJob (Tabs._jobRunTable.CurrentJob) : Tabs._jobRunTable.CurrentJob;

				if (main.JobReportAttached) ShouldSignService = true;
				foreach(Job child in main.ChildJobs)
				{
					if (child.JobReportAttached) ShouldSignService = true;
					break;
				}
				/*
				if (main.Type.Code == "SER") ShouldSignService = true;
				foreach(Job child in main.ChildJobs)
					if (child.Type.Code == "SER") ShouldSignService = true;
				*/

				if (ShouldSignService)
				{
					Tabs.SigningNav.PopToRootViewController (false);
					Tabs.SigningNav.PushViewController (Tabs.SignService, true);
				}
				
				// else if pre-plumbing should be signed, push pre-plumbing
				else if ( ! Tabs._prePlumbView.IsDefault () )
				{
					// push pre-plumbing check
					Tabs.SigningNav.PopToRootViewController (false);
					Tabs.SigningNav.PushViewController (Tabs.SignPre, true);
				}
				else 		// otherwise, return back to payment screen
				{
					Tabs.SigningNav.PopToRootViewController (false);
					Tabs.SelectedViewController = Tabs.ViewControllers[Tabs.LastSelectedTab];
				}
			};
			
			GoForward = delegate {								
				if ( (this.hasBeenSigned && !SigningMode) || SkippedSigning)
				{
					//string currentNetwork = MyConstants.GetCurrentWiFiNetworkID ();
					//bool networkOK = (currentNetwork == "247");
					//this.Tabs._scView.Log (String.Format("Current network returned by MyConstants.GetCurrentWiFiNetworkID : {0}", currentNetwork));

					//networkOK = true; // debug

					if (true) // networkOK
					{
						// AllPrintingDone.Reset ();

						BeginPrinting ();
						// this starts a new thread to handle the printing task
					}
					/*
					else
					{
						// we're connected to another network, alert the user
						var networkAlert = new UIAlertView( String.Format("Your iPad is connected to \"{0}\"", "currentNetwork"), 
						                            "You have to go to Settings -> Wi-Fi and connect to printer network (\"247\") for printing to work. What would you like to do?", null, 
						                            "Skip printing", "Change network");
						networkAlert.Dismissed += HandleNetworkAlertDismissed;
						networkAlert.Show ();
					}
					*/
				}
				else
				{
					var alert = new UIAlertView("", "Please finish signing the document first", null, "OK");
					alert.Show();					
				}
				
				if (SkippedSigning)
				{
					// Here we should take whatever measures to somehow record that the receipt has not been signed by the customer
					SkippedSigning = false;
				}

			};

			ClearSignature = delegate {
				// Signature.Image = new UIImage();
				Signature.Clear ();
				hasBeenSigned = false;
			};
			
			SkipSigning = delegate {
				skipAlert = new UIAlertView("Are you sure?", "The receipt will be printed out without the customer signature.", null, "No, never mind", "Yes, please");
				skipAlert.Dismissed += HandleSkipAlertDismissed;
				skipAlert.Show ();
			};
			
			StartSigning = delegate {
				signing = new UIBarButtonItem("Done", UIBarButtonItemStyle.Done, FinishSigning);
				this.SetToolbarItems (new UIBarButtonItem[] {
						back, 				new UIBarButtonItem(UIBarButtonSystemItem.FlexibleSpace),
						clearSignature,		new UIBarButtonItem(UIBarButtonSystemItem.FlexibleSpace),
						signing,			new UIBarButtonItem(UIBarButtonSystemItem.FlexibleSpace),
						skipSigning,		new UIBarButtonItem(UIBarButtonSystemItem.FlexibleSpace),
						forward
					}, true );

				Signature.Clear ();
				hasBeenSigned = false;
				SigningMode = true;

				this.NavigationController.SetNavigationBarHidden (false, true);
				this.NavigationItem.SetLeftBarButtonItem(new UIBarButtonItem("Clear Signature", UIBarButtonItemStyle.Plain, ClearSignature), true);
				this.NavigationItem.SetRightBarButtonItem(new UIBarButtonItem("Done", UIBarButtonItemStyle.Done, FinishSigning), true);

//				this.NavigationItem.SetRightBarButtonItems (new UIBarButtonItem[] { 
//					new UIBarButtonItem("Done", UIBarButtonItemStyle.Done, FinishSigning), 
//					new UIBarButtonItem("Clear Signature", UIBarButtonItemStyle.Bordered, ClearSignature) }, true);

				this.NavigationController.SetToolbarHidden (true, true);
			};
			
			FinishSigning = delegate {
				SigningMode = false;
				signing = new UIBarButtonItem("Start signing", UIBarButtonItemStyle.Plain, StartSigning);				

				this.SetToolbarItems (new UIBarButtonItem[] {
						back, 				new UIBarButtonItem(UIBarButtonSystemItem.FlexibleSpace),
						signing,			new UIBarButtonItem(UIBarButtonSystemItem.FlexibleSpace),
						email,			new UIBarButtonItem(UIBarButtonSystemItem.FlexibleSpace),
						skipSigning,		new UIBarButtonItem(UIBarButtonSystemItem.FlexibleSpace),
						forward
					}, true );

				this.NavigationItem.SetLeftBarButtonItems  (new UIBarButtonItem[] { }, true );
				this.NavigationItem.SetRightBarButtonItems (new UIBarButtonItem[] { }, true );
				this.NavigationController.SetToolbarHidden (false, true);
				
				UIImageView iv = new UIImageView();
				iv = (UIImageView)Tabs._payment.GeneratedPdfView.ViewWithTag(MyConstants.ReceiptPDFTemplateTags.Signature);
				iv.ContentMode = UIViewContentMode.ScaleAspectFit;
				UIImage im = Signature.GetDrawingImage (); // this.Signature.Image;
				iv.Image = im;
				
				if (hasBeenSigned)
				{
					PDFView.ScrollView.ScrollsToTop = false;
					Tabs._payment.RedrawReceiptPDF (true);
					// Signature.Image = new UIImage();
					Signature.Clear ();
				}

				if (iv != null) { iv.Dispose (); iv = null; }
				if (im != null) { im.Dispose (); im = null; }
			};
				
			DoSendReceiptEmail = delegate {
				if (MFMailComposeViewController.CanSendMail) {
					// check if there is an existing email address on file
					string existingEmail = Tabs._jobRunTable.CurrentCustomer.EmailAddress;
					if (ValidateEmailAddress(existingEmail)) {
						// there is an email address on file
						var alertUseExistingEmail = new UIAlertView("Use this email address?", existingEmail, null, "No", "Yes");
						alertUseExistingEmail.Dismissed += delegate (object sender, UIButtonEventArgs e) {
							if (e.ButtonIndex == 1) {
								// user confirmed using existing email
								emailRecepients = existingEmail;
								SaveEmailAndPresentMailComposingView();
							} else {
								// user declined using existing email, collect email address
								PresentGetEmailAddressView();
							}
						};
						alertUseExistingEmail.Show();
					} else {
						// collect email address
						PresentGetEmailAddressView();
					}
				} else {
					var alertCannotSendEmails = new UIAlertView ("", "It seems like this iPad cannot send e-mails at the time. Please check the network settings and try again", null, "OK");
					alertCannotSendEmails.Show ();					
				}
			};
			
			back = new UIBarButtonItem(UIBarButtonSystemItem.Reply);
			signing = new UIBarButtonItem("Start signing", UIBarButtonItemStyle.Plain, StartSigning);
			email = new UIBarButtonItem ("Email receipt", UIBarButtonItemStyle.Plain, DoSendReceiptEmail);
			skipSigning = new UIBarButtonItem("Skip signing", UIBarButtonItemStyle.Plain, SkipSigning);
			forward = new UIBarButtonItem(UIBarButtonSystemItem.Action);
			clearSignature = new UIBarButtonItem("Clear signature", UIBarButtonItemStyle.Plain, ClearSignature);
			
			ToolbarItems = new UIBarButtonItem[] {
				back, 				new UIBarButtonItem(UIBarButtonSystemItem.FlexibleSpace),
				signing,			new UIBarButtonItem(UIBarButtonSystemItem.FlexibleSpace),
				email,				new UIBarButtonItem(UIBarButtonSystemItem.FlexibleSpace),
				skipSigning,		new UIBarButtonItem(UIBarButtonSystemItem.FlexibleSpace),
				forward
			};
			
			back.Clicked += GoBack;
			forward.Clicked += GoForward;
		}

		bool ValidateEmailAddress(string addressString) {
			try {
				return !String.IsNullOrEmpty(addressString) &&
					Regex.IsMatch(addressString, 
						@"[a-z0-9!#$%&'*+/=?^_`{|}~-]+(?:\.[a-z0-9!#$%&'*+/=?^_`{|}~-]+)*@" +
						@"(?:[a-z0-9](?:[a-z0-9-]*[a-z0-9])?\.)+[a-z0-9](?:[a-z0-9-]*[a-z0-9])?", 
					RegexOptions.IgnoreCase, TimeSpan.FromMilliseconds(250));

						// This is one less complex, but still not allowing i+me@you.com
				// return Regex.IsMatch(addressString, REGEX_EXPRESSION, RegexOptions.IgnoreCase, TimeSpan.FromMilliseconds(250)
				// @"[a-z0-9!#$%&'*+/=?^_`{|}~-]+(?:\.[a-z0-9!#$%&'*+/=?^_`{|}~-]+)*@" +
				// @"(?:[a-z0-9](?:[a-z0-9-]*[a-z0-9])?\.)+[a-z0-9](?:[a-z0-9-]*[a-z0-9])?",

						// This is from MSDN as of 01.12.2014
					// @"^(?("")("".+?(?<!\\)""@)|(([0-9a-z]((\.(?!\.))|[-!#\$%&'\*\+/=\?\^`\{\}\|~\w])*)(?<=[0-9a-z])@))" +
					// @"(?(\[)(\[(\d{1,3}\.){3}\d{1,3}\])|(([0-9a-z][-\w]*[0-9a-z]*\.)+[a-z0-9][\-a-z0-9]{0,22}[a-z0-9]))$",
			}
			catch (RegexMatchTimeoutException) {
				return false;
			}
		}

		void PresentGetEmailAddressView () {
			// collect the recepient's email address (through a separate dialog window)
			getEmailAddressView = new UIAlertView("Please enter email address\n"+
				"\nCustomer #"+Tabs._jobRunTable.CurrentCustomer.CustomerNumber, "", null, "Cancel", "OK");
			getEmailAddressView.AlertViewStyle = UIAlertViewStyle.PlainTextInput;
			getEmailAddressView.Dismissed += Handle_GetEmailAddressViewWillDismiss;
			getEmailAddressView.Show ();
		}
			
		void Handle_GetEmailAddressViewWillDismiss (object sender, UIButtonEventArgs e) {
			if (e.ButtonIndex == 1) {
				SignInvoiceViewController.emailRecepients = this.getEmailAddressView.GetTextField (0).Text;
				// validate e-mail address
				bool emailValid = ValidateEmailAddress (SignInvoiceViewController.emailRecepients);

				if (emailValid) {
					SaveEmailAndPresentMailComposingView ();
				} else {
					// email did not pass validation
					SignInvoiceViewController.emailRecepients = "";
					var emailInvalidAlert = new UIAlertView ("Sorry",  "The email appears to be invalid. Please try again.", null, "OK");
					emailInvalidAlert.Show ();
				}						
			} else { // user cancelled
				this.sendingEmailCancelled = true;
			}
		}

		void SaveEmailAndPresentMailComposingView () {
			// save email address to SQLite database
			UpdateCustomerEmail (Tabs._jobRunTable.CurrentCustomer.CustomerNumber, emailRecepients);

			// get data for the attachment
			string receiptFilePath = (this.NavigationController as SigningNavigationController).Tabs._payment.pdfReceiptFileName;
			string receiptFileName = Path.GetFileName (receiptFilePath);
			NSData fileContents = NSData.FromFile (receiptFilePath);

			// display mail composition view controller
			MFMailComposeViewController mail = new MFMailComposeViewController ();
			if (fileContents != null) {
				mail.AddAttachmentData (fileContents, "application/pdf", receiptFileName);

				Action emptyAction = delegate { };
				mail.SetToRecipients (new string[] { emailRecepients });
				mail.SetSubject (String.Format ("Puratap receipt: CN# {0} {1}", Tabs._jobRunTable.CurrentCustomer.CustomerNumber, MyConstants.DEBUG_TODAY));
				mail.SetMessageBody (String.Format ("Dear customer,\n\nPlease find attached your receipt.\n\nKind regards, \n   Puratap"), false);

				mail.Finished += delegate(object _sender, MFComposeResultEventArgs _e) {
					if (_e.Result == MFMailComposeResult.Sent) {
						var alert = new UIAlertView ("", "Email sent to: " + SignInvoiceViewController.emailRecepients, null, "OK");
						alert.Show ();				
					} else {
						var alert = new UIAlertView (_e.Result.ToString (), "Email has not been sent.", null, "OK");
						alert.Show ();				
					}
					this.DismissViewController (true, emptyAction);				
				};
				this.PresentViewController (mail, true, emptyAction);
			}		}

		void UpdateCustomerEmail(long CN, string email) {
			// updates WCLIENT table in SQLite
			try {
				if (File.Exists (ServerClientViewController.dbFilePath)) {
					// UPDATE the customer record in WCLIENT, setting EMAILAD field
					using (var connection = new SqliteConnection ("Data Source=" + ServerClientViewController.dbFilePath)) {
						connection.Open ();
						var cmd = connection.CreateCommand ();
						cmd.CommandText = "UPDATE WCLIENT SET EmailAd = ?" +
											" WHERE CusNum = ?";
						cmd.Parameters.AddWithValue ("@EmailAddress", email);
						cmd.Parameters.AddWithValue ("@CustomerID", CN);
						cmd.ExecuteNonQuery ();
					}

					SaveEmailUpdateToCustomerDetailUpdates(CN, Tabs._jobRunTable.CurrentJob.JobBookingNumber, email);
				}
			} catch {
			}
		}

		void SaveEmailUpdateToCustomerDetailUpdates (long customerID, long jobID, string email) {
			try {
				Tabs._customersView.UpdateCustomerInfo(CustomerDetailsUpdatableField.Email, 
					Tabs._jobRunTable.CurrentCustomer.EmailAddress, email, customerID, jobID);
				Tabs._jobRunTable.CurrentCustomer.EmailAddress = email;
			} catch {
			}
		}

//		void GetEmailRecepients (long CN, UIView view) {
//			// NOTE :: this does not work since iOS 6, because
//					// XPC infrastructure along with remote view controllers is used by mail composition
//					// we will have to collect email address before presenting the mail view controller
//			if (view is UITextField) {
//				if ((view as UITextField).Text.Contains ("@")) {
//					emailRecepients = (view as UITextField).Text;
//					return;
//				}
//			}
//
//			if (view.Subviews.Length > 0) {
//				foreach (UIView subView in view.Subviews) {
//					GetEmailRecepients (CN, subView);
//				}
//			}
//		}

		void BeginPrinting()
		{
			ThreadStart ts = new ThreadStart( delegate {
				LoadingView wait = new LoadingView();
				wait.Show ("Printing, please wait");
				printOK = false;

				if ( ! Tabs._prePlumbView.IsDefault () )
				{
					if (Tabs._jobRunTable.CurrentCustomer.FilesToPrint != null)
						this.Tabs._jobRunTable.CurrentCustomer.FilesToPrint.Add (Tabs._prePlumbView.pdfPrePlumbingFileName);

					PrePlumbingPrintingDone.Reset ();
					ThreadStart tsPrintPreplumbing = new ThreadStart( delegate {
						printOK = MyConstants.PrintPDFFile (Tabs._prePlumbView.pdfPrePlumbingFileName);
						PrePlumbingPrintingDone.Set ();
					});
					Thread tPrintPrePlumbing = new Thread(tsPrintPreplumbing);
					tPrintPrePlumbing.Start ();
					PrePlumbingPrintingDone.WaitOne (5000);
					if (tPrintPrePlumbing.ThreadState == ThreadState.Running) tPrintPrePlumbing.Abort ();
				}
				else printOK = true;
				// WAS :: printOK = printOK && MyConstants.PrintPDFFile (Tabs._payment.pdfReceiptFileName);

				if (Tabs._jobRunTable.CurrentCustomer.FilesToPrint != null)
					this.Tabs._jobRunTable.CurrentCustomer.FilesToPrint.Add (Tabs._payment.pdfReceiptFileName);

				bool printReceiptOK = false; 
				ReceiptPrintingDone.Reset ();
				ThreadStart tsPrintReceipt = new ThreadStart( delegate {
					printReceiptOK = MyConstants.PrintPDFFile (Tabs._payment.pdfReceiptFileName);
					ReceiptPrintingDone.Set ();
				});
				Thread tPrintReceipt = new Thread(tsPrintReceipt);
				tPrintReceipt.Start ();

				ReceiptPrintingDone.WaitOne (5000);
				if (tPrintReceipt.ThreadState == ThreadState.Running) tPrintReceipt.Abort ();

				printOK = printOK && printReceiptOK;

				InvokeOnMainThread (delegate() 
				{
					wait.Hide ();
					if (printOK)
					{
						Tabs._navWorkflow._finishWorkflow(null, null);
					}
					else // printing failed for some reason 
					{
						UIAlertView printingError = new UIAlertView("Warning", "An error occurred during printing. What would you like to do?", null, "Try again", "Skip printing");
						printingError.Dismissed += HandlePrintingErrorDismissed;
						printingError.Show ();
					}
				});
			});

			Thread t = new Thread(ts);
			t.Start ();
		}

		void HandleNetworkAlertDismissed (object sender, UIButtonEventArgs e)
		{
			if (e.ButtonIndex == 1)
			{
				// try to open the iPad's Settings app here
			}
			else
			{
				// the user has chosen to skip printing
				Tabs._navWorkflow._finishWorkflow(null, null);
			}
		}

		void HandlePrintingErrorDismissed (object sender, UIButtonEventArgs e)
		{
			if (e.ButtonIndex == 1)
				Tabs._navWorkflow._finishWorkflow(null, null);
			else
			{
				// The user has chosen to try again
			}
		}

		void HandlePrintAlertDismissed (object sender, UIButtonEventArgs e)
		{
			// we could write something into log here
			Tabs._scView.Log (String.Format ( "Printer error when trying to print receipt for job id {0}, customer {1}.", Tabs._jobRunTable.CurrentJob.JobBookingNumber, Tabs._jobRunTable.CurrentCustomer.CustomerNumber));
			Tabs._navWorkflow._finishWorkflow(null, null);			
		}

		void HandleSkipAlertDismissed (object sender, UIButtonEventArgs e)
		{
			if (e.ButtonIndex == 1)
			{
				SkippedSigning = true;

				// redraw the recipt with empty signature
				Signature.Clear ();
				UIImageView iv = new UIImageView();
				iv = (UIImageView)Tabs._payment.GeneratedPdfView.ViewWithTag(MyConstants.ReceiptPDFTemplateTags.Signature);
				iv.ContentMode = UIViewContentMode.ScaleAspectFit;
				UIImage im = Signature.GetDrawingImage (); // this.Signature.Image;
				iv.Image = im;
				
				Tabs._payment.RedrawReceiptPDF (true);

				iv.Dispose ();	im.Dispose ();
				im = null; iv = null;

				GoForward(this, new EventArgs());				
			}
		}

		public override void ViewWillAppear (bool animated)
		{
			base.ViewWillAppear (animated);

			string pdfFileName = (this.NavigationController as SigningNavigationController).Tabs._payment.pdfReceiptFileName;
			PDFView.LoadRequest (new NSUrlRequest( NSUrl.FromFilename (pdfFileName)));
		}

		public override void ViewDidAppear (bool animated)
		{
			hasBeenSigned = false;
			Tabs.SigningNav.SetToolbarHidden (false, true);
			Tabs.SigningNav.SetToolbarItems (this.ToolbarItems, true);

			base.ViewDidAppear (animated);
		}	
	}
}

