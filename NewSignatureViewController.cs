using MonoTouch.GLKit;
using MonoTouch.UIKit;
using MonoTouch.CoreGraphics;

using System;
using System.IO;
using System.Drawing;
using System.Runtime.InteropServices;

using MonoTouch.Foundation;

using MonoTouch.OpenGLES;
using OpenTK;
using OpenTK.Graphics;
using OpenTK.Graphics.ES20;

namespace Puratap
{
	public class NewSignatureViewController : UIViewController
	{
		public DetailedTabs Tabs { get; set; }		
		public bool hasBeenSigned;

		public EventHandler ClearSignature { get; set; }
		
		private SignableDocuments _mode;
		public SignableDocuments Mode { 
			get { return _mode; } 
			set { 
				_mode = value;
				switch (_mode)
				{
				case SignableDocuments.PrePlumbingCheck: this.Title = "Sign pre-plumbing check sheet"; break;
				case SignableDocuments.ServiceReport: this.Title = "Sign service report form"; break;
				case SignableDocuments.Receipt: this.Title = "Sign receipt"; break;
				}
			} 
		}
		
		private bool _signingMode;
		public bool SigningMode { get { return _signingMode; } set { _signingMode = value; }	}
		
		private UIWebView pdfView;
		
		public GLSignatureView Signature; 	// BezierSignatureView is working now, SignatureView is a previous implementation, the laggy one and should not be used anymore
		public UIWebView PDFView  { get { return pdfView; } set { pdfView = value; } }
		
		public NewSignatureViewController (DetailedTabs tabs) : base ()
		{
			this.Tabs = tabs;			
			// PDFView = new UIWebView(new RectangleF(0,20,703,748));
			pdfView = new UIWebView (new RectangleF(0, 64, 703, 448)); //(20,40,663,448));

			Signature = new GLSignatureView(new RectangleF(0,516,743,150), this); // new BezierSignatureView
		}
		
		public override void ViewDidLoad ()
		{
			base.ViewDidLoad ();
			this.View.Add (pdfView);
			this.View.Add (Signature);
			this.SigningMode = false;
		}

		[Obsolete]
		public override void ViewDidUnload ()
		{
			base.ViewDidUnload ();
			// Release any retained subviews of the main view.
			// e.g. this.myOutlet = null;
		}

		[Obsolete]
		public override bool ShouldAutorotateToInterfaceOrientation (UIInterfaceOrientation toInterfaceOrientation)
		{
			// Return true for supported orientations
			return true;
		}
	}

	[Register]
	public class BezierSignatureView : UIView
	{		
		NewSignatureViewController nsvc;

		private PointF touchLocation;
		private PointF prevTouchLocation;
		private CGPath drawPath;

		private UIPanGestureRecognizer panner = null;

		[Export ("BezierSignatureViewPan")]
		protected void pan(UIPanGestureRecognizer sender)
		{
			if (nsvc.SigningMode == true) {
				this.touchLocation = sender.LocationInView (this);
				PointF mid = BezierSignatureView.MidPoint (this.touchLocation, this.prevTouchLocation);

				switch (sender.State) {
				case UIGestureRecognizerState.Began:
					{ 
						this.drawPath.MoveToPoint (this.touchLocation);
						break; 
					}
				case UIGestureRecognizerState.Changed:
					{
						this.drawPath.AddQuadCurveToPoint (this.prevTouchLocation.X, this.prevTouchLocation.Y, mid.X, mid.Y);					
						this.nsvc.hasBeenSigned = true;
						break; 
					}
				default:
					{
						break; }

				}
				this.prevTouchLocation = this.touchLocation;
				this.SetNeedsDisplay ();
			}
		}

		public void Clear ()
		{
			drawPath.Dispose ();
			drawPath = new CGPath ();
			SetNeedsDisplay ();			
		}

