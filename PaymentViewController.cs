using System;
using System.IO;
using System.Drawing;
using System.Collections.Generic;
using MonoTouch.UIKit;
using MonoTouch.Dialog;
using MonoTouch.Foundation;
using MonoTouch.CoreGraphics;
using Mono.Data.Sqlite;
using ZSDK_Binding;

namespace Puratap
{
	public partial class PaymentViewController : UIViewController
	{
		private Job selectedJob;
		private string ccNumberMask = "9999  9999  9999  9999";

		WorkflowNavigationController _navWorkflow;
		
		public UIView GeneratedPdfView { get; set; }
		public string pdfReceiptFileName { get; set; }
		
		public JobSummary Summary { get; set; }
		
		public bool CanProceed { get { return CheckIfCanProceed(); } }
		public bool CCExpiryOK { get { return IsCreditCardExpiryOK(); } }
		public bool CCNumberOK { get { return IsCreditCardNumberOK(); } }
		public bool CCNameOK { get { return IsCreditCardNameOK(); } }		
		public bool ChqNumberOK { get { return IsChqNumberOK(); } }

		private bool _splitPaymentMode;
		public bool SplitPaymentMode { get { return _splitPaymentMode; } 
			set {
				_splitPaymentMode = value;
				if (value == true)		// going from normal payment to split
				{
					btnSplitPayment.Title = "Normal payment";
					HideAllPaymentOptions ();

					lbTip.Text = "Entered split payment mode. Please enter details of two payments received.";

					if (scPaymentType != null) 
					{
						// DON'T CARE if (scPaymentType.SelectedSegment<2 && scPaymentType.SelectedSegment != -1) 
					
						scPaymentType.SelectedSegment = -1;						// if one of these was selected, "unselect" it in the UI
						this._payments.Clear ();								// clear all payment types selected previously

						// add 2 payments
						this._payments.Add (new JobPayment() { Amount = selectedJob.MoneyToCollect } );				
						this._payments.Add (new JobPayment() { Amount = CalculateMoneyToCollect () - selectedJob.MoneyToCollect });

						// clear all amounts and types in all jobs, add 2 amounts equal to 0, add 2 types equal to "None"
						if (this.selectedJob != null)
						{
							if (this.selectedJob.HasNoParent ())
							{
								this.selectedJob.Payments.Clear ();
								this.selectedJob.Payments.Add (new JobPayment() { Amount = selectedJob.MoneyToCollect });
								this.selectedJob.Payments.Add (new JobPayment() { Amount = CalculateMoneyToCollect () - selectedJob.MoneyToCollect });
								foreach(Job child in selectedJob.ChildJobs)
								{
									child.Payments.Clear ();
									child.Payments.Add (new JobPayment() { Amount = selectedJob.MoneyToCollect });
									child.Payments.Add (new JobPayment() { Amount = CalculateMoneyToCollect () - selectedJob.MoneyToCollect });
								}
							}
							else 
							{
								Job main = _navWorkflow._tabs._jobRunTable.FindParentJob (selectedJob);
								main.Payments.Clear ();
								main.Payments.Add (new JobPayment() { Amount = selectedJob.MoneyToCollect });
								main.Payments.Add (new JobPayment() { Amount = CalculateMoneyToCollect () - selectedJob.MoneyToCollect });
								foreach(Job child in main.ChildJobs)
								{
									child.Payments.Clear ();
									child.Payments.Add (new JobPayment() { Amount = selectedJob.MoneyToCollect });
									child.Payments.Add (new JobPayment() { Amount = CalculateMoneyToCollect () - selectedJob.MoneyToCollect });
								}
							}
						}
					}
					// hide top segmented control that allows the user to choose payment type in normal mode
					// show elements corresponding to split payment mode
					ShowSplitModeUIElements();
				}
				else // going back from split payment to normal
				{
					// hide top segmented control that allows the user to choose payment type in normal mode
					// show elements corresponding to split payment mode
					ShowNormalModeUIElements ();


					if (btnSplitPayment != null) btnSplitPayment.Title = "Split payment";
					if (scPaymentType != null) scPaymentType.SelectedSegment = -1;
					HideAllPaymentOptions ();
					if (lbTip != null) lbTip.Text = "Please choose a payment type above.";

					// clear all previously selected payment types and amounts (if not resetting the view controller after the job data input has been completed)
					if (resettingToDefaults == false)
					{
						this._payments.Clear ();
						this._payments.Add (new JobPayment() { PaymentCustomerNumber = (selectedJob!=null)? selectedJob.CustomerNumber : 0, Amount = 0 } );
						if (this.selectedJob != null)
						{
							if (this.selectedJob.HasNoParent ())
							{
								this.selectedJob.Payments.Clear ();
								this.selectedJob.Payments.Add (new JobPayment() { PaymentCustomerNumber = (selectedJob!=null)? selectedJob.CustomerNumber : 0, Amount = 0 } );
								foreach(Job child in selectedJob.ChildJobs)
								{
									child.Payments.Clear ();
									child.Payments.Add (new JobPayment() { PaymentCustomerNumber = (selectedJob!=null)? selectedJob.CustomerNumber : 0, Amount = 0 } );
								}
							}
							else 
							{
								Job main = _navWorkflow._tabs._jobRunTable.FindParentJob (selectedJob);
								main.Payments.Clear ();
								main.Payments.Add (new JobPayment() { PaymentCustomerNumber = (selectedJob!=null)? selectedJob.CustomerNumber : 0, Amount = 0 } );
								foreach(Job child in main.ChildJobs)
								{
									child.Payments.Clear ();
									child.Payments.Add (new JobPayment() { PaymentCustomerNumber = (selectedJob!=null)? selectedJob.CustomerNumber : 0, Amount = 0 } );
								}
							}
						}
					}
				}
			}
		}

		private List<JobPayment> _payments;
		/*
		private List<PaymentTypes> _paymentType;
		public List<PaymentTypes> PaymentType { get { return _paymentType; } 
			set { 
				// if (_paymentType.Count == 0) _paymentType.Add ( (PaymentTypes)value);
				// else _paymentType[0] = (PaymentTypes)value;

				_paymentType = value; 
				if (_paymentType.Count > 0) tfPaymentType.Text = MyConstants.OutputStringForValue(_paymentType[0]);

			} // end setter method for the property
		} // end property
		*/

		// private GetChoicesForObject ac;
		
		public double MoneyReceived = 0;
		
		public PaymentViewController (WorkflowNavigationController nav) : base ("PaymentViewController", null)
		{
			this._navWorkflow = nav;
			this.Title = NSBundle.MainBundle.LocalizedString ("Payment", "Payment");
			using (var image = UIImage.FromBundle ("Images/192-credit-card") ) this.TabBarItem.Image =  image;
			this._payments = new List<JobPayment>();
			this.SplitPaymentMode = false;
		}

		public void ShowSplitModeUIElements()
		{
			if (scSplitPaymentMethod1 != null) { scSplitPaymentMethod1.Hidden = false; scSplitPaymentMethod1.SelectedSegment = -1; }
			if (scSplitPaymentMethod2 != null) { scSplitPaymentMethod2.Hidden = false; scSplitPaymentMethod2.SelectedSegment = -1; }
			if (tfSplitPaymentAmount1 != null) tfSplitPaymentAmount1.Hidden = false;
			if (tfSplitPaymentAmount2 != null) tfSplitPaymentAmount2.Hidden = false;

			if (scPaymentType != null) { scPaymentType.Hidden = true; scPaymentType.SelectedSegment = -1; }

			if (selectedJob != null)
			{
				tfSplitPaymentAmount1.Text = String.Format ("$ {0:0.00}", selectedJob.MoneyToCollect);
				selectedJob.Payments[0].Amount = selectedJob.MoneyToCollect;
				tfSplitPaymentAmount2.Text = String.Format ("$ {0:0.00}", CalculateMoneyToCollect () - selectedJob.MoneyToCollect);

				if (selectedJob.Payments.Count == 1) selectedJob.Payments.Add (new JobPayment());
				selectedJob.Payments[1].Amount = CalculateMoneyToCollect () - selectedJob.MoneyToCollect;
			}
		}

		public void ShowNormalModeUIElements()
		{
			if (scSplitPaymentMethod1 != null) { scSplitPaymentMethod1.Hidden = true; scSplitPaymentMethod1.SelectedSegment = -1; }
			if (scSplitPaymentMethod2 != null) { scSplitPaymentMethod2.Hidden = true; scSplitPaymentMethod2.SelectedSegment = -1; }
			if (tfSplitPaymentAmount1 != null) tfSplitPaymentAmount1.Hidden = true; // TODO :: default value?
			if (tfSplitPaymentAmount2 != null) tfSplitPaymentAmount2.Hidden = true; // TODO :: default value? 

			if (scPaymentType != null) { scPaymentType.Hidden = false; scPaymentType.SelectedSegment = -1; }
		}

		public bool ContainsInvoicePaymentType(List<JobPayment> payments)
		{
			foreach (JobPayment payment in payments)			
				if (payment.Type == PaymentTypes.CCDetails || payment.Type == PaymentTypes.CreditCard || payment.Type == PaymentTypes.Invoice)				
					return true;	
			// otherwise
			return false;
		}

		public bool ContainsPaymentType(List<JobPayment> payments, PaymentTypes type)
		{
			foreach (JobPayment payment in payments)
				if (payment.Type == type)
					return true;
			return false;
		}
		
