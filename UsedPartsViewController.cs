using MonoTouch.UIKit;
using System.Drawing;
using System;
using System.IO;
using System.Threading;
using System.Collections.Generic;
using MonoTouch.Foundation;
using MonoTouch.Dialog;
using MonoTouch.CoreGraphics;
using Mono.Data.Sqlite;

namespace Puratap
{
	public enum PartButtons { Left, Mid, Right }
	
	/*
	public interface IImageUpdated {
		void UpdatedImage (Uri uri);
	}
	*/
	
	public partial class UsedPartsViewController : DialogViewController
	{
		// linked this with a Job object, now we can access Job's UsedParts property and work with it
		// then should we want to save a job, we save its JobResult and UsedParts properties to the database
		
		private Job _thisJob;
		public Job ThisJob { get { return _thisJob; } set { _thisJob = value; } }
		
		//private List<Part> _parts;
		//public List<Part> Parts { get { return _parts; } set { _parts = value; if (ThisJob != null) ThisJob.UsedParts = _parts; } }
		
		private List<Part> _dbParts;
		public List<Part> DBParts { get { return _dbParts; } set { _dbParts = value; } }
		
		private WorkflowNavigationController _navWorkflow;
		public WorkflowNavigationController NavWorkflow { get { return _navWorkflow; } set { _navWorkflow = value; } }
		
		public UsedPartsNavigationController NavUsedParts { get; set; }
		// public bool JobReportAttached { get; set; }

		public override void ViewDidAppear (bool animated)
		{
			this.NavigationItem.SetHidesBackButton (true, false);
			var warrantySection = Root[0];
			var warrantyElement = warrantySection[0];
			if (warrantyElement is BooleanElement)
				(warrantyElement as BooleanElement).Value = ThisJob.Warranty;
			base.ViewDidAppear (animated);

			// ThisJob = NavUsedParts.Tabs._jobRunTable.CurrentJob;
		}
		
		public override void ViewDidDisappear (bool animated)
		{
			base.ViewDidDisappear (animated);
			
			// job done on a warranty are not paid for, therefore money is set to 0 
			if (ThisJob.Warranty) ThisJob.MoneyToCollect = 0;
			else { // jobs that are not done under warranty with a price of 0.00 are set back to their retail price

				// this leads to situations when the job was booked with money to collect = 0 and is set back to its retail price
				if (ThisJob.MoneyToCollect < 0.01) ThisJob.MoneyToCollect = ThisJob.Type.RetailPrice;
			}
			
			// ThisJob.UsedParts = Parts;
			// _parts.Clear ();
		}

		public void AddJobReport(bool isRemovable)
		{
			// job report section is the first one (index 0)
			JobReportSection sec = new JobReportSection(this, "Job Report", isRemovable);
			Root.RemoveAt (0);
			ReloadData ();
			Root.Insert (0, sec);
			// Thread.Sleep (100);
			ReloadData ();

			if (ThisJob != null) 
				ThisJob.JobReportAttached = true;
		}
		
		public void RemoveJobReport()
		{
			// job report section is the first one (index 0)
			/*
			var warrantyElement = new BooleanElement("Warranty", false);
			warrantyElement.ValueChanged += delegate {
				if (warrantyElement.Value == true)
					AddJobReport();
				else RemoveJobReport ();
			}; */

			for (int i = Root[0].Elements.Count; i>0; i--)
				Root[0].Remove (i);
			Root[0].Caption = " ";

			ReloadData ();

			if (ThisJob != null) 
				ThisJob.JobReportAttached = false;
		}

		public void SaveJobReport(JobReportData jrd)
		{
			using (var connection = new SqliteConnection("Data Source="+ServerClientViewController.dbFilePath))
			{
				connection.Open();
				using (var cmd = connection.CreateCommand())
				{
					// delete all records from DB with the current jobID
					string sql = 	"DELETE FROM JOB_REPORTS WHERE  JOB_OID = ?";
					cmd.CommandText = sql;
					cmd.Parameters.Add ("@JobID", System.Data.DbType.Int64).Value = ThisJob.JobBookingNumber;
					cmd.ExecuteNonQuery();

					// insert a new record into Job_Reports table
					cmd.CommandText = "INSERT INTO JOB_REPORTS (JOB_OID, REASON_OID, POINT_OID, PRESSURE, COMMENT) VALUES (?, ?, ?, ?, ?) ";
					cmd.Parameters.Clear ();
					cmd.Parameters.Add ("@JobID", System.Data.DbType.Int64).Value = ThisJob.JobBookingNumber;
					cmd.Parameters.Add ("@ReasonID", System.Data.DbType.Int64).Value = jrd.ReasonOID;
					cmd.Parameters.Add ("@PointID", System.Data.DbType.Int64).Value = jrd.PointOID;
					cmd.Parameters.Add ("@Pressure", System.Data.DbType.Int32).Value = jrd.Pressure;
					cmd.Parameters.Add ("@Comment", System.Data.DbType.String).Value = jrd.Comment;
					cmd.ExecuteNonQuery();
				}
				connection.Close ();
			}
		}

		
		public class PartsSource : DialogViewController.SizingSource // .Source
		{
			public PartsSource(UsedPartsViewController fcvc):base(fcvc)
			{ }
			
			public override bool CanEditRow (UITableView tableView, NSIndexPath indexPath)
			{
				if (indexPath.Section > 1) return true;
				else return false;
			}
			
			public override UITableViewCellEditingStyle EditingStyleForRow (UITableView tableView, NSIndexPath indexPath)
			{
				if (indexPath.Section == 2)
				{
					if (indexPath.Row == 0) return UITableViewCellEditingStyle.Insert;
					else return UITableViewCellEditingStyle.Delete;
				}
				else return UITableViewCellEditingStyle.None;
			}
			
			public override void CommitEditingStyle (UITableView tableView, UITableViewCellEditingStyle editingStyle, NSIndexPath indexPath)
			{
				if (editingStyle == UITableViewCellEditingStyle.Insert)
				{
					// add another part by displaying another view controller that allows user to pick one
					(this.Container as UsedPartsViewController).GoChoosePart ();
				}
				if (editingStyle == UITableViewCellEditingStyle.Delete)
				{
					// remove the underlying data item (part)
					var section = this.Container.Root[indexPath.Section];
					PartWithImageElement item = (PartWithImageElement)section[indexPath.Row];
					(this.Container as UsedPartsViewController).ThisJob.UsedParts.Remove (item.Part);
					// remove the element from the table
					section.Remove (item);
				}
			}
		}
		
