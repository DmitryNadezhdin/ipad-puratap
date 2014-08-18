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
	public enum AssemblyButtons { Left, Mid, Right }
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
		
		private static List<Part> _dbParts;
		public static List<Part> DBParts { get { return _dbParts; } set { _dbParts = value; } }

		private static List<Assembly> _dbAssemblies;
		public static List<Assembly> DBAssemblies { get { return _dbAssemblies; } set { _dbAssemblies = value; } }
		
		private WorkflowNavigationController _navWorkflow;
		public WorkflowNavigationController NavWorkflow { get { return _navWorkflow; } set { _navWorkflow = value; } }
		
		public UsedPartsNavigationController NavUsedParts { get; set; }

		public override void ViewDidAppear (bool animated)
		{
			this.NavigationItem.SetHidesBackButton (true, false);
			var warrantySection = Root[0];
			var warrantyElement = warrantySection[0];
			if (warrantyElement is BooleanElement)
				(warrantyElement as BooleanElement).Value = ThisJob.Warranty;
			base.ViewDidAppear (animated);

		}
		
		public override void ViewDidDisappear (bool animated)
		{
			base.ViewDidDisappear (animated);
			
			// jobs done on a warranty are not paid for, therefore money is set to 0 
			if (ThisJob.Warranty) ThisJob.MoneyToCollect = 0;
			else { // jobs that are not done under warranty with a price of 0.00 are set back to their retail price

				// this leads to situations when the job was booked with money to collect = 0 and is set back to its retail price
				if (ThisJob.MoneyToCollect < 0.01) ThisJob.MoneyToCollect = ThisJob.Type.RetailPrice;
			}
		}

		public void AddJobReport(bool isRemovable)
		{
			// job report section is the first one (index 0)
			JobReportSection sec = new JobReportSection(this, "Job Report", isRemovable);
			Root.RemoveAt (0);
			ReloadData ();
			Root.Insert (0, sec);
			ReloadData ();

			if (ThisJob != null) 
				ThisJob.JobReportAttached = true;
		}
		
		public void RemoveJobReport()
		{
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
					var elmnt = section [indexPath.Row];

					if (elmnt is PartWithImageElement) {
						var item = elmnt as PartWithImageElement;
						(this.Container as UsedPartsViewController).ThisJob.UsedParts.Remove (item.Part);
					} else {
						if (elmnt is AssemblyWithImageElement) {
							var item = elmnt as AssemblyWithImageElement;
							(this.Container as UsedPartsViewController).ThisJob.UsedAssemblies.Remove (item.ThisAssembly);
						}
					}
					// remove the element from the table
					section.Remove (elmnt);
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
			if (DBParts.Count == 0) {
				PartsSection.ReadPartsFromDatabase (ServerClientViewController.dbFilePath);
			}
			if (DBAssemblies.Count == 0) {
				AssembliesSection.ReadAssembliesFromDatabase (ServerClientViewController.dbFilePath);
			}

			using (var connection = new SqliteConnection("Data Source="+ServerClientViewController.dbFilePath)) {
				using (var cmd = connection.CreateCommand()) {
					connection.Open();
					string sql = "SELECT 'P' as Element_Type, p.Partno as Element_OID, p.Prtdesc as Element_Desc, " +
										" cp.Comp_p_uni as Amount" +
								" FROM Parts p, Componen cp, Standard st " +
								" WHERE p.Deletedprt = 0" +
									" AND st.Stdnumber = ?" +
									" AND cp.Compno = st.Compno" +
									" AND p.Partno = cp.Element_OID" +
								" " +
								" UNION SELECT 'A' as Element_Type, a.Assembly_ID as Element_OID, a.Name as Element_Desc, " +
										" cp.Comp_p_uni as Amount" +
								" FROM Assemblies a, Componen cp, Standard st" +
								" WHERE a.Is_Active = 1" +
									" AND st.StdNumber = ?" +
									" AND cp.CompNo = st.CompNo" +
									" AND a.Assembly_ID = cp.Element_OID";	
//						"SELECT Parts.Partno, Parts.Prtdesc, Componen.Comp_p_uni FROM Parts, Componen, Standard" +
//						" WHERE Parts.Deletedprt = 0 " +
//							" AND Standard.Stdnumber = ? " +
//							" AND Componen.Compno = Standard.Compno " +
//							" AND Parts.Partno = Componen.Partno" ;
					cmd.CommandText = sql;
					cmd.Parameters.Add ("P_BuildID", System.Data.DbType.Int32).Value = number;
					cmd.Parameters.Add ("A_BuildID", System.Data.DbType.Int32).Value = number;
					using (var reader = cmd.ExecuteReader()) {
						if (reader.HasRows) {
							while (reader.Read () ) {
								char elType = Convert.ToChar (reader ["Element_Type"]);
								switch (elType) {
								case 'P':
									{
										int partno = Convert.ToInt32 ( reader["Element_OID"] );
										double prtQuantity = (double) reader["Amount"];
										foreach(Part part in DBParts) {
											if (part.PartNo == partno) {
												part.Quantity = prtQuantity;
												PartRemoved (part.PartNo, part.Quantity);
												break;
											}
										}									
										break;
									}
								case 'A':
									{
										int asmID = Convert.ToInt32 ( reader["Element_OID"] );
										double asmQuantity = (double) reader["Amount"];
										foreach(Assembly a in DBAssemblies) {
											if (a.aID == asmID) {
												a.Quantity = asmQuantity;
												AssemblyRemoved (a.aID, a.Quantity);
												break;
											}
										}
										break;
									}
								}

							}
						}
						else { // reader is empty
							_navWorkflow._tabs._scView.Log (String.Format("SetPartsToBuildNumber: Build is not found in the database or is empty: Build number {0}", number));
						}
					}
				}
			}
		}
		
		public void SetPartsToBuildNumber(long buildID)
		{	// this fills the parts list with parts from the build with the passed ID
			if (DBParts.Count == 0) 
				PartsSection.ReadPartsFromDatabase (ServerClientViewController.dbFilePath);			
			if (DBAssemblies.Count == 0)
				AssembliesSection.ReadAssembliesFromDatabase (ServerClientViewController.dbFilePath);

			using (var connection = new SqliteConnection("Data Source="+ServerClientViewController.dbFilePath))
			{
				using (var cmd = connection.CreateCommand())
				{
					connection.Open();
					string sql = "SELECT 'P' as Element_Type, p.Partno as Element_OID, p.Prtdesc as Element_Desc, " +
										" cp.Comp_p_uni as Amount" +
								" FROM Parts p, Componen cp, Standard st " +
								" WHERE p.Deletedprt = 0" +
									" AND st.Stdnumber = ?" +
									" AND cp.Compno = st.Compno" +
									" AND cp.Element_Type = 'P' " +
									" AND p.Partno = cp.Element_OID" +
						" " +
						" UNION SELECT 'A' as Element_Type, a.Assembly_ID as Element_OID, a.Name as Element_Desc, " +
										" cp.Comp_p_uni as Amount" +
								" FROM Assemblies a, Componen cp, Standard st" +
								" WHERE a.Is_Active = 1" +
										" AND st.StdNumber = ?" +
										" AND cp.CompNo = st.CompNo" +
										" AND cp.Element_Type = 'A' " +
										" AND a.Assembly_ID = cp.Element_OID";
//						"SELECT Parts.Partno, Parts.Prtdesc, Componen.Comp_p_uni FROM Parts, Componen, Standard" +
//						" WHERE Parts.Deletedprt = 0 " +
//						" AND Standard.Stdnumber = ? " +
//						" AND Componen.Compno = Standard.Compno " +
//						" AND Parts.Partno = Componen.Partno" ;
					cmd.CommandText = sql;
					cmd.Parameters.Add ("P_BuildID", System.Data.DbType.Int32).Value = buildID;
					cmd.Parameters.Add ("A_BuildID", System.Data.DbType.Int32).Value = buildID;
					using (var reader = cmd.ExecuteReader()) {
						if (reader.HasRows) {
							while (reader.Read ()) {
								char elType = Convert.ToChar(reader ["Element_Type"]);
								switch (elType) {
								case 'P': {
										int partno = Convert.ToInt32 ( reader["Element_OID"] );
										double prtQuantity = (double) reader["Amount"];
										foreach(Part part in DBParts) {
											if (part.PartNo == partno) {
												part.Quantity = prtQuantity;
												PartChosen (part, true);
												break;
											}
										}
										break;
									}
								case 'A': {
										int asmID = Convert.ToInt32 (reader ["Element_OID"]);
										double asmQuantity = (double) reader ["Amount"];
										foreach (Assembly a in DBAssemblies) {
											if (a.aID == asmID) {
												a.Quantity = asmQuantity;
												AssemblyChosen (a, true);
											}
										}
										break;
									}
								}

							}
						}
						else { // reader is empty
							_navWorkflow._tabs._scView.Log (String.Format("SetPartsToBuildNumber: Build is not found in the database or is empty: Build number {0}", buildID));
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

			// IMPLEMENTED :: added assembly versions of classes that had to do with displaying and selecting parts, assemblies are now in their own section in the same format (3 large pics per row)
			// TODO :: add code to ChosenAssembly method to add it to job's used stock and save it in database
			// TODO :: implement AssemblyWithImageElement, AssemblyWithImageCell classes so that selected assemblies can be displayed in PartsSections
			// TODO :: add code to keep a separate data file with current "float" of stock, updating it every time data is submitted to the server
			
			PartsSection psec = new PartsSection("Parts", this);
			AssembliesSection asec = new AssembliesSection ("Assemblies", this);
			// populate the table view from database here
			psec.LoadParts ();
			asec.LoadAssemblies ();
			root.Add (asec);
			root.Add (psec);
			
			DialogViewController dvc = new DialogViewController(root, true) { Autorotate = false };
			NavUsedParts.PushViewController (dvc, true);
		}

		public void PartRemoved(int partNo, double partQuantity)
		{
			if (ThisJob == null)
				ThisJob = _navWorkflow._tabs._jobRunTable.CurrentJob;

			foreach(Part x in ThisJob.UsedParts) {
				if (x.PartNo == partNo) {
					if (x.Quantity - partQuantity > 0) {
						x.Quantity -= partQuantity;
					} else {
						x.Quantity = 0;
						ThisJob.UsedParts.Remove (x);
					}

					// search through the sections
					foreach( Section sec in this.Root) {
						if (sec is PartsSection) {
							// found the section with parts
							foreach(Element pel in sec) {
								if (pel is PartWithImageElement) {
									if ( (pel as PartWithImageElement).Part.PartNo == x.PartNo) {
										// set the quantity
										if ((pel as PartWithImageElement).Quantity - partQuantity > 0) {
											(pel as PartWithImageElement).Quantity -= partQuantity;
											break;
										}
										else {
											sec.Remove (pel);
											break;
										}
									}
								}
							}
						}
					} // foreach Section in Root
					this.ReloadData ();
					break;
				}
			}
		}

		public void AssemblyRemoved(int asmID, double quantityRemoved)
		{
			if (ThisJob == null) {
				ThisJob = _navWorkflow._tabs._jobRunTable.CurrentJob;
			}
			foreach (Assembly a in ThisJob.UsedAssemblies) {
				if (a.aID == asmID) {
					a.Quantity = ((a.Quantity - quantityRemoved) > 0)? a.Quantity - quantityRemoved : 0;
				}
				if (a.Quantity == 0)
					ThisJob.UsedAssemblies.Remove (a);
				foreach (Section sec in this.Root) {
					if (sec is PartsSection) {
						foreach (Element ael in sec) {
							if (ael is AssemblyWithImageElement) {
								if ((ael as AssemblyWithImageElement).ThisAssembly.aID == a.aID) {
									if ((ael as AssemblyWithImageElement).Quantity - quantityRemoved > 0) {
										(ael as AssemblyWithImageElement).Quantity -= quantityRemoved;
										break;
									}
									else {
										sec.Remove (ael);
										break;
									}
								}
							}
						}
					}
				} // foreach Section in Root
				this.ReloadData ();
				break;
			} // foreach Assembly in ThisJob.UsedAssemblies
		}

		public void PartRemoved(int partNo)
		{
			PartRemoved (partNo, 1);
		}

		public void PartChosen(int partNo)
		{
			if (ThisJob == null)
				ThisJob = _navWorkflow._tabs._jobRunTable.CurrentJob;

			foreach (Part x in UsedPartsViewController.DBParts) {
				if ( x.PartNo == partNo ) {
					PartChosen (x, false);
					break;
				}
			}
		}

		public void AssemblyChosen(Assembly asm, bool settingToStandard) {
			if (ThisJob == null)
				ThisJob = _navWorkflow._tabs._jobRunTable.CurrentJob;

			if (ThisJob.UsedAssemblies == null)
				ThisJob.UsedAssemblies = new List<Assembly> ();

			bool exists = false;
			foreach(Assembly a in ThisJob.UsedAssemblies) {
				if (a.aID == asm.aID) {
					exists = true;
					if (!settingToStandard)
						a.Quantity += 1;

					foreach (Section sec in this.Root) {
						if (sec is PartsSection) {
							foreach (Element elmnt in sec) {
								if (elmnt is AssemblyWithImageElement) {
									if ((elmnt as AssemblyWithImageElement).ThisAssembly.aID == a.aID) {
										(elmnt as AssemblyWithImageElement).Quantity = a.Quantity;
									}
								}
							}
						}
					}
				}
				break;
			}

			if (!exists) {
				if (asm.Quantity <= 0)
					asm.Quantity = 1;
				ThisJob.UsedAssemblies.Add (asm);
				this.Root[2].Add(new AssemblyWithImageElement(asm, asm.Quantity));
				this.ReloadData ();
			}
		}
			
		public void PartChosen(Part part, bool settingToStandard)
		{
			if (ThisJob == null)
				ThisJob = _navWorkflow._tabs._jobRunTable.CurrentJob;

			bool exists = false;
			foreach (Part x in ThisJob.UsedParts) {
				if ( x.PartNo == part.PartNo ) { 
					exists = true;

					if (! settingToStandard) 
						x.Quantity += 1;

					// search through the sections
					foreach( Section sec in this.Root) {
						if (sec.Elements.Count > 0) {
							if (sec is PartsSection) {
								// found the section with parts
								foreach (Element pel in sec) {
									if (pel is PartWithImageElement) {
										if ((pel as PartWithImageElement).Part.PartNo == x.PartNo) {
											// set the quantity
											(pel as PartWithImageElement).Quantity = x.Quantity;
										}
									}
								}
							}
						}
					}
					break;
				}
			}
			if (! exists) {
				if (part.Quantity <= 0)
					part.Quantity = 1;
				ThisJob.UsedParts.Add (part);
				this.Root[2].Add(new PartWithImageElement(part, part.Quantity));
				this.ReloadData ();
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

			if (ThisJob.UsedParts != null) {
				foreach(Part part in ThisJob.UsedParts)
					partsSection.Add (new PartWithImageElement(part, part.Quantity));
			}

			if (ThisJob.UsedAssemblies != null) {
				foreach (Assembly asm in ThisJob.UsedAssemblies) {
					partsSection.Add (new AssemblyWithImageElement (asm, asm.Quantity));
				}
			}
			
			DeactivateEditingMode ();
		}
		
		public void ClearPartsList()
		{
			if (ThisJob == null)
				ThisJob = _navWorkflow._tabs._jobRunTable.CurrentJob;

			if (ThisJob.UsedParts != null)
				ThisJob.UsedParts.Clear ();
			else
				ThisJob.UsedParts = new List<Part> ();

			if (ThisJob.UsedAssemblies != null)
				ThisJob.UsedAssemblies.Clear ();
			else 
				ThisJob.UsedAssemblies = new List<Assembly> ();

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
	
	public class PartWithImageCell : UITableViewCell
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

		UILabel textLabel;
		UIImageView imageView;
		UIStepper stepper;

		// Constructor: create the UIViews that we will use here, layout happens in LayoutSubviews
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
			
			UpdateCell ( this.element.Part, this.element.Quantity);
		
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

	public class AssemblyWithImageCell : UITableViewCell
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

		AssemblyWithImageElement element;

		UILabel textLabel;
		UIImageView imageView;
		UIStepper stepper;

		// Constructor: we'll create the UIViews that we will use here, layout happens in LayoutSubviews
		public AssemblyWithImageCell(UITableViewCellStyle style, 
			NSString key, AssemblyWithImageElement element, double quantity) {

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

			UpdateCell ( this.element.ThisAssembly, this.element.Quantity);

			ContentView.Add (textLabel);
			ContentView.Add (imageView);

			this.AccessoryView = stepper;
			((UIStepper)this.AccessoryView).ValueChanged += delegate {
				double newValue = ((UIStepper)this.AccessoryView).Value;
				UpdateCellQuantity (newValue);
			};
		}

		public void UpdateCell(Assembly newAsm, double newQuantity)
		{
			this.element.ThisAssembly = newAsm;
			this.element.Quantity = newQuantity;
			this.stepper.Value = newQuantity;
			textLabel.Text = String.Format (" Part number: {0} \n Description: {1} \n Quantity: {2:0.0}", newAsm.aID, newAsm.Description, newAsm.Quantity);

			imageView.Image = (UIImage) newAsm.Image;
		}

		public void UpdateCellQuantity(double newQuantity)
		{
			this.element.ThisAssembly.Quantity = newQuantity;
			this.element.Quantity = newQuantity;
			textLabel.Text = String.Format (" Part number: {0} \n Description: {1} \n Quantity: {2:0.0}", element.ThisAssembly.aID, element.ThisAssembly.Description, element.ThisAssembly.Quantity);
		}

		public void UpdateCellElement(AssemblyWithImageElement newElement)
		{
			this.element = newElement;
			this.imageView.Image = newElement.ThisAssembly.Image;
			this.stepper.Value = newElement.ThisAssembly.Quantity;
			textLabel.Text = String.Format (" Part number: {0} \n Description: {1} \n Quantity: {2:0.0}", newElement.ThisAssembly.aID, newElement.ThisAssembly.Description, newElement.ThisAssembly.Quantity);
		}

		public static float GetCellHeight(RectangleF bounds, string caption) {
			bounds.Height = 999;

			// Keep the same as LayoutSubviews
			bounds.X = TextLeftStart;
			bounds.Width -= TextLeftStart+TextHeightPadding;

			using (var nss = new NSString (caption)){
				var dim = nss.StringSize (textFont, bounds.Size, UILineBreakMode.WordWrap);
				return Math.Max (dim.Height + TextYOffset + 2*TextHeightPadding, MinHeight);
			}
		}

		public override void LayoutSubviews()
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

	public class AssemblyWithImageElement : Element, IElementSizing 
	{
		static NSString key = new NSString("AssemblyImageStringElement");
		private Assembly _assembly;
		public Assembly ThisAssembly { get { return _assembly; } set { _assembly = value; } }

		private double _quantity;
		public double Quantity {
			get { return _quantity; }
			set {
				_quantity = value;
				_assembly.Quantity = _quantity;
			}
		}

		public AssemblyWithImageElement(Assembly asm, double quan) : base (null) {
			this._assembly = asm;
			this.Quantity = quan;
		}

		public override UITableViewCell GetCell (UITableView tv) {
			var cell = tv.DequeueReusableCell (CellKey);
			if (cell == null)
				cell = new AssemblyWithImageCell (UITableViewCellStyle.Default, key, this, _quantity);
			else
				(cell as AssemblyWithImageCell).UpdateCellElement (this);
			return cell;
		}

		protected override NSString CellKey {
			get {
				return AssemblyWithImageElement.key;
			}
		}

		public float GetHeight(UITableView tableView, NSIndexPath indexPath)
		{
			return AssemblyWithImageCell.GetCellHeight (tableView.Bounds,  String.Format (" Part number: {0} \n Description: {1} \n Quantity: {2:0.0}", _assembly.PartNo, _assembly.Description, Quantity) );
		}

		public event NSAction Tapped;
		public override void Selected (DialogViewController dvc, UITableView tableView, NSIndexPath path)
		{
			if (Tapped != null) Tapped();
			tableView.DeselectRow (path, true);
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
			this.Quantity = quan;
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

	public class ThreeAssembliesView : UIView {
		List<Assembly> _assemblies;

		public UIButton leftButton, midButton, rightButton;
		public UILabel leftLabel, midLabel, rightLabel;

		public ThreeAssembliesView(List<Assembly> assemblies) {
			_assemblies = assemblies;

			leftButton = new UIButton( new RectangleF(13, 20, 190, 190) );
			midButton = new UIButton( new RectangleF(214, 20, 190, 190) );
			rightButton = new UIButton( new RectangleF(415, 20, 190, 190) );

			leftLabel = new UILabel( new RectangleF(13,220,190,50) );
			midLabel = new UILabel( new RectangleF(214, 220, 190, 50) );
			rightLabel = new UILabel( new RectangleF(415, 220, 190, 50) );

			leftLabel.TextAlignment = midLabel.TextAlignment = rightLabel.TextAlignment = UITextAlignment.Center;
			leftLabel.Lines = midLabel.Lines = rightLabel.Lines = 2;
			leftLabel.LineBreakMode = midLabel.LineBreakMode = rightLabel.LineBreakMode = UILineBreakMode.WordWrap;
			leftButton.Tag = (int)AssemblyButtons.Left; midButton.Tag = (int)AssemblyButtons.Mid; rightButton.Tag = (int)AssemblyButtons.Right;

			leftButton.TouchUpInside += HandleButtonTouch;
			midButton.TouchUpInside += HandleButtonTouch;
			rightButton.TouchUpInside += HandleButtonTouch;

			SetBackGroundColors (UIColor.Clear);

			this.Add (leftButton); this.Add (midButton); this.Add (rightButton);
			this.Add (leftLabel); this.Add (midLabel); this.Add (rightLabel);

			this.Update (_assemblies);
		}

		void HandleButtonTouch (object sender, EventArgs e) {
			switch( (sender as UIButton).Tag )
			{
			case (int)AssemblyButtons.Left: { ThreePartsCell.Nav.ChosenAssembly = _assemblies[0]; break; }
			case (int)AssemblyButtons.Mid: { ThreePartsCell.Nav.ChosenAssembly = _assemblies[1]; break; }
			case (int)AssemblyButtons.Right: { ThreePartsCell.Nav.ChosenAssembly = _assemblies[2]; break; }
			}
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

		public void Update(List<Assembly> newAssemblies)
		{
			this._assemblies = newAssemblies;

			// always show the left assembly button
			if (_assemblies[0].Image == null) leftButton.SetBackgroundImage (Part.PlaceholderImage, UIControlState.Normal);
			else leftButton.SetBackgroundImage (_assemblies[0].Image, UIControlState.Normal);
			leftLabel.Text = _assemblies[0].Description;

			// if two or more, show mid assembly button
			if (_assemblies.Count >= 2) {	
				if (_assemblies[1].Image == null) { midButton.SetBackgroundImage (Part.PlaceholderImage, UIControlState.Normal); }
				else midButton.SetBackgroundImage (_assemblies[1].Image, UIControlState.Normal);				
				midLabel.Text = _assemblies[1].Description;
				midButton.Hidden = false; midLabel.Hidden = false;
			}
			else { 
				midButton.Hidden = true; midLabel.Hidden = true;
				rightButton.Hidden = true; rightLabel.Hidden = true;
			}

			// if three, show right assembly button
			if (_assemblies.Count >= 3) {
				if (_assemblies[2].Image == null) { rightButton.SetBackgroundImage (Part.PlaceholderImage, UIControlState.Normal); }
				else rightButton.SetBackgroundImage (_assemblies[2].Image, UIControlState.Normal);
				rightLabel.Text = _assemblies[2].Description;
				rightButton.Hidden = false; rightLabel.Hidden = false;
			}
			else { rightButton.Hidden = true; rightLabel.Hidden = true; }		

			SetNeedsDisplay ();
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
			// RE-IMPLEMENTED :: this is now done on UsedPartsNavigationController level (Nav) :: ThreePartsCell.Nav.PopViewControllerAnimated (true);
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


	public class ThreeAssembliesCell : UITableViewCell {
		ThreeAssembliesView _tav;
		public static UsedPartsNavigationController Nav;

		public ThreeAssembliesCell(List<Assembly> assemblies, NSString nskey) : base (UITableViewCellStyle.Default, nskey)
		{
			this.SelectionStyle = UITableViewCellSelectionStyle.None;
			this.BackgroundColor = UIColor.Clear;
			_tav = new ThreeAssembliesView (assemblies);
			this.ContentView.Add (_tav);
		}

		public override void LayoutSubviews ()
		{
			base.LayoutSubviews ();
			_tav.Frame = this.ContentView.Bounds;
			_tav.SetNeedsDisplay ();
		}

		public void UpdateCell(List<Assembly> newAssemblies) {
			_tav.Update (newAssemblies);
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

		public float GetHeight(UITableView tv, NSIndexPath indexPath) { return 280f; }
	}

	public class ThreeAssembliesElement : Element, IElementSizing {
		static NSString nskey = new NSString ("ThreeAssembliesElement");
		static UsedPartsNavigationController _nav;
		public List<Assembly> _assemblies;

		public ThreeAssembliesElement(List<Assembly> assemblies, UsedPartsNavigationController nav) : base (null)
		{
			this._assemblies = assemblies;
			_nav = nav;
			if (ThreeAssembliesCell.Nav == null)
				ThreeAssembliesCell.Nav = ThreeAssembliesElement._nav;
		}

		public override UITableViewCell GetCell (UITableView tv)
		{
			UITableViewCell cell = tv.DequeueReusableCell (nskey) as ThreeAssembliesCell;
			if (cell == null)
				cell = new ThreeAssembliesCell (_assemblies, nskey);
			else
				(cell as ThreeAssembliesCell).UpdateCell (_assemblies);
			return cell;
		}

		public float GetHeight(UITableView tv, NSIndexPath indexPath) { return 280f; }
	}

	public class AssembliesSection : Section {
		UsedPartsViewController _upvc;
		public AssembliesSection(string title, UsedPartsViewController upvc) : base (title) { _upvc = upvc; }

		public void LoadAssemblies() {
			string dbPath = ServerClientViewController.dbFilePath;

			if ( File.Exists(dbPath) && UsedPartsViewController.DBAssemblies.Count == 0 )
				ReadAssembliesFromDatabase(dbPath); // into _upvc.DBAssemblies

			// now we have DBAssemblies filled up with data, it's time to add the elements to the section
			for (int i = 0; i < UsedPartsViewController.DBAssemblies.Count; i += 3)
			{
				// check if this iteration is last
				bool isLast = ( (UsedPartsViewController.DBAssemblies.Count - i) <= 3 );

				if (isLast) {
					// check to see how many parts left
					int partsLeft = UsedPartsViewController.DBAssemblies.Count - i;

					List<Assembly> tmpAssemblies = new List<Assembly> ();
					tmpAssemblies.Add(UsedPartsViewController.DBAssemblies[i]); // we are guaranteed to have 1 part because the iteration brought us here

					if (partsLeft > 1) { tmpAssemblies.Add(UsedPartsViewController.DBAssemblies[i+1]); }
					if (partsLeft > 2) { tmpAssemblies.Add(UsedPartsViewController.DBAssemblies[i+2]); }

					this.Add (new ThreeAssembliesElement (tmpAssemblies, _upvc.NavUsedParts) );

				}
				else {
					// three assemblies available
					List<Assembly> tmpAssemblies = new List<Assembly> ();
					tmpAssemblies.Add(UsedPartsViewController.DBAssemblies[i]);
					tmpAssemblies.Add(UsedPartsViewController.DBAssemblies[i+1]);
					tmpAssemblies.Add(UsedPartsViewController.DBAssemblies[i+2]);
					this.Add (new ThreeAssembliesElement (tmpAssemblies, _upvc.NavUsedParts) );
				}
			}
		}

		public static UIImage GetImageForAssembly(string asmID) {
			UIImage foundImage = null;
			string[] fileNames = Directory.GetFiles (NSBundle.MainBundle.BundlePath+"/Images/Parts");
			foreach (string fileName in fileNames) {
				FileInfo fi = new FileInfo (fileName);
				if (fi.Name.StartsWith (asmID)) {
					foundImage = UIImage.FromBundle ("/Images/Parts/"+fi.Name);
					break;
				}
			}
			return foundImage;
		}

		public static bool ReadAssembliesFromDatabase(string dbPath) {
			using (var connection = new SqliteConnection ("Data Source=" + dbPath)) {
				using (var cmd = connection.CreateCommand ()) {
					connection.Open ();
					string sql = (MyConstants.EmployeeType == MyConstants.EmployeeTypes.Franchisee) ? 
						"SELECT asm.Assembly_ID, asm.Name, asm.Picture " +
						"  FROM ASSEMBLIES asm" +
						"  WHERE asm.Is_Active = 1 AND asm.Plumbers_Only = 0" :
						"SELECT asm.Assembly_ID, asm.Name, asm.Picture " +
						"  FROM ASSEMBLIES asm" +
						"  WHERE asm.Is_Active = 1 ";

					cmd.CommandText = sql;

					try {
						UsedPartsViewController.DBAssemblies = new List<Assembly>();
						using (var reader = cmd.ExecuteReader ()) {
							while (reader.Read ()) {
								Assembly asm = new Assembly();
								asm.aID = Convert.ToInt32(reader["Assembly_ID"]);
								asm.Description = (string)reader["Name"];
								if (reader["Picture"] == DBNull.Value || ((byte[])reader["Picture"]).Length == 0) {
									UIImage asmImgFromFile = GetImageForAssembly('A' + asm.aID.ToString());
									if (asmImgFromFile == null) {
										asm.ImageNotFound = true;
										asm.Image = Assembly.PlaceholderImage;
									} else {
										asm.ImageNotFound = false;
										asm.Image = asmImgFromFile;
									}
								} else {
									asm.ImageNotFound = false;
									NSData data = new NSData();
									data = NSData.FromArray( (byte[])reader["picture"]);
									asm.Image = UIImage.LoadFromData(data);
								}
								UsedPartsViewController.DBAssemblies.Add (asm);
							}
						}
						return true;
					} catch {
						Assembly asm = new Assembly ();
						asm.aID = 9999999;
						asm.Description = "";
						asm.ImageNotFound = true;
						UsedPartsViewController.DBAssemblies.Add (asm);
						return false;
					}
				}
			}
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
			
			if ( File.Exists(dbPath) && UsedPartsViewController.DBParts.Count==0 )
				ReadPartsFromDatabase(dbPath); // into _upvc.DBParts
			
			// now we have _parts filled up with data, it's time to add the elements to the section
			for (int i = 0; i < UsedPartsViewController.DBParts.Count; i += 3)
			{
				// check if this iteration is last
				bool isLast = ( (UsedPartsViewController.DBParts.Count - i) <= 3 );
				
				if (isLast) {
					// check to see how many parts left
					int partsLeft = UsedPartsViewController.DBParts.Count - i;
					
					List<Part> tmpParts = new List<Part> ();
					tmpParts.Add(UsedPartsViewController.DBParts[i]); // we are guaranteed to have 1 part because the iteration brought us here
					
					if (partsLeft > 1) { tmpParts.Add(UsedPartsViewController.DBParts[i+1]); }
					if (partsLeft > 2) { tmpParts.Add(UsedPartsViewController.DBParts[i+2]); }
					
					this.Add (new ThreePartsElement (tmpParts, _upvc.NavUsedParts) );
					
				}
				else {
					// three parts available
					List<Part> tmpParts = new List<Part> ();
					tmpParts.Add(UsedPartsViewController.DBParts[i]);
					tmpParts.Add(UsedPartsViewController.DBParts[i+1]);
					tmpParts.Add(UsedPartsViewController.DBParts[i+2]);
					this.Add (new ThreePartsElement (tmpParts, _upvc.NavUsedParts) );
				}
			}
		}

		public static UIImage GetImageForPart(string partID)
		{
			UIImage foundImage = null;
			string[] fileNames = Directory.GetFiles (NSBundle.MainBundle.BundlePath+"/Images/Parts");
			foreach (string fileName in fileNames) {
				FileInfo fi = new FileInfo (fileName);
				if (fi.Name.StartsWith (partID)) {
					foundImage = UIImage.FromBundle ("/Images/Parts/"+fi.Name);
					break;
				}
			}
			return foundImage;
		}
		
		public static bool ReadPartsFromDatabase(string dbPath)
		{
			using (var connection = new SqliteConnection("Data Source="+dbPath))
			{
				using (var cmd = connection.CreateCommand())
				{
					connection.Open();
					string sql = "";
					if (MyConstants.EmployeeType == MyConstants.EmployeeTypes.Franchisee)				
						sql = 	"SELECT Parts.Partno, Parts.Prtdesc, Parts_Pics.Picture " +
							" FROM Parts LEFT JOIN Parts_Pics ON Parts.Partno = Parts_Pics.PartNo " +
							" WHERE Parts.Deletedprt = 0 AND Parts.Plumpparts = 0";							// the last condition hides the plumbing parts for franchisees, so that the list is shorter for them
					else 
						sql = 	"SELECT Parts.Partno, Parts.Prtdesc, Parts_Pics.Picture " +
							" FROM Parts LEFT JOIN Parts_Pics ON Parts.Partno = Parts_Pics.PartNo " +
							" WHERE Parts.Deletedprt = 0";													// if not franchisee, show the full parts list

					cmd.CommandText = sql;

					try {
						UsedPartsViewController.DBParts = new List<Part>();
						using (var reader = cmd.ExecuteReader())
						{
							while (reader.Read () )
							{
								int partID = Convert.ToInt32 (reader["partno"]);
								string partDesc = " "+ (string)reader["prtdesc"];

								if ( reader["picture"] == DBNull.Value ) 
								{	// picture is null, we try too look up the pic in app resources
									UIImage partImg = GetImageForPart('P' + reader["partno"].ToString());
									Part newPart = new Part { PartNo = partID, Description = partDesc } ;

									if (partImg == null) {
										// use a placeholder image
										newPart.Image = Part.PlaceholderImage;
										newPart.ImageNotFound = true;
									}
									else {
										newPart.Image = partImg;
										newPart.ImageNotFound = false;
									}
									UsedPartsViewController.DBParts.Add(newPart);
								}
								else 
								{	// a picture exists in the database, so we use it
									Part part = new Part()
									{ 
										PartNo = Convert.ToInt32 (reader["partno"]),
										Description = " "+ (string)reader["prtdesc"],
										ImageNotFound = false
									};
									NSData data = new NSData();
									data = NSData.FromArray( (byte[])reader["picture"]);
									part.Image = UIImage.LoadFromData(data);
									UsedPartsViewController.DBParts.Add (part);
								}
							}
							return true;
						}
					}
					catch (Exception e) {
						Part part = new Part() {
							PartNo = 99999999,
							Description = "Sorry...",
							ImageNotFound = true
						};
						UsedPartsViewController.DBParts.Add (part);
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

