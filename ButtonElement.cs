using System;
using MonoTouch.Dialog;
using MonoTouch.UIKit;
using System.Drawing;
using MonoTouch.Foundation;
using MonoTouch.CoreAnimation;

namespace Application {
	public class ButtonElement : Element
	{
		static NSString skey = new NSString ("ButtonElement");
		public UITextAlignment Alignment = UITextAlignment.Center;
		public event NSAction Tapped;
		public UIColor Color;
		public ButtonElement (string caption,UIColor color) : base (caption)
		{
			Color = color;
		}
		public ButtonElement (string caption,UIColor color, NSAction tapped) : base (caption)
		{
			Color = color;
			Tapped += tapped;
		}
				
		public override UITableViewCell GetCell (UITableView tv)
		{
			var cell = tv.DequeueReusableCell (skey) as ButtonCellView;
			if (cell == null)
				cell = new ButtonCellView(this);
			else
				cell.UpdateFrom(this);
			return cell;
		}
	
		public override string Summary ()
		{
			return Caption;
		}
		
		public override void Selected (DialogViewController dvc, UITableView tableView, NSIndexPath indexPath)
		{
			tableView.DeselectRow (indexPath, true);
		}
		
		public class ButtonCellView : UITableViewCell {
			UIGlassyButton btn;		
			ButtonElement parent;
					
			public ButtonCellView (ButtonElement element) : base (UITableViewCellStyle.Value1, skey)
			{
				parent = element;
				this.BackgroundColor = UIColor.Clear;
				btn = new UIGlassyButton(RectangleF.Empty);
				btn.Color = parent.Color;
				btn.Title = element.Caption;
				btn.TouchUpInside += delegate{
					if(parent.Tapped != null)
						parent.Tapped();
				};
				ContentView.Add (btn);
			} 
			public override void LayoutSubviews ()
			{
				
				base.LayoutSubviews ();
				btn.Frame = ContentView.Bounds;
			}
			
			public void UpdateFrom (ButtonElement element)
			{
				btn.Title = element.Caption;
				parent = element;
				btn.Color = element.Color;
			}
			
		}
	}
	
	public class UIGlassyButton : UIButton
	{
		private bool _Initialized;
	
		public UIColor Color { get; set; }		
		public UIColor HighlightColor { get; set; }
	
		public string _Title = string.Empty;
		public new string Title 
		{ 
			get { return _Title; } 
			set 
			{ 
				_Title = value;
	
				SetNeedsDisplay();
			} 
		}
		
		public UIGlassyButton(RectangleF rect): base(rect)
		{
			Color = UIColor.FromRGB(88f, 170f, 34f);
			HighlightColor = UIColor.Black;
		}
		
		public void Init(RectangleF rect)
		{
			Layer.MasksToBounds = true;
			Layer.CornerRadius = 8;
			
			var gradientFrame = rect;
			
			var shineFrame = gradientFrame;
			shineFrame.Y += 1;
			shineFrame.X += 1;
			shineFrame.Width -= 2;
			shineFrame.Height = (shineFrame.Height / 2);
	
			var shineLayer = new CAGradientLayer();
			shineLayer.Frame = shineFrame;
			shineLayer.Colors = new MonoTouch.CoreGraphics.CGColor[] { UIColor.White.ColorWithAlpha (0.75f).CGColor, UIColor.White.ColorWithAlpha (0.10f).CGColor };
			shineLayer.CornerRadius = 8;
			
			var backgroundLayer = new CAGradientLayer();
			backgroundLayer.Frame = gradientFrame;
			backgroundLayer.Colors = new MonoTouch.CoreGraphics.CGColor[] { Color.ColorWithAlpha(0.99f).CGColor, Color.ColorWithAlpha(0.80f).CGColor };
	
			var highlightLayer = new CAGradientLayer();
			highlightLayer.Frame = gradientFrame;
			
			Layer.AddSublayer(backgroundLayer);
			Layer.AddSublayer(highlightLayer);
			Layer.AddSublayer(shineLayer);
		
			VerticalAlignment = UIControlContentVerticalAlignment.Center;
			Font = UIFont.BoldSystemFontOfSize (17);
			SetTitle (Title, UIControlState.Normal);
			SetTitleColor (UIColor.White, UIControlState.Normal);
			
			_Initialized = true;
		}
	
		public override void Draw(RectangleF rect)
		{
			base.Draw(rect);
	
			if(!_Initialized)
				Init(rect);
	
			var highlightLayer = Layer.Sublayers[1] as CAGradientLayer;
			
			if (Highlighted)
			{
				if (HighlightColor == UIColor.Blue) 
				{
					highlightLayer.Colors = new MonoTouch.CoreGraphics.CGColor[] { HighlightColor.ColorWithAlpha(0.60f).CGColor, HighlightColor.ColorWithAlpha(0.95f).CGColor };
				} 
				else 
				{
					highlightLayer.Colors = new MonoTouch.CoreGraphics.CGColor[] { HighlightColor.ColorWithAlpha(0.10f).CGColor, HighlightColor.ColorWithAlpha(0.40f).CGColor };
				}
				
			}
			
			highlightLayer.Hidden = !Highlighted;
		}
		
		public override bool BeginTracking(UITouch uitouch, UIEvent uievent)
		{
			if (uievent.Type == UIEventType.Touches)
			{
				SetNeedsDisplay();
			}
	
			return base.BeginTracking(uitouch, uievent); 
		}
		
		public override void EndTracking(UITouch uitouch, UIEvent uievent)
		{
			if (uievent.Type == UIEventType.Touches)
			{
				SetNeedsDisplay();
			}
	
			base.EndTracking(uitouch, uievent);
		}
	}
}