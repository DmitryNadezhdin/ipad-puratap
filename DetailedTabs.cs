using System;
using System.IO;
using System.Collections.Generic;
using System.Threading;
using System.Linq;
using System.Text;
using MonoTouch.Foundation;
using MonoTouch.CoreLocation;
using MonoTouch.AddressBook;
using MonoTouch.AddressBookUI;
using MonoTouch.UIKit;
using System.Drawing;
using Mono.Data.Sqlite;
using MonoTouch.Dialog;
using MonoTouch.MapKit;

namespace Application
{
	public enum DetailedTabsMode { Workflow, Lookup }
	
	public class DetailedTabs : UITabBarController 
	{		
		private DetailedTabsMode _mode;
		public DetailedTabsMode Mode { 
			get { return _mode; } 
			set 
			{ 
				_mode = value;
				switch (_mode)
				{
					case DetailedTabsMode.Workflow: {
							this.ViewControllers = new UIViewController[] { 	
								_prePlumbView,
								ServiceNav,
								UsedPartsNav,
								_payment,
								SigningNav
							};

						this.TabBar.UserInteractionEnabled = false;
						break;
					}
					case DetailedTabsMode.Lookup: {
						this.ViewControllers = new UIViewController[] {
							CustomerNav, // _customersView,
							_jobHistoryView,
							_memosView,
							_photosView,
							SummaryNav,
							ServerNav // _scView					
						};
						
						this.SelectedViewController = this.ViewControllers[0];
						this.TabBar.UserInteractionEnabled = true;
						break;
					}
					default: return;
				}
			} 
		}

		// AppDelegate reference to be able to start and stop updating location
		public AppDelegate _app;
		
		// Navigation bar buttons and their event handlers
		UIBarButtonItem _btnEdit;
		UIBarButtonItem _btnStuff;
		// button management throughout the program is clunky:
		// DEPRECATED :: the general idea is that DetailedTabs handles buttons on its NavigationBar (the top stripe), while WorkflowNavigationController handles the toolbar (bottom stripe)
		
		EventHandler _doneEditingJobList;
		EventHandler _editJobList;
		EventHandler _createNewMemo;
		EventHandler _searchCustomersByStreet;
		EventHandler _deleteSelectedMemo;
		EventHandler _jobWasNotDoneClicked;
		EventHandler _startWorkflow;
		
		UIAlertView _newMemoView;	// an alert dialog used when creating a new memo
		
		int _lastSelectedTab;
		
		// comments about view controllers are deprecated, the interface uses another logic now
		
		// View controllers
		public JobRunTable _jobRunTable { get; set; }							// table on the left side
		public CustomersViewController _customersView { get; set; }		// customer details screen (tab)
		public JobHistoryViewController _jobHistoryView { get; set; }		// job history screen (tab)
		public Memos _memosView { get; set; }									// memos screen (tab)
		public ServerClientViewController _scView { get; set; }				// server/client (tab)
		public TakePhotosViewController _photosView { get; set; }			// photos (tab)
		public PaymentsSummary _paySummaryView { get; set; }			// payment summary screen (tab)
		
		public WorkflowNavigationController _navWorkflow { get; set; }	// TODO :: GET RID OF THIS CLASS, ROLL THE FUNCTIONALITY INTO DetailedTabs
		
		public JobSummary _jobSummaryView { get; set; }						// job summary screen (used in workflow)
		public PrePlumbingCheckView _prePlumbView { get; set; }				// pre-plumbing check screen (used in workflow)
		public JobInstallationViewController _jobInstall {get; set; }		// installation screen (used in workflow)
		public JobUninstallViewController _jobUninstall { get; set; }		// un-install screen (used in workflow)
		public FilterChangeViewController _jobFilter { get; set; }			// filter change screen (used in workflow)
		public JobServiceCallViewController _jobService { get; set; }		// service call screen (used in workflow)
		public JobTubingUpgrade _jobTubingUpgrade { get; set; }				// tubing upgrade screen  (used in workflow)
		public JobHDTubingUpgrade _jobHDTubingUpgrade {get; set; }			// HD tubing upgrade screen  (used in workflow)
		public JobUnitUpgrade _jobUnitUpgrade { get; set; }					// unit upgrade screen (used in workflow)
		public JobNewTap _jobNewTap { get; set; }							// new tap screen (used in workflow)
		public JobReinstallViewController _jobReinstall { get; set; }		// re-installation screen (used in workflow)
		public JobDeliveryViewController _jobDelivery { get; set; }			// delivery screen (used in workflow)
		
		public ServiceUsedPartsViewController _serviceParts { get; set; }	// choose parts used for service operation screen (used by service call)
		
		// OLD :: NOT TO BE USED public SignatureViewController _signView { get; set; }				// signature screen
		public PaymentViewController _payment { get; set; }					// payment screen
		
		public ServiceNavigationController ServiceNav { get; set; }
		public UsedPartsNavigationController UsedPartsNav { get; set;}
		public SigningNavigationController SigningNav { get; set; }
		public PaymentsSummaryNavigationController SummaryNav { get; set; }

		public CustomerNavigationController CustomerNav { get; set; }
		public ServerClientNavigatonController ServerNav { get; set; }
		
		public SignPrePlumbingViewController SignPre { get; set; }
		public SignServiceReportViewController SignService { get; set; }
		public SignInvoiceViewController SignInvoice { get; set; }


		
		// public StockControlViewController _stockControl { get; set; } 	REPLACED BY UsedPartsViewControllers: ServiceUsedPartsViewController, FilterChangeViewController, JobInstallationViewController
		// public UsedPartsViewController _jobFilter { get; set; }				REPLACED BY derived classes: ServiceUsedPartsViewController, FilterChangeViewController, JobInstallationViewController
		// public InvoiceViewController _invoiceView { get; set; }				REPLACED BY PaymentViewController
		
