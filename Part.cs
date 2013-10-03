using System;
using System.IO;
using MonoTouch.UIKit;

namespace Puratap
{
	public class Part
	{
		// this class holds the appropriate part data
		private double _quantity;
		private int _partNo;
		private string _name;
		private string _description;
		private double _price;
		private string _imagePath;
		private UIImage _image;
		private bool _imageNotFound;
		
		public double Quantity { get { return _quantity; } set { _quantity = value; } }
		public int PartNo { get { return _partNo; } set { _partNo = value; } }
		public string Name { get { return _name; } set { _name = value; } }
		public string Description  { get { return _description; } set {_description = value; } }
		public double Price  { get { return _price; }  set { _price = value; } }
		public string ImagePath  { get { return _imagePath; } set { _imagePath = value; } }
		public UIImage Image  { get { return _image; } set { _image = value; _imageNotFound = false; } }
		public bool ImageNotFound { get { return _imageNotFound; } set { _imageNotFound = value; } }
		
		public static UIImage PlaceholderImage = UIImage.FromBundle ("Images/Puratap_logo_72");
	}
}

