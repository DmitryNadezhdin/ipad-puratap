using MonoTouch.UIKit;
using System.Drawing;
using System;
using System.Data;
using MonoTouch.Foundation;
using System.IO;
using Mono.Data.Sqlite;
using MonoTouch.CoreGraphics;
using ZSDK_Test;

namespace Puratap
{
	public partial class CustomersViewController : UIViewController
	{
		DetailedTabs _tabs;
		
		#region Displayed customer properties: first name, last name, etc.
		private string _customerCompanyName;
		private string _customerFirstName;
		private string _customerLastName;
		private string _customerMainAddress;
		private string _customerSuburb;
		private string _customerPhoneNumber;
		private string _customerMobileNumber;
		private DateTime _lastInstallDate;
		private string _fallbackContact;
		private string _fallbackPhone;
		private int _numberOfMemos;
		private string _lastJobType;

		public string CompanyName
		{
			get { return _customerCompanyName; }
			set {
				_customerCompanyName = value;
				this.cCompanyName.Text = _customerCompanyName;
			}
		}

		public string FirstName 
		{ 

			get { return _customerFirstName; }
			set { 
				_customerFirstName = value;
				this.cFirstName.Text = _customerFirstName;
			} 
		}	
		public string LastName 
		{ 
			get { return _customerLastName; }
			set { 
				_customerLastName = value;
				this.cLastName.Text = _customerLastName;
			} 
		}
		public string MainAddress 
		{ 
			get { return _customerMainAddress; }
			set { 
				_customerMainAddress = value;
				this.cAddress.Text = _customerMainAddress;
			} 
		}	
		public string Suburb 
		{ 
			get { return _customerSuburb; }
			set { 
				_customerSuburb = value;
				this.cSuburb.Text = _customerSuburb;
			} 
		}	
		public string PhoneNumber 
		{ 
			get { return _customerPhoneNumber; }
			set { 
				_customerPhoneNumber = value;
				this.cPhoneNumber.Text = _customerPhoneNumber;
			} 
		}
		public string MobileNumber 
		{ 
			get { return _customerMobileNumber; }
			set { 
				_customerMobileNumber = value;
				this.cMobileNumber.Text = _customerMobileNumber;
			} 
		}
		public DateTime LastInstallDate
		{ 
			get { return _lastInstallDate; }
			set { 
				_lastInstallDate = value;
				this.cLastInstallDate.Text = _lastInstallDate.ToString("dd/MM/yyyy");
			} 
		}
		public string FallbackContact
		{
			get { return _fallbackContact; }
			set { 
				_fallbackContact = value;
				this.cFallbackContact.Text = _fallbackContact;
			}
		}
		public string FallbackPhone
		{
			get { return _fallbackPhone; }
			set { 
				_fallbackPhone = value;
				this.cFallbackPhone.Text = _fallbackPhone;
			}
		}
		
		public string LastJobType
		{
			get { return _lastJobType; }
			set {
				_lastJobType = value;
				this.cLastJobType.Text = _lastJobType;
			}
		}
		#endregion
		
		#region Displayed job details: date, price, type, comments, etc.
		private long _JobBookingNumber;		
		private long _JobCustomerNumber;
		private long _JobUnitNumber;
		private DateTime _JobTime;
		private DateTime _JobDate;
		private string _JobType;
		private double _JobMoneyToCollect;
		private string _JobSpecialInstructions;
		private string _JobPlumbingComments;
		private bool _attentionFlag;
		private string _attentionReason;
		private string _contactPerson;

		public string ContactPerson
		{
			get { return _contactPerson; }
			set {
				_contactPerson = value;
				this.cContactPerson.Text = _contactPerson;
			}
		}

		public string AttentionReason
		{
			get { return _attentionReason; }
			set {
				_attentionReason = value;
				this.cAttentionReason.Text = _attentionReason;
			}
		}

		public bool AttentionFlag
		{
			get { return _attentionFlag; }
			set {
				UIView.SetAnimationDuration (0.5);
				UIView.BeginAnimations(null);

				// update job record in the database if the value changed
				if (_tabs._jobRunTable.CurrentJob != null && value != _tabs._jobRunTable.CurrentJob.AttentionFlag)
				{
					if (File.Exists (ServerClientViewController.dbFilePath))
					{
						// UPDATE the job's record in PL_RECOR, setting ATTENTION flag to the supplied value
						using (var connection = new SqliteConnection("Data Source="+ServerClientViewController.dbFilePath) )
						{
							connection.Open();
							var cmd = connection.CreateCommand();
							string val = (value)? "1" : "0";
							cmd.CommandText = "UPDATE PL_RECOR SET ATTENTION="+val+
								" WHERE BOOKNUM="+this.JobBookingNumber;
							cmd.ExecuteNonQuery ();
							this._tabs._jobRunTable.CurrentJob.AttentionFlag = value;

							// redraw the cells

							// this._tabs._jobRunTable.TableView.ReloadData (); -- this is so blunt, almost rude
							this._tabs._jobRunTable.TableView.ReloadRows ( new NSIndexPath[] { this._tabs._jobRunTable.LastSelectedRowPath }, UITableViewRowAnimation.Automatic);
						}
					}
				}
				// display the value
				_attentionFlag = value;

				if (_attentionFlag) 				
					SetAttentionReasonVisible ();
				else SetAttentionReasonHidden ();

				UIView.CommitAnimations ();
			}
		}
		
