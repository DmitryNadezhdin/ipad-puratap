using MonoTouch.UIKit;
using System.Drawing;
using System;
using System.IO;
using MonoTouch.Foundation;
using System.Collections.Generic;
using MonoTouch.Dialog;
using Mono.Data.Sqlite;

namespace Application
{
	public class ProblemAndAction { // rewritten to use classes that allow to get proper string representations of choices made by user
		public /* ProblemPointsEnum */ ProblemPoints ProblemPoint { get; set; }
		public /* EnumProblemTypes */ ProblemTypes ProblemType { get; set; }
		public /* PossibleActionsEnum */ PossibleActions ActionTaken { get; set; }
		
		public ProblemAndAction() {	// needs a constructor to create instances of classes it holds
			this.ProblemPoint = new ProblemPoints();
			this.ProblemPoint.Point = new ProblemPointsHierarchy();
			this.ProblemType = new ProblemTypes(); 
			this.ActionTaken = new PossibleActions();
		}
	}
	
	public class ListOfProblems : List<ProblemAndAction> {
		private ServiceCallResult _sr;
		public ServiceCallResult SR { get { return _sr; } }
		
		public void AddProblem (ProblemAndAction value)
		{
			base.Add (value);
			// SR.Controller.btnChooseActions.Title = "Choose actions (" + SR.ProblemsAndActions.Count +")";
			// SR.Controller.serviceNav.ToolbarItems[2].Title = "Choose actions (" + SR.ProblemsAndActions.Count +")";
			_sr.Controller.ProceedToSigningEnabled = false;
		}
		
		public bool IsOk()
		{
			if (this.Count > 0) {
				bool ok = true;
				foreach (ProblemAndAction pr in this)
				{
					if (pr.ActionTaken.Action == PossibleActionsEnum.None) { ok = false; break; }
				}
				return ok;
			}
			else return false;
		}
		
		public ListOfProblems(ServiceCallResult SR) {
			_sr = SR;
		}
	}

	public class ServiceCallResult {
		private JobServiceCallViewController _controller;
		private ListOfProblems _problemsAndActions;
		private JobServiceCallFilterTapType _filterType;
		private JobServiceCallFilterTapType _tapType;
		private JobServiceCallChoices _wasUnitHung;
		private JobServiceCallChoices _unitCondition;
		private JobServiceCallChoices _chemicalsInCupboard;
		private int _pressureTestResult;
		private string _comments;
		
		public ListOfProblems ProblemsAndActions { 
			get { return _problemsAndActions; } 
			set { 
				_problemsAndActions = value;
				_controller.CheckProceedToSigning ();
			} 
		}
		public JobServiceCallViewController Controller { get { return _controller; } set { _controller = value; } }
		public JobServiceCallFilterTapType FilterType { get { return _filterType; } set { _filterType = value; } }
		public JobServiceCallFilterTapType TapType { get { return _tapType; } set { _tapType = value; } }
		public JobServiceCallChoices WasUnitHung { get { return _wasUnitHung; } set { _wasUnitHung = value; } }
		public JobServiceCallChoices UnitCondition { get { return _unitCondition; } set { _unitCondition = value; } }
		public JobServiceCallChoices ChemicalsInCupboard { get { return _chemicalsInCupboard; } set { _chemicalsInCupboard = value; } }
		public int PressureTestResult { get { return _pressureTestResult; } set { _pressureTestResult = value; } }
		
		// DEPRECATED
		// private bool _warranty;
		// private double _plumbingFee;
		// private double _customerCharges ;
		// public bool Warranty { get { return _warranty; } set { _warranty = value; } }
		// public double PlumbingFee { get { return _plumbingFee; } set { _plumbingFee = value; } }
		// public double CustomerCharges { get { return _customerCharges; } set { _customerCharges = value; } }

		public string Comments { get { return _comments; } set { _comments = value; } }
		
		public ServiceCallResult(JobServiceCallViewController controller) {
			_controller = controller;
			this.ProblemsAndActions = new ListOfProblems(this);
		}

		public void Save()
		{
			// Saves ServiceCallResult to database for reporting
		}
	}
	
	
	public partial class JobServiceCallViewController : UIViewController
	{	
		private ServiceNavigationController serviceNav;
		private WorkflowNavigationController _navOld;
		private GetChoicesForObject ac;
		
		// private float MovedViewY;
		private bool _proceedToSigningEnabled;
		public bool ProceedToSigningEnabled {
			get { return _proceedToSigningEnabled; }
			set {
				_proceedToSigningEnabled = value;
				/*
				if (_navOld.Toolbar != null)
					if (_navOld.Toolbar.Items != null)
						foreach(var item in _navOld.Toolbar.Items)
						{
							if (item.Title == "Proceed to signing") { item.Enabled = _proceedToSigningEnabled; }
						}   
				*/
			}
		}
					
		private ServiceCallResult _sr;
		public ServiceCallResult SR { get { return _sr; } set { _sr = value; } }
		
		private ProblemsDialogViewController dvc;
		public UIView PdfListOfIssues;
		
		private UIView _generatedPDFView;
		public UIView GeneratedPDFView { get { return _generatedPDFView; } set { _generatedPDFView = value; } }
		public string pdfServiceReportFileName { get; set; }

		// TODO :: Look into this one
		private bool followUpSaved = false;
		public bool FollowUpEntered { get { return followUpSaved; } 
			set {
				followUpSaved = value;
				CheckProceedToSigning ();
			} }
		