		public override Source CreateSizingSource (bool unevenRows)
		{
			// IMPLEMENTED ::  if (unevenRows) throw new NotImplementedException("Need... to... implement... new SizingSource subclass");
			return new PartsSource (this);
		}
		
		public UsedPartsViewController(RootElement root, bool pushing) : base (root, pushing)
		{	
			if (ThisJob != null) 
				ThisJob.JobReportAttached = false;
		}
		
		public UsedPartsViewController(RootElement root, WorkflowNavigationController nav, bool pushing) : base (root, pushing)
		{
			// THIS SHOULD NOT GET CALLED AT ALL
			// Console.WriteLine (String.Format ("{0} : WRONG CONSTRUCTOR CALLED !", this.GetType().Name));
			
			/*
			_navWorkflow = nav;
			_parts = new List<Part>();

			DeactivateEditingMode ();
			
			Section FilterChangeTypeSection = new Section("Filter change type");
			FilterChangeTypeSection.Add(new StyledStringElement("Tap to choose filter change type", "Standard", UITableViewCellStyle.Value1) );
			Root.Add(FilterChangeTypeSection);
			
			Section AdditionalPartsSection = new Section("Additional parts used");
			AdditionalPartsSection.Add (new StyledStringElement("Tap here to add a part"));

			Root.Add (AdditionalPartsSection); */
		}
		
		public List<Part> CreateDummyPartsList(int num)
		{
			
			List<Part> result = new List<Part>();
			for (int i = 0; i < 3; i++)
			{
				Part part = new Part() {
					PartNo = i,
					Description = String.Format ("DEBUG: Description: {0}", i),
				};
				result.Add (part);
			}
			
			switch(num)
			{
			case 0: {
				result[0].Image = UIImage.FromBundle ("Images/Parts-HeadUnitSide"); result[0].Description = "Head Unit";
				result[1].Image = UIImage.FromBundle ("Images/Parts-Sump"); result[1].Description = "Sump";
				result[2].Image = UIImage.FromBundle ("Images/Parts-Cap"); result[2].Description = "Cap";
				break;
			}
			case 1: {
				result[0].Image = UIImage.FromBundle ("Images/Parts-Tap1"); result[0].Description = "Tap (1)";
				result[1].Image = UIImage.FromBundle ("Images/Parts-Tap2"); result[1].Description = "Tap (2)";
				result.RemoveAt (2);
				// result[2].Image = UIImage.FromBundle ("Images/Parts-Tap3"); result[2].Description = "Tap (3)";
				break;
			}
			default: break;
			}
			
			return result;
		}

		public void RemovePartsByBuildNumber(long number)
		{
			if (DBParts.Count == 0)
			{
				foreach(Section sec in Root)
				if (sec is PartsSection) {
					(sec as PartsSection).ReadPartsFromDatabase(ServerClientViewController.dbFilePath);
					break; 
				}
			}	

			using (var connection = new SqliteConnection("Data Source="+ServerClientViewController.dbFilePath))
			{
				using (var cmd = connection.CreateCommand())
				{
					connection.Open();
					string sql = 	"SELECT Parts.Partno, Parts.Prtprice, Parts.Prtdesc, Componen.Comp_p_uni FROM Parts, Componen, Standard" +
						" WHERE Parts.Deletedprt = 0 " +
							" AND Standard.Stdnumber = ? " +
							" AND Componen.Compno = Standard.Compno " +
							" AND Parts.Partno = Componen.Partno" ;
					cmd.CommandText = sql;
					cmd.Parameters.Add ("@BuildID", System.Data.DbType.Int32).Value = number;
					using (var reader = cmd.ExecuteReader())
					{
						if (reader.HasRows)
						{
							while (reader.Read () )
							{
								int partno = Convert.ToInt32 ( reader["partno"] );
								double prtQuantity = (double) reader["comp_p_uni"];
								foreach(Part part in DBParts)
								{
									if (part.PartNo == partno)
									{
										part.Quantity = prtQuantity;
										PartRemoved (part.PartNo, part.Quantity);
										break;
									}
								}
							}
						}
						else // reader is empty 
						{
							_navWorkflow._tabs._scView.Log (String.Format("SetPartsToBuildNumber: Build is not found in the database or is empty: Build number {0}", number));
						}
					}
				}
			}
		}
		
		public void SetPartsToBuildNumber(long number)
		{	
			// this should fill the chosen parts list with the parts from the build with passed number
			if (DBParts.Count == 0)
			{
				foreach(Section sec in Root)
					if (sec is PartsSection) {
						(sec as PartsSection).ReadPartsFromDatabase(ServerClientViewController.dbFilePath);
					break; 
				}
			}	
			
			using (var connection = new SqliteConnection("Data Source="+ServerClientViewController.dbFilePath))
			{
				using (var cmd = connection.CreateCommand())
				{
					connection.Open();
					string sql = 	"SELECT Parts.Partno, Parts.Prtprice, Parts.Prtdesc, Componen.Comp_p_uni FROM Parts, Componen, Standard" +
						" WHERE Parts.Deletedprt = 0 " +
						" AND Standard.Stdnumber = ? " +
						" AND Componen.Compno = Standard.Compno " +
						" AND Parts.Partno = Componen.Partno" ;
					cmd.CommandText = sql;
					cmd.Parameters.Add ("@BuildID", System.Data.DbType.Int32).Value = number;
					using (var reader = cmd.ExecuteReader())
					{
						if (reader.HasRows)
						{
							while (reader.Read () )
							{
								int partno = Convert.ToInt32 ( reader["partno"] );
								double prtQuantity = (double) reader["comp_p_uni"];
								foreach(Part part in DBParts)
								{
									if (part.PartNo == partno)
									{
										part.Quantity = prtQuantity;
										PartChosen (part, true);
										break;
									}
								}
							}
						}
						else // reader is empty 
						{
							_navWorkflow._tabs._scView.Log (String.Format("SetPartsToBuildNumber: Build is not found in the database or is empty: Build number {0}", number));
						}
					}
				}
			}
		}
		
