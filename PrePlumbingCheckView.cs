using MonoTouch.UIKit;
using System.Drawing;
using System;
using System.IO;
using MonoTouch.Foundation;
using System.Reflection;
using System.Collections.Generic;

namespace Application
{
	public partial class PrePlumbingCheckView : UIViewController
	{			
		WorkflowNavigationController _navWorkflow;
		UIActionSheet ac;
		
		float MovedViewY;
		
		public enum Choices { YesPuratap, YesNonPuratap, Yes, No, Option1, Option2 }
		public static string OutputStringForChoice(Choices c)
			{
				switch(c) {
				case Choices.Option1: return "Yes, Option 1";
				case Choices.Option2: return "Yes, Option 2";
				case Choices.YesPuratap: return "Yes, Puratap";
				case Choices.YesNonPuratap: return "Yes, non-Puratap";
				default : return c.ToString ();
				}
			}
		
		public class CheckResult {
			PrePlumbingCheckView _controller;
			
			public CheckResult(PrePlumbingCheckView controller) 
			{ 
				_controller = controller; 
				LeakingFittings = Choices.No;
				LeakingTap = Choices.No;
				PotentialLeak = Choices.No; 
				OldTubing = Choices.No; 
				NonPuratapComponents = Choices.No; 
				ExistingDamage = Choices.No;
				UpgradeOffered = Choices.No;
				CustomerAcceptedUpgrade = Choices.No;
				OfficeFollowUpRequired = Choices.No;
				UnwillingToSign = Choices.No;
				NotAPuratapProblem = Choices.No;
			}
			
			private Choices _leakingFittings;
			private Choices _leakingTap;
			private Choices _potentialLeak;
			private Choices _oldTubing;
			private Choices _nonPuratapComponents;
			private Choices _existingDamage;
			private Choices _upgradeOffered;
			private Choices _customerAcceptedUpgrade;
			private Choices _officeFollowUpRequired;
			private Choices _unwillingToSign;
			private Choices _notAPuratapProblem;
			
			public Choices LeakingFittings 
			{	get { return _leakingFittings; } 
				set { _leakingFittings = value; _controller.SetTextFieldValue ("leakFittings", OutputStringForChoice (_leakingFittings)); } }
			
			public Choices LeakingTap
			{	get { return _leakingTap; }
				set { _leakingTap = value; _controller.SetTextFieldValue ("leakTap", OutputStringForChoice (_leakingTap)); } }
			
			public Choices PotentialLeak
			{	get { return _potentialLeak; } 
				set { _potentialLeak = value; _controller.SetTextFieldValue ("leakPotential", OutputStringForChoice (_potentialLeak)); } }
			public Choices OldTubing
			{	get { return _oldTubing; } 
				set { _oldTubing = value; _controller.SetTextFieldValue ("oldTubing", OutputStringForChoice (_oldTubing)); } }
			public Choices NonPuratapComponents
			{	get { return _nonPuratapComponents; } 
				set { _nonPuratapComponents = value; _controller.SetTextFieldValue ("nonPuratapComponents", OutputStringForChoice (_nonPuratapComponents)); } }
			public Choices ExistingDamage
			{	get { return _existingDamage; } 
				set { _existingDamage = value; _controller.SetTextFieldValue ("existingDamage", OutputStringForChoice (_existingDamage)); } }
			public Choices UpgradeOffered
			{	get { return _upgradeOffered; } 
				set { _upgradeOffered = value; _controller.SetTextFieldValue ("upgradeOffered", OutputStringForChoice (_upgradeOffered)); } }
			public Choices CustomerAcceptedUpgrade
			{	get { return _customerAcceptedUpgrade; } 
				set { _customerAcceptedUpgrade = value; _controller.SetTextFieldValue ("customerAcceptedUpgrade", OutputStringForChoice (_customerAcceptedUpgrade)); } }
			public Choices OfficeFollowUpRequired
			{	get { return _officeFollowUpRequired; } 
				set { _officeFollowUpRequired = value; _controller.SetTextFieldValue ("officeFollowUpRequired", OutputStringForChoice (_officeFollowUpRequired)); } }
			public Choices UnwillingToSign
			{	get { return _unwillingToSign; } 
				set { _unwillingToSign = value; _controller.SetTextFieldValue ("unwillingToSign", OutputStringForChoice (_unwillingToSign)); } }
			public Choices NotAPuratapProblem
			{	get { return _notAPuratapProblem; } 
				set { _notAPuratapProblem = value; _controller.SetTextFieldValue ("notAPuratapProblem", OutputStringForChoice (_notAPuratapProblem)); } }
			
		}
		
		public void SetTextFieldValue(string fieldName, string newText)
		{ this.GetType().GetMethod ("SetText_"+fieldName).Invoke (this, new object[] {newText} );	}
		
		public void SetText_leakFittings(string text)
		{ this.leakFittingsTextField.Text = text; }
		
		public void SetText_leakTap(string text)
		{ this.leakTapTextField.Text = text; }
		
		public void SetText_leakPotential(string text)
		{ this.leakPotentialTextField.Text = text; }
		
		public void SetText_oldTubing(string text)
		{ this.oldTubingTextField.Text = text; }
		
		public void SetText_nonPuratapComponents(string text)
		{ this.nonPuratapTextField.Text = text; }
		
		public void SetText_existingDamage(string text)
		{ this.existingDamageTextField.Text = text; }
		
		public void SetText_upgradeOffered(string text)
		{ this.upgradeOfferedTextfield.Text = text; }
		
		public void SetText_customerAcceptedUpgrade(string text)
		{ this.customerAcceptedTextField.Text = text; }
		
		public void SetText_officeFollowUpRequired(string text)
		{ this.officeFollowupTextField.Text = text; }
		