		public void GenerateServicePDFPreview()
		{
			Customer c = _navOld._tabs._jobRunTable.CurrentCustomer;
			
			NSArray a = NSBundle.MainBundle.LoadNib ("JobServiceCallPDFView", this, null);
			GeneratedPDFView = (UIView)MonoTouch.ObjCRuntime.Runtime.GetNSObject (a.ValueAt (0));
			
			UILabel tl = (UILabel)GeneratedPDFView.ViewWithTag (MyConstants.ServiceCallPDFTemplateTags.CustomerNumber);
			tl.Text = String.Format ("Customer Number: {0}", c.CustomerNumber);

			tl = (UILabel)GeneratedPDFView.ViewWithTag (MyConstants.ServiceCallPDFTemplateTags.CustomerName);
			tl.Text = (c.isCompany)? String.Format ("Company name: {0}", c.CompanyName) : String.Format ("Customer Name: {0} {1} {2}", c.Title, c.FirstName, c.LastName);
			
			tl = (UILabel)GeneratedPDFView.ViewWithTag (MyConstants.ServiceCallPDFTemplateTags.Date);
			tl.Text = String.Format ("Date: {0}", DateTime.Now.Date.ToString ("dd/MM/yyyy"));
			
			tl = (UILabel)GeneratedPDFView.ViewWithTag (MyConstants.ServiceCallPDFTemplateTags.ServiceRepName);
			tl.Text = String.Format ("Serviceman name: {0}", MyConstants.EmployeeName);

			/*
			tl = (UILabel)GeneratedPDFView.ViewWithTag (MyConstants.ServiceCallPDFTemplateTags.FilterType);
			switch (SR.FilterType)
			{
				case JobServiceCallFilterTapType.GI2500: { tl.Text = "Unit Type: GI-2500 Twin Filter"; break; }
				case JobServiceCallFilterTapType.GI2600: { tl.Text = "Unit Type: GI-2600 Twin Filter"; break; }
			}
						
			tl = (UILabel)GeneratedPDFView.ViewWithTag (MyConstants.ServiceCallPDFTemplateTags.TapType);
			switch (SR.TapType)
			{
				case JobServiceCallFilterTapType.Standard: { tl.Text = "Tap Type: Standard"; break; }
				case JobServiceCallFilterTapType.Imperial:  { tl.Text = "Tap Type: Imperial"; break; }
				case JobServiceCallFilterTapType.Mark2: { tl.Text = "Tap Type: Mark II"; break; }
			}

			UITextView tvc = (UITextView)GeneratedPDFView.ViewWithTag (MyConstants.ServiceCallPDFTemplateTags.Comments);
			tvc.Text = SR.Comments;
			*/

			DialogViewController tst = new DialogViewController( _navOld._tabs._serviceParts.Root , false);
			tst.TableView = (UITableView) GeneratedPDFView.ViewWithTag (MyConstants.ServiceCallPDFTemplateTags.IssuesFoundView);

			tst.TableView.Source = ((UITableView)PdfListOfIssues).Source;

			// copy values from the job's JobReportData to tst
			JobReportSection section = tst.Root[0] as JobReportSection;
			// section.EntryAlignment = new SizeF(565, 20);
			foreach(Element el in tst.Root[0].Elements)
			{
				if (el is EntryElement && el.Caption == "Pressure")
					(el as EntryElement).Value = section.jrd.Pressure.ToString();

				if (el is MultilineEntryElement && el.Caption == "Comment")
					(el as MultilineEntryElement).Value = section.jrd.Comment;
			}

			tst.TableView.ReloadData ();
			// WAS :: tst.TableView = (UITableView)_pdfListOfIssues;
			
			a.Dispose (); a = null;
			tl.Dispose (); tl = null;
			// tst.TableView = _navOld._tabs._serviceParts.TableView;
			// tvc.Dispose (); tvc = null;
			// tst.Dispose (); tst = null;
		}


		public void RedrawServiceCallPDF(bool DocumentSigned)
		{
			// render created preview in PDF context
			NSMutableData pdfData = new NSMutableData();
			UIGraphics.BeginPDFContext (pdfData, GeneratedPDFView.Bounds, null);
			UIGraphics.BeginPDFPage ();
			GeneratedPDFView.Layer.RenderInContext (UIGraphics.GetCurrentContext ()); // OLD :: this.View.Layer.RenderInContext (UIGraphics.GetCurrentContext ());
			UIGraphics.EndPDFContent ();
			
			// save the rendered context to disk
			NSError err;
			string pdfID = _navOld._tabs._jobRunTable.CurrentCustomer.CustomerNumber.ToString() + "_ServiceReportPDF"; // proper file name here, i. e. pdfID = Date + CustomerNumber + Job ID + DocumentType
			string pdfFileName;
			if (DocumentSigned)	{ pdfFileName = Path.Combine (Environment.GetFolderPath (Environment.SpecialFolder.Personal), pdfID+"_Signed.pdf"); }
			else pdfFileName = Path.Combine (Environment.GetFolderPath (Environment.SpecialFolder.Personal), pdfID+"_Not_Signed.pdf");
			pdfData.Save (pdfFileName, true, out err);
			if (err != null) { err.Dispose (); err = null; }
			// if (pdfData != null) { pdfData.Dispose (); pdfData = null; }
			
			// set up UIWebView in signature view controller			
			serviceNav.Tabs.SignService.PDFView.MultipleTouchEnabled = true;
			serviceNav.Tabs.SignService.PDFView.ScalesPageToFit = true;
			serviceNav.Tabs.SignService.PDFView.LoadRequest (new NSUrlRequest( NSUrl.FromFilename (pdfFileName)));
			pdfServiceReportFileName = pdfFileName;
		}
		
		public JobServiceCallViewController (WorkflowNavigationController nav, ServiceNavigationController sNav) : base ("JobServiceCallViewController", null)
		{
			this.serviceNav = sNav;
			this._navOld = nav;
			this.Title = NSBundle.MainBundle.LocalizedString ("Service", "Service");
			using (var image = UIImage.FromBundle ("Images/157-wrench") ) this.TabBarItem.Image = image;
		}
		
		public override void ViewWillAppear (bool animated)
		{
			base.ViewWillAppear (animated);
			Customer c = _navOld._tabs._jobRunTable.CurrentCustomer;
			using (var image = UIImage.FromBundle ("Images/FilterScheme2") ) ivUnitImage.Image = image;
			
			lbDate.Text = String.Format ("Date: {0}", DateTime.Now.Date.ToString ("dd/MM/yyyy"));
			lbCustomerNumber.Text = String.Format ("Customer Number: {0}", c.CustomerNumber.ToString ());
			lbCustomerName.Text = String.Format ("Customer Name: {0} {1} {2}", c.Title, c.FirstName, c.LastName);
			lbServiceRepName.Text = "Service Rep Name: " + MyConstants.EmployeeName; // IMPLEMENTED :: create a public variable that will hold the device owner's name (gets it from server)
		}
		
		public override void ViewDidAppear (bool animated)
		{
			if (tfPressureTest.Text == "") 
				tfPressureTest.BackgroundColor = UIColor.Red;
			else tfPressureTest.BackgroundColor = UIColor.White;
			if (tfFollowUp.Text == "") 
				tfFollowUp.BackgroundColor = UIColor.Red;
			else tfFollowUp.BackgroundColor = UIColor.White;
			base.ViewDidAppear (animated);
			
			CheckProceedToSigning ();
			serviceNav.SetToolbarItems (new UIBarButtonItem[] {
					new UIBarButtonItem (UIBarButtonSystemItem.Reply),
					new UIBarButtonItem (UIBarButtonSystemItem.FlexibleSpace),
					// new UIBarButtonItem("Choose actions", UIBarButtonItemStyle.Bordered, HandleBtnChooseActionsClicked),
					new UIBarButtonItem (UIBarButtonSystemItem.FlexibleSpace),
					new UIBarButtonItem (UIBarButtonSystemItem.Action)
				}, false);
			serviceNav.ToolbarItems[0].Clicked += delegate {	acBack (null); };
			serviceNav.ToolbarItems[3].Clicked += delegate {	acProceed (null); };
			serviceNav.SetToolbarHidden (false, true);
			serviceNav.Toolbar.SetItems(serviceNav.ToolbarItems, true);
			
			if (! serviceNav.NavigationBar.Hidden) serviceNav.SetNavigationBarHidden (true, false);
		}
		
		public void CheckProceedToSigning()
		{
			if (! followUpSaved) { this.ProceedToSigningEnabled = false; return; }
			if (SR != null) this.ProceedToSigningEnabled = SR.ProblemsAndActions.IsOk ();
			else this.ProceedToSigningEnabled = false;
		}
		
		void acBack (NSObject sender)
		{
			// if this is a child job, we go back to payment
			if (_navOld._tabs._jobRunTable.CurrentJob.HasParent ())
				_navOld._tabs.SelectedViewController = _navOld._tabs.ViewControllers[3];
			// otherwise we go to pre-plumbing
			else _navOld._tabs.SelectedViewController = _navOld._tabs.ViewControllers[0];
		}
		