		public override void Selected (NSIndexPath indexPath)
		{
			// if (indexPath.Section == 2 && indexPath.Row == 0 && this.TableView.CellAt (indexPath).TextLabel.Text.Contains ("Tap here to add a part")) {
			if (this.TableView.CellAt (indexPath).TextLabel.Text != null)
				if ( this.TableView.CellAt (indexPath).TextLabel.Text.Contains ("Tap here to add a part") ) {
					// present another dialog view controller to choose the part to be used
					GoChoosePart ();
				}
			base.Selected (indexPath);
		}
		
		public void GoChoosePart()
		{
			RootElement root = new RootElement("Choose a part");
			
			// IMPLEMENTED :: subclassed MonoTouch.Dialog.Section here : added a function LoadParts( List<Part> listParts)
			// IMPLEMENTED :: LoadParts : it will create the necessary number of ThreePartsElements ( listParts.Count div 3 )
			// IMPLEMENTED :: Data for the elements is read from database (part #, description, price, picture, etc). When no picture is found for a part, it uses a placeholder picture
			
			// IMPLEMENTED :: added code to ThreePartsView to handle situations when its constructor gets a list with 1 or 2 parts
			// TODO :: add code to ThreePartsView to handle situations when one of the Parts is null for some weird reason
			
			PartsSection sec = new PartsSection("Please choose a part", this);
			
			// populate the table view from database here
			sec.LoadParts();		
			root.Add (sec);
			
			DialogViewController dvc = new DialogViewController(root, true) { Autorotate = false /* true */ };
			//_navWorkflow.PushViewController (dvc, true);
			NavUsedParts.PushViewController (dvc, true);
		}

		public void PartRemoved(int partNo, double partQuantity)
		{
			if (ThisJob == null)
				ThisJob = _navWorkflow._tabs._jobRunTable.CurrentJob;

			foreach(Part x in ThisJob.UsedParts)
			{
				if (x.PartNo == partNo)
				{
					if (x.Quantity - partQuantity > 0)
					{
						x.Quantity -= partQuantity;
					}
					else
					{
						x.Quantity = 0;
						ThisJob.UsedParts.Remove (x);
					}

					// search through the sections
					foreach( Section sec in this.Root)
					{
						if (sec[0].Caption == "Tap here to add a part")
						{
							// found the section with parts
							foreach(Element pel in sec)
							{
								if (pel is PartWithImageElement)
								{
									if ( (pel as PartWithImageElement).Part.PartNo == x.PartNo)
									{
										// set the quantity
										if ( (pel as PartWithImageElement).Quantity - partQuantity > 0 )
											(pel as PartWithImageElement).Quantity -= partQuantity;
										else {
											sec.Remove (pel);
											break;
										}
									}
								}
							}
						}
					}
					this.ReloadData ();
					break;
				}
			}
		}

		public void PartRemoved(int partNo)
		{
			PartRemoved (partNo, 1);
		}

		public void PartChosen(int partNo)
		{
			foreach (Part x in this.DBParts)
			{
				if ( x.PartNo == partNo )
				{
					// x.Quantity = 1;
					PartChosen (x, false);
					break;
				}
			}
		}

		public void PartChosen(Part part, bool settingToStandard)
		{
			bool exists = false;
			foreach (Part x in ThisJob.UsedParts)
			{
				if ( x.PartNo == part.PartNo )
				{ 
					exists = true;

					if (! settingToStandard) 
						x.Quantity += 1;

					// search through the sections
					foreach( Section sec in this.Root)
					{
						if (sec.Elements.Count > 0)
							if (sec.Elements[0].Caption == "Tap here to add a part")
							{
								// found the section with parts
								foreach(Element pel in sec)
								{
									if (pel is PartWithImageElement)
									{
										if ( (pel as PartWithImageElement).Part.PartNo == x.PartNo)
										{
											// set the quantity
											(pel as PartWithImageElement).Quantity = x.Quantity;
											// this.ReloadData ();
										}
									}
								}
							}
					}

					break; 
				}
			}
			if (! exists) 
			{
				ThisJob.UsedParts.Add (part);
				ThisJob.UsedParts[ThisJob.UsedParts.Count-1].Quantity = (part.Quantity == 0) ? 1 : part.Quantity;
				Root[2].Add( new PartWithImageElement(part, part.Quantity));
			}
		}
		
		public void DeactivateEditingMode()
		{
			this.NavigationItem.RightBarButtonItem = new UIBarButtonItem("Edit parts list", UIBarButtonItemStyle.Bordered, delegate {
					this.TableView.SetEditing (true, true);
					ActivateEditingMode();				
				}
			);
		}
		
		public void ActivateEditingMode()
		{
			this.NavigationItem.RightBarButtonItem = new UIBarButtonItem("Done", UIBarButtonItemStyle.Done, delegate {
					this.TableView.SetEditing (false, true);
					DeactivateEditingMode();				
				}
			);
		}

		public bool JobReportDataValid()
		{
			try {
				JobReportSection jrSection = Root[0] as JobReportSection;

				if ( jrSection != null)
				{
					// check the pressure element's value
					double pressureValue;
					bool pressureOK = Double.TryParse ((jrSection.Elements[1] as EntryElement).Value, out pressureValue);
					if (!pressureOK)
					{
						// alert the user and return false
						var pressureInvalid = new UIAlertView("Invalid pressure value", "Please enter a number", null, "OK");
						pressureInvalid.Show ();
						
						Selected (NSIndexPath.FromRowSection (1,0));
						return false;
					}

					bool reasonOK = ( (jrSection.Elements[2] as StringElement).Value != "Not chosen" );
					if (!reasonOK)
					{
						// alert the user and return false
						var pointNotChosen = new UIAlertView("Service reason", "Please choose a service reason", null, "OK");
						pointNotChosen.Show ();

						return false;
					}

					bool pointOK = ( (jrSection.Elements[3] as StringElement).Value != "Not chosen" );
					if (!pointOK)
					{
						// alert the user and return false
						var pointNotChosen = new UIAlertView("Service point", "Please choose a service point", null, "OK");
						pointNotChosen.Show ();
						
						return false;
					}
					
					bool commentOK = ( ( jrSection.Elements[4] as MultilineEntryElement).Value != "");
					if (!commentOK)
					{
						// alert the user and return false
						var commentInvalid = new UIAlertView("Empty comment field", "Please describe the issue in a few words", null, "OK");
						commentInvalid.Show ();
						
						Selected (NSIndexPath.FromRowSection (4,0));
						return false;
					}
				}
				return true;
			} catch (Exception e)
			{
				if (_navWorkflow._tabs._scView != null) 
					_navWorkflow._tabs._scView.Log (String.Format ("JobReportDataValid: EXCEPTION: {0} \n {1}", e.Message, e.StackTrace));
				return false;
			}
		}
		
