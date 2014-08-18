using System;
using System.IO;
using System.Drawing;
using System.Collections.Generic;
using Mono.Data.Sqlite;
using MonoTouch.UIKit;
using MonoTouch.Dialog;
using MonoTouch.Foundation;

namespace Puratap
{
	public class SignDailyStockUsed : NewSignatureViewController
	{
		public DialogViewController dvc { get; set; }
		public UIView GeneratedPDFView { get; set; }
		public string PdfFileName { get; set; }

		public EventHandler GoBack { get; set; }
		public EventHandler GoForward { get; set; }
		public EventHandler StartSigning { get; set; }
		public EventHandler FinishSigning { get; set; }
		// public EventHandler ClearSignature { get; set; } declared in base class
		
		UIBarButtonItem back;
		UIBarButtonItem forward;	
		UIBarButtonItem signing;		
		UIBarButtonItem clearSignature;

		public SignDailyStockUsed (DetailedTabs tabs) : base(tabs)
		{
			// todo :: EXCEPTION HANDLING here

			// fill the DVC property here with the stock details
			dvc = new DialogViewController(new RootElement("Used stock"));
			dvc.Root.Add (new Section(""));

			if (File.Exists (ServerClientViewController.dbFilePath) )
			{
				// read the data from database here
				using (var connection = new SqliteConnection("Data Source="+ServerClientViewController.dbFilePath) )
				{
					try 
					{
						using (var cmd = connection.CreateCommand())
						{
							connection.Open();
							cmd.CommandText = "SELECT su.Element_Type as Type, su.Element_OID as ID, p.Part_Desc as Desc, SUM(su.Num_Used) as USED_TODAY " +
												"FROM STOCKUSED su, PARTS p " + 
												"WHERE su.Element_OID = p.Part_ID  " +
													"AND su.Element_Type = 'P'  " +
												"GROUP BY Element_Type, Element_OID, Part_Desc " +
											"UNION SELECT su.Element_Type as Type, su.Element_OID as ID, a.Name as Desc, SUM(su.Num_Used) as USED_TODAY " +
												"FROM STOCKUSED su, ASSEMBLIES a " +
												"WHERE su.Element_OID = a.Assembly_ID  " +
													"AND su.Element_Type = 'A'  " +
											"GROUP BY Element_Type, Element_OID, Name";
							using (var reader = cmd.ExecuteReader())
							{
								while ( reader.Read() )
								{
									string id = (string)(reader["type"]) + Convert.ToString(reader["id"]);
									string description = (string) reader["desc"];
									double used = Convert.ToDouble (reader["used_today"]);

									dvc.Root[0].Add ( new StyledStringElement(
										id + " " + description,
										used.ToString(), 
										UITableViewCellStyle.Value1));
								}
								if (! reader.IsClosed) reader.Close ();
							}
						}
					}
					catch (Exception e) {
						// Console.WriteLine (e.Message);
					}
				}
			}
			GenerateStockUsedPDFPreview ();
			RedrawDailyStockPDF(false);

			GoBack = delegate {
				if (SigningMode)
				{
					FinishSigning(null,null);
					hasBeenSigned = false;
				}
				this.NavigationController.PopToRootViewController (true);
			};

			GoForward = delegate {
				if (this.hasBeenSigned)
				{
					this.NavigationController.SetNavigationBarHidden(true, false);
					this.NavigationController.PopToRootViewController (true);

					this.Tabs._scView.StartNewDataExchange();
					// this.Tabs._scView.InitDataExchange ();
				}
				else
				{
					var alert = new UIAlertView("", "Please finish signing the document first", null, "OK");
					alert.Show();
				}
			};


			ClearSignature = delegate {
				// Signature.Image = new UIImage();
				Signature.Clear ();
				hasBeenSigned = false;
			};


			StartSigning = delegate {
				signing = new UIBarButtonItem("Done", UIBarButtonItemStyle.Done, FinishSigning);
				this.SetToolbarItems (new UIBarButtonItem[] {
						back, 				new UIBarButtonItem(UIBarButtonSystemItem.FlexibleSpace),
						clearSignature,	new UIBarButtonItem(UIBarButtonSystemItem.FlexibleSpace),
						signing,				new UIBarButtonItem(UIBarButtonSystemItem.FlexibleSpace),
						forward
					}, true );
				
				// Signature.Image = new UIImage();
				Signature.Clear ();
				hasBeenSigned = false;
				SigningMode = true;
			};
			
			FinishSigning = delegate {
				SigningMode = false;
				signing = new UIBarButtonItem("Start signing", UIBarButtonItemStyle.Bordered, StartSigning);				

				this.SetToolbarItems (new UIBarButtonItem[] {
						back, 				new UIBarButtonItem(UIBarButtonSystemItem.FlexibleSpace),
						signing,				new UIBarButtonItem(UIBarButtonSystemItem.FlexibleSpace),
						forward
					}, true );
				
				UIImageView iv = new UIImageView();
				iv = (UIImageView) GeneratedPDFView.ViewWithTag(4);
				iv.ContentMode = UIViewContentMode.ScaleAspectFit;
				UIImage im = Signature.GetDrawingImage (); // this.Signature.Image;

				iv.Image = im;
				
				if (hasBeenSigned)
				{
					RedrawDailyStockPDF (true);
					
					PointF offset = new PointF(0, this.PDFView.ScrollView.ContentSize.Height - this.PDFView.ScrollView.Bounds.Height);
					PDFView.ScrollView.SetContentOffset (offset, true);
					// Signature.Image = new UIImage();
					Signature.Clear ();
				}
				
				iv.Dispose ();	im.Dispose ();
				im = null; iv = null;
			};

			back = new UIBarButtonItem(UIBarButtonSystemItem.Reply);
			signing = new UIBarButtonItem("Start signing", UIBarButtonItemStyle.Bordered, StartSigning);
			forward = new UIBarButtonItem(UIBarButtonSystemItem.Action);
			clearSignature = new UIBarButtonItem("Clear signature", UIBarButtonItemStyle.Bordered, ClearSignature);
			
			ToolbarItems = new UIBarButtonItem[] {
				back, 				new UIBarButtonItem(UIBarButtonSystemItem.FlexibleSpace),
				signing,				new UIBarButtonItem(UIBarButtonSystemItem.FlexibleSpace),
				forward
			};
			
			back.Clicked += GoBack;
			forward.Clicked += GoForward;
		}