		public void SetText_unwillingToSign(string text)
		{ this.UnwillingToSignTextField.Text = text; }
		
		public void SetText_notAPuratapProblem(string text)
		{ this.notPuratapProblemTextField.Text = text; }
		
		
		public CheckResult pr;
		
		public void ResetChoices() {
			if (pr != null) {
				pr.LeakingFittings = Choices.No;
				pr.LeakingTap = Choices.No;
				pr.PotentialLeak = Choices.No; 
				pr.OldTubing = Choices.No; 
				pr.NonPuratapComponents = Choices.No; 
				pr.ExistingDamage = Choices.No;
				pr.UpgradeOffered = Choices.No;
				pr.CustomerAcceptedUpgrade = Choices.No;
				pr.OfficeFollowUpRequired = Choices.No;
				pr.UnwillingToSign = Choices.No;
				pr.NotAPuratapProblem = Choices.No;
			}
			
			if (commentsTextView != null) commentsTextView.Text = "";
		}
		
		public bool IsDefault()
		{
			return CheckResultIsDefault (pr);
		}
			
		public bool CheckResultIsDefault(CheckResult cr)
		{
			bool result = true;
			
			if (cr.LeakingFittings != Choices.No || 
			    cr.LeakingTap != Choices.No || 
			    cr.PotentialLeak != Choices.No || 
			    cr.OldTubing != Choices.No || 
			    cr.NonPuratapComponents != Choices.No || 
			    cr.ExistingDamage != Choices.No ||
			    cr.UpgradeOffered != Choices.No ||
			    cr.CustomerAcceptedUpgrade != Choices.No ||
			    cr.OfficeFollowUpRequired != Choices.No ||
			    cr.NotAPuratapProblem != Choices.No) 
					result = false;
			
			return result;
		}
		
		UIAlertView _jobNotDone; 		// an alert view dialog used when asking if a job was started at all
		
		private UIView _generatedPdfView;
		public UIView GeneratedPDFView { get { return _generatedPdfView; } set { _generatedPdfView = value; } }
		public string pdfPrePlumbingFileName { get; set; }
		
		public PrePlumbingCheckView (WorkflowNavigationController workflow) : base ("PrePlumbingCheckView", null)
		{
			this.Title = NSBundle.MainBundle.LocalizedString ("Pre-plumbing check", "Pre-plumbing check");
			using (var image = UIImage.FromBundle ("Images/117-todo") ) this.TabBarItem.Image = image;
			this._navWorkflow = workflow;
			
			this.ToolbarItems = new UIBarButtonItem[] { new UIBarButtonItem("Done", UIBarButtonItemStyle.Done, null) };
		}
		
		public void JobWasNotDoneClicked()
		{
			if (_navWorkflow._tabs._jobRunTable.CurrentJob.Started == MyConstants.JobStarted.None) 
			{
				if (_jobNotDone == null) // if this is the first time, we create the dialog
				{
					_jobNotDone = new UIAlertView("", "", null, "Cancel",
				                               "Customer not at home",
				                               "Customer to be rebooked",
				                               "I wasn't there in time",
				                              "The address was wrong",
					                              "Other");
					_jobNotDone.Dismissed += delegate(object _sender, UIButtonEventArgs e) 
					{
						bool reload = true;
						Job selectedJob = _navWorkflow._tabs._jobRunTable.CurrentJob;
						switch (e.ButtonIndex) {
							// IMPLEMENTED :: added an object to the customer that holds the result of this dialog
							// IMPLEMENTED :: set up job results according to user choice here
							case 0: { // user pressed "Cancel"
								reload = false;
								break;
							}
							/*
							case 1: { // answered "Not at home" -- act accordingly (job is to be rebooked)
								_navWorkflow._tabs._jobService.SaveFollowupToDatabase (selectedJob.JobBookingNumber, 10, "Job not done: Not home");
								selectedJob.Started = MyConstants.JobStarted.CustomerNotAtHome;
								selectedJob.ShouldPayFee = false;
								_navWorkflow.SaveJobResultsToDatabase (selectedJob, false);
								_navWorkflow.ResetWorkflow(this, null);
								break; 
							}
							case 2: { // answered "Rebooked the job" -- act accordingly (job has been rebooked, create a followup just to be safe)
								_navWorkflow._tabs._jobService.SaveFollowupToDatabase (selectedJob.JobBookingNumber, 10, "Job not done: Customer rebooked");
								selectedJob.Started = MyConstants.JobStarted.CustomerRebooked;
								selectedJob.ShouldPayFee = false;
								_navWorkflow.SaveJobResultsToDatabase (selectedJob, false);
								_navWorkflow.ResetWorkflow(this, null);
								break; 
							}
							case 3: { // answered "I was late" -- act accordingly (job is to be rebooked)
								_navWorkflow._tabs._jobService.SaveFollowupToDatabase (selectedJob.JobBookingNumber, 10, "Job not done: Late");
								selectedJob.Started = MyConstants.JobStarted.PuratapLate;
								selectedJob.ShouldPayFee = false;
								_navWorkflow.SaveJobResultsToDatabase (selectedJob, false);
								_navWorkflow.ResetWorkflow(this, null);
								break; 
							}
							case 4: { // answered "Address was wrong" -- act accordingly (job is to be rebooked)
								_navWorkflow._tabs._jobService.SaveFollowupToDatabase (selectedJob.JobBookingNumber, 10, "Job not done: Wrong address");
								selectedJob.Started = MyConstants.JobStarted.AddressWrong;
								selectedJob.ShouldPayFee = false;
								_navWorkflow.SaveJobResultsToDatabase (selectedJob, false);
								_navWorkflow.ResetWorkflow(this, null);
								break;
							} */
							default: 
							{ 	
								// all the answers
								string chosenOption = "";
								var alert = new UIAlertView("", "Please enter a comment", null, "Cancel", "OK"); 	// NotDoneCommentAlert();
								alert.AlertViewStyle = UIAlertViewStyle.PlainTextInput;
								alert.Dismissed += delegate(object sender, UIButtonEventArgs ea) 
								{
									if (ea.ButtonIndex != alert.CancelButtonIndex)
									{
										// save the text that the user has entered (in FOLLOWUPS)
										if (selectedJob.HasParent())
										{
											Job main = _navWorkflow._tabs._jobRunTable.FindParentJob (selectedJob);
											selectedJob = main;
										}

										_navWorkflow._tabs._jobService.SaveFollowupToDatabase (selectedJob.JobBookingNumber, 10, String.Format ("Job not done: {0}: {1}", chosenOption, alert.GetTextField (0).Text));
										selectedJob.ShouldPayFee = false;
										selectedJob.JobDone = true;
										selectedJob.ChildJobs.Clear ();
										selectedJob.UsedParts.Clear ();
										_navWorkflow._tabs._jobRunTable.CurrentJob = selectedJob;
										_navWorkflow._finishWorkflow(this, null);

										// _navWorkflow.SaveJobResultsToDatabase (selectedJob, false);
										// _navWorkflow.ResetWorkflow(this, null);
										_navWorkflow._tabs._jobRunTable.TableView.ReloadRows ( new NSIndexPath[] { _navWorkflow._tabs._jobRunTable.LastSelectedRowPath }, UITableViewRowAnimation.Automatic );
									}
								};


								// alert.Show ();
								
								switch (e.ButtonIndex)
								{
								case 1:	chosenOption = "Not home"; selectedJob.Started = MyConstants.JobStarted.CustomerNotAtHome; break;
								case 2:	chosenOption = "To be rebooked"; selectedJob.Started = MyConstants.JobStarted.CustomerRebooked; break;
								case 3:	chosenOption = "Late"; selectedJob.Started = MyConstants.JobStarted.PuratapLate; break;
								case 4:	chosenOption = "Wrong address"; selectedJob.Started = MyConstants.JobStarted.AddressWrong; break;
								case 5:	chosenOption = "Other"; selectedJob.Started = MyConstants.JobStarted.Other; break;
								default:	chosenOption = "Unknown?"; break;
								}

								var test = new NotDoneCommentViewController(this, chosenOption);
								this.NavigationController.PushViewController (test, true);

								break;								
							}
						}
						if (reload) {
							_navWorkflow._tabs._jobRunTable.TableView.ReloadData ();
							_navWorkflow._tabs._jobRunTable.TableView.SelectRow(_navWorkflow._tabs._jobRunTable.LastSelectedRowPath, true, UITableViewScrollPosition.None);
							
							// check if all jobs have been done
							bool allDone = true;
							foreach (Job j in _navWorkflow._tabs._jobRunTable.MainJobList)
							{
								if (j.JobDone == false) allDone = false;
							}
							// all jobs have been done, grats
							if (allDone) _navWorkflow._tabs._jobRunTable.AllJobsDone = true;
						}
					};
				}
			}
			// show the dialog allowing the user to pick a reason for not having done the job
			_jobNotDone.Show ();
		}

