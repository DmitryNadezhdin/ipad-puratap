using System;
using System.Drawing;
using MonoTouch.UIKit;
using MonoTouch.Foundation;
using System.IO;
using System.Threading;

namespace Application
{
	public class SignInvoiceViewController : NewSignatureViewController
	{
		public EventHandler GoBack { get; set; }
		public EventHandler GoForward { get; set; }
		public EventHandler SkipSigning { get; set; }
		public EventHandler StartSigning { get; set; }
		public EventHandler FinishSigning { get; set; }
		
		public bool SkippedSigning { get; set; }
		
		UIBarButtonItem back;
		UIBarButtonItem forward;
		UIBarButtonItem skipSigning;		
		UIBarButtonItem signing;		
		UIBarButtonItem clearSignature;

		UIAlertView skipAlert;

		private volatile bool printOK;
		// public ManualResetEvent AllPrintingDone = new ManualResetEvent(false);
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
						clearSignature,	new UIBarButtonItem(UIBarButtonSystemItem.FlexibleSpace),
						signing,				new UIBarButtonItem(UIBarButtonSystemItem.FlexibleSpace),
						skipSigning,		new UIBarButtonItem(UIBarButtonSystemItem.FlexibleSpace),
						forward
					}, true );
				
				// Signature.Image = new UIImage();
				Signature.Clear ();
				hasBeenSigned = false;
				SigningMode = true;

				this.NavigationController.SetNavigationBarHidden (false, true);
				this.NavigationItem.SetRightBarButtonItems (new UIBarButtonItem[] { 
					new UIBarButtonItem("Done", UIBarButtonItemStyle.Done, FinishSigning), 
					new UIBarButtonItem("Clear Signature", UIBarButtonItemStyle.Bordered, ClearSignature) }, true);
				this.NavigationController.SetToolbarHidden (true, true);
			};
			
			FinishSigning = delegate {
				SigningMode = false;
				signing = new UIBarButtonItem("Start signing", UIBarButtonItemStyle.Bordered, StartSigning);				

				this.SetToolbarItems (new UIBarButtonItem[] {
						back, 				new UIBarButtonItem(UIBarButtonSystemItem.FlexibleSpace),
						signing,				new UIBarButtonItem(UIBarButtonSystemItem.FlexibleSpace),
						skipSigning,		new UIBarButtonItem(UIBarButtonSystemItem.FlexibleSpace),
						forward
					}, true );

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
				
				iv.Dispose ();	im.Dispose ();
				im = null; iv = null;
			};			
			
			back = new UIBarButtonItem(UIBarButtonSystemItem.Reply);
			signing = new UIBarButtonItem("Start signing", UIBarButtonItemStyle.Bordered, StartSigning);
			skipSigning = new UIBarButtonItem("Skip signing", UIBarButtonItemStyle.Bordered, SkipSigning);
			forward = new UIBarButtonItem(UIBarButtonSystemItem.Action);
			clearSignature = new UIBarButtonItem("Clear signature", UIBarButtonItemStyle.Bordered, ClearSignature);
			
			ToolbarItems = new UIBarButtonItem[] {
				back, 				new UIBarButtonItem(UIBarButtonSystemItem.FlexibleSpace),
				signing,				new UIBarButtonItem(UIBarButtonSystemItem.FlexibleSpace),
				skipSigning,		new UIBarButtonItem(UIBarButtonSystemItem.FlexibleSpace),
				forward
			};
			
			back.Clicked += GoBack;
			forward.Clicked += GoForward;		
		}

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

		public override void ViewDidAppear (bool animated)
		{
			hasBeenSigned = false;
			string pdfFileName = (this.NavigationController as SigningNavigationController).Tabs._payment.pdfReceiptFileName;
			PDFView.LoadRequest (new NSUrlRequest( NSUrl.FromFilename (pdfFileName)));
			
			Tabs.SigningNav.SetToolbarHidden (false, true);
			Tabs.SigningNav.SetToolbarItems (this.ToolbarItems, true);

			base.ViewDidAppear (animated);
		}	
	}
}

