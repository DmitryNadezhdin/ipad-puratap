using System;
using MonoTouch.Dialog;
using MonoTouch.Foundation;
using MonoTouch.UIKit;

namespace Puratap
{
	public class JobStringElement : StringElement
	{
		public JobStringElement(string caption, string val) : base(caption, val)
		{	}
		
		public override void Selected (DialogViewController dvc, UITableView tableView, NSIndexPath path)
		{
			base.Selected (dvc, tableView, path);
			
			// set the view of all cells to default
			foreach(UITableViewCell c in tableView.VisibleCells)
			{
				c.BackgroundColor = UIColor.White;
				c.TextLabel.TextColor = UIColor.Black;
				c.DetailTextLabel.TextColor = UIColor.DarkGray;				
			}
			
			var cell = tableView.CellAt (path);
			cell.BackgroundColor = UIColor.Blue;
			cell.TextLabel.TextColor = UIColor.White;
			cell.DetailTextLabel.TextColor = UIColor.White;
			
			int row = path.Row;
			if (row == 0) // main job
			{				
				(dvc as JobSummary).Nav._tabs._jobRunTable.CurrentJob = (dvc as JobSummary).mainJob; 
			}
			else // child job
			{
				(dvc as JobSummary).Nav._tabs._jobRunTable.CurrentJob = (dvc as JobSummary).mainJob.ChildJobs[row-1];
			}
		}
	}
	
	public class JobSummary : DialogViewController
	{
		public WorkflowNavigationController Nav { get; set; }
		public Job mainJob;
		
		DetailedTabs _tabs;
		
		// This is our subclass of the fixed-size Source that allows editing
		public class EditingSource : DialogViewController.Source 
		{
			public EditingSource (JobSummary dvc) : base (dvc) { }
			
			public override bool CanEditRow (UITableView tableView, NSIndexPath indexPath)
			{
				// Trivial implementation: we let all rows be editable, regardless of section or row
				return true;
			}
			
			public override UITableViewCellEditingStyle EditingStyleForRow (UITableView tableView, NSIndexPath indexPath)
			{
				// Trivial implementation: show a delete button always except for the main job which cannot be deleted
				return (indexPath.Row == 0)? UITableViewCellEditingStyle.None : UITableViewCellEditingStyle.Delete;
			}
			
			public override void CommitEditingStyle (UITableView tableView, UITableViewCellEditingStyle editingStyle, NSIndexPath indexPath)
			{
				//
				// In this method, we need to actually carry out the request
				//
				var section = Container.Root [indexPath.Section];
				var element = section [indexPath.Row];
				section.Remove (element);
				section.Footer = "";
				
				// If deleted job was a service operation, reset the corresponding view controllers to defaults
				if ( (Container as JobSummary).mainJob.ChildJobs[indexPath.Row-1].Type.Code == "SER" )
				{
					(Container as JobSummary).Nav._tabs._jobService.ResetToDefaults ();
				}
				
				// REMOVE the job from list of child jobs and erase its results from database
				(Container as JobSummary).Nav.EraseChildJobFromDatabase ( (Container as JobSummary).mainJob.ChildJobs[indexPath.Row-1] );
				(Container as JobSummary).mainJob.ChildJobs.RemoveAt (indexPath.Row-1);
				(Container as JobSummary).ReloadData ();
				(Container as JobSummary).Nav._tabs._jobRunTable.CurrentJob = (Container as JobSummary).mainJob;
			}
		}
		
		public override Source CreateSizingSource (bool unevenRows)
		{
			if (unevenRows)
				throw new NotImplementedException ("You need to create a new SourceSizing subclass, this sample does not have it");
			return new EditingSource (this);
		}		
		
		public JobSummary (WorkflowNavigationController nav, DetailedTabs tabs, RootElement root, bool pushing) : base (root, pushing)
		{
			Nav = nav;
			_tabs = tabs;
		}
		