		public void LoadParts()
		{
			var partsSection = Root[2];
			for (int i = partsSection.Count - 1; i>=1; i--)
				partsSection.Remove (partsSection.Elements[i]);

			if (ThisJob.UsedParts != null)
			{
				foreach(Part part in ThisJob.UsedParts)
					partsSection.Add( new PartWithImageElement(part, part.Quantity));
			}
			
			DeactivateEditingMode ();
		}
		
		public void ClearPartsList()
		{
			if (ThisJob != null && ThisJob.UsedParts != null) ThisJob.UsedParts.Clear ();
			var partsSection = Root[2];
			if (partsSection.Count > 1)
			{
				for (int i = partsSection.Count - 1; i>=1; i--)
					partsSection.Remove (partsSection.Elements[i]);
			}
			DeactivateEditingMode ();
		}
		
		public override bool ShouldAutorotateToInterfaceOrientation (UIInterfaceOrientation toInterfaceOrientation)
		{
			return (toInterfaceOrientation == UIInterfaceOrientation.LandscapeLeft || toInterfaceOrientation == UIInterfaceOrientation.LandscapeRight);
		}
	}
	
	public class PartWithImageCell : UITableViewCell // , IImageUpdated
	{
		const int textSize = 18;
		
		const int PicSizeX = 96;
		const int PicSizeY = 96; // 48;
		const int PicXPad = 5;
		const int PicYPad = 5;
		
		const int TextLeftStart = 2 * PicXPad + PicSizeX;
		
		const int TextHeightPadding = 4;
		const int TextYOffset = 0;
		const int MinHeight = PicSizeY + 2 * PicYPad;
		
		static UIFont textFont = UIFont.SystemFontOfSize (textSize);
		
		PartWithImageElement element;
		
		// double _quantity = 0;
		// public double Quantity { get { return _quantity; } set { _quantity = value; } }
		
		UILabel textLabel;
		UIImageView imageView;
		UIStepper stepper;
		
		public PartWithImageCell (IntPtr handle) : base (handle) {
			// Console.WriteLine (Environment.StackTrace);
		}

		// Create the UIViews that we will use here, layout happens in LayoutSubviews
		public PartWithImageCell (UITableViewCellStyle style, NSString ident, PartWithImageElement element, double quantity) : base (style, ident)
		{
			this.element = element;
			this.element.Quantity = quantity;
			
			SelectionStyle = UITableViewCellSelectionStyle.None;
			
			textLabel = new UILabel () {
				Font = textFont,
				TextAlignment = UITextAlignment.Left,
				Lines = 0,
				LineBreakMode = UILineBreakMode.WordWrap
			};
			
			imageView = new UIImageView (new RectangleF (PicXPad, PicYPad, PicSizeX, PicSizeY));
			stepper = new UIStepper() { Value = this.element.Quantity }; // new RectangleF(0, 0, 10, 0) 
			
			UpdateCell ( element.Part, this.element.Quantity);
		
			ContentView.Add (textLabel);
			ContentView.Add (imageView);
			
			this.AccessoryView = stepper;
			((UIStepper)this.AccessoryView).ValueChanged += delegate {
				double newValue = ((UIStepper)this.AccessoryView).Value;
				UpdateCellQuantity (newValue);
			};
		}


		public void UpdateCell (Part newPart, double newQuantity)
		{
			this.element.Part = newPart;
			this.element.Quantity = newQuantity;
			this.stepper.Value = newQuantity;
			textLabel.Text = String.Format (" Part number: {0} \n Description: {1} \n Quantity: {2:0.0}", newPart.PartNo, newPart.Description, newPart.Quantity);

			imageView.Image = (UIImage) newPart.Image;
		}

		public void UpdateCellElement(PartWithImageElement newElement)
		{
			this.element = newElement;
			imageView.Image = newElement.Part.Image;
			this.stepper.Value = newElement.Part.Quantity;
			textLabel.Text = String.Format (" Part number: {0} \n Description: {1} \n Quantity: {2:0.0}", newElement.Part.PartNo, newElement.Part.Description, newElement.Part.Quantity);
		}

		void UpdateCellQuantity(double newQuantity)
		{
			this.element.Part.Quantity = newQuantity;
			this.element.Quantity = newQuantity;
			textLabel.Text = String.Format (" Part number: {0} \n Description: {1} \n Quantity: {2:0.0}", element.Part.PartNo, element.Part.Description, element.Part.Quantity);
		}

		public static float GetCellHeight (RectangleF bounds, string caption)
		{
			bounds.Height = 999;
			
			// Keep the same as LayoutSubviews
			bounds.X = TextLeftStart;
			bounds.Width -= TextLeftStart+TextHeightPadding;
			
			using (var nss = new NSString (caption)){
				var dim = nss.StringSize (textFont, bounds.Size, UILineBreakMode.WordWrap);
				return Math.Max (dim.Height + TextYOffset + 2*TextHeightPadding, MinHeight);
			}
		}
		
		public override void LayoutSubviews ()
		{
			base.LayoutSubviews ();
			var full = ContentView.Bounds;
			var tmp = full;

			tmp.Y += TextYOffset;
			tmp.Height -= TextYOffset;
			tmp.X = TextLeftStart;
			tmp.Width -= TextLeftStart+TextHeightPadding;
			textLabel.Frame = tmp;
		}
	}
	
	public class PartWithImageElement : Element, IElementSizing
	{
		static NSString key = new NSString ("PartImageStringElement");
		
		private Part _part;
		public Part Part { get { return _part; } set { _part = value; } }
		private double _quantity;
		public double Quantity { 
			get { return _quantity; } 
			set { 
				_quantity = value; 
				_part.Quantity = _quantity; 

			} 
		}
		
		public PartWithImageElement(Part part, double quan) : base (null)
		{
			this._part = part;
			this._quantity = quan;
		}


		public override UITableViewCell GetCell (UITableView tv)
		{
			var cell = tv.DequeueReusableCell (CellKey);
			
			// get cell's values from underlying data object (MonoTouch.Dialog Element)
			// otherwise you cannot distinguish between cells that have been deleted and cells that were not visible


			if (cell == null) 
				cell = new PartWithImageCell(UITableViewCellStyle.Default, key, this, _quantity);
			else 
				(cell as PartWithImageCell).UpdateCellElement (this); //.UpdateCell(_part, _quantity); 

			return cell;
		}

