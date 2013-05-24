using MonoTouch.UIKit;
using System.Drawing;
using System.IO;
using System;
using MonoTouch.Foundation;
using Mono.Data.Sqlite;
using MonoTouch.Dialog;
using System.Collections.Generic;

namespace Application
{
	public class ProductsDVC : DialogViewController
	{
		static JobRunTable jrt;
		static JobInstallationViewController jivc;

		public static List<Product> dbProducts;
		public static List<Product> ReadProducts()
		{
			List<Product> result = null;
			string dbPath = ServerClientViewController.dbFilePath;

			// read products data here
			using (var dbConnection = new SqliteConnection("Data Source = "+dbPath))
			{
				dbConnection.Open();
				using (var dbCommand = dbConnection.CreateCommand ())
				{
					string sql = "SELECT * FROM PRODUCTS WHERE PRDCT_ACTIVE=1";
					dbCommand.CommandText = sql;
					using (var reader = dbCommand.ExecuteReader ())
					{
						result = new List<Product>();
						while (reader.Read ())
						{
							result.Add (new Product() {
								ID = (long)reader["product_id"],
								Name = (string)reader["product_name"],
								Image = UIImage.LoadFromData (NSData.FromArray ( (byte[])reader["picture"])),
								PartsLink = (long)reader["stndrd_part_oid"]
							});
						}
						if (!reader.IsClosed) reader.Close ();
					}
				}
			}
			return result;
		}

		public ProductsDVC(RootElement root, bool pushing, bool filling, JobRunTable JRT, JobInstallationViewController JIVC) : base (root, pushing)
		{
			jivc = JIVC;
			jrt = JRT;
			if (ProductsDVC.dbProducts == null || ProductsDVC.dbProducts.Count == 0)
				dbProducts = ProductsDVC.ReadProducts ();

			if (filling)
			{
				this.Root = new RootElement("Choose product");
				Root.Add (new Section("Products"));

				foreach(Product product in dbProducts)
					Root[0].Add (new ProductBadgeElement(product, null));
			}
		}

		public override void Selected (NSIndexPath indexPath)
		{
			ProductBadgeElement element = (this.Root[0].Elements[indexPath.Row] as ProductBadgeElement);
			Product product = element.ThisProduct;

			jivc.AddPartsRangeToList (product.PartsLink);
			jivc.GoChooseInstallType ();
		}
	}

	public class InstallTypesDVC : DialogViewController
	{
		static JobRunTable jrt;
		static JobInstallationViewController jivc;

		// this reads install_types table from DB

		public static List<InstallationType> dbInstallTypes;
		public static List<InstallationType> ReadInstallTypes()
		{
			List<InstallationType> result = null;
			string dbPath = ServerClientViewController.dbFilePath;
			// read install types data here
			using (var dbConnection = new SqliteConnection("Data Source = "+dbPath))
			{
				dbConnection.Open();
				using (var dbCommand = dbConnection.CreateCommand ())
				{
					string sql = "SELECT * FROM INSTALL_TYPES WHERE IT_USED=1";
					dbCommand.CommandText = sql;
					using (var reader = dbCommand.ExecuteReader ())
					{
						result = new List<InstallationType>();
						while (reader.Read ())
						{
							InstallationType type = new InstallationType() {
								ID = (long)reader["it_id"],
								Name = (string)reader["it_name"],
								Image = UIImage.LoadFromData (NSData.FromArray ( (byte[])reader["picture"])),
								PartsLink = (long)reader["stnd_oid"]
							};

							if (type.Image == null)
							{
								// using (var image = UIImage.FromBundle ("/Images/puratap-logo"))
									type.Image = UIImage.FromBundle ("/Images/puratap-logo"); //image;
							}

							result.Add (type);
						}
						if (!reader.IsClosed) reader.Close ();
					}
				}
			}
			return result;
		}