		// private UINavigationBar _navBar;		// the upper stripe on the tabs' half of the screen
		UIPopoverController _pc;					// this controller is JobRunTable, it's in portrait mode
		
		public UIPopoverController Popover {
            get { return _pc; }
            set { _pc = value; }
        }

		public UINavigationBar MyNavigationBar {
			get { 
				// return _navBar; }
				if (this.NavigationController != null)
					return this.NavigationController.NavigationBar;
				else return null;
			} 
		}

		public int LastSelectedTab { get { return _lastSelectedTab; } set { _lastSelectedTab = value; } }
		
		public UIBarButtonItem BtnEdit {
			get { return _btnEdit; }
			set { _btnEdit = value; }
		}
		public UIBarButtonItem BtnStuff {
			get { return _btnStuff; }
			set { _btnStuff = value; }
		}
		
		public UIBarButtonItem BtnStartWorkflow { get; set; }
		
		public EventHandler EditJobList 
		{
			get { return _editJobList; }
			set { _editJobList = value; }
		}

		public EventHandler SearchCustomersByStreet {
			get { return _searchCustomersByStreet; }
			set { _searchCustomersByStreet = value; }
		}
		public EventHandler CreateNewMemo {
			get { return _createNewMemo; }
			set { _createNewMemo = value; }
		}
		public EventHandler DeleteSelectedMemo {
			get { return _deleteSelectedMemo; }
			set { _deleteSelectedMemo = value; }
		}
		
		void StartWorkflow() 
		{	
			_jobRunTable.HighlightedMode = false;
			if (_jobRunTable.TableView.IndexPathForSelectedRow == null)
			{
				// shows alert that row must be selected for this functionality to work
				using(var alert = new UIAlertView("Please", "Select a customer", null, "OK", null))
				{
					alert.Show();
					_jobRunTable.TableView.ReloadData ();
				}
			}
			else 
			{
				// _table.CurrentJob = _table.MainJobList[indexPath.Row];
				switch(_jobRunTable.TableView.IndexPathForSelectedRow.Section)
				{
				case 0: _jobRunTable.CurrentJob = _jobRunTable.MainJobList[ _jobRunTable.TableView.IndexPathForSelectedRow.Row]; break;
				case 1: _jobRunTable.CurrentJob = _jobRunTable.UserCreatedJobs[ _jobRunTable.TableView.IndexPathForSelectedRow.Row]; break;
				default : return;
				}
				
				if (_jobRunTable.CurrentJob != null && _jobRunTable.CurrentJob.JobDone)
				{
					var alert = new UIAlertView("Warning", "This job has been marked as DONE. Are you sure you want to go through all the motions again?", null, "No, never mind", "Yes, start over");
					{
						alert.WillDismiss += HandleStartOverAlertWillDismiss;
						alert.Show();
						//return false;
					}	
				}
				else {
					UIView.BeginAnimations (null);
					UIView.SetAnimationDuration (0.3f);
					MyNavigationBar.Hidden = true;
					this.Mode = DetailedTabsMode.Workflow;
					UIView.CommitAnimations ();
					
					_jobRunTable.TableView.UserInteractionEnabled = false;	

					//	_navWorkflow.SetToolbarHidden (false, true);
					//	if (_navWorkflow.ViewControllers.Length == 0) 
					//	_navWorkflow.PushViewController (_prePlumbView, true);
					//	SetNavigationButtons (NavigationButtonsMode.PrePlumbing);
					//	return true;							
				}
			}			
		}		
		