		public void SaveInfo_JobNotDone(string notDoneReason, string notDoneComment)
		{
			Job selectedJob = _navWorkflow._tabs._jobRunTable.CurrentJob;

			if (selectedJob.HasParent())
			{
				Job main = _navWorkflow._tabs._jobRunTable.FindParentJob (selectedJob);
				selectedJob = main;
			}
			
			// save the text that the user has entered (in FOLLOWUPS)

			_navWorkflow._tabs._jobService.SaveFollowupToDatabase (selectedJob.JobBookingNumber, 10, String.Format ("Job not done: {0}: {1}", notDoneReason, notDoneComment));
			selectedJob.ShouldPayFee = false;
			selectedJob.JobDone = true;
			selectedJob.ChildJobs.Clear ();
			selectedJob.UsedParts.Clear ();
			_navWorkflow._tabs._jobRunTable.CurrentJob = selectedJob;
			_navWorkflow._finishWorkflow(this, null);
			_navWorkflow._tabs._jobRunTable.TableView.ReloadRows ( new NSIndexPath[] { _navWorkflow._tabs._jobRunTable.LastSelectedRowPath }, UITableViewRowAnimation.Automatic );
		}
		
		public void ProceedToSign()
		{
			if ( (pr.ExistingDamage == Choices.Yes || pr.NotAPuratapProblem == Choices.Yes) && (commentsTextView.Text == "") )
			// there is existing damage OR a problem not related to Puratap AND the guy did not enter any comments
			{
				string msg = "";
				if (pr.ExistingDamage == Choices.Yes) msg += "about pre-existing property damage ";
				if (pr.NotAPuratapProblem == Choices.Yes)
				{
					if (pr.ExistingDamage == Choices.Yes) msg += "and problems not related to Puratap";
					if (pr.ExistingDamage == Choices.No) msg += "about problems not related to Puratap";
				}
				using(var alert = new UIAlertView("Please, enter some comments", msg, null, "OK", null))
					{
						alert.Show();
						return;
					}
			}
			else {
				GeneratePrePlumbingPDFpreview();		// creates a pdf preview file here (unsigned pdf)
				// _navWorkflow._tabs._signView.Mode = SignableDocuments.PrePlumbingCheck;
				// _navWorkflow.PushViewController (_navWorkflow._tabs._signView, true);
				RedrawPrePlumbingPDF(false, false);	// draws a pdf in the signature capturing view controller
			}			
		}
		