		void acProceed (NSObject sender)
		{
			// if (ProceedToSigningEnabled) 
			{
				_navOld._tabs.LastSelectedTab = _navOld._tabs.SelectedIndex;

				// _navOld._tabs._jobService.GenerateServicePDFPreview ();
				// _navOld._tabs._jobService.RedrawServiceCallPDF(false);
				
				// proceed to choose used parts
				_navOld._tabs._serviceParts.ThisJob = _navOld._tabs._jobRunTable.CurrentJob;
				_navOld._tabs.SelectedViewController = _navOld._tabs.ViewControllers[2];
				_navOld._tabs.UsedPartsNav.PopToRootViewController (false);
				_navOld._tabs.UsedPartsNav.PushViewController (_navOld._tabs._serviceParts, false);
			}
			/*
			else {
				var alert = new UIAlertView("", "Please choose actions first", null, "OK");
				alert.Show ();
			} */
		}
		
		partial void acFollowUpTouchDown ()
		{
			// FollowUp TouchDown action implementation
			ac = new GetChoicesForObject("Is follow-up required for this?", null, "", null, "Yes", "No");
			ac.WillDismiss += delegate(object sender, UIButtonEventArgs e) {
				const int i = 2;
				switch(e.ButtonIndex)
				{
				case i: { break; }
				case i-1: { // clicked "No"
					tfFollowUp.Text = "No"; 
					FollowUpEntered = true;
					break; }
				case i-2: { // clicked "yes", show another dialog with reasons
					tfFollowUp.Text = "Yes";
					Dictionary<int, string> Reasons = MyConstants.GetFollowUpReasonsFromDB();
					UIActionSheet act = new UIActionSheet("Please specify a reason");
					foreach(int j in Reasons.Keys) act.AddButton (Reasons[j]);
					act.WillDismiss += delegate(object _sender, UIButtonEventArgs ee) {
						if (ee.ButtonIndex!=-1)
						{
							// IMPLEMENTED :: save the followup reason to database
							string pickedReason = ((UIActionSheet)_sender).ButtonTitle (ee.ButtonIndex); // Reasons[ee.ButtonIndex+1];							
							int reasonID = Reasons.FindKeyByValue (pickedReason);
							long jobID = _navOld._tabs._jobRunTable.CurrentJob.JobBookingNumber;

							if (act.ButtonTitle (ee.ButtonIndex).ToUpper ().Contains ("OTHER") || act.ButtonTitle (ee.ButtonIndex).ToUpper ().Contains ("TECHNICAL ISSUES"))
							{
								var getComments = new UIAlertView("Comment", "Type in a few words about why this needs to be followed up", null, "Cancel", "OK");
								getComments.AlertViewStyle = UIAlertViewStyle.PlainTextInput;
								getComments.Dismissed += delegate(object the_sender, UIButtonEventArgs eee) {
									if (eee.ButtonIndex != getComments.CancelButtonIndex)
									{
										string desc = getComments.GetTextField (0).Text;
										SaveFollowupToDatabase(jobID, reasonID, desc);
									}
								};
								getComments.Show ();
							}
							else
							{
								SaveFollowupToDatabase(jobID, reasonID, "");
							}
							// FollowUpEntered = true;
							tfFollowUp.Text = pickedReason;
						}
						else
						{
							tfFollowUp.Text = "";
							FollowUpEntered = false;
							tfFollowUp.BackgroundColor = UIColor.Red;
						}
					};
					act.ShowInView (this.View);
					/*
					var act = new GetChoicesForObject("What is the reason for follow up?", FollowUp);				
					act.WillDismiss += delegate(object _sender, UIButtonEventArgs ee) {
						if (ee.ButtonIndex!=-1) {
							FollowUp = (FollowUpsRequired) ee.ButtonIndex+1;
							tfFollowUp.Text = MyConstants.OutputStringForValue (FollowUp);
						}
					};
					*/
						break;
					}
				}
			};
			ac.ShowInView (this.View);
			tfFollowUp.BackgroundColor = UIColor.White;
		}
		
		public void SaveFollowupToDatabase(long jobID, int reason_id, string comment)
		{
			if (File.Exists (ServerClientViewController.dbFilePath) )
			{
				// read the data from database here
				using (SqliteConnection connection = new SqliteConnection("Data Source="+ServerClientViewController.dbFilePath) )
				{					
					var cmd = connection.CreateCommand();
					connection.Open();
					// determine if a followup reason for this job id already exists
					string sql = 	"SELECT * FROM Followups WHERE Job_id = ?";
					cmd.Parameters.Add ("@Job_ID", System.Data.DbType.Int64).Value = jobID;
					cmd.CommandText = sql;
					var reader = cmd.ExecuteReader ();
					if (! reader.HasRows)
					{
						// if not, insert a new record
						reader.Close ();
						cmd.Parameters.Clear ();
						sql = "INSERT INTO FOLLOWUPS (JOB_ID, REASON_ID, DONE, COMMENT) VALUES (?, ?, ?, ?)";
						cmd.CommandText = sql;
						cmd.Parameters.Add ("@Job_ID", System.Data.DbType.Int64).Value = jobID;
						cmd.Parameters.Add ("@Reason_ID", System.Data.DbType.Int32).Value = reason_id;
						cmd.Parameters.Add ("@Done", System.Data.DbType.Boolean).Value = false;
						cmd.Parameters.Add ("@Comment", System.Data.DbType.String).Value = comment;
						cmd.ExecuteNonQuery ();
					}
					else {
						// if it does, update the existing one
						reader.Close ();
						cmd.Parameters.Clear ();
						sql = "UPDATE Followups SET Reason_ID = ?, Comment = ? WHERE Job_id = ?"; 
						cmd.CommandText = sql;
						cmd.Parameters.Add ("@Reason_ID", System.Data.DbType.Int32).Value = reason_id;
						cmd.Parameters.Add ("@Comment", System.Data.DbType.String).Value = comment;
						cmd.Parameters.Add ("@Job_ID", System.Data.DbType.Int64).Value = jobID;
						cmd.ExecuteNonQuery ();
					}
				}
				FollowUpEntered = true;
				return;
			} else FollowUpEntered = false;
		}
		
		partial void acFilterTypeTouchDown (NSObject sender)
		{
			ac = new GetChoicesForObject("Please choose filter type", null, "Cancel", null, "GI-2500 Twin Filter", "GI-2600 Twin Filter");
			ac.WillDismiss += delegate(object _sender, UIButtonEventArgs e) {
				const int i = 2; // ac.CancelButtonIndex;
				switch (e.ButtonIndex)
				{
					case i: { /* tfFilterType.Text = "?";*/ return; }
					case i-2: { tfFilterType.Text = "GI-2500 Twin Filter"; SR.FilterType = JobServiceCallFilterTapType.GI2500; return; }
					case i-1: { tfFilterType.Text = "GI-2600 Twin Filter"; SR.FilterType = JobServiceCallFilterTapType.GI2600; return; }
				}
			};
			ac.ShowInView (this.View);							 
		}
		