		public BezierSignatureView (RectangleF frame, NewSignatureViewController root) : base(frame)
		{
			this.nsvc = root;
			this.drawPath = new CGPath ();
			this.BackgroundColor = UIColor.Yellow;
			this.MultipleTouchEnabled = false;

			panner = new UIPanGestureRecognizer (this, new MonoTouch.ObjCRuntime.Selector("BezierSignatureViewPan"));
			panner.MaximumNumberOfTouches = panner.MinimumNumberOfTouches = 1;
			this.AddGestureRecognizer (panner);
		}

		public UIImage GetDrawingImage()
		{
			UIImage returnImg = null;

			UIGraphics.BeginImageContext (this.Bounds.Size);
			using (CGContext context = UIGraphics.GetCurrentContext()) 
			{
				context.SetStrokeColor (UIColor.Black.CGColor);
				context.SetLineWidth (5f);
				context.AddPath (this.drawPath);
				context.StrokePath ();
				returnImg = UIGraphics.GetImageFromCurrentImageContext ();
			}
			UIGraphics.EndImageContext ();
			return returnImg;
		}

		public override void Draw (RectangleF rect)
		{
			using (CGContext context = UIGraphics.GetCurrentContext()) 
			{
				context.SetStrokeColor (UIColor.Black.CGColor);
				context.SetLineWidth (5f);
				context.AddPath (this.drawPath);
				context.StrokePath ();
			}
		}

		public static PointF MidPoint(PointF p0, PointF p1) {
			return new PointF() {
				X = (float)((p0.X + p1.X) / 2.0),
				Y = (float)((p0.Y + p1.Y) / 2.0)
			};
		}

	}

	[Register]
	public class GLSignatureView : GLKView
	{
		// Vertex structure containing 3D point and color
		public struct NICSignaturePoint {
			public Vector3 Vertex;
			public Vector3 Color;
		}
		
		#region Utility constants

		// Defines
		public const double STROKE_WIDTH_MIN = 0.002;			// Stroke width is determined by touch velocity
		public const double STROKE_WIDTH_MAX = 0.010;

		// Low pass filter alpha
		public const double STROKE_WIDTH_SMOOTHING = 0.5;		

		public const float VELOCITY_CLAMP_MIN = 20;
		public const float VELOCITY_CLAMP_MAX = 5000;

		// Minimum distance to make a curve
		public const double QUADRATIC_DISTANCE_TOLERANCE = 3.0;	
		public const int MAXIMUM_VERTECES = 100000;

		// Maximum verteces in signature
		private const int maxLength = MAXIMUM_VERTECES;

		public static Vector3 StrokeColor = new Vector3(0f, 0f, 0f);	// OpenTK.Vector3 cannot be declared const

		#endregion



		#region Fields

		// OpenGL state
		EAGLContext context;
		GLKBaseEffect effect;

		uint vertexArray;
		uint vertexBuffer;
		uint dotsArray;
		uint dotsBuffer;

		// Array of verteces, with current length
		NICSignaturePoint[] SignatureVertexData = new NICSignaturePoint[maxLength];
		uint vertexLength;

		// Array of dots, with current length
		NICSignaturePoint[] SignatureDotsData = new NICSignaturePoint[maxLength];
		uint dotsLength;

		// Width of line at current and previous vertex
		float penThickness;
		float previousThickness;

		// Previous points for quadratic bezier computations
		PointF previousPoint;
		PointF previousMidPoint;
		NICSignaturePoint previousVertex;

		// NICSignaturePoint currentVelocity;

		// Gesture recongnizers
		UIPanGestureRecognizer panner;
		UITapGestureRecognizer tapper;

		// Reference to view controller
		NewSignatureViewController nsvc;

		#endregion


		#region Implementation

		private byte[] SigPointBytes(NICSignaturePoint point)
		{
			// Returns raw signature point data as byte array

			int rawsize = Marshal.SizeOf (typeof(NICSignaturePoint));
			IntPtr buffer = Marshal.AllocHGlobal(rawsize);
			Marshal.StructureToPtr(point, buffer, false);
			byte[] rawdata = new byte[rawsize];
			Marshal.Copy(buffer, rawdata, 0, rawsize);
			Marshal.FreeHGlobal(buffer);

			return rawdata;
		}