		partial void acLeaveWorkflow (NSObject sender)
		{
			var alert = new UIAlertView("Warning", "This will reset the workflow for all jobs for selected customer. Are you sure?", null, "No, never mind", "Yes");
			alert.Dismissed += delegate(object ssender, UIButtonEventArgs e) {
				if (e.ButtonIndex != alert.CancelButtonIndex)
				{
					_navWorkflow.ResetWorkflow(this, null);					
				}
			};
			alert.Show ();
		}
		
		partial void acProceed (NSObject sender)
		{
			string currentJobCode = _navWorkflow._tabs._jobRunTable.CurrentJob.Type.Code;
			if (IsDefault () && currentJobCode != "DLV")
			{
				// show a modal view controller for the user to acknowledge that he has indeed done the pre-plumbing check
				var prePlumbingAcknowledge = new UIAlertView("ATTENTION!", 
				                                             "You are declaring that there are no pre-existing issues that should be reported and require customer's attention.\n", null, "I'll look again", "I am sure"); 
															// "Be mindful that you will have to pay the potential claim costs."
				prePlumbingAcknowledge.Dismissed += delegate(object _sender, UIButtonEventArgs e) 
				{
					if (e.ButtonIndex != prePlumbingAcknowledge.CancelButtonIndex)
					{
						if (_navWorkflow._tabs._jobRunTable.CurrentJob.Type.Code == "UNK") 
						{
							var alert = new UIAlertView("Job type unknown", "Please change job type", null, "OK");
							alert.Show ();
						}
						else
						{
							GeneratePrePlumbingPDFpreview();
							RedrawPrePlumbingPDF (false, false);
							_navWorkflow.ProceedToBookedJobType (this, null);
						}
					}
				};
				prePlumbingAcknowledge.Show ();
			}
			else 
			{
				if ( (pr.ExistingDamage == Choices.Yes || pr.NotAPuratapProblem == Choices.Yes) && (commentsTextView.Text == "") )
					// there is existing damage OR a problem not related to Puratap AND the guy did not enter any comments
				{
					string msg = "";
					if (pr.ExistingDamage == Choices.Yes) msg += "about pre-existing property damage ";
					if (pr.NotAPuratapProblem == Choices.Yes)
					{
						if (pr.ExistingDamage == Choices.Yes) msg += "and problems not related to Puratap";
						if (pr.ExistingDamage == Choices.No) msg += "about problems not related to Puratap";
					}
					using(var alert = new UIAlertView("Please, enter some comments", msg, null, "OK", null))
					{
						alert.Show();
						return;
					}
				}
				else {
					if (_navWorkflow._tabs._jobRunTable.CurrentJob.Type.Code == "UNK") 
					{
						var alert = new UIAlertView("Job type unknown", "Please change job type", null, "OK");
						alert.Show ();
					}
					else
					{
						GeneratePrePlumbingPDFpreview();
						RedrawPrePlumbingPDF (false, false);
						_navWorkflow.ProceedToBookedJobType (this, null);
					}
				}				
			}
		}
		
		partial void btnNotDoneClicked (NSObject sender)
		{
			JobWasNotDoneClicked ();
		}
		
		partial void btnChangeJobTypeClicked (NSObject sender)
		{
			_navWorkflow.ChangeJobType (this, null);
		}
		
		public void SetJobTypeText(string text)
		{
			lbJobType.Text = "to change this job's type ("+text+")";
		}
		 