		public long JobBookingNumber
		{ 
			get { return _JobBookingNumber; }
			set { _JobBookingNumber = value;
				this.cBookingNumber.Text = _JobBookingNumber.ToString();
			}
		}
		
		public long CustomerNumber
		{ 
			get { return _JobCustomerNumber; }
			set { 
				_JobCustomerNumber = value;
				this.cCustomerNumber.Text = _JobCustomerNumber.ToString();
			}
		}
		
		public long UnitNumber
		{
			get { return _JobUnitNumber; }
			set {
				_JobUnitNumber = value;
				this.cUnitNumber.Text = _JobUnitNumber.ToString();
			}
		}
		
		public DateTime JobTime
		{
			get { return _JobTime; }
			set { 
				_JobTime = value;
				// this.cJobTime.Text = _JobTime.ToString ("dd/MM/yyyy hh:mm tt");
			}
		}
		public DateTime JobDate
		{
			get { return _JobDate; }
			set { 
				_JobDate = value;
				// this.cJobTime.Text = _JobDate.ToString("dd/MM/yyyy");
			}
		}
		public string JobType
		{
			get { return _JobType; }
			set { 
				_JobType = value;
				this.cJobType.Text = _JobType.ToString();
			}
		}
		public double MoneyToCollect
		{
			get { return _JobMoneyToCollect; }
			set { 
				_JobMoneyToCollect = value;
				this.cMoneyToCollect.Text = String.Format("${0}", _JobMoneyToCollect.ToString() );
			}
		}
		public string JobSpecialInstructions
		{
			get { return _JobSpecialInstructions; }
			set { 
				_JobSpecialInstructions = value;
				this.cSpecialInstructions.Text = _JobSpecialInstructions;
			}
		}
		public string JobPlumbingComments
		{
			get { return _JobPlumbingComments; }
			set { 
				_JobPlumbingComments = value;
				this.cPlumbingComments.Text = _JobPlumbingComments;
			}
		}
		
		public int NumberOfMemos
		{
			get { return _numberOfMemos; }
			set {
				_numberOfMemos = value;
				this.cNumberOfMemos.Text = _numberOfMemos.ToString();
			}
		}
		#endregion
		
		public CustomersViewController (DetailedTabs tabs) : base ("CustomersViewController", null)
		{
			this.Title = NSBundle.MainBundle.LocalizedString ("Customer", "Customer");
			using (var image = UIImage.FromBundle ("Images/111-user") ) this.TabBarItem.Image = image;
			this._tabs = tabs;
		}
		
		public override void ViewDidAppear (bool animated)
		{
			base.ViewDidAppear (animated);
			if (_tabs.MyNavigationBar.Hidden)
			{
				_tabs.MyNavigationBar.Hidden = false;
			}
			if (_tabs.MyNavigationBar.TopItem.RightBarButtonItems == null) _tabs.SetNavigationButtons (NavigationButtonsMode.CustomerDetails);

			if (this._tabs.CustomerNav.ViewControllers.Length==1)
			{
				this._tabs.MyNavigationBar.Hidden = false;
				this._tabs.CustomerNav.NavigationBarHidden = true;
			}
		}