		public override void ViewDidAppear (bool animated)
		{
 			// Nav.SetToolbarButtons (WorkflowToolbarButtonsMode.JobSummary);
			mainJob = new Job(false);
			int i = 0;
			if (Nav._tabs._jobRunTable.CurrentJob.HasNoParent ())
			{
				mainJob = Nav._tabs._jobRunTable.CurrentJob;
			}
			else {
				bool found = false;
				foreach(Job j in Nav._tabs._jobRunTable.MainJobList)
				{
					if (j.JobBookingNumber == Nav._tabs._jobRunTable.CurrentJob.ParentJobBookingNumber)
					{ 
						mainJob = j;
						found = true;
						break; 
					}
				}
				if (! found)
				{
					foreach (Job j in Nav._tabs._jobRunTable.UserCreatedJobs)
					{
						if (j.JobBookingNumber == Nav._tabs._jobRunTable.CurrentJob.ParentJobBookingNumber)
						{
							mainJob = j;
							found = true;
							break;
						}
					}
				}
			}
			
			Title = "Job Summary";
			Root = new RootElement("Job Summary");
			Section jobSection = new Section("Jobs");
			i = 1;
			if (mainJob.Warranty == false)
			{
				jobSection.Add (new JobStringElement(
					String.Format ("{0} Main job : {1}", i, mainJob.Type.Description),
					String.Format ("${0:0.00}", mainJob.MoneyToCollect) ));
			}
			else {
				jobSection.Add (new JobStringElement(
					String.Format ("{0} Main job : {1} (under warranty)", i, mainJob.Type.Description),
					String.Format ("${0:0.00}", mainJob.MoneyToCollect) ));				
			}
			
			if (mainJob.ChildJobs != null)
			{
			foreach(Job childJob in mainJob.ChildJobs)
				{
					i++;
					if (childJob.Warranty == false)
					{
						jobSection.Add (new JobStringElement(
							String.Format ("{0} Added job : {1}", i, childJob.Type.Description),
							String.Format ("${0:0.00}", childJob.MoneyToCollect) ));
					}
					else {
						jobSection.Add (new JobStringElement(
							String.Format ("{0} Added job : {1} (under warranty)", i, childJob.Type.Description),
							String.Format ("${0:0.00}", childJob.MoneyToCollect) ));						
					}
				}
			}
			
			Root.Add (jobSection);
			
			EditPrices (this);
			SelectCurrentJobRow();
		}
		
		public override void ViewDidDisappear (bool animated)
		{
			base.ViewDidDisappear (animated);
			Root.Clear ();
			ReloadData ();
			_tabs.MyNavigationBar.Hidden = true;
		}
		
		void MoveViewUp()
		{
			UIView.BeginAnimations (null);
			UIView.SetAnimationDuration (0.3);
			this.View.Frame = new System.Drawing.RectangleF(20,50,663,313);
			UIView.CommitAnimations ();
		}
		void MoveViewDown()
		{
			UIView.BeginAnimations (null);
			UIView.SetAnimationDuration (0.3);			
			this.View.Frame = new System.Drawing.RectangleF(20,411,663,233);
			UIView.CommitAnimations ();
		}
		
		public void EditPrices (DialogViewController dvc)
		{
			if (_tabs.MyNavigationBar.Hidden)
				_tabs.MyNavigationBar.Hidden = false;
			
			_tabs.MyNavigationBar.TopItem.SetRightBarButtonItems(new UIBarButtonItem[] { new UIBarButtonItem ("Adjust prices", UIBarButtonItemStyle.Bordered, delegate {
				// Bring the table view up, so that the keyboard does not obscure it
				MoveViewUp();
				// Activate editing
				// Switch the root to editable elements		
				dvc.Root = CreateEditableRoot(dvc.Root, true);
				dvc.ReloadData();
				// Activate row editing & deleting
				dvc.TableView.SetEditing (true, true);
				
				// disable the toolbar
				Nav._tabs._payment.DisableToolbar();
					
				EditPricesDone(dvc);
			}) }, true);
			_tabs.MyNavigationBar.TopItem.SetLeftBarButtonItems (new UIBarButtonItem[] { }, true);
		}