		public InstallTypesDVC(RootElement root, bool pushing, bool filling, JobRunTable JRT, JobInstallationViewController JIVC) : base(root, pushing)
		{
			jrt = JRT; jivc = JIVC;
			if (InstallTypesDVC.dbInstallTypes == null || InstallTypesDVC.dbInstallTypes.Count == 0)
				dbInstallTypes = InstallTypesDVC.ReadInstallTypes ();

			if (filling)
			{
				this.Root = new RootElement("Choose install type");
				Root.Add (new Section("Install types"));

				foreach(InstallationType installType in dbInstallTypes)
				{
					Root[0].Add (new InstallTypeBadgeElement(installType, null));
				}
			}
		}

		public override void Selected (NSIndexPath indexPath)
		{
			InstallTypeBadgeElement selectedElement = (this.Root[0].Elements[indexPath.Row] as InstallTypeBadgeElement);
			InstallationType insType = selectedElement.ThisInstall;
			SaveInstallTypeToDatabase(insType.ID);

			jivc.AddPartsRangeToList (insType.PartsLink);
			jivc.GoChooseSaleOptions();

			// find if the database contains a roof install sale option
			InstallationType roofIns = InstallTypesDVC.dbInstallTypes.FindLast(
					delegate(InstallationType ins) 
						{ return ins.Name == "Roof" ;} );

			// if it does, we must check if roof installation was chosen by the user
			if (roofIns != null)
			{
				// if a roof installation has been chosen, an extra fee must be applied to the job
				if (insType.ID == roofIns.ID)
				{
					jivc.dvcSO.SaveOptionToDatabase (11);
					jivc.dvcSO.ApplyExtraFeeForSaleOption(11);
				}
			}

			// base.Selected (indexPath);
		}

		void SaveInstallTypeToDatabase(long id)
		{	
			string dbPath = MyConstants.DBReceivedFromServer;
			using (var connection = new SqliteConnection("Data Source="+dbPath))
			{
				connection.Open ();
				using (var cmd = connection.CreateCommand())
				{
					cmd.CommandText = "UPDATE WSALES SET OPTIONS = ? WHERE UNITNUM = ?";
					cmd.Parameters.Add ("@InstallTypeID", System.Data.DbType.Int64).Value = id;
					cmd.Parameters.Add ("@UnitNumber", System.Data.DbType.Double).Value = InstallTypesDVC.jrt.CurrentJob.UnitNumber;
					try {
						cmd.ExecuteNonQuery ();
					}
					catch (Exception e) {
						InstallTypesDVC.jrt._tabs._scView.Log ("Failed to update WSALES table (SET OPTIONS = "+id.ToString ()+") in "+Path.GetFileName (dbPath));
						InstallTypesDVC.jrt._tabs._scView.Log (String.Format ("Exception: {0}, Source: {1}", e.Message, e.Source));
					}
				}
			}			
		}
	}



	public class SaleOptionsDVC : DialogViewController
	{
		static JobRunTable jrt;
		static JobInstallationViewController jivc;

		// this reads tap model data from DB

		public static List<SaleOption> dbSaleOptions;
		public static List<SaleOption> ReadSaleOptions()
		{
			List<SaleOption> result = null;
			string dbPath = ServerClientViewController.dbFilePath;

			try {
				// read sale options data here
				using (var dbConnection = new SqliteConnection("Data Source = "+dbPath))
				{
					dbConnection.Open();
					using (var dbCommand = dbConnection.CreateCommand ())
					{
						string sql = "SELECT * FROM OPTIONS_REF "; // WHERE OPTION_ACTIVE=1";
						dbCommand.CommandText = sql;
						using (var reader = dbCommand.ExecuteReader ())
						{
							result = new List<SaleOption>();
							while (reader.Read ())
							{
								SaleOption option = new SaleOption() {
									Active = Convert.ToBoolean ( (byte) reader["option_active"] ),
									ID = (long) reader["option_id"],
									Name = (string) reader["option_desc"],
									Type = (string) reader["option_type"],
									CustomerSurcharge = Convert.ToDouble (reader["surcharge"]),
									ExtraFee = Convert.ToDouble (reader["extra_fee"]),
									Image = UIImage.LoadFromData (NSData.FromArray ( (byte[])reader["picture"])),
									PartLink = (reader["partno_oid"] == DBNull.Value) ? -1 : (long)reader["partno_oid"]
								};
								if (option.Image == null)
								{
									// using (var image = UIImage.FromBundle ("/Images/puratap-logo"))
									option.Image = UIImage.FromBundle ("/Images/puratap-logo"); // image;
								}

								result.Add (option);
							}
							if (!reader.IsClosed) reader.Close ();
						}
					}
				}
				return result;
			}
			catch (Exception e) { 
				return null; 
			}
		}