		public override void ViewDidLoad ()
		{
			base.ViewDidLoad ();
			
			//any additional setup after loading the view, typically from a nib.
			cFirstName.EditingDidEnd += HandleEditingDidEnd;
			cFirstName.EditingDidEndOnExit += HandleEditingDidEnd;
			
			cLastName.EditingDidEnd += HandleEditingDidEnd;
			cLastName.EditingDidEndOnExit += HandleEditingDidEnd;
			
			cAddress.EditingDidEnd += HandleEditingDidEnd;
			cAddress.EditingDidEndOnExit += HandleEditingDidEnd;
			
			cSuburb.EditingDidEnd += HandleEditingDidEnd;
			cSuburb.EditingDidEndOnExit += HandleEditingDidEnd;
			
			cPhoneNumber.EditingDidEnd += HandleEditingDidEnd;
			cPhoneNumber.EditingDidEndOnExit += HandleEditingDidEnd;
			
			cMobileNumber.EditingDidEnd += HandleEditingDidEnd;
			cMobileNumber.EditingDidEndOnExit += HandleEditingDidEnd;
			
			cFallbackContact.EditingDidEnd += HandleEditingDidEnd;
			cFallbackContact.EditingDidEndOnExit += HandleEditingDidEnd;
			
			cFallbackPhone.EditingDidEnd += HandleEditingDidEnd;
			cFallbackPhone.EditingDidEndOnExit += HandleEditingDidEnd;

			cCompanyName.EditingDidEnd += HandleEditingDidEnd;
			cCompanyName.EditingDidEndOnExit += HandleEditingDidEnd;

			cSpecialInstructions.ShouldBeginEditing = delegate(UITextView textView) {
				// bring the text view up
				UIView.BeginAnimations (null);
				UIView.SetAnimationDuration (0.3);

				cCompanyName.Hidden = true;
				cContactPerson.Hidden = true;
				lbCompanyName.Hidden = true;
				lbContactPerson.Hidden = true;

				int iOSversion = Convert.ToInt32(UIDevice.CurrentDevice.SystemVersion.Split('.')[0]);
				int newY = (iOSversion == 7 || iOSversion == 8)? 45+18 : 45;

				cSpecialInstructions.Frame = new System.Drawing.RectangleF(10, newY, 690, 352);
				this._tabs._jobRunTable.TableView.UserInteractionEnabled = false;
				foreach (UIBarButtonItem btn in this._tabs.MyNavigationBar.TopItem.RightBarButtonItems)	//NavigationItem.RightBarButtonItem.Enabled = false;
					btn.Enabled = false;
				UIView.CommitAnimations ();

				return true;
			};

			cAttentionReason.ShouldBeginEditing = delegate(UITextView textView) {
				// bring the text view up
				UIView.BeginAnimations (null);
				UIView.SetAnimationDuration (0.3);

				cCompanyName.Hidden = true;
				cContactPerson.Hidden = true;
				lbCompanyName.Hidden = true;
				lbContactPerson.Hidden = true;

				int iOSversion = Convert.ToInt32(UIDevice.CurrentDevice.SystemVersion.Split('.')[0]);
				int newY = (iOSversion == 7 || iOSversion == 8)? 45+18 : 45;

				cAttentionReason.Frame = new System.Drawing.RectangleF(10, newY, 690, 352);
				this._tabs._jobRunTable.TableView.UserInteractionEnabled = false;
				foreach (UIBarButtonItem btn in this._tabs.MyNavigationBar.TopItem.RightBarButtonItems)	//NavigationItem.RightBarButtonItem.Enabled = false;
					btn.Enabled = false;
				UIView.CommitAnimations ();

				return true;
			};

			cAttentionReason.BackgroundColor = UIColor.Yellow;
			cAttentionReason.Ended += HandleAttentionReasonEditingEnded;
			cSpecialInstructions.Ended += HandleSpecialInstructionsEditingEnded;

			string imgLocation = (MyConstants.iOSVersion == 8) ? "Images/uicheckbox_unchecked" : "/Images/uicheckbox_unchecked";
			this.btnTUDone.SetImage ( UIImage.FromBundle (imgLocation), UIControlState.Normal);
			imgLocation = (MyConstants.iOSVersion == 8) ? "Images/uicheckbox_checked" : "/Images/uicheckbox_checked";
			this.btnTUDone.SetImage ( UIImage.FromBundle (imgLocation), UIControlState.Selected);
			this.btnTUDone.TouchUpInside += btnTUDoneHandleTouchUpInside;

			imgLocation = (MyConstants.iOSVersion == 8) ? "Images/uicheckbox_unchecked" : "/Images/uicheckbox_unchecked";
			this.btnAttention.SetImage ( UIImage.FromBundle (imgLocation), UIControlState.Normal);
			imgLocation = (MyConstants.iOSVersion == 8) ? "Images/uicheckbox_checked" : "/Images/uicheckbox_checked";
			this.btnAttention.SetImage ( UIImage.FromBundle (imgLocation), UIControlState.Selected);
			this.btnAttention.TouchUpInside += HandleBtnAttentionTouchUpInside;
		}

		void btnTUDoneHandleTouchUpInside (object sender, EventArgs e)
		{


			if (this._tabs._jobRunTable.CurrentCustomer != null)
			{
				if (this._tabs._jobRunTable.CurrentJob != null) {
					this._tabs._jobRunTable.CurrentCustomer.TubingUpgradeDone = !this._tabs._jobRunTable.CurrentCustomer.TubingUpgradeDone;
					this.btnTUDone.Selected = !this.btnTUDone.Selected;

					this.UpdateCustomerInfo (CustomerDetailsUpdatableField.TubingUpgradeDone,
					                         Convert.ToByte (!this._tabs._jobRunTable.CurrentCustomer.TubingUpgradeDone).ToString (),
					                         Convert.ToByte (this._tabs._jobRunTable.CurrentCustomer.TubingUpgradeDone).ToString (),
					                         this._tabs._jobRunTable.CurrentCustomer.CustomerNumber,
					                         this._tabs._jobRunTable.CurrentJob.JobBookingNumber);

					this._tabs._jobRunTable.TableView.BeginUpdates ();
					this._tabs._jobRunTable.TableView.ReloadRows (
						new NSIndexPath[] { this._tabs._jobRunTable.LastSelectedRowPath }, 
						UITableViewRowAnimation.Automatic
					);
					this._tabs._jobRunTable.TableView.EndUpdates ();
				} else {
					var alertNoCustomerSelected = new UIAlertView ("Please", "Select a customer", null, "OK");
					alertNoCustomerSelected.Show ();
				}
			} else {
				var alertNoCustomerSelected = new UIAlertView ("Please", "Select a customer", null, "OK");
				alertNoCustomerSelected.Show ();
			}
		}

		public void DisplayJobTimeFrame(Job j)
		{
			this.cJobTime.Text = j.JobTimeStart.ToString ("dd/MM/yyyy hh:mm tt") + " " +
									j.JobTimeEnd.ToString ("hh:mm tt");
		}

		public void SetBtnTUDoneState(bool tuState)
		{
			this.btnTUDone.Selected = tuState;
		}

		void HandleAttentionReasonEditingEnded (object sender, EventArgs e)
		{
			// bring the text view down
			UIView.BeginAnimations (null);
			UIView.SetAnimationDuration (0.3);
			cAttentionReason.Frame = new System.Drawing.RectangleF(156,657,403,32);
			cCompanyName.Hidden = false;
			lbCompanyName.Hidden = false;
			cCompanyName.Hidden = false;
			cContactPerson.Hidden = false;
			this._tabs._jobRunTable.TableView.UserInteractionEnabled = true;
			// enable the workflow and extra actions buttons
			foreach (UIBarButtonItem btn in this._tabs.MyNavigationBar.TopItem.RightBarButtonItems)	//NavigationItem.RightBarButtonItem.Enabled = false;
				btn.Enabled = true;

			UIView.CommitAnimations ();

			if (this._tabs._jobRunTable.CurrentCustomer != null) 
				this._tabs._jobRunTable.CurrentCustomer.AttentionReason = cAttentionReason.Text;
			this.AttentionReason = cAttentionReason.Text;

			HandleEditingDidEnd (this.cAttentionReason, null);
		}

