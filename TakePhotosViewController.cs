using MonoTouch.UIKit;
using MonoTouch.ObjCRuntime;
using System.Drawing;
using System;
using System.IO;
using MonoTouch.Foundation;
using MonoTouch.CoreGraphics;

namespace Puratap
{
	public partial class TakePhotosViewController : UIViewController
	{
		private DetailedTabs _tabs;
		private int _photosCounter;
		private long _customerNumber;
		public int PhotosCounter { get { return _photosCounter; } set { _photosCounter = value; } }
		public long CustomerNumber { get { return _customerNumber; } set { _customerNumber = value; } }

		UIImagePickerController _ipc;
		Action<UIImagePickerController, NSDictionary> HandleFinishedPickingMedia;
		Action<UIImagePickerController> HandleCanceledPickingMedia;
		
		public TakePhotosViewController (DetailedTabs tabs) : base ("TakePhotosViewController", null)
		{
			this._tabs = tabs;
			this.Title = NSBundle.MainBundle.LocalizedString ("Take photos", "Take photos");
			using (var image = UIImage.FromBundle ("Images/86-camera") ) this.TabBarItem.Image = image;
		}

		public override void ViewDidLoad ()
		{
			base.ViewDidLoad ();
			HandleFinishedPickingMedia = new Action<UIImagePickerController, NSDictionary> (
				delegate(UIImagePickerController picker, NSDictionary info) {
					UIImage im = (UIImage)info.ObjectForKey(UIImagePickerController.OriginalImage);
					NSError err;
					string path = Path.Combine (Environment.GetFolderPath (Environment.SpecialFolder.Personal), 
						String.Format ("{0}_{1}_{2}.jpg", _customerNumber.ToString (), 
							DateTime.Now.Date.ToString ("yyyy-MM-dd"),
							_photosCounter.ToString ()));
					_photosCounter++;

					im = TakePhotosViewController.ScaleImage(im, 500);

					im.AsJPEG ().Save (path, true, out err);
					im.SaveToPhotosAlbum (null);
					im.Dispose (); im = null;

					// error handling here according to NSError err
					if (err != null) {
						using (var alert = new UIAlertView ("Error:" + err.LocalizedDescription, "Unable to save image to: " + path, null, "OK", null)) {
							alert.Show ();
						}
						err.Dispose ();
						err = null;
					}

					picker.DismissViewController (true, null); // _ipc.DismissModalViewControllerAnimated (true);
					_tabs.SelectedViewController = _tabs.ViewControllers [_tabs.LastSelectedTab];
					_tabs._jobRunTable.TableView.SelectRow (_tabs._jobRunTable.LastSelectedRowPath, true, UITableViewScrollPosition.None);
				});

			HandleCanceledPickingMedia = new Action<UIImagePickerController> (
				delegate(UIImagePickerController picker) {
					if (! (_tabs.ViewControllers[_tabs.LastSelectedTab] is TakePhotosViewController))
						_tabs.SelectedViewController = _tabs.ViewControllers [_tabs.LastSelectedTab];
					else 
						_tabs.SelectedViewController = _tabs.ViewControllers[0];

					_tabs._jobRunTable.TableView.SelectRow (_tabs._jobRunTable.LastSelectedRowPath, true, UITableViewScrollPosition.None);
					picker.DismissViewController(true, null);
			});
		}

		public static UIImage ScaleImage(UIImage image, int maxSize)
		{
			UIImage result;
			int width, height;

			using (CGImage imageRef = image.CGImage) {
				CGImageAlphaInfo alphaInfo = imageRef.AlphaInfo;
				CGColorSpace colorSpaceInfo = CGColorSpace.CreateDeviceRGB();

				if (alphaInfo == CGImageAlphaInfo.None) {
					alphaInfo = CGImageAlphaInfo.NoneSkipLast;
				}
					
				width = imageRef.Width;
				height = imageRef.Height;


				if (height >= width) {
					width = (int)Math.Floor((double)width * ((double)maxSize / (double)height));
					height = maxSize;
				} else {
					height = (int)Math.Floor((double)height * ((double)maxSize / (double)width));
					width = maxSize;
				}
					
				CGBitmapContext bitmap;

				if (image.Orientation == UIImageOrientation.Up || image.Orientation == UIImageOrientation.Down) {
					bitmap = new CGBitmapContext(IntPtr.Zero, width, height, imageRef.BitsPerComponent, imageRef.BytesPerRow, colorSpaceInfo, alphaInfo);
				} else {
					bitmap = new CGBitmapContext(IntPtr.Zero, height, width, imageRef.BitsPerComponent, imageRef.BytesPerRow, colorSpaceInfo, alphaInfo);
				}

				switch (image.Orientation) {
					case UIImageOrientation.Left:
						bitmap.RotateCTM((float)Math.PI / 2);
						bitmap.TranslateCTM(0, -height);
						break;
					case UIImageOrientation.Right:
						bitmap.RotateCTM(-((float)Math.PI / 2));
						bitmap.TranslateCTM(-width, 0);
						break;
					case UIImageOrientation.Up:
						break;
					case UIImageOrientation.Down:
						bitmap.TranslateCTM(width, height);
						bitmap.RotateCTM(-(float)Math.PI);
						break;
				}

				bitmap.DrawImage(new Rectangle(0, 0, width, height), imageRef);

				result = UIImage.FromImage(bitmap.ToImage());
				bitmap = null;
			}
			return result;
		} 
		
		public override void ViewDidAppear (bool animated)
		{
			base.ViewDidAppear (animated);
			 
			if (Runtime.Arch != Arch.SIMULATOR) {
				_ipc = new UIImagePickerController ();
				_ipc.ModalPresentationStyle = UIModalPresentationStyle.FullScreen;
				_ipc.SourceType = UIImagePickerControllerSourceType.Camera;
				_ipc.ShowsCameraControls = true;
				_ipc.AllowsEditing = false;
				_ipc.Delegate = new ImagePickerDelegate (HandleFinishedPickingMedia, HandleCanceledPickingMedia);

				this.PresentViewController (_ipc, animated, null);
			}
		}

		// [Obsolete]
		public override bool ShouldAutorotateToInterfaceOrientation (UIInterfaceOrientation toInterfaceOrientation)
		{
			// Return true for supported orientations
			return (toInterfaceOrientation == UIInterfaceOrientation.LandscapeLeft || toInterfaceOrientation == UIInterfaceOrientation.LandscapeRight);
		}
	}

	public class ImagePickerDelegate : UIImagePickerControllerDelegate {
		private readonly Action<UIImagePickerController, NSDictionary> _captureEvent;
		private readonly Action<UIImagePickerController> _cancelEvent;

		public ImagePickerDelegate(Action<UIImagePickerController, NSDictionary> captureEvent, 
									Action<UIImagePickerController>cancelEvent)
		{
			_captureEvent = captureEvent;
			_cancelEvent = cancelEvent;
		}

		public override void FinishedPickingMedia(UIImagePickerController picker, NSDictionary info)
		{
			_captureEvent(picker, info);
		}

		public override void Canceled (UIImagePickerController picker)
		{
			_cancelEvent (picker);
		}
	}
}