		public void GenerateReceiptPDFPreview()
		{
			Customer c = _navWorkflow._tabs._jobRunTable.CurrentCustomer;
			int jobCount = this.Summary.mainJob.ChildJobs.Count + 1;
			
			NSArray a = NSBundle.MainBundle.LoadNib ("ReceiptPDFView", this, null);
			GeneratedPdfView = (UIView)MonoTouch.ObjCRuntime.Runtime.GetNSObject (a.ValueAt (0));
			// Getting a Puratap logo on the receipt
			using (var image = UIImage.FromBundle("/Images/puratap-logo") )
				((UIImageView)GeneratedPdfView.ViewWithTag(MyConstants.ReceiptPDFTemplateTags.Logo)).Image = image;
			//Getting the current job's info into the labels on the template
			((UILabel)GeneratedPdfView.ViewWithTag (2)).Text = "Receipt for Job # "+this.Summary.mainJob.JobBookingNumber.ToString();
			((UILabel)GeneratedPdfView.ViewWithTag (3)).Text = "Customer # "+c.CustomerNumber.ToString();
			((UILabel)GeneratedPdfView.ViewWithTag (4)).Text = "Jobs performed: "+(jobCount).ToString ();
			((UILabel)GeneratedPdfView.ViewWithTag (5)).Text = (c.isCompany)? "Company: "+c.CompanyName + "\n" + "Customer: "+" "+c.FirstName+" "+c.LastName : 
																			"Customer: "+" "+c.FirstName+" "+c.LastName;
			((UILabel)GeneratedPdfView.ViewWithTag (31)).Text = "Address: "+c.Address+", "+c.Suburb;
			((UILabel)GeneratedPdfView.ViewWithTag (6)).Text = "Date: "+DateTime.Now.Date.ToShortDateString();

			double GSTAmount = this.CalculateMoneyToCollect () / 11;
			((UILabel)GeneratedPdfView.ViewWithTag (MyConstants.ReceiptPDFTemplateTags.GSTLabel)).Text = "GST amount: " + String.Format("$ {0:0.00}", GSTAmount);

			if (c.DepositAmount - c.DepositUsed > 0) {
				((UILabel)GeneratedPdfView.ViewWithTag (MyConstants.ReceiptPDFTemplateTags.DepositLabel)).Text = 
					"Deposit: " + String.Format ("$ {0:0.00}", c.DepositAmount - c.DepositUsed);
				((UILabel)GeneratedPdfView.ViewWithTag (MyConstants.ReceiptPDFTemplateTags.DepositLabel)).Hidden = false;
			} else {
				((UILabel)GeneratedPdfView.ViewWithTag (MyConstants.ReceiptPDFTemplateTags.DepositLabel)).Hidden = true;
			}

			double displayedTotalToReceive = this.CalculateMoneyToCollect ();
			((UILabel)GeneratedPdfView.ViewWithTag (8)).Text = "Total to receive (inc. GST): " + String.Format("$ {0:0.00}", displayedTotalToReceive); // + this.tfToBeCollected.Text;
			((UILabel)GeneratedPdfView.ViewWithTag (9)).Text = "Received: " + String.Format("{0:0.00}", this.tfTotalMoneyReceived.Text);

			double displayedBalance = (this.CalculateMoneyToCollect () - this.MoneyReceived - c.DepositAmount);
			((UILabel)GeneratedPdfView.ViewWithTag (24)).Text = "Balance: " + String.Format("$ {0:0.00}", displayedBalance);
			if (displayedBalance > 0) 
				((UILabel)GeneratedPdfView.ViewWithTag (24)).Font = UIFont.BoldSystemFontOfSize (20);
			else ((UILabel)GeneratedPdfView.ViewWithTag (24)).Font = UIFont.SystemFontOfSize (20);

			((UILabel)GeneratedPdfView.ViewWithTag (27)).Text = String.Format ("App version: {0}", NSBundle.MainBundle.ObjectForInfoDictionary("CFBundleVersion").ToString());

			((UILabel)GeneratedPdfView.ViewWithTag (25)).Text = "Puratap representative: "+MyConstants.EmployeeName;
			if (! this.SplitPaymentMode)
				if (this._payments.Count == 0)
					((UILabel)GeneratedPdfView.ViewWithTag (23)).Text = String.Format ("Payment method: {0}", MyConstants.OutputStringForValue (PaymentTypes.None)); // this.tfPaymentType.Text				
				else
					((UILabel)GeneratedPdfView.ViewWithTag (23)).Text = String.Format ("Payment method: {0}", MyConstants.OutputStringForValue (this._payments[0].Type)); // this.tfPaymentType.Text
			else ((UILabel)GeneratedPdfView.ViewWithTag (23)).Text = String.Format ("Payment method: {0}", "Split"); // this.tfPaymentType.Text
			
			((UILabel)GeneratedPdfView.ViewWithTag (10)).Text = "Job 1: "+this.Summary.mainJob.Type.Description;
			((UILabel)GeneratedPdfView.ViewWithTag (11)).Text = String.Format ("$ {0:0.00}", this.Summary.mainJob.MoneyToCollect);

			// If the selected payment method is "Invoice", the invoice will be sent out separately, so we make a reminder of that visible
			if (this.ContainsInvoicePaymentType (_payments))
				((UILabel)GeneratedPdfView.ViewWithTag (30)).Hidden = false;
			else ((UILabel)GeneratedPdfView.ViewWithTag (30)).Hidden = true;

			bool tuInJobCluster = false;
			if ((this.Summary.mainJob.Type.Code == "TUBINGUPGR") && (this.Summary.mainJob.Warranty == false))
				tuInJobCluster = true;
			else
				foreach (Job child in this.Summary.mainJob.ChildJobs)
					if ((child.Type.Code == "TUBINGUPGR") && (child.Warranty == false))
						{	tuInJobCluster = true; break; }

			((UILabel)GeneratedPdfView.ViewWithTag (33)).Text = String.Format("Warranty on tubing components extended for another 3 years (until {0}) due to tubing upgrade. " +
				"Please note that this extension does not cover the purifier and the tap.", DateTime.Now.Date.AddYears(3).ToShortDateString());
			((UILabel)GeneratedPdfView.ViewWithTag (33)).Hidden = !tuInJobCluster;



			bool newtapInJobCluster = false;
			if ((this.Summary.mainJob.Type.Code == "NEWTAP") && (this.Summary.mainJob.Warranty == false))
				newtapInJobCluster = true;
			else
				foreach (Job child in this.Summary.mainJob.ChildJobs)
					if ((child.Type.Code == "NEWTAP") && (child.Warranty == false))
				{	newtapInJobCluster = true; break; }
			((UILabel)GeneratedPdfView.ViewWithTag (34)).Text = String.Format("Warranty on tap extended for another 3 years (until {0}) due to tap replacement. " +
			                                                                  "Please note that this extension does not cover the purifier and the tubing components.", DateTime.Now.Date.AddYears(3).ToShortDateString());
			((UILabel)GeneratedPdfView.ViewWithTag (34)).Hidden = !newtapInJobCluster;

			
			if (jobCount < 6)
			{
				// Update the labels corresponding to the jobs that were done
				for (int k = 0; k<jobCount; k++)
				{
					int labelTag = 10+k*2;
					if (this.Summary.mainJob.Warranty)
					{
						((UILabel)GeneratedPdfView.ViewWithTag (labelTag)).Text = (k==0)? String.Format ("Job {0}: {1} (under warranty)", k+1, this.Summary.mainJob.Type.Description) : 
																															String.Format ("Job {0}: {1} (under warranty)", k+1, this.Summary.mainJob.ChildJobs[k-1].Type.Description);
					}
					else
					{
						((UILabel)GeneratedPdfView.ViewWithTag (labelTag)).Text = (k==0)? String.Format ("Job {0}: {1}", k+1, this.Summary.mainJob.Type.Description) : 
																															String.Format ("Job {0}: {1}", k+1, this.Summary.mainJob.ChildJobs[k-1].Type.Description);					
					}
					((UILabel)GeneratedPdfView.ViewWithTag (labelTag+1)).Text = (k==0)? String.Format ("$ {0:0.00}", this.Summary.mainJob.MoneyToCollect) : 
																															String.Format ("$ {0:0.00}", this.Summary.mainJob.ChildJobs[k-1].MoneyToCollect);					
				}
				
				// Set the labels corresponding to non-existing jobs to "Hidden" in the template
				for (int k=0; k < 6-jobCount; k++)
				{
					int labelTag = 20-k*2;
					((UILabel)GeneratedPdfView.ViewWithTag (labelTag)).Hidden = true;
					((UILabel)GeneratedPdfView.ViewWithTag (labelTag+1)).Hidden = true;
				}
			}
			
			// Adjusting the dimensions of the views for the job list to fit and for payment info to be placed directly after the end of the job list
			UIView jobList = (UIView)GeneratedPdfView.ViewWithTag (28);
			jobList.Frame = new RectangleF(jobList.Frame.X, jobList.Frame.Y, jobList.Frame.Width, Math.Min (41 + (jobCount-1)*29, 186));
			UIView paymentInfo = (UIView)GeneratedPdfView.ViewWithTag (29);
			paymentInfo.Frame = new RectangleF(paymentInfo.Frame.X, jobList.Frame.Y+jobList.Frame.Height+20, paymentInfo.Frame.Width, paymentInfo.Frame.Height);
			
			// Adjusting the frame of the main view so that we won't use any whitespace unnecessarily
			float lowestPdfPoint = paymentInfo.Frame.Y + paymentInfo.Frame.Height;
			GeneratedPdfView.Frame = new RectangleF(GeneratedPdfView.Frame.X, GeneratedPdfView.Frame.Y, GeneratedPdfView.Frame.Width, lowestPdfPoint+10);
			// MyConstants.PrintPDFFile (pdfFileName);
		}
		
		public void RedrawReceiptPDF(bool DocumentSigned)
		{
			Customer c = _navWorkflow._tabs._jobRunTable.CurrentCustomer;
			// Preparations for saving the PDF to disk
			NSMutableData pdfData = new NSMutableData();
			UIGraphics.BeginPDFContext (pdfData, GeneratedPdfView.Bounds, null);
			UIGraphics.BeginPDFPage ();
			GeneratedPdfView.Layer.RenderInContext (UIGraphics.GetCurrentContext ());
			UIGraphics.EndPDFContent ();
			
			// Saving PDF file to disk
			NSError err;
			string pdfFileName;

			// this had to be changed so that we always receive a receipt, even if it was not signed
			//	if (DocumentSigned) pdfFileName = Path.Combine (Environment.GetFolderPath (Environment.SpecialFolder.Personal), c.CustomerNumber.ToString()+"_Receipt_Signed.pdf");
			//	else pdfFileName = Path.Combine (Environment.GetFolderPath (Environment.SpecialFolder.Personal), c.CustomerNumber.ToString()+"_Receipt_Not_Signed.pdf");

			pdfFileName = Path.Combine (Environment.GetFolderPath (Environment.SpecialFolder.Personal), 
			                            String.Format ("{0}_{1}_Receipt.pdf", c.CustomerNumber, this.Summary.mainJob.JobBookingNumber));

			pdfData.Save (pdfFileName, true, out err);
			err = null; pdfData = null;
			
			pdfReceiptFileName = pdfFileName;
			_navWorkflow._tabs.SignInvoice.PDFView.MultipleTouchEnabled = true;
			_navWorkflow._tabs.SignInvoice.PDFView.ScalesPageToFit = true;
			_navWorkflow._tabs.SignInvoice.PDFView.LoadRequest (new NSUrlRequest( NSUrl.FromFilename (pdfReceiptFileName)));

			// PointF offset = new PointF(0, _navWorkflow._tabs.SignInvoice.PDFView.ScrollView.ContentSize.Height - _navWorkflow._tabs.SignInvoice.PDFView.ScrollView.Bounds.Height);
			// _navWorkflow._tabs.SignInvoice.PDFView.ScrollView.SetContentOffset(offset, true);
		}
		
		public void SetTotalToCollect(double total)
		{
			if (this.selectedJob.Type.Code == "TWI" || this.selectedJob.Type.Code == "RAI" || this.selectedJob.Type.Code == "ROOF" 
			    && (this._navWorkflow._tabs._jobRunTable.CurrentCustomer.DepositAmount > 0)) 
			{
				tfToBeCollected.Text = String.Format ("$ {0:0.00}", total - double.Parse (tfDeposit.Text.Replace ("$", " ")));
				tfTotalMoneyReceived.Text = tfToBeCollected.Text;
			}
			else {
				tfToBeCollected.Text = String.Format ("$ {0:0.00}", total);
				tfTotalMoneyReceived.Text = tfToBeCollected.Text;
			}
		}
		public void SetTotalReceived(double total)
		{
			tfTotalMoneyReceived.Text = String.Format ("$ {0:0.00}", total);
		}