		void HandleSpecialInstructionsEditingEnded (object sender, EventArgs e)
		{
			// bring the text view down
			UIView.BeginAnimations (null);
			UIView.SetAnimationDuration (0.3);
			cSpecialInstructions.Frame = new System.Drawing.RectangleF(355,454,328,195);
			cCompanyName.Hidden = false;
			cContactPerson.Hidden = false;
			lbCompanyName.Hidden = false;
			lbContactPerson.Hidden = false;
			this._tabs._jobRunTable.TableView.UserInteractionEnabled = true;

			// enable the workflow and extra actions buttons
			foreach (UIBarButtonItem btn in this._tabs.MyNavigationBar.TopItem.RightBarButtonItems)	//NavigationItem.RightBarButtonItem.Enabled = false;
				btn.Enabled = true;

			UIView.CommitAnimations ();

			/*
			if (this._tabs._jobRunTable.CurrentJob.HasParent ())
				this._tabs._jobRunTable.FindParentJob (this._tabs._jobRunTable.CurrentJob).JobSpecialInstructions = cSpecialInstructions.Text;
			else this._tabs._jobRunTable.CurrentJob.JobSpecialInstructions = cSpecialInstructions.Text;
			*/
			this.JobSpecialInstructions = cSpecialInstructions.Text;
			HandleEditingDidEnd(this.cSpecialInstructions, null);
		}

		void HandleBtnAttentionTouchUpInside (object sender, EventArgs e)
		{
			if (this._tabs._jobRunTable.CurrentCustomer != null && this._tabs._jobRunTable.CurrentJob != null) {
				this.btnAttention.Selected = !this.btnAttention.Selected;
				this.AttentionFlag = this.btnAttention.Selected; // swAttention.On;
			} else {
				var alertNoCustomerSelected = new UIAlertView ("Please", "Select a customer", null, "OK");
				alertNoCustomerSelected.Show ();
			}
		}

		public void SetBtnAttentionState(bool state)
		{
			this.btnAttention.Selected = state;
		}

		public void SetAttentionReasonHidden()
		{
			this.cAttentionReason.Hidden = true;
		}

		public void SetAttentionReasonVisible()
		{
			this.cAttentionReason.Hidden = false;
		}