		partial void acTapTypeTouchDown (NSObject sender)
		{
			ac = new GetChoicesForObject("Please choose tap type", null, "Cancel", null, "Standard", "Imperial", "Mark II");
			ac.WillDismiss += delegate(object _sender, UIButtonEventArgs e) {
				const int i = 3; // ac.CancelButtonIndex;
				switch (e.ButtonIndex)
				{
					case i: { /* tfTapType.Text = "?";*/ return; }
					case i-3: { tfTapType.Text = "Standard"; SR.TapType = JobServiceCallFilterTapType.Standard; return; }
					case i-2: { tfTapType.Text = "Imperial"; SR.TapType = JobServiceCallFilterTapType.Imperial; return; }
					case i-1: { tfTapType.Text = "Mark II"; SR.TapType = JobServiceCallFilterTapType.Mark2; return; }
				}
			};
			ac.ShowInView (this.View);							 
		}
		/*
		partial void acUnitHungTouchDown (NSObject sender)
		{
			ac = new GetChoicesForObject("Was the unit hung?", null, "Cancel", null, "Yes", "No");
			ac.WillDismiss += delegate(object _sender, UIButtonEventArgs e) {
				const int i = 2; // ac.CancelButtonIndex;
				switch (e.ButtonIndex)
				{
					case i: {  tfUnitHung.Text = "?"; return; }
					case i-2: { tfUnitHung.Text = "Yes"; SR.WasUnitHung = JobServiceCallChoices.Yes; return; }
					case i-1: { tfUnitHung.Text = "No"; SR.WasUnitHung = JobServiceCallChoices.No; return; }
				}
			};
			ac.ShowInView (this.View);							 			
		}*/
		
		partial void acUnitConditionTouchDown (NSObject sender)
		{
			ac = new GetChoicesForObject("Please specify unit condition", null, "Cancel", null, "Clean", "Normal", "Dirty");
			ac.WillDismiss += delegate(object _sender, UIButtonEventArgs e) {
				const int i = 3; // ac.CancelButtonIndex;
				switch (e.ButtonIndex)
				{
					case i: { /* tfUnitCondition.Text = "?";*/ return; }
					case i-3: { tfUnitCondition.Text = "Clean"; SR.UnitCondition = JobServiceCallChoices.Clean; return; }
					case i-2: { tfUnitCondition.Text = "Normal"; SR.UnitCondition = JobServiceCallChoices.Normal; return; }
					case i-1: { tfUnitCondition.Text = "Dirty"; SR.UnitCondition = JobServiceCallChoices.Dirty; return; }
				}
			};
			ac.ShowInView (this.View);							 						
		}
		
		partial void acChemicalsInCupboardTouchDown (NSObject sender)
		{
			ac = new GetChoicesForObject("Were there any chemicals in the cupboard?", null, "Cancel", null, "Yes", "No");
			ac.WillDismiss += delegate(object _sender, UIButtonEventArgs e) {
				const int i = 2; // ac.CancelButtonIndex;
				switch (e.ButtonIndex)
				{
					case i: { /* tfChemicalsInCupboard.Text = "?";*/ return; }
					case i-2: { tfChemicalsInCupboard.Text = "Yes"; SR.ChemicalsInCupboard = JobServiceCallChoices.Yes; return; }
					case i-1: { tfChemicalsInCupboard.Text = "No"; SR.ChemicalsInCupboard = JobServiceCallChoices.No; return; }
				}
			};
			ac.ShowInView (this.View);							 						
		}
		
		partial void acPressureTestEditingDidEnd (NSObject sender)
		{
			try {
				string s = "";
				for (int j = 0; j < tfPressureTest.Text.Length; j++)
				{
					char c = tfPressureTest.Text[j];
					if (char.IsDigit (c)) { s += c; }
				}
				int i = Convert.ToInt32 (s);
				tfPressureTest.Text = String.Format ("{0} KPI", i);
				SR.PressureTestResult = i;
				tfPressureTest.BackgroundColor = UIColor.White;
			}
			catch (FormatException e) { 
				// Console.WriteLine ("acPressureTestEditingDidEnd: Conversion exception: " + e.Message);
				tfPressureTest.Text = ""; 
				tfPressureTest.Placeholder = "Incorrect value";
			}
		}
				
		public void ClearProblemsList() { 
			if (dvc != null) dvc.RemoveAllElements ();
			if (_sr != null) {
				if (_sr.ProblemsAndActions != null) 	_sr.ProblemsAndActions.Clear (); 
				// _sr.Warranty = false; 
			}
			ProceedToSigningEnabled = false;
		}
		
		public void ResetToDefaults()
		{
			ClearProblemsList ();
			
			// Added statements to set text field values to defaults
			followUpSaved = false;
			if (tfFollowUp != null)	tfFollowUp.Text = "";
			if (tfPressureTest != null) tfPressureTest.Text = "";
			if (tfTapType != null) tfTapType.Text = "Standard";
			if (tfFilterType != null) tfFilterType.Text = "GI-2600 Twin Filter";
			if (tfUnitCondition != null) tfUnitCondition.Text = "Normal";
			if (tfChemicalsInCupboard != null) tfChemicalsInCupboard.Text = "No";
			// if (tvComments != null) tvComments.Text = "";
			serviceNav.PopToRootViewController (false);
		}
		
		public void ChooseActionsTaken()
		{
			if (SR.ProblemsAndActions.Count > -1) // DEBUG 
			{
				var root = new RootElement("Problem List");
				dvc = new ProblemsDialogViewController(serviceNav, root, SR.ProblemsAndActions, _navOld._tabs._jobRunTable.CurrentJob, false) { Autorotate = true };
				
				/*
				EventHandler back = delegate {
					_navWorkflow.PopViewControllerAnimated (true);
				};
				dvc.NavigationItem.LeftBarButtonItem = new UIBarButtonItem("Back to Service screen", UIBarButtonItemStyle.Bordered, back); */
				
				DeactivateEditingModeForProblemsDialogViewController(dvc);	// adds "Edit this list" button on the right side of the top toolbar and sets up appropriate event handlers
				dvc.ViewDissapearing += delegate(object _sender, EventArgs e) {
					// SR.Warranty = (dvc.Root[1].Elements[0] as BooleanElement).Value;
					ProceedToSigningEnabled = SR.ProblemsAndActions.IsOk();
				};

				
				dvc.NavigationItem.HidesBackButton = true;
				serviceNav.SetNavigationBarHidden (false, true);
				
				serviceNav.SetToolbarItems (dvc.ToolbarItems, true);
				serviceNav.SetToolbarHidden (false, true);
				
				serviceNav.PushViewController (dvc, true);
			} 
			else 
			{
				using (var a = new UIAlertView("No problems chosen", "Please tap the picture at the problem's general area", null, "OK"))
				{
					a.Show ();
				}
			}
		}
	
		public void DeactivateEditingModeForProblemsDialogViewController(ProblemsDialogViewController dvc)
		{
			dvc.NavigationItem.RightBarButtonItem = new UIBarButtonItem("Edit this list", UIBarButtonItemStyle.Bordered, delegate {
					dvc.TableView.SetEditing (true, true);
					ActivateEditingModeForProblemsDialogViewController(dvc);				
				}
			);
		}
		