		void HandleCreditCardNumberEditingDidEndOnExit (object sender, EventArgs e)
		{
			char a; string newString=""; bool ok = false;
			for (int i=0; i < tfCreditCardNumber.Text.Length; i++) // throwing out all non-digit characters from input
			{
				a = tfCreditCardNumber.Text[i];
				if ( char.IsDigit (a) ) newString+=a;
			}
			if (newString.Length != 16)
			{
				return;	// if length of digit string is not 16, it's invalid
			}
			else {
				ok = ValidateCreditCardNumber (newString);
				if (ok) {
					// find the payment that has Credit Card payment type and put the new number in
					foreach(JobPayment payment in selectedJob.Payments)
					{
						if (payment.Type == PaymentTypes.CreditCard)
							payment.CreditCardNumber = newString;
					}
				}
				else { // not ok
				}
			}
		}
		
		void acBack (NSObject sender)
		{
			// we should go back depending on a job type AND we must ensure that the appropriate viewcontroller will be shown
			
			switch (_navWorkflow._tabs._jobRunTable.CurrentJob.Type.Code)
			{
			case "SER" : { 
				// WAS :: _navWorkflow._tabs.SelectedViewController = _navWorkflow._tabs.ViewControllers[1]; 

				_navWorkflow._tabs._serviceParts.ThisJob = _navWorkflow._tabs._jobRunTable.CurrentJob;
				_navWorkflow._tabs._serviceParts.LoadParts ();
				_navWorkflow._tabs.SelectedViewController = _navWorkflow._tabs.ViewControllers[2];

				if (! (_navWorkflow._tabs.UsedPartsNav.VisibleViewController is ServiceUsedPartsViewController) )
				{
					if (_navWorkflow._tabs.UsedPartsNav.ViewControllers.Length > 1) _navWorkflow._tabs.UsedPartsNav.PopToRootViewController (false);
					_navWorkflow._tabs.UsedPartsNav.PushViewController (_navWorkflow._tabs._serviceParts, false);
				}

				break; 
			}
			case "FRC": { 
				_navWorkflow._tabs._jobFilter.ThisJob = _navWorkflow._tabs._jobRunTable.CurrentJob;
				_navWorkflow._tabs._jobFilter.LoadParts ();
				_navWorkflow._tabs.SelectedViewController = _navWorkflow._tabs.ViewControllers[2];
				if (_navWorkflow._tabs.UsedPartsNav.VisibleViewController is FilterChangeViewController)
				{
					// do nothing
				}
				else 
				{
					if (_navWorkflow._tabs.UsedPartsNav.ViewControllers.Length > 1) _navWorkflow._tabs.UsedPartsNav.PopToRootViewController (false);
					_navWorkflow._tabs.UsedPartsNav.PushViewController (_navWorkflow._tabs._jobFilter, false);
				}
				break; 
			}
			case "FIL" : { 
				_navWorkflow._tabs._jobFilter.ThisJob = _navWorkflow._tabs._jobRunTable.CurrentJob;
				_navWorkflow._tabs._jobFilter.LoadParts ();
				_navWorkflow._tabs.SelectedViewController = _navWorkflow._tabs.ViewControllers[2];
				if (_navWorkflow._tabs.UsedPartsNav.VisibleViewController is FilterChangeViewController)
				{
					// do nothing
				}
				else 
				{
					if (_navWorkflow._tabs.UsedPartsNav.ViewControllers.Length > 1) _navWorkflow._tabs.UsedPartsNav.PopToRootViewController (false);
					_navWorkflow._tabs.UsedPartsNav.PushViewController (_navWorkflow._tabs._jobFilter, false);
				}
				break; 
			}
			case "UP" : { 
				_navWorkflow._tabs._jobUnitUpgrade.ThisJob = _navWorkflow._tabs._jobRunTable.CurrentJob;
				_navWorkflow._tabs._jobUnitUpgrade.LoadParts ();
				_navWorkflow._tabs.SelectedViewController = _navWorkflow._tabs.ViewControllers[2];
				if (_navWorkflow._tabs.UsedPartsNav.VisibleViewController is JobUnitUpgrade)
				{
					// do nothing
				}
				else 
				{
					if (_navWorkflow._tabs.UsedPartsNav.ViewControllers.Length > 1) _navWorkflow._tabs.UsedPartsNav.PopToRootViewController (false);
					_navWorkflow._tabs.UsedPartsNav.PushViewController (_navWorkflow._tabs._jobUnitUpgrade, false);
				}
				break; 
			}
			case "NEWTAP" : { 
				_navWorkflow._tabs._jobNewTap.ThisJob = _navWorkflow._tabs._jobRunTable.CurrentJob;
				_navWorkflow._tabs._jobNewTap.LoadParts ();
				_navWorkflow._tabs.SelectedViewController = _navWorkflow._tabs.ViewControllers[2];
				if (_navWorkflow._tabs.UsedPartsNav.VisibleViewController is JobNewTap)
				{
					// do nothing
				}
				else 
				{
					if (_navWorkflow._tabs.UsedPartsNav.ViewControllers.Length > 1) _navWorkflow._tabs.UsedPartsNav.PopToRootViewController (false);
					_navWorkflow._tabs.UsedPartsNav.PushViewController (_navWorkflow._tabs._jobNewTap, false);
				}
				break; 
			}
			case "TUBINGUPGR" : { 
				_navWorkflow._tabs._jobTubingUpgrade.ThisJob = _navWorkflow._tabs._jobRunTable.CurrentJob;
				_navWorkflow._tabs._jobTubingUpgrade.LoadParts ();
				_navWorkflow._tabs.SelectedViewController = _navWorkflow._tabs.ViewControllers[2];
				if (_navWorkflow._tabs.UsedPartsNav.VisibleViewController is JobTubingUpgrade)
				{
					// do nothing
				}
				else 
				{
					if (_navWorkflow._tabs.UsedPartsNav.ViewControllers.Length > 1) _navWorkflow._tabs.UsedPartsNav.PopToRootViewController (false);
					_navWorkflow._tabs.UsedPartsNav.PushViewController (_navWorkflow._tabs._jobTubingUpgrade, false);
				}
				break; 
			}
			case "HDTUBING" : { 
				_navWorkflow._tabs._jobHDTubingUpgrade.ThisJob = _navWorkflow._tabs._jobRunTable.CurrentJob;
				_navWorkflow._tabs._jobHDTubingUpgrade.LoadParts ();
				_navWorkflow._tabs.SelectedViewController = _navWorkflow._tabs.ViewControllers[2];
				if (_navWorkflow._tabs.UsedPartsNav.VisibleViewController is JobHDTubingUpgrade)
				{
					// do nothing
				}
				else 
				{
					if (_navWorkflow._tabs.UsedPartsNav.ViewControllers.Length > 1) _navWorkflow._tabs.UsedPartsNav.PopToRootViewController (false);
					_navWorkflow._tabs.UsedPartsNav.PushViewController (_navWorkflow._tabs._jobHDTubingUpgrade, false);
				}
				break; 
			}
			case "TWI" : { 
				_navWorkflow._tabs._jobInstall.ThisJob = _navWorkflow._tabs._jobRunTable.CurrentJob;
				_navWorkflow._tabs._jobInstall.LoadParts ();
				_navWorkflow._tabs.SelectedViewController = _navWorkflow._tabs.ViewControllers[2];
				if (_navWorkflow._tabs.UsedPartsNav.VisibleViewController is JobInstallationViewController)
				{
					// do nothing
				}
				else 
				{
					if (_navWorkflow._tabs.UsedPartsNav.ViewControllers.Length > 1) _navWorkflow._tabs.UsedPartsNav.PopToRootViewController (false);
					_navWorkflow._tabs.UsedPartsNav.PushViewController (_navWorkflow._tabs._jobInstall, false);
				}
				break; 
			}
			case "UNI" : { 
				_navWorkflow._tabs._jobUninstall.ThisJob = _navWorkflow._tabs._jobRunTable.CurrentJob;
				_navWorkflow._tabs._jobUninstall.LoadParts ();
				_navWorkflow._tabs.SelectedViewController = _navWorkflow._tabs.ViewControllers[2];
				if (_navWorkflow._tabs.UsedPartsNav.VisibleViewController is JobUninstallViewController)
				{
					// do nothing
				}
				else 
				{
					if (_navWorkflow._tabs.UsedPartsNav.ViewControllers.Length > 1) _navWorkflow._tabs.UsedPartsNav.PopToRootViewController (false);
					_navWorkflow._tabs.UsedPartsNav.PushViewController (_navWorkflow._tabs._jobUninstall, false);
				}
				break; 
			}
			case "DLV" : {
				_navWorkflow._tabs._jobDelivery.ThisJob = _navWorkflow._tabs._jobRunTable.CurrentJob;
				_navWorkflow._tabs._jobDelivery.LoadParts ();
				_navWorkflow._tabs.SelectedViewController = _navWorkflow._tabs.ViewControllers [2];

				if (_navWorkflow._tabs.UsedPartsNav.VisibleViewController is JobDeliveryViewController) {
					// do nothing
				} 
				else {
					if (_navWorkflow._tabs.UsedPartsNav.ViewControllers.Length > 1)
						_navWorkflow._tabs.UsedPartsNav.PopToRootViewController (false);
					
					_navWorkflow._tabs.UsedPartsNav.PushViewController (_navWorkflow._tabs._jobDelivery, false);
				}
				break;
			}
			
			default : { _navWorkflow._tabs.SelectedViewController = _navWorkflow._tabs.ViewControllers[0]; break; }
			}
			
			// _navWorkflow._tabs.SelectedViewController = _navWorkflow._tabs.ViewControllers[_navWorkflow._tabs.LastSelectedTab];
		}
		
		public bool ArePartsOK()
		{
			var curj = this.selectedJob;
			if (curj.HasParent ()) curj = this._navWorkflow._tabs._jobRunTable.FindParentJob (curj);
			
			if ( (curj.Type.Code != "SER" && curj.Type.Code != "UNI" && curj.Type.Code != "REI") 
			    	&& (curj.UsedParts.Count == 0))
				return false;
			else {
				// uninstall, re-install and service jobs should be allowed to be performed without using any stock parts
				return true;
			}
		}
		