		public void GeneratePrePlumbingPDFpreview()
		{
			Customer c = _navWorkflow._tabs._jobRunTable.CurrentCustomer;
			
			NSArray a = NSBundle.MainBundle.LoadNib ("PrePlumbingPDFView", this, null);
			_generatedPdfView = (UIView)MonoTouch.ObjCRuntime.Runtime.GetNSObject (a.ValueAt (0));
			
			UIImageView imgv = (UIImageView)_generatedPdfView.ViewWithTag (MyConstants.PrePlumbingPDFTemplateTags.PuratapLogo);
			using (var image = UIImage.FromBundle ("/Images/puratap-logo") ) imgv.Image = image;
			
			UILabel tl = (UILabel)_generatedPdfView.ViewWithTag (MyConstants.PrePlumbingPDFTemplateTags.CustomerNumber);
			tl.Text = "Customer Number: "+c.CustomerNumber;
			tl = (UILabel)_generatedPdfView.ViewWithTag (MyConstants.PrePlumbingPDFTemplateTags.PuratapEmployeeName);
			tl.Text = MyConstants.EmployeeName; // "Puratap representative: "
			tl = (UILabel)_generatedPdfView.ViewWithTag (MyConstants.PrePlumbingPDFTemplateTags.Date);
			tl.Text = "Date: " + DateTime.Now.Date.ToString ("dd/MM/yyyy");
			tl = (UILabel)_generatedPdfView.ViewWithTag (MyConstants.PrePlumbingPDFTemplateTags.CustomerName);
			tl.Text = "Customer Name: " + String.Format ("{0} {1} {2}", c.Title, c.FirstName, c.LastName);
			
			tl = (UILabel)_generatedPdfView.ViewWithTag (MyConstants.PrePlumbingPDFTemplateTags.LeakingBrassFittings);
			switch (pr.LeakingFittings)
			{
				case Choices.YesPuratap: { tl.Text = "Leaking brass fittings (Puratap)"; tl.Enabled = true; break; }
				case Choices.YesNonPuratap: { tl.Text = "Leaking brass fittings (non-Puratap)"; tl.Enabled = true; break; }
				case Choices.No: { tl.Text = "No leaking brass fittings"; tl.Enabled = false; break; }
			}
			tl = (UILabel)_generatedPdfView.ViewWithTag (MyConstants.PrePlumbingPDFTemplateTags.LeakingTap);
			switch (pr.LeakingTap)
			{
				case Choices.YesPuratap: { tl.Text = "Leaking tap (Puratap)"; tl.Enabled = true; break; }
				case Choices.YesNonPuratap: { tl.Text = "Leaking tap (non-Puratap)"; tl.Enabled = true; break; }
				case Choices.No: { tl.Text = "No leaking tap"; tl.Enabled = false; break; }
			}
			tl = (UILabel)_generatedPdfView.ViewWithTag (MyConstants.PrePlumbingPDFTemplateTags.PotentialLeak);
			switch (pr.PotentialLeak)
			{
				case Choices.YesPuratap: { tl.Text = "Potential leak (discolouration of Puratap ball valve)"; tl.Enabled = true; break; }
				case Choices.YesNonPuratap: { tl.Text = "Potential leak (discolouration of Non-Puratap ball valve)"; tl.Enabled = true; break; }
				case Choices.No: { tl.Text = "No discolouration of ball valve found"; tl.Enabled = false; break; }
			}
			tl = (UILabel)_generatedPdfView.ViewWithTag (MyConstants.PrePlumbingPDFTemplateTags.OldTubing);
			switch (pr.OldTubing)
			{
				case Choices.Yes: { tl.Text = "Old \"industry stanard\" thin-walled tubing (needs an upgrade)"; tl.Enabled = true; break; }
				case Choices.No: { tl.Text = "Tubing not discoloured and not showing visual signs of wear"; tl.Enabled = false; break; }
			}
			tl = (UILabel)_generatedPdfView.ViewWithTag (MyConstants.PrePlumbingPDFTemplateTags.NonPuratapComponents);
			switch (pr.NonPuratapComponents)
			{
				case Choices.Yes: { tl.Text = "Non-Puratap plumbing components used"; tl.Enabled = true; break; }
				case Choices.No: { tl.Text = "No non-Puratap components "; tl.Enabled = false; break; }
			}
			tl = (UILabel)_generatedPdfView.ViewWithTag (MyConstants.PrePlumbingPDFTemplateTags.ExistingDamage);
			switch (pr.ExistingDamage)
			{
				case Choices.Yes: { tl.Text = "Existing property damage (see comments for details)"; tl.Enabled = true; break; }
				case Choices.No: { tl.Text = "No pre-existing property damage"; tl.Enabled = false; break; }
			}
			tl = (UILabel)_generatedPdfView.ViewWithTag (MyConstants.PrePlumbingPDFTemplateTags.NotPuratapProblem);
			switch (pr.NotAPuratapProblem)
			{
				case Choices.Yes: { tl.Text = "There is a problem not related to Puratap (see comments for details)"; tl.Enabled = true; break; }
				case Choices.No: { tl.Text = "No other problems (not related to Puratap)"; tl.Enabled = false; break; }
			}
			tl = (UILabel)_generatedPdfView.ViewWithTag (MyConstants.PrePlumbingPDFTemplateTags.CustomerAcceptedUpgrade);
			switch (pr.UpgradeOffered)
			{
				case Choices.Option1: { 
					if (pr.CustomerAcceptedUpgrade == Choices.Yes) tl.Text = "Customer has accepted an offer of Option 1 upgrade";
					if (pr.CustomerAcceptedUpgrade == Choices.No)  tl.Text = "Customer has declined an offer of Option 1 upgrade"; 
					break; }
				case Choices.Option2: { 
					if (pr.CustomerAcceptedUpgrade == Choices.Yes) tl.Text = "Customer has accepted an offer of Option 2 upgrade";
					if (pr.CustomerAcceptedUpgrade == Choices.No)  tl.Text = "Customer has declined an offer of Option 2 upgrade"; 
					break; }
				case Choices.No: { 
					tl.Hidden = true;
					tl = (UILabel)_generatedPdfView.ViewWithTag (MyConstants.PrePlumbingPDFTemplateTags.UpgradeOfferText);
					tl.Hidden = true;
					break; }
			}
			tl = (UILabel)_generatedPdfView.ViewWithTag (MyConstants.PrePlumbingPDFTemplateTags.CustomerSignatureLabel);
			switch (pr.UnwillingToSign)
			{
				case Choices.No: { tl.Text = "Customer signature"; break; }
				case Choices.Yes: { tl.Text = "Customer unable or unwilling to sign off. Signed by service representative"; break; }
			}
			tl = (UILabel)_generatedPdfView.ViewWithTag (MyConstants.PrePlumbingPDFTemplateTags.OfficeFollowUpRequired);
			switch (pr.OfficeFollowUpRequired)
			{
				case Choices.Yes : { tl.Hidden = false; break; }
				case Choices.No: { tl.Hidden = true; break; }
			}
			
			UITextView comments = (UITextView)_generatedPdfView.ViewWithTag (MyConstants.PrePlumbingPDFTemplateTags.Comments);
			comments.Text = this.commentsTextView.Text;

			tl = (UILabel)_generatedPdfView.ViewWithTag (MyConstants.PrePlumbingPDFTemplateTags.TubingUpgradeNotice);
			if (pr.UpgradeOffered != Choices.No)
			{
				if (pr.CustomerAcceptedUpgrade != Choices.No)
				{
					// upgrade was offered and customer accepted
					Job main = _navWorkflow._tabs._jobRunTable.CurrentJob;
					if (main.HasParent())
						main = _navWorkflow._tabs._jobRunTable.FindParentJob (_navWorkflow._tabs._jobRunTable.CurrentJob);
					bool tuInJobCluster = false;
					if ((main.Type.Code == "TUBINGUPGR") && (main.Warranty == false))
						tuInJobCluster = true;
					else
						foreach (Job child in main.ChildJobs)
							if ((child.Type.Code == "TUBINGUPGR") && (child.Warranty == false))
						{	tuInJobCluster = true; break; }

					if (tuInJobCluster)
						tl.Text = "Tubing upgrade was offered, accepted and done on the day";
					else tl.Text = "Tubing upgrade was offered, accepted, but NOT DONE on the day.";	// todo :: create a followup here?

				}
				else
				{
					tl.Hidden = true;
				}
			}
			else tl.Hidden = true;
			
			tl.Dispose(); tl = null;
			comments.Dispose (); comments = null;
			a.Dispose (); a = null;
			imgv.Dispose (); imgv = null;
		}
		