		public void ActivateEditingModeForProblemsDialogViewController(ProblemsDialogViewController dvc)
		{
			dvc.NavigationItem.RightBarButtonItem = new UIBarButtonItem("Done", UIBarButtonItemStyle.Done, delegate {
					dvc.TableView.SetEditing (false, true);
					DeactivateEditingModeForProblemsDialogViewController(dvc);				
				}
			);
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
			tfPressureTest.EditingDidEndOnExit += delegate {
				acPressureTestEditingDidEnd(null);
			};
			
			SR = new ServiceCallResult(this) {
				FilterType = JobServiceCallFilterTapType.GI2600,
				TapType = JobServiceCallFilterTapType.Standard,
				WasUnitHung = JobServiceCallChoices.Yes,
				UnitCondition = JobServiceCallChoices.Normal,
				PressureTestResult = 0,
				Comments = ""
			};

			/* tvComments.ShouldEndEditing = delegate {
				SR.Comments = tvComments.Text;
				return true;
			}; */

			// tvComments.Text = "Additional comments";
			//btnChooseActions.Clicked += HandleBtnChooseActionsClicked;
			
			ProceedToSigningEnabled = false; // proceed to signing is disabled initially
			
			
			/*
			tvComments.ShouldBeginEditing = delegate {
				UIView.BeginAnimations (null);
				UIView.SetAnimationDuration (0.5);
				MovedViewY = tvComments.Frame.Y;
				if (tvComments.Text == "Additional comments") { tvComments.Text = ""; }
				tvComments.Frame = new RectangleF(tvComments.Frame.X, 280, tvComments.Frame.Size.Width, tvComments.Frame.Size.Height);
				ivUnitImage.Alpha = 0.05f;  // hide the picture view object here
				UIView.CommitAnimations ();
				return true;
			};
			
			tvComments.ShouldEndEditing = delegate {
				if (tvComments.Text == "") { tvComments.Text = "Additional comments"; }
				UIView.BeginAnimations (null);
				UIView.SetAnimationDuration (0.5);
				tvComments.Frame = new RectangleF(tvComments.Frame.X, MovedViewY, tvComments.Frame.Size.Width, tvComments.Frame.Size.Height);
				ivUnitImage.Alpha = 1f;	// Un-hide the picture view object here
				UIView.CommitAnimations ();
				return true;
			};
			
			tfPressureTest.ShouldBeginEditing = delegate {
				UIView.BeginAnimations (null);
				UIView.SetAnimationDuration (0.5);
				MovedViewY = tfPressureTest.Frame.Y;
				float offset = tfPressureTest.Frame.Y - 330;
				tfPressureTest.Frame = new RectangleF(tfPressureTest.Frame.X, tfPressureTest.Frame.Y - offset, tfPressureTest.Frame.Size.Width, tfPressureTest.Frame.Size.Height);
				lbPressureTestResults.Frame = new RectangleF(lbPressureTestResults.Frame.X, lbPressureTestResults.Frame.Y - offset, lbPressureTestResults.Frame.Size.Width, lbPressureTestResults.Frame.Size.Height);
				ivUnitImage.Alpha = 0.05f;  // hide the picture view object here
				UIView.CommitAnimations ();
				return true;
			};
			
			tfPressureTest.ShouldEndEditing = delegate {
				UIView.BeginAnimations (null);
				UIView.SetAnimationDuration (0.5);
				float offset = MovedViewY - tfPressureTest.Frame.Y;
				tfPressureTest.Frame = new RectangleF(tfPressureTest.Frame.X, tfPressureTest.Frame.Y + offset, tfPressureTest.Frame.Size.Width, tfPressureTest.Frame.Size.Height);
				lbPressureTestResults.Frame = new RectangleF(lbPressureTestResults.Frame.X, lbPressureTestResults.Frame.Y + offset, lbPressureTestResults.Frame.Size.Width, lbPressureTestResults.Frame.Size.Height);
				ivUnitImage.Alpha = 1f;  // hide the picture view object here
				UIView.CommitAnimations ();
				return true;
			};
			*/
		}

		void HandleBtnChooseActionsClicked (object sender, EventArgs e)
		{ 
			if (tfFollowUp.Text == "" || tfFollowUp.Text == "Yes" || tfPressureTest.Text == "")
			{
				// a dialog explaining that you cannot proceed until you fill out the required fields
				var alert = new UIAlertView("", "Please fill out the required fields first (follow-up and pressure test results)", null, "OK");
				alert.Show ();
			}
			else 
			{
				ChooseActionsTaken ();
			}
		}

		bool ButtonTitleUnique(UIActionSheet ash, string newButtonTitle)
		{
			for (int i = 0; i < ash.ButtonCount; i++)
			{
				if ( ash.ButtonTitle (i) == newButtonTitle )
					return false;
			}
			return true;
		}
		