		void acProceed (NSObject sender)
		{
			// analyze parts used for the jobs in the current cluster
			bool partsOK = ArePartsOK();
			if (! partsOK)
			{
				var alert = new UIAlertView("Cannot proceed", "One or more of the jobs have empty lists of parts used for them. Please check the stock used for jobs and correct this.", null, "OK");
				alert.Show ();
				return;
			}

			if (CanProceed || CalculateMoneyToCollect() < 0.1) // if money to collect is 0, we allow the user to proceed regardless
			{
				double moneyToCollect = CalculateMoneyToCollect ();
				double moneyReceived;
				string tmp = "";
				for (int i = 0; i < tfTotalMoneyReceived.Text.Length; i++)
				{
					char c = tfTotalMoneyReceived.Text[i];
					if (char.IsLetterOrDigit (c) || char.IsPunctuation (c))
						tmp += c;
				}
				moneyReceived = double.Parse (tmp);
				
				if (moneyReceived < moneyToCollect)
				{
					// check if there are deposits involved, it may still be fine
					double depositFieldValue = double.Parse (tfDeposit.Text.Replace ("$", " "));
					if (moneyReceived < moneyToCollect - depositFieldValue) {
						var moneyAlert = new UIAlertView ("", "Money received is less than money to be collected. Are you sure?", null, "No", "Yes");
						moneyAlert.Dismissed += HandleMoneyAlertDismissed;
						moneyAlert.Show ();
					} else {
						MoneyReceived = moneyToCollect - depositFieldValue;
						GoToSigning ();
					}
				}
				else
				{
					MoneyReceived = moneyToCollect; // we do not want to record that we received more than we should, this is between our serviceman and the customer
					if (moneyToCollect < 0.01)
					{
						var ReceivedZero = new UIAlertView("Money received is zero. Are you sure?", "", null, "No", "Yes");
						ReceivedZero.Dismissed += delegate(object _sender, UIButtonEventArgs e) {
							if (e.ButtonIndex != ReceivedZero.CancelButtonIndex)
								GoToSigning ();
						};
						ReceivedZero.Show ();
					}
					else GoToSigning ();
				}
			}
			else // cannot proceed = must enter valid payment info first
			{ // FIXME :: sometimes this will be incorrect (split payment)
				if (this._payments.Count > 0)
				{
					switch (this._payments[0].Type)
					{
						case PaymentTypes.Cheque: 
							var enterChequeNumberAlert = new UIAlertView("Cannot proceed", "Please enter a valid cheque number.", null, "OK");
							enterChequeNumberAlert.Show ();
						break;
						case PaymentTypes.CreditCard:
							var enterCreditCardDetailsAlert = new UIAlertView("Cannot proceed", "Please enter valid credit card details.", null, "OK");
							enterCreditCardDetailsAlert.Show ();
						break;
						default:
							var choosePaymentMethodAlert = new UIAlertView("Cannot proceed", "Please choose a payment method first.", null, "OK");
							choosePaymentMethodAlert.Show ();
						break;
					}
				}
				else // payments list is empty -- no payment type selected
				{
					var choosePaymentMethodAlert = new UIAlertView("Cannot proceed", "Please choose a payment method first.", null, "OK");
					choosePaymentMethodAlert.Show ();
				}
			}
		}

		void HandleMoneyAlertDismissed (object sender, UIButtonEventArgs e)
		{
			if (e.ButtonIndex == (sender as UIAlertView).CancelButtonIndex)
			{
				// user canceled
			}
			else 
			{
				// so the guy tells us that he has received an incomplete payment -- we must INSERT a record INTO Followups
				using (var connection = new SqliteConnection("Data Source=" + ServerClientViewController.dbFilePath) )
				{
					var cmd = connection.CreateCommand();
					connection.Open();
					// add a followup so that someone can look into it and issue an invoice or something
					string sql = "INSERT INTO Followups (JOB_ID, REASON_ID, DONE) VALUES (?, ?, ?)";
					cmd.CommandText = sql;
					cmd.Parameters.Clear ();
					cmd.Parameters.Add ("@Job_ID", System.Data.DbType.Int64).Value = (selectedJob.HasParent ()) ? selectedJob.ParentJobBookingNumber : selectedJob.JobBookingNumber;
					cmd.Parameters.Add ("@Reason_ID", System.Data.DbType.Int32).Value = 8; // 8 is ID for "Payment Incomplete" follow-up reason (see FU_REASONS table)
					cmd.Parameters.Add ("@Done", System.Data.DbType.Int32).Value = 0; // 0 is for "Not Done" -- the follow-up will be marked as "Done" when the situation is resolved
					cmd.ExecuteNonQuery ();
				}				
				string tmp = "";
				for (int i = 0; i < tfTotalMoneyReceived.Text.Length; i++)
				{
					char c = tfTotalMoneyReceived.Text[i];
					if (char.IsLetterOrDigit (c) || char.IsPunctuation (c))
						tmp += c;
				}
				MoneyReceived = double.Parse (tmp);
				GoToSigning ();
			}
		}
		
		void GoToSigning()
		{
			if (! this.SplitPaymentMode)
			{
				if (_navWorkflow._tabs._jobRunTable.CurrentJob.Payments.Count == 0)
					_navWorkflow._tabs._jobRunTable.CurrentJob.Payments.Add (new JobPayment());
				else 
				{
					_navWorkflow._tabs._jobRunTable.CurrentJob.Payments[0].Amount = MoneyReceived;

					if (this._payments.Count == 0)					
						_navWorkflow._tabs._jobRunTable.CurrentJob.Payments[0].Type = PaymentTypes.None;
					else 
						_navWorkflow._tabs._jobRunTable.CurrentJob.Payments[0].Type = this._payments[0].Type;
				}
			}
			else {
				// split payments

				// TODO :: think of a way when this could be incorrect
				// I see no reason to implement the below algorithms now, but I could be wrong

				// determine the payment type chosen for payment 1
				// look up the list of payments and set the amount to the text field value

				// determine the payment type chosen for payment 2
				// look up the list of payments and set the amount to the text field value
			}


			GenerateReceiptPDFPreview();
			RedrawReceiptPDF(false);

			_navWorkflow._tabs._prePlumbView.GeneratePrePlumbingPDFpreview ();
			_navWorkflow._tabs._prePlumbView.RedrawPrePlumbingPDF (false, false);
		
			_navWorkflow._tabs.LastSelectedTab = _navWorkflow._tabs.SelectedIndex;
			_navWorkflow._tabs.SelectedViewController = _navWorkflow._tabs.ViewControllers[_navWorkflow._tabs.LastSelectedTab + 1];
			if ( ! _navWorkflow._tabs._prePlumbView.IsDefault () )
			{
				// pop to fake root
				_navWorkflow._tabs.SigningNav.PopToRootViewController (false);	
				_navWorkflow._tabs.SigningNav.PushViewController (_navWorkflow._tabs.SignPre, false);
			}
			else // no pre-plumbing to sign, trying service
			{
				DetailedTabs Tabs = _navWorkflow._tabs;
				bool ShouldSignService = false;

				Job main = (Tabs._jobRunTable.CurrentJob.HasParent ())?  Tabs._jobRunTable.FindParentJob (Tabs._jobRunTable.CurrentJob) : Tabs._jobRunTable.CurrentJob;
				if (main.JobReportAttached) ShouldSignService = true;
				foreach(Job child in main.ChildJobs)
				{
					if (child.JobReportAttached)
					{ 
						ShouldSignService = true; 
						break; 
					}
				}

				if (ShouldSignService)
				{
					Tabs.SigningNav.PopToRootViewController (false);
					Tabs.SigningNav.PushViewController (Tabs.SignService, true);
				}
				else // no pre-plumbing to sign, no service report to sign, push invoice
				{
					Tabs.SigningNav.PopToRootViewController(false);
					Tabs.SigningNav.PushViewController (Tabs.SignInvoice, true);
					// _navWorkflow._finishWorkflow(null, null);
					this.selectedJob = null;
				}			
			}
		}
		
		void acClearChildJobs (NSObject sender)
		{
			Summary.ClearChildJobs ();
		}
		
		void acSetLoyalty (NSObject sender)
		{
			_navWorkflow._setLoyaltyPrices(sender, null);
		}
		
		void acAddAnotherJob (NSObject sender)
		{
			_navWorkflow._extraJobs(sender, null);
		}
		
		void acAmountEditingDidEnd ()
		{
			
			double a;
			bool canConvert = double.TryParse (tfToBeCollected.Text, out a);
			if (canConvert && a >= 0)
			{
				selectedJob.Payments[0].Amount = a;
				tfToBeCollected.Text = String.Format ("$ {0:0.00}", a);
			}
			else {
				using (UIAlertView alert = new UIAlertView("Cannot parse input", "You probably mistyped a number, please try again.", null, "OK") ) 
				{	alert.Show (); }
				tfToBeCollected.Text = String.Format ("$ {0:0.00}", selectedJob.Payments[0].Amount);					
			}
		}
		
		void acChequeNumberEditingDidEnd (NSObject sender)
		{
			char a; string newString = ""; bool ok =true;
			if (tfChequeNumber.Text == "") 
			{
				// empty strings are not valid
				lbChequeNumberInvalid.Hidden = false;			
			}
			else 
			{
				for (int i=0; i < tfChequeNumber.Text.Length; i++)
				{
					a = tfChequeNumber.Text[i];
					if ( char.IsDigit (a) ) newString+=a; // throwing out non-digit characters
					else { ok = false; break; }
				}
				
				if ( newString.Length > 10) ok = false; // assuming that 10 digits is the maximum length for cheque numbers
				if (ok) {
					tfChequeNumber.Text = newString;
					// find the payment that has Cheque type and put the new value in
					foreach(JobPayment payment in selectedJob.Payments)
						if (payment.Type == PaymentTypes.Cheque)
							payment.ChequeNumber = newString;
					lbChequeNumberInvalid.Hidden = true;
				}
				else {
					// clear out all cheque numbers for payments of "Cheque" type
					foreach(JobPayment payment in selectedJob.Payments)
						if (payment.Type == PaymentTypes.Cheque)
							payment.ChequeNumber = "";
					lbChequeNumberInvalid.Hidden = false;
				}
			}
		}
		
		void acExpiryDateEditingDidEnd (NSObject sender)
		{
			char a; string newString=""; bool ok = true;
			for (int i = 0; i < tfCreditCardExpiry.Text.Length; i++)		// throwing out all non-digit characters from input
			{
				a = tfCreditCardExpiry.Text[i];
				if ( char.IsDigit (a) ) newString+=a;
			}
			if (newString.Length != 4) // if length of digits in input is not 4, it's invalid 
				ok = false;
			else {
				string tmp = newString.Substring (0,2); // tmp is month number now
				int r;
				if ( int.TryParse (tmp, out r) ) {
					if (r > 12 || r == 0)
						ok = false;	// month number must not be 0 or exceed 12
				}
				else ok = false;
				tmp = newString.Substring (2,2); // tmp is year
				if ( int.TryParse (tmp, out r) ) {
					if (r < DateTime.Now.Date.Year % 100 ) ok = false;		// year must not be less than current , otherwise we're dealing with expired card
				}
				else ok=false;
			}
			if (! ok) {
				lbExpiryDateInvalid.Hidden = false;
				foreach(JobPayment payment in selectedJob.Payments)
					if (payment.Type == PaymentTypes.CreditCard)
						payment.CreditCardExpiry = "";
			}
			else {
				lbExpiryDateInvalid.Hidden = true;
				foreach(JobPayment payment in selectedJob.Payments)
					if (payment.Type == PaymentTypes.CreditCard)
						payment.CreditCardExpiry = newString;
				tfCreditCardExpiry.Text = newString.Substring (0,2) + "/" + newString.Substring (2,2);
			}
		}
		
		void acCreditCardNameEditingDidEnd (NSObject sender)
		{
			if (tfCreditCardName.Text == "") {
				return;
			}
			char a; bool ok = true;
			for (int i=0; i < tfCreditCardName.Text.Length; i++)
			{
				a = tfCreditCardName.Text[i];
				if ( char.IsDigit (a) ) ok = false;	// name should contain no digits
			}
			if (! ok) {
				lbCardOwnerNameInvalid.Hidden = false;
				foreach(JobPayment payment in selectedJob.Payments)
					if (payment.Type == PaymentTypes.CreditCard)
						payment.CreditCardName = "";
				// selectedJob.Payments.CreditCardName = "";
				// tfCreditCardName.Text = selectedJob.Payment.CreditCardName;
			}
			else { 
				lbCardOwnerNameInvalid.Hidden = true;
				foreach(JobPayment payment in selectedJob.Payments)
					if (payment.Type == PaymentTypes.CreditCard)
						payment.CreditCardName = tfCreditCardName.Text;
				// selectedJob.Payments.CreditCardName = tfCreditCardName.Text;
			}
		}
		
