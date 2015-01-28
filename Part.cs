using System;
using System.IO;
using UIKit;

namespace Puratap
{
	public class Part
	{
		// this class holds the appropriate part data

		// private double _quantity;
		private int _partNo;
		private string _name;
		private string _description;
		private string _imagePath;
		private UIImage _image;
		private bool _imageNotFound;

		public double Quantity { get; set; }
		public int PartNo { get { return _partNo; } set { _partNo = value; } }
		public string Name { get { return _name; } set { _name = value; } }
		public string Description  { get { return _description; } set {_description = value; } }
		public string ImagePath  { get { return _imagePath; } set { _imagePath = value; } }
		public UIImage Image  { get { return _image; } set { _image = value; _imageNotFound = false; } }
		public bool ImageNotFound { get { return _imageNotFound; } set { _imageNotFound = value; } }

		public static UIImage PlaceholderImage = UIImage.FromBundle ("Images/Puratap_logo_72");
	}

	public class Assembly : Part
	{
		public int aID { get; set; }
		public bool SplitForFranchisees { get; set; }
	}
}

