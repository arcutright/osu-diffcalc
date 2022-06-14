namespace OsuDiffCalc.UserInterface.Controls {
	using System;
	using System.Collections.Generic;
	using System.ComponentModel;
	using System.Drawing;
	using System.Linq;
	using System.Text;
	using System.Threading.Tasks;
	using System.Windows.Forms;

	/// <summary>
	/// Tooltip that implements extra properties, like a custom font (in exchange for being much slower & hammering the GC)
	/// </summary>
	[DesignerCategory("")]
	internal class CustomToolTip : ToolTip {
		private Font _originalFont = new Font("Segoe UI", 12);
		private Font _font = null;
		private float _xScale = 1, _yScale = 1;
		private string _lastTooltipText = " ";

		public CustomToolTip() {
			_font = _originalFont;
			OwnerDraw = false;
			Draw += CustomToolTip_Draw;
			Popup += CustomToolTip_Popup;
		}

		public Font Font {
			get => _font;
			set {
				if (_font == value) return;
				OwnerDraw = value != _originalFont;
				_font = value;
				UpdateFontScale();
			}
		}

		public string LastToolTipText {
			get => _lastTooltipText;
			private set {
				if (_lastTooltipText == value) return;
				_lastTooltipText = value;
				UpdateFontScale();
			}
		}

		private void UpdateFontScale() {
			if (_font is not null) {
				var defaultFontSize = TextRenderer.MeasureText(_lastTooltipText, _originalFont);
				var currentFontSize = TextRenderer.MeasureText(_lastTooltipText, _font);
				_xScale = Math.Max(1f, (float)currentFontSize.Width / defaultFontSize.Width);
				_yScale = Math.Max(1f, (float)currentFontSize.Height / defaultFontSize.Height);
			}
			else {
				(_xScale, _yScale) = (1, 1);
			}
		}

		private Size _lastToolTipBaseSize, _lastToolTipScaledSize;

		// can only change the tooltip size in the Popup event (comes before Draw)
		private void CustomToolTip_Popup(object sender, PopupEventArgs e) {
			LastToolTipText = this.GetToolTip(e.AssociatedControl);
			if (e.ToolTipSize != _lastToolTipBaseSize) {
				_lastToolTipBaseSize = e.ToolTipSize;
				_lastToolTipScaledSize = new Size(
					(int)(e.ToolTipSize.Width * _xScale) + 1,
					(int)(e.ToolTipSize.Height * _yScale) + 1
				);
			}
			e.ToolTipSize = _lastToolTipScaledSize;
		}

		private void CustomToolTip_Draw(object sender, DrawToolTipEventArgs e) {
			_originalFont = e.Font;

			// draw background
			// e.Graphics.FillRectangle(new SolidBrush(BackColor), e.Bounds);
			e.Graphics.FillRectangle(SystemBrushes.ControlLight, e.Bounds);

			// draw tooltip popup border
			ControlPaint.DrawBorder(e.Graphics, e.Bounds, SystemColors.WindowFrame, ButtonBorderStyle.Solid);

			// draw text
			const TextFormatFlags flags = TextFormatFlags.Left | TextFormatFlags.VerticalCenter;
			var bounds = e.Bounds with { X = 3, Y = 5 };
			TextRenderer.DrawText(e.Graphics, e.ToolTipText, _font, bounds, ForeColor, flags); // font looks good
			// e.Graphics.DrawString(e.ToolTipText, _font, new SolidBrush(ForeColor), e.Bounds, _sf); // font looks jank
		}

		private bool _isDisposed = false;
		protected override void Dispose(bool disposing) {
			if (!_isDisposed && disposing) {
				Font?.Dispose();
				if (Font != _originalFont)
					_originalFont.Dispose();

				Draw -= CustomToolTip_Draw;
				Popup -= CustomToolTip_Popup;

				_isDisposed = true;
			}
			base.Dispose(disposing);
		}
	}
}
