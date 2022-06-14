namespace OsuDiffCalc.UserInterface.Controls {
	using System.Globalization;

	/// <summary>
	/// Validating TextBox which only accepts valid <see cref="double"/> input
	/// </summary>
	public class DoubleTextBox : NumericTextBox<double> {
		private bool _textValidating = false;

		protected override void OnTextValidating(TextValidatingEventArgs e) {
			if (_textValidating) return;
			base.OnTextValidating(e);
			try {
				_textValidating = true;
				e.IsValid = double.TryParse(e.NewText, _numberStyle, CultureInfo.CurrentUICulture, out var value);
				if (e.IsValid)
					Value = value;
			}
			finally {
				_textValidating = false;
			}
		}
	}
}