		public void RedrawPrePlumbingPDF(bool ThereAndBack, bool DocumentSigned)
		{
			if (ThereAndBack) _navWorkflow.PopToRootViewController (false);
			// render created preview in PDF context
			NSMutableData pdfData = new NSMutableData();
			UIGraphics.BeginPDFContext (pdfData, _generatedPdfView.Bounds, null);
			UIGraphics.BeginPDFPage ();
			_generatedPdfView.Layer.RenderInContext (UIGraphics.GetCurrentContext ());
			UIGraphics.EndPDFContent ();
			// save the rendered context to disk
			NSError err;
			string pdfFileName;

			string pdfID = _navWorkflow._tabs._jobRunTable.CurrentCustomer.CustomerNumber.ToString () + "_PrePlumbingPDF";
			if (DocumentSigned)	{ pdfFileName = Path.Combine (Environment.GetFolderPath (Environment.SpecialFolder.Personal), pdfID+"_Signed.pdf"); }
			else pdfFileName = Path.Combine (Environment.GetFolderPath (Environment.SpecialFolder.Personal), pdfID+"_Not_Signed.pdf");
			
			pdfData.Save (pdfFileName, true, out err);
			err = null; pdfData = null;
			
			// load the content into Signing view controller
			
			_navWorkflow._tabs.SignPre.PDFView.MultipleTouchEnabled = true;
			_navWorkflow._tabs.SignPre.PDFView.ScalesPageToFit = true;
			_navWorkflow._tabs.SignPre.PDFView.LoadRequest (new NSUrlRequest( NSUrl.FromFilename (pdfFileName)));
			pdfPrePlumbingFileName = pdfFileName;
			// if (ThereAndBack) _navWorkflow.PushViewController (_navWorkflow._tabs._signView, true);
		}
		
		partial void leakFittingsTouchDown (NSObject sender)
		{
			ac = new UIActionSheet("Are the brass fittings leaking?", null, "Cancel", null, "Yes (Own)", "Yes (Puratap)",  "No");
			ac.WillDismiss += delegate(object _sender, UIButtonEventArgs e) {
				const int i = 3; // ac.CancelButtonIndex;
				switch (e.ButtonIndex)
				{
					case i: { /* leakFittingsTextField.Text = "?";*/ break; }
					case i-3: { pr.LeakingFittings = Choices.YesNonPuratap; break; }
					case i-2: { pr.LeakingFittings = Choices.YesPuratap; break; }
					case i-1: { pr.LeakingFittings = Choices.No; break; }
				}
			};
			
			ac.ShowInView (this.View);
		}
		
		partial void leakTapTouchDown (NSObject sender)
		{
			ac = new UIActionSheet("Is the tap leaking?", null, "Cancel", null, "Yes (Own)", "Yes (Puratap)",  "No");
			ac.WillDismiss += delegate(object _sender, UIButtonEventArgs e) {
				const int i = 3; // ac.CancelButtonIndex;
				switch (e.ButtonIndex)
				{
					case i: { /* leakTapTextField.Text = "?";*/ break; }
					case i-3: { pr.LeakingTap = Choices.YesNonPuratap; break; }
					case i-2: { pr.LeakingTap = Choices.YesPuratap; break; }
					case i-1: { pr.LeakingTap = Choices.No; break; }
				}
			};
			ac.ShowInView (this.View);			
		}
		
		partial void leakPotentialTouchDown (NSObject sender)
		{
			ac = new UIActionSheet("Is there a potential leak (discolouration of Puratap ball valve)?", null, "Cancel", null, "Yes (Puratap ball valve)", "Yes (Non-Puratap ball valve)", "No");
			ac.WillDismiss += delegate(object _sender, UIButtonEventArgs e) {
				const int i = 3; // ac.CancelButtonIndex;
				switch (e.ButtonIndex)
				{
					case i: { /* leakPotentialTextField.Text = "?";*/ return; }
					case i-3: { leakPotentialTextField.Text = "Yes (Puratap ball valve)"; pr.PotentialLeak = Choices.YesPuratap; return; }
					case i-2: { leakPotentialTextField.Text = "Yes (Non-Puratap ball valve)"; pr.PotentialLeak = Choices.YesNonPuratap; return; }
					case i-1: { leakPotentialTextField.Text = "No"; pr.PotentialLeak = Choices.No; return; }
				}
			};
			ac.ShowInView (this.View);			
		}
		
		partial void oldTubingTouchDown (NSObject sender)
		{
			ac = new UIActionSheet("Is there an old \"industry standard\" thin-walled tubing?", null, "Cancel", null, "Yes (needs an upgrade)", "No");
			ac.WillDismiss += delegate(object _sender, UIButtonEventArgs e) {
				const int i = 2; // ac.CancelButtonIndex;
				switch (e.ButtonIndex)
				{
					case i: { /* oldTubingTextField.Text = "?";*/ return; }
					case i-2: { oldTubingTextField.Text = "Yes (needs an upgrade)"; pr.OldTubing = Choices.Yes; return; }
					case i-1: { oldTubingTextField.Text = "No"; pr.OldTubing = Choices.No; return; }
				}
			};
			ac.ShowInView (this.View);
		}
		