		public override void TouchesBegan (NSSet touches, UIEvent evt)
		{
			UITouch touch = (UITouch)touches.AnyObject;
			PointF tappedPoint = touch.LocationInView (ivUnitImage);
			touch.Dispose (); touch = null;

			JobReportSection reportSection = _navOld._tabs._serviceParts.Root[0] as JobReportSection;
			if (reportSection == null)
			{
				_navOld._tabs._serviceParts.ThisJob = _navOld._tabs._jobRunTable.CurrentJob;
				_navOld._tabs._serviceParts.AddJobReport (false);
				reportSection = _navOld._tabs._serviceParts.Root[0] as JobReportSection;
			}
			else
			{
				// job reports was attached previously : 
				Job currentJob = _navOld._tabs._jobRunTable.CurrentJob;
				if (currentJob != null) 
					currentJob.JobReportAttached = true;
			}

			List<ServicePoint> dbPoints = reportSection.ReadAllDBServicePointsWithCoordinates ();

			var choosePoint = new UIActionSheet("Please choose an issue from the list");
			foreach( ServicePoint point in dbPoints)
			{
				if ( point.RectAround.Contains (tappedPoint) )
					if (ButtonTitleUnique (choosePoint, point.PointDesc))
						choosePoint.AddButton (point.PointDesc);
			}

			choosePoint.Dismissed += delegate(object sender, UIButtonEventArgs e) {
				if (e.ButtonIndex != choosePoint.CancelButtonIndex)
				{
					foreach( ServicePoint point in dbPoints)
					{
						if (point.PointDesc == choosePoint.ButtonTitle (e.ButtonIndex))
						{
							reportSection.jrd.PointOID = point.PointID;
							foreach(Element el in reportSection.Elements)
							{
								if (el is StringElement)
									if ( (el as StringElement).Caption == "Issue description" )
									{
										(el as StringElement).Value = point.PointDesc;
									_navOld._tabs._serviceParts.ReloadData ();
									}
							}
							break;
						}
					}
					acProceed (this);
				}
			};

			if (choosePoint.ButtonCount > 0)
				choosePoint.ShowInView (this.View);

			/* THIS LOGIC WAS USED UP UNTIL VERSION 0.9.8 (16.11.12)
			if (tappedPoint.X > 0 && tappedPoint.Y > 0)
			{
				ProblemAndAction pa = new ProblemAndAction();
				pa.ActionTaken.Action = PossibleActionsEnum.None;
				
				GetChoicesForObject chooseProblemType = new GetChoicesForObject("What has gone wrong with this?", pa.ProblemType);
				chooseProblemType.WillDismiss += delegate(object sender, UIButtonEventArgs e) {
					if (e.ButtonIndex != chooseProblemType.CancelButtonIndex)
					{
						// if "None of the above" has been chosen then we need to display a UIAlertView for the user to enter the description
						if (chooseProblemType.ButtonTitle (e.ButtonIndex) == "None of above")
						{
							var enterProblemDescription = new UIAlertView("Briefly describe the problem", "", null, "Cancel", "OK");
							enterProblemDescription.AlertViewStyle = UIAlertViewStyle.PlainTextInput;
							enterProblemDescription.WillDismiss += delegate(object obj, UIButtonEventArgs _e) {
								if (_e.ButtonIndex != enterProblemDescription.CancelButtonIndex)
								{
									// add custom problem description to PA object
									SR.ProblemsAndActions.AddProblem (pa);
									// this.tvComments.Text += String.Format ( "{0}: {1}\n", pa.ProblemPoint.OutputString(), enterProblemDescription.GetTextField (0).Text);
								}	
							};
							enterProblemDescription.Show ();
						}
						else SR.ProblemsAndActions.AddProblem (pa);
					}
				};
				
				if ( MyConstants.rectAroundBallValve.Contains ( tappedPoint ) )
				{	
					ac = new GetChoicesForObject("You tapped at the ball valve. Please specify the problem point", null, "Cancel", null, "Ball valve", "T");
					ac.WillDismiss += delegate(object sender, UIButtonEventArgs e) {
						if (e.ButtonIndex != ac.CancelButtonIndex) {
							pa.ProblemPoint.Point.GeneralArea.Area = (GeneralAreasEnum) e.ButtonIndex;
							GetChoicesForObject choosePoint = new GetChoicesForObject("Please choose location of a problem", pa.ProblemPoint);
							choosePoint.WillDismiss += delegate(object obj, UIButtonEventArgs _e) { if (_e.ButtonIndex != choosePoint.CancelButtonIndex) chooseProblemType.ShowInView (this.View); };
							choosePoint.ShowInView (this.View);
						}
					};
					ac.ShowInView (this.View);
				}
				
				if ( MyConstants.rectAroundTap.Contains ( tappedPoint ) )
				{	
					pa.ProblemPoint.Point.GeneralArea.Area = GeneralAreasEnum.Tap;
					GetChoicesForObject choosePoint = new GetChoicesForObject("Please choose location of a problem", pa.ProblemPoint);
					choosePoint.WillDismiss += delegate (object obj, UIButtonEventArgs e) { if (e.ButtonIndex != choosePoint.CancelButtonIndex) chooseProblemType.ShowInView (this.View); };
					choosePoint.ShowInView (this.View);							
				}
				if ( MyConstants.rectAroundInletTubing.Contains ( tappedPoint ) )
				{	
					pa.ProblemPoint.Point.GeneralArea.Area = GeneralAreasEnum.InletTubing;
					GetChoicesForObject choosePoint = new GetChoicesForObject("Please choose location of a problem", pa.ProblemPoint);
					choosePoint.WillDismiss += delegate(object obj, UIButtonEventArgs e) { if (e.ButtonIndex != choosePoint.CancelButtonIndex) chooseProblemType.ShowInView (this.View); };
					choosePoint.ShowInView (this.View);
				}
				if ( MyConstants.rectAroundUnit.Contains (tappedPoint) )
				{
					pa.ProblemPoint.Point.GeneralArea.Area = GeneralAreasEnum.Purifier;
					GetChoicesForObject choosePoint = new GetChoicesForObject("Please choose location of a problem", pa.ProblemPoint);
					choosePoint.WillDismiss += delegate(object obj, UIButtonEventArgs e) { if (e.ButtonIndex != choosePoint.CancelButtonIndex) chooseProblemType.ShowInView (this.View); };
					choosePoint.ShowInView (this.View);					
				}
				
				if ( MyConstants.rectAroundOutletTubing.Contains (tappedPoint) )
				{
					pa.ProblemPoint.Point.GeneralArea.Area = GeneralAreasEnum.OutletTubing;
					GetChoicesForObject choosePoint = new GetChoicesForObject("Please choose location of a problem", pa.ProblemPoint);
					choosePoint.WillDismiss += delegate(object obj, UIButtonEventArgs e) { if (e.ButtonIndex != choosePoint.CancelButtonIndex) chooseProblemType.ShowInView (this.View); };
					choosePoint.ShowInView (this.View);					
				}
			}
			*/
		}

		/* [Obsolete] :: started to get crashes after marking the method with [Obsolete] attribute
		public override void ViewDidUnload ()
		{
			base.ViewDidUnload ();
			
			// Release any retained subviews of the main view.
			// e.g. this.myOutlet = null;

			//			ac.Dispose (); ac = null;
			//			_generatedPDFView.Dispose ();	_generatedPDFView = null;
			//			PdfListOfIssues.Dispose ();	PdfListOfIssues = null;

// The code above seems to be leading to a crash alike this one:
//					0 Puratap 0x006d9746 testflight_backtrace + 158
//					1 Puratap 0x006da370 TFSignalHandler + 244
//					2 libsystem_c.dylib 0x34c2f7ec _sigtramp + 48
//					3 libsystem_c.dylib 0x34c2520e pthread_kill + 54
//					4 libsystem_c.dylib 0x34c1e29e abort + 94
//					5 Puratap 0x0062145e monoeg_assertion_message + 58
//					6 Puratap 0x006d7ba7 monotouch_unhandled_exception_handler + 167
//					7 Puratap 0x006495f8 mono_invoke_unhandled_exception_hook + 92
//					8 Puratap 0x0060443e mono_thread_abort + 46
//					9 Puratap 0x0064b8f6 mono_handle_exception_internal + 2138
//					10 Puratap 0x0064b9c4 mono_handle_exception + 12
//					11 Puratap 0x0067d0e8 handle_signal_exception + 84
//					12 0x02400007 + 0
//					13 Puratap 0x002e72d3 wrapper_runtime_invoke_object_runtime_invoke_dynamic_intptr_intptr_intptr_intptr_0 + 199
//					14 Puratap 0x00606996 mono_jit_runtime_invoke + 1054
//					15 Puratap 0x00674306 mono_runtime_invoke + 90
//					16 Puratap 0x005f09b2 native_to_managed_trampoline_Application_JobServiceCallViewController_ViewDidUnload + 178
//					17 UIKit 0x306dea36 -[UIViewController unloadViewForced:] + 250
//					18 UIKit 0x30829834 -[UINavigationController purgeMemoryForReason:] + 128
//					19 Foundation 0x3301c4fe __57-[NSNotificationCenter addObserver:selector:name:object:]_block_invoke_0 + 18
//					20 CoreFoundation 0x374d7546 ___CFXNotificationPost_block_invoke_0 + 70
//					21 CoreFoundation 0x37463096 _CFXNotificationPost + 1406
//					22 Foundation 0x32f903ea -[NSNotificationCenter postNotificationName:object:userInfo:] + 66
//					23 Foundation 0x32f91c1a -[NSNotificationCenter postNotificationName:object:] + 30
//					24 UIKit 0x307f10ec -[UIApplication _performMemoryWarning] + 80
//					25 UIKit 0x307f11e6 -[UIApplication _receivedMemoryNotification] + 174
//					26 libdispatch.dylib 0x346bf2e0 _dispatch_source_invoke + 516
//					27 libdispatch.dylib 0x346bcb80 _dispatch_queue_invoke$VARIANT$mp + 52
//					28 libdispatch.dylib 0x346bcec0 _dispatch_main_queue_callback_4CF$VARIANT$mp + 156
//					29 CoreFoundation 0x374de2ac __CFRunLoopRun + 1268
//					30 CoreFoundation 0x374614a4 CFRunLoopRunSpecific + 300
//					31 CoreFoundation 0x3746136c CFRunLoopRunInMode + 104
//					32 GraphicsServices 0x3765a438 GSEventRunModal + 136
//					33 UIKit 0x3066dcd4 UIApplicationMain + 1080
//					34 Puratap 0x0006750b wrapper_managed_to_native_MonoTouch_UIKit_UIApplication_UIApplicationMain_int_string___intptr_intptr + 239
//					35 Puratap 0x0052e43b Puratap_Application_Application_Main_string__ + 23
//					36 Puratap 0x002e72d3 wrapper_runtime_invoke_object_runtime_invoke_dynamic_intptr_intptr_intptr_intptr_0 + 199
//					37 Puratap 0x00606996 mono_jit_runtime_invoke + 1054
//					38 Puratap 0x00674306 mono_runtime_invoke + 90
//					39 Puratap 0x0067706e mono_runtime_exec_main + 306
//					40 Puratap 0x0067a992 mono_runtime_run_main + 482
//					41 Puratap 0x0061e8aa mono_jit_exec + 94
//					42 Puratap 0x006c618b main + 2235
//					43 Puratap 0x0000bfa7 start + 39
		}*/

