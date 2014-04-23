using MonoTouch.UIKit;
using System.Drawing;
using System;
using System.IO;
using System.Collections.Generic;
using MonoTouch.Foundation;
using Mono.Data.Sqlite;

namespace Puratap
{
	public partial class Memos : UIViewController
	{
		DetailedTabs _tabs;
		private MemoTableSource _memoSource;
		private List<Memo> _memos;
		private string _memosLabel;
		public string MemosLabel
		{
			get { return _memosLabel; }
			set { _memosLabel = value;
					oMemoLabel.Text = _memosLabel;
			}
		}
		
		public List<Memo> CustomerMemos
		{
			get { return _memos; }
			set { _memos = value; }
		}
		
		public UITableView MemosTable 
		{
			get { return oMemosTable; }
			set { oMemosTable = value; }
		}
		
		public void ReloadMemosTable()
		{
			this.oMemosTable.ReloadData();
			this.oMemoText.Text="";
		}
		
		public Memos (DetailedTabs tabs) : base ("Memos", null)
		{
			this.Title = NSBundle.MainBundle.LocalizedString ("Memos", "Memos");
			using (var image = UIImage.FromBundle ("Images/179-notepad") ) this.TabBarItem.Image = image;
			this._memos = new List<Memo> ();
			this._memoSource = new MemoTableSource (this);
			this._tabs = tabs;
		}
		
		public class MemoTableSource : UITableViewDataSource
		{
			Memos _memosView;
			const string ReusableMemosTableCellID = "memoCell";
			public MemoTableSource (Memos m)
			{
				this._memosView = m;
				_memosView._memos = new List<Memo>();
			}
			public override UITableViewCell GetCell (UITableView tableView, NSIndexPath indexPath)
			{
				var cell = tableView.DequeueReusableCell(ReusableMemosTableCellID) ?? new UITableViewCell(UITableViewCellStyle.Subtitle, ReusableMemosTableCellID);
				
				Memo m = new Memo("");
				m = _memosView._memos[indexPath.Row];
				cell.TextLabel.Text = String.Format ("{0} {1} ", 
				                                     m.MemoTimeEntered.ToString ("dd/MM/yyyy HH:mm:ss"),
				                                     m.MemoDescription);
				
				cell.DetailTextLabel.Text = _memosView._memos[indexPath.Row].MemoContents;
				return cell;
			}
			
			public override int RowsInSection (UITableView tableView, int section)
			{	return (_memosView.CustomerMemos != null) ? _memosView.CustomerMemos.Count : 0;	}
			public override bool CanEditRow (UITableView tableView, NSIndexPath indexPath)
			{	return true;	}
			public override bool CanMoveRow (UITableView tableView, NSIndexPath indexPath)
			{	return false;	}
		}
		
		public class MemoTableDelegate : UITableViewDelegate
		{
			Memos _memoView;
			
			public MemoTableDelegate(Memos m)
			{
				_memoView = m;
			}
			
			public override void RowSelected (UITableView tableView, NSIndexPath indexPath)
			{ // to support memo editing functionality, we should enable editing when the user selects a memo that he has created today
				_memoView.oMemoText.Text = _memoView._memos[indexPath.Row].MemoContents;
				if (_memoView._memos[indexPath.Row].MemoType == "FRC" && 
				    _memoView._memos[indexPath.Row].MemoDateEntered.Date == DateTime.Today &&
				    _memoView._memos[indexPath.Row].Editable == true)
				{
					_memoView.oMemoText.Editable = true;
					_memoView.oMemoText.ShouldBeginEditing = delegate {
						UIView.SetAnimationDuration(0.3);
						UIView.BeginAnimations(null);
						_memoView.oMemoText.Frame = new RectangleF(10,70,683,300);
						_memoView.oMemosTable.Hidden = true;
						UIView.CommitAnimations();
						return true;		// this means that oMemoText object is permitted to begin its editing

					};
					
					_memoView.oMemoText.ShouldEndEditing = delegate {
						_memoView.oMemoText.Editable = false;
						UIView.SetAnimationDuration(0.3);
						UIView.BeginAnimations(null);
						_memoView.oMemoText.Frame = new RectangleF(20,523,663,166);
						_memoView._memos[indexPath.Row].MemoContents = _memoView.oMemoText.Text;

						UIView.CommitAnimations();
						
						// updates WCMemo database table here (as the editing has been done)
						string dbPath = ServerClientViewController.dbFilePath;
						if (File.Exists( dbPath ))
						{
							string sql = 	"UPDATE Wcmemo SET wmore = :_wmore WHERE wctime = :_timeentered AND wmemnum="+MyConstants.DUMMY_MEMO_NUMBER.ToString();		// Updating created memo in WCMEMO table
							
							// create SQLite connection to file and write the data
							SqliteConnection connection = new SqliteConnection("Data Source="+dbPath);
							using (SqliteCommand cmd = connection.CreateCommand())
							{
								connection.Open();
								
								cmd.CommandText = sql;

								cmd.Parameters.Add("_wmore", System.Data.DbType.String).Value = _memoView.oMemoText.Text;		// memo contents
								cmd.Parameters.Add("_timeentered", System.Data.DbType.String).Value = _memoView._memos[indexPath.Row].MemoTimeEntered.ToString ("HH:mm:ss");
		
								// TODO:: error handling here :: IMPORTANT since if UPDATE statement won't execute for some, there will be discrepancy between database and displayed memo list
								cmd.ExecuteNonQuery();
								
								// if the update statement did execute correctly, we'll have to update the memo in a list of customer's memos as well
								_memoView._tabs._jobRunTable.CurrentCustomer.CustomerMemos[indexPath.Row].MemoContents = _memoView.oMemoText.Text;
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
						_memoView.ReloadMemosTable();
						_memoView.oMemosTable.Hidden = false;
						return true;		// this means that oMemoText object is permitted to end its editing
					};
				}
				else 
				{
					_memoView.oMemoText.ShouldBeginEditing = null;
					_memoView.oMemoText.ShouldEndEditing = null;
					_memoView.oMemoText.Editable = false;
				}
			}
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
			oMemosTable.DataSource = this._memoSource;
			this.oMemosTable.Delegate = new MemoTableDelegate(this);
			this.oMemosTable.AutoresizingMask = UIViewAutoresizing.FlexibleWidth;
			this.oMemoText.AutoresizingMask = UIViewAutoresizing.FlexibleWidth;
			
			/* This Does Work
			UINavigationBar dismiss = new UINavigationBar(new RectangleF(0,0,320,27));
					dismiss.PushNavigationItem(new UINavigationItem("See?"), false);	
			        UIBarButtonItem dismissBtn = new UIBarButtonItem("Done", UIBarButtonItemStyle.Done, null);
					dismiss.TopItem.SetLeftBarButtonItem(dismissBtn, false);
			        
					// dismissBtn.TouchDown += delegate { oMemoText.ResignFirstResponder();  };
				
			oMemoText.InputAccessoryView = dismiss;*/	
			
			// oMemoText.InputAccessoryView = t;
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
			return (toInterfaceOrientation == UIInterfaceOrientation.LandscapeLeft || toInterfaceOrientation == UIInterfaceOrientation.LandscapeRight);
		}
	}
}