		unsafe private void AddVertex(uint* length, NICSignaturePoint v)
		{
			if (*length >= maxLength) { return; }

			IntPtr data = GL.Oes.MapBuffer (All.ArrayBuffer, All.WriteOnlyOes);

			byte[] pointBytes = SigPointBytes (v);
			int offset = (int)(pointBytes.Length * (*length));
			IntPtr targetAddress = IntPtr.Add (data, offset);
			Marshal.Copy( pointBytes, 0, targetAddress, pointBytes.Length);

			GL.Oes.UnmapBuffer (All.ArrayBuffer);
			(*length) ++;
		}

		private PointF QuadraticPointInCurve(PointF start, PointF end, PointF controlPoint, float percent) {
			double a = Math.Pow ((1.0 - percent), 2.0);
			double b = 2.0 * percent * (1.0 - percent);
			double c = Math.Pow (percent, 2.0);

			return new PointF { 
				X = (float) (a * start.X + b * controlPoint.X + c * end.X),
				Y = (float) (a * start.Y + b * controlPoint.Y + c * end.Y)
			};
		}

		private float GenerateRandom(float fro, float to) {
			Random r = new Random ();
			return (float) (fro + (r.NextDouble () * (to - fro)));
		}

		private float Clamp(float min, float max, float value) {
			return Math.Max (min, Math.Min (max, value));
		}

		// Calculates perpendicular vector from two other vectors to compute triangle strip around line
		private Vector3 Perpendicular(NICSignaturePoint p1, NICSignaturePoint p2) {
			Vector3 pVector = new Vector3();
			pVector.X = p2.Vertex.Y - p1.Vertex.Y;
			pVector.Y = -1 * (p2.Vertex.X - p1.Vertex.X);
			pVector.Z = 0;
			return pVector;
		}

		private NICSignaturePoint ViewPointToGL (PointF viewPoint, RectangleF bounds, Vector3 color) {
			NICSignaturePoint GLPoint = new NICSignaturePoint ();
			GLPoint.Vertex.X = (float) (viewPoint.X / bounds.Size.Width * 2.0 - 1);
			GLPoint.Vertex.Y = (float) ( ((viewPoint.Y / bounds.Size.Height) * 2.0 - 1) * (-1) );
			GLPoint.Vertex.Z = 0;
			GLPoint.Color = color;

			return GLPoint;
		}

		unsafe private void AddTriangleStripPointsForPreviousPoint(NICSignaturePoint previous, NICSignaturePoint next){
			float toTravel = this.penThickness / 2.0f;

			for (int i = 0; i < 2; i++) {
				Vector3 p = this.Perpendicular (previous, next);
				Vector3 p1 = next.Vertex;
				Vector3 pref = Vector3.Add (p1, p);

				float difX = p1.X - pref.X;
				float difY = p1.Y - pref.Y;
				float distance = (float) Math.Sqrt (Math.Pow (difX, 2) + Math.Pow (difY, 2) + Math.Pow (p1.Z-pref.Z, 2)); // 3d distance between p1 and reference point
				float ratio = -1.0f * (toTravel / distance);

				difX = difX * ratio;
				difY = difY * ratio;

				NICSignaturePoint stripPoint = new NICSignaturePoint ();
				stripPoint.Color = GLSignatureView.StrokeColor;
				stripPoint.Vertex = new Vector3 { X = p1.X+difX, Y = p1.Y+difY, Z = 0.0f };

				fixed(uint* pvl = &vertexLength) {
					this.AddVertex (pvl, stripPoint);
				}
				toTravel *= -1;
			}
		}
			
