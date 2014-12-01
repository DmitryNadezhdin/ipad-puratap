using System;
using System.IO;
using System.Data;
using System.Collections.Generic;
using System.Linq;
using MonoTouch.Foundation;
using MonoTouch.UIKit;
using System.Drawing;
using Mono.Data.Sqlite;
using MonoTouch.Dialog;

namespace Puratap
{
	public enum WorkflowToolbarButtonsMode { 
		PrePlumbingCheck, SigningPrePlumbingCheck,
		Installation,
		ServiceCallDetails, ServiceCallProblemsList, SigningServiceCallReport,
		FilterChange,
		JobSummary,
		Payment,
		Default, None };
	
	public class WorkflowNavigationController : UINavigationController
	// this controller is deprecated and is used as a container for methods that should belong to DetailedTabs class
	{
		
		public DetailedTabs _tabs { get; set; }
		UIBarButtonItem _leftButton;
		UIBarButtonItem _flexibleButtonSpace;
		UIBarButtonItem _rightButton;
		UIBarButtonItem[] _buttons;
		
		public UIBarButtonItem LeftButton { get { return _leftButton; } set { _leftButton = value; } }
		public UIBarButtonItem RightButton { get { return _rightButton; } set { _rightButton = value; } }
		public UIBarButtonItem FlexibleButtonSpace { get { return _flexibleButtonSpace; } set { _flexibleButtonSpace = value; } }
		public UIBarButtonItem[] Buttons { get { return _buttons; } set { _buttons = value; } }

		// these event handlers ease debugging, allowing to understand what has happened in the application flow
		EventHandler _resetWorkflow;
		EventHandler _proceedToSignPrePlumbing;
		EventHandler _startSigning;
		EventHandler _finishSigning;
		EventHandler _proceedAfterSigningPrePlumbing;
		public EventHandler _refusedToPay;
		public EventHandler _extraJobs;
		public EventHandler _clearJobsList;
		
		public EventHandler ResetWorkflow { get { return _resetWorkflow; } }
		public EventHandler StartSigning {	get { return _startSigning; }	set { _startSigning = value; } }
		public EventHandler FinishSigning { get { return _finishSigning; } set { _finishSigning = value; } }
		public EventHandler ProceedAfterSigningPrePlumbing { get { return _proceedAfterSigningPrePlumbing; }	set { _proceedAfterSigningPrePlumbing = value; } }
				
		EventHandler _proceedAfterSigningServiceReport;
		EventHandler _chooseActionsTaken;
		EventHandler _clearProblemsList;
		EventHandler _clearAdditionalPartsList;
		EventHandler _proceedToPayment;
		public EventHandler _finishWorkflow;
		
		EventHandler _proceedToSignServiceReport;
		public EventHandler ProceedToSignServiceReport { get { return _proceedToSignServiceReport; } set { _proceedToSignServiceReport = value; } }
		
		Part _chosenPart;
		public Part ChosenPart {
			get { return _chosenPart; }
			set {
				_chosenPart = value;
				PopViewControllerAnimated (true);
				if (this.TopViewController != null) // && this.TopViewController.GetType ().Name == "FilterChangeViewController")
					(this.TopViewController as UsedPartsViewController).PartChosen(ChosenPart, false);
			}
		}
		
		public void ProceedToBookedJobType(object obj, EventArgs evargs)
		{
			Job j = _tabs._jobRunTable.CurrentJob;
			if (j.Started != MyConstants.JobStarted.Yes) {
				j.Started = MyConstants.JobStarted.Yes;
			}
			StartWorkflowForJob (j);
		}
		
		public void ChangeJobType(object obj, EventArgs evargs)
		{
			// get a list of possible job types considering the employee type
			
			// display the list
			// set the ShouldPayFee to the appropriate default value (false for SERVICE, true for everything else)
			// if this is not a main job, set the job's MoneyToCollect property to the default retail price
			InvokeOnMainThread( delegate {
				Job curj = _tabs._jobRunTable.CurrentJob;

				UIActionSheet _alert = new UIActionSheet("Choose a job type", null, null, null); //, buttonTitles);

				// since the job types that one can do may change over time (e.g. when going from PLUMBER mode to FRANCHISEE mode),
				// reload list of job types available
				JobRunTable.JobTypes = MyConstants.GetJobTypesFromDB ();

				foreach(JobType jt in JobRunTable.JobTypes) {
					if (jt.CanDo) {
						_alert.AddButton (jt.Description);
					}
				}

				_alert.Dismissed += delegate(object sender, UIButtonEventArgs e) {
					if (e.ButtonIndex != _alert.CancelButtonIndex)
					{
						string chosenType = _alert.ButtonTitle(e.ButtonIndex);
						foreach(JobType jt in JobRunTable.JobTypes)
						{
							if (jt.Description == chosenType)
							{
								curj.UsedParts.Clear ();

								// resetting the job results: reverting service operation view controller to defaults
								_tabs._jobService.ResetToDefaults ();
								curj.SetJobType(jt.Code);
								_tabs._prePlumbView.SetJobTypeText (jt.Description);
								
								if (_tabs._jobRunTable.LastSelectedRowPath.Section == 1) { // if the current job was user-created
									curj.MoneyToCollect = curj.Type.RetailPrice;
								}
								break;
							}
						}
					}
				};
				_alert.ShowInView (this._tabs.View);
			} );		
		}
		
		public void GoFilterChange()
		{	
			_tabs.SelectedViewController = _tabs.ViewControllers[2];
			if (_tabs.UsedPartsNav.ViewControllers.Length>1) _tabs.UsedPartsNav.PopToRootViewController(false);
			_tabs.UsedPartsNav.PushViewController (_tabs._jobFilter, false);
		}
		
		public void GoNewTap()
		{
			_tabs.SelectedViewController = _tabs.ViewControllers[2];		
			if (_tabs.UsedPartsNav.ViewControllers.Length > 1) _tabs.UsedPartsNav.PopToRootViewController (false);
			_tabs.UsedPartsNav.PushViewController (_tabs._jobNewTap, false);			
		}
		
		public void GoTubingUpgrade()
		{
			_tabs.SelectedViewController = _tabs.ViewControllers[2];
			if (_tabs.UsedPartsNav.ViewControllers.Length > 1) _tabs.UsedPartsNav.PopToRootViewController (false);
			_tabs.UsedPartsNav.PushViewController (_tabs._jobTubingUpgrade, false);			
		}

		public void GoHDTubingUpgrade()
		{
			_tabs.SelectedViewController = _tabs.ViewControllers[2];
			if (_tabs.UsedPartsNav.ViewControllers.Length > 1) _tabs.UsedPartsNav.PopToRootViewController (false);
			_tabs.UsedPartsNav.PushViewController (_tabs._jobHDTubingUpgrade, false);			
		}
		
		public void GoUnitUpgrade()
		{
			_tabs.SelectedViewController = _tabs.ViewControllers[2];		
			if (_tabs.UsedPartsNav.ViewControllers.Length > 1)  _tabs.UsedPartsNav.PopToRootViewController (false);
			_tabs.UsedPartsNav.PushViewController (_tabs._jobUnitUpgrade, false);			
		}

		public void GoService()
		{	
			_tabs.SelectedViewController = _tabs.ViewControllers[1];
			if (_tabs.ServiceNav.ViewControllers.Length == 0)
				_tabs.ServiceNav.PushViewController (_tabs._jobService, false);
		}
		
		public void GoInstall()
		{
			// here we must push an install view controller onto UsedPartsNav and select the appropriate tab
			_tabs.SelectedViewController = _tabs.ViewControllers[2];
			if (_tabs.UsedPartsNav.ViewControllers.Length > 1)  _tabs.UsedPartsNav.PopToRootViewController (false);
			_tabs.UsedPartsNav.PushViewController (_tabs._jobInstall, false);
		}
		
		public void GoUninstall()
		{
			// here we must push an uninstall view controller onto UsedPartsNav and select the appropriate tab
			_tabs.SelectedViewController = _tabs.ViewControllers[2];	
			if (_tabs.UsedPartsNav.ViewControllers.Length > 1)  _tabs.UsedPartsNav.PopToRootViewController (false);
			_tabs.UsedPartsNav.PushViewController (_tabs._jobUninstall, false);

		}

		public void GoReinstall()
		{
			// here we must push a re-install view controller onto UsedPartsNav and select the appropriate tab
			_tabs.SelectedViewController = _tabs.ViewControllers[2];	
			if (_tabs.UsedPartsNav.ViewControllers.Length > 1)  _tabs.UsedPartsNav.PopToRootViewController (false);
			_tabs.UsedPartsNav.PushViewController (_tabs._jobReinstall, false);			
		}

		public void GoDelivery()
		{
			// we must proceed to the payment screen directly
			_tabs.SelectedViewController = _tabs.ViewControllers[2];
			if (_tabs.UsedPartsNav.ViewControllers.Length > 1)  _tabs.UsedPartsNav.PopToRootViewController (false);
			_tabs.UsedPartsNav.PushViewController (_tabs._jobDelivery, false);			
		}
		
		public WorkflowNavigationController (DetailedTabs tabs) 
		// this controller is deprecated and is used as a container for methods that should belong to DetailedTabs class
		{
			this._tabs = tabs;
			this.ChosenPart = new Part();
			
			this.Title = NSBundle.MainBundle.LocalizedString ("Workflow", "Workflow");
			using (var image = UIImage.FromBundle ("Images/103-map") ) this.TabBarItem.Image = image;

			this.NavigationBar.BarStyle = UIBarStyle.Black;
			this.NavigationBar.Translucent = true;
			this.NavigationBar.Hidden = true; 

			this.Toolbar.BarStyle = UIBarStyle.Black;
			this.Toolbar.Translucent = true;
			this.ToolbarHidden = false;

			_refusedToPay = delegate{
				UIAlertView alert = new UIAlertView("Did the customer refuse to pay for the job?", "", null, "Yes", "No, never mind");
				alert.WillDismiss += HandleRefusedToPayWillDismiss;
				alert.Show ();				
			};
			
			_clearJobsList = delegate {
				// This clears the job list (except the main job, of course)
				if (TopViewController is PaymentViewController)
					(TopViewController as PaymentViewController).Summary.ClearChildJobs ();
			};
			
			_extraJobs = delegate(object _sender, EventArgs _e) {
				// first of all we must ensure that the user wants to commit this action
				// to do this, we bring up a dialog which asks for a job type
				// if he presses "Cancel", we won't do anything
				// if he commits and chooses a job type, we proceed
				
				// replaced basic alert with a subclass that displays obly those job types that the current employee can perform
				// the job types are stored in JOB_TYPES database table

				if (JobRunTable.JobTypes == null)
					JobRunTable.JobTypes = MyConstants.GetJobTypesFromDB ();
				UIActionSheet alert = new UIActionSheet("Choose a job type", null, null, null);
				foreach(JobType jt in JobRunTable.JobTypes)
				{
					if (jt.CanDo) {
						alert.AddButton (jt.Description);
					}
				}
				alert.Dismissed += delegate(object sender, UIButtonEventArgs e) {
					if (e.ButtonIndex != alert.CancelButtonIndex) {
						Job currentJob = _tabs._jobRunTable.CurrentJob;
						string chosenTypeDescription = alert.ButtonTitle(e.ButtonIndex);
						
						// whatever job type the user has chosen, we should check if one already exists in the cluster
						Job mainJob = ( currentJob.HasParent () ) ? _tabs._jobRunTable.FindParentJob(currentJob) : currentJob;
						
						bool ok = true;
						foreach(Job j in mainJob.ChildJobs)
							if (j.Type.Description == chosenTypeDescription || ( 
							      (j.Type.Code == "FRC" || j.Type.Code == "FIL") && 
							      (chosenTypeDescription == "Rainwater filter change" || chosenTypeDescription == "Filter change")
							)) { ok = false; break; }
						if (!ok || mainJob.Type.Description == chosenTypeDescription || 
						    ((mainJob.Type.Code == "FIL" || mainJob.Type.Code == "FRC") && (chosenTypeDescription == "Rainwater filter change" || chosenTypeDescription == "Filter change")))
						{
							// if it does exist, we should deny the user's request
							var jobTypeExists = new UIAlertView("A job of the chosen type already exists in the job list", "You should add or change information there", null, "Ah, right");
							jobTypeExists.Show ();
							return;
						}
						
						// now that the user committed to creating a new job, we should save the results of current job to database (except for payment data, which should only be saved when the last job in the cluster is saved)
						currentJob = _tabs._jobRunTable.CurrentJob;
						//  Why save job results before the workflow is over? Apparently, it was done to avoid losing the jobs created when the app restarts in the middle of data input
						SaveJobResultsToDatabase (currentJob, false); // false = payment does not get saved (since this is presumed to be not the last job in the cluster)
						// create a new job object instance
						Job newJob = new Job(true); // true = sets the new job instance's booking number to some unique number (unique within the current device's database)
						
						// set its parent job property to current job (selected in JobRunTable on the left-hand side)						
						newJob.ParentJobBookingNumber = (currentJob.ParentJobBookingNumber <= 0) ? currentJob.JobBookingNumber : currentJob.ParentJobBookingNumber;
						
						// copy what we can from parent, set everything else to defaults (like money to collect from customer)
						newJob.CustomerNumber = currentJob.CustomerNumber;
						newJob.JobDone = false;
						newJob.Started = MyConstants.JobStarted.Yes;
						newJob.JobBookedOn = currentJob.JobBookedOn;
						newJob.JobDate = currentJob.JobDate;
						newJob.UnitNumber = currentJob.UnitNumber;
						newJob.JobPlumbingComments = "";
						newJob.JobSpecialInstructions = "";
						newJob.JobTime = currentJob.JobTime;
						newJob.Payments = new List<JobPayment> { new JobPayment(newJob.CustomerNumber, newJob.JobBookingNumber) };
						newJob.UsedParts = new List<Part>();
						
						// set the new job's type
						// replace the line above with a switch statement that determines the job type according to the button pressed

						foreach (JobType jt in JobRunTable.JobTypes)
						{
							if (jt.Description == chosenTypeDescription)
							{
								newJob.SetJobType (jt.Code);
								newJob.MoneyToCollect = newJob.Type.RetailPrice;
								newJob.Payments[0].Amount = newJob.Type.RetailPrice;
								break;
							}
						}
						// set the JobRunTable.CurrentJob to this newly created job instance
						if ( currentJob.HasNoParent() )
						{
							if (currentJob.ChildJobs != null) currentJob.ChildJobs.Add (newJob);
							else currentJob.ChildJobs = new List<Job> { newJob };
						}
						else 
						{ // current job has a parent, therefore this newly created job should be added to that parent job's list of child jobs
							Job main = _tabs._jobRunTable.FindParentJob (newJob);
							main.ChildJobs.Add (newJob);
						} 
						_tabs._jobRunTable.CurrentJob = newJob;
						// save the job to database (into PL_RECOR table)
						SaveJobToPLRecor (newJob);
					
						// start the workflow for this job, skipping the pre-plumbing check
						StartWorkflowForJob(newJob);	
					}
					else { // the user cancelled, so we should not bother to do anything
						return;
					}		
				};
				alert.ShowInView (this._tabs.View); // .Show ();
			};
			
			_finishWorkflow = delegate {

				Customer c = _tabs._jobRunTable.CurrentCustomer;
				if (c.DepositAmount > 0) {
					c.DepositUsed = c.DepositAmount;
					c.DepositAmount = 0;
				}

				// workflow has been finished normally, which means that the jobs in current job cluster have been performed and (hopefully) paid for
				Job selectedJob = _tabs._jobRunTable.CurrentJob;
				Job main;

				if (selectedJob.HasParent())
				{
					main = _tabs._jobRunTable.FindParentJob (selectedJob);
					_tabs._jobRunTable.CurrentJob = main;

					// found the main (booked) job
					// copy all payment data from the current job INCLUDING the amount 
					main.Payments = selectedJob.Payments;
					selectedJob = main; // switch to the main job which will be saved first
				}
				else main = selectedJob;
				
				if (main.Started == MyConstants.JobStarted.None) 
					main.Started = MyConstants.JobStarted.Yes;
				main.JobDone = true;
				SaveJobResultsToDatabase(main, true);		// IMPLEMENTED :: save job results to database here, true = save payment data
				
				foreach(Job childJob in main.ChildJobs)		// save results of child jobs in the job cluster
				{
					childJob.Started = MyConstants.JobStarted.Yes;
					childJob.JobDone = true;
					SaveJobResultsToDatabase (childJob, false);
				}

				tabs._app.myLocationDelegate.GeocodeLastKnownLocation(); 		// this also sends checkpoint data to TestFlight: customer number, coordinates, geocoded address of the location
				tabs._app.myLocationDelegate.DumpLocationsBufferToDatabase ();	// this will write locations buffer into database in a thread-safe way

				// reset the signing nav controller
				_tabs.SigningNav.PopToRootViewController (false);
				
				// check if every job in the table has been done
				bool allDone = true;
				foreach(Job j in _tabs._jobRunTable.MainJobList) {
					if (j.JobDone == false) { allDone = false; break; }
				}
				if (allDone)
				{
					foreach (Job j in _tabs._jobRunTable.UserCreatedJobs)
					{
						if (j.JobDone == false) { allDone = false; break; }
					}
				}

				_tabs._jobRunTable.AllJobsDone = allDone;
				this._tabs._scView.Log (String.Format ("Workflow finished for Job ID: {0}", _tabs._jobRunTable.CurrentJob.JobBookingNumber));

				ResetToDefaultView();	// return to the default view : customer tab selected, navigation buttons, toolbar buttons, tab pictures, all tabs enabled
				ResetViewControllersToDefaults(false);	// clear all user data that is still maintained in view controllers
			};
			
			_resetWorkflow = delegate {
				ResetViewControllersToDefaults(true);	// clear all user data that is still maintained in view controllers
				ResetUserCreatedJobs();				// clear all data about user created jobs
				if (_tabs._jobRunTable.CurrentJob != null) 
				{
					_tabs._jobRunTable.CurrentJob.NotDoneComment = "";
					_tabs._jobRunTable.CurrentJob.Started = MyConstants.JobStarted.None;
					if (_tabs._jobRunTable.CurrentJob.Payments != null) 
						_tabs._jobRunTable.CurrentJob.Payments.Clear ();
					_tabs._scView.Log (String.Format ("Workflow reset for Job ID: {0}", _tabs._jobRunTable.CurrentJob.JobBookingNumber));
				}
				UIView.BeginAnimations (null);
				UIView.SetAnimationDuration (0.3f);
				_tabs.Mode = DetailedTabsMode.Lookup;
				UIView.CommitAnimations ();
				
				ResetToDefaultView();	// return to the default view : customer tab selected, navigation buttons, toolbar buttons, tab pictures, all tabs enabled
			};
			
			_proceedToSignPrePlumbing = delegate {
				_tabs._prePlumbView.ProceedToSign ();
			};
			
			_startSigning = delegate {
				this._tabs.TabBar.UserInteractionEnabled = false;
				this._tabs._jobRunTable.TableView.UserInteractionEnabled = false;
				LeftButton = new UIBarButtonItem("Done", UIBarButtonItemStyle.Done, FinishSigning);
				RightButton.Enabled = false;
				this.Toolbar.SetItems (new UIBarButtonItem[] { LeftButton, FlexibleButtonSpace, RightButton }, true);
			};
			
			_finishSigning = delegate {
				this._tabs.TabBar.UserInteractionEnabled = true;
				// this._tabs._signView.SigningMode = false;
				LeftButton = new UIBarButtonItem("Start signing", UIBarButtonItemStyle.Bordered, StartSigning);
				this.Toolbar.SetItems (new UIBarButtonItem[] { LeftButton, FlexibleButtonSpace, RightButton }, true);				
			};
			
			_proceedAfterSigningPrePlumbing = delegate {
				ProceedToBookedJobType (this,null);
			};
			
			_chooseActionsTaken = delegate {
				(this.TopViewController as JobServiceCallViewController).ChooseActionsTaken ();
			};
			
			_clearProblemsList = delegate {
				// (this.TopViewController as JobServiceCallViewController).ClearProblemsList ();
				// the above line is not enough, because sometimes we want to clear problems list when the top view controller is a ProblemsDialogViewController
				foreach(UIViewController vc in this.ViewControllers)
				{
					if (vc.GetType ().Name == "JobServiceCallViewController")
					{
						(vc as JobServiceCallViewController).ClearProblemsList ();
					}
				}
			};
			
			_clearAdditionalPartsList = delegate {
				(this.TopViewController as UsedPartsViewController).ClearPartsList ();
			};
			
			_proceedToSignServiceReport = delegate {
				// _tabs._signView.Mode = SignableDocuments.ServiceReport;
				// PushViewController(_tabs._signView, true);
				_tabs._jobService.GenerateServicePDFPreview();		// creates a service call report pdf preview file here (unsigned pdf)
				_tabs._jobService.RedrawServiceCallPDF(false);		// draws a pdf in the signature capturing view controller
			};
			
			_proceedAfterSigningServiceReport = delegate {
				PushViewController (_tabs._serviceParts, true);
			};
			
			_proceedToPayment = delegate {
				 PushViewController (_tabs._payment, true);
			};
			
			_leftButton = new UIBarButtonItem("No pre-plumbing problems", UIBarButtonItemStyle.Done, ProceedToBookedJobType);
			_flexibleButtonSpace = new UIBarButtonItem(UIBarButtonSystemItem.FlexibleSpace, null, null);
			_rightButton = new UIBarButtonItem("Leave workflow", UIBarButtonItemStyle.Bordered, _resetWorkflow);
			_buttons = new UIBarButtonItem[] { _leftButton, _flexibleButtonSpace, _rightButton };
		}

		public void SaveJobToPLRecor(Job j)
		{
			_tabs._scView.Log (String.Format ("Saving child job to pl_RECOR: ID = {0}", j.JobBookingNumber));

			Job mainJob = (j.HasParent()) ? _tabs._jobRunTable.FindParentJob(j) : j;

			using (var connection = new SqliteConnection("Data Source="+ServerClientViewController.dbFilePath) )
			{
				connection.Open();
				var cmd = connection.CreateCommand();
				
				cmd.CommandText = "INSERT INTO Pl_recor (CUSNUM, PLAPPDATE, TYPE, PAY_PL, SHEETTYPE, SHEETT, SUBURB, TIME, PLNUM, TIMEENTERED, BOOKNUM, REPNUM, " +
					"JDONE, UNITNUM, INSTALLED, CODE, RUN, REBOOKED, OCODE, WARRANTY, NOSEARCH, PARENTNUM, ATTENTION, TIME_START, TIME_END) " +
					"Values (?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?)";
				cmd.Parameters.Add ("CUSNUM", DbType.Int32).Value = j.CustomerNumber;
				cmd.Parameters.Add ("PLAPPDATE", DbType.String).Value = j.JobDate.ToString ("yyyy-MM-dd");
				cmd.Parameters.Add ("TYPE", DbType.String).Value = j.Type.Code;
				cmd.Parameters.Add ("PAY_PL", DbType.Double).Value = j.Payments[0].Amount;
				cmd.Parameters.Add ("SHEETTYPE", DbType.String).Value = "Custom";
				cmd.Parameters.Add ("SHEETT", DbType.Int32).Value = -1;
				cmd.Parameters.Add ("SUBURB", DbType.String).Value = _tabs._jobRunTable.CurrentCustomer.Suburb; // could be taken from customer address
				cmd.Parameters.Add ("TIME", DbType.String).Value = mainJob.JobTime.ToString ("yyyy-MM-dd HH:mm:ss");
				cmd.Parameters.Add ("PLNUM", DbType.Int32).Value = (MyConstants.EmployeeType == MyConstants.EmployeeTypes.Plumber) ? MyConstants.EmployeeID : 0;
				cmd.Parameters.Add ("TIMEENTERED", DbType.String).Value = DateTime.Now.ToString ("yyyy-MM-dd HH:mm:ss");
				cmd.Parameters.Add ("BOOKNUM", DbType.Int64).Value = j.JobBookingNumber;
				cmd.Parameters.Add ("REPNUM", DbType.Int32).Value = (MyConstants.EmployeeType == MyConstants.EmployeeTypes.Franchisee) ? MyConstants.EmployeeID : 0;
				cmd.Parameters.Add ("JDONE", DbType.Int32).Value = 0;
				cmd.Parameters.Add ("UNITNUM", DbType.Int32).Value = j.UnitNumber;
				cmd.Parameters.Add ("INSTALLED", DbType.String).Value = "";
				cmd.Parameters.Add ("CODE", DbType.Int32).Value = 0;
				cmd.Parameters.Add ("RUN", DbType.Int32).Value = 0;
				cmd.Parameters.Add ("REBOOKED", DbType.Int32).Value = 0;
				cmd.Parameters.Add ("OCODE", DbType.Int32).Value = 0;
				cmd.Parameters.Add ("WARRANTY", DbType.Int32).Value = Convert.ToInt16(j.Warranty);
				cmd.Parameters.Add ("NOSEARCH", DbType.Int32).Value = 0;
				cmd.Parameters.Add ("PARENTNUM", DbType.Int64).Value = j.ParentJobBookingNumber;
				cmd.Parameters.Add ("ATTENTION", DbType.Byte).Value = 0;
				cmd.Parameters.Add ("TIME_START", DbType.String).Value = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
				cmd.Parameters.Add ("TIME_END", DbType.String).Value = DateTime.Now.AddMinutes(15).ToString("yyyy-MM-dd HH:mm:ss");

				cmd.ExecuteNonQuery ();
			}
			_tabs._scView.Log (string.Format("Saved child job: ID = {0}", j.JobBookingNumber));
			return;
		}
		
		public void StartWorkflowForJob(Job j)
		{
			j.ShouldPayFee = true;
			_tabs.LastSelectedTab = _tabs.SelectedIndex;
			switch (j.Type.Code)
			{	
				case "FIL": // Filter change
				{
					_tabs._jobFilter.ThisJob = _tabs._jobRunTable.CurrentJob;
					_tabs._jobFilter.ClearPartsList ();
					GoFilterChange ();
					break; 
				}
				case "FRC": // Rainwater filter change
				{
					_tabs._jobFilter.ThisJob = _tabs._jobRunTable.CurrentJob;
					_tabs._jobFilter.ClearPartsList ();
					GoFilterChange ();
					break;
				}
				case "TWI": // Installation
				{ 
					_tabs._jobInstall.ThisJob = _tabs._jobRunTable.CurrentJob;
					_tabs._jobInstall.ClearPartsList ();
					GoInstall ();
					break; 
				}

				case "REI": // Re-installation
				{
					_tabs._jobReinstall.ThisJob = _tabs._jobRunTable.CurrentJob;
					_tabs._jobReinstall.ClearPartsList ();
					GoReinstall();
					break;
				}
				case "SER": // Service
				{ 
					j.ShouldPayFee = false;
					GoService ();
					break; 
				}
				case "UNI": // Uninstall
				{
					_tabs._jobUninstall.ThisJob = _tabs._jobRunTable.CurrentJob;
					_tabs._jobUninstall.ClearPartsList ();
					GoUninstall ();
					break;
				}
				case "NEWTAP": // New tap
				{
					_tabs._jobNewTap.ThisJob = _tabs._jobRunTable.CurrentJob;
					_tabs._jobNewTap.ClearPartsList ();
					GoNewTap ();
					break;
				}
				case "UP":
				{
					_tabs._jobUnitUpgrade.ThisJob = _tabs._jobRunTable.CurrentJob;
					_tabs._jobUnitUpgrade.ClearPartsList ();
					GoUnitUpgrade();
					break;
				}
				case "TUBING": // Tubing upgrade
				{
					_tabs._jobTubingUpgrade.ThisJob = _tabs._jobRunTable.CurrentJob;
					_tabs._jobTubingUpgrade.ClearPartsList ();
					GoTubingUpgrade();
					break;
				}
				case "TUBINGUPGR": // Tubing upgrade
				{
					_tabs._jobTubingUpgrade.ThisJob = _tabs._jobRunTable.CurrentJob;
					_tabs._jobTubingUpgrade.ClearPartsList ();
					GoTubingUpgrade();
					break;
				}
				case "HDTUBING": // HD tubing upgrade
				{
					_tabs._jobHDTubingUpgrade.ThisJob = _tabs._jobRunTable.CurrentJob;
					_tabs._jobHDTubingUpgrade.ClearPartsList ();
					GoHDTubingUpgrade();
					break;
				}
				case "DLV": // Delivery
				{
					_tabs._jobDelivery.ThisJob =  _tabs._jobRunTable.CurrentJob;
					GoDelivery ();
					break;
				}

				default : // Unknown job type -- inform the user
				{
					var unknownJobTypeAlert = new UIAlertView("Unknown job type", "Please select a valid job type by tapping the appropriate button in the bottom left corner of this page.", null, "OK");
					unknownJobTypeAlert.Show ();
					break;
				}
			}
		}
		
		
		void HandleRefusedToPayWillDismiss (object sender, UIButtonEventArgs e)
		{
			if (e.ButtonIndex==0) {
				Job selectedJob = _tabs._jobRunTable.CurrentJob;
				Job mainJob = new Job(false);
				if (selectedJob.HasNoParent ()) mainJob = selectedJob;
				else {
					foreach (Job j in _tabs._jobRunTable.MainJobList)
					{
						if (j.JobBookingNumber == selectedJob.ParentJobBookingNumber)
						{
							mainJob = j;
							break;
						}
					}
				}
				// found the main job, process it
				mainJob.Started = MyConstants.JobStarted.Yes;
				mainJob.JobDone = true;
				// mainJob.Payment.Received = false;
				// mainJob.Payment.Type = PaymentTypes.RefusedToPay;
				
				SaveJobResultsToDatabase(mainJob, true);
				// process child jobs
				foreach(Job childJob in mainJob.ChildJobs)
				{
					childJob.Started = MyConstants.JobStarted.Yes;
					childJob.JobDone = true;
					// childJob.Payment.Type = PaymentTypes.RefusedToPay;
					SaveJobResultsToDatabase (childJob, true);
				}
				
				_tabs._jobRunTable.TableView.ReloadData ();
				
				ResetViewControllersToDefaults(false);
				ResetToDefaultView();
				
				// check if every job in the table has been done
				bool allDone = true;
				foreach(Job j in _tabs._jobRunTable.MainJobList) {
					if (j.JobDone == false) allDone = false; 
				}
				_tabs._jobRunTable.AllJobsDone = allDone;
			}
		}
		
		public void ResetViewControllersToDefaults(bool resetInstallFees)
		{
			_tabs._jobFilter.ClearPartsList ();
			_tabs._serviceParts.ResetToDefaults ();
			_tabs._jobService.ResetToDefaults ();
			_tabs._jobInstall.ResetToDefaults (resetInstallFees);
			_tabs._jobUninstall.ClearPartsList ();

			_tabs._prePlumbView.ResetChoices ();
			_tabs._payment.ResetToDefaults ();
			
			_tabs.SetNavigationButtons (NavigationButtonsMode.CustomerDetails);
			_tabs.BtnEdit.Enabled = true;
		}
		
		public void ResetUserCreatedJobs()
		{
			Job curj = _tabs._jobRunTable.CurrentJob;
			if (curj == null)
				return;

			if (curj.HasParent ()) { // this is a child job, we should find its parent, loop through the list of child jobs and erase everything
				foreach (Job j in _tabs._jobRunTable.MainJobList) {
					if (j.JobBookingNumber == curj.ParentJobBookingNumber) {	// erase all child job results from database
						foreach (Job childJob in j.ChildJobs) {
							EraseChildJobFromDatabase (childJob);
						}
						j.ChildJobs.Clear ();	// clear list of child jobs
						break;
					}
				}				
			} else { // this is a main job, erase all child job results
				foreach (Job childJob in curj.ChildJobs) {
					EraseChildJobFromDatabase (childJob);
				}
			}
			curj.ChildJobs.Clear ();
		}
		
		public void ResetToDefaultView()
		{
			UIView.SetAnimationDuration (0.3f);
			UIView.BeginAnimations (null);

			_tabs.MyNavigationBar.Hidden = false;
			_tabs.Mode = DetailedTabsMode.Lookup;
			_tabs.SelectedViewController = _tabs.ViewControllers[0]; // selects Customer Details view controller (leftmost tab)
			_tabs._jobRunTable.TableView.UserInteractionEnabled = true;

			if (_tabs._jobRunTable.LastSelectedRowPath != null)
			{
				_tabs._jobRunTable.TableView.ReloadRows (new NSIndexPath[] { _tabs._jobRunTable.LastSelectedRowPath }, UITableViewRowAnimation.Automatic);
				_tabs._jobRunTable.TableView.SelectRow ( _tabs._jobRunTable.LastSelectedRowPath, true, UITableViewScrollPosition.None );
			}

			UIView.CommitAnimations ();
		}
		
		public void SetToolbarButtons(WorkflowToolbarButtonsMode mode)
		{
			switch (mode) {
				case WorkflowToolbarButtonsMode.PrePlumbingCheck: 
				
					UIBarButtonItem _centerButton = new UIBarButtonItem("Proceed to sign", UIBarButtonItemStyle.Bordered, _proceedToSignPrePlumbing);
				
					_leftButton = new UIBarButtonItem("No pre-plumbing problems", UIBarButtonItemStyle.Done, ProceedToBookedJobType);
					_flexibleButtonSpace = new UIBarButtonItem(UIBarButtonSystemItem.FlexibleSpace, null, null);
					_rightButton = new UIBarButtonItem("Leave workflow", UIBarButtonItemStyle.Bordered, _resetWorkflow);
					UIBarButtonItem proceedToChooseJobType = new UIBarButtonItem("Change job type", UIBarButtonItemStyle.Bordered, ChangeJobType);
					_buttons = new UIBarButtonItem[] { _leftButton, _flexibleButtonSpace, proceedToChooseJobType, _flexibleButtonSpace, _centerButton, _flexibleButtonSpace, _rightButton };
					this.Toolbar.SetItems (_buttons, true);
				
					this.NavigationBar.TopItem.SetRightBarButtonItems (new UIBarButtonItem[] { 
							_tabs.BtnEdit, _tabs.BtnStuff	}, false);
				
					return;
				
				case WorkflowToolbarButtonsMode.SigningPrePlumbingCheck:
					_leftButton = new UIBarButtonItem("Start signing", UIBarButtonItemStyle.Done, _startSigning);
					_flexibleButtonSpace = new UIBarButtonItem(UIBarButtonSystemItem.FlexibleSpace, null, null);
					_rightButton = new UIBarButtonItem("Proceed", UIBarButtonItemStyle.Bordered, _proceedAfterSigningPrePlumbing);
					_rightButton.Enabled = false;
					_buttons = new UIBarButtonItem[] { _leftButton, _flexibleButtonSpace, _rightButton };
					this.Toolbar.SetItems (_buttons, true);
					return;
				
				case WorkflowToolbarButtonsMode.ServiceCallDetails: 
					_leftButton = new UIBarButtonItem("Proceed to signing", UIBarButtonItemStyle.Bordered, _proceedToSignServiceReport);
					_flexibleButtonSpace = new UIBarButtonItem(UIBarButtonSystemItem.FlexibleSpace, null, null);
					UIBarButtonItem _extraButton = new UIBarButtonItem("Clear problems list", UIBarButtonItemStyle.Bordered, _clearProblemsList);
					_rightButton = new UIBarButtonItem("Choose actions taken", UIBarButtonItemStyle.Done, _chooseActionsTaken);					
					_buttons = new UIBarButtonItem[] { _leftButton, _flexibleButtonSpace, _extraButton, _rightButton };
					this.Toolbar.SetItems (_buttons, true);
					return;
				
				case WorkflowToolbarButtonsMode.ServiceCallProblemsList:
					_leftButton = new UIBarButtonItem("Proceed to signing", UIBarButtonItemStyle.Done, _proceedToSignServiceReport);
					_flexibleButtonSpace = new UIBarButtonItem(UIBarButtonSystemItem.FlexibleSpace, null, null);
					UIBarButtonItem _clearButton = new UIBarButtonItem("Clear problems list", UIBarButtonItemStyle.Bordered, _clearProblemsList);
					foreach(var vc in this.ViewControllers)
					{
						if (vc.GetType ().Name == "JobServiceCallViewController")	_leftButton.Enabled = (vc as JobServiceCallViewController).ProceedToSigningEnabled;
					}
					_buttons = new UIBarButtonItem[] { _leftButton, _flexibleButtonSpace, _clearButton };
					this.Toolbar.SetItems (_buttons, true);
					return;
				
				case WorkflowToolbarButtonsMode.SigningServiceCallReport:
					_leftButton = new UIBarButtonItem("Start signing", UIBarButtonItemStyle.Done, _startSigning);
					_flexibleButtonSpace = new UIBarButtonItem(UIBarButtonSystemItem.FlexibleSpace, null, null);
					_rightButton = new UIBarButtonItem("Select used parts", UIBarButtonItemStyle.Bordered, _proceedAfterSigningServiceReport);
					_rightButton.Enabled = false;
					_buttons = new UIBarButtonItem[] { _leftButton, _flexibleButtonSpace, _rightButton };
					this.Toolbar.SetItems (_buttons, true);
					return;				
				
				case WorkflowToolbarButtonsMode.Default:
					_leftButton = new UIBarButtonItem("No pre-plumbing problems", UIBarButtonItemStyle.Done, ChangeJobType);
					_flexibleButtonSpace = new UIBarButtonItem(UIBarButtonSystemItem.FlexibleSpace, null, null);
					_rightButton = new UIBarButtonItem("Leave workflow", UIBarButtonItemStyle.Bordered, _resetWorkflow);
					_buttons = new UIBarButtonItem[] { _leftButton, _flexibleButtonSpace, _rightButton };
					this.Toolbar.SetItems (_buttons, true);
					return;
			
				case WorkflowToolbarButtonsMode.FilterChange:
					_leftButton = new UIBarButtonItem("Done", UIBarButtonItemStyle.Done, _proceedToPayment);
					_flexibleButtonSpace = new UIBarButtonItem(UIBarButtonSystemItem.FlexibleSpace, null, null);
					_rightButton = new UIBarButtonItem("Clear list", UIBarButtonItemStyle.Bordered, _clearAdditionalPartsList);
					_buttons = new UIBarButtonItem[] { _leftButton, _flexibleButtonSpace, _rightButton };
					this.Toolbar.SetItems (_buttons, true);
					return;
				
				case WorkflowToolbarButtonsMode.Payment:
					_leftButton = new UIBarButtonItem("Finish", UIBarButtonItemStyle.Done, _finishWorkflow);
					_flexibleButtonSpace = new UIBarButtonItem(UIBarButtonSystemItem.FlexibleSpace, null, null);
					_rightButton = new UIBarButtonItem("Refused to pay", UIBarButtonItemStyle.Bordered, _refusedToPay);				
					var extraJobButton = new UIBarButtonItem("Add another job", UIBarButtonItemStyle.Bordered, _extraJobs);
				
					_buttons = new UIBarButtonItem[] { _leftButton, _flexibleButtonSpace, extraJobButton, _flexibleButtonSpace, _rightButton };
					this.Toolbar.SetItems (_buttons, true);					
					return;
				
				case WorkflowToolbarButtonsMode.JobSummary:
					_leftButton = new UIBarButtonItem("Loyalty", UIBarButtonItemStyle.Done, null);
					_flexibleButtonSpace = new UIBarButtonItem(UIBarButtonSystemItem.FlexibleSpace, null, null);
					_rightButton = new UIBarButtonItem("Clear jobs list", UIBarButtonItemStyle.Bordered, _clearJobsList);
					_buttons = new UIBarButtonItem[] { _leftButton, _flexibleButtonSpace, _rightButton };
					this.Toolbar.SetItems (_buttons, true);
					return;
				
				
				case WorkflowToolbarButtonsMode.Installation:
					_leftButton = new UIBarButtonItem("Done", UIBarButtonItemStyle.Done, _proceedToPayment);
					_flexibleButtonSpace = new UIBarButtonItem(UIBarButtonSystemItem.FlexibleSpace, null, null);
					_rightButton = new UIBarButtonItem("Clear list", UIBarButtonItemStyle.Bordered, _clearAdditionalPartsList);
					_buttons = new UIBarButtonItem[] { _leftButton, _flexibleButtonSpace, _rightButton };
					this.Toolbar.SetItems (_buttons, true);
					return;
			}
		}
		
		public void SaveJobResultsToDatabase(Job j, bool savePayment)
		{
			// we know the Job's ID which is j.BookingNumber
			// we know parts as shown below
			// we should save them to an appropriate database tables (Pl_Recor, Used_Parts, Payments)
			// IMPLEMENTED: if that job was a service call, we save the job report in JOB_REPORTS
			if (File.Exists (ServerClientViewController.dbFilePath) )
			{
				// connect to database
				using (var connection = new SqliteConnection("Data Source="+ServerClientViewController.dbFilePath) )
				{
					using (var cmd = connection.CreateCommand())
					{
						connection.Open();
						
						// set job results data				
						string jResult = "Result";
						if (j.Started == MyConstants.JobStarted.Yes) {

							// remove any records in FOLLOWUPS with reasonID = 10 
							// there could be some if user selects "job not done" first, then finishes job workflow
							cmd.CommandText = "DELETE FROM FOLLOWUPS WHERE Job_ID = ? AND Reason_ID = 10";
							cmd.Parameters.Clear ();
							cmd.Parameters.Add ("@JobID", System.Data.DbType.Int64).Value = j.JobBookingNumber;
							cmd.ExecuteNonQuery ();

							switch(j.Type.Code) {
								case "INS": { jResult = "Installed "; break; }
								case "SIN": { jResult = "Installed "; break; }
								case "TWI": { jResult = "Installed "; break; }
								case "REI": { jResult = "Installed "; break; }
								case "UP": { jResult = "Upgraded  "; break; }
								case "TUBING": { jResult = "Upgraded"; break; }
								case "TUBINGUPGR": { jResult = "Upgraded"; break; }
								case "HDTUBING": { jResult = "Upgraded"; break; }
								case "NEWTAP": { jResult = "New Tap"; break; }
								case "UNI": { jResult = "Uninstall "; break; }
								case "FIL": { jResult = "Changed   "; break; }
								case "MIL": { jResult = "Changed   "; break; }
								case "FRC": { jResult = "Changed   "; break; }
								case "SER": { jResult = "Service Done"; break; }
								case "DLV": { jResult = "Delivered "; break; }
							}
						}
						else {
							switch (j.Started) {
							case MyConstants.JobStarted.AddressWrong : 			jResult = "Wrong address"; break;
							case MyConstants.JobStarted.CustomerNotAtHome : 	jResult = "Not home"; break;
							case MyConstants.JobStarted.CustomerCancelled : 	jResult = "Customer cancelled"; break;
							case MyConstants.JobStarted.PuratapLate : 			jResult = "Late to site"; break;
							case MyConstants.JobStarted.Other :					jResult = "No: other"; break;
							default : jResult = "Not done"; break;
							}
						}
						
						// Saving payment data may overwrite the price, so we save it first in case it does
						cmd.CommandText = "SELECT SUM(Pay_Pl) AS Amount FROM PL_RECOR WHERE CusNum = ? AND (BookNum = ? OR ParentNum = ?)";
						cmd.Parameters.Clear ();
						cmd.Parameters.Add ("@CustomerID", System.Data.DbType.Int64).Value = j.CustomerNumber;
						cmd.Parameters.Add ("@JobID", System.Data.DbType.Int64).Value = j.JobBookingNumber;
						
							// for child jobs we add the parent job's number as the third parameter
						if (j.HasParent ()) cmd.Parameters.Add ("@ParentJobID", System.Data.DbType.Int64).Value = j.ParentJobBookingNumber;
							// for main jobs, we add an arbitrary large negative number as parent job's number (should not affect query results in any way)
						else cmd.Parameters.Add ("@ParentJobID", System.Data.DbType.Int64).Value = -999999;
						
						// First we find the amount currently stored for this job cluster
						double previousAmount = 0;
						using (var reader = cmd.ExecuteReader () )
						{
							if (reader.HasRows)
							{	
								previousAmount = (double)reader["amount"]; 
							}
							else
							{
								// something weird has happened, should never get to the line below
								_tabs._scView.Log (String.Format ("This cannot be. Could not determine the price for JobID: {0}", j.JobBookingNumber));
							}
						}
						
						// Now we should check if the amount we are going to store is different from the current total
						double total;
						long mainBookingNumber;
						if (j.HasParent ()) 
						{
							total = this._tabs._jobRunTable.FindParentJob (j).TotalToCollect ();
							mainBookingNumber = j.ParentJobBookingNumber;
						}
						else 
						{
							total = j.TotalToCollect ();
							mainBookingNumber = j.JobBookingNumber;
						}
						
						if (previousAmount != total )
						{
							// the amounts are different, we should make a new record in CustUpdate and store that information
							_tabs._customersView.UpdateCustomerInfo (CustomerDetailsUpdatableField.JobPriceTotal, previousAmount.ToString(), total.ToString(), j.CustomerNumber, mainBookingNumber);
						}
						else 
						{
							// the money to collect did not change, so there's nothing to save as nothing will be overwritten
						}
						
						string sql = 	"UPDATE Pl_recor" 	+
											" SET jdone = ?, " 	+ 
											" pay_pl = ?, " 		+
											" installed = ?, " 	+
											" warranty = ? "		+
											" WHERE cusnum = " + j.CustomerNumber.ToString() +
												" AND booknum = " + j.JobBookingNumber.ToString ();
						cmd.CommandText = sql;
						cmd.Parameters.Clear ();
						cmd.Parameters.Add ("@JobDone", System.Data.DbType.Int32).Value = Convert.ToInt32 (j.JobDone);	// save whether the job was done
						cmd.Parameters.Add ("@Price", DbType.Double).Value = j.MoneyToCollect;	// save how much we expected to receive for that job
						cmd.Parameters.Add("@Installed", System.Data.DbType.StringFixedLength).Value = jResult;		// save the job result
						cmd.Parameters.Add("@Warranty", System.Data.DbType.Int16).Value = Convert.ToInt16 (j.Warranty);		// save the warranty parameter
						cmd.ExecuteNonQuery();
						_tabs._scView.Log (String.Format ("SQL UPDATE statement executed: {0}", sql));
						_tabs._scView.Log (String.Format ("Parameter: jDone = {0}", j.JobDone.ToString ()));
						_tabs._scView.Log (String.Format ("Parameter: Installed = {0}", jResult));
						_tabs._scView.Log (String.Format ("Parameter: Pay_Pl = {0}", j.MoneyToCollect.ToString ()));
						_tabs._scView.Log (String.Format ("Parameter: Warranty = {0}", j.Warranty.ToString ()));
						
						if (savePayment) 
						{
							// save payment data

							if (_tabs._jobRunTable.CurrentCustomer.DepositUsed > 0) {
								// delete deposit record(s)
								sql = "DELETE FROM Journal WHERE CusNum = ? AND BookNum = ? AND jDesc = 'Deposit used on iPad' ";
								cmd.CommandText = sql;
								cmd.Parameters.Clear ();
								cmd.Parameters.Add ("@CustomerID", System.Data.DbType.Int64).Value = j.CustomerNumber;
								cmd.Parameters.Add ("@JobID", System.Data.DbType.Int64).Value = j.JobBookingNumber;
								cmd.ExecuteNonQuery ();

								// generate deposit record
								sql = "INSERT INTO JOURNAL (JDATE, COMPUPDATE, ACCNUM, JORNTYPE, JNUM, DEBIT, CREDIT, CUSNUM, REPNUM, PLNUM, JDESC, BOOKNUM) " +
									" VALUES (?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?)";
								cmd.CommandText = sql;
								cmd.Parameters.Clear ();
								cmd.Parameters.Add ("@jDate", System.Data.DbType.String).Value = j.JobDate.ToString("yyyy-MM-dd");
								cmd.Parameters.Add ("@CompUpdate", System.Data.DbType.String).Value = j.JobDate.ToString("yyyy-MM-dd");
								cmd.Parameters.Add ("Account", System.Data.DbType.Double).Value = 2.1300;
								cmd.Parameters.Add ("JournalType", System.Data.DbType.String).Value = "SJ";
								cmd.Parameters.Add ("JournalID", System.Data.DbType.Int64).Value = 0;
								cmd.Parameters.Add ("DebitAmount", System.Data.DbType.Double).Value = _tabs._jobRunTable.CurrentCustomer.DepositUsed;
								cmd.Parameters.Add ("CreditAmount", System.Data.DbType.Double).Value = 0.00;
								cmd.Parameters.Add ("CustomerID", System.Data.DbType.Int64).Value = j.CustomerNumber;
								cmd.Parameters.Add ("FranchiseeID", System.Data.DbType.Int32).Value = (MyConstants.EmployeeType == MyConstants.EmployeeTypes.Franchisee)? MyConstants.EmployeeID : 0;
								cmd.Parameters.Add ("PlumberID", System.Data.DbType.Int32).Value = (MyConstants.EmployeeType == MyConstants.EmployeeTypes.Plumber)? MyConstants.EmployeeID : 0;
								cmd.Parameters.Add ("Description", System.Data.DbType.String).Value = "Deposit used on iPad";
								cmd.Parameters.Add ("JobID", System.Data.DbType.Int64).Value = j.JobBookingNumber;
								cmd.ExecuteNonQuery ();
							}

							sql = "DELETE FROM Payments WHERE BookingNum = ? AND Payment_ID = ?";
							cmd.Parameters.Clear ();
							cmd.Parameters.Add ("@JobID", System.Data.DbType.Int64).Value = j.JobBookingNumber;
							cmd.Parameters.Add ("@PaymentID", System.Data.DbType.Int64).Value = j.JobBookingNumber;
							cmd.CommandText = sql;
							cmd.ExecuteNonQuery();
							_tabs._scView.Log (String.Format ("SQL DELETE statement executed: {0}", sql));
							_tabs._scView.Log (String.Format ("Parameters: JobID = {0}", j.JobBookingNumber));

							sql = "INSERT INTO Payments (PAYMENT_ID, CUSNUM, BOOKINGNUM, PAYMENT_TYPE, PAYMENT_DATE, " +
								"AMOUNT, CHEQUENUM, CREDITCARDNUM, CRCARD_EXPIRY, CRCARD_NAME) " +
								"VALUES (?, ?, ?, ?, ?, ?, ?, ?, ?, ?)";
							cmd.CommandText = sql;
							foreach(JobPayment payment in j.Payments)
							{
								cmd.Parameters.Clear ();
								cmd.Parameters.Add ("@PaymentID", System.Data.DbType.Int32).Value = j.JobBookingNumber; // we put a somewhat random number here because this field does not matter much in SQLite database
																																										// it will matter in FoxPro, however, and should be replaced with a proper value when processing data from the device
								cmd.Parameters.Add ("@CustomerNumber", System.Data.DbType.Int32).Value = j.CustomerNumber;
								cmd.Parameters.Add ("@JobID", System.Data.DbType.Int64).Value = j.JobBookingNumber;
								cmd.Parameters.Add ("@PaymentType", System.Data.DbType.String).Value = MyConstants.OutputStringForValue (payment.Type);
								cmd.Parameters.Add ("@PaymentDate", System.Data.DbType.Date).Value = j.JobDate;

									// IF THERE WAS A SERVICE REQUEST, THE AMOUNT VALUE SAVED WILL BE 0.00 IN ALL CASES
									// if (_tabs._payment.ContainsInvoicePaymentType (j.Payments)) payment.Amount = 0;
								if (payment.Type == PaymentTypes.Invoice) payment.Amount = 0;
								cmd.Parameters.Add ("@Amount", System.Data.DbType.Double).Value = payment.Amount;

								cmd.Parameters.Add ("@ChequeNumber", System.Data.DbType.String).Value = payment.ChequeNumber;
								cmd.Parameters.Add ("@CCNumber", System.Data.DbType.String).Value = payment.CreditCardNumber;
								cmd.Parameters.Add ("@CCExpiry", System.Data.DbType.String).Value = payment.CreditCardExpiry;
								cmd.Parameters.Add ("@CCName", System.Data.DbType.String).Value = payment.CreditCardName;
								
								cmd.ExecuteNonQuery ();
								
								_tabs._scView.Log (String.Format ("SQL INSERT statement executed: {0}", sql));
								_tabs._scView.Log (String.Format ("Parameter: CustomerNumber = {0}\n" +
									"Parameter: JobBookingNumber = {1}\n" +
									"Parameter: PaymentType = {2}\n" +
									"Parameter: PaymentDate = {3}\n" +
									"Parameter: Amount = {4}", payment.PaymentCustomerNumber, j.JobBookingNumber, payment.Type, j.JobDate.ToShortDateString(), payment.Amount));
							}
						}

						// save employee fees to FEES table
						if (j.ShouldPayFee)
						{
							// IMPLEMENTED :: check here to see if there's a row in the FEES table that is exactly identical to the one we were going to insert to avoid double-saving of fees
							sql = "SELECT * FROM Fees WHERE Job_ID=? AND Empl_Type=? AND Empl_ID=?";
							cmd.CommandText = sql;
							cmd.Parameters.Clear ();
							cmd.Parameters.Add ("@Job_ID", DbType.Int64).Value = j.JobBookingNumber;
							cmd.Parameters.Add ("@Employee_Type", DbType.Int32).Value = (MyConstants.EmployeeType == MyConstants.EmployeeTypes.Franchisee)? 1 : 2;
							cmd.Parameters.Add ("@Employee_ID", DbType.Int32).Value = MyConstants.EmployeeID;
							using (var reader = cmd.ExecuteReader ())
							{
								if (! reader.HasRows)
								{
									reader.Close ();
									sql = "INSERT INTO Fees (Job_ID, EMPL_TYPE, EMPL_ID, FEE_AMOUNT) VALUES (?, ?, ?, ?)";
									cmd.Parameters.Clear ();
									cmd.CommandText = sql;
									cmd.Parameters.Add ("@Job_ID", DbType.Int64).Value = j.JobBookingNumber;
									cmd.Parameters.Add ("@Employee_Type", DbType.Int32).Value = (MyConstants.EmployeeType == MyConstants.EmployeeTypes.Franchisee)? 1 : 2;
									cmd.Parameters.Add ("@Employee_ID", DbType.Int32).Value = MyConstants.EmployeeID;
									cmd.Parameters.Add ("@Fee_Amount", DbType.Double).Value = j.EmployeeFee;
									cmd.ExecuteNonQuery ();
								}
								else // the row is already in the table 
								{
									 reader.Close ();
									sql = "UPDATE Fees SET Fee_Amount=? WHERE Job_ID=? AND EMPL_TYPE=? AND EMPL_ID=?";
									cmd.Parameters.Clear ();
									cmd.CommandText = sql;
									cmd.Parameters.Add ("@Amount", DbType.Double).Value = j.EmployeeFee;
									cmd.Parameters.Add ("@Job_ID", DbType.Int64).Value = j.JobBookingNumber;
									cmd.Parameters.Add ("@Employee_Type", DbType.Int32).Value = (MyConstants.EmployeeType == MyConstants.EmployeeTypes.Franchisee)? 1 : 2;
									cmd.Parameters.Add ("@Employee_ID", DbType.Int32).Value = MyConstants.EmployeeID;
									cmd.ExecuteNonQuery ();
								}
							}
						}
							// save used parts data
						// delete all previous records for this job booking number						 
						sql = "DELETE FROM StockUsed WHERE booknum = ?";
						cmd.CommandText = sql;
						cmd.Parameters.Clear ();
						cmd.Parameters.Add ("@JobID", System.Data.DbType.Int64).Value = j.JobBookingNumber;
						cmd.ExecuteNonQuery();
						_tabs._scView.Log (String.Format ("SQL DELETE statement executed: {0}", sql));
						_tabs._scView.Log (String.Format ("Parameters: JobID = {0}", j.JobBookingNumber));
						
						// insert records for individual parts
						if (j.UsedParts != null) {
							if (j.UsedParts.Count > 0) {
								sql = "INSERT INTO StockUsed (CUSNUM, PLNUM, PARTNO, ELEMENT_TYPE, ELEMENT_OID, REPNUM, NUM_USED, USE_DATE, BOOKNUM) VALUES (?, ?, ?, ?, ?, ?, ?, ?, ?)";
								cmd.CommandText = sql;
								
								foreach (Part p in j.UsedParts)
								{
									cmd.Parameters.Clear ();									
									cmd.Parameters.Add ("@CustomerNumber", System.Data.DbType.Int32).Value = j.CustomerNumber;
									cmd.Parameters.Add ("@PlNum", System.Data.DbType.Int32).Value = 
										(MyConstants.EmployeeType==MyConstants.EmployeeTypes.Franchisee) ? -1 : MyConstants.EmployeeID;
									cmd.Parameters.Add ("@PartNo", System.Data.DbType.Int32).Value = p.PartNo;
									cmd.Parameters.Add ("@ElementType", System.Data.DbType.String).Value = 'P';
									cmd.Parameters.Add ("@ElementID", System.Data.DbType.Int32).Value = p.PartNo;

									cmd.Parameters.Add ("@RepNum", System.Data.DbType.Int32).Value = 
										(MyConstants.EmployeeType==MyConstants.EmployeeTypes.Franchisee) ? MyConstants.EmployeeID : -1;
									cmd.Parameters.Add ("@QuantityUsed", System.Data.DbType.Double).Value = p.Quantity;
									cmd.Parameters.Add ("@DateUsed", System.Data.DbType.Date).Value = j.JobDate; // DateTime.Now.Date;
									
									cmd.Parameters.Add ("@JobID", System.Data.DbType.Int64).Value = j.JobBookingNumber;
			
									cmd.ExecuteNonQuery ();
								}
							}	else _tabs._scView.Log (String.Format ("Job ID {0}: Used parts list is EMPTY: Nothing to write to database", j.JobBookingNumber));
						}	else _tabs._scView.Log (String.Format ("Job ID {0}: Used parts list is NULL: Nothing to write to database", j.JobBookingNumber));

						if (j.UsedAssemblies != null) {
							if (j.UsedAssemblies.Count > 0) {
								sql = "INSERT INTO StockUsed (CUSNUM, PLNUM, ELEMENT_TYPE, ELEMENT_OID, REPNUM, NUM_USED, USE_DATE, BOOKNUM) VALUES (?, ?, ?, ?, ?, ?, ?, ?)";
								cmd.CommandText = sql;

								foreach (Assembly a in j.UsedAssemblies) {
									cmd.Parameters.Clear ();
									cmd.Parameters.Add ("@CustomerNumber", System.Data.DbType.Int32).Value = j.CustomerNumber;
									cmd.Parameters.Add ("@PlNum", System.Data.DbType.Int32).Value = 
										(MyConstants.EmployeeType==MyConstants.EmployeeTypes.Franchisee) ? -1 : MyConstants.EmployeeID;
									cmd.Parameters.Add ("@ElementType", System.Data.DbType.String).Value = 'A';
									cmd.Parameters.Add ("@ElementID", System.Data.DbType.Int32).Value = a.aID;

									cmd.Parameters.Add ("@RepNum", System.Data.DbType.Int32).Value = 
										(MyConstants.EmployeeType==MyConstants.EmployeeTypes.Franchisee) ? MyConstants.EmployeeID : -1;
									cmd.Parameters.Add ("@QuantityUsed", System.Data.DbType.Double).Value = a.Quantity;
									cmd.Parameters.Add ("@DateUsed", System.Data.DbType.Date).Value = j.JobDate; // DateTime.Now.Date;
									cmd.Parameters.Add ("@JobID", System.Data.DbType.Int64).Value = j.JobBookingNumber;
									cmd.ExecuteNonQuery ();
								}
							} else _tabs._scView.Log (String.Format ("Job ID {0}: Used assemblies list is EMPTY: Nothing to write to database.", j.JobBookingNumber));
						} else _tabs._scView.Log (String.Format ("Job ID {0}: Used assemblies list is NULL: Nothing to write to database.", j.JobBookingNumber));
						_tabs._scView.Log (String.Format ("SQL INSERT INTO STOCKUSED statements executed."));
					} // end using (var cmd = connection.CreateCommand())
				} // end using (var connection = new SqliteConnection("Data Source="+ServerClientViewController.dbFilePath) )
			}
			else // database file does not exist for some reason... weird
			{
				// should never happen, but anyway
				using (var sc = _tabs._scView) {
					sc.Log (String.Format ("SaveJobResultsToDatabase: ERROR: Database file not found: {0}", ServerClientViewController.dbFilePath) );
				}
				_tabs.SelectedViewController = _tabs.ViewControllers[5];
			}
		}
		
		public void ResetWorkflowForJob(Job j)
		{
			// reset deposit data
			Customer c = _tabs._jobRunTable.FindCustomerForJob (j);
			c.DepositUsed = 0;

			// reset job properties
			j.Started = MyConstants.JobStarted.None;
			j.JobDone = false;
			j.NotDoneComment = "";
			if (j.UsedParts != null) j.UsedParts.Clear ();
			if (j.Payments != null)
			{
				j.Payments.Clear ();
				j.Payments = null;
				j.Payments = new List<JobPayment> ();
			}

			// clear all data maintained in view controllers
			ResetViewControllersToDefaults (true);
			// IMPLEMENTED :: erase results from database
			EraseMainJobResultsFromDatabase(j);
			if (j.ChildJobs != null)
			{
				foreach(Job child in j.ChildJobs)
				{
					child.Payments.Clear ();
					child.Payments = null;
				}
				j.ChildJobs.Clear ();
			}
			
			_tabs._jobRunTable.TableView.ReloadData ();
			_tabs._jobRunTable.TableView.SelectRow( _tabs._jobRunTable.LastSelectedRowPath, true, UITableViewScrollPosition.None);
		}
		
		public void EraseCustomerRecordFromDatabase(Customer c)
		{
			_tabs._scView.Log (String.Format ("Erasing customer record, ID = {0}", c.CustomerNumber));
			if (File.Exists (ServerClientViewController.dbFilePath) )
			{
				using (var connection = new SqliteConnection("Data Source="+ServerClientViewController.dbFilePath) )
				{
					using (var cmd = connection.CreateCommand())
					{
						connection.Open();
						// reset data :: to accomplish that, we delete all records from STOCKUSED, PAYMENTS and FOLLOWUPS tables for the job AND ITS CHILD JOBS
						string sql = "DELETE FROM Wclient WHERE Cusnum = ?";
						cmd.CommandText = sql;
						cmd.Parameters.Add ("@CustomerID", DbType.Int64).Value = c.CustomerNumber;
						cmd.ExecuteNonQuery ();
					}
				}
			}
			else // database file does not exist for some reason... weird
			{
				// should never happen, but anyway
				_tabs._scView.Log(String.Format ("EraseCustomerRecordFromDatabase: FATAL ERROR: Database file missing: {0}", ServerClientViewController.dbFilePath) );
				_tabs.SelectedViewController = _tabs.ViewControllers[5];
			}			
		}
		
		public void EraseJobRecordFromDatabase(Job j)
		{
			_tabs._scView.Log (String.Format ("Erasing main job record, ID = {0}", j.JobBookingNumber));
			if (File.Exists (ServerClientViewController.dbFilePath) )
			{
				using (var connection = new SqliteConnection("Data Source="+ServerClientViewController.dbFilePath) )
				{
					using (var cmd = connection.CreateCommand())
					{
						connection.Open();
						// reset data :: to accomplish that, we delete all records from STOCKUSED, PAYMENTS and FOLLOWUPS tables for the job AND ITS CHILD JOBS
						string sql = "DELETE FROM Pl_recor WHERE Booknum = ?";
						cmd.CommandText = sql;
						cmd.Parameters.Add ("@JobID", DbType.Int64).Value = j.JobBookingNumber;
						cmd.ExecuteNonQuery ();
					}
				}
			}
			else // database file does not exist for some reason... weird
			{
				// should never happen, but anyway
				_tabs._scView.Log(String.Format ("EraseJobRecordFromDatabase: FATAL ERROR: Database file missing: {0}", ServerClientViewController.dbFilePath) );
				_tabs.SelectedViewController = _tabs.ViewControllers[5];
			}
		}
		
		public void EraseMainJobResultsFromDatabase(Job j)
		{ // this method erases all child job data entirely as well
			_tabs._scView.Log (String.Format ("Erasing main job data, ID = {0}", j.JobBookingNumber));
						
			if (File.Exists (ServerClientViewController.dbFilePath) )
			{
				// connect to database
				using (var connection = new SqliteConnection("Data Source="+ServerClientViewController.dbFilePath) )
				{
					using (var cmd = connection.CreateCommand())
					{
						connection.Open();
						// reset data :: delete all records from STOCKUSED, PAYMENTS, FOLLOWUPS, JOB_REPORTS, FEES, CUSTUPDATE tables for the job AND ITS CHILD JOBS
						string sql = "DELETE FROM StockUsed WHERE Booknum IN (SELECT Booknum FROM Pl_recor WHERE Booknum = ? OR Parentnum = ?)";
						cmd.CommandText = sql;
						cmd.Parameters.Clear();
						cmd.Parameters.Add ("@JobID", System.Data.DbType.Int64).Value = j.JobBookingNumber;
						cmd.Parameters.Add ("@ParentJobID", System.Data.DbType.Int64).Value = j.JobBookingNumber;
						cmd.ExecuteNonQuery ();
						// reset payment data
						sql = "DELETE FROM Payments WHERE BookingNum IN (SELECT Booknum FROM Pl_recor WHERE Booknum = ? OR Parentnum = ?)";
						cmd.CommandText = sql;
						cmd.Parameters.Clear();
						cmd.Parameters.Add ("@JobID", System.Data.DbType.Int64).Value = j.JobBookingNumber;
						cmd.Parameters.Add ("@ParentJobID", System.Data.DbType.Int64).Value = j.JobBookingNumber;
						cmd.ExecuteNonQuery ();

						// reset deposit info
						// 22.04.2014 -- I don't think this is to be done here -- what if someone re-entered info about a job with a deposit?
//						sql = "DELETE FROM JOURNAL WHERE BookNum = ? AND AccNum = 2.1300";
//						cmd.CommandText = sql;
//						cmd.Parameters.Clear ();
//						cmd.Parameters.Add ("@JobID", System.Data.DbType.Int64).Value = j.JobBookingNumber;
//						int rowsDeleted = cmd.ExecuteNonQuery ();
//						if (rowsDeleted > 0) {
//							// find the customer and reset DepositUsed to 0
//							foreach (Customer c in _tabs._jobRunTable.Customers) {
//								if (c.CustomerNumber == j.CustomerNumber) {
//									
//									// check if the deposit was used for the job that is being reset
//									c.DepositAmount = c.DepositUsed;
//									c.DepositUsed = 0;
//								}
//							}
//						}

						// reset follow up data
						sql = "DELETE FROM Followups WHERE Job_ID IN (SELECT Booknum FROM Pl_recor WHERE Booknum = ? OR Parentnum = ?)";
						cmd.CommandText = sql;
						cmd.Parameters.Clear();
						cmd.Parameters.Add ("@JobID", System.Data.DbType.Double).Value = j.JobBookingNumber;
						cmd.Parameters.Add ("@ParentJobID", System.Data.DbType.Int64).Value = j.JobBookingNumber;
						cmd.ExecuteNonQuery ();
						// reset fee data
						sql = "DELETE FROM Fees WHERE Job_ID IN (SELECT Booknum FROM Pl_recor WHERE Booknum = ? OR Parentnum = ?)";
						cmd.CommandText = sql;
						cmd.Parameters.Clear();
						cmd.Parameters.Add ("@JobID", System.Data.DbType.Double).Value = j.JobBookingNumber;
						cmd.Parameters.Add ("@ParentJobID", System.Data.DbType.Int64).Value = j.JobBookingNumber;
						cmd.ExecuteNonQuery ();
						// reset updates to pricing for child jobs
						sql =  "DELETE FROM CustUpdate WHERE Job_OID IN (SELECT Booknum FROM Pl_recor WHERE Parentnum = ?) AND Field=\"JobPriceTotal\"";
						cmd.Parameters.Clear();
						cmd.Parameters.Add ("@JobID", System.Data.DbType.Double).Value = j.JobBookingNumber;
						cmd.Parameters.Add ("@ParentJobID", System.Data.DbType.Int64).Value = j.JobBookingNumber;
						cmd.ExecuteNonQuery ();
						// reset job reports data
						sql = "DELETE FROM Job_Reports WHERE Job_OID IN (SELECT BookNum FROM Pl_Recor WHERE BookNum = ? OR ParentNum = ?)";
						cmd.Parameters.Clear();
						cmd.Parameters.Add ("@JobID", System.Data.DbType.Double).Value = j.JobBookingNumber;
						cmd.Parameters.Add ("@ParentJobID", System.Data.DbType.Int64).Value = j.JobBookingNumber;
						cmd.ExecuteNonQuery ();

						// reset job results data
						// update the record in PL_RECOR table where BOOKINGNUMBER = job.BookingNumber, setting the following values: JDONE=0, INSTALLED=""
						sql = (MyConstants.EmployeeType == MyConstants.EmployeeTypes.Franchisee)? "UPDATE PL_RECOR SET jDone = ?, Installed = ?, RepNum = ? WHERE Booknum = ?" : 
							"UPDATE PL_RECOR SET jDone = ?, Installed = ?, PlNum = ? WHERE Booknum = ?";
						cmd.Parameters.Clear ();
						cmd.CommandText = sql;
						cmd.Parameters.Add ("@JobDone", System.Data.DbType.Int32).Value = 0; // set jDone = 0 (false)
						cmd.Parameters.Add ("@JobResult", System.Data.DbType.String).Value = ""; // set installed = "" (empty string)
						cmd.Parameters.Add ("@EmployeeID", System.Data.DbType.Int32).Value = MyConstants.EmployeeID;
						cmd.Parameters.Add ("@JobID", System.Data.DbType.Int64).Value = j.JobBookingNumber;

						_tabs._scView.Log (String.Format ("SQL UPDATE statement executed: {0}", sql));
						_tabs._scView.Log (string.Format ("Parameter: JobDone = {0}", 0));
						_tabs._scView.Log (string.Format ("Parameter: JobResult = {0}", "\"\""));
						_tabs._scView.Log (string.Format ("Parameter: JobID = {0}", j.JobBookingNumber));					
						
						cmd.ExecuteNonQuery ();						

						// if this job has child jobs, delete their records entirely, 					
						sql = "DELETE FROM PL_RECOR WHERE Booknum IN (SELECT Booknum FROM Pl_recor WHERE Parentnum = ?)";
						cmd.CommandText = sql;
						cmd.Parameters.Clear ();
						cmd.Parameters.Add ("@JobID", System.Data.DbType.Int64).Value = j.JobBookingNumber;
						cmd.ExecuteNonQuery ();

						_tabs._scView.Log (String.Format ("SQL DELETE statement executed: {0}", sql));
						_tabs._scView.Log (String.Format ("Parameter: JobID = {0}", j.JobBookingNumber));					
						
						_tabs._scView.Log (String.Format ("Erased job results from database (with child jobs): JobID = {0}", j.JobBookingNumber));	
					} // end using command = connection.CreateCommand()
				} // end using connection = new SqliteConnection("Data Source="+ServerClientViewController.dbPath) 
			} // end if File.Exists
			else // database file does not exist for some reason... weird
			{
				// should never happen, but anyway
				_tabs._scView.Log(String.Format ("EraseMainJobResultsFromDatabase: ERROR: Database file missing: {0}", ServerClientViewController.dbFilePath) );
				_tabs.SelectedViewController = _tabs.ViewControllers[5];
			}
		}
		
		public void EraseChildJobFromDatabase(Job j)
		{
			_tabs._scView.Log (String.Format ("Erasing child job data, ID = {0}", j.JobBookingNumber));
						
			if (File.Exists (ServerClientViewController.dbFilePath) )
			{
				// connect to database
				using (var connection = new SqliteConnection("Data Source="+ServerClientViewController.dbFilePath) )
				{
					using (var cmd = connection.CreateCommand())
					{
						connection.Open();

						// erase used parts data :: to accomplish that, we delete all records from STOCKUSED, PAYMENTS and FOLLOWUPS tables for the job AND ITS CHILD JOBS
						string sql = "DELETE FROM StockUsed WHERE Booknum = ?";
						cmd.CommandText = sql;
						cmd.Parameters.Add ("@JobID", System.Data.DbType.Int64).Value = j.JobBookingNumber;
						cmd.ExecuteNonQuery ();
						_tabs._scView.Log (String.Format ("SQL DELETE statement executed: {0}", sql));
						_tabs._scView.Log (string.Format ("Parameter: JobID = {0}", j.JobBookingNumber));
						// erase payment data
						sql = "DELETE FROM Payments WHERE BookingNum = ?";
						cmd.CommandText = sql;
						cmd.ExecuteNonQuery ();
						_tabs._scView.Log (String.Format ("SQL DELETE statement executed: {0}", sql));
						_tabs._scView.Log (string.Format ("Parameter: JobID = {0}", j.JobBookingNumber));
						// erase follow up data
						sql = "DELETE FROM Followups WHERE Job_ID = ?";
						cmd.CommandText = sql;
						cmd.ExecuteNonQuery ();
						_tabs._scView.Log (String.Format ("SQL DELETE statement executed: {0}", sql));
						_tabs._scView.Log (string.Format ("Parameter: JobID = {0}", j.JobBookingNumber));
						// erase fee data
						sql = "DELETE FROM Fees WHERE Job_ID = ?";
						cmd.CommandText = sql;
						cmd.ExecuteNonQuery ();
						_tabs._scView.Log (String.Format ("SQL DELETE statement executed: {0}", sql));
						_tabs._scView.Log (string.Format ("Parameter: JobID = {0}", j.JobBookingNumber));
						// erase job reports
						sql = "DELETE FROM Job_Reports WHERE Job_OID = ?";
						cmd.CommandText = sql;
						cmd.ExecuteNonQuery ();
						_tabs._scView.Log (String.Format ("SQL DELETE statement executed: {0}", sql));
						_tabs._scView.Log (string.Format ("Parameter: JobID = {0}", j.JobBookingNumber));

						// delete job record from pl_recor
						sql = "DELETE FROM PL_RECOR WHERE Booknum = ?";
						cmd.CommandText = sql;
						cmd.Parameters.Clear ();
						cmd.Parameters.Add ("@JobID", System.Data.DbType.Int64).Value = j.JobBookingNumber;
						cmd.ExecuteNonQuery ();

						_tabs._scView.Log (String.Format ("SQL DELETE statement executed: {0}", sql));
						_tabs._scView.Log (String.Format ("Parameter: JobID = {0}", j.JobBookingNumber));					
						
						_tabs._scView.Log (String.Format ("Erased child job from database: JobID = {0}", j.JobBookingNumber));
					}
				}
			}
			else // database file does not exist for some reason... weird
			{
				// should never happen, but anyway
				_tabs._scView.Log(String.Format ("EraseMainJobResultsFromDatabase: FATAL ERROR: Database file missing: {0}", ServerClientViewController.dbFilePath) );
				_tabs.SelectedViewController = _tabs.ViewControllers[5];
			}

			return;
		}
		
		public void DisableTabsAtWorkflowStart()
		{
			foreach( var tbi in this._tabs.TabBar.Items )
			{
				if (tbi.Title == "Payments Summary" || tbi.Title == "Server/Client") tbi.Enabled = false;
			} 
		}
		
		public void EnableTabsAtWorkflowEnd()
		{
			foreach( var tbi in this._tabs.TabBar.Items )
			{
				if (tbi.Title == "Payments Summary" || tbi.Title == "Server/Client") tbi.Enabled = true;
			} 			
		}
	}

}