		protected override NSString CellKey {
			get {
				return PartWithImageElement.key;
			}
		}
		
		public event NSAction Tapped;
		public override void Selected (DialogViewController dvc, UITableView tableView, NSIndexPath path)
		{
			if (Tapped != null) Tapped();
			tableView.DeselectRow (path, true);
		}
		
		public float GetHeight(UITableView tableView, NSIndexPath indexPath)
		{
			return PartWithImageCell.GetCellHeight (tableView.Bounds,  String.Format (" Part number: {0} \n Description: {1} \n Quantity: {2:0.0}", _part.PartNo, _part.Description, _quantity) );
		}
	}
	
	public class ThreePartsView : UIView
	{
		List<Part> _parts;
		
		public UIButton leftButton, midButton, rightButton;
		public UILabel leftLabel, midLabel, rightLabel;
		
		public ThreePartsView(List<Part> parts)
		{
			_parts = parts;
			// foreach (Part p in _parts) p.Quantity = 1;
			
			leftButton = new UIButton( new RectangleF(13, 20, 190, 190) );
			midButton = new UIButton( new RectangleF(214, 20, 190, 190) );
			rightButton = new UIButton( new RectangleF(415, 20, 190, 190) );
			
			leftLabel = new UILabel( new RectangleF(13,220,190,50) );
			midLabel = new UILabel( new RectangleF(214, 220, 190, 50) );
			rightLabel = new UILabel( new RectangleF(415, 220, 190, 50) );
			
			leftLabel.TextAlignment = midLabel.TextAlignment = rightLabel.TextAlignment = UITextAlignment.Center;
			leftLabel.Lines = midLabel.Lines = rightLabel.Lines = 2;
			leftLabel.LineBreakMode = midLabel.LineBreakMode = rightLabel.LineBreakMode = UILineBreakMode.WordWrap;
			// leftLabel.Layer.BorderWidth = 2f;
			// leftLabel.Layer.BorderColor = UIColor.Black.CGColor;
			
			leftButton.Tag = (int)PartButtons.Left; midButton.Tag = (int)PartButtons.Mid; rightButton.Tag = (int)PartButtons.Right;

			leftButton.TouchUpInside += HandleButtonTouch;
			midButton.TouchUpInside += HandleButtonTouch;
			rightButton.TouchUpInside += HandleButtonTouch;
			
			SetBackGroundColors (UIColor.Clear);
			
			this.Add (leftButton); this.Add (midButton); this.Add (rightButton);
			this.Add (leftLabel); this.Add (midLabel); this.Add (rightLabel);
			
			this.Update (_parts);
		}

		void HandleButtonTouch (object sender, EventArgs e)
		{
			// RE-IMPLEMENTED :: this is now done on workflow navigation controller level (Nav) :: ThreePartsCell.Nav.PopViewControllerAnimated (true);
			switch( (sender as UIButton).Tag )
			{
			case (int)PartButtons.Left: { ThreePartsCell.Nav.ChosenPart = _parts[0]; break; }
			case (int)PartButtons.Mid: { ThreePartsCell.Nav.ChosenPart = _parts[1]; break; }
			case (int)PartButtons.Right: { ThreePartsCell.Nav.ChosenPart = _parts[2]; break; }
			}
		}
		
		public void Update (List<Part> parts)
		{
			this._parts = parts;
			
			// SetBackGroundColors (UIColor.Clear);

			if (_parts[0].Image == null) leftButton.SetBackgroundImage (Part.PlaceholderImage, UIControlState.Normal);
			else leftButton.SetBackgroundImage (_parts[0].Image, UIControlState.Normal);
			
			leftLabel.Text = _parts[0].Description;

			if (_parts.Count >= 2)
			{	
				if (_parts[1].Image == null) { midButton.SetBackgroundImage (Part.PlaceholderImage, UIControlState.Normal); }
				else midButton.SetBackgroundImage (_parts[1].Image, UIControlState.Normal);				
				midLabel.Text = _parts[1].Description;
				midButton.Hidden = false; midLabel.Hidden = false;
			}
			else 
			{ 
				midButton.Hidden = true; midLabel.Hidden = true;
				rightButton.Hidden = true; rightLabel.Hidden = true;
			}
			if (_parts.Count >= 3)
			{
				if (_parts[2].Image == null) { rightButton.SetBackgroundImage (Part.PlaceholderImage, UIControlState.Normal); }
				else rightButton.SetBackgroundImage (_parts[2].Image, UIControlState.Normal);
				rightLabel.Text = _parts[2].Description;
				rightButton.Hidden = false; rightLabel.Hidden = false;
			}
			else { rightButton.Hidden = true; rightLabel.Hidden = true; }		
			
			SetNeedsDisplay ();
		}
		
		public void SetBackGroundColors(UIColor color)
		{
			this.BackgroundColor = color;
			leftButton.BackgroundColor = color;
			midButton.BackgroundColor = color;
			rightButton.BackgroundColor = color;
			leftLabel.BackgroundColor = color;
			midLabel.BackgroundColor = color;
			rightLabel.BackgroundColor = color;
		}
	}
	
	public class ThreePartsCell :  UITableViewCell 
	{
		ThreePartsView _threePartsView;
		public static UsedPartsNavigationController Nav;
		
		public ThreePartsCell(List<Part> parts, NSString nskey) : base(UITableViewCellStyle.Default, nskey)
		{
			// Configuring cell here : selection style, colors, properties
			this.SelectionStyle = UITableViewCellSelectionStyle.None;
			this.BackgroundColor = UIColor.Clear;
			
			_threePartsView = new ThreePartsView(parts);
			ContentView.Add (_threePartsView);
		}
		
		public override void LayoutSubviews ()
		{
			base.LayoutSubviews ();
			_threePartsView.Frame = ContentView.Bounds;
			_threePartsView.SetNeedsDisplay ();
		}
		
		public void UpdateCell(List<Part> newParts)
		{
			_threePartsView.Update (newParts);
		}
	}
	
	public class ThreePartsElement : Element, IElementSizing
	{
		static NSString nskey = new NSString("ThreePartsElement");
		static UsedPartsNavigationController _nav;
		public List<Part> _parts;
		
		public ThreePartsElement(List<Part> parts, UsedPartsNavigationController nav) : base (null)
		{
			this._parts = parts;
			_nav = nav;
			if (ThreePartsCell.Nav == null) ThreePartsCell.Nav = ThreePartsElement._nav;
		}
		