		partial void nonPuratapTouchDown (NSObject sender)
		{
			ac = new UIActionSheet("Are there any non-Puratap components?", null, "Cancel", null, "Yes", "No");
			ac.WillDismiss += delegate(object _sender, UIButtonEventArgs e) {
				const int i = 2; // ac.CancelButtonIndex;
				switch (e.ButtonIndex)
				{
					case i: { /* nonPuratapTextField.Text = "?";*/ return; }
					case i-2: { nonPuratapTextField.Text = "Yes"; pr.NonPuratapComponents = Choices.Yes; return; }
					case i-1: { nonPuratapTextField.Text = "No"; pr.NonPuratapComponents = Choices.No; return; }
				}
			};
			ac.ShowInView (this.View);			
		}
		
		partial void exisitingDamageTouchDown (NSObject sender)
		{
			ac = new UIActionSheet("Is there any existing property damage?\n(If yes, please describe it in comments.)", null, "Cancel", null, "Yes", "No");
			ac.WillDismiss += delegate(object _sender, UIButtonEventArgs e) {
				const int i = 2; // ac.CancelButtonIndex;
				switch (e.ButtonIndex)
				{
					case i: { /* existingDamageTextField.Text = "?";*/ return; }
					case i-2: { pr.ExistingDamage = Choices.Yes; existingDamageTextField.Text = "Yes (describe in comments)"; return; }
					case i-1: { existingDamageTextField.Text = "No"; pr.ExistingDamage = Choices.No; return; }
				}
			};
			ac.ShowInView (this.View);
		}
		
		partial void NotPuratapProblemTouchDown (NSObject sender)
		{
			ac = new UIActionSheet("Is there a problem not related tp Puratap?\n(If yes, please describe it in comments.)", null, "Cancel", null, "Yes", "No");
			ac.WillDismiss += delegate(object _sender, UIButtonEventArgs e) {
				const int i = 2; // ac.CancelButtonIndex;
				switch (e.ButtonIndex)
				{
					case i: { /* existingDamageTextField.Text = "?";*/ return; }
					case i-2: { pr.NotAPuratapProblem = Choices.Yes; notPuratapProblemTextField.Text = "Yes (describe in comments)"; return; }
					case i-1: { notPuratapProblemTextField.Text = "No"; pr.NotAPuratapProblem = Choices.No; return; }
				}
			};
			ac.ShowInView (this.View);			
		}
		
		partial void upgradeOfferedTouchDown (NSObject sender)
		{
			ac = new UIActionSheet("What was the upgrade offer?", null, "Cancel", null, "Option 1 upgrade", "Option 2 upgrade",  "No offer made");
			ac.WillDismiss += delegate(object _sender, UIButtonEventArgs e) {
				const int i = 3; // ac.CancelButtonIndex;
				switch (e.ButtonIndex)
				{
					case i: { /* upgradeOfferedTextfield.Text = "?";*/ return; }
					case i-3: { upgradeOfferedTextfield.Text = "Option 1"; pr.UpgradeOffered = Choices.Option1; return; }
					case i-2: { upgradeOfferedTextfield.Text = "Option 2"; pr.UpgradeOffered = Choices.Option2; return; }
					case i-1: { upgradeOfferedTextfield.Text = "No offer made"; pr.UpgradeOffered = Choices.No; return; }
				}
			};
			ac.ShowInView (this.View);
		}
		
		partial void customerAcceptedTouchDown (NSObject sender)
		{
			ac = new UIActionSheet("Did the customer accept the upgrade offer?", null, "Cancel", null, "Yes", "No");
			ac.WillDismiss += delegate(object _sender, UIButtonEventArgs e) {
				const int i = 2; // ac.CancelButtonIndex;
				switch (e.ButtonIndex)
				{
					case i: { /* customerAcceptedTextField.Text = "?";*/ return; }
					case i-2: { customerAcceptedTextField.Text = "Yes"; pr.CustomerAcceptedUpgrade = Choices.Yes; return; }
					case i-1: { customerAcceptedTextField.Text = "No"; pr.CustomerAcceptedUpgrade = Choices.No; return; }
				}
			};
			ac.ShowInView (this.View);			
		}
		
		partial void officeFollowupTouchDown (NSObject sender)
		{
			ac = new UIActionSheet("Is office follow-up required for this?", null, "Cancel", null, "Yes", "No");
			ac.WillDismiss += delegate(object _sender, UIButtonEventArgs e) {
				const int i = 2; // ac.CancelButtonIndex;
				switch (e.ButtonIndex)
				{
				case i: { /* officeFollowupTextField.Text = "?";*/ break; }
				case i-2: { 
					officeFollowupTextField.Text = "Yes"; 
					pr.OfficeFollowUpRequired = Choices.Yes; 

					Dictionary<int, string> Reasons = MyConstants.GetFollowUpReasonsFromDB();
					UIActionSheet act = new UIActionSheet("Please specify a reason");
					foreach(int j in Reasons.Keys) act.AddButton (Reasons[j]);
					act.WillDismiss += delegate(object __sender, UIButtonEventArgs ee) 
					{
						if (ee.ButtonIndex!=-1)
						{
							// IMPLEMENTED :: saves the followup reason to database
							string pickedReason = ((UIActionSheet)__sender).ButtonTitle (ee.ButtonIndex); // Reasons[ee.ButtonIndex+1];							
							int reasonID = Reasons.FindKeyByValue (pickedReason);
							long jobID = _navWorkflow._tabs._jobRunTable.CurrentJob.JobBookingNumber;

							if (act.ButtonTitle (ee.ButtonIndex).ToUpper().Contains ("OTHER") || act.ButtonTitle (ee.ButtonIndex).ToUpper().Contains ("TECHNICAL ISSUES"))
							{
								// display an additional dialog to get a description of what happened
								var getDescription = new UIAlertView("Comment", "Type in a few words about why this needs to be followed up", null, "Cancel", "OK");
								getDescription.AlertViewStyle = UIAlertViewStyle.PlainTextInput;
								getDescription.Dismissed += delegate(object desc_sender, UIButtonEventArgs btn) {
									if (btn.ButtonIndex != getDescription.CancelButtonIndex)
									{
										string desc = getDescription.GetTextField (0).Text;
										_navWorkflow._tabs._jobService.SaveFollowupToDatabase(jobID, reasonID, desc);
									}
								};
								getDescription.Show ();
							}
							else {
								_navWorkflow._tabs._jobService.SaveFollowupToDatabase(jobID, reasonID, "");
							}
							officeFollowupTextField.Text = pickedReason;
						}
						else
						{
							officeFollowupTextField.Text = "No"; 
							pr.OfficeFollowUpRequired = Choices.No; 
						}
					};
					act.ShowInView (this.View);
					break; 
				}
				case i-1: { officeFollowupTextField.Text = "No"; pr.OfficeFollowUpRequired = Choices.No; break; }
				}
			};
			ac.ShowInView (this.View);			
		}
		