		public DetailedTabs(JobRunTable jrt, AppDelegate app)		// constructor for DetailedTabs
		{
			/*
			_navBar = new UINavigationBar(new RectangleF(this.View.Bounds.X, this.View.Bounds.Y, this.View.Bounds.Width-100, 40));		// creates the navigation bar
			_navBar.BarStyle = UIBarStyle.Black;
			_navBar.Translucent = true;
			_navBar.PushNavigationItem(new UINavigationItem("Puratap") {
				HidesBackButton = true,
				LeftItemsSupplementBackButton = true
			}, false);
			// _navBar.TopItem.Prompt = "Puratap";

			_navBar.Frame = new RectangleF(0, 0, 604, 40);

			// this.Add(_navBar);	// displays the navigation bar
			// SetNavigationButtons (NavigationButtonsMode.CustomerDetails);			
			*/

			this._app = app;
			this._jobRunTable = jrt;		// allows to reference properties of tableView object displayed on the left-hand side of the screen and call its methods

			_createNewMemo = delegate {				// event handler for the rightmost button on the navigation bar: creates new memos
				if (_jobRunTable.CurrentCustomer == null)
				{
					var noCustomerChosen = new UIAlertView("No customer selected", "Please select a customer", null, null, "OK");
					noCustomerChosen.Show ();
				}
				else
				{
					string s = String.Format ("{0} {1} {2}", 	_jobRunTable.CurrentCustomer.Title,
					                          										_jobRunTable.CurrentCustomer.FirstName,
					                          										_jobRunTable.CurrentCustomer.LastName);
					_newMemoView = new UIAlertView("Create a new memo for\n"+s+
					                               "\nCustomer #"+_jobRunTable.CurrentCustomer.CustomerNumber+"\n\n\n\n\n", "", null, "Cancel", "OK");

					_newMemoView.AlertViewStyle = UIAlertViewStyle.PlainTextInput;

//					UITextField memoText = new UITextField(new RectangleF(12,95,260,25));
//					memoText.BackgroundColor = UIColor.White;
//					memoText.Placeholder = "Enter memo text here";
//					_newMemoView.AddSubview (memoText);
					
					_newMemoView.Show ();
					_newMemoView.WillDismiss += Handle_newMemoViewWillDismiss;
				}
			};
			
			_startWorkflow = delegate {
				StartWorkflow ();
			};
			
			_jobWasNotDoneClicked = delegate {
				if (_prePlumbView!=null)
					_prePlumbView.JobWasNotDoneClicked ();
			};
			
			_deleteSelectedMemo = delegate {
				if (_memosView.MemosTable.IndexPathForSelectedRow == null)		// no memo is selected
				{
					using(var alert = new UIAlertView("Please", "Select a memo to delete", null, "OK", null))
					{
						alert.Show();
						return;
					}
				}
				else 	// a row is selected in a table of memos
				{
					// check if it is editable
					int row = _memosView.MemosTable.IndexPathForSelectedRow.Row;
					
					if (_memosView.CustomerMemos[row].Editable) 
					{ // if it is, show an alert asking for confirmation
						long cn = _memosView.CustomerMemos[row].MemoCustomerNumber;
						foreach (Customer c in _jobRunTable.Customers)
						{ 
							if (c.CustomerNumber == cn) 
							{
								UIAlertView alert = new UIAlertView( 
								                                    String.Format ("Delete this memo? \n{0} {1} {2}\nCustomer # {3}", c.Title, c.FirstName, c.LastName, c.CustomerNumber.ToString()), 
								                                    _memosView.CustomerMemos[row].MemoContents, null, "Cancel", "Delete");
								alert.WillDismiss += Handle_deleteMemoAlertWillDismiss;
								alert.Show();
								return;
							} 
						}
					}
					else { // if it's not editable, show an alert informing the user that he is not able to delete that
						using(var alert = new UIAlertView("Cannot delete memo", "Either it isn't a franchisee/plumber memo OR it wasn't created today", null, "OK", null))
						{
							alert.Show();
							return;
						}					
					}
				}
			};
			
			_searchCustomersByStreet = delegate {		// event handler for the rightmost button on the navigation bar: highlights customers ont the same street as currently selected customer
				if (_jobRunTable.TableView.IndexPathForSelectedRow == null)
				{
					// shows alert that row must be selected for this functionality to work
					using(var alert = new UIAlertView("Please", "Select a customer", null, "OK", null))
					{
						alert.Show();
						return;
					}
				}
				else 
				{
					_jobRunTable.HighlightedMode = true;
					_jobRunTable.TableView.ReloadData();
				}
			};
			
			_doneEditingJobList = delegate {		// event handler for a button that allows rearranging of customers in the jobs table on the left-hand side of the screen
																	// this handler is used when the table is in editing mode
				UIView.BeginAnimations (null);
				UIView.SetAnimationDuration (0.5);
				_jobRunTable.TableView.SetEditing(false,true);	// table exits editing mode
				foreach(UITabBarItem item in this.TabBar.Items)
				{
					item.Enabled = true;
				}
				_btnEdit = new UIBarButtonItem("More actions", UIBarButtonItemStyle.Bordered, _editJobList);	// the "done" button should revert to its original state
				MyNavigationBar.TopItem.SetRightBarButtonItems(new UIBarButtonItem[] { BtnStartWorkflow, BtnEdit}, true);		// pushes new button objects into navigation bar
				this.SelectedViewController.View.Alpha = 1;
				this.SelectedViewController.View.UserInteractionEnabled = true;
				UIView.CommitAnimations ();
			};
			
			 _editJobList = delegate {			// event handler for a button that allows rearranging of customers in the jobs table on the left-hand side of the screen
															// this handler is used when the table is in NOT editing mode
				
				var ac = new UIActionSheet("", null, null, null, "Rearrange jobs", "Reset jobs to default order", "Show in Apple maps", "Show on Google maps", "Reprint docs for customer") { 
					Style = UIActionSheetStyle.BlackTranslucent
				};

				ac.Dismissed += delegate(object sender, UIButtonEventArgs e) {
					switch(e.ButtonIndex)
					{
					case 4: { DoReprintDocsForCustomer (); break; }
					case 3: { DoShowCustomerGoogleMaps(); break; }
					case 2: { DoShowCustomerAppleMaps(); break; } //  SearchCustomersByStreet(null, null); break; }
					case 1: { DoResetToDefault (); break; }
					case 0: { DoRearrange (); break; }
					}
					BtnEdit.Enabled = true;
				};
				ac.ShowFrom (BtnEdit, true);	
				BtnEdit.Enabled = false;
			};


			// Create navigation controllers for the tabs
			_navWorkflow = new WorkflowNavigationController(this);
			
			ServiceNav = new ServiceNavigationController(this);
			ServiceNav.Title = NSBundle.MainBundle.LocalizedString ("Service", "Service");
			using(var image = UIImage.FromBundle ("/Images/157-wrench") ) ServiceNav.TabBarItem.Image = image;
			ServiceNav.NavigationBar.BarStyle = UIBarStyle.Black;
			ServiceNav.NavigationBar.Translucent = true;
			ServiceNav.NavigationBarHidden = true;
			ServiceNav.Toolbar.BarStyle = UIBarStyle.Black;
			ServiceNav.Toolbar.Translucent = true;
			ServiceNav.Toolbar.Hidden = false;
		
			UsedPartsNav = new UsedPartsNavigationController(this);
			UsedPartsNav.Title = "Parts";
			using(var image = UIImage.FromBundle ("/Images/20-gear2") ) UsedPartsNav.TabBarItem.Image = image;
			UsedPartsNav.NavigationBar.BarStyle = UIBarStyle.Black;
			UsedPartsNav.NavigationBar.Translucent = true;
			UsedPartsNav.Toolbar.BarStyle = UIBarStyle.Black;
			UsedPartsNav.Toolbar.Translucent = true;
			UsedPartsNav.Toolbar.Hidden = false;

			SigningNav = new SigningNavigationController(this);
			SigningNav.Title = "Sign";
			using(var image = UIImage.FromBundle ("/Images/187-pencil") ) SigningNav.TabBarItem.Image = image;
			SigningNav.NavigationBar.BarStyle = UIBarStyle.Black;
			SigningNav.NavigationBar.Translucent = true;
			SigningNav.Toolbar.BarStyle = UIBarStyle.Black;
			SigningNav.Toolbar.Translucent = true;
			SigningNav.Toolbar.Hidden = false;
			SigningNav.View.AutosizesSubviews = true;
			
			SummaryNav = new PaymentsSummaryNavigationController(this);
			SummaryNav.TabBarItem.Title = "Summary";
			using(var image = UIImage.FromBundle ("/Images/162-receipt") ) SummaryNav.TabBarItem.Image = image;
			SummaryNav.NavigationBar.BarStyle = UIBarStyle.Black;
			SummaryNav.NavigationBar.Translucent = true;
			SummaryNav.Toolbar.BarStyle = UIBarStyle.Black;
			SummaryNav.Toolbar.Translucent = true;
			SummaryNav.Toolbar.Hidden = false;

			ServerNav = new ServerClientNavigatonController(this);
			using(var image = UIImage.FromBundle ("/Images/174-imac") ) ServerNav.TabBarItem.Image = image;
			ServerNav.TabBarItem.Title = "Server/Client";
			ServerNav.NavigationBar.BarStyle = UIBarStyle.Black;
			ServerNav.NavigationBar.Translucent = true;
			ServerNav.Toolbar.BarStyle = UIBarStyle.Black;
			ServerNav.Toolbar.Translucent = true;
			ServerNav.Toolbar.Hidden = false;

			CustomerNav =  new  CustomerNavigationController(this);
			using(var image = UIImage.FromBundle ("/Images/111-user") ) CustomerNav.TabBarItem.Image = image;
			CustomerNav.TabBarItem.Title = "Customer";
			CustomerNav.NavigationBar.BarStyle = UIBarStyle.Black;
			CustomerNav.NavigationBar.Translucent = true;
			CustomerNav.NavigationBar.Hidden = true;

			CustomerNav.Toolbar.BarStyle = UIBarStyle.Black;
			CustomerNav.Toolbar.Translucent = true;
			CustomerNav.Toolbar.Hidden = true;

			// Create views corresponding to each of the tabs
			_customersView = new CustomersViewController(this);
			_jobHistoryView = new JobHistoryViewController(this);
			_memosView = new Memos(this);
			
			// _signView = new SignatureViewController(this);
			SignPre = new SignPrePlumbingViewController(this);
			SignService = new SignServiceReportViewController(this);
			SignInvoice = new SignInvoiceViewController(this);
			
			// _invoiceView = new InvoiceViewController();
			_scView = new ServerClientViewController(this);
			_photosView = new TakePhotosViewController(this);

			_paySummaryView = new PaymentsSummary(new RootElement(""), SummaryNav, _jobRunTable);
			_jobSummaryView = new JobSummary(_navWorkflow, this, new RootElement(""), true);
			
			_prePlumbView = new PrePlumbingCheckView(_navWorkflow);
			
			_jobFilter = new FilterChangeViewController(new RootElement("Filter change"), _navWorkflow, UsedPartsNav, true );
			
			_jobInstall = new JobInstallationViewController(new RootElement("Installation"), _navWorkflow, UsedPartsNav, true);
			_jobUninstall = new JobUninstallViewController(new RootElement("Uninstallation"), _navWorkflow, UsedPartsNav, true);
			_jobReinstall = new JobReinstallViewController(new RootElement("Re-installation"), _navWorkflow, UsedPartsNav, true);
			_jobTubingUpgrade = new JobTubingUpgrade(new RootElement("Tubing upgrade"), _navWorkflow, UsedPartsNav, true);
			_jobHDTubingUpgrade = new JobHDTubingUpgrade(new RootElement("HD tubing upgrade"), _navWorkflow, UsedPartsNav, true);
			_jobNewTap = new JobNewTap(new RootElement("New tap"), _navWorkflow, UsedPartsNav, true);
			_jobUnitUpgrade = new JobUnitUpgrade(new RootElement("Unit upgrade"), _navWorkflow, UsedPartsNav, true);
			_jobDelivery = new JobDeliveryViewController (new RootElement("Delivery"), _navWorkflow, UsedPartsNav, true);
			
			_jobService = new JobServiceCallViewController(_navWorkflow, ServiceNav);
			_serviceParts = new ServiceUsedPartsViewController(new RootElement(""), _navWorkflow, UsedPartsNav, true);
			_serviceParts.Title = "Service parts";
			using(var image = UIImage.FromBundle ("/Images/20-gear2") ) _serviceParts.TabBarItem.Image = image;
			
			var tmp = new FilterChangeViewController(new RootElement(""), null, null, false);
			tmp.Title = "Parts";
			tmp.NavigationItem.HidesBackButton = true;
			UsedPartsNav.PushViewController (tmp, false);
			
			var smp = new NewSignatureViewController(this);
			smp.Title = "Sign";
			smp.NavigationItem.HidesBackButton = true;
			SigningNav.PushViewController (smp, false);
			
			_payment = new PaymentViewController(_navWorkflow);

			SummaryNav.PushViewController (_paySummaryView, false);
			ServerNav.PushViewController (_scView, false);
			CustomerNav.PushViewController (_customersView, false);

			this.ShouldSelectViewController = delegate(UITabBarController tabBarController, UIViewController viewController) 
			{
				LastSelectedTab = this.SelectedIndex;
				// _scView.Log ("DetailedTabs.ShouldSelectViewController: attempt to select "+viewController.GetType().Name);
				if (viewController.GetType().Name == "CustomerNavigationController")
				{
					if (MyNavigationBar.Hidden) MyNavigationBar.Hidden=false;
					SetNavigationButtons (NavigationButtonsMode.CustomerDetails);
				}
				if (viewController.GetType ().Name == "JobHistoryViewController")
				{ if (MyNavigationBar.Hidden) MyNavigationBar.Hidden = false; }
				if (viewController.GetType().Name == "Memos")
				{	
					if (MyNavigationBar.Hidden) MyNavigationBar.Hidden=false;
					SetNavigationButtons (NavigationButtonsMode.CustomerMemos);
				}
				if (viewController.GetType ().Name == "PaymentsSummaryNavigationController")
				{	MyNavigationBar.Hidden = true;	}
				if  (viewController.GetType ().Name == "TakePhotosViewController")
				{
					if (_jobRunTable.TableView.IndexPathForSelectedRow == null)
					{
						// show alert telling the user to select a customer
						using(var alert = new UIAlertView("Please", "Select a customer", null, "OK", null))
						{
							alert.Show();
							return false;
						}
					}
				}
				if (viewController.GetType ().Name == "WorkflowNavigationController")
				{
					StartWorkflow();
				}
				
				// if all else fails :-)
				return true;
			};
			
			this.Mode = DetailedTabsMode.Lookup;
		}