		public void HideAllPaymentOptions()
		{
			if (tfChequeNumber != null) tfChequeNumber.Hidden = true;
			if (tfCreditCardNumber != null) tfCreditCardNumber.Hidden = true;
			if (tfCreditCardName != null) tfCreditCardName.Hidden = true;
			if (tfCreditCardExpiry != null) tfCreditCardExpiry.Hidden = true;
			if (lbCCNumberTip != null) lbCCNumberTip.Hidden = true;
			if (lbCardOwnerNameInvalid != null) lbCardOwnerNameInvalid.Hidden = true;
			if (lbChequeNumberInvalid != null) lbChequeNumberInvalid.Hidden = true;
		}
		
		public void ShowPaymentChoiceOptions()
		{
			tfPaymentType.Hidden = false;
			HideChequePaymentOptions ();
			HideCreditCardPaymentOptions ();
		}
		
		public void HideChequePaymentOptions()
		{
			tfChequeNumber.Hidden = true;
			lbChequeNumberInvalid.Hidden = true;
		}
		
		public void ShowChequePaymentOptions()
		{
			tfChequeNumber.Hidden = false;
		}
		
		public void HideCreditCardPaymentOptions()
		{
			tfCreditCardNumber.Hidden = true;
			tfCreditCardName.Hidden = true;
			tfCreditCardExpiry.Hidden = true;
			// lbEnterExpiryDate.Hidden = true;
			lbCardOwnerNameInvalid.Hidden = true;
			lbCCNumberTip.Hidden = true;
			lbExpiryDateInvalid.Hidden = true;
		}
		
		public void ShowCreditCardPaymentOptions()
		{
			tfCreditCardNumber.Hidden = false;
			tfCreditCardName.Hidden = false;
			tfCreditCardExpiry.Hidden = false;
			// lbEnterExpiryDate.Hidden = false;
		}

		private bool resettingToDefaults;
		public void ResetToDefaults() 
		{
			resettingToDefaults = true;
			if (this.View != null) {
				this.scPaymentType.SelectedSegment = -1;

				tfToBeCollected.Hidden = false;
				
				HideAllPaymentOptions();
				lbTip.Text = "Please choose a payment type above.";
				
				tfChequeNumber.Text = "";
				tfCreditCardExpiry.Text = "";
				tfCreditCardName.Text = "";
				tfCreditCardNumber.Text = "";
				tfInvoicePO.Text = "";
				tfPaymentType.Text = "";
			}

			if (this.SplitPaymentMode == true)
				this.SplitPaymentMode = false;
			resettingToDefaults = false;

			// unlink the payments from the _jobRunTable.CurrentJob
			_payments = new List<JobPayment>();
		}
		
		public void DisableToolbar()
		{
			UIView.BeginAnimations (null);
			UIView.SetAnimationDuration (0.3);
			myToolbar.UserInteractionEnabled = false;
			myToolbar.Alpha = 0.5f;
			UIView.CommitAnimations();
		}
		
		public void EnableToolbar()
		{			
			UIView.BeginAnimations (null);
			UIView.SetAnimationDuration (0.3);
			myToolbar.UserInteractionEnabled = true;
			myToolbar.Alpha = 1f;
			UIView.CommitAnimations();
		}
		
		public bool ValidateCreditCardNumber(string CardNumber)
		{
			byte[] number = new byte[16];
			int len = 0;
			
			for(int i = 0; i < CardNumber.Length; i++)
			{
				if(char.IsDigit(CardNumber, i))
				{
					if(len == 16) return false;
					number[len++] = byte.Parse(CardNumber[i].ToString());
				}
			}
			if (len != 16) return false;
			
			// Using Luhn Algorithm to validate
		    int sum = 0;
			for(int i = len - 1; i >= 0; i--)
			{
			    if(i % 2 == len % 2)
			    {
			    	int n = number[i] * 2;
			    	sum += (n / 10) + (n % 10);
			    }
				else sum += number[i];
			}
			return (sum % 10 == 0);			
		}

		public override void ViewDidAppear (bool animated)
		{
			if (selectedJob != null)
			{
				Job current = _navWorkflow._tabs._jobRunTable.CurrentJob;

				// if no payment choices have been made for current job and some have been made for the job that was selected on payment screen

				// WAS :: if ( (!selectedJob.Payments.Type.Contains (PaymentTypes.None)) && current.Payments.Type.Contains(PaymentTypes.None))
				if ((!ContainsPaymentType (selectedJob.Payments, PaymentTypes.None)) && ContainsPaymentType (current.Payments, PaymentTypes.None))
				{
					// copy payment choices made previously to CurrentJob
					current.Payments.Clear ();
					foreach(JobPayment payment in selectedJob.Payments)
					{
						current.Payments.Add (new JobPayment() {
							Type = payment.Type,
							Amount = payment.Amount,
							PaymentCustomerNumber = payment.PaymentCustomerNumber,
							CreditCardExpiry = payment.CreditCardExpiry,
							CreditCardName = payment.CreditCardName,
							CreditCardNumber = payment.CreditCardNumber,
							ChequeNumber = payment.ChequeNumber
						});
					}
				}
			}
			selectedJob = _navWorkflow._tabs._jobRunTable.CurrentJob;

			if (selectedJob.Payments.Count > 0)
			{
				if (selectedJob.HasNoParent ())
				{
					if (SplitPaymentMode == false && selectedJob.Payments.Count > 1)
					{
						selectedJob.Payments.RemoveAt (1);
						foreach(Job child in selectedJob.ChildJobs)
							if (child.Payments.Count > 1) child.Payments.RemoveAt (1);
						if (this._payments.Count > 1) this._payments.RemoveAt (1);
					}
					selectedJob.Payments[0].Amount = selectedJob.MoneyToCollect;
				}
				else {
					if (SplitPaymentMode)		// split payment
					{
						// then the first payment amount should be equal to mainJob.MoneyToCollect
						Job main = _navWorkflow._tabs._jobRunTable.FindParentJob (selectedJob);
						selectedJob.Payments[0].Amount = main.MoneyToCollect;
						// the second payment amount should be equal to sum of child jobs' MoneyToCollect properties
						if (selectedJob.Payments.Count < 2) // if the job does not have 2 payments associated, we add one
							selectedJob.Payments.Add (new JobPayment() {
								Type = PaymentTypes.None,
								Amount = 0,
								PaymentCustomerNumber = selectedJob.CustomerNumber,
							} );
						selectedJob.Payments[1].Amount = 0;
						foreach(Job child in main.ChildJobs)
							selectedJob.Payments[1].Amount += child.MoneyToCollect;
					}
					else {
						// normal payment, main's payments[0] amount should be equal to sum of main and all child job prices
						Job main = _navWorkflow._tabs._jobRunTable.FindParentJob (selectedJob);
						selectedJob.Payments[0].Amount = main.MoneyToCollect;
						foreach(Job child in main.ChildJobs)
							selectedJob.Payments[0].Amount += child.MoneyToCollect;
					}
				}
			}
			else {
				selectedJob.Payments.Add ( new JobPayment() {
				    PaymentCustomerNumber = selectedJob.CustomerNumber, 
					Amount = selectedJob.MoneyToCollect, 
					Type = PaymentTypes.None });
			}
			
			double money = CalculateMoneyToCollect ();
			double deposit = _navWorkflow._tabs._jobRunTable.CurrentCustomer.DepositAmount - _navWorkflow._tabs._jobRunTable.CurrentCustomer.DepositUsed;
			tfToBeCollected.Text = String.Format ("$ {0:0.00}", money - deposit);

			if (deposit > 0) {
				tfDeposit.Text = String.Format ("$ {0:0.00}", deposit);
				ShowDeposit ();
			} else {
				HideDeposit ();
			}

			tfTotalMoneyReceived.Text = (this.ContainsInvoicePaymentType (_payments)) ? String.Format ("$ {0:0.00}", 0) : tfToBeCollected.Text;
			CalculateFees();
			
			base.ViewDidAppear (animated);
			Summary.ViewDidAppear (animated);
			// lbJobsInCluster.Text = "Jobs for current customer: " + jobCount.ToString ();
		}

		public void HideDeposit() {
			// hide the appropriate interface elements
			tfDeposit.Hidden = true;
			lbDeposit.Hidden = true;
		}

		public void ShowDeposit() {
			// make the appropriate interface elements visible
			tfDeposit.Hidden = false;
			lbDeposit.Hidden = false;
		}
		
		public double CalculateMoneyToCollect()
		{
			int jobCount = 1; double money = selectedJob.MoneyToCollect;
			if ( selectedJob.HasParent() ) { // CurrentJob is a ChildJob
				
				// looking for parent job
				Job parentJob = _navWorkflow._tabs._jobRunTable.FindParentJob (selectedJob); 
				
				// found parent, now let's count
				money = parentJob.MoneyToCollect;
				foreach(Job j in parentJob.ChildJobs) 
				{
					jobCount += 1;
					money += j.MoneyToCollect;
				}
			}
			else 
			{
				money = selectedJob.MoneyToCollect;
				if (selectedJob.ChildJobs != null)
					foreach(Job child in selectedJob.ChildJobs)
					{
						money += child.MoneyToCollect;
						jobCount += 1;
					}
			}

			return money;
		}
		
		public void CalculateFees()
		{
			selectedJob = _navWorkflow._tabs._jobRunTable.CurrentJob;
			if (MyConstants.EmployeeType == MyConstants.EmployeeTypes.Plumber)
			{
				if (selectedJob.HasParent() )
				{
					Job main = _navWorkflow._tabs._jobRunTable.FindParentJob (selectedJob);
					selectedJob = main;
					selectedJob.ShouldPayFee = true;
					selectedJob.EmployeeFee = selectedJob.Type.EmployeeFee;

					if (selectedJob.ChildJobs != null)
						foreach(Job childJob in selectedJob.ChildJobs)
						{
							childJob.ShouldPayFee = false;
							childJob.EmployeeFee = 0;
						}
				}
				else {
					selectedJob.ShouldPayFee = true;

					// if the fee has not yet been determined by other means (e.g. setting extras in JobInstallationViewController), set the fee to standard for the job type 
					if (selectedJob.EmployeeFee < 1)
						selectedJob.EmployeeFee = selectedJob.Type.EmployeeFee;

					// plumbers do not get paid for the child jobs separately, so the child jobs' fees are set to 0
					if (selectedJob.ChildJobs != null)
						foreach(Job childJob in selectedJob.ChildJobs)
						{
							childJob.ShouldPayFee = false;
							childJob.EmployeeFee = 0;
						}
				}
			}
			
			if (MyConstants.EmployeeType == MyConstants.EmployeeTypes.Franchisee)
			{
				if (selectedJob.HasParent ())
				{
					Job main = _navWorkflow._tabs._jobRunTable.FindParentJob (selectedJob);
					selectedJob = main;
				}
			
				selectedJob.ShouldPayFee = true; // THIS LOGIC HAS BEEN RECONSIDERED 15.11.2012 : All jobs generate a fee now :: (selectedJob.Type.Code == "SER" && selectedJob.HasChildJobs ()) ? false : true;

				if (selectedJob.Type.Code != "HDTUBING") selectedJob.EmployeeFee = selectedJob.Type.EmployeeFee; // if Main job is HDTubing, the fees have been set on the Parts screen

				if (selectedJob.ChildJobs != null)
					foreach(Job childJob in selectedJob.ChildJobs)
					{
						/* THIS LOGIC HAS BEEN RECONSIDERED 15.11.2012 :: All jobs generate a fee now
						if (childJob.Type.Code == "SER")
						{
							childJob.ShouldPayFee = false;
							childJob.EmployeeFee = 0;
						}
						else { */
							childJob.ShouldPayFee = true;
							if (childJob.Type.Code != "HDTUBING") childJob.EmployeeFee = childJob.Type.EmployeeFee;	// if child job is HDTubing, the fees have been set on the Parts screen
						// }
					}
			}
		}
		