		// Constructor(s)
		public GLSignatureView(RectangleF frame, NewSignatureViewController root) : base(frame)
		{
			nsvc = root;
			this.EnableSetNeedsDisplay = true;
			this.BackgroundColor = UIColor.Yellow;

			// set up OpenGL context
			this.context = new EAGLContext (EAGLRenderingAPI.OpenGLES2);
			this.Context = this.context;

			if (this.context != null) {
				this.DrawableDepthFormat = GLKViewDrawableDepthFormat.Format24;

				// Turn on antialiasing
				this.DrawableMultisample = GLKViewDrawableMultisample.Sample4x;

				// More OpenGL setup
				this.SetupGL ();

				// set up gesture recognizers
				panner = new UIPanGestureRecognizer (this, new MonoTouch.ObjCRuntime.Selector ("GLSignatureViewPan"));
				panner.MaximumNumberOfTouches = panner.MinimumNumberOfTouches = 1;
				this.AddGestureRecognizer (panner);

				tapper = new UITapGestureRecognizer (this, new MonoTouch.ObjCRuntime.Selector ("GLSignatureViewTap"));
				this.AddGestureRecognizer (tapper);
			} else
				throw new Exception ("Failed to create OpenGL ES2 context");
		}

		// Gesture recognizer actions
		[Export ("GLSignatureViewTap")]
		unsafe protected void tap(UITapGestureRecognizer sender)
		{
			if (nsvc.SigningMode) {
				PointF l = sender.LocationInView (this);
				if (sender.State == UIGestureRecognizerState.Recognized) {

					this.nsvc.hasBeenSigned = true;

					GL.BindBuffer (BufferTarget.ArrayBuffer, dotsBuffer);
					NICSignaturePoint touchPoint = this.ViewPointToGL (l, this.Frame, new Vector3 { X = 1.0f, Y = 1.0f, Z = 1.0f });

					fixed(uint* pdl = &dotsLength) {
						this.AddVertex (pdl, touchPoint);
					}

					NICSignaturePoint centerPoint = touchPoint;
					centerPoint.Color = GLSignatureView.StrokeColor;

					fixed(uint* pdl = &dotsLength) {
						this.AddVertex (pdl, centerPoint);
					}

					const int segments = 20;
					Vector2 radius = new Vector2 {
						X = penThickness * this.GenerateRandom (0.5f, 1.0f),
						Y = penThickness * this.GenerateRandom (0.5f, 1.0f)
					};
					Vector2 velocityRadius = radius;
					double angle = 0;

					// Our view height is much less than width, for them dots to be more roundy
					float uncompressY = this.Frame.Width / this.Frame.Height;

					for (int i = 0; i <= segments; i++) {
						NICSignaturePoint p = centerPoint;
						p.Vertex.X += velocityRadius.X * ((float)Math.Cos (angle));
						p.Vertex.Y += velocityRadius.Y * ((float)Math.Sin (angle)) * uncompressY;

						fixed(uint* pdl = &dotsLength) {
							this.AddVertex (pdl, p);
						}
						fixed(uint* pdl = &dotsLength) {
							this.AddVertex (pdl, centerPoint);
						}

						angle += Math.PI * 2.0f / segments;
					}

					fixed(uint* pdl = &dotsLength) {
						this.AddVertex (pdl, touchPoint);
					}
					GL.BindBuffer (BufferTarget.ArrayBuffer, 0);
				}
				
				this.SetNeedsDisplay ();
			} // end if the corresponding view controller is in Signing mode
		}