		void DoReprintDocsForCustomer()
		{
			if (_jobRunTable.CurrentCustomer != null)
			{
				ThreadStart ts = new ThreadStart( delegate {
					LoadingView wait = new LoadingView();
					wait.Show ("Printing, please wait");
					bool printOK = false;

					foreach (string fileName in _jobRunTable.CurrentCustomer.FilesToPrint)
					{
						ManualResetEvent printDone = new ManualResetEvent(false);
						ThreadStart tsPrintStart = new ThreadStart( delegate {
							printOK = MyConstants.PrintPDFFile (fileName);
							printDone.Set ();
						});

						Thread tPrintFile = new Thread(tsPrintStart);
						tPrintFile.Start ();
						printDone.WaitOne (5000);
						if (tPrintFile.ThreadState == ThreadState.Running)
							tPrintFile.Abort ();
					}

					InvokeOnMainThread (delegate {
						wait.Hide ();

						if (printOK)
						{
							
						}
						else {
							var printError = new UIAlertView("Error", "An error occured during printing...", null, "OK");
							printError.Show ();
						}
					});

				});

				Thread t = new Thread(ts);
				t.Start();
			}
			else {
				var noCustomerSelected = new UIAlertView("", "Please select a customer first", null, "OK");
				noCustomerSelected.Show ();
			}
		}