		public bool CheckIfCanProceed()
		{
			if (_navWorkflow._tabs._jobRunTable.CurrentJob != null)
			{
				Job current = _navWorkflow._tabs._jobRunTable.CurrentJob;
				Job main;
				if (current.HasParent () )
					main = _navWorkflow._tabs._jobRunTable.FindParentJob (current);
				else main = current;
				
				double money = CalculateMoneyToCollect ();
				// foreach(Job child in main.ChildJobs)
				// 	money += child.MoneyToCollect;
				
				if (money < 0.01) return true;

				if (!this.SplitPaymentMode)
				{
					switch(main.Payments[0].Type)
					{
					case PaymentTypes.Invoice : { return true; }
					case PaymentTypes.Cash : { return true; }
					case PaymentTypes.Cheque : { return IsChqNumberOK(); }
					case PaymentTypes.CreditCard : { return (IsCreditCardNumberOK() && IsCreditCardNameOK() && IsCreditCardExpiryOK()); }
					case PaymentTypes.EFTPOS : { return true; }
					case PaymentTypes.CCDetails : { return true; }
					default: return false;
					}
				}
				else // split payment mode
				{
					// check if both payment methods are selected
					if ( scSplitPaymentMethod1.SelectedSegment == -1 || scSplitPaymentMethod2.SelectedSegment == -1 )
						return false;
					// sum the numbers in payment fields
					double moneyReceived = CalculateSplitPaymentSum();
					if (moneyReceived < CalculateMoneyToCollect())
						return false; 

					bool IsOk = true;
					foreach (JobPayment payment in main.Payments)					
						if (payment.Type == PaymentTypes.Cheque)
							IsOk = IsOk && IsChqNumberOK (); 

					return IsOk;
				}
			}
			else return false;
		}

		public double CalculateSplitPaymentSum()
		{
			string text1 = ""; string text2 = "";
			char c;
			for (int i=0; i<tfSplitPaymentAmount1.Text.Length; i++)
			{
				c = tfSplitPaymentAmount1.Text[i];
				if (char.IsLetterOrDigit(c) || char.IsPunctuation (c)) text1 += c;
			}
			for (int i=0; i<tfSplitPaymentAmount2.Text.Length; i++)
			{
				c = tfSplitPaymentAmount2.Text[i];
				if (char.IsLetterOrDigit(c) || char.IsPunctuation (c)) text2 += c;
			}

			double amount1 = double.Parse (text1);
			double amount2 = double.Parse (text2);
			double sum = amount1 + amount2;
			return sum;
		}
		
		public bool IsChqNumberOK()
		{
			char a; string newString = ""; bool ok =true;
			if (tfChequeNumber.Text == "") 
			{
				return false;
			}
			else 
			{
				for (int i=0; i < tfChequeNumber.Text.Length; i++)
				{
					a = tfChequeNumber.Text[i];
					if ( char.IsDigit (a) ) newString+=a;
					else { ok = false; break; }
				}
				
				if ( newString.Length > 8) ok = false; // assuming that 8 digits is the maximum length for cheque numbers
				return ok;
			}
		}
		
		public bool IsCreditCardNumberOK()
		{
			return ValidateCreditCardNumber ( tfCreditCardNumber.Text );
		}
		
		public bool IsCreditCardNameOK()
		{
			if (tfCreditCardName.Text == "") {
				return false;
			}
			char a; bool ok = true;
			for (int i=0; i < tfCreditCardName.Text.Length; i++)
			{
				a = tfCreditCardName.Text[i];
				if ( char.IsDigit (a) ) ok = false;	// name should contain no digits
			}
			return ok;
		}
		
		public bool IsCreditCardExpiryOK()
		{
			char a; string newString=""; bool ok = true;
			for (int i = 0; i < tfCreditCardExpiry.Text.Length; i++)		// throwing out all non-digit characters from input
			{
				a = tfCreditCardExpiry.Text[i];
				if ( char.IsDigit (a) ) newString+=a;
			}
			if (newString.Length != 4) // if length of digits in input is not 4, it's invalid 
				ok = false;
			else {
				string tmp = newString.Substring (0,2); // tmp is month number now
				int r;
				if ( int.TryParse (tmp, out r) ) {
					if (r > 12 || r == 0)
						ok = false;	// month number must not be 0 or exceed 12
				}
				else ok = false;
				tmp = newString.Substring (2,2); // tmp is year
				if ( int.TryParse (tmp, out r) ) {
					if (r < DateTime.Now.Date.Year % 100 ) ok = false;		// year must not be less than current , otherwise we're dealing with expired card
				}
				else ok=false;
			}
			return ok;
		}
		
		public override void ViewDidDisappear (bool animated)
		{
			// CanProceed = CheckIfCanProceed();
			base.ViewDidDisappear (animated);
			Summary.ViewDidDisappear (animated);
		}
		
		public override void DidReceiveMemoryWarning ()
		{
			// Releases the view if it doesn't have a superview.
			// base.DidReceiveMemoryWarning ();
			
			// Release any cached data, images, etc that aren't in use.
		}
		
		public void FormatInput(UITextField textField, NSRange range, string replacementString)
		{
			string CurrentValue = textField.Text;
			string formattedValue = CurrentValue;
			
			char maskChar = ccNumberMask[range.Location];
			if (maskChar != '9') 
			{ 
				formattedValue += maskChar; 
				if (range.Location+1 < ccNumberMask.Length) 
				{
					maskChar = ccNumberMask[range.Location+1];
					if (maskChar==' ') formattedValue += maskChar;
				}
			}
		
			formattedValue += replacementString;
			textField.Text = formattedValue;
			
			if (formattedValue.Length == ccNumberMask.Length) 
			{
				if ( ValidateCreditCardNumber (formattedValue) )
				{  
					// green label signaling that the number is correct
					lbCCNumberTip.Text = "Credit Card number valid";
					lbCCNumberTip.TextColor = UIColor.Green;
					foreach(JobPayment payment in selectedJob.Payments)
						if (payment.Type == PaymentTypes.CreditCard)
							payment.CreditCardNumber = formattedValue;
					// selectedJob.Payments.CreditCardNumber = formattedValue;
				}
				else {
					// red label signaling that the number is incorrect
					lbCCNumberTip.Text = "Credit Card number is invalid, please double-check";
					lbCCNumberTip.TextColor = UIColor.Red;
				}
				lbCCNumberTip.Hidden = false;
			}
			else lbCCNumberTip.Hidden = true;
		}
		
		private bool CCNShouldChangeCharacters(UITextField textField, NSRange range, string replacementString)
		{
			// TODO :: less restrictions on user input, bring cursor to the end of the input after the field text has been formatted
			if (range.Location != textField.Text.Length && 
			    !(replacementString == "" && range.Location == textField.Text.Length-1) ) 
				return false; 	// denies input when the user attempts to enter or delete stuff in the middle of the string, except for deleting the last character
			if (replacementString == "") 
			{
				lbCCNumberTip.Hidden = true;
				return true;		// combined with previous logic, allows to delete characters (only at the end of the string)
			}
			
			if (! char.IsDigit (replacementString[0])) return false; // denies all non-digit characters
			
			if (textField.Text.Length == ccNumberMask.Length)
			{
				return false;			// denies input when user tries to enter stuff after the string length has reached ccNumberMask's length									
			}	
			else {
					FormatInput (textField, range, replacementString);	// calls the formatting routine to set the field's Text value
					return false;		// denies default input handling (all necessary things have already happened in FormatInput() call
			}
		}
		
		public override void ViewDidLoad ()
		{
			base.ViewDidLoad ();
			
			// if iOS 7 -- bring the toolbar down 14 pixels
			if (UIDevice.CurrentDevice.SystemVersion.Split ('.')[0] == "7") {
				RectangleF current = this.myToolbar.Frame;

				this.myToolbar.Frame = new RectangleF (current.X, current.Y+14, current.Width, current.Height);
				this.myToolbar.SetNeedsLayout ();
			}


			//any additional setup after loading the view, typically from a nib.			
			lbExpiryDateInvalid.TextColor = UIColor.Red;
			lbChequeNumberInvalid.TextColor = UIColor.Red;
			lbCardOwnerNameInvalid.TextColor = UIColor.Red;
			
			tfCreditCardNumber.ShouldChangeCharacters = CCNShouldChangeCharacters;
			
			 /*
			 // FUCK THIS SHIT
			 // After a MonoTouch update to 5.2.12 the interaction with XCode's interface builder has been broken (Xamarin admitted it was their fuckup), 
			 // and I needed to get the interface working somehow, so... see below
			 */
			
			btnBack.Clicked += HandlebtnBackClicked;
			btnProceed.Clicked += HandlebtnProceedClicked;
			btnClearChildJobs.Clicked += HandleBtnClearChildJobsClicked;
			btnAddChildJob.Clicked += HandleBtnAddAnotherJobClicked;
			btnSplitPayment.Clicked += HandleBtnSplitPaymentClicked;

			scPaymentType.ValueChanged += HandlePaymentControlValueChanged;
			scSplitPaymentMethod1.ValueChanged += HandleSplitMethod1ValueChanged;
			scSplitPaymentMethod2.ValueChanged += HandleSplitMethod2ValueChanged;

			tfCreditCardExpiry.EditingDidEndOnExit += HandleTfCreditCardExpiryEditingDidEndOnExit;
			tfCreditCardNumber.EditingDidEndOnExit += HandleCreditCardNumberEditingDidEndOnExit;
			tfCreditCardName.EditingDidEndOnExit += HandleTfCreditCardNameEditingDidEndOnExit;
			tfChequeNumber.EditingDidEndOnExit += HandleTfChequeNumberEditingDidEndOnExit;
			tfToBeCollected.EditingDidEndOnExit += HandleTfToBeCollectedEditingDidEndOnExit;
			tfTotalMoneyReceived.EditingDidEndOnExit += HandleTfTotalMoneyReceivedEditingDidEnd;

			tfCreditCardNumber.EditingDidEnd += HandleCreditCardNumberEditingDidEndOnExit;
			tfCreditCardName.EditingDidEnd += HandleTfCreditCardNameEditingDidEndOnExit;
			tfCreditCardExpiry.EditingDidEnd += HandleTfCreditCardExpiryEditingDidEndOnExit;
			tfChequeNumber.EditingDidEnd += HandleTfChequeNumberEditingDidEndOnExit;
			tfToBeCollected.EditingDidEnd += HandleTfToBeCollectedEditingDidEndOnExit;
			tfTotalMoneyReceived.EditingDidEnd += HandleTfTotalMoneyReceivedEditingDidEnd;

			tfSplitPaymentAmount1.EditingDidEndOnExit += HandleTfSplitPaymentAmount1EditingDidEndOnExit;
			tfSplitPaymentAmount2.EditingDidEndOnExit += HandleTfSplitPaymentAmount2EditingDidEndOnExit;

			tfSplitPaymentAmount1.EditingDidEnd += HandleTfSplitPaymentAmount1EditingDidEndOnExit;
			tfSplitPaymentAmount2.EditingDidEnd += HandleTfSplitPaymentAmount2EditingDidEndOnExit;
			
			Summary = new JobSummary(_navWorkflow, _navWorkflow._tabs, new RootElement("Job Summary"), false);
			Summary.View.AutosizesSubviews = false;
			Summary.View.AutoresizingMask = UIViewAutoresizing.None;
			Summary.View.Frame = new RectangleF(20,411,663,233); // upper position = (20,60,663,353)
			this.View.AddSubview (Summary.TableView);
			this.View.SetNeedsLayout();

			tfTotalMoneyReceived.ShouldBeginEditing = delegate(UITextField textField) {
				if (this.SplitPaymentMode)		// not allowed in split payment mode
				{
					var notAllowedInSplitPaymentMode = new UIAlertView("Not allowed", "Editing amount of money received is not allowed in split payment mode", null, "OK");
					notAllowedInSplitPaymentMode.Show ();
					return false;
				}
				else
				{			// not allowed for certain payment types
					// WAS :: if (this.PaymentType.Contains (PaymentTypes.Invoice) || this.PaymentType.Contains (PaymentTypes.CCDetails) || this.PaymentType.Contains (PaymentTypes.CreditCard))
					if (this.ContainsInvoicePaymentType (_payments))
					{
						var notAllowedForPaymentType = new UIAlertView("Not allowed", 
							String.Format("Editing amount of money received is not allowed because selected payment type is \"{0}\"", 
							MyConstants.OutputStringForValue (this._payments[0].Type)), null, "OK");
						notAllowedForPaymentType.Show ();
						return false;
					}
				}
				return true;
			};

			tfToBeCollected.ShouldBeginEditing = delegate(UITextField textField) {
				var notAllowedAlert = new UIAlertView("Not allowed", 
					                                      String.Format("To change job prices, use the \"Adjust prices\" button in the upper right corner of the screen."), null, "OK");
				notAllowedAlert.Show ();
				return false;
			};
			/*
			tfCreditCardNumber.ShouldBeginEditing = delegate(UITextField textField) {
				tfCreditCardExpiry.EditingDidEnd(null, null);
				tfCreditCardName.EditingDidEnd(null, null);
			};

			tfCreditCardExpiry.ShouldBeginEditing = delegate(UITextField textField) {
				tfCreditCardName.EditingDidEnd(null,null);
				tfCreditCardNumber.EditingDidEnd(null,null);
			};

			tfCreditCardName.ShouldBeginEditing = delegate(UITextField textField) {
				tfCreditCardNumber.EditingDidEnd(null,null);
				tfCreditCardExpiry.EditingDidEnd(null,null);
			};
			*/
		}