		public override UITableViewCell GetCell (UITableView tv)
		{
			UITableViewCell cell = tv.DequeueReusableCell(nskey) as ThreePartsCell;
			if (cell == null) 
				cell = new ThreePartsCell(_parts, nskey);
			else (cell as ThreePartsCell).UpdateCell(_parts);
			
			return cell;
		}
		
		public float GetHeight(UITableView tv, NSIndexPath indexPath) 
		{
			return 280f;
		}
	}
	
	public class PartsSection : Section
	{
		UsedPartsViewController _upvc;
		public PartsSection(string title, UsedPartsViewController upvc) : base (title)
		{ _upvc = upvc;	}
		
		public void LoadParts()
		{
			string dbPath = ServerClientViewController.dbFilePath;
			
			if ( File.Exists(dbPath) && _upvc.DBParts.Count==0 )
				ReadPartsFromDatabase(dbPath); // into _upvc.DBParts
			
			// now we have _parts filled up with data, it's time to add the elements to the section
			for (int i = 0; i < _upvc.DBParts.Count; i += 3)
			{
				// check if this iteration is last
				bool isLast = ( (_upvc.DBParts.Count - i) <= 3 );
				
				if (isLast) {
					// check to see how many parts left
					int partsLeft = this._upvc.DBParts.Count - i;
					
					List<Part> tmpParts = new List<Part> ();
					tmpParts.Add(_upvc.DBParts[i]); // we are guaranteed to have 1 part because the iteration brought us here
					
					if (partsLeft > 1) { tmpParts.Add(_upvc.DBParts[i+1]); }
					if (partsLeft > 2) { tmpParts.Add(_upvc.DBParts[i+2]); }
					
					this.Add (new ThreePartsElement (tmpParts, _upvc.NavUsedParts) );
					
				}
				else {
					// three parts available
					List<Part> tmpParts = new List<Part> ();
					tmpParts.Add(_upvc.DBParts[i]);
					tmpParts.Add(_upvc.DBParts[i+1]);
					tmpParts.Add(_upvc.DBParts[i+2]);
					this.Add (new ThreePartsElement (tmpParts, _upvc.NavUsedParts) );
				}
			}
		}
		
		public bool ReadPartsFromDatabase(string dbPath)
		{	
			using (var connection = new SqliteConnection("Data Source="+dbPath))
			{
				using (var cmd = connection.CreateCommand())
				{
					connection.Open();
					string sql = "";
					if (MyConstants.EmployeeType == MyConstants.EmployeeTypes.Franchisee)				
						sql = 	"SELECT Parts.Partno, Parts.Prtprice, Parts.Prtdesc, Parts_Pics.Picture " +
							" FROM Parts LEFT JOIN Parts_Pics ON Parts.Partno = Parts_Pics.PartNo " +
							" WHERE Parts.Deletedprt = 0 AND Parts.Plumpparts = 0";							// the last condition hides the plumbing parts for franchisees, so that the list is shorter for them
					else 
						sql = 	"SELECT Parts.Partno, Parts.Prtprice, Parts.Prtdesc, Parts_Pics.Picture " +
							" FROM Parts LEFT JOIN Parts_Pics ON Parts.Partno = Parts_Pics.PartNo " +
							" WHERE Parts.Deletedprt = 0";																	// if not franchisee, show the full parts list

					cmd.CommandText = sql;

					try {
						_upvc.DBParts = new List<Part>();
						using (var reader = cmd.ExecuteReader())
						{
							while (reader.Read () )
							{
								if ( reader["picture"] == DBNull.Value ) 
								{	// picture is null which, we use a placeholder instead
									_upvc.DBParts.Add (new Part 
									{ 
										PartNo = Convert.ToInt32 (reader["partno"]),
										Description = " "+ (string)reader["prtdesc"],
										Price = (double)reader["prtprice"],
										ImageNotFound = true,
										Image = Part.PlaceholderImage
									}	 );
								}
								else 
								{	// a picture exists in the database, so we use it
									Part part = new Part()
									{ 
										PartNo = Convert.ToInt32 (reader["partno"]),
										Description = " "+ (string)reader["prtdesc"],
										Price = (double)reader["prtprice"],
										ImageNotFound = false
									};
									NSData data = new NSData();
									data = NSData.FromArray( (byte[])reader["picture"]);
									part.Image = UIImage.LoadFromData(data);
									_upvc.DBParts.Add (part);
								}
							}
							return true;
						}
					}
					catch {
						Part part = new Part() {
							PartNo = 99999999,
							Description = "We are SORRY that you have to go through this...",
							Price = 0.00f,
							ImageNotFound = true
						};
						_upvc.DBParts.Add (part);
						// Console.WriteLine (string.Format ("ReadPartsFromDatabase exception: {0}", e.Message));
						return false;
					}
				}
			}
		}
	}

	public class MultilineEntryElement : Element, IElementSizing 
	{
		UITextView entry;


		private string _value;
		public string Value { 
			get { return _value; } 
			set {
				_value = value;
				if (entry != null) 
					entry.Text = _value;
			}
		}

		static NSString ekey = new NSString ("myMultilineEntryElement");
		static UIFont font = UIFont.SystemFontOfSize (18); // UIFont.BoldSystemFontOfSize (20);

		// bool isPassword;
		// string placeholder;
		
		/// <summary>
		/// Constructs an EntryElement with the given caption, placeholder and initial value.
		/// </summary>
		/// <param name="caption">
		/// The caption to use
		/// </param>
		/// <param name="placeholder">
		/// Placeholder to display.
		/// </param>
		/// <param name="value">
		/// Initial value.
		/// </param>
		public MultilineEntryElement (string caption, string placeholder, string value) : base (caption)
		{
			Value = value;
			// this.placeholder = placeholder;
		}
		
		/// <summary>
		/// Constructs an EntryElement for password entry with the given caption, placeholder and initial value.
		/// </summary>
		/// <param name="caption">
		/// The caption to use
		/// </param>
		/// <param name="placeholder">
		/// Placeholder to display.
		/// </param>
		/// <param name="value">
		/// Initial value.
		/// </param>
		/// <param name="isPassword">
		/// True if this should be used to enter a password.
		/// </param>
		
		public override string Summary ()
		{
			return Value;
		}

		protected override NSString CellKey {
			get { return MultilineEntryElement.ekey; }
		}
		