		public SaleOptionsDVC(RootElement root, bool pushing, bool filling, JobRunTable JRT, JobInstallationViewController JIVC) : base (root, pushing)
		{
			jrt = JRT; jivc = JIVC;
			if (SaleOptionsDVC.dbSaleOptions == null || SaleOptionsDVC.dbSaleOptions.Count == 0)
				dbSaleOptions = SaleOptionsDVC.ReadSaleOptions ();

			if (filling)
			{
				this.Root = new RootElement("Choose install options");
				Section scAllDone = new Section("");
				var allDone = new StringElement("Finished choosing options", "");
				scAllDone.Add (allDone);
				this.Root.Add (scAllDone);

				// fill the Tap Models section
				Section scTapModels = new Section("Tap models");
				if (dbSaleOptions != null)
				{
					foreach(SaleOption option in dbSaleOptions)
					{
						if (option.Type == "Taps" && option.Active) {
							scTapModels.Add (new SaleOptionBadgeElement(option, null));
						}
					}
				}
				this.Root.Add (scTapModels);

				// fill the Extras section
				Section scExtras = new Section("Extras");
				if (dbSaleOptions != null)
				{
					foreach(SaleOption option in dbSaleOptions)
					{
						if (option.Type != "Taps" && option.Active) {
							scExtras.Add (new SaleOptionBadgeElement(option, null));
						}
					}
				}
				this.Root.Add (scExtras);
			}
		}

		public void SaveOptionToDatabase(long optionID)
		{
			long unitID = SaleOptionsDVC.jrt.CurrentJob.UnitNumber;
			long jobID = SaleOptionsDVC.jrt.CurrentJob.JobBookingNumber;

			string dbPath = MyConstants.DBReceivedFromServer;
			using (var connection = new SqliteConnection("Data Source="+dbPath))
			{
				connection.Open ();
				using (var cmd = connection.CreateCommand())
				{
					// check if the option was saved previously
					cmd.CommandText = "INSERT INTO SALES_OPTIONS (UNIT_OID, OPTION_OID, JOB_OID) VALUES (?, ?, ?)";

					cmd.Parameters.Add ("@UnitID", System.Data.DbType.Int64).Value = unitID;
					cmd.Parameters.Add ("@OptionID", System.Data.DbType.Int64).Value = optionID;
					cmd.Parameters.Add ("@JobID", System.Data.DbType.Int64).Value = jobID;

					try {
						cmd.ExecuteNonQuery ();
					}
					catch (Exception e) {
						SaleOptionsDVC.jrt._tabs._scView.Log (String.Format ("Failed to select from SALES_OPTIONS table (jobID = {0}, unitID = {1}, optionID = {2}", jobID, unitID, optionID));
						SaleOptionsDVC.jrt._tabs._scView.Log (String.Format ("Exception: {0}, Source: {1}", e.Message, e.Source));
					}
				}
			}
		}