		void DoShowCustomerAppleMaps()
		{
			Customer currentCustomer = this._jobRunTable.CurrentCustomer;
			string tmp = String.Format ("http://maps.apple.com/?q={0}+{1}", currentCustomer.Address.Replace (' ', '+'), currentCustomer.Suburb.Replace (' ', '+'));
			NSUrl url = new NSUrl(tmp);
			UIApplication.SharedApplication.OpenUrl (url);
		}

		void DoShowCustomerGoogleMaps()
		{
			if (_jobRunTable.CurrentCustomer != null)
			{
				UIView.BeginAnimations (null);
				UIView.SetAnimationDuration (0.3f);

				UIViewController mapController = new UIViewController();
				UIWebView map = new UIWebView(new RectangleF(0, 40, this._customersView.View.Frame.Width, this._customersView.View.Frame.Height-40));
				map.LoadHtmlString ( BuildHTML (), new NSUrl( Path.Combine (NSBundle.MainBundle.BundlePath, "Content/"), true));
				mapController.Add (map);
				if (this.CustomerNav.ViewControllers.Count() < 2) 
					this.CustomerNav.PushViewController (mapController, true);
				this.MyNavigationBar.Hidden = true;
				CustomerNav.NavigationBarHidden = false;
				UIView.CommitAnimations ();
			}
			else
			{
				// shows alert that row must be selected for this functionality to work
				using(var alert = new UIAlertView("Please", "Select a customer", null, "OK", null))
				{
					alert.Show();
					_jobRunTable.TableView.ReloadData ();
				}
			}
		}