		[Obsolete]
		public override bool ShouldAutorotateToInterfaceOrientation (UIInterfaceOrientation toInterfaceOrientation)
		{
			// Return true for supported orientations
			return (toInterfaceOrientation == UIInterfaceOrientation.LandscapeLeft || toInterfaceOrientation == UIInterfaceOrientation.LandscapeRight);
		}
		
		public class ProblemStyledString : StyledStringElement
		{
			ProblemAndAction _pa;
			public ProblemAndAction PA { get { return _pa; } set { _pa = value; } }
			
			public ProblemStyledString(string caption, string val, UITableViewCellStyle style, ProblemAndAction pa) : base(caption, val, style)
			{
				_pa = pa;
				this.Caption = String.Format ("{0}: {1}", _pa.ProblemPoint.OutputString() , _pa.ProblemType.OutputString() );
				this.Value = String.Format ("Action taken: {0}", _pa.ActionTaken.OutputString() );
			}
			
			public override UITableViewCell GetCell (UITableView tv)
			{
				UITableViewCell cell = base.GetCell (tv);
				cell.TextLabel.Text = String.Format ("{0}: {1}", _pa.ProblemPoint.OutputString() , _pa.ProblemType.OutputString() );
				cell.DetailTextLabel.Text = String.Format ("Action taken: {0}", _pa.ActionTaken.OutputString() );
				return cell;
			}
			
			public override void Selected (DialogViewController dvc, UITableView tableView, NSIndexPath path)
			{
				base.Selected (dvc, tableView, path);
				// Console.WriteLine ( String.Format ("You selected cell: Section: {0}; Row: {1} \t {2} : {3}", path.Section, path.Row, _pa.ProblemPoint.OutputString (), _pa.ActionTaken.OutputString ()));
				GetChoicesForObject a = new GetChoicesForObject("Please choose an action taken for this problem", _pa.ActionTaken, (ProblemsDialogViewController)dvc );
				a.ShowInView (dvc.View);
			}
		}
		
		public class ProblemsDialogViewController : DialogViewController
		{
			//public UIToolbar Toolbar { get; set; }
			// private Job current;
			private ServiceNavigationController _nav;
			private ListOfProblems _problems;
			public ListOfProblems Problems { get { return _problems; } }
						
			public class ProblemsSource : DialogViewController.Source
			{
				private ProblemsDialogViewController _dvc;
				public ProblemsSource(ProblemsDialogViewController dvc) : base (dvc) { _dvc = dvc; } // constructor inherited from DialogViewController.Source
				
				public override bool CanEditRow (UITableView tableView, NSIndexPath indexPath)
				{
					if (indexPath.Section == 0) { return true; }
					else return false;
				}
				
				public override UITableViewCellEditingStyle EditingStyleForRow (UITableView tableView, NSIndexPath indexPath)
				{
					if (indexPath.Section == 0) { return UITableViewCellEditingStyle.Delete; }
					else return UITableViewCellEditingStyle.None;
				}
				
				public override void CommitEditingStyle (UITableView tableView, UITableViewCellEditingStyle editingStyle, NSIndexPath indexPath)
				{
					var section = this.Container.Root[indexPath.Section];
					var element = section[indexPath.Row];
					section.Remove (element);
					_dvc._problems.RemoveAt (indexPath.Row);
					_dvc._problems.SR.Controller.CheckProceedToSigning ();
				}
			}
			
			public void SaveViewToPDF()
			{
				_problems.SR.Controller.PdfListOfIssues = (UIView)this.TableView;	// saves the tableview to _pdfListOfIssues
			}
			
			public void RemoveAllElements()
			{
				Section section = this.Root[0];
				section.RemoveRange (0, section.Count);
				_problems.Clear();
				SaveViewToPDF ();
			}
			
			public void ReloadDataAndCheck()
			{
				this.ReloadData ();
				_problems.SR.Controller.CheckProceedToSigning ();
				SaveViewToPDF ();
			}
			
			public override void ViewDidAppear (bool animated)
			{
				_nav.SetToolbarItems (this.ToolbarItems, true);
				using (var image = UIImage.FromBundle ("Images/152-rolodex") )	_nav.TabBarItem.Image = image;
				_nav.TabBarItem.Title = "Problems list";
				base.ViewDidAppear (animated);
				//_problems.SR.Controller.serviceNav.TabBarItem.Image = UIImage.FromBundle ("Images/152-rolodex");
				//_problems.SR.Controller.serviceNav.TabBarItem.Title = "Problems list";
				SaveViewToPDF ();				
			}
			
			public override void ViewDidDisappear (bool animated)
			{
				base.ViewDidDisappear (animated);
				// This is now done at UsedPartsViewController level
				// if (_problems.SR.Warranty == true)
					// set the customer charge for the current job to 0
					// current.MoneyToCollect = 0;
			}
			
			public override Source CreateSizingSource (bool unevenRows)
			{
				if (unevenRows) throw new NotImplementedException("Need... to... implement... new SizingSource subclass");
				return new ProblemsSource (this);
			}
			