		void HandleEditingDidEnd (object sender, EventArgs e)
		{
			JobRunTable jrt = _tabs._jobRunTable;

			if (jrt.CurrentCustomer != null)
			{
				
				if (jrt.Customers.IndexOf (jrt.CurrentCustomer) != -1)
					jrt.CurrentJob = jrt.MainJobList[ jrt.Customers.IndexOf (jrt.CurrentCustomer) ];
				else jrt.CurrentJob = jrt.UserCreatedJobs[ jrt.UserAddedCustomers.IndexOf (jrt.CurrentCustomer) ];

				CustomerDetailsUpdatableField field = CustomerDetailsUpdatableField.None;
				if (sender is UITextField)
					field = (CustomerDetailsUpdatableField) (sender as UITextField).Tag;
				else if (sender is UITextView)
					field = (CustomerDetailsUpdatableField) (sender as UIView).Tag;
				string OldValue;
				switch(field)
				{
				case CustomerDetailsUpdatableField.FirstName:
					OldValue = jrt.CurrentCustomer.FirstName;
					jrt.CurrentCustomer.FirstName = cFirstName.Text;
					UpdateCustomerInfo (field, OldValue, jrt.CurrentCustomer.FirstName, jrt.CurrentCustomer.CustomerNumber, jrt.CurrentJob.JobBookingNumber);
					_tabs._scView.Log (String.Format ("Database updated for Customer #{0} : Set First Name to {1}", jrt.CurrentCustomer.CustomerNumber, cFirstName.Text));
					break;					
				case CustomerDetailsUpdatableField.LastName:
					OldValue = jrt.CurrentCustomer.LastName;
					jrt.CurrentCustomer.LastName = cLastName.Text;
					UpdateCustomerInfo (field, OldValue, jrt.CurrentCustomer.LastName, jrt.CurrentCustomer.CustomerNumber, jrt.CurrentJob.JobBookingNumber);
					_tabs._scView.Log (String.Format ("Database updated for Customer #{0} : Set Last Name to {1}", jrt.CurrentCustomer.CustomerNumber, cLastName.Text));
					break;					
				case CustomerDetailsUpdatableField.Address:
					OldValue = jrt.CurrentCustomer.Address;
					jrt.CurrentCustomer.Address = cAddress.Text;
					UpdateCustomerInfo (field, OldValue, jrt.CurrentCustomer.Address, jrt.CurrentCustomer.CustomerNumber, jrt.CurrentJob.JobBookingNumber);
					_tabs._scView.Log (String.Format ("Database updated for Customer #{0} : Set Address to {1}", jrt.CurrentCustomer.CustomerNumber, cAddress.Text));
					break;										
				case CustomerDetailsUpdatableField.Suburb:
					OldValue = jrt.CurrentCustomer.Suburb;
					jrt.CurrentCustomer.Suburb = cSuburb.Text;
					UpdateCustomerInfo (field, OldValue, jrt.CurrentCustomer.Suburb, jrt.CurrentCustomer.CustomerNumber, jrt.CurrentJob.JobBookingNumber);
					_tabs._scView.Log (String.Format ("Database updated for Customer #{0} : Set Suburb to {1}", jrt.CurrentCustomer.CustomerNumber, cSuburb.Text));
					break;

				case CustomerDetailsUpdatableField.Phone:
					OldValue = jrt.CurrentCustomer.PhoneNumber;

					string newPhoneValue = String.Empty;
					for (int i = 0; i < cPhoneNumber.Text.Length; i++)
					{
						char c = cPhoneNumber.Text[i];
						if ( char.IsDigit (c) || char.IsWhiteSpace(c) )
							newPhoneValue += c;
					}

					jrt.CurrentCustomer.PhoneNumber = newPhoneValue;
					cPhoneNumber.Text = newPhoneValue;

					UpdateCustomerInfo (field, OldValue, jrt.CurrentCustomer.PhoneNumber, jrt.CurrentCustomer.CustomerNumber, jrt.CurrentJob.JobBookingNumber);
					_tabs._scView.Log (String.Format ("Database updated for Customer #{0} : Set Phone Number to {1}", jrt.CurrentCustomer.CustomerNumber, cPhoneNumber.Text));
					break;					

				case CustomerDetailsUpdatableField.MobilePhone:
					OldValue = jrt.CurrentCustomer.MobileNumber;

					string newMobileValue = String.Empty;
					for (int i = 0; i < cMobileNumber.Text.Length; i++)
					{
						char c = cMobileNumber.Text[i];
						if ( char.IsDigit (c) || char.IsWhiteSpace(c) )
							newMobileValue += c;
					}

					jrt.CurrentCustomer.MobileNumber = newMobileValue;
					cMobileNumber.Text = newMobileValue;

					UpdateCustomerInfo (field, OldValue, jrt.CurrentCustomer.MobileNumber, jrt.CurrentCustomer.CustomerNumber, jrt.CurrentJob.JobBookingNumber);
					_tabs._scView.Log (String.Format ("Database updated for Customer #{0} : Set Mobile Phone Number to {1}", jrt.CurrentCustomer.CustomerNumber, cMobileNumber.Text));
					break;										
				case CustomerDetailsUpdatableField.FallbackContact:
					OldValue = jrt.CurrentCustomer.FallbackContact;
					jrt.CurrentCustomer.FallbackContact = cFallbackContact.Text;
					UpdateCustomerInfo (field, OldValue, jrt.CurrentCustomer.FallbackContact, jrt.CurrentCustomer.CustomerNumber, jrt.CurrentJob.JobBookingNumber);
					_tabs._scView.Log (String.Format ("Database updated for Customer #{0} : Set Fallback Contact to {1}", jrt.CurrentCustomer.CustomerNumber, cFallbackContact.Text));
					break;					
				case CustomerDetailsUpdatableField.FallbackPhone:
					OldValue = jrt.CurrentCustomer.FallbackPhoneNumber;

					string newFallbackPhoneValue = String.Empty;
					for (int i = 0; i < cFallbackPhone.Text.Length; i++)
					{
						char c = cFallbackPhone.Text[i];
						if ( char.IsDigit (c) || char.IsWhiteSpace(c) )
							newFallbackPhoneValue += c;
					}

					jrt.CurrentCustomer.FallbackPhoneNumber = newFallbackPhoneValue;
					cFallbackPhone.Text = newFallbackPhoneValue;

					UpdateCustomerInfo (field, OldValue, jrt.CurrentCustomer.FallbackPhoneNumber, jrt.CurrentCustomer.CustomerNumber, jrt.CurrentJob.JobBookingNumber);
					_tabs._scView.Log (String.Format ("Database updated for Customer #{0} : Set Fallback Phone to {1}", jrt.CurrentCustomer.CustomerNumber, cFallbackPhone.Text));
					break;
				case CustomerDetailsUpdatableField.CompanyName:
					OldValue = jrt.CurrentCustomer.CompanyName;
					jrt.CurrentCustomer.CompanyName = cCompanyName.Text;
					UpdateCustomerInfo (field, OldValue, jrt.CurrentCustomer.CompanyName, jrt.CurrentCustomer.CustomerNumber, jrt.CurrentJob.JobBookingNumber);
					_tabs._scView.Log (String.Format ("Database updated for Customer #{0} : Set Company Name to {1}", jrt.CurrentCustomer.CustomerNumber, cCompanyName.Text));
					break;

				case CustomerDetailsUpdatableField.SpecialComments:
					OldValue = (jrt.CurrentJob.HasParent ()) ? jrt.FindParentJob(jrt.CurrentJob).JobSpecialInstructions : jrt.CurrentJob.JobSpecialInstructions;
					if (jrt.CurrentJob.HasParent ())
					{
						jrt.FindParentJob(jrt.CurrentJob).JobSpecialInstructions = cSpecialInstructions.Text;
						UpdateCustomerInfo (field, OldValue, this.cSpecialInstructions.Text, jrt.CurrentCustomer.CustomerNumber, jrt.FindParentJob(jrt.CurrentJob).JobBookingNumber);
					}
					else 
					{
						jrt.CurrentJob.JobSpecialInstructions = cSpecialInstructions.Text;
						UpdateCustomerInfo (field, OldValue, this.cSpecialInstructions.Text, jrt.CurrentCustomer.CustomerNumber, jrt.CurrentJob.JobBookingNumber);
					}
					break;

				case CustomerDetailsUpdatableField.AttentionReason:
					OldValue = (jrt.CurrentJob.HasParent ()) ? jrt.FindParentJob(jrt.CurrentJob).AttentionReason : jrt.CurrentJob.AttentionReason;
					if (jrt.CurrentJob.HasParent ())
					{
						jrt.FindParentJob(jrt.CurrentJob).AttentionReason = cAttentionReason.Text;
						UpdateCustomerInfo (field, OldValue, this.cAttentionReason.Text, jrt.CurrentCustomer.CustomerNumber, jrt.FindParentJob(jrt.CurrentJob).JobBookingNumber);
					}
					else 
					{
						jrt.CurrentJob.AttentionReason = cAttentionReason.Text;
						UpdateCustomerInfo (field, OldValue, this.cAttentionReason.Text, jrt.CurrentCustomer.CustomerNumber, jrt.CurrentJob.JobBookingNumber);
					}
					break;

				default: 
					if (_tabs._scView != null)
					{
						_tabs._scView.Log (String.Format ("HandleEditingDidEnd: Attempt to update unknown field: Tag {0}", field));					
					}
					break;
				}
				
				// jrt.CurrentJob = null;
				if (jrt.HighlightedMode) jrt.HighlightedMode = false;
				if (jrt.LastSelectedRowPath != null) 
				{
					jrt.TableView.ReloadRows (new NSIndexPath[] { jrt.LastSelectedRowPath }, UITableViewRowAnimation.Automatic);

					for (int i = 0; i < jrt.TableView.NumberOfRowsInSection (0); i++)
					{
							if (jrt.Customers[i].HighLighted) 
								{ jrt.TableView.ReloadRows ( new NSIndexPath[] { NSIndexPath.FromRowSection (i,0) }, UITableViewRowAnimation.Automatic ); }
					}
					
					if (jrt.TableView.NumberOfRowsInSection (1) > 0)
						for (int i = 0; i < jrt.TableView.NumberOfRowsInSection (1); i++)
						{
									if (jrt.UserAddedCustomers[i].HighLighted) 
										{ jrt.TableView.ReloadRows ( new NSIndexPath[] { NSIndexPath.FromRowSection (i,1) }, UITableViewRowAnimation.Automatic ); }
						}
							
					// jrt.TableView.CellAt (jrt.LastSelectedRowPath).Selected = true ;
					jrt.TableView.SelectRow ( jrt.LastSelectedRowPath, true, UITableViewScrollPosition.None );
				}
			}						
		}