		[Export ("GLSignatureViewPan")]
		unsafe protected void pan(UIPanGestureRecognizer sender)
		{
			if (nsvc.SigningMode) {
				GL.BindBuffer (BufferTarget.ArrayBuffer, vertexBuffer);

				PointF vel = sender.VelocityInView (this);
				PointF loc = sender.LocationInView (this);

				// currentVelocity = this.ViewPointToGL (vel, this.Frame, GLSignatureView.StrokeColor);
				float distance = 0.0f;

				if (previousPoint.X > 0) {
					distance = (float) Math.Sqrt ( Math.Pow(loc.X-previousPoint.X, 2) + Math.Pow(loc.Y-previousPoint.Y, 2) );
				}

				float velocityMagnitude = (float) Math.Sqrt (vel.X*vel.X + vel.Y*vel.Y);
				float clampedVelocityMagnitude = this.Clamp (VELOCITY_CLAMP_MIN, VELOCITY_CLAMP_MAX, velocityMagnitude);
				float normalizedVelocity = (clampedVelocityMagnitude - VELOCITY_CLAMP_MIN) / (VELOCITY_CLAMP_MAX - VELOCITY_CLAMP_MIN);
				float lowPassFilterAlpha = (float) STROKE_WIDTH_SMOOTHING;
				float newThickness = (float) ((STROKE_WIDTH_MAX - STROKE_WIDTH_MIN) * (1 - normalizedVelocity) + STROKE_WIDTH_MIN);
				this.penThickness = this.penThickness * lowPassFilterAlpha + newThickness * (1 - lowPassFilterAlpha);

				switch (sender.State) {
				case UIGestureRecognizerState.Began: {
						this.previousPoint = loc;
						this.previousMidPoint = loc;

						NICSignaturePoint startPoint = this.ViewPointToGL (loc, this.Frame, new Vector3 { X = 1, Y = 1, Z = 1 });
						this.previousVertex = startPoint;
						this.previousThickness = penThickness;

						fixed(uint* pvl = &vertexLength) {
							this.AddVertex (pvl, startPoint);
						}
						fixed(uint* pvl = &vertexLength) {
							this.AddVertex (pvl, previousVertex);
						}
		
						this.nsvc.hasBeenSigned = true;

						break;
					}
				case UIGestureRecognizerState.Changed: {
						PointF mid = new PointF ((loc.X + previousPoint.X) / 2.0f, (loc.Y + previousPoint.Y) / 2.0f);

						if (distance > QUADRATIC_DISTANCE_TOLERANCE) {
							// Plot quadratic Bezier line instead of a straight
							uint i;
							int segments = (int)(distance / 1.5f);
							float startPenThickness = this.previousThickness;
							float endPenThickness = this.penThickness;
							this.previousThickness = this.penThickness;

							for (i = 0; i < segments; i++) {
								this.penThickness = startPenThickness + ((endPenThickness - startPenThickness) / segments) * i;

								PointF quadPoint = QuadraticPointInCurve (previousMidPoint, mid, previousPoint, (float)i / (float)segments);
								NICSignaturePoint v = this.ViewPointToGL (quadPoint, this.Frame, GLSignatureView.StrokeColor);
								this.AddTriangleStripPointsForPreviousPoint (this.previousVertex, v);
								this.previousVertex = v;
							}
						} else if (distance > 1.0f) {
							NICSignaturePoint v = this.ViewPointToGL (loc, this.Frame, GLSignatureView.StrokeColor);
							this.AddTriangleStripPointsForPreviousPoint (this.previousVertex, v);
							this.previousVertex = v;
							this.previousThickness = this.penThickness;
						}
						this.previousPoint = loc;
						this.previousMidPoint = mid;

						break;
					}
				case UIGestureRecognizerState.Ended: {
						NICSignaturePoint v = this.ViewPointToGL (loc, this.Frame, new Vector3 { X = 1, Y = 1, Z = 1 });
						fixed (uint* pvl = &vertexLength) {
							this.AddVertex (pvl, v);
						}
						this.previousVertex = v;
						fixed (uint* pvl = &vertexLength) {
							this.AddVertex (pvl, v);
						}

						break;
					}
				case UIGestureRecognizerState.Cancelled: {
						NICSignaturePoint v = this.ViewPointToGL (loc, this.Frame, new Vector3 { X = 1, Y = 1, Z = 1 });
						fixed (uint* pvl = &vertexLength) {
							this.AddVertex (pvl, v);
						}
						this.previousVertex = v;
						fixed (uint* pvl = &vertexLength) {
							this.AddVertex (pvl, v);
						}

						break;
					}
				}
				this.SetNeedsDisplay ();
			} // end if the corresponding ciew controller is in Signing mode

		}

		// Destructor(s)
		protected override void Dispose (bool disposing)
		{
			this.TearDownGL ();
			if (this.context == EAGLContext.CurrentContext) {
				EAGLContext.SetCurrentContext (null);
			}
			this.context = null;

			base.Dispose (disposing);
		}