		void ClearSaleOptionsInDB()
		{
			// delete from SALES_OPTIONS by jobID and unitID

			long unitID = SaleOptionsDVC.jrt.CurrentJob.UnitNumber;
			long jobID = SaleOptionsDVC.jrt.CurrentJob.JobBookingNumber;
			
			string dbPath = MyConstants.DBReceivedFromServer;
			using (var connection = new SqliteConnection("Data Source="+dbPath))
			{
				connection.Open ();
				using (var cmd = connection.CreateCommand())
				{
					cmd.CommandText = "DELETE FROM SALES_OPTIONS WHERE UNIT_OID = ? AND JOB_OID = ?";
					cmd.Parameters.Add ("@UnitID", System.Data.DbType.Int64).Value = unitID;
					cmd.Parameters.Add ("@JobID", System.Data.DbType.Int64).Value = jobID;

					try {
						cmd.ExecuteNonQuery ();
					}
					catch (Exception e) {
						SaleOptionsDVC.jrt._tabs._scView.Log (String.Format ("Failed to delete from SALES_OPTIONS table (jobID = {0}, unitID = {1}", jobID, unitID));
						SaleOptionsDVC.jrt._tabs._scView.Log (String.Format ("Exception: {0}, Source: {1}", e.Message, e.Source));
					}
				}
			}
		}

		void SaveSaleOptionsToDatabase()
		{
			ClearSaleOptionsInDB ();

			foreach(SaleOptionBadgeElement element in Root[1])
			{
				if (element.Accessory == UITableViewCellAccessory.Checkmark)
					SaveOptionToDatabase (element.ThisOption.ID);
			}

			foreach(SaleOptionBadgeElement element in Root[2])
				if (element.Accessory == UITableViewCellAccessory.Checkmark)
					SaveOptionToDatabase (element.ThisOption.ID);
		}

		public override void Selected (NSIndexPath indexPath)
		{
			if (indexPath.Section == 0)
			{
				SaveSaleOptionsToDatabase ();
				jivc.InstallDataGathered ();
			}
			else
			{
				SaleOptionBadgeElement element = (this.Root[indexPath.Section].Elements[indexPath.Row] as SaleOptionBadgeElement);
				SaleOption option = element.ThisOption;
				if (indexPath.Section == 1)
				{
					// A tap model is selected

					// Tap models act as a radiogroup, this means we will have to find the option that was selected previously and "unselect" it
					foreach (SaleOptionBadgeElement _element in Root[indexPath.Section])
					{
						if (_element.Accessory != UITableViewCellAccessory.None)
						{
							// exclude the part(s) associated with this option from the parts list
							SaleOption _option = _element.ThisOption;
							if (_option.PartLink != 0) 
								jivc.PartRemoved (Convert.ToInt32 (_option.PartLink));

							// remove the "tick" from the element
							_element.Accessory = UITableViewCellAccessory.None;

							// invoke the routines to remove customer surcharge and extra fee
							RemoveSurchargeForSaleOption (_option.ID);
							RemoveExtraFeeForSaleOption (_option.ID);
						}
					}

					// add the part(s) associated with the option selected by user to the list
					jivc.PartChosen ( Convert.ToInt32 (option.PartLink) );
					// "tick" the chosen element
					element.Accessory = UITableViewCellAccessory.Checkmark;

					// invoke the routines to apply customer surcharge and extra fee
					ApplySurchargeForSaleOption (option.ID);
					ApplyExtraFeeForSaleOption (option.ID);
				}
				else // non-tap option was selected
				{
					// check if it was selected already
					if (element.Accessory != UITableViewCellAccessory.None)
					{
						// the option was selected previously, it must be "unselected" now
						element.Accessory = UITableViewCellAccessory.None;
						if (option.PartLink != 0)
							jivc.RemovePartsRangeFromList(option.PartLink);

						RemoveSurchargeForSaleOption (option.ID);
						RemoveExtraFeeForSaleOption (option.ID);
					}
					else {
						// the option was not selected, apply the appropriate changes
						element.Accessory = UITableViewCellAccessory.Checkmark;
						if (option.PartLink != 0)
							jivc.AddPartsRangeToList (option.PartLink);

						ApplySurchargeForSaleOption (option.ID);
						ApplyExtraFeeForSaleOption (option.ID);

						// Console.WriteLine (String.Format ("Fee: {0}, Price: {1}", SaleOptionsDVC.jrt.CurrentJob.EmployeeFee, SaleOptionsDVC.jrt.CurrentJob.MoneyToCollect));
					}
				}
			}
			this.ReloadData ();
			base.Selected ( indexPath);
		}