			public ProblemsDialogViewController(RootElement root, bool pushing) : base (root, pushing)	{ } // constructors inherited from DialogViewController
			public ProblemsDialogViewController(ServiceNavigationController nav, RootElement root, ListOfProblems problems, Job selected, bool pushing) : base (root, pushing)	{
				_nav = nav;
				_problems = problems;
				// current = selected;
				Title = "Issues";
				var problemsSection = new Section("Below is a list of issues found:");
				
				foreach (var problem in problems)
				{
					var pr = new ProblemStyledString("", "", UITableViewCellStyle.Value1, problem);
					problemsSection.Add (pr);
				}
				
				// var warranty = new Section();
				// warranty.Add (new BooleanElement("Warranty", Warranty));
				// root.Add (warranty);

				root.Add (problemsSection);			
				
				ToolbarItems = new UIBarButtonItem[] {
					new UIBarButtonItem(UIBarButtonSystemItem.Reply),
					new UIBarButtonItem(UIBarButtonSystemItem.FlexibleSpace),
					new UIBarButtonItem(UIBarButtonSystemItem.Action)
				};
				
				this.ToolbarItems[0].Clicked += delegate {
					if (_nav.Tabs._jobService.ProceedToSigningEnabled) 
					{
						_nav.Tabs._jobService.GenerateServicePDFPreview ();
						_nav.Tabs._jobService.RedrawServiceCallPDF(false);
					}
					_nav.SetNavigationBarHidden (true, false);
					_nav.PopToRootViewController (true);
					_nav.TabBarItem.Image = _nav.Tabs._jobService.TabBarItem.Image;
					_nav.TabBarItem.Title = _nav.Tabs._jobService.TabBarItem.Title;
				};
				
				this.ToolbarItems[2].Clicked += delegate {
					if (_nav.Tabs._jobService.ProceedToSigningEnabled) 
					{
						_nav.Tabs._jobService.GenerateServicePDFPreview ();
						_nav.Tabs._jobService.RedrawServiceCallPDF(false);
						_nav.Tabs._serviceParts.ThisJob = _nav.Tabs._jobRunTable.CurrentJob;
						_nav.Tabs.SelectedViewController = _nav.Tabs.ViewControllers[2];
						_nav.Tabs.UsedPartsNav.PopToRootViewController (false);
						_nav.Tabs.UsedPartsNav.PushViewController (_nav.Tabs._serviceParts, false);
					}
					else {
						var alert = new UIAlertView("", "Please choose an action for every issue in the list", null, "OK");
						alert.Show ();
					}
				};
			} 
		}
	}
	
	public class ServiceUsedPartsViewController : UsedPartsViewController
	{
		public ServiceUsedPartsViewController(RootElement root, WorkflowNavigationController nav, UsedPartsNavigationController upnav, bool pushing) : base (root, pushing)
		{
			NavigationItem.SetHidesBackButton (true, false);
			NavigationItem.HidesBackButton = true;
			NavUsedParts = upnav;
			NavWorkflow = nav;
			DBParts = new List<Part>();
			DeactivateEditingMode ();
			ThisJob = nav._tabs._jobRunTable.CurrentJob;

			Section WarrantySection = new Section("");
			BooleanElement warrantyElement = new BooleanElement("Warranty", false);
			warrantyElement.ValueChanged += delegate {
				this.NavWorkflow._tabs._jobRunTable.CurrentJob.Warranty = warrantyElement.Value;
			};
			WarrantySection.Add (warrantyElement);
			Root.Add(WarrantySection);
			
			Root.Caption = "Service Parts";
			Section emptySection = new Section(" "); // new Section("Treasures gained by wickedness do not profit, but righteousness delivers from death.");
			Root.Add(emptySection);
			
			Section AdditionalPartsSection = new Section("Parts used for service");
			AdditionalPartsSection.Add (new StyledStringElement("Tap here to add a part"));

			Root.Add (AdditionalPartsSection);			
			
			// set toolbar items here (to be displayed by NavUsedParts)
			ToolbarItems = new UIBarButtonItem[] {
				new UIBarButtonItem(UIBarButtonSystemItem.Reply),
				new UIBarButtonItem(UIBarButtonSystemItem.FlexibleSpace),
				new UIBarButtonItem("Clear parts list", UIBarButtonItemStyle.Bordered, delegate { ClearPartsList (); } ),
				new UIBarButtonItem(UIBarButtonSystemItem.FlexibleSpace),
				new UIBarButtonItem(UIBarButtonSystemItem.Action)				
			};
			
			ToolbarItems[0].Clicked += delegate {
				NavUsedParts.Tabs.SelectedViewController = NavUsedParts.Tabs.ViewControllers[1];
			};
			
			ToolbarItems[4].Clicked += delegate {
				if (ThisJob.JobReportAttached)
				{				
					if (JobReportDataValid () )	// check that pressure and comment fields have been filled
					{
						foreach(Section section in Root)					
							if (section is JobReportSection)
							{
								(section as JobReportSection).jrd.Pressure = Convert.ToInt32 ( (section.Elements[1] as EntryElement).Value );
								(section as JobReportSection).jrd.Comment = (section.Elements[4] as MultilineEntryElement).Value;

								SaveJobReport ( (section as JobReportSection).jrd );
							
								// save the service report
								nav._tabs._jobService.PdfListOfIssues = (UIView) this.TableView;
								nav._tabs._jobService.GenerateServicePDFPreview ();
								nav._tabs._jobService.RedrawServiceCallPDF (false);
							}

						NavUsedParts.Tabs.SelectedViewController = NavUsedParts.Tabs.ViewControllers[3];
					}
					else
					{
						// 		job report data is invalid: there should be some notification about this
						// var reportDataInvalid = new UIAlertView("Cannot proceed", "Something is wrong with report data. Please make sure that all required information has been entered.", null, "OK");
						// reportDataInvalid.Show ();
					}
				}
				else
				{
					// should never end up here since service jobs must include job reports, right?
					var wtf = new UIAlertView("Cannot proceed", "No problem point selected on the unit picture. Please go back and pick one.", null, "Oh, okay");
					wtf.Show ();
				}
			};
			
			NavUsedParts.SetToolbarHidden (false, false);
		}

		public void ResetToDefaults()
		{
			foreach(Section section in Root)					
				if (section is JobReportSection)
				{
					(section as JobReportSection).jrd.Comment = "";
					(section.Elements[4] as MultilineEntryElement).Value = "";
					
					(section as JobReportSection).jrd.Pressure = 0;
					(section.Elements[1] as EntryElement).Value = "";

					(section as JobReportSection).jrd.ReasonOID = 0;
					(section.Elements[2] as StringElement).Value = "Not chosen";

					(section as JobReportSection).jrd.PointOID = 0;
					(section.Elements[3] as StringElement).Value = "Not chosen";
				}
			ReloadData ();
		}

		public override void ViewWillAppear (bool animated)
		{
			// AddJobReport (false);
			/*
			BooleanElement warrantyElement = Root[0].Elements[0] as BooleanElement;
			warrantyElement.ValueChanged += delegate {
				ThisJob.Warranty = warrantyElement.Value;
			}; */
			/*
			if (this.Root[0] is JobReportSection)
			{
				foreach(Element el in Root[0])
					if (el is EntryElement && el.Caption == "Pressure")
						(el as EntryElement).Value = (Root[0] as JobReportSection).jrd.Pressure.ToString();
			}
			*/
			ReloadData ();
			base.ViewWillAppear (animated);
		}
		
		public override void ViewDidAppear (bool animated)
		{
			// NavUsedParts.TabBarItem.Image = null;
			NavUsedParts.TabBarItem.Title = Root.Caption;
			base.ViewDidAppear (animated);
		}
	}
}