		//
		// Computes the X position for the entry by aligning all the entries in the Section
		//
		SizeF ComputeEntryPosition (UITableView tv, UITableViewCell cell)
		{
			Section s = Parent as Section;

			if (s.EntryAlignment.Width != 0)
				return s.EntryAlignment;
			
			SizeF max = new SizeF (-1, -1);
			foreach (var e in s.Elements)
			{
				var ee = e as MultilineEntryElement;
				if (ee == null)
					continue;
				
				var size = tv.StringSize (ee.Caption, font);
				if (size.Width > max.Width)
					max = size;	
			}
			s.EntryAlignment = new SizeF (25 + Math.Min (max.Width, 160), max.Height);
			return s.EntryAlignment;
		}
		
		public override UITableViewCell GetCell (UITableView tv)
		{
			var cell = tv.DequeueReusableCell (ekey);
			if (cell == null)
			{
				cell = new UITableViewCell (UITableViewCellStyle.Default, ekey);
				cell.SelectionStyle = UITableViewCellSelectionStyle.None;
			} 
			else
				RemoveTag (cell, 1);
			
			
			if (entry == null){
				SizeF size = ComputeEntryPosition (tv, cell);
				/*
				 	entry = new UITextField (new RectangleF (size.Width, (cell.ContentView.Bounds.Height-size.Height)/2-1, 320-size.Width, size.Height))
				 	{
						Tag = 1,
						Placeholder = placeholder,
						SecureTextEntry = isPassword
					}; 
				// entry = new UITextView(new RectangleF( size.Width, (cell.ContentView.Bounds.Height-size.Height)/2-1, size.Width-320, 96));
				// entry.AutoresizingMask = UIViewAutoresizing.FlexibleWidth | UIViewAutoresizing.FlexibleLeftMargin;
				 */

				entry = new UITextView( new RectangleF(150, 7, size.Width-110, 96));
				entry.BackgroundColor = UIColor.FromRGB (240,240,240);
				entry.Text = Value ?? "";
				entry.Font = MultilineEntryElement.font;

				
				entry.Ended += delegate {
					Value = entry.Text;

					JobReportSection section = Parent as JobReportSection;
					section.jrd.Comment = Value;
				};

				entry.ReturnKeyType = UIReturnKeyType.Done;
				entry.Changed += delegate(object sender, EventArgs e) {
					if (entry.Text.Length > 0)
					{
						int i = entry.Text.IndexOf("\n", entry.Text.Length - 1);
						if (i > -1)
						{
							entry.Text = entry.Text.Substring(0, entry.Text.Length - 1);
							entry.ResignFirstResponder();	
						}
					}
				};
			}
						
			cell.TextLabel.Text = Caption;
			cell.ContentView.AddSubview (entry);
			return cell;
		}

		public override void Selected (DialogViewController dvc, UITableView tableView, NSIndexPath path)
		{
			entry.BecomeFirstResponder ();
			// base.Selected (dvc, tableView, path);
		}
		
		protected override void Dispose (bool disposing)
		{
			if (disposing){
				entry.Dispose ();
				entry = null;
			}
		}
		
		public float GetHeight (UITableView tableView, NSIndexPath indexPath)
		{
			return 112;
		}
	}

	
	public class JobReportSection : Section
	{
		UsedPartsViewController controller;
		
		public JobReportData jrd = new JobReportData();
		public JobReportSection(UsedPartsViewController _con, string caption, bool isRemovable) : base (caption)
		{
			controller = _con;

			bool warrantyInitialValue = (controller.ThisJob != null) ? controller.ThisJob.Warranty : false;

			var warrantyElement = new BooleanElement("Warranty", warrantyInitialValue);
			warrantyElement.ValueChanged += delegate {
				controller.ThisJob.Warranty = warrantyElement.Value;
				if (isRemovable)
				{
					if (warrantyElement.Value == false)
						controller.RemoveJobReport();
					else controller.AddJobReport (isRemovable);
				}
			};
			
			var pressureElement = new EntryElement("Pressure", "Value", "", false);
			pressureElement.KeyboardType = UIKeyboardType.NumbersAndPunctuation;
			
			var serviceReasonElement = new StringElement("Service reason", "Leak");
			serviceReasonElement.Tapped += HandleServiceReasonElementTapped;
			jrd.ReasonOID = 1; // this is the default value of service reasons for all the jobs

			var dbDefaultPoint = ReadDBServicePoints (true);
			List<string> listDefaultPointDescription = new List<string> (dbDefaultPoint.Values);
			var problemPointElement = new StringElement("Issue description", listDefaultPointDescription[0] );

			List<long> listDefaultPointID = new List<long>( dbDefaultPoint.Keys);
			jrd.PointOID = listDefaultPointID[0];

			problemPointElement.Tapped += HandleProblemElementTapped;
			
			var commentElement = new MultilineEntryElement("Comment", "Your story here", String.Empty); // new MultilineElement("Comment");
			
			this.Add (warrantyElement);
			this.Add (pressureElement);
			this.Add (serviceReasonElement);
			this.Add (problemPointElement);
			this.EntryAlignment = new SizeF(565, 20);
			this.Add (commentElement);
		}
		