		[Obsolete]
		public override void ViewDidUnload ()
		{
			// base.ViewDidUnload ();
			
			// Release any retained subviews of the main view.
			// e.g. this.myOutlet = null;
		}

		[Obsolete]
		public override bool ShouldAutorotateToInterfaceOrientation (UIInterfaceOrientation toInterfaceOrientation)
		{
			// Return true for supported orientations
			return true; // (toInterfaceOrientation == UIInterfaceOrientation.LandscapeLeft || toInterfaceOrientation == UIInterfaceOrientation.LandscapeRight);
		}

		public void UpdateCustomerInfo(CustomerDetailsUpdatableField field, string OldValue, string NewValue, long CustomerNumber, long JobID)
		{
			if (OldValue == NewValue) return;
			if (File.Exists (ServerClientViewController.dbFilePath))
			{
				// INSERT a record about the change to the CUSTUPDATE table (look for an existing record first, if there is one, update it)
				using (var connection = new SqliteConnection("Data Source="+ServerClientViewController.dbFilePath) )
				{
					connection.Open();
					var cmd = connection.CreateCommand();
					cmd.CommandText = "SELECT * FROM CustUpdate WHERE Cust_OID = ? AND Field = \""+ field.ToString() +"\" ";
					cmd.Parameters.Add ("@CustomerID", DbType.Int32).Value = CustomerNumber;
					var reader = cmd.ExecuteReader();
					if ( reader.HasRows )
					{
						reader.Close ();
						cmd.Parameters.Clear ();
						// UPDATE existing record in CUSTUPDATE
						cmd.CommandText = "UPDATE CustUpdate SET NewValue = ? WHERE Cust_OID = ? AND Field = \"" + field.ToString() + "\" ";
						cmd.Parameters.Add ("@NewValue", DbType.String).Value = NewValue;
						cmd.Parameters.Add ("@CustomerID", DbType.Int32).Value = CustomerNumber;
						cmd.ExecuteNonQuery ();
						_tabs._scView.Log (String.Format ("UPDATE statement: {0}", cmd.CommandText));
						foreach(SqliteParameter param in cmd.Parameters)
						{
							if (_tabs._scView != null && param.Value != null)
							_tabs._scView.Log (String.Format ("Param: {0} = {1}", param.ParameterName, param.Value.ToString ()));
						}
					}
					else
					{
						reader.Close ();
						cmd.Parameters.Clear ();
						// INSERT new record into CUSTUPDATE
						cmd.CommandText = "INSERT INTO CustUpdate (Empl_OID, Job_OID, FIELD, OldValue, NewValue, Cust_OID) VALUES (?, ?, \"" + field.ToString() + "\", ?, ?, ?)";
						cmd.Parameters.Add ("@EmployeeID", DbType.Int32).Value = MyConstants.EmployeeID;
						cmd.Parameters.Add ("@JobID", DbType.Int32).Value = JobID;
						cmd.Parameters.Add ("@OldValue", DbType.String).Value = OldValue;
						cmd.Parameters.Add ("@NewValue", DbType.String).Value = NewValue;
						cmd.Parameters.Add ("@CustomerID", DbType.Int32).Value = CustomerNumber;
						cmd.ExecuteNonQuery ();
						_tabs._scView.Log (String.Format ("UPDATE statement: {0}", cmd.CommandText));
						foreach(SqliteParameter param in cmd.Parameters)
						{
							if (_tabs._scView != null)
							{
								if (param.Value != null)
								{
									_tabs._scView.Log (String.Format ("Param: {0} = {1}", param.ParameterName, param.Value.ToString ()));
								}
								else
								{
									_tabs._scView.Log (String.Format ("Param: {0} = {1}", param.ParameterName, DBNull.Value.ToString ()));
								}
							}
						}
					}
			
					// UPDATE the field(s) in WCLIENT table
					// depends a lot on the field being updated
					cmd.Parameters.Clear ();
					switch (field)
					{
						case CustomerDetailsUpdatableField.CompanyName : {
							cmd.CommandText = "UPDATE Wclient SET wComName = ? WHERE Cusnum = ?";
							cmd.Parameters.Add ("@CompanyName", DbType.String).Value = NewValue;

							foreach (Customer c in this._tabs._jobRunTable.Customers) {
								if (c.CustomerNumber == CustomerNumber) {
									c.isCompany = ! String.IsNullOrEmpty (NewValue);
								}
							}
							foreach (Customer c in this._tabs._jobRunTable.UserAddedCustomers) {
								if (c.CustomerNumber == CustomerNumber) {
									c.isCompany = ! String.IsNullOrEmpty (NewValue);
								}
							}
							
							break;
						}
						case CustomerDetailsUpdatableField.FirstName : {
							cmd.CommandText = "UPDATE Wclient SET Wconame = ? WHERE Cusnum = ?";
							cmd.Parameters.Add ("@FirstName", DbType.String).Value = NewValue;				
							break;
						}
						case CustomerDetailsUpdatableField.LastName : {
							cmd.CommandText = "UPDATE Wclient SET Wcsname = ? WHERE Cusnum = ?";
							cmd.Parameters.Add ("@LastName", DbType.String).Value = NewValue;				
							break;
						}
						case CustomerDetailsUpdatableField.Address : {
							cmd.CommandText = "UPDATE Wclient SET Wcadd1 = ? WHERE Cusnum = ?";
							cmd.Parameters.Add ("@Address", DbType.String).Value = NewValue;				
							break;
						}
						case CustomerDetailsUpdatableField.Suburb : {
							cmd.CommandText = "UPDATE Wclient SET Wcadd2 = ? WHERE Cusnum = ?";
							cmd.Parameters.Add ("@Suburb", DbType.String).Value = NewValue;				
							break;
						}
						case CustomerDetailsUpdatableField.Phone : {
							string newval = "";
							for (int i = 0; i < NewValue.Length; i++)
							{
								char c = NewValue[i];
								if ( char.IsDigit (c) ) newval += c;
							}
							cmd.CommandText = "UPDATE Wclient SET Wcacde = ?, Wcphone = ? WHERE Cusnum = ?";
							if (newval.Length > 2)
							{
								cmd.Parameters.Add ("@AreaCode", DbType.String).Value = newval.Substring (0,2);
								cmd.Parameters.Add ("@PhoneNumber", DbType.String).Value = newval.Substring (2);		
							}
							else
							{
								cmd.Parameters.Add ("@AreaCode", DbType.String).Value = "";
								cmd.Parameters.Add ("@PhoneNumber", DbType.String).Value = newval;		
							}
							break;
						}						
						case CustomerDetailsUpdatableField.MobilePhone : {
							string newval = "";
							for (int i = 0; i < NewValue.Length; i++)
							{
								char c = NewValue[i];
								if ( char.IsDigit (c) ) newval += c;
							}
							cmd.CommandText = "UPDATE Wclient SET Mobpre = ?, Mobile = ? WHERE Cusnum = ?";
							
							if (newval.Length > 2)
							{
								cmd.Parameters.Add ("@MobilePrefix", DbType.String).Value = newval.Substring (0,2);
								cmd.Parameters.Add ("@MobileNumber", DbType.String).Value = newval.Substring (2);	
							}
							else 
							{
								cmd.Parameters.Add ("@MobilePrefix", DbType.String).Value = "";
								cmd.Parameters.Add ("@MobileNumber", DbType.String).Value = newval;
							}
							break;
						}						
						case CustomerDetailsUpdatableField.FallbackContact : {
							cmd.CommandText = "UPDATE Wclient SET Wcsoname = ?, Wcssname = ? WHERE Cusnum = ?";
							int i = NewValue.LastIndexOf (' ');
							if (i != -1)
							{
								cmd.Parameters.Add ("@FirstName", DbType.String).Value = NewValue.Substring (0,i);				
								cmd.Parameters.Add ("@LastName", DbType.String).Value = NewValue.Substring (i);
							}
							else
							{
								cmd.Parameters.Add ("@FirstName", DbType.String).Value = NewValue;				
								cmd.Parameters.Add ("@LastName", DbType.String).Value = "";
							}
							break;
						}
						case CustomerDetailsUpdatableField.FallbackPhone : {
							string newval = "";
							for (int i = 0; i < NewValue.Length; i++)
							{
								char c = NewValue[i];
								if ( char.IsDigit (c) ) newval += c;
							}
							cmd.CommandText = "UPDATE Wclient SET Wccoacde = ?, Wccophone = ? WHERE Cusnum = ?";
							if (newval.Length > 2)
							{
								cmd.Parameters.Add ("@AreaCode", DbType.String).Value = newval.Substring (0,2);
								cmd.Parameters.Add ("@PhoneNumber", DbType.String).Value = newval.Substring (2);
							}
							else 
							{
								cmd.Parameters.Add ("@AreaCode", DbType.String).Value = "";
								cmd.Parameters.Add ("@PhoneNumber", DbType.String).Value = newval;
							}
							break;
						}
						case CustomerDetailsUpdatableField.SpecialComments : {
							cmd.CommandText = "UPDATE WSALES SET SPECIALINSTRUCT=? WHERE UnitNum=? AND CusNum=?";
							cmd.Parameters.Add ("@InstructionsText", DbType.String).Value = NewValue;
							cmd.Parameters.Add ("@UnitNumber", DbType.Int64).Value = ( this._tabs._jobRunTable.CurrentJob.HasParent ()) ? 
								this._tabs._jobRunTable.FindParentJob(this._tabs._jobRunTable.CurrentJob).UnitNumber :
								this._tabs._jobRunTable.CurrentJob.UnitNumber;
							break;
						}
						case CustomerDetailsUpdatableField.AttentionReason : {
							cmd.CommandText = "UPDATE PL_RECOR SET ATTENTION_REASON=? WHERE BookNum=? AND CusNum=?";
							cmd.Parameters.Add ("@AttentionText", DbType.String).Value = NewValue;
							cmd.Parameters.Add ("@Job_ID", DbType.Int64).Value = ( this._tabs._jobRunTable.CurrentJob.HasParent ()) ? 
								this._tabs._jobRunTable.FindParentJob(this._tabs._jobRunTable.CurrentJob).JobBookingNumber :
									this._tabs._jobRunTable.CurrentJob.JobBookingNumber;
							break;
						}
						case CustomerDetailsUpdatableField.TubingUpgradeDone : {
							cmd.CommandText = "UPDATE Wclient SET tu_Done = ? WHERE Cusnum = ?";
							cmd.Parameters.Add ("@tubingDone", DbType.String).Value = NewValue;
							break;
						}
						case CustomerDetailsUpdatableField.Email : {
							cmd.CommandText = "UPDATE Wclient SET EmailAd = ? WHERE Cusnum = ?";
							cmd.Parameters.Add ("@EmailAddress", DbType.String).Value = NewValue;
							break;
						}
					}
					cmd.Parameters.Add ("@CustomerID", DbType.Int32).Value = CustomerNumber;
					if (field != CustomerDetailsUpdatableField.JobPriceTotal)
					{
						cmd.ExecuteNonQuery ();
						_tabs._scView.Log (String.Format ("UPDATE statement: {0}", cmd.CommandText));
						foreach(SqliteParameter param in cmd.Parameters)
							_tabs._scView.Log (String.Format ("Param: {0} = {1}", param.ParameterName, param.Value.ToString ()));					
					}
				}
			}
			else 
			{
				_tabs._scView.Log ("UpdateCustomerInfo: ERROR: Database file not found: "+ServerClientViewController.dbFilePath + "\n WHAT?");
			}			
		}

