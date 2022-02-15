namespace OsuDiffCalc.UserInterface {
	using System.Globalization;

	public abstract class NumericTextBox<T> : ValidatingTextBox {
		private protected NumberStyles _numberStyle;

		public NumericTextBox(bool allowNegative = true) : base() {
			AllowNegative = allowNegative;
		}

		public bool AllowNegative {
			get => _numberStyle.HasFlag(NumberStyles.AllowLeadingSign);
			set => _numberStyle = value
				? NumberStyles.Float
				: NumberStyles.AllowLeadingWhite | NumberStyles.AllowTrailingWhite | NumberStyles.AllowExponent | NumberStyles.AllowDecimalPoint;
		}

		/// <summary>
		/// Current or last valid input value
		/// </summary>
		public T Value { get; private protected set; }
	}
}