		public string BuildHTML()
		{
			string speechmark = ((char)(34)).ToString();
			StringBuilder buildHTML = new StringBuilder();
			buildHTML.AppendLine("<!DOCTYPE html>");
			buildHTML.AppendLine("<html xmlns=%%1%%http://www.w3.org/1999/xhtml%%1%% xmlns:v=%%1%%urn:schemas-microsoft-com:vml%%1%%>");
			buildHTML.AppendLine("<head>");
			buildHTML.AppendLine("<meta http-equiv=%%1%%content-type%%1%% content=%%1%%text/html; charset=utf-8%%1%%/>");
			buildHTML.AppendLine("<title>Google Street View</title>");
			buildHTML.AppendLine("<link href=%%1%%/maps/documentation/javascript/examples/default.css%%1%% rel=%%1%%stylesheet%%1%% type=%%1%%text/css%%1%% />");
			buildHTML.AppendLine("<script src=%%1%%https://maps.googleapis.com/maps/api/js?sensor=false%%1%% type=%%1%%text/javascript%%1%%></script>");
			buildHTML.AppendLine("<script type=%%1%%text/javascript%%1%%>");

			buildHTML.AppendLine("<script type=%%1%%text/javascript%%1%% src=%%1%%http://maps.googleapis.com/maps/api/js?sensor=false%%1%%></script>");

			buildHTML.AppendLine("<script type=%%1%%text/javascript%%1%%>");
			    buildHTML.AppendLine("var geocoder;");
			    buildHTML.AppendLine("var map;");
			    buildHTML.AppendLine("var address = '';");
			    buildHTML.AppendLine("var panorama;");

			    buildHTML.AppendLine("var panoramaOptions = {");
			        buildHTML.AppendLine("position: fenway,");
			            buildHTML.AppendLine("pov: {heading: 34,");
			                buildHTML.AppendLine("pitch: 10,");
			                buildHTML.AppendLine("zoom: 1}");
			    buildHTML.AppendLine("};");

			    buildHTML.AppendLine("function initialize() {");
			        buildHTML.AppendLine("geocoder = new google.maps.Geocoder();");
			        buildHTML.AppendLine("var latlng = new google.maps.LatLng(51.507526, -0.12795);");
			        buildHTML.AppendLine("var myOptions = {");
			            buildHTML.AppendLine("zoom: 18,"); // 18
			            buildHTML.AppendLine("center: latlng,");
			            buildHTML.AppendLine("mapTypeId: google.maps.MapTypeId.ROADMAP");
			        buildHTML.AppendLine("}");
			        buildHTML.AppendLine("map = new google.maps.Map(document.getElementById(%%1%%map_canvas%%1%%), myOptions);");    
			        buildHTML.AppendLine("panorama = new google.maps.StreetViewPanorama(document.getElementById(%%1%%panorama%%1%%),panoramaOptions);");
			        // buildHTML.AppendLine("map.setStreetView(panorama);");
					buildHTML.AppendLine("returnMapLocation();");
			    buildHTML.AppendLine("}");

			    buildHTML.AppendLine("function returnMapLocation() {");
			        buildHTML.AppendLine("var address = document.getElementById(%%1%%address%%1%%).value;");
			        buildHTML.AppendLine("geocoder.geocode( { 'address': address, 'region': 'au'}, function(results, status) {");
			            buildHTML.AppendLine("if (status == google.maps.GeocoderStatus.OK) {");
			                buildHTML.AppendLine("map.setCenter(results[0].geometry.location);");
			                buildHTML.AppendLine("var marker = new google.maps.Marker({");
			                    buildHTML.AppendLine("map: map,");
			                    buildHTML.AppendLine("position: results[0].geometry.location");
			                buildHTML.AppendLine("});");

			                buildHTML.AppendLine("panorama = new google.maps.StreetViewPanorama(document.getElementById(%%1%%panorama%%1%%),panoramaOptions);");
			                buildHTML.AppendLine("map.setStreetView(panorama);");

			            buildHTML.AppendLine("} else {");
			                buildHTML.AppendLine("alert(%%1%%Could not find address: %%1%% + status);");
			            buildHTML.AppendLine("}");
			        buildHTML.AppendLine("});");
			    buildHTML.AppendLine("}");
			buildHTML.AppendLine("</script>");
			buildHTML.AppendLine("<body onload=%%1%%initialize()%%1%%>");
			  buildHTML.AppendLine("<div>");
			buildHTML.AppendLine("<input id=%%1%%address%%1%% type=%%1%%textbox%%1%% style=%%1%%height:30px;width:480px;font-size:14pt;%%1%% value=%%1%%"+this._jobRunTable.CurrentCustomer.Address+" "+this._jobRunTable.CurrentCustomer.Suburb+"%%1%%>");
			    buildHTML.AppendLine("<input type=%%1%%button%%1%% style=%%1%%height:25px;width:100px;font-size:14pt;%%1%% value=%%1%%Search%%1%% onclick=%%1%%returnMapLocation()%%1%%>");
			  buildHTML.AppendLine("</div>");
			        buildHTML.AppendLine("<div id=%%1%%map_canvas%%1%% style=%%1%%width:600px; height:600px%%1%%></div>");
			        // buildHTML.AppendLine("<div id=%%1%%panorama%%1%%   style=%%1%%width: 320px; height: 300px%%1%%></div>");
			buildHTML.AppendLine("</body>");

			buildHTML.AppendLine("</html>");
			buildHTML = buildHTML.Replace("%%1%%", speechmark);
			return buildHTML.ToString();
		}


		void DoRearrange()
		{
			UIView.BeginAnimations (null);
			UIView.SetAnimationDuration (0.5);
			_jobRunTable.TableView.SetEditing(true, true);	// table enters editing mode				
			this.SelectedViewController.View.Alpha = 0.25f;
			this.SelectedViewController.View.UserInteractionEnabled = false;
			_btnEdit = new UIBarButtonItem("Done", UIBarButtonItemStyle.Done, _doneEditingJobList);			// the edit button should change appearance and event handler
			foreach(UITabBarItem item in this.TabBar.Items)
			{
				item.Enabled = false;
			}
			MyNavigationBar.TopItem.SetRightBarButtonItems(new UIBarButtonItem[] { _btnEdit }, true);		// pushes new button objects into navigation bar
			UIView.CommitAnimations ();
			
		}
		
		void DoResetToDefault()
		{
			_jobRunTable._ds.LoadJobRun (false);
			_jobRunTable.TableView.ReloadData ();
		}
			