		public void GenerateStockUsedPDFPreview()
		{
			NSArray a = NSBundle.MainBundle.LoadNib ("DailyStockUsedPDFTemplate", this, null);
			GeneratedPDFView = (UIView)MonoTouch.ObjCRuntime.Runtime.GetNSObject (a.ValueAt (0));

			UILabel tl = (UILabel) GeneratedPDFView.ViewWithTag (1);
			tl.Text = "Employee name: " + MyConstants.EmployeeName;
			tl = (UILabel) GeneratedPDFView.ViewWithTag (2);
			tl.Text = "Date: "+DateTime.Now.Date.ToString ("dd/MM/yyyy");

			UITableViewController usedStock = new UITableViewController();
			usedStock.TableView = (UITableView) GeneratedPDFView.ViewWithTag (3);
			usedStock.TableView.Source = dvc.CreateSizingSource(false);
			usedStock.TableView.ReloadData ();

			// WAS :: usedStock.TableView = (UITableView) ((UIView)dvc.TableView);


			// if (dvc.Root[0].Count > 17)
			{
				float calculatedHeight = dvc.Root[0].Count * dvc.TableView.RowHeight+44;
				GeneratedPDFView.Frame = new RectangleF(GeneratedPDFView.Frame.X, GeneratedPDFView.Frame.Y, GeneratedPDFView.Frame.Width, usedStock.TableView.Frame.Y+calculatedHeight+114); 
				usedStock.TableView.Frame = new RectangleF(usedStock.TableView.Frame.X, usedStock.TableView.Frame.Y, usedStock.TableView.Frame.Width, calculatedHeight);

				UIView sig = GeneratedPDFView.ViewWithTag (4);
				sig.Frame = new RectangleF(sig.Frame.X, usedStock.TableView.Frame.Y+usedStock.TableView.Frame.Height+8, sig.Frame.Width, sig.Frame.Height);
				tl = (UILabel) GeneratedPDFView.ViewWithTag (5);
				tl.Frame = new RectangleF(tl.Frame.X, usedStock.TableView.Frame.Y+usedStock.TableView.Frame.Height+8, tl.Frame.Width, tl.Frame.Height);


				// sig.Dispose (); sig = null;
			}

			if (a!=null) { a.Dispose (); a = null; }
			if (tl != null) { tl.Dispose (); tl = null; }
			if (usedStock != null) { usedStock.Dispose (); usedStock = null; }
		}