		public void EditPricesDone (DialogViewController dvc)
		{
			if (_tabs.MyNavigationBar.Hidden)
				_tabs.MyNavigationBar.Hidden = false;
				_tabs.MyNavigationBar.TopItem.RightBarButtonItem = new UIBarButtonItem (UIBarButtonSystemItem.Done, delegate {			
				MoveViewDown();
				// Deactivate editing
				dvc.ReloadData();
				// Switch updated entry elements to StringElements
				dvc.Root = CreateEditableRoot(dvc.Root, false);
				dvc.TableView.SetEditing (false, true);
				
				// check the input here
				bool ok = true;
				double total = 0;
				for (int j = 0; j < dvc.Root[0].Count; j++) {
					double result;
					StringElement element = (dvc.Root[0].Elements[j] as StringElement);
					string enteredPrice = element.Value;
					ok = double.TryParse (enteredPrice, out result);
					ok = (ok && result > 0);
					if ( ! ok )
					{
						element.Value = (j>0)? String.Format ("${0:0.00}", mainJob.ChildJobs[j-1].MoneyToCollect) : String.Format ("${0:0.00}", mainJob.MoneyToCollect);	// old value, if the input was invalid	
						total += (j>0)? mainJob.ChildJobs[j-1].MoneyToCollect : mainJob.MoneyToCollect;
						TableView.ReloadData ();
					}
					else 
					{
						element.Value = String.Format ("${0:0.00}", result);	// new entered value, if it's correct
						
						// normal or split payment -- does not matter -- we can still assume that every job has at least one payment in their payments list
						if (j==0) { 
							mainJob.MoneyToCollect = result; 
							mainJob.Payments[0].Amount = mainJob.MoneyToCollect;
							total += mainJob.MoneyToCollect;
						}
						else { 
							mainJob.ChildJobs[j-1].MoneyToCollect = result; 
							mainJob.ChildJobs[j-1].Payments[0].Amount = mainJob.ChildJobs[j-1].MoneyToCollect; 
							total += mainJob.ChildJobs[j-1].MoneyToCollect;
						}
					} 					
				}
				
				if (!ok) {
					var alert = new UIAlertView("Incorrect input value for price ignored", "Please try again", null, "OK");
					alert.Show ();
				}

				_tabs._payment.SetTotalToCollect (total);

				if (this._tabs._payment.ContainsInvoicePaymentType (mainJob.Payments))
					_tabs._payment.SetTotalReceived (0);
				// else _tabs._payment.SetTotalReceived (total); // --- no need for this, this has been done in SetTotalToCollect() already

				if (Root[0].Footer != "") Root[0].Footer = "";
				
				EditPrices (dvc);
				// enable the toolbar
				Nav._tabs._payment.EnableToolbar();
				SelectCurrentJobRow();
			});
		}
		
		public void SelectCurrentJobRow()
		{
			Job currentJob = _tabs._jobRunTable.CurrentJob;
			if (mainJob.JobBookingNumber == currentJob.JobBookingNumber)
			{
				Root[0].Elements[0].Selected (this, TableView, NSIndexPath.FromRowSection(0,0));
			}
			else 
			{
				for (int i = 0; i < mainJob.ChildJobs.Count; i++)
				{
					if (mainJob.ChildJobs[i].JobBookingNumber == currentJob.JobBookingNumber)
					{
						Root[0].Elements[i+1].Selected (this, TableView, NSIndexPath.FromRowSection(i+1,0));
						break;
					}
				}
			}
		}
		
		RootElement CreateEditableRoot (RootElement root, bool editable)
		{
		    var rootElement = new RootElement("Job summary") {
				new Section("Jobs")
			};
			
			foreach (var element in root[0].Elements) {
				if(element is StringElement) {
					rootElement[0].Add(CreateEditableElement (element.Caption, (element as JobStringElement).Value, editable));
				} else {
					rootElement[0].Add(CreateEditableElement (element.Caption, (element as EntryElement).Value, editable));
				}
			}
			rootElement[0].Footer = root[0].Footer;
		    return rootElement;
		}
		
		Element CreateEditableElement (string caption, string content, bool editable)
		{
			if (editable) {
				return new EntryElement(caption, "Price", content);
			} else {
				return new JobStringElement(caption, content);
			}
		}
		
		public void ClearChildJobs()
		{
			if (mainJob.HasNoChildJobs ()) return;
			else {
				Section section = this.Root[0];
				for (int i = section.Elements.Count-1; i > 0; i--)
				{
					section.Remove (i);
					Nav.EraseChildJobFromDatabase (mainJob.ChildJobs[i-1]);
					mainJob.ChildJobs.RemoveAt (i-1);
				}
				section.Footer = "";
				ReloadData ();
				this.Nav._tabs._jobRunTable.CurrentJob = mainJob; // after clearing out all the child jobs we should set our current job to main for that customer
			}

			SelectCurrentJobRow ();
			Nav._tabs._payment.SetTotalToCollect (Nav._tabs._payment.CalculateMoneyToCollect ());

			// WAS :: if (mainJob.Payments.Type.Contains ( PaymentTypes.CCDetails) || mainJob.Payments.Type.Contains ( PaymentTypes.CreditCard) || mainJob.Payments.Type.Contains ( PaymentTypes.Invoice))
			if (Nav._tabs._payment.ContainsInvoicePaymentType (mainJob.Payments))
				_tabs._payment.SetTotalReceived (0);
			else _tabs._payment.SetTotalReceived ( _tabs._payment.CalculateMoneyToCollect ());

			if (mainJob.Type.Code != "SER")
				Nav._tabs._jobService.ResetToDefaults (); 
			else {
				// TODO :: what if main job was a service?
			}
		}
	}
}