		public void ApplySurchargeForSaleOption(long optionID)
		{
			Job curJob = SaleOptionsDVC.jrt.CurrentJob;

			if (curJob.MoneyToCollect > 1)
			{
				curJob.MoneyToCollect += GetSurchargeForOptionID (optionID);
			}
			else
			{
				// should never happen unless the job was booked with a price of 0
				curJob.MoneyToCollect = curJob.Type.RetailPrice;
				curJob.MoneyToCollect += GetSurchargeForOptionID (optionID);
			}
		}		

		public void RemoveSurchargeForSaleOption(long optionID)
		{
			Job curJob = SaleOptionsDVC.jrt.CurrentJob;
			curJob.MoneyToCollect -= GetSurchargeForOptionID (optionID);
		}		

		public void ApplyExtraFeeForSaleOption(long optionID)
		{
			Job curJob = SaleOptionsDVC.jrt.CurrentJob;

			if (curJob.EmployeeFee < 1) curJob.EmployeeFee = curJob.Type.EmployeeFee;
			curJob.EmployeeFee += GetExtraFeeForOptionID(optionID);
		}		

		public void RemoveExtraFeeForSaleOption(long optionID)
		{
			Job curJob = SaleOptionsDVC.jrt.CurrentJob;

			if (curJob.EmployeeFee - GetExtraFeeForOptionID(optionID) > 0)
				curJob.EmployeeFee -= GetExtraFeeForOptionID (optionID);
			else curJob.EmployeeFee = 0;
		}

		public double GetSurchargeForOptionID(long optionID)
		{
			foreach(SaleOption option in dbSaleOptions)
				if ( option.ID == optionID)
					return option.CustomerSurcharge;

			return -1;
		}

		public double GetExtraFeeForOptionID(long optionID)
		{
			foreach(SaleOption option in dbSaleOptions)
				if ( option.ID == optionID)
					return option.ExtraFee;
			
			return -1;
		}

		public void ClearTapSelection()
		{
			foreach(SaleOptionBadgeElement element in Root[1])
				element.Accessory = UITableViewCellAccessory.None;
			ReloadData ();
		}

		public void ClearExtras()
		{
			foreach(SaleOptionBadgeElement element in Root[2])
				element.Accessory = UITableViewCellAccessory.None;
			ReloadData ();
		}
	}

	public class InstallTypeBadgeElement : BadgeElement
	{
		public InstallationType ThisInstall;
		public InstallTypeBadgeElement(InstallationType inst, NSAction tapped) : base (inst.Image, inst.Name, tapped)
		{
			ThisInstall = inst;
		}
	}

	public class ProductBadgeElement : BadgeElement
	{
		public Product ThisProduct;
		public ProductBadgeElement(Product product, NSAction tapped) : base (product.Image, product.Name, tapped)
		{
			ThisProduct = product;
		}
	}

	public class SaleOptionBadgeElement : BadgeElement
	{
		public SaleOption ThisOption;
		public SaleOptionBadgeElement(SaleOption option, NSAction tapped) : base (option.Image, option.Name, tapped)
		{
			ThisOption = option;
		}
	}

	public class InstallationType 
	{
		public long ID { get; set; }
		public string Name { get; set; }
		public UIImage Image { get; set; }
		public long PartsLink { get; set; }
	}

	public class Product
	{
		public long ID { get; set; }
		public string Name { get; set; }
		public UIImage Image { get; set; }
		public long PartsLink { get; set; }
	}