		public void RedrawDailyStockPDF(bool DocumentSigned)
		{
			// render created preview in PDF context
			NSMutableData pdfData = new NSMutableData();
			UIGraphics.BeginPDFContext (pdfData, GeneratedPDFView.Bounds, null);
			UIGraphics.BeginPDFPage ();
			GeneratedPDFView.Layer.RenderInContext (UIGraphics.GetCurrentContext ());
			UIGraphics.EndPDFContent ();

			// save the rendered context to disk
			NSError err;
			string pdfID = String.Format ("{0}_{1}_{2}_UsedStock", MyConstants.EmployeeID, MyConstants.EmployeeName, DateTime.Now.Date.ToString ("yyyy-MM-dd"));
			string pdfFileName;
			if (DocumentSigned)	{ pdfFileName = Path.Combine (Environment.GetFolderPath (Environment.SpecialFolder.Personal), pdfID+"_Signed.pdf"); }
			else pdfFileName = Path.Combine (Environment.GetFolderPath (Environment.SpecialFolder.Personal), pdfID+"_Not_Signed.pdf");
			pdfData.Save (pdfFileName, true, out err);

			if (err != null) { err.Dispose (); err = null; }
			if (pdfData != null) { pdfData.Dispose (); pdfData = null; }

			// set up UIWebView in signature view controller			
			this.PDFView.MultipleTouchEnabled = true;
			this.PDFView.ScalesPageToFit = true;
			this.PdfFileName = pdfFileName;

			if (DocumentSigned) PDFView.LoadRequest(new NSUrlRequest( NSUrl.FromFilename (PdfFileName)));
		}

		public override void ViewDidAppear (bool animated)
		{
			PDFView.LoadRequest(new NSUrlRequest( NSUrl.FromFilename (PdfFileName)));
			this.NavigationController.SetToolbarHidden (false, true);
			this.NavigationController.SetToolbarItems (this.ToolbarItems, true);
			base.ViewDidAppear (animated);
		}

		public static bool IsEmpty()
		{
			if (File.Exists (ServerClientViewController.dbFilePath) ) {
				// read the data from database here
				using (var connection = new SqliteConnection("Data Source="+ServerClientViewController.dbFilePath) ) {
					try {
						using (var cmd = connection.CreateCommand()) {
							connection.Open();
							cmd.CommandText = "SELECT su.Element_Type as Type, su.Element_OID as ID, p.Part_Desc as Desc, SUM(su.Num_Used) as USED_TODAY " +
												"FROM STOCKUSED su, PARTS p " + 
												"WHERE su.Element_OID = p.Part_ID  " +
													"AND su.Element_Type = 'P'  " +
												"GROUP BY Element_Type, Element_OID, Part_Desc " +
											"UNION SELECT su.Element_Type as Type, su.Element_OID as ID, a.Name as Desc, SUM(su.Num_Used) as USED_TODAY " +
												"FROM STOCKUSED su, ASSEMBLIES a " +
												"WHERE su.Element_OID = a.Assembly_ID  " +
													"AND su.Element_Type = 'A'  " +
											"GROUP BY Element_Type, Element_OID, Name";
							using (var reader = cmd.ExecuteReader()) {
								if ( reader.Read() ) {
									reader.Close ();
									return false;
								} else {
									reader.Close ();
									return true;
								}
							}
						}
					}
					catch (Exception e) {
						// Console.WriteLine (e.Message);
						return true;
					}
				}
			}
			else // database file does not exist 
				return true;
		}
	}
}