		bool generatingImage = false; 
		public override void Draw (RectangleF rect)
		{
			if (!generatingImage) 
				// view background is yellow while signing
				GL.ClearColor (1.0f, 1.0f, 0.0f, 1.0f);
			else
				// when generating image, the background should be white
				GL.ClearColor (1.0f, 1.0f, 1.0f, 1.0f);

			GL.Clear (ClearBufferMask.ColorBufferBit);

			this.effect.PrepareToDraw ();

			// draw the lines
			if (vertexLength > 2) {
				GL.Oes.BindVertexArray (vertexArray);
				GL.DrawArrays (BeginMode.TriangleStrip, 0, (int) vertexLength);
			}

			// draw the dots
			if (dotsLength > 0) {
				GL.Oes.BindVertexArray (dotsArray);
				GL.DrawArrays (BeginMode.TriangleStrip, 0, (int) dotsLength);
			}
		}

		// Utility
		public void Clear()
		{
			this.vertexLength = 0;
			this.dotsLength = 0;
			this.SetNeedsDisplay ();
		}

		public UIImage GetDrawingImage()
		{
			this.generatingImage = true;
			this.SetNeedsDisplay ();
			UIImage result = this.Snapshot ();
			this.generatingImage = false;
			this.SetNeedsDisplay ();

			return result;
		}

		#endregion

		// Setup and Teardown
		private void SetupGL()
		{
			// Context
			EAGLContext.SetCurrentContext (this.context);
			this.effect = new GLKBaseEffect ();
			GL.Disable (EnableCap.DepthTest);

			// Signature lines
			GL.Oes.GenVertexArrays (1, out vertexArray);
			GL.Oes.BindVertexArray (vertexArray);

			GL.GenBuffers (1, out vertexBuffer);
			GL.BindBuffer (BufferTarget.ArrayBuffer, vertexBuffer);

			int s = Marshal.SizeOf(typeof(NICSignaturePoint)) * this.SignatureVertexData.Length;
			GL.BufferData (BufferTarget.ArrayBuffer, (IntPtr) s, this.SignatureVertexData, BufferUsage.DynamicDraw);
			this.BindShaderAttributes ();

			// Signature dots
			GL.Oes.GenVertexArrays (1, out dotsArray);
			GL.Oes.BindVertexArray (dotsArray);

			GL.GenBuffers (1, out dotsBuffer);
			GL.BindBuffer (BufferTarget.ArrayBuffer, dotsBuffer);

			s = Marshal.SizeOf (typeof(NICSignaturePoint)) * this.SignatureDotsData.Length;
			GL.BufferData (BufferTarget.ArrayBuffer, (IntPtr)s, this.SignatureDotsData, BufferUsage.DynamicDraw);
			this.BindShaderAttributes ();

			GL.Oes.BindVertexArray (0);

			// Perspective
			Matrix4 ortho = Matrix4.CreateOrthographic (2.0f, 2.0f, 0.1f, 2.0f);
			effect.Transform.ProjectionMatrix = ortho;
			Matrix4 modelViewMatrix = Matrix4.CreateTranslation (new Vector3 { X = 0.0f, Y = 0.0f, Z = -1.0f });
			effect.Transform.ModelViewMatrix = modelViewMatrix;

			vertexLength = 0;
			dotsLength = 0;
			penThickness = 0.004f;
			previousPoint = new PointF { X = -100, Y = -100 };
		}

		private void TearDownGL()
		{
			EAGLContext.SetCurrentContext (this.context);
			GL.DeleteBuffers (1, ref vertexBuffer);
			GL.Oes.DeleteVertexArrays (1, ref vertexArray);

			this.effect = null;
		}

		private void BindShaderAttributes()
		{
			GL.EnableVertexAttribArray ( (int) GLKVertexAttrib.Position);
			GL.VertexAttribPointer ((int)GLKVertexAttrib.Position, 3, VertexAttribPointerType.Float, false, Marshal.SizeOf(typeof(NICSignaturePoint)), 0);
			GL.EnableVertexAttribArray ( (int) GLKVertexAttrib.Color);
			GL.VertexAttribPointer ((int)GLKVertexAttrib.Color, 3, VertexAttribPointerType.Float, false, 6 * sizeof(float), 12);
		}
	}
}