	public class SaleOption
	{
		public bool Active { get; set; }
		public long ID { get; set; }
		public string Name { get; set; }
		public string Type { get; set; }
		public long PartLink { get; set; }
		public double CustomerSurcharge { get; set; }
		public double ExtraFee { get; set; }
		public UIImage Image { get; set; }
	}
	
	public class JobInstallationViewController : UsedPartsViewController
	{
		public SaleOptionsDVC dvcSO;

		public void ResetToDefaults()
		{
			ClearPartsList ();
			SetCurrentJobFeeToDefault ();
			EntryElement pressureElement = (this.Root[0].Elements[0] as EntryElement);
			pressureElement.Value = String.Empty;
			dvcSO = new SaleOptionsDVC(null, false, true, this.NavUsedParts.Tabs._jobRunTable, this);
		}

		public void GoChooseProduct()
		{
			ClearPartsList ();
			SetCurrentJobFeeToDefault ();
			dvcSO.ClearTapSelection ();
			dvcSO.ClearExtras ();
			var dvcProducts = new ProductsDVC(null, true, true, this.NavUsedParts.Tabs._jobRunTable, this);
			NavigationController.PushViewController (dvcProducts, true);
		}

		public void GoChooseInstallType()
		{
			var dvcInstallTypes = new InstallTypesDVC(null, true, true, this.NavUsedParts.Tabs._jobRunTable, this);
			NavigationController.PushViewController (dvcInstallTypes, true);
		}

		public void GoChooseSaleOptions()
		{
			// var dvcSaleOptions = new SaleOptionsDVC(null, true, true, this.NavUsedParts.Tabs._jobRunTable, this);
			// NavigationController.PushViewController (dvcSaleOptions, true);

			NavigationController.PushViewController (this.dvcSO, true);
		}

		public void AddPartsRangeToList(long rangeNumber)
		{
			SetPartsToBuildNumber (rangeNumber);
		}

		public void RemovePartsRangeFromList(long rangeNumber)
		{
			RemovePartsByBuildNumber(rangeNumber);
		}

		public virtual void InstallDataGathered()
		{
			this.NavUsedParts.PopToViewController (this.NavUsedParts.ViewControllers[1], true);

			// TRIED using both "this.NavigationController" and "NavigationController" properties without much success, had to resort to NavUsedParts

			//	if (NavigationController != null)
			//		NavigationController.PopToViewController (NavigationController.ViewControllers[1], true);
			//	else
			//		this.NavUsedParts.PopToViewController (this.NavUsedParts.ViewControllers[1], true);
			// NavigationController.PopToViewController (NavigationController.ViewControllers[1], true);		
		}

		public JobInstallationViewController(RootElement root, bool pushing) : base(root, pushing)
		{

		}