		void HandleStartOverAlertWillDismiss (object sender, UIButtonEventArgs e)
		{
			switch (e.ButtonIndex)
			{
			case 1: { // user clicked "Start over"
				Job selectedJob = _jobRunTable.CurrentJob;

				_navWorkflow.ResetWorkflowForJob (selectedJob);
				
				MyNavigationBar.Hidden = true;
				_jobRunTable.TableView.UserInteractionEnabled = false;	
				_navWorkflow.SetToolbarHidden (false, true);
				UIView.BeginAnimations (null);
				UIView.SetAnimationDuration (0.3f);
				this.Mode = DetailedTabsMode.Workflow;
				// this.SelectedViewController = this.ViewControllers[3];
				UIView.CommitAnimations ();

				this._app.myLocationManager.StartUpdatingLocation ();
				this._app.myLocationManager.StartMonitoringSignificantLocationChanges ();

				/* OLD LOGIC -- NAVWORKFLOW pushing viewcontrollers
				if (_navWorkflow.ViewControllers.Length == 0) 
					_navWorkflow.PushViewController (_prePlumbView, true);
				SetNavigationButtons (NavigationButtonsMode.PrePlumbing);
				this.SelectedViewController = this.ViewControllers[3];
				*/
				
				break;
			}
			case 0: {
				break;
			}
			}
		}

		void Handle_deleteMemoAlertWillDismiss (object sender, UIButtonEventArgs e)
		{
			if (e.ButtonIndex == 1)
			{
				int row = _memosView.MemosTable.IndexPathForSelectedRow.Row;
				long cn = _memosView.CustomerMemos[row].MemoCustomerNumber;
				// delete memo from database (DELETE FROM Wcmemo WHERE...)
				string dbPath = ServerClientViewController.dbFilePath;
				if (File.Exists( dbPath ))
				{
					string sql = 	"DELETE FROM Wcmemo WHERE wctime = :_timeentered AND cusnum=:_cn";	// Deleting memo in WCMEMO table
					
					// create SQLite connection to file and write the data
					SqliteConnection connection = new SqliteConnection("Data Source="+dbPath);
					using (SqliteCommand cmd = connection.CreateCommand())
					{
						connection.Open();
						cmd.CommandText = sql;

						cmd.Parameters.Add("_cn", System.Data.DbType.Double).Value = cn;		// customer number
						cmd.Parameters.Add("_timeentered", System.Data.DbType.String).Value = _memosView.CustomerMemos[row].MemoTimeEntered.ToString ("HH:mm:ss");

						// TODO:: error handling here :: IMPORTANT since if DELETE statement won't execute for some weird reason, there will be discrepancy between database and displayed memo list
						cmd.ExecuteNonQuery();
						
						// if the update statement did execute correctly, we'll have to remove that memo from the list of customer memos (_memosView.CustomerMemos)
						_memosView.CustomerMemos.RemoveAt(row);
					}
					connection.Close();
				}
				else /* ! FileExists(dbPath) */
				{
					// DB file doesn't exist, cannot do much about that
					using(var alert = new UIAlertView("Database problem", "Database file not found. Cannot update memo.", null, "Oh noes!", null))
						{
							alert.Show();
						}							
				}
				_memosView.ReloadMemosTable();
			}
			else
			{
				return;  // user pressed "Cancel"
			}
		} // end void Handle_deleteMemoAlertWillDismiss

		void Handle_newMemoViewWillDismiss (object sender, UIButtonEventArgs e)
		{
			if (e.ButtonIndex == 1) {
				// implement writing memos to database here
				string dbPath = ServerClientViewController.dbFilePath;
				if (File.Exists( dbPath ))
				{
					string sql = 	"INSERT INTO Wcmemo (cusnum, wmdate, wmtype, wmm, wmore, wctime, repnum, wirdesc, wdone, towgo, callt, wdate_done, wmemnum) VALUES " +
											"(:_cusnum, :_wmdate, :_wmtype, :_wmm, :_wmore, :_wctime, :_repnum, \"\", 0, 0, 0, '1996-01-01', :_wmemnum)";		// Writing created memo into WCMEMO table
					
					// create SQLite connection to file and write the data
					SqliteConnection connection = new SqliteConnection("Data Source="+dbPath);
					using (SqliteCommand cmd = connection.CreateCommand())
					{
						connection.Open();
						
						cmd.Parameters.Add("_cusnum", System.Data.DbType.Double).Value = _jobRunTable.CurrentCustomer.CustomerNumber;	// number of the customer selected
						cmd.Parameters.Add("_wmdate", System.Data.DbType.String).Value = DateTime.Now.ToString ("yyyy-MM-dd");			// date entered
						cmd.Parameters.Add("_wctime", System.Data.DbType.String).Value = DateTime.Now.ToString ("HH:mm:ss");			// time entered
						cmd.Parameters.Add("_wmtype", System.Data.DbType.String).Value = "FRC"; 										// stands for FRachisee Comment
						cmd.Parameters.Add("_wmm", System.Data.DbType.String).Value = "Entered on an iPad";								// memo description
						cmd.Parameters.Add ("_wmore", System.Data.DbType.String).Value = _newMemoView.GetTextField (0).Text; 			// memo contents
						cmd.Parameters.Add ("_repnum", System.Data.DbType.Int64).Value = MyConstants.EmployeeID;						// employee id
						cmd.Parameters.Add("_wmemnum", System.Data.DbType.Double).Value = MyConstants.DUMMY_MEMO_NUMBER; 				// arbitrary large number to be replaced by a proper memo number when processing iPad data in FoxPro

						cmd.CommandText = sql;
						// TODO:: error handling here :: IMPORTANT since if INSERT statement won't execute for some weird reason, there will be discrepancy between database and displayed memo list (until an app restarts)
						cmd.ExecuteNonQuery();
						
						// if the insert statement did execute correctly, we'll add the memo to a list customer's memos
						Memo m = new Memo(_newMemoView.GetTextField (0).Text);
						m.MemoCustomerNumber = _jobRunTable.CurrentCustomer.CustomerNumber;
						m.MemoDateEntered  = DateTime.Now;
						m.MemoTimeEntered = DateTime.Now;
						m.MemoType = "FRC";
						m.MemoDescription = "Entered on an iPad";
						m.Editable = true;
						
						_jobRunTable.CurrentCustomer.CustomerMemos.Add (m);
						_memosView.ReloadMemosTable();
					}
					
				}
				else /* ! FileExists(dbPath) */
				{
					// DB file doesn't exist, cannot do much about that
					using(var alert = new UIAlertView("Database problem", "Database file not found. Cannot save memo.", null, "Oh noes!", null))
						{
							alert.Show();
						}
					return;
				}
			}
			else /* e.ButtonIndex != 1 */
			{
				// User pressed "Cancel"
				return;
			}
		} // end void Handle_newMemoViewWillDismiss
		