		Dictionary<long, string> ReadDBServiceReasons()
		{
			Dictionary<long, string> result = new Dictionary<long, string>();
			
			// read service reasons from database
			using (var connection = new SqliteConnection("Data Source="+ServerClientViewController.dbFilePath))
			{
				using (var cmd = connection.CreateCommand())
				{
					connection.Open();
					string sql = 	"SELECT * FROM SERVICE_REASONS " +
						" WHERE RSN_ACTIVE = 1" +
							" AND ( JOB_TYPE_OID = 0 " +
							" OR JOB_TYPE_OID = (SELECT JOB_TYPE_ID FROM JOB_TYPES WHERE JOB_TYPE_CODE = ? ))" ;
					cmd.CommandText = sql;
					cmd.Parameters.Add ("@JobType", System.Data.DbType.String).Value = controller.ThisJob.Type.Code;
					using (var reader = cmd.ExecuteReader())
					{
						if (reader.HasRows)
						{
							while (reader.Read () )
							{
								// add reasons to dictionary
								result.Add ( (long)reader["reason_id"], (string)reader["reason_desc"]);
							}
						}
						else // reader is empty 
						{
							controller.NavUsedParts.Tabs._scView.Log (String.Format("ReadDBServiceReasons call failed."));
							return null;
						}
					}
				}
			}
			return result;
		}


		
		Dictionary<long, string> ReadDBServicePoints(bool defaultsOnly)
		{
			Dictionary<long, string> result = new Dictionary<long, string> ();
			
			// read the service location points from database
			using (var connection = new SqliteConnection("Data Source="+ServerClientViewController.dbFilePath))
			{
				using (var cmd = connection.CreateCommand())
				{
					connection.Open();
					string sql = "SELECT * FROM SERVICE_POINTS " +
										" WHERE ( JOB_TYPE_OID = 0 " +
											" OR JOB_TYPE_OID = (SELECT JOB_TYPE_ID FROM JOB_TYPES WHERE JOB_TYPE_CODE = ? ))" ;
					if (defaultsOnly)
						sql = "SELECT * FROM SERVICE_POINTS " +
								" WHERE IS_DEFAULT = 1 " + 
									" AND ( JOB_TYPE_OID = 0 " +
										" OR JOB_TYPE_OID = (SELECT JOB_TYPE_ID FROM JOB_TYPES WHERE JOB_TYPE_CODE = ? ))" ;
					cmd.CommandText = sql;
					cmd.Parameters.Add ("@JobType", System.Data.DbType.String).Value = controller.ThisJob.Type.Code;
					using (var reader = cmd.ExecuteReader())
					{
						if (reader.HasRows)
						{
							while (reader.Read () )
							{
								// add points to dictionary
								result.Add ( (long)reader["point_id"], (string)reader["point_desc"]);
							}
						}
						else // reader is empty 
						{
							controller.NavUsedParts.Tabs._scView.Log (String.Format("ReadDBServicePoints call failed."));
							return null;
						}
					}
				}
			}
			return result;			
		}

		public List<ServicePoint> ReadAllDBServicePointsWithCoordinates()
		{
			List<ServicePoint> result = new List<ServicePoint> ();
			// read the service location points from database
			using (var connection = new SqliteConnection("Data Source="+ServerClientViewController.dbFilePath))
			{
				using (var cmd = connection.CreateCommand())
				{
					connection.Open();
					string sql = "SELECT * FROM SERVICE_POINTS";

					cmd.CommandText = sql;
					using (var reader = cmd.ExecuteReader())
					{
						if (reader.HasRows)
						{
							while (reader.Read () )
							{
								// add points to dictionary
								ServicePoint point = new ServicePoint() {
									PointID = (long)reader["point_id"],
									PointDesc = (string)reader["point_desc"],
								};

								point.RectAround = new RectangleF( (long)reader["prec_x"], (long)reader["prec_y"], (long)reader["prec_width"], (long)reader["prec_height"]);

								result.Add ( point );
							}
						}
						else // reader is empty 
						{
							controller.NavUsedParts.Tabs._scView.Log (String.Format("ReadDBServicePoints call failed."));
							return null;
						}
					}
				}
			}
			return result;	
		}
		
		void HandleServiceReasonElementTapped ()
		{
			Dictionary<long, string> dbServiceReasons = ReadDBServiceReasons ();
			
			if (dbServiceReasons.Count > 1)
			{
				// create an uiActionSheet to display the choices
				var chooseReason = new UIActionSheet("Please choose a service reason");
				foreach(string caption in dbServiceReasons.Values)
					chooseReason.AddButton (caption);
				
				chooseReason.Dismissed += delegate(object sender, UIButtonEventArgs e) {
					if (e.ButtonIndex != chooseReason.CancelButtonIndex )
					{
						// when the user makes his choice, save it to JobReportData
						long reasonID = dbServiceReasons.FindKeyByValue ( chooseReason.ButtonTitle (e.ButtonIndex));
						jrd.ReasonOID = reasonID;
						foreach(Element el in this.Elements)
						{
							if (el is StringElement)
							{
								if (el.Caption == "Service reason")
									(el as StringElement).Value = chooseReason.ButtonTitle (e.ButtonIndex);
							}
						}
					}
					controller.ReloadData ();
				};
				
				chooseReason.ShowInView (controller.View);
			}
			else
			{
				// we've got one reason, so... no point in choosing anything
				List<long> reasonIDs = new List<long>(dbServiceReasons.Keys);
				jrd.ReasonOID = reasonIDs[0];
				
				foreach(Element el in this.Elements)
				{
					if (el is StringElement)
					{
						if (el.Caption == "Service reason")
						{
							List<string> descriptions = new List<string>(dbServiceReasons.Values);
							(el as StringElement).Value = descriptions[0];
						}
					}
				}
			}
		}
		
		void HandleProblemElementTapped ()
		{
			// read problem points from database
			Dictionary<long, string> dbServicePoints = ReadDBServicePoints (false);
			
			if (dbServicePoints.Count > 1)
			{
				// create an uiActionSheet to display the choices
				var choosePoint = new UIActionSheet("Please choose a problem location");
				foreach (string caption in dbServicePoints.Values)
					choosePoint.AddButton (caption);
				
				choosePoint.Dismissed += delegate(object sender, UIButtonEventArgs e) {
					if (e.ButtonIndex != choosePoint.CancelButtonIndex)
					{
						// when the user makes his choice, save it to JobReportData
						long pointID = dbServicePoints.FindKeyByValue ( choosePoint.ButtonTitle (e.ButtonIndex));
						jrd.PointOID = pointID;
						
						foreach(Element el in this.Elements)
						{
							if (el is StringElement)
							{
								if (el.Caption == "Issue description")
									(el as StringElement).Value = choosePoint.ButtonTitle (e.ButtonIndex);
							}
						}
					}
					controller.ReloadData ();
				};
				
				choosePoint.ShowInView (controller.View);
			}
			else
			{
				// only one point, choose it
				List<long> pointIDs = new List<long>(dbServicePoints.Keys);
				jrd.PointOID = pointIDs[0];
				
				foreach(Element el in this.Elements)
					if (el is StringElement)
						if ( (el as StringElement).Caption == "Issue description" )
					{
						List<string> points = new List<string>(dbServicePoints.Values);
						(el as StringElement).Value = points[0];
					}
			}
		}
	}

	public class ServicePoint
	{
		public long PointID = 0;
		public string PointDesc = String.Empty;
		public RectangleF RectAround = new RectangleF();
	}

	public class JobReportData
	{
		public long JobOID;
		public long ReasonOID;
		public long PointOID;
		public int Pressure;
		public string Comment;
	}
}