		public void SetJobTimeEnabled()
		{
			cJobTime.Enabled = true;
			cJobTime.UserInteractionEnabled = true;
		}

		public void SetJobTimeDisabled()
		{
			cJobTime.Enabled = false;
			cJobTime.UserInteractionEnabled = false;
		}
	
		void acPrinterTest (NSObject sender)
		{	
			CGPDFDocument pdfDoc = CGPDFDocument.FromFile (Path.Combine (Environment.GetFolderPath(Environment.SpecialFolder.Personal), "100605 PrePlumbingPDF_Signed.pdf"));
			UIImage img = MyConstants.ImageFromPDF(pdfDoc, 1);			

			TcpPrinterConnection myConn;
			myConn = new TcpPrinterConnection("10.11.1.3", 6101, 10, 10);
			
			NSError err;

			bool connectionOK = myConn.Open ();
			
			if (connectionOK)
			{
				try {				
					// string test = SGD.Get ("appl.name", myConn, out err); // -- SGD class from Zebra API works

					ZebraPrinterCpcl zprn = ZebraPrinterFactory.GetInstance(myConn, PrinterLanguage.PRINTER_LANGUAGE_CPCL);
					GraphicsUtilCpcl gu = zprn.GetGraphicsUtil();
					
					string testSETFF ="! U1 JOURNAL\r\n! U1 SETFF 50 5\r\n";
					NSData testData = NSData.FromArray (System.Text.UTF8Encoding.UTF8.GetBytes (testSETFF));
					myConn.Write (testData, out err);
					// gu.printImage(img.CGImage, 280, 5, -1, -1, false, out err);
					
					gu.printImage(img.CGImage, 280, 5, -1, -1, false, out err);
					if (err != null)
					{
						// Console.WriteLine (err.LocalizedDescription);
					}
				}	
				finally {
					myConn.Close ();
				}
			}
		}
	}
}