		void HandleTfSplitPaymentAmount2EditingDidEndOnExit (object sender, EventArgs e)
		{
			double totalToReceive = CalculateMoneyToCollect ();
			double amount2;
			string newValue = "";
			for (int i = 0; i < tfSplitPaymentAmount2.Text.Length; i++)
			{
				char c = tfSplitPaymentAmount2.Text[i];
				if ( char.IsLetterOrDigit( c) || char.IsPunctuation (c) ) { newValue += c; }
			}
			
			bool ok = double.TryParse (newValue, out amount2);
			ok = (ok && amount2 >= 0 && amount2 < totalToReceive);
			if (ok) 
			{
				tfSplitPaymentAmount2.Text = String.Format ("$ {0:0.00}", amount2);
				tfSplitPaymentAmount1.Text = String.Format ("$ {0:0.00}", totalToReceive-amount2);

				this._payments[1].Amount = amount2;
				this._payments[0].Amount = totalToReceive-amount2;

				selectedJob.Payments[1].Amount = amount2;
				selectedJob.Payments[0].Amount = totalToReceive - amount2;
			}
			else 
			{
				// invalid amount entered, ignore
				tfSplitPaymentAmount1.Text = String.Format ("$ {0:0.00}", selectedJob.Payments[0].Amount);
				tfSplitPaymentAmount2.Text = String.Format ("$ {0:0.00}", selectedJob.Payments[1].Amount);
			}
		}

		void HandleTfSplitPaymentAmount1EditingDidEndOnExit (object sender, EventArgs e)
		{
			double totalToReceive = CalculateMoneyToCollect ();
			double amount1;
			string newValue = "";
			for (int i = 0; i < tfSplitPaymentAmount1.Text.Length; i++)
			{
				char c = tfSplitPaymentAmount1.Text[i];
				if ( char.IsLetterOrDigit( c) || char.IsPunctuation (c) ) { newValue += c; }
			}
			
			bool ok = double.TryParse (newValue, out amount1);
			ok = (ok && amount1 >= 0 && amount1 < totalToReceive);
			if (ok) 
			{
				tfSplitPaymentAmount1.Text = String.Format ("$ {0:0.00}", amount1);
				tfSplitPaymentAmount2.Text = String.Format ("$ {0:0.00}", totalToReceive-amount1);

				this._payments[0].Amount = amount1;
				this._payments[1].Amount = totalToReceive-amount1;

				selectedJob.Payments[0].Amount = amount1;
				selectedJob.Payments[1].Amount = totalToReceive - amount1;
			}
			else 
			{
				// invalid amount entered, ignore
				tfSplitPaymentAmount1.Text = String.Format ("$ {0:0.00}", selectedJob.Payments[0].Amount);
				tfSplitPaymentAmount2.Text = String.Format ("$ {0:0.00}", selectedJob.Payments[1].Amount);
			}
		}

		[Obsolete]
		public override void ViewDidUnload ()
		{
			base.ViewDidUnload ();
			
			// Release any retained subviews of the main view.
			// e.g. this.myOutlet = null;
			tfCreditCardExpiry.EditingDidEndOnExit -= HandleTfCreditCardExpiryEditingDidEndOnExit;
			tfCreditCardNumber.EditingDidEndOnExit -= HandleCreditCardNumberEditingDidEndOnExit;
			tfCreditCardName.EditingDidEndOnExit -= HandleTfCreditCardNameEditingDidEndOnExit;
			tfChequeNumber.EditingDidEndOnExit -= HandleTfChequeNumberEditingDidEndOnExit;
			tfToBeCollected.EditingDidEndOnExit -= HandleTfToBeCollectedEditingDidEndOnExit;
			tfTotalMoneyReceived.EditingDidEndOnExit -= HandleTfTotalMoneyReceivedEditingDidEnd;
			
			tfCreditCardExpiry.EditingDidEnd -= HandleTfCreditCardExpiryEditingDidEndOnExit;
			tfCreditCardNumber.EditingDidEnd -= HandleCreditCardNumberEditingDidEndOnExit;
			tfCreditCardName.EditingDidEnd -= HandleTfCreditCardNameEditingDidEndOnExit;
			tfChequeNumber.EditingDidEnd -= HandleTfChequeNumberEditingDidEndOnExit;
			tfToBeCollected.EditingDidEnd -= HandleTfToBeCollectedEditingDidEndOnExit;
			tfTotalMoneyReceived.EditingDidEnd -= HandleTfTotalMoneyReceivedEditingDidEnd;

			btnBack.Clicked -= HandlebtnBackClicked;
			btnProceed.Clicked -= HandlebtnProceedClicked;
			btnClearChildJobs.Clicked -= HandleBtnClearChildJobsClicked;
			btnAddChildJob.Clicked -= HandleBtnAddAnotherJobClicked;		
			btnSplitPayment.Clicked -= HandleBtnSplitPaymentClicked;

			scPaymentType.ValueChanged -= HandlePaymentControlValueChanged;
			scSplitPaymentMethod1.ValueChanged -= HandleSplitMethod1ValueChanged;
			scSplitPaymentMethod2.ValueChanged -= HandleSplitMethod2ValueChanged;

			tfSplitPaymentAmount1.EditingDidEndOnExit -= HandleTfSplitPaymentAmount1EditingDidEndOnExit;
			tfSplitPaymentAmount2.EditingDidEndOnExit -= HandleTfSplitPaymentAmount2EditingDidEndOnExit;

			tfSplitPaymentAmount1.EditingDidEnd -= HandleTfSplitPaymentAmount1EditingDidEndOnExit;
			tfSplitPaymentAmount2.EditingDidEnd -= HandleTfSplitPaymentAmount2EditingDidEndOnExit;
		}
		

		void HandleSplitMethod1ValueChanged (object sender, EventArgs e)
		{
			if (scSplitPaymentMethod2.SelectedSegment != scSplitPaymentMethod1.SelectedSegment)
			{
				switch( (sender as UISegmentedControl).SelectedSegment )
				{
				case 0 : this._payments[0].Type = PaymentTypes.Cash; break;
				case 1 : 
					this._payments[0].Type = PaymentTypes.Cheque;
					ShowChequePaymentOptions ();
					break;
				case 2 : this._payments[0].Type = PaymentTypes.EFTPOS; break;
				default : this._payments[0].Type = PaymentTypes.None; break;
				}

				// if we changed a value and now none of the payment types are "Cheque", we should hide the Cheque number field
				if (scSplitPaymentMethod1.SelectedSegment != 1 && scSplitPaymentMethod2.SelectedSegment != 1)
					HideChequePaymentOptions ();

				if (selectedJob != null)
				{
					selectedJob.Payments = this._payments; // we need to set the selected payments for the current job and all other jobs in the cluster
					if (selectedJob.HasNoParent ()) 
					{
						foreach(Job child in selectedJob.ChildJobs)
							child.Payments = this._payments;
					}
					else 
					{ // current job could be a child job, then we need to find its parent and apply the procedure above to the main and all other child jobs
						Job main = _navWorkflow._tabs._jobRunTable.FindParentJob (selectedJob);
						if (main != null)
						{
							main.Payments = selectedJob.Payments;
							foreach(Job child in main.ChildJobs)
								child.Payments = main.Payments;
						}
						else {
							// could not find parent job for one that has a parent -- something really weird
							var weird = new UIAlertView("Weird", "Could not find parent job for a child job. This is really sad...", null, "OK");
							weird.Show ();
						} 
					} // end if selected job has no parent
				} // end if selectedJob != null
			}
			else {		// the user has chosen the same payment method for both
				var samePaymentMethodsNotAllowed = new UIAlertView("Not allowed", "You cannot select the same payment methods for both parts of a split payment.", null, "OK");
				samePaymentMethodsNotAllowed.Show ();
				scSplitPaymentMethod1.SelectedSegment = -1;
			}
		}

