namespace OsuDiffCalc.UserInterface.Controls {
	using System;
	using System.Globalization;
	using System.ComponentModel;
	using System.Windows.Forms;

	[DesignerCategory("")]
	public abstract class NumericTextBox<T> : ValidatingTextBox {
		private protected NumberStyles _numberStyle;

		public NumericTextBox() : base() {
			AllowNegative = true;
			TextAlign = HorizontalAlignment.Right;
		}

		public NumericTextBox(bool allowNegative) : base() {
			AllowNegative = allowNegative;
			TextAlign = HorizontalAlignment.Right;
		}

		public bool AllowNegative {
			get => _numberStyle.HasFlag(NumberStyles.AllowLeadingSign);
			set => _numberStyle = (
				value
				? NumberStyles.Float
				: NumberStyles.AllowLeadingWhite | NumberStyles.AllowTrailingWhite | NumberStyles.AllowExponent | NumberStyles.AllowDecimalPoint
			);
		}

		/// <summary>
		/// Current or last valid input value
		/// </summary>
		public T Value { get; private protected set; }
	}
}