		partial void unwillingToSignTouchDown (NSObject sender)
		{
			ac = new UIActionSheet("Is customer unable/unwilling/not there to sign the pre-plumbing check sheet?", null, "Cancel", null, "Yes", "No");
			ac.WillDismiss += delegate(object _sender, UIButtonEventArgs e) {
				const int i = 2; // ac.CancelButtonIndex;
				switch (e.ButtonIndex)
				{
					case i: { /* UnwillingToSignTextField.Text = "?";*/ return; }
					case i-2: { UnwillingToSignTextField.Text = "Yes"; pr.UnwillingToSign = Choices.Yes; return; }
					case i-1: { UnwillingToSignTextField.Text = "No"; pr.UnwillingToSign = Choices.No; return; }
				}
			};
			ac.ShowInView (this.View);					
		}
		
		public override void ViewDidAppear (bool animated)
		{
			if (this.pr == null)
				this.pr = new CheckResult(this);

			if (_navWorkflow != null)
				if (_navWorkflow._tabs != null)
					if (_navWorkflow._tabs._jobRunTable.CurrentJob != null)
					{
						lbJobType.Text = "to change this job's type (" + _navWorkflow._tabs._jobRunTable.CurrentJob.Type.Description +")";
					}
			base.ViewDidAppear (animated);


			/* OLD LOGIC
			if (_navWorkflow.Toolbar.Hidden) { _navWorkflow.SetToolbarHidden (false, animated); }
			_navWorkflow.SetToolbarButtons (WorkflowToolbarButtonsMode.PrePlumbingCheck);
			_navWorkflow.Title = this.Title;
			_navWorkflow.TabBarItem.Image = this.TabBarItem.Image;
			_navWorkflow.DisableTabsAtWorkflowStart();
			*/
			// the code below allows to disable any tabs that should be disabled when the workflow starts

			/*
			foreach( var tbi in _navWorkflow._tabs.TabBar.Items )
			{
				if (tbi.Title == "Invoice" || tbi.Title == "Server/Client") tbi.Enabled = false;
			} */
		}
		
		public override void DidReceiveMemoryWarning ()
		{
			// Releases the view if it doesn't have a superview.
			base.DidReceiveMemoryWarning ();
			
			// Release any cached data, images, etc that aren't in use.
		}
		
		public override void ViewDidLoad ()
		{
			base.ViewDidLoad ();
			
			//any additional setup after loading the view, typically from a nib.
			this.commentsTextView.ShouldBeginEditing = delegate {
				MovedViewY = commentsTextView.Frame.Y;
				float offset = commentsTextView.Frame.Y - 280;
				UIView.BeginAnimations (null);
				UIView.SetAnimationDuration (0.5);
				commentsTextView.Frame = new RectangleF(commentsTextView.Frame.X, 280, commentsTextView.Frame.Size.Width, commentsTextView.Frame.Size.Height);
				lbComments.Frame = new RectangleF(lbComments.Frame.X, lbComments.Frame.Y-offset, lbComments.Frame.Size.Width, lbComments.Frame.Size.Height);
				// hide the necessary objects
				lbExistingDamage.Alpha = 0.05f;
				lbNotAPuratapProblem.Alpha = 0.05f;
				existingDamageTextField.Alpha = 0.05f;
				notPuratapProblemTextField.Alpha = 0.05f;
				UIView.CommitAnimations ();
				return true;				
			};
			
			this.commentsTextView.ShouldEndEditing = delegate {
				float offset = MovedViewY - commentsTextView.Frame.Y;
				UIView.BeginAnimations (null);
				UIView.SetAnimationDuration (0.5);
				commentsTextView.Frame = new RectangleF(commentsTextView.Frame.X, MovedViewY, commentsTextView.Frame.Size.Width, commentsTextView.Frame.Size.Height);
				lbComments.Frame = new RectangleF(lbComments.Frame.X, lbComments.Frame.Y+offset, lbComments.Frame.Size.Width, lbComments.Frame.Size.Height);
				// Un-hide the necessary objects
				lbExistingDamage.Alpha = 1f;
				lbNotAPuratapProblem.Alpha = 1f;
				existingDamageTextField.Alpha = 1f;
				notPuratapProblemTextField.Alpha = 1f;
				UIView.CommitAnimations ();
				return true;				
			};
		}

		[Obsolete]
		public override void ViewDidUnload ()
		{
			// base.ViewDidUnload ();
			
			// Release any retained subviews of the main view.
			// e.g. this.myOutlet = null;
			
			// _generatedPdfView = null
		}

		[Obsolete]
		public override bool ShouldAutorotateToInterfaceOrientation (UIInterfaceOrientation toInterfaceOrientation)
		{
			// Return true for supported orientations
			return (toInterfaceOrientation == UIInterfaceOrientation.LandscapeLeft || toInterfaceOrientation == UIInterfaceOrientation.LandscapeRight);
		}
	}
}