		void HandleSplitMethod2ValueChanged (object sender, EventArgs e)
		{
			if (scSplitPaymentMethod2.SelectedSegment != scSplitPaymentMethod1.SelectedSegment)
			{
				switch( (sender as UISegmentedControl).SelectedSegment )
				{
				case 0 : 
					if (_payments.Count < 2) 
						_payments.Add (new JobPayment() { Type = PaymentTypes.Cash, PaymentCustomerNumber = selectedJob.CustomerNumber });
					else this._payments[1].Type = PaymentTypes.Cash; 
					break;
				case 1 : 
					if (_payments.Count < 2) 
						_payments.Add (new JobPayment() { Type = PaymentTypes.Cheque, PaymentCustomerNumber = selectedJob.CustomerNumber });
					else this._payments[1].Type = PaymentTypes.Cheque;
					ShowChequePaymentOptions ();
					break;
				case 2 : 
					if (_payments.Count < 2) 
						_payments.Add (new JobPayment() { Type = PaymentTypes.EFTPOS, PaymentCustomerNumber = selectedJob.CustomerNumber });
					else this._payments[1].Type = PaymentTypes.EFTPOS; 
					break;
				default : 
					this._payments[0].Type = PaymentTypes.None; 
					break;
				}

				// if we changed a value and now none of the payment types are "Cheque", we should hide the Cheque number field
				if (scSplitPaymentMethod1.SelectedSegment != 1 && scSplitPaymentMethod2.SelectedSegment != 1)
					HideChequePaymentOptions ();

				if (selectedJob != null)
				{
					selectedJob.Payments = this._payments; // we need to set the selected payments for the current job and all other jobs in the cluster
					if (selectedJob.HasNoParent ()) 
					{
						foreach(Job child in selectedJob.ChildJobs)
							child.Payments = this._payments;
					}
					else 
					{ // current job could be a child job, then we need to find its parent and apply the procedure above to the main and all other child jobs
						Job main = _navWorkflow._tabs._jobRunTable.FindParentJob (selectedJob);
						if (main != null)
						{
							main.Payments = selectedJob.Payments;
							foreach(Job child in main.ChildJobs)
								child.Payments = main.Payments;
						}
						else {
							// could not find parent job for one that has a parent -- something really weird
							var weird = new UIAlertView("Weird", "Could not find parent job for a child job. This is really sad...", null, "OK");
							weird.Show ();
						} 
					} // end if selected job has no parent
				} // end if selectedJob != null
			}
			else {	// the user has chosen the same payment method for both
				var samePaymentMethodsNotAllowed = new UIAlertView("Not allowed", "You cannot select the same payment methods for both parts of a split payment.", null, "OK");
				samePaymentMethodsNotAllowed.Show ();
				scSplitPaymentMethod2.SelectedSegment = -1;
			}
		}


		void HandleBtnSplitPaymentClicked (object sender, EventArgs e)
		{
			// Check if there are at least 2 jobs? Otherwise it makes little sense
			this.SplitPaymentMode = !this.SplitPaymentMode;
		}

		void HandlePaymentControlValueChanged (object sender, EventArgs e)
		{
			if (this._payments.Count == 0) this._payments.Add (new JobPayment());

			switch (((UISegmentedControl)sender).SelectedSegment)
			{
			case 0: 
				if (this.SplitPaymentMode == true)
				{
					var notAllowedInSplitPayment = new UIAlertView("Not allowed", "This payment method is not allowed in split payments.\n" +
						"Please choose cash, cheque, EFT POS or switch to normal payment mode.", null, "OK");
					notAllowedInSplitPayment.Show ();
					scPaymentType.SelectedSegment = -1;
					return;
				}
				else 
				{
					this._payments[0].Type = PaymentTypes.Invoice;
					break; 
				}
			case 1: 
				if (this.SplitPaymentMode == true)
				{
					var notAllowedInSplitPayment = new UIAlertView("Not allowed", "This payment method is not allowed in split payments.\n" +
						"Please choose cash, cheque, EFT POS or switch to normal payment mode.", null, "OK");
					notAllowedInSplitPayment.Show ();
					scPaymentType.SelectedSegment = -1;
					return;
				}
				else 
				{
					this._payments[0].Type = PaymentTypes.CCDetails;
					break;
				}
			case 2: 
				if (this.SplitPaymentMode == true)
				{
					var notAllowedInSplitPayment = new UIAlertView("Not allowed", "This payment method is not allowed in split payments.\n" +
						"Please choose cash, cheque, EFT POS or switch to normal payment mode.", null, "OK");
					notAllowedInSplitPayment.Show ();
					scPaymentType.SelectedSegment = -1;
					return;
				}
				else 
				{
					this._payments[0].Type = PaymentTypes.CreditCard;
					break;
				}
			case 3: { this._payments[0].Type = PaymentTypes.Cash; break; }
			case 4: { this._payments[0].Type = PaymentTypes.Cheque; break; }
			case 5: { this._payments[0].Type = PaymentTypes.EFTPOS; break; }
			default : { this._payments[0].Type = PaymentTypes.None; break; }
			}

			if (selectedJob != null)
			{
				selectedJob.Payments = this._payments; // we need to set the selected payments for the current job and all other jobs in the cluster
				if (selectedJob.HasNoParent ()) 
				{
					foreach(Job child in selectedJob.ChildJobs)
						child.Payments = this._payments;
				}
				else 
				{ // current job could be a child job, then we need to find its parent and apply the procedure above to the main and all other child jobs
					Job main = _navWorkflow._tabs._jobRunTable.FindParentJob (selectedJob);
					if (main != null)
					{
						main.Payments = selectedJob.Payments;
						foreach(Job child in main.ChildJobs)
							child.Payments = main.Payments;
					}
					else {
						// could not find parent job for one that has a parent -- something really weird
						var weird = new UIAlertView("Weird", "Could not find parent job for a child job. This is really sad...", null, "OK");
						weird.Show ();
					} 
				} // end if selected job has no parent
			} // end if selectedJob != null

			if (! this.SplitPaymentMode) // if in split payment mode, we will not display any hints and make any UI changes
			{
				switch (_payments[0].Type) 
				{
				case PaymentTypes.Cash : 
					HideAllPaymentOptions (); 
					lbTip.Text = "Please collect cash and tap \"Forward\" arrow to proceed.";
					tfTotalMoneyReceived.Text = tfToBeCollected.Text;
					HandleTfTotalMoneyReceivedEditingDidEnd (this, null);
					break;
				case PaymentTypes.Cheque : 
					ShowChequePaymentOptions ();
					HideCreditCardPaymentOptions ();
					lbTip.Text = "Please enter the cheque number into the field. When done, tap \"Forward\" arrow to proceed."; 
					tfTotalMoneyReceived.Text = tfToBeCollected.Text;
					HandleTfTotalMoneyReceivedEditingDidEnd (this, null);
					break;
				case PaymentTypes.EFTPOS : 
					HideAllPaymentOptions (); 
					lbTip.Text = "Please process the payment with the EFT POS. When done, tap \"Forward\" arrow to proceed.";
					tfTotalMoneyReceived.Text = tfToBeCollected.Text;
					HandleTfTotalMoneyReceivedEditingDidEnd (this, null);
					break;

				// in all other valid cases, money collected should be set to 0, disallowing the user to change that
				case PaymentTypes.Invoice:
					HideAllPaymentOptions ();
					lbTip.Text = "No payment to be collected, customer will be invoiced. Tap \"Forward\" arrow to proceed.";
					tfTotalMoneyReceived.Text = String.Format ("$ {0:0.00}", 0);
					HandleTfTotalMoneyReceivedEditingDidEnd (this, null);
					break;
				case PaymentTypes.CreditCard: 
					ShowCreditCardPaymentOptions (); 
					HideChequePaymentOptions (); 
					lbTip.Text = "Please enter credit card details into the fields. When done, tap \"Forward\" arrow to proceed.";
					tfTotalMoneyReceived.Text = String.Format ("$ {0:0.00}", 0);
					HandleTfTotalMoneyReceivedEditingDidEnd (this, null);
					break;
				case PaymentTypes.CCDetails:
					HideAllPaymentOptions ();
					tfPaymentType.Text = "";
					lbTip.Text = "Credit card will be drawn by accounting department. Tap \"Forward\" arrow to proceed.";
					tfTotalMoneyReceived.Text = String.Format ("$ {0:0.00}", 0);
					HandleTfTotalMoneyReceivedEditingDidEnd (this, null);
					break;

				// default
				case PaymentTypes.None :
					HideAllPaymentOptions ();
					tfPaymentType.Text = "";
					lbTip.Text = "Please specify how the customer paid for the job.";
					break;
				}	// end switch
			} // end if
			else 
			{
				// if we are in split payment mode, we should never get here
			}
		}

		/*
		void HandletfPaymentTypeTouchDown (object sender, EventArgs e)
		{
			acPaymentTypeTouchDown ();
		}

		void HandletfInvoicePOTouchDown (object sender, EventArgs e)
		{
			acInvoicePOTouchDown();
		}
		*/


		void HandleBtnAddAnotherJobClicked (object sender, EventArgs e)
		{
			acAddAnotherJob(this);
		}

		void HandlebtnSetLoyaltyClicked (object sender, EventArgs e)
		{
			acSetLoyalty(this);
		}

		void HandleBtnClearChildJobsClicked (object sender, EventArgs e)
		{
			acClearChildJobs (this);
		}

		void HandlebtnProceedClicked (object sender, EventArgs e)
		{
			acProceed (this);
		}

		void HandlebtnBackClicked (object sender, EventArgs e)
		{
			acBack (this);
		}
		
		void HandleTfTotalMoneyReceivedEditingDidEnd (object sender, EventArgs e)
		{
			double totalToReceive = CalculateMoneyToCollect ();
			double received;
			string newValue = "";
			for (int i = 0; i < tfTotalMoneyReceived.Text.Length; i++)
			{
				char c = tfTotalMoneyReceived.Text[i];
				if ( char.IsLetterOrDigit( c) || char.IsPunctuation (c) ) { newValue += c; }
			}
			
			bool ok = double.TryParse (newValue, out received);
			ok = (ok && received >= 0 && received <= totalToReceive);
			if (ok) tfTotalMoneyReceived.Text = String.Format ("$ {0:0.00}", received);
			else 
			{
				// tfTotalMoneyReceived.Text = String.Format ("$ {0:0.00}", totalToReceive); --- this was fine until the deposits thingy kicked in
				tfTotalMoneyReceived.Text = tfToBeCollected.Text;
			}

			// check if we are now receiving less than we should
			ReceivedLessMoneyThanShould ();
		}

		bool ReceivedLessMoneyThanShould()
		{
			if (tfToBeCollected != null && tfTotalMoneyReceived != null) {
				double should = double.Parse (tfToBeCollected.Text.Replace ("$", " "));
				double received = double.Parse (tfTotalMoneyReceived.Text.Replace ("$", " "));

				if (received < should) {
					return true;
				} else
					return false;
			} else
				return false;
		}

		void HandleTfToBeCollectedEditingDidEndOnExit (object sender, EventArgs e)
		{
			acAmountEditingDidEnd ();
		}

		void HandleTfChequeNumberEditingDidEndOnExit (object sender, EventArgs e)
		{
			acChequeNumberEditingDidEnd (this);
		}

		void HandleTfCreditCardNameEditingDidEndOnExit (object sender, EventArgs e)
		{
			acCreditCardNameEditingDidEnd (this);
		}

		void HandleTfCreditCardExpiryEditingDidEndOnExit (object sender, EventArgs e)
		{
			acExpiryDateEditingDidEnd (this);
		}

		[Obsolete]
		public override bool ShouldAutorotateToInterfaceOrientation (UIInterfaceOrientation toInterfaceOrientation)
		{
			// Return true for supported orientations
			return (toInterfaceOrientation == UIInterfaceOrientation.LandscapeLeft || toInterfaceOrientation == UIInterfaceOrientation.LandscapeRight);
		}
	}
}