		public void SetNavigationButtons(NavigationButtonsMode mode)
		{
			switch(mode) {
				case NavigationButtonsMode.CustomerDetails: {
					BtnEdit = new UIBarButtonItem("More actions", UIBarButtonItemStyle.Bordered, _editJobList);		// this button allows to rearrange cells in the table on the left
					BtnStuff = new UIBarButtonItem("On same street", UIBarButtonItemStyle.Bordered, _searchCustomersByStreet);	// allows to look up customers on the same street
					if (_jobRunTable.CurrentJob == null) {
						BtnStuff.Enabled = false;
						// BtnEdit.Enabled = false;
					}
					// if (NavigationBar.TopItem.LeftBarButtonItem == null) NavigationBar.TopItem.SetLeftBarButtonItem (BtnStartWorkflow, true); // .LeftBarButtonItem = BtnStartWorkflow;
					BtnStartWorkflow = new UIBarButtonItem ("Start workflow", UIBarButtonItemStyle.Done, _startWorkflow); 
					MyNavigationBar.TopItem.SetRightBarButtonItems(new UIBarButtonItem [] { BtnStartWorkflow, /*BtnStuff,*/ BtnEdit }, true);		// adds the buttons to navigation bar			
					break;			
				}
				case NavigationButtonsMode.CustomerMemos: {
					BtnStuff = new UIBarButtonItem("New Memo", UIBarButtonItemStyle.Bordered, CreateNewMemo);
					BtnEdit = new UIBarButtonItem("Delete Memo", UIBarButtonItemStyle.Bordered, DeleteSelectedMemo);
					MyNavigationBar.TopItem.SetRightBarButtonItems(new UIBarButtonItem[] { BtnStuff, BtnEdit }, true);
					break;
				}
				case NavigationButtonsMode.PrePlumbing: {
					BtnEdit = new UIBarButtonItem("Job was not done", UIBarButtonItemStyle.Bordered, _jobWasNotDoneClicked);
					BtnStuff = _navWorkflow.FlexibleButtonSpace;
					MyNavigationBar.TopItem.SetRightBarButtonItems(new UIBarButtonItem[] { BtnStuff, BtnEdit }, true);				
					break;
				}
				case NavigationButtonsMode.JobHistory: {
					BtnStuff = _navWorkflow.FlexibleButtonSpace;
					BtnEdit = _navWorkflow.FlexibleButtonSpace;					
					MyNavigationBar.TopItem.SetRightBarButtonItems(new UIBarButtonItem[] { BtnStuff, BtnEdit }, true);				
					break;
				}
				case NavigationButtonsMode.ServerClient: {
					BtnStuff = _navWorkflow.FlexibleButtonSpace;
					BtnEdit = _navWorkflow.FlexibleButtonSpace;					
					MyNavigationBar.TopItem.SetRightBarButtonItems(new UIBarButtonItem[] { BtnStuff, BtnEdit }, true);				
					break;
				}
			}
		}

		[Obsolete]
		public override bool ShouldAutorotateToInterfaceOrientation (UIInterfaceOrientation toInterfaceOrientation)
		{ // allows to adjust the view when device is turned
			return true; // (toInterfaceOrientation == UIInterfaceOrientation.LandscapeLeft || toInterfaceOrientation == UIInterfaceOrientation.LandscapeRight);
		}
		
		public override void ViewDidLoad ()
		{ // allows to implement additional customizations to be applied immidiately after loading the view (before showing it to the end user)
			base.ViewDidLoad ();
		}
		
		public override void ViewWillAppear (bool animated)
		{
			base.ViewWillAppear (animated);
		}
		
		public void AddLeftNavBarButton (UIBarButtonItem button)		// called when device enters portrait mode
        {
            button.Title = "Customer List";
			MyNavigationBar.TopItem.SetLeftBarButtonItem (button, false);	// this adds the butoon on a toolbar which allows to select a customer in portrait orientation
        }

        public void RemoveLeftNavBarButton ()									// called when device enters landscape mode
        {
			MyNavigationBar.TopItem.SetLeftBarButtonItem (null, false);		// this removes the toolbar button that serves no purpose in landscape orientation
        }
	}
/*	
	public class TabsDelegate : UITabBarControllerDelegate
	{
		public TabsDelegate(DetailedTabs tabs) 
		{
			// this._tabs = tabs;
		}
		public override bool ShouldSelectViewController (UITabBarController tabBarController, UIViewController viewController)
		{
			// Console.WriteLine ("TabsDelegate: should select "+viewController.GetType ().ToString ());
			return true;
		}
		public override void ViewControllerSelected (UITabBarController tabBarController, UIViewController viewController)
		{
			
		}
	}
	*/
}