		public JobInstallationViewController(RootElement root, WorkflowNavigationController nav, UsedPartsNavigationController upnav, bool pushing) : base(root, pushing)
		{
			NavUsedParts = upnav;
			NavWorkflow = nav;
			DBParts = new List<Part>();
			dvcSO = new SaleOptionsDVC(null, false, true, this.NavUsedParts.Tabs._jobRunTable, this);
			DeactivateEditingMode ();

			Section OptionsSection = new Section("");
			EntryElement pressureElement = new EntryElement("Pressure", "Value", "", false);
			pressureElement.KeyboardType = UIKeyboardType.NumbersAndPunctuation;
			OptionsSection.Add (pressureElement);
			OptionsSection.EntryAlignment = new SizeF(565, 20);

			/*
			CheckboxElement drillElement = new CheckboxElement("Drilling", false);
			drillElement.Tapped += delegate {
				Job currentJob = NavWorkflow._tabs._jobRunTable.CurrentJob;
				if (drillElement.Value == true)
				{
					// increase the job price by 40
					currentJob.MoneyToCollect += 40;
					Console.WriteLine (String.Format ("CurrentJob.TotalToCollect = {0}", currentJob.TotalToCollect ()));
					NavWorkflow._tabs._customersView.UpdateCustomerInfo (CustomerDetailsUpdatableField.JobPriceTotal, 
					                                                     (currentJob.MoneyToCollect-40).ToString (), currentJob.MoneyToCollect.ToString (), currentJob.CustomerNumber, currentJob.JobBookingNumber);

					// if current fee value is 0, set it to standard fee for the job type
					if (currentJob.EmployeeFee < 1 || 
					    currentJob.EmployeeFee > currentJob.Type.EmployeeFee)
					{
						currentJob.EmployeeFee = currentJob.Type.EmployeeFee;
					}
					// increase the fee by 49.5
					currentJob.EmployeeFee += 49.5;

					Console.WriteLine (String.Format ("Job fee: {0}, job type (standard fee): {1}", NavWorkflow._tabs._jobRunTable.CurrentJob.EmployeeFee, NavWorkflow._tabs._jobRunTable.CurrentJob.Type.EmployeeFee));
				}
				else
				{
					// decrease the fee if it's exceeding the standard fee for the job by 49.5

					if (currentJob.EmployeeFee - currentJob.Type.EmployeeFee > 49)
						currentJob.EmployeeFee -= 49.5;

					Console.WriteLine (String.Format ("Job fee: {0}, job type (standard fee): {1}", currentJob.EmployeeFee, currentJob.Type.EmployeeFee));
				}
			}; */
			// OptionsSection.Add (drillElement);

			Root.Add(OptionsSection); 
			
			Section InstallationTypeSection = new Section("Installation information");

			var img = UIImage.FromBundle ("Images/181-hammer");
			InstallationTypeSection.Add(new BadgeElement(img, "Enter info", delegate {
				GoChooseProduct ();
			}));
			img.Dispose ();

			Root.Add(InstallationTypeSection);

			PartsSection UsedPartsSection = new PartsSection("Parts used", this);
			UsedPartsSection.Add (new StyledStringElement("Tap here to add a part"));
			Root.Add (UsedPartsSection);
			
			this.Title = "Installation";
			using (var image = UIImage.FromBundle ("/Images/181-hammer") ) this.TabBarItem.Image = image;
			
			ToolbarItems = new UIBarButtonItem[] {
				new UIBarButtonItem(UIBarButtonSystemItem.Reply),
				new UIBarButtonItem(UIBarButtonSystemItem.FlexibleSpace),
				new UIBarButtonItem("Clear parts list", UIBarButtonItemStyle.Bordered, delegate { ClearPartsList (); }),
				new UIBarButtonItem(UIBarButtonSystemItem.FlexibleSpace),
				new UIBarButtonItem(UIBarButtonSystemItem.Action)			
			};

			ToolbarItems[0].Clicked += delegate {
				if (NavUsedParts.Tabs._jobRunTable.CurrentJob.HasParent ())	// for child jobs, we jump to payment screen
				 	NavUsedParts.Tabs.SelectedViewController = NavUsedParts.Tabs.ViewControllers[3];
				else // for main jobs we jump to pre-plumbing check
					NavUsedParts.Tabs.SelectedViewController = NavUsedParts.Tabs.ViewControllers[0];
			};
			
			ToolbarItems[4].Clicked += delegate {
				// various checks here: pressure has to be entered and correct, parts list must not be empty
				if ( PressureValueOK () )
				{
					SavePressureValueToDatabase ();

					if ( PartsListNotEmpty () )
					{
						// all good, jump to payment
						NavUsedParts.Tabs.LastSelectedTab = NavUsedParts.Tabs.SelectedIndex;
						NavUsedParts.Tabs.SelectedViewController = NavUsedParts.Tabs.ViewControllers[3]; // jump to payment
					}
					else
					{
						// alert the user that install data must be entered 
						var enterInstallData = new UIAlertView("", "Parts list is empty, how come?", null, "OK");
						enterInstallData.Show ();
					}
				}
				else
				{
					// alert the user that pressure value must be entered
					var pressureNotEntered = new UIAlertView("", "Cannot parse pressure value. Please enter a number.", null, "OK");
					pressureNotEntered.Show ();
					this.Root[0].Elements[0].Selected (this, this.TableView, NSIndexPath.FromRowSection (0,0));
				}
			};
		}

		public bool PressureValueOK()
		{
			if ((Root[0].Elements[0] as EntryElement).Value != String.Empty)
			{
				double pressure;
				try {
					if (double.TryParse ((Root[0].Elements[0] as EntryElement).Value, out pressure))
				    	return true;
					else return false;
				}
				catch { return false; }
			}
			else return false;
		}

		public bool PartsListNotEmpty()
		{
			return Root[2].Elements.Count > 1;
		}

		public void SavePressureValueToDatabase()
		{
			double pressure;
			double.TryParse ((Root[0].Elements[0] as EntryElement).Value, out pressure);

			string dbPath = MyConstants.DBReceivedFromServer;
			using (var connection = new SqliteConnection("Data Source="+dbPath))
			{
				connection.Open ();
				using (var cmd = connection.CreateCommand())
				{
					cmd.CommandText = "UPDATE WSALES SET PRESSURE = ? WHERE UNITNUM = ?";
					cmd.Parameters.Add ("@PressureValue", System.Data.DbType.Double).Value = pressure;
					cmd.Parameters.Add ("@UnitNumber", System.Data.DbType.Double).Value = this.NavUsedParts.Tabs._jobRunTable.CurrentJob.UnitNumber;
					try {
						cmd.ExecuteNonQuery ();
					}
					catch (Exception e) {
						NavUsedParts.Tabs._scView.Log ("Failed to update WSALES table (SET PRESSURE = "+pressure.ToString ()+") in "+Path.GetFileName (dbPath));
						NavUsedParts.Tabs._scView.Log (String.Format ("Exception: {0}, Source: {1}", e.Message, e.Source));
					}
				}
			}
		}

		public void SetCurrentJobFeeToDefault()
		{
			Job curJob = this.NavUsedParts.Tabs._jobRunTable.CurrentJob;
			if (curJob != null) 
				curJob.EmployeeFee = curJob.Type.EmployeeFee;
		}

		public override void Selected (NSIndexPath indexPath)
		{
			/*
			if (indexPath.Section == 0 && indexPath.Row == 0) {
				var gc = new GetChoicesForObject("Please choose an installation type", InstallationType);
				gc.Dismissed += delegate {
					((StyledStringElement)Root[0].Elements[0]).Caption = "Installation type chosen";
					((StyledStringElement)Root[0].Elements[0]).Value = InstallationType.OutputString ();
					ReloadData();		
				};
				gc.ShowInView (this.View);
			}*/

			// we will have to handle adding parts in this class instead of its parent
			base.Selected (indexPath); // this handles adding parts to list
		}
		
		public override void ViewWillAppear (bool animated)
		{
			NavigationItem.HidesBackButton = true;
			NavUsedParts.Title = this.Title;
			NavUsedParts.TabBarItem.Image = this.TabBarItem.Image;
			base.ViewWillAppear (animated);
		}

		public override void ViewDidAppear (bool animated)
		{
			base.ViewDidAppear (animated);
			/*
			if (NavWorkflow.Toolbar.Hidden) 
				NavWorkflow.SetToolbarHidden (false, animated);
			NavWorkflow.SetToolbarButtons (WorkflowToolbarButtonsMode.Installation);
			*/
		}
		
		public override bool ShouldAutorotateToInterfaceOrientation (UIInterfaceOrientation toInterfaceOrientation)
		{
			// Return true for supported orientations
			return (toInterfaceOrientation == UIInterfaceOrientation.LandscapeLeft || toInterfaceOrientation == UIInterfaceOrientation.LandscapeRight);
		}
	}
}

